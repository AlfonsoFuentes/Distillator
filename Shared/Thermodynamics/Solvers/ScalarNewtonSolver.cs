using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Thermodynamics.Solvers
{

    /// <summary>
    /// Resultado de un solver Newton-Raphson escalar.
    /// </summary>
    public class ScalarSolverResult
    {
        public bool Converged { get; }
        public int Iterations { get; }
        public double Value { get; }
        public double Residual { get; }

        public ScalarSolverResult(bool converged, int iter, double val, double res)
        {
            Converged = converged;
            Iterations = iter;
            Value = val;
            Residual = res;
        }
    }

    public static class ScalarNewtonSolver
    {
        public const double DefaultAdimTolerance = 1e-6;
        // Cambia esto de 1e-6 a 1e-4 o incluso 1e-3
        public const double DefaultAdimPerturbation = 0.1;
        public const int DefaultMaxIterations = 50;

        // ========================================================================
        // 1. EL MOTOR PRINCIPAL (Inteligente, robusto y con visión de tendencia)
        // ========================================================================
        public static ScalarSolverResult Solve(
             Func<double, double> func, double x0, double x_norm = 1.0, double f_norm = 1.0,
             double tolAdim = DefaultAdimTolerance, int maxIter = DefaultMaxIterations, double adimperturbation = DefaultAdimPerturbation, string debugTag = "Newton")
        {
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"\n[DEBUG-{debugTag}] ⚡ Iniciando Solve (Motor IQI 3-Puntos) | x0={x0:F4}");
#endif
            double xAdim = x0 / x_norm;

            double UseLinearSolve = tolAdim * 10;
            for (int iter = 0; iter < maxIter; iter++)
            {
                // --- 📍 PUNTO A: La Base ---
                double xReal = xAdim * x_norm;
                double fReal = func(xReal);
                double fAdim = fReal / f_norm;

                if (Math.Abs(fAdim) < tolAdim)
                    return LogSuccess(debugTag, true, iter + 1, xReal, fReal, sw);

                // --- 📍 PUNTO B: La Derivada ---
                double hAdim = adimperturbation;
                double xPlusHAdim = xAdim + hAdim;
                double fPlusReal = func(xPlusHAdim * x_norm);
                double fPlusAdim = fPlusReal / f_norm;

                double dfDx = (fPlusAdim - fAdim) / hAdim;

                if (Math.Abs(dfDx) < 1e-12)
                {
#if DEBUG
                    Console.WriteLine($"[DEBUG-{debugTag}] ⚠️ Derivada singular (df/dx ≈ 0). Abortando.");
#endif
                    break;
                }

                // --- 📍 PUNTO C: El Salto Normalito de Newton ---
                double dxAdim = -fAdim / dfDx;
                double xNewtonAdim = xAdim + dxAdim;
                if (Math.Abs(xNewtonAdim - xAdim) > 50)
                {
                }
                double fNewtonReal = func(xNewtonAdim * x_norm);
                double fNewtonAdim = fNewtonReal / f_norm;

                // ¿Newton le atinó al blanco directamente?
                if (Math.Abs(fNewtonAdim) < tolAdim)
                    return LogSuccess(debugTag, true, iter + 1, xNewtonAdim * x_norm, fNewtonReal, sw);

                // 🚀 ACELERADOR: El Evaluador de 3 Puntos (IQI) propuesto por la Ingeniera
                var predictor = PredictRootLinear(xPlusHAdim, fPlusAdim, xNewtonAdim, fNewtonAdim);

                if (predictor.IsValid && Math.Abs(fNewtonAdim) > UseLinearSolve)
                {
#if DEBUG
                    Console.WriteLine($"      🎯 [Iter {iter + 1}] IQI Activo: NR sugirió X={xNewtonAdim:F4}. Evaluador 3-puntos nos catapultó a X={predictor.PredictedX:F4}");
#endif
                    // Boom. Tomamos el dato analítico inteligente
                    xAdim = predictor.PredictedX;
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"      🛡️ [Iter {iter + 1}] IQI Ilogico/Inestable. Seguimos con el NR normalito a X={xNewtonAdim:F4}");
#endif
                    // El acelerador dio un salto absurdo, nos protegemos y seguimos con Newton
                    xAdim = xNewtonAdim;
                }
            }

            return LogSuccess(debugTag, false, maxIter, xAdim * x_norm, func(xAdim * x_norm), sw);
        }

        // ==============================================================================
        // 🧠 MOTOR ANALÍTICO: Interpolación Cuadrática Inversa (El Acelerador de 3 Puntos)
        // ==============================================================================
        private static (bool IsValid, double PredictedX) PredictRootLinear(
    double xH, double yH, double xDx, double yDx)
        {
            const double eps = 1e-12;

            // 🛡️ ESCUDO 1: Si la recta es totalmente plana (división por cero), abortamos
            if (Math.Abs(yDx - yH) < eps)
                return (false, 0.0);

            // 📈 La matemática de tu línea de tendencia (Método de la Secante)
            double pendienteSecante = (yDx - yH) / (xDx - xH);
            double xPred = xDx - (yDx / pendienteSecante);

            // 🛡️ ESCUDO 2: Realidad Física
            // a) No permitimos presiones negativas o cero
            if (xPred <= 1e-5)
                return (false, 0.0);

            // b) Collar de Perro Suave: Evitar un salto al hiperespacio si la recta es casi plana
            double maxJump = 5.0 * Math.Abs(xDx - xH);
            if (Math.Abs(xPred - xDx) > maxJump)
                return (false, 0.0);

            return (true, xPred);
        }

        // ========================================================================
        // 2. EL MOTOR DE RESERVA (Sin amortiguador, solo para funciones dóciles)
        // ========================================================================
        public static ScalarSolverResult SolveRaw(
             Func<double, double> func, double x0, double x_norm = 1.0, double f_norm = 1.0,
             double tolAdim = DefaultAdimTolerance, int maxIter = DefaultMaxIterations, string debugTag = "NewtonRaw")
        {
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"\n[DEBUG-{debugTag}] ⚡ Iniciando SolveRaw | x0={x0:F4}");
#endif
            double xAdim = x0 / x_norm;

            for (int iter = 0; iter < maxIter; iter++)
            {
                double xReal = xAdim * x_norm;
                double fReal = func(xReal);
                double fAdim = fReal / f_norm;

                if (Math.Abs(fAdim) < tolAdim)
                    return LogSuccess(debugTag, true, iter + 1, xReal, fReal, sw);

                double dfDx = CalculateForwardDerivative(func, xAdim, x_norm, fAdim, f_norm);

                if (Math.Abs(dfDx) < 1e-12) break;

                xAdim -= fAdim / dfDx; // Salto ciego a fondo
            }

            return LogSuccess(debugTag, false, maxIter, xAdim * x_norm, func(xAdim * x_norm), sw);
        }

        // ========================================================================
        // 🔹 MÉTODOS PRIVADOS (Magia encapsulada)
        // ========================================================================

        private static double CalculateForwardDerivative(Func<double, double> func, double xAdim, double xNorm, double fAdim, double fNorm)
        {
            double hAdim = DefaultAdimPerturbation * Math.Max(1.0, Math.Abs(xAdim));
            double fPlusReal = func((xAdim + hAdim) * xNorm);
            return ((fPlusReal / fNorm) - fAdim) / hAdim;
        }

        private static (bool Success, double BestXAdim, double BestFAdimAbs, double BestFReal) PerformLineSearch(
            Func<double, double> func, double xAdim, double dxAdim, double xNorm, double fNorm, double currentFAdim, double currentFReal)
        {
            //            if (Math.Abs(dxAdim) < 1e-7)
            //            {
            //#if DEBUG
            //                Console.WriteLine($"      🛑 [LineSearch] Abortado: Salto propuesto (dx={dxAdim:E4}) es ruido numérico.");
            //#endif
            //                return (false, xAdim, Math.Abs(currentFAdim), currentFReal);
            //            }

            double alpha = 1.0;
            double f0 = Math.Abs(currentFAdim);
            double f0_sq = f0 * f0;

            for (int k = 0; k < 5; k++)
            {
                double testX = xAdim + alpha * dxAdim;
                double testFReal = func(testX * xNorm);
                double f1 = Math.Abs(testFReal / fNorm);

#if DEBUG
                Console.WriteLine($"      🔍 [LineSearch] k={k}, alpha={alpha:F4} | error={f1:E4}");
#endif

                if (f1 < f0)
                {
                    return (true, testX, f1, testFReal);
                }

                // Interpolación cuadrática: usamos la caída de la parábola para calcular el freno exacto
                double f1_sq = f1 * f1;
                double theta = f0_sq / (f0_sq + f1_sq);

                // Guardrail térmico: ni muy suave, ni muy agresivo
                theta = Math.Max(0.1, Math.Min(0.5, theta));
                alpha *= theta;
            }

#if DEBUG
            Console.WriteLine($"      🛑 [LineSearch] Abortado: 5 intentos agotados sin mejoría.");
#endif
            return (false, xAdim, f0, currentFReal);
        }

        private static ScalarSolverResult LogSuccess(string tag, bool converged, int iter, double xReal, double fReal, System.Diagnostics.Stopwatch sw)
        {
#if DEBUG
            sw?.Stop();
            string status = converged ? "✅ Convergió" : "❌ Falló";
            Console.WriteLine($"[DEBUG-{tag}] {status} en {iter} iters | x={xReal:F6}, f={fReal:E4} | Tiempo: {sw?.Elapsed.TotalMilliseconds:F3} ms\n");
#endif
            return new ScalarSolverResult(converged, iter, xReal, fReal);
        }

    }
}