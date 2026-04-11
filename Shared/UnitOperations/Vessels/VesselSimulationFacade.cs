using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Vessels
{
    public enum VesselStateType { Created, PartiallyConnected, Solved }

    public class VesselSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "V-101";
        public double LiquidLevel { get; set; } = 45.0; // %

        public string StatusColor => "#CBD5E0"; // Gris por defecto
        public string StatusText => "Operational";

        public Dictionary<string, string> GetQuickViewData()
        {
            return new Dictionary<string, string> {
                { "Level", $"{LiquidLevel}%" },
                { "Status", "In Service" }
            };
        }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade) { OnTopologyChanged?.Invoke(); }
        public void DetachConnection(string portName) { OnTopologyChanged?.Invoke(); }
        public Action? OnTopologyChanged { get; set; }
    }
}
