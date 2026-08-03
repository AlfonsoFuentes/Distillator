using Distillator.Domain.Inputs;
using Shared.SolverConsecutive;
using UnitSystem;

namespace Distillator.Core.Tests.Inputs;

public sealed class VariableInputCommandTests
{
    [Fact]
    public void Apply_WhenTemperatureInputIsValid_ShouldCommitUserValueAndAudit()
    {
        var handler = new VariableInputCommandHandler();
        var variable = new Variable<Temperature>(
            new Temperature(298.15, TemperatureUnits.Kelvin),
            TemperatureUnits.DegreeCelcius,
            298);
        var originalAmount = variable.Value;

        var result = handler.Apply(
            new SetVariableInputCommand<Temperature>(
                variable,
                35,
                TemperatureUnits.DegreeCelcius,
                "user-1",
                "Alfonso"));

        Assert.Equal(VariableInputCommandStatus.Applied, result.Status);
        Assert.True(result.ShouldRunSimulation);
        Assert.True(variable.IsDefinedByUI);
        Assert.Equal(35, variable.GetDisplayValue(), precision: 8);
        Assert.Equal("user-1", variable.DefinedByUserId);
        Assert.Equal("Alfonso", variable.DefinedByUserName);
        Assert.NotNull(variable.DefinedAtUtc);
        Assert.NotSame(originalAmount, variable.Value);
        Assert.Equal(298.15, originalAmount.GetValue(TemperatureUnits.Kelvin), precision: 8);
    }

    [Fact]
    public void Apply_WhenTemperatureUsesDisplayUnit_ShouldValidateAgainstInternalUnit()
    {
        var handler = new VariableInputCommandHandler();
        var variable = new Variable<Temperature>(
            new Temperature(298.15, TemperatureUnits.Kelvin),
            TemperatureUnits.DegreeCelcius,
            298);

        var result = handler.Apply(
            new SetVariableInputCommand<Temperature>(
                variable,
                20,
                TemperatureUnits.DegreeCelcius,
                null,
                null));

        Assert.True(result.Succeeded);
        Assert.True(result.ShouldRunSimulation);
        Assert.Equal(20, variable.GetDisplayValue(), precision: 8);
    }

    [Fact]
    public void Apply_WhenValueIsNotFinite_ShouldRejectWithoutMutatingVariable()
    {
        var handler = new VariableInputCommandHandler();
        var variable = new Variable<MassFlow>(
            new MassFlow(1, MassFlowUnits.Kg_sg),
            MassFlowUnits.Kg_hr,
            3);
        var originalValue = variable.Value.Value;

        var result = handler.Apply(
            new SetVariableInputCommand<MassFlow>(
                variable,
                double.NaN,
                MassFlowUnits.Kg_hr,
                "user-1",
                "Alfonso"));

        Assert.Equal(VariableInputCommandStatus.Rejected, result.Status);
        Assert.False(result.ShouldRunSimulation);
        Assert.False(variable.IsDefined);
        Assert.Equal(originalValue, variable.Value.Value, precision: 8);
        Assert.Null(variable.DefinedByUserId);
    }

    [Fact]
    public void Apply_WhenUnitIsIncompatible_ShouldRejectWithoutMutatingVariable()
    {
        var handler = new VariableInputCommandHandler();
        var variable = new Variable<MassFlow>(
            new MassFlow(1, MassFlowUnits.Kg_sg),
            MassFlowUnits.Kg_hr,
            3);
        var originalValue = variable.Value.Value;

        var result = handler.Apply(
            new SetVariableInputCommand<MassFlow>(
                variable,
                35,
                TemperatureUnits.DegreeCelcius,
                "user-1",
                "Alfonso"));

        Assert.Equal(VariableInputCommandStatus.Rejected, result.Status);
        Assert.False(result.ShouldRunSimulation);
        Assert.Contains("not compatible", result.ErrorMessage);
        Assert.False(variable.IsDefined);
        Assert.Equal(originalValue, variable.Value.Value, precision: 8);
        Assert.Null(variable.DefinedByUserId);
    }

    [Fact]
    public void Apply_WhenUnitLessInputIsValid_ShouldCommitWithoutDisplayUnitConstructor()
    {
        var handler = new VariableInputCommandHandler();
        var variable = new Variable<UnitLess>(
            new UnitLess(1),
            UnitLessUnits.None,
            1);

        var result = handler.Apply(
            new SetVariableInputCommand<UnitLess>(
                variable,
                2.5,
                UnitLessUnits.None,
                null,
                null));

        Assert.Equal(VariableInputCommandStatus.Applied, result.Status);
        Assert.True(result.ShouldRunSimulation);
        Assert.Equal(2.5, variable.Value.Value, precision: 8);
        Assert.True(variable.IsDefinedByUI);
    }

    [Fact]
    public void Apply_WhenClearingUserInput_ShouldClearAndRequestSimulation()
    {
        var handler = new VariableInputCommandHandler();
        var variable = new Variable<Percentage>(
            new Percentage(0, PercentageUnits.Percentage),
            PercentageUnits.Percentage,
            100);

        handler.Apply(
            new SetVariableInputCommand<Percentage>(
                variable,
                42,
                PercentageUnits.Percentage,
                "user-1",
                "Alfonso"));

        var result = handler.Apply(new ClearVariableInputCommand<Percentage>(variable));

        Assert.Equal(VariableInputCommandStatus.Cleared, result.Status);
        Assert.True(result.ShouldRunSimulation);
        Assert.False(variable.IsDefined);
        Assert.Null(variable.DefinedByUserId);
        Assert.Null(variable.DefinedByUserName);
        Assert.Null(variable.DefinedAtUtc);
    }
}
