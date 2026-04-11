using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.ControlValves
{
    public enum ValveStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class ControlValveSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "CV-101";

        // Variables de la Válvula
        public double OpeningPercentage { get; set; } = 50.0; // % de apertura
        public double Cv { get; set; } = 15.5; // Coeficiente de flujo
        public double PressureDrop { get; set; } = 0.5; // bar

        public ValveStateType State { get; private set; } = ValveStateType.Created;

        public string StatusColor => State switch
        {
            ValveStateType.Created => "#CBD5E0",
            ValveStateType.PartiallyConnected => "#F6AD55",
            ValveStateType.ReadyToCalculate => "#63B3ED",
            ValveStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public string StatusText => State switch
        {
            ValveStateType.Created => "Ready",
            ValveStateType.PartiallyConnected => "Underspecified",
            ValveStateType.ReadyToCalculate => "Ready to Solve",
            ValveStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public Dictionary<string, string> GetQuickViewData()
        {
            var data = new Dictionary<string, string>();
            data.Add("Opening", $"{OpeningPercentage} %");
            data.Add("ΔP", $"{PressureDrop} bar");
            return data;
        }

        public IEquipmentFacade? InletStream { get; private set; }
        public IEquipmentFacade? OutletStream { get; private set; }

        public Action? OnTopologyChanged { get; set; }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade)
        {
            if (portName == "Inlet") InletStream = connectedFacade;
            else if (portName == "Outlet") OutletStream = connectedFacade;
            EvaluateAutoCalculation();
        }

        public void DetachConnection(string portName)
        {
            if (portName == "Inlet") InletStream = null;
            else if (portName == "Outlet") OutletStream = null;
            EvaluateAutoCalculation();
        }

        private void EvaluateAutoCalculation()
        {
            if (InletStream != null && OutletStream != null) State = ValveStateType.Solved;
            else if (InletStream != null || OutletStream != null) State = ValveStateType.PartiallyConnected;
            else State = ValveStateType.Created;

            OnTopologyChanged?.Invoke();
        }
    }
}
