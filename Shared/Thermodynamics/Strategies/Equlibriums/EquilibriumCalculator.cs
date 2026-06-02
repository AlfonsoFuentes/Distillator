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
            RemoveVariables(VariableDataProcedence.StreamCalculated);

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

        public HashSet<IProcessVariable> Variables { get; } = new();

        public void AddVariable(IProcessVariable variable)
        {
            if (!Variables.Contains(variable) && variable.DataProcedence == VariableDataProcedence.StreamCalculated)
            {
                Variables.Add(variable);
            }
        }

        public void RemoveVariables(VariableDataProcedence _procedence)
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
        private bool IsDefined<T>(ProcessVariable<T> variable) where T : Amount
        {
            return variable.IsSpecToCalculate;
        }

        private bool IsCompositionDefined(CompositionOrchestrator composition)
        {
            return composition.IsValid;
        }

      

    }
    public class EquilibriumCalculator4 : IProcessVariableOwner
    {

        private readonly IFacadeStream _facade;
        private IMaterialStream MaterialStream => _facade.MaterialStream;
        private IEquilibriumStrategy? _currentStrategy;
        private EquilibriumMode _currentMode;

        public event Action? EquilibriumReady;
        public event Action? FlowsReady;
        public EquilibriumMode CurrentMode => _currentMode;

        public EquilibriumCalculator4(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
            _currentMode = EquilibriumMode.None;
        }
        public void Execute()
        {
            // Resetear estado previo
            _facade.IsEquilibriumSolved = false;
            MaterialStream.CurrentState = ThermodynamicState.Undefined;
            RemoveVariables(VariableDataProcedence.StreamCalculated);

            // 1. CREAR ESTRATEGIA (La validación termodinámica/Gibbs es implícita aquí)
            _currentStrategy = CreateStrategy();

            if (_currentStrategy == null)
            {
            
                _currentMode = EquilibriumMode.None;
                FlowsReady?.Invoke(); // Permitir que flujos se evalúen incluso sin equilibrio
                return;
            }

            // 2. EJECUTAR ESTRATEGIA
            try
            {
                _currentStrategy.Execute();
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"Error during equilibrium calculation: {ex.Message}");
                MaterialStream.CurrentState = ThermodynamicState.Undefined;
                FlowsReady?.Invoke();
                return;
            }

            // 3. VERIFICAR ÉXITO Y NOTIFICAR
            if (MaterialStream.CurrentState != ThermodynamicState.Undefined)
            {
                _facade.IsEquilibriumSolved = true;
                EquilibriumReady?.Invoke(); // FacadeStream sincronizará resultados hacia UI
            }

            FlowsReady?.Invoke(); // Permitir que balances de flujo se recalculen
        }
    
        public HashSet<IProcessVariable> Variables { get; } = new();
        public void AddVariable(IProcessVariable variable)
        {
            if (!Variables.Contains(variable) && variable.DataProcedence == VariableDataProcedence.StreamCalculated)
            {
                Variables.Add(variable);
            }
        }
        public void RemoveVariables(VariableDataProcedence _procedence)
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
        private bool IsDefined<T>(ProcessVariable<T> variable) where T : Amount
        {
            return variable.IsSpecToCalculate;
        }

        private bool IsCompositionDefined(CompositionOrchestrator composition)
        {
            return  composition.IsValid;
        }

        private GibbsValidationResult ValidateGibbsPhaseRule()
        {
            // 1. Validar que hay componentes
            if (!_facade.Composition.Components.Any())
            {
                return GibbsValidationResult.Invalid("No components defined. At least 1 component required for VLE.");
            }

            // 2. Grados de libertad permitidos: siempre 2 para flash VLE
            const int allowedDegreesOfFreedom = 2;

            // 3. Contar variables intensivas especificadas (T, P, FV)
            int specifiedVariables = CountSpecifiedIntensiveVariables();

            // 4. Validar: no sobre-especificar
            if (specifiedVariables > allowedDegreesOfFreedom)
            {
                return GibbsValidationResult.Invalid(
                    $"System OVER-SPECIFIED: {specifiedVariables} intensive variables defined (e.g., T, P, VF), " +
                    $"but only {allowedDegreesOfFreedom} degrees of freedom allowed for VLE flash.");
            }

            // 5. Validar: se requieren exactamente 2 variables para flash
            if (specifiedVariables < allowedDegreesOfFreedom)
            {
                return GibbsValidationResult.Invalid(
                    $"System UNDER-SPECIFIED: {specifiedVariables} intensive variable(s) defined. " +
                    $"Exactly 2 intensive variables required for VLE flash calculation.");
            }

            // 6. Validar composición si está definida
            if (_facade.Composition.IsValid)
            {
                var compositionValidation = ValidateComposition();
                if (!compositionValidation.IsValid)
                {
                    return compositionValidation;
                }
            }
            else
            {
                return GibbsValidationResult.Invalid("Composition not defined. Composition must be defined for VLE flash.");
            }

            return GibbsValidationResult.Valid();
        }

        /// <summary>
        /// Cuenta variables intensivas especificadas (T, P, FV).
        /// La composición NO cuenta aquí (se valida separadamente).
        /// </summary>
        private int CountSpecifiedIntensiveVariables()
        {
            int count = 0;
            if (IsDefined(_facade.Temperature)) count++;
            if (IsDefined(_facade.Pressure)) count++;
            if (IsDefined(_facade.VaporFraction)) count++;
            // Nota: MassEnthalpy solo cuenta para modo PH, no para grados de libertad base
            return count;
        }

        /// <summary>
        /// Valida composición delegando a CompositionOrchestrator.
        /// </summary>
        private GibbsValidationResult ValidateComposition()
        {
            // ✅ Delegar toda la validación a CompositionOrchestrator.IsValid
            if (!_facade.Composition.IsValid)
            {
                // Obtener mensaje de error específico para diagnóstico
                if (!_facade.Composition.ValidateMassFractions(out string massError))
                    return GibbsValidationResult.Invalid($"Mass fractions invalid: {massError}");

                if (!_facade.Composition.ValidateMoleFractions(out string moleError))
                    return GibbsValidationResult.Invalid($"Mole fractions invalid: {moleError}");

                // Fallback si pasa validación individual pero no IsValid (ej: no todos definidos)
                return GibbsValidationResult.Invalid("Composition not fully defined or inconsistent.");
            }

            return GibbsValidationResult.Valid();
        }
    }

    public class EquilibriumCalculator3
    {
        private readonly IStreamFacade _facade;
        private IMaterialStream _materialStream => _facade.MaterialStream;
        private IEquilibriumStrategy? _currentStrategy;
        private EquilibriumMode _currentMode;


        public event Action? EquilibriumReady;

        public event Action? FlowsReady;
        //public bool IsEquilibriumReady { get; private set; }
        public EquilibriumMode CurrentMode => _currentMode;

        // ✅ Constructor inyecta interfaz
        public EquilibriumCalculator3(IStreamFacade facade)
        {
            _facade = facade;  // ← No conoce el Facade concreto

            _currentMode = EquilibriumMode.None;

        }

        public void Execute()
        {

            _facade.IsEquilibriumSolved = false;
            _facade.RemoveEquilibriumCalculate();
            // ✅ PASO 1: Validar Regla de Fases de Gibbs
            var gibbsValidation = ValidateGibbsPhaseRule();

            // (Borramos el _facade.IsEquilibriumSolved = false porque el Reset de arriba ya lo hace)

            if (!gibbsValidation.IsValid)
            {
                // ❌ Sistema no válido termodinámicamente
                _currentMode = EquilibriumMode.None;
                _currentStrategy = null;
                _materialStream.CurrentState = ThermodynamicState.Undefined;
                // Avisamos a los flujos para que se evalúen (ellos sabrán si pueden calcular masa/moles o no)
                FlowsReady?.Invoke();
                return;  // ← Retornar temprano, NO ejecutar estrategia
            }

            _currentStrategy = CreateStrategy(
                P: _facade.Pressure,
                T: _facade.Temperature,
                FV: _facade.VaporFraction,
                H: _facade.MassEnthalpy,

                Comp: _facade.StreamComposition);

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


        private IEquilibriumStrategy? CreateStrategy(VariableAmount<Pressure> P, VariableAmount<Temperature> T, VariableDouble FV, VariableAmount<MassEnergy> H,
           VariableComposition Comp)
        {
            // ─────────────────────────────────────────────────────────
            // 🔹 MODO PT: Se conocen P y T → se calcula VF
            // ─────────────────────────────────────────────────────────
            if (P.IsDefined && T.IsDefined && Comp.IsDefined)
            {
                return new PTStrategy3(_facade);
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO P-FV: Se conocen P y VF → se calcula T
            // ─────────────────────────────────────────────────────────
            if (P.IsDefined && FV.IsDefined && Comp.IsDefined)
            {
                return FV.Value switch
                {
                    <= 0 => new PFVLiquidStrategy3(_facade),    // Líquido subenfriado
                    >= 1 => new PFVVaporStrategy3(_facade),     // Vapor sobrecalentado
                    _ => new PFVTwoPhaseStrategy3(_facade)   // Bifásico (0 < VF < 1)
                };
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO T-FV: Se conocen T y VF → se calcula P
            // ─────────────────────────────────────────────────────────
            if (T.IsDefined && FV.IsDefined && Comp.IsDefined)
            {
                return FV.Value switch
                {
                    <= 0 => new TFVLiquidStrategy3(_facade),    // Líquido subenfriado
                    >= 1 => new TFVVaporStrategy3(_facade),     // Vapor sobrecalentado
                    _ => new TFVTwoPhaseStrategy3(_facade)   // Bifásico (0 < VF < 1)
                };
            }
            if (P.IsDefined && H.IsDefined && Comp.IsDefined && !T.IsDefined)
            {
                return new PHStrategy3(_facade);
            }


            return null;
        }


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
            if (_facade.MassEnthalpy.IsDefined) count++;

            // ✅ Composición NO cuenta aquí (se valida separadamente en ValidateComposition)
            return count;
        }

        // EN: EquilibriumCalculator2.cs (modificar ValidateComposition para manejar el caso edge)

        private GibbsValidationResult ValidateComposition()
        {
            var composition = _facade.StreamComposition.Value;

            if (composition == null || composition.Components == null || composition.Components.Count == 0)
            {
                return GibbsValidationResult.Invalid("Composición es null o vacía.");
            }

            // Validar que TODAS las fracciones estén definidas (no solo la primera)
            bool allMolarDefined = composition.Components.All(c => c.MolarFractionSolver.IsDefined);
            bool allMassDefined = composition.Components.All(c => c.MassFractionSolver.IsDefined);

            if (!allMolarDefined && !allMassDefined)
            {
                return GibbsValidationResult.Invalid("No todas las fracciones de composición están definidas.");
            }

            // Validar suma de fracciones
            double sum = 0;
            if (allMolarDefined)
            {
                sum = composition.Components.Sum(c => c.MolarFractionSolver.Value);
            }
            else if (allMassDefined)
            {
                sum = composition.Components.Sum(c => c.MassFractionSolver.Value);
            }

            const double tolerance = 0.01;  // 0.01% de tolerancia

            if (Math.Abs(sum - 100.0) > tolerance)
            {
                return GibbsValidationResult.Invalid(
                    $"Suma de fracciones = {sum:F4}%, debe estar en [{100.0 - tolerance}%, {100.0 + tolerance}%]");
            }

            return GibbsValidationResult.Valid();
        }

    }

    public class EquilibriumCalculator2
    {
        private readonly IStreamFacade2 _facade;
        private IMaterialStream _materialStream => _facade.MaterialStream;
        private IEquilibriumStrategy? _currentStrategy;
        private EquilibriumMode _currentMode;


        public event Action? EquilibriumReady;

        public event Action? FlowsReady;
        //public bool IsEquilibriumReady { get; private set; }
        public EquilibriumMode CurrentMode => _currentMode;

        // ✅ Constructor inyecta interfaz
        public EquilibriumCalculator2(IStreamFacade2 facade)
        {
            _facade = facade;  // ← No conoce el Facade concreto

            _currentMode = EquilibriumMode.None;

        }

        public void Execute()
        {

            _facade.IsEquilibriumSolved = false;
            _facade.RemoveEquilibriumCalculate();
            // ✅ PASO 1: Validar Regla de Fases de Gibbs
            var gibbsValidation = ValidateGibbsPhaseRule();

            // (Borramos el _facade.IsEquilibriumSolved = false porque el Reset de arriba ya lo hace)

            if (!gibbsValidation.IsValid)
            {
                // ❌ Sistema no válido termodinámicamente
                _currentMode = EquilibriumMode.None;
                _currentStrategy = null;
                _materialStream.CurrentState = ThermodynamicState.Undefined;
                // Avisamos a los flujos para que se evalúen (ellos sabrán si pueden calcular masa/moles o no)
                FlowsReady?.Invoke();
                return;  // ← Retornar temprano, NO ejecutar estrategia
            }

            _currentStrategy = CreateStrategy(
                P: _facade.Pressure,
                T: _facade.Temperature,
                FV: _facade.VaporFraction,
                H: _facade.MassEnthalpy,

                Comp: _facade.StreamComposition);

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


        private IEquilibriumStrategy? CreateStrategy(NewNewVariableAmount<Pressure> P, NewNewVariableAmount<Temperature> T, NewNewVariableDouble FV, NewNewVariableAmount<MassEnergy> H,
           NewNewVariableComposition Comp)
        {
            // ─────────────────────────────────────────────────────────
            // 🔹 MODO PT: Se conocen P y T → se calcula VF
            // ─────────────────────────────────────────────────────────
            if (P.IsDefined && T.IsDefined && Comp.IsDefined)
            {
                return new PTStrategy2(_facade);
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO P-FV: Se conocen P y VF → se calcula T
            // ─────────────────────────────────────────────────────────
            if (P.IsDefined && FV.IsDefined && Comp.IsDefined)
            {
                return FV.Value switch
                {
                    <= 0 => new PFVLiquidStrategy2(_facade),    // Líquido subenfriado
                    >= 1 => new PFVVaporStrategy2(_facade),     // Vapor sobrecalentado
                    _ => new PFVTwoPhaseStrategy2(_facade)   // Bifásico (0 < VF < 1)
                };
            }

            // ─────────────────────────────────────────────────────────
            // 🔹 MODO T-FV: Se conocen T y VF → se calcula P
            // ─────────────────────────────────────────────────────────
            if (T.IsDefined && FV.IsDefined && Comp.IsDefined)
            {
                return FV.Value switch
                {
                    <= 0 => new TFVLiquidStrategy2(_facade),    // Líquido subenfriado
                    >= 1 => new TFVVaporStrategy2(_facade),     // Vapor sobrecalentado
                    _ => new TFVTwoPhaseStrategy2(_facade)   // Bifásico (0 < VF < 1)
                };
            }
            if (P.IsDefined && H.IsDefined && Comp.IsDefined && !T.IsDefined)
            {
                return new PHStrategy2(_facade);
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
            if (_facade.MassEnthalpy.IsDefined) count++;

            // ✅ Composición NO cuenta aquí (se valida separadamente en ValidateComposition)
            return count;
        }

        // EN: EquilibriumCalculator2.cs (modificar ValidateComposition para manejar el caso edge)

        private GibbsValidationResult ValidateComposition()
        {
            var composition = _facade.StreamComposition.Value;

            if (composition == null || composition.Components == null || composition.Components.Count == 0)
            {
                return GibbsValidationResult.Invalid("Composición es null o vacía.");
            }

            // Validar que TODAS las fracciones estén definidas (no solo la primera)
            bool allMolarDefined = composition.Components.All(c => c.MolarFractionSolver.IsDefined);
            bool allMassDefined = composition.Components.All(c => c.MassFractionSolver.IsDefined);

            if (!allMolarDefined && !allMassDefined)
            {
                return GibbsValidationResult.Invalid("No todas las fracciones de composición están definidas.");
            }

            // Validar suma de fracciones
            double sum = 0;
            if (allMolarDefined)
            {
                sum = composition.Components.Sum(c => c.MolarFractionSolver.Value);
            }
            else if (allMassDefined)
            {
                sum = composition.Components.Sum(c => c.MassFractionSolver.Value);
            }

            const double tolerance = 0.01;  // 0.01% de tolerancia

            if (Math.Abs(sum - 100.0) > tolerance)
            {
                return GibbsValidationResult.Invalid(
                    $"Suma de fracciones = {sum:F4}%, debe estar en [{100.0 - tolerance}%, {100.0 + tolerance}%]");
            }

            return GibbsValidationResult.Valid();
        }

    }

}
