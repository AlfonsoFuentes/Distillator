using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.PropertiesDtos.WaterProperties;
using Shared.Thermodynamics.Componentes;
using Shared.Thermodynamics.PureComponents;
using Shared.Thermodynamics.Solvers;
using UnitSystem;

namespace Shared.Thermodynamics.Phases
{
    public class VaporPhaseMixture : Phase
    {
        public string Name { get; init; }
        public List<VaporComponentNode> Components { get; } = new();
        public double[,] KijMatrix { get; private set; } = new double[0, 0];
        public EosParameters EosParams { get; private set; } = new();
        public double CompressibilityFactor { get; private set; } = 1.0;

        protected override IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation => Components;

        public VaporPhaseMixture(string name = "Vapor Phase") => Name = name;

        public override void SetComponentsProperties(ThermodynamicMethodFullDto method)
        {
            Components.Clear();
            foreach (var dto in method.Components)
            {
                var node = new VaporComponentNode();
                node.SetComponentData(PureComponentFactory.CreateFromDto(dto.FullData), method.LiquidModel, method.VaporModel);
                Components.Add(node);
            }

            // ✅ Inicializar Kij usando el Manager Híbrido que hicimos antes
            int n = Components.Count;
            KijMatrix = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    KijMatrix[i, j] = BinaryInteractionManager.GetKij(
                        Components[i].Id, Components[j].Id, Components[i].Name, Components[j].Name,
                        method.VaporModel, method.BinaryParameters);
        }

        public void CalculateEquilibrium(Temperature temperature, Pressure pressure)
        {
            if (Components.Count == 0) return;
            Temperature = temperature;
            Pressure = pressure;

            // 1. Pure Properties
            foreach (var comp in Components) comp.CalculateEquilibrium(temperature, pressure);

            if (ThermoMethod.VaporModel == VaporPhaseModel.IdealGas)
            {
                CompressibilityFactor = 1.0;
                foreach (var c in Components) c.FugacityCoefficient = 1.0;
            }
            else
            {
                // 2. ✅ Mezcla EoS
                EosParams = EosMixtureManager.CalculateMixtureParameters(Components, KijMatrix, temperature, pressure);

                // 3. Z-Factor
                var roots = CubicSolver.Solve(EosParams.Factors);
                CompressibilityFactor = roots.Any(r => r > 0) ? roots.Max() : 1.0;

                // 4. ✅ Fugacidad Parcial
                VaporFugacityCalculator.Calculate(Components, EosParams, KijMatrix, CompressibilityFactor);
            }

            // 5. Finalizar Denominador (φ_i * P)
            double pKpa = pressure.GetValue(PressureUnits.KiloPascala);
            foreach (var comp in Components)
                comp.VaporFugacityDenominator = comp.FugacityCoefficient * pKpa;
        }

        public override void ClearComponentsProperties()
        {
            foreach (var component in Components)
            {
                component.ClearComponentData();
            }
            Components.Clear();
            KijMatrix = new double[0, 0];
            EosParams = new EosParameters();
            CompressibilityFactor = 1.0;
        }

        public void CalculateBulkProperties(Temperature temperature, Pressure pressure)
        {
            if (Components is null || Components.Count == 0) return;

            CalculateEquilibrium(temperature, pressure);

            // 2. Densidad
            CalculateMixtureDensity(temperature, pressure);

            // 3. Propiedades de mezcla
            CalculateGasMixtureHeatCapacity();
            CalculateGasMixtureEnthalpy();
            CalculateGasMixtureEntropy(); // 👈 NUEVO: Llamada al cálculo de entropía
            CalculateGasMixtureThermalConductivity();
            CalculateGasMixtureViscosity();
        }

        private void CalculateMixtureDensity(Temperature temperature, Pressure pressure)
        {
            if (Components is null || Components.Count == 0) return;

            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double pKpa = pressure.GetValue(PressureUnits.KiloPascala);
            const double R_Gas = 8.314472;

            if (pKpa <= 0 || tempK <= 0) return;

            double mwMix = CalculateMixtureMolecularWeight();

            bool isPureWater = Components.Count == 1 &&
                (Components[0].Name.Equals("Agua", StringComparison.OrdinalIgnoreCase) ||
                 Components[0].Name.Equals("Water", StringComparison.OrdinalIgnoreCase));

            double vMolar = 0.0;

            if (isPureWater && ThermoMethod.VaporModel == VaporPhaseModel.IdealGas)
            {
                double pBar = pressure.GetValue(PressureUnits.Bara);
                double massDensityWater = CPropiAgua.densSatVapPW(pBar);
                if (massDensityWater > 0)
                    vMolar = mwMix / massDensityWater;
            }
            else
            {
                vMolar = (CompressibilityFactor * R_Gas * tempK) / pKpa;
            }

            if (vMolar > 0)
            {
                double massDensity = mwMix / vMolar;
                MassDensity = new MassDensity(massDensity, MassDensityUnits.Kg_m3);
                MolarDensity = new MolarDensity(1.0 / vMolar, MolarDensityUnits.Kgmol_m3);
            }
        }

        private double CalculateMixtureMolecularWeight()
        {
            double mwMix = 0.0;
            foreach (var component in Components)
            {
                mwMix += component.MolarFraction * component.PureComponentData.MolecularWeight;
            }
            return mwMix;
        }

        private void CalculateGasMixtureHeatCapacity()
        {
            if (Components is null || Components.Count == 0) return;

            double cpMixMolar = 0.0;

            foreach (var component in Components)
            {
                cpMixMolar += component.MolarFraction * component.PureComponentData.GetGasHeatCapacity(component.Temperature)
                    .GetValue(MolarEntropyUnits.KJ_Kgmol_C);
            }

            double mwMix = CalculateMixtureMolecularWeight();
            MolarHeatCapacity = new MolarEntropy(cpMixMolar, MolarEntropyUnits.KJ_Kgmol_C);
            MassHeatCapacity = new MassEntropy(cpMixMolar / mwMix, MassEntropyUnits.KJ_Kg_C);
        }

        // ========================================================================
        // ENTALPÍA DE MEZCLA
        // ========================================================================
        private void CalculateGasMixtureEnthalpy()
        {
            if (Components is null || Components.Count == 0) return;

            double hMixMolar = 0.0;

            foreach (var component in Components)
            {
                hMixMolar += component.MolarFraction * component.PureComponentData.GetGasEnthalpy(component.Temperature)
                    .GetValue(MolarEnergyUnits.J_Kgmol);
            }

            double mwMix = CalculateMixtureMolecularWeight();
            MolarEnthalpy = new MolarEnergy(hMixMolar, MolarEnergyUnits.J_Kgmol);
            MassEnthalpy = new MassEnergy(hMixMolar / mwMix, MassEnergyUnits.J_Kg);
        }

        // ========================================================================
        // NUEVO: ENTROPÍA DE MEZCLA GASEOSA
        // ========================================================================
        private void CalculateGasMixtureEntropy()
        {
            if (Components is null || Components.Count == 0) return;

            double sMixMolar = 0.0;
            const double R_Gas = 8314.462; // J/(Kgmol·K)

            foreach (var component in Components)
            {
                // Entropía del componente puro
                sMixMolar += component.MolarFraction * 1;// component.PureComponentData.GetGasEntropy(component.Temperature).GetValue(MolarEntropyUnits.J_Kgmol_C);

                // Entropía Ideal de Mezcla: -R * sum(y_i * ln(y_i))
                if (component.MolarFraction > 0)
                {
                    sMixMolar -= R_Gas * component.MolarFraction * Math.Log(component.MolarFraction);
                }
            }

            // Nota: Asumimos que GetGasEntropy ya incluye la corrección por presión (-R*ln(P/Pref)) 
            // internamente en los datos de DIPPR/PureComponentData. Si no lo incluye, habría que restarlo aquí.

            double mwMix = CalculateMixtureMolecularWeight();
            MolarEntropy = new MolarEntropy(sMixMolar, MolarEntropyUnits.J_Kgmol_C);
            MassEntropy = new MassEntropy(sMixMolar / mwMix, MassEntropyUnits.J_Kg_C);
        }

        // ========================================================================
        // CONDUCTIVIDAD TÉRMICA DE MEZCLA
        // ========================================================================
        private void CalculateGasMixtureThermalConductivity()
        {
            if (Components is null || Components.Count == 0) return;

            double kMix = 0.0;

            foreach (var component in Components)
            {
                kMix += component.MolarFraction * component.PureComponentData.GetGasThermalConductivity(component.Temperature)
                    .GetValue(ThermalConductivityUnits.W_m_K);
            }

            ThermalConductivity = new ThermalConductivity(kMix, ThermalConductivityUnits.W_m_K);
        }

        // ========================================================================
        // VISCOSIDAD DE MEZCLA
        // ========================================================================
        private void CalculateGasMixtureViscosity()
        {
            if (Components is null || Components.Count == 0) return;

            double sum = 0.0;

            foreach (var c in Components)
            {
                double viscPaS = c.PureComponentData
                    .GetGasViscosity(c.Temperature)
                    .GetValue(ViscosityUnits.Pa_s);

                double x = c.MolarFraction;

                double viscCubeRoot = Math.Pow(Math.Max(viscPaS, 0), 1.0 / 3.0);
                sum += viscCubeRoot * x;
            }

            double mixVisc = Math.Pow(sum, 3.0);

            Viscosity = new Viscosity(mixVisc, ViscosityUnits.Pa_s);
        }
    }
    public class VaporPhaseMixture2 : Phase
    {
        public string Name { get; init; }
        public List<VaporComponentNode> Components { get; } = new();
        public double[,] KijMatrix { get; private set; } = new double[0, 0];
        public EosParameters EosParams { get; private set; } = new();
        public double CompressibilityFactor { get; private set; } = 1.0;

        protected override IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation => Components;

        public VaporPhaseMixture2(string name = "Vapor Phase") => Name = name;

        public override void SetComponentsProperties(ThermodynamicMethodFullDto method)
        {
            Components.Clear();
            foreach (var dto in method.Components)
            {
                var node = new VaporComponentNode();
                node.SetComponentData(PureComponentFactory.CreateFromDto(dto.FullData), method.LiquidModel, method.VaporModel);
                Components.Add(node);
            }

            // ✅ Inicializar Kij usando el Manager Híbrido que hicimos antes
            int n = Components.Count;
            KijMatrix = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    KijMatrix[i, j] = BinaryInteractionManager.GetKij(
                        Components[i].Id, Components[j].Id, Components[i].Name, Components[j].Name,
                        method.VaporModel, method.BinaryParameters);
        }

        public void CalculateEquilibrium(Temperature temperature, Pressure pressure)
        {
            if (Components.Count == 0) return;
            Temperature = temperature; 
            Pressure = pressure;

            // 1. Pure Properties
            foreach (var comp in Components) comp.CalculateEquilibrium(temperature, pressure);

            if (ThermoMethod.VaporModel == VaporPhaseModel.IdealGas)
            {
                CompressibilityFactor = 1.0;
                foreach (var c in Components) c.FugacityCoefficient = 1.0;
            }
            else
            {
                // 2. ✅ Mezcla EoS
                EosParams = EosMixtureManager.CalculateMixtureParameters(Components, KijMatrix, temperature, pressure);

                // 3. Z-Factor
                var roots = CubicSolver.Solve(EosParams.Factors);
                CompressibilityFactor = roots.Any(r => r > 0) ? roots.Max() : 1.0;

                // 4. ✅ Fugacidad Parcial
                VaporFugacityCalculator.Calculate(Components, EosParams, KijMatrix, CompressibilityFactor);
            }

            // 5. Finalizar Denominador (φ_i * P)
            double pKpa = pressure.GetValue(PressureUnits.KiloPascala);
            foreach (var comp in Components)
                comp.VaporFugacityDenominator = comp.FugacityCoefficient * pKpa;
        }

        public override void ClearComponentsProperties()
        {
            foreach (var component in Components)
            {
                component.ClearComponentData();
            }
            Components.Clear();
            KijMatrix = new double[0, 0];
            EosParams = new EosParameters();
            CompressibilityFactor = 1.0;
        }
        public void CalculateBulkProperties(Temperature temperature, Pressure pressure)
        {
            if (Components is null || Components.Count == 0) return;

   
            CalculateEquilibrium(temperature, pressure);

            // 2. Densidad
            CalculateMixtureDensity(temperature, pressure);

            // 3. Propiedades de mezcla
            CalculateGasMixtureHeatCapacity();
            CalculateGasMixtureEnthalpy();
            CalculateGasMixtureThermalConductivity();
            CalculateGasMixtureViscosity();
        }
        private void CalculateMixtureDensity(Temperature temperature, Pressure pressure)
        {
            if (Components is null || Components.Count == 0) return;

            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double pKpa = pressure.GetValue(PressureUnits.KiloPascala);
            const double R_Gas = 8.314472;

            if (pKpa <= 0 || tempK <= 0) return;

            double mwMix = CalculateMixtureMolecularWeight();

            // Verificar si es agua pura con tablas de vapor
            bool isPureWater = Components.Count == 1 &&
                (Components[0].Name.Equals("Agua", StringComparison.OrdinalIgnoreCase) ||
                 Components[0].Name.Equals("Water", StringComparison.OrdinalIgnoreCase));

            double vMolar = 0.0;

            if (isPureWater && ThermoMethod.VaporModel == VaporPhaseModel.IdealGas)
            {
                double pBar = pressure.GetValue(PressureUnits.Bara);
                double massDensityWater = CPropiAgua.densSatVapPW(pBar);
                if (massDensityWater > 0)
                    vMolar = mwMix / massDensityWater;
            }
            else
            {
                vMolar = (CompressibilityFactor * R_Gas * tempK) / pKpa;
            }

            if (vMolar > 0)
            {
                double massDensity = mwMix / vMolar;
                MassDensity = new MassDensity(massDensity, MassDensityUnits.Kg_m3);
                MolarDensity = new MolarDensity(1.0 / vMolar, MolarDensityUnits.Kgmol_m3);
            }
        }
        private double CalculateMixtureMolecularWeight()
        {
            double mwMix = 0.0;
            foreach (var component in Components)
            {
                mwMix += component.MolarFraction * component.PureComponentData.MolecularWeight;
            }
            return mwMix;
        }
        private void CalculateGasMixtureHeatCapacity()
        {
            if (Components is null || Components.Count == 0) return;

            double cpMixMolar = 0.0;

            foreach (var component in Components)
            {
                cpMixMolar += component.MolarFraction * component.PureComponentData.GetGasHeatCapacity(component.Temperature)
                    .GetValue(MolarEntropyUnits.KJ_Kgmol_C);
            }

            double mwMix = CalculateMixtureMolecularWeight();
            MolarHeatCapacity = new MolarEntropy(cpMixMolar, MolarEntropyUnits.KJ_Kgmol_C);
            MassHeatCapacity = new MassEntropy(cpMixMolar / mwMix, MassEntropyUnits.KJ_Kg_C);
        }

        // ========================================================================
        // ENTALPÍA DE MEZCLA
        // ========================================================================
        private void CalculateGasMixtureEnthalpy()
        {
            if (Components is null || Components.Count == 0) return;

            double hMixMolar = 0.0;

            foreach (var component in Components)
            {
                // Esto ya incluye Cp_liq + dH_vap + Cp_gas internamente. ¡Es el valor real total!
                hMixMolar += component.MolarFraction * component.PureComponentData.GetGasEnthalpy(component.Temperature)
                    .GetValue(MolarEnergyUnits.J_Kgmol);
            }

            double mwMix = CalculateMixtureMolecularWeight();
            MolarEnthalpy = new MolarEnergy(hMixMolar, MolarEnergyUnits.J_Kgmol);
            MassEnthalpy = new MassEnergy(hMixMolar / mwMix, MassEnergyUnits.J_Kg);
        }

        // ========================================================================
        // CONDUCTIVIDAD TÉRMICA DE MEZCLA
        // ========================================================================
        private void CalculateGasMixtureThermalConductivity()
        {
            if (Components is null || Components.Count == 0) return;

            double kMix = 0.0;

            foreach (var component in Components)
            {
                kMix += component.MolarFraction * component.PureComponentData.GetGasThermalConductivity(component.Temperature)
                    .GetValue(ThermalConductivityUnits.W_m_K);
            }

            ThermalConductivity = new ThermalConductivity(kMix, ThermalConductivityUnits.W_m_K);
        }

        // ========================================================================
        // VISCOSIDAD DE MEZCLA
        // ========================================================================
        private void CalculateGasMixtureViscosity()
        {
            if (Components is null || Components.Count == 0) return;

            double sum = 0.0;

            foreach (var c in Components)
            {
                // 👇 Obtener viscosidad del componente puro en Pa·s
                double viscPaS = c.PureComponentData
                    .GetGasViscosity(c.Temperature)
                    .GetValue(ViscosityUnits.Pa_s);

                // 👇 Fracción molar (ya está en [0,1], no dividir por 100)
                double x = c.MolarFraction;

                // 👇 Regla de mezcla: suma de (x_i * μ_i^(1/3))
                // Usar Math.Max para evitar problemas con valores negativos muy pequeños
                double viscCubeRoot = Math.Pow(Math.Max(viscPaS, 0), 1.0 / 3.0);
                sum += viscCubeRoot * x;
            }

            // 👇 Elevar al cubo: μ_mix = (suma)³
            double mixVisc = Math.Pow(sum, 3.0);

            Viscosity = new Viscosity(mixVisc, ViscosityUnits.Pa_s);
        }
    }
    
}
