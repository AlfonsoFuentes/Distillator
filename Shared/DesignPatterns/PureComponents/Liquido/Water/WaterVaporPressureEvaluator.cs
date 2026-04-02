using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Water
{
    public class WaterVaporPressureEvaluator : IPropertyEvaluator<Temperature, Pressure>
    {
        private readonly Pressure _criticalPressure;
        private readonly Temperature _criticalTemperature;

        public WaterVaporPressureEvaluator(Pressure pc, Temperature tc)
        {
            _criticalPressure = pc;
            _criticalTemperature = tc;
        }

        public Pressure EvaluateAt(Temperature temperature)
        {
            double tempKelvin = temperature.GetValue(TemperatureUnits.Kelvin);
            double tcKelvin = _criticalTemperature.GetValue(TemperatureUnits.Kelvin);

            if (tempKelvin >= tcKelvin)
            {
                return _criticalPressure;
            }

            double pBar = CPropiAgua.pSatW(tempKelvin);
            return new Pressure(pBar, PressureUnits.Bar);
        }
    }

}
