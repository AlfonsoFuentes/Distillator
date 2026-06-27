using Shared.SolverQwen.Variables;
using UnitSystem;

namespace Shared.SolverConsecutive
{
    

    public interface IVariable
    {
        VariableDefinedBy DataProcedence { get; }
        bool IsDefined { get; }
        bool IsCalculated { get; }
        bool IsCleareable { get; }

        void SetDisplayUnit(UnitMeasure unit);
        void Clear(VariableDefinedBy _procedence);

        event Action? ValueChanged;
        event Action<IVariable>? AddVariableToList;
        event Action? SpecificationChanged;
        event Action? ClearSpecificationChanged;

        void SetValueFromSolver(double solverValue, VariableDefinedBy _procedence);
        double GetSolverValue();

        string ToUiString(string format = "F2");
        void SetName(string name);
        string Name { get; }
        double NormalizeValue { get; }
    }

    public interface IVariable<T> : IVariable where T : Amount
    {
        UnitMeasure InternalUnit { get; }
        UnitMeasure DisplayUnit { get; }
        T Value { get; }
        bool HasChanged { get; }
        bool ShouldTriggerRecalculation { get; }
        void SetValue(T value, VariableDefinedBy _procedence);
    }

    public class Variable<T> : IVariable<T> where T : Amount
    {
        private T _value;
        private T _initValue = null!;

        public double NormalizeValue { get; } = 0;

        public string Name { get; private set; } = string.Empty;
        public void SetName(string name)
        {
            Name = $"{name}";
        }
        public bool IsCleareable { get; } = false;
        public bool HasChanged { get; private set; } = false;
        public bool IsDefined => DataProcedence != VariableDefinedBy.Undefined;

        public bool ShouldTriggerRecalculation =>
            DataProcedence == VariableDefinedBy.UserInput ||
            DataProcedence == VariableDefinedBy.Solver ||
            DataProcedence == VariableDefinedBy.Specification;

        public bool IsCalculated => DataProcedence != VariableDefinedBy.Undefined && DataProcedence != VariableDefinedBy.UserInput;

        public VariableDefinedBy DataProcedence { get; protected set; }
        public UnitMeasure InternalUnit { get; }
        public UnitMeasure DisplayUnit { get; private set; }

        public event Action<IVariable>? AddVariableToList;
        public event Action? SpecificationChanged = null!;
        public event Action? ClearSpecificationChanged = null!;
        public event Action? ValueChanged = null!;

        public void OnAddVariableToList(IVariable variable)
        {
            AddVariableToList?.Invoke(variable);
        }

        public T Value => _value;

        public Variable(T initialValue, UnitMeasure displayUnit, double _normalizeValue, bool _IsCleareable = false)
        {
            if (initialValue == null) throw new ArgumentNullException(nameof(initialValue));
            if (displayUnit == null) throw new ArgumentNullException(nameof(displayUnit));

            InternalUnit = initialValue.Unit;
            DisplayUnit = displayUnit;
            _value = initialValue;
            _initValue = initialValue;
            NormalizeValue = Math.Abs(_normalizeValue) < 1e-9 ? 1.0 : _normalizeValue;
            this.IsCleareable = _IsCleareable;
        }

        public void SetValue(T value, VariableDefinedBy _procedence)
        {
            HasChanged = true;
            _value = value;
            DataProcedence = _procedence;

            OnAddVariableToList(this);

            if (ShouldTriggerRecalculation)
            {
                ValueChanged?.Invoke();
            }
            HasChanged = false;
            SpecificationChanged?.Invoke();
        }

        public double GetSolverValue()
        {
            var valueInInternalUnit = Value.GetValue(InternalUnit);
            var solverValue = valueInInternalUnit / NormalizeValue;

            return solverValue;
        }

        public void SetValueFromSolver(double solverValue, VariableDefinedBy _procedence)
        {
            HasChanged = true;

            var newValue = solverValue * NormalizeValue;

            _value.SetValue(newValue, InternalUnit);
            DataProcedence = DataProcedence == VariableDefinedBy.Undefined ? _procedence : DataProcedence;
            OnAddVariableToList(this);
            if (ShouldTriggerRecalculation)
            {
                ValueChanged?.Invoke();
            }
            HasChanged = false;
            SpecificationChanged?.Invoke();
        }

        public void Clear(VariableDefinedBy _procedence)
        {
            if (DataProcedence != _procedence) return;

            var oldIsSpecToEquilibrium = ShouldTriggerRecalculation;
            DataProcedence = VariableDefinedBy.Undefined;

            if (oldIsSpecToEquilibrium)
            {
                ValueChanged?.Invoke();
            }
            ClearSpecificationChanged?.Invoke();
        }

        public void SetDisplayUnit(UnitMeasure unit) => DisplayUnit = unit;

        public string ToUiString(string format = "F2")
        {
            if (DataProcedence == VariableDefinedBy.Undefined)
                return "<Not defined>";

            double valueInDisplayUnit = _value.GetValue(DisplayUnit);
            return $"{valueInDisplayUnit.ToString(format)} {DisplayUnit.Symbol}";
        }

        // ========== 🔥 AGREGADOS EXCLUSIVOS PARA CONEXIÓN CON LA UI ==========

        // Flags que la UI usa en GetSourceClass()
        public bool IsDefinedByUI => DataProcedence == VariableDefinedBy.UserInput;
        public bool IsDefinedByStream => DataProcedence == VariableDefinedBy.StreamCalculated;

       

        // Propiedad "Source" (string) que la UI usa para el tooltip ("Calculated by: ...")
        public string Source => DataProcedence switch
        {
            VariableDefinedBy.UserInput => "UserInput",
            VariableDefinedBy.StreamCalculated => "StreamCalculated",
            VariableDefinedBy.Solver => "Solver",
            VariableDefinedBy.Specification => "Specification",
            _ => "Undefined"
        };

        // Métodos puente que la UI llama (sin alterar la lógica original de SetValue/Clear)
        public void ClearFromUI() => Clear(VariableDefinedBy.UserInput);
        public void SetValueFromUI(T value) => SetValue(value, VariableDefinedBy.UserInput);
        public double GetDisplayValue() => _value.GetValue(DisplayUnit);
        public string GetDisplayUnit() => DisplayUnit.Symbol;
        public UnitMeasure UnitForUI => DisplayUnit;
        public void ChangeUnitForUI(UnitMeasure unit) => SetDisplayUnit(unit);
    }

}
