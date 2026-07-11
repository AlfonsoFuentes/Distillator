using UnitSystem;

namespace Shared.Helpers
{
    /// <summary>
    /// Calcula la presión atmosférica a partir de la elevación de la planta
    /// usando la fórmula del aire estándar ISA.
    /// </summary>
    public static class AtmosphericPressureCalculator
    {
        private const double P0 = 101325.0; // Pa, presión al nivel del mar
        private const double Factor = 2.25577e-5;
        private const double Exponent = 5.25588;

        public static Pressure CalculateFromElevation(Length elevation)
        {
            var elevationMeters = elevation.GetValue(LengthUnits.Meter);
            var pressurePa = P0 * Math.Pow(1 - Factor * elevationMeters, Exponent);
            return new Pressure(pressurePa, PressureUnits.Pascala);
        }
    }
}
