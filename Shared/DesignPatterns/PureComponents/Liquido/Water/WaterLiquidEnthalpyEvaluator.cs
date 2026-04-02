using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Water
{
    public class WaterLiquidEnthalpyEvaluator : IPropertyEvaluator<Temperature, MolarEnergy>
    {
        public MolarEnergy EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double hMolar = CPropiAgua.enthalpySatLiqTW(tempK) * 1000.0 * 18.01528;

            return new MolarEnergy(hMolar, MolarEnergyUnits.J_Kgmol);
        }
    }

}
