using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Others
{
    public class RackettSaturatedMolarVolumeEvaluator : IPropertyEvaluator<Temperature, MolarVolumeSpecific>
    {
        private readonly Temperature _criticalTemperature;
        private readonly Pressure _criticalPressure;
        private readonly double _acentricFactor;
        private const double R_Gas = 8.314472;  // kPa·m³/(kmol·K)

        public RackettSaturatedMolarVolumeEvaluator(Temperature tc, Pressure pc, double omega)
        {
            _criticalTemperature = tc;
            _criticalPressure = pc;
            _acentricFactor = omega;
        }

        public MolarVolumeSpecific EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tcK = _criticalTemperature.GetValue(TemperatureUnits.Kelvin);
            double pcKpa = _criticalPressure.GetValue(PressureUnits.KiloPascal);

            // Factor ZRa
            double zRa = 0.29056 - 0.08775 * _acentricFactor;

            // Temperatura reducida
            double tr = tempK / tcK;
            if (tr > 1.0) tr = 1.0;

            // Exponente de Rackett: 1 + (1-Tr)^(2/7)
            double exponente = 1.0 + Math.Pow(1.0 - tr, 0.2857);

            // Volumen molar saturado
            double molarVolume = (R_Gas * tcK / pcKpa) * Math.Pow(zRa, exponente);

            return new MolarVolumeSpecific(molarVolume, MolarVolumeSpecificUnits.m3_Kgmol);
        }
    }

}
