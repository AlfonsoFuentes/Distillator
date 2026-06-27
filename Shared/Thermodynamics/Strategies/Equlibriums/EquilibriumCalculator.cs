using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.ControlledVariables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using System.Diagnostics;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{

  
    public class EquilibriumCalculator : IProcessVariableOwner
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream MaterialStream => _facade.MaterialStream;
        private IEquilibriumStrategy? _currentStrategy;
        private EquilibriumMode _currentMode;

        public event Action? EquilibriumReady;
        public event Action? FlowsReady;
        public EquilibriumMode CurrentMode => _currentMode;

        public EquilibriumCalculator(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
            _currentMode = EquilibriumMode.None;
        }

        public void Execute()
        {
            // Resetear estado previo
            _facade.IsEquilibriumSolved = false;
            MaterialStream.CurrentState = ThermodynamicState.Undefined;
            RemoveVariables(VariableDefinedBy.StreamCalculated);

#if DEBUG
            Console.WriteLine($"\n  [EquilCalc] 🔄 INICIANDO EVALUACIÓN DE EQUILIBRIO para '{_facade.Name}'");
#endif

            // 1. CREAR ESTRATEGIA (La validación termodinámica/Gibbs es implícita aquí)
            _currentStrategy = CreateStrategy();

            if (_currentStrategy == null)
            {
#if DEBUG
                Console.WriteLine($"  [EquilCalc] 🛑 Abortado: No se pudo determinar una estrategia de cálculo. (Faltan grados de libertad o composición).");
#endif
                _currentMode = EquilibriumMode.None;
                FlowsReady?.Invoke(); // Permitir que flujos se evalúen incluso sin equilibrio
                return;
            }

#if DEBUG
            Console.WriteLine($"  [EquilCalc] ⚡ Ejecutando Estrategia: {_currentStrategy.GetType().Name} (Modo: {_currentMode})");
#endif

            // 2. EJECUTAR ESTRATEGIA
            try
            {
                _currentStrategy.Execute();
            }
            catch (Exception ex)
            {
                string message = ex.ToString();
#if DEBUG
                Console.WriteLine($"  [EquilCalc] ❌ ERROR CRÍTICO durante el cálculo termodinámico: {ex.Message}");
#endif
                MaterialStream.CurrentState = ThermodynamicState.Undefined;
                FlowsReady?.Invoke();
                return;
            }

            // 3. VERIFICAR ÉXITO Y NOTIFICAR
            if (MaterialStream.CurrentState != ThermodynamicState.Undefined)
            {
#if DEBUG
                Console.WriteLine($"  [EquilCalc] ✅ Equilibrio Termodinámico Resuelto. (Estado: {MaterialStream.CurrentState})");
#endif
                _facade.IsEquilibriumSolved = true;
                EquilibriumReady?.Invoke(); // FacadeStream sincronizará resultados hacia UI
            }
            else
            {
#if DEBUG
                Console.WriteLine($"  [EquilCalc] ⚠️ Estrategia finalizó pero el estado térmico quedó como Undefined.");
#endif
            }

            FlowsReady?.Invoke(); // Permitir que balances de flujo se recalculen
        }

        public HashSet<IVariable> Variables { get; } = new();

        public void AddVariable(IVariable variable)
        {
            if (!Variables.Contains(variable) && variable.DataProcedence == VariableDefinedBy.StreamCalculated)
            {
                Variables.Add(variable);
            }
        }

        public void RemoveVariables(VariableDefinedBy _procedence)
        {
            var toRemove = Variables.Where(v => v.DataProcedence == _procedence).ToList();
            foreach (var v in toRemove)
            {
                v.Clear(_procedence);
                Variables.Remove(v);
            }
        }

        private IEquilibriumStrategy? CreateStrategy()
        {
            var P = _facade.Pressure;
            var T = _facade.Temperature;
            var FV = _facade.VaporFraction;
            var H = _facade.MassEnthalpy;
            var Comp = _facade.Composition;

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO PT: P + T definidos → calcular VF
            // ─────────────────────────────────────────────────────────
            if (IsDefined(P) && IsDefined(T) && IsCompositionDefined(Comp))
            {
                _currentMode = EquilibriumMode.PT;
                return new PTStrategy(_facade);
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO P-FV: P + VF definidos → calcular T
            // ─────────────────────────────────────────────────────────
            if (IsDefined(P) && IsDefined(FV) && IsCompositionDefined(Comp))
            {
                _currentMode = EquilibriumMode.PFV;
                double vfPercent = FV.Value.GetValue(PercentageUnits.Percentage);
                return vfPercent switch
                {
                    <= 0 => new PFVLiquidStrategy(_facade),      // Líquido subenfriado
                    >= 100 => new PFVVaporStrategy(_facade),     // Vapor sobrecalentado
                    _ => new PFVTwoPhaseStrategy(_facade)        // Bifásico
                };
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO T-FV: T + VF definidos → calcular P
            // ─────────────────────────────────────────────────────────
            if (IsDefined(T) && IsDefined(FV) && IsCompositionDefined(Comp))
            {
                _currentMode = EquilibriumMode.TFV;
                double vfPercent = FV.Value.GetValue(PercentageUnits.Percentage);
                return vfPercent switch
                {
                    <= 0 => new TFVLiquidStrategy(_facade),
                    >= 100 => new TFVVaporStrategy(_facade),
                    _ => new TFVTwoPhaseStrategy(_facade)
                };
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO PH: P + H definidos → calcular T (T debe ser Undefined)
            // ─────────────────────────────────────────────────────────
            if (IsDefined(P) && IsDefined(H) && !IsDefined(T) && IsCompositionDefined(Comp))
            {
                _currentMode = EquilibriumMode.PH;
                return new PHStrategy(_facade);
            }

            return null;
        }

        /// <summary>
        /// Helper: verifica si una variable está definida (UserSpecified o Calculated).
        /// </summary>
        private bool IsDefined<T>(IVariable<T> variable) where T : Amount
        {
            return variable.IsDefined;
        }

        private bool IsCompositionDefined(CompositionOrchestrator composition)
        {
            return composition.IsValid;
        }

      

    }
    

}
