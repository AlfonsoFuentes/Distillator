using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.HeatExchangers
{
    public enum PlateExchangerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class PlateExchangerSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "E-103";

        // Variables del Intercambiador de Placas
        public double HeatDuty { get; set; } = 0.0; // kW
        public int NumberOfPlates { get; set; } = 120;
        public double U { get; set; } = 3500.0; // W/(m2*K) - Usualmente muy alto en placas

        public PlateExchangerStateType State { get; private set; } = PlateExchangerStateType.Created;

        public string StatusColor => State switch
        {
            PlateExchangerStateType.Created => "#CBD5E0",
            PlateExchangerStateType.PartiallyConnected => "#F6AD55",
            PlateExchangerStateType.ReadyToCalculate => "#63B3ED",
            PlateExchangerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public string StatusText => State switch
        {
            PlateExchangerStateType.Created => "Ready",
            PlateExchangerStateType.PartiallyConnected => "Underspecified",
            PlateExchangerStateType.ReadyToCalculate => "Ready to Solve",
            PlateExchangerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public Dictionary<string, string> GetQuickViewData()
        {
            var data = new Dictionary<string, string>();
            data.Add("Plates", $"{NumberOfPlates}");
            data.Add("U Value", $"{U} W/m2.K");
            data.Add("Duty", State == PlateExchangerStateType.Solved ? $"{Math.Round(HeatDuty, 2)} kW" : "-- kW");
            return data;
        }

        public IEquipmentFacade? HotInStream { get; private set; }
        public IEquipmentFacade? HotOutStream { get; private set; }
        public IEquipmentFacade? ColdInStream { get; private set; }
        public IEquipmentFacade? ColdOutStream { get; private set; }

        public Action? OnTopologyChanged { get; set; }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade)
        {
            if (portName == "HotIn") HotInStream = connectedFacade;
            else if (portName == "HotOut") HotOutStream = connectedFacade;
            else if (portName == "ColdIn") ColdInStream = connectedFacade;
            else if (portName == "ColdOut") ColdOutStream = connectedFacade;
            EvaluateAutoCalculation();
        }

        public void DetachConnection(string portName)
        {
            if (portName == "HotIn") HotInStream = null;
            else if (portName == "HotOut") HotOutStream = null;
            else if (portName == "ColdIn") ColdInStream = null;
            else if (portName == "ColdOut") ColdOutStream = null;
            EvaluateAutoCalculation();
        }

        private void EvaluateAutoCalculation()
        {
            int connections = (HotInStream != null ? 1 : 0) + (HotOutStream != null ? 1 : 0) +
                              (ColdInStream != null ? 1 : 0) + (ColdOutStream != null ? 1 : 0);

            if (connections == 4)
            {
                State = PlateExchangerStateType.Solved;
                HeatDuty = 2150.0;
            }
            else if (connections > 0) State = PlateExchangerStateType.PartiallyConnected;
            else State = PlateExchangerStateType.Created;

            OnTopologyChanged?.Invoke();
        }
    }
}
