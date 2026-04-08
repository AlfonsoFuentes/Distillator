using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    // ========================================================================
    // CALCULADORA DE EQUILIBRIO
    // ========================================================================
    public class EquilibriumCalculator
    {
        private readonly StreamSimulationFacade _facade;
        private readonly MaterialStream _materialStream;
        private IEquilibriumStrategy? _currentStrategy;
        private EquilibriumMode _currentMode;

        private double VaporFraction => _facade.VaporFractionControlled.Value;
        public event Action? EquilibriumReady;

        public event Action? FlowsReady;
        public bool IsEquilibriumReady { get; private set; }
        public EquilibriumMode CurrentMode => _currentMode;

        // ✅ Constructor inyecta interfaz
        public EquilibriumCalculator(StreamSimulationFacade facade)
        {
            _facade = facade;  // ← No conoce el Facade concreto
            _materialStream = facade.MaterialStream;
            _currentMode = EquilibriumMode.None;
            IsEquilibriumReady = false;
        }

        public void OnConstraintsChanged()
        {
            _facade.ResetCalculatedVariable();
            _facade.ResetFlowsCalculatedVariable();
            // ✅ PASO 1: Validar Regla de Fases de Gibbs
            var gibbsValidation = ValidateGibbsPhaseRule();
            _facade.State = StreamStateType.MethodDefined;
            if (!gibbsValidation.IsValid)
            {
                // ❌ Sistema no válido termodinámicamente
                IsEquilibriumReady = false;
                _currentMode = EquilibriumMode.None;
                _currentStrategy = null;

                // ✅ Opcional: Log o evento de error
                System.Diagnostics.Debug.WriteLine(
                    $"[Gibbs] {gibbsValidation.ErrorMessage}");

                return;  // ← Retornar temprano, NO ejecutar estrategia
            }

       

            _currentStrategy = CreateStrategy(
                P: _facade.PressureControlled.IsDefined,
                T: _facade.TemperatureControlled.IsDefined,
                FV: _facade.VaporFractionControlled.IsDefined,
                Comp: _facade.StreamCompositionControlled.IsDefined,
                FV_defined: _facade.VaporFractionControlled.Value
);
            IsEquilibriumReady = _currentStrategy != null;
            _materialStream.CurrentState = ThermodynamicState.Undefined;
            _currentStrategy?.Execute();
            if (_materialStream.CurrentState != ThermodynamicState.Undefined)
            {
                _facade.State = StreamStateType.EquilibriumCalculated;
                IsEquilibriumReady = true;

                EquilibriumReady?.Invoke();
                FlowsReady?.Invoke();
            }
        }

        /// <summary>
        /// Crea la estrategia de equilibrio según las variables definidas.
        /// </summary>
        /// <param name="P">¿Pressure está definido?</param>
        /// <param name="T">¿Temperature está definido?</param>
        /// <param name="FV">¿VaporFraction está definido como entrada?</param>
        /// <param name="Comp">¿Composition está definido?</param>
        /// <param name="FV_defined">Valor actual de VaporFraction (usado solo si FV=true)</param>
        /// <returns>IEquilibriumStrategy o null si no hay modo válido</returns>
        private IEquilibriumStrategy? CreateStrategy(bool P, bool T, bool FV, bool Comp, double FV_defined)
        {
            // ─────────────────────────────────────────────────────────
            // 🔹 MODO PT: Se conocen P y T → se calcula VF
            // ─────────────────────────────────────────────────────────
            if (P && T && Comp)
            {
                return new PTStrategy(_facade);
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO P-FV: Se conocen P y VF → se calcula T
            // ─────────────────────────────────────────────────────────
            if (P && FV && Comp)
            {
                return FV_defined switch
                {
                    <= 0 => new PFVLiquidStrategy(_facade),    // Líquido subenfriado
                    >= 1 => new PFVVaporStrategy(_facade),     // Vapor sobrecalentado
                    _ => new PFVTwoPhaseStrategy(_facade)   // Bifásico (0 < VF < 1)
                };
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO T-FV: Se conocen T y VF → se calcula P
            // ─────────────────────────────────────────────────────────
            if (T && FV && Comp)
            {
                return FV_defined switch
                {
                    <= 0 => new TFVLiquidStrategy(_facade),    // Líquido subenfriado
                    >= 1 => new TFVVaporStrategy(_facade),     // Vapor sobrecalentado
                    _ => new TFVTwoPhaseStrategy(_facade)   // Bifásico (0 < VF < 1)
                };
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 Sin combinación válida → no hay estrategia
            // ─────────────────────────────────────────────────────────
            return null;
        }


        public void CalculateEquilibrium()
        {
            if (_currentStrategy != null && IsEquilibriumReady)
            {
                _currentStrategy.Execute();
            }
        }
        /// <summary>
        /// Valida la Regla de las Fases de Gibbs para el sistema VLE actual.
        /// F = C - P + 2, donde P = 2 (líquido + vapor)
        /// Para C componentes: F = C grados de libertad permitidos.
        /// </summary>
        private GibbsValidationResult ValidateGibbsPhaseRule()
        {
            // ✅ Obtener número de componentes
            int componentCount = GetComponentCount();

            if (componentCount == 0)
            {
                return GibbsValidationResult.Invalid(
                    "No hay componentes definidos. Se requiere al menos 1 componente para equilibrio VLE.");
            }

            // ✅ Calcular grados de libertad permitidos (F = C para VLE)
            int allowedDegreesOfFreedom = componentCount;

            // ✅ Contar variables intensivas especificadas
            int specifiedVariables = CountSpecifiedIntensiveVariables();

            // ✅ Validar: especificadas <= permitidas
            if (specifiedVariables > allowedDegreesOfFreedom)
            {
                return GibbsValidationResult.Invalid(
                    $"Sistema SOBRE-ESPECIFICADO: {specifiedVariables} variables definidas, " +
                    $"pero solo {allowedDegreesOfFreedom} grados de libertad permitidos " +
                    $"para {componentCount} componente(s) en equilibrio VLE.");
            }

            // ✅ Validar: se requieren al menos 2 variables para flash (ej: T+P, P+FV, T+FV)
            if (specifiedVariables < 2)
            {
                return GibbsValidationResult.Invalid(
                    $"Sistema SUB-ESPECIFICADO: {specifiedVariables} variables definidas. " +
                    $"Se requieren al menos 2 variables intensivas para cálculo de flash VLE.");
            }

            // ✅ Validación de composición (si está definida)
            if (_facade.StreamCompositionControlled.IsDefined)
            {
                var compositionValidation = ValidateComposition();
                if (!compositionValidation.IsValid)
                {
                    return compositionValidation;
                }
            }

            return GibbsValidationResult.Valid();
        }
        /// <summary>
        /// Obtiene el número de componentes en la mezcla.
        /// </summary>
        private int GetComponentCount()
        {
            // ✅ Intentar obtener desde StreamComposition
            var composition = _facade.StreamCompositionControlled.Value;

            return composition?.Components?.Count ?? 0;
        }

        /// <summary>
        /// Cuenta variables intensivas especificadas (T, P, FV, composiciones).
        /// </summary>
        private int CountSpecifiedIntensiveVariables()
        {
            int count = 0;

            if (_facade.TemperatureControlled.IsDefined) count++;
            if (_facade.PressureControlled.IsDefined) count++;
            if (_facade.VaporFractionControlled.IsDefined) count++;

            // ✅ Composición NO cuenta aquí (se valida separadamente en ValidateComposition)
            return count;
        }

        /// <summary>
        /// Valida que la composición sume ~1.0 y todas las fracciones estén definidas.
        /// </summary>
        private GibbsValidationResult ValidateComposition()
        {
            var composition = _facade.StreamCompositionControlled.Value;

            if (composition == null || composition.Components == null)
            {
                return GibbsValidationResult.Invalid("Composición es null.");
            }

            // ✅ Validar suma de fracciones
            double sum = 0;

            // Determinar tipo de fracción (masa o molar)
            var firstComponent = composition.Components.FirstOrDefault();
            if (firstComponent?.MolarFraction.HasValue == true)
            {
                sum = composition.Components.Sum(c => c.MolarFraction ?? 0);
            }
            else if (firstComponent?.MassFraction.HasValue == true)
            {
                sum = composition.Components.Sum(c => c.MassFraction ?? 0);
            }

            const double tolerance = 0.01;  // 1% de tolerancia

            if (Math.Abs(sum - 100) > tolerance)
            {
                return GibbsValidationResult.Invalid(
                    $"Suma de fracciones = {sum:F4}, debe estar en [{1.0 - tolerance}, {1.0 + tolerance}]");
            }

            return GibbsValidationResult.Valid();
        }
    }
}
