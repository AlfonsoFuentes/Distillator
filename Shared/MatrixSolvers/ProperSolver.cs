using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.MatrixSolvers
{
    public class Vector
    {
        public double[] Data = null!;
    }

    public class Matrix
    {
        public double[,] Data = null!;
    }
    public static class LinearSolver
    {
        public static MatrixResult<double[]> TrySolve(double[,] A, double[] b)
        {
            if (A.GetLength(0) != A.GetLength(1))
                return MatrixResult<double[]>.Fail("Matriz no es cuadrada");

            var luResult = LUDecomposition.Decompose(A);

            if (!luResult.IsSuccess)
                return MatrixResult<double[]>.Fail(luResult.Error);

            var x = LUSolver.Solve(luResult.Value, b);

            return MatrixResult<double[]>.Ok(x);
        }
    }
    public class MatrixResult<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string Error { get; }

        private MatrixResult(bool isSuccess, T value, string error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static MatrixResult<T> Ok(T value)
            => new MatrixResult<T>(true, value, null!);

        public static MatrixResult<T> Fail(string error)
            => new MatrixResult<T>(false, default!, error);
    }

    public class LUResult
    {
        public double[,] LU = null!;
        public int[] Pivots = null!;
    }
    public static class LUDecomposition
    {
        public static MatrixResult<LUResult> Decompose(double[,] A)
        {
            int n = A.GetLength(0);
            int m = A.GetLength(1);

            if (n != m)
                return MatrixResult<LUResult>.Fail("La matriz debe ser cuadrada");

            var LU = (double[,])A.Clone();
            var pivots = new int[n];

            for (int i = 0; i < n; i++)
                pivots[i] = i;

            for (int k = 0; k < n; k++)
            {
                // 🔍 Pivoting
                double max = Math.Abs(LU[k, k]);
                int pivotRow = k;

                for (int i = k + 1; i < n; i++)
                {
                    double val = Math.Abs(LU[i, k]);
                    if (val > max)
                    {
                        max = val;
                        pivotRow = i;
                    }
                }

                // ⚠️ Matriz singular o mal condicionada
                if (Math.Abs(max) < 1e-12)
                    return MatrixResult<LUResult>.Fail(
                        $"Matriz singular en columna {k}");

                if (pivotRow != k)
                {
                    for (int j = 0; j < n; j++)
                    {
                        (LU[k, j], LU[pivotRow, j]) =
                        (LU[pivotRow, j], LU[k, j]);
                    }

                    (pivots[k], pivots[pivotRow]) =
                    (pivots[pivotRow], pivots[k]);
                }

                // 🔧 Eliminación
                for (int i = k + 1; i < n; i++)
                {
                    LU[i, k] /= LU[k, k];

                    for (int j = k + 1; j < n; j++)
                    {
                        LU[i, j] -= LU[i, k] * LU[k, j];
                    }
                }
            }

            var result = new LUResult
            {
                LU = LU,
                Pivots = pivots
            };

            return MatrixResult<LUResult>.Ok(result);
        }
    }
    public static class LUSolver
    {
        public static double[] Solve(LUResult luRes, double[] b)
        {
            int n = b.Length;
            var x = new double[n];
            var y = new double[n];

            var LU = luRes.LU;
            var piv = luRes.Pivots;

            // 🔁 Aplicar pivoteo a b
            for (int i = 0; i < n; i++)
                y[i] = b[piv[i]];

            // 🔽 Forward substitution (Ly = b)
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    y[i] -= LU[i, j] * y[j];
                }
            }

            // 🔼 Back substitution (Ux = y)
            for (int i = n - 1; i >= 0; i--)
            {
                for (int j = i + 1; j < n; j++)
                {
                    y[i] -= LU[i, j] * x[j];
                }

                x[i] = y[i] / LU[i, i];
            }

            return x;
        }
    }
    public static class VectorOps
    {
        public static double[] Negate(double[] v)
        {
            var result = new double[v.Length];

            for (int i = 0; i < v.Length; i++)
                result[i] = -v[i];

            return result;
        }

        public static MatrixResult<double[]> Add(double[] a, double[] b)
        {
            if (a.Length != b.Length)
                return MatrixResult<double[]>.Fail("Vectores de diferente tamaño");

            var result = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                result[i] = a[i] + b[i];

            return MatrixResult<double[]>.Ok(result);
        }

        public static double Norm(double[] v)
        {
            double sum = 0;

            for (int i = 0; i < v.Length; i++)
                sum += v[i] * v[i];

            return Math.Sqrt(sum);
        }
        // EN: VectorOps.cs - Agregar este método nuevo
        public static double[] Scale(double[] v, double factor)
        {
            var result = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
                result[i] = v[i] * factor;
            return result;
        }
    }

    public enum EquationType
    {
        Model,
        Specification
    }


    public class Equation
    {
        public Func<double[], double> Function { get; set; } = null!;
        public EquationType Type { get; set; }

    }
}
