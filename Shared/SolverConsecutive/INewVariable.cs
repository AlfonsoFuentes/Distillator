using Shared.SolverQwen.Variables;
using UnitSystem;

namespace Shared.SolverConsecutive
{
    public interface INewVariable
    {


        VariableDefinedBy DataProcedence { get; }
        bool IsDefined { get; }
        bool IsCalculated { get; }
        bool IsCleareable { get; }
       
        void SetDisplayUnit(UnitMeasure unit);
        void Clear(VariableDefinedBy _procedence);

        event Action? ValueChanged;
        event Action<INewVariable>? AddVariableToList;
        event Action? SpecificationChanged;
        event Action? ClearSpecificationChanged;

        public void SetValueFromSolver(double solverValue, VariableDefinedBy _procedence);
        double GetSolverValue();

        string ToUiString(string format = "F2");
        void SetName(string name);
        string Name { get; }
        double NormalizeValue { get; }
    }
    public interface INewVariable<T> : INewVariable where T : Amount
    {
        UnitMeasure InternalUnit { get; }
        UnitMeasure DisplayUnit { get; }
        /// </summary>
        T Value { get; }

        /// <summary>
        bool HasChanged { get; }
        bool ShouldTriggerRecalculation { get; }



        /// </summary>
        void SetValue(T value, VariableDefinedBy _procedence);


    }
    public class NewVariable<T> : INewVariable<T> where T : Amount
    {
        private T _value;

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
        public event Action<INewVariable>? AddVariableToList;

        public event Action? SpecificationChanged = null!;
        public event Action? ClearSpecificationChanged = null!; // Nuevo evento
        public void OnAddVariableToList(INewVariable variable)
        {
            AddVariableToList?.Invoke(variable);
        }


        public T Value => _value;


        public event Action? ValueChanged = null!;

        T _initValue = null!;

        public NewVariable(T initialValue, UnitMeasure displayUnit, double _normalizeValue, bool _IsCleareable = false)
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

            //var initialValueInInternalUnit = _initValue.GetValue(InternalUnit);
            //SetValueFromSolver(initialValueInInternalUnit / NormalizeValue, _procedence);
            var oldIsSpecToEquilibrium = ShouldTriggerRecalculation;
            DataProcedence = VariableDefinedBy.Undefined;

            if (oldIsSpecToEquilibrium)
            {
                ValueChanged?.Invoke();
            }
            ClearSpecificationChanged?.Invoke(); // En lugar de SpecificationChanged
        }


        public void SetDisplayUnit(UnitMeasure unit) => DisplayUnit = unit;
        // En ProcessVariable<T>
        public string ToUiString(string format = "F2")
        {
            // ✅ Si no está definida, retornar placeholder visual
            if (DataProcedence == VariableDefinedBy.Undefined)
                return "<Not defined>";

            // ✅ Obtener valor en unidad de visualización y formatear
            double valueInDisplayUnit = _value.GetValue(DisplayUnit);
            return $"{valueInDisplayUnit.ToString(format)} {DisplayUnit.Symbol}";
        }


    }
}
