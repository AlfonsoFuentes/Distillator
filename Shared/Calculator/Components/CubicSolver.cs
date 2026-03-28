namespace Shared.Calculator.Components
{
    public static class CubicSolver
    {
        public static List<double> Solve(double[] factors)
        {
            // El polinomio es: Z^3 + a*Z^2 + b*Z + c = 0
            // factors[0] = 1, factors[1] = a, factors[2] = b, factors[3] = c
            double a = factors[1];
            double b = factors[2];
            double c = factors[3];

            List<double> realRoots = new List<double>();

            double Q = (Math.Pow(a, 2.0) - 3.0 * b) / 9.0;
            double R = (2.0 * Math.Pow(a, 3.0) - 9.0 * a * b + 27.0 * c) / 54.0;

            double R2 = Math.Pow(R, 2.0);
            double Q3 = Math.Pow(Q, 3.0);

            if (R2 < Q3)
            {
                // Condición termodinámica de 3 raíces reales (Zona de equilibrio L-V)
                double theta = Math.Acos(R / Math.Sqrt(Q3));
                double sqrtQ = Math.Sqrt(Q);

                double z1 = -2.0 * sqrtQ * Math.Cos(theta / 3.0) - a / 3.0;
                double z2 = -2.0 * sqrtQ * Math.Cos((theta + 2.0 * Math.PI) / 3.0) - a / 3.0;
                double z3 = -2.0 * sqrtQ * Math.Cos((theta - 2.0 * Math.PI) / 3.0) - a / 3.0;

                realRoots.Add(z1);
                realRoots.Add(z2);
                realRoots.Add(z3);
            }
            else
            {
                // Condición de 1 raíz real y 2 complejas conjugadas (Fuera de la campana)
                double signR = Math.Sign(R);
                if (signR == 0) signR = 1.0;

                double A = -signR * Math.Pow(Math.Abs(R) + Math.Sqrt(R2 - Q3), 1.0 / 3.0);
                double B = (A == 0) ? 0.0 : Q / A;

                double z1 = (A + B) - a / 3.0;
                realRoots.Add(z1);
            }

            return realRoots;
        }
    }
}
