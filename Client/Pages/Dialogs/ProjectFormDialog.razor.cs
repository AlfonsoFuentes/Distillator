using Client.Pages.Dialogs.ProjectForm;
using Client.Services;
using Distillator.Domain.Configuration;
using Distillator.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Shared.Helpers;
using Shared.PropertiesDtos.Methods;
using UnitSystem;

namespace Client.Pages.Dialogs;

public partial class ProjectFormDialog
{    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public Project? Project { get; set; }
    [Parameter] public List<Project> ExistingProjects { get; set; } = new();
    private NamingConfiguration? _originalNamingConfiguration;
    private NamingMode? _originalNamingMode;
    private string? _originalNamingPattern;
    private NamingCounterScope? _originalCounterScope;
    private int? _originalStartingNumber;
   
    private string _projectName = "";
    private string _selectedMethodId = "";
    private Length _plantElevation = new Length(0, LengthUnits.Meter);
    private Pressure? _atmosphericPressure;
    private List<ThermodynamicMethodFullDto> _methods = new();
    private List<IProjectUnitSystem> _unitSystems = new();
    private string _activeUnitSystemName = "SI";
    private bool _isCreatingUnitSystem;
    private string _newUnitSystemName = "";

    private double _defaultZoom = 1.0;
    private double _defaultPanX = 0.0;
    private double _defaultPanY = 0.0;
    private double _globalScale = 0.7;
    private double _gridSize = 20.0;
    private double _minZoom = 0.2;
    private double _maxZoom = 3.0;

    private string _reportDefaultFormat = "PDF";
    private bool _autoExportOnSimulation;

    private string _equipmentDesignStandard = "API";
    private string _equipmentDesignRatingBasis = "normal";

    private NamingMode _namingMode = NamingMode.ProjectSequential;
    private int _namingStartingNumber = 101;
    private string _namingBaseNumber = "1151";
    private string _namingAreaPrefix = "";
    private NamingCounterScope _namingCounterScope = NamingCounterScope.Project;
    private List<NamingPatternPart> _namingPatternParts = new();
    private Dictionary<string, string> _namingPrefixes = new(StringComparer.OrdinalIgnoreCase);
    private NamingPatternPartKind _newNamingPartKind = NamingPatternPartKind.Literal;
    private string _newNamingLiteral = "-";
    private NamingSlot _selectedNamingSlot = NamingSlot.EquipmentPrefix;
    private bool _useDiagramPrefix;
    private string _separatorAfterDiagram = "-";
    private string _separatorAfterPrefix = "-";

    private string? _nameError;
    private string? _methodError;
    private string? _configurationError;
    private static readonly string[] NamingEquipmentTypes =
    {
        "Stream",
        "Pump",
        "Column",
        "HeatExchanger",
        "FlashDrum",
        "Tank",
        "ControlValve",
        "Mixer",
        "Splitter",
        "OffPageConnector",
        "Instrument"
    };

    public enum NamingSlot
    {
        DiagramPrefix,
        SeparatorAfterDiagram,
        EquipmentPrefix,
        SeparatorAfterPrefix,
        Sequence
    }

    private bool IsEditing => Project != null;
    private string DialogTitle => IsEditing ? "Edit Project" : "New Project";
    private string SaveButtonText => IsEditing ? "Save" : "Create";
    private IProjectUnitSystem? ActiveUnitSystem => _unitSystems.FirstOrDefault(system => system.Name == _activeUnitSystemName);

    private bool IsValid =>
        !string.IsNullOrWhiteSpace(_projectName) &&
        _nameError == null &&
        _methodError == null &&
        _configurationError == null &&
        !string.IsNullOrWhiteSpace(_selectedMethodId);

    protected override async Task OnInitializedAsync()
    {
        await LoadThermodynamicMethods();

        if (IsEditing && Project != null)
        {
            _projectName = Project.Name;
            _selectedMethodId = Project.Configuration.ThermodynamicMethodId != Guid.Empty
                ? Project.Configuration.ThermodynamicMethodId.ToString()
                : "";
            _plantElevation = Project.Configuration.PlantElevation;
            LoadConfiguration(Project.Configuration);
        }
        else
        {
            LoadConfiguration(new ProjectConfiguration());
        }

        CalculateAtmosphericPressure();
    }
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Capturamos el estado original la primera vez que se carga el proyecto en el diálogo
        if (Project != null && _originalNamingMode == null)
        {
            _originalNamingConfiguration = NamingConfiguration.Clone(Project.Configuration.NamingConfig);
            _originalNamingMode = Project.Configuration.NamingConfig.Mode;
            _originalNamingPattern = Project.Configuration.NamingConfig.Pattern;
            _originalCounterScope = Project.Configuration.NamingConfig.CounterScope;
            _originalStartingNumber = Project.Configuration.NamingConfig.StartingNumber;
        }
    }
    private void LoadConfiguration(IProjectConfiguration configuration)
    {
        _unitSystems = configuration.UnitSystems?.Select(CloneUnitSystem).ToList() ?? new List<IProjectUnitSystem>();

        if (_unitSystems.Count == 0)
        {
            _unitSystems.Add(ProjectUnitSystem.SI());
            _unitSystems.Add(ProjectUnitSystem.English());
        }

        if (!_unitSystems.Any(system => system.Name == "SI"))
        {
            _unitSystems.Insert(0, ProjectUnitSystem.SI());
        }

        if (!_unitSystems.Any(system => system.Name == "English"))
        {
            _unitSystems.Insert(Math.Min(1, _unitSystems.Count), ProjectUnitSystem.English());
        }

        _activeUnitSystemName = !string.IsNullOrWhiteSpace(configuration.ActiveUnitSystemName)
            && _unitSystems.Any(system => system.Name == configuration.ActiveUnitSystemName)
                ? configuration.ActiveUnitSystemName
                : _unitSystems.First().Name;

        _defaultZoom = configuration.CameraDefaults.DefaultZoom;
        _defaultPanX = configuration.CameraDefaults.DefaultPanX;
        _defaultPanY = configuration.CameraDefaults.DefaultPanY;
        _globalScale = configuration.CameraDefaults.GlobalScale;
        _gridSize = configuration.CameraDefaults.GridSize;
        _minZoom = configuration.CameraDefaults.MinZoom;
        _maxZoom = configuration.CameraDefaults.MaxZoom;

        _reportDefaultFormat = configuration.ReportConfig.DefaultFormat;
        _autoExportOnSimulation = configuration.ReportConfig.AutoExportOnSimulation;

        _equipmentDesignStandard = configuration.EquipmentDesignConfig.Standard;
        _equipmentDesignRatingBasis = configuration.EquipmentDesignConfig.RatingBasis;

        LoadNamingConfiguration(configuration.NamingConfig);
    }

    private void OnElevationChanged(Length elevation)
    {
        _plantElevation = elevation;
        CalculateAtmosphericPressure();
    }

    private async Task LoadThermodynamicMethods()
    {
        var result = await HttpServices.PostAsync<GetAllCompleteMethods, List<ThermodynamicMethodFullDto>>(new GetAllCompleteMethods());
        if (result.Succeeded && result.Data != null)
        {
            _methods = result.Data;
        }
    }

    private void CalculateAtmosphericPressure()
    {
        _atmosphericPressure = AtmosphericPressureCalculator.CalculateFromElevation(_plantElevation);
    }

    private void OnNameInput(ChangeEventArgs e)
    {
        _projectName = e.Value?.ToString() ?? "";
        ValidateName();
    }
    // Cambia la validación para protegerte de nulos
    private void ValidateName()
    {
        _configurationError = null;
        var trimmedName = _projectName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            _nameError = "Project name is required.";
            return;
        }

        // Se añade ExistingProjects?. para evitar NullReferenceException
        var duplicate = ExistingProjects?
            .Where(p => Project == null || p.Id != Project.Id)
            .Any(p => p.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)) ?? false;

        if (duplicate)
        {
            _nameError = "A project with this name already exists.";
        }
        else
        {
            _nameError = null;
        }
    }
    
    private void ValidateMethod()
    {
        _methodError = string.IsNullOrWhiteSpace(_selectedMethodId)
            ? "Thermodynamic method is required."
            : null;
    }
    private async Task Save()
    {
        ValidateName();
        ValidateMethod();
        _configurationError = null;
        if (_nameError != null || _methodError != null || string.IsNullOrWhiteSpace(_selectedMethodId)) return;

        var user = AuthProvider.CurrentUser;
        if (user == null) return;

        var thermodynamicMethodId = Guid.Parse(_selectedMethodId);
        var selectedMethod = await LoadThermodynamicMethodFullAsync(thermodynamicMethodId);
        var configuration = new ProjectConfiguration(
            unitDefaults: BuildUnitConfiguration(),
            unitSystems: BuildUnitSystems(),
            activeUnitSystemName: _activeUnitSystemName,
            cameraDefaults: BuildCameraConfiguration(),
            namingConfig: BuildNamingConfiguration(),
            thermodynamicMethodId: thermodynamicMethodId,
            thermodynamicMethod: selectedMethod,
            reportConfig: BuildReportConfiguration(),
            equipmentDesignConfig: BuildEquipmentDesignConfiguration(),
            plantElevation: _plantElevation);

        bool renameExistingEquipment = false;
        Dictionary<Guid, string>? diagramNumberUpdates = null;

        if (IsEditing && Project != null)
        {
            // 1. Detectar si la NUEVA configuración requiere números
            bool newConfigRequiresNumbers = ProjectSessionService.RequiresDiagramNumberForNaming(configuration.NamingConfig);

            // 2. Solo verificamos errores de números si la configuración nueva los necesita
            string? diagramError = null;
            if (newConfigRequiresNumbers)
            {
                diagramError = ProjectSessionService.GetDiagramNumberConfigurationError(Project, configuration);
            }

            // 3. Detectar si cambiaron las reglas de nombrado
            bool namingRulesChanged = _originalNamingConfiguration != null &&
                                      !AreNamingConfigurationsEquivalent(_originalNamingConfiguration, configuration.NamingConfig);

            // 4. Decidir si abrir el diálogo: 
            // O bien faltan números obligatorios, O el usuario cambió reglas y quiere renombrar.
            bool needsDiagramNumbers = diagramError != null && diagramError.Contains("requires a unique diagram number");

            if (needsDiagramNumbers || namingRulesChanged)
            {
                var parameters = new DialogParameters
                {
                    ["Project"] = Project,
                    ["RequiresDiagramNumbers"] = newConfigRequiresNumbers
                };
                var options = new DialogOptions { CloseOnEscapeKey = false, MaxWidth = MaxWidth.Small, BackdropClick = false };

                var dialogTitle = newConfigRequiresNumbers ? "Review Diagram Numbers" : "Naming Rules Changed";
                var dialog = await DialogService.ShowAsync<NamingMigrationDialog>(dialogTitle, parameters, options);
                var result = await dialog.Result;

                if (result?.Canceled == true || result?.Data is not NamingMigrationResult migrationResult)
                {
                    return;
                }

                diagramNumberUpdates = migrationResult.UpdatedDiagrams
                    .ToDictionary(item => item.Flowsheet.Id, item => item.DiagramNumber);
                renameExistingEquipment = migrationResult.RenameExisting;
            }
            else if (diagramError != null)
            {
                _configurationError = diagramError;
                return;
            }
        }

        if (!IsValid) return;

        if (IsEditing && Project != null)
        {
            var saved = await SessionService.UpdateProjectConfigurationAsync(
                _projectName.Trim(),
                configuration,
                renameExistingEquipment,
                diagramNumberUpdates);
            if (!saved)
            {
                _configurationError = "Project configuration was not saved. Please refresh and try again.";
                return;
            }

            _originalNamingMode = configuration.NamingConfig.Mode;
            _originalNamingPattern = configuration.NamingConfig.Pattern;
            _originalCounterScope = configuration.NamingConfig.CounterScope;
            _originalStartingNumber = configuration.NamingConfig.StartingNumber;
            _originalNamingConfiguration = NamingConfiguration.Clone(configuration.NamingConfig);

            MudDialog.Close(SessionService.CurrentProject ?? Project);
        }
        else
        {
            var project = (Project)user.CreateProject(_projectName.Trim(), configuration);
            var flowsheet = project.CreateFlowsheet("Main Area", "PFD");
            if (ProjectSessionService.RequiresDiagramNumberForNaming(configuration.NamingConfig))
            {
                flowsheet.DiagramNumber = "1";
            }
            SessionService.NotifyProjectChanged();
            MudDialog.Close(project);
        }
    }

    private async Task<ThermodynamicMethodFullDto?> LoadThermodynamicMethodFullAsync(Guid methodId)
    {
        var result = await HttpServices.PostAsync<GetMethodFullRequest, ThermodynamicMethodFullDto>(new GetMethodFullRequest(methodId));
        return result.Succeeded ? result.Data : null;
    }

    private static bool AreNamingConfigurationsEquivalent(INamingConfiguration left, INamingConfiguration right)
    {
        if (left.Mode != right.Mode ||
            left.Pattern != right.Pattern ||
            left.StartingNumber != right.StartingNumber ||
            !string.Equals(left.BaseNumber, right.BaseNumber, StringComparison.Ordinal) ||
            !string.Equals(left.AreaPrefix, right.AreaPrefix, StringComparison.Ordinal) ||
            left.CounterScope != right.CounterScope)
        {
            return false;
        }

        if (left.PatternParts.Count != right.PatternParts.Count)
        {
            return false;
        }

        for (var i = 0; i < left.PatternParts.Count; i++)
        {
            var a = left.PatternParts[i];
            var b = right.PatternParts[i];
            if (a.Kind != b.Kind || !string.Equals(a.Value, b.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        if (left.PrefixesByEquipmentType.Count != right.PrefixesByEquipmentType.Count)
        {
            return false;
        }

        foreach (var kvp in left.PrefixesByEquipmentType)
        {
            if (!right.PrefixesByEquipmentType.TryGetValue(kvp.Key, out var value) ||
                !string.Equals(kvp.Value, value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private IUnitConfiguration BuildUnitConfiguration()
    {
        return UnitConfiguration.Clone(ActiveUnitSystem?.Units ?? UnitConfiguration.SI());
    }

    private IList<IProjectUnitSystem> BuildUnitSystems()
    {
        return _unitSystems.Select(CloneUnitSystem).ToList();
    }

    private static IProjectUnitSystem CloneUnitSystem(IProjectUnitSystem system)
    {
        return new ProjectUnitSystem(system.Name, system.Units, system.IsBuiltIn);
    }

    private ICameraConfiguration BuildCameraConfiguration()
    {
        return new CameraConfiguration(
            defaultZoom: _defaultZoom,
            defaultPanX: _defaultPanX,
            defaultPanY: _defaultPanY,
            globalScale: _globalScale,
            gridSize: _gridSize,
            minZoom: _minZoom,
            maxZoom: _maxZoom);
    }

    private IReportConfiguration BuildReportConfiguration()
    {
        var templates = Project?.Configuration.ReportConfig.AvailableTemplates
            ?? new ReportConfiguration().AvailableTemplates;

        return new ReportConfiguration(
            availableTemplates: templates,
            defaultFormat: _reportDefaultFormat,
            autoExportOnSimulation: _autoExportOnSimulation);
    }

    private IEquipmentDesignConfiguration BuildEquipmentDesignConfiguration()
    {
        return new EquipmentDesignConfiguration(
            standard: _equipmentDesignStandard,
            ratingBasis: _equipmentDesignRatingBasis);
    }

    private void LoadNamingConfiguration(INamingConfiguration configuration)
    {
        var clone = NamingConfiguration.Clone(configuration);
        _namingMode = clone.Mode;
        _namingStartingNumber = clone.StartingNumber;
        _namingBaseNumber = clone.BaseNumber;
        _namingAreaPrefix = clone.AreaPrefix;
        _namingCounterScope = clone.CounterScope;
        _namingPatternParts = clone.PatternParts
            .Select(part => new NamingPatternPart(part.Kind, part.Value))
            .ToList();
        _namingPrefixes = new Dictionary<string, string>(clone.PrefixesByEquipmentType, StringComparer.OrdinalIgnoreCase);
        LoadNamingSlotsFromPattern();
    }

    private INamingConfiguration BuildNamingConfiguration()
    {
        _namingPatternParts = BuildNamingPatternPartsFromSlots();

        return new NamingConfiguration(
            mode: _namingMode,
            startingNumber: _namingStartingNumber,
            baseNumber: _namingBaseNumber,
            areaPrefix: _namingAreaPrefix,
            counterScope: _namingCounterScope,
            patternParts: _namingPatternParts,
            prefixesByEquipmentType: _namingPrefixes);
    }

    private void SetNamingMode(NamingMode mode)
    {
        _namingMode = mode;
        _namingCounterScope = NamingConfiguration.GetDefaultScope(mode);
        _namingPatternParts = NamingConfiguration.GetDefaultPatternParts(mode)
            .Select(part => new NamingPatternPart(part.Kind, part.Value))
            .ToList();
        LoadNamingSlotsFromPattern();
    }

    private void LoadNamingSlotsFromPattern()
    {
        _useDiagramPrefix = _namingPatternParts.Any(part => part.Kind == NamingPatternPartKind.AreaPrefix);

        _separatorAfterDiagram = GetLiteralAfter(NamingPatternPartKind.AreaPrefix, "-");
        _separatorAfterPrefix = GetLiteralAfter(NamingPatternPartKind.EquipmentPrefix, "-");

        if (_namingCounterScope == NamingCounterScope.MainEquipmentPackage)
        {
            _namingCounterScope = NamingCounterScope.Diagram;
        }
    }

    private string GetLiteralAfter(NamingPatternPartKind kind, string fallback)
    {
        for (var index = 0; index < _namingPatternParts.Count - 1; index++)
        {
            if (_namingPatternParts[index].Kind == kind &&
                _namingPatternParts[index + 1].Kind == NamingPatternPartKind.Literal)
            {
                return _namingPatternParts[index + 1].Value;
            }
        }

        return fallback;
    }

    private List<NamingPatternPart> BuildNamingPatternPartsFromSlots()
    {
        var parts = new List<NamingPatternPart>();

        if (_useDiagramPrefix)
        {
            parts.Add(new NamingPatternPart(NamingPatternPartKind.AreaPrefix, string.Empty));
            AddLiteralPart(parts, _separatorAfterDiagram);
        }

        parts.Add(new NamingPatternPart(NamingPatternPartKind.EquipmentPrefix, string.Empty));
        AddLiteralPart(parts, _separatorAfterPrefix);
        parts.Add(new NamingPatternPart(NamingPatternPartKind.Number, string.Empty));

        return parts;
    }

    private static void AddLiteralPart(List<NamingPatternPart> parts, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            parts.Add(new NamingPatternPart(NamingPatternPartKind.Literal, value));
        }
    }

    private void SelectNamingSlot(NamingSlot slot)
    {
        _selectedNamingSlot = slot;
    }

    private void SetUseDiagramPrefix(bool useDiagramPrefix)
    {
        _useDiagramPrefix = useDiagramPrefix;
        if (_useDiagramPrefix && string.IsNullOrWhiteSpace(_namingAreaPrefix))
        {
            _namingAreaPrefix = "100";
        }

        if (_useDiagramPrefix && _namingCounterScope is NamingCounterScope.Project or NamingCounterScope.EquipmentType)
        {
            _namingCounterScope = NamingCounterScope.Diagram;
        }
    }

    private bool IsSeparatorSlot(NamingSlot slot)
    {
        return slot is NamingSlot.SeparatorAfterDiagram or NamingSlot.SeparatorAfterPrefix;
    }

    private string GetSeparatorValue(NamingSlot slot)
    {
        return slot switch
        {
            NamingSlot.SeparatorAfterDiagram => _separatorAfterDiagram,
            NamingSlot.SeparatorAfterPrefix => _separatorAfterPrefix,
            _ => string.Empty
        };
    }

    private void SetSeparatorValue(NamingSlot slot, string? value)
    {
        var separator = value ?? string.Empty;
        switch (slot)
        {
            case NamingSlot.SeparatorAfterDiagram:
                _separatorAfterDiagram = separator;
                break;
            case NamingSlot.SeparatorAfterPrefix:
                _separatorAfterPrefix = separator;
                break;
        }
        StateHasChanged();
    }

    private void SetNamingCounterScope(NamingCounterScope scope)
    {
        _namingCounterScope = scope;
    }

    private static string GetOptionCardCss(bool selected)
    {
        return selected ? "option-card selected" : "option-card";
    }

    private IEnumerable<(string Label, string Value)> GetSeparatorOptions()
    {
        yield return ("None", string.Empty);
        yield return ("-", "-");
        yield return ("_", "_");
        yield return (".", ".");
    }

    private string GetNamingSlotCss(NamingSlot slot)
    {
        var classes = new List<string> { "naming-slot" };

        if (IsSeparatorSlot(slot))
        {
            classes.Add("separator");
        }

        if (_selectedNamingSlot == slot)
        {
            classes.Add("selected");
        }

        if (!IsNamingSlotActive(slot))
        {
            classes.Add("disabled");
        }

        return string.Join(" ", classes);
    }

    private bool IsNamingSlotActive(NamingSlot slot)
    {
        return slot switch
        {
            NamingSlot.DiagramPrefix => _useDiagramPrefix,
            NamingSlot.SeparatorAfterDiagram => _useDiagramPrefix && !string.IsNullOrEmpty(_separatorAfterDiagram),
            _ => true
        };
    }

    private string GetNamingSlotValue(NamingSlot slot)
    {
        return slot switch
        {
            NamingSlot.DiagramPrefix => _useDiagramPrefix ? SafePreviewValue(_namingAreaPrefix, "A1") : "Not used",
            NamingSlot.SeparatorAfterDiagram => _useDiagramPrefix ? SafePreviewValue(_separatorAfterDiagram, "None") : "None",
            NamingSlot.EquipmentPrefix => SafePreviewValue(GetEquipmentPrefix("Pump"), "P"),
            NamingSlot.SeparatorAfterPrefix => SafePreviewValue(_separatorAfterPrefix, "None"),
            NamingSlot.Sequence => GetSequencePreviewNumber("100", 0).ToString("D3"),
            _ => string.Empty
        };
    }

    private static string SafePreviewValue(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private string NamingSlotTitle(NamingSlot slot)
    {
        return slot switch
        {
            NamingSlot.DiagramPrefix => "Diagram Prefix",
            NamingSlot.SeparatorAfterDiagram => "Separator after Diagram Prefix",
            NamingSlot.EquipmentPrefix => "Equipment Prefix",
            NamingSlot.SeparatorAfterPrefix => "Separator after Equipment Prefix",
            NamingSlot.Sequence => "Sequence",
            _ => slot.ToString()
        };
    }

    private string NamingSlotDescription(NamingSlot slot)
    {
        return slot switch
        {
            NamingSlot.DiagramPrefix => "Optional text that identifies the diagram, area, or unit before the equipment prefix.",
            NamingSlot.SeparatorAfterDiagram => "Character placed between the diagram prefix and the equipment prefix.",
            NamingSlot.EquipmentPrefix => "Letters used to identify each equipment type.",
            NamingSlot.SeparatorAfterPrefix => "Character placed between the equipment prefix and the number section.",
            NamingSlot.Sequence => "The final counter used to keep names unique.",
            _ => string.Empty
        };
    }

    private string GetNamingEffectText()
    {
        var scopeText = _namingCounterScope switch
        {
            NamingCounterScope.Project => "The counter is shared by the whole project, so numbering continues across diagrams and equipment types.",
            NamingCounterScope.EquipmentType => "Each equipment type has its own counter for the whole project.",
            NamingCounterScope.Diagram => "Each diagram has its own counter. If a diagram starts at 200, its first generated equipment number is 201.",
            NamingCounterScope.DiagramAndType => "Each diagram and equipment type combination has its own counter.",
            NamingCounterScope.DiagramNumberRange => "Each diagram provides the number range used by its generated equipment names.",
            _ => "The counter follows the selected project rule."
        };

        var parts = new List<string>();

        if (_selectedNamingSlot == NamingSlot.DiagramPrefix && !_useDiagramPrefix)
        {
            parts.Add("Diagram prefix is not included in equipment names. It can be enabled when reports or equipment lists need diagram-based identification.");
        }
        else if (_useDiagramPrefix)
        {
            parts.Add("Each diagram provides its own prefix. The prefix is used in reports and equipment lists, but it is not shown as a large label on the diagram canvas.");
        }

        if (_selectedNamingSlot == NamingSlot.EquipmentPrefix || _selectedNamingSlot == NamingSlot.Sequence || _selectedNamingSlot == NamingSlot.SeparatorAfterPrefix)
        {
            parts.Add("Equipment letters come from the prefix table.");
        }

        parts.Add(scopeText);

        return string.Join(" ", parts);
    }

    private void AddNamingPart()
    {
        var value = _newNamingPartKind == NamingPatternPartKind.Literal
            ? _newNamingLiteral
            : string.Empty;

        _namingPatternParts.Add(new NamingPatternPart(_newNamingPartKind, value));
    }

    private void RemoveNamingPart(int index)
    {
        if (index < 0 || index >= _namingPatternParts.Count) return;
        _namingPatternParts.RemoveAt(index);
    }

    private void MoveNamingPart(int index, int delta)
    {
        var target = index + delta;
        if (index < 0 || index >= _namingPatternParts.Count) return;
        if (target < 0 || target >= _namingPatternParts.Count) return;

        (_namingPatternParts[index], _namingPatternParts[target]) = (_namingPatternParts[target], _namingPatternParts[index]);
    }

    private void SetNamingPartLiteral(int index, string? value)
    {
        if (index < 0 || index >= _namingPatternParts.Count) return;
        _namingPatternParts[index].Value = value ?? string.Empty;
    }

    private void SetNamingPrefix(string equipmentType, string? prefix)
    {
        _namingPrefixes[equipmentType] = prefix?.Trim() ?? string.Empty;
        StateHasChanged();
    }

    private IEnumerable<(string Type, string Prefix)> GetNamingPrefixRows()
    {
        return NamingEquipmentTypes.Select(type =>
        {
            var prefix = _namingPrefixes.TryGetValue(type, out var value) ? value : type[..1].ToUpperInvariant();
            return (type, prefix);
        });
    }

    private IEnumerable<(string Type, string Name)> GetNamingPreview()
    {
        var samples = new[] { "Stream", "Pump", "Column", "HeatExchanger", "ControlValve", "Splitter" };
        var number = Math.Max(_namingStartingNumber, 1);

        foreach (var type in samples)
        {
            yield return (NamingTypeLabel(type), BuildNamingPreview(type, number++));
        }
    }

    private string BuildNamingPreview(string equipmentType, int number)
    {
        return BuildNamingPreview(equipmentType, number, SafePreviewValue(_namingAreaPrefix, "100"));
    }

    private string BuildNamingPreview(string equipmentType, int number, string diagramPrefix)
    {
        var prefix = GetEquipmentPrefix(equipmentType);
        var patternParts = BuildNamingPatternPartsFromSlots();

        return string.Concat(patternParts.Select(part => part.Kind switch
        {
            NamingPatternPartKind.EquipmentPrefix => prefix,
            NamingPatternPartKind.AreaPrefix => diagramPrefix,
            NamingPatternPartKind.BaseNumber => _namingBaseNumber,
            NamingPatternPartKind.Number => number.ToString("D3"),
            NamingPatternPartKind.Sequence => (number - Math.Max(_namingStartingNumber, 1) + 1).ToString(),
            NamingPatternPartKind.Literal => part.Value,
            _ => string.Empty
        }));
    }

    private IEnumerable<(string Name, IEnumerable<(string Type, string Name)> Items)> GetDiagramNamingPreview()
    {
        var diagrams = new[]
        {
            new { Name = "Diagram 100", Prefix = "100", Offset = 0 },
            new { Name = "Diagram 200", Prefix = "200", Offset = 1 }
        };

        foreach (var diagram in diagrams)
        {
            var items = new[]
            {
                (Type: "Pump", Name: BuildNamingPreview("Pump", GetSequencePreviewNumber(diagram.Prefix, diagram.Offset, "Pump", 0), diagram.Prefix)),
                (Type: "Stream", Name: BuildNamingPreview("Stream", GetSequencePreviewNumber(diagram.Prefix, diagram.Offset, "Stream", 1), diagram.Prefix)),
                (Type: "Heat Exchanger", Name: BuildNamingPreview("HeatExchanger", GetSequencePreviewNumber(diagram.Prefix, diagram.Offset, "HeatExchanger", 2), diagram.Prefix))
            };

            yield return (diagram.Name, items);
        }
    }

    private int GetSequencePreviewNumber(string diagramPrefix, int diagramIndex, string equipmentType = "Pump", int itemIndex = 0)
    {
        var start = Math.Max(_namingStartingNumber, 1);
        var diagramStart = int.TryParse(diagramPrefix, out var parsedPrefix)
            ? parsedPrefix + 1
            : start;

        return _namingCounterScope switch
        {
            NamingCounterScope.Project => start + diagramIndex * 3 + itemIndex,
            NamingCounterScope.EquipmentType => start + diagramIndex,
            NamingCounterScope.Diagram => diagramStart + itemIndex,
            NamingCounterScope.DiagramAndType => diagramStart,
            NamingCounterScope.DiagramNumberRange => diagramStart + itemIndex,
            _ => start + itemIndex
        };
    }

    private IEnumerable<NamingCounterScope> GetAvailableNamingCounterScopes()
    {
        yield return NamingCounterScope.Project;
        yield return NamingCounterScope.EquipmentType;
        yield return NamingCounterScope.Diagram;
        yield return NamingCounterScope.DiagramAndType;
    }

    private IEnumerable<NamingMode> GetAvailableNamingModes()
    {
        yield return NamingMode.ProjectSequential;
        yield return NamingMode.ProjectSequentialByType;
        yield return NamingMode.DiagramSequentialWithAreaPrefix;
        yield return NamingMode.DiagramSequentialByType;
    }

    private string GetEquipmentPrefix(string equipmentType)
    {
        return _namingPrefixes.TryGetValue(equipmentType, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : equipmentType[..1].ToUpperInvariant();
    }

    private static string NamingModeLabel(NamingMode mode)
    {
        return mode switch
        {
            NamingMode.ProjectSequential => "Project Sequential",
            NamingMode.ProjectSequentialByType => "By Equipment Type",
            NamingMode.DiagramSequentialWithAreaPrefix => "By Diagram Prefix",
            NamingMode.DiagramSequentialByType => "By Diagram + Type",
            NamingMode.MainEquipmentPackageSequential => "Main Equipment Package",
            NamingMode.DiagramNumberRangeSequential => "Diagram Number Range",
            _ => mode.ToString()
        };
    }

    private static string NamingModeExample(NamingMode mode)
    {
        return mode switch
        {
            NamingMode.ProjectSequential => "S-101, P-102, E-103",
            NamingMode.ProjectSequentialByType => "S-101, S-102, P-101",
            NamingMode.DiagramSequentialWithAreaPrefix => "A1-S-101, A1-P-102",
            NamingMode.DiagramSequentialByType => "A1-S-101, A1-P-101",
            NamingMode.MainEquipmentPackageSequential => "P-1151_1, S-1151_2",
            NamingMode.DiagramNumberRangeSequential => "S-101, S-201, S-301",
            _ => string.Empty
        };
    }

    private static string NamingCounterScopeLabel(NamingCounterScope scope)
    {
        return scope switch
        {
            NamingCounterScope.Project => "Project",
            NamingCounterScope.EquipmentType => "Equipment Type",
            NamingCounterScope.Diagram => "Diagram",
            NamingCounterScope.DiagramAndType => "Diagram + Equipment Type",
            NamingCounterScope.MainEquipmentPackage => "Main Equipment Package",
            NamingCounterScope.DiagramNumberRange => "Diagram Number Range",
            _ => scope.ToString()
        };
    }

    private static string NamingCounterScopeOptionText(NamingCounterScope scope)
    {
        return scope switch
        {
            NamingCounterScope.Project => "One counter continues across the whole project.",
            NamingCounterScope.EquipmentType => "Each equipment type has its own project-wide counter.",
            NamingCounterScope.Diagram => "Each diagram starts from its own diagram number.",
            NamingCounterScope.DiagramAndType => "Each diagram and equipment type has its own counter.",
            NamingCounterScope.DiagramNumberRange => "Each diagram provides the range used by its equipment.",
            _ => string.Empty
        };
    }

    private static string NamingPartLabel(NamingPatternPart part)
    {
        return part.Kind == NamingPatternPartKind.Literal
            ? "Literal"
            : NamingPartKindLabel(part.Kind);
    }

    private static string NamingPartKindLabel(NamingPatternPartKind kind)
    {
        return kind switch
        {
            NamingPatternPartKind.EquipmentPrefix => "Equipment Prefix",
            NamingPatternPartKind.AreaPrefix => "Area Prefix",
            NamingPatternPartKind.BaseNumber => "Base Number",
            NamingPatternPartKind.Number => "Number",
            NamingPatternPartKind.Sequence => "Sequence",
            NamingPatternPartKind.Literal => "Literal",
            _ => kind.ToString()
        };
    }

    private static string NamingTypeLabel(string type)
    {
        return type switch
        {
            "HeatExchanger" => "Heat Exchanger",
            "ControlValve" => "Control Valve",
            _ => type
        };
    }

    private void OnUnitSystemChanged(ChangeEventArgs e)
    {
        _activeUnitSystemName = e.Value?.ToString() ?? _activeUnitSystemName;
    }

    private void StartCreateUnitSystem()
    {
        _newUnitSystemName = GetUniqueUnitSystemName($"{_activeUnitSystemName} Custom");
        _isCreatingUnitSystem = true;
    }

    private void CancelCreateUnitSystem()
    {
        _isCreatingUnitSystem = false;
        _newUnitSystemName = "";
    }

    private void CreateUnitSystem()
    {
        var source = ActiveUnitSystem;
        if (source == null) return;

        var requestedName = string.IsNullOrWhiteSpace(_newUnitSystemName)
            ? $"{source.Name} Custom"
            : _newUnitSystemName.Trim();

        var name = GetUniqueUnitSystemName(requestedName);
        var customSystem = new ProjectUnitSystem(name, source.Units);
        _unitSystems.Add(customSystem);
        _activeUnitSystemName = customSystem.Name;
        _isCreatingUnitSystem = false;
        _newUnitSystemName = "";
    }

    private string GetUniqueUnitSystemName(string baseName)
    {
        var name = baseName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Custom Units";
        }

        var candidate = name;
        var counter = 2;
        while (_unitSystems.Any(system => system.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{name} {counter}";
            counter++;
        }

        return candidate;
    }

    private IEnumerable<UnitRow> GetUnitRows()
    {
        var units = ActiveUnitSystem?.Units;
        if (units == null) yield break;

        yield return new("Geometry", "Length", UnitSlot.Length, units.DefaultLengthUnit);
        yield return new("Geometry", "Diameter", UnitSlot.Diameter, units.DefaultDiameterUnit);
        yield return new("Geometry", "Surface", UnitSlot.Surface, units.DefaultSurfaceUnit);
        yield return new("Geometry", "Volume", UnitSlot.Volume, units.DefaultVolumeUnit);
        yield return new("Time & Motion", "Time", UnitSlot.Time, units.DefaultTimeUnit);
        yield return new("Time & Motion", "Velocity", UnitSlot.Velocity, units.DefaultVelocityUnit);
        yield return new("Time & Motion", "Motor Velocity", UnitSlot.MotorVelocity, units.DefaultMotorVelocityUnit);
        yield return new("Mechanical", "Mass", UnitSlot.Mass, units.DefaultMassUnit);
        yield return new("Mechanical", "Force", UnitSlot.Force, units.DefaultForceUnit);
        yield return new("Mechanical", "Pressure", UnitSlot.Pressure, units.DefaultPressureUnit);
        yield return new("Mechanical", "Pressure Drop", UnitSlot.PressureDrop, units.DefaultPressureDropUnit);
        yield return new("Mechanical", "Pressure Drop / Length", UnitSlot.PressureDropLength, units.DefaultPressureDropLengthUnit);
        yield return new("Electrical", "Electric", UnitSlot.Electric, units.DefaultElectricUnit);
        yield return new("Thermal", "Temperature", UnitSlot.Temperature, units.DefaultTemperatureUnit);
        yield return new("Thermal", "Energy", UnitSlot.Energy, units.DefaultEnergyUnit);
        yield return new("Thermal", "Power", UnitSlot.Power, units.DefaultPowerUnit);
        yield return new("Thermal", "Thermal Conductivity", UnitSlot.ThermalConductivity, units.DefaultThermalConductivityUnit);
        yield return new("Thermal", "Heat Transfer Coefficient", UnitSlot.HeatTransferCoefficient, units.DefaultHeatTransferCoefficientUnit);
        yield return new("Thermal", "Heat Surface Flow", UnitSlot.HeatSurfaceFlow, units.DefaultHeatSurfaceFlowUnit);
        yield return new("Amount & Density", "Amount of Substance", UnitSlot.AmountOfSubstance, units.DefaultAmountOfSubstanceUnit);
        yield return new("Amount & Density", "Mass Density", UnitSlot.Density, units.DefaultDensityUnit);
        yield return new("Amount & Density", "Molar Density", UnitSlot.MolarDensity, units.DefaultMolarDensityUnit);
        yield return new("Specific Properties", "Mass Specific Volume", UnitSlot.MassVolumeSpecific, units.DefaultMassVolumeSpecificUnit);
        yield return new("Specific Properties", "Molar Specific Volume", UnitSlot.MolarVolumeSpecific, units.DefaultMolarVolumeSpecificUnit);
        yield return new("Specific Properties", "Volume Energy", UnitSlot.VolumeEnergy, units.DefaultVolumeEnergyUnit);
        yield return new("Specific Properties", "Mass Energy", UnitSlot.MassEnergy, units.DefaultMassEnergyUnit);
        yield return new("Specific Properties", "Molar Energy", UnitSlot.MolarEnergy, units.DefaultMolarEnergyUnit);
        yield return new("Specific Properties", "Mass Entropy", UnitSlot.MassEntropy, units.DefaultMassEntropyUnit);
        yield return new("Specific Properties", "Molar Entropy", UnitSlot.MolarEntropy, units.DefaultMolarEntropyUnit);
        yield return new("Flow", "Mass Flow", UnitSlot.MassFlow, units.DefaultMassFlowUnit);
        yield return new("Flow", "Molar Flow", UnitSlot.MolarFlow, units.DefaultMolarFlowUnit);
        yield return new("Flow", "Volumetric Flow", UnitSlot.VolumetricFlow, units.DefaultVolumetricFlowUnit);
        yield return new("Flow", "Energy Flow", UnitSlot.EnergyFlow, units.DefaultEnergyFlowUnit);
        yield return new("Transport", "Viscosity", UnitSlot.Viscosity, units.DefaultViscosityUnit);
        yield return new("Transport", "Superficial Tension", UnitSlot.SuperficialTension, units.DefaultSuperficialTensionUnit);
    }

    private IEnumerable<UnitOption> GetUnitOptions(UnitSlot slot)
    {
        return GetUnitClass(slot)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.FieldType == typeof(UnitMeasure))
            .Select(field => field.GetValue(null))
            .OfType<UnitMeasure>()
            .Select(Option);
    }

    private static UnitOption Option(UnitMeasure unit)
    {
        return new UnitOption(UnitText(unit), unit);
    }

    private void SetUnit(UnitSlot slot, string? unitName)
    {
        var system = ActiveUnitSystem;
        if (system?.IsBuiltIn != false || string.IsNullOrWhiteSpace(unitName)) return;

        var option = GetUnitOptions(slot).FirstOrDefault(item => item.Unit.Name == unitName);
        if (option == null) return;

        switch (slot)
        {
            case UnitSlot.Pressure:
                system.Units.DefaultPressureUnit = option.Unit;
                break;
            case UnitSlot.Temperature:
                system.Units.DefaultTemperatureUnit = option.Unit;
                break;
            case UnitSlot.MassFlow:
                system.Units.DefaultMassFlowUnit = option.Unit;
                break;
            case UnitSlot.MolarFlow:
                system.Units.DefaultMolarFlowUnit = option.Unit;
                break;
            case UnitSlot.Energy:
                system.Units.DefaultEnergyUnit = option.Unit;
                break;
            case UnitSlot.Power:
                system.Units.DefaultPowerUnit = option.Unit;
                break;
            case UnitSlot.Length:
                system.Units.DefaultLengthUnit = option.Unit;
                break;
            case UnitSlot.Diameter:
                system.Units.DefaultDiameterUnit = option.Unit;
                break;
            case UnitSlot.Surface:
                system.Units.DefaultSurfaceUnit = option.Unit;
                break;
            case UnitSlot.Volume:
                system.Units.DefaultVolumeUnit = option.Unit;
                break;
            case UnitSlot.Time:
                system.Units.DefaultTimeUnit = option.Unit;
                break;
            case UnitSlot.Velocity:
                system.Units.DefaultVelocityUnit = option.Unit;
                break;
            case UnitSlot.Mass:
                system.Units.DefaultMassUnit = option.Unit;
                break;
            case UnitSlot.Force:
                system.Units.DefaultForceUnit = option.Unit;
                break;
            case UnitSlot.Electric:
                system.Units.DefaultElectricUnit = option.Unit;
                break;
            case UnitSlot.MotorVelocity:
                system.Units.DefaultMotorVelocityUnit = option.Unit;
                break;
            case UnitSlot.AmountOfSubstance:
                system.Units.DefaultAmountOfSubstanceUnit = option.Unit;
                break;
            case UnitSlot.HeatTransferCoefficient:
                system.Units.DefaultHeatTransferCoefficientUnit = option.Unit;
                break;
            case UnitSlot.Density:
                system.Units.DefaultDensityUnit = option.Unit;
                break;
            case UnitSlot.MolarDensity:
                system.Units.DefaultMolarDensityUnit = option.Unit;
                break;
            case UnitSlot.MassVolumeSpecific:
                system.Units.DefaultMassVolumeSpecificUnit = option.Unit;
                break;
            case UnitSlot.MolarVolumeSpecific:
                system.Units.DefaultMolarVolumeSpecificUnit = option.Unit;
                break;
            case UnitSlot.PressureDropLength:
                system.Units.DefaultPressureDropLengthUnit = option.Unit;
                break;
            case UnitSlot.PressureDrop:
                system.Units.DefaultPressureDropUnit = option.Unit;
                break;
            case UnitSlot.Viscosity:
                system.Units.DefaultViscosityUnit = option.Unit;
                break;
            case UnitSlot.ThermalConductivity:
                system.Units.DefaultThermalConductivityUnit = option.Unit;
                break;
            case UnitSlot.VolumeEnergy:
                system.Units.DefaultVolumeEnergyUnit = option.Unit;
                break;
            case UnitSlot.MassEnergy:
                system.Units.DefaultMassEnergyUnit = option.Unit;
                break;
            case UnitSlot.MolarEnergy:
                system.Units.DefaultMolarEnergyUnit = option.Unit;
                break;
            case UnitSlot.MassEntropy:
                system.Units.DefaultMassEntropyUnit = option.Unit;
                break;
            case UnitSlot.MolarEntropy:
                system.Units.DefaultMolarEntropyUnit = option.Unit;
                break;
            case UnitSlot.HeatSurfaceFlow:
                system.Units.DefaultHeatSurfaceFlowUnit = option.Unit;
                break;
            case UnitSlot.VolumetricFlow:
                system.Units.DefaultVolumetricFlowUnit = option.Unit;
                break;
            case UnitSlot.EnergyFlow:
                system.Units.DefaultEnergyFlowUnit = option.Unit;
                break;
            case UnitSlot.SuperficialTension:
                system.Units.DefaultSuperficialTensionUnit = option.Unit;
                break;
        }
    }

    private static Type GetUnitClass(UnitSlot slot)
    {
        return slot switch
        {
            UnitSlot.Length => typeof(LengthUnits),
            UnitSlot.Diameter => typeof(DiameterUnits),
            UnitSlot.Surface => typeof(SurfaceUnits),
            UnitSlot.Volume => typeof(VolumeUnits),
            UnitSlot.Time => typeof(TimeUnits),
            UnitSlot.Velocity => typeof(VelocityUnits),
            UnitSlot.Mass => typeof(MassUnits),
            UnitSlot.Force => typeof(ForceUnits),
            UnitSlot.Electric => typeof(ElectricUnits),
            UnitSlot.Power => typeof(PowerUnits),
            UnitSlot.Energy => typeof(EnergyUnits),
            UnitSlot.Temperature => typeof(TemperatureUnits),
            UnitSlot.Pressure => typeof(PressureUnits),
            UnitSlot.MotorVelocity => typeof(MotorVelocityUnits),
            UnitSlot.AmountOfSubstance => typeof(AmountOfSubstanceUnits),
            UnitSlot.HeatTransferCoefficient => typeof(HeatTransferCoefficientUnits),
            UnitSlot.Density => typeof(MassDensityUnits),
            UnitSlot.MolarDensity => typeof(MolarDensityUnits),
            UnitSlot.MassVolumeSpecific => typeof(MassVolumeSpecificUnits),
            UnitSlot.MolarVolumeSpecific => typeof(MolarVolumeSpecificUnits),
            UnitSlot.PressureDropLength => typeof(PressureDropLengthUnits),
            UnitSlot.PressureDrop => typeof(PressureDropUnits),
            UnitSlot.ThermalConductivity => typeof(ThermalConductivityUnits),
            UnitSlot.VolumeEnergy => typeof(VolumeEnergyUnits),
            UnitSlot.MassEnergy => typeof(MassEnergyUnits),
            UnitSlot.MolarEnergy => typeof(MolarEnergyUnits),
            UnitSlot.MassEntropy => typeof(MassEntropyUnits),
            UnitSlot.MolarEntropy => typeof(MolarEntropyUnits),
            UnitSlot.MassFlow => typeof(MassFlowUnits),
            UnitSlot.MolarFlow => typeof(MolarFlowUnits),
            UnitSlot.HeatSurfaceFlow => typeof(HeatSurfaceFlowUnits),
            UnitSlot.VolumetricFlow => typeof(VolumetricFlowUnits),
            UnitSlot.EnergyFlow => typeof(EnergyFlowUnits),
            UnitSlot.Viscosity => typeof(ViscosityUnits),
            UnitSlot.SuperficialTension => typeof(SuperficialTensionUnits),
            _ => typeof(LengthUnits)
        };
    }

    private static string UnitText(UnitMeasure unit)
    {
        if (unit == null || unit == UnitMeasure.None) return "Not configured";
        return !string.IsNullOrWhiteSpace(unit.Symbol) ? unit.Symbol : unit.Name;
    }

    private void Cancel() => MudDialog.Cancel();

    public enum UnitSlot
    {
        Length,
        Diameter,
        Surface,
        Volume,
        Time,
        Velocity,
        Mass,
        Force,
        Electric,
        Power,
        Energy,
        Temperature,
        Pressure,
        MotorVelocity,
        AmountOfSubstance,
        HeatTransferCoefficient,
        Density,
        MolarDensity,
        MassVolumeSpecific,
        MolarVolumeSpecific,
        PressureDropLength,
        PressureDrop,
        ThermalConductivity,
        VolumeEnergy,
        MassEnergy,
        MolarEnergy,
        MassEntropy,
        MolarEntropy,
        MassFlow,
        MolarFlow,
        HeatSurfaceFlow,
        VolumetricFlow,
        EnergyFlow,
        Viscosity,
        SuperficialTension
    }

    public sealed record UnitRow(string Group, string Label, UnitSlot Slot, UnitMeasure Unit);

    public sealed record UnitOption(string Label, UnitMeasure Unit);
}
