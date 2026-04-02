using Shared.DesignPatterns.NewFolder;
using Shared.DesignPatterns.Solvers;
using Shared.DesignPatterns.Thermodynamics.Componentes;
using Shared.Thermodynamics.Enums;
using Shared.Thermodynamics.Methods;
using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics.Phases
{
    public class MaterialStream : Phase
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
                    if (comp.MassFraction.HasValue)
                        localComponent.MassFraction = comp.MassFraction.Value / 100;
                    if (comp.MolarFraction.HasValue)
                        localComponent.MolarFraction = comp.MolarFraction.Value / 100;
                }
            }
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
                var pressure = new Pressure(pKpa, PressureUnits.KiloPascal);
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
                    .GetValue(PressureUnits.KiloPascal);
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
                var liquidComp = LiquidPhase.Components[i];
                var vaporComp = VaporPhase.Components[i];
                liquidComp.MolarFraction /= sumx;
                vaporComp.MolarFraction /= sumy;
            }
            // ✅ Residuo: debe ser 0 en el punto de burbuja
            return sumy - sumx;
        }
        public void PerformFlashPT()
        {
            PerformFlashPT(Temperature,Pressure);
        }
        private void PerformFlashPT(Temperature temperature, Pressure pressure)
        {
            Temperature = temperature;
            Pressure = pressure;
            SetLiquidVaporCompositionInitial();
            int maxIters = 50;
            double tolerance = 1e-6;
            double error = 1.0;
            int iter = 0;

            // 1. Inicialización: Estimación de K_i (Correlación de Wilson)
            double[] K = EstimateInitialKValues(temperature, pressure);
            double[] z = Components.Select(c => c.MolarFraction).ToArray();
            double beta = 0.5; // Suposición inicial de la fracción de vapor (V/F)

            while (error > tolerance && iter < maxIters)
            {
                // 2. Análisis de Estabilidad y Resolución de Rachford-Rice
                beta = SolveRachfordRice(z, K);

                // Actualizar el estado termodinámico de la corriente
                if (beta <= 0.0)
                {
                    beta = 0.0;
                    CurrentState = ThermodynamicState.SubcooledLiquid;
                }
                else if (beta >= 1.0)
                {
                    beta = 1.0;
                    CurrentState = ThermodynamicState.SuperheatedVapor;
                }
                else
                {
                    CurrentState = ThermodynamicState.VaporLiquidMixture;
                }

                VaporFraction = beta;

                // 3. Calcular nuevas composiciones (x_i, y_i)
                double[] x = new double[Components.Count];
                double[] y = new double[Components.Count];

                for (int i = 0; i < Components.Count; i++)
                {
                    x[i] = z[i] / (1.0 + beta * (K[i] - 1.0));
                    y[i] = K[i] * x[i];

                    // Pasamos los valores a los nodos de fase
                    LiquidPhase.Components[i].MolarFraction = x[i];
                    VaporPhase.Components[i].MolarFraction = y[i];
                }

                // Si es una fase única, no necesitamos recalcular K_i iterativamente
                if (CurrentState != ThermodynamicState.VaporLiquidMixture) break;

                // 4. Actualización Termodinámica Rigurosa (NRTL, SRK, PR)
                LiquidPhase.CalculateEquilibrium(temperature, pressure);
                VaporPhase.CalculateEquilibrium(temperature, pressure);

                double sumDeltaK = 0.0;

                for (int i = 0; i < Components.Count; i++)
                {
                    double num = LiquidPhase.Components[i].LiquidFugacityNumerator;
                    double den = VaporPhase.Components[i].VaporFugacityDenominator;

                    double newK = (den != 0) ? num / den : K[i];

                    // Medir el error relativo del K_i para la convergencia
                    sumDeltaK += Math.Abs((newK - K[i]) / K[i]);
                    K[i] = newK;
                }

                error = sumDeltaK;
                iter++;
            }

            // Normalización final por seguridad
            NormalizePhaseCompositions();
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
        private double[] EstimateInitialKValues(Temperature t, Pressure p)
        {
            double[] K = new double[Components.Count];
            double tK = t.GetValue(TemperatureUnits.Kelvin);
            double pKpa = p.GetValue(PressureUnits.KiloPascal);

            for (int i = 0; i < Components.Count; i++)
            {
                var compData = Components[i].PureComponentData;
                double tc = compData.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
                double pc = compData.CriticalPressure.GetValue(PressureUnits.KiloPascal);
                double omega = compData.AcentricFactor;

                // Correlación de Wilson para K_i inicial
                // K_i = (Pc_i / P) * exp[ 5.373 * (1 + ω_i) * (1 - Tc_i / T) ]
                double exponent = 5.373 * (1.0 + omega) * (1.0 - tc / tK);
                K[i] = (pc / pKpa) * Math.Exp(exponent);
            }

            return K;
        }

        private void NormalizePhaseCompositions()
        {
            double sumX = LiquidPhase.Components.Sum(c => c.MolarFraction);
            double sumY = VaporPhase.Components.Sum(c => c.MolarFraction);

            for (int i = 0; i < Components.Count; i++)
            {
                if (sumX > 0) LiquidPhase.Components[i].MolarFraction /= sumX;
                if (sumY > 0) VaporPhase.Components[i].MolarFraction /= sumY;
            }
        }

        private void CalculateMixtureMolecularWeight()
        {
            // Esto es termodinámicamente universal, sea líquido subenfriado, 
            // mezcla bifásica o vapor sobrecalentado.
            MolecularWeight = Components.Sum(c => c.MolarFraction * c.PureComponentData.MolecularWeight);
        }
        // ========================================================================
        // PROPIEDADES GLOBALES DE LA CORRIENTE (BULK PROPERTIES)
        // ========================================================================
        public void CalculateBulkProperties(Temperature _temperature,Pressure _pressure)
        {
            if (CurrentState == ThermodynamicState.Undefined) return;
            Temperature = _temperature;
            Pressure = _pressure;
            CalculateMixtureMolecularWeight();
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
            // 3. MEZCLA DE PROPIEDADES TERMODINÁMICAS (H, Cp, Densidad)
            // ====================================================================

            double hMolarMix = 0.0, hMassMix = 0.0;
            double cpMolarMix = 0.0, cpMassMix = 0.0;
            double vMolarMix = 0.0; // Volumen específico molar

            if (CurrentState == ThermodynamicState.SubcooledLiquid)
            {
                hMolarMix = LiquidPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                hMassMix = LiquidPhase.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg);
                cpMolarMix = LiquidPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C);
                cpMassMix = LiquidPhase.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C);
                vMolarMix = 1.0 / LiquidPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3);
            }
            else if (CurrentState == ThermodynamicState.SuperheatedVapor)
            {
                hMolarMix = VaporPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                hMassMix = VaporPhase.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg);
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

                cpMolarMix = (1.0 - VaporFraction) * LiquidPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C) +
                             VaporFraction * VaporPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C);
                cpMassMix = (1.0 - vaporMassFraction) * LiquidPhase.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C) +
                            vaporMassFraction * VaporPhase.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C);

                vMolarMix = (1.0 - VaporFraction) * (1.0 / LiquidPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3)) +
                            VaporFraction * (1.0 / VaporPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3));
            }

            MolarEnthalpy = new MolarEnergy(hMolarMix, MolarEnergyUnits.J_Kgmol);
            MassEnthalpy = new MassEnergy(hMassMix, MassEnergyUnits.J_Kg);
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
        // ========================================================================
        // FLASH ADIABÁTICO P-H (Válvulas, Mezcladores, Intercambiadores)
        // ========================================================================
        public void PerformFlashPH(Pressure targetPressure, MolarEnergy targetEnthalpy)
        {
            double hTargetJ = targetEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
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
                CalculateBulkProperties(Temperature, Pressure);

                double hCalc = MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                double cpMix = MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C) * 1000.0; // Convertir a J/(Kgmol·K)

                // Error actual
                error = hCalc - hTargetJ;

                if (Math.Abs(error) < tolerance) break;

                // 4. Newton-Raphson Termodinámico (ΔT = -ΔH / Cp)
                // Protegemos el Cp para evitar divisiones por cero
                if (cpMix <= 1e-6) cpMix = 1000.0; // Fallback de seguridad

                double deltaT = error / cpMix;

                // Clamping (Limitador de paso) para evitar que la T salte a valores negativos o locos
                // Máximo permitimos saltos de 50 grados por iteración
                deltaT = Math.Clamp(deltaT, -50.0, 50.0);

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
            Func<double, double> objFunc = (tK) => {
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
            Func<double, double> objFunc = (pKpa) => {
                var p = new Pressure(pKpa, PressureUnits.KiloPascal);
                return CalculateEquilibrium(T, p, targetVF);
            };

            var result = BisectionSolver.Solve(objFunc, pMin, pMax, (pMin + pMax) / 2.0);

            CurrentState = ThermodynamicState.VaporLiquidMixture;
            return result.Value;
        }

    }
}
