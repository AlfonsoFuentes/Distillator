using Shared.Thermodynamics.Components;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents
{
    // Otros componentes - con inyección
    public class SecantSatTemperatureEvaluator : IPropertyEvaluator<Pressure, Temperature>
    {
        private readonly IPropertyEvaluator<Temperature, Pressure> _vaporPressureEvaluator;
        private readonly CorrelationCoefficientsDto _coeffs;
        private readonly Temperature _criticalTemperature;
        private readonly Pressure _criticalPressure;

        // Configuraciones de seguridad para el método numérico
        private const int MaxIterations = 50;
        private const double ToleranceP = 1e-4; // Tolerancia en kPa

        public SecantSatTemperatureEvaluator(
            IPropertyEvaluator<Temperature, Pressure> vaporPressureEvaluator,
            CorrelationCoefficientsDto coeffs,
            Temperature tc,
            Pressure pc)
        {
            _vaporPressureEvaluator = vaporPressureEvaluator;
            _coeffs = coeffs;
            _criticalTemperature = tc;
            _criticalPressure = pc;
        }

        public Temperature EvaluateAt(Pressure pressure)
        {
            double pTargetKpa = pressure.GetValue(PressureUnits.KiloPascal);
            double pcKpa = _criticalPressure.GetValue(PressureUnits.KiloPascal);
            double tcK = _criticalTemperature.GetValue(TemperatureUnits.Kelvin);
            double tMinK = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);

            // 1. Validaciones rápidas de límites termodinámicos
            if (pTargetKpa >= pcKpa)
                return new Temperature(tcK, TemperatureUnits.Kelvin);

            if (pTargetKpa <= 0)
                return new Temperature(tMinK, TemperatureUnits.Kelvin);

            // 2. Configuración inicial para la secante
            // Tomamos dos puntos cercanos a la temperatura crítica para arrancar hacia abajo
            double t0 = tcK - 1.0;
            double t1 = tcK - 5.0;

            double f0 = EvaluateError(t0, pTargetKpa);
            double f1 = EvaluateError(t1, pTargetKpa);

            // 3. Bucle Iterativo Seguro
            for (int i = 0; i < MaxIterations; i++)
            {
                // Condición de éxito: el error es menor a la tolerancia
                if (Math.Abs(f1) < ToleranceP)
                    break;

                double df = f1 - f0;

                // Protección contra división por cero (curva plana)
                if (Math.Abs(df) < 1e-9)
                    break;

                // Fórmula matemática de la Secante
                double t2 = t1 - f1 * (t1 - t0) / df;

                // CLAMPING: Restringir estrictamente a los límites físicos del componente
                t2 = Math.Clamp(t2, tMinK, tcK);

                // Preparar la siguiente iteración
                t0 = t1;
                f0 = f1;
                t1 = t2;
                f1 = EvaluateError(t1, pTargetKpa);

                // Condición de salida por estancamiento (Delta T muy pequeño)
                if (Math.Abs(t1 - t0) < 1e-5)
                    break;
            }

            return new Temperature(t1, TemperatureUnits.Kelvin);
        }

        // Método auxiliar para limpiar el cálculo del error (F(x) = P_calc - P_target)
        private double EvaluateError(double currentTempK, double targetPressureKpa)
        {
            var tempIter = new Temperature(currentTempK, TemperatureUnits.Kelvin);
            var pCalc = _vaporPressureEvaluator.EvaluateAt(tempIter);
            return pCalc.GetValue(PressureUnits.KiloPascal) - targetPressureKpa;
        }
    }

}
