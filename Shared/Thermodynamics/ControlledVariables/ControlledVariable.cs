using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.ControlledVariables
{
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
        public MethodSource Source { get; set; } = MethodSource.None;
        public string SourceId { get; set; } = string.Empty;
        public bool IsDefined => Source != MethodSource.None;


        public Action<ValueChangedEventArgs<T>>? StateChanged;

        /// <summary>
        /// Se dispara EXCLUSIVAMENTE para ordenar al motor local (ej. EquilibriumCalculator) que re-evalúe.
        /// </summary>
        public Action? LocalCalculationRequested;

        public Action<ControlledVariable<T>>? AddCalculatedVariable;
        public Action? OnExecuteSolver { get; set; }

        // ─────────────────────────────────────────────────────────
        // 🔹 MÉTODOS
        // ─────────────────────────────────────────────────────────

        public virtual void SetValue(T newValue, MethodSource source, string sourceId = "")
        {
            var wasDefined = IsDefined;

            Value = newValue;
            Source = source;
            SourceId = sourceId ?? string.Empty;

            // 1. Avisamos al exterior (UI / Facade) del cambio
            StateChanged?.Invoke(new ValueChangedEventArgs<T>(Value, Source, SourceId));

            // 2. Exigimos que la corriente recalcule su termodinámica local
            LocalCalculationRequested?.Invoke();
            if (source != MethodSource.Other)
            {
                if (OnExecuteSolver != null)
                {
                    OnExecuteSolver.Invoke();
                }
            }
        }

        public virtual void ClearValue()
        {
            var oldValue = Value;
            var wasDefined = IsDefined;


            Source = MethodSource.None; // El estado interno queda limpio
            SourceId = string.Empty;

            // 👇 AQUÍ ESTÁ LA MAGIA: Avisamos que el valor cambió (a nulo) y QUIÉN ordenó limpiarlo
            SetValue(Value!, Source, SourceId);
        }

        // ✅ DEJAMOS ASÍ: Silenciado para no causar recálculos locales infinitos.
        public void SetValueCalculated(T? newValue, string sourceId = "System")
        {
            Value = newValue;
            Source = MethodSource.Other;
            SourceId = sourceId;

            // Opcional: Solo repintamos UI, NO disparamos LocalCalculationRequested
            StateChanged?.Invoke(new ValueChangedEventArgs<T>(Value, Source, SourceId));

            AddCalculatedVariable?.Invoke(this);
        }
        // Lo agregas en IControlledVariable y en ControlledVariable<T>
        public void RevertCalculatedValue()
        {
            // Si lo puso el humano, la máquina no lo toca
            if (Source == MethodSource.UserInterface) return;

            var wasDefined = IsDefined;

            // No borramos el Value por seguridad de la UI de Blazor, como bien notaste antes.
            Source = MethodSource.None;
            SourceId = string.Empty;


        }
    }
    
}



