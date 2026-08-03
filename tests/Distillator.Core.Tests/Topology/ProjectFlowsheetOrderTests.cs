using Distillator.Domain.Models;

namespace Distillator.Core.Tests.Topology;

public sealed class ProjectFlowsheetOrderTests
{
    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "09")]
    [Trait("Level", "Unit")]
    public void ReorderFlowsheet_ShouldMoveDiagramWithoutChangingMembership()
    {
        var owner = new User(Guid.Parse("5689b4d9-71e6-4efb-8734-473da7f9a060"), "test@example.com", "Test", "User", false);
        var project = new Project("Order persistence test", owner);
        var pfd1 = project.CreateFlowsheet("PFD 1", "PFD");
        var pfd2 = project.CreateFlowsheet("PFD 2", "PFD");
        var pfd3 = project.CreateFlowsheet("PFD 3", "PFD");

        project.ReorderFlowsheet(pfd3, 0);

        Assert.Equal(new[] { pfd3.Id, pfd1.Id, pfd2.Id }, project.Flowsheets.Select(flowsheet => flowsheet.Id));
    }
}
