using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments.Columns;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Distillator.Core.Tests.Solver;

public sealed class ColumnBalanceTests
{
    [Fact]
    [Trait("Spec", "Column")]
    [Trait("Level", "Regression")]
    public async Task RunSimulation_WhenColumnHasRefluxFeedVaporAndTwoProducts_ShouldSolveMissingFlows()
    {
        var caseData = CreateCase();

        SetPressurePsig(caseData.VaporOutlet, 25);
        SetVaporFraction(caseData.VaporOutlet, 100);
        SetMassComposition(caseData.VaporOutlet, ethanolPercent: 95, waterPercent: 5);
        SetMassFlow(caseData.VaporOutlet, 6000);

        SetPressurePsig(caseData.RefluxInlet, 25);
        SetVaporFraction(caseData.RefluxInlet, 0);
        SetMassComposition(caseData.RefluxInlet, ethanolPercent: 95, waterPercent: 5);
        SetMassFlow(caseData.RefluxInlet, 5000);

        SetPressure(caseData.Feed, 4);
        SetTemperature(caseData.Feed, 85);
        SetMassComposition(caseData.Feed, ethanolPercent: 8, waterPercent: 92);

        SetPressurePsig(caseData.VaporInlet, 27);
        SetVaporFraction(caseData.VaporInlet, 100);
        SetMassComposition(caseData.VaporInlet, ethanolPercent: 0, waterPercent: 100);

        SetMassComposition(caseData.BottomOutlet, ethanolPercent: 0.1, waterPercent: 99.9);
        SetPressurePsig(caseData.BottomOutlet, 27);
        SetVaporFraction(caseData.BottomOutlet, 0);

        var result = await caseData.Solver.RunSimulationAsync();

        Assert.True(result.Converged);
        Assert.True(caseData.Feed.MassFlow.IsDefined);
        Assert.True(caseData.VaporInlet.MassFlow.IsDefined);
        Assert.True(caseData.BottomOutlet.MassFlow.IsDefined);
    }

    private static ColumnCase CreateCase()
    {
        var solver = new MainSolver();
        var column = new SolverColumn("C-101");
        var reflux = new FacadeStream("Reflux");
        var feed = new FacadeStream("Feed");
        var vaporInlet = new FacadeStream("Vapor inlet");
        var vaporOutlet = new FacadeStream("Vapor outlet");
        var bottomOutlet = new FacadeStream("Bottom outlet");
        var method = CreateEthanolWaterMethod();

        reflux.SetThermodynamicMethod(method);
        feed.SetThermodynamicMethod(method);
        vaporInlet.SetThermodynamicMethod(method);
        vaporOutlet.SetThermodynamicMethod(method);
        bottomOutlet.SetThermodynamicMethod(method);

        column.SetRefluxInlet(reflux);
        column.AddFeed(feed);
        column.SetVaporInlet(vaporInlet);
        column.SetTopVaporOutlet(vaporOutlet);
        column.SetBottomOutlet(bottomOutlet);

        solver.AddEquipment(column);
        solver.AddStream(reflux);
        solver.AddStream(feed);
        solver.AddStream(vaporInlet);
        solver.AddStream(vaporOutlet);
        solver.AddStream(bottomOutlet);

        return new ColumnCase(solver, column, reflux, feed, vaporInlet, vaporOutlet, bottomOutlet);
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
                    Guid.Parse("b595e4f2-54db-4058-9a2c-d36b7f88962c"),
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

    private sealed record ColumnCase(
        MainSolver Solver,
        SolverColumn Column,
        IFacadeStream RefluxInlet,
        IFacadeStream Feed,
        IFacadeStream VaporInlet,
        IFacadeStream VaporOutlet,
        IFacadeStream BottomOutlet);
}
