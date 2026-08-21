using Distillator.Domain.Models;

namespace Distillator.Domain.Session;

public sealed record ProjectWorkspaceSelection(Project? Project, IFlowsheet? Flowsheet)
{
    public bool HasSelection => Project != null;
}

public sealed class ProjectWorkspaceSelectionService
{
    public ProjectWorkspaceSelection SelectInitialProject(
        IReadOnlyList<Project> projects,
        IUserSessionState? session)
    {
        ArgumentNullException.ThrowIfNull(projects);

        if (projects.Count == 0)
        {
            return new ProjectWorkspaceSelection(null, null);
        }

        var project = session?.LastProjectId is { } lastProjectId
            ? projects.FirstOrDefault(candidate => candidate.Id == lastProjectId)
            : null;
        project ??= projects[0];

        var preferredFlowsheetId = ResolvePreferredFlowsheetId(project.Id, session);
        return SelectProject(project, preferredFlowsheetId);
    }

    public ProjectWorkspaceSelection SelectProject(Project project, Guid? preferredFlowsheetId = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var flowsheet = preferredFlowsheetId.HasValue
            ? project.GetFlowsheet(preferredFlowsheetId.Value)
            : null;
        flowsheet ??= project.Flowsheets.FirstOrDefault();

        return new ProjectWorkspaceSelection(project, flowsheet);
    }

    public Guid? ResolvePreferredFlowsheetId(Guid projectId, IUserSessionState? session)
    {
        if (session?.LastFlowsheetIdsByProject.TryGetValue(projectId, out var flowsheetId) == true)
        {
            return flowsheetId;
        }

        return session?.LastProjectId == projectId ? session.LastFlowsheetId : null;
    }
}
