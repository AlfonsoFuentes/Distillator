using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Helpers
{
    public enum MixerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class MixerSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "MIX-101";

        public MixerStateType State { get; private set; } = MixerStateType.Created;

        public string StatusColor => State switch
        {
            MixerStateType.Created => "#CBD5E0",
            MixerStateType.PartiallyConnected => "#F6AD55",
            MixerStateType.ReadyToCalculate => "#63B3ED",
            MixerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public string StatusText => State switch
        {
            MixerStateType.Created => "Ready",
            MixerStateType.PartiallyConnected => "Underspecified",
            MixerStateType.ReadyToCalculate => "Ready to Solve",
            MixerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public Dictionary<string, string> GetQuickViewData()
        {
            var data = new Dictionary<string, string>();
            data.Add("Type", "Mass Mixer");
            data.Add("Status", State == MixerStateType.Solved ? "Blended" : "Pending");
            return data;
        }

        public Dictionary<string, IEquipmentFacade> InletStreams { get; } = new();
        public IEquipmentFacade? OutletStream { get; private set; }

        public Action? OnTopologyChanged { get; set; }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade)
        {
            if (portName == "Outlet") OutletStream = connectedFacade;
            else if (portName.StartsWith("Inlet")) InletStreams[portName] = connectedFacade;
            EvaluateAutoCalculation();
        }

        public void DetachConnection(string portName)
        {
            if (portName == "Outlet") OutletStream = null;
            else if (portName.StartsWith("Inlet")) InletStreams.Remove(portName);
            EvaluateAutoCalculation();
        }

        private void EvaluateAutoCalculation()
        {
            // Un mezclador necesita al menos 2 entradas y 1 salida para estar resuelto
            if (OutletStream != null && InletStreams.Count >= 2)
            {
                State = MixerStateType.Solved;
            }
            else if (OutletStream != null || InletStreams.Count > 0)
            {
                State = MixerStateType.PartiallyConnected;
            }
            else
            {
                State = MixerStateType.Created;
            }
            OnTopologyChanged?.Invoke();
        }
    }
}
