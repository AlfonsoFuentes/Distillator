using Shared.Projects;

namespace Distillator.Core.Tests.Persistence;

public sealed class ProjectVisualIntentPolicyTests
{
    [Fact]
    [Trait("Spec", "06")]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void CanReapplyExistingElementVisuals_WhenAllIntendedElementsExist_ShouldReturnTrue()
    {
        var equipmentA = Guid.Parse("41588b6d-1cb3-491b-8252-4ee2f8e9f3c4");
        var equipmentB = Guid.Parse("e08e539e-8479-4feb-80f9-d6950cd1077c");

        var result = ProjectVisualIntentPolicy.CanReapplyExistingElementVisuals(
            new[] { equipmentA, equipmentB },
            new[] { equipmentA });

        Assert.True(result);
    }

    [Fact]
    [Trait("Spec", "06")]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void CanReapplyExistingElementVisuals_WhenIntentOnlyChangesCamera_ShouldReturnTrue()
    {
        var result = ProjectVisualIntentPolicy.CanReapplyExistingElementVisuals(
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        Assert.True(result);
    }

    [Fact]
    [Trait("Spec", "06")]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void CanReapplyExistingElementVisuals_WhenIntendedElementIsMissing_ShouldReturnFalse()
    {
        var existingEquipment = Guid.Parse("22099e8e-9a59-4da4-b4d6-1899131e15bd");
        var missingEquipment = Guid.Parse("0a6135d6-571c-4a75-9448-55318d13bfa4");

        var result = ProjectVisualIntentPolicy.CanReapplyExistingElementVisuals(
            new[] { existingEquipment },
            new[] { existingEquipment, missingEquipment });

        Assert.False(result);
    }
}
