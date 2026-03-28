using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.DesignPatterns.Thermodynamics
{
    public interface IEquilibriumStrategy
    {
        /// <summary>
        /// Ejecuta el cálculo de equilibrio específico del modo.
        /// Si no converge, no hace nada (fail silently).
        /// </summary>
        void Execute();
    }
    public enum EquilibriumMode
    {
        None = 0,       // No hay combinación completa
        PT = 1,         // Presión + Temperatura + Composición
        P_FV = 2,       // Presión + Fracción de Vapor + Composición
        T_FV = 3        // Temperatura + Fracción de Vapor + Composición
    }
    public class EquilibriumCalculator
    {
        private readonly StreamSimulationFacade _facade;
        private readonly MaterialStream _materialStream;
        private IEquilibriumStrategy? _currentStrategy;
        private EquilibriumMode _currentMode;

        // ─────────────────────────────────────────────────────────
        // 🔹 EVENTO: Notifica cuando el equilibrio está listo para calcular
        // ─────────────────────────────────────────────────────────
        public event Action? EquilibriumReady;

        // ─────────────────────────────────────────────────────────
        // 🔹 PROPIEDAD: Indica si hay una combinación válida para calcular
        // ─────────────────────────────────────────────────────────
        public bool IsEquilibriumReady { get; private set; }

        // ─────────────────────────────────────────────────────────
        // 🔹 CONSTRUCTOR
        // ─────────────────────────────────────────────────────────
        public EquilibriumCalculator(StreamSimulationFacade facade, MaterialStream materialStream)
        {
            _facade = facade;
            _materialStream = materialStream;
            _currentMode = EquilibriumMode.None;
            IsEquilibriumReady = false;
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 MÉTODO: Se llama cuando alguna variable relevante cambió
        // ─────────────────────────────────────────────────────────
        public void OnConstraintsChanged()
        {
            // Leer estado actual directamente del Facade
            var P = _facade.PressureControlled.IsDefined;
            var T = _facade.TemperatureControlled.IsDefined;
            var FV = _facade.VaporFractionControlled.IsDefined;
            var Comp = _facade.StreamCompositionControlled.Value != null;

            // Evaluar combinaciones (Regla de Gibbs)
            var newMode = EvaluateMode(P, T, FV, Comp);

            // Si el modo cambió, actualizar estrategia
            if (newMode != _currentMode)
            {
                _currentMode = newMode;
                _currentStrategy = CreateStrategy(newMode);
                IsEquilibriumReady = _currentStrategy != null;

                // Notificar que el estado cambió
                EquilibriumReady?.Invoke();
            }
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 MÉTODO PRIVADO: Determina el modo según variables definidas
        // ─────────────────────────────────────────────────────────
        private EquilibriumMode EvaluateMode(bool P, bool T, bool FV, bool Comp)
        {
            if (P && T && Comp) return EquilibriumMode.PT;
            if (P && Comp && FV) return EquilibriumMode.P_FV;
            if (T && Comp && FV) return EquilibriumMode.T_FV;
            return EquilibriumMode.None;
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 MÉTODO PRIVADO: Crea la estrategia correspondiente al modo
        // ─────────────────────────────────────────────────────────
        private IEquilibriumStrategy? CreateStrategy(EquilibriumMode mode)
        {
            return mode switch
            {
                EquilibriumMode.PT => new PTStrategy(_materialStream),
                EquilibriumMode.P_FV => new PFVStrategy(_materialStream),
                EquilibriumMode.T_FV => new TFVStrategy(_materialStream),
                _ => null
            };
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 MÉTODO PÚBLICO: Ejecuta el cálculo de equilibrio
        // ─────────────────────────────────────────────────────────
        public void CalculateEquilibrium()
        {
            if (_currentStrategy != null && IsEquilibriumReady)
            {
                _currentStrategy.Execute();
                // Si no converge, la estrategia no hace nada (fail silently)
            }
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 PROPIEDAD: Modo actual de cálculo
        // ─────────────────────────────────────────────────────────
        public EquilibriumMode CurrentMode => _currentMode;
    }
    public class PTStrategy : IEquilibriumStrategy
    {
        private readonly MaterialStream _materialStream;

        public PTStrategy(MaterialStream materialStream)
        {
            _materialStream = materialStream;
        }

        public void Execute()
        {
            // 👇 AQUÍ IRÁ LA LÓGICA REAL DE CÁLCULO (pendiente)
            // Ejemplo conceptual:
            // 1. Leer P y T de _materialStream
            // 2. Calcular fracción de vapor (flash PT)
            // 3. Calcular x_i, y_i
            // 4. Actualizar _materialStream con resultados
            // 5. Si no converge → no hacer nada (fail silently)
        }
    }
    public class PFVStrategy : IEquilibriumStrategy
    {
        private readonly MaterialStream _materialStream;

        public PFVStrategy(MaterialStream materialStream)
        {
            _materialStream = materialStream;
        }

        public void Execute()
        {
            // 👇 AQUÍ IRÁ LA LÓGICA REAL DE CÁLCULO (pendiente)
        }
    }
    public class TFVStrategy : IEquilibriumStrategy
    {
        private readonly MaterialStream _materialStream;

        public TFVStrategy(MaterialStream materialStream)
        {
            _materialStream = materialStream;
        }

        public void Execute()
        {
            // 👇 AQUÍ IRÁ LA LÓGICA REAL DE CÁLCULO (pendiente)
        }
    }
}

