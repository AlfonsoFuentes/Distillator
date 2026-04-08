using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Others
{
    // ============================================================
    // LIQUID ENTHALPY
    // ============================================================
    public class LiquidEnthalpyEvaluator : IPropertyEvaluator<Temperature, MolarEnergy>
    {
        private readonly CorrelationCoefficientsDto _cpCoeffs;
        private readonly double _molecularWeight;
        private const double Tref = 273.15;  // 0°C referencia

        public LiquidEnthalpyEvaluator(CorrelationCoefficientsDto cpCoeffs, double mw)
        {
            _cpCoeffs = cpCoeffs;
            _molecularWeight = mw;
        }

        public MolarEnergy EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _cpCoeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _cpCoeffs.Tmax.GetValue(TemperatureUnits.Kelvin);
            double tCalc = Math.Clamp(tempK, tMin, tMax);

            // H = ∫[Tref→T] Cp dT
            double hMolar = IntegralCpL(tCalc) - IntegralCpL(Tref);

            return new MolarEnergy(hMolar, MolarEnergyUnits.J_Kgmol);
        }

        private double IntegralCpL(double t)
        {
            return _cpCoeffs.C1 * t
                + (_cpCoeffs.C2 / 2.0) * Math.Pow(t, 2)
                + (_cpCoeffs.C3 / 3.0) * Math.Pow(t, 3)
                + (_cpCoeffs.C4 / 4.0) * Math.Pow(t, 4)
                + (_cpCoeffs.C5 / 5.0) * Math.Pow(t, 5);
        }
    }

}
