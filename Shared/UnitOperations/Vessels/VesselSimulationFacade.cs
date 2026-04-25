using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Vessels
{
   

    public enum VesselStateType { Created, PartiallyConnected, Solved }

    public class VesselSimulationFacade : EquipmentFacade
    {
        public VesselStateType State { get; set; } = VesselStateType.Created;

        // --- Topología Dinámica ---
        public Dictionary<string, StreamSimulationFacade> InletStreams { get; } = new();
        public Dictionary<string, StreamSimulationFacade> OutletStreams { get; } = new();

        public override string StatusColor => State switch
        {
            VesselStateType.Created => "#CBD5E0",
            VesselStateType.PartiallyConnected => "#F6AD55",
            VesselStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => State switch
        {
            VesselStateType.Created => "Ready",
            VesselStateType.PartiallyConnected => "Underspecified",
            VesselStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            return new List<ToolTipLegend>();
        }

        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            var stream = connectedFacade as StreamSimulationFacade;
            if (stream == null) return;

            if (portName.StartsWith("Inlet")) InletStreams[portName] = stream;
            else if (portName.StartsWith("Outlet")) OutletStreams[portName] = stream;

            OnExecuteSolver?.Invoke(this);
        }

        public override void DetachConnection(string portName)
        {
            if (portName.StartsWith("Inlet")) InletStreams.Remove(portName);
            else if (portName.StartsWith("Outlet")) OutletStreams.Remove(portName);

            OnExecuteSolver?.Invoke(this);
        }

        protected override void CalculatedEquipment()
        {
            // Placeholder para futuros cálculos de inventario o balance
            State = VesselStateType.Solved;
        }
    }
}
