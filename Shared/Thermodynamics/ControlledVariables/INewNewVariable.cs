using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.ControlledVariables
{   // ═══════════════════════════════════════════════════════════════
    // 🔹 INewNewVariable.cs - VERSIÓN CON NUEVO SOLVER REACTIVO
    // 🔹 Backup recomendado antes de reemplazar
    // ═══════════════════════════════════════════════════════════════

    using System;

    // ───────────────────────────────────────────────────────────────
    // 🔹 INTERFAZ BASE
    // ───────────────────────────────────────────────────────────────
    public interface INewNewVariable
    {
        // ═══════════════════════════════════════════════════════════
        // 🔥 NUEVAS PROPIEDADES PARA EL SOLVER REACTIVO
        // ═══════════════════════════════════════════════════════════
        double? NewSolverValue { get; set; }
        bool IsToDefineByNewSolver { get; set; }
        double GetEffectiveSolverValue();

        // ═══════════════════════════════════════════════════════════
        // 🔹 PROPIEDADES EXISTENTES
        // ═══════════════════════════════════════════════════════════
        string Source { get; }
        double InitValue { get; }
        int Index { get; set; }
        double SolverValue { get; set; }
        bool IsDefinedByUI { get; }

        void ClearFromUI();
        void ClearFromUINoEvents();
        void ClearFromStream();
        void ClearFromEquipmentSolver();
        void ClearFromGeneralSolver();

        bool IsDefinedByStream { get; }
        bool IsCalculated { get; }

        bool IsToDefineByEquipmentSolver { get; set; }
        bool IsToDefineByGeneralSolver { get; set; }
        bool IsDefinedByEquipmentSolver { get; }
        bool IsDefinedByGeneralSolver { get; }

        bool IsNotDefined { get; }
        bool IsDefined { get; }

        Action? ExecuteGeneralSolver { get; set; }
        Action? ExecuteEquipmentSolver { get; set; }
        Action? ExecuteStreamCalculation { get; set; }
        Action? SendToFacadeInside { get; set; }

        void SetValueFromEquipmentSolver(double value);
        void SetValueFromGeneralSolver(double value);
        double GetSolverValue();
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 INTERFAZ GENÉRICA
    // ───────────────────────────────────────────────────────────────
    public interface INewNewVariable<T> : INewNewVariable
    {
        T Value { get; set; }
        void SetValueFromUI(T value);
        void SetValueFromStream(T value, string _name);
        Action<INewNewVariable<T>>? AddToDefinedList { get; set; }
        T GetValue(double _value);
        // GetSolverValue() y GetEffectiveSolverValue() vienen de la base
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 CLASE BASE ABSTRACTA
    // ───────────────────────────────────────────────────────────────
    public abstract class NewNewVariable<T> : INewNewVariable<T>
    {
        // ═══════════════════════════════════════════════════════════
        // 🔥 NUEVO: Campos para el solver reactivo
        // ═══════════════════════════════════════════════════════════
        private double? _newSolverValue;
        private bool _isToDefineByNewSolver;

        // ═══════════════════════════════════════════════════════════
        // 🔥 NUEVO: Propiedad reactiva NewSolverValue
        // ═══════════════════════════════════════════════════════════
        public double? NewSolverValue
        {
            get => _newSolverValue;
            set
            {
                // 🔥 Solo propagar si el valor cambió significativamente (evita bucles infinitos)
                if (value.HasValue && (!_newSolverValue.HasValue || Math.Abs(value.Value - _newSolverValue.Value) > 1e-9))
                {
                    _newSolverValue = value;

                    // 🔥 Disparar propagación inmediata a dependientes
                    SendToFacadeInside?.Invoke();
                    ExecuteStreamCalculation?.Invoke();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 🔥 NUEVO: Flag para marcar variables que este solver puede modificar
        // ═══════════════════════════════════════════════════════════
        public bool IsToDefineByNewSolver
        {
            get => _isToDefineByNewSolver;
            set
            {
                _isToDefineByNewSolver = value;
                if (value && !IsDefined)
                {
                    // Si se marca para resolver y no tiene valor, inicializar con InitValue
                    _newSolverValue = InitValue;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 🔥 NUEVO: Helper para obtener valor efectivo con prioridad clara
        // ═══════════════════════════════════════════════════════════
        public double GetEffectiveSolverValue()
        {
            if (IsDefinedByUI) return GetSolverValue();
            if (IsToDefineByNewSolver && NewSolverValue.HasValue) return NewSolverValue.Value;
            if (IsDefinedByStream) return GetSolverValue();
            if (IsDefinedByEquipmentSolver) return GetSolverValue();
            if (IsDefinedByGeneralSolver) return GetSolverValue();
            return InitValue;
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 PROPIEDADES EXISTENTES
        // ═══════════════════════════════════════════════════════════
        public string Source { get; private set; } = "";
        public double InitValue { get; private set; }
        public int Index { get; set; }
        public T Value { get; set; } = default(T)!;
        public double SolverValue { get; set; }
        public bool IsDefinedByUI { get; private set; }
        public bool IsDefinedByStream { get; private set; }

        public bool IsToDefineByGeneralSolver { get; set; }
        public bool IsToDefineByEquipmentSolver { get; set; }
        public bool IsDefinedByEquipmentSolver { get; private set; }
        public bool IsDefinedByGeneralSolver { get; private set; }

        public bool IsDefined => IsDefinedByUI || IsDefinedByStream || IsDefinedByEquipmentSolver || IsDefinedByGeneralSolver;
        public bool IsNotDefined => !IsDefined;

        public Action<INewNewVariable<T>>? AddToDefinedList { get; set; }
        public Action? ExecuteGeneralSolver { get; set; }
        public Action? ExecuteEquipmentSolver { get; set; }
        public Action? ExecuteStreamCalculation { get; set; }
        public Action? SendToFacadeInside { get; set; }

        private bool IsCalculatingBySolver = false;
        public bool IsCalculated => IsDefinedByStream || IsDefinedByEquipmentSolver || IsDefinedByGeneralSolver;

        public abstract string GetDisplayString();

        // ═══════════════════════════════════════════════════════════
        // 🔹 CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════
        protected NewNewVariable(T _value, double initValue = 0)
        {
            Value = _value;
            InitValue = initValue;
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 MÉTODOS EXISTENTES
        // ═══════════════════════════════════════════════════════════
        public void SetValueFromUINotEvents(T value)
        {
            Value = value;
            IsDefinedByUI = true;
            SolverValue = GetSolverValue();
            Source = "UI";
        }

        public virtual void SetValueFromUI(T value)
        {
            Value = value;
            IsDefinedByUI = true;
            SolverValue = GetSolverValue();
            SendToFacadeInside?.Invoke();
            ExecuteStreamCalculation?.Invoke();
            ExecuteEquipmentSolver?.Invoke();
            ExecuteGeneralSolver?.Invoke();
            Source = "UI";
        }

        public virtual void ClearFromUI()
        {
            IsDefinedByUI = false;
            ExecuteStreamCalculation?.Invoke();
            ExecuteEquipmentSolver?.Invoke();
            ExecuteGeneralSolver?.Invoke();
            Source = "";
        }

        public void SetValueFromStream(T value, string _name)
        {
            Value = value;
            IsDefinedByStream = true;
            SolverValue = GetSolverValue();
            SendToFacadeInside?.Invoke();
            AddToDefinedList?.Invoke(this);

            if (!IsCalculatingBySolver)
                ExecuteEquipmentSolver?.Invoke();

            Source = _name;
        }

        public void ClearFromStream()
        {
            IsDefinedByStream = false;
            Source = "";
        }

        public void SetValueFromEquipmentSolver(double value)
        {
            Value = GetValue(value);
            IsDefinedByEquipmentSolver = true;
            SolverValue = GetSolverValue();
            SendToFacadeInside?.Invoke();

            IsCalculatingBySolver = true;
            ExecuteStreamCalculation?.Invoke();
            IsCalculatingBySolver = false;

            Source = "Solver";
        }

        public void ClearFromEquipmentSolver()
        {
            IsDefinedByEquipmentSolver = false;
            IsDefinedByGeneralSolver = false;
            ExecuteStreamCalculation?.Invoke();
            IsToDefineByEquipmentSolver = false;
            Source = "";
        }

        public void SetValueFromGeneralSolver(double value)
        {
            IsDefinedByGeneralSolver = true;
            Value = GetValue(value);
            SolverValue = GetSolverValue();
            SendToFacadeInside?.Invoke();

            IsCalculatingBySolver = true;
            ExecuteStreamCalculation?.Invoke();
            IsCalculatingBySolver = false;

            Source = "Solver";
        }

        public void ClearFromGeneralSolver()
        {
            IsDefinedByGeneralSolver = false;
            ExecuteStreamCalculation?.Invoke();
            IsToDefineByGeneralSolver = false;
            Source = "";
        }

        public abstract double GetSolverValue();
        public abstract T GetValue(double _value);

        public void ClearFromUINoEvents()
        {
            IsDefinedByUI = false;
            Source = "";
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 IMPLEMENTACIÓN PARA double
    // ───────────────────────────────────────────────────────────────
    public class NewNewVariableDouble : NewNewVariable<double>
    {
        public NewNewVariableDouble(double _value = 0) : base(_value) { }

        public override double GetSolverValue() => Value;
        public override double GetValue(double _value) => _value;

        public override string GetDisplayString()
        {
            if (!IsDefined) return "<Not Defined>";
            return $"{Value:F2}";
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 IMPLEMENTACIÓN PARA Amount<T> (con unidades)
    // ───────────────────────────────────────────────────────────────
    public class NewNewVariableAmount<T> : NewNewVariable<T> where T : Amount
    {
        public UnitMeasure UnitForUI { get; private set; }
        public UnitMeasure UnitForSolver { get; private set; }
        private readonly Func<double, UnitMeasure, T> _factory;

        public new double InitValue { get; private set; } = 0;
        public Action? OnUnitChanged { get; set; }

        public NewNewVariableAmount(
            T _Value,
            UnitMeasure unitUI,
            UnitMeasure unitSolver,
            Func<double, UnitMeasure, T> factory,
            double _InitValue = 0) : base(_Value)
        {
            UnitForUI = unitUI;
            UnitForSolver = unitSolver;
            _factory = factory;
            InitValue = _InitValue;
        }

        public override double GetSolverValue() => Value.GetValue(UnitForSolver);

        public override T GetValue(double _value)
        {
            if (double.IsNaN(_value) || double.IsInfinity(_value))
                return _factory(SolverValue, UnitForSolver);
            return _factory(_value, UnitForSolver);
        }

        public void ChangeUnitForUI(UnitMeasure newUnit)
        {
            if (newUnit == null) throw new ArgumentNullException(nameof(newUnit));
            UnitForUI = newUnit;
            OnUnitChanged?.Invoke();
        }

        public double GetDisplayValue()
        {
            var amount = _factory(SolverValue, UnitForSolver);
            return amount.GetValue(UnitForUI);
        }

        public string GetDisplayUnit() => UnitForUI.Symbol;
        public T GetCurrentAmount() => _factory(SolverValue, UnitForSolver);

        public override string GetDisplayString()
        {
            if (!IsDefined) return "<Not Defined>";
            return $"{GetDisplayValue():F2} {GetDisplayUnit()}";
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 IMPLEMENTACIÓN PARA StreamComposition
    // ───────────────────────────────────────────────────────────────
    public class NewNewVariableComposition : NewNewVariable<StreamComposition>
    {
        public NewNewVariableComposition(StreamComposition _value) : base(_value) { }

        public override double GetSolverValue() => 0;
        public override StreamComposition GetValue(double _value) => Value;

        public override string GetDisplayString()
        {
            if (!IsDefined) return "<Not Defined>";
            return ""; // Personalizar según necesites
        }

        public override void SetValueFromUI(StreamComposition value)
        {
            value?.CalculateMassMolarFractions();
            base.SetValueFromUI(value!);
        }

        public override void ClearFromUI()
        {
            Value?.ClearMassMolarFractions();
            base.ClearFromUI();
        }
    }
    
}



