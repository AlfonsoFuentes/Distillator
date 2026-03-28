using Shared.Calculator.Components;
using Shared.Thermodynamics.Enums;
using Shared.Thermodynamics.Methods;
using Shared.Thermodynamics.WaterProperties.Server.Thermodynamics.Engines;
using UnitSystem;

namespace Shared.Calculator.MaterialStreams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class VaporPhase : PhaseMixtureBase<VaporComponent>
    {
        public VaporPhase() : base()
        {
        }

        public override void SetMethod(ThermodynamicMethodFullDto thermoMethod)
        {
            ThermoMethod = thermoMethod ?? throw new ArgumentNullException(nameof(thermoMethod));

            Components.Clear();

            foreach (var methodComponent in ThermoMethod.Components)
            {
                var vaporComponent = new VaporComponent(
                    methodComponent.FullData,
                    ThermoMethod.LiquidModel,
                    ThermoMethod.VaporModel);

                Components.Add(vaporComponent);
            }

            InitializeKijMatrix(ThermoMethod.BinaryParameters);
        }

        protected override void CalculatePhaseInteractions(Amount temperature, Amount pressure)
        {
            CalculateMixtureFugacity(temperature, pressure);
        }

        protected override double SelectRoot(List<double> roots)
        {
            return roots.Any() ? roots.Max() : 1.0;
        }

        public  void CalculateBulkProperties()
        {
            if (Components is null or { Count: 0 })
                return;

            foreach (var component in Components)
            {
                component.CalculateIntensiveProperties();
            }

            CalculateGasMixtureHeatCapacity();
            CalculateGasMixtureEnthalpy();
            CalculateMixtureDensity();
            CalculateGasMixtureThermalConductivity();
            CalculateGasMixtureViscosity();
        }

        // =========================================================================
        // DENSIDAD DE MEZCLA DEL VAPOR (Ecuación de Estado / Z)
        // =========================================================================
        public void CalculateMixtureDensity()
        {
            if (Components is null || Components.Count == 0)
            {
                MassDensity.Reset();
                MolarDensity.Reset();
                return;
            }

            double tempK = Temperature.Data.GetValue(TemperatureUnits.Kelvin);
            double presKpa = Pressure.Data.GetValue(PressureUnits.KiloPascal);

            if (presKpa <= ThermodynamicConstants.MinPositiveValue || tempK <= ThermodynamicConstants.MinPositiveValue)
            {
                MassDensity.Reset();
                MolarDensity.Reset();
                return;
            }

            // 1. Calcular el Peso Molecular de la mezcla
            double mwMix = CalculateMixtureMolecularWeight();

            bool isPureWater = Components.Count == 1 &&
                               (Components[0].BaseProperties.Name.Equals("Agua", StringComparison.OrdinalIgnoreCase) ||
                                Components[0].BaseProperties.Name.Equals("Water", StringComparison.OrdinalIgnoreCase));

            // 2. Ruta de Cálculo de Volumen Molar (m3 / kmol)
            double vMolar = 0.0;
            string originRule = "EoS_Compressibility";

            if (isPureWater && VapourModel == VaporPhaseModel.IdealGas)
            {
                double presBar = Pressure.Data.GetValue(PressureUnits.Bar);
                double massDensityWater = CPropiAgua.densSatVapPW(presBar);

                if (massDensityWater > 0)
                {
                    vMolar = mwMix / massDensityWater;
                    originRule = "SteamTables";
                }
            }
            else
            {
                // Ley de los Gases Reales
                double z = CompressibilityFactor;
                vMolar = (z * ThermodynamicConstants.R_Gas * tempK) / presKpa;
            }

            // 3. Asignación de Resultados
            if (vMolar > 0)
            {
                SetDensityProperties(vMolar, mwMix);
            }
            else
            {
                MassDensity.Reset();
                MolarDensity.Reset();
            }
        }

        public void CalculateMixtureFugacity(Amount temperature, Amount pressure)
        {
            if (Components is null or { Count: 0 })
                return;

            if (VapourModel == VaporPhaseModel.IdealGas)
            {
                foreach (var component in Components)
                {
                    component.FugacityCoefficient = 1.0;
                }
                return;
            }

            if (EosParams.A <= ThermodynamicConstants.MinPositiveValue ||
                EosParams.B <= ThermodynamicConstants.MinPositiveValue)
            {
                return;
            }

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
        }

        private void CalculateComponentFugacityCoefficient(
            VaporComponent component,
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
            if (argLog1 <= ThermodynamicConstants.MinPositiveValue)
                argLog1 = ThermodynamicConstants.MinPositiveValue;

            double term2 = -Math.Log(argLog1);

            double sqrtTerm = Math.Sqrt(uParam * uParam - 4.0 * wParam);
            if (sqrtTerm <= ThermodynamicConstants.MinPositiveValue)
                sqrtTerm = ThermodynamicConstants.MinPositiveValue;

            double term3 = (aAsterisk / (bAsterisk * sqrtTerm)) * ((bComponent / bMix) - delta1);
            double term4 = 2.0 * compressibility + bAsterisk * (uParam + sqrtTerm);
            double term5 = 2.0 * compressibility + bAsterisk * (uParam - sqrtTerm);

            double lnPhi = 0.0;
            if (term4 > ThermodynamicConstants.MinPositiveValue &&
                term5 > ThermodynamicConstants.MinPositiveValue)
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
                double moleFractionJ = Components[j].MoleFraction;
                double aComponentJ = Components[j].EosParams.A;
                double kij = KijMatrix[componentIndex, j];

                partialSum += moleFractionJ * Math.Sqrt(aComponent * aComponentJ) * (1.0 - kij);
            }

            return partialSum;
        }
    }

    //public class VaporPhase2 : PhaseMixtureBase<VaporComponent>
    //{
    //    // =========================================================================
    //    // CONSTRUCTOR
    //    // =========================================================================
    //    public VaporPhase2() : base()
    //    {
    //    }

    //    // =========================================================================
    //    // CONFIGURACIÓN DEL MÉTODO TERMODINÁMICO
    //    // =========================================================================
    //    public override void SetMethod(ThermodynamicMethodFullDto thermoMethod)
    //    {
    //        ThermoMethod = thermoMethod ?? throw new ArgumentNullException(nameof(thermoMethod));

    //        Components.Clear();

    //        foreach (var methodComponent in ThermoMethod.Components)
    //        {
    //            var vaporComponent = new VaporComponent(
    //                methodComponent.FullData,
    //                ThermoMethod.LiquidModel,
    //                ThermoMethod.VaporModel);

    //            Components.Add(vaporComponent);
    //        }

    //        InitializeKijMatrix(ThermoMethod.BinaryParameters);
    //    }

    //    // =========================================================================
    //    // CÁLCULO DE INTERACCIONES DE FASE (Fugacidad de mezcla para vapor)
    //    // =========================================================================
    //    protected override void CalculatePhaseInteractions(Amount temperature, Amount pressure)
    //    {
    //        CalculateMixtureFugacity(temperature, pressure);
    //    }

    //    // =========================================================================
    //    // SELECCIÓN DE RAÍZ PARA EoS CÚBICA (VAPOR = Raíz MAYOR)
    //    // =========================================================================
    //    protected override double SelectRoot(List<double> roots)
    //    {
    //        return roots.Any() ? roots.Max() : 1.0;
    //    }

    //    // =========================================================================
    //    // ORQUESTADOR DE PROPIEDADES BULK (Llama a todos los calculadores)
    //    // =========================================================================
    //    public override void CalculateBulkProperties()
    //    {
    //        if (Components is null or { Count: 0 })
    //            return;
    //        foreach (var component in Components)
    //        {
    //            component.CalculateIntensiveProperties();
    //        }
    //        // 1. Propiedades térmicas (reglas heredadas de PhaseMixtureBase)
    //        CalculateGasMixtureHeatCapacity();
    //        CalculateGasMixtureEnthalpy();

    //        // 2. Propiedades específicas de vapor (termodinámica de gases reales)
    //        CalculateMixtureDensity();
    //        CalculateGasMixtureThermalConductivity();
    //        CalculateGasMixtureViscosity();
    //    }

    //    // =========================================================================
    //    // FUGACIDAD DE MEZCLA (Ecuación de Estado Cúbica - Riguroso)
    //    // =========================================================================
    //    public void CalculateMixtureFugacity(Amount temperature, Amount pressure)
    //    {
    //        if (Components is null or { Count: 0 })
    //            return;

    //        // Caso ideal: fugacidad = 1 para todos los componentes
    //        if (VapourModel == VaporPhaseModel.IdealGas)
    //        {
    //            foreach (var component in Components)
    //            {
    //                component.FugacityCoefficient = 1.0;
    //            }
    //            return;
    //        }

    //        // Validar parámetros de EoS antes de calcular
    //        if (EosParams.A <= ThermodynamicConstants.MinPositiveValue ||
    //            EosParams.B <= ThermodynamicConstants.MinPositiveValue)
    //        {
    //            return;
    //        }

    //        // Extraer parámetros de EoS para claridad
    //        double compressibility = CompressibilityFactor;
    //        double aMix = EosParams.A;
    //        double bMix = EosParams.B;
    //        double aAsterisk = EosParams.AAsterisk;
    //        double bAsterisk = EosParams.BAsterisk;
    //        double uParam = EosParams.U;
    //        double wParam = EosParams.W;

    //        // Calcular coeficiente de fugacidad para cada componente
    //        for (int i = 0; i < Components.Count; i++)
    //        {
    //            var component = Components[i];
    //            CalculateComponentFugacityCoefficient(
    //                component,
    //                i,
    //                compressibility,
    //                aMix,
    //                bMix,
    //                aAsterisk,
    //                bAsterisk,
    //                uParam,
    //                wParam);
    //        }
    //    }

    //    // =========================================================================
    //    // MÉTODO PRIVADO: Cálculo de fugacidad para un componente individual
    //    // =========================================================================
    //    private void CalculateComponentFugacityCoefficient(
    //        VaporComponent component,
    //        int componentIndex,
    //        double compressibility,
    //        double aMix,
    //        double bMix,
    //        double aAsterisk,
    //        double bAsterisk,
    //        double uParam,
    //        double wParam)
    //    {
    //        double bComponent = component.EosParams.B;
    //        double aComponent = component.EosParams.A;

    //        // Calcular sumatoria parcial para el parámetro 'a' de la mezcla
    //        double partialSumA = CalculatePartialSumA(componentIndex, aComponent);

    //        // Calcular delta1 (derivada parcial respecto a n_i)
    //        double delta1 = (2.0 * partialSumA) / aMix;

    //        // Calcular términos de la ecuación de fugacidad
    //        double term1 = (bComponent / bMix) * (compressibility - 1.0);

    //        double argLog1 = compressibility - bAsterisk;
    //        if (argLog1 <= ThermodynamicConstants.MinPositiveValue)
    //            argLog1 = ThermodynamicConstants.MinPositiveValue;

    //        double term2 = -Math.Log(argLog1);

    //        double sqrtTerm = Math.Sqrt(uParam * uParam - 4.0 * wParam);
    //        if (sqrtTerm <= ThermodynamicConstants.MinPositiveValue)
    //            sqrtTerm = ThermodynamicConstants.MinPositiveValue;

    //        double term3 = (aAsterisk / (bAsterisk * sqrtTerm)) * ((bComponent / bMix) - delta1);
    //        double term4 = 2.0 * compressibility + bAsterisk * (uParam + sqrtTerm);
    //        double term5 = 2.0 * compressibility + bAsterisk * (uParam - sqrtTerm);

    //        // Calcular ln(phi) con validación de dominio logarítmico
    //        double lnPhi = 0.0;
    //        if (term4 > ThermodynamicConstants.MinPositiveValue &&
    //            term5 > ThermodynamicConstants.MinPositiveValue)
    //        {
    //            lnPhi = term1 + term2 + term3 * Math.Log(term4 / term5);
    //        }

    //        // Guardar coeficiente de fugacidad de mezcla
    //        component.FugacityCoefficient = Math.Exp(lnPhi);
    //    }

    //    // =========================================================================
    //    // MÉTODO PRIVADO: Sumatoria parcial para parámetro 'a' de la mezcla
    //    // =========================================================================
    //    private double CalculatePartialSumA(int componentIndex, double aComponent)
    //    {
    //        double partialSum = 0.0;

    //        for (int j = 0; j < Components.Count; j++)
    //        {
    //            double moleFractionJ = Components[j].MoleFraction;
    //            double aComponentJ = Components[j].EosParams.A;
    //            double kij = KijMatrix[componentIndex, j];

    //            partialSum += moleFractionJ * Math.Sqrt(aComponent * aComponentJ) * (1.0 - kij);
    //        }

    //        return partialSum;
    //    }

       
       
    //}
   

}
