using System.Reflection;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Distillator.Core.Tests.Solver;

public sealed class MainSolverSpecificationPlanTests
{
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

        public TestEquation(string name, IVariable variable)
        {
            Name = name;
            _variable = variable;
        }

        public string Name { get; }
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => [0.0];
        public List<IVariable> Variables => [_variable];
        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Regular;
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
    }
}
