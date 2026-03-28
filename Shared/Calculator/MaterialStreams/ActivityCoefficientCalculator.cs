using Shared.Calculator.Components;
using Shared.Thermodynamics.Enums;
using UnitSystem;

namespace Shared.Calculator.MaterialStreams
{
    public static class ActivityCoefficientCalculator
    {
        public static void Calculate(
            LiquidPhaseModel model,
            IReadOnlyList<LiquidComponent> components,
            double[][,] activityMatrices,
            Amount temperature)
        {
            if (components.Count == 0) return;

            switch (model)
            {
                case LiquidPhaseModel.IdealLiquid:
                case LiquidPhaseModel.SteamTables:
                    CalculateIdeal(components);
                    break;

                case LiquidPhaseModel.EA_Van_Laar:
                    CalculateVanLaar(components, activityMatrices);
                    break;

                case LiquidPhaseModel.Wilson:
                case LiquidPhaseModel.WilsonASPEN:
                    CalculateWilson(components, activityMatrices, temperature);
                    break;

                case LiquidPhaseModel.NRTL_ASPEN:
                    CalculateNRTL(components, activityMatrices, temperature);
                    break;

                default:
                    CalculateIdeal(components);
                    break;
            }
        }

        private static void CalculateIdeal(IReadOnlyList<LiquidComponent> components)
        {
            foreach (var comp in components)
            {
                comp.ActivityCoefficient = 1.0;
            }
        }

        private static void CalculateVanLaar(IReadOnlyList<LiquidComponent> components, double[][,] matrices)
        {
            int n = components.Count;
            for (int i = 0; i < n; i++)
            {
                double xi = components[i].MoleFraction;
                double sumAij = 0.0;
                double sumAji = 0.0;

                for (int j = 0; j < n; j++)
                {
                    double xj = components[j].MoleFraction;
                    double Aij = (i == j) ? 0.0 : matrices[0][i, j];
                    double Aji = (i == j) ? 0.0 : matrices[0][j, i];

                    sumAij += xj * Aij;
                    sumAji += xj * Aji;
                }

                double lnGamma = 0.0;
                double num = xi * sumAij;
                double den = xi * sumAij + (1.0 - xi) * sumAji;

                if ((1.0 - xi) != 0 && den != 0)
                {
                    lnGamma = (sumAij / (1.0 - xi)) * Math.Pow(1.0 - (num / den), 2.0);
                }

                components[i].ActivityCoefficient = Math.Exp(lnGamma);
            }
        }

        private static void CalculateWilson(IReadOnlyList<LiquidComponent> components, double[][,] matrices, Amount temperature)
        {
            int n = components.Count;
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double[,] lambda = new double[n, n];

            // 1. Matriz térmica Lambda
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        lambda[i, j] = 1.0;
                    }
                    else
                    {
                        double a_ij = matrices[0][i, j];
                        double b_ij = matrices[1][i, j];
                        lambda[i, j] = Math.Exp(a_ij + (b_ij / tempK));
                    }
                }
            }

            // 2. Sumatoria S
            double[] S = new double[n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    S[i] += components[j].MoleFraction * lambda[i, j];
                }
            }

            // 3. Ecuación
            for (int i = 0; i < n; i++)
            {
                double lnGamma = 0.0;
                double sum_k = 0.0;

                for (int k = 0; k < n; k++)
                {
                    if (S[k] != 0) sum_k += components[k].MoleFraction * lambda[k, i] / S[k];
                }

                if (S[i] != 0) lnGamma = 1.0 - Math.Log(S[i]) - sum_k;

                components[i].ActivityCoefficient = Math.Exp(lnGamma);
            }
        }

        private static void CalculateNRTL(IReadOnlyList<LiquidComponent> components, double[][,] matrices, Amount temperature)
        {
            int n = components.Count;
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);

            double[,] tau = new double[n, n];
            double[,] G = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        tau[i, j] = 0.0;
                        G[i, j] = 1.0;
                    }
                    else
                    {
                        double a_ij = matrices[0][i, j];
                        double b_ij = matrices[1][i, j];
                        double alpha_ij = matrices[2][i, j];

                        tau[i, j] = a_ij + (b_ij / tempK);
                        G[i, j] = Math.Exp(-alpha_ij * tau[i, j]);
                    }
                }
            }

            double[] sumXG = new double[n];
            double[] sumXTauG = new double[n];

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < n; k++)
                {
                    double x_k = components[k].MoleFraction;
                    sumXG[i] += x_k * G[k, i];
                    sumXTauG[i] += x_k * tau[k, i] * G[k, i];
                }
            }

            for (int i = 0; i < n; i++)
            {
                double term1 = sumXG[i] != 0 ? (sumXTauG[i] / sumXG[i]) : 0.0;
                double term2 = 0.0;

                for (int j = 0; j < n; j++)
                {
                    double x_j = components[j].MoleFraction;
                    if (sumXG[j] != 0)
                    {
                        term2 += (x_j * G[i, j] / sumXG[j]) * (tau[i, j] - (sumXTauG[j] / sumXG[j]));
                    }
                }

                components[i].ActivityCoefficient = Math.Exp(term1 + term2);
            }
        }
    }

}
