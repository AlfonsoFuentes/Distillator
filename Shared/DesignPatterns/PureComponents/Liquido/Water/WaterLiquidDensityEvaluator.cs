using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Water
{
    public class WaterLiquidDensityEvaluator : IPropertyEvaluator<Temperature, MolarDensity>
    {
        public MolarDensity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double densityMass = CPropiAgua.densSatLiqTW(tempK);  // kg/m³
            double densityMolar = densityMass / 18.01528;           // kmol/m³

            return new MolarDensity(densityMolar, MolarDensityUnits.Kgmol_m3);
        }
    }

}
