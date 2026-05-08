namespace Shared.Thermodynamics.ControlledVariables
{
    public class NewControlledVariableDouble : INewVariable<double>
    {
        public Action? OnPropagateVariable { get; set; }
        public NewControlledVariableDouble()
        {


        }

        public NewControlledVariableDouble(double _value,double _initValue)
        {
            Value = _value;
            SolverValue = 0;
            InitValue = _initValue;

        }
        public double InitValue { get;  }
        public Action? OnSetMaterialStreamValue { get; set; }
        public Action? OnGoToLocalCalculation { get; set; }

        public Action<INewVariable>? OnAddLocalCalculatedVariable { get; set; }
        public Action? OnExecuteSolver { get; set; }
        public int Index { get; set; }

        public virtual double SolverValue { get; set; } = 0;

        public bool IsSpecifiedbyUI { get; protected set; } = false;
        public bool IsSpecifiedbySolver { get; set; } = false;
        public bool IsEspecifiedbyLocalCalculation { get; set; } = false;

        public bool IsEspecified => IsSpecifiedbyUI || IsSpecifiedbySolver || IsEspecifiedbyLocalCalculation;

        public double? SpecifiedValue { get; protected set; }



        public double Value { get; set; }

        public virtual void SetValueFromUI(double value)
        {

            SolverValue = value;
            Value = value;
            IsSpecifiedbyUI = true;
            SpecifiedValue = value;
            OnSetMaterialStreamValue?.Invoke();
            OnGoToLocalCalculation?.Invoke();

            OnExecuteSolver?.Invoke();
        }
        public virtual void SetValueFromLocalCalculatedVariable(double value)
        {
            Value = value;
            IsEspecifiedbyLocalCalculation = true;
            SpecifiedValue = value;
            SolverValue = value;
            OnSetMaterialStreamValue?.Invoke();
            OnAddLocalCalculatedVariable?.Invoke(this);

        }

        public virtual void SetValueFromSolver(double value)
        {
            SolverValue = value;
            Value=value;
            IsSpecifiedbySolver = true;
            OnSetMaterialStreamValue?.Invoke();
            OnGoToLocalCalculation?.Invoke();
        }

        public virtual void ClearFromUI()
        {
            IsSpecifiedbyUI = false;
            IsSpecifiedbySolver = false;
            SolverValue = 0;
            SpecifiedValue = null;
            
            OnGoToLocalCalculation?.Invoke();
            OnExecuteSolver?.Invoke();

        }



        public virtual void ClearFromSolver()
        {
            if (!IsSpecifiedbySolver) return;

            IsSpecifiedbySolver = false;
            SpecifiedValue = null;
            // opcional: resetear valor o dejar último valor
            OnGoToLocalCalculation?.Invoke();
        }


    }
}



