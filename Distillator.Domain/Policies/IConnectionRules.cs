using Distillator.Domain.Models;
using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Policies;

/// <summary>
/// Reglas de conexión entre equipos. Cada tipo de diagrama implementa sus propias reglas.
/// </summary>
public interface IConnectionRules
{
    /// <summary>
    /// Determina si se puede conectar dos elementos en un diagrama.
    /// </summary>
    bool CanConnect(
        IVisualElement source,
        string sourcePortName,
        IVisualElement? target,
        string? targetPortName,
        IFlowsheet flowsheet);

    /// <summary>
    /// Devuelve los tipos de equipo permitidos en este tipo de diagrama.
    /// </summary>
    IEnumerable<EquipmentType> GetAllowedEquipmentTypes();

    /// <summary>
    /// Indica si este tipo de conexión requiere que se cree una corriente intermedia.
    /// </summary>
    bool RequiresIntermediateStream(IVisualElement source, IVisualElement target);
}

public class PfdConnectionRules : IConnectionRules
{
    public IEnumerable<EquipmentType> GetAllowedEquipmentTypes()
    {
        return new[]
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
        };
    }

    public bool CanConnect(
        IVisualElement source,
        string sourcePortName,
        IVisualElement? target,
        string? targetPortName,
        IFlowsheet flowsheet)
    {
        if (string.IsNullOrWhiteSpace(sourcePortName)) return false;

        // Conexión a espacio vacío: solo permitida desde un equipo (no desde una corriente).
        if (target == null || string.IsNullOrWhiteSpace(targetPortName))
        {
            var sourceIsStream = source.Type == EquipmentType.MaterialStream || source.Type == EquipmentType.EnergyStream;
            return !sourceIsStream && source.Ports.Any(p => p.Name == sourcePortName);
        }

        return source.CanConnect(sourcePortName, target, targetPortName);
    }

    public bool RequiresIntermediateStream(IVisualElement source, IVisualElement target)
    {
        var sourceIsStream = source.Type == EquipmentType.MaterialStream || source.Type == EquipmentType.EnergyStream;
        var targetIsStream = target.Type == EquipmentType.MaterialStream || target.Type == EquipmentType.EnergyStream;
        return !sourceIsStream && !targetIsStream;
    }
}

public class PandidConnectionRules : IConnectionRules
{
    public IEnumerable<EquipmentType> GetAllowedEquipmentTypes()
    {
        return new[]
        {
            EquipmentType.Pump,
            EquipmentType.ControlValve,
            EquipmentType.Tank,
            EquipmentType.Instrument,
            EquipmentType.OffPageConnector
        };
    }

    public bool CanConnect(
        IVisualElement source,
        string sourcePortName,
        IVisualElement? target,
        string? targetPortName,
        IFlowsheet flowsheet)
    {
        if (target == null || string.IsNullOrWhiteSpace(targetPortName)) return false;
        return source.CanConnect(sourcePortName, target, targetPortName);
    }

    public bool RequiresIntermediateStream(IVisualElement source, IVisualElement target)
    {
        return false;
    }
}

public class ElectricalConnectionRules : IConnectionRules
{
    public IEnumerable<EquipmentType> GetAllowedEquipmentTypes()
    {
        return Array.Empty<EquipmentType>();
    }

    public bool CanConnect(
        IVisualElement source,
        string sourcePortName,
        IVisualElement? target,
        string? targetPortName,
        IFlowsheet flowsheet)
    {
        return false;
    }

    public bool RequiresIntermediateStream(IVisualElement source, IVisualElement target)
    {
        return false;
    }
}
