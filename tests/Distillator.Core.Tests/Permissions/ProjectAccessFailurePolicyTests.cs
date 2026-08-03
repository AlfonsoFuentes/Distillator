using Distillator.Domain.Policies;

namespace Distillator.Core.Tests.Permissions;

public sealed class ProjectAccessFailurePolicyTests
{
    [Theory]
    [Trait("Spec", "08")]
    [Trait("Level", "Unit")]
    [InlineData("Project not found or access denied.", true)]
    [InlineData("Only the project owner can manage sharing.", false)]
    [InlineData("Connection error: TypeError: Failed to fetch", false)]
    [InlineData("Request timed out. Please try again.", false)]
    public void IsAccessDenied_ShouldIdentifyPermissionLoss(string message, bool expected)
    {
        Assert.Equal(expected, ProjectAccessFailurePolicy.IsAccessDenied(new[] { message }));
    }
}
