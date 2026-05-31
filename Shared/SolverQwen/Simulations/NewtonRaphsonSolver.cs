using Shared.SolverQwen.Variables;
using UnitSystem;

namespace Shared.SolverQwen.Simlations
{
    public class NewtonRaphsonSolver
    {
        public int MaxIterations { get; set; } = 50;
        public double ToleranceResidual { get; set; } = 1e-4;
        public double ToleranceStep { get; set; } = 1e-6;
        public double SingularityTolerance { get; set; } = 1e-12;
        public double PerturbationFactor { get; set; } = 1e-4;
        public double MinPerturbation { get; set; } = 1e-6;

        public SolverResult Solve(ISimulationSystem system, VariableDataProcedence procedence)
        {
            List<IProcessVariable> activeVariables = system.CouplingVariables.OfType<IProcessVariable>().ToList();

            int nEquations = system.GetResiduals().Length;
            int nUnknowns = activeVariables.Count;

            if (nEquations != nUnknowns)
                return new SolverResult(false, 0, double.MaxValue);

            double[] x_old = activeVariables.Select(v => v.GetSolverValue()).ToArray();
            double[] F_old = system.GetResiduals();

            int iter = 0;
            double alpha = 0.1;
            double normF_old = GetNorm(F_old);

            if (normF_old < ToleranceResidual)
            {
                // 🚨 FILTRO DE FALSO POSITIVO: Convergencia Trivial
                bool isTrivialSolution = activeVariables.All(v => Math.Abs(v.GetSolverValue()) <= 1e-10);

                if (isTrivialSolution)
                {
                    // Retorna FALSE explícitamente. El sistema está en ceros por falta de datos.
                    return new SolverResult(false, 0, normF_old);
                }

                foreach (IProcessVariable variable in activeVariables)
                {
                    variable.SetValueFromSolver(variable.GetSolverValue(), procedence);
                }

                return new SolverResult(true, 0, normF_old);
            }

            while (iter < MaxIterations)
            {
                double[,] J = CalculateJacobian(system, x_old, F_old, procedence, activeVariables);
                double[] dx = LinearSystemSolver.Solve(J, F_old.Select(v => -v).ToArray(), SingularityTolerance);

                if (dx == null)
                    return new SolverResult(false, iter, GetNorm(F_old));

                normF_old = GetNorm(F_old);
                var stepResult = ApplyDampedStep(system, x_old, dx, alpha, F_old, procedence, activeVariables);

                if (stepResult.Converged)
                    return new SolverResult(true, iter + 1, stepResult.FinalError);

                x_old = stepResult.XNew;
                F_old = system.GetResiduals();

                double normF_new = GetNorm(F_old);
                alpha = normF_new > normF_old * 1.1
                    ? Math.Max(alpha * 0.5, 0.01)
                    : Math.Min(alpha * 1.2, 1.0);

                iter++;
            }

            return new SolverResult(false, iter, GetNorm(F_old));
        }

        private void InjectStateVector(double[] vector, VariableDataProcedence procedence, List<IProcessVariable> vars)
        {
            for (int i = 0; i < vars.Count; i++)
                vars[i].SetValueFromSolver(vector[i], procedence);
        }

        private double[,] CalculateJacobian(ISimulationSystem system, double[] x_base, double[] F_base,
                                   VariableDataProcedence procedence, List<IProcessVariable> activeVariables)
        {
            int n = x_base.Length, m = F_base.Length;
            double[,] J = new double[m, n];

            for (int j = 0; j < n; j++)
            {
                double originalValue = x_base[j];
                double h = Math.Max(Math.Abs(originalValue) * PerturbationFactor, MinPerturbation);

                activeVariables[j].SetValueFromSolver(originalValue + h, VariableDataProcedence.Undefined);
                double[] F_pert = system.GetResiduals();

                for (int i = 0; i < m; i++)
                    J[i, j] = (F_pert[i] - F_base[i]) / h;

                activeVariables[j].SetValueFromSolver(originalValue, VariableDataProcedence.Undefined);
            }

            return J;
        }

        private DampedStepResult ApplyDampedStep(ISimulationSystem system, double[] x, double[] dx, double alpha,
                                                 double[] F_old, VariableDataProcedence procedence,
                                                 List<IProcessVariable> activeVariables)
        {
            double[] x_new = new double[x.Length];
            double minError = GetNorm(F_old);
            double[] bestX = (double[])x.Clone();
            double currentAlpha = alpha;

            for (int k = 0; k < 5; k++)
            {
                foreach (var v in activeVariables.Where(v => v.IsCleareable))
                    v.ClearForSolver(procedence);

                for (int i = 0; i < x.Length; i++)
                {
                    x_new[i] = x[i] + currentAlpha * dx[i];
                    activeVariables[i].SetValueFromSolver(x_new[i], procedence);
                }

                var residuals = system.GetResiduals();
                double error = GetNorm(residuals);

                if (error < ToleranceResidual)
                    return new DampedStepResult { XNew = x_new, Converged = true, FinalError = error };

                if (error < minError)
                {
                    minError = error;
                    bestX = (double[])x_new.Clone();
                    break;
                }
                currentAlpha *= 0.5;
            }

            InjectStateVector(bestX, VariableDataProcedence.Undefined, activeVariables);
            return new DampedStepResult { XNew = bestX, Converged = false, FinalError = minError };
        }

        private double GetNorm(double[] v) => Math.Sqrt(v.Sum(x => x * x));
    }

    public class NewtonRaphsonSolver2
    {
        public int MaxIterations { get; set; } = 50;
        public double ToleranceResidual { get; set; } = 1e-4;
        public double ToleranceStep { get; set; } = 1e-6;
        public double SingularityTolerance { get; set; } = 1e-12;
        // En NewtonRaphsonSolver:
        public double PerturbationFactor { get; set; } = 1e-5;  // ← Cambiar de 1e-3 a 1e-4
        public double MinPerturbation { get; set; } = 1e-7;     // ← Cambiar de 1e-4 a 1e-6

        public SolverResult Solve(ISimulationSystem system, VariableDataProcedence procedence)
        {
            List<IProcessVariable> activeVariables = system.CouplingVariables.OfType<IProcessVariable>().ToList();

            int nEquations = system.GetResiduals().Length;
            int nUnknowns = activeVariables.Count;
            if (nEquations != nUnknowns)
                return new SolverResult(false, 0, double.MaxValue);

            double[] x_old = activeVariables.Select(v => v.GetSolverValue()).ToArray();
            double[] F_old = system.GetResiduals();

            int iter = 0;
            double alpha = 1;  // ← Empezar con 0.1 en lugar de 1.0
            double normF_old = GetNorm(F_old);
    
            if (normF_old < ToleranceResidual)
            {
                // 🚨 FILTRO DE FALSO POSITIVO:
                // Si las variables están en cero (no inicializadas), es una solución trivial.
                // No debemos sellarlas con la procedencia, porque engañará al Orquestador.
                bool isTrivialSolution = activeVariables.All(v => Math.Abs(v.GetSolverValue()) <= 1e-10);

                if (!isTrivialSolution)
                {
                    // Solo estampamos la procedencia si la solución tiene valores reales
                    foreach (IProcessVariable variable in activeVariables)
                    {
                        variable.SetValueFromSolver(variable.GetSolverValue(), procedence);
                    }
                }

                return new SolverResult(true, 0, normF_old);
            }
            while (iter < MaxIterations)
            {
                // 1. Jacobiano (costoso, pero optimizado con Undefined)
                double[,] J = CalculateJacobian(system, x_old, F_old, procedence, activeVariables);
                double[] dx = LinearSystemSolver.Solve(J, F_old.Select(v => -v).ToArray(), SingularityTolerance);

                // ✅ REMOVIDO: Check temprano de convergencia numérica
                // Por qué: Incluso si normF < tolerance, necesitamos ApplyDampedStep para actualizar DataProcedence

                if (dx == null)
                    return new SolverResult(false, iter, GetNorm(F_old));

                // Guardar error anterior para ajuste de alpha
                normF_old = GetNorm(F_old);

                // 2. Paso amortiguado: AHORA también decide convergencia (numérica + metadata)
                var stepResult = ApplyDampedStep(system, x_old, dx, alpha, F_old, procedence, activeVariables);

                // ✅ Si ApplyDampedStep convergió (valores + metadata correctos), retornar
                if (stepResult.Converged)
                    return new SolverResult(true, iter + 1, stepResult.FinalError);

                // 3. Preparar siguiente iteración (solo si no convergimos)
                x_old = stepResult.XNew;
                F_old = system.GetResiduals();

                // Ajuste de alpha: comparar nuevo error vs anterior
                double normF_new = GetNorm(F_old);
                alpha = normF_new > normF_old * 1.1
                    ? Math.Max(alpha * 0.5, 0.01)
                    : Math.Min(alpha * 1.2, 1.0);

                iter++;
            }

            return new SolverResult(false, iter, GetNorm(F_old));
        }

        private void InjectStateVector(double[] vector, VariableDataProcedence procedence, List<IProcessVariable> vars)
        {

            for (int i = 0; i < vars.Count; i++)
                vars[i].SetValueFromSolver(vector[i], procedence);
        }
        private double[,] CalculateJacobian(ISimulationSystem system, double[] x_base, double[] F_base,
                                   VariableDataProcedence procedence, List<IProcessVariable> activeVariables)
        {
            int n = x_base.Length, m = F_base.Length;
            double[,] J = new double[m, n];

            for (int j = 0; j < n; j++)
            {
                double originalValue = x_base[j];
                double h = Math.Max(Math.Abs(originalValue) * PerturbationFactor, MinPerturbation);

                // 1️⃣ Perturbar variable j
                activeVariables[j].SetValueFromSolver(originalValue + h, VariableDataProcedence.Undefined);

                // 2️⃣ Calcular F_pert con sistema "semi-limpio" (solo x[j] perturbado)
                double[] F_pert = system.GetResiduals();

                // 3️⃣ Calcular columna j
                for (int i = 0; i < m; i++)
                    J[i, j] = (F_pert[i] - F_base[i]) / h;

                // 4️⃣ ✅ RESTAURAR INMEDIATAMENTE variable j
                activeVariables[j].SetValueFromSolver(originalValue, VariableDataProcedence.Undefined);
            }

            // Ya no necesitas InjectStateVector al final. El sistema queda limpio automáticamente.
            return J;
        }


        private DampedStepResult ApplyDampedStep(ISimulationSystem system, double[] x, double[] dx, double alpha,
                                                 double[] F_old, VariableDataProcedence procedence,
                                                 List<IProcessVariable> activeVariables)
        {
            double[] x_new = new double[x.Length];
            double minError = GetNorm(F_old);
            double[] bestX = (double[])x.Clone();
            double currentAlpha = alpha;

            for (int k = 0; k < 5; k++)
            {
                foreach (var v in activeVariables.Where(v => v.IsCleareable))
                    v.ClearForSolver(procedence);

                for (int i = 0; i < x.Length; i++)
                {
                    x_new[i] = x[i] + currentAlpha * dx[i];
                    activeVariables[i].SetValueFromSolver(x_new[i], procedence); // 🔥 Termodinámica
                }

                var residuals = system.GetResiduals();
                double error = GetNorm(residuals); // 🔥 Más termodinámica

                // ✅ EARLY-EXIT POR TOLERANCIA: ¡Ya convergimos!
                if (error < ToleranceResidual)
                    return new DampedStepResult { XNew = x_new, Converged = true, FinalError = error };

                // ✅ GREEDY: Primera mejora es suficiente
                if (error < minError)
                {
                    minError = error;
                    bestX = (double[])x_new.Clone();
                    break;
                }
                currentAlpha *= 0.5;
            }

            // Fallback: aplicar mejor estado si no convergimos temprano
            InjectStateVector(bestX, VariableDataProcedence.Undefined, activeVariables);
            return new DampedStepResult { XNew = bestX, Converged = false, FinalError = minError };
        }

        private double GetNorm(double[] v) => Math.Sqrt(v.Sum(x => x * x));
    }

    public class DampedStepResult
    {
        public double[] XNew { get; set; } = null!;
        public bool Converged { get; set; }
        public double FinalError { get; set; }
    }

}
