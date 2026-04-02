using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Water
{
    public class WaterHeatOfVaporizationEvaluator : IPropertyEvaluator<Temperature, MolarEnergy>
    {
        public MolarEnergy EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double mw = 18.01528;

            // kJ/kg - kJ/kg = kJ/kg
            double hVapor = CPropiAgua.enthalpySatVapTW(tempK);
            double hLiquido = CPropiAgua.enthalpySatLiqTW(tempK);

            // kJ/kg × kg/kmol = kJ/kmol × 1000 = J/kmol
            double hvapMolar = (hVapor - hLiquido) * mw * 1000.0;

            return new MolarEnergy(hvapMolar, MolarEnergyUnits.J_Kgmol);
        }
    }


}
