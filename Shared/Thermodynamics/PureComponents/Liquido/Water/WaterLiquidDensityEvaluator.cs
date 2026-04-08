using Shared.PropertiesDtos.WaterProperties;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Water
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
