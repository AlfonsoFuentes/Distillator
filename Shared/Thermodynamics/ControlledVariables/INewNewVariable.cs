using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.ControlledVariables
{
    public interface INewNewVariable
    {
        double InitValue { get; }
        int Index { get; set; }
        double SolverValue { get; set; }
        bool IsDefinedByUI { get; }
        void ClearFromUI();
        void ClearFromStream();
        void ClearFromEquipmentSolver();
        void ClearFromGeneralSolver();
        bool IsDefinedByStream { get; }

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
    }
    public interface INewNewVariable<T> : INewNewVariable
    {
        T Value { get; set; }


        void SetValueFromUI(T value);

        void SetValueFromStream(T value, string _name);










        Action<INewNewVariable<T>>? AddToDefinedList { get; set; }

        double GetSolverValue();
        T GetValue(double _value);
    }
    public abstract class NewNewVariable<T> : INewNewVariable<T>
    {
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
        protected NewNewVariable(T _value, double initValue = 0)
        {
            Value = _value;
            InitValue = initValue;
        }
        public void SetValueFromUI(T value)
        {
            Value = value;
            IsDefinedByUI = true;
            SolverValue = GetSolverValue();
            SendToFacadeInside?.Invoke();
            ExecuteStreamCalculation?.Invoke();
            ExecuteEquipmentSolver?.Invoke();
            ExecuteGeneralSolver?.Invoke();

        }
        public void ClearFromUI()
        {
            IsDefinedByUI = false;
            ExecuteStreamCalculation?.Invoke();
            ExecuteEquipmentSolver?.Invoke();
            ExecuteGeneralSolver?.Invoke();
        }
        public void SetValueFromStream(T value, string _name)
        {
            Value = value;
            IsDefinedByStream = true;
            SolverValue = GetSolverValue();
            SendToFacadeInside?.Invoke();

            AddToDefinedList?.Invoke(this);
            ExecuteEquipmentSolver?.Invoke();
            //ExecuteGeneralSolver?.Invoke();
        }
        public void ClearFromStream()
        {
            IsDefinedByStream = false;
            //ExecuteStreamCalculation?.Invoke();
            //ExecuteEquipmentSolver?.Invoke();
            //ExecuteGeneralSolver?.Invoke();

        }

        public void SetValueFromEquipmentSolver(double value)
        {
            Value = GetValue(value);
            IsDefinedByEquipmentSolver = true;
            SolverValue = GetSolverValue();
            SendToFacadeInside?.Invoke();
            ExecuteStreamCalculation?.Invoke();



        }

        public void ClearFromEquipmentSolver()
        {
            IsDefinedByEquipmentSolver = false;
            ExecuteStreamCalculation?.Invoke();
            IsToDefineByEquipmentSolver = false;
        }

        public void SetValueFromGeneralSolver(double value)
        {
            IsDefinedByGeneralSolver = true;
            Value = GetValue(value);
            SolverValue = GetSolverValue();
            SendToFacadeInside?.Invoke();
            ExecuteStreamCalculation?.Invoke();
        }

        public void ClearFromGeneralSolver()
        {
            IsDefinedByGeneralSolver = false;
            ExecuteStreamCalculation?.Invoke();
            IsToDefineByGeneralSolver = false;
        }

        public abstract double GetSolverValue();
        public abstract T GetValue(double _value);
    }
    public class NewNewVariableDouble : NewNewVariable<double>
    {
        public NewNewVariableDouble(double _value = 0) : base(_value)
        {
        }
        public override double GetSolverValue()
        {
            return Value;
        }
        public override double GetValue(double _value)
        {
            return _value;
        }
    }
    public class NewNewVariableAmount<T> : NewNewVariable<T> where T : Amount
    {
        public UnitMeasure UnitForUI { get; private set; }
        public UnitMeasure UnitForSolver { get; private set; }
        private readonly Func<double, UnitMeasure, T> _factory;

        public new double InitValue { get; private set; } = 0;
        // Opcional: notifica a la UI cuando cambia la unidad de visualización
        public Action? OnUnitChanged { get; set; }

        public NewNewVariableAmount(T _Value, UnitMeasure unitUI, UnitMeasure unitSolver, Func<double, UnitMeasure, T> factory, double _InitValue = 0) : base(_Value)
        {
            UnitForUI = unitUI;
            UnitForSolver = unitSolver;
            _factory = factory;

            // 🔒 Sincroniza estado inicial para evitar lecturas inconsistentes
            InitValue = _InitValue;
        }

        public override double GetSolverValue()
        {
            return Value.GetValue(UnitForSolver);
        }

        public override T GetValue(double _value)
        {
            // 🛡️ Blindaje numérico: evita que NaN/Infinity rompan el objeto Amount
            if (double.IsNaN(_value) || double.IsInfinity(_value))
                return _factory(SolverValue, UnitForSolver); // Fallback al último valor válido

            return _factory(_value, UnitForSolver);
        }

        public void ChangeUnitForUI(UnitMeasure newUnit)
        {
            if (newUnit == null) throw new ArgumentNullException(nameof(newUnit));

            UnitForUI = newUnit;
            OnUnitChanged?.Invoke(); // 🔄 Notifica a la UI para refrescar bindings
        }

        /// ✅ Para UI: valor numérico convertido a la unidad del usuario
        public double GetDisplayValue()
        {
            var amount = _factory(SolverValue, UnitForSolver);
            return amount.GetValue(UnitForUI);
        }

        /// ✅ Para UI: símbolo de la unidad actual (ej: "bar", "kgmol/hr")
        public string GetDisplayUnit()
        {
            return UnitForUI.Symbol;
        }

        /// 🔹 Para lógica interna: devuelve el Amount en unidades del solver
        public T GetCurrentAmount()
        {
            return _factory(SolverValue, UnitForSolver);
        }
    }
    public class NewNewVariableComposition : NewNewVariable<StreamComposition>
    {
        public NewNewVariableComposition(StreamComposition _value) : base(_value)
        {
        }
        public override double GetSolverValue()
        {
            return 0;
        }
        public override StreamComposition GetValue(double _value)
        {
            return Value;
        }
    }
}



