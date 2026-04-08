using System;
using UnitSystem;

namespace Shared.Thermodynamics.ControlledVariables
{
    /// <summary>
    /// Variable controlada especializada para tipos Amount (Temperature, Pressure, etc.).
    /// Agrega unidad preferida para presentación en UI y conversión automática.
    /// </summary>
    public class ControlledAmountVariable<T> : ControlledVariable<T> where T : Amount
    {
        public ControlledAmountVariable(UnitMeasure preferredUnit, T? initialValue = default, MethodSource source = MethodSource.None, string sourceId = "")
            : base(initialValue, source, sourceId)
        {
            if (preferredUnit == null)
                throw new ArgumentNullException(nameof(preferredUnit), "La unidad preferida es obligatoria para ControlledAmountVariable<T>");

            _preferredUnit = preferredUnit;
        }

        private UnitMeasure? _preferredUnit;

        public UnitMeasure? GetPreferredUnit() => _preferredUnit;

        public void SetPreferredUnit(UnitMeasure newUnit)
        {
            if (newUnit == null) return;

            _preferredUnit = newUnit;

            if (Value != null)
            {
                try
                {
                    // 👇 Obtener valor convertido y crear NUEVA instancia del tipo T
                    var convertedValue = Value.GetValue(newUnit);
                    Value.SetValue(convertedValue, newUnit);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Obtiene el valor NUMÉRICO en la unidad preferida para UI.
        /// Retorna double para evitar problemas de casting.
        /// </summary>
        public double? GetDisplayValueAsDouble()
        {
            if (Value == null) return null;
            if (_preferredUnit == null) return Value.Value;

            try
            {
                // 👇 CLAVE: Usar GetValue() que ya hace la conversión internamente
                return Value.GetValue(_preferredUnit);
            }
            catch (Exception)
            {
                return Value.Value;
            }
        }

        /// <summary>
        /// Obtiene la unidad preferida actual para formateo en UI.
        /// </summary>
        public UnitMeasure? GetDisplayUnit() => _preferredUnit ?? Value?.Unit;

        /// <summary>
        /// Obtiene el valor numérico en una unidad específica (para cálculos internos).
        /// </summary>
        public double GetValueInUnit(UnitMeasure targetUnit)
        {
            if (Value == null || targetUnit == null) return 0;

            try
            {
                return Value.GetValue(targetUnit);
            }
            catch (UnitConversionException)
            {
                return Value.Value;
            }
        }

       
    }
}