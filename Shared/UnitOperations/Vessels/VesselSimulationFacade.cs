using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Basiss;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Vessels
{
    public enum VesselStateType { Created, PartiallyConnected, Solved }

    public class VesselSimulationFacade : EquipmentFacade
    {


        public override string StatusColor => "#63B3ED"; // Azul claro (Ready to solve)
        public override string StatusText => "Awaiting Feed";

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();

            return result;

        }

        public override void AttachConnection(string portName, IFacade connectedFacade) => OnExecuteSolver?.Invoke(this);
        public override void DetachConnection(string portName) => OnExecuteSolver?.Invoke(this);
  

        protected override void CalculatedEquipment()
        {

        }
    }
}
