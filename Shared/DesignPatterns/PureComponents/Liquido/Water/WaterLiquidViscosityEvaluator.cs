using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Water
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
