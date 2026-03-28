using Shared.Thermodynamics.Components;
using Shared.Thermodynamics.Enums;
using Shared.Thermodynamics.WaterProperties.Server.Thermodynamics.Engines;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.Calculator.Components
{
    public class LiquidComponent : PhaseComponent
    {
        // =========================================================================
        // PROPIEDADES ESPECÍFICAS DEL LÍQUIDO
        // =========================================================================
      
        public double ActivityCoefficient { get; set; }
        public double PureLiquidFugacity { get; private set; } = 0.0;

        // =========================================================================
        // CONSTRUCTOR
        // =========================================================================
        public LiquidComponent(ChemicalComponentDto dto, LiquidPhaseModel liquidmodel, VaporPhaseModel vapormodel)
            : base(dto, liquidmodel, vapormodel)
        {
            
            ActivityCoefficient = 1.0;
            PoyntingFactor = 1.0;
        }

        // =========================================================================
        // 1. PROPIEDADES VOLUMÉTRICAS
        // =========================================================================
        protected override void CalculateVolumetricProperties(Amount temperature)
        {
            MolarVolume = CalcSaturatedMolarVolume(temperature);
            MassVolume = CalcSaturatedMassVolume(MolarVolume);
        }

        // =========================================================================
        // 2. FUGACIDAD DE FASE
        // =========================================================================
        protected override void CalculatePhaseFugacity()
        {
            double phiSat = SaturationFugacityCoefficient;
            double psatKpa = SaturationPressure.GetValue(PressureUnits.KiloPascal);
            double poyn = PoyntingFactor;

            PureLiquidFugacity = phiSat * psatKpa * poyn;

            double gamma = ActivityCoefficient;
            LiquidFugacityNumerator = gamma * PureLiquidFugacity;
            RealFugacity = MoleFraction * gamma * PureLiquidFugacity;
        }

        // =========================================================================
        // 3. RECÁLCULO DE K DESPUÉS DE ACTIVIDAD
        // =========================================================================
        public void CalculateEquilibriumConstant()
        {
            LiquidFugacityNumerator = ActivityCoefficient * PureLiquidFugacity;
            RealFugacity = MoleFraction * ActivityCoefficient * PureLiquidFugacity;
        }

        // =========================================================================
        // 4. VOLUMEN MOLAR SATURADO (Rackett)
        // =========================================================================
        public override Amount CalcSaturatedMolarVolume(Amount _Temperature)
        {
            double pcKpa = BaseProperties.CriticalPressure.GetValue(PressureUnits.KiloPascal);
            double tcKelvin = BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
            double w = BaseProperties.AcentricFactor;

            double zRa = 0.29056 - 0.08775 * w;
            double tKelvin = _Temperature.GetValue(TemperatureUnits.Kelvin);
            double tr = tKelvin / tcKelvin;

            if (tr > 1.0) tr = 1.0;

            double exponente = 1.0 + Math.Pow(1.0 - tr, 0.2857);
            const double R_Gas = 8.314472;

            double molarVolumeResult = (R_Gas * tcKelvin / pcKpa) * Math.Pow(zRa, exponente);
            double mw = BaseProperties.MolecularWeight;

            return new Amount(molarVolumeResult, MolarVolumeSpecificUnits.m3_Kgmol);
        }

        // =========================================================================
        // 5. PROPIEDADES INTENSIVAS DEL LÍQUIDO
        // =========================================================================
        public void CalculateIntensiveProperties()
        {
            CalculateLiquidDensity();
            CalculateLiquidHeatCapacity();

            CalculateLiquidViscosity();
            CalculateLiquidThermalConductivity();
            CalculateSurfaceTension();
            CalculateLiquidEnthalpy();
        }

       

       

       

       

       

       
    }
}
