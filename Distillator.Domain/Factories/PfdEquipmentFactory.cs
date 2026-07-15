using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Columns;
using Shared.ProcessFlowDiagram.ControlValves;
using Shared.ProcessFlowDiagram.HeatExchangers;
using Shared.ProcessFlowDiagram.Helpers;
using Shared.ProcessFlowDiagram.Instruments;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;
using Shared.ProcessFlowDiagram.Vessels;
using Shared.WorkSpaceManagers;

namespace Distillator.Domain.Factories;

/// <summary>
/// Fábrica concreta de equipos para diagramas PFD.
/// Crea instancias reales de IVisualElement del proyecto Shared.
/// </summary>
public class PfdEquipmentFactory : IEquipmentFactory, IPfdEquipmentFactory
{
    public IVisualElement? Create(EquipmentType type, double x, double y, Func<double, double> snap)
    {
        IVisualElement? element = type switch
        {
            EquipmentType.MaterialStream => new StreamVisualElement(),
            EquipmentType.Pump => new PumpVisualElement(),
            EquipmentType.Column => new ColumnVisualElement(),
            EquipmentType.FlashDrum => new FlashTankVisualElement(),
            EquipmentType.Exchanger => new HeatExchangerVisualElement(),
            EquipmentType.PlateExchanger => new PlateExchangerVisualElement(),
            EquipmentType.Reboiler => new ReboilerVisualElement(),
            EquipmentType.ControlValve => new ControlValveVisualElement(),
            EquipmentType.Splitter => new SplitterVisualElement(),
            EquipmentType.Mixer => new StreamMixerVisualElement(),
            EquipmentType.Tank => new VesselVisualElement(),
            EquipmentType.OffPageConnector => new OffPageConnectorElement(),
            _ => null
        };

        if (element != null)
        {
            element.SetDropPosition(x, y, snap);
        }

        return element;
    }

    public void Register(EquipmentType type, Func<IVisualElement> factory)
    {
        // No se permite registro dinámico en la fábrica concreta por defecto.
    }
}

/// <summary>
/// Fábrica concreta de equipos para diagramas P&ID.
/// Estructura preparada; la implementación completa de equipos P&ID se hará en fase posterior.
/// </summary>
public class PandidEquipmentFactory : IEquipmentFactory, IPandidEquipmentFactory
{
    public IVisualElement? Create(EquipmentType type, double x, double y, Func<double, double> snap)
    {
        // TODO: implementar equipos visuales específicos de P&ID.
        return null;
    }

    public void Register(EquipmentType type, Func<IVisualElement> factory)
    {
    }
}

/// <summary>
/// Fábrica concreta de equipos para diagramas eléctricos.
/// Estructura preparada; la implementación completa de equipos eléctricos se hará en fase posterior.
/// </summary>
public class ElectricalEquipmentFactory : IEquipmentFactory, IElectricalEquipmentFactory
{
    public IVisualElement? Create(EquipmentType type, double x, double y, Func<double, double> snap)
    {
        // TODO: implementar equipos visuales específicos de Electrical.
        return null;
    }

    public void Register(EquipmentType type, Func<IVisualElement> factory)
    {
    }
}
