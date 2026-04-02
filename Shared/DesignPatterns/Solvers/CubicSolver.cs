namespace Shared.Calculator.Components
{
    public static class CubicSolver
    {
        public static List<double> Solve(double[] factors)
        {
            // ✅ VALIDACIÓN DE INPUT (Crítico)
            if (factors == null || factors.Length < 4)
            {
                // Fallback seguro: retornar Z=1 (gas ideal) si los coeficientes son inválidos
                return new List<double> { 1.0 };
            }

            // El polinomio es: Z^3 + a*Z^2 + b*Z + c = 0
            double a = factors[1];
            double b = factors[2];
            double c = factors[3];

            List<double> realRoots = new List<double>();

            double Q = (Math.Pow(a, 2.0) - 3.0 * b) / 9.0;
            double R = (2.0 * Math.Pow(a, 3.0) - 9.0 * a * b + 27.0 * c) / 54.0;

            double R2 = Math.Pow(R, 2.0);
            double Q3 = Math.Pow(Q, 3.0);

            // ✅ VALIDAR DOMINIO MATEMÁTICO para evitar NaN
            const double epsilon = 1e-10;  // Tolerancia pequeña para comparaciones

            if (R2 < Q3 + epsilon)  // Zona de 3 raíces reales
            {
                // ✅ Proteger Math.Acos: el argumento debe estar en [-1, 1]
                double acosArg = R / Math.Sqrt(Math.Max(Q3, epsilon));
                acosArg = Math.Clamp(acosArg, -1.0, 1.0);  // Forzar rango válido

                double theta = Math.Acos(acosArg);
                double sqrtQ = Math.Sqrt(Math.Max(Q, 0));  // Evitar sqrt de negativo

                double z1 = -2.0 * sqrtQ * Math.Cos(theta / 3.0) - a / 3.0;
                double z2 = -2.0 * sqrtQ * Math.Cos((theta + 2.0 * Math.PI) / 3.0) - a / 3.0;
                double z3 = -2.0 * sqrtQ * Math.Cos((theta - 2.0 * Math.PI) / 3.0) - a / 3.0;

                realRoots.Add(z1);
                realRoots.Add(z2);
                realRoots.Add(z3);
            }
            else  // Zona de 1 raíz real
            {
                double signR = Math.Sign(R);
                if (signR == 0) signR = 1.0;

                double discriminant = Math.Max(R2 - Q3, 0);  // ✅ Evitar sqrt de negativo por errores numéricos
                double A = -signR * Math.Pow(Math.Abs(R) + Math.Sqrt(discriminant), 1.0 / 3.0);
                double B = (Math.Abs(A) > epsilon) ? Q / A : 0.0;  // ✅ Evitar división por cero

                double z1 = (A + B) - a / 3.0;
                realRoots.Add(z1);
            }

            return realRoots;
        }
    }
}
