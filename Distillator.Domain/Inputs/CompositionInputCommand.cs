using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.ControlledVariables;
using UnitSystem;

namespace Distillator.Domain.Inputs;

public enum CompositionInputCommandStatus
{
    Applied,
    Cleared,
    Rejected
}

public sealed record CompositionInputCommandResult(
    CompositionInputCommandStatus Status,
    bool ShouldRunSimulation,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status != CompositionInputCommandStatus.Rejected;
}

public sealed record SetCompositionFractionCommand(
    CompositionOrchestrator Composition,
    ComponentFacade Component,
    ComponentInputType InputType,
    double Percentage,
    string? UserId,
    string? UserName);

public sealed record SetEthanolWaterMassCompositionCommand(
    CompositionOrchestrator Composition,
    ComponentFacade Ethanol,
    ComponentFacade Water,
    double EthanolMassPercentage,
    string? UserId,
    string? UserName);

public sealed record ClearCompositionInputCommand(
    CompositionOrchestrator Composition);

public sealed class CompositionInputCommandHandler
{
    private const double CompleteSumTolerance = 1.0e-6;
    private readonly VariableInputCommandHandler _variableInputCommandHandler;

    public CompositionInputCommandHandler(VariableInputCommandHandler variableInputCommandHandler)
    {
        _variableInputCommandHandler = variableInputCommandHandler;
    }

    public CompositionInputCommandResult Apply(SetCompositionFractionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Composition);
        ArgumentNullException.ThrowIfNull(command.Component);

        var previousInputType = command.Composition.InputType;
        var inputTypeChanged = previousInputType != command.InputType;
        var validationError = ValidatePercentage(command.Percentage);
        if (validationError != null)
        {
            return Rejected(validationError);
        }

        command.Composition.InputType = command.InputType;
        var result = command.InputType switch
        {
            ComponentInputType.MassFraction => _variableInputCommandHandler.Apply(
                new SetVariableInputCommand<Percentage>(
                    command.Component.MassFraction,
                    command.Percentage,
                    PercentageUnits.Percentage,
                    command.UserId,
                    command.UserName)),
            ComponentInputType.MolarFraction => _variableInputCommandHandler.Apply(
                new SetVariableInputCommand<Percentage>(
                    command.Component.MolarFraction,
                    command.Percentage,
                    PercentageUnits.Percentage,
                    command.UserId,
                    command.UserName)),
            _ => throw new ArgumentOutOfRangeException(nameof(command), "Unsupported composition input type.")
        };

        if (!result.Succeeded)
        {
            command.Composition.InputType = previousInputType;
            return Rejected(result.ErrorMessage ?? "Invalid composition input.");
        }

        var isComplete = IsComplete(command.Composition, command.InputType);
        var changed = inputTypeChanged || result.Changed;
        if (isComplete && changed)
        {
            command.Composition.CompositionChanged();
        }

        return new CompositionInputCommandResult(
            CompositionInputCommandStatus.Applied,
            isComplete && changed);
    }

    public CompositionInputCommandResult Apply(SetEthanolWaterMassCompositionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Composition);
        ArgumentNullException.ThrowIfNull(command.Ethanol);
        ArgumentNullException.ThrowIfNull(command.Water);

        var previousInputType = command.Composition.InputType;
        var inputTypeChanged = previousInputType != ComponentInputType.MassFraction;
        var validationError = ValidatePercentage(command.EthanolMassPercentage);
        if (validationError != null)
        {
            return Rejected(validationError);
        }

        command.Composition.InputType = ComponentInputType.MassFraction;
        var ethanolResult = _variableInputCommandHandler.Apply(
            new SetVariableInputCommand<Percentage>(
                command.Ethanol.MassFraction,
                command.EthanolMassPercentage,
                PercentageUnits.Percentage,
                command.UserId,
                command.UserName));
        if (!ethanolResult.Succeeded)
        {
            command.Composition.InputType = previousInputType;
            return Rejected(ethanolResult.ErrorMessage ?? "Invalid ethanol composition input.");
        }

        var waterResult = _variableInputCommandHandler.Apply(
            new SetVariableInputCommand<Percentage>(
                command.Water.MassFraction,
                100 - command.EthanolMassPercentage,
                PercentageUnits.Percentage,
                command.UserId,
                command.UserName));
        if (!waterResult.Succeeded)
        {
            command.Composition.InputType = previousInputType;
            return Rejected(waterResult.ErrorMessage ?? "Invalid water composition input.");
        }

        var isComplete = IsComplete(command.Composition, ComponentInputType.MassFraction);
        var changed = inputTypeChanged || ethanolResult.Changed || waterResult.Changed;
        if (isComplete && changed)
        {
            command.Composition.CompositionChanged();
        }

        return new CompositionInputCommandResult(
            CompositionInputCommandStatus.Applied,
            isComplete && changed);
    }

    public CompositionInputCommandResult Apply(ClearCompositionInputCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Composition);

        var hadUserInput = command.Composition.InputType != ComponentInputType.None ||
                           command.Composition.Components.Any(component =>
                               component.MassFraction.IsDefined || component.MolarFraction.IsDefined);

        command.Composition.Clear();

        return new CompositionInputCommandResult(
            CompositionInputCommandStatus.Cleared,
            hadUserInput);
    }

    private static string? ValidatePercentage(double percentage)
    {
        if (!double.IsFinite(percentage))
        {
            return "Composition value must be finite.";
        }

        if (percentage < 0 || percentage > 100)
        {
            return "Composition value must be between 0 and 100.";
        }

        return null;
    }

    private static bool IsComplete(CompositionOrchestrator composition, ComponentInputType inputType)
    {
        var components = composition.Components;
        if (components.Count == 0) return false;

        var values = inputType switch
        {
            ComponentInputType.MassFraction => components
                .Select(component => component.MassFraction)
                .ToList(),
            ComponentInputType.MolarFraction => components
                .Select(component => component.MolarFraction)
                .ToList(),
            _ => []
        };

        if (values.Count == 0 || values.Any(variable => !variable.IsDefined))
        {
            return false;
        }

        var sum = values.Sum(variable => variable.GetDisplayValue());
        return Math.Abs(sum - 100) <= CompleteSumTolerance;
    }

    private static CompositionInputCommandResult Rejected(string errorMessage)
    {
        return new CompositionInputCommandResult(
            CompositionInputCommandStatus.Rejected,
            ShouldRunSimulation: false,
            errorMessage);
    }
}
