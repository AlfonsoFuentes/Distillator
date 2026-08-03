using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Distillator.Core.Tests.Solver;

public sealed class VesselBalanceTests
{
    private const double MassFlowToleranceKgPerHour = 0.01;
    private const double CompositionTolerancePercent = 0.01;

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_OneInOneOut_WhenInletMassFlowDefined_ShouldCalculateOutletMassFlow()
    {
        var caseData = CreateOneInOneOutCase();

        SetMassFlow(caseData.Inlet, 1000);

        var result = await caseData.Solver.RunSimulationAsync();

        Assert.Equal(1000, GetMassFlow(caseData.Outlet), MassFlowToleranceKgPerHour);
        Assert.Equal(VariableDefinedBy.Solver, caseData.Outlet.MassFlow.DataProcedence);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_OneInOneOut_WhenOutletMassFlowDefined_ShouldCalculateInletMassFlow()
    {
        var caseData = CreateOneInOneOutCase();

        SetMassFlow(caseData.Outlet, 750);

        var result = await caseData.Solver.RunSimulationAsync();

        Assert.Equal(750, GetMassFlow(caseData.Inlet), MassFlowToleranceKgPerHour);
        Assert.Equal(VariableDefinedBy.Solver, caseData.Inlet.MassFlow.DataProcedence);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_OneInOneOut_WhenInletMassFlowAndCompositionDefined_ShouldCalculateOutletMassFlowAndComposition()
    {
        var caseData = CreateOneInOneOutCase();

        SetMassFlow(caseData.Inlet, 1000);
        SetMassComposition(caseData.Inlet, ethanolPercent: 40, waterPercent: 60);

        _ = await caseData.Solver.RunSimulationAsync();

        Assert.Equal(1000, GetMassFlow(caseData.Outlet), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Outlet, ethanolPercent: 40, waterPercent: 60);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_OneInOneOut_WhenOutletMassFlowAndCompositionDefined_ShouldCalculateInletMassFlowAndComposition()
    {
        var caseData = CreateOneInOneOutCase();

        SetMassFlow(caseData.Outlet, 500);
        SetMassComposition(caseData.Outlet, ethanolPercent: 25, waterPercent: 75);

        _ = await caseData.Solver.RunSimulationAsync();

        Assert.Equal(500, GetMassFlow(caseData.Inlet), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Inlet, ethanolPercent: 25, waterPercent: 75);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_OneInOneOut_WhenOnlyInletCompositionDefined_ShouldCalculateOutletComposition()
    {
        var caseData = CreateOneInOneOutCase();

        SetMassComposition(caseData.Inlet, ethanolPercent: 40, waterPercent: 60);

        var result = await caseData.Solver.RunSimulationAsync();

        AssertMassComposition(caseData.Outlet, ethanolPercent: 40, waterPercent: 60);
        Assert.False(caseData.Inlet.MassFlow.IsDefined);
        Assert.False(caseData.Outlet.MassFlow.IsDefined);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_OneInOneOut_WhenMassFlowAndCompositionAreCrossDefined_ShouldCalculateMissingFlowAndComposition()
    {
        var caseData = CreateOneInOneOutCase();

        SetMassFlow(caseData.Inlet, 1000);
        SetMassComposition(caseData.Outlet, ethanolPercent: 40, waterPercent: 60);

        _ = await caseData.Solver.RunSimulationAsync();

        Assert.Equal(1000, GetMassFlow(caseData.Outlet), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Inlet, ethanolPercent: 40, waterPercent: 60);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Regression")]
    public async Task RunSimulation_OneInOneOut_WhenOutletCompositionChangesFromZeroToNonZero_ShouldRecalculateInletComposition()
    {
        var caseData = CreateOneInOneOutCase();

        SetMassFlow(caseData.Inlet, 1000);

        await AssertOutletToInletCompositionAsync(caseData, ethanolPercent: 33.43, waterPercent: 66.57);
        await AssertOutletToInletCompositionAsync(caseData, ethanolPercent: 0, waterPercent: 100);
        await AssertOutletToInletCompositionAsync(caseData, ethanolPercent: 24.695598949197815, waterPercent: 75.30440105080219);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_TwoInOneOut_WhenFlowsAndBoundaryCompositionsAreDefined_ShouldCalculateMissingInlet()
    {
        var caseData = CreateTwoInOneOutCase();

        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 400));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet, 1000));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet1, ethanolPercent: 40, waterPercent: 60));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet, ethanolPercent: 24, waterPercent: 76));

        Assert.Equal(600, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Inlet2, ethanolPercent: 13.333333333333334, waterPercent: 86.66666666666667);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Regression")]
    public async Task RunSimulation_TwoInOneOut_WhenKnownInletCompositionIsZero_ShouldCalculateMissingInlet()
    {
        var caseData = CreateTwoInOneOutCase();

        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 400));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet, 1000));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet1, ethanolPercent: 0, waterPercent: 100));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet, ethanolPercent: 24, waterPercent: 76));

        Assert.Equal(600, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Inlet2, ethanolPercent: 40, waterPercent: 60);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Regression")]
    public async Task RunSimulation_TwoInOneOut_WhenBoundaryCompositionsMoveFromNonZeroToZero_ShouldCalculateMissingInlet()
    {
        var caseData = CreateTwoInOneOutCase();

        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 400));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet, 1000));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet1, ethanolPercent: 40, waterPercent: 60));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet, ethanolPercent: 24, waterPercent: 76));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet1, ethanolPercent: 0, waterPercent: 100));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet, ethanolPercent: 0, waterPercent: 100));

        Assert.Equal(600, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Inlet2, ethanolPercent: 0, waterPercent: 100);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_TwoInOneOut_WhenInlet1CompositionAndInlet2FlowAreUnknown_ShouldCalculateBoth()
    {
        var caseData = CreateTwoInOneOutCase();

        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 400));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet2, ethanolPercent: 13.333333333333334, waterPercent: 86.66666666666667));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet, 1000));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet, ethanolPercent: 24, waterPercent: 76));

        Assert.Equal(600, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Inlet1, ethanolPercent: 40, waterPercent: 60);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_TwoInOneOut_WhenInlet1FlowAndInlet2CompositionAreUnknown_ShouldCalculateBoth()
    {
        var caseData = CreateTwoInOneOutCase();

        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet1, ethanolPercent: 40, waterPercent: 60));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet2, 600));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet, 1000));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet, ethanolPercent: 24, waterPercent: 76));

        Assert.Equal(400, GetMassFlow(caseData.Inlet1), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Inlet2, ethanolPercent: 13.333333333333334, waterPercent: 86.66666666666667);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_TwoInOneOut_WhenOutletCompositionAndInlet2FlowAreUnknown_ShouldCalculateBoth()
    {
        var caseData = CreateTwoInOneOutCase();

        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 400));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet1, ethanolPercent: 40, waterPercent: 60));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet2, ethanolPercent: 13.333333333333334, waterPercent: 86.66666666666667));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet, 1000));

        Assert.Equal(600, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Outlet, ethanolPercent: 24, waterPercent: 76);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Regression")]
    public async Task RunSimulation_TwoInOneOut_WhenCrossUnknownsIncludeZeroComposition_ShouldCalculateBoth()
    {
        var caseData = CreateTwoInOneOutCase();

        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 400));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet2, ethanolPercent: 40, waterPercent: 60));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet, 1000));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet, ethanolPercent: 24, waterPercent: 76));

        Assert.Equal(600, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        AssertMassComposition(caseData.Inlet1, ethanolPercent: 0, waterPercent: 100);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_TwoInTwoOut_WhenInlet2AndOutlet2FlowsAreUnknown_ShouldCalculateBoth()
    {
        var caseData = CreateTwoInTwoOutCase();

        await DefineStandardTwoInTwoOutCompositionsAsync(caseData);
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 400));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet1, 500));

        Assert.Equal(300, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        Assert.Equal(200, GetMassFlow(caseData.Outlet2), MassFlowToleranceKgPerHour);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_TwoInTwoOut_WhenInlet1AndOutlet2FlowsAreUnknown_ShouldCalculateBoth()
    {
        var caseData = CreateTwoInTwoOutCase();

        await DefineStandardTwoInTwoOutCompositionsAsync(caseData);
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet2, 300));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet1, 500));

        Assert.Equal(400, GetMassFlow(caseData.Inlet1), MassFlowToleranceKgPerHour);
        Assert.Equal(200, GetMassFlow(caseData.Outlet2), MassFlowToleranceKgPerHour);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_TwoInTwoOut_WhenInlet2AndOutlet1FlowsAreUnknown_ShouldCalculateBoth()
    {
        var caseData = CreateTwoInTwoOutCase();

        await DefineStandardTwoInTwoOutCompositionsAsync(caseData);
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 400));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet2, 200));

        Assert.Equal(300, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        Assert.Equal(500, GetMassFlow(caseData.Outlet1), MassFlowToleranceKgPerHour);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Regression")]
    public async Task RunSimulation_TwoInTwoOut_WhenCompositionIncludesZeroAndTwoFlowsAreUnknown_ShouldCalculateBoth()
    {
        var caseData = CreateTwoInTwoOutCase();

        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet1, ethanolPercent: 0, waterPercent: 100));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet2, ethanolPercent: 40, waterPercent: 60));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet1, ethanolPercent: 30, waterPercent: 70));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet2, ethanolPercent: 10, waterPercent: 90));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 400));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Outlet1, 600));

        Assert.Equal(533.3333333333334, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        Assert.Equal(333.3333333333333, GetMassFlow(caseData.Outlet2), MassFlowToleranceKgPerHour);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Unit")]
    public async Task RunSimulation_TwoInTwoOut_WhenAllCompositionsAndEnthalpiesAreDefined_ShouldCalculateThreeMassFlows()
    {
        var caseData = CreateTwoInTwoOutCase();

        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet1, ethanolPercent: 40, waterPercent: 60));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet2, ethanolPercent: 10, waterPercent: 90));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet1, ethanolPercent: 30, waterPercent: 70));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet2, ethanolPercent: 10, waterPercent: 90));
        await DefineAndSolveAsync(caseData, () => SetMassEnthalpy(caseData.Inlet1, 100));
        await DefineAndSolveAsync(caseData, () => SetMassEnthalpy(caseData.Inlet2, 300));
        await DefineAndSolveAsync(caseData, () => SetMassEnthalpy(caseData.Outlet1, 200));
        await DefineAndSolveAsync(caseData, () => SetMassEnthalpy(caseData.Outlet2, 266.6666666666667));
        await DefineAndSolveAsync(caseData, () => SetMassFlow(caseData.Inlet1, 100));

        Assert.Equal(200, GetMassFlow(caseData.Inlet2), MassFlowToleranceKgPerHour);
        Assert.Equal(150, GetMassFlow(caseData.Outlet1), MassFlowToleranceKgPerHour);
        Assert.Equal(150, GetMassFlow(caseData.Outlet2), MassFlowToleranceKgPerHour);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Regression")]
    public async Task RunSimulation_TwoInOneOut_WhenOutletTemperatureClosesEnergyBalance_ShouldCalculateMissingFlows()
    {
        var caseData = CreateTwoInOneOutCase();

        SetMassComposition(caseData.Inlet1, ethanolPercent: 0, waterPercent: 100);
        SetPressure(caseData.Inlet1, 1);
        SetVaporFraction(caseData.Inlet1, 100);
        SetMassFlow(caseData.Inlet1, 100);

        SetMassComposition(caseData.Inlet2, ethanolPercent: 8, waterPercent: 92);
        SetPressure(caseData.Inlet2, 4);
        SetTemperature(caseData.Inlet2, 25);

        SetPressure(caseData.Outlet, 1);
        SetTemperature(caseData.Outlet, 50);

        var result = await caseData.Solver.RunSimulationAsync();

        Assert.True(result.Converged);
        Assert.True(caseData.Inlet2.MassFlow.IsDefined);
        Assert.True(caseData.Outlet.MassFlow.IsDefined);
        Assert.True(caseData.Outlet.MassEnthalpy.IsDefined);
    }

    [Fact]
    [Trait("Spec", "Vessel")]
    [Trait("Level", "Regression")]
    public async Task RunSimulation_ThreeInTwoOut_WhenEnergyAndComponentDataAreDefined_ShouldSolveMissingFeeds()
    {
        var caseData = CreateThreeInTwoOutCase();

        SetPressurePsig(caseData.Outlet1, 25);
        SetVaporFraction(caseData.Outlet1, 100);
        SetMassComposition(caseData.Outlet1, ethanolPercent: 95, waterPercent: 5);
        SetMassFlow(caseData.Outlet1, 6000);

        SetPressurePsig(caseData.Inlet1, 25);
        SetVaporFraction(caseData.Inlet1, 0);
        SetMassComposition(caseData.Inlet1, ethanolPercent: 95, waterPercent: 5);
        SetMassFlow(caseData.Inlet1, 5000);

        SetPressure(caseData.Inlet2, 4);
        SetTemperature(caseData.Inlet2, 85);
        SetMassComposition(caseData.Inlet2, ethanolPercent: 8, waterPercent: 92);

        SetPressurePsig(caseData.Inlet3, 27);
        SetVaporFraction(caseData.Inlet3, 100);
        SetMassComposition(caseData.Inlet3, ethanolPercent: 0, waterPercent: 100);

        SetMassComposition(caseData.Outlet2, ethanolPercent: 0.1, waterPercent: 99.9);
        SetPressurePsig(caseData.Outlet2, 27);
        SetVaporFraction(caseData.Outlet2, 0);

        var result = await caseData.Solver.RunSimulationAsync();

        Assert.True(result.Converged);
        Assert.True(caseData.Inlet2.MassFlow.IsDefined);
        Assert.True(caseData.Inlet3.MassFlow.IsDefined);
        Assert.True(caseData.Outlet2.MassFlow.IsDefined);
    }

    private static VesselCase CreateOneInOneOutCase()
    {
        var solver = new MainSolver();
        var vessel = new SolverVessel("V-101");
        var inlet = new FacadeStream("S-101");
        var outlet = new FacadeStream("S-102");
        var method = CreateEthanolWaterMethod();

        inlet.SetThermodynamicMethod(method);
        outlet.SetThermodynamicMethod(method);

        vessel.AddInlet(inlet);
        vessel.AddOutlet(outlet);

        solver.AddEquipment(vessel);
        solver.AddStream(inlet);
        solver.AddStream(outlet);

        return new VesselCase(solver, vessel, inlet, outlet);
    }

    private static TwoInOneOutVesselCase CreateTwoInOneOutCase()
    {
        var solver = new MainSolver();
        var vessel = new SolverVessel("V-101");
        var inlet1 = new FacadeStream("S-101");
        var inlet2 = new FacadeStream("S-102");
        var outlet = new FacadeStream("S-103");
        var method = CreateEthanolWaterMethod();

        inlet1.SetThermodynamicMethod(method);
        inlet2.SetThermodynamicMethod(method);
        outlet.SetThermodynamicMethod(method);

        vessel.AddInlet(inlet1);
        vessel.AddInlet(inlet2);
        vessel.AddOutlet(outlet);

        solver.AddEquipment(vessel);
        solver.AddStream(inlet1);
        solver.AddStream(inlet2);
        solver.AddStream(outlet);

        return new TwoInOneOutVesselCase(solver, vessel, inlet1, inlet2, outlet);
    }

    private static TwoInTwoOutVesselCase CreateTwoInTwoOutCase()
    {
        var solver = new MainSolver();
        var vessel = new SolverVessel("V-101");
        var inlet1 = new FacadeStream("S-101");
        var inlet2 = new FacadeStream("S-102");
        var outlet1 = new FacadeStream("S-103");
        var outlet2 = new FacadeStream("S-104");
        var method = CreateEthanolWaterMethod();

        inlet1.SetThermodynamicMethod(method);
        inlet2.SetThermodynamicMethod(method);
        outlet1.SetThermodynamicMethod(method);
        outlet2.SetThermodynamicMethod(method);

        vessel.AddInlet(inlet1);
        vessel.AddInlet(inlet2);
        vessel.AddOutlet(outlet1);
        vessel.AddOutlet(outlet2);

        solver.AddEquipment(vessel);
        solver.AddStream(inlet1);
        solver.AddStream(inlet2);
        solver.AddStream(outlet1);
        solver.AddStream(outlet2);

        return new TwoInTwoOutVesselCase(solver, vessel, inlet1, inlet2, outlet1, outlet2);
    }

    private static ThreeInTwoOutVesselCase CreateThreeInTwoOutCase()
    {
        var solver = new MainSolver();
        var vessel = new SolverVessel("V-101");
        var inlet1 = new FacadeStream("S-101");
        var inlet2 = new FacadeStream("S-102");
        var inlet3 = new FacadeStream("S-103");
        var outlet1 = new FacadeStream("S-104");
        var outlet2 = new FacadeStream("S-105");
        var method = CreateEthanolWaterMethod();

        inlet1.SetThermodynamicMethod(method);
        inlet2.SetThermodynamicMethod(method);
        inlet3.SetThermodynamicMethod(method);
        outlet1.SetThermodynamicMethod(method);
        outlet2.SetThermodynamicMethod(method);

        vessel.AddInlet(inlet1);
        vessel.AddInlet(inlet2);
        vessel.AddInlet(inlet3);
        vessel.AddOutlet(outlet1);
        vessel.AddOutlet(outlet2);

        solver.AddEquipment(vessel);
        solver.AddStream(inlet1);
        solver.AddStream(inlet2);
        solver.AddStream(inlet3);
        solver.AddStream(outlet1);
        solver.AddStream(outlet2);

        return new ThreeInTwoOutVesselCase(solver, vessel, inlet1, inlet2, inlet3, outlet1, outlet2);
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

    private static void SetMassEnthalpy(IFacadeStream stream, double kcalPerKg)
    {
        stream.MassEnthalpy.SetValue(new MassEnergy(kcalPerKg, MassEnergyUnits.Kcal_Kg), VariableDefinedBy.UserInput);
    }

    private static void SetPressure(IFacadeStream stream, double bara)
    {
        stream.Pressure.SetValue(new Pressure(bara, PressureUnits.Bara), VariableDefinedBy.UserInput);
    }

    private static void SetPressurePsig(IFacadeStream stream, double psig)
    {
        stream.Pressure.SetValue(new Pressure(psig, PressureUnits.Psig), VariableDefinedBy.UserInput);
    }

    private static void SetTemperature(IFacadeStream stream, double degreeCelsius)
    {
        stream.Temperature.SetValue(new Temperature(degreeCelsius + 273.15, TemperatureUnits.Kelvin), VariableDefinedBy.UserInput);
    }

    private static void SetVaporFraction(IFacadeStream stream, double percent)
    {
        stream.VaporFraction.SetValue(new Percentage(percent, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
    }

    private static void SetMassComposition(IFacadeStream stream, double ethanolPercent, double waterPercent)
    {
        var ethanol = stream.Composition.Components.Single(component => component.Name == "Ethanol");
        var water = stream.Composition.Components.Single(component => component.Name == "Water");

        ethanol.MassFraction.SetValue(new Percentage(ethanolPercent, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        water.MassFraction.SetValue(new Percentage(waterPercent, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        stream.Composition.InputType = Shared.Thermodynamics.ControlledVariables.ComponentInputType.MassFraction;
        stream.Composition.CompositionChanged();
    }

    private static async Task AssertOutletToInletCompositionAsync(
        VesselCase caseData,
        double ethanolPercent,
        double waterPercent)
    {
        SetMassComposition(caseData.Outlet, ethanolPercent, waterPercent);

        _ = await caseData.Solver.RunSimulationAsync();

        AssertMassComposition(caseData.Outlet, ethanolPercent, waterPercent);
        AssertMassComposition(caseData.Inlet, ethanolPercent, waterPercent);
    }

    private static async Task DefineAndSolveAsync(TwoInOneOutVesselCase caseData, Action define)
    {
        define();
        _ = await caseData.Solver.RunSimulationAsync();
    }

    private static async Task DefineAndSolveAsync(TwoInTwoOutVesselCase caseData, Action define)
    {
        define();
        _ = await caseData.Solver.RunSimulationAsync();
    }

    private static async Task DefineStandardTwoInTwoOutCompositionsAsync(TwoInTwoOutVesselCase caseData)
    {
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet1, ethanolPercent: 40, waterPercent: 60));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Inlet2, ethanolPercent: 10, waterPercent: 90));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet1, ethanolPercent: 30, waterPercent: 70));
        await DefineAndSolveAsync(caseData, () => SetMassComposition(caseData.Outlet2, ethanolPercent: 20, waterPercent: 80));
    }

    private static void AssertMassComposition(IFacadeStream stream, double ethanolPercent, double waterPercent)
    {
        var ethanol = stream.Composition.Components.Single(component => component.Name == "Ethanol");
        var water = stream.Composition.Components.Single(component => component.Name == "Water");

        Assert.True(ethanol.MassFraction.IsDefined, $"{stream.Name} Ethanol MassFraction is not defined.");
        Assert.True(water.MassFraction.IsDefined, $"{stream.Name} Water MassFraction is not defined.");
        Assert.Equal(ethanolPercent, ethanol.MassFraction.Value.GetValue(PercentageUnits.Percentage), CompositionTolerancePercent);
        Assert.Equal(waterPercent, water.MassFraction.Value.GetValue(PercentageUnits.Percentage), CompositionTolerancePercent);
    }

    private sealed record VesselCase(
        MainSolver Solver,
        SolverVessel Vessel,
        IFacadeStream Inlet,
        IFacadeStream Outlet);

    private sealed record TwoInOneOutVesselCase(
        MainSolver Solver,
        SolverVessel Vessel,
        IFacadeStream Inlet1,
        IFacadeStream Inlet2,
        IFacadeStream Outlet);

    private sealed record TwoInTwoOutVesselCase(
        MainSolver Solver,
        SolverVessel Vessel,
        IFacadeStream Inlet1,
        IFacadeStream Inlet2,
        IFacadeStream Outlet1,
        IFacadeStream Outlet2);

    private sealed record ThreeInTwoOutVesselCase(
        MainSolver Solver,
        SolverVessel Vessel,
        IFacadeStream Inlet1,
        IFacadeStream Inlet2,
        IFacadeStream Inlet3,
        IFacadeStream Outlet1,
        IFacadeStream Outlet2);
}
