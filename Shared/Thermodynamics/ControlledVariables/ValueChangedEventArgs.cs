namespace Shared.Thermodynamics.ControlledVariables
{
    // ─────────────────────────────────────────────────────────
    // 🔹 EVENT ARGS PARA EL EVENTO DE CAMBIO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Argumentos del evento ValueChanged para ControlledVariable<T>.
    /// </summary>
    /// <typeparam name="T">Tipo de Amount</typeparam>
    public class ValueChangedEventArgs<T> : EventArgs
    {
        public T? NewValue { get; }
        public MethodSource Source { get; }
        public string SourceId { get; }

        public ValueChangedEventArgs(T? newValue, MethodSource source, string sourceId)
        {
            NewValue = newValue;
            Source = source;
            SourceId = sourceId ?? string.Empty;
        }
    }
}



