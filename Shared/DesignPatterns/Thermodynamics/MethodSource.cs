using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics
{
    public interface IControlledVariable
    {
        MethodSource Source { get; set; }
        string SourceId { get; set; }
    }
    public enum MethodSource
    {
        None = 0,         // No está definido aún
        UserInterface = 1, // Definido manualmente por el ingeniero en la UI
        Other = 2      // Definido automáticamente por una operación unitaria (Ej: Separador)
    }

    public class ControlledVariable<T> : IControlledVariable
    {

        public ControlledVariable()
        {
            IsDefinedByUI = true;
        }
        public ControlledVariable(bool definedByUI)
        {
            IsDefinedByUI = definedByUI;
        }
        public bool IsDefinedByUI { get; init; }
        public T? Value { get; set; } = default(T)!;
        public MethodSource Source { get; set; } = MethodSource.None;
        public string SourceId { get; set; } = string.Empty;

        // ✅ Flag anti-reentrancia
        private bool _isNotifying;
        public bool IsDefined => Source != MethodSource.None;
        // Events
        public event Action<ValueChangedEventArgs<T>>? ValueChanged;
        public event Action? ConstraintsChanged;

        // ✅ SetValue con protección
        public void SetValue(T? newValue, MethodSource source, string sourceId = "UI")
        {
            if (_isNotifying) return;
            try
            {
                _isNotifying = true;
                var oldValue = Value;
                Value = newValue;
                Source = source;
                SourceId = sourceId;
                if (ValueChanged != null)
                    ValueChanged.Invoke(new ValueChangedEventArgs<T>(oldValue, newValue, source, sourceId));
                ConstraintsChanged?.Invoke();
            }
            finally
            {
                _isNotifying = false;
            }
        }

        // ... ClearValue() y otros métodos (sin cambios)

        /// <summary>
        /// Limpia el valor (Source = None, Value = default).
        /// El caller es responsable de validar permisos antes de llamar.
        /// </summary>
        public void ClearValue()
        {
            var oldValue = Value;
            //Value = default(T)!;
            Source = MethodSource.None;
            SourceId = string.Empty;

            ValueChanged?.Invoke(new ValueChangedEventArgs<T>(oldValue, default, MethodSource.None, string.Empty));
            ConstraintsChanged?.Invoke();
        }
        public void SetValueCalculated(T? newValue, string sourceId = "System")
        {
            // ✅ Actualizar estado interno
            Value = newValue;
            Source = MethodSource.Other;  // ← Marca como "calculado por sistema"
            SourceId = sourceId;

            // ✅ NO disparar eventos → evita re-evaluación innecesaria
            // La UI puede reaccionar vía binding, pero no se re-trigger el cálculo
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
