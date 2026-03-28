using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics
{
    public enum MethodSource
    {
        None = 0,         // No está definido aún
        UserInterface = 1, // Definido manualmente por el ingeniero en la UI
        Other = 2      // Definido automáticamente por una operación unitaria (Ej: Separador)
    }

    public class ControlledVariable<T>
    {
        // ─────────────────────────────────────────────────────────
        // 🔹 PROPIEDADES DE ESTADO
        // ─────────────────────────────────────────────────────────
        public T? Value { get; set; } = default(T)!;
        public MethodSource Source { get; set; } = MethodSource.None;
        public string SourceId { get; set; } = string.Empty;

        // ─────────────────────────────────────────────────────────
        // 🔹 PROPIEDAD DERIVADA
        // ─────────────────────────────────────────────────────────
        public bool IsDefined => Source != MethodSource.None;

        // ─────────────────────────────────────────────────────────
        // 🔹 EVENTOS (Patrón Observer)
        // ─────────────────────────────────────────────────────────
        public event Action<ValueChangedEventArgs<T>>? ValueChanged;
        public event Action? ConstraintsChanged;

        // ─────────────────────────────────────────────────────────
        // 🔹 MÉTODOS PÚBLICOS (SIN VALIDACIONES DE PERMISO)
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Establece un nuevo valor y actualiza el origen.
        /// El caller es responsable de validar permisos antes de llamar.
        /// </summary>
        public void SetValue(T? newValue, MethodSource source, string sourceId = "UI")
        {
            var oldValue = Value;
            Value = newValue;
            Source = source;
            SourceId = sourceId;

            ValueChanged?.Invoke(new ValueChangedEventArgs<T>(oldValue, newValue, source, sourceId));
            ConstraintsChanged?.Invoke();
        }

        /// <summary>
        /// Limpia el valor (Source = None, Value = default).
        /// El caller es responsable de validar permisos antes de llamar.
        /// </summary>
        public void ClearValue()
        {
            var oldValue = Value;
            Value = default(T)!;
            Source = MethodSource.None;
            SourceId = string.Empty;

            ValueChanged?.Invoke(new ValueChangedEventArgs<T>(oldValue, default, MethodSource.None, string.Empty));
            ConstraintsChanged?.Invoke();
        }
    }

    // 👇 Clase auxiliar para pasar contexto en el evento
    public class ValueChangedEventArgs<T>
    {
        public T? OldValue { get; }
        public T? NewValue { get; }
        public MethodSource Source { get; }
        public string SourceId { get; }

        public ValueChangedEventArgs(T? oldValue, T? newValue, MethodSource source, string sourceId)
        {
            OldValue = oldValue;
            NewValue = newValue;
            Source = source;
            SourceId = sourceId;
        }
    }

   
}
