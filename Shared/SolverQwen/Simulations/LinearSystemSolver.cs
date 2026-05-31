namespace Shared.SolverQwen.Simlations
{
    public static class LinearSystemSolver
    {
        /// <summary>
        /// Resuelve Ax = b usando eliminación Gaussiana con pivoteo.
        /// Retorna x, o null si la matriz es singular.
        /// </summary>
        public static double[] Solve(double[,] A, double[] b, double singularityTolerance = 1e-12)
        {
            int n = b.Length;
            double[] x = new double[n];
            double[,] M = (double[,])A.Clone();
            double[] c = (double[])b.Clone();

            // Pivoteo y Eliminación
            for (int i = 0; i < n; i++)
            {
                // Buscar pivote máximo en columna i
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                {
                    if (Math.Abs(M[k, i]) > Math.Abs(M[maxRow, i]))
                        maxRow = k;
                }

                // Intercambiar filas
                for (int k = i; k < n; k++)
                {
                    double temp = M[i, k];
                    M[i, k] = M[maxRow, k];
                    M[maxRow, k] = temp;
                }
                double tempC = c[i];
                c[i] = c[maxRow];
                c[maxRow] = tempC;

                if (Math.Abs(M[i, i]) < singularityTolerance) return null!; // Singular

                // Hacer 0 debajo del pivote
                for (int k = i + 1; k < n; k++)
                {
                    double factor = M[k, i] / M[i, i];
                    for (int j = i; j < n; j++)
                        M[k, j] -= factor * M[i, j];
                    c[k] -= factor * c[i];
                }
            }

            // Sustitución hacia atrás
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0.0;
                for (int j = i + 1; j < n; j++)
                    sum += M[i, j] * x[j];
                x[i] = (c[i] - sum) / M[i, i];
            }

            return x;
        }
    }
}