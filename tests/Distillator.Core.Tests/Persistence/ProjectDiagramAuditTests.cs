using Shared.Projects;

namespace Distillator.Core.Tests.Persistence;

public sealed class ProjectDiagramAuditTests
{
    [Fact]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    public void Summarize_ShouldKeepDiagramAuditLightweight()
    {
        var diagram = new ProjectDiagramDto
        {
            Id = Guid.Parse("d6c46599-b937-45bb-9dc5-2523e26c55b5"),
            Name = "Main",
            TypeCode = "PFD",
            DiagramNumber = "10",
            Order = 2,
            CanvasStateJson = """
            {
              "camera": { "zoom": 1 },
              "elements": [
                { "id": "6fda50c5-13f7-462b-9d89-2b100ca76d26" },
                { "id": "60c5e1fb-7d84-4183-b527-820537422a73" }
              ],
              "pipes": [
                { "id": "c61b8701-dd7e-42f2-b84e-3c14e25bc77d" }
              ]
            }
            """
        };

        var summary = ProjectDiagramAudit.Summarize(diagram);

        Assert.Equal(diagram.Id, summary.Id);
        Assert.Equal("Main", summary.Name);
        Assert.Equal("PFD", summary.TypeCode);
        Assert.Equal("10", summary.DiagramNumber);
        Assert.Equal(2, summary.Order);
        Assert.Equal(2, summary.ElementCount);
        Assert.Equal(1, summary.PipeCount);
        Assert.Equal(diagram.CanvasStateJson.Length, summary.CanvasJsonLength);
    }

    [Fact]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    public void Summarize_WhenCanvasJsonIsInvalid_ShouldStillReturnMetadata()
    {
        var diagram = new ProjectDiagramDto
        {
            Id = Guid.Parse("f6ca5cc7-7c10-4840-b850-23f86c1e7c6a"),
            Name = "Broken",
            TypeCode = "PFD",
            CanvasStateJson = "{invalid"
        };

        var summary = ProjectDiagramAudit.Summarize(diagram);

        Assert.Equal("Broken", summary.Name);
        Assert.Equal(0, summary.ElementCount);
        Assert.Equal(0, summary.PipeCount);
        Assert.Equal(8, summary.CanvasJsonLength);
    }
}
