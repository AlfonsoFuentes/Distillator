using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Gas.Water
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
