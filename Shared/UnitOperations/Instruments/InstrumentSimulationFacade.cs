using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Instruments
{
    public class InstrumentSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "PT-101";

        // Valor que se muestra en la UI
        public double CurrentValue { get; set; } = 0.0;
        public string Unit { get; set; } = "bar";

        public string StatusColor => "#FFFFFF"; // Fondo blanco típico de instrumentos
        public string StatusText => $"{CurrentValue} {Unit}";

        public Dictionary<string, string> GetQuickViewData()
        {
            return new Dictionary<string, string> {
                { "Tag", Name },
                { "Reading", $"{CurrentValue} {Unit}" }
            };
        }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade) { }
        public void DetachConnection(string portName) { }
        public Action? OnTopologyChanged { get; set; }
    }
}
