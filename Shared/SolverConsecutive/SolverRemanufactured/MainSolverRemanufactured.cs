using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using UnitSystem;

namespace Shared.SolverConsecutive.SolverRemanufactured;

public sealed class MainSolverRemanufactured : IMainSolver
{
    private const int MaxPasses = 10;

    private static readonly SolverEquationType[] SimpleEquationOrder =
    [
        SolverEquationType.Pressure,
        SolverEquationType.Concentration,
        SolverEquationType.VaporFraction,
        SolverEquationType.Enthalpy
    ];

    private static readonly SolverEquationType[] BalanceEquationOrder =
    [
        SolverEquationType.MassBalance,
        SolverEquationType.MassEnergyBalance
    ];
    private static readonly SolverEquationType[] SpecsEquationOrder =
   [
       SolverEquationType.Specification
   ];

    private readonly INewtonSolver _newtonSolver;
    private Length _altitude;
    private ISolverTraceSink? _traceSink;

    public MainSolverRemanufactured()
        : this(new NewtonSolverRemanufactured())
    {
    }

    public MainSolverRemanufactured(INewtonSolver newtonSolver)
    {
        _newtonSolver = newtonSolver;
        _altitude = new Length(0, LengthUnits.Meter);
        AtmosphericPressure = new Pressure(101325, PressureUnits.Pascala);
        UpdateAtmosphericPressure();
    }

    public List<IFacadeStream> Streams { get; } = new();

    public List<ISolverEquipment> Equipments { get; } = new();

    public event Action? OnSimulationCompleted;

    public ThermodynamicMethodFullDto ThermoMethod { get; set; } = null!;

    public Pressure AtmosphericPressure { get; set; }

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

            foreach (var equipment in Equipments)
            {
                SetEquipmentTraceSink(equipment, value);
            }
        }
    }

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
        SetEquipmentTraceSink(equipment, TraceSink);
        Equipments.Add(equipment);
    }

    public void RemoveEquipment(ISolverEquipment equipment)
    {
        Equipments.Remove(equipment);
    }

    public void ClearOrphanStream(IFacadeStream stream)
    {
        foreach (var variable in GetStreamVariables(stream))
        {
            variable.Clear(VariableDefinedBy.Solver);
            variable.Clear(VariableDefinedBy.Specification);
            variable.Clear(VariableDefinedBy.Equipment);
        }
    }

    public async Task<SimulationRunResult> RunSimulationAsync()
    {
        var runId = Guid.NewGuid();
        var diagnostics = new List<string>();
        int currentpass = 0;
        int pendingWorkItems = 0;
        var postSolveSucceeded = false;

        try
        {
            TraceSink?.TraceSolver(string.Empty);
            ClearCalculatedVariables();
            var allEquations = BuildEquations();
            var simpleEquations = BuildEquationItems(allEquations, SimpleEquationOrder);
            var balanceEquations = BuildEquationItems(allEquations, BalanceEquationOrder);
            var specsEquations = BuildEquationItems(allEquations, SpecsEquationOrder);
            var clusterEquations = BuildClusterEquationItems(allEquations);


            int priorpass = 0;
            bool stop = false;
            do
            {
                priorpass = currentpass;
                currentpass = 0;

                var resultCurrentSolver = SolveEquationItems(simpleEquations);
                currentpass = resultCurrentSolver.passCount;
                pendingWorkItems = resultCurrentSolver.nEquations;

                resultCurrentSolver = SolveEquationItems(specsEquations);
                currentpass += resultCurrentSolver.passCount;
                pendingWorkItems+=resultCurrentSolver.nEquations;

                resultCurrentSolver = SolveEquationItems(balanceEquations);
                currentpass += resultCurrentSolver.passCount;
                pendingWorkItems += resultCurrentSolver.nEquations;

                resultCurrentSolver = SolveEquationItems(clusterEquations);
                currentpass += resultCurrentSolver.passCount;
                pendingWorkItems += resultCurrentSolver.nEquations;

                stop = priorpass == currentpass || pendingWorkItems == 0;

            }
            while (!stop);

            postSolveSucceeded = await ExecutePostSolveCalculationsAsync(diagnostics);
        }
        finally
        {
            OnSimulationCompleted?.Invoke();
        }

        diagnostics.Add($"Passes: {currentpass}");
        diagnostics.Add($"Pending work items: {pendingWorkItems}");

        return postSolveSucceeded
            ? SimulationRunResult.Completed(runId, pendingWorkItems == 0, diagnostics)
            : SimulationRunResult.Failed(runId, diagnostics);
    }

    private void ClearCalculatedVariables()
    {
        foreach (var variable in GetAllSolverStateVariables().Distinct())
        {
            variable.Clear(VariableDefinedBy.Solver);
            variable.Clear(VariableDefinedBy.Specification);
            variable.Clear(VariableDefinedBy.Equipment);
        }
    }

    private List<ISolverEquation> BuildEquations()
    {
        var equations = Equipments
            .SelectMany(equipment => equipment.Equations)
            .ToList();

        equations.AddRange(Equipments
            .SelectMany(equipment => equipment.Specifications)
            .Select(specification => new SpecificationEquation(specification)));

        return equations;
    }

    private static List<SolverWorkItem> BuildEquationItems(
        IReadOnlyCollection<ISolverEquation> equations,
        IReadOnlyCollection<SolverEquationType> equationTypes)
    {
        if (equationTypes.Contains(SolverEquationType.Specification))
        {
            return equations
                .Where(equation => equation.EquationTypeModifer == SolverEquationTypeModifier.Spec)
                .Select(equation => new SolverWorkItem(equation, [equation]))
                .ToList();
        }

        var items = new List<SolverWorkItem>();
        foreach (var equationType in equationTypes)
        {
            items.AddRange(equations
                .Where(equation => equation.EquationTypeModifer == SolverEquationTypeModifier.Regular)
                .Where(equation => equation.EquationType == equationType)
                .Select(equation => new SolverWorkItem(equation, [equation])));
        }

        return items;
    }

  

    private List<SolverWorkItem> BuildClusterEquationItems(IReadOnlyCollection<ISolverEquation> equations)
    {
        var items = new List<SolverWorkItem>();
        var specificationEquations = equations
            .OfType<SpecificationEquation>()
            .ToDictionary(equation => equation.Specification.Id);

        foreach (var equipment in Equipments.Where(equipment => equipment.Specifications.Any()))
        {
            foreach (var specification in equipment.Specifications)
            {
                if (!specificationEquations.TryGetValue(specification.Id, out var specificationEquation))
                {
                    continue;
                }

                var clusterStreams = GetSpecificationClusterStreams(equipment, specification);
                var clusterEquipments = GetEquipmentsConnectedTo(clusterStreams, equipment);

                AddSpecificationClusterItem(
                    items,
                    specification,
                    specificationEquation,
                    clusterStreams,
                    [equipment],
                    useTargetEquationFilter: true);

                if (clusterEquipments.Count > 1)
                {
                    AddSpecificationClusterItem(
                        items,
                        specification,
                        specificationEquation,
                        clusterStreams,
                        clusterEquipments,
                        useTargetEquationFilter: false);
                }
            }
        }

        return items;
    }

    private static void AddSpecificationClusterItem(
        List<SolverWorkItem> items,
        ISpecification specification,
        SpecificationEquation specificationEquation,
        HashSet<IFacadeStream> clusterStreams,
        IEnumerable<ISolverEquipment> clusterEquipments,
        bool useTargetEquationFilter)
    {
        var clusterVariables = GetSpecificationClusterVariables(clusterStreams, specification);
        var equations = clusterEquipments
            .SelectMany(equipment => equipment.Equations)
            .Where(equation => BelongsToEquationGroup(equation, BalanceEquationOrder, includeSpecifications: false))
            .Where(equation => !useTargetEquationFilter || IsRelevantForSpecificationCluster(equation, specification))
            .Where(equation => equation.Variables.Any(clusterVariables.Contains))
            .Distinct()
            .ToList();

        if (equations.Count == 0)
        {
            return;
        }

        equations.Add(specificationEquation);

        items.Add(new SolverWorkItem(
            new EquationClusterInDevelopment(
                SolverEquationType.Specification,
                SolverEquationTypeModifier.Spec,
                equations),
            equations));
    }

    private (int passCount, int nEquations) SolveEquationItems(List<SolverWorkItem> workItems)
    {

        int priorpassCount = 0;
        int currentpassCount = 0;
        bool stop = false;
        do
        {
            priorpassCount = currentpassCount;
            currentpassCount = 0;
            for (var index = 0; index < workItems.Count;)
            {
                var item = workItems[index];
                var result = _newtonSolver.Solve(item.Equation);
                if (!result.Converged)
                {
                    index++;
                    currentpassCount++;
                    continue;
                }

                workItems.RemoveAt(index);

            }
            stop = priorpassCount == currentpassCount || workItems.Count == 0;
        }
        while (!stop);


        return (currentpassCount, workItems.Count);
    }

   

    private static bool BelongsToEquationGroup(
        ISolverEquation equation,
        IReadOnlyCollection<SolverEquationType> equationTypes,
        bool includeSpecifications)
    {
        if (includeSpecifications && equation.EquationTypeModifer == SolverEquationTypeModifier.Spec)
        {
            return true;
        }

        return equation.EquationTypeModifer == SolverEquationTypeModifier.Regular
            && equationTypes.Contains(equation.EquationType);
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

            foreach (var facade in facades)
            {
                await facade.PostSolveAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            diagnostics.Add($"[MainSolverRemanufactured] Error en post-calculos: {ex.Message}");
            return false;
        }
    }

    private static void SetEquipmentTraceSink(ISolverEquipment equipment, ISolverTraceSink? traceSink)
    {
        if (equipment is SolverEquipmentBase solverEquipment)
        {
            solverEquipment.TraceSink = traceSink;
        }
    }

    private void UpdateAtmosphericPressure()
    {
        var altitudeMeters = Altitude.GetValue(LengthUnits.Meter);
        const double seaLevelPressure = 101325.0;
        const double factor = 2.25577e-5;
        const double exponent = 5.25588;

        var pressure = seaLevelPressure * Math.Pow(1 - factor * altitudeMeters, exponent);
        AtmosphericPressure.SetValue(pressure, PressureUnits.Pascala);
        UnitManager.SetAtmosphericPressureReference(AtmosphericPressure);
    }

    private sealed record SolverWorkItem(
        ISolverEquation Equation,
        IReadOnlyList<ISolverEquation> SourceEquations);
}
