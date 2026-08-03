using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Distillator.Core.Tests.Solver;

public sealed class StreamMixerBalanceTests
{
    private const double MassFlowToleranceKgPerHour = 0.01;

    [Fact]
    [Trait("Spec", "TEST")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_WhenOnlyInletMassFlowsAreDefined_ShouldCalculateOutletMassFlow()
    {
        var caseData = CreateCase();

        SetMassFlow(caseData.Inlet1, 1000);
        SetMassFlow(caseData.Inlet2, 2000);

        await caseData.Solver.RunSimulationAsync();

        Assert.Equal(3000, GetMassFlow(caseData.Outlet), MassFlowToleranceKgPerHour);
        Assert.False(caseData.Outlet.Composition.Components[0].MassFlow.IsDefined);
    }

    private static MixerCase CreateCase()
    {
        var solver = new MainSolver();
        var mixer = new SolverStreamMixer("M-101");
        var inlet1 = new FacadeStream("S-101");
        var inlet2 = new FacadeStream("S-102");
        var outlet = new FacadeStream("S-103");
        var method = CreateEthanolWaterMethod();

        inlet1.SetThermodynamicMethod(method);
        inlet2.SetThermodynamicMethod(method);
        outlet.SetThermodynamicMethod(method);

        mixer.AddInlet(inlet1);
        mixer.AddInlet(inlet2);
        mixer.SetOutlet(outlet);

        solver.AddEquipment(mixer);
        solver.AddStream(inlet1);
        solver.AddStream(inlet2);
        solver.AddStream(outlet);

        return new MixerCase(solver, inlet1, inlet2, outlet);
    }

    private static ThermodynamicMethodFullDto CreateEthanolWaterMethod()
    {
        return new ThermodynamicMethodFullDto
        {
            Id = Guid.Parse("6a28cb5e-6c6d-4a67-9070-03d1cc1449a1"),
            Name = "Regression Ethanol Water",
            LiquidModel = LiquidPhaseModel.IdealLiquid,
            VaporModel = VaporPhaseModel.IdealGas,
            Components =
            [
                CreateComponent(
                    Guid.Parse("b148ce7e-a0c2-439a-aa17-e916324bce61"),
                    "Ethanol",
                    "C2H6O",
                    molecularWeight: 46.07,
                    matrixIndex: 0),
                CreateComponent(
                    Guid.Parse("b595e4f2-54db-4058-a9c2-d36b7f88962c"),
                    "Water",
                    "H2O",
                    molecularWeight: 18.015,
                    matrixIndex: 1)
            ]
        };
    }

    private static MethodComponentFullDto CreateComponent(
        Guid id,
        string name,
        string formula,
        double molecularWeight,
        int matrixIndex)
    {
        return new MethodComponentFullDto
        {
            ComponentId = id,
            ComponentName = name,
            MatrixIndex = matrixIndex,
            FullData = new ChemicalComponentDto
            {
                Id = id,
                Name = name,
                Formula = formula,
                MolecularWeight = molecularWeight,
                CriticalTemperature = new Temperature(500, TemperatureUnits.Kelvin),
                CriticalPressure = new Pressure(50, PressureUnits.Bara),
                BoilingPoint = new Temperature(350, TemperatureUnits.Kelvin),
                MeltingPoint = new Temperature(250, TemperatureUnits.Kelvin)
            }
        };
    }

    private static void SetMassFlow(IFacadeStream stream, double kgPerHour)
    {
        stream.MassFlow.SetValue(new MassFlow(kgPerHour, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
    }

    private static double GetMassFlow(IFacadeStream stream)
    {
        return stream.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);
    }

    private sealed record MixerCase(
        MainSolver Solver,
        IFacadeStream Inlet1,
        IFacadeStream Inlet2,
        IFacadeStream Outlet);
}
