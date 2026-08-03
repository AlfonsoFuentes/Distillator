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
        bool HasDisplayUnitOverride { get; }
        string? DefinedByUserId { get; }
        string? DefinedByUserName { get; }
        DateTime? DefinedAtUtc { get; }
        void SetProjectDefaultDisplayUnit(UnitMeasure unit);
        void RestorePersistedState(
            bool isDefined,
            double value,
            UnitMeasure valueUnit,
            VariableDefinedBy dataProcedence,
            UnitMeasure? displayUnit,
            bool hasDisplayUnitOverride,
            bool restoreProjectDefaultDisplayUnit = false,
            string? definedByUserId = null,
            string? definedByUserName = null,
            DateTime? definedAtUtc = null);
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
        public bool HasDisplayUnitOverride { get; private set; }
        public string? DefinedByUserId { get; private set; }
        public string? DefinedByUserName { get; private set; }
        public DateTime? DefinedAtUtc { get; private set; }
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
            if (_procedence != VariableDefinedBy.UserInput && _procedence != VariableDefinedBy.Specification)
            {
                ClearDefinitionAudit();
            }

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
            if ( _procedence == VariableDefinedBy.Undefined)
            {

            }
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
            if ( _procedence == VariableDefinedBy.Undefined)
            {
            }
            if (DataProcedence != _procedence)
            {
                return;
            }

            var oldIsSpecToEquilibrium = ShouldTriggerRecalculation;
            DataProcedence = VariableDefinedBy.Undefined;
            ClearDefinitionAudit();

            if (oldIsSpecToEquilibrium)
            {
                ValueChanged?.Invoke();
            }
            ClearSpecificationChanged?.Invoke();
        }

        public void SetDisplayUnit(UnitMeasure unit)
        {
            DisplayUnit = unit;
            HasDisplayUnitOverride = true;
        }

        public void SetProjectDefaultDisplayUnit(UnitMeasure unit)
        {
            if (HasDisplayUnitOverride) return;
            DisplayUnit = unit;
        }

        public void RestorePersistedState(
            bool isDefined,
            double value,
            UnitMeasure valueUnit,
            VariableDefinedBy dataProcedence,
            UnitMeasure? displayUnit,
            bool hasDisplayUnitOverride,
            bool restoreProjectDefaultDisplayUnit = false,
            string? definedByUserId = null,
            string? definedByUserName = null,
            DateTime? definedAtUtc = null)
        {
            HasDisplayUnitOverride = false;

            if (displayUnit != null && hasDisplayUnitOverride)
            {
                SetDisplayUnit(displayUnit);
            }
            else if (displayUnit != null && restoreProjectDefaultDisplayUnit)
            {
                SetProjectDefaultDisplayUnit(displayUnit);
            }

            if (!isDefined || dataProcedence == VariableDefinedBy.Undefined)
            {
                DataProcedence = VariableDefinedBy.Undefined;
                ClearDefinitionAudit();
                return;
            }

            var persistedValue = (T)Activator.CreateInstance(typeof(T), value, valueUnit)!;
            SetValue(persistedValue, dataProcedence);
            DefinedByUserId = string.IsNullOrWhiteSpace(definedByUserId) ? null : definedByUserId;
            DefinedByUserName = string.IsNullOrWhiteSpace(definedByUserName) ? null : definedByUserName;
            DefinedAtUtc = definedAtUtc;
        }

        public string ToUiString(string format = "F2")
        {
            if (DataProcedence == VariableDefinedBy.Undefined)
                return "<Not defined>";

            double valueInDisplayUnit = NormalizeDisplayValue(_value.GetValue(DisplayUnit));
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
        public void SetValueFromUI(T value, string? userId, string? userName)
        {
            SetValue(value, VariableDefinedBy.UserInput);
            DefinedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
            DefinedByUserName = string.IsNullOrWhiteSpace(userName) ? null : userName;
            DefinedAtUtc = DateTime.UtcNow;
        }

        public void SetDefinitionAudit(string? userId, string? userName, DateTime? definedAtUtc = null)
        {
            DefinedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
            DefinedByUserName = string.IsNullOrWhiteSpace(userName) ? null : userName;
            DefinedAtUtc = definedAtUtc ?? DateTime.UtcNow;
        }

        private void ClearDefinitionAudit()
        {
            DefinedByUserId = null;
            DefinedByUserName = null;
            DefinedAtUtc = null;
        }

        public double GetDisplayValue() => NormalizeDisplayValue(_value.GetValue(DisplayUnit));
        public string GetDisplayUnit() => DisplayUnit.Symbol;
        public UnitMeasure UnitForUI => DisplayUnit;
        public void ChangeUnitForUI(UnitMeasure unit) => SetDisplayUnit(unit);

        private static double NormalizeDisplayValue(double value)
        {
            const double tolerance = 1.0e-7;

            if (Math.Abs(value) <= tolerance)
            {
                return 0.0;
            }

            if (typeof(T) == typeof(Percentage) && Math.Abs(value - 100.0) <= tolerance)
            {
                return 100.0;
            }

            return value;
        }
    }

}
