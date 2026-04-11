using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Vessels
{
    public class FlashTankSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "V-102";
        public double Pressure { get; set; } = 1.0; // bar
        public double LiquidLevel { get; set; } = 50.0; // %

        public string StatusColor => "#63B3ED"; // Azul claro (Ready to solve)
        public string StatusText => "Awaiting Feed";

        public Dictionary<string, string> GetQuickViewData()
        {
            return new Dictionary<string, string> {
                { "P", $"{Pressure} bar" },
                { "Level", $"{LiquidLevel}%" }
            };
        }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade) => OnTopologyChanged?.Invoke();
        public void DetachConnection(string portName) => OnTopologyChanged?.Invoke();
        public Action? OnTopologyChanged { get; set; }
    }
}
