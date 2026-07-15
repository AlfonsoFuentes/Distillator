using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Databases;
using Server.Entities.Projects.Hubs;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Projects;
using Shared.Results;

namespace Server.Entities.Projects.EndPoints
{
    public class ProjectEndPoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/")
                .RequireAuthorization()
                .WithTags("Projects");

            group.MapPost("/GetUserProjectsRequest", async (
                [FromBody] GetUserProjectsRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<List<ProjectSummaryDto>>.Fail("Invalid session.");
                }

                var projects = await context.ProjectCollaborators
                    .AsNoTracking()
                    .Where(collaborator => collaborator.UserId == userId)
                    .Select(collaborator => new { collaborator.Project, collaborator.Role })
                    .OrderByDescending(item => item.Project!.UpdatedOnUtc)
                    .Select(item => new ProjectSummaryDto
                    {
                        Id = item.Project!.Id,
                        Name = item.Project.Name,
                        OwnerUserId = item.Project.OwnerUserId,
                        CurrentUserRole = item.Role.ToString(),
                        Version = item.Project.Version,
                        CreatedOn = item.Project.CreatedOn,
                        UpdatedOnUtc = item.Project.UpdatedOnUtc
                    })
                    .ToListAsync();

                return Result<List<ProjectSummaryDto>>.Success(projects);
            });

            group.MapPost("/GetProjectRequest", async (
                [FromBody] GetProjectRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<ProjectDocumentDto>.Fail("Invalid session.");
                }

                var project = await LoadProjectForUserAsync(context, request.ProjectId, userId);
                if (project == null)
                {
                    return Result<ProjectDocumentDto>.Fail("Project not found or access denied.");
                }

                return Result<ProjectDocumentDto>.Success(ToDocumentDto(project));
            });

            group.MapPost("/GetUserWorkspaceStateRequest", async (
                [FromBody] GetUserWorkspaceStateRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<UserWorkspaceStateDto>.Fail("Invalid session.");
                }

                var state = await context.ProjectUserWorkspaceStates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.UserId == userId);

                return Result<UserWorkspaceStateDto>.Success(ToWorkspaceStateDto(state));
            });

            group.MapPost("/UpdateUserWorkspaceStateRequest", async (
                [FromBody] UpdateUserWorkspaceStateRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<UserWorkspaceStateDto>.Fail("Invalid session.");
                }

                var state = await context.ProjectUserWorkspaceStates
                    .FirstOrDefaultAsync(item => item.UserId == userId);

                if (state == null)
                {
                    state = new ProjectUserWorkspaceState
                    {
                        TenantId = userId,
                        UserId = userId,
                        CreatedBy = userId
                    };

                    context.ProjectUserWorkspaceStates.Add(state);
                }

                ApplyWorkspaceState(state, request.State, userId);

                return await context.SaveResultAsync(
                    () => ToWorkspaceStateDto(state),
                    string.Empty,
                    "Workspace state was not saved.");
            });

            group.MapPost("/GetProjectSharingRequest", async (
                [FromBody] GetProjectSharingRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context,
                UserManager<ApplicationUser> userManager) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<ProjectSharingDto>.Fail("Invalid session.");
                }

                var project = await LoadProjectForUserAsync(context, request.ProjectId, userId);
                if (project == null)
                {
                    return Result<ProjectSharingDto>.Fail("Project not found or access denied.");
                }

                if (!IsOwner(project, userId))
                {
                    return Result<ProjectSharingDto>.Fail("Only the project owner can manage sharing.");
                }

                return Result<ProjectSharingDto>.Success(await ToProjectSharingDtoAsync(project, userManager));
            });

            group.MapPost("/UpdateProjectSharingRequest", async (
                [FromBody] UpdateProjectSharingRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context,
                UserManager<ApplicationUser> userManager,
                IHubContext<ProjectCollaborationHub> hubContext) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<ProjectSharingDto>.Fail("Invalid session.");
                }

                var project = await LoadProjectForUserAsync(context, request.ProjectId, userId);
                if (project == null)
                {
                    return Result<ProjectSharingDto>.Fail("Project not found or access denied.");
                }

                if (!IsOwner(project, userId))
                {
                    return Result<ProjectSharingDto>.Fail("Only the project owner can manage sharing.");
                }

                var allowedUsers = await userManager.Users
                    .Where(item => item.IsActive && item.Id != userId)
                    .Select(item => item.Id)
                    .ToListAsync();
                var allowedUserSet = allowedUsers.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var requested = request.Collaborators
                    .Where(item => !string.IsNullOrWhiteSpace(item.UserId) && allowedUserSet.Contains(item.UserId))
                    .GroupBy(item => item.UserId, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.Last())
                    .ToList();

                var affectedUserIds = requested.Select(item => item.UserId).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var collaborator in project.Collaborators.Where(item => item.Role != ProjectCollaboratorRole.Owner).ToList())
                {
                    if (requested.All(item => !string.Equals(item.UserId, collaborator.UserId, StringComparison.OrdinalIgnoreCase)))
                    {
                        affectedUserIds.Add(collaborator.UserId);
                        context.ProjectCollaborators.Remove(collaborator);
                    }
                }

                foreach (var item in requested)
                {
                    var role = ParseCollaboratorRole(item.Role);
                    if (role == ProjectCollaboratorRole.Owner)
                    {
                        role = ProjectCollaboratorRole.Viewer;
                    }

                    var collaborator = await context.ProjectCollaborators
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(existing =>
                            existing.ProjectId == project.Id &&
                            existing.UserId == item.UserId);

                    if (collaborator == null)
                    {
                        context.ProjectCollaborators.Add(new ProjectCollaborator
                        {
                            TenantId = project.TenantId,
                            ProjectId = project.Id,
                            UserId = item.UserId,
                            Role = role,
                            CreatedBy = userId
                        });
                    }
                    else if (collaborator.Role != ProjectCollaboratorRole.Owner)
                    {
                        collaborator.Role = role;
                        collaborator.IsDeleted = false;
                        collaborator.DeletedOnUtc = null;
                    }
                }

                TouchProject(project, userId);
                AddChangeLog(
                    context,
                    project,
                    userId,
                    ProjectChangeOperation.Updated,
                    nameof(ProjectCollaborator),
                    project.Id,
                    "Project.Collaborators",
                    null,
                    requested.Select(item => new { item.UserId, item.Role }).ToList());

                var affectedRows = await context.SaveChangesAsync();
                var updatedProject = await LoadProjectByIdAsync(context, project.Id) ?? project;
                if (affectedRows <= 0)
                {
                    return Result<ProjectSharingDto>.Fail("Project sharing was not updated.");
                }

                await BroadcastProjectChangedAsync(hubContext, updatedProject, userId, "SharingUpdated", nameof(ProjectCollaborator), project.Id.ToString());
                await BroadcastProjectChangedToUsersAsync(
                    hubContext,
                    updatedProject,
                    userId,
                    affectedUserIds,
                    "SharingUpdated",
                    nameof(ProjectCollaborator),
                    project.Id.ToString());
                return Result<ProjectSharingDto>.Success(await ToProjectSharingDtoAsync(updatedProject, userManager), "Project sharing updated.");
            });

            group.MapPost("/CreateProjectRequest", async (
                [FromBody] CreateProjectRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<ProjectDocumentDto>.Fail("Invalid session.");
                }

                if (request.ProjectId.HasValue)
                {
                    var existingProject = await LoadProjectByIdAsync(context, request.ProjectId.Value);
                    if (existingProject != null)
                    {
                        if (!existingProject.Collaborators.Any(collaborator => collaborator.UserId == userId))
                        {
                            return Result<ProjectDocumentDto>.Fail("Project already exists or access denied.");
                        }

                        return Result<ProjectDocumentDto>.Success(ToDocumentDto(existingProject), "Project already exists.");
                    }
                }

                var project = new ProjectRecord
                {
                    Id = request.ProjectId ?? Guid.NewGuid(),
                    TenantId = userId,
                    Name = NormalizeProjectName(request.Name),
                    OwnerUserId = userId,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    UpdatedOnUtc = DateTime.UtcNow,
                    Version = 1
                };

                ApplyConfiguration(project, request.Configuration);

                project.Collaborators.Add(new ProjectCollaborator
                {
                    TenantId = userId,
                    UserId = userId,
                    Role = ProjectCollaboratorRole.Owner,
                    CreatedBy = userId
                });

                AddChangeLog(
                    context,
                    project,
                    userId,
                    ProjectChangeOperation.Created,
                    nameof(ProjectRecord),
                    project.Id,
                    "Project",
                    null,
                    new { project.Name, project.Version });

                context.Projects.Add(project);
                return await context.SaveResultAsync(
                    () => ToDocumentDto(project),
                    "Project created.",
                    "Project was not created.");
            });

            group.MapPost("/UpdateProjectConfigurationRequest", async (
                [FromBody] UpdateProjectConfigurationRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context,
                IHubContext<ProjectCollaborationHub> hubContext) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<ProjectDocumentDto>.Fail("Invalid session.");
                }

                var project = await LoadProjectForUserAsync(context, request.ProjectId, userId);
                if (project == null)
                {
                    return Result<ProjectDocumentDto>.Fail("Project not found or access denied.");
                }

                if (!CanEdit(project, userId))
                {
                    return Result<ProjectDocumentDto>.Fail("You do not have permission to edit this project.");
                }

                var oldValue = ProjectConfigurationAuditValue(project);

                project.Name = NormalizeProjectName(request.Name);
                ApplyConfiguration(project, request.Configuration);
                TouchProject(project, userId);

                AddChangeLog(
                    context,
                    project,
                    userId,
                    ProjectChangeOperation.Updated,
                    nameof(ProjectRecord),
                    project.Id,
                    "Project.Configuration",
                    oldValue,
                    ProjectConfigurationAuditValue(project));

                var result = await context.SaveResultAsync(
                    () => ToDocumentDto(project),
                    "Project updated.",
                    "Project was not updated.");
                if (result.Succeeded)
                {
                    await BroadcastProjectChangedAsync(hubContext, project, userId, "ProjectConfigurationUpdated", nameof(ProjectRecord), project.Id.ToString());
                }

                return result;
            });

            group.MapPost("/DeleteProjectRequest", async (
                [FromBody] DeleteProjectRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context,
                IHubContext<ProjectCollaborationHub> hubContext) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result.Fail("Invalid session.");
                }

                var project = await LoadProjectForUserAsync(context, request.ProjectId, userId);
                if (project == null)
                {
                    return Result.Fail("Project not found or access denied.");
                }

                if (!IsOwner(project, userId))
                {
                    return Result.Fail("Only the project owner can delete this project.");
                }

                TouchProject(project, userId);
                AddChangeLog(
                    context,
                    project,
                    userId,
                    ProjectChangeOperation.Deleted,
                    nameof(ProjectRecord),
                    project.Id,
                    "Project",
                    new { project.Name },
                    null);

                var projectVersion = project.Version;
                context.Projects.Remove(project);
                var result = await context.SaveResultAsync(
                    "Project deleted.",
                    "Project was not deleted.");
                if (result.Succeeded)
                {
                    await BroadcastProjectChangedAsync(
                        hubContext,
                        project.Id,
                        projectVersion,
                        userId,
                        "ProjectDeleted",
                        nameof(ProjectRecord),
                        project.Id.ToString());
                }

                return result;
            });

            group.MapPost("/CreateDiagramRequest", async (
                [FromBody] CreateDiagramRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context,
                IHubContext<ProjectCollaborationHub> hubContext) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<ProjectDocumentDto>.Fail("Invalid session.");
                }

                var project = await LoadProjectForUserAsync(context, request.ProjectId, userId);
                if (project == null)
                {
                    return Result<ProjectDocumentDto>.Fail("Project not found or access denied.");
                }

                if (!CanEdit(project, userId))
                {
                    return Result<ProjectDocumentDto>.Fail("You do not have permission to edit this project.");
                }

                var diagramId = request.Diagram.Id == Guid.Empty ? Guid.NewGuid() : request.Diagram.Id;
                var existingDiagram = await context.ProjectDiagrams
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(diagram => diagram.ProjectId == project.Id && diagram.Id == diagramId);

                if (existingDiagram is { IsDeleted: false })
                {
                    return Result<ProjectDocumentDto>.Fail("Diagram already exists.");
                }

                if (HasDuplicateDiagramNumber(project, request.Diagram.DiagramNumber, diagramId))
                {
                    return Result<ProjectDocumentDto>.Fail("A diagram with this number already exists.");
                }

                var diagram = existingDiagram ?? new ProjectDiagramRecord
                {
                    Id = diagramId,
                    TenantId = project.TenantId,
                    ProjectId = project.Id,
                    CreatedBy = userId
                };

                ApplyDiagram(diagram, request.Diagram);
                diagram.IsDeleted = false;
                diagram.DeletedOnUtc = null;

                if (existingDiagram == null)
                {
                    context.ProjectDiagrams.Add(diagram);
                }

                TouchProject(project, userId);
                AddChangeLog(
                    context,
                    project,
                    userId,
                    ProjectChangeOperation.Created,
                    nameof(ProjectDiagramRecord),
                    diagram.Id,
                    "Project.Diagrams",
                    null,
                    ToDiagramDto(diagram));

                var result = await context.SaveResultAsync(
                    () => ToDocumentDto(project),
                    "Diagram created.",
                    "Diagram was not created.");
                if (result.Succeeded)
                {
                    await BroadcastProjectChangedAsync(hubContext, project, userId, "DiagramCreated", nameof(ProjectDiagramRecord), diagram.Id.ToString());
                }

                return result;
            });

            group.MapPost("/UpdateDiagramRequest", async (
                [FromBody] UpdateDiagramRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context,
                IHubContext<ProjectCollaborationHub> hubContext) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<ProjectDocumentDto>.Fail("Invalid session.");
                }

                var project = await LoadProjectForUserAsync(context, request.ProjectId, userId);
                if (project == null)
                {
                    return Result<ProjectDocumentDto>.Fail("Project not found or access denied.");
                }

                if (!CanEdit(project, userId))
                {
                    return Result<ProjectDocumentDto>.Fail("You do not have permission to edit this project.");
                }

                var diagram = project.Diagrams.FirstOrDefault(item => item.Id == request.Diagram.Id);
                if (diagram == null)
                {
                    return Result<ProjectDocumentDto>.Fail("Diagram not found.");
                }

                if (HasDuplicateDiagramNumber(project, request.Diagram.DiagramNumber, diagram.Id))
                {
                    return Result<ProjectDocumentDto>.Fail("A diagram with this number already exists.");
                }

                var oldValue = ToDiagramDto(diagram);

                ApplyDiagram(diagram, request.Diagram);
                TouchProject(project, userId);

                AddChangeLog(
                    context,
                    project,
                    userId,
                    ProjectChangeOperation.Updated,
                    nameof(ProjectDiagramRecord),
                    diagram.Id,
                    "Project.Diagrams",
                    oldValue,
                    ToDiagramDto(diagram));

                var result = await context.SaveResultAsync(
                    () => ToDocumentDto(project),
                    "Diagram updated.",
                    "Diagram was not updated.");
                if (result.Succeeded)
                {
                    await BroadcastProjectChangedAsync(hubContext, project, userId, "DiagramUpdated", nameof(ProjectDiagramRecord), diagram.Id.ToString());
                }

                return result;
            });

            group.MapPost("/UpdateDiagramsRequest", async (
                [FromBody] UpdateDiagramsRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context,
                IHubContext<ProjectCollaborationHub> hubContext) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<ProjectDocumentDto>.Fail("Invalid session.");
                }

                if (request.Diagrams.Count == 0 ||
                    request.Diagrams.Any(diagram => diagram.Id == Guid.Empty) ||
                    request.Diagrams.Select(diagram => diagram.Id).Distinct().Count() != request.Diagrams.Count)
                {
                    return Result<ProjectDocumentDto>.Fail("The diagram update batch is invalid.");
                }

                var project = await LoadProjectForUserAsync(context, request.ProjectId, userId);
                if (project == null)
                {
                    return Result<ProjectDocumentDto>.Fail("Project not found or access denied.");
                }

                if (!CanEdit(project, userId))
                {
                    return Result<ProjectDocumentDto>.Fail("You do not have permission to edit this project.");
                }

                var recordsById = project.Diagrams.ToDictionary(diagram => diagram.Id);
                if (request.Diagrams.Any(diagram => !recordsById.ContainsKey(diagram.Id)))
                {
                    return Result<ProjectDocumentDto>.Fail("One or more diagrams were not found.");
                }

                foreach (var diagramDto in request.Diagrams)
                {
                    var diagram = recordsById[diagramDto.Id];
                    if (HasDuplicateDiagramNumber(project, diagramDto.DiagramNumber, diagram.Id))
                    {
                        return Result<ProjectDocumentDto>.Fail("A diagram with this number already exists.");
                    }
                }

                foreach (var diagramDto in request.Diagrams)
                {
                    var diagram = recordsById[diagramDto.Id];
                    var oldValue = ToDiagramDto(diagram);
                    ApplyDiagram(diagram, diagramDto);
                    AddChangeLog(
                        context,
                        project,
                        userId,
                        ProjectChangeOperation.Updated,
                        nameof(ProjectDiagramRecord),
                        diagram.Id,
                        "Project.Diagrams",
                        oldValue,
                        ToDiagramDto(diagram));
                }

                TouchProject(project, userId);
                var result = await context.SaveResultAsync(
                    () => ToDocumentDto(project),
                    "Diagrams updated.",
                    "Diagrams were not updated.");
                if (result.Succeeded)
                {
                    await BroadcastProjectChangedAsync(
                        hubContext,
                        project,
                        userId,
                        "DiagramsUpdated",
                        nameof(ProjectDiagramRecord),
                        project.Id.ToString());
                }

                return result;
            });

            group.MapPost("/DeleteDiagramRequest", async (
                [FromBody] DeleteDiagramRequest request,
                ClaimsPrincipal user,
                ApplicationDbContext context,
                IHubContext<ProjectCollaborationHub> hubContext) =>
            {
                var userId = GetUserId(user);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Result<ProjectDocumentDto>.Fail("Invalid session.");
                }

                var project = await LoadProjectForUserAsync(context, request.ProjectId, userId);
                if (project == null)
                {
                    return Result<ProjectDocumentDto>.Fail("Project not found or access denied.");
                }

                if (!CanEdit(project, userId))
                {
                    return Result<ProjectDocumentDto>.Fail("You do not have permission to edit this project.");
                }

                if (project.Diagrams.Count(diagram => !diagram.IsDeleted) <= 1)
                {
                    return Result<ProjectDocumentDto>.Fail("A project must keep at least one diagram.");
                }

                var diagram = project.Diagrams.FirstOrDefault(item => item.Id == request.DiagramId);
                if (diagram == null)
                {
                    return Result<ProjectDocumentDto>.Fail("Diagram not found.");
                }

                var oldValue = ToDiagramDto(diagram);

                diagram.DiagramNumber = null;
                TouchProject(project, userId);
                AddChangeLog(
                    context,
                    project,
                    userId,
                    ProjectChangeOperation.Deleted,
                    nameof(ProjectDiagramRecord),
                    diagram.Id,
                    "Project.Diagrams",
                    oldValue,
                    null);

                var diagramId = diagram.Id;
                context.ProjectDiagrams.Remove(diagram);
                var result = await context.SaveResultAsync(
                    () => ToDocumentDto(project),
                    "Diagram deleted.",
                    "Diagram was not deleted.");
                if (result.Succeeded)
                {
                    await BroadcastProjectChangedAsync(hubContext, project, userId, "DiagramDeleted", nameof(ProjectDiagramRecord), diagramId.ToString());
                }

                return result;
            });
        }

        private static async Task<ProjectRecord?> LoadProjectForUserAsync(ApplicationDbContext context, Guid projectId, string userId)
        {
            return await context.Projects
                .Include(project => project.Diagrams.OrderBy(diagram => diagram.Order))
                .Include(project => project.Collaborators)
                    .ThenInclude(collaborator => collaborator.User)
                .FirstOrDefaultAsync(project =>
                    project.Id == projectId &&
                    project.Collaborators.Any(collaborator => collaborator.UserId == userId));
        }

        private static async Task<ProjectRecord?> LoadProjectByIdAsync(ApplicationDbContext context, Guid projectId)
        {
            return await context.Projects
                .Include(project => project.Diagrams.OrderBy(diagram => diagram.Order))
                .Include(project => project.Collaborators)
                    .ThenInclude(collaborator => collaborator.User)
                .FirstOrDefaultAsync(project => project.Id == projectId);
        }

        private static void ApplyConfiguration(ProjectRecord project, ProjectBasicConfigurationDto configuration)
        {
            project.ThermodynamicMethodId = configuration.ThermodynamicMethodId;
            project.PlantElevationValue = configuration.PlantElevationValue;
            project.PlantElevationUnit = NormalizeText(configuration.PlantElevationUnit, "Meter");
            project.ActiveUnitSystemName = NormalizeText(configuration.ActiveUnitSystemName, "SI");
            project.UnitSystemsJson = NormalizeJson(configuration.UnitSystemsJson, "[]");
            project.CameraConfigurationJson = NormalizeJson(configuration.CameraConfigurationJson);
            project.NamingConfigurationJson = NormalizeJson(configuration.NamingConfigurationJson);
            project.ReportConfigurationJson = NormalizeJson(configuration.ReportConfigurationJson);
            project.EquipmentDesignConfigurationJson = NormalizeJson(configuration.EquipmentDesignConfigurationJson);
        }

        private static void ApplyDiagram(ProjectDiagramRecord record, ProjectDiagramDto diagram)
        {
            record.Name = NormalizeText(diagram.Name, "PFD 1");
            record.TypeCode = NormalizeText(diagram.TypeCode, "PFD");
            record.DiagramNumber = string.IsNullOrWhiteSpace(diagram.DiagramNumber) ? null : diagram.DiagramNumber.Trim();
            record.CanvasStateJson = NormalizeJson(diagram.CanvasStateJson);
            record.Order = diagram.Order;
        }

        private static bool HasDuplicateDiagramNumber(ProjectRecord project, string? diagramNumber, Guid currentDiagramId)
        {
            if (string.IsNullOrWhiteSpace(diagramNumber))
            {
                return false;
            }

            var normalized = diagramNumber.Trim();
            return project.Diagrams.Any(diagram =>
                !diagram.IsDeleted &&
                diagram.Id != currentDiagramId &&
                string.Equals(diagram.DiagramNumber?.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static void TouchProject(ProjectRecord project, string userId)
        {
            project.UpdatedBy = userId;
            project.UpdatedOnUtc = DateTime.UtcNow;
            project.Version++;
        }

        private static void AddChangeLog(
            ApplicationDbContext context,
            ProjectRecord project,
            string userId,
            ProjectChangeOperation operation,
            string entityType,
            Guid entityId,
            string path,
            object? oldValue,
            object? newValue)
        {
            context.ProjectChangeLogs.Add(new ProjectChangeLog
            {
                TenantId = project.TenantId,
                ProjectId = project.Id,
                UserId = userId,
                Operation = operation,
                EntityType = entityType,
                EntityId = entityId.ToString(),
                Path = path,
                OldValueJson = oldValue == null ? null : JsonSerializer.Serialize(oldValue),
                NewValueJson = newValue == null ? null : JsonSerializer.Serialize(newValue),
                ProjectVersion = project.Version,
                CreatedBy = userId
            });
        }

        private static bool CanEdit(ProjectRecord project, string userId)
        {
            return project.Collaborators.Any(collaborator =>
                collaborator.UserId == userId &&
                collaborator.Role is ProjectCollaboratorRole.Owner or ProjectCollaboratorRole.Editor);
        }

        private static bool IsOwner(ProjectRecord project, string userId)
        {
            return project.Collaborators.Any(collaborator =>
                collaborator.UserId == userId &&
                collaborator.Role == ProjectCollaboratorRole.Owner);
        }

        private static Task BroadcastProjectChangedAsync(
            IHubContext<ProjectCollaborationHub> hubContext,
            ProjectRecord project,
            string userId,
            string changeType,
            string entityType,
            string entityId)
        {
            return BroadcastProjectChangedAsync(
                hubContext,
                project.Id,
                project.Version,
                userId,
                changeType,
                entityType,
                entityId);
        }

        private static Task BroadcastProjectChangedAsync(
            IHubContext<ProjectCollaborationHub> hubContext,
            Guid projectId,
            long version,
            string userId,
            string changeType,
            string entityType,
            string entityId)
        {
            var realtimeEvent = new ProjectRealtimeEventDto
            {
                ProjectId = projectId,
                Version = version,
                ChangeType = changeType,
                EntityType = entityType,
                EntityId = entityId,
                ChangedByUserId = userId,
                OccurredOnUtc = DateTime.UtcNow
            };

            return hubContext.Clients
                .Group(ProjectCollaborationHub.ProjectGroup(projectId))
                .SendAsync("ProjectChanged", realtimeEvent);
        }

        private static Task BroadcastProjectChangedToUsersAsync(
            IHubContext<ProjectCollaborationHub> hubContext,
            ProjectRecord project,
            string userId,
            IEnumerable<string> targetUserIds,
            string changeType,
            string entityType,
            string entityId)
        {
            var users = targetUserIds
                .Where(targetUserId => !string.IsNullOrWhiteSpace(targetUserId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (users.Count == 0)
            {
                return Task.CompletedTask;
            }

            var realtimeEvent = new ProjectRealtimeEventDto
            {
                ProjectId = project.Id,
                Version = project.Version,
                ChangeType = changeType,
                EntityType = entityType,
                EntityId = entityId,
                ChangedByUserId = userId,
                OccurredOnUtc = DateTime.UtcNow
            };

            return hubContext.Clients
                .Users(users)
                .SendAsync("ProjectChanged", realtimeEvent);
        }

        private static ProjectCollaboratorRole ParseCollaboratorRole(string role)
        {
            return Enum.TryParse<ProjectCollaboratorRole>(role, true, out var parsed)
                ? parsed
                : ProjectCollaboratorRole.Viewer;
        }

        private static async Task<ProjectSharingDto> ToProjectSharingDtoAsync(
            ProjectRecord project,
            UserManager<ApplicationUser> userManager)
        {
            var users = await userManager.Users
                .Where(user => user.IsActive && user.Id != project.OwnerUserId)
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .Select(user => new ProjectShareUserDto
                {
                    UserId = user.Id,
                    DisplayName = user.FullName,
                    Email = user.Email ?? string.Empty
                })
                .ToListAsync();

            return new ProjectSharingDto
            {
                ProjectId = project.Id,
                OwnerUserId = project.OwnerUserId,
                AvailableUsers = users,
                Collaborators = project.Collaborators
                    .Where(collaborator => collaborator.Role != ProjectCollaboratorRole.Owner)
                    .OrderBy(collaborator => collaborator.User!.FirstName)
                    .ThenBy(collaborator => collaborator.User!.LastName)
                    .Select(ToCollaboratorDto)
                    .ToList()
            };
        }

        private static string? GetUserId(ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private static string NormalizeProjectName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "Untitled Project" : name.Trim();
        }

        private static string NormalizeText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string NormalizeJson(string value, string fallback = "{}")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static object ProjectConfigurationAuditValue(ProjectRecord project)
        {
            return new
            {
                project.Name,
                project.ThermodynamicMethodId,
                project.PlantElevationValue,
                project.PlantElevationUnit,
                project.ActiveUnitSystemName,
                project.UnitSystemsJson,
                project.CameraConfigurationJson,
                project.NamingConfigurationJson,
                project.ReportConfigurationJson,
                project.EquipmentDesignConfigurationJson,
                project.Version
            };
        }

        private static void ApplyWorkspaceState(ProjectUserWorkspaceState state, UserWorkspaceStateDto dto, string userId)
        {
            state.LastProjectId = dto.LastProjectId;
            state.LastFlowsheetId = dto.LastFlowsheetId;
            state.IsProjectExplorerCollapsed = dto.IsProjectExplorerCollapsed;
            state.IsDiagramExplorerCollapsed = dto.IsDiagramExplorerCollapsed;
            state.ExpandedDiagramTypeCodesJson = dto.ExpandedDiagramTypeCodes == null
                ? null
                : JsonSerializer.Serialize(NormalizeDiagramTypeCodes(dto.ExpandedDiagramTypeCodes));
            state.UpdatedOnUtc = DateTime.UtcNow;
            state.CreatedBy ??= userId;
        }

        private static UserWorkspaceStateDto ToWorkspaceStateDto(ProjectUserWorkspaceState? state)
        {
            if (state == null)
            {
                return new UserWorkspaceStateDto();
            }

            return new UserWorkspaceStateDto
            {
                LastProjectId = state.LastProjectId,
                LastFlowsheetId = state.LastFlowsheetId,
                IsProjectExplorerCollapsed = state.IsProjectExplorerCollapsed,
                IsDiagramExplorerCollapsed = state.IsDiagramExplorerCollapsed,
                ExpandedDiagramTypeCodes = string.IsNullOrWhiteSpace(state.ExpandedDiagramTypeCodesJson)
                    ? null
                    : NormalizeDiagramTypeCodes(JsonSerializer.Deserialize<List<string>>(state.ExpandedDiagramTypeCodesJson) ?? new List<string>()),
                LastAccessAt = state.UpdatedOnUtc
            };
        }

        private static List<string> NormalizeDiagramTypeCodes(IEnumerable<string> typeCodes)
        {
            return typeCodes
                .Where(typeCode => !string.IsNullOrWhiteSpace(typeCode))
                .Select(typeCode => typeCode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(typeCode => typeCode, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static ProjectDocumentDto ToDocumentDto(ProjectRecord project)
        {
            return new ProjectDocumentDto
            {
                Id = project.Id,
                Name = project.Name,
                OwnerUserId = project.OwnerUserId,
                Version = project.Version,
                CreatedOn = project.CreatedOn,
                UpdatedOnUtc = project.UpdatedOnUtc,
                Configuration = new ProjectBasicConfigurationDto
                {
                    ThermodynamicMethodId = project.ThermodynamicMethodId,
                    PlantElevationValue = project.PlantElevationValue,
                    PlantElevationUnit = project.PlantElevationUnit,
                    ActiveUnitSystemName = project.ActiveUnitSystemName,
                    UnitSystemsJson = project.UnitSystemsJson,
                    CameraConfigurationJson = project.CameraConfigurationJson,
                    NamingConfigurationJson = project.NamingConfigurationJson,
                    ReportConfigurationJson = project.ReportConfigurationJson,
                    EquipmentDesignConfigurationJson = project.EquipmentDesignConfigurationJson
                },
                Diagrams = project.Diagrams
                    .Where(diagram => !diagram.IsDeleted)
                    .OrderBy(diagram => diagram.Order)
                    .Select(ToDiagramDto)
                    .ToList(),
                Collaborators = project.Collaborators
                    .Select(ToCollaboratorDto)
                    .ToList()
            };
        }

        private static ProjectCollaboratorDto ToCollaboratorDto(ProjectCollaborator collaborator)
        {
            return new ProjectCollaboratorDto
            {
                UserId = collaborator.UserId,
                DisplayName = collaborator.User?.FullName ?? string.Empty,
                Email = collaborator.User?.Email ?? string.Empty,
                Role = collaborator.Role.ToString()
            };
        }

        private static ProjectDiagramDto ToDiagramDto(ProjectDiagramRecord diagram)
        {
            return new ProjectDiagramDto
            {
                Id = diagram.Id,
                Name = diagram.Name,
                TypeCode = diagram.TypeCode,
                DiagramNumber = diagram.DiagramNumber,
                Order = diagram.Order,
                CanvasStateJson = diagram.CanvasStateJson
            };
        }
    }
}
