using Distillator.Domain.Factories;
using Distillator.Domain.Policies;
using Shared.ProcessFlowDiagram;
using Shared.WorkSpaceManagers;

namespace Distillator.Domain.Models;

public class FlowsheetType : IFlowsheetType
{
    public string Code { get; }
    public string DisplayName { get; }
    public bool SupportsSimulation { get; }
    public IEnumerable<EquipmentType> AllowedEquipmentTypes { get; }
    public string ConnectionTypeCode { get; }
    public IEquipmentFactory EquipmentFactory { get; }
    public IConnectionRules ConnectionRules { get; }

    public FlowsheetType(
        string code,
        string displayName,
        bool supportsSimulation,
        IEnumerable<EquipmentType> allowedEquipmentTypes,
        string connectionTypeCode,
        IEquipmentFactory equipmentFactory,
        IConnectionRules connectionRules)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        SupportsSimulation = supportsSimulation;
        AllowedEquipmentTypes = allowedEquipmentTypes?.ToList() ?? new List<EquipmentType>();
        ConnectionTypeCode = connectionTypeCode ?? throw new ArgumentNullException(nameof(connectionTypeCode));
        EquipmentFactory = equipmentFactory ?? throw new ArgumentNullException(nameof(equipmentFactory));
        ConnectionRules = connectionRules ?? throw new ArgumentNullException(nameof(connectionRules));
    }
}

public class FlowsheetTypeRegistry : IFlowsheetTypeRegistry
{
    private readonly Dictionary<string, IFlowsheetType> _types = new(StringComparer.OrdinalIgnoreCase);

    public FlowsheetTypeRegistry()
    {
        // Tipos built-in
        Register(new FlowsheetType("PFD", "Process Flow Diagram", true,
            new[]
            {
                EquipmentType.MaterialStream,
                EquipmentType.Pump,
                EquipmentType.Column,
                EquipmentType.FlashDrum,
                EquipmentType.Exchanger,
                EquipmentType.PlateExchanger,
                EquipmentType.Reboiler,
                EquipmentType.ControlValve,
                EquipmentType.Splitter,
                EquipmentType.Mixer,
                EquipmentType.Tank
            },
            "MaterialPipe",
            new PfdEquipmentFactory(),
            new PfdConnectionRules()));

        Register(new FlowsheetType("PAndID", "Piping and Instrumentation Diagram", false,
            new[]
            {
                EquipmentType.Pump,
                EquipmentType.ControlValve,
                EquipmentType.Tank,
                EquipmentType.Instrument,
                EquipmentType.OffPageConnector
            },
            "Signal",
            new PandidEquipmentFactory(),
            new PandidConnectionRules()));

        Register(new FlowsheetType("Electrical", "Electrical Diagram", false,
            Array.Empty<EquipmentType>(),
            "Cable",
            new ElectricalEquipmentFactory(),
            new ElectricalConnectionRules()));
    }

    public void Register(IFlowsheetType type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        _types[type.Code] = type;
    }

    public IFlowsheetType? GetByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return _types.GetValueOrDefault(code);
    }

    public IEnumerable<IFlowsheetType> AllTypes => _types.Values.ToList().AsReadOnly();
}


