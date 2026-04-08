using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Thermodynamics.Solvers
{
    public static class SecantSolver
    {
        // =========================================================================
        // CONFIGURACIÓN INTERNA (No expuesta al caller)
        // =========================================================================
      

        // =========================================================================
        // RESULTADO DEL SOLVER
        // =========================================================================
        public class SecantResult
        {
            public double Value { get; set; }
            public bool Converged { get; set; }
            public int Iterations { get; set; }
        }

        // =========================================================================
        // MÉTODO PRINCIPAL
        // =========================================================================
        /// <summary>
        /// Resuelve f(x) = 0 usando el método de la Secante.
        /// </summary>
        /// <param name="func">Función objetivo que debe converger a 0</param>
        /// <param name="x1">Límite inferior inicial</param>
        /// <param name="x2">Límite superior inicial</param>
        /// <returns>Resultado con valor convergido, estado de convergencia e iteraciones</returns>
        public static SecantResult Solve(Func<double, double> func, double x1, double x2, double guess)
        {
            double p1 = x1;
            double p2 = (guess > 0 && guess >= x1 && guess <= x2) ? guess : x2;
            double sumaActual = 0.0;
            double sumaAnterior = 0.0;
            bool formula = false;
            int iter = 0;
            bool converged = false;

            do
            {
                iter++;
                sumaActual = func(p2);

                if (formula)
                {
                    // ✅ Verificar convergencia
                    if (Math.Abs(sumaActual) < ThermodynamicConstants.MinPositiveValue)
                    {
                        converged = true;
                        break;
                    }

                    // ✅ Método de la Secante: p_new = p2 - m * sumaActual
                    double denominator = sumaActual - sumaAnterior;
                    if (Math.Abs(denominator) > ThermodynamicConstants.ValueChangeEpsilon)
                    {
                        double m = (p2 - p1) / denominator;
                        double b = p2 - m * sumaActual;

                        p1 = p2;
                        sumaAnterior = sumaActual;
                        p2 = b;

                        // Si la presión se vuelve negativa, reiniciar con límite inferior
                        if (p2 < 0)
                        {
                            p2 = x1;
                            formula = false;
                        }
                    }
                    else
                    {
                        // Evitar división por cero
                        p2 = x1;
                        formula = false;
                    }
                }
                else
                {
                    // ✅ Primera iteración: preparar para secante
                    p1 = p2;
                    sumaAnterior = sumaActual;
                    p2 = x2 - ThermodynamicConstants.SecantInitialPerturbation;  // Pequeña perturbación inicial
                    formula = true;
                }

                // ✅ Límite de iteraciones
                if (iter > ThermodynamicConstants.MaxIterations)
                {
                    break;
                }

            } while (true);

            return new SecantResult
            {
                Value = p2,
                Converged = converged,
                Iterations = iter
            };
        }
    }
}
