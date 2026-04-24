using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Basiss;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.HeatExchangers
{
    public enum PlateExchangerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class PlateExchangerSimulationFacade : EquipmentFacade
    {


        public override string StatusColor => "#63B3ED"; // Azul claro (Ready to solve)
        public override string StatusText => "Awaiting Feed";

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();

            return result;

        }

        public override void AttachConnection(string portName, IFacade connectedFacade)
        {

        }
        public override void DetachConnection(string portName)
        {

        }


        protected override void CalculatedEquipment()
        {

        }
    }
}
