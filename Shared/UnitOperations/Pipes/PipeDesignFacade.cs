using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Pipes
{
    public class PipeDesignFacade : EquipmentFacade
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
        public override void BuildEquations(EquationSystem eqs)
        {

        }

        public override IEnumerable<INewVariable> GetSolverVariables()
        {
            return null!;
        }
    }
}
