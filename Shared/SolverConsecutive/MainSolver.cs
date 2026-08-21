using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Simlations;
using Shared.UnitOperations.Basiss;
using UnitSystem;

namespace Shared.SolverConsecutive
{
    public sealed class MainSolver : IMainSolver, INewtonSolverObserver
    {
        private const double ResidualSolvedTolerance = 1e-4;

        private static readonly SolverEquationType[] DefaultEquationTypes =
        [
            SolverEquationType.Pressure,
            SolverEquationType.Concentration,
            SolverEquationType.VaporFraction,
            SolverEquationType.Enthalpy,
            SolverEquationType.Specification,
            SolverEquationType.MassBalance,
            SolverEquationType.MassEnergyBalance
        ];

        private static readonly string[] DebugEquipmentNames = ["C-140", "C-101", "C-127"];

        private readonly INewtonSolver _solver;
        private readonly HashSet<IVariable> _solverCalculatedVariables = new();
        private readonly List<string> _divergenceProbeRunEvents = new();
        private readonly object _simulationSyncRoot = new();
        private Task? _simulationCoordinatorTask;
        private bool _simulationRerunRequested;
        private TaskCompletionSource<SimulationRunResult>? _rerunCompletion;
        private string? _activeSolverEquationName;

        public MainSolver()
            : this(new NewtonSolver())
        {
        }

        public MainSolver(INewtonSolver solver)
        {
            _solver = solver;
           
            AtmosphericPressure = new Pressure(101325, PressureUnits.Pascala);
            _altitude = new Length(0, LengthUnits.Meter);
        }

        public event Action? OnSimulationCompleted;

        public List<IFacadeStream> Streams { get; } = new();

        public List<ISolverEquipment> Equipments { get; } = new();

        public ThermodynamicMethodFullDto ThermoMethod { get; set; } = null!;
        private ISolverTraceSink? _traceSink;
        public ISolverTraceSink? TraceSink
        {
            get => _traceSink;
            set
            {
                _traceSink = value;
                foreach (var stream in Streams)
                {
                    stream.TraceSink = value;
                }

                foreach (var equipment in Equipments.OfType<SolverEquipmentBase>())
                {
                    equipment.TraceSink = value;
                }
            }
        }

        public Pressure AtmosphericPressure { get; set; }

        private Length _altitude = null!;
        public Length Altitude
        {
            get => _altitude;
            set
            {
                _altitude = value;
                UpdateAtmosphericPressure();
            }
        }

        public void AddStream(IFacadeStream stream)
        {
            stream.TraceSink = TraceSink;
            Streams.Add(stream);
        }

        public void RemoveStream(IFacadeStream stream)
        {
            Streams.Remove(stream);
        }

        public void AddEquipment(ISolverEquipment equipment)
        {
            if (equipment is SolverEquipmentBase solverEquipment)
            {
                solverEquipment.TraceSink = TraceSink;
            }

            Equipments.Add(equipment);
        }

        public void RemoveEquipment(ISolverEquipment equipment)
        {
            Equipments.Remove(equipment);
        }

        public Task<SimulationRunResult> RunSimulationAsync()
        {
            lock (_simulationSyncRoot)
            {
                if (_simulationCoordinatorTask is null || _simulationCoordinatorTask.IsCompleted)
                {
                    var firstRunCompletion = NewTaskCompletionSource();
                    _simulationCoordinatorTask = Task.Run(() => RunCoalescedSimulationLoopAsync(firstRunCompletion));
                    return firstRunCompletion.Task;
                }

                _simulationRerunRequested = true;
                _rerunCompletion ??= NewTaskCompletionSource();
                return _rerunCompletion.Task;
            }
        }

        private async Task RunCoalescedSimulationLoopAsync(TaskCompletionSource<SimulationRunResult> firstRunCompletion)
        {
            var currentRunCompletion = firstRunCompletion;

            while (true)
            {
                var result = await ExecuteSimulationAsync();

                lock (_simulationSyncRoot)
                {
                    if (!_simulationRerunRequested)
                    {
                        currentRunCompletion.TrySetResult(result);
                        _simulationCoordinatorTask = null;
                        return;
                    }

                    currentRunCompletion.TrySetResult(result.Supersede());

                    _simulationRerunRequested = false;
                    currentRunCompletion = _rerunCompletion ?? NewTaskCompletionSource();
                    _rerunCompletion = null;
                }
            }
        }

        private static TaskCompletionSource<SimulationRunResult> NewTaskCompletionSource()
        {
            return new TaskCompletionSource<SimulationRunResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private async Task<SimulationRunResult> ExecuteSimulationAsync()
        {
            var runId = Guid.NewGuid();
            var diagnostics = new List<string>();
            var converged = false;
            _activeSolverEquationName = null;
            _divergenceProbeRunEvents.Clear();

            try
            {
                TraceSolver("Run started", $"streams={Streams.Count}; equipment={Equipments.Count}");
                TraceDivergenceProbe("Run context", $"atmosphericPressure={DescribePressure(AtmosphericPressure)}; altitude={_altitude.GetValue(LengthUnits.Meter):G6} m");
                TraceWatchedStreams("Before clear");
                TraceCriticalBalances("Before clear");
                ClearCalculatedBySolver();
                TraceWatchedStreams("After clear");
                TraceCriticalBalances("After clear");
                var solvePlan = BuildFullSolvePlan();
                TraceSolver("Solve plan built", string.Join(", ", solvePlan.EquationsByType.Select(pair => $"{pair.Key}:{pair.Value.Count}")));
                TraceDivergenceProbe("Solve plan watch", DescribeWatchedSolvePlan(solvePlan));
                converged = ExecuteSolvePlan(solvePlan, diagnostics);
                var postSolveSucceeded = await ExecutePostSolveCalculationsAsync(diagnostics);
                TraceWatchedStreams("Before run completed");
                TraceCriticalBalances("Before run completed");
                TraceDivergenceProbeSummary();
                TraceSolver("Run completed", $"converged={converged}; postSolve={postSolveSucceeded}; diagnostics={diagnostics.Count}");
                return postSolveSucceeded
                    ? SimulationRunResult.Completed(runId, converged, diagnostics)
                    : SimulationRunResult.Failed(runId, diagnostics);
            }
            catch (Exception ex)
            {
                var message = $"[MainSolver] Error en simulacion: {ex.Message}";
                diagnostics.Add(message);
                TraceSolver("Run failed", ex.Message);
                OnSimulationCompleted?.Invoke();
                return SimulationRunResult.Failed(runId, diagnostics);
            }
        }

        private SolvePlan BuildFullSolvePlan()
        {
            var equationsByType = CreateRegularEquationsByType();
            AddStandaloneSpecificationEquations(equationsByType);
            AddSeedEquipmentSpecificationClusters(equationsByType);
            AddSpecificationClusters(equationsByType);
            return new SolvePlan(equationsByType);
        }

        private Dictionary<SolverEquationType, List<ISolverEquation>> CreateRegularEquationsByType()
        {
            var equationsByType = new Dictionary<SolverEquationType, List<ISolverEquation>>();

            foreach (var equationType in DefaultEquationTypes)
            {
                foreach (var equipment in Equipments)
                {
                    var equations = equipment.Equations
                        .Where(equation =>
                            equation.EquationType == equationType &&
                            equation.EquationTypeModifer == SolverEquationTypeModifier.Regular)
                        .ToList();

                    if (equations.Count == 0)
                    {
                        continue;
                    }

                    if (!equationsByType.TryGetValue(equationType, out var existingEquations))
                    {
                        existingEquations = new List<ISolverEquation>();
                        equationsByType[equationType] = existingEquations;
                    }

                    existingEquations.AddRange(equations);
                }
            }

            return equationsByType;
        }

        private void AddStandaloneSpecificationEquations(Dictionary<SolverEquationType, List<ISolverEquation>> equationsByType)
        {
            var standaloneSpecifications = BuildStandaloneSpecificationEquations();
            if (standaloneSpecifications.Count == 0)
            {
                return;
            }

            if (!equationsByType.TryGetValue(SolverEquationType.Specification, out var equations))
            {
                equations = new List<ISolverEquation>();
                equationsByType[SolverEquationType.Specification] = equations;
            }

            equations.AddRange(standaloneSpecifications);
        }

        private List<ISolverEquation> BuildStandaloneSpecificationEquations()
        {
            return Equipments
                .SelectMany(equipment => equipment.Specifications)
                .Select(specification => new SpecificationEquation(specification))
                .Cast<ISolverEquation>()
                .ToList();
        }

        private void AddSeedEquipmentSpecificationClusters(Dictionary<SolverEquationType, List<ISolverEquation>> equationsByType)
        {
            var specificationClusters = BuildSeedEquipmentSpecificationClusters();
            if (specificationClusters.Count == 0)
            {
                return;
            }

            if (!equationsByType.TryGetValue(SolverEquationType.Specification, out var equations))
            {
                equations = new List<ISolverEquation>();
                equationsByType[SolverEquationType.Specification] = equations;
            }

            equations.AddRange(specificationClusters);
        }

        private List<EquationClusterInDevelopment> BuildSeedEquipmentSpecificationClusters()
        {
            var clusters = new List<EquationClusterInDevelopment>();

            foreach (var equipment in Equipments.Where(equipment => equipment.Specifications.Any()))
            {
                foreach (var specification in equipment.Specifications)
                {
                    clusters.Add(BuildSeedEquipmentSpecificationCluster(equipment, specification));
                }
            }

            return clusters;
        }

        private EquationClusterInDevelopment BuildSeedEquipmentSpecificationCluster(
            ISolverEquipment seedEquipment,
            ISpecification specification)
        {
            var clusterStreams = GetSpecificationClusterStreams(seedEquipment, specification);
            var clusterVariables = GetSpecificationClusterVariables(clusterStreams, specification);

            var cluster = new EquationClusterInDevelopment(
                SolverEquationType.Specification,
                SolverEquationTypeModifier.Spec);

            var equations = seedEquipment.Equations
                .Where(equation => equation.EquationTypeModifer == SolverEquationTypeModifier.Regular)
                .Where(equation => IsRelevantForSpecificationCluster(equation, specification))
                .Where(equation => equation.Variables.Any(clusterVariables.Contains))
                .ToList();

            foreach (var equation in equations)
            {
                cluster.AddEquation(equation);
            }

            cluster.AddEquation(new SpecificationEquation(specification));
            return cluster;
        }

        private void AddSpecificationClusters(Dictionary<SolverEquationType, List<ISolverEquation>> equationsByType)
        {
            var specificationClusters = BuildSpecificationClusters();
            if (specificationClusters.Count == 0)
            {
                return;
            }

            if (!equationsByType.TryGetValue(SolverEquationType.Specification, out var equations))
            {
                equations = new List<ISolverEquation>();
                equationsByType[SolverEquationType.Specification] = equations;
            }

            equations.AddRange(specificationClusters);
        }

        private List<EquationClusterInDevelopment> BuildSpecificationClusters()
        {
            var clusters = new List<EquationClusterInDevelopment>();

            foreach (var equipment in Equipments.Where(equipment => equipment.Specifications.Any()))
            {
                foreach (var specification in equipment.Specifications)
                {
                    var clusterStreams = GetSpecificationClusterStreams(equipment, specification);
                    var clusterEquipments = GetEquipmentsConnectedTo(clusterStreams, equipment);
                    if (clusterEquipments.Count <= 1)
                    {
                        continue;
                    }

                    clusters.Add(BuildSpecificationCluster(equipment, specification, clusterStreams, clusterEquipments));
                }
            }

            return clusters;
        }

        private EquationClusterInDevelopment BuildSpecificationCluster(
            ISolverEquipment seedEquipment,
            ISpecification specification,
            HashSet<IFacadeStream> clusterStreams,
            HashSet<ISolverEquipment> clusterEquipments)
        {
            var clusterVariables = GetSpecificationClusterVariables(clusterStreams, specification);

            var cluster = new EquationClusterInDevelopment(
                SolverEquationType.Specification,
                SolverEquationTypeModifier.Spec);
            foreach (var equipment in clusterEquipments)
            {
                var equations = equipment.Equations
                    .Where(equation => equation.EquationTypeModifer == SolverEquationTypeModifier.Regular)
                    .Where(equation => equation.Variables.Any(clusterVariables.Contains))
                    .ToList();

                foreach (var equation in equations)
                {
                    cluster.AddEquation(equation);
                }
            }

            cluster.AddEquation(new SpecificationEquation(specification));
            return cluster;
        }

        private static bool IsRelevantForSpecificationCluster(
            ISolverEquation equation,
            ISpecification specification)
        {
            if (equation.EquationType == specification.TargetEquationType)
            {
                return true;
            }

            return specification.TargetEquationType == SolverEquationType.MassBalance
                && equation.EquationType == SolverEquationType.MassEnergyBalance;
        }

        private static HashSet<IFacadeStream> GetSpecificationClusterStreams(
            ISolverEquipment seedEquipment,
            ISpecification specification)
        {
            return new HashSet<IFacadeStream>(
                seedEquipment.Inlets
                    .Concat(seedEquipment.Outlets)
                    .Concat(specification.AssociatedStreams));
        }

        private static HashSet<ISolverEquipment> GetEquipmentsConnectedTo(
            IEnumerable<IFacadeStream> streams,
            ISolverEquipment seedEquipment)
        {
            var equipments = new HashSet<ISolverEquipment> { seedEquipment };

            foreach (var stream in streams)
            {
                if (stream.EquipmentInlet != null)
                {
                    equipments.Add(stream.EquipmentInlet);
                }

                if (stream.EquipmentOutlet != null)
                {
                    equipments.Add(stream.EquipmentOutlet);
                }
            }

            return equipments;
        }

        private static HashSet<IVariable> GetSpecificationClusterVariables(
            IEnumerable<IFacadeStream> streams,
            ISpecification specification)
        {
            var variables = specification.GetVariables().ToHashSet();

            if (specification is not StreamSpecificationBase streamSpecification)
            {
                return variables;
            }

            foreach (var stream in streams)
            {
                var variable = GetStreamVariable(stream, streamSpecification.VariableType);
                if (variable != null)
                {
                    variables.Add(variable);
                }
            }

            return variables;
        }

        private static IVariable? GetStreamVariable(IFacadeStream stream, SpecVariableType variableType)
        {
            return variableType switch
            {
                SpecVariableType.TotalMassFlow => stream.MassFlow,
                SpecVariableType.TotalMolarFlow => stream.MolarFlow,
                SpecVariableType.TotalVolumetricFlow => stream.VolumetricFlow,
                _ => null
            };
        }

        private bool ExecuteSolvePlan(SolvePlan solvePlan, List<string> diagnostics)
        {
            var pendingTypes = solvePlan.EquationTypesWithPendingWork().ToList();
            var specificationTargetVariables = GetSpecificationTargetVariables();
            var hasGlobalProgress = true;
            var iteration = 0;
            const int maxIterations = 10;

            while (hasGlobalProgress && iteration < maxIterations && pendingTypes.Count > 0)
            {
                hasGlobalProgress = false;

                foreach (var equationType in pendingTypes.ToList())
                {
                    var equations = solvePlan.EquationsByType[equationType];
                    var protectedVariables = equationType == SolverEquationType.Specification
                        ? new HashSet<IVariable>()
                        : specificationTargetVariables;
                    if (equationType == SolverEquationType.Specification)
                    {
                        TraceSolver("Specifications started", $"equations={equations.Count}");
                    }

                    var hasLocalProgress = ResolveEquationType(equationType, equations, protectedVariables);
                    hasGlobalProgress |= hasLocalProgress;

                    if (equationType == SolverEquationType.Specification)
                    {
                        TraceSolver("Specifications finished", $"remaining={equations.Count}; progress={hasLocalProgress}");
                    }

                    if (equations.Count == 0)
                    {
                        pendingTypes.Remove(equationType);
                    }
                }

                if (!hasGlobalProgress)
                {
                    break;
                }

                iteration++;
            }

            return LogSolvePlanResult(pendingTypes, iteration, diagnostics);
        }

        private HashSet<IVariable> GetSpecificationTargetVariables()
        {
            return Equipments
                .SelectMany(equipment => equipment.Specifications)
                .SelectMany(specification => specification.GetTargetVariables())
                .ToHashSet();
        }

        private bool ResolveEquationType(
            SolverEquationType equationType,
            List<ISolverEquation> equations,
            HashSet<IVariable> protectedVariables)
        {
            var hasProgress = false;
            var hasLocalProgress = true;

            while (hasLocalProgress)
            {
                hasLocalProgress = false;
                var clusteredEquations = ClusterEquations(equations);

                for (var index = 0; index < clusteredEquations.Count;)
                {
                    var equation = clusteredEquations[index];
                    var shouldTraceEquation = ShouldTraceEquation(equationType, equation);
                    var isDebugEquipmentEquation = IsDebugEquipmentEquation(equation);
                    var shouldTraceDiagnosticEquation = ShouldTraceDiagnosticEquation(equation) || isDebugEquipmentEquation;
                    if (isDebugEquipmentEquation)
                    {
                        TraceSolver("Debug equipment checkpoint", DescribeEquationCompact(equation));
                    }

                    if (ShouldDeferToSpecification(equation, protectedVariables))
                    {
                        if (shouldTraceEquation || shouldTraceDiagnosticEquation)
                        {
                            TraceSolver("Deferred to specification", DescribeEquationCompact(equation));
                        }

                        index++;
                        continue;
                    }

                    if (!equation.CanEvaluate)
                    {
                        if (shouldTraceEquation || shouldTraceDiagnosticEquation)
                        {
                            TraceSolver("Equation not ready", DescribeEquationCompact(equation));
                        }

                        index++;
                        continue;
                    }

                    equation.RefreshEquation();
                    var beforeSolve = CaptureEquationSnapshot(equation);
                    if (equation.AdjustableVariables().Count == 0)
                    {
                        if (IsResidualSolved(beforeSolve.ResidualNorm))
                        {
                            if (shouldTraceEquation || shouldTraceDiagnosticEquation)
                            {
                                TraceSolver("Equation satisfied", DescribeEquationCompact(equation));
                            }

                            RemoveSolvedEquation(equations, equation);
                            clusteredEquations.RemoveAt(index);
                            hasLocalProgress = true;
                            hasProgress = true;
                            continue;
                        }

                        if (shouldTraceEquation || shouldTraceDiagnosticEquation)
                        {
                            TraceSolver("Equation has no adjustable variables", DescribeNewtonStartCompact(equation, beforeSolve));
                        }

                        index++;
                        continue;
                    }

                    if (shouldTraceEquation || shouldTraceDiagnosticEquation)
                    {
                        TraceSolver("Newton start", DescribeNewtonStartCompact(equation, beforeSolve));
                    }

                    if (IsWatchedEquation(equation))
                    {
                        TraceDivergenceProbe("Before equation", DescribeWatchedEquation(equation, beforeSolve));
                    }

                    SolverResult result;
                    _activeSolverEquationName = equation.Name;
                    try
                    {
                        result = _solver.Solve(equation);
                    }
                    finally
                    {
                        _activeSolverEquationName = null;
                    }

                    var afterSolve = CaptureEquationSnapshot(equation);
                    if (IsWatchedEquation(equation))
                    {
                        TraceDivergenceProbe("After equation", DescribeWatchedEquationResult(equation, beforeSolve, afterSolve, result));
                    }

                    if (ShouldTraceNewtonResult(result, shouldTraceEquation, shouldTraceDiagnosticEquation, beforeSolve, afterSolve))
                    {
                        TraceSolver(
                            result.Converged ? "Newton solved" : "Newton failed",
                            DescribeNewtonResultCompact(equation, beforeSolve, afterSolve, result));
                    }

                    if (!result.Converged)
                    {
                        index++;
                        continue;
                    }

                    RemoveSolvedEquation(equations, equation);
                    clusteredEquations.RemoveAt(index);
                    hasLocalProgress = true;
                    hasProgress = true;
                }
            }

            return hasProgress;
        }

        private static bool IsResidualSolved(double residualNorm)
        {
            return double.IsFinite(residualNorm) && residualNorm < ResidualSolvedTolerance;
        }

        private static bool ShouldDeferToSpecification(
            ISolverEquation equation,
            HashSet<IVariable> protectedVariables)
        {
            return protectedVariables.Count > 0
                && equation.EquationTypeModifer == SolverEquationTypeModifier.Regular
                && equation.AdjustableVariables().Any(protectedVariables.Contains);
        }

        private static bool ShouldTraceEquation(SolverEquationType equationType, ISolverEquation equation)
        {
            return equationType == SolverEquationType.Specification ||
                   equation.Name.Contains("Formula", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldTraceDiagnosticEquation(ISolverEquation equation)
        {
            return IsWatchedEquation(equation);
        }

        private static bool IsDebugEquipmentEquation(ISolverEquation equation)
        {
            return equation.EquationType == SolverEquationType.MassEnergyBalance
                && DebugEquipmentNames.Any(name => equation.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsWatchedEquation(ISolverEquation equation)
        {
            return WatchedEquationNames.Any(name => equation.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
                   equation.Variables.Any(IsCriticalDiagnosticVariable);
        }

        private static bool ShouldTraceNewtonResult(
            SolverResult result,
            bool shouldTraceEquation,
            bool shouldTraceDiagnosticEquation,
            EquationSolveSnapshot before,
            EquationSolveSnapshot after)
        {
            if (result.Converged)
            {
                return true;
            }

            if (shouldTraceEquation || shouldTraceDiagnosticEquation)
            {
                return true;
            }

            return after.Variables.Any(pair =>
                IsDiagnosticVariable(pair.Key) &&
                DescribeVariableChange(pair.Key.Name, before.Variables[pair.Key], pair.Value).Length > 0);
        }

        private static List<ISolverEquation> ClusterEquations(List<ISolverEquation> equations)
        {
            var protectedClusters = equations
                .OfType<EquationClusterInDevelopment>()
                .Where(cluster => cluster.EquationType == SolverEquationType.Specification)
                .Cast<ISolverEquation>()
                .ToList();

            var unassignedEquations = equations
                .Where(equation => equation is not EquationClusterInDevelopment cluster ||
                    cluster.EquationType != SolverEquationType.Specification)
                .ToList();

            var discoveredClusters = BuildVariableSharingClusters(unassignedEquations)
                .Select(cluster => cluster.Count == 1
                    ? cluster[0]
                    : new EquationClusterInDevelopment(
                        cluster[0].EquationType,
                        SolverEquationTypeModifier.Spec,
                        cluster))
                .Cast<ISolverEquation>()
                .ToList();

            discoveredClusters.AddRange(protectedClusters);
            return discoveredClusters;
        }

        private static List<List<ISolverEquation>> BuildVariableSharingClusters(List<ISolverEquation> equations)
        {
            var clusters = new List<List<ISolverEquation>>();
            var unassigned = equations.ToList();

            while (unassigned.Count > 0)
            {
                var cluster = new List<ISolverEquation> { unassigned[0] };
                unassigned.RemoveAt(0);

                var expanded = true;
                while (expanded)
                {
                    expanded = false;
                    var clusterUnknowns = cluster
                        .SelectMany(equation => equation.AdjustableVariables())
                        .Distinct()
                        .ToList();

                    for (var index = unassigned.Count - 1; index >= 0; index--)
                    {
                        var candidate = unassigned[index];
                        var sharesUnknown = candidate.AdjustableVariables().Intersect(clusterUnknowns).Any();
                        var hasDifferentEquationType = cluster.All(equation => equation.EquationType != candidate.EquationType);

                        if (!sharesUnknown || !hasDifferentEquationType)
                        {
                            continue;
                        }

                        cluster.Add(candidate);
                        unassigned.RemoveAt(index);
                        expanded = true;
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;
        }

        private static void RemoveSolvedEquation(List<ISolverEquation> sourceEquations, ISolverEquation solvedEquation)
        {
            if (solvedEquation is EquationClusterInDevelopment cluster)
            {
                if (sourceEquations.Remove(cluster))
                {
                    return;
                }

                foreach (var equation in cluster.Equations)
                {
                    sourceEquations.Remove(equation);
                }

                return;
            }

            sourceEquations.Remove(solvedEquation);
        }

        private static bool LogSolvePlanResult(
            IReadOnlyCollection<SolverEquationType> pendingTypes,
            int iteration,
            List<string> diagnostics)
        {
            if (pendingTypes.Count == 0)
            {
                var message = $"[MainSolver] Todas las ecuaciones resueltas en {iteration} iteraciones globales";
                diagnostics.Add(message);
                return true;
            }

            var diagnostic = $"[MainSolver] Convergencia incompleta. Tipos sin resolver: {string.Join(", ", pendingTypes)}";
            diagnostics.Add(diagnostic);
            return false;
        }

        private void ClearCalculatedBySolver()
        {
            var variables = _solverCalculatedVariables.ToList();

            foreach (var variable in variables)
            {
                ClearCalculatedVariable(variable, VariableDefinedBy.Solver);
            }

            _solverCalculatedVariables.Clear();

            foreach (var variable in GetAllSolverStateVariables().Distinct())
            {
                ClearCalculatedVariable(variable, VariableDefinedBy.Solver);
                ClearCalculatedVariable(variable, VariableDefinedBy.Specification);
                ClearCalculatedVariable(variable, VariableDefinedBy.Equipment);
            }
        }

        private IEnumerable<IVariable> GetAllSolverStateVariables()
        {
            foreach (var stream in Streams)
            {
                foreach (var variable in GetStreamVariables(stream))
                {
                    yield return variable;
                }
            }

            foreach (var equipment in Equipments)
            {
                foreach (var equation in equipment.Equations)
                {
                    foreach (var variable in equation.Variables)
                    {
                        yield return variable;
                    }
                }

                foreach (var specification in equipment.Specifications)
                {
                    foreach (var variable in specification.GetVariables())
                    {
                        yield return variable;
                    }
                }
            }
        }

        private void ClearCalculatedVariable(IVariable variable, VariableDefinedBy procedence)
        {
            if (variable.DataProcedence != procedence)
            {
                return;
            }

            var before = CaptureVariableSnapshot(variable);
            ResetToDeterministicInitialSeed(variable);
            variable.Clear(procedence);
            if (IsDiagnosticVariable(variable))
            {
                TraceDivergenceProbe("Variable cleared", $"{variable.Name}; source={procedence}; before={DescribeVariableSnapshot(before)}; after={DescribeVariable(variable)}");
            }
        }

        private static void ResetToDeterministicInitialSeed(IVariable variable)
        {
            // Prueba de diagnostico: no reutilizar la solucion anterior como semilla.
            variable.SetValueFromSolver(1.0, VariableDefinedBy.Undefined);
        }

        public void OnVariableSolved(IVariable variable)
        {
            if (variable.DataProcedence is VariableDefinedBy.Solver or VariableDefinedBy.Specification)
            {
                _solverCalculatedVariables.Add(variable);
            }

            if (IsCriticalDiagnosticVariable(variable))
            {
                TraceDivergenceProbe("Variable solved", $"equation={_activeSolverEquationName ?? "<external>"}; {variable.Name}; value={DescribeVariable(variable)}");
            }
        }

        private void TraceSolver(string message, string? detail = null)
        {
            TraceSink?.TraceSolver(message, detail);
        }

        private void TraceDivergenceProbe(string message, string? detail = null)
        {
            TraceSink?.TraceSolver($"Watch {message}", detail);
            AddDivergenceProbeRunEvent(message, detail);
        }

        private void TraceDivergenceProbeSummary()
        {
            if (_divergenceProbeRunEvents.Count == 0)
            {
                return;
            }

            TraceSink?.TraceSolver("Watch run summary", JoinLimited(_divergenceProbeRunEvents, 60));
        }

        private void AddDivergenceProbeRunEvent(string message, string? detail)
        {
            if (_divergenceProbeRunEvents.Count >= 80)
            {
                if (_divergenceProbeRunEvents.Count == 80)
                {
                    _divergenceProbeRunEvents.Add("+more watch events omitted");
                }

                return;
            }

            var compactDetail = string.IsNullOrWhiteSpace(detail)
                ? string.Empty
                : $": {CompactTraceDetail(detail)}";
            _divergenceProbeRunEvents.Add($"{message}{compactDetail}");
        }

        private static string CompactTraceDetail(string detail)
        {
            const int maxLength = 700;
            return detail.Length <= maxLength
                ? detail
                : $"{detail[..maxLength]}...";
        }

        private static string DescribeEquation(ISolverEquation equation)
        {
            var variables = string.Join(", ", equation.Variables.Select(variable => $"{variable.Name}={DescribeVariable(variable)}"));
            var adjustable = string.Join(", ", equation.AdjustableVariables().Select(variable => variable.Name));
            return $"{equation.Name}; type={equation.EquationType}; modifier={equation.EquationTypeModifer}; adjustable=[{adjustable}]; variables=[{variables}]";
        }

        private static string DescribeEquationCompact(ISolverEquation equation)
        {
            var adjustable = equation.AdjustableVariables().Select(variable => variable.Name);
            var definedDiagnostic = equation.Variables
                .Where(IsDiagnosticVariable)
                .Where(variable => variable.IsDefined)
                .Select(variable => $"{variable.Name}={DescribeVariable(variable)}");

            return $"{equation.Name}; type={equation.EquationType}; modifier={equation.EquationTypeModifer}; adjustable=[{JoinLimited(adjustable, 8)}]; watch=[{JoinLimited(definedDiagnostic, 8)}]";
        }

        private EquationSolveSnapshot CaptureEquationSnapshot(ISolverEquation equation)
        {
            var residuals = equation.Residuals
                .Where(double.IsFinite)
                .ToArray();
            var residualNorm = residuals.Length == 0
                ? double.NaN
                : Math.Sqrt(residuals.Sum(residual => residual * residual));

            var variables = equation.Variables
                .Distinct()
                .ToDictionary(variable => variable, CaptureVariableSnapshot);

            var streams = GetEquationStreams(equation)
                .ToDictionary(stream => stream.Name, CaptureStreamSnapshot, StringComparer.OrdinalIgnoreCase);

            return new EquationSolveSnapshot(residualNorm, variables, streams);
        }

        private string DescribeNewtonStart(ISolverEquation equation, EquationSolveSnapshot snapshot)
        {
            var adjustable = equation.AdjustableVariables()
                .Select(variable => $"{variable.Name}={DescribeVariableSnapshot(snapshot.Variables[variable])}");
            var defined = equation.Variables
                .Distinct()
                .Where(variable => variable.IsDefined)
                .Select(variable => $"{variable.Name}={DescribeVariableSnapshot(snapshot.Variables[variable])}");
            var streams = snapshot.Streams.Values.Select(DescribeStreamSnapshot);

            return $"{equation.Name}; type={equation.EquationType}; modifier={equation.EquationTypeModifer}; residual={FormatDouble(snapshot.ResidualNorm)}; adjustable=[{JoinLimited(adjustable, 8)}]; defined=[{JoinLimited(defined, 8)}]; streams=[{JoinLimited(streams, 6)}]";
        }

        private string DescribeNewtonStartCompact(ISolverEquation equation, EquationSolveSnapshot snapshot)
        {
            var adjustable = equation.AdjustableVariables()
                .Select(variable => $"{variable.Name}={DescribeVariableSnapshot(snapshot.Variables[variable])}");
            var watchStreams = snapshot.Streams.Values
                .Where(stream => IsDiagnosticStreamName(stream.Name))
                .Select(DescribeStreamSnapshot);

            return $"{equation.Name}; type={equation.EquationType}; modifier={equation.EquationTypeModifer}; residual={FormatDouble(snapshot.ResidualNorm)}; adjustable=[{JoinLimited(adjustable, 8)}]; watch=[{JoinLimited(watchStreams, 8)}]";
        }

        private string DescribeNewtonResult(
            ISolverEquation equation,
            EquationSolveSnapshot before,
            EquationSolveSnapshot after,
            SolverResult result)
        {
            var changes = equation.Variables
                .Distinct()
                .Select(variable => DescribeVariableChange(variable.Name, before.Variables[variable], after.Variables[variable]))
                .Where(change => !string.IsNullOrWhiteSpace(change));
            var streams = after.Streams.Values.Select(DescribeStreamSnapshot);

            return $"{equation.Name}; converged={result.Converged}; iterations={result.Iterations}; error={result.FinalError:G6}; residual {FormatDouble(before.ResidualNorm)}->{FormatDouble(after.ResidualNorm)}; changed=[{JoinLimited(changes, 10)}]; streams=[{JoinLimited(streams, 6)}]";
        }

        private string DescribeNewtonResultCompact(
            ISolverEquation equation,
            EquationSolveSnapshot before,
            EquationSolveSnapshot after,
            SolverResult result)
        {
            var diagnosticChanges = equation.Variables
                .Distinct()
                .Select(variable => DescribeVariableChange(variable.Name, before.Variables[variable], after.Variables[variable]))
                .Where(change => !string.IsNullOrWhiteSpace(change));

            var watchStreams = after.Streams.Values
                .Where(stream => IsDiagnosticStreamName(stream.Name))
                .Select(DescribeStreamSnapshot);

            return $"{equation.Name}; converged={result.Converged}; iterations={result.Iterations}; error={result.FinalError:G6}; residual {FormatDouble(before.ResidualNorm)}->{FormatDouble(after.ResidualNorm)}; changed=[{JoinLimited(diagnosticChanges, 8)}]; watch=[{JoinLimited(watchStreams, 8)}]";
        }

        private string DescribeWatchedEquation(ISolverEquation equation, EquationSolveSnapshot snapshot)
        {
            var adjustable = equation.AdjustableVariables()
                .Select(variable => $"{variable.Name}={DescribeVariableSnapshot(snapshot.Variables[variable])}");
            var equationVariables = equation.Variables
                .Distinct()
                .Where(IsDiagnosticVariable)
                .Select(variable => $"{variable.Name}={DescribeVariableSnapshot(snapshot.Variables[variable])}");
            var watchStreams = GetWatchedStreamSnapshots().Select(DescribeStreamSnapshot);

            return $"{equation.Name}; type={equation.EquationType}; modifier={equation.EquationTypeModifer}; residual={FormatDouble(snapshot.ResidualNorm)}; adjustable=[{JoinLimited(adjustable, 12)}]; equationVars=[{JoinLimited(equationVariables, 12)}]; streams=[{JoinLimited(watchStreams, 18)}]";
        }

        private string DescribeWatchedEquationResult(
            ISolverEquation equation,
            EquationSolveSnapshot before,
            EquationSolveSnapshot after,
            SolverResult result)
        {
            var changes = equation.Variables
                .Distinct()
                .Where(IsDiagnosticVariable)
                .Select(variable => DescribeVariableChange(variable.Name, before.Variables[variable], after.Variables[variable]))
                .Where(change => !string.IsNullOrWhiteSpace(change));
            var watchStreams = GetWatchedStreamSnapshots().Select(DescribeStreamSnapshot);

            return $"{equation.Name}; converged={result.Converged}; iterations={result.Iterations}; error={result.FinalError:G6}; residual {FormatDouble(before.ResidualNorm)}->{FormatDouble(after.ResidualNorm)}; changed=[{JoinLimited(changes, 12)}]; streams=[{JoinLimited(watchStreams, 18)}]";
        }

        private void TraceWatchedStreams(string stage)
        {
            TraceDivergenceProbe(stage, string.Join(" | ", GetWatchedStreamSnapshots().Select(DescribeStreamSnapshot)));
        }

        private void TraceCriticalBalances(string stage)
        {
            var balances = new[]
            {
                DescribeMassBalance("SP-121", ("S-120", 1.0), ("S-122", -1.0), ("S-123", -1.0)),
                DescribeMassBalance("SP-145", ("S-144", 1.0), ("S-146", -1.0), ("S-147", -1.0)),
                DescribeFormulaBalance("Formula S-143=5*S-146", "S-143", 1.0, "S-146", -5.0),
                DescribeMassBalance("C-127 external", ("S-131", 1.0), ("S-122", 1.0), ("S-128", -1.0), ("S-130", -1.0))
            };

            TraceDivergenceProbe($"{stage} balances", string.Join(" | ", balances));
        }

        private string DescribeMassBalance(string label, params (string StreamName, double Sign)[] terms)
        {
            var hasAllValues = true;
            var residual = 0.0;
            var values = new List<string>();

            foreach (var term in terms)
            {
                if (TryGetMassFlow(term.StreamName, out var massFlow))
                {
                    residual += term.Sign * massFlow;
                    values.Add($"{term.StreamName}={massFlow:G6}");
                    continue;
                }

                hasAllValues = false;
                values.Add($"{term.StreamName}=n/a");
            }

            var residualText = hasAllValues ? residual.ToString("G6") : "n/a";
            return $"{label}: {string.Join(", ", values)}; residual={residualText}";
        }

        private string DescribeFormulaBalance(
            string label,
            string leftStreamName,
            double leftFactor,
            string rightStreamName,
            double rightFactor)
        {
            var hasLeft = TryGetMassFlow(leftStreamName, out var leftMassFlow);
            var hasRight = TryGetMassFlow(rightStreamName, out var rightMassFlow);
            var residualText = hasLeft && hasRight
                ? (leftFactor * leftMassFlow + rightFactor * rightMassFlow).ToString("G6")
                : "n/a";

            return $"{label}: {leftStreamName}={(hasLeft ? leftMassFlow.ToString("G6") : "n/a")}, {rightStreamName}={(hasRight ? rightMassFlow.ToString("G6") : "n/a")}; residual={residualText}";
        }

        private bool TryGetMassFlow(string streamName, out double massFlow)
        {
            var stream = Streams.FirstOrDefault(candidate => candidate.Name.Equals(streamName, StringComparison.OrdinalIgnoreCase));
            if (stream?.MassFlow.IsDefined == true)
            {
                massFlow = stream.MassFlow.GetSolverValue();
                return double.IsFinite(massFlow);
            }

            massFlow = double.NaN;
            return false;
        }

        private IEnumerable<StreamSolveSnapshot> GetWatchedStreamSnapshots()
        {
            return DiagnosticStreamNames
                .Select(name => Streams.FirstOrDefault(stream => stream.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .Where(stream => stream != null)
                .Select(stream => CaptureStreamSnapshot(stream!));
        }

        private string DescribeWatchedSolvePlan(SolvePlan solvePlan)
        {
            var equations = solvePlan.EquationsByType
                .SelectMany(pair => pair.Value.Select(equation => (Type: pair.Key, Equation: equation)))
                .Where(pair => IsWatchedEquation(pair.Equation))
                .Select(pair => $"{pair.Type}:{pair.Equation.Name}; adjustable=[{JoinLimited(pair.Equation.AdjustableVariables().Select(variable => variable.Name), 10)}]; canEvaluate={pair.Equation.CanEvaluate}");

            return JoinLimited(equations, 30);
        }

        private IEnumerable<IFacadeStream> GetEquationStreams(ISolverEquation equation)
        {
            var variables = equation.Variables.Distinct().ToList();
            return Streams.Where(stream => variables.Any(variable => BelongsToStream(variable, stream)));
        }

        private static bool BelongsToStream(IVariable variable, IFacadeStream stream)
        {
            return variable.Name.StartsWith($"{stream.Name} ", StringComparison.OrdinalIgnoreCase) ||
                   variable.Name.StartsWith($"{stream.Name}.", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDiagnosticVariable(IVariable variable)
        {
            return DiagnosticStreamNames.Any(streamName =>
                variable.Name.StartsWith($"{streamName} ", StringComparison.OrdinalIgnoreCase) ||
                variable.Name.StartsWith($"{streamName}.", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsCriticalDiagnosticVariable(IVariable variable)
        {
            return CriticalDiagnosticStreamNames.Any(streamName =>
                variable.Name.StartsWith($"{streamName} ", StringComparison.OrdinalIgnoreCase) ||
                variable.Name.StartsWith($"{streamName}.", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsDiagnosticStreamName(string streamName) =>
            DiagnosticStreamNames.Contains(streamName, StringComparer.OrdinalIgnoreCase);

        private static readonly string[] DiagnosticStreamNames =
        [
            "S-112",
            "S-117",
            "S-118",
            "S-120",
            "S-122",
            "S-123",
            "S-125",
            "S-126",
            "S-128",
            "S-129",
            "S-130",
            "S-131",
            "S-132",
            "S-138",
            "S-139",
            "S-143",
            "S-144",
            "S-145",
            "S-146",
            "S-147",
            "S-148",
            "S-149",
            "S-150",
            "S-151",
            "S-155"
        ];

        private static readonly string[] WatchedEquationNames =
        [
            "SP-121",
            "SP-137",
            "SP-145",
            "E-124",
            "E-142",
            "P-119",
            "P-148",
            "Formula: S-143",
            "Formula: S-128",
            "Formula: S-138"
        ];

        private static readonly string[] CriticalDiagnosticStreamNames =
        [
            "S-120",
            "S-122",
            "S-123",
            "S-126",
            "S-128",
            "S-130",
            "S-131",
            "S-132",
            "S-143",
            "S-144",
            "S-146",
            "S-147"
        ];

        private static VariableSolveSnapshot CaptureVariableSnapshot(IVariable variable)
        {
            return variable.IsDefined
                ? new VariableSolveSnapshot(variable.GetSolverValue(), variable.ToUiString("F2"), variable.DataProcedence)
                : new VariableSolveSnapshot(double.NaN, "<Not defined>", variable.DataProcedence);
        }

        private static StreamSolveSnapshot CaptureStreamSnapshot(IFacadeStream stream)
        {
            return new StreamSolveSnapshot(
                stream.Name,
                stream.State,
                CaptureVariableSnapshot(stream.Temperature),
                CaptureVariableSnapshot(stream.Pressure),
                CaptureVariableSnapshot(stream.VaporFraction),
                CaptureVariableSnapshot(stream.MassFlow),
                CaptureVariableSnapshot(stream.MolarFlow),
                CaptureVariableSnapshot(stream.VolumetricFlow),
                CaptureVariableSnapshot(stream.EnthalpyFlow),
                CaptureVariableSnapshot(stream.MassEnthalpy));
        }

        private static string DescribeStreamSnapshot(StreamSolveSnapshot snapshot)
        {
            return $"{snapshot.Name}:{snapshot.State}; T={DescribeVariableSnapshot(snapshot.Temperature)}; P={DescribeVariableSnapshot(snapshot.Pressure)}; VF={DescribeVariableSnapshot(snapshot.VaporFraction)}; MF={DescribeVariableSnapshot(snapshot.MassFlow)}; MolF={DescribeVariableSnapshot(snapshot.MolarFlow)}; VFw={DescribeVariableSnapshot(snapshot.VolumetricFlow)}; Q={DescribeVariableSnapshot(snapshot.EnthalpyFlow)}; Hm={DescribeVariableSnapshot(snapshot.MassEnthalpy)}";
        }

        private static string DescribeVariableChange(
            string variableName,
            VariableSolveSnapshot before,
            VariableSolveSnapshot after)
        {
            if (before.Source == after.Source && !HasSignificantChange(before.SolverValue, after.SolverValue))
            {
                return string.Empty;
            }

            return $"{variableName}: {DescribeVariableSnapshot(before)} -> {DescribeVariableSnapshot(after)}";
        }

        private static string DescribeVariableSnapshot(VariableSolveSnapshot snapshot)
        {
            return $"{snapshot.Text} [{snapshot.Source}]";
        }

        private static bool HasSignificantChange(double before, double after)
        {
            if (double.IsNaN(before) || double.IsNaN(after))
            {
                return double.IsNaN(before) != double.IsNaN(after);
            }

            var absoluteDelta = Math.Abs(before - after);
            if (absoluteDelta > 1e-7)
            {
                return true;
            }

            var scale = Math.Max(1.0, Math.Max(Math.Abs(before), Math.Abs(after)));
            return absoluteDelta / scale > 1e-6;
        }

        private static string FormatDouble(double value)
        {
            return double.IsFinite(value) ? value.ToString("G6") : "n/a";
        }

        private static string JoinLimited(IEnumerable<string> values, int maxItems)
        {
            var list = values.Where(value => !string.IsNullOrWhiteSpace(value)).Take(maxItems + 1).ToList();
            if (list.Count == 0)
            {
                return string.Empty;
            }

            if (list.Count <= maxItems)
            {
                return string.Join("; ", list);
            }

            return $"{string.Join("; ", list.Take(maxItems))}; +more";
        }

        private static string DescribeVariable(IVariable variable)
        {
            return variable.IsDefined
                ? $"{variable.ToUiString("F2")} [{variable.DataProcedence}]"
                : "<Not defined> [Undefined]";
        }

        private static string DescribePressure(Pressure pressure)
        {
            return $"{pressure.GetValue(PressureUnits.Pascala):G8} Pa; {pressure.GetValue(PressureUnits.Bara):G8} bara";
        }

        private sealed record EquationSolveSnapshot(
            double ResidualNorm,
            IReadOnlyDictionary<IVariable, VariableSolveSnapshot> Variables,
            IReadOnlyDictionary<string, StreamSolveSnapshot> Streams);

        private sealed record VariableSolveSnapshot(
            double SolverValue,
            string Text,
            VariableDefinedBy Source);

        private sealed record StreamSolveSnapshot(
            string Name,
            StreamStateType State,
            VariableSolveSnapshot Temperature,
            VariableSolveSnapshot Pressure,
            VariableSolveSnapshot VaporFraction,
            VariableSolveSnapshot MassFlow,
            VariableSolveSnapshot MolarFlow,
            VariableSolveSnapshot VolumetricFlow,
            VariableSolveSnapshot EnthalpyFlow,
            VariableSolveSnapshot MassEnthalpy);

        public void ClearOrphanStream(IFacadeStream stream)
        {
            foreach (var variable in GetStreamVariables(stream))
            {
                variable.Clear(VariableDefinedBy.Solver);
            }
        }

        private static IEnumerable<IVariable> GetStreamVariables(IFacadeStream stream)
        {
            yield return stream.Temperature;
            yield return stream.Pressure;
            yield return stream.MassFlow;
            yield return stream.MolarFlow;
            yield return stream.VolumetricFlow;
            yield return stream.VaporFraction;
            yield return stream.EnthalpyFlow;
            yield return stream.ThermalConductivity;
            yield return stream.Viscosity;
            yield return stream.MassCp;
            yield return stream.MolarCp;
            yield return stream.MassEnthalpy;
            yield return stream.MolarEnthalpy;
            yield return stream.MassDensity;
            yield return stream.MolarDensity;
            yield return stream.MolecularWeight;
            yield return stream.SuperficialTension;

            if (stream.Composition == null)
            {
                yield break;
            }

            foreach (var component in stream.Composition.Components)
            {
                yield return component.MassFlow;
                yield return component.MolarFlow;
                yield return component.MassFraction;
                yield return component.MolarFraction;
            }
        }

        private async Task<bool> ExecutePostSolveCalculationsAsync(List<string> diagnostics)
        {
            try
            {
                var facades = new List<IFacade>();
                facades.AddRange(Equipments);
                facades.AddRange(Streams);

                await Task.WhenAll(facades.Select(ExecuteFacadePostSolveAsync));
                return true;
            }
            catch (Exception ex)
            {
                var diagnostic = $"[MainSolver] Error en post-calculos: {ex.Message}";
                diagnostics.Add(diagnostic);
                return false;
            }
            finally
            {
                OnSimulationCompleted?.Invoke();
            }
        }

        private async Task ExecuteFacadePostSolveAsync(IFacade facade)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var name = facade switch
            {
                IEquipmentFacade equipment => equipment.Name,
                IFacadeStream stream => stream.Name,
                _ => facade.GetType().Name
            };
            TraceSolver("PostSolve started", $"{name}; type={facade.GetType().Name}");

            try
            {
                await facade.PostSolveAsync();

                stopwatch.Stop();
                TraceSolver("PostSolve finished", $"{name}; type={facade.GetType().Name}; elapsedMs={stopwatch.ElapsedMilliseconds}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                TraceSolver("PostSolve failed", $"{name}; type={facade.GetType().Name}; elapsedMs={stopwatch.ElapsedMilliseconds}; error={ex.Message}");
                throw;
            }
        }

        private void UpdateAtmosphericPressure()
        {
            if (_altitude == null)
            {
                return;
            }

            var altitudeMeters = Altitude.GetValue(LengthUnits.Meter);
            const double seaLevelPressure = 101325.0;
            const double factor = 2.25577e-5;
            const double exponent = 5.25588;

            var pressure = seaLevelPressure * Math.Pow(1 - factor * altitudeMeters, exponent);
            AtmosphericPressure.SetValue(pressure, PressureUnits.Pascala);
            UnitManager.SetAtmosphericPressureReference(AtmosphericPressure);
            TraceDivergenceProbe("Atmospheric pressure updated", $"altitude={altitudeMeters:G6} m; atmosphericPressure={DescribePressure(AtmosphericPressure)}");
        }

        private sealed class SolvePlan
        {
            public SolvePlan(Dictionary<SolverEquationType, List<ISolverEquation>> equationsByType)
            {
                EquationsByType = equationsByType;
            }

            public Dictionary<SolverEquationType, List<ISolverEquation>> EquationsByType { get; }

            public IEnumerable<SolverEquationType> EquationTypesWithPendingWork()
            {
                return EquationsByType
                    .Where(pair => pair.Value.Count > 0)
                    .Select(pair => pair.Key);
            }
        }
    }
}
