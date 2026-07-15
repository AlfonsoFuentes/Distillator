using System.Text.Json;
using Client.Services.HttpServices;
using Client.Services.Security;
using Distillator.Domain.Configuration;
using Distillator.Domain.Models;
using Distillator.Domain.Policies;
using Distillator.Domain.Repositories;
using Distillator.Domain.Repositories.InMemory;
using Distillator.Domain.Services;
using Distillator.Domain.Session;
using Shared.ProcessFlowDiagram;
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
    private UserSessionState? _workspaceState;
    private readonly Dictionary<Guid, string> _projectRoles = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _visualSaveDebounces = new();
    private readonly object _visualSaveDebounceSync = new();
    private readonly SemaphoreSlim _visualPersistenceLock = new(1, 1);
    private readonly SemaphoreSlim _realtimeReloadLock = new(1, 1);
    private long _lastAppliedRealtimeVersion;
    private Guid? _lastAppliedRealtimeProjectId;
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

    public event Action? ProjectChanged;
    public event Action<Project>? ProjectReloaded;
    public event Action? ProjectListRefreshRequested;
    public event Action? ProjectPresenceChanged;
    public event Action? ProjectHydrationChanged;

    public void NotifyProjectChanged() => ProjectChanged?.Invoke();

    private void SetProjectHydration(bool isHydrating, string message)
    {
        IsProjectHydrating = isHydrating;
        ProjectLoadingMessage = message;
        ProjectHydrationChanged?.Invoke();
    }

    public bool IsCurrentUserProjectOwner(Project project)
    {
        return CurrentUser != null && project.OwnerUserId == CurrentUser.Id;
    }

    public bool CanCurrentUserEditProject(Project project)
    {
        if (IsCurrentUserProjectOwner(project)) return true;

        return _projectRoles.TryGetValue(project.Id, out var role) &&
               role.Equals("Editor", StringComparison.OrdinalIgnoreCase);
    }

    public ProjectSessionService(
        CustomAuthenticationStateProvider authProvider,
        IProjectRepository projectRepository,
        IUserSessionStateRepository userSessionStateRepository,
        IFlowsheetTypeRegistry? flowsheetTypeRegistry = null,
        IEquipmentNamingService? namingService = null,
        IHttpService? httpService = null,
        ProjectRealtimeService? realtimeService = null)
    {
        _authProvider = authProvider;
        _projectRepository = projectRepository;
        _userSessionStateRepository = userSessionStateRepository;
        _flowsheetTypeRegistry = flowsheetTypeRegistry ?? new FlowsheetTypeRegistry();
        _namingService = namingService ?? new EquipmentNamingService();
        _httpService = httpService;
        _realtimeService = realtimeService;

        if (_realtimeService != null)
        {
            _realtimeService.ProjectChangedReceived += OnRealtimeProjectChanged;
            _realtimeService.PresenceChangedReceived += OnRealtimePresenceChanged;
        }
    }

    public async Task InitializeAsync()
    {
        // 1. Asegurar que el usuario está cargado. Si aún no lo está, forzar la carga.
        if (CurrentUser == null)
            await _authProvider.GetAuthenticationStateAsync();

        if (CurrentUser == null)
            throw new InvalidOperationException("No current user available.");

        var userProjects = await LoadUserProjectsAsync();
        await InitializeFromProjectsAsync(userProjects);
    }

    public async Task InitializeFromProjectsAsync(IReadOnlyList<Project> userProjects)
    {
        await EnsureCurrentUserAsync();
        if (CurrentUser == null)
            throw new InvalidOperationException("No current user available.");

        var session = await LoadWorkspaceStateAsync();
        _workspaceState = session;

        if (userProjects.Count == 0)
        {
            // 4a. Si no tiene proyectos, la UI debe mostrar estado vacío.
            CurrentProject = null;
            ActiveFlowsheet = null;
            ProjectChanged?.Invoke();
            return;
        }
        else if (userProjects.Count == 1)
        {
            // 4b. Si tiene exactamente 1, cargarlo
            CurrentProject = userProjects.First();
        }
        else
        {
            // 4c. Si tiene 2 o más, intentar cargar el último activo según la sesión
            var lastProject = _workspaceState?.LastProjectId != null
                ? userProjects.FirstOrDefault(p => p.Id == _workspaceState.LastProjectId.Value)
                : null;

            CurrentProject = lastProject ?? userProjects.First();
        }

        // 5. Establecer el flowsheet activo
        if (_workspaceState?.LastFlowsheetId != null)
        {
            ActiveFlowsheet = CurrentProject.GetFlowsheet(_workspaceState.LastFlowsheetId.Value);
        }

        ActiveFlowsheet ??= CurrentProject.Flowsheets.FirstOrDefault();
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

        CurrentProject = project;

        // Asegurar que el proyecto tenga al menos un flowsheet
        if (CurrentProject.Flowsheets.Count == 0)
        {
            ActiveFlowsheet = CurrentProject.CreateFlowsheet("PFD 1", "PFD");
        }
        else
        {
            ActiveFlowsheet = CurrentProject.Flowsheets.FirstOrDefault();
        }

        await SaveSessionAsync(ActiveFlowsheet?.Id);
        await JoinRealtimeProjectAsync(CurrentProject.Id);
        ProjectChanged?.Invoke();
    }

    public async Task<List<Project>> LoadUserProjectsAsync()
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
            var localProjects = await _projectRepository.GetByUserIdAsync(CurrentUser.Id);
            return localProjects.OrderByDescending(project => project.CreatedAt).Cast<Project>().ToList();
        }

        _projectRoles.Clear();
        var projects = new List<Project>();
        foreach (var summary in summariesResult.Data.OrderByDescending(project => project.UpdatedOnUtc))
        {
            _projectRoles[summary.Id] = summary.CurrentUserRole;
            var project = await LoadProjectAsync(summary.Id);
            if (project != null)
            {
                projects.Add(project);
            }
        }

        return projects;
    }

    public async Task<Project?> LoadProjectAsync(Guid projectId)
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

        return await FromPersistenceDtoAsync(document, CurrentUser);
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

        foreach (var diagram in ToDiagramDtos(project))
        {
            await PersistDiagramCreatedAsync(project.Id, diagram);
        }
    }

    public async Task SetActiveFlowsheetAsync(IFlowsheet flowsheet)
    {
        if (flowsheet.Project.Id != CurrentProject?.Id)
            throw new InvalidOperationException("Flowsheet does not belong to the current project.");

        ActiveFlowsheet = flowsheet;
        ProjectChanged?.Invoke();
        await SaveSessionAsync(flowsheet.Id);
        await UpdateRealtimeActiveDiagramAsync();
    }

    public async Task<IFlowsheet> CreateFlowsheetAsync(string typeCode, string? baseName = null, string? diagramNumber = null)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

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

    public async Task UpdateProjectConfigurationAsync(IProjectConfiguration configuration, bool renameExistingEquipment = false)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        ValidateDiagramNumbersForConfiguration(CurrentProject, configuration);
        CurrentProject.UpdateConfiguration(configuration);
        _namingService.SetConfiguration(configuration.NamingConfig);

        if (renameExistingEquipment)
        {
            RenameExistingEquipment(CurrentProject, configuration.NamingConfig);
        }

        await SaveSessionAsync(ActiveFlowsheet?.Id);
        await PersistCurrentProjectConfigurationAsync();
        await PersistDiagramNumbersForNamingAsync(configuration);
        ProjectChanged?.Invoke();
    }

    public async Task DeleteFlowsheetAsync(Guid flowsheetId)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

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
        foreach (var affectedFlowsheetId in affectedFlowsheetIds)
        {
            var affectedFlowsheet = CurrentProject.GetFlowsheet(affectedFlowsheetId);
            if (affectedFlowsheet != null)
            {
                await PersistDiagramUpdatedAsync(affectedFlowsheet);
            }
        }

        await PersistDiagramDeletedAsync(CurrentProject.Id, flowsheetId);
        ProjectChanged?.Invoke();
    }

    public async Task<bool> DeleteProjectAsync(Project project)
    {
        if (_httpService == null) return true;

        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return false;

        var result = await _httpService.PostAsync<DeleteProjectRequest>(new DeleteProjectRequest
        {
            ProjectId = project.Id
        });

        return result.Succeeded;
    }

    public void ReorderFlowsheet(IFlowsheet flowsheet, int newIndex)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        CurrentProject.ReorderFlowsheet(flowsheet, newIndex);
        ProjectChanged?.Invoke();
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
        return HandleRealtimeProjectChangedAsync(realtimeEvent);
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
            if (_lastAppliedRealtimeProjectId == realtimeEvent.ProjectId &&
                realtimeEvent.Version <= _lastAppliedRealtimeVersion)
            {
                return;
            }

            var activeFlowsheetId = ActiveFlowsheet?.Id;
            var reloadedDocument = await LoadProjectDocumentAsync(realtimeEvent.ProjectId);
            if (reloadedDocument == null)
            {
                _projectRoles.Remove(realtimeEvent.ProjectId);
                CurrentProject = null;
                ActiveFlowsheet = null;
                ProjectListRefreshRequested?.Invoke();
                ProjectChanged?.Invoke();
                return;
            }

            var reloadedProject = await FromPersistenceDtoAsync(reloadedDocument, CurrentUser);
            CurrentProject = reloadedProject;
            _lastAppliedRealtimeVersion = reloadedDocument.Version;
            _lastAppliedRealtimeProjectId = reloadedDocument.Id;
            RefreshCurrentUserProjectRole(realtimeEvent.ProjectId, reloadedDocument);
            ActiveFlowsheet = activeFlowsheetId.HasValue
                ? CurrentProject.GetFlowsheet(activeFlowsheetId.Value)
                : null;
            ActiveFlowsheet ??= CurrentProject.Flowsheets.FirstOrDefault();
            await UpdateRealtimeActiveDiagramAsync();

            ProjectReloaded?.Invoke(CurrentProject);
            if (IsProjectAccessChange(realtimeEvent))
            {
                ProjectListRefreshRequested?.Invoke();
            }

            ProjectChanged?.Invoke();
        }
        finally
        {
            _realtimeReloadLock.Release();
        }
    }

    private static bool IsProjectAccessChange(ProjectRealtimeEventDto realtimeEvent)
    {
        return realtimeEvent.ChangeType is "SharingUpdated" or "ProjectDeleted";
    }

    private async Task<ProjectDocumentDto?> LoadProjectDocumentAsync(Guid projectId)
    {
        await EnsureCurrentUserAsync();
        if (CurrentUser == null || _httpService == null) return null;

        var result = await _httpService.PostAsync<GetProjectRequest, ProjectDocumentDto>(new GetProjectRequest
        {
            ProjectId = projectId
        });

        return result.Succeeded ? result.Data : null;
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

    public static string? GetDiagramNumberConfigurationError(Project project, IProjectConfiguration configuration)
    {
        if (!RequiresDiagramNumberForNaming(configuration.NamingConfig))
        {
            return null;
        }

        var missing = project.Flowsheets
            .Where(flowsheet => string.IsNullOrWhiteSpace(flowsheet.DiagramNumber))
            .Select(flowsheet => flowsheet.Name)
            .ToList();

        if (missing.Count > 0)
        {
            return $"Diagram-based naming requires a unique diagram number for every diagram. Missing: {string.Join(", ", missing)}.";
        }

        var duplicate = project.Flowsheets
            .GroupBy(flowsheet => flowsheet.DiagramNumber.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate != null)
        {
            return $"Diagram number '{duplicate.Key}' is already used by more than one diagram.";
        }

        return null;
    }

    private static void ValidateDiagramNumbersForConfiguration(Project project, IProjectConfiguration configuration)
    {
        var error = GetDiagramNumberConfigurationError(project, configuration);
        if (error != null)
        {
            throw new InvalidOperationException(error);
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

        foreach (var item in elements)
        {
            project.EquipmentRegistry.Unregister(item.Element.Id);
            SetElementName(item.Element, $"__renaming_{item.Element.Id:N}");
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
    }

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

    private async Task PersistCurrentProjectConfigurationAsync()
    {
        if (_httpService == null || CurrentProject == null) return;

        await EnsureCurrentUserAsync();
        if (CurrentUser == null) return;

        var request = new UpdateProjectConfigurationRequest
        {
            ProjectId = CurrentProject.Id,
            Name = CurrentProject.Name,
            Configuration = ToPersistenceDto(CurrentProject.Configuration)
        };

        await _httpService.PostAsync<UpdateProjectConfigurationRequest, ProjectDocumentDto>(request, showSnackbar: false);
    }

    private async Task PersistDiagramCreatedAsync(Guid projectId, ProjectDiagramDto diagram)
    {
        if (_httpService == null) return;

        await _httpService.PostAsync<CreateDiagramRequest, ProjectDocumentDto>(new CreateDiagramRequest
        {
            ProjectId = projectId,
            Diagram = diagram
        }, showSnackbar: false);
    }

    private async Task PersistDiagramUpdatedAsync(IFlowsheet flowsheet)
    {
        if (_httpService == null || CurrentProject == null) return;

        await _httpService.PostAsync<UpdateDiagramRequest, ProjectDocumentDto>(new UpdateDiagramRequest
        {
            ProjectId = CurrentProject.Id,
            Diagram = ToDiagramDto(flowsheet, GetFlowsheetOrder(CurrentProject, flowsheet))
        }, showSnackbar: false);
    }

    private async Task PersistDiagramDeletedAsync(Guid projectId, Guid diagramId)
    {
        if (_httpService == null) return;

        await _httpService.PostAsync<DeleteDiagramRequest, ProjectDocumentDto>(new DeleteDiagramRequest
        {
            ProjectId = projectId,
            DiagramId = diagramId
        }, showSnackbar: false);
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

    private async Task<Project> FromPersistenceDtoAsync(ProjectDocumentDto document, User currentUser)
    {
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
            foreach (var diagram in diagrams)
            {
                var flowsheet = project.CreateFlowsheet(
                    string.IsNullOrWhiteSpace(diagram.Name) ? "PFD 1" : diagram.Name,
                    string.IsNullOrWhiteSpace(diagram.TypeCode) ? "PFD" : diagram.TypeCode,
                    diagram.Id == Guid.Empty ? null : diagram.Id);

                flowsheet.DiagramNumber = diagram.DiagramNumber?.Trim() ?? string.Empty;
                flowsheet.DiagramWidth = flowsheet.DiagramWidth > 0 ? flowsheet.DiagramWidth : 5000;
                flowsheet.DiagramHeight = flowsheet.DiagramHeight > 0 ? flowsheet.DiagramHeight : 5000;
                ApplyCanvasState(project, flowsheet, diagram.CanvasStateJson);
            }

            RestoreInterFlowsheetConnections(project);

            SetProjectHydration(true, "Recalculating simulation...");
            project.RunSimulation();

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
        var unitSystems = BuildUnitSystemsFromSnapshots(
            Deserialize(configuration.UnitSystemsJson, new List<ProjectUnitSystemSnapshot>()));

        return new ProjectConfiguration(
            unitSystems: unitSystems,
            activeUnitSystemName: configuration.ActiveUnitSystemName,
            cameraDefaults: FromSnapshot(Deserialize(configuration.CameraConfigurationJson, ToSnapshot(new CameraConfiguration()))),
            namingConfig: FromSnapshot(Deserialize(configuration.NamingConfigurationJson, ToSnapshot(new NamingConfiguration()))),
            thermodynamicMethodId: configuration.ThermodynamicMethodId,
            thermodynamicMethod: thermodynamicMethod,
            reportConfig: FromSnapshot(Deserialize(configuration.ReportConfigurationJson, ToSnapshot(new ReportConfiguration()))),
            equipmentDesignConfig: FromSnapshot(Deserialize(configuration.EquipmentDesignConfigurationJson, ToSnapshot(new EquipmentDesignConfiguration()))),
            plantElevation: new UnitSystem.Length(
                configuration.PlantElevationValue,
                ResolveUnit(configuration.PlantElevationUnit, LengthUnits.Meter)));
    }

    private static List<ProjectDiagramDto> ToDiagramDtos(Project project)
    {
        return project.Flowsheets
            .Select(ToDiagramDto)
            .ToList();
    }

    private static ProjectDiagramDto ToDiagramDto(IFlowsheet flowsheet, int index)
    {
        return new ProjectDiagramDto
        {
            Id = flowsheet.Id,
            Name = flowsheet.Name,
            TypeCode = flowsheet.TypeCode,
            DiagramNumber = string.IsNullOrWhiteSpace(flowsheet.DiagramNumber) ? null : flowsheet.DiagramNumber,
            Order = index < 0 ? 0 : index,
            CanvasStateJson = Serialize(ToCanvasState(flowsheet.Project, flowsheet))
        };
    }

    public async Task PersistDiagramVisualStateAsync(IFlowsheet flowsheet)
    {
        if (_httpService == null || CurrentProject == null) return;
        if (flowsheet.Project.Id != CurrentProject.Id) return;
        if (!CanCurrentUserEditProject(CurrentProject)) return;

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

        await _visualPersistenceLock.WaitAsync();
        try
        {
            var result = await _httpService.PostAsync<UpdateDiagramRequest, ProjectDocumentDto>(new UpdateDiagramRequest
            {
                ProjectId = CurrentProject.Id,
                Diagram = ToDiagramDto(flowsheet, GetFlowsheetOrder(CurrentProject, flowsheet))
            }, showSnackbar: false);
            LogVisualSaveFailure(result, flowsheet.Id.ToString());
        }
        finally
        {
            _visualPersistenceLock.Release();
            debounce.Dispose();
        }
    }

    public async Task PersistDiagramVisualStatesAsync(IReadOnlyCollection<IFlowsheet> flowsheets)
    {
        if (_httpService == null || CurrentProject == null) return;
        if (!CanCurrentUserEditProject(CurrentProject)) return;

        var changedFlowsheets = flowsheets
            .Where(flowsheet => flowsheet.Project.Id == CurrentProject.Id)
            .DistinctBy(flowsheet => flowsheet.Id)
            .ToList();
        if (changedFlowsheets.Count == 0) return;
        if (changedFlowsheets.Count == 1)
        {
            await PersistDiagramVisualStateAsync(changedFlowsheets[0]);
            return;
        }

        foreach (var flowsheet in changedFlowsheets)
        {
            CancelVisualSaveDebounce(flowsheet.Id);
        }

        await _visualPersistenceLock.WaitAsync();
        try
        {
            var result = await _httpService.PostAsync<UpdateDiagramsRequest, ProjectDocumentDto>(
                new UpdateDiagramsRequest
                {
                    ProjectId = CurrentProject.Id,
                    Diagrams = changedFlowsheets
                        .Select(flowsheet => ToDiagramDto(
                            flowsheet,
                            GetFlowsheetOrder(CurrentProject, flowsheet)))
                        .ToList()
                },
                showSnackbar: false);
            LogVisualSaveFailure(result, string.Join(", ", changedFlowsheets.Select(flowsheet => flowsheet.Id)));
        }
        finally
        {
            _visualPersistenceLock.Release();
        }
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

        Console.Error.WriteLine(
            $"Visual autosave failed for diagram(s) {diagramIds}: {string.Join("; ", result.Messages)}");
    }

    private static DiagramCanvasStateSnapshot ToCanvasState(IProject project, IFlowsheet flowsheet)
    {
        return new DiagramCanvasStateSnapshot(
            new DiagramCameraSnapshot(
                flowsheet.Zoom,
                flowsheet.PanX,
                flowsheet.PanY,
                flowsheet.DiagramWidth,
                flowsheet.DiagramHeight,
                flowsheet.GridSize,
                flowsheet.GlobalScale),
            flowsheet.Elements
                .Select(reference =>
                {
                    var element = project.EquipmentRegistry.GetById(reference.ElementId);
                    return element == null
                        ? null
                        : new DiagramElementSnapshot(
                            element.Id,
                            element.Type.ToString(),
                            element.Name,
                            element.Label,
                            FacadeStateSerializer.Serialize(element.Facade),
                            ToFormulaSpecificationSnapshots(element.Facade),
                            reference.X,
                            reference.Y,
                            element.Width,
                            element.Height,
                            reference.RotationAngle,
                            reference.ZIndex,
                            reference.IsFlippedHorizontal,
                            reference.IsFlippedVertical,
                            element.ShowLabel,
                            element.IsLocked,
                            ToOffPageConnectorSnapshot(reference, element));
                })
                .Where(snapshot => snapshot != null)
                .Cast<DiagramElementSnapshot>()
                .ToList(),
            flowsheet.Pipes
                .Select(pipe => new DiagramPipeSnapshot(
                    pipe.Id,
                    pipe.SourceElementId,
                    pipe.TargetElementId,
                    pipe.SourcePortName,
                    pipe.TargetPortName))
                .ToList());
    }

    private static void ApplyCanvasState(Project project, IFlowsheet flowsheet, string? canvasStateJson)
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
                offPageConnector.TargetAreaId = elementSnapshot.OffPageConnector.TargetFlowsheetId;
                offPageConnector.TargetConnectorId = elementSnapshot.OffPageConnector.TargetConnectorId;
                offPageConnector.TargetAreaName = elementSnapshot.OffPageConnector.TargetFlowsheetName;
                offPageConnector.ConnectedEquipmentName = elementSnapshot.OffPageConnector.ConnectedEquipmentName;
                offPageConnector.RefreshPorts();
            }

            RegisterCanvasElementInSolver(project, element);
            project.AddEquipment(element);
            FacadeStateSerializer.Apply(element.Facade, elementSnapshot.FacadeStateJson);
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
                    elementSnapshot.OffPageConnector.IsOutlet)
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
            var source = project.EquipmentRegistry.GetById(pipeSnapshot.SourceElementId);
            var target = project.EquipmentRegistry.GetById(pipeSnapshot.TargetElementId);
            if (source == null || target == null) continue;
            if (string.IsNullOrWhiteSpace(pipeSnapshot.SourcePortName) ||
                string.IsNullOrWhiteSpace(pipeSnapshot.TargetPortName))
            {
                continue;
            }

            source.Connect(pipeSnapshot.SourcePortName, target, pipeSnapshot.TargetPortName);
            flowsheet.AddPipe(new PipeReference(
                pipeSnapshot.SourceElementId,
                pipeSnapshot.TargetElementId,
                pipeSnapshot.SourcePortName,
                pipeSnapshot.TargetPortName,
                pipeSnapshot.Id == Guid.Empty ? null : pipeSnapshot.Id));
        }

        foreach (var pending in pendingFormulaSpecifications)
        {
            RestoreFormulaSpecifications(project, pending.Equipment, pending.Specifications);
        }
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
                connectorReference.IsOutlet);
        }

        return element is OffPageConnectorElement connector
            ? new OffPageConnectorSnapshot(
                connector.TargetAreaId,
                connector.TargetConnectorId,
                connector.TargetAreaName,
                connector.ConnectedEquipmentName,
                connector.IsOutlet)
            : null;
    }

    private static void RestoreInterFlowsheetConnections(Project project)
    {
        var restoredConnectorIds = new HashSet<Guid>();

        foreach (var sourceFlowsheet in project.Flowsheets)
        {
            foreach (var sourceReference in sourceFlowsheet.Elements.OfType<IOffPageConnectorReference>())
            {
                if (!sourceReference.TargetFlowsheetId.HasValue ||
                    !sourceReference.TargetConnectorId.HasValue ||
                    !restoredConnectorIds.Add(sourceReference.ElementId))
                {
                    continue;
                }

                var targetFlowsheet = project.GetFlowsheet(sourceReference.TargetFlowsheetId.Value);
                var targetReference = targetFlowsheet?.Elements
                    .OfType<IOffPageConnectorReference>()
                    .FirstOrDefault(reference => reference.ElementId == sourceReference.TargetConnectorId.Value);
                if (targetFlowsheet == null || targetReference == null)
                {
                    continue;
                }

                restoredConnectorIds.Add(targetReference.ElementId);
                project.AddInterFlowsheetConnection(new InterFlowsheetConnection(
                    sourceFlowsheet.Id,
                    targetFlowsheet.Id,
                    sourceReference.ElementId,
                    targetReference.ElementId));

                RestoreInterFlowsheetSimulationConnection(
                    project,
                    sourceFlowsheet,
                    sourceReference,
                    targetFlowsheet,
                    targetReference);
            }
        }
    }

    private static void RestoreInterFlowsheetSimulationConnection(
        Project project,
        IFlowsheet sourceFlowsheet,
        IOffPageConnectorReference sourceConnector,
        IFlowsheet targetFlowsheet,
        IOffPageConnectorReference targetConnector)
    {
        var sourceEndpoint = GetConnectedEndpoint(project, sourceFlowsheet, sourceConnector.ElementId);
        var targetEndpoint = GetConnectedEndpoint(project, targetFlowsheet, targetConnector.ElementId);
        if (sourceEndpoint.Element == null || targetEndpoint.Element == null) return;

        if (sourceEndpoint.Element.Facade is IEquipmentFacade &&
            targetEndpoint.Element.Facade is IFacadeStream targetStream)
        {
            sourceEndpoint.Element.AttachConnection(sourceEndpoint.PortName, targetStream);
        }
        else if (targetEndpoint.Element.Facade is IEquipmentFacade &&
                 sourceEndpoint.Element.Facade is IFacadeStream sourceStream)
        {
            targetEndpoint.Element.AttachConnection(targetEndpoint.PortName, sourceStream);
        }
    }

    private static (IVisualElement? Element, string PortName) GetConnectedEndpoint(
        Project project,
        IFlowsheet flowsheet,
        Guid connectorId)
    {
        var pipe = flowsheet.Pipes.FirstOrDefault(candidate =>
            candidate.SourceElementId == connectorId || candidate.TargetElementId == connectorId);
        if (pipe == null) return (null, string.Empty);

        return pipe.SourceElementId == connectorId
            ? (project.EquipmentRegistry.GetById(pipe.TargetElementId), pipe.TargetPortName)
            : (project.EquipmentRegistry.GetById(pipe.SourceElementId), pipe.SourcePortName);
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
        return new ProjectBasicConfigurationDto
        {
            ThermodynamicMethodId = configuration.ThermodynamicMethodId == Guid.Empty
                ? null
                : configuration.ThermodynamicMethodId,
            PlantElevationValue = configuration.PlantElevation.Value,
            PlantElevationUnit = UnitName(configuration.PlantElevation.Unit),
            ActiveUnitSystemName = configuration.ActiveUnitSystemName,
            UnitSystemsJson = Serialize(configuration.UnitSystems.Select(ToSnapshot).ToList()),
            CameraConfigurationJson = Serialize(ToSnapshot(configuration.CameraDefaults)),
            NamingConfigurationJson = Serialize(ToSnapshot(configuration.NamingConfig)),
            ReportConfigurationJson = Serialize(ToSnapshot(configuration.ReportConfig)),
            EquipmentDesignConfigurationJson = Serialize(ToSnapshot(configuration.EquipmentDesignConfig))
        };
    }

    private static ProjectUnitSystemSnapshot ToSnapshot(IProjectUnitSystem system)
    {
        return new ProjectUnitSystemSnapshot(
            system.Name,
            system.IsBuiltIn,
            ToSnapshot(system.Units));
    }

    private static UnitConfigurationSnapshot ToSnapshot(IUnitConfiguration units)
    {
        return new UnitConfigurationSnapshot(
            UnitName(units.DefaultPressureUnit),
            UnitName(units.DefaultTemperatureUnit),
            UnitName(units.DefaultMassFlowUnit),
            UnitName(units.DefaultMolarFlowUnit),
            UnitName(units.DefaultEnergyUnit),
            UnitName(units.DefaultPowerUnit),
            UnitName(units.DefaultLengthUnit),
            UnitName(units.DefaultDiameterUnit),
            UnitName(units.DefaultSurfaceUnit),
            UnitName(units.DefaultVolumeUnit),
            UnitName(units.DefaultTimeUnit),
            UnitName(units.DefaultVelocityUnit),
            UnitName(units.DefaultMassUnit),
            UnitName(units.DefaultForceUnit),
            UnitName(units.DefaultElectricUnit),
            UnitName(units.DefaultMotorVelocityUnit),
            UnitName(units.DefaultAmountOfSubstanceUnit),
            UnitName(units.DefaultHeatTransferCoefficientUnit),
            UnitName(units.DefaultDensityUnit),
            UnitName(units.DefaultMolarDensityUnit),
            UnitName(units.DefaultMassVolumeSpecificUnit),
            UnitName(units.DefaultMolarVolumeSpecificUnit),
            UnitName(units.DefaultPressureDropLengthUnit),
            UnitName(units.DefaultPressureDropUnit),
            UnitName(units.DefaultViscosityUnit),
            UnitName(units.DefaultThermalConductivityUnit),
            UnitName(units.DefaultVolumeEnergyUnit),
            UnitName(units.DefaultMassEnergyUnit),
            UnitName(units.DefaultMolarEnergyUnit),
            UnitName(units.DefaultMassEntropyUnit),
            UnitName(units.DefaultMolarEntropyUnit),
            UnitName(units.DefaultHeatSurfaceFlowUnit),
            UnitName(units.DefaultVolumetricFlowUnit),
            UnitName(units.DefaultEnergyFlowUnit),
            UnitName(units.DefaultSuperficialTensionUnit));
    }

    private static CameraConfigurationSnapshot ToSnapshot(ICameraConfiguration camera)
    {
        return new CameraConfigurationSnapshot(
            camera.DefaultZoom,
            camera.DefaultPanX,
            camera.DefaultPanY,
            camera.GlobalScale,
            camera.GridSize,
            camera.MinZoom,
            camera.MaxZoom);
    }

    private static NamingConfigurationSnapshot ToSnapshot(INamingConfiguration naming)
    {
        return new NamingConfigurationSnapshot(
            naming.Mode.ToString(),
            naming.Pattern,
            naming.StartingNumber,
            naming.BaseNumber,
            naming.AreaPrefix,
            naming.CounterScope.ToString(),
            naming.PatternParts.Select(part => new NamingPatternPartSnapshot(part.Kind.ToString(), part.Value)).ToList(),
            new Dictionary<string, string>(naming.PrefixesByEquipmentType, StringComparer.OrdinalIgnoreCase));
    }

    private static ReportConfigurationSnapshot ToSnapshot(IReportConfiguration report)
    {
        return new ReportConfigurationSnapshot(
            report.AvailableTemplates.ToList(),
            report.DefaultFormat,
            report.AutoExportOnSimulation);
    }

    private static EquipmentDesignConfigurationSnapshot ToSnapshot(IEquipmentDesignConfiguration design)
    {
        return new EquipmentDesignConfigurationSnapshot(design.Standard, design.RatingBasis);
    }

    private static ProjectUnitSystem FromSnapshot(ProjectUnitSystemSnapshot snapshot)
    {
        return new ProjectUnitSystem(
            string.IsNullOrWhiteSpace(snapshot.Name) ? "Custom" : snapshot.Name,
            FromSnapshot(snapshot.Units),
            snapshot.IsBuiltIn);
    }

    private static List<IProjectUnitSystem> BuildUnitSystemsFromSnapshots(List<ProjectUnitSystemSnapshot> snapshots)
    {
        var systems = new List<IProjectUnitSystem>
        {
            ProjectUnitSystem.SI(),
            ProjectUnitSystem.English()
        };

        foreach (var snapshot in snapshots.Where(item => !item.IsBuiltIn))
        {
            var name = string.IsNullOrWhiteSpace(snapshot.Name) ? "Custom" : snapshot.Name;
            if (systems.Any(system => system.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            systems.Add(FromSnapshot(snapshot));
        }

        return systems;
    }

    private static UnitConfiguration FromSnapshot(UnitConfigurationSnapshot snapshot)
    {
        return new UnitConfiguration(
            ResolveUnit(snapshot.Pressure, PressureUnits.Bara),
            ResolveUnit(snapshot.Temperature, TemperatureUnits.DegreeCelcius),
            ResolveUnit(snapshot.MassFlow, MassFlowUnits.Kg_hr),
            ResolveUnit(snapshot.MolarFlow, MolarFlowUnits.Kgmol_hr),
            ResolveUnit(snapshot.Energy, EnergyUnits.KiloJoule),
            ResolveUnit(snapshot.Power, PowerUnits.KiloWatt),
            ResolveUnit(snapshot.Length, LengthUnits.Meter),
            ResolveUnit(snapshot.Density, MassDensityUnits.Kg_m3),
            ResolveUnit(snapshot.Viscosity, ViscosityUnits.cPoise),
            ResolveUnit(snapshot.ThermalConductivity, ThermalConductivityUnits.W_m_K),
            ResolveUnit(snapshot.Diameter, DiameterUnits.MilliMeter),
            ResolveUnit(snapshot.Surface, SurfaceUnits.Meter2),
            ResolveUnit(snapshot.Volume, VolumeUnits.Meter3),
            ResolveUnit(snapshot.Time, TimeUnits.Second),
            ResolveUnit(snapshot.Velocity, VelocityUnits.MeterPerSecond),
            ResolveUnit(snapshot.Mass, MassUnits.KiloGram),
            ResolveUnit(snapshot.Force, ForceUnits.Newton),
            ResolveUnit(snapshot.Electric, ElectricUnits.Ampere),
            ResolveUnit(snapshot.MotorVelocity, MotorVelocityUnits.RPM),
            ResolveUnit(snapshot.AmountOfSubstance, AmountOfSubstanceUnits.KMole),
            ResolveUnit(snapshot.HeatTransferCoefficient, HeatTransferCoefficientUnits.Watt_m2_K),
            ResolveUnit(snapshot.MolarDensity, MolarDensityUnits.Kgmol_m3),
            ResolveUnit(snapshot.MassVolumeSpecific, MassVolumeSpecificUnits.m3_Kg),
            ResolveUnit(snapshot.MolarVolumeSpecific, MolarVolumeSpecificUnits.m3_Kgmol),
            ResolveUnit(snapshot.PressureDropLength, PressureDropLengthUnits.Kpa_m),
            ResolveUnit(snapshot.PressureDrop, PressureDropUnits.KiloPascal),
            ResolveUnit(snapshot.VolumeEnergy, VolumeEnergyUnits.KJ_m3),
            ResolveUnit(snapshot.MassEnergy, MassEnergyUnits.KJ_Kg),
            ResolveUnit(snapshot.MolarEnergy, MolarEnergyUnits.KJ_Kgmol),
            ResolveUnit(snapshot.MassEntropy, MassEntropyUnits.KJ_Kg_C),
            ResolveUnit(snapshot.MolarEntropy, MolarEntropyUnits.KJ_Kgmol_C),
            ResolveUnit(snapshot.HeatSurfaceFlow, HeatSurfaceFlowUnits.W_m2),
            ResolveUnit(snapshot.VolumetricFlow, VolumetricFlowUnits.m3_hr),
            ResolveUnit(snapshot.EnergyFlow, EnergyFlowUnits.KJ_hr),
            ResolveUnit(snapshot.SuperficialTension, SuperficialTensionUnits.N_m));
    }

    private static CameraConfiguration FromSnapshot(CameraConfigurationSnapshot snapshot)
    {
        return new CameraConfiguration(
            snapshot.DefaultZoom,
            snapshot.DefaultPanX,
            snapshot.DefaultPanY,
            snapshot.GlobalScale,
            snapshot.GridSize,
            snapshot.MinZoom,
            snapshot.MaxZoom);
    }

    private static NamingConfiguration FromSnapshot(NamingConfigurationSnapshot snapshot)
    {
        return new NamingConfiguration(
            mode: ParseEnum(snapshot.Mode, NamingMode.ProjectSequential),
            pattern: snapshot.Pattern,
            startingNumber: snapshot.StartingNumber,
            baseNumber: snapshot.BaseNumber,
            areaPrefix: snapshot.AreaPrefix,
            counterScope: ParseEnum(snapshot.CounterScope, NamingCounterScope.Project),
            patternParts: snapshot.PatternParts
                .Select(part => new NamingPatternPart(ParseEnum(part.Kind, NamingPatternPartKind.Literal), part.Value))
                .ToList(),
            prefixesByEquipmentType: snapshot.PrefixesByEquipmentType);
    }

    private static ReportConfiguration FromSnapshot(ReportConfigurationSnapshot snapshot)
    {
        return new ReportConfiguration(
            snapshot.AvailableTemplates,
            snapshot.DefaultFormat,
            snapshot.AutoExportOnSimulation);
    }

    private static EquipmentDesignConfiguration FromSnapshot(EquipmentDesignConfigurationSnapshot snapshot)
    {
        return new EquipmentDesignConfiguration(snapshot.Standard, snapshot.RatingBasis);
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, PersistenceJsonOptions);
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

    private static UnitMeasure ResolveUnit(string? unitName, UnitMeasure fallback)
    {
        if (string.IsNullOrWhiteSpace(unitName)) return fallback;

        try
        {
            return UnitManager.GetUnitByName(unitName);
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

    private static string UnitName(UnitMeasure? unit)
    {
        return string.IsNullOrWhiteSpace(unit?.Name) ? UnitMeasure.None.Name : unit.Name;
    }

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
        bool IsOutlet);

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

    private sealed record ProjectUnitSystemSnapshot(string Name, bool IsBuiltIn, UnitConfigurationSnapshot Units);

    private sealed class UnitConfigurationSnapshot
    {
        public UnitConfigurationSnapshot()
        {
        }

        public UnitConfigurationSnapshot(
            string pressure,
            string temperature,
            string massFlow,
            string molarFlow,
            string energy,
            string power,
            string length,
            string diameter,
            string surface,
            string volume,
            string time,
            string velocity,
            string mass,
            string force,
            string electric,
            string motorVelocity,
            string amountOfSubstance,
            string heatTransferCoefficient,
            string density,
            string molarDensity,
            string massVolumeSpecific,
            string molarVolumeSpecific,
            string pressureDropLength,
            string pressureDrop,
            string viscosity,
            string thermalConductivity,
            string volumeEnergy,
            string massEnergy,
            string molarEnergy,
            string massEntropy,
            string molarEntropy,
            string heatSurfaceFlow,
            string volumetricFlow,
            string energyFlow,
            string superficialTension)
        {
            Pressure = pressure;
            Temperature = temperature;
            MassFlow = massFlow;
            MolarFlow = molarFlow;
            Energy = energy;
            Power = power;
            Length = length;
            Diameter = diameter;
            Surface = surface;
            Volume = volume;
            Time = time;
            Velocity = velocity;
            Mass = mass;
            Force = force;
            Electric = electric;
            MotorVelocity = motorVelocity;
            AmountOfSubstance = amountOfSubstance;
            HeatTransferCoefficient = heatTransferCoefficient;
            Density = density;
            MolarDensity = molarDensity;
            MassVolumeSpecific = massVolumeSpecific;
            MolarVolumeSpecific = molarVolumeSpecific;
            PressureDropLength = pressureDropLength;
            PressureDrop = pressureDrop;
            Viscosity = viscosity;
            ThermalConductivity = thermalConductivity;
            VolumeEnergy = volumeEnergy;
            MassEnergy = massEnergy;
            MolarEnergy = molarEnergy;
            MassEntropy = massEntropy;
            MolarEntropy = molarEntropy;
            HeatSurfaceFlow = heatSurfaceFlow;
            VolumetricFlow = volumetricFlow;
            EnergyFlow = energyFlow;
            SuperficialTension = superficialTension;
        }

        public string Pressure { get; set; } = UnitName(PressureUnits.Bara);
        public string Temperature { get; set; } = UnitName(TemperatureUnits.DegreeCelcius);
        public string MassFlow { get; set; } = UnitName(MassFlowUnits.Kg_hr);
        public string MolarFlow { get; set; } = UnitName(MolarFlowUnits.Kgmol_hr);
        public string Energy { get; set; } = UnitName(EnergyUnits.KiloJoule);
        public string Power { get; set; } = UnitName(PowerUnits.KiloWatt);
        public string Length { get; set; } = UnitName(LengthUnits.Meter);
        public string Diameter { get; set; } = UnitName(DiameterUnits.MilliMeter);
        public string Surface { get; set; } = UnitName(SurfaceUnits.Meter2);
        public string Volume { get; set; } = UnitName(VolumeUnits.Meter3);
        public string Time { get; set; } = UnitName(TimeUnits.Second);
        public string Velocity { get; set; } = UnitName(VelocityUnits.MeterPerSecond);
        public string Mass { get; set; } = UnitName(MassUnits.KiloGram);
        public string Force { get; set; } = UnitName(ForceUnits.Newton);
        public string Electric { get; set; } = UnitName(ElectricUnits.Ampere);
        public string MotorVelocity { get; set; } = UnitName(MotorVelocityUnits.RPM);
        public string AmountOfSubstance { get; set; } = UnitName(AmountOfSubstanceUnits.KMole);
        public string HeatTransferCoefficient { get; set; } = UnitName(HeatTransferCoefficientUnits.Watt_m2_K);
        public string Density { get; set; } = UnitName(MassDensityUnits.Kg_m3);
        public string MolarDensity { get; set; } = UnitName(MolarDensityUnits.Kgmol_m3);
        public string MassVolumeSpecific { get; set; } = UnitName(MassVolumeSpecificUnits.m3_Kg);
        public string MolarVolumeSpecific { get; set; } = UnitName(MolarVolumeSpecificUnits.m3_Kgmol);
        public string PressureDropLength { get; set; } = UnitName(PressureDropLengthUnits.Kpa_m);
        public string PressureDrop { get; set; } = UnitName(PressureDropUnits.KiloPascal);
        public string Viscosity { get; set; } = UnitName(ViscosityUnits.cPoise);
        public string ThermalConductivity { get; set; } = UnitName(ThermalConductivityUnits.W_m_K);
        public string VolumeEnergy { get; set; } = UnitName(VolumeEnergyUnits.KJ_m3);
        public string MassEnergy { get; set; } = UnitName(MassEnergyUnits.KJ_Kg);
        public string MolarEnergy { get; set; } = UnitName(MolarEnergyUnits.KJ_Kgmol);
        public string MassEntropy { get; set; } = UnitName(MassEntropyUnits.KJ_Kg_C);
        public string MolarEntropy { get; set; } = UnitName(MolarEntropyUnits.KJ_Kgmol_C);
        public string HeatSurfaceFlow { get; set; } = UnitName(HeatSurfaceFlowUnits.W_m2);
        public string VolumetricFlow { get; set; } = UnitName(VolumetricFlowUnits.m3_hr);
        public string EnergyFlow { get; set; } = UnitName(EnergyFlowUnits.KJ_hr);
        public string SuperficialTension { get; set; } = UnitName(SuperficialTensionUnits.N_m);
    }

    private sealed record CameraConfigurationSnapshot(
        double DefaultZoom,
        double DefaultPanX,
        double DefaultPanY,
        double GlobalScale,
        double GridSize,
        double MinZoom,
        double MaxZoom);

    private sealed record NamingConfigurationSnapshot(
        string Mode,
        string Pattern,
        int StartingNumber,
        string BaseNumber,
        string AreaPrefix,
        string CounterScope,
        List<NamingPatternPartSnapshot> PatternParts,
        Dictionary<string, string> PrefixesByEquipmentType);

    private sealed record NamingPatternPartSnapshot(string Kind, string Value);

    private sealed record ReportConfigurationSnapshot(
        List<string> AvailableTemplates,
        string DefaultFormat,
        bool AutoExportOnSimulation);

    private sealed record EquipmentDesignConfigurationSnapshot(string Standard, string RatingBasis);
}
