using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Helpers
{
    public enum SplitterStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class SplitterSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "SP-101";

        public SplitterStateType State { get; private set; } = SplitterStateType.Created;

        public string StatusColor => State switch
        {
            SplitterStateType.Created => "#CBD5E0",
            SplitterStateType.PartiallyConnected => "#F6AD55",
            SplitterStateType.ReadyToCalculate => "#63B3ED",
            SplitterStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public string StatusText => State switch
        {
            SplitterStateType.Created => "Ready",
            SplitterStateType.PartiallyConnected => "Underspecified",
            SplitterStateType.ReadyToCalculate => "Ready to Solve",
            SplitterStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public Dictionary<string, string> GetQuickViewData()
        {
            var data = new Dictionary<string, string>();
            data.Add("Type", "Mass Splitter");
            data.Add("Outlets", State == SplitterStateType.Solved ? "Balanced" : "Pending");
            return data;
        }

        public IEquipmentFacade? InletStream { get; private set; }
        public Dictionary<string, IEquipmentFacade> OutletStreams { get; } = new();

        public Action? OnTopologyChanged { get; set; }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade)
        {
            if (portName == "Inlet") InletStream = connectedFacade;
            else if (portName.StartsWith("Outlet")) OutletStreams[portName] = connectedFacade;
            EvaluateAutoCalculation();
        }

        public void DetachConnection(string portName)
        {
            if (portName == "Inlet") InletStream = null;
            else if (portName.StartsWith("Outlet")) OutletStreams.Remove(portName);
            EvaluateAutoCalculation();
        }

        private void EvaluateAutoCalculation()
        {
            // Para resolver un divisor, necesitamos la entrada y al menos 2 salidas conectadas
            if (InletStream != null && OutletStreams.Count >= 2)
            {
                State = SplitterStateType.Solved;
            }
            else if (InletStream != null || OutletStreams.Count > 0)
            {
                State = SplitterStateType.PartiallyConnected;
            }
            else
            {
                State = SplitterStateType.Created;
            }
            OnTopologyChanged?.Invoke();
        }
    }
}
