using Distillator.Domain.Models;
using Distillator.Domain.Session;

namespace Distillator.Core.Tests.Session;

public sealed class ProjectWorkspaceSelectionServiceTests
{
    [Fact]
    [Trait("Spec", "08")]
    [Trait("Level", "Unit")]
    public void SelectInitialProject_WhenProjectListIsEmpty_ShouldReturnNoSelection()
    {
        var selection = new ProjectWorkspaceSelectionService()
            .SelectInitialProject(Array.Empty<Project>(), null);

        Assert.False(selection.HasSelection);
        Assert.Null(selection.Project);
        Assert.Null(selection.Flowsheet);
    }

    [Fact]
    [Trait("Spec", "08")]
    [Trait("Level", "Unit")]
    public void SelectInitialProject_WhenLastProjectIsAvailable_ShouldSelectItAndItsLastFlowsheet()
    {
        var first = CreateProject("First");
        first.CreateFlowsheet("PFD 1", "PFD");
        var second = CreateProject("Second");
        var secondFirstFlowsheet = second.CreateFlowsheet("PFD 1", "PFD");
        var secondLastFlowsheet = second.CreateFlowsheet("PFD 2", "PFD");
        var session = new UserSessionState(Guid.NewGuid())
        {
            LastProjectId = second.Id,
            LastFlowsheetId = secondLastFlowsheet.Id
        };

        var selection = new ProjectWorkspaceSelectionService()
            .SelectInitialProject(new[] { first, second }, session);

        Assert.Same(second, selection.Project);
        Assert.Same(secondLastFlowsheet, selection.Flowsheet);
        Assert.NotSame(secondFirstFlowsheet, selection.Flowsheet);
    }

    [Fact]
    [Trait("Spec", "08")]
    [Trait("Level", "Unit")]
    public void SelectInitialProject_WhenLastProjectIsUnavailable_ShouldUseFirstProjectAndIgnoreForeignFlowsheet()
    {
        var first = CreateProject("First");
        var firstFlowsheet = first.CreateFlowsheet("PFD 1", "PFD");
        var unavailableProjectId = Guid.NewGuid();
        var foreignFlowsheetId = Guid.NewGuid();
        var session = new UserSessionState(Guid.NewGuid())
        {
            LastProjectId = unavailableProjectId,
            LastFlowsheetId = foreignFlowsheetId
        };

        var selection = new ProjectWorkspaceSelectionService()
            .SelectInitialProject(new[] { first }, session);

        Assert.Same(first, selection.Project);
        Assert.Same(firstFlowsheet, selection.Flowsheet);
    }

    [Fact]
    [Trait("Spec", "08")]
    [Trait("Level", "Unit")]
    public void SelectProject_WhenPreferredFlowsheetBelongsToProject_ShouldSelectIt()
    {
        var project = CreateProject("Project");
        project.CreateFlowsheet("PFD 1", "PFD");
        var preferred = project.CreateFlowsheet("PFD 2", "PFD");

        var selection = new ProjectWorkspaceSelectionService()
            .SelectProject(project, preferred.Id);

        Assert.Same(project, selection.Project);
        Assert.Same(preferred, selection.Flowsheet);
    }

    private static Project CreateProject(string name)
    {
        var owner = new User(Guid.NewGuid(), "test@example.com", "Test", "User", false);
        return new Project(name, owner);
    }
}
