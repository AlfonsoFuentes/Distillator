using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.HeatExchangers
{
 

    public enum ReboilerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class ReboilerSimulationFacade : EquipmentFacade
    {
        public ReboilerStateType State { get; set; } = ReboilerStateType.Created;

        // --- Lado de los Tubos (Líquido de fondo de la torre) ---
        public StreamSimulationFacade? TubeInStream { get; private set; }
        public StreamSimulationFacade? TubeOutStream { get; private set; }

        // --- Lado de la Coraza (Vapor de calentamiento) ---
        public StreamSimulationFacade? ShellInStream { get; private set; }
        public StreamSimulationFacade? CondensateOutStream { get; private set; }

        public override string StatusColor => State switch
        {
            ReboilerStateType.Created => "#CBD5E0",
            ReboilerStateType.PartiallyConnected => "#F6AD55",
            ReboilerStateType.ReadyToCalculate => "#63B3ED",
            ReboilerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => State switch
        {
            ReboilerStateType.Created => "Ready",
            ReboilerStateType.PartiallyConnected => "Underspecified",
            ReboilerStateType.ReadyToCalculate => "Ready to Solve",
            ReboilerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            return new List<ToolTipLegend>(); // Se llenará con el Heat Duty cuando se calcule
        }

        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            var stream = connectedFacade as StreamSimulationFacade;
            if (stream == null) return;

            if (portName == "TubeIn") TubeInStream = stream;
            else if (portName == "TubeOut") TubeOutStream = stream;
            else if (portName == "ShellIn") ShellInStream = stream;
            else if (portName == "CondensateOut") CondensateOutStream = stream;

            OnExecuteSolver?.Invoke(this);
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "TubeIn") TubeInStream = null;
            else if (portName == "TubeOut") TubeOutStream = null;
            else if (portName == "ShellIn") ShellInStream = null;
            else if (portName == "CondensateOut") CondensateOutStream = null;

            OnExecuteSolver?.Invoke(this);
        }

        protected override void CalculatedEquipment()
        {
            // Placeholder para balance térmico del fondo de la columna
            State = ReboilerStateType.ReadyToCalculate;
        }
    }
}
