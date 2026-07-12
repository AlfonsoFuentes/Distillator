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
using Shared.Projects;
using Shared.SolverConsecutive;
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
    private static readonly JsonSerializerOptions PersistenceJsonOptions = new(JsonSerializerDefaults.Web);

    public User? CurrentUser => _authProvider.CurrentUser;
    public Project? CurrentProject { get; private set; }
    public IFlowsheet? ActiveFlowsheet { get; private set; }

    public event Action? ProjectChanged;

    public void NotifyProjectChanged() => ProjectChanged?.Invoke();

    public ProjectSessionService(
        CustomAuthenticationStateProvider authProvider,
        IProjectRepository projectRepository,
        IUserSessionStateRepository userSessionStateRepository,
        IFlowsheetTypeRegistry? flowsheetTypeRegistry = null,
        IEquipmentNamingService? namingService = null,
        IHttpService? httpService = null)
    {
        _authProvider = authProvider;
        _projectRepository = projectRepository;
        _userSessionStateRepository = userSessionStateRepository;
        _flowsheetTypeRegistry = flowsheetTypeRegistry ?? new FlowsheetTypeRegistry();
        _namingService = namingService ?? new EquipmentNamingService();
        _httpService = httpService;
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
        var userProjects = await LoadUserProjectsAsync();

        if (userProjects.Count == 0)
        {
            // 4a. Si no tiene proyectos, crear uno nuevo con un PFD por defecto
            CurrentProject = (Project)CurrentUser.CreateProject("Main Project");
            var defaultFlowsheet = CurrentProject.CreateFlowsheet("PFD 1", "PFD");
            await PersistProjectCreatedAsync(CurrentProject);
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

        var projects = new List<Project>();
        foreach (var summary in summariesResult.Data.OrderByDescending(project => project.UpdatedOnUtc))
        {
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

        var result = await _httpService.PostAsync<GetProjectRequest, ProjectDocumentDto>(new GetProjectRequest
        {
            ProjectId = projectId
        });

        if (!result.Succeeded || result.Data == null)
        {
            return null;
        }

        return FromPersistenceDto(result.Data, CurrentUser);
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
            Configuration = ToPersistenceDto(project.Configuration),
            Diagrams = ToDiagramDtos(project)
        };

        await _httpService.PostAsync<CreateProjectRequest, ProjectDocumentDto>(request);
    }

    public async Task SetActiveFlowsheetAsync(IFlowsheet flowsheet)
    {
        if (flowsheet.Project.Id != CurrentProject?.Id)
            throw new InvalidOperationException("Flowsheet does not belong to the current project.");

        ActiveFlowsheet = flowsheet;
        ProjectChanged?.Invoke();
        await SaveSessionAsync(flowsheet.Id);
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
        await PersistCurrentProjectConfigurationAsync();
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
        await PersistCurrentProjectConfigurationAsync();
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
        await PersistCurrentProjectConfigurationAsync();
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
            Configuration = ToPersistenceDto(CurrentProject.Configuration),
            Diagrams = ToDiagramDtos(CurrentProject)
        };

        var result = await _httpService.PostAsync<UpdateProjectConfigurationRequest, ProjectDocumentDto>(request);
        if (!result.Succeeded &&
            result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            await PersistProjectCreatedAsync(CurrentProject);
        }
    }

    private async Task EnsureCurrentUserAsync()
    {
        if (CurrentUser == null)
        {
            await _authProvider.GetAuthenticationStateAsync();
        }
    }

    private static Project FromPersistenceDto(ProjectDocumentDto document, User owner)
    {
        var project = new Project(
            document.Name,
            owner,
            FromPersistenceDto(document.Configuration),
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

        foreach (var diagram in diagrams)
        {
            var flowsheet = project.CreateFlowsheet(
                string.IsNullOrWhiteSpace(diagram.Name) ? "PFD 1" : diagram.Name,
                string.IsNullOrWhiteSpace(diagram.TypeCode) ? "PFD" : diagram.TypeCode,
                diagram.Id == Guid.Empty ? null : diagram.Id);

            flowsheet.DiagramNumber = diagram.DiagramNumber?.Trim() ?? string.Empty;
            flowsheet.DiagramWidth = flowsheet.DiagramWidth > 0 ? flowsheet.DiagramWidth : 5000;
            flowsheet.DiagramHeight = flowsheet.DiagramHeight > 0 ? flowsheet.DiagramHeight : 5000;
        }

        return project;
    }

    private static IProjectConfiguration FromPersistenceDto(ProjectBasicConfigurationDto configuration)
    {
        var unitSystems = Deserialize(configuration.UnitSystemsJson, new List<ProjectUnitSystemSnapshot>())
            .Select(FromSnapshot)
            .Cast<IProjectUnitSystem>()
            .ToList();

        if (unitSystems.Count == 0)
        {
            unitSystems.Add(ProjectUnitSystem.SI());
            unitSystems.Add(ProjectUnitSystem.English());
        }

        return new ProjectConfiguration(
            unitSystems: unitSystems,
            activeUnitSystemName: configuration.ActiveUnitSystemName,
            cameraDefaults: FromSnapshot(Deserialize(configuration.CameraConfigurationJson, ToSnapshot(new CameraConfiguration()))),
            namingConfig: FromSnapshot(Deserialize(configuration.NamingConfigurationJson, ToSnapshot(new NamingConfiguration()))),
            thermodynamicMethodId: configuration.ThermodynamicMethodId,
            reportConfig: FromSnapshot(Deserialize(configuration.ReportConfigurationJson, ToSnapshot(new ReportConfiguration()))),
            equipmentDesignConfig: FromSnapshot(Deserialize(configuration.EquipmentDesignConfigurationJson, ToSnapshot(new EquipmentDesignConfiguration()))),
            plantElevation: new UnitSystem.Length(
                configuration.PlantElevationValue,
                ResolveUnit(configuration.PlantElevationUnit, LengthUnits.Meter)));
    }

    private static List<ProjectDiagramDto> ToDiagramDtos(Project project)
    {
        return project.Flowsheets
            .Select((flowsheet, index) => new ProjectDiagramDto
            {
                Id = flowsheet.Id,
                Name = flowsheet.Name,
                TypeCode = flowsheet.TypeCode,
                DiagramNumber = string.IsNullOrWhiteSpace(flowsheet.DiagramNumber) ? null : flowsheet.DiagramNumber,
                Order = index,
                CanvasStateJson = "{}"
            })
            .ToList();
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
            UnitName(units.DefaultDensityUnit),
            UnitName(units.DefaultViscosityUnit),
            UnitName(units.DefaultThermalConductivityUnit));
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
            ResolveUnit(snapshot.ThermalConductivity, ThermalConductivityUnits.W_m_K));
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

    private sealed record ProjectUnitSystemSnapshot(string Name, bool IsBuiltIn, UnitConfigurationSnapshot Units);

    private sealed record UnitConfigurationSnapshot(
        string Pressure,
        string Temperature,
        string MassFlow,
        string MolarFlow,
        string Energy,
        string Power,
        string Length,
        string Density,
        string Viscosity,
        string ThermalConductivity);

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
