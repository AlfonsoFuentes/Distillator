using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using UnitSystem;

namespace Shared.SolverConsecutive
{
    public sealed class MainSolver : IMainSolver
    {
        private static readonly SolverEquationType[] DefaultEquationTypes =
        [
            SolverEquationType.Pressure,
            SolverEquationType.Concentration,
            SolverEquationType.VaporFraction,
            SolverEquationType.Enthalpy,
            SolverEquationType.MassBalance,
            SolverEquationType.MassEnergyBalance,
            SolverEquationType.Specification
        ];

        private readonly INewtonSolver _solver;

        public MainSolver()
            : this(new NewtonSolver())
        {
        }

        public MainSolver(INewtonSolver solver)
        {
            _solver = solver;
            AtmosphericPressure = new Pressure(101325, PressureUnits.Pascala);
            Altitude = new Length(0, LengthUnits.Meter);
        }

        public event Action? OnSimulationCompleted;

        public List<IFacadeStream> Streams { get; } = new();

        public List<ISolverEquipment> Equipments { get; } = new();

        public ThermodynamicMethodFullDto ThermoMethod { get; set; } = null!;

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
            Streams.Add(stream);
        }

        public void RemoveStream(IFacadeStream stream)
        {
            Streams.Remove(stream);
        }

        public void AddEquipment(ISolverEquipment equipment)
        {
            Equipments.Add(equipment);
        }

        public void RemoveEquipment(ISolverEquipment equipment)
        {
            Equipments.Remove(equipment);
        }

        public void RunSimulation()
        {
            _ = Task.Run(RunSimulationAsync);
        }

        private async Task RunSimulationAsync()
        {
            try
            {
                ClearCalculatedBySolver();
                var solvePlan = BuildFullSolvePlan();
                ExecuteSolvePlan(solvePlan);
                await ExecutePostSolveCalculationsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainSolver] Error en simulacion: {ex.Message}");
                OnSimulationCompleted?.Invoke();
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

        private void ExecuteSolvePlan(SolvePlan solvePlan)
        {
            var pendingTypes = solvePlan.EquationTypesWithPendingWork().ToList();
            var hasGlobalProgress = true;
            var iteration = 0;
            const int maxIterations = 10;

            while (hasGlobalProgress && iteration < maxIterations && pendingTypes.Count > 0)
            {
                hasGlobalProgress = false;

                foreach (var equationType in pendingTypes.ToList())
                {
                    var equations = solvePlan.EquationsByType[equationType];
                    var hasLocalProgress = ResolveEquationType(equations);
                    hasGlobalProgress |= hasLocalProgress;

                    if (equations.Count == 0)
                    {
                        pendingTypes.Remove(equationType);
                        Console.WriteLine($"[MainSolver] Tipo '{equationType}' resuelto. Pendientes: {pendingTypes.Count}");
                    }
                }

                if (!hasGlobalProgress)
                {
                    break;
                }

                iteration++;
            }

            LogSolvePlanResult(pendingTypes, iteration);
        }

        private bool ResolveEquationType(List<ISolverEquation> equations)
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
                    if (!equation.CanEvaluate)
                    {
                        index++;
                        continue;
                    }

                    equation.RefreshEquation();
                    var result = _solver.Solve(equation);

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

        private static void LogSolvePlanResult(IReadOnlyCollection<SolverEquationType> pendingTypes, int iteration)
        {
            if (pendingTypes.Count == 0)
            {
                Console.WriteLine($"[MainSolver] Todas las ecuaciones resueltas en {iteration} iteraciones globales");
                return;
            }

            Console.WriteLine($"[MainSolver] Convergencia incompleta. Tipos sin resolver: {string.Join(", ", pendingTypes)}");
        }

        private void ClearCalculatedBySolver()
        {
            var variables = Equipments
                .SelectMany(equipment => equipment.Equations)
                .SelectMany(equation => equation.Variables)
                .Where(variable => variable.DataProcedence == VariableDefinedBy.Solver)
                .Distinct()
                .ToList();

            foreach (var variable in variables)
            {
                variable.Clear(VariableDefinedBy.Solver);
            }
        }

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

        private async Task ExecutePostSolveCalculationsAsync()
        {
            try
            {
                var facades = new List<IFacade>();
                facades.AddRange(Equipments);
                facades.AddRange(Streams);

                await Task.WhenAll(facades.Select(facade => facade.PostSolveAsync()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainSolver] Error en post-calculos: {ex.Message}");
            }
            finally
            {
                OnSimulationCompleted?.Invoke();
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
