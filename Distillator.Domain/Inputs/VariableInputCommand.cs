using Shared.SolverConsecutive;
using UnitSystem;

namespace Distillator.Domain.Inputs;

public enum VariableInputCommandStatus
{
    Applied,
    Cleared,
    Rejected
}

public sealed record VariableInputCommandResult(
    VariableInputCommandStatus Status,
    bool ShouldRunSimulation,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status != VariableInputCommandStatus.Rejected;
    public bool Changed { get; init; }
}

public sealed record SetVariableInputCommand<T>(
    Variable<T> Variable,
    double Value,
    UnitMeasure Unit,
    string? UserId,
    string? UserName)
    where T : Amount;

public sealed record ClearVariableInputCommand<T>(
    Variable<T> Variable)
    where T : Amount;

public sealed class VariableInputCommandHandler
{
    public VariableInputCommandResult Apply<T>(SetVariableInputCommand<T> command)
        where T : Amount
    {
        ArgumentNullException.ThrowIfNull(command.Variable);
        ArgumentNullException.ThrowIfNull(command.Unit);

        var value = CreateAmount<T>(command.Value, command.Unit);
        var validationError = ValidateValue(command.Variable, value);
        if (validationError != null)
        {
            return new VariableInputCommandResult(
                VariableInputCommandStatus.Rejected,
                ShouldRunSimulation: false,
                validationError)
            {
                Changed = false
            };
        }

        if (IsSameUserInput(command.Variable, value))
        {
            return new VariableInputCommandResult(
                VariableInputCommandStatus.Applied,
                ShouldRunSimulation: false)
            {
                Changed = false
            };
        }

        command.Variable.SetValueFromUI(value, command.UserId, command.UserName);

        return new VariableInputCommandResult(
            VariableInputCommandStatus.Applied,
            command.Variable.ShouldTriggerRecalculation)
        {
            Changed = true
        };
    }

    public VariableInputCommandResult Apply<T>(ClearVariableInputCommand<T> command)
        where T : Amount
    {
        ArgumentNullException.ThrowIfNull(command.Variable);

        if (!command.Variable.IsDefinedByUI)
        {
            return new VariableInputCommandResult(
                VariableInputCommandStatus.Cleared,
                ShouldRunSimulation: false)
            {
                Changed = false
            };
        }

        var shouldRunSimulation = command.Variable.ShouldTriggerRecalculation;
        command.Variable.ClearFromUI();

        return new VariableInputCommandResult(
            VariableInputCommandStatus.Cleared,
            shouldRunSimulation)
        {
            Changed = true
        };
    }

    private static T CreateAmount<T>(double value, UnitMeasure unit)
        where T : Amount
    {
        if (typeof(T) == typeof(UnitLess))
        {
            return (T)(Amount)new UnitLess(value);
        }

        return (T)Activator.CreateInstance(typeof(T), value, unit)!;
    }

    private static string? ValidateValue<T>(Variable<T> variable, T value)
        where T : Amount
    {
        if (!double.IsFinite(value.Value))
        {
            return "Value must be finite.";
        }

        if (IsCompatibleUnit(value.Unit, variable.InternalUnit) ||
            IsCompatibleUnit(value.Unit, variable.DisplayUnit))
        {
            return null;
        }

        try
        {
            _ = value.GetValue(variable.InternalUnit);
            return null;
        }
        catch (UnitConversionException)
        {
            return $"Unit '{value.Unit.Symbol}' is not compatible with '{variable.InternalUnit.Symbol}'.";
        }
    }

    private static bool IsCompatibleUnit(UnitMeasure left, UnitMeasure right)
    {
        return left.IsCompatibleTo(right) ||
               string.Equals(left.Family, right.Family, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameUserInput<T>(Variable<T> variable, T value)
        where T : Amount
    {
        if (!variable.IsDefinedByUI)
        {
            return false;
        }

        var currentValue = variable.Value.GetValue(variable.InternalUnit);
        var newValue = value.GetValue(variable.InternalUnit);
        return AreEquivalent(currentValue, newValue);
    }

    private static bool AreEquivalent(double left, double right)
    {
        var scale = Math.Max(1.0, Math.Max(Math.Abs(left), Math.Abs(right)));
        return Math.Abs(left - right) <= scale * 1.0e-10;
    }
}
