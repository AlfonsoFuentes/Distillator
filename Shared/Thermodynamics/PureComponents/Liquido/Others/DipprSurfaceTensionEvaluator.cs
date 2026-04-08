using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Others
{
    public class DipprSurfaceTensionEvaluator : IPropertyEvaluator<Temperature, SuperficialTension>
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        private readonly Temperature _criticalTemperature;

        public DipprSurfaceTensionEvaluator(CorrelationCoefficientsDto coeffs, Temperature tc)
        {
            _coeffs = coeffs;
            _criticalTemperature = tc;
        }

        public SuperficialTension EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tcK = _criticalTemperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);

            if (tempK < tMin || tempK > tMax || tcK <= 0)
                return new SuperficialTension(0.0, SuperficialTensionUnits.N_m);

            double tr = tempK / tcK;

            double exponente = _coeffs.C2
                + _coeffs.C3 * tr
                + _coeffs.C4 * tr * tr
                + _coeffs.C5 * Math.Pow(tr, 3.0);

            double sigma = _coeffs.C1 * Math.Pow(1.0 - tr, exponente);

            return new SuperficialTension(sigma/1000, SuperficialTensionUnits.N_m);
        }
    }

}
