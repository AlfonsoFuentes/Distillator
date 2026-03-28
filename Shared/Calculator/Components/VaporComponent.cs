using Shared.Thermodynamics.Components;
using Shared.Thermodynamics.Enums;
using Shared.Thermodynamics.WaterProperties.Server.Thermodynamics.Engines;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;
namespace Shared.Calculator.Components
{
    public class VaporComponent : PhaseComponent
    {
        // =========================================================================
        // CONSTRUCTOR
        // =========================================================================
        public VaporComponent(ChemicalComponentDto dto, LiquidPhaseModel liquidmodel, VaporPhaseModel vapormodel)
            : base(dto, liquidmodel, vapormodel)
        {
        }

        // =========================================================================
        // 1. VOLUMEN MOLAR SATURADO (EoS - Raíz MAYOR)
        // =========================================================================
        public override Amount CalcSaturatedMolarVolume(Amount _Temperature)
        {
            double psatKpa = SaturationPressure.GetValue(PressureUnits.KiloPascal);
            double tKelvin = _Temperature.GetValue(TemperatureUnits.Kelvin);
            const double R_Gas = 8.314472;

            EosParameters paramSat = CreateEosParameters(_Temperature, SaturationPressure);
            List<double> raices = CubicSolver.Solve(paramSat.Factors);
            var validas = raices.Where(r => r > 0.0).ToList();

            double zVaporSat = validas.Any() ? validas.Max() : 1.0;

            double molarVolumeResult = (zVaporSat * R_Gas * tKelvin) / psatKpa;

            return new Amount(molarVolumeResult, MolarVolumeSpecificUnits.m3_Kgmol);
        }

        // =========================================================================
        // 2. PROPIEDADES VOLUMÉTRICAS
        // =========================================================================
        protected override void CalculateVolumetricProperties(Amount temperature)
        {
            MolarVolume = CalcSaturatedMolarVolume(temperature);
            MassVolume = CalcSaturatedMassVolume(MolarVolume);
        }

        // =========================================================================
        // 3. FUGACIDAD DE FASE (Denominador de K)
        // =========================================================================
        protected override void CalculatePhaseFugacity()
        {
            double phiV = FugacityCoefficient;
            double pKpa = Pressure.GetValue(PressureUnits.KiloPascal);

            VaporFugacityDenominator = phiV * pKpa;
        }

        // =========================================================================
        // 4. PROPIEDADES INTENSIVAS DEL VAPOR
        // =========================================================================
        public void CalculateIntensiveProperties()
        {
            CalculateGasDensity();
            CalculateGasHeatCapacity();
            CalculateHeatOfVaporization();
            CalculateGasViscosity();
            CalculateGasThermalConductivity();
          
            CalculateGasEnthalpy();
      
        }

       
       

        
    }
}
