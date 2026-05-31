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
        public static SecantResult Solve(Func<double, double> func, double guess)
        {
            // 🔹 1. EL "ARRANQUE" (Reemplaza tu bloque 'else' y elimina x1/x2)
            double p1 = guess;
            double sumaAnterior = func(p1);

            // Fabricamos el p2 basándonos en el guess (nunca en cero)
            // Usamos Math.Max por si acaso el guess que llega es 0.0
            double perturbation = ThermodynamicConstants.SecantInitialPerturbation;
            if (guess > 0) perturbation = guess * 0.01; // Perturbación del 1% es más estable

            double p2 = guess + perturbation;

            double sumaActual = 0.0;
            int iter = 0;
            bool converged = false;

            // 🔹 2. EL MOTOR (Sin el 'if (formula)', puro cálculo directo)
            do
            {
                iter++;
                sumaActual = func(p2);

                // ✅ Verificar convergencia
                if (Math.Abs(sumaActual) < ThermodynamicConstants.MinPositiveValue)
                {
                    converged = true;
                    break;
                }

                // ✅ Método de la Secante
                double denominator = sumaActual - sumaAnterior;

                if (Math.Abs(denominator) > ThermodynamicConstants.ValueChangeEpsilon)
                {
                    double m = (p2 - p1) / denominator;
                    double pNew = p2 - m * sumaActual;

                    p1 = p2;
                    sumaAnterior = sumaActual;
                    p2 = pNew;

                    // Si la presión se vuelve negativa, frenamos el desplome
                    if (p2 <= 0)
                    {
                        // En lugar de mandarlo a 0 (que daña la termodinámica), 
                        // lo frenamos en un valor pequeño positivo
                        p2 = guess * 0.1;
                    }
                }
                else
                {
                    // Evitar división por cero si la curva se aplanó
                    break;
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
