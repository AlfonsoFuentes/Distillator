namespace Shared.Thermodynamics.ControlledVariables
{
    /// <summary>
    /// Variable controlada que mantiene una unidad preferida para presentación en UI.
    /// Los cálculos internos pueden usar cualquier unidad; la UI siempre muestra en la unidad que eligió el usuario.
    /// </summary>
    /// <typeparam name="T">Tipo derivado de Amount (Temperature, Pressure, Viscosity, etc.)</typeparam>
    public class ControlledVariable<T> : IControlledVariable
    {
        // ─────────────────────────────────────────────────────────
        // 🔹 CONSTRUCTOR
        // ─────────────────────────────────────────────────────────

        public ControlledVariable(T? initialValue = default, MethodSource source = MethodSource.None, string sourceId = "")
        {
            Value = initialValue;
            Source = source;
            SourceId = sourceId ?? string.Empty;
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 PROPIEDADES
        // ─────────────────────────────────────────────────────────

        public T? Value { get; protected set; }
        public MethodSource Source { get;  set; } = MethodSource.None;
        public string SourceId { get;  set; } = string.Empty;
        public bool IsDefined => Value != null && Source != MethodSource.None;

        // ─────────────────────────────────────────────────────────
        // 🔹 MÉTODOS
        // ─────────────────────────────────────────────────────────

        public virtual void SetValue(T newValue, MethodSource source, string sourceId = "")
        {
            var wasDefined = IsDefined;
            var oldSource = Source;

            Value = newValue;
            Source = source;
            SourceId = sourceId ?? string.Empty;

            ValueChanged?.Invoke(new ValueChangedEventArgs<T>(Value, Source, SourceId));

            ConstraintsChanged?.Invoke();
        }

        public virtual void ClearValue()
        {
            var oldValue = Value;
            var wasDefined = IsDefined;

            Value = default;
            Source = MethodSource.None;
            SourceId = string.Empty;

            ValueChanged?.Invoke(new ValueChangedEventArgs<T>(oldValue, MethodSource.None, string.Empty));

            if (wasDefined)
            {
                ConstraintsChanged?.Invoke();
            }
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 EVENTOS
        // ─────────────────────────────────────────────────────────

        public event Action<ValueChangedEventArgs<T>>? ValueChanged;
        public event Action? ConstraintsChanged;

        // ✅ DEJAR ASÍ:
        public void SetValueCalculated(T? newValue, string sourceId = "System")
        {
            Value = newValue;
            Source = MethodSource.Other;
            SourceId = sourceId;
            // Sin eventos → perfecto ✅
        }
    }
}



