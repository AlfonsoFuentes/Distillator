using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.ControlValves;
using Shared.UnitOperations.Pumps;
using Shared.UnitOperations.Streams;

namespace Shared.MatrixSolvers
{
    public enum ReactiveEquationType
    {
        Model,
        Connection,
        Specification,
        Thermodynamic,
        EnergyBalance,
        ComponentBalance,
        PhaseEquilibrium,
        OperationalConstraint
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 ENUM: SolverStatus (sin cambios)
    // ───────────────────────────────────────────────────────────────
    public enum SolverStatus
    {
        NotStarted,
        Converged,
        PartialConvergence,
        NotConverged,
        NoEquations,
        Error
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 CLASE: SolverResult (sin cambios)
    // ───────────────────────────────────────────────────────────────
    public class SolverResult
    {
        public SolverStatus Status { get; set; } = SolverStatus.NotStarted;
        public int Iterations { get; set; }
        public int IterationCount { get; set; }
        public double MaxResidual { get; set; }
        public double FinalResidual { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool Success => Status == SolverStatus.Converged || Status == SolverStatus.PartialConvergence;
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 CLASE: GlobalEquation (CORREGIDA: usa IVariable)
    // ───────────────────────────────────────────────────────────────
    public class GlobalEquation
    {
        /// <summary>
        /// Función que calcula el residual de la ecuación
        /// Recibe lista de IVariable y retorna f(x)
        /// </summary>
        public Func<List<IVariable>, double> Function { get; set; } = null!;

        /// <summary>
        /// Tipo de ecuación (ReactiveEquationType)
        /// </summary>
        public ReactiveEquationType Type { get; set; }

        /// <summary>
        /// ID del equipo que generó esta ecuación
        /// </summary>
        public string EquipmentId { get; set; } = string.Empty;

        /// <summary>
        /// Descripción opcional de la ecuación
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 CLASE: GlobalEquationSystem (CORREGIDA: usa IVariable e IEquipmentFacade)
    // ───────────────────────────────────────────────────────────────
    public class GlobalEquationSystem
    {
        public List<GlobalEquation> Equations { get; } = new();
        public List<IVariable> Variables { get; } = new();

        public void AddEquation(GlobalEquation equation, IEnumerable<IVariable> involvedVariables)
        {
            if (equation == null) return;
            Equations.Add(equation);
            if (involvedVariables != null)
            {
                foreach (var v in involvedVariables)
                {
                    if (v != null && !Variables.Contains(v))
                        Variables.Add(v);
                }
            }
        }

        public void AddEquationsFromEquipment(IEquipmentFacade equipment, List<IVariable> allVariables)
        {
            if (equipment == null) return;

            var reactiveEquations = equipment.GetReactiveEquations(allVariables);

            if (reactiveEquations == null || reactiveEquations.Count == 0)
                return;

            foreach (var eq in reactiveEquations)
            {
                AddEquation(eq, allVariables?.Where(v => v != null).ToList() ?? new List<IVariable>());
            }
        }

        public double[] EvaluateResiduals()
        {
            return Equations?.Select(eq => eq?.Function?.Invoke(Variables) ?? 0).ToArray() ?? Array.Empty<double>();
        }

        public double[,] ComputeJacobianNumerical(double epsilon = 1e-6)
        {
            var varsToSolve = Variables?.Where(v => v?.IsToDefineByNewSolver == true).ToList() ?? new List<IVariable>();
            var n = Equations?.Count ?? 0;
            var m = varsToSolve.Count;

            if (n == 0 || m == 0) return new double[0, 0];

            var jacobian = new double[n, m];
            var baseValues = varsToSolve.Select(v => v?.NewSolverValue ?? v?.GetSolverValue() ?? 0).ToArray();

            for (int j = 0; j < m; j++)
            {
                var perturbed = baseValues.ToArray();
                perturbed[j] += epsilon;
                SetPerturbedValues(varsToSolve, perturbed);

                var fPlus = EvaluateResiduals();
                var fBase = EvaluateResiduals();

                for (int i = 0; i < n; i++)
                {
                    jacobian[i, j] = (fPlus[i] - fBase[i]) / epsilon;
                }

                SetPerturbedValues(varsToSolve, baseValues);
            }

            return jacobian;
        }

        private void SetPerturbedValues(List<IVariable> vars, double[] values)
        {
            if (vars == null || values == null) return;
            int idx = 0;
            foreach (var v in vars.Where(v => v?.IsToDefineByNewSolver == true))
            {
                if (v != null && idx < values.Length)
                {
                    v.NewSolverValue = values[idx++];
                }
            }
        }

        public void ApplySolution()
        {
            foreach (var v in Variables?.Where(v => v?.IsToDefineByNewSolver == true && v.NewSolverValue.HasValue) ?? Enumerable.Empty<IVariable>())
            {
                if (v != null)
                {
                    v.SolverValue = v.NewSolverValue!.Value;
                }
            }
        }
    }


    public class ReactiveNewtonSolver
    {
        private readonly List<IEquipmentFacade> _equipments;
        private readonly List<IStreamFacade> _streams;
        private readonly double _tolerance;
        private readonly int _maxIterations;
        private readonly bool _enablePartialSolve;

        public ReactiveNewtonSolver(
            List<IEquipmentFacade> equipments,
            List<IStreamFacade> streams,
            double tolerance = 1e-4,
            int maxIterations = 100,
            bool enablePartialSolve = true)
        {
            _equipments = equipments ?? new List<IEquipmentFacade>();
            _streams = streams ?? new List<IStreamFacade>();
            _tolerance = tolerance;
            _maxIterations = maxIterations;
            _enablePartialSolve = enablePartialSolve;
        }

        public SolverResult Solve()
        {
            var result = new SolverResult();
            try
            {
                PrepareVariables();
                var globalSystem = BuildGlobalSystem();
                if (globalSystem?.Equations?.Count == 0)
                {
                    result.Status = SolverStatus.NoEquations;
                    return result;
                }
                bool converged = SolveNewtonRaphson(globalSystem!, result);
                if (converged)
                {
                    globalSystem?.ApplySolution();
                    result.Status = SolverStatus.Converged;
                }
                else if (_enablePartialSolve)
                {
                    ApplyPartialSolution(globalSystem!);
                    result.Status = SolverStatus.PartialConvergence;
                }
                else
                {
                    result.Status = SolverStatus.NotConverged;
                }
                result.Iterations = result.IterationCount;
                result.FinalResidual = globalSystem?.EvaluateResiduals()?.Max(Math.Abs) ?? 0;
            }
            catch (Exception ex)
            {
                result.Status = SolverStatus.Error;
                result.ErrorMessage = ex.Message;
            }
            return result;
        }

        private void PrepareVariables()
        {
            // Streams
            foreach (var stream in _streams)
            {
                if (stream?.Pressure != null) ResetVariableFlags(stream.Pressure);
                if (stream?.Temperature != null) ResetVariableFlags(stream.Temperature);
                if (stream?.MassFlow != null) ResetVariableFlags(stream.MassFlow);
            }

            // 🔥 GENÉRICO: El equipo dice qué variables controla
            foreach (var equipment in _equipments)
            {
                foreach (var variable in equipment.GetControlledVariables())
                {
                    ResetVariableFlags(variable);
                }
            }

            // Marcar para resolver
            foreach (var stream in _streams)
            {
                if (stream?.Pressure != null) MarkVariableForSolving(stream.Pressure);
                if (stream?.Temperature != null) MarkVariableForSolving(stream.Temperature);
                if (stream?.MassFlow != null) MarkVariableForSolving(stream.MassFlow);
            }
        }

        private void ResetVariableFlags(IVariable v)
        {
            if (v == null) return;
            v.IsToDefineByNewSolver = false;
            v.NewSolverValue = null;
        }

        private void MarkVariableForSolving(IVariable v)
        {
            if (v == null) return;
            if (!v.IsDefinedByUI)
            {
                v.IsToDefineByNewSolver = true;
                v.NewSolverValue = v.GetSolverValue();
            }
        }

        private GlobalEquationSystem BuildGlobalSystem()
        {
            var system = new GlobalEquationSystem();
            var allVariables = CollectAllVariables();
            if (allVariables != null) system.Variables.AddRange(allVariables);

            foreach (var equipment in _equipments)
            {
                if (equipment != null)
                    system.AddEquationsFromEquipment(equipment, allVariables ?? new List<IVariable>());
            }

            AddConnectionEquations(system, allVariables ?? new List<IVariable>());
            return system;
        }

        private List<IVariable> CollectAllVariables()
        {
            var vars = new List<IVariable>();

            // Variables de streams
            foreach (var stream in _streams)
            {
                AddIfNotNull(vars, stream.Pressure);
                AddIfNotNull(vars, stream.Temperature);
                AddIfNotNull(vars, stream.MassFlow);
            }

            // 🔥 GENÉRICO: El equipo dice qué variables controla
            foreach (var equipment in _equipments)
            {
                foreach (var variable in equipment.GetControlledVariables())
                {
                    AddIfNotNull(vars, variable);
                }
            }

            return vars;
        }

        private void AddIfNotNull(List<IVariable> list, IVariable v)
        {
            if (v != null && !list.Contains(v)) list.Add(v);
        }

        private void AddConnectionEquations(GlobalEquationSystem system, List<IVariable> allVars)
        {
            foreach (var equipment in _equipments)
            {
                // 🔥 GENÉRICO: Iterar sobre puertos del equipo
                foreach (var portName in equipment.GetPortNames())
                {
                    var stream1 = equipment!.GetConnectedStream(portName);
                    if (stream1?.MassFlow == null) continue;

                    // Buscar otro puerto con stream para hacer balance
                    foreach (var otherPort in equipment.GetPortNames())
                    {
                        if (otherPort == portName) continue;
                        var stream2 = equipment.GetConnectedStream(otherPort);
                        if (stream2?.MassFlow == null) continue;

                        // Agregar ecuación de balance de masa entre stream1 y stream2
                        system.AddEquation(new GlobalEquation
                        {
                            Function = (vars) =>
                            {
                                var m1 = vars?.FirstOrDefault(v => v == stream1.MassFlow)?.GetEffectiveSolverValue()
                                      ?? vars?.FirstOrDefault(v => v?.Index == stream1.MassFlow.Index)?.GetEffectiveSolverValue()
                                      ?? stream1.MassFlow.GetSolverValue();
                                var m2 = vars?.FirstOrDefault(v => v == stream2.MassFlow)?.GetEffectiveSolverValue()
                                      ?? vars?.FirstOrDefault(v => v?.Index == stream2.MassFlow.Index)?.GetEffectiveSolverValue()
                                      ?? stream2.MassFlow.GetSolverValue();
                                return m2 - m1;
                            },
                            Type = ReactiveEquationType.Connection,
                            EquipmentId = $"Connection_{equipment?.Id.ToString() ?? "Unknown"}_{portName}_{otherPort}",
                            Description = $"ṁ_{otherPort} = ṁ_{portName} (Balance de conexión)"
                        }, new[] { stream1.MassFlow, stream2.MassFlow });

                        break; // Solo un balance por par de puertos
                    }
                }
            }
        }

        private bool SolveNewtonRaphson(GlobalEquationSystem system, SolverResult result)
        {
            for (int iteration = 0; iteration < _maxIterations; iteration++)
            {
                result.IterationCount = iteration + 1;
                var residuals = system?.EvaluateResiduals() ?? Array.Empty<double>();
                var maxResidual = residuals.Length > 0 ? residuals.Max(Math.Abs) : 0;
                result.MaxResidual = maxResidual;
                if (maxResidual < _tolerance) return true;

                var jacobian = system?.ComputeJacobianNumerical();
                if (jacobian == null || jacobian.GetLength(0) == 0 || jacobian.GetLength(1) == 0)
                {
                    result.ErrorMessage = "Jacobian computation failed";
                    return false;
                }

                var delta = SolveLinearSystem(jacobian, residuals);
                if (delta == null)
                {
                    result.ErrorMessage = "Linear system solve failed";
                    return false;
                }

                int varIdx = 0;
                var varsToSolve = system?.Variables?.Where(v => v?.IsToDefineByNewSolver == true).ToList() ?? new List<IVariable>();
                foreach (var v in varsToSolve)
                {
                    if (v != null && varIdx < delta.Length)
                    {
                        var currentValue = v.NewSolverValue ?? v.GetSolverValue();
                        v.NewSolverValue = currentValue + delta[varIdx];
                        varIdx++;
                    }
                }
            }
            return false;
        }

        private double[] SolveLinearSystem(double[,] jacobian, double[] residuals)
        {
            if (jacobian == null || residuals == null) return null!;
            int n = jacobian.GetLength(0), m = jacobian.GetLength(1);
            if (n != residuals.Length || n == 0 || m == 0) return null!;

            var augmented = new double[n, m + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++) augmented[i, j] = jacobian[i, j];
                augmented[i, m] = -residuals[i];
            }

            for (int col = 0; col < Math.Min(n, m); col++)
            {
                int maxRow = col;
                for (int row = col + 1; row < n; row++)
                    if (Math.Abs(augmented[row, col]) > Math.Abs(augmented[maxRow, col])) maxRow = row;
                if (Math.Abs(augmented[maxRow, col]) < 1e-12) continue;
                if (maxRow != col)
                    for (int j = 0; j <= m; j++)
                    { var t = augmented[col, j]; augmented[col, j] = augmented[maxRow, j]; augmented[maxRow, j] = t; }
                var pivot = augmented[col, col];
                for (int j = 0; j <= m; j++) augmented[col, j] /= pivot;
                for (int row = 0; row < n; row++)
                {
                    if (row != col && Math.Abs(augmented[row, col]) > 1e-12)
                    {
                        var factor = augmented[row, col];
                        for (int j = 0; j <= m; j++) augmented[row, j] -= factor * augmented[col, j];
                    }
                }
            }

            var solution = new double[m];
            for (int i = 0; i < Math.Min(n, m); i++) solution[i] = augmented[i, m];
            return solution;
        }

        private void ApplyPartialSolution(GlobalEquationSystem system)
        {
            var residuals = system?.EvaluateResiduals() ?? Array.Empty<double>();
            for (int i = 0; i < (system?.Equations?.Count ?? 0); i++)
                if (i < residuals.Length && Math.Abs(residuals[i]) < _tolerance * 10) { }
            foreach (var v in system?.Variables?.Where(v => v?.IsToDefineByNewSolver == true && v.NewSolverValue.HasValue) ?? Enumerable.Empty<IVariable>())
                if (v != null) v.SolverValue = v.NewSolverValue!.Value;
        }
    }
}

