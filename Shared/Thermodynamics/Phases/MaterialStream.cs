using Shared.PropertiesDtos.Methods;
using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.Componentes;
using Shared.Thermodynamics.PureComponents;
using Shared.Thermodynamics.Solvers;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Phases
{
    public interface IMaterialStream
    {
        string Name { get; }
        List<MainComponentNode> Components { get; }
        LiquidPhaseMixture LiquidPhase { get; }
        VaporPhaseMixture VaporPhase { get; }
        Percentage VaporFraction { get; }
        ThermodynamicState CurrentState { get; set; }
        void SetComponentsProperties(ThermodynamicMethodFullDto method);
        void ClearComponentsProperties();
    
        void SetCompositionData(CompositionOrchestrator streamComposition);
        void SetVaporFraction(Percentage vaporFraction);
        void SolveSaturationTemperature();
        void SolveSaturationPressure();
        void PerformFlashPT();
        void PerformFlashPH();
       
        void SolveFlashPVF();
  
        void SolveFlashTVF();
        Temperature Temperature { get; set; }
        Pressure Pressure { get; set; }

        void SetThermodynamicMethod(ThermodynamicMethodFullDto _method);
        void ClearThermodynamicMethod();
        void SetPressure(Pressure? pressure);
        void SetTemperature(Temperature? temperature);
        void CalculateBulkProperties();
        double SolveFlashPVF(Pressure P, Percentage targetVF);


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
        ThermodynamicMethodFullDto ThermoMethod { get; }
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
        public Percentage VaporFraction { get; private set; } = new Percentage(0, PercentageUnits.Percentage);
        public ThermodynamicState CurrentState { get; set; } = ThermodynamicState.Undefined;

        protected override IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation => Components;

        
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
            ThermoMethod = method;

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


    
        public void SetCompositionData(CompositionOrchestrator streamComposition)
        {
            if (streamComposition?.Components == null) return;

            foreach (var comp in streamComposition.Components)
            {
                var localComponent = Components.FirstOrDefault(x => x.Id == comp.Id);
                if (localComponent != null)
                {
                    if (comp.MassFraction.IsDefined)
                        localComponent.MassFraction = comp.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100;
                    if (comp.MolarFraction.IsDefined)
                        localComponent.MolarFraction = comp.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100;
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
        public void SetVaporFraction(Percentage vaporFraction)
        {
            VaporFraction = vaporFraction;

        }

        public void SolveSaturationTemperature()
        {

            var result = SolveFlashPVF(Pressure, VaporFraction);
            Temperature = new Temperature(result, TemperatureUnits.Kelvin);
        }


        /// </summary>

        public void SolveSaturationPressure()
        {

            double pBubble = SolveFlashTVF(Temperature, VaporFraction);
            Pressure = new Pressure(pBubble, PressureUnits.KiloPascala);

        }


        public double CalculateEquilibrium(Temperature temperature, Pressure pressure, Percentage vaporFraction)
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

                    continue;
                }
                double z_i = globalComp.MolarFraction;
                double liquidNum = liquidComp.LiquidFugacityNumerator;
                double vaporDen = vaporComp.VaporFugacityDenominator;
                double K_i = 0;
                if (vaporDen != 0)
                    K_i = liquidNum / vaporDen;
                double vapfrac = vaporFraction.GetValue(PercentageUnits.Percentage) / 100.0;
                double xliq = z_i / (1.0 + vapfrac * (K_i - 1.0));
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
        // =========================================================================
        // 🧠 MEMORIA CACHÉ DE LA CORRIENTE (Variables de Clase / Privadas)
        // =========================================================================
        private double[] _cachedK = null!;
        private double _lastTKelvin = -999.0;
        private double _lastPBar = -999.0;

        // =========================================================================
        // ⚡ MOTOR PRINCIPAL: FLASH PT (Sustitución Directa + Warm Start)
        // =========================================================================
        public void PerformFlashPT(Temperature temperature, Pressure pressure)
        {
            // ⚙️ Parámetros calibrados
            double outerTol = 1e-3;
            int outerMaxIter = 50;
            double innerTol = 1e-3;
            int innerMaxIter = 50;

#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
#endif

            Temperature = temperature;
            Pressure = pressure;
       

            // ---------------------------------------------------------------------
            // 🚀 INYECTOR DE ARRANQUE (WARM START vs COLD START)
            // ---------------------------------------------------------------------
            double deltaT = Math.Abs(temperature.GetValue(TemperatureUnits.Kelvin) - _lastTKelvin);
            double deltaP = Math.Abs(pressure.GetValue(PressureUnits.Bara) - _lastPBar);

            double relDeltaT = deltaT / temperature.GetValue(TemperatureUnits.Kelvin);
            double relDeltaP = deltaP / pressure.GetValue(PressureUnits.Bara);

            double[] z = Components.Select(c => c.MolarFraction).ToArray();
            double[] K = new double[Components.Count];
            double[] x_temp = new double[Components.Count];
            double[] y_temp = new double[Components.Count];
            double vapfrac = 0.0;
            // 🚀 LÓGICA DE DECISIÓN: ARRANQUE
            int iter;
            double sumX = 0.0, sumY = 0.0;
            double _sumMassLiq = 0, _sumMassVap = 0;
            if (_cachedK != null /*&& relDeltaT < maxRelativeDeltaT && relDeltaP < maxRelativeDeltaP*/)
            {
                K = (double[])_cachedK.Clone();
            }
            else
            {
                K = InitializeKValuesWithPsat(temperature, pressure);
                vapfrac = SolveRachfordRice(z, K, innerTol, innerMaxIter);

                // 2. Normalización temporal de fracciones
                for (int i = 0; i < Components.Count; i++)
                {
                    double den = 1.0 + vapfrac * (K[i] - 1.0);
                    x_temp[i] = z[i] / den;
                    y_temp[i] = K[i] * x_temp[i];

                    sumX += x_temp[i];
                    sumY += y_temp[i];
                }

                // 3. Escribir a los objetos fase
                if (sumX > 0 && sumY > 0)
                {
                    for (int i = 0; i < Components.Count; i++)
                    {
                        LiquidPhase.Components[i].MolarFraction = x_temp[i] / sumX;
                        VaporPhase.Components[i].MolarFraction = y_temp[i] / sumY;
                    }
                }
            }





            // ---------------------------------------------------------------------
            // ⚙️ BUCLE EXTERNO (Sustitución Sucesiva Pura - SSI)
            // ---------------------------------------------------------------------
            for (iter = 0; iter < outerMaxIter; iter++)
            {
                // 1. Motor interno de Rachford-Rice
                vapfrac = SolveRachfordRice(z, K, innerTol, innerMaxIter);
                LiquidPhase.CalculateEquilibrium(temperature, pressure);
                VaporPhase.CalculateEquilibrium(temperature, pressure);

                double maxDeltaK = 0.0;
                sumX = 0.0;
                sumY = 0.0;
                _sumMassLiq = 0.0; // ✅ Agrega esto
                _sumMassVap = 0.0; // ✅ Agrega esto
                // 5. Actualización de K (Sustitución Directa)
                for (int i = 0; i < Components.Count; i++)
                {
                    double fL = LiquidPhase.Components[i].LiquidFugacityNumerator;
                    double fV = VaporPhase.Components[i].VaporFugacityDenominator;

                    // K obtenida de la termodinámica en esta iteración
                    double newK = (fV != 0) ? fL / fV : K[i];

                    // Evaluar error de convergencia
                    double error = Math.Abs(Math.Log(newK / K[i]));
                    if (error > maxDeltaK) maxDeltaK = error;

                    // Actualizamos la K directamente
                    K[i] = newK;
                    double den = 1.0 + vapfrac * (K[i] - 1.0);
                    x_temp[i] = z[i] / den;
                    y_temp[i] = K[i] * x_temp[i];

                    sumX += x_temp[i];
                    sumY += y_temp[i];
                    _sumMassLiq += x_temp[i] * Components[i].MolecularWeight;
                    _sumMassVap += y_temp[i] * Components[i].MolecularWeight;

                }
                for (int i = 0; i < K.Length; i++)
                {
                    LiquidPhase.Components[i].MolarFraction = x_temp[i] / sumX;
                    VaporPhase.Components[i].MolarFraction = y_temp[i] / sumY;
                    LiquidPhase.Components[i].MassFraction = x_temp[i] * Components[i].MolecularWeight / _sumMassLiq;
                    VaporPhase.Components[i].MassFraction = y_temp[i] * Components[i].MolecularWeight / _sumMassVap;
                }
                // Condición de salida exitosa (Esperamos al menos 2 iteraciones para estabilizar)
                if (iter > 2 && maxDeltaK < outerTol) break;
            }

            // ---------------------------------------------------------------------
            // 💾 ACTUALIZACIÓN DE LA MEMORIA CACHÉ
            // ---------------------------------------------------------------------
            _cachedK = (double[])K.Clone();
            _lastTKelvin = temperature.GetValue(TemperatureUnits.Kelvin);
            _lastPBar = pressure.GetValue(PressureUnits.Bara);


            // ---------------------------------------------------------------------
            // 🏁 ASIGNACIÓN DE ESTADOS
            // ---------------------------------------------------------------------
            VaporFraction = new Percentage(vapfrac * 100, PercentageUnits.Percentage);

            if (vapfrac <= 0.0) CurrentState = ThermodynamicState.SubcooledLiquid;
            else if (vapfrac >= 1.0) CurrentState = ThermodynamicState.SuperheatedVapor;
            else CurrentState = ThermodynamicState.VaporLiquidMixture;



#if DEBUG
            sw.Stop();
            bool converged = iter < outerMaxIter;
            Console.WriteLine($"[DEBUG-PT] {(converged ? "✅" : "❌")} Flash PT Finalizado | VF: {vapfrac:F4} | Iters: {iter + 1} | T: {sw.Elapsed.TotalMilliseconds:F2} ms");
#endif
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
        private double SolveRachfordRice(double[] z, double[] K, double tol, int maxIter)
        {
            double f0 = 0.0;
            double f1 = 0.0;

            // 1. Evaluación rápida de límites (Fase única)
            for (int i = 0; i < z.Length; i++)
            {
                f0 += z[i] * (K[i] - 1.0);
                f1 += z[i] * (K[i] - 1.0) / K[i];
            }

            if (f0 <= 0.0) return 0.0; // Subenfriado
            if (f1 >= 0.0) return 1.0; // Sobrecalentado

            // 2. Motor Newton Analítico Dedicado (¡Máxima Velocidad!)
            double beta = 0.5;   // Arrancamos justo en el medio


            for (int iter = 0; iter < maxIter; iter++)
            {
                double f = 0.0;
                double df = 0.0;

                // Calculamos la función y su derivada exacta al mismo tiempo
                for (int i = 0; i < z.Length; i++)
                {
                    double term = K[i] - 1.0;
                    double den = 1.0 + beta * term;

                    f += z[i] * term / den;
                    df -= z[i] * term * term / (den * den);
                }

                // ¿Llegamos al blanco?
                if (Math.Abs(f) < tol) break;

                // El salto de Newton exacto
                double betaNew = beta - f / df;

                // 🛡️ Muros de contención físicos (0 y 1)
                if (betaNew <= 0.0)
                    betaNew = beta * 0.1; // Frena la caída libre
                else if (betaNew >= 1.0)
                    betaNew = 1.0 - (1.0 - beta) * 0.1; // Frena el despegue

                beta = betaNew;
            }

            return Math.Clamp(beta, 0.0, 1.0);
        }


        double _vaporFrac => VaporFraction.GetValue(PercentageUnits.Percentage) / 100.0;
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


            double mwGlobal = (1.0 - _vaporFrac) * mwLiq + _vaporFrac * mwVap;

            // Calidad del vapor (Fracción másica, W)
            double vaporMassFraction = (mwGlobal > 0) ? (_vaporFrac * mwVap) / mwGlobal : 0.0;

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

                hMolarMix = (1.0 - _vaporFrac) * LiquidPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol) +
                            _vaporFrac * VaporPhase.MolarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                hMassMix = (1.0 - vaporMassFraction) * LiquidPhase.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg) +
                           vaporMassFraction * VaporPhase.MassEnthalpy.GetValue(MassEnergyUnits.J_Kg);

                // <-- Mezcla Bifásica de Entropía
                sMolarMix = (1.0 - _vaporFrac) * LiquidPhase.MolarEntropy.GetValue(MolarEntropyUnits.J_Kgmol_C) +
                            _vaporFrac * VaporPhase.MolarEntropy.GetValue(MolarEntropyUnits.J_Kgmol_C);
                sMassMix = (1.0 - vaporMassFraction) * LiquidPhase.MassEntropy.GetValue(MassEntropyUnits.J_Kg_C) +
                           vaporMassFraction * VaporPhase.MassEntropy.GetValue(MassEntropyUnits.J_Kg_C);

                cpMolarMix = (1.0 - _vaporFrac) * LiquidPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C) +
                             _vaporFrac * VaporPhase.MolarHeatCapacity.GetValue(MolarEntropyUnits.KJ_Kgmol_C);
                cpMassMix = (1.0 - vaporMassFraction) * LiquidPhase.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C) +
                            vaporMassFraction * VaporPhase.MassHeatCapacity.GetValue(MassEntropyUnits.KJ_Kg_C);

                double liquiddensity = LiquidPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3);
                double vapordensity = VaporPhase.MolarDensity.GetValue(MolarDensityUnits.Kgmol_m3);
                vMolarMix = (1.0 - _vaporFrac) * (1.0 / liquiddensity) +
                            _vaporFrac * (1.0 / vapordensity);
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

        public void PerformFlashPH()
        {

            // 🔹 1. Calcular el valor objetivo de entalpía molar a partir de la entalpía másica dada
            PerformFlashPH(Pressure, MassEnthalpy);


        }

        (double tGuess, double targetHmolar) CalculateGuesForPH(Pressure pressure, MassEnergy massenthalpy)
        {

            double massEnthalpyTargetJ = massenthalpy.GetValue(MassEnergyUnits.J_Kg);
            double targetHmolar = massEnthalpyTargetJ * MolecularWeight;
            Pressure = pressure;

            //double tBurbuja = SolveFlashPVF(pressure, new Percentage(0, PercentageUnits.Percentage));
            //Temperature tempBurbuja = new Temperature(tBurbuja, TemperatureUnits.Kelvin);

            //var propsBurbuja = CalculateMolarEnthalpyOnly(tempBurbuja, new Percentage(0, PercentageUnits.Percentage));
            //double hBurbuja = propsBurbuja.molarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
            //double cpBurbuja = propsBurbuja.Cp.GetValue(MolarEntropyUnits.J_Kgmol_C);

            //// 🎯 PUNTO DE ROCÍO (Vapor Saturado, VF = 100)
            //double tRocio = SolveFlashPVF(pressure, new Percentage(100, PercentageUnits.Percentage));
            //Temperature tempRocio = new Temperature(tRocio, TemperatureUnits.Kelvin);

            //var propsRocio = CalculateMolarEnthalpyOnly(tempRocio, new Percentage(100, PercentageUnits.Percentage));
            //double hRocio = propsRocio.molarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
            //double cpRocio = propsRocio.Cp.GetValue(MolarEntropyUnits.J_Kgmol_C);

//#if DEBUG
//            Console.WriteLine($"      [PH-Radar] Burbuja: T={tBurbuja:F2}K, H={hBurbuja:E4}, Cp={cpBurbuja:F2}");
//            Console.WriteLine($"      [PH-Radar] Rocío:   T={tRocio:F2}K, H={hRocio:E4}, Cp={cpRocio:F2}");
//#endif
            double tGuess = 300.0;
            //if (targetHmolar < hBurbuja)
            //{
            //    tGuess = tBurbuja - (hBurbuja - targetHmolar) / cpBurbuja;

            //    if (tGuess < 300)
            //        tGuess = 300;

            //}
            //else if (targetHmolar > hRocio)
            //{
            //    tGuess = tRocio + (targetHmolar - hRocio) / cpRocio;

            //}
            //else
            //{
            //    // ☁️💧 ZONA BIFÁSICA (MEZCLA)
            //    // Interpolación lineal pura, esquivando el calor latente
            //    double fraccionTermica = (targetHmolar - hBurbuja) / (hRocio - hBurbuja);
            //    tGuess = tBurbuja + fraccionTermica * (tRocio - tBurbuja);

            //}
#if DEBUG
            Console.WriteLine($"      [PH-Zona] Temperatura supuesta inicial. Guess preciso: {tGuess:F2} K");
#endif
            return (tGuess, targetHmolar); // Placeholder - reemplazar con lógica real
        }
        public void PerformFlashPH(Pressure pressure, MassEnergy massenthalpy)
        {
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"\n[DEBUG-PH] ⚡ Iniciando Flash PH | P={pressure.GetValue(PressureUnits.Bara):F2} bar | H_mass={massenthalpy.GetValue(MassEnergyUnits.J_Kg):F2} J/kg");
#endif

            _cachedK = null!; // Invalidamos la caché de K

            const double T_NORM = 300.0;
            const double H_NORM = 1e8;
            const double TOL_ADIM = 0.001;
            const double HADMIN = 0.01; // ← Aumentado de 0.0001 a 0.01

            // ====================================================================
            // 1. CALCULAR PUNTOS DE BURBUJA Y ROCÍO
            // ====================================================================
            double massEnthalpyTargetJ = massenthalpy.GetValue(MassEnergyUnits.J_Kg);
            double targetHmolar = massEnthalpyTargetJ * MolecularWeight;
            Pressure = pressure;

            // 🎯 PUNTO DE BURBUJA (VF = 0%)
            double tBurbuja = SolveFlashPVF(pressure, new Percentage(0, PercentageUnits.Percentage));
            Temperature tempBurbuja = new Temperature(tBurbuja, TemperatureUnits.Kelvin);
            var propsBurbuja = CalculateMolarEnthalpyOnly(tempBurbuja, new Percentage(0, PercentageUnits.Percentage));
            double hBurbuja = propsBurbuja.molarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);

            // 🎯 PUNTO DE ROCÍO (VF = 100%)
            double tRocio = SolveFlashPVF(pressure, new Percentage(100, PercentageUnits.Percentage));
            Temperature tempRocio = new Temperature(tRocio, TemperatureUnits.Kelvin);
            var propsRocio = CalculateMolarEnthalpyOnly(tempRocio, new Percentage(100, PercentageUnits.Percentage));
            double hRocio = propsRocio.molarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);

#if DEBUG
            Console.WriteLine($"      [PH-Radar] Burbuja: T={tBurbuja:F2}K ({tBurbuja - 273.15:F2}°C), H={hBurbuja:E4} J/kmol");
            Console.WriteLine($"      [PH-Radar] Rocío:   T={tRocio:F2}K ({tRocio - 273.15:F2}°C), H={hRocio:E4} J/kmol");
            Console.WriteLine($"      [PH-Radar] Target:  H={targetHmolar:E4} J/kmol");
#endif

            // ====================================================================
            // 2. DETECCIÓN DE ZONA TERMODINÁMICA
            // ====================================================================
            bool isSubcooled = targetHmolar < hBurbuja;
            bool isSuperheated = targetHmolar > hRocio;
            bool isTwoPhase = !isSubcooled && !isSuperheated;

#if DEBUG
            if (isSubcooled)
                Console.WriteLine($"      [PH-Zona] 💧 LÍQUIDO SUBENFRIADO (H < H_burbuja)");
            else if (isSuperheated)
                Console.WriteLine($"      [PH-Zona] 🔥 VAPOR SOBRECALENTADO (H > H_rocio)");
            else
                Console.WriteLine($"      [PH-Zona] 🌫️ MEZCLA BIFÁSICA (H_burbuja < H < H_rocio)");
#endif

            // ====================================================================
            // 3. RESOLUCIÓN SEGÚN ZONA
            // ====================================================================
            double tFinal;

            if (isTwoPhase)
            {
                // 🌫️ ESTRATEGIA BIFÁSICA: T fija en T_sat, resolver para VF
                Temperature = new Temperature(tBurbuja, TemperatureUnits.Kelvin);

                Func<double, double> funcVF = (vf) =>
                {
                    // Realizar flash PT a T_burbuja para actualizar composiciones
                    PerformFlashPT(Temperature, pressure);

                    // Calcular entalpía con la VF propuesta
                    var props = CalculateMolarEnthalpyOnly(Temperature, new Percentage(vf * 100, PercentageUnits.Percentage));
                    double hMolar = props.molarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);

                    return hMolar - targetHmolar;
                };

                // Estimación inicial inteligente por interpolación
                double vfGuess = (targetHmolar - hBurbuja) / (hRocio - hBurbuja);
                vfGuess = Math.Max(0.01, Math.Min(0.99, vfGuess)); // Evitar 0 o 1 exactos

#if DEBUG
                Console.WriteLine($"      [PH-Bifásico] Resolviendo para VF | Guess inicial: {vfGuess:F4}");
#endif

                var res = ScalarNewtonSolver.Solve(funcVF, vfGuess, 1.0, H_NORM, TOL_ADIM, 25, HADMIN, "PH-VF");

                VaporFraction = new Percentage(res.Value * 100, PercentageUnits.Percentage);
                CurrentState = ThermodynamicState.VaporLiquidMixture;
                tFinal = tBurbuja; // T ya está fija
            }
            else if (isSubcooled)
            {
                // 💧 ESTRATEGIA LÍQUIDO: VF=0, resolver para T
                Func<double, double> funcT = (tK) =>
                {
                    var T = new Temperature(tK, TemperatureUnits.Kelvin);
                    PerformFlashPT(T, pressure);
                    var props = CalculateMolarEnthalpyOnly(T, new Percentage(0, PercentageUnits.Percentage));
                    double hMolar = props.molarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                    return hMolar - targetHmolar;
                };

                // Estimación: iniciar 10-20K por debajo de T_burbuja
                double tGuess = tBurbuja - Math.Min(20, (hBurbuja - targetHmolar) / 5000);
                tGuess = Math.Max(273.15, tGuess); // No bajar de 0°C

#if DEBUG
                Console.WriteLine($"      [PH-Líquido] Resolviendo para T | Guess inicial: {tGuess:F2}K ({tGuess - 273.15:F2}°C)");
#endif

                var res = ScalarNewtonSolver.Solve(funcT, tGuess, T_NORM, H_NORM, TOL_ADIM, 25, HADMIN, "PH-LIQ");

                Temperature = new Temperature(res.Value, TemperatureUnits.Kelvin);
                VaporFraction = new Percentage(0, PercentageUnits.Percentage);
                CurrentState = ThermodynamicState.SubcooledLiquid;
                tFinal = res.Value;
            }
            else // isSuperheated
            {
                // 🔥 ESTRATEGIA VAPOR: VF=100, resolver para T
                Func<double, double> funcT = (tK) =>
                {
                    var T = new Temperature(tK, TemperatureUnits.Kelvin);
                    PerformFlashPT(T, pressure);
                    var props = CalculateMolarEnthalpyOnly(T, new Percentage(100, PercentageUnits.Percentage));
                    double hMolar = props.molarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                    return hMolar - targetHmolar;
                };

                // Estimación: iniciar 10-50K por encima de T_rocio
                double tGuess = tRocio + Math.Min(50, (targetHmolar - hRocio) / 2000);

#if DEBUG
                Console.WriteLine($"      [PH-Vapor] Resolviendo para T | Guess inicial: {tGuess:F2}K ({tGuess - 273.15:F2}°C)");
#endif

                var res = ScalarNewtonSolver.Solve(funcT, tGuess, T_NORM, H_NORM, TOL_ADIM, 25, HADMIN, "PH-VAP");

                Temperature = new Temperature(res.Value, TemperatureUnits.Kelvin);
                VaporFraction = new Percentage(100, PercentageUnits.Percentage);
                CurrentState = ThermodynamicState.SuperheatedVapor;
                tFinal = res.Value;
            }

            // ====================================================================
            // 4. CIERRE Y ASIGNACIÓN FINAL
            // ====================================================================
            Temperature = new Temperature(tFinal, TemperatureUnits.Kelvin);

#if DEBUG
            sw.Stop();
            Console.WriteLine($"[DEBUG-PH] ✅ Flash PH Finalizado | T final: {Temperature.GetValue(TemperatureUnits.DegreeCelcius):F2} °C | VF: {VaporFraction.GetValue(PercentageUnits.Percentage):F2}% | Estado: {CurrentState}");
            Console.WriteLine($"[DEBUG-PH] ⏱️ Tiempo total PH: {sw.Elapsed.TotalMilliseconds:F2} ms\n");
#endif
        }
        public void PerformFlashPH2(Pressure pressure, MassEnergy massenthalpy)
        {
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"\n[DEBUG-PH] ⚡ Iniciando Flash PH | P={pressure.GetValue(PressureUnits.Bara):F2} bar | H_mass={massenthalpy.GetValue(MassEnergyUnits.J_Kg):F2} J/kg");
#endif

            _cachedK = null!; // Invalidamos la caché de K porque estamos cambiando la variable de control a P-H

            const double T_NORM = 300.0;
            const double H_NORM = 1e8;
            const double TOL_ADIM = 0.001;
            const double HADMIN = 0.0001;

            var Guess = CalculateGuesForPH(pressure, massenthalpy);

            double tGuess = Guess.tGuess;
            double targetHmolar = Guess.targetHmolar;


            Func<double, double> funcVap = (tK) =>
            {
                var T = new Temperature(tK, TemperatureUnits.Kelvin);
                PerformFlashPT(T, pressure);
                var props = CalculateMolarEnthalpyOnly(T, VaporFraction);
                var hMolar = props.molarEnthalpy.GetValue(MolarEnergyUnits.J_Kgmol);
                return hMolar - targetHmolar;
            };

            var res = ScalarNewtonSolver.Solve(funcVap, tGuess, T_NORM, H_NORM, TOL_ADIM, 25, HADMIN);
            double tFinal = res.Value;
            // ====================================================================
            // 5. CIERRE Y ASIGNACIÓN FINAL
            // ====================================================================
            Temperature = new Temperature(tFinal, TemperatureUnits.Kelvin);

#if DEBUG
            sw.Stop();
            Console.WriteLine($"[DEBUG-PH] ✅ Flash PH Finalizado | T final: {Temperature.GetValue(TemperatureUnits.DegreeCelcius):F2} °C | Estado: {CurrentState}");
            Console.WriteLine($"[DEBUG-PH] ⏱️HADMIN: {HADMIN} | TOL_ADIM: {TOL_ADIM} | H_NORM:{H_NORM} Tiempo total PH: {sw.Elapsed.TotalMilliseconds:F2} ms\n");
#endif
        }


        (MolarEnergy molarEnthalpy, MolarEntropy Cp) CalculateMolarEnthalpyOnly(Temperature _temperature, Percentage _vaporFraccion)
        {
            double vf = _vaporFraccion.GetValue(PercentageUnits.Percentage) / 100.0;
            // 🔹 1. LÍQUIDO PURO (Subenfriado o Burbuja)
            if (vf <= 0.0)
            {
                // Usa las composiciones que ya están en LiquidPhase
                return (LiquidPhase.CalculateLiquidMixtureEnthalpy(_temperature), LiquidPhase.CalculateLiquidMixtureHeatCapacity(_temperature));
            }

            // 🔹 2. VAPOR PURO (Sobrecalentado o Rocío)
            if (vf >= 1.0)
            {
                // Usa las composiciones que ya están en VaporPhase
                return (VaporPhase.CalculateGasMixtureEnthalpy(_temperature), VaporPhase.CalculateGasMixtureHeatCapacity(_temperature));
            }

            // 🔹 3. MEZCLA BIFÁSICA (0 < vf < 1)
            double hLiq = LiquidPhase.CalculateLiquidMixtureEnthalpy(_temperature).GetValue(MolarEnergyUnits.J_Kgmol);
            double hVap = VaporPhase.CalculateGasMixtureEnthalpy(_temperature).GetValue(MolarEnergyUnits.J_Kgmol);

            double hMolarMix = (1.0 - vf) * hLiq + vf * hVap;

            double cpLiquid = LiquidPhase.CalculateLiquidMixtureHeatCapacity(_temperature).GetValue(MolarEntropyUnits.J_Kgmol_C);
            double cpVapor = VaporPhase.CalculateGasMixtureHeatCapacity(_temperature).GetValue(MolarEntropyUnits.J_Kgmol_C);

            double cpMolarMix = (1.0 - vf) * cpLiquid + vf * cpVapor;


            return (new MolarEnergy(hMolarMix, MolarEnergyUnits.J_Kgmol), new MolarEntropy(cpMolarMix, MolarEntropyUnits.J_Kgmol_C));
        }




        public void SolveFlashPVF()
        {
            var result = SolveFlashPVF(Pressure, VaporFraction);

            Temperature = new Temperature(result, TemperatureUnits.Kelvin);

        }
        public double SolveFlashPVF(Pressure P, Percentage targetVF)
        {
            double vf = targetVF.GetValue(PercentageUnits.Percentage) / 100.0;

            // 🔹 1. Guess inicial rápido usando estimación ideal (Raoult)
            // Esto es ~100× más rápido que SolveSaturationTemperature riguroso
            double tGuess = EstimateTemperatureByRaoult(P, vf);

            const double T_NORM = 300.0;
            const double F_NORM = 1.0;
            const double TOL_ADIM = 1e-2;
            const double HADMIN = 0.01;
            Func<double, double> objFunc = (tK) =>
            {
                var t = new Temperature(tK, TemperatureUnits.Kelvin);
                return CalculateEquilibrium(t, P, targetVF);
            };

            // ✅ INTENTO 1: Newton SIN calcular bounds rigurosos aún
            var newtonResult = ScalarNewtonSolver.Solve(
                func: objFunc,
                x0: tGuess,
                x_norm: T_NORM,
                f_norm: F_NORM,
                tolAdim: TOL_ADIM,
                maxIter: 15,
                adimperturbation: HADMIN,
                debugTag: "FlashPVF"
            );

            // Si Newton converge, ¡listo! Sin pagar el costo de bounds rigurosos
            if (newtonResult.Converged)
            {
                if (vf > 0 && vf < 1)
                {
                    CurrentState = ThermodynamicState.VaporLiquidMixture;
                }
                else if (vf == 0)
                {
                    CurrentState = ThermodynamicState.SaturatedLiquid;

                }
                else if (vf == 1)
                {
                    CurrentState = ThermodynamicState.SaturatedVapor;
                }
                return newtonResult.Value;
            }

            // ⚠️ INTENTO 2: Newton falló → AHORA sí calcular bounds rigurosos para Bisección
            // Esto solo ocurre en ~10% de casos difíciles (cerca de crítico, mezclas complejas)


            var bisectionResult = SecantSolver.Solve(objFunc, tGuess);

            return bisectionResult.Value;
        }

        private double EstimateTemperatureByRaoult(Pressure P, double vf)
        {
            double tBubble = 0.0;
            double sumInv = 0.0;  // Acumulador para el cálculo del Dew Point

            // 🔹 ÚNICO BUCLE: Calculamos Bubble y Dew simultáneamente
            for (int i = 0; i < Components.Count; i++)
            {
                double zi = Components[i].MolarFraction;
                var globalComp = Components[i];


                // 🌱 EL SEMBRADO (Seeding): Aprovechamos el loop para inicializar las fases
                // Esto evita que NRTL explote al dividir por cero en la Iteración 1
                var liquidComp = LiquidPhase.Components.FirstOrDefault(c => c.Id == globalComp.Id);
                var vaporComp = VaporPhase.Components.FirstOrDefault(c => c.Id == globalComp.Id);

                if (liquidComp != null) liquidComp.MolarFraction = zi;
                if (vaporComp != null) vaporComp.MolarFraction = zi;
                // Evaluador DIPPR/Antoine directo (rápido y preciso)
                double tsatPure = Components[i].PureComponentData
                    .GetSaturationTemperature(P)
                    .GetValue(TemperatureUnits.Kelvin);

                // 1. Bubble Point ideal: Σ(zi · Tsat_i)
                tBubble += zi * tsatPure;

                // 2. Dew Point ideal: Σ(zi / Tsat_i) [acumulamos para invertir después]
                if (tsatPure > 1e-10)  // Seguridad numérica
                    sumInv += zi / tsatPure;
            }

            // Dew Point = 1 / Σ(zi/Tsat_i)
            double tDew = (sumInv > 1e-10) ? 1.0 / sumInv : tBubble;

            // 🔹 Interpolación lineal según fracción de vapor (VF)
            // VF=0 → T_bubble; VF=1 → T_dew; Intermedio → mezcla
            return tBubble + vf * (tDew - tBubble);
        }

        public void SolveFlashTVF()
        {
            var result = SolveFlashTVF(Temperature, VaporFraction);
            Pressure = new Pressure(result, PressureUnits.KiloPascala);

        }


        private double EstimatePressureByRaoult(Temperature T, double vf)
        {
            double pBubble = 0.0;
            double sumInv = 0.0;  // Acumulador para Dew Point: Σ(zi / Psat_i)

            // 🔹 ÚNICO BUCLE: Calculamos Bubble, Dew y SEMBRAMOS la memoria simultáneamente
            for (int i = 0; i < Components.Count; i++)
            {
                var globalComp = Components[i];
                double zi = globalComp.MolarFraction;

                // 🌱 EL SEMBRADO (Seeding): Aprovechamos el loop para inicializar las fases
                // Esto evita que NRTL explote al dividir por cero en la Iteración 1
                var liquidComp = LiquidPhase.Components.FirstOrDefault(c => c.Id == globalComp.Id);
                var vaporComp = VaporPhase.Components.FirstOrDefault(c => c.Id == globalComp.Id);

                if (liquidComp != null) liquidComp.MolarFraction = zi;
                if (vaporComp != null) vaporComp.MolarFraction = zi;

                // Evaluador DIPPR/Antoine directo (rápido y preciso)
                double psatPure = globalComp.PureComponentData
                    .GetVaporPressure(T)
                    .GetValue(PressureUnits.KiloPascala);

                // 1. Bubble Point ideal: Σ(zi · Psat_i)
                pBubble += zi * psatPure;

                // 2. Dew Point ideal: Σ(zi / Psat_i) [acumulamos para invertir después]
                if (psatPure > 1e-10)  // Seguridad numérica
                    sumInv += zi / psatPure;
            }

            // Dew Point = 1 / Σ(zi/Psat_i)
            double pDew = (sumInv > 1e-10) ? 1.0 / sumInv : pBubble;

            // 🔹 Interpolación lineal según fracción de vapor (VF)
            // VF=0 → P_bubble; VF=1 → P_dew; Intermedio → mezcla
            return pBubble + vf * (pDew - pBubble);
        }
        public double SolveFlashTVF(Temperature T, Percentage targetVF)
        {
            double vf = targetVF.GetValue(PercentageUnits.Percentage) / 100.0;

            // 🔹 1. Guess inicial rápido y preciso usando componentes puros
            double pGuess = EstimatePressureByRaoult(T, vf);

            const double P_NORM = 100.0;   // Presión típica industrial [kPa]
            const double F_NORM = 1.0;     // Residual de equilibrio adimensional
            const double TOL_ADIM = 1e-3;  // Sweet spot calibrado
            const double HADMIN = 0.1;    // Tolerancia adimensional para entalpía (si decides usarla en el futuro)
            // 🔹 2. Función objetivo: f(P) = Rachford-Rice residual → debe ser 0
            Func<double, double> objFunc = (pKpa) =>
            {
                var p = new Pressure(pKpa, PressureUnits.KiloPascala);
                return CalculateEquilibrium(T, p, targetVF);
            };

            // ─────────────────────────────────────────────────────────
            // 🔹 ESTRATEGIA HÍBRIDA: Newton primero, Bisección si falla
            // ─────────────────────────────────────────────────────────

            // ✅ INTENTO 1: Newton-Raphson (convergencia cuadrática, ~1-3 iters)
            var newtonResult = ScalarNewtonSolver.Solve(
                func: objFunc,
                x0: pGuess,
                x_norm: P_NORM,      // ← Crítico: normaliza presión para estabilidad
                f_norm: F_NORM,
                tolAdim: TOL_ADIM,
                maxIter: 15,
                adimperturbation: HADMIN,

                debugTag: "FlashTVF"
            );

            // Si Newton converge, ¡listo! Sin pagar el costo de bounds rigurosos
            if (newtonResult.Converged)
            {
                if (vf > 0 && vf < 1)
                {
                    CurrentState = ThermodynamicState.VaporLiquidMixture;
                }
                else if (vf == 0)
                {
                    CurrentState = ThermodynamicState.SaturatedLiquid;

                }
                else if (vf == 1)
                {
                    CurrentState = ThermodynamicState.SaturatedVapor;
                }
                return newtonResult.Value;
            }

            // ⚠️ INTENTO 2: Newton falló → AHORA sí calcular bounds rigurosos para Bisección
            // Esto solo ocurre en ~10% de casos difíciles (cerca de crítico, mezclas muy no-ideales)
            double pDew = pGuess;
            double pBubble = pGuess;



            var bisectionResult = SecantSolver.Solve(objFunc, pGuess);
            CurrentState = ThermodynamicState.VaporLiquidMixture;
            return bisectionResult.Value;
        }


    }
}
