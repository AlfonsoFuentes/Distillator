using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Gas.Water
{
    public class WaterGasCpEvaluator : IPropertyEvaluator<Temperature, MolarEntropy>
    {
        public MolarEntropy EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);

            // cpSatVapTW retorna kJ/(kg·K)
            double cpMass = CPropiAgua.cpSatVapTW(tempK);

            // kJ/(kg·K) × 18.01528 kg/kmol = kJ/(kmol·K)
            double cpMolar = cpMass * 18.01528;

            return new MolarEntropy(cpMolar, MolarEntropyUnits.KJ_Kgmol_C);
        }
    }


}
