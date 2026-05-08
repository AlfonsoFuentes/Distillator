using Shared.PropertiesDtos.WaterProperties;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Water
{
    public class WaterLiquidEnthalpyEvaluator : IPropertyEvaluator<Temperature, MolarEnergy>
    {
        public MolarEnergy EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);

            double entalpiaSatLiq = CPropiAgua.enthalpySatLiqTW(tempK);
            double hMolar = entalpiaSatLiq * 1000.0 * 18.01528;

            return new MolarEnergy(hMolar, MolarEnergyUnits.J_Kgmol);
        }
    }

}
