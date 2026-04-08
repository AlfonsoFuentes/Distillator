using Shared.PropertiesDtos.WaterProperties;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Water
{
    public class WaterLiquidViscosityEvaluator : IPropertyEvaluator<Temperature, Viscosity>
    {
        public Viscosity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double visc = CPropiAgua.viscSatLiqTW(tempK);

            return new Viscosity(visc, ViscosityUnits.Pa_s);
        }
    }

}
