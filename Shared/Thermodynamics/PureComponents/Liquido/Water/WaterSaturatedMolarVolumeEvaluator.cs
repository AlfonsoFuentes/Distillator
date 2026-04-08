using Shared.PropertiesDtos.WaterProperties;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Water
{
    public class WaterSaturatedMolarVolumeEvaluator : IPropertyEvaluator<Temperature, MolarVolumeSpecific>
    {
        private const double MW_Water = 18.01528;  // kg/kmol

        public MolarVolumeSpecific EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);

            // CPropiAgua.densSatLiqTW retorna kg/m³
            double densityKgM3 = CPropiAgua.densSatLiqTW(tempK);

            // V = MW / ρ → m³/kmol
            double molarVolume = MW_Water / densityKgM3;

            return new MolarVolumeSpecific(molarVolume, MolarVolumeSpecificUnits.m3_Kgmol);
        }
    }

}
