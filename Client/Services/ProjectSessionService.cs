using System.Text.Json;
using Client.Services.Diagnostics;
using Client.Services.HttpServices;
using Client.Services.Security;
using Distillator.Domain.Configuration;
using Distillator.Domain.Inputs;
using Distillator.Domain.Models;
using Distillator.Domain.Persistence;
using Distillator.Domain.Policies;
using Distillator.Domain.Repositories;
using Distillator.Domain.Repositories.InMemory;
using Distillator.Domain.Services;
using Distillator.Domain.Session;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Streams;
using Shared.PropertiesDtos.Methods;
using Shared.Projects;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using UnitSystem;

namespace Client.Services;

/// <summary>
/// Orquesta el flujo de entrada de la aplicación:
/// 1. Usa el usuario ya cargado por CustomAuthenticationStateProvider.
/// 2. Carga el proyecto del usuario: si tiene 1 o más, carga el último activo.
///    Si no tiene proyectos, crea uno nuevo con un PFD por defecto.
/// 3. Expone el proyecto activo y el flowsheet activo a la UI.
/// </summary>
public class ProjectSessionService
{
    private readonly CustomAuthenticationStateProvider _authProvider;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserSessionStateRepository _userSessionStateRepository;
    private readonly IFlowsheetTypeRegistry _flowsheetTypeRegistry;
    private readonly IEquipmentNamingService _namingService;
    private readonly IHttpService? _httpService;
    private readonly ProjectRealtimeService? _realtimeService;
    private readonly ProjectActivityLogService? _activityLog;
    private UserSessionState? _workspaceState;
    private readonly Dictionary<Guid, string> _projectRoles = new();
    private readonly HashSet<Guid> _confirmedDiagramIds = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _visualSaveDebounces = new();
    private readonly object _visualSaveDebounceSync = new();
    private readonly ProjectAutosaveCoordinator<DiagramAutosavePayload> _diagramAutosaveCoordinator = new();
    private readonly ProjectEquipmentHydrationRegistry _equipmentHydrationRegistry = new();
    private readonly ProjectPipeHydrationService _pipeHydrationService = new();
    private readonly ProjectFormulaHydrationService _formulaHydrationService = new();
    private readonly ProjectInterFlowsheetConnectionHydrationService _interFlowsheetConnectionHydrationService = new();
    private readonly ProjectHydrationPublicationGate _hydrationPublicationGate = new();
    private readonly ProjectWorkspaceSelectionService _workspaceSelectionService = new();
    private readonly SemaphoreSlim _realtimeReloadLock = new(1, 1);
    private CancellationTokenSource? _connectionRecoveryCts;
    private bool _isConnectionRecoveryRunning;
    private long _currentProjectVersion;
    private long _lastAppliedRealtimeVersion;
    private long _lastRenderedProjectVersion;
    private Guid? _lastAppliedRealtimeProjectId;
    private ProjectRealtimeEventDto? _deferredRealtimeEvent;
    private static readonly JsonSerializerOptions PersistenceJsonOptions = new(JsonSerializerDefaults.Web);

    public User? CurrentUser => _authProvider.CurrentUser;
    public Project? CurrentProject { get; private set; }
    public IFlowsheet? ActiveFlowsheet { get; private set; }
    public bool IsProjectExplorerCollapsed => _workspaceState?.IsProjectExplorerCollapsed ?? false;
    public bool IsDiagramExplorerCollapsed => _workspaceState?.IsDiagramExplorerCollapsed ?? false;
    public IReadOnlyCollection<string>? ExpandedDiagramTypeCodes => _workspaceState?.ExpandedDiagramTypeCodes;
    public IReadOnlyList<ProjectPresenceDto> ProjectPresence { get; private set; } = Array.Empty<ProjectPresenceDto>();
    public bool IsProjectHydrating { get; private set; }
    public string ProjectLoadingMessage { get; private set; } = string.Empty;
    public AutosaveRevisionState DiagramAutosaveState => _diagramAutosaveCoordinator.State;

    public event Action? ProjectChanged;
    public event Action<Project>? ProjectReloaded;
    public event Action? ProjectListRefreshRequested;
    public event Action? ProjectPresenceChanged;
    public event Action? ProjectHydrationChanged;
    public Func<bool>? IsSimulationRunning { get; set; }
    public Func<bool>? HasActiveVisualOperation { get; set; }

    private sealed record DiagramAutosavePayload(
        Guid ProjectId,
        Guid OperationId,
        IReadOnlyList<ProjectDiagramDto> Diagrams,
        string DiagramIds,
        string SkippedReason = "");

    private sealed record ProjectDocumentLoadResult(
        bool Succeeded,
        bool ConnectionFailed,
        bool AccessDenied,
        ProjectDocumentDto? Document);

    public void NotifyProjectChanged() => ProjectChanged?.Invoke();

    private void SetProjectHydration(bool isHydrating, string message)
    {
        IsProjectHydrating = isHydrating;
        ProjectLoadingMessage = message;
        _activityLog?.Add("Hydration", isHydrating ? message : "Project hydration finished", CurrentProject?.Name);
        ProjectHydrationChanged?.Invoke();
    }

    public bool IsCurrentUserProjectOwner(Project project)
    {
        return CurrentUser != null && project.OwnerUserId == CurrentUser.Id;
    }

    public bool CanCurrentUserEditProject(Project project)
    {
        _projectRoles.TryGetValue(project.Id, out var role);
        return ProjectPermissionPolicy.CanEdit(IsCurrentUserProjectOwner(project), role);
    }

    public bool CanCurrentUserManageProject(Project project)
    {
        _projectRoles.TryGetValue(project.Id, out var role);
        return ProjectPermissionPolicy.CanManage(IsCurrentUserProjectOwner(project), role);
    }

    public ProjectSessionService(
        CustomAuthenticationStateProvider authProvider,
        IProjectRepository projectRepository,
        IUserSessionStateRepository userSessionStateRepository,
        IFlowsheetTypeRegistry? flowsheetTypeRegistry = null,
        IEquipmentNamingService? namingService = null,
        IHttpService? httpService = null,
        ProjectRealtimeService? realtimeService = null,
        ProjectActivityLogService? activityLog = null)
    {
        _authProvider = authProvider;
        _projectRepository = projectRepository;
        _userSessionStateRepository = userSessionStateRepository;
        _flowsheetTypeRegistry = flowsheetTypeRegistry ?? new FlowsheetTypeRegistry();
        _namingService = namingService ?? new EquipmentNamingService();
        _httpService = httpService;
        _realtimeService = realtimeService;
        _activityLog = activityLog;

        if (_realtimeService != null)
        {
            _realtimeService.ProjectChangedReceived += OnRealtimeProjectChanged;
            _realtimeService.Reconnected += OnRealtimeReconnected;
            _realtimeService.PresenceChangedReceived += OnRealtimePresenceChanged;
        }

        _authProvider.CurrentUserChanged += OnCurrentUserChanged;
    }

    public async Task InitializeAsync()
    {
        // 1. Asegurar que el usuario está cargado. Si aún no lo está, forzar la carga.
        if (CurrentUser == null)
            await _authProvider.GetAuthenticationStateAsync();

        if (CurrentUser == null)
            throw new InvalidOperationException("No current user available.");

        var userProjects = await LoadUserProjectsAsync();
        if (userProjects == null)
        {
            return;
        }

        await InitializeFromProjectsAsync(userProjects);
    }

    public async Task InitializeFromProjectsAsync(IReadOnlyList<Project> userProjects)
    {
        await EnsureCurrentUserAsync();
        if (CurrentUser == null)
            throw new InvalidOperationException("No current user available.");

        var session = await LoadWorkspaceStateAsync();
        _workspaceState = session;

        var selection = _workspaceSelectionService.SelectInitialProject(userProjects, _workspaceState);
        if (!selection.HasSelection || selection.Project == null)
        {
            CurrentProject = null;
            ActiveFlowsheet = null;
            ProjectChanged?.Invoke();
            return;
        }

        CurrentProject = selection.Project;
        ActiveFlowsheet = selection.Flowsheet;
        await ConfirmCurrentProjectDocumentAsync(CurrentProject.Id);
        if (_workspaceState != null &&
            (_workspaceState.LastProjectId != CurrentProject.Id || _workspaceState.LastFlowsheetId != ActiveFlowsheet?.Id))
        {
            await SaveSessionAsync(ActiveFlowsheet?.Id);
        }

        await JoinRealtimeProjectAsync(CurrentProject.Id);
        ProjectChanged?.Invoke();
    }

    public async Task SetCurrentProjectAsync(Project project)
    {
        if (CurrentUser == null)
            throw new InvalidOperationException("No current user available.");

        if (project.Flowsheets.Count == 0)
        {
            project.CreateFlowsheet("PFD 1", "PFD");
        }

        var selection = _workspaceSelectionService.SelectProject(project);
        PublishHydratedProject(project, selection.Flowsheet?.Id);
        await ConfirmCurrentProjectDocumentAsync(project.Id);
        await SaveSessionAsync(ActiveFlowsheet?.Id);
        await JoinRealtimeProjectAsync(project.Id);
        ProjectChanged?.Invoke();
    }

    public async Task ClearCurrentSessionAsync()
    {
        ClearCurrentSessionState();

        if (_realtimeService != null)
        {
            await _realtimeService.LeaveCurrentProjectAsync();
        }
    }

    private void OnCurrentUserChanged()
    {
        if (CurrentUser != null) return;

        ClearCurrentSessionState();
    }

    private void ClearCurrentSessionState()
    {
        CurrentProject = null;
        ActiveFlowsheet = null;
        _workspaceState = null;
        _projectRoles.Clear();
        _confirmedDiagramIds.Clear();
        _currentProjectVersion = 0;
        _lastAppliedRealtimeVersion = 0;
        _lastRenderedProjectVersion = 0;
        _lastAppliedRealtimeProjectId = null;
        _deferredRealtimeEvent = null;
        _connectionRecoveryCts?.Cancel();
        ProjectPresence = Array.Empty<ProjectPresenceDto>();

        ProjectPresenceChanged?.Invoke();
        ProjectChanged?.Invoke();
    }

    public async Task<List<Project>?> LoadUserProjectsAsync()
    {
        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return new List<Project>();

        if (_httpService == null)
        {
            var localProjects = await _projectRepository.GetByUserIdAsync(CurrentUser.Id);
            return localProjects.OrderByDescending(project => project.CreatedAt).Cast<Project>().ToList();
        }

        var summariesResult = await _httpService.PostAsync<GetUserProjectsRequest, List<ProjectSummaryDto>>(new GetUserProjectsRequest());
        if (!summariesResult.Succeeded || summariesResult.Data == null)
        {
            if (IsConnectionFailure(summariesResult.Messages))
            {
                ScheduleConnectionRecovery();
            }

            return null;
        }

        _projectRoles.Clear();
        var projects = new List<Project>();
        foreach (var summary in summariesResult.Data.OrderByDescending(project => project.UpdatedOnUtc))
        {
            _projectRoles[summary.Id] = summary.CurrentUserRole;
            var project = await LoadProjectForListAsync(summary.Id);
            if (project != null)
            {
                projects.Add(project);
            }
        }

        return projects;
    }

    public async Task<Project?> LoadProjectAsync(Guid projectId, bool recalculate = true)
    {
        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return null;

        if (_httpService == null)
        {
            var localProjects = await _projectRepository.GetByUserIdAsync(CurrentUser.Id);
            return localProjects.Cast<Project>().FirstOrDefault(project => project.Id == projectId);
        }

        var document = await LoadProjectDocumentAsync(projectId);
        if (document == null)
        {
            return null;
        }

        return await FromPersistenceDtoAsync(document, CurrentUser, recalculate);
    }

    private async Task<Project?> LoadProjectForListAsync(Guid projectId)
    {
        if (CurrentUser == null) return null;

        var document = await LoadProjectDocumentAsync(projectId);
        if (document == null)
        {
            return null;
        }

        return await FromPersistenceDtoAsync(document, CurrentUser, recalculate: true, updateSessionState: false);
    }

    public async Task<ProjectSharingDto?> GetProjectSharingAsync(Guid projectId)
    {
        if (_httpService == null) return null;

        var result = await _httpService.PostAsync<GetProjectSharingRequest, ProjectSharingDto>(new GetProjectSharingRequest
        {
            ProjectId = projectId
        });

        return result.Succeeded ? result.Data : null;
    }

    public async Task<ProjectSharingDto?> UpdateProjectSharingAsync(Guid projectId, IEnumerable<ProjectCollaboratorDto> collaborators)
    {
        if (_httpService == null) return null;

        var result = await _httpService.PostAsync<UpdateProjectSharingRequest, ProjectSharingDto>(new UpdateProjectSharingRequest
        {
            ProjectId = projectId,
            Collaborators = collaborators.ToList()
        });

        return result.Succeeded ? result.Data : null;
    }

    public async Task PersistProjectCreatedAsync(Project project)
    {
        if (_httpService == null) return;

        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return;

        var request = new CreateProjectRequest
        {
            ProjectId = project.Id,
            Name = project.Name,
            Configuration = ToPersistenceDto(project.Configuration)
        };

        var result = await _httpService.PostAsync<CreateProjectRequest, ProjectDocumentDto>(request);
        if (!result.Succeeded)
        {
            return;
        }
        UpdateConfirmedProjectVersion(result.Data);

        foreach (var diagram in ToDiagramDtos(project))
        {
            await PersistDiagramCreatedAsync(project.Id, diagram);
        }
    }

    public async Task SetActiveFlowsheetAsync(IFlowsheet flowsheet)
    {
        if (flowsheet.Project.Id != CurrentProject?.Id)
            throw new InvalidOperationException("Flowsheet does not belong to the current project.");

        var selection = _workspaceSelectionService.SelectProject((Project)flowsheet.Project, flowsheet.Id);
        if (selection.Flowsheet == null)
        {
            throw new InvalidOperationException("Flowsheet does not belong to the current project.");
        }

        ActiveFlowsheet = selection.Flowsheet;
        ProjectChanged?.Invoke();
        await SaveSessionAsync(flowsheet.Id);
        await UpdateRealtimeActiveDiagramAsync();
    }

    public async Task<IFlowsheet> CreateFlowsheetAsync(string typeCode, string? baseName = null, string? diagramNumber = null)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        if (!CanCurrentUserEditProject(CurrentProject))
            throw new InvalidOperationException("Current user cannot edit this project.");

        var type = CurrentProject.FlowsheetTypes.GetByCode(typeCode)
            ?? throw new InvalidOperationException($"Unknown flowsheet type: {typeCode}");

        var name = GenerateUniqueFlowsheetName(typeCode, baseName ?? type.DisplayName);
        var flowsheet = CurrentProject.CreateFlowsheet(name, typeCode);
        SetDiagramNumber(flowsheet, diagramNumber);
        ActiveFlowsheet = flowsheet;
        await SaveSessionAsync(flowsheet.Id);
        await UpdateRealtimeActiveDiagramAsync();
        await PersistDiagramCreatedAsync(CurrentProject.Id, ToDiagramDto(flowsheet, GetFlowsheetOrder(CurrentProject, flowsheet)));
        ProjectChanged?.Invoke();
        return flowsheet;
    }

    public async Task RenameFlowsheetAsync(IFlowsheet flowsheet, string newName)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        if (!CanCurrentUserEditProject(CurrentProject)) return;

        if (flowsheet.Project.Id != CurrentProject.Id)
            throw new InvalidOperationException("Flowsheet does not belong to the current project.");

        var existing = CurrentProject.Flowsheets
            .Where(f => f.TypeCode.Equals(flowsheet.TypeCode, StringComparison.OrdinalIgnoreCase)
                        && f.Id != flowsheet.Id)
            .Any(f => f.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));

        if (existing)
            throw new InvalidOperationException("A flowsheet with that name already exists for this type.");

        flowsheet.Name = newName;
        await SaveSessionAsync(ActiveFlowsheet?.Id);
        await PersistDiagramUpdatedAsync(flowsheet);
        ProjectChanged?.Invoke();
    }

    public async Task UpdateFlowsheetConfigurationAsync(
        IFlowsheet flowsheet,
        string? newName,
        string? diagramNumber,
        double? diagramWidth,
        double? diagramHeight,
        double? globalScale,
        double? gridSize,
        double? zoom,
        double? panX,
        double? panY)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        if (!CanCurrentUserEditProject(CurrentProject)) return;

        if (flowsheet.Project.Id != CurrentProject.Id)
            throw new InvalidOperationException("Flowsheet does not belong to the current project.");

        if (!string.IsNullOrWhiteSpace(newName) && !newName.Equals(flowsheet.Name, StringComparison.OrdinalIgnoreCase))
        {
            var existing = CurrentProject.Flowsheets
                .Where(f => f.TypeCode.Equals(flowsheet.TypeCode, StringComparison.OrdinalIgnoreCase)
                            && f.Id != flowsheet.Id)
                .Any(f => f.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));

            if (existing)
                throw new InvalidOperationException("A flowsheet with that name already exists for this type.");

            flowsheet.Name = newName;
        }

        SetDiagramNumber(flowsheet, diagramNumber);

        if (diagramWidth.HasValue && diagramWidth.Value > 0) flowsheet.DiagramWidth = diagramWidth.Value;
        if (diagramHeight.HasValue && diagramHeight.Value > 0) flowsheet.DiagramHeight = diagramHeight.Value;
        if (globalScale.HasValue && globalScale.Value > 0) flowsheet.GlobalScale = globalScale.Value;
        if (gridSize.HasValue && gridSize.Value > 0) flowsheet.GridSize = gridSize.Value;
        if (zoom.HasValue && zoom.Value > 0) flowsheet.Zoom = zoom.Value;
        if (panX.HasValue) flowsheet.PanX = panX.Value;
        if (panY.HasValue) flowsheet.PanY = panY.Value;

        await SaveSessionAsync(ActiveFlowsheet?.Id);
        await PersistDiagramUpdatedAsync(flowsheet);
        ProjectChanged?.Invoke();
    }

    public async Task<bool> UpdateProjectConfigurationAsync(
        string projectName,
        IProjectConfiguration configuration,
        bool renameExistingEquipment = false,
        IReadOnlyDictionary<Guid, string>? diagramNumberUpdates = null)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        if (!CanCurrentUserEditProject(CurrentProject)) return false;

        ValidateDiagramNumbersForConfiguration(CurrentProject, configuration, diagramNumberUpdates);

        var shouldRunSimulation = ShouldRunSimulationAfterConfigurationChange(CurrentProject.Configuration, configuration);
        var requiresAtomicNamingMigration = renameExistingEquipment ||
                                            (diagramNumberUpdates != null && diagramNumberUpdates.Count > 0);
        ProjectConfigurationDraftSnapshot? rollbackSnapshot = null;
        IReadOnlyList<ProjectDiagramDto>? migratedDiagrams = null;

        if (requiresAtomicNamingMigration)
        {
            rollbackSnapshot = ProjectConfigurationDraftSnapshot.Capture(CurrentProject);
            try
            {
                ApplyConfigurationDraft(CurrentProject, projectName, configuration, renameExistingEquipment, diagramNumberUpdates);
            }
            catch
            {
                rollbackSnapshot.Restore(CurrentProject);
                return false;
            }

            migratedDiagrams = ToDiagramDtos(CurrentProject);
        }

        var savedDocument = await PersistProjectConfigurationAsync(projectName, configuration, migratedDiagrams);
        if (savedDocument == null)
        {
            rollbackSnapshot?.Restore(CurrentProject);
            return false;
        }

        if (!requiresAtomicNamingMigration)
        {
            ApplyConfigurationDraft(CurrentProject, projectName, configuration, renameExistingEquipment, diagramNumberUpdates);
        }

        if (shouldRunSimulation)
        {
            await CurrentProject.RunSimulationAsync();
        }

        await SaveSessionAsync(ActiveFlowsheet?.Id);
        if (!requiresAtomicNamingMigration)
        {
            await PersistDiagramNumbersForNamingAsync(configuration);
        }
        ProjectChanged?.Invoke();
        return true;
    }

    public async Task DeleteFlowsheetAsync(Guid flowsheetId)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        if (!CanCurrentUserEditProject(CurrentProject)) return;

        if (CurrentProject.Flowsheets.Count <= 1)
            return;

        var affectedFlowsheetIds = CurrentProject.InterFlowsheetConnections
            .Where(connection =>
                connection.SourceFlowsheetId == flowsheetId ||
                connection.TargetFlowsheetId == flowsheetId)
            .Select(connection => connection.SourceFlowsheetId == flowsheetId
                ? connection.TargetFlowsheetId
                : connection.SourceFlowsheetId)
            .Distinct()
            .ToList();

        CurrentProject.RemoveFlowsheet(flowsheetId);

        if (ActiveFlowsheet?.Id == flowsheetId)
        {
            ActiveFlowsheet = CurrentProject.Flowsheets.FirstOrDefault();
        }

        await SaveSessionAsync(ActiveFlowsheet?.Id);
        var affectedFlowsheets = affectedFlowsheetIds
            .Select(id => CurrentProject.GetFlowsheet(id))
            .OfType<IFlowsheet>()
            .ToArray();
        if (affectedFlowsheets.Length > 0)
        {
            await PersistDiagramVisualStatesAsync(affectedFlowsheets);
        }

        await PersistDiagramDeletedAsync(CurrentProject.Id, flowsheetId);
        ProjectChanged?.Invoke();
    }

    public async Task<bool> DeleteProjectAsync(Project project)
    {
        if (!CanCurrentUserManageProject(project)) return false;
        if (_httpService == null) return true;

        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return false;

        var result = await _httpService.PostAsync<DeleteProjectRequest>(new DeleteProjectRequest
        {
            ProjectId = project.Id
        });

        return result.Succeeded;
    }

    public async Task ReorderFlowsheetAsync(IFlowsheet flowsheet, int newIndex)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        if (!CanCurrentUserEditProject(CurrentProject)) return;

        CurrentProject.ReorderFlowsheet(flowsheet, newIndex);
        ProjectChanged?.Invoke();
        await SaveSessionAsync(ActiveFlowsheet?.Id);
        await PersistDiagramVisualStatesAsync(CurrentProject.Flowsheets.ToArray());
    }

    public async Task SaveSessionAsync(Guid? lastFlowsheetId = null)
    {
        if (CurrentUser == null || CurrentProject == null) return;

        var session = await GetOrCreateWorkspaceStateAsync();

        session.LastProjectId = CurrentProject.Id;
        if (lastFlowsheetId != null)
            session.LastFlowsheetId = lastFlowsheetId;
        session.LastAccessAt = DateTime.UtcNow;

        await SaveWorkspaceStateAsync(session);
    }

    private Task OnRealtimeProjectChanged(ProjectRealtimeEventDto realtimeEvent)
    {
        _activityLog?.Add("SignalR", "Project change received", $"{realtimeEvent.ChangeType} v{realtimeEvent.Version}");
        return HandleRealtimeProjectChangedAsync(realtimeEvent);
    }

    private Task OnRealtimeReconnected()
    {
        _activityLog?.Add("SignalR", "Connection reconnected");
        return HandleRealtimeReconnectedAsync();
    }

    private async Task HandleRealtimeReconnectedAsync()
    {
        ScheduleConnectionRecovery();
        ProjectListRefreshRequested?.Invoke();

        if (CurrentProject == null) return;

        var document = await LoadProjectDocumentAsync(CurrentProject.Id);
        if (document == null) return;

        if (!ProjectReconnectVersionPolicy.ShouldCatchUp(
                _lastAppliedRealtimeProjectId,
                _lastAppliedRealtimeVersion,
                document.Id,
                document.Version))
        {
            return;
        }

        var catchUpEvent = new ProjectRealtimeEventDto
        {
            ProjectId = document.Id,
            Version = document.Version,
            ChangeType = "ReconnectCatchUp",
            EntityType = nameof(ProjectDocumentDto),
            EntityId = document.Id.ToString(),
            ChangedByUserId = string.Empty,
            OccurredOnUtc = DateTime.UtcNow
        };

        if (ProjectRealtimeDirtyPolicy.ShouldDeferRemoteReload(DiagramAutosaveState))
        {
            RememberDeferredRealtimeEvent(catchUpEvent);
            return;
        }

        await ApplyLoadedProjectDocumentAsync(document, catchUpEvent, ActiveFlowsheet?.Id);
    }

    private void ScheduleConnectionRecovery(Guid? projectId = null)
    {
        var targetProjectId = projectId ?? CurrentProject?.Id;
        if (!targetProjectId.HasValue || CurrentProject?.Id != targetProjectId.Value) return;
        if (_connectionRecoveryCts is { IsCancellationRequested: false }) return;

        _connectionRecoveryCts?.Dispose();
        _connectionRecoveryCts = new CancellationTokenSource();
        _ = RunConnectionRecoveryAsync(targetProjectId.Value, _connectionRecoveryCts.Token);
    }

    private async Task RunConnectionRecoveryAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (_isConnectionRecoveryRunning) return;

        _isConnectionRecoveryRunning = true;
        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   CurrentUser != null &&
                   CurrentProject?.Id == projectId)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    if (await TryRecoverCurrentProjectAsync(projectId))
                    {
                        return;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
        finally
        {
            _isConnectionRecoveryRunning = false;
            if (_connectionRecoveryCts is { IsCancellationRequested: false })
            {
                _connectionRecoveryCts.Cancel();
            }
        }
    }

    private async Task<bool> TryRecoverCurrentProjectAsync(Guid projectId)
    {
        if (CurrentUser == null || CurrentProject?.Id != projectId) return true;

        var activeFlowsheetId = ActiveFlowsheet?.Id;
        var result = await TryLoadProjectDocumentAsync(projectId, scheduleRecoveryOnConnectionFailure: false);
        if (!result.Succeeded || result.Document == null)
        {
            return false;
        }

        var document = result.Document;
        var catchUpEvent = new ProjectRealtimeEventDto
        {
            ProjectId = document.Id,
            Version = document.Version,
            ChangeType = ProjectReconnectVersionPolicy.ShouldCatchUp(
                _lastAppliedRealtimeProjectId,
                _lastAppliedRealtimeVersion,
                document.Id,
                document.Version)
                ? "ConnectionRecoveryCatchUp"
                : "ConnectionRecoveryRefresh",
            EntityType = nameof(ProjectDocumentDto),
            EntityId = document.Id.ToString(),
            ChangedByUserId = string.Empty,
            OccurredOnUtc = DateTime.UtcNow
        };

        if (ProjectRealtimeDirtyPolicy.ShouldDeferRemoteReload(DiagramAutosaveState))
        {
            RememberDeferredRealtimeEvent(catchUpEvent);
            return true;
        }

        await ApplyLoadedProjectDocumentAsync(document, catchUpEvent, activeFlowsheetId);
        return true;
    }

    private async Task HandleRealtimeProjectChangedAsync(ProjectRealtimeEventDto realtimeEvent)
    {
        await _realtimeReloadLock.WaitAsync();
        try
        {
            if (CurrentUser == null) return;
            if (CurrentProject == null)
            {
                if (IsProjectAccessChange(realtimeEvent))
                {
                    ProjectListRefreshRequested?.Invoke();
                }

                return;
            }

            if (realtimeEvent.ProjectId != CurrentProject.Id)
            {
                if (IsProjectAccessChange(realtimeEvent))
                {
                    ProjectListRefreshRequested?.Invoke();
                }

                return;
            }

            if (string.Equals(realtimeEvent.ChangedByUserId, CurrentUser.Id.ToString(), StringComparison.OrdinalIgnoreCase)) return;
            if (ProjectRealtimeVersionPolicy.ShouldIgnoreEvent(
                    _lastAppliedRealtimeProjectId,
                    _lastAppliedRealtimeVersion,
                    realtimeEvent.ProjectId,
                    realtimeEvent.Version))
            {
                return;
            }

            if (ProjectRealtimeDirtyPolicy.ShouldDeferRemoteReload(DiagramAutosaveState))
            {
                RememberDeferredRealtimeEvent(realtimeEvent);
                _activityLog?.Add("SignalR", "Project reload deferred", $"Autosave state: {DiagramAutosaveState}");
                return;
            }

            var activeFlowsheetId = ActiveFlowsheet?.Id;
            var hydrationRequest = _hydrationPublicationGate.Begin(realtimeEvent.ProjectId);
            var loadResult = await TryLoadProjectDocumentAsync(realtimeEvent.ProjectId, scheduleRecoveryOnConnectionFailure: true);
            if (!loadResult.Succeeded)
            {
                if (loadResult.AccessDenied)
                {
                    ClearProjectAccessAfterRemoteLoss(realtimeEvent.ProjectId);
                    return;
                }

                if (loadResult.ConnectionFailed)
                {
                    RememberDeferredRealtimeEvent(realtimeEvent);
                    return;
                }

                return;
            }

            if (loadResult.Document == null)
            {
                if (!_hydrationPublicationGate.CanPublish(hydrationRequest, realtimeEvent.ProjectId))
                {
                    return;
                }

                ClearProjectAccessAfterRemoteLoss(realtimeEvent.ProjectId);
                return;
            }

            var reloadedDocument = loadResult.Document;
            if (!ProjectRealtimeVersionPolicy.CanPublishLoadedDocument(
                    _lastAppliedRealtimeProjectId,
                    _lastAppliedRealtimeVersion,
                    realtimeEvent.ProjectId,
                    realtimeEvent.Version,
                    reloadedDocument.Id,
                    reloadedDocument.Version))
            {
                return;
            }

            await ApplyLoadedProjectDocumentAsync(reloadedDocument, realtimeEvent, activeFlowsheetId, hydrationRequest);
        }
        finally
        {
            _realtimeReloadLock.Release();
        }
    }

    private async Task ApplyLoadedProjectDocumentAsync(
        ProjectDocumentDto document,
        ProjectRealtimeEventDto realtimeEvent,
        Guid? activeFlowsheetId,
        ProjectHydrationRequest? existingHydrationRequest = null)
    {
        if (CurrentUser == null) return;

        var hydrationRequest = existingHydrationRequest ?? _hydrationPublicationGate.Begin(document.Id);
        var reloadedProject = await FromPersistenceDtoAsync(document, CurrentUser, recalculate: true);
        if (!_hydrationPublicationGate.CanPublish(hydrationRequest, reloadedProject.Id))
        {
            return;
        }

        _lastAppliedRealtimeVersion = document.Version;
        _currentProjectVersion = document.Version;
        _lastAppliedRealtimeProjectId = document.Id;
        RefreshCurrentUserProjectRole(realtimeEvent.ProjectId, document);
        PublishHydratedProject(reloadedProject, activeFlowsheetId);
        _lastRenderedProjectVersion = ProjectVersionConfirmation.Confirm(_lastRenderedProjectVersion, document.Version);
        await UpdateRealtimeActiveDiagramAsync();

        ProjectReloaded?.Invoke(reloadedProject);
        if (IsProjectAccessChange(realtimeEvent))
        {
            ProjectListRefreshRequested?.Invoke();
        }

        ProjectChanged?.Invoke();
    }

    private void RememberDeferredRealtimeEvent(ProjectRealtimeEventDto realtimeEvent)
    {
        if (_deferredRealtimeEvent == null ||
            realtimeEvent.Version > _deferredRealtimeEvent.Version)
        {
            _deferredRealtimeEvent = realtimeEvent;
            _activityLog?.Add("SignalR", "Deferred project change remembered", $"{realtimeEvent.ChangeType} v{realtimeEvent.Version}");
        }
    }

    private async Task ProcessDeferredRealtimeIfCleanAsync()
    {
        if (ProjectRealtimeDirtyPolicy.ShouldDeferRemoteReload(DiagramAutosaveState))
        {
            return;
        }

        var realtimeEvent = _deferredRealtimeEvent;
        _deferredRealtimeEvent = null;
        if (realtimeEvent == null)
        {
            return;
        }

        _activityLog?.Add("SignalR", "Processing deferred project change", $"{realtimeEvent.ChangeType} v{realtimeEvent.Version}");
        await HandleRealtimeProjectChangedAsync(realtimeEvent);
    }

    private void PublishHydratedProject(Project project, Guid? preferredFlowsheetId)
    {
        if (CurrentProject?.Id != project.Id)
        {
            _lastRenderedProjectVersion = 0;
        }

        CurrentProject = project;
        ActiveFlowsheet = preferredFlowsheetId.HasValue
            ? CurrentProject.GetFlowsheet(preferredFlowsheetId.Value)
            : null;
        ActiveFlowsheet ??= CurrentProject.Flowsheets.FirstOrDefault();
    }

    private void ClearProjectAccessAfterRemoteLoss(Guid projectId)
    {
        if (CurrentProject?.Id != projectId) return;

        _projectRoles.Remove(projectId);
        CurrentProject = null;
        ActiveFlowsheet = null;
        _currentProjectVersion = 0;
        _lastAppliedRealtimeVersion = 0;
        _lastRenderedProjectVersion = 0;
        _lastAppliedRealtimeProjectId = null;
        _deferredRealtimeEvent = null;
        ProjectPresence = Array.Empty<ProjectPresenceDto>();

        ProjectPresenceChanged?.Invoke();
        ProjectListRefreshRequested?.Invoke();
        ProjectChanged?.Invoke();
    }

    private void UpdateConfirmedProjectVersion(ProjectDocumentDto? document)
    {
        if (document == null) return;
        if (CurrentProject != null && document.Id != CurrentProject.Id) return;

        _currentProjectVersion = ProjectVersionConfirmation.Confirm(_currentProjectVersion, document.Version);
        SyncConfirmedDiagramIds(document);
        _lastAppliedRealtimeProjectId = document.Id;
        _lastAppliedRealtimeVersion = ProjectVersionConfirmation.Confirm(_lastAppliedRealtimeVersion, document.Version);
        _lastRenderedProjectVersion = ProjectVersionConfirmation.Confirm(_lastRenderedProjectVersion, document.Version);
    }

    private void SyncConfirmedDiagramIds(ProjectDocumentDto document)
    {
        _confirmedDiagramIds.Clear();
        foreach (var diagram in document.Diagrams)
        {
            _confirmedDiagramIds.Add(diagram.Id);
        }
    }

    private static bool IsProjectAccessChange(ProjectRealtimeEventDto realtimeEvent)
    {
        return realtimeEvent.ChangeType is "SharingUpdated" or "ProjectDeleted";
    }

    private async Task<ProjectDocumentDto?> LoadProjectDocumentAsync(Guid projectId)
    {
        var result = await TryLoadProjectDocumentAsync(projectId, scheduleRecoveryOnConnectionFailure: true);
        return result.Document;
    }

    private async Task ConfirmCurrentProjectDocumentAsync(Guid projectId)
    {
        if (_httpService == null || CurrentProject?.Id != projectId)
        {
            return;
        }

        var document = await LoadProjectDocumentAsync(projectId);
        UpdateConfirmedProjectVersion(document);
    }

    private async Task<ProjectDocumentLoadResult> TryLoadProjectDocumentAsync(
        Guid projectId,
        bool scheduleRecoveryOnConnectionFailure)
    {
        await EnsureCurrentUserAsync();
        if (CurrentUser == null || _httpService == null)
        {
            return new ProjectDocumentLoadResult(false, false, false, null);
        }

        var result = await _httpService.PostAsync<GetProjectRequest, ProjectDocumentDto>(
            new GetProjectRequest
            {
                ProjectId = projectId
            },
            showSnackbar: false);

        if (result.Succeeded)
        {
            return new ProjectDocumentLoadResult(true, false, false, result.Data);
        }

        var connectionFailed = IsConnectionFailure(result.Messages);
        var accessDenied = ProjectAccessFailurePolicy.IsAccessDenied(result.Messages);
        if (scheduleRecoveryOnConnectionFailure && connectionFailed)
        {
            ScheduleConnectionRecovery();
        }

        return new ProjectDocumentLoadResult(false, connectionFailed, accessDenied, null);
    }

    private void RefreshCurrentUserProjectRole(Guid projectId, ProjectDocumentDto document)
    {
        if (CurrentUser == null) return;

        if (Guid.TryParse(document.OwnerUserId, out var ownerId) && ownerId == CurrentUser.Id)
        {
            _projectRoles[projectId] = "Owner";
            return;
        }

        var role = document.Collaborators.FirstOrDefault(collaborator =>
            collaborator.UserId.Equals(CurrentUser.Id.ToString(), StringComparison.OrdinalIgnoreCase))?.Role;

        if (string.IsNullOrWhiteSpace(role))
        {
            _projectRoles.Remove(projectId);
            return;
        }

        _projectRoles[projectId] = role;
    }

    private async Task JoinRealtimeProjectAsync(Guid projectId)
    {
        if (_realtimeService == null) return;

        try
        {
            await _realtimeService.JoinProjectAsync(projectId, ActiveFlowsheet?.Id, ActiveFlowsheet?.Name);
        }
        catch
        {
            // La carga HTTP sigue siendo la fuente de verdad si SignalR no está disponible.
        }
    }

    private async Task UpdateRealtimeActiveDiagramAsync()
    {
        if (_realtimeService == null) return;

        try
        {
            await _realtimeService.UpdateActiveDiagramAsync(ActiveFlowsheet?.Id, ActiveFlowsheet?.Name);
        }
        catch
        {
            // La presencia no debe bloquear el flujo principal de la aplicación.
        }
    }

    private void OnRealtimePresenceChanged(IReadOnlyList<ProjectPresenceDto> presence)
    {
        ProjectPresence = presence;
        ProjectPresenceChanged?.Invoke();
    }

    public async Task SetProjectExplorerCollapsedAsync(bool isCollapsed)
    {
        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return;

        var session = await GetOrCreateWorkspaceStateAsync();
        session.IsProjectExplorerCollapsed = isCollapsed;
        session.LastAccessAt = DateTime.UtcNow;
        await SaveWorkspaceStateAsync(session);
    }

    public async Task SetDiagramExplorerCollapsedAsync(bool isCollapsed)
    {
        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return;

        var session = await GetOrCreateWorkspaceStateAsync();
        session.IsDiagramExplorerCollapsed = isCollapsed;
        session.LastAccessAt = DateTime.UtcNow;
        await SaveWorkspaceStateAsync(session);
    }

    public async Task SetExpandedDiagramTypeCodesAsync(IEnumerable<string> typeCodes)
    {
        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return;

        var session = await GetOrCreateWorkspaceStateAsync();
        session.ExpandedDiagramTypeCodes = typeCodes
            .Where(typeCode => !string.IsNullOrWhiteSpace(typeCode))
            .Select(typeCode => typeCode.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        session.LastAccessAt = DateTime.UtcNow;
        await SaveWorkspaceStateAsync(session);
    }

    private string GenerateUniqueFlowsheetName(string typeCode, string baseName)
    {
        if (CurrentProject == null) return baseName;

        var existingNames = CurrentProject.Flowsheets
            .Where(f => f.TypeCode.Equals(typeCode, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingNames.Contains(baseName))
            return baseName;

        int counter = 1;
        while (true)
        {
            var candidate = $"{baseName} {counter}";
            if (!existingNames.Contains(candidate))
                return candidate;
            counter++;
        }
    }

    public bool RequiresDiagramNumberForNaming()
    {
        return CurrentProject != null && RequiresDiagramNumberForNaming(CurrentProject.Configuration.NamingConfig);
    }

    public static bool RequiresDiagramNumberForNaming(INamingConfiguration configuration)
    {
        return configuration.CounterScope is NamingCounterScope.Diagram or NamingCounterScope.DiagramAndType;
    }

    public static string? GetDiagramNumberConfigurationError(
        Project project,
        IProjectConfiguration configuration,
        IReadOnlyDictionary<Guid, string>? diagramNumberUpdates = null)
    {
        if (!RequiresDiagramNumberForNaming(configuration.NamingConfig))
        {
            return null;
        }

        var missing = project.Flowsheets
            .Where(flowsheet => string.IsNullOrWhiteSpace(GetProjectedDiagramNumber(flowsheet, diagramNumberUpdates)))
            .Select(flowsheet => flowsheet.Name)
            .ToList();

        if (missing.Count > 0)
        {
            return $"Diagram-based naming requires a unique diagram number for every diagram. Missing: {string.Join(", ", missing)}.";
        }

        var duplicate = project.Flowsheets
            .GroupBy(flowsheet => GetProjectedDiagramNumber(flowsheet, diagramNumberUpdates)!.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate != null)
        {
            return $"Diagram number '{duplicate.Key}' is already used by more than one diagram.";
        }

        return null;
    }

    private static string? GetProjectedDiagramNumber(
        IFlowsheet flowsheet,
        IReadOnlyDictionary<Guid, string>? diagramNumberUpdates)
    {
        return diagramNumberUpdates != null && diagramNumberUpdates.TryGetValue(flowsheet.Id, out var diagramNumber)
            ? diagramNumber
            : flowsheet.DiagramNumber;
    }

    private static void ValidateDiagramNumbersForConfiguration(
        Project project,
        IProjectConfiguration configuration,
        IReadOnlyDictionary<Guid, string>? diagramNumberUpdates = null)
    {
        var error = GetDiagramNumberConfigurationError(project, configuration, diagramNumberUpdates);
        if (error != null)
        {
            throw new InvalidOperationException(error);
        }
    }

    private static bool ShouldRunSimulationAfterConfigurationChange(
        IProjectConfiguration current,
        IProjectConfiguration next)
    {
        if (current.ThermodynamicMethodId != next.ThermodynamicMethodId)
        {
            return true;
        }

        const double altitudeToleranceMeters = 1e-6;
        var currentElevation = current.PlantElevation.GetValue(LengthUnits.Meter);
        var nextElevation = next.PlantElevation.GetValue(LengthUnits.Meter);
        return Math.Abs(currentElevation - nextElevation) > altitudeToleranceMeters;
    }

    private void ApplyConfigurationDraft(
        Project project,
        string projectName,
        IProjectConfiguration configuration,
        bool renameExistingEquipment,
        IReadOnlyDictionary<Guid, string>? diagramNumberUpdates)
    {
        project.Name = projectName.Trim();
        project.UpdateConfiguration(configuration);

        if (diagramNumberUpdates != null)
        {
            foreach (var flowsheet in project.Flowsheets)
            {
                if (diagramNumberUpdates.TryGetValue(flowsheet.Id, out var diagramNumber))
                {
                    flowsheet.DiagramNumber = diagramNumber;
                }
            }
        }

        _namingService.SetConfiguration(configuration.NamingConfig);

        if (renameExistingEquipment)
        {
            RenameExistingEquipment(project, configuration.NamingConfig);
        }
    }

    private void RenameExistingEquipment(Project project, INamingConfiguration namingConfiguration)
    {
        var elements = GetElementsWithFlowsheets(project);
        if (elements.Count == 0)
        {
            _namingService.SetConfiguration(namingConfiguration);
            return;
        }

        var streamOldNames = elements
            .Where(item => item.Element is StreamVisualElement && item.Element.Facade is IFacadeStream)
            .ToDictionary(
                item => item.Element.Id,
                item => item.Element.Facade!.Name,
                EqualityComparer<Guid>.Default);
        var originalNames = elements.ToDictionary(
            item => item.Element.Id,
            item => item.Element.Facade?.Name ?? item.Element.Name,
            EqualityComparer<Guid>.Default);

        var temporaryIndex = 0;
        foreach (var item in elements)
        {
            project.EquipmentRegistry.Unregister(item.Element.Id);
            SetElementName(item.Element, $"__renaming_{ToAlphabeticToken(temporaryIndex++)}");
            project.EquipmentRegistry.Register(item.Element);
        }

        _namingService.SetConfiguration(namingConfiguration);

        foreach (var item in elements)
        {
            project.EquipmentRegistry.Unregister(item.Element.Id);
            var typeCode = GetEquipmentTypeCode(item.Element);
            var newName = _namingService.GenerateNextName(typeCode, project, item.Flowsheet);
            SetElementName(item.Element, newName);
            project.EquipmentRegistry.Register(item.Element);
        }

        if (elements.Count > 0 &&
            elements.All(item => string.Equals(
                originalNames[item.Element.Id],
                item.Element.Facade?.Name ?? item.Element.Name,
                StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Naming migration did not rename any equipment.");
        }

        var streamRenames = elements
            .Where(item => streamOldNames.ContainsKey(item.Element.Id) && item.Element.Facade is IFacadeStream)
            .Select(item => new StreamRename(
                item.Element.Id,
                streamOldNames[item.Element.Id],
                item.Element.Facade!.Name))
            .Where(rename => !string.Equals(rename.OldName, rename.NewName, StringComparison.Ordinal))
            .ToList();

        UpdateFormulaSpecificationsAfterStreamRenames(project, streamRenames);
    }

    private static string ToAlphabeticToken(int index)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var value = index;
        var token = string.Empty;

        do
        {
            token = alphabet[value % alphabet.Length] + token;
            value = (value / alphabet.Length) - 1;
        }
        while (value >= 0);

        return token;
    }

    private static void UpdateFormulaSpecificationsAfterStreamRenames(
        Project project,
        IReadOnlyCollection<StreamRename> streamRenames)
    {
        if (streamRenames.Count == 0) return;

        var updates = new List<FormulaSpecificationRenameUpdate>();
        var renamedStreamIds = streamRenames
            .Select(rename => rename.StreamId)
            .ToHashSet();

        foreach (var equipment in project.EquipmentRegistry.AllEquipments
                     .Select(element => element.Facade)
                     .OfType<SolverEquipmentBase>())
        {
            foreach (var specification in equipment.Specifications.OfType<FormulaSpecification>().ToList())
            {
                if (!specification.AssociatedStreams.Any(stream => renamedStreamIds.Contains(stream.Id)))
                {
                    continue;
                }

                var formula = specification.Equation.ToFormulaText();
                if (string.Equals(formula, specification.Formula, StringComparison.Ordinal))
                {
                    continue;
                }

                updates.Add(new FormulaSpecificationRenameUpdate(
                    equipment,
                    specification,
                    new FormulaSpecification(formula, specification.Equation)
                    {
                        Id = specification.Id,
                        DefinedByUserId = specification.DefinedByUserId,
                        DefinedByUserName = specification.DefinedByUserName,
                        DefinedAtUtc = specification.DefinedAtUtc
                    }));
            }
        }

        foreach (var update in updates)
        {
            update.Equipment.RemoveSpec(update.Original);
            update.Equipment.AddSpec(update.Replacement);
        }
    }

    private sealed record StreamRename(Guid StreamId, string OldName, string NewName);

    private sealed record FormulaSpecificationRenameUpdate(
        SolverEquipmentBase Equipment,
        FormulaSpecification Original,
        FormulaSpecification Replacement);

    private static List<(IVisualElement Element, IFlowsheet? Flowsheet)> GetElementsWithFlowsheets(Project project)
    {
        var result = new List<(IVisualElement Element, IFlowsheet? Flowsheet)>();
        var added = new HashSet<Guid>();

        foreach (var flowsheet in project.Flowsheets)
        {
            foreach (var reference in flowsheet.Elements)
            {
                var element = project.GetEquipment(reference.ElementId);
                if (element == null || !added.Add(element.Id)) continue;
                result.Add((element, flowsheet));
            }
        }

        foreach (var element in project.EquipmentRegistry.AllEquipments)
        {
            if (added.Add(element.Id))
            {
                result.Add((element, null));
            }
        }

        return result;
    }

    private static void SetElementName(IVisualElement element, string name)
    {
        element.Name = name;
        element.Label = name;
        if (element.Facade != null)
        {
            element.Facade.Name = name;
        }
    }

    private static string GetEquipmentTypeCode(IVisualElement element)
    {
        return element.Type switch
        {
            EquipmentType.Pump => "Pump",
            EquipmentType.MaterialStream => "Stream",
            EquipmentType.Column => "Column",
            EquipmentType.FlashDrum => "FlashDrum",
            EquipmentType.Exchanger => "HeatExchanger",
            EquipmentType.PlateExchanger => "HeatExchanger",
            EquipmentType.Reboiler => "HeatExchanger",
            EquipmentType.ControlValve => "ControlValve",
            EquipmentType.Splitter => "Splitter",
            EquipmentType.Mixer => "Mixer",
            EquipmentType.Tank => "Tank",
            EquipmentType.Instrument => "Instrument",
            EquipmentType.OffPageConnector => "OffPageConnector",
            _ => element.Prefix
        };
    }

    private void SetDiagramNumber(IFlowsheet flowsheet, string? diagramNumber)
    {
        if (CurrentProject == null) return;

        var normalized = diagramNumber?.Trim() ?? string.Empty;
        if (!RequiresDiagramNumberForNaming(CurrentProject.Configuration.NamingConfig) && string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (RequiresDiagramNumberForNaming(CurrentProject.Configuration.NamingConfig) && string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Diagram number is required by the current naming configuration.");
        }

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            var exists = CurrentProject.Flowsheets
                .Any(f => f.Id != flowsheet.Id &&
                          string.Equals(f.DiagramNumber?.Trim(), normalized, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                throw new InvalidOperationException("A diagram with this number already exists.");
            }
        }

        flowsheet.DiagramNumber = normalized;
    }

    private async Task<ProjectDocumentDto?> PersistProjectConfigurationAsync(
        string projectName,
        IProjectConfiguration configuration,
        IReadOnlyList<ProjectDiagramDto>? migratedDiagrams = null)
    {
        if (_httpService == null || CurrentProject == null) return null;

        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return null;

        var request = new UpdateProjectConfigurationRequest
        {
            ProjectId = CurrentProject.Id,
            ExpectedVersion = _currentProjectVersion,
            OperationId = Guid.NewGuid(),
            Name = projectName.Trim(),
            Configuration = ToPersistenceDto(configuration),
            MigratedDiagrams = migratedDiagrams?.ToList()
        };

        var result = await _httpService.PostAsync<UpdateProjectConfigurationRequest, ProjectDocumentDto>(request, showSnackbar: false);
        UpdateConfirmedProjectVersion(result.Data);
        return result.Succeeded ? result.Data : null;
    }

    private async Task<bool> PersistDiagramCreatedAsync(Guid projectId, ProjectDiagramDto diagram)
    {
        if (_httpService == null) return false;

        var result = await _httpService.PostAsync<CreateDiagramRequest, ProjectDocumentDto>(new CreateDiagramRequest
        {
            ProjectId = projectId,
            ExpectedVersion = _currentProjectVersion,
            OperationId = Guid.NewGuid(),
            Diagram = diagram
        }, showSnackbar: false);
        UpdateConfirmedProjectVersion(result.Data);
        if (result.Succeeded)
        {
            _confirmedDiagramIds.Add(diagram.Id);
        }

        return result.Succeeded;
    }

    private async Task PersistDiagramUpdatedAsync(IFlowsheet flowsheet)
    {
        if (_httpService == null || CurrentProject == null) return;

        await EnqueueDiagramAutosaveAsync(new[] { flowsheet });
    }

    private async Task PersistDiagramDeletedAsync(Guid projectId, Guid diagramId)
    {
        if (_httpService == null) return;

        var result = await _httpService.PostAsync<DeleteDiagramRequest, ProjectDocumentDto>(new DeleteDiagramRequest
        {
            ProjectId = projectId,
            ExpectedVersion = _currentProjectVersion,
            OperationId = Guid.NewGuid(),
            DiagramId = diagramId
        }, showSnackbar: false);
        UpdateConfirmedProjectVersion(result.Data);
    }

    private async Task PersistDiagramNumbersForNamingAsync(IProjectConfiguration configuration)
    {
        if (_httpService == null || CurrentProject == null) return;
        if (!RequiresDiagramNumberForNaming(configuration.NamingConfig)) return;

        foreach (var flowsheet in CurrentProject.Flowsheets.Where(flowsheet => !string.IsNullOrWhiteSpace(flowsheet.DiagramNumber)))
        {
            await PersistDiagramUpdatedAsync(flowsheet);
        }
    }

    private async Task<UserSessionState> LoadWorkspaceStateAsync()
    {
        if (CurrentUser == null)
            throw new InvalidOperationException("No current user available.");

        if (_httpService != null)
        {
            var result = await _httpService.PostAsync<GetUserWorkspaceStateRequest, UserWorkspaceStateDto>(new GetUserWorkspaceStateRequest());
            if (result.Succeeded && result.Data != null)
            {
                return FromWorkspaceStateDto(CurrentUser.Id, result.Data);
            }
        }

        return (await _userSessionStateRepository.GetByUserIdAsync(CurrentUser.Id) as UserSessionState)
            ?? new UserSessionState(CurrentUser.Id);
    }

    private async Task<UserSessionState> GetOrCreateWorkspaceStateAsync()
    {
        if (CurrentUser == null)
            throw new InvalidOperationException("No current user available.");

        if (_workspaceState != null)
        {
            return _workspaceState;
        }

        _workspaceState = await LoadWorkspaceStateAsync();
        return _workspaceState;
    }

    private async Task SaveWorkspaceStateAsync(UserSessionState session)
    {
        _workspaceState = session;
        await _userSessionStateRepository.SaveAsync(session);

        if (_httpService == null) return;

        await _httpService.PostAsync<UpdateUserWorkspaceStateRequest, UserWorkspaceStateDto>(new UpdateUserWorkspaceStateRequest
        {
            State = ToWorkspaceStateDto(session)
        });
    }

    private async Task EnsureCurrentUserAsync()
    {
        if (CurrentUser == null)
        {
            await _authProvider.GetAuthenticationStateAsync();
        }
    }

    private static UserSessionState FromWorkspaceStateDto(Guid userId, UserWorkspaceStateDto dto)
    {
        return new UserSessionState(userId)
        {
            LastProjectId = dto.LastProjectId,
            LastFlowsheetId = dto.LastFlowsheetId,
            IsProjectExplorerCollapsed = dto.IsProjectExplorerCollapsed,
            IsDiagramExplorerCollapsed = dto.IsDiagramExplorerCollapsed,
            ExpandedDiagramTypeCodes = dto.ExpandedDiagramTypeCodes,
            LastAccessAt = dto.LastAccessAt
        };
    }

    private static UserWorkspaceStateDto ToWorkspaceStateDto(UserSessionState session)
    {
        return new UserWorkspaceStateDto
        {
            LastProjectId = session.LastProjectId,
            LastFlowsheetId = session.LastFlowsheetId,
            IsProjectExplorerCollapsed = session.IsProjectExplorerCollapsed,
            IsDiagramExplorerCollapsed = session.IsDiagramExplorerCollapsed,
            ExpandedDiagramTypeCodes = session.ExpandedDiagramTypeCodes,
            LastAccessAt = session.LastAccessAt
        };
    }

    private async Task<Project> FromPersistenceDtoAsync(
        ProjectDocumentDto document,
        User currentUser,
        bool recalculate,
        bool updateSessionState = true)
    {
        if (updateSessionState)
        {
            _currentProjectVersion = document.Version;
            SyncConfirmedDiagramIds(document);
        }
        var owner = CreateProjectOwner(document, currentUser);
        try
        {
            SetProjectHydration(true, "Loading project...");
            var thermodynamicMethod = await LoadThermodynamicMethodFullAsync(document.Configuration.ThermodynamicMethodId);
            SetProjectHydration(true, "Applying unit system...");
            var project = new Project(
                document.Name,
                owner,
                FromPersistenceDto(document.Configuration, thermodynamicMethod),
                id: document.Id,
                createdAt: document.CreatedOn);

            var createdDefaultDiagram = document.Diagrams.Count == 0;
            var diagrams = document.Diagrams.Count > 0
                ? document.Diagrams.OrderBy(diagram => diagram.Order).ToList()
                : new List<ProjectDiagramDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "PFD 1",
                        TypeCode = "PFD",
                        Order = 0
                    }
                };

            SetProjectHydration(true, "Restoring diagrams...");
            IFlowsheet? createdDefaultFlowsheet = null;
            foreach (var diagram in diagrams)
            {
                var flowsheet = project.CreateFlowsheet(
                    string.IsNullOrWhiteSpace(diagram.Name) ? "PFD 1" : diagram.Name,
                    string.IsNullOrWhiteSpace(diagram.TypeCode) ? "PFD" : diagram.TypeCode,
                    diagram.Id == Guid.Empty ? null : diagram.Id);

                flowsheet.DiagramNumber = diagram.DiagramNumber?.Trim() ?? string.Empty;
                flowsheet.DiagramWidth = flowsheet.DiagramWidth > 0 ? flowsheet.DiagramWidth : 5000;
                flowsheet.DiagramHeight = flowsheet.DiagramHeight > 0 ? flowsheet.DiagramHeight : 5000;
                ApplyCanvasState(
                    project,
                    flowsheet,
                    diagram.CanvasStateJson,
                    _equipmentHydrationRegistry,
                    _pipeHydrationService,
                    _formulaHydrationService);

                if (createdDefaultDiagram)
                {
                    createdDefaultFlowsheet = flowsheet;
                }
            }

            if (createdDefaultFlowsheet != null && updateSessionState && CanCurrentUserEditProject(project))
            {
                await PersistDiagramCreatedAsync(
                    project.Id,
                    ToDiagramDto(createdDefaultFlowsheet, GetFlowsheetOrder(project, createdDefaultFlowsheet)));
            }

            _interFlowsheetConnectionHydrationService.Restore(project);

            if (recalculate)
            {
                SetProjectHydration(true, "Recalculating simulation...");
                await project.RunSimulationAsync();
            }

            return project;
        }
        finally
        {
            SetProjectHydration(false, string.Empty);
        }
    }

    private async Task<ThermodynamicMethodFullDto?> LoadThermodynamicMethodFullAsync(Guid? methodId)
    {
        if (_httpService == null || methodId == null || methodId == Guid.Empty) return null;

        var result = await _httpService.PostAsync<GetMethodFullRequest, ThermodynamicMethodFullDto>(
            new GetMethodFullRequest(methodId.Value));

        return result.Succeeded ? result.Data : null;
    }

    private static User CreateProjectOwner(ProjectDocumentDto document, User currentUser)
    {
        if (!Guid.TryParse(document.OwnerUserId, out var ownerId) || ownerId == currentUser.Id)
        {
            return currentUser;
        }

        var ownerCollaborator = document.Collaborators.FirstOrDefault(collaborator => collaborator.UserId == document.OwnerUserId);
        var (firstName, lastName) = SplitDisplayName(ownerCollaborator?.DisplayName);
        return new User(ownerId, ownerCollaborator?.Email ?? string.Empty, firstName, lastName, false, currentUser.DefaultPreferences);
    }

    private static (string FirstName, string LastName) SplitDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return (string.Empty, string.Empty);
        }

        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return (parts[0], string.Empty);
        }

        return (parts[0], string.Join(" ", parts.Skip(1)));
    }

    private static IProjectConfiguration FromPersistenceDto(
        ProjectBasicConfigurationDto configuration,
        ThermodynamicMethodFullDto? thermodynamicMethod = null)
    {
        return ProjectConfigurationPersistenceMapper.FromDto(configuration, thermodynamicMethod);
    }

    private static List<ProjectDiagramDto> ToDiagramDtos(Project project)
    {
        return ProjectDiagramDocumentMapper.ToDiagramDtos(project);
    }

    private static ProjectDiagramDto ToDiagramDto(IFlowsheet flowsheet, int index)
    {
        return ProjectDiagramDocumentMapper.ToDiagramDto(flowsheet, index);
    }

    public async Task PersistDiagramVisualStateAsync(IFlowsheet flowsheet)
    {
        if (_httpService == null)
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "HTTP service is not available.");
            return;
        }

        if (CurrentProject == null)
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "No current project.");
            return;
        }

        if (flowsheet.Project.Id != CurrentProject.Id)
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", $"Diagram belongs to another project: {flowsheet.Name}");
            return;
        }

        if (!CanCurrentUserEditProject(CurrentProject))
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "Current user cannot edit this project.");
            return;
        }

        CancellationTokenSource debounce;
        lock (_visualSaveDebounceSync)
        {
            if (_visualSaveDebounces.Remove(flowsheet.Id, out var previousDebounce))
            {
                previousDebounce.Cancel();
                previousDebounce.Dispose();
            }

            debounce = new CancellationTokenSource();
            _visualSaveDebounces[flowsheet.Id] = debounce;
        }

        try
        {
            _activityLog?.StartAutosaveCountdown("Diagram autosave", TimeSpan.FromMilliseconds(250));
            await Task.Delay(250, debounce.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        lock (_visualSaveDebounceSync)
        {
            if (!_visualSaveDebounces.TryGetValue(flowsheet.Id, out var currentDebounce) || currentDebounce != debounce)
            {
                debounce.Dispose();
                return;
            }

            _visualSaveDebounces.Remove(flowsheet.Id);
        }

        try
        {
            _activityLog?.Add("Autosave", "Diagram autosave debounce completed", flowsheet.Name);
            await EnqueueDiagramAutosaveAsync(new[] { flowsheet });
        }
        finally
        {
            debounce.Dispose();
        }
    }

    public async Task PersistDiagramVisualStatesAsync(IReadOnlyCollection<IFlowsheet> flowsheets)
    {
        if (_httpService == null)
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "HTTP service is not available.");
            return;
        }

        if (CurrentProject == null)
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "No current project.");
            return;
        }

        if (!CanCurrentUserEditProject(CurrentProject))
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "Current user cannot edit this project.");
            return;
        }

        var changedFlowsheets = flowsheets
            .Where(flowsheet => flowsheet.Project.Id == CurrentProject.Id)
            .DistinctBy(flowsheet => flowsheet.Id)
            .ToList();
        if (changedFlowsheets.Count == 0)
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "No changed diagram belongs to current project.");
            return;
        }

        if (changedFlowsheets.Count == 1)
        {
            await PersistDiagramVisualStateAsync(changedFlowsheets[0]);
            return;
        }

        foreach (var flowsheet in changedFlowsheets)
        {
            CancelVisualSaveDebounce(flowsheet.Id);
        }

        await EnqueueDiagramAutosaveAsync(changedFlowsheets);
    }

    private Task EnqueueDiagramAutosaveAsync(IReadOnlyCollection<IFlowsheet> flowsheets)
    {
        if (_httpService == null)
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "HTTP service is not available.");
            return Task.CompletedTask;
        }

        if (CurrentProject == null)
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "No current project.");
            return Task.CompletedTask;
        }

        if (!CanCurrentUserEditProject(CurrentProject))
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", "Current user cannot edit this project.");
            return Task.CompletedTask;
        }

        var payload = CreateDiagramAutosavePayload(flowsheets);
        if (payload.Diagrams.Count == 0)
        {
            _activityLog?.SkipAutosave("Diagram autosave skipped", payload.SkippedReason);
            return Task.CompletedTask;
        }

        _diagramAutosaveCoordinator.MarkDirty(payload);
        _activityLog?.Add("Autosave", "Autosave snapshot queued", $"{payload.Diagrams.Count} diagram(s): {payload.DiagramIds}");
        return SaveDiagramAutosaveAndProcessDeferredRealtimeAsync();
    }

    private async Task SaveDiagramAutosaveAndProcessDeferredRealtimeAsync()
    {
        await _diagramAutosaveCoordinator.SaveLatestAsync(PersistDiagramAutosaveSnapshotAsync);
        await ProcessDeferredRealtimeIfCleanAsync();
    }

    private DiagramAutosavePayload CreateDiagramAutosavePayload(IReadOnlyCollection<IFlowsheet> flowsheets)
    {
        if (CurrentProject == null)
        {
            return new DiagramAutosavePayload(Guid.Empty, Guid.Empty, Array.Empty<ProjectDiagramDto>(), string.Empty, "No current project.");
        }

        var currentProjectFlowsheets = flowsheets
            .Where(flowsheet => flowsheet.Project.Id == CurrentProject.Id)
            .DistinctBy(flowsheet => flowsheet.Id)
            .ToList();
        var unconfirmedFlowsheets = currentProjectFlowsheets
            .Where(flowsheet => !_confirmedDiagramIds.Contains(flowsheet.Id))
            .ToList();
        var diagrams = flowsheets
            .Where(flowsheet => flowsheet.Project.Id == CurrentProject.Id)
            .Where(flowsheet => _confirmedDiagramIds.Contains(flowsheet.Id))
            .DistinctBy(flowsheet => flowsheet.Id)
            .Select(flowsheet => ToDiagramDto(
                flowsheet,
                GetFlowsheetOrder(CurrentProject, flowsheet)))
            .ToList();

        return new DiagramAutosavePayload(
            CurrentProject.Id,
            Guid.NewGuid(),
            diagrams,
            string.Join(", ", diagrams.Select(diagram => diagram.Id)),
            BuildDiagramAutosaveSkippedReason(flowsheets, currentProjectFlowsheets, unconfirmedFlowsheets));
    }

    private static string BuildDiagramAutosaveSkippedReason(
        IReadOnlyCollection<IFlowsheet> requestedFlowsheets,
        IReadOnlyCollection<IFlowsheet> currentProjectFlowsheets,
        IReadOnlyCollection<IFlowsheet> unconfirmedFlowsheets)
    {
        if (requestedFlowsheets.Count == 0)
        {
            return "No diagrams were provided.";
        }

        if (currentProjectFlowsheets.Count == 0)
        {
            return "No provided diagram belongs to current project.";
        }

        if (unconfirmedFlowsheets.Count > 0)
        {
            var names = string.Join(", ", unconfirmedFlowsheets.Select(flowsheet => $"{flowsheet.Name} ({flowsheet.Id})"));
            return $"Diagram is not confirmed by server: {names}.";
        }

        return "Payload is empty.";
    }

    private async Task<AutosavePersistenceResult> PersistDiagramAutosaveSnapshotAsync(
        AutosaveSnapshot<DiagramAutosavePayload> snapshot,
        CancellationToken cancellationToken)
    {
        if (_httpService == null)
        {
            return AutosavePersistenceResult.Failure("HTTP service is not available.");
        }

        var payload = snapshot.Payload;
        if (payload.ProjectId == Guid.Empty || payload.Diagrams.Count == 0)
        {
            return AutosavePersistenceResult.Success();
        }

        if (payload.Diagrams.Count == 1)
        {
            _activityLog?.Add("Autosave", "Saving diagram to server", payload.Diagrams[0].Name);
            var result = await _httpService.PostAsync<UpdateDiagramRequest, ProjectDocumentDto>(
                new UpdateDiagramRequest
                {
                    ProjectId = payload.ProjectId,
                    ExpectedVersion = _currentProjectVersion,
                    OperationId = payload.OperationId,
                    Diagram = payload.Diagrams[0]
                },
                showSnackbar: false);

            LogVisualSaveFailure(result, payload.DiagramIds);
            _activityLog?.CompleteAutosave(
                result.Succeeded
                    ? $"Diagram saved: {payload.Diagrams[0].Name}"
                    : $"Diagram save failed: {string.Join("; ", result.Messages)}",
                result.Succeeded);
            if (!result.Succeeded && IsConnectionFailure(result.Messages))
            {
                ScheduleConnectionRecovery(payload.ProjectId);
            }

            UpdateConfirmedProjectVersion(result.Data);
            if (!result.Succeeded && IsProjectVersionConflict(result))
            {
                await ReloadAuthoritativeProjectAfterConflictAsync(payload, cancellationToken);
                return AutosavePersistenceResult.Success();
            }

            return result.Succeeded
                ? AutosavePersistenceResult.Success()
                : AutosavePersistenceResult.Failure(string.Join("; ", result.Messages));
        }

        _activityLog?.Add("Autosave", "Saving diagrams to server", payload.DiagramIds);
        var batchResult = await _httpService.PostAsync<UpdateDiagramsRequest, ProjectDocumentDto>(
            new UpdateDiagramsRequest
            {
                ProjectId = payload.ProjectId,
                ExpectedVersion = _currentProjectVersion,
                OperationId = payload.OperationId,
                Diagrams = payload.Diagrams.ToList()
            },
            showSnackbar: false);

        LogVisualSaveFailure(batchResult, payload.DiagramIds);
        _activityLog?.CompleteAutosave(
            batchResult.Succeeded
                ? $"Diagrams saved: {payload.DiagramIds}"
                : $"Diagram batch save failed: {string.Join("; ", batchResult.Messages)}",
            batchResult.Succeeded);
        if (!batchResult.Succeeded && IsConnectionFailure(batchResult.Messages))
        {
            ScheduleConnectionRecovery(payload.ProjectId);
        }

        UpdateConfirmedProjectVersion(batchResult.Data);
        if (!batchResult.Succeeded && IsProjectVersionConflict(batchResult))
        {
            await ReloadAuthoritativeProjectAfterConflictAsync(payload, cancellationToken);
            return AutosavePersistenceResult.Success();
        }

        return batchResult.Succeeded
            ? AutosavePersistenceResult.Success()
            : AutosavePersistenceResult.Failure(string.Join("; ", batchResult.Messages));
    }

    private void CancelVisualSaveDebounce(Guid flowsheetId)
    {
        lock (_visualSaveDebounceSync)
        {
            if (!_visualSaveDebounces.Remove(flowsheetId, out var debounce)) return;

            debounce.Cancel();
            debounce.Dispose();
        }
    }

    private static void LogVisualSaveFailure<T>(Shared.Results.Result<T> result, string diagramIds)
    {
        if (result.Succeeded) return;

        var message = string.Join("; ", result.Messages);
        if (IsProjectVersionConflict(result))
        {
            Console.WriteLine(
                $"Visual autosave conflict for diagram(s) {diagramIds}: {message}");
            return;
        }

        Console.Error.WriteLine(
            $"Visual autosave failed for diagram(s) {diagramIds}: {message}");
    }

    private static bool IsProjectVersionConflict<T>(Shared.Results.Result<T> result)
    {
        return result.Messages.Any(ProjectVersionConcurrency.IsConflictMessage);
    }

    private static bool IsConnectionFailure(IEnumerable<string> messages)
    {
        return messages.Any(message =>
            message.Contains("Connection error", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Request timed out", StringComparison.OrdinalIgnoreCase));
    }

    private async Task ReloadAuthoritativeProjectAfterConflictAsync(
        DiagramAutosavePayload rejectedPayload,
        CancellationToken cancellationToken)
    {
        var projectId = rejectedPayload.ProjectId;
        if (CurrentUser == null || CurrentProject?.Id != projectId)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var activeFlowsheetId = ActiveFlowsheet?.Id;
        var hydrationRequest = _hydrationPublicationGate.Begin(projectId);
        var reloadedDocument = await LoadProjectDocumentAsync(projectId);
        if (reloadedDocument == null || !_hydrationPublicationGate.CanPublish(hydrationRequest, projectId))
        {
            return;
        }

        var reloadedProject = await FromPersistenceDtoAsync(reloadedDocument, CurrentUser, recalculate: true);
        if (!_hydrationPublicationGate.CanPublish(hydrationRequest, reloadedProject.Id))
        {
            return;
        }

        _lastAppliedRealtimeVersion = reloadedDocument.Version;
        _lastAppliedRealtimeProjectId = reloadedDocument.Id;
        RefreshCurrentUserProjectRole(projectId, reloadedDocument);

        PublishHydratedProject(reloadedProject, activeFlowsheetId);
        var reappliedFlowsheets = ReapplySafeVisualIntentAfterConflict(rejectedPayload, CurrentUser.Id.ToString());
        if (reappliedFlowsheets.Count > 0)
        {
            _diagramAutosaveCoordinator.MarkDirty(CreateDiagramAutosavePayload(reappliedFlowsheets));
        }

        await UpdateRealtimeActiveDiagramAsync();

        ProjectReloaded?.Invoke(CurrentProject);
        ProjectChanged?.Invoke();
    }

    private IReadOnlyList<IFlowsheet> ReapplySafeVisualIntentAfterConflict(DiagramAutosavePayload rejectedPayload, string currentUserId)
    {
        if (CurrentProject == null || rejectedPayload.Diagrams.Count == 0)
        {
            return Array.Empty<IFlowsheet>();
        }

        var reapplied = new List<IFlowsheet>();
        foreach (var diagram in rejectedPayload.Diagrams)
        {
            var flowsheet = CurrentProject.GetFlowsheet(diagram.Id);
            if (flowsheet == null) continue;

            if (TryApplySafeDiagramVisualIntent(CurrentProject, flowsheet, diagram.CanvasStateJson, currentUserId))
            {
                reapplied.Add(flowsheet);
            }
        }

        return reapplied;
    }

    private static bool TryApplySafeDiagramVisualIntent(Project project, IFlowsheet flowsheet, string? canvasStateJson, string currentUserId)
    {
        var intendedState = Deserialize(canvasStateJson ?? "{}", new DiagramCanvasStateSnapshot());
        var intendedElementIds = intendedState.Elements.Select(element => element.Id);
        var authoritativeElementIds = flowsheet.Elements.Select(reference => reference.ElementId);
        if (!ProjectVisualIntentPolicy.CanReapplyExistingElementVisuals(authoritativeElementIds, intendedElementIds))
        {
            return false;
        }

        flowsheet.Zoom = intendedState.Camera.Zoom <= 0 ? flowsheet.Zoom : intendedState.Camera.Zoom;
        flowsheet.PanX = intendedState.Camera.PanX;
        flowsheet.PanY = intendedState.Camera.PanY;
        flowsheet.DiagramWidth = intendedState.Camera.DiagramWidth <= 0 ? flowsheet.DiagramWidth : intendedState.Camera.DiagramWidth;
        flowsheet.DiagramHeight = intendedState.Camera.DiagramHeight <= 0 ? flowsheet.DiagramHeight : intendedState.Camera.DiagramHeight;
        flowsheet.GridSize = intendedState.Camera.GridSize <= 0 ? flowsheet.GridSize : intendedState.Camera.GridSize;
        flowsheet.GlobalScale = intendedState.Camera.GlobalScale <= 0 ? flowsheet.GlobalScale : intendedState.Camera.GlobalScale;

        var referencesByElementId = flowsheet.Elements.ToDictionary(reference => reference.ElementId);
        foreach (var elementSnapshot in intendedState.Elements)
        {
            if (!referencesByElementId.TryGetValue(elementSnapshot.Id, out var reference))
            {
                continue;
            }

            var element = project.EquipmentRegistry.GetById(elementSnapshot.Id);
            reference.X = elementSnapshot.X;
            reference.Y = elementSnapshot.Y;
            reference.RotationAngle = elementSnapshot.RotationAngle;
            reference.ZIndex = elementSnapshot.ZIndex;
            reference.IsFlippedHorizontal = elementSnapshot.IsFlippedHorizontal;
            reference.IsFlippedVertical = elementSnapshot.IsFlippedVertical;

            if (element == null) continue;

            element.X = elementSnapshot.X;
            element.Y = elementSnapshot.Y;
            element.Width = elementSnapshot.Width <= 0 ? element.Width : elementSnapshot.Width;
            element.Height = elementSnapshot.Height <= 0 ? element.Height : elementSnapshot.Height;
            element.RotationAngle = elementSnapshot.RotationAngle;
            element.ZIndex = elementSnapshot.ZIndex;
            element.IsFlippedHorizontal = elementSnapshot.IsFlippedHorizontal;
            element.IsFlippedVertical = elementSnapshot.IsFlippedVertical;
            element.ShowLabel = elementSnapshot.ShowLabel;
            element.IsLocked = elementSnapshot.IsLocked;

            if (FacadeStateSerializer.ApplyNewerUserInputStates(element.Facade, elementSnapshot.FacadeStateJson, currentUserId) &&
                element.Facade is IFacadeStream streamFacade &&
                !string.IsNullOrWhiteSpace(elementSnapshot.FacadeStateJson) &&
                elementSnapshot.FacadeStateJson.Contains("Composition", StringComparison.Ordinal))
            {
                streamFacade.Composition.CompositionChanged();
            }
        }

        return true;
    }

    private static void ApplyCanvasState(
        Project project,
        IFlowsheet flowsheet,
        string? canvasStateJson,
        ProjectEquipmentHydrationRegistry equipmentHydrationRegistry,
        ProjectPipeHydrationService pipeHydrationService,
        ProjectFormulaHydrationService formulaHydrationService)
    {
        var state = Deserialize(canvasStateJson ?? "{}", new DiagramCanvasStateSnapshot());
        var pendingFormulaSpecifications = new List<(
            SolverEquipmentBase Equipment,
            List<FormulaSpecificationSnapshot> Specifications)>();
        flowsheet.Zoom = state.Camera.Zoom <= 0 ? flowsheet.Zoom : state.Camera.Zoom;
        flowsheet.PanX = state.Camera.PanX;
        flowsheet.PanY = state.Camera.PanY;
        flowsheet.DiagramWidth = state.Camera.DiagramWidth <= 0 ? flowsheet.DiagramWidth : state.Camera.DiagramWidth;
        flowsheet.DiagramHeight = state.Camera.DiagramHeight <= 0 ? flowsheet.DiagramHeight : state.Camera.DiagramHeight;
        flowsheet.GridSize = state.Camera.GridSize <= 0 ? flowsheet.GridSize : state.Camera.GridSize;
        flowsheet.GlobalScale = state.Camera.GlobalScale <= 0 ? flowsheet.GlobalScale : state.Camera.GlobalScale;

        foreach (var elementSnapshot in state.Elements)
        {
            if (!Enum.TryParse<EquipmentType>(elementSnapshot.Type, true, out var equipmentType))
            {
                continue;
            }

            var element = flowsheet.TypeDefinition.EquipmentFactory.Create(equipmentType, 0, 0, value => value);
            if (element == null) continue;

            element.Id = elementSnapshot.Id == Guid.Empty ? Guid.NewGuid() : elementSnapshot.Id;
            if (element.Facade != null)
            {
                element.Facade.Id = element.Id;
            }

            element.X = elementSnapshot.X;
            element.Y = elementSnapshot.Y;
            element.Width = elementSnapshot.Width <= 0 ? element.Width : elementSnapshot.Width;
            element.Height = elementSnapshot.Height <= 0 ? element.Height : elementSnapshot.Height;
            element.RotationAngle = elementSnapshot.RotationAngle;
            element.ZIndex = elementSnapshot.ZIndex;
            element.IsFlippedHorizontal = elementSnapshot.IsFlippedHorizontal;
            element.IsFlippedVertical = elementSnapshot.IsFlippedVertical;
            element.ShowLabel = elementSnapshot.ShowLabel;
            element.IsLocked = elementSnapshot.IsLocked;

            if (!string.IsNullOrWhiteSpace(elementSnapshot.Name))
            {
                element.Name = elementSnapshot.Name;
            }

            if (!string.IsNullOrWhiteSpace(elementSnapshot.Label))
            {
                element.Label = elementSnapshot.Label;
            }

            if (element is OffPageConnectorElement offPageConnector && elementSnapshot.OffPageConnector != null)
            {
                offPageConnector.IsOutlet = elementSnapshot.OffPageConnector.IsOutlet;
                offPageConnector.PortSide = ResolveOpcPortSide(
                    elementSnapshot.OffPageConnector.IsOutlet,
                    elementSnapshot.OffPageConnector.PortSide);
                offPageConnector.TargetAreaId = elementSnapshot.OffPageConnector.TargetFlowsheetId;
                offPageConnector.TargetConnectorId = elementSnapshot.OffPageConnector.TargetConnectorId;
                offPageConnector.TargetAreaName = elementSnapshot.OffPageConnector.TargetFlowsheetName;
                offPageConnector.ConnectedEquipmentName = elementSnapshot.OffPageConnector.ConnectedEquipmentName;
                offPageConnector.RefreshPorts();
            }

            if (!equipmentHydrationRegistry.TryRegister(project, element))
            {
                continue;
            }

            FacadeStateSerializer.Apply(element.Facade, elementSnapshot.FacadeStateJson);
            if (element.Facade is IFacadeStream streamFacade &&
                !string.IsNullOrWhiteSpace(elementSnapshot.FacadeStateJson) &&
                elementSnapshot.FacadeStateJson.Contains("Composition", StringComparison.Ordinal))
            {
                streamFacade.Composition.CompositionChanged();
            }

            if (element.Facade is SolverEquipmentBase solverEquipment
                && elementSnapshot.FormulaSpecifications is { Count: > 0 })
            {
                pendingFormulaSpecifications.Add((solverEquipment, elementSnapshot.FormulaSpecifications));
            }

            IFlowsheetElementReference reference = elementSnapshot.OffPageConnector == null
                ? new FlowsheetElementReference(element.Id, element.X, element.Y)
                : new OffPageConnectorReference(
                    element.Id,
                    element.X,
                    element.Y,
                    elementSnapshot.OffPageConnector.IsOutlet,
                    ResolveOpcPortSide(
                        elementSnapshot.OffPageConnector.IsOutlet,
                        elementSnapshot.OffPageConnector.PortSide))
                {
                    TargetFlowsheetId = elementSnapshot.OffPageConnector.TargetFlowsheetId,
                    TargetConnectorId = elementSnapshot.OffPageConnector.TargetConnectorId,
                    TargetFlowsheetName = elementSnapshot.OffPageConnector.TargetFlowsheetName,
                    ConnectedEquipmentName = elementSnapshot.OffPageConnector.ConnectedEquipmentName
                };

            reference.RotationAngle = element.RotationAngle;
            reference.ZIndex = element.ZIndex;
            reference.IsFlippedHorizontal = element.IsFlippedHorizontal;
            reference.IsFlippedVertical = element.IsFlippedVertical;
            flowsheet.AddElementReference(reference);
        }

        foreach (var pipeSnapshot in state.Pipes)
        {
            pipeHydrationService.TryRestore(project, flowsheet, new PipeHydrationSnapshot(
                pipeSnapshot.Id,
                pipeSnapshot.SourceElementId,
                pipeSnapshot.TargetElementId,
                pipeSnapshot.SourcePortName,
                pipeSnapshot.TargetPortName));
        }

        foreach (var pending in pendingFormulaSpecifications)
        {
            formulaHydrationService.Restore(
                pending.Equipment,
                pending.Specifications.Select(ToFormulaHydrationSnapshot),
                project.SimulationService.Solver.Streams);
        }
    }

    private static FormulaSpecificationHydrationSnapshot ToFormulaHydrationSnapshot(FormulaSpecificationSnapshot snapshot)
    {
        return new FormulaSpecificationHydrationSnapshot(
            snapshot.Id,
            snapshot.Formula,
            snapshot.DefinedByUserId,
            snapshot.DefinedByUserName,
            snapshot.DefinedAtUtc);
    }

    private static OffPageConnectorSnapshot? ToOffPageConnectorSnapshot(
        IFlowsheetElementReference reference,
        IVisualElement element)
    {
        if (reference is IOffPageConnectorReference connectorReference)
        {
            return new OffPageConnectorSnapshot(
                connectorReference.TargetFlowsheetId,
                connectorReference.TargetConnectorId,
                connectorReference.TargetFlowsheetName,
                connectorReference.ConnectedEquipmentName,
                connectorReference.IsOutlet,
                connectorReference.PortSide);
        }

        return element is OffPageConnectorElement connector
            ? new OffPageConnectorSnapshot(
                connector.TargetAreaId,
                connector.TargetConnectorId,
                connector.TargetAreaName,
                connector.ConnectedEquipmentName,
                connector.IsOutlet,
                connector.PortSide)
            : null;
    }

    private static OffPageConnectorPortSide ResolveOpcPortSide(
        bool isOutlet,
        OffPageConnectorPortSide? portSide)
    {
        return portSide ?? (isOutlet ? OffPageConnectorPortSide.Left : OffPageConnectorPortSide.Right);
    }

    private static void RestoreInterFlowsheetConnections(Project project)
    {
        new ProjectInterFlowsheetConnectionHydrationService().Restore(project);
    }

    private static List<FormulaSpecificationSnapshot> ToFormulaSpecificationSnapshots(IFacade? facade)
    {
        return facade is not SolverEquipmentBase equipment
            ? new List<FormulaSpecificationSnapshot>()
            : equipment.Specifications
                .OfType<FormulaSpecification>()
                .Select(specification => new FormulaSpecificationSnapshot(
                    specification.Id,
                    specification.Formula,
                    specification.DefinedByUserId,
                    specification.DefinedByUserName,
                    specification.DefinedAtUtc))
                .ToList();
    }

    private static void RestoreFormulaSpecifications(
        Project project,
        SolverEquipmentBase equipment,
        IEnumerable<FormulaSpecificationSnapshot> snapshots)
    {
        var streams = project.SimulationService.Solver.Streams;
        foreach (var snapshot in snapshots)
        {
            var result = FormulaParser.Parse(snapshot.Formula, streams);
            if (!result.Succeeded)
            {
                continue;
            }

            equipment.AddSpec(new FormulaSpecification(snapshot.Formula, result.Data)
            {
                Id = snapshot.Id == Guid.Empty ? Guid.NewGuid() : snapshot.Id,
                DefinedByUserId = snapshot.DefinedByUserId,
                DefinedByUserName = snapshot.DefinedByUserName,
                DefinedAtUtc = snapshot.DefinedAtUtc
            });
        }
    }

    private static void RegisterCanvasElementInSolver(Project project, IVisualElement element)
    {
        if (element.Facade is IFacadeStream stream)
        {
            project.SimulationService.Solver.AddStream(stream);
        }
        else if (element.Facade is ISolverEquipment equipment)
        {
            project.SimulationService.Solver.AddEquipment(equipment);
        }
    }

    private static int GetFlowsheetOrder(Project project, IFlowsheet flowsheet)
    {
        return project.Flowsheets.ToList().IndexOf(flowsheet);
    }

    private static ProjectBasicConfigurationDto ToPersistenceDto(IProjectConfiguration configuration)
    {
        return ProjectConfigurationPersistenceMapper.ToDto(configuration);
    }

    private static T Deserialize<T>(string json, T fallback)
    {
        if (string.IsNullOrWhiteSpace(json)) return fallback;

        try
        {
            return JsonSerializer.Deserialize<T>(json, PersistenceJsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }

    private sealed class ProjectConfigurationDraftSnapshot
    {
        private readonly string _projectName;
        private readonly IProjectConfiguration _configuration;
        private readonly Dictionary<Guid, string> _diagramNumbers;
        private readonly Dictionary<Guid, ElementNameSnapshot> _elementNames;

        private ProjectConfigurationDraftSnapshot(
            string projectName,
            IProjectConfiguration configuration,
            Dictionary<Guid, string> diagramNumbers,
            Dictionary<Guid, ElementNameSnapshot> elementNames)
        {
            _projectName = projectName;
            _configuration = configuration;
            _diagramNumbers = diagramNumbers;
            _elementNames = elementNames;
        }

        public static ProjectConfigurationDraftSnapshot Capture(Project project)
        {
            return new ProjectConfigurationDraftSnapshot(
                project.Name,
                project.Configuration,
                project.Flowsheets.ToDictionary(flowsheet => flowsheet.Id, flowsheet => flowsheet.DiagramNumber),
                project.EquipmentRegistry.AllEquipments.ToDictionary(
                    element => element.Id,
                    element => new ElementNameSnapshot(
                        element.Name,
                        element.Label,
                        element.Facade?.Name)));
        }

        public void Restore(Project project)
        {
            project.Name = _projectName;
            project.UpdateConfiguration(_configuration);

            foreach (var flowsheet in project.Flowsheets)
            {
                if (_diagramNumbers.TryGetValue(flowsheet.Id, out var diagramNumber))
                {
                    flowsheet.DiagramNumber = diagramNumber;
                }
            }

            foreach (var element in project.EquipmentRegistry.AllEquipments.ToList())
            {
                if (!_elementNames.TryGetValue(element.Id, out var snapshot)) continue;

                project.EquipmentRegistry.Unregister(element.Id);
                element.Name = snapshot.Name;
                element.Label = snapshot.Label;
                if (element.Facade != null && snapshot.FacadeName != null)
                {
                    element.Facade.Name = snapshot.FacadeName;
                }
                project.EquipmentRegistry.Register(element);
            }
        }
    }

    private sealed record ElementNameSnapshot(string Name, string Label, string? FacadeName);

    private sealed record DiagramCanvasStateSnapshot(
        DiagramCameraSnapshot Camera,
        List<DiagramElementSnapshot> Elements,
        List<DiagramPipeSnapshot> Pipes)
    {
        public DiagramCanvasStateSnapshot()
            : this(new DiagramCameraSnapshot(), new List<DiagramElementSnapshot>(), new List<DiagramPipeSnapshot>())
        {
        }
    }

    private sealed record DiagramCameraSnapshot(
        double Zoom,
        double PanX,
        double PanY,
        double DiagramWidth,
        double DiagramHeight,
        double GridSize,
        double GlobalScale)
    {
        public DiagramCameraSnapshot()
            : this(1, 0, 0, 5000, 5000, 20, 0.7)
        {
        }
    }

    private sealed record DiagramElementSnapshot(
        Guid Id,
        string Type,
        string Name,
        string Label,
        string? FacadeStateJson,
        List<FormulaSpecificationSnapshot>? FormulaSpecifications,
        double X,
        double Y,
        double Width,
        double Height,
        int RotationAngle,
        int ZIndex,
        bool IsFlippedHorizontal,
        bool IsFlippedVertical,
        bool ShowLabel,
        bool IsLocked,
        OffPageConnectorSnapshot? OffPageConnector = null);

    private sealed record OffPageConnectorSnapshot(
        Guid? TargetFlowsheetId,
        Guid? TargetConnectorId,
        string TargetFlowsheetName,
        string ConnectedEquipmentName,
        bool IsOutlet,
        OffPageConnectorPortSide? PortSide = null);

    private sealed record FormulaSpecificationSnapshot(
        Guid Id,
        string Formula,
        string? DefinedByUserId = null,
        string? DefinedByUserName = null,
        DateTime? DefinedAtUtc = null);

    private sealed record DiagramPipeSnapshot(
        Guid Id,
        Guid SourceElementId,
        Guid TargetElementId,
        string SourcePortName,
        string TargetPortName);

}
