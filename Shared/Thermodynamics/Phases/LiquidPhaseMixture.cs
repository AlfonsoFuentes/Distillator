using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.Componentes;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.Phases
{
    public class LiquidPhaseMixture : Phase
    {
        public string Name { get; init; }
        public List<LiquidComponentNode> Components { get; } = new();
        public double[][,] ActivityMatrices { get; private set; } = Array.Empty<double[,]>();

        protected override IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation => Components;

        public LiquidPhaseMixture(string name = "Liquid Phase") => Name = name;

        public override void SetComponentsProperties(ThermodynamicMethodFullDto method)
        {
            Components.Clear();
            foreach (var dto in method.Components)
            {
                var node = new LiquidComponentNode();
                node.SetComponentData(PureComponentFactory.CreateFromDto(dto.FullData), method.LiquidModel, method.VaporModel);
                Components.Add(node);
            }
            ActivityMatrices = ActivityParameterFactory.BuildMatrices(method.LiquidModel, Components, method.BinaryParameters);
        }

        public void CalculateEquilibrium(Temperature temperature, Pressure pressure)
        {
            if (Components.Count == 0) return;
            foreach (var comp in Components) comp.CalculatePureProperties(temperature, pressure);

            // ✅ Delegación al Calculator
            ActivityCoefficientCalculator.Calculate(ThermoMethod.LiquidModel, Components, ActivityMatrices, temperature);

            foreach (var comp in Components) comp.CalculatePhaseFugacity();
        }

        public void CalculateBulkProperties(Temperature temperature, Pressure pressure)
        {
            CalculateEquilibrium(temperature, pressure);
            CalculateLiquidMixtureHeatCapacity();
            CalculateLiquidMixtureEnthalpy();
            CalculateLiquidMixtureDensity();
            CalculateLiquidMixtureThermalConductivity();
            CalculateLiquidMixtureViscosity();
            CalculateMixtureSurfaceTension();
        }

        // --- Métodos de propiedades de mezcla (Reglas de Mezcla) ---
        private void CalculateLiquidMixtureHeatCapacity()
        {
            double cpMixMolar = 0, mwMix = 0;
            foreach (var c in Components)
            {
                cpMixMolar += c.MolarFraction * c.PureComponentData.GetLiquidHeatCapacity(c.Temperature).GetValue(MolarEntropyUnits.KJ_Kgmol_C);
                mwMix += c.MolarFraction * c.PureComponentData.MolecularWeight;
            }
            MolarHeatCapacity = new MolarEntropy(cpMixMolar, MolarEntropyUnits.KJ_Kgmol_C);
            MassHeatCapacity = new MassEntropy(cpMixMolar / mwMix, MassEntropyUnits.KJ_Kg_C);
        }

        private void CalculateLiquidMixtureDensity()
        {
            double sumInvDens = 0, mwMix = 0;
            foreach (var c in Components)
            {
                double dMolar = c.PureComponentData.GetLiquidDensity(c.Temperature).GetValue(MolarDensityUnits.Kgmol_m3);

                double dMass = dMolar * c.PureComponentData.MolecularWeight;
                if (dMass > 0) sumInvDens += c.MassFraction / dMass;
                mwMix += c.MolarFraction * c.PureComponentData.MolecularWeight;
            }
            if (sumInvDens > 0)
            {
                double massDensityValue = 1.0 / sumInvDens;
                MassDensity = new MassDensity(massDensityValue, MassDensityUnits.Kg_m3);
                double molarDensityValue = massDensityValue / mwMix;
                MolarDensity = new MolarDensity(molarDensityValue, MolarDensityUnits.Kgmol_m3);
            }
        }

        private void CalculateLiquidMixtureEnthalpy()
        {
            double hMixMolar = 0;
             double mwMix = 0;  
            foreach (var c in Components)
            {
                hMixMolar += c.MolarFraction * c.PureComponentData.GetLiquidEnthalpy(c.Temperature).GetValue(MolarEnergyUnits.J_Kgmol);
                mwMix += c.MolarFraction * c.PureComponentData.MolecularWeight;
            }
               
            MolarEnthalpy = new MolarEnergy(hMixMolar, MolarEnergyUnits.J_Kgmol);
            MassEnthalpy = new MassEnergy(hMixMolar / mwMix, MassEnergyUnits.J_Kg);
        }

        private void CalculateLiquidMixtureThermalConductivity()
        {
            double kMix = 0;
            foreach (var c in Components)
                kMix += c.MolarFraction * c.PureComponentData.GetLiquidThermalConductivity(c.Temperature).GetValue(ThermalConductivityUnits.W_m_K);
            ThermalConductivity = new ThermalConductivity(kMix, ThermalConductivityUnits.W_m_K);
        }

        private void CalculateLiquidMixtureViscosity()
        {
            double sumCube = 0;
            foreach (var c in Components)
            {
                double v = c.PureComponentData.GetLiquidViscosity(c.Temperature).GetValue(ViscosityUnits.Pa_s);
                if (v > 0) sumCube += c.MolarFraction * Math.Pow(v, 1.0 / 3.0);
            }
            Viscosity = new Viscosity(Math.Pow(sumCube, 3.0), ViscosityUnits.Pa_s);
        }

        private void CalculateMixtureSurfaceTension()
        {
            // 1. Detectar si es una solución acuosa (Agua + al menos otro componente)
            bool isAqueousSolution = Components.Count > 1 &&
                                     Components.Any(c => c.PureComponentData.Name.Equals("Agua", StringComparison.OrdinalIgnoreCase) ||
                                                         c.PureComponentData.Name.Equals("Water", StringComparison.OrdinalIgnoreCase));

            if (isAqueousSolution)
            {
                CalculateAqueousSurfaceTension();
            }
            else
            {
                CalculateIdealSurfaceTension();
            }
        }

        private void CalculateAqueousSurfaceTension()
        {
            var water = Components.FirstOrDefault(c => c.PureComponentData.Name.Equals("Agua", StringComparison.OrdinalIgnoreCase) ||
                                                       c.PureComponentData.Name.Equals("Water", StringComparison.OrdinalIgnoreCase));

            var alcohol = Components.FirstOrDefault(c => c.PureComponentData.Family.Equals("Alcohol", StringComparison.OrdinalIgnoreCase));

            // Fallback: Si es acuosa pero no hay alcohol definido, aplicamos mezcla ideal
            if (water == null || alcohol == null)
            {
                CalculateIdealSurfaceTension();
                return;
            }

            double xa = water.MolarFraction;
            double xe = alcohol.MolarFraction;
            var temp = water.Temperature; // Asumimos equilibrio térmico, todos tienen la misma T

            // Casos límite: Si es agua o alcohol casi puro, tomamos la propiedad directa
            if (xa >= 0.9999)
            {
                SurfaceTension = water.PureComponentData.GetSurfaceTension(temp);
                return;
            }
            if (xe >= 0.9999)
            {
                SurfaceTension = alcohol.PureComponentData.GetSurfaceTension(temp);
                return;
            }

            // Obtener tensiones superficiales puras
            double ta = water.PureComponentData.GetSurfaceTension(temp).GetValue(SuperficialTensionUnits.N_m) * 1000.0;
            double te = alcohol.PureComponentData.GetSurfaceTension(temp).GetValue(SuperficialTensionUnits.N_m) * 1000.0;

            // Volúmenes molares (1 / Densidad Molar). En C++ usaban Kmol/m3, el inverso es m3/Kmol
            double vma = (1.0 / water.PureComponentData.GetLiquidDensity(temp).GetValue(MolarDensityUnits.Kgmol_m3)) ;
            double vme = (1.0 / alcohol.PureComponentData.GetLiquidDensity(temp).GetValue(MolarDensityUnits.Kgmol_m3)) ;

            int q = CountCarbonAtoms(alcohol.PureComponentData.StructuralFormula);
            if (q <= 0) q = 1; // Seguridad contra división por cero

            double tempK = temp.GetValue(TemperatureUnits.Kelvin);

            // --- 2. ECUACIÓN EMPÍRICA (La matemática queda intacta) ---
            double term1 = Math.Log(Math.Pow(xa * vma, 2.0) / (vme * xe) / ((xa * vma) + (xe * vme)));
            double term2 = (44.1 * q / tempK) * ((te / q) * Math.Pow(vme, 2.0 / 3.0) - ta * Math.Pow(vma, 2.0 / 3.0));

            double logphi = Math.Pow(10.0, term1 + term2);

            double phiagua = (-logphi + Math.Sqrt(Math.Pow(logphi, 2.0) + 4.0 * logphi)) / 2.0;
            double phietanol = 1.0 - phiagua;

            // Regla de Macleod-Sugden (El resultado sale en dyn/cm)
            double sigmaMix = Math.Pow((phiagua * Math.Pow(ta, 0.25)) + (phietanol * Math.Pow(te, 0.25)), 4.0);

            // 3. RETORNAR AL SISTEMA MODERNO DE TU ARQUITECTURA
            // Convertimos de vuelta de dyn/cm a N/m dividiendo por 1000
            SurfaceTension = new SuperficialTension(sigmaMix / 1000.0, SuperficialTensionUnits.N_m);
        }

        private void CalculateIdealSurfaceTension()
        {
            // Esta era tu lógica original
            double stMix = 0;
            foreach (var c in Components)
            {
                stMix += c.MolarFraction * c.PureComponentData.GetSurfaceTension(c.Temperature).GetValue(SuperficialTensionUnits.N_m);
            }
            SurfaceTension = new SuperficialTension(stMix, SuperficialTensionUnits.N_m);
        }

        // Helper químico
        private int CountCarbonAtoms(string structuralFormula)
        {
            if (string.IsNullOrWhiteSpace(structuralFormula)) return 0;
            return structuralFormula.Count(c => c == 'C');
        }

        public override void ClearComponentsProperties()
        {
            foreach (var component in Components)
            {
                component.ClearComponentData();
            }
            Components.Clear();
            ActivityMatrices = Array.Empty<double[,]>();
        }
    }
  
}
