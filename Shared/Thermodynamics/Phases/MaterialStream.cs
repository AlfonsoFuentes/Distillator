using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.Componentes;
using Shared.Thermodynamics.PureComponents;
using Shared.Thermodynamics.Solvers;
using Shared.UnitOperations.Streams;
using UnitSystem;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Shared.Thermodynamics.Phases
{
    public interface IMaterialStream
    {
        string Name { get; }
        List<MainComponentNode> Components { get; }
        LiquidPhaseMixture LiquidPhase { get; }
        VaporPhaseMixture VaporPhase { get; }
        double VaporFraction { get; }
        ThermodynamicState CurrentState { get; set; }
        void SetComponentsProperties(ThermodynamicMethodFullDto method);
        void ClearComponentsProperties();
        void SetCompositionData(StreamComposition streamComposition);
        void SetVaporFraction(double vaporFraction);
        double SolveSaturationTemperature();
        double SolveSaturationPressure();
        void PerformFlashPT();

        void PerformFlashPH(Pressure targetPressure, MolarEnergy targetEnthalpy);
        double SolveFlashPVF(Pressure P, double targetVF);
        double SolveFlashTVF(Temperature T, double targetVF);

        Temperature Temperature { get; set; }
        Pressure Pressure { get; set; }

        void SetThermodynamicMethod(ThermodynamicMethodFullDto _method);
        void ClearThermodynamicMethod();
        void SetPressure(Pressure? pressure);
        void SetTemperature(Temperature? temperature);
        void CalculateBulkProperties();


        // Extensive Properties (Allowed to be set in the leaf)
        MolarFlow MolarFlow { get; set; }
        MassFlow MassFlow { get; set; }
        VolumetricFlow VolumetricFlow { get; set; }
        EnergyFlow EnthalpyFlow { get; set; }

        // Intensive Properties
        double MolecularWeight { get; set; }
        MassDensity MassDensity { get; set; }
        MolarDensity MolarDensity { get; set; }
        MolarEnergy MolarEnthalpy { get; set; }
        MassEnergy MassEnthalpy { get; set; }
        MassEntropy MassHeatCapacity { get; set; }
        MolarEntropy MolarHeatCapacity { get; set; }
        MassEntropy MassEntropy { get; set; }
        MolarEntropy MolarEntropy { get; set; }
        Viscosity Viscosity { get; set; }
        ThermalConductivity ThermalConductivity { get; set; }
        SuperficialTension SurfaceTension { get; set; }


        MolarVolumeSpecific MolarVolume { get; set; }
    }
    public class MaterialStream : Phase, IMaterialStream
    {
        // ========================================================================
        // PROPIEDADES
        // ========================================================================
        public string Name { get; init; }
        public List<MainComponentNode> Components { get; } = new List<MainComponentNode>();
        public LiquidPhaseMixture LiquidPhase { get; }
        public VaporPhaseMixture VaporPhase { get; }
        public double VaporFraction { get; private set; }
        public ThermodynamicState CurrentState { get; set; } = ThermodynamicState.Undefined;

        protected override IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation => Components;

        // ========================================================================
        // CONSTRUCTOR
        // ========================================================================
        public MaterialStream(string name = "New Stream")
        {
            Name = name;
            LiquidPhase = new LiquidPhaseMixture($"{name} - Liquid");
            VaporPhase = new VaporPhaseMixture($"{name} - Vapor");
        }

        // ========================================================================
        // INICIALIZACIÓN
        // ========================================================================
        public override void SetComponentsProperties(ThermodynamicMethodFullDto method)
        {
            Components.Clear();

            foreach (var componentDto in method.Components)
            {
                var newComponent = new MainComponentNode();
                newComponent.SetComponentData(
                    PureComponentFactory.CreateFromDto(componentDto.FullData),
                    method.LiquidModel,
                    method.VaporModel
                );
                Components.Add(newComponent);
            }

            LiquidPhase.SetThermodynamicMethod(method);
            VaporPhase.SetThermodynamicMethod(method);
        }

        public override void ClearComponentsProperties()
        {
            foreach (var component in Components)
                component.ClearComponentData();
            Components.Clear();
        }

        protected override void ClearThermodynamicMethodInternal()
        {
            base.ClearThermodynamicMethodInternal();
            ClearComponentsProperties();
            LiquidPhase.ClearThermodynamicMethod();
            VaporPhase.ClearThermodynamicMethod();
        }

        // ========================================================================
        // SETTERS
        // ========================================================================


        public void SetCompositionData(StreamComposition streamComposition)
        {
            if (streamComposition?.Components == null) return;

            foreach (var comp in streamComposition.Components)
            {
                var localComponent = Components.FirstOrDefault(x => x.Id == comp.ComponentId);
                if (localComponent != null)
                {
                    if (comp.MassFractionSolver.IsDefined)
                        localComponent.MassFraction = comp.MassFractionSolver.Value / 100;
                    if (comp.MolarFractionSolver.IsDefined)
                        localComponent.MolarFraction = comp.MolarFractionSolver.Value / 100;
                }
            }
            CalculateMixtureMolecularWeight();
        }





        // En MaterialStream.SetTemperature():
        public override void SetTemperature(Temperature? temperature)
        {
            base.SetTemperature(temperature);

            foreach (var component in ComponentsForPropagation)
                component.SetTemperature(temperature);
        }

        // En MaterialStream.SetPressure():
        public override void SetPressure(Pressure? pressure)
        {
            base.SetPressure(pressure);

            foreach (var component in ComponentsForPropagation)
                component.SetPressure(pressure);
        }
        public void SetMolarEnthalpy(MolarEnergy? molarEnthalpy)
        {


        }

        // En MaterialStream.SetVaporFraction():
        public void SetVaporFraction(double vaporFraction)
        {
            VaporFraction = vaporFraction;

        }

        public double SolveSaturationTemperature()
        {
            return SolveSaturationTemperature(Temperature, Pressure, VaporFraction);
        }
        private double SolveSaturationTemperature(Temperature temperature, Pressure pressure, double vaporfraction)
        {
            SetLiquidVaporCompositionInitial();

            // Estimación inicial basada en puntos de ebullición puros
            var (tGuess, tMin, tMax) = CalculateBubblePointBounds(Pressure);

            // Función objetivo: f(T) = Σ(K_i(T,P) · z_i) - 1 → debe ser 0
            Func<double, double> bubbleEquation = (tKelvin) =>
            {
                var temperature = new Temperature(tKelvin, TemperatureUnits.Kelvin);
                return CalculateEquilibrium(temperature, Pressure, VaporFraction);
            };

            var result = BisectionSolver.Solve(
                func: bubbleEquation,
                x1: tMin,
                x2: tMax,
                guess: tGuess
            );

            // Validar convergencia
            if (!result.Converged)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PFVLiquid] Bubble point no convergió. Residuo final: {bubbleEquation(result.Value):E3}");
                return tGuess; // Fallback
            }
            CurrentState = vaporfraction == 0 ? ThermodynamicState.SaturatedLiquid : ThermodynamicState.SaturatedVapor;
            return result.Value;
        }


        /// </summary>
        private (double guess, double min, double max) CalculateBubblePointBounds(Pressure pressure)
        {
            double minT = double.MaxValue;
            double maxT = double.MinValue;
            double tBubbleEstimate = 0.0;

            foreach (var comp in Components)
            {
                double tsat = comp.PureComponentData.GetSaturationTemperature(pressure)
                    .GetValue(TemperatureUnits.Kelvin);
                double xi = comp.MolarFraction;

                minT = Math.Min(minT, tsat);
                maxT = Math.Max(maxT, tsat);
                tBubbleEstimate += xi * tsat; // Estimación lineal
            }

            // Clamp para seguridad numérica
            double guess = Math.Clamp(tBubbleEstimate, minT, maxT);
            return (guess, minT * 0.95, maxT * 1.05); // Margen del 5%
        }
        public double SolveSaturationPressure()
        {
            return SolveSaturationPressure(Temperature, Pressure, VaporFraction);
        }
        private double SolveSaturationPressure(Temperature temperature, Pressure pressure, double vaporfraction)
        {
            SetLiquidVaporCompositionInitial();


            // Estimación inicial basada en presiones de vapor puras
            var (pGuess, pMin, pMax) = CalculateBubblePointBounds(temperature);

            // Función objetivo: f(P) = Σ(K_i(T,P) · z_i) - 1 → debe ser 0
            Func<double, double> bubbleEquation = (pKpa) =>
            {
                var pressure = new Pressure(pKpa, PressureUnits.KiloPascala);
                return CalculateEquilibrium(temperature, pressure, vaporfraction);
            };

            var result = SecantSolver.Solve(
                func: bubbleEquation,
                x1: pMin,
                x2: pMax,
                guess: pGuess
            );

            // Validar convergencia
            if (!result.Converged)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[TFVLiquid] Bubble pressure no convergió. Residuo final: {bubbleEquation(result.Value):E3}");
                return pGuess; // Fallback
            }
            CurrentState = vaporfraction == 0 ? ThermodynamicState.SaturatedLiquid : ThermodynamicState.SaturatedVapor;
            return result.Value;
        }
        private (double guess, double min, double max) CalculateBubblePointBounds(Temperature temperature)
        {
            double minP = double.MaxValue;
            double maxP = double.MinValue;
            double pBubbleEstimate = 0.0;

            foreach (var comp in Components)
            {
                double psat = comp.PureComponentData.GetVaporPressure(temperature)
                    .GetValue(PressureUnits.KiloPascala);
                double xi = comp.MolarFraction;

                minP = Math.Min(minP, psat);
                maxP = Math.Max(maxP, psat);
                pBubbleEstimate += xi * psat; // Ley de Raoult para estimación inicial
            }

            // Clamp para seguridad numérica
            double guess = Math.Clamp(pBubbleEstimate, minP, maxP);
            return (guess, minP * 0.95, maxP * 1.05); // Margen del 5%
        }
        public void SetLiquidVaporCompositionInitial()
        {
            for (int i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                var liquidcomponete = LiquidPhase.Components.FirstOrDefault(x => x.Id == component.Id);
                var vaporcomponete = VaporPhase.Components.FirstOrDefault(x => x.Id == component.Id);
                if (liquidcomponete != null)
                {
                    liquidcomponete.MassFraction = component.MassFraction;
                    liquidcomponete.MolarFraction = component.MolarFraction;
                }
                if (vaporcomponete != null)
                {
                    vaporcomponete.MassFraction = component.MassFraction;
                    vaporcomponete.MolarFraction = component.MolarFraction;
                }

            }
        }



        public double CalculateEquilibrium(Temperature temperature, Pressure pressure, double vaporFraction)
        {
            // ✅ Calcular equilibrio de AMBAS fases (necesario para K_i)
            LiquidPhase.CalculateEquilibrium(temperature, pressure);
            VaporPhase.CalculateEquilibrium(temperature, pressure);

            double sumy = 0.0;
            double sumx = 0.0;


            for (int i = 0; i < Components.Count; i++)
            {
                var globalComp = Components[i];
                var liquidComp = LiquidPhase.Components.FirstOrDefault(c => c.Id == globalComp.Id);
                var vaporComp = VaporPhase.Components.FirstOrDefault(c => c.Id == globalComp.Id);

                if (liquidComp == null || vaporComp == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Component {globalComp.Name} not found in phase");
                    continue;
                }
                double z_i = globalComp.MolarFraction;
                double liquidNum = liquidComp.LiquidFugacityNumerator;
                double vaporDen = vaporComp.VaporFugacityDenominator;
                double K_i = 0;
                if (vaporDen != 0)
                    K_i = liquidNum / vaporDen;

                double xliq = z_i / (1.0 + vaporFraction * (K_i - 1.0));
                double yvap = K_i * xliq;






                sumy += yvap;
                sumx += xliq;

                liquidComp.MolarFraction = xliq;
                vaporComp.MolarFraction = yvap;

            }
            for (int i = 0; i < Components.Count; i++)
            {
                var globalComp = Components[i];
                var liquidComp = LiquidPhase.Components.FirstOrDefault(c => c.Id == globalComp.Id);
                var vaporComp = VaporPhase.Components.FirstOrDefault(c => c.Id == globalComp.Id);
                if (liquidComp == null || vaporComp == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Component {globalComp.Name} not found in phase");
                    continue;
                }
                liquidComp.MolarFraction /= sumx;
                vaporComp.MolarFraction /= sumy;
            }
            // ✅ Residuo: debe ser 0 en el punto de burbuja
            return sumy - sumx;
        }
        public void PerformFlashPT()
        {
            PerformFlashPT(Temperature, Pressure);
        }
        private void PerformFlashPT(Temperature temperature, Pressure pressure)
        {
            Temperature = temperature;
            Pressure = pressure;

            // 1. Inicialización: Usamos Psat/P en lugar de Wilson para mayor estabilidad en polares
            double[] z = Components.Select(c => c.MolarFraction).ToArray();
            double[] K = InitializeKValuesWithPsat(temperature, pressure);

            int maxIters = 50;
            double tolerance = 1e-7;
            int iter = 0;


            while (iter < maxIters)
            {
                // 2. Resolver Rachford-Rice
                // IMPORTANTE: Aquí NO hacemos break si beta es 0 o 1 todavía.
                VaporFraction = SolveRachfordRice(z, K);

                // 3. Actualizar composiciones de fase (x_i, y_i)
                for (int i = 0; i < Components.Count; i++)
                {
                    double ki_minus_1 = K[i] - 1.0;
                    double den = 1.0 + VaporFraction * ki_minus_1;

                    // Calculamos fracciones molares de fase
                    double xi = z[i] / den;
                    double yi = K[i] * xi;

                    // Inyectamos las fracciones en los nodos de LiquidPhase y VaporPhase
                    // Esto es CRÍTICO para que el modelo de actividad (NRTL) sepa qué calcular
                    LiquidPhase.Components[i].MolarFraction = xi;
                    VaporPhase.Components[i].MolarFraction = yi;
                }

                // 4. CÁLCULO RIGUROSO (Aquí entra NRTL/UNIQUAC y Fugacidades)
                // Este método dispara CalculateEquilibrium que ya tienes definido
                LiquidPhase.CalculateEquilibrium(temperature, pressure);
                VaporPhase.CalculateEquilibrium(temperature, pressure);

                // 5. Actualizar K y verificar convergencia
                double sumDeltaK = 0.0;
                for (int i = 0; i < Components.Count; i++)
                {
                    // Usamos las propiedades que definiste en tus nodos
                    double fL = LiquidPhase.Components[i].LiquidFugacityNumerator; // Gamma * Psat...
                    double fV = VaporPhase.Components[i].VaporFugacityDenominator; // Phi_vapor * P...

                    double newK = (fV != 0) ? fL / fV : K[i];

                    // Amortiguamiento opcional para evitar oscilaciones en la primera iteración
                    newK = 0.8 * newK + 0.2 * K[i];

                    sumDeltaK += Math.Abs(Math.Log(newK / K[i]));
                    K[i] = newK;
                }

                // 6. TEST DE ESTABILIDAD RIGUROSO (Solo después de tener Gammas reales)
                // Solo permitimos salir por fase única si el error de K ya es bajo 
                // O si hemos iterado al menos 2 veces para que NRTL se estabilice.
                if (iter > 2)
                {
                    double f0 = CalculateRRFunction(0.0, z, K);
                    double f1 = CalculateRRFunction(1.0, z, K);

                    if (f0 <= 0)
                    {
                        VaporFraction = 0.0;
                        CurrentState = ThermodynamicState.SubcooledLiquid;
                        break;
                    }
                    if (f1 >= 0)
                    {
                        VaporFraction = 1.0;
                        CurrentState = ThermodynamicState.SuperheatedVapor;
                        break;
                    }
                }

                if (sumDeltaK < tolerance)
                {
                    CurrentState = ThermodynamicState.VaporLiquidMixture;

                    break;
                }

                iter++;
            }

            // Aseguramos que las fases finales reflejen el estado encontrado
            NormalizePhaseCompositions();
        }


        // 🔥 NUEVO: Encontrar intervalo [T_low, T_high] que contenga la solución
        private (double T_low, double T_high) FindTemperatureBracket(
            Pressure P, double hTarget, double T_guess)
        {
            // Evaluar H en T_guess
            var T_test = new Temperature(T_guess, TemperatureUnits.Kelvin);
            PerformFlashPT(T_test, P);
            CalculateBulkProperties();
            double h_test = MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);

            // Si ya estamos cerca, retornar intervalo estrecho
            if (Math.Abs(h_test - hTarget) < 100) // 100 J/kmol tolerancia inicial
                return (T_guess - 10, T_guess + 10);

            // Dirección de búsqueda: ¿H_target > H_test? → buscar T más alta
            bool searchUp = hTarget > h_test;

            // Expandir intervalo exponencialmente hasta bracketear
            double T_low = T_guess, T_high = T_guess;
            double step = 20; // K

            for (int i = 0; i < 20; i++) // Máximo 20 intentos
            {
                if (searchUp)
                    T_high += step;
                else
                    T_low -= step;

                // Evaluar extremo
                var T_extreme = new Temperature(searchUp ? T_high : T_low, TemperatureUnits.Kelvin);
                PerformFlashPT(T_extreme, P);
                CalculateBulkProperties();
                double h_extreme = MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);

                // ¿Bracketeamos? (h_target entre h_low y h_high)
                double h_low = searchUp ? h_test : h_extreme;
                double h_high = searchUp ? h_extreme : h_test;

                if ((hTarget >= h_low && hTarget <= h_high) ||
                    (hTarget <= h_low && hTarget >= h_high))
                {
                    return (T_low, T_high);
                }

                step *= 1.5; // Expandir paso exponencialmente
            }

            // Fallback: intervalo amplio por defecto
            return (250, 800); // K, cubre mayoría de casos industriales
        }// 🔥 NUEVO: Calcular Cp efectivo que incluye término de calor latente en bifásico

        private MolarEntropy GetEffectiveCpMolar(Temperature _temperature)
        {
            MolarEntropy result = new MolarEntropy(0, MolarEntropyUnits.J_Kgmol_C);

            // Líquido: calcular Cp si el estado lo requiere
            if (CurrentState == ThermodynamicState.SubcooledLiquid ||
                CurrentState == ThermodynamicState.SaturatedLiquid ||
                CurrentState == ThermodynamicState.VaporLiquidMixture)
            {
                // Calcula y almacena Cp en LiquidPhase.MolarHeatCapacity
                result = LiquidPhase.CalculateLiquidMixtureHeatCapacity(_temperature);
            }

            // Vapor: calcular Cp si el estado lo requiere
            if (CurrentState == ThermodynamicState.SuperheatedVapor ||
                CurrentState == ThermodynamicState.SaturatedVapor ||
                CurrentState == ThermodynamicState.VaporLiquidMixture)
            {
                // Calcula y almacena Cp en VaporPhase.MolarHeatCapacity
                result = VaporPhase.CalculateGasMixtureHeatCapacity(_temperature);
            }

            // ====================================================================
            // 2. CALCULAR Cp EFECTIVO SEGÚN ESTADO TERMODINÁMICO
            // ====================================================================

            if (CurrentState == ThermodynamicState.SubcooledLiquid ||
                CurrentState == ThermodynamicState.SaturatedLiquid)
            {
                // 🔹 Líquido puro: Cp sensible directo de fase líquida
                return result;
            }
            else if (CurrentState == ThermodynamicState.SuperheatedVapor ||
                     CurrentState == ThermodynamicState.SaturatedVapor)
            {
                // 🔹 Vapor puro: Cp sensible directo de fase vapor
                return result;
            }
            else if (CurrentState == ThermodynamicState.VaporLiquidMixture)
            {
                // 🔹 Zona bifásica: Cp_eff = Cp_sensible + (dVF/dT)_P · ΔH_vap

                // --- Paso A: Cp sensible ponderado por fracción de vapor ---
                double cpLiq = LiquidPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C) * 1000.0; // → J/(Kgmol·K)
                double cpVap = VaporPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C) * 1000.0;
                double cp_sensible = (1.0 - VaporFraction) * cpLiq + VaporFraction * cpVap;

                // --- Paso B: Término de calor latente (dVF/dT · ΔH_vap) ---
                const double dT = 0.1; // K, pequeño para derivada numérica estable

                // Guardar estado original
                var T_orig = new Temperature(_temperature.GetValue(TemperatureUnits.Kelvin), TemperatureUnits.Kelvin);
                var vf_orig = VaporFraction;

                try
                {
                    // Evaluar VF en T + dT
                    var T_plus = new Temperature(T_orig.GetValue(TemperatureUnits.Kelvin) + dT, TemperatureUnits.Kelvin);
                    PerformFlashPT(T_plus, Pressure);
                    double vf_plus = VaporFraction;

                    // Calcular derivada numérica dVF/dT
                    double dVF_dT = (vf_plus - vf_orig) / dT;

                    // Calcular ΔH_vap a T original (restaurar estado primero)
                    PerformFlashPT(T_orig, Pressure);
                    VaporFraction = vf_orig; // Restaurar fracción de vapor original

                    // Entalpías de fase a T original (asegurar que están calculadas)
                    LiquidPhase.CalculateLiquidMixtureEnthalpy(_temperature);
                    VaporPhase.CalculateGasMixtureEnthalpy(_temperature);

                    double h_vap = VaporPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                    double h_liq = LiquidPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                    double deltaH_vap = h_vap - h_liq;

                    // --- Paso C: Cp efectivo total ---
                    double cp_effective = cp_sensible + dVF_dT * deltaH_vap;

                    // Validar que Cp no sea negativo o extremo (protección numérica)
                    if (cp_effective < 10.0) cp_effective = 1000.0; // Fallback razonable para mezclas
                    if (cp_effective > 1e6) cp_effective = 1e5;     // Límite superior por seguridad

                    return new MolarEntropy(cp_effective, MolarEntropyUnits.J_Kgmol_C);
                }
                catch (Exception ex)
                {
                    // Fallback robusto si la derivada numérica falla
                    System.Diagnostics.Debug.WriteLine(
                        $"[GetEffectiveCpMolar] Fallback por excepción: {ex.Message}. Usando Cp sensible.");
                    return new MolarEntropy(cp_sensible > 10.0 ? cp_sensible : 1000.0, MolarEntropyUnits.J_Kgmol_C);
                }
            }
            else
            {
                // 🔹 Fallback por seguridad (estado no manejado)
                System.Diagnostics.Debug.WriteLine(
                    $"[GetEffectiveCpMolar] WARNING: Estado no manejado: {CurrentState}. Usando Cp líquido por defecto.");

                // Asegurar que LiquidPhase tenga Cp calculado
                result = LiquidPhase.CalculateLiquidMixtureHeatCapacity(_temperature);
                return result;
            }
        }
        private double CalculateRRFunction(double beta, double[] z, double[] K)
        {
            double f = 0.0;
            for (int i = 0; i < z.Length; i++)
                f += z[i] * (K[i] - 1.0) / (1.0 + beta * (K[i] - 1.0));
            return f;
        }
        private double[] InitializeKValuesWithPsat(Temperature t, Pressure p)
        {
            double[] K = new double[Components.Count];
            double P_total = p.GetValue(PressureUnits.KiloPascala);

            for (int i = 0; i < Components.Count; i++)
            {
                // Usamos tus evaluadores DIPPR que ya verificamos
                double Psat = Components[i].PureComponentData.GetVaporPressure(t).GetValue(PressureUnits.KiloPascala);
                K[i] = Psat / P_total;
            }
            return K;
        }

        private double SolveRachfordRice(double[] z, double[] K)
        {
            // Función objetivo: f(β) = Σ [ z_i * (K_i - 1) / (1 + β*(K_i - 1)) ] = 0

            // Primero, evaluamos los límites para ver si está en 1 sola fase
            double f0 = 0.0; // f(0)
            double f1 = 0.0; // f(1)

            for (int i = 0; i < z.Length; i++)
            {
                f0 += z[i] * (K[i] - 1.0);
                f1 += z[i] * (K[i] - 1.0) / K[i];
            }

            // Si f(0) < 0, la mezcla está subenfriada (todo líquido)
            if (f0 <= 0.0) return 0.0;

            // Si f(1) > 0, la mezcla está sobrecalentada (todo vapor)
            if (f1 >= 0.0) return 1.0;

            // Si cruza el cero, está en 2 fases. Usamos Newton-Raphson.
            double beta = 0.5; // Valor semilla
            double tol = 1e-7;
            int maxIters = 50;

            for (int it = 0; it < maxIters; it++)
            {
                double f = 0.0;
                double df = 0.0;

                for (int i = 0; i < z.Length; i++)
                {
                    double ki_minus_1 = K[i] - 1.0;
                    double den = 1.0 + beta * ki_minus_1;

                    f += z[i] * ki_minus_1 / den;
                    df -= z[i] * Math.Pow(ki_minus_1 / den, 2.0); // Derivada siempre negativa
                }

                if (Math.Abs(f) < tol) break;

                // Actualización Newton-Raphson
                double step = f / df;
                beta -= step;

                // Clamping para evitar que el solver se dispare matemáticamente
                beta = Math.Clamp(beta, 0.0, 1.0);
            }

            return beta;
        }

        private void NormalizePhaseCompositions()
        {
            double sumX = LiquidPhase.Components.Sum(c => c.MolarFraction);
            double sumY = VaporPhase.Components.Sum(c => c.MolarFraction);

            double sumMassX = LiquidPhase.Components.Sum(c => c.MolarFraction * c.PureComponentData.MolecularWeight);
            double sumMassY = VaporPhase.Components.Sum(c => c.MolarFraction * c.PureComponentData.MolecularWeight);

            for (int i = 0; i < Components.Count; i++)
            {
                if (sumX > 0 && sumMassX > 0)
                {
                    // 1. Calculamos la fracción másica usando el MolarFraction ORIGINAL
                    LiquidPhase.Components[i].MassFraction = (LiquidPhase.Components[i].MolarFraction * LiquidPhase.Components[i].PureComponentData.MolecularWeight) / sumMassX;
                    // 2. AHORA SÍ normalizamos el MolarFraction
                    LiquidPhase.Components[i].MolarFraction /= sumX;
                }

                if (sumY > 0 && sumMassY > 0)
                {
                    VaporPhase.Components[i].MassFraction = (VaporPhase.Components[i].MolarFraction * VaporPhase.Components[i].PureComponentData.MolecularWeight) / sumMassY;
                    VaporPhase.Components[i].MolarFraction /= sumY;
                }
            }
        }

        private void CalculateMixtureMolecularWeight()
        {
            // Esto es termodinámicamente universal, sea líquido subenfriado, 
            // mezcla bifásica o vapor sobrecalentado.
            MolecularWeight = Components.Sum(c => c.MolarFraction * c.PureComponentData.MolecularWeight);
        }

        public void CalculateBulkProperties()
        {
            if (CurrentState == ThermodynamicState.Undefined) return;



            // 1. Calcular propiedades de las fases usando la T y P reales de la corriente
            // (Esto garantiza que capturemos el subenfriamiento o sobrecalentamiento)
            if (CurrentState != ThermodynamicState.SuperheatedVapor)
            {
                LiquidPhase.CalculateBulkProperties(Temperature, Pressure);
            }

            if (CurrentState != ThermodynamicState.SubcooledLiquid)
            {
                VaporPhase.CalculateBulkProperties(Temperature, Pressure);
            }

            // 2. Pesos Moleculares y Fracciones
            double mwLiq = 0.0, mwVap = 0.0;

            if (CurrentState != ThermodynamicState.SuperheatedVapor)
                mwLiq = LiquidPhase.Components.Sum(c => c.MolarFraction * c.PureComponentData.MolecularWeight);

            if (CurrentState != ThermodynamicState.SubcooledLiquid)
                mwVap = VaporPhase.Components.Sum(c => c.MolarFraction * c.PureComponentData.MolecularWeight);

            double mwGlobal = (1.0 - VaporFraction) * mwLiq + VaporFraction * mwVap;

            // Calidad del vapor (Fracción másica, W)
            double vaporMassFraction = (mwGlobal > 0) ? (VaporFraction * mwVap) / mwGlobal : 0.0;

            // ====================================================================
            // 3. MEZCLA DE PROPIEDADES TERMODINÁMICAS (H, S, Cp, Densidad)
            // ====================================================================

            double hMolarMix = 0.0, hMassMix = 0.0;
            double sMolarMix = 0.0, sMassMix = 0.0; // <-- Agregado para Entropía
            double cpMolarMix = 0.0, cpMassMix = 0.0;
            double vMolarMix = 0.0; // Volumen específico molar

            if (CurrentState == ThermodynamicState.SubcooledLiquid)
            {
                hMolarMix = LiquidPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                hMassMix = LiquidPhase.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg);

                sMolarMix = LiquidPhase.MolarEntropy.GetValue(MolarEntropyUnits.J_Kgmol_C); // <-- Entropía Líquido
                sMassMix = LiquidPhase.MassEntropy.GetValue(MassEntropyUnits.J_Kg_C);       // <-- Entropía Líquido

                cpMolarMix = LiquidPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C);
                cpMassMix = LiquidPhase.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C);
                vMolarMix = 1.0 / LiquidPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3);
            }
            else if (CurrentState == ThermodynamicState.SuperheatedVapor)
            {
                hMolarMix = VaporPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                hMassMix = VaporPhase.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg);

                sMolarMix = VaporPhase.MolarEntropy.GetValue(MolarEntropyUnits.J_Kgmol_C); // <-- Entropía Vapor
                sMassMix = VaporPhase.MassEntropy.GetValue(MassEntropyUnits.J_Kg_C);       // <-- Entropía Vapor

                cpMolarMix = VaporPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C);
                cpMassMix = VaporPhase.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C);
                vMolarMix = 1.0 / VaporPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3);
            }
            else // Zona Bifásica (VaporLiquidMixture, SaturatedLiquid, SaturatedVapor)
            {
                hMolarMix = (1.0 - VaporFraction) * LiquidPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol) +
                            VaporFraction * VaporPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                hMassMix = (1.0 - vaporMassFraction) * LiquidPhase.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg) +
                           vaporMassFraction * VaporPhase.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg);

                // <-- Mezcla Bifásica de Entropía
                sMolarMix = (1.0 - VaporFraction) * LiquidPhase.MolarEntropy.GetValue(MolarEntropyUnits.J_Kgmol_C) +
                            VaporFraction * VaporPhase.MolarEntropy.GetValue(MolarEntropyUnits.J_Kgmol_C);
                sMassMix = (1.0 - vaporMassFraction) * LiquidPhase.MassEntropy.GetValue(MassEntropyUnits.J_Kg_C) +
                           vaporMassFraction * VaporPhase.MassEntropy.GetValue(MassEntropyUnits.J_Kg_C);

                cpMolarMix = (1.0 - VaporFraction) * LiquidPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C) +
                             VaporFraction * VaporPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C);
                cpMassMix = (1.0 - vaporMassFraction) * LiquidPhase.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C) +
                            vaporMassFraction * VaporPhase.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C);

                double liquiddensity = LiquidPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3);
                double vapordensity = VaporPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3);
                vMolarMix = (1.0 - VaporFraction) * (1.0 / liquiddensity) +
                            VaporFraction * (1.0 / vapordensity);
            }

            MolarEnthalpy = new MolarEnergy(hMolarMix, MolarEnergyUnits.J_Kgmol);
            MassEnthalpy = new MassEnergy(hMassMix, MassEnergyUnits.J_Kg);

            MolarEntropy = new MolarEntropy(sMolarMix, MolarEntropyUnits.J_Kgmol_C); // <-- Asignación Entropía Molar
            MassEntropy = new MassEntropy(sMassMix, MassEntropyUnits.J_Kg_C);       // <-- Asignación Entropía Másica

            MolarHeatCapacity = new MolarEntropy(cpMolarMix, MolarEntropyUnits.KJ_Kgmol_C);
            MassHeatCapacity = new MassEntropy(cpMassMix, MassEntropyUnits.KJ_Kg_C);

            if (vMolarMix > 0)
            {
                MolarDensity = new MolarDensity(1.0 / vMolarMix, MolarDensityUnits.Kgmol_m3);
                MassDensity = new MassDensity((1.0 / vMolarMix) * mwGlobal, MassDensityUnits.Kg_m3);
            }

            // ====================================================================
            // 4. MEZCLA DE PROPIEDADES DE TRANSPORTE (Viscosidad, Conductividad)
            // ====================================================================

            if (CurrentState == ThermodynamicState.SubcooledLiquid)
            {
                Viscosity = new Viscosity(LiquidPhase.Viscosity.GetValue(ViscosityUnits.Pa_s), ViscosityUnits.Pa_s);
                ThermalConductivity = new ThermalConductivity(LiquidPhase.ThermalConductivity.GetValue(ThermalConductivityUnits.W_m_K), ThermalConductivityUnits.W_m_K);
                SurfaceTension = new SuperficialTension(LiquidPhase.SurfaceTension.GetValue(SuperficialTensionUnits.N_m), SuperficialTensionUnits.N_m);
            }
            else if (CurrentState == ThermodynamicState.SuperheatedVapor)
            {
                Viscosity = new Viscosity(VaporPhase.Viscosity.GetValue(ViscosityUnits.Pa_s), ViscosityUnits.Pa_s);
                ThermalConductivity = new ThermalConductivity(VaporPhase.ThermalConductivity.GetValue(ThermalConductivityUnits.W_m_K), ThermalConductivityUnits.W_m_K);
                SurfaceTension = new SuperficialTension(0.0, SuperficialTensionUnits.N_m); // El vapor no tiene tensión superficial
            }
            else // Zona Bifásica
            {
                double viscLiq = LiquidPhase.Viscosity.GetValue(ViscosityUnits.Pa_s);
                double viscVap = VaporPhase.Viscosity.GetValue(ViscosityUnits.Pa_s);

                // Modelo de McAdams para viscosidad bifásica (basado en fracciones másicas)
                double mixViscosity = 0.0;
                if (viscLiq > 0 && viscVap > 0)
                {
                    double invViscMix = (vaporMassFraction / viscVap) + ((1.0 - vaporMassFraction) / viscLiq);
                    mixViscosity = 1.0 / invViscMix;
                }
                Viscosity = new Viscosity(mixViscosity, ViscosityUnits.Pa_s);

                // Conductividad Térmica (ponderación por fracción volumétrica del líquido - "Liquid Holdup")
                double densLiqMass = LiquidPhase.MassDensity.GetValue(MassDensityUnits.Kg_m3);
                double densVapMass = VaporPhase.MassDensity.GetValue(MassDensityUnits.Kg_m3);

                double volLiq = (1.0 - vaporMassFraction) / densLiqMass;
                double volVap = vaporMassFraction / densVapMass;
                double totalVol = volLiq + volVap;

                double liquidVolFraction = (totalVol > 0) ? (volLiq / totalVol) : 0.0;

                double kLiq = LiquidPhase.ThermalConductivity.GetValue(ThermalConductivityUnits.W_m_K);
                double kVap = VaporPhase.ThermalConductivity.GetValue(ThermalConductivityUnits.W_m_K);

                double mixConductivity = liquidVolFraction * kLiq + (1.0 - liquidVolFraction) * kVap;
                ThermalConductivity = new ThermalConductivity(mixConductivity, ThermalConductivityUnits.W_m_K);

                // La tensión superficial en mezcla bifásica es simplemente la del líquido existente
                SurfaceTension = new SuperficialTension(LiquidPhase.SurfaceTension.GetValue(SuperficialTensionUnits.N_m), SuperficialTensionUnits.N_m);
            }
        }
        // 🔥 REEMPLAZAR el while loop de PerformFlashPH por este:
        /// <summary>
        /// Flash isentálpico (P-H): encuentra T tal que H(T,P) = H_target.
        /// Enfoque: While-scan para bracketing + while-bisección para convergencia.
        /// Cero for-loops, control manual de iteraciones.
        /// </summary>
        public void PerformFlashPH(Pressure targetPressure, MolarEnergy targetEnthalpy)
        {
            // 🔹 1. Normalizar unidades: target en J/Kgmol para consistencia interna
            double hTargetJ = targetEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol);
            Pressure = targetPressure;

            // 🔹 2. Parámetros del scan incremental
            const double T_START = 273.15;    // 0°C, punto de partida
            const double T_MAX = 1000.0;      // Límite superior de seguridad
            const double T_STEP = 20.0;       // Paso del scan: 20 K
            const double TOL_SCAN = 1e-1;     // Tolerancia relajada durante el scan
            const double TOL_BISECTION_H = 1e-2;  // Tolerancia en entalpía para bisección
            const double TOL_BISECTION_T = 1e-4;  // Tolerancia en temperatura para bisección
            const int MAX_BISECTION_ITERS = 100;

            // 🔹 3. Inicializar variables del scan
            double T_current = T_START;
            double T_prev = T_START;
            double error_prev = double.NaN;
            bool bracketFound = false;
            double T_low = 0, T_high = 0;

            // 🔹 4. FASE 1: WHILE-SCAN para encontrar bracket [T_low, T_high]
            while (T_current <= T_MAX)
            {
                var T_obj = new Temperature(T_current, TemperatureUnits.Kelvin);

                // Flash PT para estado físico actual
                PerformFlashPT(T_obj, Pressure);

                // Calcular entalpía (sin side-effects)
                var hResult = CalculateMolarEnthalpyOnly(T_obj);
                double hCalc = hResult.GetValue(MolarEnergyUnits.KJ_Kgmol);

                double error = hCalc - hTargetJ;

                // ✅ Convergencia directa durante el scan (caso afortunado)
                if (Math.Abs(error) < TOL_SCAN)
                {
                    Temperature = T_obj;
                    MolarEnthalpy = hResult;
                    return;
                }

                // 🔍 Detectar cambio de signo → bracket encontrado
                if (!double.IsNaN(error_prev) && (error * error_prev) < 0)
                {
                    // ✅ Bracket: [T_prev, T_current] contiene la solución
                    T_low = T_prev;
                    T_high = T_current;
                    bracketFound = true;
                    break;
                }

                // Preparar para siguiente iteración
                T_prev = T_current;
                error_prev = error;
                T_current += T_STEP;
            }

            // 🔹 5. Si no se encontró bracket, reportar error
            if (!bracketFound)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Flash P-H] ERROR: No se encontró bracket con cambio de signo. " +
                    $"hTarget={hTargetJ:E3} J/kmol, H_final={CalculateMolarEnthalpyOnly(new Temperature(T_current, TemperatureUnits.Kelvin)).GetValue(MolarEnergyUnits.J_Kgmol):E3} J/kmol, " +
                    $"T_final={T_current:F1} K, VF={VaporFraction:F3}");
                return;
            }

            // 🔹 6. FASE 2: WHILE-BISECCIÓN dentro del bracket [T_low, T_high]
            double T_solution = (T_low + T_high) / 2;
            double hFinal = double.NaN;
            int bisectionIter = 0;  // 👈 Contador manual, no for

            while (bisectionIter < MAX_BISECTION_ITERS)
            {
                var T_mid = new Temperature(T_solution, TemperatureUnits.Kelvin);

                // Flash PT y cálculo de entalpía
                PerformFlashPT(T_mid, Pressure);
                var hResult = CalculateMolarEnthalpyOnly(T_mid);
                hFinal = hResult.GetValue(MolarEnergyUnits.KJ_Kgmol);

                double error = hFinal - hTargetJ;

                // ✅ Convergencia
                if (Math.Abs(error) < TOL_BISECTION_H || Math.Abs(T_high - T_low) < TOL_BISECTION_T)
                {
                    // Actualizar estado global al converger
                    Temperature = T_mid;
                    MolarEnthalpy = hResult;
                    return;
                }

                // 🔍 Determinar subintervalo: evaluar error en T_low
                var T_low_obj = new Temperature(T_low, TemperatureUnits.Kelvin);
                PerformFlashPT(T_low_obj, Pressure);
                double hLow = CalculateMolarEnthalpyOnly(T_low_obj).GetValue(MolarEnergyUnits.KJ_Kgmol);
                double errorLow = hLow - hTargetJ;

                if (errorLow * error <= 0)
                {
                    // Raíz en [T_low, T_mid]
                    T_high = T_solution;
                }
                else
                {
                    // Raíz en [T_mid, T_high]
                    T_low = T_solution;
                }

                T_solution = (T_low + T_high) / 2;
                bisectionIter++;  // 👈 Incremento manual explícito
            }

            // ❌ Bisección no convergió en MAX_ITERS
            System.Diagnostics.Debug.WriteLine(
                $"[Flash P-H] ADVERTENCIA: Bisección no convergió en {bisectionIter} iteraciones. " +
                $"Error final: {hFinal - hTargetJ:F2} J/kmol, T={T_solution:F2} K, VF={VaporFraction:F3}");
        }
        public void PerformFlashPH3(Pressure targetPressure, MolarEnergy targetEnthalpy)
        {
            double hTargetJ = targetEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol);
            Pressure = targetPressure;

            // 🔥 1. Bracketing inicial inteligente
            double T_guess = (Temperature != null && Temperature.GetValue(TemperatureUnits.Kelvin) > 0)
                             ? Temperature.GetValue(TemperatureUnits.Kelvin)
                             : 298.15;

            var (T_low, T_high) = FindTemperatureBracket(targetPressure, hTargetJ, T_guess);
            double tK = (T_low + T_high) / 2; // Empezar en el centro del bracket

            int maxIters = 50;
            double tolerance = 1e-2; // J/Kgmol (relajado para empezar)
            int iter = 0;
            double prevError = double.MaxValue;
            // Flag para detectar si estamos cerca de cambio de fase
            bool nearPhaseChange = false;
            double damping = 1.0; // Factor de amortiguamiento para Newton-Raphson
            double error = double.MaxValue;
            while (iter < maxIters)
            {
                Temperature = new Temperature(tK, TemperatureUnits.Kelvin);

                // 2. Flash PT para estado físico actual
                PerformFlashPT(Temperature, Pressure);
                var _MolarEnthalpy = CalculateMolarEnthalpyOnly(Temperature);

                double hCalc = _MolarEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol);
                error = hCalc - hTargetJ;

                // ✅ Convergencia
                if (Math.Abs(error) < tolerance)
                {
                    // Refinar tolerancia para última iteración
                    if (tolerance > 1e-3)
                    {
                        tolerance = 1e-3;
                        continue;
                    }
                    break;
                }

                // 🔥 3. Detectar si estamos cerca de cambio de fase
                // (VF cerca de 0 o 1, o Cp efectivo muy grande)
                var _cp_eff = GetEffectiveCpMolar(Temperature);
                var cp_eff = _cp_eff.GetValue(MolarEntropyUnits.J_Kgmol_C); // Convertir a J/Kgmol
                nearPhaseChange = (VaporFraction < 0.05 || VaporFraction > 0.95 || cp_eff > 1e5);

                double deltaT;

                if (nearPhaseChange || iter > 10)
                {
                    // 🔹 USAR BISECCIÓN cuando hay cambio de fase o Newton falla
                    // Bisección es más lenta pero garantizada si h(T) es monótona
                    if (error > 0)
                        T_high = tK; // H calculada > target → bajar T
                    else
                        T_low = tK;  // H calculada < target → subir T

                    tK = (T_low + T_high) / 2;
                }
                else
                {
                    // 🔹 NEWTON-RAPHSON con Cp efectivo y damping adaptativo
                    if (Math.Abs(cp_eff) < 1e-3) cp_eff = 1000.0; // Fallback

                    deltaT = -error / cp_eff;

                    // Damping adaptativo: reducir paso si error no disminuye
                    if (iter > 3 && Math.Abs(error) > prevError * 0.9)
                    {
                        damping *= 0.7; // Reducir paso
                    }
                    damping = Math.Max(damping, 0.1); // Límite inferior

                    deltaT *= damping;

                    // Clamping suave (no cortar abruptamente)
                    deltaT = Math.Clamp(deltaT, -20.0, 20.0);

                    tK += deltaT;
                }

                // 🔥 4. Mantener T dentro de límites físicos
                tK = Math.Clamp(tK, 200.0, 1000.0); // K, ajustable según componentes

                // 🔥 5. Actualizar bracket si Newton se sale
                if (!nearPhaseChange)
                {
                    if (error > 0) T_high = Math.Min(T_high, tK + 10);
                    else T_low = Math.Max(T_low, tK - 10);
                }

                prevError = Math.Abs(error);
                iter++;
            }

            if (iter >= maxIters)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Flash P-H] ADVERTENCIA: No convergió. Error final: {error:F2} J/kmol, VF={VaporFraction:F3}, T={tK:F1} K");
            }
        }
        /// <summary>
        /// Calcula la entalpía molar de la corriente para el estado actual (T, P, VF).
        /// Optimizado: solo calcula entalpía, no Cp, S, densidad, viscosidad, etc.
        /// </summary>
        /// <returns>Entalpía molar en J/Kgmol</returns>
        public MolarEnergy CalculateMolarEnthalpyOnly(Temperature _temperature)
        {
            if (CurrentState == ThermodynamicState.Undefined)
                return new MolarEnergy(0, MolarEnergyUnits.J_Kgmol);

            MolarEnergy liquidenthalpy = new MolarEnergy(0, MolarEnergyUnits.J_Kgmol);
            MolarEnergy vaporenthalpy = new MolarEnergy(0, MolarEnergyUnits.J_Kgmol);

            // Líquido: calcular si el estado lo requiere
            if (CurrentState == ThermodynamicState.SubcooledLiquid ||
                CurrentState == ThermodynamicState.SaturatedLiquid ||
                CurrentState == ThermodynamicState.VaporLiquidMixture)
            {
                // Calcula y retorna entalpía líquida (asume que también la almacena en LiquidPhase.MolarEnthalpy)
                liquidenthalpy = LiquidPhase.CalculateLiquidMixtureEnthalpy(_temperature);
            }

            // Vapor: calcular si el estado lo requiere
            if (CurrentState == ThermodynamicState.SuperheatedVapor ||
                CurrentState == ThermodynamicState.SaturatedVapor ||
                CurrentState == ThermodynamicState.VaporLiquidMixture)
            {
                // Calcula y retorna entalpía vapor (asume que también la almacena en VaporPhase.MolarEnthalpy)
                vaporenthalpy = VaporPhase.CalculateGasMixtureEnthalpy(_temperature);
            }

            // ====================================================================
            // 2. MEZCLAR ENTALPÍAS SEGÚN ESTADO TERMODINÁMICO
            // ====================================================================
            double hMolarMix = 0.0;

            if (CurrentState == ThermodynamicState.SubcooledLiquid ||
                CurrentState == ThermodynamicState.SaturatedLiquid)
            {
                // 🔹 Líquido puro (subenfriado o saturado): entalpía directa de fase líquida
                hMolarMix = liquidenthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
            }
            else if (CurrentState == ThermodynamicState.SuperheatedVapor ||
                     CurrentState == ThermodynamicState.SaturatedVapor)
            {
                // 🔹 Vapor puro (sobrecalentado o saturado): entalpía directa de fase vapor
                hMolarMix = vaporenthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
            }
            else if (CurrentState == ThermodynamicState.VaporLiquidMixture)
            {
                // 🔹 Zona bifásica: mezcla ponderada por fracción molar de vapor (VF)
                double hLiq = liquidenthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                double hVap = vaporenthalpy.GetValue(MolarEnergyUnits.J_Kgmol);

                hMolarMix = (1.0 - VaporFraction) * hLiq + VaporFraction * hVap;
            }
            else
            {
                // 🔹 Fallback por seguridad (no debería ocurrir si CurrentState está bien definido)
                System.Diagnostics.Debug.WriteLine(
                    $"[CalculateMolarEnthalpyOnly] WARNING: Estado no manejado: {CurrentState}");
                return new MolarEnergy(0, MolarEnergyUnits.J_Kgmol);
            }

            // ====================================================================
            // 3. RETORNAR (y opcionalmente actualizar propiedad global para consistencia)
            // ====================================================================

            // Opción A: Solo retornar (sin side-effects) → más puro, pero menos consistente con el resto del código
            // return new MolarEnergy(hMolarMix, MolarEnergyUnits.J_Kgmol);

            // Opción B: Retornar + actualizar propiedad global → consistente con CalculateBulkProperties()
            var _MolarEnthalpy = new MolarEnergy(hMolarMix, MolarEnergyUnits.J_Kgmol);
            return _MolarEnthalpy;
        }

        public void PerformFlashPH2(Pressure targetPressure, MolarEnergy targetEnthalpy)
        {
            double hTargetJ = targetEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol);
            Pressure = targetPressure;

            // 1. Estimación inicial de Temperatura
            // Si la corriente ya tiene una T, la usamos. Si no, asumimos 25°C (298.15 K)
            double tK = (Temperature != null && Temperature.GetValue(TemperatureUnits.Kelvin) > 0)
                        ? Temperature.GetValue(TemperatureUnits.Kelvin)
                        : 298.15;

            Temperature = new Temperature(tK, TemperatureUnits.Kelvin);

            int maxIters = 50;
            double tolerance = 1e-3; // Tolerancia en Joules/Kgmol (muy fina)
            double error = double.MaxValue;
            int iter = 0;

            while (Math.Abs(error) > tolerance && iter < maxIters)
            {
                // 2. Resolver el estado físico para la T y P actuales (Flash P-T)
                // Esto descubre si a esta T de prueba la mezcla hierve, condensa o es 1 sola fase
                PerformFlashPT(Temperature, Pressure);

                // 3. Calcular la Entalpía y el Cp global de la corriente
                CalculateBulkProperties();

                double hCalc = MolarEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol);
                double cpMix = MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C); // Convertir a J/(Kgmol·K)

                // Error actual
                error = hCalc - hTargetJ;

                if (Math.Abs(error) < tolerance) break;

                // 4. Newton-Raphson Termodinámico (ΔT = -ΔH / Cp)
                // Protegemos el Cp para evitar divisiones por cero
                if (cpMix <= 1e-6) cpMix = 1000.0; // Fallback de seguridad

                double deltaT = error / cpMix;

                // Clamping (Limitador de paso) para evitar que la T salte a valores negativos o locos
                // Máximo permitimos saltos de 50 grados por iteración
                deltaT = Math.Clamp(deltaT, -10.0, 10.0);

                tK -= deltaT;
                Temperature = new Temperature(tK, TemperatureUnits.Kelvin);

                iter++;
            }

            if (iter >= maxIters)
            {
                System.Diagnostics.Debug.WriteLine($"[Flash P-H] ADVERTENCIA: No convergió después de {maxIters} iteraciones. Error final: {error:F2} J/kmol");
            }
        }

        // Agrega esto a MaterialStream.cs

        public double SolveFlashPVF(Pressure P, double targetVF)
        {
            // 1. Obtener límites: T a VF=0 (burbuja) y T a VF=1 (rocío)
            double tMin = SolveSaturationTemperature(Temperature, P, 0.0);
            double tMax = SolveSaturationTemperature(Temperature, P, 1.0);

            // Si por alguna razón tMin > tMax, los invertimos para el solver
            if (tMin > tMax) (tMin, tMax) = (tMax, tMin);

            // 2. Función objetivo: f(T) = Rachford-Rice residual -> 0
            Func<double, double> objFunc = (tK) =>
            {
                var t = new Temperature(tK, TemperatureUnits.Kelvin);
                return CalculateEquilibrium(t, P, targetVF);
            };

            // Usamos Bisección porque es infalible entre burbuja y rocío
            var result = BisectionSolver.Solve(objFunc, tMin, tMax, (tMin + tMax) / 2.0);

            CurrentState = ThermodynamicState.VaporLiquidMixture;
            return result.Value;
        }

        public double SolveFlashTVF(Temperature T, double targetVF)
        {
            // 1. Obtener límites: P a VF=1 (rocío) y P a VF=0 (burbuja)
            double pDew = SolveSaturationPressure(T, Pressure, 1.0);
            double pBubble = SolveSaturationPressure(T, Pressure, 0.0);

            double pMin = Math.Min(pDew, pBubble);
            double pMax = Math.Max(pDew, pBubble);

            // 2. Función objetivo: f(P) = Rachford-Rice residual -> 0
            Func<double, double> objFunc = (pKpa) =>
            {
                var p = new Pressure(pKpa, PressureUnits.KiloPascala);
                return CalculateEquilibrium(T, p, targetVF);
            };

            var result = BisectionSolver.Solve(objFunc, pMin, pMax, (pMin + pMax) / 2.0);

            CurrentState = ThermodynamicState.VaporLiquidMixture;
            return result.Value;
        }
        public void PerformFlashPS(Pressure targetPressure, MolarEntropy targetEntropy)
        {
            double sTargetJ_K = targetEntropy.GetValue(MolarEntropyUnits.J_Kgmol_C);
            Pressure = targetPressure;

            // Estimación inicial
            double tK = (Temperature != null && Temperature.GetValue(TemperatureUnits.Kelvin) > 0)
                        ? Temperature.GetValue(TemperatureUnits.Kelvin)
                        : 298.15;
            Temperature = new Temperature(tK, TemperatureUnits.Kelvin);

            int maxIters = 50;
            double tolerance = 1e-4; // Tolerancia estricta para entropía
            double error = double.MaxValue;
            int iter = 0;

            while (Math.Abs(error) > tolerance && iter < maxIters)
            {
                PerformFlashPT(Temperature, Pressure);
                CalculateBulkProperties();

                // NOTA: Asegúrate de que CalculateBulkProperties esté calculando MolarEntropy
                double sCalc = MolarEntropy.GetValue(MolarEntropyUnits.J_Kgmol_C);
                double cpMix = MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C) * 1000.0;

                error = sCalc - sTargetJ_K;

                if (Math.Abs(error) < tolerance) break;

                if (cpMix <= 1e-6) cpMix = 1000.0;

                // Newton-Raphson: dS/dT = Cp / T
                double derivative = cpMix / tK;
                double deltaT = error / derivative;

                // Clamping para estabilidad
                deltaT = Math.Clamp(deltaT, -50.0, 50.0);

                tK -= deltaT;
                Temperature = new Temperature(tK, TemperatureUnits.Kelvin);

                iter++;
            }

            if (iter >= maxIters)
            {
                System.Diagnostics.Debug.WriteLine($"[Flash P-S] ADVERTENCIA: No convergió. Error final: {error:E3}");
            }
        }

    }
}
