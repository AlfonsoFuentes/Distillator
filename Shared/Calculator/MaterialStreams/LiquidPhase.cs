using Shared.Calculator.Components;
using Shared.Calculator.MaterialStreams;
using Shared.Thermodynamics.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using UnitSystem;
namespace Shared.Calculator.MaterialStreams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class LiquidPhase : PhaseMixtureBase<LiquidComponent>
    {
        public double[][,] ActivityMatrices { get; private set; } = Array.Empty<double[,]>();

        public LiquidPhase() : base()
        {
        }

        public override void SetMethod(ThermodynamicMethodFullDto thermoMethod)
        {
            ThermoMethod = thermoMethod ?? throw new ArgumentNullException(nameof(thermoMethod));

            Components.Clear();

            foreach (var methodComponent in ThermoMethod.Components)
            {
                var liquidComponent = new LiquidComponent(
                    methodComponent.FullData,
                    LiquidModel,
                    VapourModel);

                Components.Add(liquidComponent);
            }

            InitializeKijMatrix(ThermoMethod.BinaryParameters);

            ActivityMatrices = ActivityParameterFactory.BuildMatrices(
                LiquidModel,
                Components,
                ThermoMethod.BinaryParameters);
        }

        protected override void CalculatePhaseInteractions(Amount temperature, Amount pressure)
        {
            ActivityCoefficientCalculator.Calculate(
                LiquidModel,
                Components,
                ActivityMatrices,
                temperature);
        }

        protected override void UpdateEquilibriumConstants()
        {
            if (Components is null or { Count: 0 })
                return;

            foreach (var component in Components)
            {
                component.CalculateEquilibriumConstant();
            }
        }

        protected override double SelectRoot(List<double> roots)
        {
            return roots.Any() ? roots.Min() : 1.0;
        }

        public  void CalculateBulkProperties()
        {
            if (Components is null or { Count: 0 })
                return;

            foreach (var component in Components)
            {
                component.CalculateIntensiveProperties();
            }

            CalculateLiquidMixtureHeatCapacity();
            CalculateLiquidMixtureEnthalpy();
            CalculateLiquidMixtureDensity();
            CalculateLiquidMixtureThermalConductivity();
            CalculateLiquidMixtureViscosity();
            CalculateMixtureSurfaceTension();
        }

       
    }
    public class LiquidPhase2 : PhaseMixtureBase<LiquidComponent>
    {
        // =========================================================================
        // MATRICES DE ACTIVIDAD (Para modelos NRTL, Wilson, UNIQUAC)
        // =========================================================================
        public double[][,] ActivityMatrices { get; private set; } = Array.Empty<double[,]>();

        // =========================================================================
        // CONSTRUCTOR
        // =========================================================================
        public LiquidPhase2() : base()
        {
        }

        // =========================================================================
        // CONFIGURACIÓN DEL MÉTODO TERMODINÁMICO
        // =========================================================================
        public override void SetMethod(ThermodynamicMethodFullDto thermoMethod)
        {
            ThermoMethod = thermoMethod ?? throw new ArgumentNullException(nameof(thermoMethod));

            Components.Clear();

            foreach (var methodComponent in ThermoMethod.Components)
            {
                var liquidComponent = new LiquidComponent(
                    methodComponent.FullData,
                    LiquidModel,
                    VapourModel);

                Components.Add(liquidComponent);
            }

            InitializeKijMatrix(ThermoMethod.BinaryParameters);

            // Delegar la creación de matrices de actividad a la fábrica externa
            ActivityMatrices = ActivityParameterFactory.BuildMatrices(
                LiquidModel,
                Components,
                ThermoMethod.BinaryParameters);
        }

        // =========================================================================
        // CÁLCULO DE INTERACCIONES DE FASE (Actividad para líquidos)
        // =========================================================================
        protected override void CalculatePhaseInteractions(Amount temperature, Amount pressure)
        {
            ActivityCoefficientCalculator.Calculate(
                LiquidModel,
                Components,
                ActivityMatrices,
                temperature);
        }

        // =========================================================================
        // ACTUALIZACIÓN DE CONSTANTES DE EQUILIBRIO (K_i)
        // =========================================================================
        protected override void UpdateEquilibriumConstants()
        {
            if (Components is null or { Count: 0 })
                return;

            foreach (var component in Components)
            {
                component.CalculateEquilibriumConstant();
            }
        }

        // =========================================================================
        // SELECCIÓN DE RAÍZ PARA EoS CÚBICA (LÍQUIDO = Raíz MENOR)
        // =========================================================================
        protected override double SelectRoot(List<double> roots)
        {
            return roots.Any() ? roots.Min() : 1.0;
        }

        // =========================================================================
        // ORQUESTADOR DE PROPIEDADES BULK (Llama a todos los calculadores)
        // =========================================================================
        public  void CalculateBulkProperties()
        {
            if (Components is null or { Count: 0 })
                return;
            foreach (var component in Components)
            {
                component.CalculateIntensiveProperties();
            }
            // 1. Propiedades térmicas (reglas heredadas de PhaseMixtureBase)
            CalculateLiquidMixtureHeatCapacity();
            CalculateLiquidMixtureEnthalpy();

            // 2. Propiedades específicas de líquido (termodinámica de fluidos incompresibles)
            CalculateLiquidMixtureDensity();
            CalculateLiquidMixtureThermalConductivity();
            CalculateLiquidMixtureViscosity();
            CalculateMixtureSurfaceTension();
        }
        public new void CalculateMixtureSurfaceTension()
        {
            if (Components is null or { Count: 0 })
                return;

            bool hasWater = false;
            bool hasAlcohol = false;
            LiquidComponent waterComponent = null!;
            LiquidComponent alcoholComponent = null!;

            // Analizar topología de la mezcla
            foreach (LiquidComponent component in Components.OfType<LiquidComponent>().ToList())
            {
                string name = component.BaseProperties.Name.ToLower();
                string family = component.BaseProperties.Family?.ToLower() ?? string.Empty;

                if (name == "agua" || name == "water")
                {
                    hasWater = true;
                    waterComponent = component;
                }
                else if (family == "alcohol")
                {
                    hasAlcohol = true;
                    alcoholComponent = component;
                }
            }

            // Usar modelo avanzado SOLO si es mezcla binaria Agua-Alcohol
            if (hasWater && hasAlcohol && Components.Count == 2 &&
                waterComponent != null && alcoholComponent != null)
            {
                CalculateTamuraKurataSurfaceTension(waterComponent, alcoholComponent);
            }
            else
            {
                CalculateLinearSurfaceTension();
            }
        }
        // =========================================================================
        // CAPACIDAD CALORÍFICA DE MEZCLA (Cp - Regla lineal másica)
        // =========================================================================
        private void CalculateLinearSurfaceTension()
        {
            double surfaceTensionMix = 0.0;

            foreach (var component in Components)
            {
                double moleFraction = component.MoleFraction;
                double surfaceTensionComponent = component.SurfaceTension.GetValue(SurfaceTensionUnits.N_m);
                surfaceTensionMix += moleFraction * surfaceTensionComponent;
            }

            SurfaceTension.SetCalculatedValue(
                surfaceTensionMix,
                SurfaceTensionUnits.N_m,
                "LinearMolarRule");
        }

        // =========================================================================
        // MODELO DE TAMURA-KURATA (Soluciones acuosas de alcoholes)
        // =========================================================================
        private void CalculateTamuraKurataSurfaceTension(LiquidComponent water, LiquidComponent alcohol)
        {
            // Normalizar fracciones molares
            double totalMoles = water.MoleFraction + alcohol.MoleFraction;
            if (totalMoles <= 0)
                return;

            double moleFractionWater = water.MoleFraction / totalMoles;
            double moleFractionAlcohol = alcohol.MoleFraction / totalMoles;

            double surfaceTensionWater = water.SurfaceTension.GetValue(SurfaceTensionUnits.N_m);
            double surfaceTensionAlcohol = alcohol.SurfaceTension.GetValue(SurfaceTensionUnits.N_m);

            // Guard clause: componente puro (evita división por cero)
            if (moleFractionWater >= ThermodynamicConstants.PureComponentThreshold)
            {
                SurfaceTension.SetCalculatedValue(
                    surfaceTensionWater,
                    SurfaceTensionUnits.N_m,
                    "TamuraKurata");
                return;
            }

            if (moleFractionAlcohol >= ThermodynamicConstants.PureComponentThreshold)
            {
                SurfaceTension.SetCalculatedValue(
                    surfaceTensionAlcohol,
                    SurfaceTensionUnits.N_m,
                    "TamuraKurata");
                return;
            }

            // Extraer propiedades físicas
            double molarVolumeWater = water.MolarVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
            double molarVolumeAlcohol = alcohol.MolarVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
            double temperatureKelvin = Temperature.Data.GetValue(TemperatureUnits.Kelvin);

            // Contar átomos de carbono (evitar q=0)
            int carbonAtoms = CountCarbonAtoms(alcohol.BaseProperties.Formula);
            if (carbonAtoms == 0)
                carbonAtoms = 1;

            // Matemática rigurosa de Tamura-Kurata
            double numeratorLog = Math.Pow(moleFractionWater * molarVolumeWater, 2.0);
            double denominatorLog = (molarVolumeAlcohol * moleFractionAlcohol) *
                                    (moleFractionWater * molarVolumeWater +
                                     moleFractionAlcohol * molarVolumeAlcohol);

            double term1 = Math.Log(numeratorLog / denominatorLog);
            double term2 = (ThermodynamicConstants.TamuraKurataConstant * carbonAtoms / temperatureKelvin) *
                           ((surfaceTensionAlcohol / carbonAtoms) * Math.Pow(molarVolumeAlcohol, 2.0 / 3.0) -
                            surfaceTensionWater * Math.Pow(molarVolumeWater, 2.0 / 3.0));

            double logPhi = Math.Pow(10.0, term1 + term2);

            // Fracciones superficiales
            double phiWater = (-logPhi + Math.Sqrt(Math.Pow(logPhi, 2.0) + 4.0 * logPhi)) / 2.0;
            double phiAlcohol = 1.0 - phiWater;

            // Regla de mezcla final (MacLeod-Sugden modificada)
            double surfaceTensionMix = Math.Pow(
                phiWater * Math.Pow(surfaceTensionWater, ThermodynamicConstants.SurfaceTensionExponent) +
                phiAlcohol * Math.Pow(surfaceTensionAlcohol, ThermodynamicConstants.SurfaceTensionExponent),
                ThermodynamicConstants.SurfaceTensionPower);

            SurfaceTension.SetCalculatedValue(
                surfaceTensionMix,
                SurfaceTensionUnits.N_m,
                "TamuraKurata");
        }

        // =========================================================================
        // UTILIDAD: Conteo de átomos de carbono en fórmula molecular
        // =========================================================================
        private int CountCarbonAtoms(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return 0;

            int carbonCount = 0;

            foreach (char character in formula)
            {
                if (character == 'C')
                    carbonCount++;
            }

            return carbonCount;
        }

    }
}

// Archivo: ThermodynamicConstants.cs
