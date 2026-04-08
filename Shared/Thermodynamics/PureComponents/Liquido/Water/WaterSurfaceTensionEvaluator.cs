using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents;
using Shared.Thermodynamics.PureComponents.Liquido.Others;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Water
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
