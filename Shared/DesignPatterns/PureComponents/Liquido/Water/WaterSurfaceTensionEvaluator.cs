using Shared.DesignPatterns.PureComponents.Liquido.Others;
using Shared.Thermodynamics.Components;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Water
{
    public class WaterSurfaceTensionEvaluator : IPropertyEvaluator<Temperature, SuperficialTension>
    {
        // Usa la misma DIPPR 106 con coeficientes específicos para agua
        private readonly DipprSurfaceTensionEvaluator _dipprEvaluator;

        public WaterSurfaceTensionEvaluator(CorrelationCoefficientsDto coeffs, Temperature tc)
        {
            _dipprEvaluator = new DipprSurfaceTensionEvaluator(coeffs, tc);
        }

        public SuperficialTension EvaluateAt(Temperature temperature)
        {
            return _dipprEvaluator.EvaluateAt(temperature);
        }
    }

}
