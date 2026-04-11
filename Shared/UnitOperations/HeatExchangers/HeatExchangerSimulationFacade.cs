using Shared.UnitOperations.Pumps;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.HeatExchangers
{
    public enum HeatExchangerStateType
    {
        Created,
        PartiallyConnected,
        ReadyToCalculate,
        Solved
    }

    public class HeatExchangerSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "E-101";

        // Variables del Intercambiador
        public double HeatDuty { get; set; } = 0.0; // kW
        public double Area { get; set; } = 50.0;    // m2
        public double U { get; set; } = 500.0;      // Coeficiente Global W/(m2*K)

        public HeatExchangerStateType State { get; private set; } = HeatExchangerStateType.Created;

        public string StatusColor => State switch
        {
            HeatExchangerStateType.Created => "#CBD5E0",
            HeatExchangerStateType.PartiallyConnected => "#F6AD55",
            HeatExchangerStateType.ReadyToCalculate => "#63B3ED",
            HeatExchangerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };
        public string StatusText => State switch
        {
            HeatExchangerStateType.Created => "Ready",
            HeatExchangerStateType.PartiallyConnected => "Underspecified",
            HeatExchangerStateType.ReadyToCalculate => "Ready to Solve",
            HeatExchangerStateType.Solved => "Converged",
            _ => "Unknown"
        };
        public Dictionary<string, string> GetQuickViewData()
        {
            var data = new Dictionary<string, string>();
            data.Add("Area", $"{Area} m2");
            data.Add("U Value", $"{U} W/m2.K");
            data.Add("Duty", State == HeatExchangerStateType.Solved ? $"{Math.Round(HeatDuty, 2)} kW" : "-- kW");
            return data;
        }

        // Topología de puertos (basada en el VisualElement)
        public IEquipmentFacade? TubeInStream { get; private set; }
        public IEquipmentFacade? TubeOutStream { get; private set; }
        public IEquipmentFacade? ShellInStream { get; private set; }
        public IEquipmentFacade? CondensateOutStream { get; private set; }
        public IEquipmentFacade? VaporVentStream { get; private set; }

        public Action? OnTopologyChanged { get; set; }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade)
        {
            if (portName == "TubeIn") TubeInStream = connectedFacade;
            else if (portName == "TubeOut") TubeOutStream = connectedFacade;
            else if (portName == "ShellIn") ShellInStream = connectedFacade;
            else if (portName == "CondensateOut") CondensateOutStream = connectedFacade;
            else if (portName == "VaporVent") VaporVentStream = connectedFacade;

            EvaluateAutoCalculation();
        }

        public void DetachConnection(string portName)
        {
            if (portName == "TubeIn") TubeInStream = null;
            else if (portName == "TubeOut") TubeOutStream = null;
            else if (portName == "ShellIn") ShellInStream = null;
            else if (portName == "CondensateOut") CondensateOutStream = null;
            else if (portName == "VaporVent") VaporVentStream = null;

            EvaluateAutoCalculation();
        }

        private void EvaluateAutoCalculation()
        {
            int connections = 0;
            if (TubeInStream != null) connections++;
            if (TubeOutStream != null) connections++;
            if (ShellInStream != null) connections++;
            if (CondensateOutStream != null) connections++;
            if (VaporVentStream != null) connections++;

            if (connections >= 4) // Asumimos que el venteo puede ser opcional
            {
                State = HeatExchangerStateType.Solved;
                HeatDuty = 1250.5; // Simulación
            }
            else if (connections > 0)
            {
                State = HeatExchangerStateType.PartiallyConnected;
                HeatDuty = 0;
            }
            else
            {
                State = HeatExchangerStateType.Created;
                HeatDuty = 0;
            }
            OnTopologyChanged?.Invoke();
        }
    }
}
