using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;

namespace Shared.UnitOperations.Basiss
{
    public abstract class EquipmentFacade : IFacade
    {
        private List<IControlledVariable> _calculatedVariables = new();
        protected void AddCalculatedVariable(IControlledVariable controlledVariable)
        {
            if (controlledVariable != null && !_calculatedVariables.Contains(controlledVariable))
            {
                _calculatedVariables.Add(controlledVariable);
            }
        }

       public void ResetCalculatedVariable()
        {
            foreach (var controlledVariable in _calculatedVariables)
            {
                // 🚩 Ahora la variable se limpia a sí misma y avisa a su dueño (S-2)
                // (Asegúrate de que IControlledVariable tenga la firma del método)
                controlledVariable.RevertCalculatedValue();
            }

            _calculatedVariables.Clear();

        }
        public void Calculate()
        {
            ResetCalculatedVariable();
            CalculatedEquipment();
        }
        protected virtual void CalculatedEquipment()
        {

        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public abstract string StatusText { get; }

        public abstract string StatusColor { get; }

        public abstract List<ToolTipLegend> GetToolTipLegend();
        public abstract void AttachConnection(string portName, IFacade connectedFacade);

        public abstract void DetachConnection(string portName);


        public Action<IFacade>? OnExecuteSolver { get; set; }
    }
}
