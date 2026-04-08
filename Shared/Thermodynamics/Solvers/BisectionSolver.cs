using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Thermodynamics.Solvers
{
    /// <summary>
    /// Solver numérico usando el método de Bisección.
    /// Encapsula la lógica de iteración para encontrar raíces de f(x) = 0.
    /// Más estable que Secante para funciones no lineales (como Temperatura).
    /// </summary>
    public static class BisectionSolver
    {
        // =========================================================================
        // CONFIGURACIÓN INTERNA (No expuesta al caller)
        // =========================================================================
        // ✅ Usar constantes centralizadas para consistencia global
        private const double TOLERANCE = ThermodynamicConstants.FugacityConvergenceTolerance;
        private const double TOLERANCE_X = ThermodynamicConstants.PressureIterationToleranceKpa;
        private const int MAX_ITERATIONS = ThermodynamicConstants.MaxIterations;

        // =========================================================================
        // RESULTADO DEL SOLVER
        // =========================================================================
        public class BisectionResult
        {
            public double Value { get; set; }
            public bool Converged { get; set; }
            public int Iterations { get; set; }
        }

        // =========================================================================
        // MÉTODO PRINCIPAL
        // =========================================================================
        /// <summary>
        /// Resuelve f(x) = 0 usando el método de Bisección.
        /// </summary>
        /// <param name="func">Función objetivo que debe converger a 0</param>
        /// <param name="x1">Límite inferior (siempre se mantiene como límite inferior)</param>
        /// <param name="x2">Límite superior (siempre se mantiene como límite superior)</param>
        /// <param name="guess">Valor inicial estimado (opcional, para primera evaluación)</param>
        /// <returns>Resultado con valor convergido, estado de convergencia e iteraciones</returns>
        public static BisectionResult Solve(
            Func<double, double> func,
            double x1,
            double x2,
            double guess = -1)
        {
            double t1 = x1;  // Límite inferior (TMenor)
            double t2 = x2;  // Límite superior (TMayor)

            // ✅ Usar guess si es válido y está dentro de rango
            double tt = (guess > 0 && guess >= x1 && guess <= x2) ? guess : x1;

            double sumaActual = 0.0;
            int iter = 0;
            bool converged = false;
            bool parar = false;

            do
            {
                iter++;

                // ✅ BISECCIÓN: Punto medio
                tt = (t1 + t2) / 2.0;

                // ✅ Evaluar función
                sumaActual = func(tt);

                // ✅ AJUSTE DE LÍMITES SEGÚN SIGNO
                if (sumaActual > 0)
                {
                    t2 = tt;  // El valor está por encima, bajar límite superior
                }
                else
                {
                    t1 = tt;  // El valor está por debajo, subir límite inferior
                }

                // ✅ CRITERIOS DE CONVERGENCIA DOBLES
                if (Math.Abs(sumaActual) < TOLERANCE)
                {
                    converged = true;
                    parar = true;
                }
                else if (Math.Abs(t2 - t1) < TOLERANCE_X)
                {
                    converged = true;
                    parar = true;
                }

                // ✅ Límite de iteraciones
                if (iter > MAX_ITERATIONS)
                {
                    parar = true;
                }

            } while (!parar);

            return new BisectionResult
            {
                Value = tt,
                Converged = converged,
                Iterations = iter
            };
        }
    }
}

