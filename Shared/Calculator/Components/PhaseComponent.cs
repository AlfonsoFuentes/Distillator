using Shared.Thermodynamics.Components;
using Shared.Thermodynamics.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.Calculator.Components
{
    public abstract class PhaseComponent : StreamComponent
    {
        // =========================================================================
        // PROPIEDADES TERMODINÁMICAS (Equilibrio)
        // =========================================================================
        public double PoyntingFactor { get; set; } = 1.0;
        public double SaturationFugacityCoefficient { get; protected set; } = 1.0;
       
        public double FugacityCoefficient { get; set; }
        public double RealFugacity { get; set; }

        // ✅ Numerador y denominador de K_i
        public double LiquidFugacityNumerator { get; set; }
        public double VaporFugacityDenominator { get; set; }

        // =========================================================================
        // MODELOS TERMODINÁMICOS
        // =========================================================================
        public LiquidPhaseModel LiquidModel { get; protected set; }
        public VaporPhaseModel VaporModel { get; protected set; }

        // =========================================================================
        // CONSTRUCTOR
        // =========================================================================
        protected PhaseComponent(ChemicalComponentDto dto, LiquidPhaseModel liquidmodel, VaporPhaseModel vapormodel)
            : base(dto)
        {
            LiquidModel = liquidmodel;
            VaporModel = vapormodel;

            CompressibilityFactor = 1.0;
            FugacityCoefficient = 1.0;
            RealFugacity = 0.0;
            LiquidFugacityNumerator = 0.0;
            VaporFugacityDenominator = 0.0;
            PoyntingFactor = 1.0;
            SaturationFugacityCoefficient = 1.0;
        }

        // =========================================================================
        // PARÁMETROS EoS PARA COMPONENTE PURO
        // =========================================================================
        protected EosParameters CreateEosParameters(Amount _Temperature, Amount _Pressure)
        {
            double pcKpa = BaseProperties.CriticalPressure.GetValue(PressureUnits.KiloPascal);
            double tcKelvin = BaseProperties.CriticalTemperature.GetValue(TemperatureUnits.Kelvin);
            double w = BaseProperties.AcentricFactor;

            double pKpa = _Pressure.GetValue(PressureUnits.KiloPascal);
            double tKelvin = _Temperature.GetValue(TemperatureUnits.Kelvin);

            return EosParameterFactory.CreateForPureComponent(
                VaporModel,
                tcKelvin,
                pcKpa,
                w,
                tKelvin,
                pKpa);
        }

        // =========================================================================
        // COEFICIENTE DE FUGACIDAD (EoS)
        // =========================================================================
        public double CalculateFugacityCoefficient(Amount _Temperature, Amount _Pressure, bool isSaturationCalc)
        {
            if (VaporModel == VaporPhaseModel.IdealGas)
            {
                if (!isSaturationCalc) CompressibilityFactor = 1.0;
                return 1.0;
            }

            EosParameters parametros = CreateEosParameters(_Temperature, _Pressure);
            List<double> raices = CubicSolver.Solve(parametros.Factors);
            var validas = raices.Where(r => r > 0.0).ToList();
            double z = SelectRoot(validas);

            if (!isSaturationCalc)
            {
                CompressibilityFactor = z;
            }

            double valor1 = z - 1.0;
            double argLog1 = z - parametros.BAsterisk;
            if (argLog1 <= 0) argLog1 = 1e-10;
            double valor2 = -Math.Log(argLog1);
            double valor3 = Math.Sqrt(Math.Pow(parametros.U, 2.0) - 4.0 * parametros.W);
            if (valor3 == 0) valor3 = 1e-10;
            double valor4 = parametros.AAsterisk / (parametros.BAsterisk * valor3);
            double valor5 = 2.0 * z + parametros.BAsterisk * (parametros.U + valor3);
            double valor6 = 2.0 * z + parametros.BAsterisk * (parametros.U - valor3);
            double valor7 = 0;

            if (valor5 > 0 && valor6 > 0)
            {
                valor7 = valor1 + valor2 - valor4 * Math.Log(valor5 / valor6);
            }

            return Math.Exp(valor7);
        }

        protected virtual double SelectRoot(List<double> roots)
        {
            return roots.Any() ? roots.Min() : 1.0;
        }

        // =========================================================================
        // FACTOR DE POYNTING
        // =========================================================================
        public double CalcPoyntingFactor(Amount _Temperature, Amount _Pressure)
        {
            var _PoyntingFactor = 1.0;

            if (VaporModel == VaporPhaseModel.IdealGas)
            {
                return _PoyntingFactor;
            }

            double psKpa = SaturationPressure.GetValue(PressureUnits.KiloPascal);
            double pKpa = _Pressure.GetValue(PressureUnits.KiloPascal);
            double vMolar = MolarVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
            double tKelvin = _Temperature.GetValue(TemperatureUnits.Kelvin);

            if (tKelvin <= 0) tKelvin = 298.15;

            const double R_Gas = 8.314472;
            double exponente = (vMolar * (pKpa - psKpa)) / (R_Gas * tKelvin);
            _PoyntingFactor = Math.Exp(exponente);
            return _PoyntingFactor;
        }

        // =========================================================================
        // VOLUMEN MOLAR SATURADO (Abstracto - Lo implementa cada fase)
        // =========================================================================
        public abstract Amount CalcSaturatedMolarVolume(Amount _temperature);

        public Amount CalcSaturatedMassVolume(Amount _molarVolume)
        {
            double mw = BaseProperties.MolecularWeight;
            var molarVolumeResult = _molarVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
            double massVolumeResult = molarVolumeResult / mw;

            return new Amount(massVolumeResult, MassVolumeSpecificUnits.m3_Kg);
        }

        // =========================================================================
        // CALCULAR T, P (Método Principal - NO TOCAR)
        // =========================================================================
        public void CalculateTP(Amount _temperature, Amount _pressure)
        {
            // 1. Asignar estado del sistema
            Temperature = _temperature;
            Pressure = _pressure;

            // 2. Calcular límites de saturación (Común)
            SaturationPressure = GetSaturedPressureAtTemperature(_temperature);
            SaturationTemperature = GetSaturedTemperatureAtPressure(_pressure);

            // 3. Calcular volumen y densidad (DELEGADO A LA ESTRATEGIA DE FASE)
            CalculateVolumetricProperties(_temperature);

            // 4. Parámetros termodinámicos base (Común)
            PoyntingFactor = CalcPoyntingFactor(_temperature, _pressure);
            EosParams = CreateEosParameters(_temperature, _pressure);

            // 5. Coeficientes de Fugacidad EOS (Común)
            SaturationFugacityCoefficient = CalculateFugacityCoefficient(_temperature, SaturationPressure, isSaturationCalc: true);
            FugacityCoefficient = CalculateFugacityCoefficient(_temperature, _pressure, isSaturationCalc: false);

            // 6. Fugacidad efectiva para el equilibrio (DELEGADO A LA ESTRATEGIA DE FASE)
            CalculatePhaseFugacity();
        }

        // =========================================================================
        // CONTRATOS PARA LAS CLASES HIJAS
        // =========================================================================
        protected abstract void CalculateVolumetricProperties(Amount temperature);
        protected abstract void CalculatePhaseFugacity();
    }
}
