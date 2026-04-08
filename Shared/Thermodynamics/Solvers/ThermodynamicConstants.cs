using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Thermodynamics.Solvers
{
    public static class ThermodynamicConstants
    {
        // Tolerancias de convergencia
        public const double CompositionSumTolerance = 1e-4;
        public const double TemperatureToleranceKelvin = 0.1;
        public const double PressureToleranceBar = 0.01;
        public const double FugacityConvergenceTolerance = 1e-6;
        public const double VaporFractionBoundsEpsilon = 1e-8;

        // Tolerancias de comparación
        public const double ValueChangeEpsilon = 1e-9;
        public const double PressureIterationToleranceKpa = 1e-3;

        // Constantes físicas
        public const double R_Gas = 8.314472; // kPa·m³/(kmol·K)

        // Límites numéricos
        public const double MinPositiveValue = 1e-10;
        public const int MaxIterations = 100;
        public const double PureComponentThreshold = 0.9999;

        // Parámetros de modelos específicos
        public const double TamuraKurataConstant = 44.1;
        public const double SurfaceTensionExponent = 0.25;
        public const double SurfaceTensionPower = 4.0;
        public const double SecantInitialPerturbation = 1e-4;
    }
}
