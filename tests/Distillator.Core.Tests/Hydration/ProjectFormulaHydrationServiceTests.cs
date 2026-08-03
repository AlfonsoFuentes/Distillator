using Distillator.Domain.Services;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Distillator.Core.Tests.Hydration;

public sealed class ProjectFormulaHydrationServiceTests
{
    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "10")]
    [Trait("Level", "Unit")]
    public void Restore_WhenFormulaIsValid_ShouldAddSpecificationWithAudit()
    {
        var equipment = new TestSolverEquipment();
        var streams = new[]
        {
            new FacadeStream { Name = "A" },
            new FacadeStream { Name = "B" }
        };
        var specificationId = Guid.NewGuid();
        var definedAt = DateTime.UtcNow.AddMinutes(-5);

        var restored = new ProjectFormulaHydrationService().Restore(
            equipment,
            new[]
            {
                new FormulaSpecificationHydrationSnapshot(
                    specificationId,
                    "A.MassFlow = B.MassFlow",
                    "user-1",
                    "Alfonso",
                    definedAt)
            },
            streams);

        Assert.Equal(1, restored);
        var specification = Assert.Single(equipment.Specifications.OfType<FormulaSpecification>());
        Assert.Equal(specificationId, specification.Id);
        Assert.Equal("A.MassFlow = B.MassFlow", specification.Formula);
        Assert.Equal("user-1", specification.DefinedByUserId);
        Assert.Equal("Alfonso", specification.DefinedByUserName);
        Assert.Equal(definedAt, specification.DefinedAtUtc);
    }

    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "10")]
    [Trait("Level", "Unit")]
    public void Restore_WhenFormulaIsInvalid_ShouldSkipSpecification()
    {
        var equipment = new TestSolverEquipment();
        var streams = new[] { new FacadeStream { Name = "A" } };

        var restored = new ProjectFormulaHydrationService().Restore(
            equipment,
            new[]
            {
                new FormulaSpecificationHydrationSnapshot(
                    Guid.NewGuid(),
                    "Unknown.MassFlow = A.MassFlow")
            },
            streams);

        Assert.Equal(0, restored);
        Assert.Empty(equipment.Specifications);
    }

    private sealed class TestSolverEquipment : SolverEquipmentBase
    {
        public override List<ISolverEquation> Equations { get; } = [];
    }
}
