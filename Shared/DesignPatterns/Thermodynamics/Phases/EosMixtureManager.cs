using Shared.DesignPatterns.PureComponents;
using Shared.DesignPatterns.Thermodynamics.Componentes;
using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics.Phases
{
    public static class EosMixtureManager
    {
        private const double R_Gas = 8.314472;

        public static EosParameters CalculateMixtureParameters(
            IReadOnlyList<VaporComponentNode> components,
            double[,] kijMatrix,
            Temperature T,
            Pressure P)
        {
            int n = components.Count;
            double tempK = T.GetValue(TemperatureUnits.Kelvin);
            double pKpa = P.GetValue(PressureUnits.KiloPascal);

            double aMix = 0.0;
            double bMix = 0.0;

            // Reglas de Mezcla de Van der Waals (Clásicas de Prausnitz)
            for (int i = 0; i < n; i++)
            {
                double yi = components[i].MolarFraction;
                bMix += yi * components[i].EosParams.B;

                for (int j = 0; j < n; j++)
                {
                    double yj = components[j].MolarFraction;
                    double ai = components[i].EosParams.A;
                    double aj = components[j].EosParams.A;
                    double kij = kijMatrix[i, j];

                    // a_ij = sqrt(ai * aj) * (1 - kij)
                    aMix += yi * yj * Math.Sqrt(ai * aj) * (1.0 - kij);
                }
            }

            // Parámetros adimensionales de la mezcla
            double aAsterisk = aMix * pKpa / Math.Pow(R_Gas * tempK, 2.0);
            double bAsterisk = bMix * pKpa / (R_Gas * tempK);

            // U y W son constantes del modelo (Peng-Robinson, SRK, etc.)
            double uParam = n > 0 ? components[0].EosParams.U : 0;
            double wParam = n > 0 ? components[0].EosParams.W : 0;

            var mixtureParams = new EosParameters
            {
                A = aMix,
                B = bMix,
                AAsterisk = aAsterisk,
                BAsterisk = bAsterisk,
                U = uParam,
                W = wParam
            };

            // Generar factores del polinomio Z^3 + αZ^2 + βZ + γ = 0
            mixtureParams.Factors[0] = 1.0;
            mixtureParams.Factors[1] = (uParam - 1.0) * bAsterisk - 1.0;
            mixtureParams.Factors[2] = aAsterisk + wParam * Math.Pow(bAsterisk, 2.0) - uParam * bAsterisk * (bAsterisk + 1.0);
            mixtureParams.Factors[3] = -(aAsterisk * bAsterisk + wParam * Math.Pow(bAsterisk, 2.0) * (bAsterisk + 1.0));

            return mixtureParams;
        }
    }
}
