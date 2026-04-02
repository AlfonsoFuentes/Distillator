using Shared.Calculator.Components;
using Shared.DesignPatterns.NewFolder;
using Shared.DesignPatterns.PureComponents;
using Shared.DesignPatterns.Thermodynamics.Componentes;
using Shared.Thermodynamics.Enums;
using Shared.Thermodynamics.Methods;
using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics.Phases
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
            double pKpa = pressure.GetValue(PressureUnits.KiloPascal);
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
            double pKpa = pressure.GetValue(PressureUnits.KiloPascal);
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
                double pBar = pressure.GetValue(PressureUnits.Bar);
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

            double num = 0.0;
            double den = 0.0;

            foreach (var c in Components)
            {
                double visc = c.PureComponentData.GetGasViscosity(c.Temperature).GetValue(ViscosityUnits.Pa_s);
                double sqrtMw = Math.Sqrt(c.PureComponentData.MolecularWeight);

                num += c.MolarFraction * visc * sqrtMw;
                den += c.MolarFraction * sqrtMw;
            }

            double mixVisc = (den > 0) ? (num / den) : 0;
            Viscosity = new Viscosity(mixVisc, ViscosityUnits.Pa_s);
        }
    }
    public class VaporPhaseMixture2 : Phase
    {
        // ========================================================================
        // PROPIEDADES
        // ========================================================================
        public string Name { get; init; }
        public List<VaporComponentNode> Components { get; } = new List<VaporComponentNode>();
        public double[,] KijMatrix { get; private set; } = new double[0, 0];
        public EosParameters EosParams { get; private set; } = new EosParameters();
        public double CompressibilityFactor { get; private set; } = 1.0;

        protected override IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation => Components;

        // ========================================================================
        // CONSTRUCTOR
        // ========================================================================
        public VaporPhaseMixture2(string name = "Vapor Phase")
        {
            Name = name;
        }

        // ========================================================================
        // INICIALIZACIÓN
        // ========================================================================
        public override void SetComponentsProperties(ThermodynamicMethodFullDto method)
        {
            Components.Clear();

            foreach (var componentDto in method.Components)
            {
                var newComponent = new VaporComponentNode();
                newComponent.SetComponentData(
                    PureComponentFactory.CreateFromDto(componentDto.FullData),
                    method.LiquidModel,
                    method.VaporModel
                );
                Components.Add(newComponent);
            }

            // Inicializar matriz Kij
            InitializeKijMatrix(method.BinaryParameters);
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

        protected override void ClearThermodynamicMethodInternal()
        {
            base.ClearThermodynamicMethodInternal();
            ClearComponentsProperties();
        }

        // ========================================================================
        // INICIALIZAR MATRIZ KIJ
        // ========================================================================
        private void InitializeKijMatrix(List<BinaryInteractionParameterDto> dbParams)
        {
            int n = Components.Count;
            KijMatrix = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        KijMatrix[i, j] = 0.0;
                    }
                    else
                    {
                        KijMatrix[i, j] = GetKij(
                            Components[i].Id,
                            Components[j].Id,
                            Components[i].Name,
                            Components[j].Name,
                            ThermoMethod.VaporModel);
                    }
                }
            }
        }

        private double GetKij(Guid compI_Id, Guid compJ_Id, string nameI, string nameJ, VaporPhaseModel eosModel)
        {
            if (compI_Id == compJ_Id) return 0.0;

            // Por ahora, usar hardcode (igual que el código viejo)
            // Después se puede mover a una clase separada
            switch (eosModel)
            {
                case VaporPhaseModel.PengRobinson:
                    return CalcularKij_PR_Hardcode(nameI, nameJ);
                case VaporPhaseModel.SoaveRedlichKwong1972:
                case VaporPhaseModel.SoaveRedlichKwong1984:
                case VaporPhaseModel.SoaveRedlichKwong1995:
                case VaporPhaseModel.RedlichKwong:
                    return CalcularKij_SRK_Hardcode(nameI, nameJ);
                default:
                    return 0.0;
            }
        }

        private double CalcularKij_PR_Hardcode(string comp1, string comp2)
        {
            comp1 = NormalizeName(comp1);
            comp2 = NormalizeName(comp2);

            // TODO: Implementar tablas hardcodeadas del código viejo
            // Por ahora retornar 0
            return 0.0;
        }

        private double CalcularKij_SRK_Hardcode(string comp1, string comp2)
        {
            comp1 = NormalizeName(comp1);
            comp2 = NormalizeName(comp2);

            // TODO: Implementar tablas hardcodeadas del código viejo
            // Por ahora retornar 0
            return 0.0;
        }

        private string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            return name.Trim().Replace(" ", "").Replace("-", "").ToLower();
        }

        // ========================================================================
        // CÁLCULO DE EQUILIBRIO
        // ========================================================================
        public void CalculateEquilibrium(Temperature temperature, Pressure pressure)
        {
            if (Components is null || Components.Count == 0) return;

            Temperature = temperature;
            Pressure = pressure;

            // 1. Calcular equilibrio de cada componente
            foreach (var component in Components)
            {
                component.CalculateEquilibrium(temperature, pressure);
            }

            // 2. Calcular parámetros EoS de la mezcla
            CalculateMixtureEosParameters(temperature, pressure);

            // 3. Calcular factor de compresibilidad de la mezcla
            CalculateMixtureCompressibilityFactor();

            // 4. Calcular fugacidades de componente
            CalculateMixtureFugacity();
        }

        // ========================================================================
        // PARÁMETROS EOS DE LA MEZCLA
        // ========================================================================
        private void CalculateMixtureEosParameters(Temperature temperature, Pressure pressure)
        {
            int n = Components.Count;
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double pKpa = pressure.GetValue(PressureUnits.KiloPascal);
            const double R_Gas = 8.314472;

            // Calcular a_mix y b_mix con reglas de mezcla
            double aMix = 0.0;
            double bMix = 0.0;

            for (int i = 0; i < n; i++)
            {
                double yi = Components[i].MolarFraction;
                double bi = Components[i].EosParams.B;
                bMix += yi * bi;

                for (int j = 0; j < n; j++)
                {
                    double yj = Components[j].MolarFraction;
                    double ai = Components[i].EosParams.A;
                    double aj = Components[j].EosParams.A;
                    double kij = KijMatrix[i, j];

                    aMix += yi * yj * Math.Sqrt(ai * aj) * (1.0 - kij);
                }
            }

            // Calcular parámetros adimensionales
            double aAsterisk = aMix * pKpa / Math.Pow(R_Gas * tempK, 2.0);
            double bAsterisk = bMix * pKpa / (R_Gas * tempK);

            // Obtener U y W del primer componente (todos deberían ser iguales)
            double uParam = Components.Count > 0 ? Components[0].EosParams.U : 0;
            double wParam = Components.Count > 0 ? Components[0].EosParams.W : 0;

            // Crear EosParameters de la mezcla
            EosParams = new EosParameters
            {
                A = aMix,
                B = bMix,
                AAsterisk = aAsterisk,
                BAsterisk = bAsterisk,
                U = uParam,
                W = wParam
            };

            // Calcular factores del polinomio cúbico
            EosParams.Factors[0] = 1.0;
            EosParams.Factors[1] = -(1.0 + bAsterisk - uParam * bAsterisk);
            EosParams.Factors[2] = aAsterisk + (wParam - uParam) * Math.Pow(bAsterisk, 2.0) - uParam * bAsterisk;
            EosParams.Factors[3] = -aAsterisk * bAsterisk - wParam * Math.Pow(bAsterisk, 2.0) - wParam * Math.Pow(bAsterisk, 3.0);
        }

        // ========================================================================
        // FACTOR DE COMPRESIBILIDAD DE LA MEZCLA
        // ========================================================================
        private void CalculateMixtureCompressibilityFactor()
        {
            if (ThermoMethod.VaporModel == VaporPhaseModel.IdealGas)
            {
                CompressibilityFactor = 1.0;
                return;
            }

            var raices = CubicSolver.Solve(EosParams.Factors);
            var validas = raices.Where(r => r > 0.0).ToList();
            CompressibilityFactor = validas.Any() ? validas.Max() : 1.0;
        }

        // ========================================================================
        // FUGACIDAD DE LA MEZCLA
        // ========================================================================
        private void CalculateMixtureFugacity()
        {
            if (Components is null || Components.Count == 0) return;

            if (ThermoMethod.VaporModel == VaporPhaseModel.IdealGas)
            {
                foreach (var component in Components)
                {
                    component.FugacityCoefficient = 1.0;
                    component.VaporFugacityDenominator = component.FugacityCoefficient * Pressure.GetValue(PressureUnits.KiloPascal);
                }
                return;
            }

            if (EosParams.A <= 0 || EosParams.B <= 0) return;

            double compressibility = CompressibilityFactor;
            double aMix = EosParams.A;
            double bMix = EosParams.B;
            double aAsterisk = EosParams.AAsterisk;
            double bAsterisk = EosParams.BAsterisk;
            double uParam = EosParams.U;
            double wParam = EosParams.W;

            for (int i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                CalculateComponentFugacityCoefficient(
                    component,
                    i,
                    compressibility,
                    aMix,
                    bMix,
                    aAsterisk,
                    bAsterisk,
                    uParam,
                    wParam);
            }

            // Calcular VaporFugacityDenominator = y_i × φ_i × P
            double pKpa = Pressure.GetValue(PressureUnits.KiloPascal);
            foreach (var component in Components)
            {
                component.VaporFugacityDenominator =  component.FugacityCoefficient * pKpa;
            }
        }

        private void CalculateComponentFugacityCoefficient(
            VaporComponentNode component,
            int componentIndex,
            double compressibility,
            double aMix,
            double bMix,
            double aAsterisk,
            double bAsterisk,
            double uParam,
            double wParam)
        {
            double bComponent = component.EosParams.B;
            double aComponent = component.EosParams.A;

            double partialSumA = CalculatePartialSumA(componentIndex, aComponent);
            double delta1 = (2.0 * partialSumA) / aMix;

            double term1 = (bComponent / bMix) * (compressibility - 1.0);

            double argLog1 = compressibility - bAsterisk;
            if (argLog1 <= 1e-10) argLog1 = 1e-10;

            double term2 = -Math.Log(argLog1);

            double sqrtTerm = Math.Sqrt(uParam * uParam - 4.0 * wParam);
            if (sqrtTerm <= 1e-10) sqrtTerm = 1e-10;

            double term3 = (aAsterisk / (bAsterisk * sqrtTerm)) * ((bComponent / bMix) - delta1);
            double term4 = 2.0 * compressibility + bAsterisk * (uParam + sqrtTerm);
            double term5 = 2.0 * compressibility + bAsterisk * (uParam - sqrtTerm);

            double lnPhi = 0.0;
            if (term4 > 1e-10 && term5 > 1e-10)
            {
                lnPhi = term1 + term2 + term3 * Math.Log(term4 / term5);
            }

            component.FugacityCoefficient = Math.Exp(lnPhi);
        }

        private double CalculatePartialSumA(int componentIndex, double aComponent)
        {
            double partialSum = 0.0;

            for (int j = 0; j < Components.Count; j++)
            {
                double moleFractionJ = Components[j].MolarFraction;
                double aComponentJ = Components[j].EosParams.A;
                double kij = KijMatrix[componentIndex, j];

                partialSum += moleFractionJ * Math.Sqrt(aComponent * aComponentJ) * (1.0 - kij);
            }

            return partialSum;
        }

        // ========================================================================
        // DENSIDAD DE MEZCLA
        // ========================================================================
        private void CalculateMixtureDensity(Temperature temperature, Pressure pressure)
        {
            if (Components is null || Components.Count == 0) return;

            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double pKpa = pressure.GetValue(PressureUnits.KiloPascal);
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
                double pBar = pressure.GetValue(PressureUnits.Bar);
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

        // ========================================================================
        // PROPIEDADES DE MEZCLA
        // ========================================================================
        public void CalculateBulkProperties(Temperature temperature, Pressure pressure)
        {
            if (Components is null || Components.Count == 0) return;

            // 1. Calcular equilibrio
            CalculateEquilibrium(temperature, pressure);

            // 2. Densidad
            CalculateMixtureDensity(temperature, pressure);

            // 3. Propiedades de mezcla
            CalculateGasMixtureHeatCapacity();
            CalculateGasMixtureEnthalpy();
            CalculateGasMixtureThermalConductivity();
            CalculateGasMixtureViscosity();
        }

        // ========================================================================
        // CP DE MEZCLA
        // ========================================================================
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

            double sumCubeRoot = 0.0;

            foreach (var component in Components)
            {
                double visc = component.PureComponentData.GetGasViscosity(component.Temperature)
                    .GetValue(ViscosityUnits.Pa_s);
                if (visc > 0)
                {
                    sumCubeRoot += component.MolarFraction * Math.Pow(visc, 1.0 / 3.0);
                }
            }

            Viscosity = new Viscosity(Math.Pow(sumCubeRoot, 3.0), ViscosityUnits.Pa_s);
        }
    }
}
