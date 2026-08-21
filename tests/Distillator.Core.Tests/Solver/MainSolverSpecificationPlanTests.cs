using System.Reflection;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Distillator.Core.Tests.Solver;

public sealed class MainSolverSpecificationPlanTests
{
    [Fact]
    public void BuildFullSolvePlan_WhenSpecificationExists_ShouldTrySpecificationsBeforeRegularBalances()
    {
        var solver = new MainSolver();
        var stream = new FacadeStream("S1");
        var equipment = new TestEquipment("Seed");
        var specification = new TestSpecification(stream.MassFlow, stream);

        equipment.Outlets.Add(stream);
        equipment.AddEquation(new TestEquation("Seed balance", stream.MassFlow));
        equipment.AddSpec(specification);

        solver.AddStream(stream);
        solver.AddEquipment(equipment);

        var pendingTypes = BuildPendingTypes(solver);

        Assert.Equal(SolverEquationType.Specification, pendingTypes[0]);
    }

    [Fact]
    public void ClearCalculatedBySolver_WhenStreamCalculatedValueExists_ShouldKeepStreamCalculatedValue()
    {
        var solver = new MainSolver();
        var stream = new FacadeStream("S1");
        stream.MassFlow.SetValue(new MassFlow(100, MassFlowUnits.Kg_hr), VariableDefinedBy.StreamCalculated);
        solver.AddStream(stream);

        ClearCalculatedBySolver(solver);

        Assert.Equal(VariableDefinedBy.StreamCalculated, stream.MassFlow.DataProcedence);
        Assert.Equal(100.0, stream.MassFlow.GetSolverValue(), precision: 8);
    }

    [Fact]
    public void ClearCalculatedBySolver_WhenUserInputExists_ShouldKeepUserValueAndProcedence()
    {
        var solver = new MainSolver();
        var stream = new FacadeStream("S1");
        stream.MassFlow.SetValue(new MassFlow(100, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
        solver.AddStream(stream);

        ClearCalculatedBySolver(solver);

        Assert.Equal(VariableDefinedBy.UserInput, stream.MassFlow.DataProcedence);
        Assert.Equal(100.0, stream.MassFlow.GetSolverValue(), precision: 8);
    }

    [Fact]
    public void NewtonSolver_WhenSpecEquationConverges_ShouldMarkAdjustedVariableAsSpecification()
    {
        var newton = new NewtonSolver();
        var stream = new FacadeStream("S1");
        var equation = new TestEquation(
            "Spec equation",
            stream.MassFlow,
            SolverEquationType.MassBalance,
            SolverEquationTypeModifier.Spec,
            () => stream.MassFlow.GetSolverValue() - 90.0);

        var result = newton.Solve(equation);

        Assert.True(result.Converged);
        Assert.Equal(VariableDefinedBy.Specification, stream.MassFlow.DataProcedence);
        Assert.Equal(90.0, stream.MassFlow.GetSolverValue(), precision: 6);
    }

    [Fact]
    public void NewtonSolver_WhenEquationFails_ShouldRestoreAdjustedVariableState()
    {
        var newton = new NewtonSolver();
        var stream = new FacadeStream("S1");
        var equation = new TestEquation(
            "Impossible equation",
            stream.MassFlow,
            residualFactory: () =>
            {
                var value = stream.MassFlow.GetSolverValue();
                return value * value + 1.0;
            });

        var result = newton.Solve(equation);

        Assert.False(result.Converged);
        Assert.Equal(VariableDefinedBy.Undefined, stream.MassFlow.DataProcedence);
        Assert.Equal(0.0, stream.MassFlow.GetSolverValue(), precision: 8);
    }

    [Fact]
    public void BuildFullSolvePlan_WhenSpecificationHasConnectedEquipment_ShouldCreateThreeAttemptsInOrder()
    {
        var solver = new MainSolver();
        var sharedStream = new FacadeStream("S1");
        var seedEquipment = new TestEquipment("Seed");
        var connectedEquipment = new TestEquipment("Connected");
        var specification = new TestSpecification(sharedStream.MassFlow, sharedStream);

        seedEquipment.Outlets.Add(sharedStream);
        connectedEquipment.Inlets.Add(sharedStream);
        sharedStream.EquipmentOutlet = seedEquipment;
        sharedStream.EquipmentInlet = connectedEquipment;

        seedEquipment.AddEquation(new TestEquation("Seed balance", sharedStream.MassFlow));
        connectedEquipment.AddEquation(new TestEquation("Connected balance", sharedStream.MassFlow));
        seedEquipment.AddSpec(specification);

        solver.AddStream(sharedStream);
        solver.AddEquipment(seedEquipment);
        solver.AddEquipment(connectedEquipment);

        var specificationWork = BuildSpecificationWork(solver);

        Assert.Equal(3, specificationWork.Count);
        Assert.IsType<SpecificationEquation>(specificationWork[0]);

        var seedCluster = Assert.IsType<EquationClusterInDevelopment>(specificationWork[1]);
        Assert.Contains(seedCluster.Equations, equation => equation.Name == "Seed balance");
        Assert.DoesNotContain(seedCluster.Equations, equation => equation.Name == "Connected balance");
        Assert.Contains(seedCluster.Equations, equation => equation is SpecificationEquation);

        var connectedCluster = Assert.IsType<EquationClusterInDevelopment>(specificationWork[2]);
        Assert.Contains(connectedCluster.Equations, equation => equation.Name == "Seed balance");
        Assert.Contains(connectedCluster.Equations, equation => equation.Name == "Connected balance");
        Assert.Contains(connectedCluster.Equations, equation => equation is SpecificationEquation);
    }

    private static List<ISolverEquation> BuildSpecificationWork(MainSolver solver)
    {
        var buildPlan = typeof(MainSolver).GetMethod(
            "BuildFullSolvePlan",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(buildPlan);

        var plan = buildPlan.Invoke(solver, null);
        Assert.NotNull(plan);

        var equationsByTypeProperty = plan.GetType().GetProperty("EquationsByType");
        Assert.NotNull(equationsByTypeProperty);

        var equationsByType = Assert.IsType<Dictionary<SolverEquationType, List<ISolverEquation>>>(
            equationsByTypeProperty.GetValue(plan));

        Assert.True(equationsByType.TryGetValue(SolverEquationType.Specification, out var specificationWork));
        return specificationWork;
    }

    private static List<SolverEquationType> BuildPendingTypes(MainSolver solver)
    {
        var buildPlan = typeof(MainSolver).GetMethod(
            "BuildFullSolvePlan",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(buildPlan);

        var plan = buildPlan.Invoke(solver, null);
        Assert.NotNull(plan);

        var pendingTypesMethod = plan.GetType().GetMethod("EquationTypesWithPendingWork");
        Assert.NotNull(pendingTypesMethod);

        var pendingTypes = Assert.IsAssignableFrom<IEnumerable<SolverEquationType>>(
            pendingTypesMethod.Invoke(plan, null));
        return pendingTypes.ToList();
    }

    private static void ClearCalculatedBySolver(MainSolver solver)
    {
        var clear = typeof(MainSolver).GetMethod(
            "ClearCalculatedBySolver",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(clear);
        clear.Invoke(solver, null);
    }

    private sealed class TestEquipment : SolverEquipmentBase
    {
        public TestEquipment(string name)
        {
            Name = name;
        }

        public override List<ISolverEquation> Equations { get; } = [];

        public void AddEquation(ISolverEquation equation)
        {
            Equations.Add(equation);
        }
    }

    private sealed class TestEquation : ISolverEquation
    {
        private readonly IVariable _variable;

        private readonly Func<double>? _residualFactory;

        public TestEquation(
            string name,
            IVariable variable,
            SolverEquationType equationType = SolverEquationType.MassBalance,
            SolverEquationTypeModifier equationTypeModifier = SolverEquationTypeModifier.Regular,
            Func<double>? residualFactory = null)
        {
            Name = name;
            _variable = variable;
            EquationType = equationType;
            EquationTypeModifer = equationTypeModifier;
            _residualFactory = residualFactory;
        }

        public string Name { get; }
        public SolverEquationType EquationType { get; }
        public List<double> Residuals => [_residualFactory?.Invoke() ?? 0.0];
        public List<IVariable> Variables => [_variable];
        public SolverEquationTypeModifier EquationTypeModifer { get; }
    }

    private sealed class TestSpecification : ISpecification
    {
        private readonly IVariable _variable;

        public TestSpecification(IVariable variable, IFacadeStream stream)
        {
            _variable = variable;
            AssociatedStreams = [stream];
        }

        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "Test specification";
        public SpecificationType Type => SpecificationType.Formula;
        public SolverEquationType TargetEquationType => SolverEquationType.MassBalance;
        public bool CanEvaluate => true;
        public IReadOnlyCollection<IFacadeStream> AssociatedStreams { get; }
        public double GetResidual() => 0.0;
        public List<IVariable> GetVariables() => [_variable];
        public List<IVariable> GetTargetVariables() => [_variable];
    }
}
