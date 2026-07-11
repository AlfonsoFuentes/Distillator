using Client.Services.Security;
using Distillator.Domain.Configuration;
using Distillator.Domain.Models;
using Distillator.Domain.Repositories;
using Distillator.Domain.Repositories.InMemory;
using Distillator.Domain.Services;
using Distillator.Domain.Session;
using Shared.SolverConsecutive;

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

    public User? CurrentUser => _authProvider.CurrentUser;
    public Project? CurrentProject { get; private set; }
    public IFlowsheet? ActiveFlowsheet { get; private set; }

    public event Action? ProjectChanged;

    public void NotifyProjectChanged() => ProjectChanged?.Invoke();

    public ProjectSessionService(
        CustomAuthenticationStateProvider authProvider,
        IProjectRepository projectRepository,
        IUserSessionStateRepository userSessionStateRepository,
        IFlowsheetTypeRegistry? flowsheetTypeRegistry = null)
    {
        _authProvider = authProvider;
        _projectRepository = projectRepository;
        _userSessionStateRepository = userSessionStateRepository;
        _flowsheetTypeRegistry = flowsheetTypeRegistry ?? new FlowsheetTypeRegistry();
    }

    public async Task InitializeAsync()
    {
        // 1. Asegurar que el usuario está cargado. Si aún no lo está, forzar la carga.
        if (CurrentUser == null)
            await _authProvider.GetAuthenticationStateAsync();

        if (CurrentUser == null)
            throw new InvalidOperationException("No current user available.");

        // 2. Cargar sesión del usuario (último proyecto/flowsheet activo)
        var session = await _userSessionStateRepository.GetByUserIdAsync(CurrentUser.Id);

        // 3. Buscar proyectos del usuario
        var userProjects = await _projectRepository.GetByUserIdAsync(CurrentUser.Id);

        if (userProjects.Count == 0)
        {
            // 4a. Si no tiene proyectos, crear uno nuevo con un PFD por defecto
            CurrentProject = (Project)CurrentUser.CreateProject("Main Project");
            var defaultFlowsheet = CurrentProject.CreateFlowsheet("PFD 1", "PFD");
            await _projectRepository.SaveAsync(CurrentProject);
            await SaveSessionAsync(defaultFlowsheet.Id);
        }
        else if (userProjects.Count == 1)
        {
            // 4b. Si tiene exactamente 1, cargarlo
            CurrentProject = userProjects.First();
        }
        else
        {
            // 4c. Si tiene 2 o más, intentar cargar el último activo según la sesión
            var lastProject = session?.LastProjectId != null
                ? userProjects.FirstOrDefault(p => p.Id == session.LastProjectId.Value)
                : null;

            CurrentProject = lastProject ?? userProjects.First();
        }

        // 5. Establecer el flowsheet activo
        if (session?.LastFlowsheetId != null)
        {
            ActiveFlowsheet = CurrentProject.GetFlowsheet(session.LastFlowsheetId.Value);
        }

        ActiveFlowsheet ??= CurrentProject.Flowsheets.FirstOrDefault();
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
        ProjectChanged?.Invoke();
    }

    public async Task SetActiveFlowsheetAsync(IFlowsheet flowsheet)
    {
        if (flowsheet.Project.Id != CurrentProject?.Id)
            throw new InvalidOperationException("Flowsheet does not belong to the current project.");

        ActiveFlowsheet = flowsheet;
        ProjectChanged?.Invoke();
        await SaveSessionAsync(flowsheet.Id);
    }

    public async Task<IFlowsheet> CreateFlowsheetAsync(string typeCode, string? baseName = null)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        var type = CurrentProject.FlowsheetTypes.GetByCode(typeCode)
            ?? throw new InvalidOperationException($"Unknown flowsheet type: {typeCode}");

        var name = GenerateUniqueFlowsheetName(typeCode, baseName ?? type.DisplayName);
        var flowsheet = CurrentProject.CreateFlowsheet(name, typeCode);
        ActiveFlowsheet = flowsheet;
        await SaveSessionAsync(flowsheet.Id);
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
        ProjectChanged?.Invoke();
    }

    public async Task UpdateFlowsheetConfigurationAsync(
        IFlowsheet flowsheet,
        string? newName,
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

        if (diagramWidth.HasValue && diagramWidth.Value > 0) flowsheet.DiagramWidth = diagramWidth.Value;
        if (diagramHeight.HasValue && diagramHeight.Value > 0) flowsheet.DiagramHeight = diagramHeight.Value;
        if (globalScale.HasValue && globalScale.Value > 0) flowsheet.GlobalScale = globalScale.Value;
        if (gridSize.HasValue && gridSize.Value > 0) flowsheet.GridSize = gridSize.Value;
        if (zoom.HasValue && zoom.Value > 0) flowsheet.Zoom = zoom.Value;
        if (panX.HasValue) flowsheet.PanX = panX.Value;
        if (panY.HasValue) flowsheet.PanY = panY.Value;

        await SaveSessionAsync(ActiveFlowsheet?.Id);
        ProjectChanged?.Invoke();
    }

    public async Task UpdateProjectConfigurationAsync(IProjectConfiguration configuration)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        CurrentProject.UpdateConfiguration(configuration);
        await SaveSessionAsync(ActiveFlowsheet?.Id);
        ProjectChanged?.Invoke();
    }

    public async Task DeleteFlowsheetAsync(Guid flowsheetId)
    {
        if (CurrentProject == null)
            throw new InvalidOperationException("No current project available.");

        if (CurrentProject.Flowsheets.Count <= 1)
            return;

        CurrentProject.RemoveFlowsheet(flowsheetId);

        if (ActiveFlowsheet?.Id == flowsheetId)
        {
            ActiveFlowsheet = CurrentProject.Flowsheets.FirstOrDefault();
        }

        await SaveSessionAsync(ActiveFlowsheet?.Id);
        ProjectChanged?.Invoke();
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

        var session = await _userSessionStateRepository.GetByUserIdAsync(CurrentUser.Id)
            ?? new UserSessionState(CurrentUser.Id);

        session.LastProjectId = CurrentProject.Id;
        if (lastFlowsheetId != null)
            session.LastFlowsheetId = lastFlowsheetId;

        await _userSessionStateRepository.SaveAsync(session);
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
}
