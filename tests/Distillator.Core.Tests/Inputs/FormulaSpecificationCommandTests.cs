using Distillator.Domain.Inputs;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Distillator.Core.Tests.Inputs;

public sealed class FormulaSpecificationCommandTests
{
    [Fact]
    public void Apply_WhenCreatingFormula_ShouldAddSpecificationWithAuditAndRequestSimulation()
    {
        var handler = new FormulaSpecificationCommandHandler();
        var equipment = new TestSolverEquipment();

        var result = handler.Apply(
            new UpsertFormulaSpecificationCommand(
                equipment,
                "A.MassFlow = B.MassFlow",
                CreateEquation(),
                ExistingSpecification: null,
                "user-1",
                "Alfonso"));

        Assert.Equal(FormulaSpecificationCommandStatus.Applied, result.Status);
        Assert.True(result.ShouldRunSimulation);
        Assert.NotNull(result.Specification);
        Assert.Single(equipment.Specifications);
        Assert.Equal("A.MassFlow = B.MassFlow", result.Specification.Formula);
        Assert.Equal("user-1", result.Specification.DefinedByUserId);
        Assert.Equal("Alfonso", result.Specification.DefinedByUserName);
        Assert.NotNull(result.Specification.DefinedAtUtc);
    }

    [Fact]
    public void Apply_WhenEditingFormula_ShouldPreserveIdAndReplaceSingleSpecification()
    {
        var handler = new FormulaSpecificationCommandHandler();
        var equipment = new TestSolverEquipment();
        var existing = new FormulaSpecification("A.MassFlow = B.MassFlow", CreateEquation())
        {
            Id = Guid.NewGuid(),
            DefinedByUserId = "old-user",
            DefinedByUserName = "Old User",
            DefinedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        equipment.AddSpec(existing);

        var result = handler.Apply(
            new UpsertFormulaSpecificationCommand(
                equipment,
                "A.MassFlow = 2 * B.MassFlow",
                CreateEquation(),
                existing,
                "user-2",
                "Alfonso"));

        Assert.True(result.Succeeded);
        Assert.True(result.ShouldRunSimulation);
        var updated = Assert.Single(equipment.Specifications.OfType<FormulaSpecification>());
        Assert.Equal(existing.Id, updated.Id);
        Assert.Equal("A.MassFlow = 2 * B.MassFlow", updated.Formula);
        Assert.Equal("user-2", updated.DefinedByUserId);
        Assert.Equal("Alfonso", updated.DefinedByUserName);
    }

    [Fact]
    public void Apply_WhenRemovingFormula_ShouldRemoveByIdentityAndRequestSimulation()
    {
        var handler = new FormulaSpecificationCommandHandler();
        var equipment = new TestSolverEquipment();
        var existing = new FormulaSpecification("A.MassFlow = B.MassFlow", CreateEquation());
        equipment.AddSpec(existing);

        var result = handler.Apply(
            new RemoveFormulaSpecificationCommand(equipment, existing));

        Assert.Equal(FormulaSpecificationCommandStatus.Removed, result.Status);
        Assert.True(result.ShouldRunSimulation);
        Assert.Empty(equipment.Specifications);
    }

    [Fact]
    public void Apply_WhenRemovingMissingFormula_ShouldRejectWithoutSimulation()
    {
        var handler = new FormulaSpecificationCommandHandler();
        var equipment = new TestSolverEquipment();
        var missing = new FormulaSpecification("A.MassFlow = B.MassFlow", CreateEquation());

        var result = handler.Apply(
            new RemoveFormulaSpecificationCommand(equipment, missing));

        Assert.Equal(FormulaSpecificationCommandStatus.Rejected, result.Status);
        Assert.False(result.ShouldRunSimulation);
        Assert.Empty(equipment.Specifications);
    }

    private static FormulaEquationExpression CreateEquation()
    {
        return new FormulaEquationExpression(
            new FormulaConstantNode(1),
            new FormulaConstantNode(1));
    }

    private sealed class TestSolverEquipment : SolverEquipmentBase
    {
        public override List<ISolverEquation> Equations { get; } = [];
    }
}
