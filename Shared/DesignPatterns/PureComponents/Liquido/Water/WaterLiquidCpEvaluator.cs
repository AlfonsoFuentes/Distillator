using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Water
{
    public class WaterLiquidCpEvaluator : IPropertyEvaluator<Temperature, MolarEntropy>
    {
        public MolarEntropy EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double cpMass = CPropiAgua.cpSatLiqTW(tempK);  // kJ/(kg·K)

            double cpMolar = cpMass * 18.01528;  // kJ/(kmol·K)

            return new MolarEntropy(cpMolar, MolarEntropyUnits.KJ_Kgmol_C);
        }
    }

}
