using Shared.SolverConsecutive;
using System.Diagnostics;
using UnitSystem;

namespace Shared.SolverQwen.Variables
{
    public interface IProcessVariableOwner
    {
        HashSet<INewVariable> Variables { get; }
        void AddVariable(INewVariable variable);
        void RemoveVariables(VariableDefinedBy _DataProcedence);
    }

    public interface IProcessVariable
    {

        bool IsSpecToSolver { get; }
        bool IsSpecToCalculate { get; }
        VariableDataProcedence DataProcedence { get; }
        bool IsDefined { get; }
        bool IsCalculated { get; }

        void SetDisplayUnit(UnitMeasure unit);
        void Clear(VariableDataProcedence _procedence);

        event Action? ValueChanged;
        event Action<IProcessVariable>? AddVariableToList;

        //double GetSolverValue();
        //void SetValueFromSolver(double solverValue, VariableDataProcedence _procedence);
        void ResetProcedence();
        void ClearForSolver(VariableDataProcedence _procedence);
        public void SetValueFromSolver(double solverValue, VariableDataProcedence _procedence);
        double GetSolverValue();
        bool IsCleareable { get; }
        string ToUiString(string format = "F2");
        void SetName(string name);
        string Name { get; }
        double NormalizeValue { get; }
    }

    /// <summary>
    /// Contrato genérico type-safe. Hereda de IProcessVariable.
    /// </summary>
    public interface IProcessVariable<T> : IProcessVariable where T : Amount
    {
        UnitMeasure InternalUnit { get; }
        UnitMeasure DisplayUnit { get; }
        /// </summary>
        T Value { get; }

        /// <summary>
        bool HasChanged { get; }
        /// </summary>
        void SetValue(T value, VariableDataProcedence _procedence);







    }



    public class ProcessVariable<T> : IProcessVariable<T> where T : Amount
    {
        private T _value;

        public double NormalizeValue { get; } = 0;

        public string Name { get; private set; } = string.Empty;
        public void SetName(string name)
        {
            Name =$"{name}";
        }
        public bool IsCleareable { get; } = false;
        public bool HasChanged { get; private set; } = false;
        public bool IsDefined => DataProcedence != VariableDataProcedence.Undefined;

        public bool IsCalculated => DataProcedence != VariableDataProcedence.Undefined && DataProcedence != VariableDataProcedence.UserInput;
        public bool IsSpecToSolver => DataProcedence == VariableDataProcedence.UserInput || DataProcedence == VariableDataProcedence.StreamCalculated;
        public bool IsSpecToCalculate => DataProcedence ==
            VariableDataProcedence.UserInput ||
            DataProcedence == VariableDataProcedence.Phase1_LocalPropagation ||
            DataProcedence == VariableDataProcedence.Phase2_EasyEquipmentNet||DataProcedence == VariableDataProcedence.Phase3_ThermoAdjustment;
        public VariableDataProcedence DataProcedence { get; protected set; }
        public UnitMeasure InternalUnit { get; }
        public UnitMeasure DisplayUnit { get; private set; }
        public event Action<IProcessVariable>? AddVariableToList;


        public void OnAddVariableToList(IProcessVariable variable)
        {
            AddVariableToList?.Invoke(variable);
        }


        public T Value => _value;


        public event Action? ValueChanged = null!;

        public void OnValueChanged()
        {
            ValueChanged?.Invoke();
        }

        public ProcessVariable(T initialValue, UnitMeasure displayUnit, double _normalizeValue, bool _IsCleareable = false)
        {
            if (initialValue == null) throw new ArgumentNullException(nameof(initialValue));
            if (displayUnit == null) throw new ArgumentNullException(nameof(displayUnit));

            InternalUnit = initialValue.Unit;
            DisplayUnit = displayUnit;
            _value = initialValue;

            NormalizeValue = Math.Abs(_normalizeValue) < 1e-9 ? 1.0 : _normalizeValue;
            this.IsCleareable = _IsCleareable;
        }

        public void SetValue(T value, VariableDataProcedence _procedence)
        {

            HasChanged = true;
            _value = value;
            DataProcedence = _procedence;

            OnAddVariableToList(this);




            if (IsSpecToCalculate)
            {
                ValueChanged?.Invoke();
            }
            HasChanged = false;
        }


        public double GetSolverValue()
        {
            
            var valueInInternalUnit = Value.GetValue(InternalUnit);
            var solverValue = valueInInternalUnit / NormalizeValue;

            return solverValue;
        }
        public void SetValueFromSolver(double solverValue, VariableDataProcedence _procedence)
        {
            HasChanged = true;

            var newValue = solverValue * NormalizeValue;

            _value.SetValue(newValue, InternalUnit);
            DataProcedence = DataProcedence == VariableDataProcedence.Undefined ? _procedence : DataProcedence;
            OnAddVariableToList(this);



            if (IsSpecToCalculate)
            {
                OnValueChanged();
            }
            HasChanged = false;
        }
        public void ClearForSolver(VariableDataProcedence _procedence)
        {
            if (DataProcedence == _procedence)
                DataProcedence = VariableDataProcedence.Undefined;
        }
        public void Clear(VariableDataProcedence _procedence)
        {

            if (DataProcedence != _procedence) return;


            var oldIsSpecToEquilibrium = IsSpecToCalculate;
            DataProcedence = VariableDataProcedence.Undefined;

            if (oldIsSpecToEquilibrium)
            {
                ValueChanged?.Invoke();
            }
        }
        public void ResetProcedence()
        {
            // Blindaje doble: Ni la termodinámica ni el usuario se tocan.
            if (DataProcedence == VariableDataProcedence.StreamCalculated ||
                DataProcedence == VariableDataProcedence.UserInput)
                return;

            DataProcedence = VariableDataProcedence.Undefined;
        }

        public void SetDisplayUnit(UnitMeasure unit) => DisplayUnit = unit;
        // En ProcessVariable<T>
        public string ToUiString(string format = "F2")
        {
            // ✅ Si no está definida, retornar placeholder visual
            if (DataProcedence == VariableDataProcedence.Undefined)
                return "<Not defined>";

            // ✅ Obtener valor en unidad de visualización y formatear
            double valueInDisplayUnit = _value.GetValue(DisplayUnit);
            return $"{valueInDisplayUnit.ToString(format)} {DisplayUnit.Symbol}";
        }

        
    }

}