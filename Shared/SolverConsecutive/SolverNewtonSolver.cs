using Shared.SolverQwen.Simlations;

namespace Shared.SolverConsecutive
{
    public class NewtonSolver : INewtonSolver
    {
        ISolverEquation equation = null!;
        int MaxIterations { get; set; } = 50;
        double ToleranceResidual { get; set; } = 1e-4;

        double SingularityTolerance { get; set; } = 1e-12;
        double PerturbationFactor { get; set; } = 1e-4;
        double MinPerturbation { get; set; } = 1e-6;
        double Alpha = 1;
        List<IVariable> _adjustableVariables = null!;
        public SolverResult Solve(ISolverEquation _equation, double _alpha = 1.0)
        {
            equation = _equation;
            _adjustableVariables = equation.AdjustableVariables().ToList();

            var F_old = equation.Residuals.ToArray();
            var x_old = _adjustableVariables.Select(v => v.GetSolverValue()).ToArray();
            int nEquations = F_old.Length;
            int nUnknowns = x_old.Length;

            if (nEquations == 0)
            {
#if DEBUG
                Console.WriteLine($"[NR-Solver] 💥 No hay ecuaciones que resolver");
#endif
                return new SolverResult(true, 0, 0);
            }
            if (nEquations != nUnknowns)
            {
#if DEBUG
                Console.WriteLine($"[NR-Solver] 💥 No hay match de equeciones eq:{nEquations} : vari{nUnknowns}");
#endif
                return new SolverResult(false, 0, double.MaxValue);
            }

            double normF_old = GetNorm(F_old);
            Alpha = _alpha;
            int iter = 0;

            if (normF_old < ToleranceResidual)
            {
                var InitialCheckResult = CheckInitialValues(normF_old);
                if (InitialCheckResult.Converged)
                {
                    return InitialCheckResult;
                }
            }


            while (iter < MaxIterations)
            {
                double[,] J = CalculateJacobian(x_old, F_old);
                double[] dx = LinearSystemSolver.Solve(J, F_old.Select(v => -v).ToArray(), SingularityTolerance);

                if (dx == null)
                {
#if DEBUG
                    Console.WriteLine($"[NR-Solver] 💥 MATRIZ SINGULAR DETECTADA en iteración {iter}. Abortando paso.");
#endif
                    return new SolverResult(false, iter, GetNorm(F_old));
                }
                var stepResult = ApplyDampedStep(x_old, dx, Alpha, F_old);

                if (stepResult.Converged)
                {
#if DEBUG
                    Console.WriteLine($"[NR-Solver] 🏆 ¡CONVERGENCIA ALCANZADA! en iter {iter + 1} | Error Final: {stepResult.FinalError:E4}");
#endif
                    return new SolverResult(true, iter + 1, stepResult.FinalError);
                }

                x_old = stepResult.XNew;
                F_old = equation.Residuals.ToArray();

                double normF_new = GetNorm(F_old);
#if DEBUG
                Console.WriteLine($"[NR-Solver] 🏁 Fin Iter {iter} | Nuevo Error: {normF_new:E4}");
#endif

                Alpha = normF_new > normF_old * 1.1
                    ? Math.Max(Alpha * 0.5, 0.01)
                    : Math.Min(Alpha * 1.2, 1.0);

                iter++;
            }
#if DEBUG
            Console.WriteLine($"[NR-Solver] ❌ Máximo de iteraciones ({MaxIterations}) alcanzado sin convergencia. Error Final: {GetNorm(F_old):E4}");
#endif
            return new SolverResult(false, iter, GetNorm(F_old));
        }

        SolverResult CheckInitialValues(double normF_old)
        {
            bool isTrivialSolution = _adjustableVariables.All(v => Math.Abs(v.GetSolverValue()) <= 1e-10);

            if (isTrivialSolution)
            {
#if DEBUG
                Console.WriteLine($"[NR-Solver] ⚠️ Convergencia trivial detectada (Estado inicial en ceros). Retornando False.");
#endif
                // Retorna FALSE explícitamente. El sistema está en ceros por falta de datos.
                return new SolverResult(false, 0, normF_old);
            }

#if DEBUG
            Console.WriteLine($"[NR-Solver] ✅ Convergencia inmediata (Error inicial {normF_old:E4} < {ToleranceResidual}).");
#endif

            foreach (IVariable variable in _adjustableVariables)
            {
                variable.SetValueFromSolver(variable.GetSolverValue(), VariableDefinedBy.Solver);
            }

            return new SolverResult(true, 0, normF_old);
        }

        private double GetNorm(double[] v) => Math.Sqrt(v.Sum(x => x * x));

        private double[,] CalculateJacobian(double[] x_base, double[] F_base)
        {
            int n = x_base.Length, m = F_base.Length;
            double[,] J = new double[m, n];

            for (int j = 0; j < n; j++)
            {
                double originalValue = x_base[j];
                double h = Math.Max(Math.Abs(originalValue) * PerturbationFactor, MinPerturbation);

                _adjustableVariables[j].SetValueFromSolver(originalValue + h, VariableDefinedBy.Undefined);
                double[] F_pert = equation.Residuals.ToArray();

                for (int i = 0; i < m; i++)
                {
                    J[i, j] = (F_pert[i] - F_base[i]) / h;
#if DEBUG
                    if (double.IsNaN(J[i, j]) || double.IsInfinity(J[i, j]))
                    {
                        Console.WriteLine($"[NR-Jacobian] 🚨 PELIGRO: Derivada (NaN o Infinito) detectada en Ecuación {i} al perturbar Variable '{_adjustableVariables[j].Name}'");
                    }
#endif
                }

                _adjustableVariables[j].SetValueFromSolver(originalValue, VariableDefinedBy.Undefined);
            }

            return J;
        }

        private DampedStepResult ApplyDampedStep(double[] x, double[] dx, double alpha,
                                                 double[] F_old)
        {
            double[] x_new = new double[x.Length];
            double minError = GetNorm(F_old);
            double[] bestX = (double[])x.Clone();
            double currentAlpha = alpha;

            for (int k = 0; k < 5; k++)
            {
                foreach (var v in _adjustableVariables.Where(v => v.IsCleareable))
                    v.Clear(VariableDefinedBy.Undefined);

                for (int i = 0; i < x.Length; i++)
                {
                    x_new[i] = x[i] + currentAlpha * dx[i];
                    _adjustableVariables[i].SetValueFromSolver(x_new[i], VariableDefinedBy.Solver);
                }

                var residuals = equation.Residuals.ToArray();
                double error = GetNorm(residuals);

#if DEBUG
                Console.WriteLine($"  [NR-DampedStep] Intento {k + 1}/5 | Alpha: {currentAlpha:F4} | Error Obj: {error:E4} | Min Histórico: {minError:E4}");
#endif

                if (error < ToleranceResidual)
                    return new DampedStepResult { XNew = x_new, Converged = true, FinalError = error };

                if (error < minError)
                {
                    minError = error;
                    bestX = (double[])x_new.Clone();
#if DEBUG
                    Console.WriteLine($"  [NR-DampedStep] ✨ Mejor paso encontrado. Aceptando estado.");
#endif
                    break;
                }

#if DEBUG
                Console.WriteLine($"  [NR-DampedStep] 🔙 El error aumentó. Reduciendo Alpha a la mitad...");
#endif
                currentAlpha *= 0.5;
            }

            for (int k = 0; k < _adjustableVariables.Count; k++)
            {
                _adjustableVariables[k].SetValueFromSolver(bestX[k], VariableDefinedBy.Undefined);
            }
            return new DampedStepResult { XNew = bestX, Converged = false, FinalError = minError };
        }

    }
}
