using Shared.SolverQwen.Simlations;

namespace Shared.SolverConsecutive
{
    public class NewtonSolver : INewtonSolver
    {
        private readonly HashSet<INewtonSolverObserver> _observers = new();

        ISolverEquation equation = null!;
        int MaxIterations { get; set; } = 50;
        double ToleranceResidual { get; set; } = 1e-4;

        double SingularityTolerance { get; set; } = 1e-12;
        double PerturbationFactor { get; set; } = 1e-4;
        double MinPerturbation { get; set; } = 1e-6;
        double Alpha = 1;
        List<IVariable> _adjustableVariables = null!;

        public void Subscribe(INewtonSolverObserver observer)
        {
            _observers.Add(observer);
        }

        public SolverResult Solve(ISolverEquation _equation, double _alpha = 1.0)
        {
            equation = _equation;
            _adjustableVariables = equation.AdjustableVariables().ToList();
            var initialVariableStates = CaptureVariableStates();
            var result = SolveCore(_alpha);
            if (!result.Converged)
            {
                RestoreVariableStates(initialVariableStates);
            }

            return result;
        }

        private SolverResult SolveCore(double _alpha)
        {
            var solvedVariableProcedence = GetSolvedVariableProcedence(equation);

            var F_old = equation.Residuals.ToArray();
            if (F_old.Any(residual => !double.IsFinite(residual)))
            {
                return new SolverResult(false, 0, double.MaxValue);
            }

            var x_old = _adjustableVariables.Select(v => v.GetSolverValue()).ToArray();
            int nEquations = F_old.Length;
            int nUnknowns = x_old.Length;

            if (nEquations == 0)
            {
                return new SolverResult(true, 0, 0);
            }
            if (nEquations != nUnknowns)
            {
                return new SolverResult(false, 0, double.MaxValue);
            }

            double normF_old = GetNorm(F_old);
            Alpha = _alpha;
            int iter = 0;

            if (normF_old < ToleranceResidual)
            {
                var InitialCheckResult = CheckInitialValues(normF_old, solvedVariableProcedence);
                if (InitialCheckResult.Converged)
                {
                    return InitialCheckResult;
                }
            }


            while (iter < MaxIterations)
            {
                double[,]? J = CalculateJacobian(x_old, F_old);
                if (J == null)
                {
                    return new SolverResult(false, iter, double.MaxValue);
                }

                double[] dx = LinearSystemSolver.Solve(J, F_old.Select(v => -v).ToArray(), SingularityTolerance);

                if (dx == null)
                {
                    return new SolverResult(false, iter, GetNorm(F_old));
                }
                var stepResult = ApplyDampedStep(x_old, dx, Alpha, F_old, solvedVariableProcedence);

                if (stepResult.Converged)
                {
                    return new SolverResult(true, iter + 1, stepResult.FinalError);
                }

                x_old = stepResult.XNew;
                F_old = equation.Residuals.ToArray();
                if (F_old.Any(residual => !double.IsFinite(residual)))
                {
                    return new SolverResult(false, iter, double.MaxValue);
                }

                double normF_new = GetNorm(F_old);

                Alpha = normF_new > normF_old * 1.1
                    ? Math.Max(Alpha * 0.5, 0.01)
                    : Math.Min(Alpha * 1.2, 1.0);

                iter++;
            }
            return new SolverResult(false, iter, GetNorm(F_old));
        }

        SolverResult CheckInitialValues(double normF_old, VariableDefinedBy solvedVariableProcedence)
        {
            bool isTrivialSolution = _adjustableVariables.All(v => Math.Abs(v.GetSolverValue()) <= 1e-10);

            if (isTrivialSolution)
            {
                // Retorna FALSE explícitamente. El sistema está en ceros por falta de datos.
                return new SolverResult(false, 0, normF_old);
            }


            foreach (IVariable variable in _adjustableVariables)
            {
                variable.SetValueFromSolver(variable.GetSolverValue(), solvedVariableProcedence);
            }

            NotifySolvedVariables();
            return new SolverResult(true, 0, normF_old);
        }

        private double GetNorm(double[] v) => Math.Sqrt(v.Sum(x => x * x));

        private double[,]? CalculateJacobian(double[] x_base, double[] F_base)
        {
            int n = x_base.Length, m = F_base.Length;
            double[,] J = new double[m, n];

            for (int j = 0; j < n; j++)
            {
                double originalValue = x_base[j];
                double h = Math.Max(Math.Abs(originalValue) * PerturbationFactor, MinPerturbation);

                _adjustableVariables[j].SetValueFromSolver(originalValue + h, VariableDefinedBy.Undefined);
                double[] F_pert = equation.Residuals.ToArray();

                if (F_pert.Any(residual => !double.IsFinite(residual)))
                {
                    _adjustableVariables[j].SetValueFromSolver(originalValue, VariableDefinedBy.Undefined);
                    return null;
                }

                for (int i = 0; i < m; i++)
                {
                    J[i, j] = (F_pert[i] - F_base[i]) / h;
                }

                _adjustableVariables[j].SetValueFromSolver(originalValue, VariableDefinedBy.Undefined);
            }

            return J;
        }

        private DampedStepResult ApplyDampedStep(double[] x, double[] dx, double alpha,
                                                 double[] F_old,
                                                 VariableDefinedBy solvedVariableProcedence)
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
                    _adjustableVariables[i].SetValueFromSolver(x_new[i], solvedVariableProcedence);
                }

                var residuals = equation.Residuals.ToArray();
                if (residuals.Any(residual => !double.IsFinite(residual)))
                {
                    currentAlpha *= 0.5;
                    continue;
                }
                double error = GetNorm(residuals);


                if (error < ToleranceResidual)
                {
                    for (int i = 0; i < x.Length; i++)
                    {
                        _adjustableVariables[i].SetValueFromSolver(x_new[i], solvedVariableProcedence);
                    }

                    NotifySolvedVariables();
                    return new DampedStepResult { XNew = x_new, Converged = true, FinalError = error };
                }

                if (error < minError)
                {
                    minError = error;
                    bestX = (double[])x_new.Clone();
                    break;
                }

                currentAlpha *= 0.5;
            }

            for (int k = 0; k < _adjustableVariables.Count; k++)
            {
                _adjustableVariables[k].SetValueFromSolver(bestX[k], VariableDefinedBy.Undefined);
            }
            return new DampedStepResult { XNew = bestX, Converged = false, FinalError = minError };
        }

        private void NotifySolvedVariables()
        {
            foreach (var variable in _adjustableVariables.Distinct())
            {
                if (!variable.IsCalculated)
                {
                    continue;
                }

                foreach (var observer in _observers)
                {
                    observer.OnVariableSolved(variable);
                }
            }
        }

        private static VariableDefinedBy GetSolvedVariableProcedence(ISolverEquation equation)
        {
            return equation.EquationTypeModifer == SolverEquationTypeModifier.Spec
                ? VariableDefinedBy.Specification
                : VariableDefinedBy.Solver;
        }

        private List<VariableState> CaptureVariableStates()
        {
            return _adjustableVariables
                .Select(variable => new VariableState(
                    variable,
                    variable.GetSolverValue(),
                    variable.DataProcedence))
                .ToList();
        }

        private static void RestoreVariableStates(IEnumerable<VariableState> states)
        {
            foreach (var state in states)
            {
                state.Variable.SetValueFromSolver(state.SolverValue, VariableDefinedBy.Undefined);
                if (state.DataProcedence == VariableDefinedBy.Undefined)
                {
                    ClearCalculatedProcedence(state.Variable);
                }
            }
        }

        private static void ClearCalculatedProcedence(IVariable variable)
        {
            variable.Clear(VariableDefinedBy.Solver);
            variable.Clear(VariableDefinedBy.Specification);
            variable.Clear(VariableDefinedBy.Equipment);
        }

        private sealed record VariableState(
            IVariable Variable,
            double SolverValue,
            VariableDefinedBy DataProcedence);

    }
}
