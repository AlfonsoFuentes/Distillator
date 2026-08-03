using Distillator.Domain.Inputs;
using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.ControlledVariables;
using UnitSystem;

namespace Distillator.Core.Tests.Inputs;

public sealed class CompositionInputCommandTests
{
    [Fact]
    public void Apply_WhenOnlyOneComponentIsDefined_ShouldApplyButNotRunSimulation()
    {
        var handler = CreateHandler();
        var (composition, ethanol, _) = CreateComposition();

        var result = handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                ethanol,
                ComponentInputType.MassFraction,
                100,
                "user-1",
                "Alfonso"));

        Assert.Equal(CompositionInputCommandStatus.Applied, result.Status);
        Assert.False(result.ShouldRunSimulation);
        Assert.Equal(ComponentInputType.MassFraction, composition.InputType);
        Assert.True(ethanol.MassFraction.IsDefinedByUI);
        Assert.Equal(100, ethanol.MassFraction.GetDisplayValue(), precision: 8);
    }

    [Fact]
    public void Apply_WhenAllComponentsAreDefinedAndSumIsOneHundred_ShouldRunSimulation()
    {
        var handler = CreateHandler();
        var (composition, ethanol, water) = CreateComposition();

        var first = handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                ethanol,
                ComponentInputType.MassFraction,
                40,
                "user-1",
                "Alfonso"));
        var second = handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                water,
                ComponentInputType.MassFraction,
                60,
                "user-1",
                "Alfonso"));

        Assert.False(first.ShouldRunSimulation);
        Assert.True(second.ShouldRunSimulation);
        Assert.True(composition.IsValid);
    }

    [Fact]
    public void Apply_WhenPercentageIsOutOfRange_ShouldRejectWithoutMutatingComposition()
    {
        var handler = CreateHandler();
        var (composition, ethanol, _) = CreateComposition();

        var result = handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                ethanol,
                ComponentInputType.MassFraction,
                120,
                "user-1",
                "Alfonso"));

        Assert.Equal(CompositionInputCommandStatus.Rejected, result.Status);
        Assert.False(result.ShouldRunSimulation);
        Assert.Equal(ComponentInputType.None, composition.InputType);
        Assert.False(ethanol.MassFraction.IsDefined);
    }

    [Fact]
    public void Apply_WhenEthanolWaterMassCompositionIsSet_ShouldDefineBothComponentsAndRunSimulation()
    {
        var handler = CreateHandler();
        var (composition, ethanol, water) = CreateComposition();

        var result = handler.Apply(
            new SetEthanolWaterMassCompositionCommand(
                composition,
                ethanol,
                water,
                35,
                "user-1",
                "Alfonso"));

        Assert.Equal(CompositionInputCommandStatus.Applied, result.Status);
        Assert.True(result.ShouldRunSimulation);
        Assert.Equal(35, ethanol.MassFraction.GetDisplayValue(), precision: 8);
        Assert.Equal(65, water.MassFraction.GetDisplayValue(), precision: 8);
        Assert.Equal(ComponentInputType.MassFraction, composition.InputType);
    }

    [Fact]
    public void Apply_WhenMassFractionChangesAfterMolarWasCalculated_ShouldKeepMolarFractionsAvailableForRefresh()
    {
        var handler = CreateHandler();
        var (composition, ethanol, water) = CreateComposition();
        ethanol.MolarFraction.SetValue(
            new Percentage(20.68, PercentageUnits.Percentage),
            VariableDefinedBy.Solver);
        water.MolarFraction.SetValue(
            new Percentage(79.32, PercentageUnits.Percentage),
            VariableDefinedBy.Solver);

        var result = handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                ethanol,
                ComponentInputType.MassFraction,
                20,
                "user-1",
                "Alfonso"));

        Assert.Equal(CompositionInputCommandStatus.Applied, result.Status);
        Assert.False(result.ShouldRunSimulation);
        Assert.True(ethanol.MassFraction.IsDefinedByUI);
        Assert.True(ethanol.MolarFraction.IsDefined);
        Assert.True(water.MolarFraction.IsDefined);
    }

    [Fact]
    public void Apply_WhenMolarFractionChangesAfterMassWasCalculated_ShouldKeepMassFractionsAvailableForRefresh()
    {
        var handler = CreateHandler();
        var (composition, ethanol, water) = CreateComposition();
        ethanol.MassFraction.SetValue(
            new Percentage(40, PercentageUnits.Percentage),
            VariableDefinedBy.Solver);
        water.MassFraction.SetValue(
            new Percentage(60, PercentageUnits.Percentage),
            VariableDefinedBy.Solver);

        var result = handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                ethanol,
                ComponentInputType.MolarFraction,
                20.68,
                "user-1",
                "Alfonso"));

        Assert.Equal(CompositionInputCommandStatus.Applied, result.Status);
        Assert.False(result.ShouldRunSimulation);
        Assert.True(ethanol.MolarFraction.IsDefinedByUI);
        Assert.True(ethanol.MassFraction.IsDefined);
        Assert.True(water.MassFraction.IsDefined);
    }

    [Fact]
    public void Apply_WhenMassCompositionIsComplete_ShouldKeepOppositeBasisAvailableForSolverRefresh()
    {
        var handler = CreateHandler();
        var (composition, ethanol, water) = CreateComposition();
        ethanol.MolarFraction.SetValue(
            new Percentage(20.68, PercentageUnits.Percentage),
            VariableDefinedBy.Solver);
        water.MolarFraction.SetValue(
            new Percentage(79.32, PercentageUnits.Percentage),
            VariableDefinedBy.Solver);

        handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                ethanol,
                ComponentInputType.MassFraction,
                40,
                "user-1",
                "Alfonso"));
        var result = handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                water,
                ComponentInputType.MassFraction,
                60,
                "user-1",
                "Alfonso"));

        Assert.True(result.ShouldRunSimulation);
        Assert.True(ethanol.MolarFraction.IsDefined);
        Assert.True(water.MolarFraction.IsDefined);
    }

    [Fact]
    public void Apply_WhenMolarCompositionIsComplete_ShouldKeepOppositeBasisAvailableForSolverRefresh()
    {
        var handler = CreateHandler();
        var (composition, ethanol, water) = CreateComposition();
        ethanol.MassFraction.SetValue(
            new Percentage(40, PercentageUnits.Percentage),
            VariableDefinedBy.Solver);
        water.MassFraction.SetValue(
            new Percentage(60, PercentageUnits.Percentage),
            VariableDefinedBy.Solver);

        handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                ethanol,
                ComponentInputType.MolarFraction,
                20,
                "user-1",
                "Alfonso"));
        var result = handler.Apply(
            new SetCompositionFractionCommand(
                composition,
                water,
                ComponentInputType.MolarFraction,
                80,
                "user-1",
                "Alfonso"));

        Assert.True(result.ShouldRunSimulation);
        Assert.True(ethanol.MassFraction.IsDefined);
        Assert.True(water.MassFraction.IsDefined);
    }

    [Fact]
    public void Apply_WhenClearingCompositionWithInput_ShouldClearAndRunSimulation()
    {
        var handler = CreateHandler();
        var (composition, ethanol, water) = CreateComposition();
        handler.Apply(
            new SetEthanolWaterMassCompositionCommand(
                composition,
                ethanol,
                water,
                35,
                "user-1",
                "Alfonso"));

        var result = handler.Apply(new ClearCompositionInputCommand(composition));

        Assert.Equal(CompositionInputCommandStatus.Cleared, result.Status);
        Assert.True(result.ShouldRunSimulation);
        Assert.Equal(ComponentInputType.None, composition.InputType);
        Assert.False(ethanol.MassFraction.IsDefined);
        Assert.False(water.MassFraction.IsDefined);
    }

    private static CompositionInputCommandHandler CreateHandler()
    {
        return new CompositionInputCommandHandler(new VariableInputCommandHandler());
    }

    private static (CompositionOrchestrator Composition, ComponentFacade Ethanol, ComponentFacade Water) CreateComposition()
    {
        var ethanol = new ComponentFacade(CreateComponent("Ethanol", "C2H6O", 46.07));
        var water = new ComponentFacade(CreateComponent("Water", "H2O", 18.015));
        var composition = new CompositionOrchestrator([ethanol, water]);

        return (composition, ethanol, water);
    }

    private static MethodComponentFullDto CreateComponent(
        string name,
        string formula,
        double molecularWeight)
    {
        var id = Guid.NewGuid();

        return new MethodComponentFullDto
        {
            ComponentId = id,
            ComponentName = name,
            FullData = new ChemicalComponentDto
            {
                Id = id,
                Name = name,
                Formula = formula,
                MolecularWeight = molecularWeight
            }
        };
    }
}
