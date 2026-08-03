using Shared.SolverConsecutive.Equipments;

namespace Distillator.Domain.Inputs;

public enum FormulaSpecificationCommandStatus
{
    Applied,
    Removed,
    Rejected
}

public sealed record FormulaSpecificationCommandResult(
    FormulaSpecificationCommandStatus Status,
    bool ShouldRunSimulation,
    FormulaSpecification? Specification = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status != FormulaSpecificationCommandStatus.Rejected;
}

public sealed record UpsertFormulaSpecificationCommand(
    SolverEquipmentBase Equipment,
    string Formula,
    FormulaEquationExpression Equation,
    FormulaSpecification? ExistingSpecification,
    string? UserId,
    string? UserName);

public sealed record RemoveFormulaSpecificationCommand(
    SolverEquipmentBase Equipment,
    FormulaSpecification Specification);

public sealed class FormulaSpecificationCommandHandler
{
    public FormulaSpecificationCommandResult Apply(UpsertFormulaSpecificationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Equipment);
        ArgumentNullException.ThrowIfNull(command.Equation);

        var formula = command.Formula.Trim();
        if (string.IsNullOrWhiteSpace(formula))
        {
            return Rejected("Formula is required.");
        }

        if (command.ExistingSpecification != null &&
            string.Equals(command.ExistingSpecification.Formula.Trim(), formula, StringComparison.Ordinal))
        {
            return new FormulaSpecificationCommandResult(
                FormulaSpecificationCommandStatus.Applied,
                ShouldRunSimulation: false,
                command.ExistingSpecification);
        }

        if (command.ExistingSpecification != null)
        {
            command.Equipment.RemoveSpec(command.ExistingSpecification);
        }

        var specification = new FormulaSpecification(formula, command.Equation)
        {
            Id = command.ExistingSpecification?.Id ?? Guid.NewGuid(),
            DefinedByUserId = command.UserId ?? command.ExistingSpecification?.DefinedByUserId,
            DefinedByUserName = command.UserName ?? command.ExistingSpecification?.DefinedByUserName,
            DefinedAtUtc = DateTime.UtcNow
        };

        command.Equipment.AddSpec(specification);

        return new FormulaSpecificationCommandResult(
            FormulaSpecificationCommandStatus.Applied,
            ShouldRunSimulation: true,
            specification);
    }

    public FormulaSpecificationCommandResult Apply(RemoveFormulaSpecificationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Equipment);
        ArgumentNullException.ThrowIfNull(command.Specification);

        var existed = command.Equipment.Specifications.Any(specification =>
            specification.Id == command.Specification.Id);
        if (!existed)
        {
            return Rejected("Formula specification was not found.");
        }

        command.Equipment.RemoveSpec(command.Specification);

        return new FormulaSpecificationCommandResult(
            FormulaSpecificationCommandStatus.Removed,
            ShouldRunSimulation: true);
    }

    private static FormulaSpecificationCommandResult Rejected(string errorMessage)
    {
        return new FormulaSpecificationCommandResult(
            FormulaSpecificationCommandStatus.Rejected,
            ShouldRunSimulation: false,
            ErrorMessage: errorMessage);
    }
}
