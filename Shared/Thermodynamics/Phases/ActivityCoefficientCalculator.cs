using Shared.PropertiesDtos.Enums;
using Shared.Thermodynamics.Componentes;
using UnitSystem;

namespace Shared.Thermodynamics.Phases
{
    public static class ActivityCoefficientCalculator
    {
        public static void Calculate(
            LiquidPhaseModel model,
            IReadOnlyList<LiquidComponentNode> components,
            double[][,] matrices,
            Temperature temperature)
        {
            if (components.Count == 0) return;

            switch (model)
            {
                case LiquidPhaseModel.EA_Van_Laar: CalculateVanLaar(components, matrices); break;
                case LiquidPhaseModel.Wilson:
                case LiquidPhaseModel.WilsonAspen: CalculateWilson(components, matrices, temperature); break;
                case LiquidPhaseModel.NRTLAspen: CalculateNRTL(components, matrices, temperature); break;
                default:
                    foreach (var c in components) c.ActivityCoefficient = 1.0;
                    break;
            }
        }

        private static void CalculateVanLaar(IReadOnlyList<LiquidComponentNode> components, double[][,] matrices)
        {
            int n = components.Count;
            for (int i = 0; i < n; i++)
            {
                double xi = components[i].MolarFraction;
                double sumAij = 0, sumAji = 0;
                for (int j = 0; j < n; j++)
                {
                    sumAij += components[j].MolarFraction * ((i == j) ? 0 : matrices[0][i, j]);
                    sumAji += components[j].MolarFraction * ((i == j) ? 0 : matrices[0][j, i]);
                }
                double den = xi * sumAij + (1.0 - xi) * sumAji;
                double lnGamma = (den != 0 && (1.0 - xi) != 0)
                    ? (sumAij / (1.0 - xi)) * Math.Pow(1.0 - (xi * sumAij / den), 2.0) : 0;
                components[i].ActivityCoefficient = Math.Exp(lnGamma);
            }
        }

        private static void CalculateWilson(IReadOnlyList<LiquidComponentNode> components, double[][,] matrices, Temperature T)
        {
            int n = components.Count;
            double tempK = T.GetValue(TemperatureUnits.Kelvin);
            double[,] lambda = new double[n, n];
            double[] S = new double[n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    lambda[i, j] = (i == j) ? 1.0 : Math.Exp(matrices[0][i, j] + (matrices[1][i, j] / tempK));
                    S[i] += components[j].MolarFraction * lambda[i, j];
                }

            for (int i = 0; i < n; i++)
            {
                double sumK = 0;
                for (int k = 0; k < n; k++)
                    if (S[k] != 0) sumK += components[k].MolarFraction * lambda[k, i] / S[k];
                double lnGamma = (S[i] != 0) ? 1.0 - Math.Log(S[i]) - sumK : 0;
                components[i].ActivityCoefficient = Math.Exp(lnGamma);
            }
        }

        private static void CalculateNRTL(IReadOnlyList<LiquidComponentNode> components, double[][,] matrices, Temperature T)
        {
            int n = components.Count;
            double tempK = T.GetValue(TemperatureUnits.Kelvin);
            double[,] tau = new double[n, n], G = new double[n, n];
            double[] sumXG = new double[n], sumXTauG = new double[n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    if (i == j) { tau[i, j] = 0; G[i, j] = 1; }
                    else
                    {
                        double m0 = matrices[0][i, j];
                        double m1 = matrices[1][i, j];
                        double m2 = matrices[2][i, j];
                        tau[i, j] = m0 + (m1 / tempK);
                        G[i, j] = Math.Exp(-m2 * tau[i, j]);
                    }
                }

            for (int i = 0; i < n; i++)
                for (int k = 0; k < n; k++)
                {
                    sumXG[i] += components[k].MolarFraction * G[k, i];
                    sumXTauG[i] += components[k].MolarFraction * tau[k, i] * G[k, i];
                }

            for (int i = 0; i < n; i++)
            {
                double term1 = (sumXG[i] != 0) ? sumXTauG[i] / sumXG[i] : 0;
                double term2 = 0;
                for (int j = 0; j < n; j++)
                    if (sumXG[j] != 0)
                        term2 += (components[j].MolarFraction * G[i, j] / sumXG[j]) * (tau[i, j] - (sumXTauG[j] / sumXG[j]));
                components[i].ActivityCoefficient = Math.Exp(term1 + term2);
            }
        }
    }
    

}
