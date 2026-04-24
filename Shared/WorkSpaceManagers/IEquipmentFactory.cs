using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Columns;
using Shared.ProcessFlowDiagram.ControlValves;
using Shared.ProcessFlowDiagram.HeatExchangers;
using Shared.ProcessFlowDiagram.Helpers;
using Shared.ProcessFlowDiagram.Instruments;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;
using Shared.ProcessFlowDiagram.Vessels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.WorkSpaceManagers
{
    public interface INamingService
    {
        string GenerateNextName(string prefix);
    }

    public interface IEquipmentFactory
    {
        IVisualElement? Create(EquipmentType type, double x, double y, Func<double, double> snap);
        void Register(EquipmentType type, Func<IVisualElement> factory);
    }

    public class EquipmentFactory : IEquipmentFactory
    {
        private readonly INamingService _naming;
        private readonly Dictionary<EquipmentType, Func<IVisualElement>> _registry = new();

        public EquipmentFactory(INamingService naming)
        {
            _naming = naming;
            // Registro tipado con el Enum
            Register(EquipmentType.MaterialStream, () => new StreamVisualElement());
            Register(EquipmentType.Column, () => new ColumnVisualElement());
            Register(EquipmentType.FlashDrum, () => new FlashTankVisualElement());
            Register(EquipmentType.Tank, () => new VesselVisualElement());
            Register(EquipmentType.Exchanger, () => new HeatExchangerVisualElement());
            Register(EquipmentType.PlateExchanger, () => new PlateExchangerVisualElement());
            Register(EquipmentType.Reboiler, () => new ReboilerVisualElement());
            Register(EquipmentType.Pump, () => new PumpVisualElement());
            Register(EquipmentType.ControlValve, () => new ControlValveVisualElement());
            Register(EquipmentType.Splitter, () => new SplitterVisualElement());
            Register(EquipmentType.Mixer, () => new MixerVisualElement());
            Register(EquipmentType.Instrument, () => new InstrumentVisualElement());
        }

        public void Register(EquipmentType type, Func<IVisualElement> factory) => _registry[type] = factory;

        public IVisualElement? Create(EquipmentType type, double x, double y, Func<double, double> snap)
        {

            if (!_registry.TryGetValue(type, out var factory)) return null;

            var element = factory();
            element.SetDropPosition(x, y, snap);

            var name = _naming.GenerateNextName(element.Prefix);
            element.Facade.Name = name;
            element.Label = name;

            return element;
        }
    }
}
