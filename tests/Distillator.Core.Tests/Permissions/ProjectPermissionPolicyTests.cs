using Distillator.Domain.Policies;

namespace Distillator.Core.Tests.Permissions;

public sealed class ProjectPermissionPolicyTests
{
    [Theory]
    [Trait("Spec", "08")]
    [Trait("Level", "Unit")]
    [InlineData(true, null, true)]
    [InlineData(false, "Owner", true)]
    [InlineData(false, "Editor", true)]
    [InlineData(false, "Viewer", false)]
    [InlineData(false, null, false)]
    public void CanEdit_ShouldAllowOnlyOwnerOrEditor(bool isOwner, string? role, bool expected)
    {
        Assert.Equal(expected, ProjectPermissionPolicy.CanEdit(isOwner, role));
    }

    [Theory]
    [Trait("Spec", "08")]
    [Trait("Level", "Unit")]
    [InlineData(true, null, true)]
    [InlineData(false, "Owner", true)]
    [InlineData(false, "Editor", false)]
    [InlineData(false, "Viewer", false)]
    [InlineData(false, null, false)]
    public void CanManage_ShouldAllowOnlyOwner(bool isOwner, string? role, bool expected)
    {
        Assert.Equal(expected, ProjectPermissionPolicy.CanManage(isOwner, role));
    }
}
