using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.HeatExchangers
{
    

    public enum PlateExchangerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class PlateExchangerSimulationFacade : EquipmentFacade
    {
        public PlateExchangerStateType State { get; set; } = PlateExchangerStateType.Created;

        // --- Lado Caliente (Hot Side) ---
        public StreamSimulationFacade? HotInStream { get; private set; }
        public StreamSimulationFacade? HotOutStream { get; private set; }

        // --- Lado Frío (Cold Side) ---
        public StreamSimulationFacade? ColdInStream { get; private set; }
        public StreamSimulationFacade? ColdOutStream { get; private set; }

        public override string StatusColor => State switch
        {
            PlateExchangerStateType.Created => "#CBD5E0",
            PlateExchangerStateType.PartiallyConnected => "#F6AD55",
            PlateExchangerStateType.ReadyToCalculate => "#63B3ED",
            PlateExchangerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => State switch
        {
            PlateExchangerStateType.Created => "Ready",
            PlateExchangerStateType.PartiallyConnected => "Underspecified",
            PlateExchangerStateType.ReadyToCalculate => "Ready to Solve",
            PlateExchangerStateType.Solved => "Converged",
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

            if (portName == "HotIn") HotInStream = stream;
            else if (portName == "HotOut") HotOutStream = stream;
            else if (portName == "ColdIn") ColdInStream = stream;
            else if (portName == "ColdOut") ColdOutStream = stream;

            OnExecuteSolver?.Invoke(this);
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "HotIn") HotInStream = null;
            else if (portName == "HotOut") HotOutStream = null;
            else if (portName == "ColdIn") ColdInStream = null;
            else if (portName == "ColdOut") ColdOutStream = null;

            OnExecuteSolver?.Invoke(this);
        }

        protected override void CalculatedEquipment()
        {
            // Placeholder para balance térmico
            State = PlateExchangerStateType.ReadyToCalculate;
        }
    }
}
