using Shared.DesignPatterns.PureComponents;
using Shared.DesignPatterns.Thermodynamics.Componentes;

namespace Shared.DesignPatterns.Thermodynamics.Phases
{
    public static class VaporFugacityCalculator
    {
        public static void Calculate(
            IReadOnlyList<VaporComponentNode> components,
            EosParameters mixParams,
            double[,] kijMatrix,
            double compressibilityZ)
        {
            int n = components.Count;
            if (n == 0 || mixParams.A <= 0 || mixParams.B <= 0) return;

            for (int i = 0; i < n; i++)
            {
                var comp = components[i];

                // 1. Calcular la sumatoria parcial de A para el componente i
                double sumYjAij = 0;
                for (int j = 0; j < n; j++)
                {
                    double aij = Math.Sqrt(comp.EosParams.A * components[j].EosParams.A) * (1.0 - kijMatrix[i, j]);
                    sumYjAij += components[j].MolarFraction * aij;
                }

                // 2. Términos de la ecuación de fugacidad parcial
                double bi_bmix = comp.EosParams.B / mixParams.B;
                double delta_i = (2.0 * sumYjAij) / mixParams.A;

                double term1 = bi_bmix * (compressibilityZ - 1.0);

                double argLog1 = compressibilityZ - mixParams.BAsterisk;
                double term2 = -Math.Log(Math.Max(argLog1, 1e-12));

                double sqrtDelta = Math.Sqrt(mixParams.U * mixParams.U - 4.0 * mixParams.W);
                double factorA = mixParams.AAsterisk / (mixParams.BAsterisk * Math.Max(sqrtDelta, 1e-12));

                double term3 = factorA * (bi_bmix - delta_i);

                double term4 = 2.0 * compressibilityZ + mixParams.BAsterisk * (mixParams.U + sqrtDelta);
                double term5 = 2.0 * compressibilityZ + mixParams.BAsterisk * (mixParams.U - sqrtDelta);

                double lnPhi_i = term1 + term2 + term3 * Math.Log(Math.Max(term4 / term5, 1e-12));

                comp.FugacityCoefficient = Math.Exp(lnPhi_i);
            }
        }
    }
}
