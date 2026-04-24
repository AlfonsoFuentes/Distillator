using Shared.Thermodynamics.ControlledVariables;
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

        private double VaporFraction => _facade.VaporFraction.Value;
        public event Action? EquilibriumReady;

        public event Action? FlowsReady;
        //public bool IsEquilibriumReady { get; private set; }
        public EquilibriumMode CurrentMode => _currentMode;

        // ✅ Constructor inyecta interfaz
        public EquilibriumCalculator(StreamSimulationFacade facade)
        {
            _facade = facade;  // ← No conoce el Facade concreto
            _materialStream = facade.MaterialStream;
            _currentMode = EquilibriumMode.None;
        
        }

        public void OnConstraintsChanged()
        {
            // ✅ 1. Solo reseteamos la memoria de la termodinámica. 
            // ¡PROHIBIDO tocar _facade.ResetFlowsCalculatedVariable() aquí!
            _facade.ResetEquilibriumCalculatedVariable();
            _facade.ResetFlowsCalculatedVariable();
            // ✅ PASO 1: Validar Regla de Fases de Gibbs
            var gibbsValidation = ValidateGibbsPhaseRule();

            // (Borramos el _facade.IsEquilibriumSolved = false porque el Reset de arriba ya lo hace)

            if (!gibbsValidation.IsValid)
            {
                // ❌ Sistema no válido termodinámicamente
                _currentMode = EquilibriumMode.None;
                _currentStrategy = null;

                // Avisamos a los flujos para que se evalúen (ellos sabrán si pueden calcular masa/moles o no)
                FlowsReady?.Invoke();
                return;  // ← Retornar temprano, NO ejecutar estrategia
            }

            _currentStrategy = CreateStrategy(
                P: _facade.Pressure.IsDefined,
                T: _facade.Temperature.IsDefined,
                FV: _facade.VaporFraction.IsDefined,
                H: _facade.MolarEnthalpy.IsDefined && _facade.MolarEnthalpy.Source != MethodSource.None,
              
                Comp: _facade.StreamComposition.IsDefined,
                FV_defined: _facade.VaporFraction.Value);

            _materialStream.CurrentState = ThermodynamicState.Undefined;
            _currentStrategy?.Execute();

            if (_materialStream.CurrentState != ThermodynamicState.Undefined)
            {
                // ✅ Solo si el Flash fue un éxito, levantamos la bandera
                _facade.IsEquilibriumSolved = true;
                EquilibriumReady?.Invoke();
            }

            FlowsReady?.Invoke();
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
        private IEquilibriumStrategy? CreateStrategy(bool P, bool T, bool FV,bool H,  bool Comp, double FV_defined)
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
            if (P && H && Comp && !T)
            {
                return new PHStrategy(_facade);
            }

            //// ─────────────────────────────────────────────────────────
            //// 🔹 MODO P-S: Se conocen Presión y Entropía (Ej: Compresión Isentrópica)
            //// ─────────────────────────────────────────────────────────
            //if (P && S && Comp && !T)
            //{
            //    return new PSStrategy(_facade);
            //}

            // ─────────────────────────────────────────────────────────
            // 🔹 Sin combinación válida → no hay estrategia
            // ─────────────────────────────────────────────────────────
            return null;
        }


        /// <summary>
        /// Valida la Regla de las Fases de Gibbs para el sistema VLE actual.
        /// F = C - P + 2, donde P = 2 (líquido + vapor)
        /// Para C componentes: F = C grados de libertad permitidos.
        /// </summary>
        /// <summary>
        /// Valida la Regla de las Fases de Gibbs para el sistema VLE actual.
        /// Según el Teorema de Duhem, para una mezcla de composición CONOCIDA,
        /// siempre se requieren EXACTAMENTE 2 variables intensivas (T y P, P y FV, etc.)
        /// </summary>
        private GibbsValidationResult ValidateGibbsPhaseRule()
        {
            int componentCount = GetComponentCount();

            if (componentCount == 0)
            {
                return GibbsValidationResult.Invalid(
                    "No hay componentes definidos. Se requiere al menos 1 componente para equilibrio VLE.");
            }

            // ✅ CORRECCIÓN CRÍTICA: Siempre 2 grados de libertad permitidos
            int allowedDegreesOfFreedom = 2;

            // Contar variables intensivas especificadas (T, P, FV)
            int specifiedVariables = CountSpecifiedIntensiveVariables();

            // Validar: especificadas <= permitidas
            if (specifiedVariables > allowedDegreesOfFreedom)
            {
                return GibbsValidationResult.Invalid(
                    $"Sistema SOBRE-ESPECIFICADO: {specifiedVariables} variables definidas (Ej: T, P y FV), " +
                    $"pero solo {allowedDegreesOfFreedom} grados de libertad son permitidos " +
                    $"para resolver el estado termodinámico.");
            }

            // Validar: se requieren al menos 2 variables para flash
            if (specifiedVariables < allowedDegreesOfFreedom)
            {
                return GibbsValidationResult.Invalid(
                    $"Sistema SUB-ESPECIFICADO: {specifiedVariables} variable(s) definida(s). " +
                    $"Se requieren exactamente 2 variables intensivas para cálculo de flash VLE.");
            }

            // Validación de composición (si está definida)
            if (_facade.StreamComposition.IsDefined)
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
            var composition = _facade.StreamComposition.Value;

            return composition?.Components?.Count ?? 0;
        }

        /// <summary>
        /// Cuenta variables intensivas especificadas (T, P, FV, composiciones).
        /// </summary>
        private int CountSpecifiedIntensiveVariables()
        {
            int count = 0;

            if (_facade.Temperature.IsDefined) count++;
            if (_facade.Pressure.IsDefined) count++;
            if (_facade.VaporFraction.IsDefined) count++;
            if (_facade.MolarEnthalpy.IsDefined) count++;

            // ✅ Composición NO cuenta aquí (se valida separadamente en ValidateComposition)
            return count;
        }

        /// <summary>
        /// Valida que la composición sume ~1.0 y todas las fracciones estén definidas.
        /// </summary>
        /// <summary>
        /// Valida que la composición sume ~100% y todas las fracciones estén definidas.
        /// </summary>
        private GibbsValidationResult ValidateComposition()
        {
            var composition = _facade.StreamComposition.Value;

            if (composition == null || composition.Components == null)
            {
                return GibbsValidationResult.Invalid("Composición es null.");
            }

            // Validar suma de fracciones
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

            const double tolerance = 0.01;  // 0.01% de tolerancia

            // ✅ CORRECCIÓN VISUAL: El mensaje ahora refleja la base 100 usada en la matemática
            if (Math.Abs(sum - 100.0) > tolerance)
            {
                return GibbsValidationResult.Invalid(
                    $"Suma de fracciones = {sum:F4}%, debe estar exactamente en el rango [{100.0 - tolerance}%, {100.0 + tolerance}%]");
            }

            return GibbsValidationResult.Valid();
        }
    }
}
