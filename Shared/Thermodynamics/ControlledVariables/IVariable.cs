using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.Thermodynamics.ControlledVariables
{

    public interface IVariable
    {
        // ═══════════════════════════════════════════════════════════
        // 🔥 SOLVER REACTIVO (Newton-Raphson)
        // ═══════════════════════════════════════════════════════════
        double? NewSolverValue { get; set; }
        bool IsToDefineByNewSolver { get; set; }
        double SolverValue { get; set; }  // ← 🔥 AGREGAR ESTO AQUÍ
        double GetEffectiveSolverValue();
        void ClearFromGeneralSolver();

        // ═══════════════════════════════════════════════════════════
        // 🔹 CALCULADORES LOCALES (Equilibrio / Flujos)
        // ═══════════════════════════════════════════════════════════
        bool IsDefinedByStream { get; }
        void ClearFromStream();
        Action<IVariable>? AddToDefinedList { get; set; }

        // ═══════════════════════════════════════════════════════════
        // 🔹 INTERACCIÓN UI & ESTADO
        // ═══════════════════════════════════════════════════════════
        bool IsDefinedByUI { get; }
        void SetValueFromUI(object value);
        void ClearFromUI();
        string Source { get; }
        double InitValue { get; }
        int Index { get; set; }
        bool IsDefined { get; }

        // ═══════════════════════════════════════════════════════════
        // 🔹 PROPAGACIÓN & TRIGGERS
        // ═══════════════════════════════════════════════════════════
        Action? ExecuteStreamCalculation { get; set; }
        Action? SendToFacadeInside { get; set; }
        Action? ExecuteGeneralSolver { get; set; }

        double GetSolverValue();
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 INTERFAZ GENÉRICA: IVariable<T>
    // ───────────────────────────────────────────────────────────────
    public interface IVariable<T> : IVariable
    {
        T Value { get; set; }
    
        void SetValueFromStream(T value, string sourceName);
        T GetValue(double solverValue);
        string GetDisplayString();
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 CLASE BASE ABSTRACTA: Variable<T>
    // ───────────────────────────────────────────────────────────────
    public abstract class Variable<T> : IVariable<T>
    {
        private double? _newSolverValue;
        private bool _isToDefineByNewSolver;
        private bool _isDefinedByUI;
        private bool _isDefinedByStream;
        private bool _isCalculatingBySolver = false;

        public double? NewSolverValue
        {
            get => _newSolverValue;
            set
            {
                if (value.HasValue && (!_newSolverValue.HasValue || Math.Abs(value.Value - _newSolverValue.Value) > 1e-9))
                {
                    _newSolverValue = value;
                    SendToFacadeInside?.Invoke();
                    ExecuteStreamCalculation?.Invoke();
                }
            }
        }

        public bool IsToDefineByNewSolver
        {
            get => _isToDefineByNewSolver;
            set
            {
                _isToDefineByNewSolver = value;
                if (value && !IsDefined && !_newSolverValue.HasValue)
                    _newSolverValue = InitValue;
            }
        }

        public double GetEffectiveSolverValue()
        {
            if (IsDefinedByUI) return GetSolverValue();
            if (IsToDefineByNewSolver && NewSolverValue.HasValue) return NewSolverValue.Value;
            if (IsDefinedByStream) return GetSolverValue();
            return InitValue;
        }

        public string Source { get; private set; } = "";
        public double InitValue { get; protected set; }
        public int Index { get; set; }
        public T Value { get; set; } = default!;
        public double SolverValue { get; set; }

        public bool IsDefinedByUI => _isDefinedByUI;
        public bool IsDefinedByStream => _isDefinedByStream;
        public bool IsDefined => IsDefinedByUI || IsDefinedByStream || NewSolverValue.HasValue;

        public Action? ExecuteStreamCalculation { get; set; }
        public Action? SendToFacadeInside { get; set; }
        public Action? ExecuteEquipmentSolver { get; set; }
        public Action? ExecuteGeneralSolver { get; set; }
        public Action<IVariable>? AddToDefinedList { get; set; }

        public abstract double GetSolverValue();
        public abstract T GetValue(double solverValue);
        public abstract string GetDisplayString();

        // 🔹 Entrada desde UI
        public virtual void SetValueFromUI(object value)
        {
            if (value is T typedValue)
            {
                Value = typedValue;
                _isDefinedByUI = true;
                _newSolverValue = null; // El solver NO sobreescribe UI
                SolverValue = GetSolverValue();
                Source = "UI";

                SendToFacadeInside?.Invoke();
                ExecuteStreamCalculation?.Invoke();
                ExecuteGeneralSolver?.Invoke();
            }
        }

        // 🔹 Entrada desde Calculador Local (Equilibrio / Flujos)
        public void SetValueFromStream(T value, string sourceName)
        {
            Value = value;
            _isDefinedByStream = true;
            SolverValue = GetSolverValue();
            Source = sourceName;

            SendToFacadeInside?.Invoke();
            AddToDefinedList?.Invoke(this); // 🔥 Hook para limpieza posterior (RemoveEquilibriumCalculate)

            if (!_isCalculatingBySolver)
                ExecuteEquipmentSolver?.Invoke();
        }

        // 🔹 Limpiezas
        public virtual void ClearFromUI()
        {
            _isDefinedByUI = false;
            Source = "";
            ExecuteStreamCalculation?.Invoke();
            ExecuteGeneralSolver?.Invoke();
        }

        public void ClearFromStream()
        {
            _isDefinedByStream = false;
            Source = "";
            // Limpieza silenciosa para evitar recursión en cascada
        }

        public virtual void ClearFromGeneralSolver()
        {
            _isToDefineByNewSolver = false;
            _newSolverValue = null;
            ExecuteStreamCalculation?.Invoke();
            Source = "";
        }

        protected Variable(T initialValue, double initValue = 0)
        {
            Value = initialValue;
            InitValue = initValue;
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 IMPLEMENTACIÓN: VariableDouble
    // ───────────────────────────────────────────────────────────────
    public class VariableDouble : Variable<double>
    {
        public VariableDouble(double initialValue = 0) : base(initialValue, initialValue) { }

        public override double GetSolverValue() => Value;
        public override double GetValue(double solverValue) => solverValue;

        public override string GetDisplayString()
        {
            if (!IsDefined) return "<Not Defined>";
            return $"{GetEffectiveSolverValue():F2}";
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 IMPLEMENTACIÓN: VariableAmount<T> (con unidades)
    // ───────────────────────────────────────────────────────────────
    public class VariableAmount<T> : Variable<T> where T : Amount
    {
        public UnitMeasure UnitForUI { get; }
        public UnitMeasure UnitForSolver { get; }
        private readonly Func<double, UnitMeasure, T> _factory;

        public VariableAmount(
            T initialValue,
            UnitMeasure unitForUI,
            UnitMeasure unitForSolver,
            Func<double, UnitMeasure, T> factory,
            double initValue = 0) : base(initialValue, initValue)
        {
            UnitForUI = unitForUI;
            UnitForSolver = unitForSolver;
            _factory = factory;
        }

        public override double GetSolverValue() => Value.GetValue(UnitForSolver);

        public override T GetValue(double solverValue)
        {
            if (double.IsNaN(solverValue) || double.IsInfinity(solverValue))
                return _factory(SolverValue, UnitForSolver);
            return _factory(solverValue, UnitForSolver);
        }

        public override string GetDisplayString()
        {
            if (!IsDefined) return "<Not Defined>";
            var displayValue = GetEffectiveSolverValue();
            var amount = _factory(displayValue, UnitForSolver);
            return $"{amount.GetValue(UnitForUI):F2} {UnitForUI.Symbol}";
        }

        public double GetDisplayValue()
        {
            var amount = _factory(GetEffectiveSolverValue(), UnitForSolver);
            return amount.GetValue(UnitForUI);
        }

        public string GetDisplayUnit() => UnitForUI.Symbol;
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 IMPLEMENTACIÓN: VariableComposition
    // ───────────────────────────────────────────────────────────────
    public class VariableComposition : Variable<StreamComposition>
    {
        public VariableComposition(StreamComposition initialValue) : base(initialValue, 0) { }

        public override double GetSolverValue() => 0; // No escalar
        public override StreamComposition GetValue(double solverValue) => Value;

        public override string GetDisplayString()
        {
            if (!IsDefined) return "<Not Defined>";
            return  "<Empty>";
        }

        public override void SetValueFromUI(object value)
        {
            if (value is StreamComposition comp && comp != null)
            {
                comp.CalculateMassMolarFractions();
                base.SetValueFromUI(comp);
            }
        }

        public override void ClearFromUI()
        {
            Value?.ClearMassMolarFractions();
            base.ClearFromUI();
        }

        public override void ClearFromGeneralSolver()
        {
            Value?.ClearMassMolarFractions();
            base.ClearFromGeneralSolver();
        }
    }
}
