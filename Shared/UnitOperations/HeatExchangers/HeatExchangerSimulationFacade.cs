using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.HeatExchangers
{
   

    public enum HeatExchangerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class HeatExchangerSimulationFacade : EquipmentFacade
    {
        public HeatExchangerStateType State { get; set; } = HeatExchangerStateType.Created;

        // --- Lado de los Tubos ---
        public StreamSimulationFacade? TubeInStream { get; private set; }
        public StreamSimulationFacade? TubeOutStream { get; private set; }

        // --- Lado de la Coraza (Shell) ---
        public StreamSimulationFacade? ShellInStream { get; private set; }
        public StreamSimulationFacade? CondensateOutStream { get; private set; }
        public StreamSimulationFacade? VaporVentStream { get; private set; }

        public override string StatusColor => State switch
        {
            HeatExchangerStateType.Created => "#CBD5E0",
            HeatExchangerStateType.PartiallyConnected => "#F6AD55",
            HeatExchangerStateType.ReadyToCalculate => "#63B3ED",
            HeatExchangerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => State switch
        {
            HeatExchangerStateType.Created => "Ready",
            HeatExchangerStateType.PartiallyConnected => "Underspecified",
            HeatExchangerStateType.ReadyToCalculate => "Ready to Solve",
            HeatExchangerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            return new List<ToolTipLegend>(); // Se llenará con el Duty (Heat Load) o el UxA luego
        }

        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            var stream = connectedFacade as StreamSimulationFacade;
            if (stream == null) return;

            if (portName == "TubeIn") TubeInStream = stream;
            else if (portName == "TubeOut") TubeOutStream = stream;
            else if (portName == "ShellIn") ShellInStream = stream;
            else if (portName == "CondensateOut") CondensateOutStream = stream;
            else if (portName == "VaporVent") VaporVentStream = stream;

            OnExecuteSolver?.Invoke(this);
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "TubeIn") TubeInStream = null;
            else if (portName == "TubeOut") TubeOutStream = null;
            else if (portName == "ShellIn") ShellInStream = null;
            else if (portName == "CondensateOut") CondensateOutStream = null;
            else if (portName == "VaporVent") VaporVentStream = null;

            OnExecuteSolver?.Invoke(this);
        }

        protected override void CalculatedEquipment()
        {
            // Placeholder para balance de energía, LMTD, o método NTU
            State = HeatExchangerStateType.ReadyToCalculate;
        }
    }
}
