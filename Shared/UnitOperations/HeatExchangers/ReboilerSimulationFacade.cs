using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.HeatExchangers
{
    public enum ReboilerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class ReboilerSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "E-102";

        // Variables del Rehervidor
        public double HeatDuty { get; set; } = 0.0; // kW
        public double VaporFractionOut { get; set; } = 0.35; // Típicamente no vaporiza todo

        public ReboilerStateType State { get; private set; } = ReboilerStateType.Created;

        public string StatusColor => State switch
        {
            ReboilerStateType.Created => "#CBD5E0",
            ReboilerStateType.PartiallyConnected => "#F6AD55",
            ReboilerStateType.ReadyToCalculate => "#63B3ED",
            ReboilerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public string StatusText => State switch
        {
            ReboilerStateType.Created => "Ready",
            ReboilerStateType.PartiallyConnected => "Underspecified",
            ReboilerStateType.ReadyToCalculate => "Ready to Solve",
            ReboilerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public Dictionary<string, string> GetQuickViewData()
        {
            var data = new Dictionary<string, string>();
            data.Add("Duty", State == ReboilerStateType.Solved ? $"{Math.Round(HeatDuty, 2)} kW" : "-- kW");
            data.Add("Vapor Frac.", $"{VaporFractionOut * 100}%");
            return data;
        }

        public IEquipmentFacade? TubeInStream { get; private set; }
        public IEquipmentFacade? TubeOutStream { get; private set; }
        public IEquipmentFacade? ShellInStream { get; private set; }
        public IEquipmentFacade? CondensateOutStream { get; private set; }

        public Action? OnTopologyChanged { get; set; }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade)
        {
            if (portName == "TubeIn") TubeInStream = connectedFacade;
            else if (portName == "TubeOut") TubeOutStream = connectedFacade;
            else if (portName == "ShellIn") ShellInStream = connectedFacade;
            else if (portName == "CondensateOut") CondensateOutStream = connectedFacade;
            EvaluateAutoCalculation();
        }

        public void DetachConnection(string portName)
        {
            if (portName == "TubeIn") TubeInStream = null;
            else if (portName == "TubeOut") TubeOutStream = null;
            else if (portName == "ShellIn") ShellInStream = null;
            else if (portName == "CondensateOut") CondensateOutStream = null;
            EvaluateAutoCalculation();
        }

        private void EvaluateAutoCalculation()
        {
            int connections = (TubeInStream != null ? 1 : 0) + (TubeOutStream != null ? 1 : 0) +
                              (ShellInStream != null ? 1 : 0) + (CondensateOutStream != null ? 1 : 0);

            if (connections == 4)
            {
                State = ReboilerStateType.Solved;
                HeatDuty = 850.0;
            }
            else if (connections > 0) State = ReboilerStateType.PartiallyConnected;
            else State = ReboilerStateType.Created;

            OnTopologyChanged?.Invoke();
        }
    }
}
