using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.ControlledVariables
{
    
    public interface INewVariable
    {
        int Index { get; set; }

        double SolverValue { get; }

        bool IsSpecifiedbyUI { get; }
        double? SpecifiedValue { get; }
        double InitValue { get; }
        void ClearFromUI();

        void SetValueFromSolver(double value);
        Action? OnPropagateVariable { get; set; }
        Action? OnSetMaterialStreamValue { get; set; }
        Action? OnGoToLocalCalculation { get; set; }
        Action<INewVariable>? OnAddLocalCalculatedVariable { get; set; }
        Action? OnExecuteSolver { get; set; }
        bool IsEspecifiedbyLocalCalculation { get; set; }
        bool IsEspecified { get; }
        bool IsSpecifiedbySolver { get; set; }

        void ClearFromSolver();
    }
    public interface INewVariable<T> : INewVariable
    {
        T Value { get; set; }


        void SetValueFromUI(T value);
        void SetValueFromLocalCalculatedVariable(T value);
    }
    public interface INewVariableAmount<T> : INewVariable where T : Amount
    {
        UnitMeasure UnitForUI { get; }
        UnitMeasure UnitForSolver { get; }

        T Value { get; set; }

        void SetValueFromUI(T value);
        void SetValueFromLocalCalculatedVariable(T value);

        void ChangeUnitForUI(UnitMeasure newUnit);
        double GetDisplayValue();
        string GetDisplayUnit();
    }
    //public class NewControlledVariableComposition : INewVariable<StreamComposition>
    //{
    //    public NewControlledVariableComposition()
    //    {


    //    }
    //    public double InitValue => 0;
    //    public NewControlledVariableComposition(StreamComposition _value)
    //    {
    //        Value = _value;
    //        SolverValue = 0;
    //        Value.OnAddLocalCalculatedVariable += OnAddLocalCalculatedVariable;

    //    }
    //    public Action? OnPropagateVariable { get; set; }
    //    public Action? OnSetMaterialStreamValue { get; set; }
    //    public Action? OnGoToLocalCalculation { get; set; }

    //    public Action<INewNewVariable<StreamComposition>>? OnAddLocalCalculatedVariable { get; set; }
    //    public Action? OnExecuteSolver { get; set; }
    //    public int Index { get; set; }

    //    public virtual double SolverValue { get; set; } = 0;

    //    public bool IsSpecifiedbyUI { get; protected set; } = false;
    //    public bool IsSpecifiedbySolver { get; set; } = false;
    //    public bool IsEspecifiedbyLocalCalculation { get; set; } = false;

    //    public bool IsEspecified => IsSpecifiedbyUI || IsSpecifiedbySolver || IsEspecifiedbyLocalCalculation;

    //    public double? SpecifiedValue { get; protected set; }



    //    public StreamComposition Value { get; set; } = default(StreamComposition)!;

    //    public virtual void SetValueFromUI(StreamComposition value)
    //    {


    //        Value = value;
    //        IsSpecifiedbyUI = true;

    //        OnSetMaterialStreamValue?.Invoke();
    //        OnGoToLocalCalculation?.Invoke();
    //        OnPropagateVariable?.Invoke();
    //        OnExecuteSolver?.Invoke();
    //    }
    //    public virtual void SetValueFromLocalCalculatedVariable(StreamComposition value)
    //    {
    //        Value = value;
    //        IsEspecifiedbyLocalCalculation = true;

    //        OnSetMaterialStreamValue?.Invoke();
    //        OnAddLocalCalculatedVariable?.Invoke(this);
    //        OnPropagateVariable?.Invoke();

    //    }

    //    public virtual void SetValueFromSolver(double value)
    //    {
    //        SolverValue = value;

    //        IsSpecifiedbySolver = true;
    //        OnSetMaterialStreamValue?.Invoke();
    //        OnGoToLocalCalculation?.Invoke();
    //    }

    //    public virtual void ClearFromUI()
    //    {
    //        IsSpecifiedbyUI = false;
    //        IsSpecifiedbySolver = false;
    //        SpecifiedValue = null;
    //        OnGoToLocalCalculation?.Invoke();
    //        OnPropagateVariable?.Invoke();
    //        OnExecuteSolver?.Invoke();

    //    }



    //    public virtual void ClearFromSolver()
    //    {
    //        if (!IsSpecifiedbySolver) return;

    //        IsSpecifiedbySolver = false;

    //        // opcional: resetear valor o dejar último valor
    //        OnGoToLocalCalculation?.Invoke();
    //    }


    //}

    public class NewControlledVariable<T> : INewVariable<T>
    {
        public Action? OnPropagateVariable { get; set; }
        public NewControlledVariable()
        {


        }
        public double InitValue { get; }
        public NewControlledVariable(T _value, double _initValue)
        {
            Value = _value;
            SolverValue = 0;
            InitValue = _initValue;
        }
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



        public T Value { get; set; } = default(T)!;

        public virtual void SetValueFromUI(T value)
        {


            Value = value;
            IsSpecifiedbyUI = true;
            OnSetMaterialStreamValue?.Invoke();
            OnGoToLocalCalculation?.Invoke();

            OnExecuteSolver?.Invoke();
        }
        public virtual void SetValueFromLocalCalculatedVariable(T value)
        {
            Value = value;
            IsEspecifiedbyLocalCalculation = true;
            OnSetMaterialStreamValue?.Invoke();
            OnAddLocalCalculatedVariable?.Invoke(this);

        }

        public virtual void SetValueFromSolver(double value)
        {
            SolverValue = value;

            IsSpecifiedbySolver = true;



            OnSetMaterialStreamValue?.Invoke();
            OnGoToLocalCalculation?.Invoke();
        }

        public virtual void ClearFromUI()
        {
            IsSpecifiedbyUI = false;
            IsSpecifiedbySolver = false;
            SpecifiedValue = null;
            OnGoToLocalCalculation?.Invoke();
            OnExecuteSolver?.Invoke();

        }



        public virtual void ClearFromSolver()
        {
            if (!IsSpecifiedbySolver) return;

            IsSpecifiedbySolver = false;

            // opcional: resetear valor o dejar último valor
            OnGoToLocalCalculation?.Invoke();
        }


    }
}



