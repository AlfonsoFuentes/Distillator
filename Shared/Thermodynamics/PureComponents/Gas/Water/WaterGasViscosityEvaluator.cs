using Shared.PropertiesDtos.WaterProperties;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Gas.Water
{
    public class WaterGasViscosityEvaluator : IPropertyEvaluator<Temperature, Viscosity>
    {
        public Viscosity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double visc = CPropiAgua.viscSatVapTW(tempK);

            return new Viscosity(visc, ViscosityUnits.Pa_s);
        }
    }

}
