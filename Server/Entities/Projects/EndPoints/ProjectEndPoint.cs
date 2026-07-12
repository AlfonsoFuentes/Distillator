using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Databases;
using Server.Entities.Projects;
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
                    .Select(collaborator => collaborator.Project!)
                    .OrderBy(project => project.Name)
                    .Select(project => new ProjectSummaryDto
                    {
                        Id = project.Id,
                        Name = project.Name,
                        OwnerUserId = project.OwnerUserId,
                        Version = project.Version,
                        CreatedOn = project.CreatedOn,
                        UpdatedOnUtc = project.UpdatedOnUtc
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

                var name = NormalizeProjectName(request.Name);

                if (request.ProjectId.HasValue)
                {
                    var existingProject = await LoadProjectByIdAsync(context, request.ProjectId.Value);
                    if (existingProject != null)
                    {
                        if (!existingProject.Collaborators.Any(collaborator => collaborator.UserId == userId))
                        {
                            return Result<ProjectDocumentDto>.Fail("Project already exists or access denied.");
                        }

                        if (!CanEdit(existingProject, userId))
                        {
                            return Result<ProjectDocumentDto>.Fail("You do not have permission to edit this project.");
                        }

                        var oldName = existingProject.Name;
                        var oldVersion = existingProject.Version;

                        existingProject.Name = name;
                        existingProject.UpdatedBy = userId;
                        existingProject.UpdatedOnUtc = DateTime.UtcNow;
                        existingProject.Version++;

                        ApplyConfiguration(existingProject, request.Configuration);
                        await SyncDiagramsAsync(context, existingProject, request.Diagrams, userId);

                        existingProject.ChangeLogs.Add(new ProjectChangeLog
                        {
                            TenantId = existingProject.TenantId,
                            UserId = userId,
                            Operation = ProjectChangeOperation.Updated,
                            EntityType = nameof(ProjectRecord),
                            EntityId = existingProject.Id.ToString(),
                            Path = "Project",
                            OldValueJson = JsonSerializer.Serialize(new { name = oldName, version = oldVersion }),
                            NewValueJson = JsonSerializer.Serialize(new { name = existingProject.Name, version = existingProject.Version }),
                            ProjectVersion = existingProject.Version,
                            CreatedBy = userId
                        });

                        await context.SaveChangesAsync();

                        return Result<ProjectDocumentDto>.Success(ToDocumentDto(existingProject), "Project updated.");
                    }
                }

                var project = new ProjectRecord
                {
                    Id = request.ProjectId ?? Guid.NewGuid(),
                    TenantId = userId,
                    Name = name,
                    OwnerUserId = userId,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    Version = 1
                };

                ApplyConfiguration(project, request.Configuration);
                await SyncDiagramsAsync(context, project, request.Diagrams, userId);

                project.Collaborators.Add(new ProjectCollaborator
                {
                    TenantId = userId,
                    UserId = userId,
                    Role = ProjectCollaboratorRole.Owner,
                    CreatedBy = userId
                });

                project.ChangeLogs.Add(new ProjectChangeLog
                {
                    TenantId = userId,
                    UserId = userId,
                    Operation = ProjectChangeOperation.Created,
                    EntityType = nameof(ProjectRecord),
                    EntityId = project.Id.ToString(),
                    Path = "Project",
                    NewValueJson = "{}",
                    ProjectVersion = project.Version,
                    CreatedBy = userId
                });

                context.Projects.Add(project);
                await context.SaveChangesAsync();

                return Result<ProjectDocumentDto>.Success(ToDocumentDto(project), "Project created.");
            });

            group.MapPost("/UpdateProjectConfigurationRequest", async (
                [FromBody] UpdateProjectConfigurationRequest request,
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

                if (!CanEdit(project, userId))
                {
                    return Result<ProjectDocumentDto>.Fail("You do not have permission to edit this project.");
                }

                var oldName = project.Name;
                var oldVersion = project.Version;

                project.Name = NormalizeProjectName(request.Name);
                project.UpdatedBy = userId;
                project.UpdatedOnUtc = DateTime.UtcNow;
                project.Version++;

                ApplyConfiguration(project, request.Configuration);
                await SyncDiagramsAsync(context, project, request.Diagrams, userId);

                project.ChangeLogs.Add(new ProjectChangeLog
                {
                    TenantId = project.TenantId,
                    UserId = userId,
                    Operation = ProjectChangeOperation.Updated,
                    EntityType = nameof(ProjectRecord),
                    EntityId = project.Id.ToString(),
                    Path = "Project.Configuration",
                    OldValueJson = JsonSerializer.Serialize(new { name = oldName, version = oldVersion }),
                    NewValueJson = JsonSerializer.Serialize(new { name = project.Name, version = project.Version }),
                    ProjectVersion = project.Version,
                    CreatedBy = userId
                });

                await context.SaveChangesAsync();

                return Result<ProjectDocumentDto>.Success(ToDocumentDto(project), "Project updated.");
            });
        }

        private static async Task<ProjectRecord?> LoadProjectForUserAsync(ApplicationDbContext context, Guid projectId, string userId)
        {
            return await context.Projects
                .Include(project => project.Diagrams.OrderBy(diagram => diagram.Order))
                .Include(project => project.Collaborators)
                    .ThenInclude(collaborator => collaborator.User)
                .Include(project => project.ChangeLogs)
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
                .Include(project => project.ChangeLogs)
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

        private static async Task SyncDiagramsAsync(
            ApplicationDbContext context,
            ProjectRecord project,
            IEnumerable<ProjectDiagramDto> diagrams,
            string userId)
        {
            var incomingDiagrams = diagrams.OrderBy(item => item.Order).ToList();
            var incomingIds = incomingDiagrams
                .Where(diagram => diagram.Id != Guid.Empty)
                .Select(diagram => diagram.Id)
                .ToHashSet();

            var existingDiagrams = await context.ProjectDiagrams
                .IgnoreQueryFilters()
                .Where(diagram => diagram.ProjectId == project.Id)
                .ToListAsync();

            var removedDiagrams = existingDiagrams
                .Where(diagram => !diagram.IsDeleted && !incomingIds.Contains(diagram.Id))
                .ToList();

            foreach (var diagram in removedDiagrams)
            {
                diagram.IsDeleted = true;
                diagram.DeletedOnUtc = DateTime.UtcNow;
            }

            foreach (var diagram in incomingDiagrams)
            {
                var diagramId = diagram.Id == Guid.Empty ? Guid.NewGuid() : diagram.Id;
                var record = existingDiagrams.FirstOrDefault(item => item.Id == diagramId);

                if (record == null)
                {
                    record = new ProjectDiagramRecord
                    {
                        Id = diagramId,
                        TenantId = project.TenantId,
                        ProjectId = project.Id,
                        CreatedBy = userId
                    };

                    project.Diagrams.Add(record);
                    existingDiagrams.Add(record);
                }
                else if (!project.Diagrams.Any(item => item.Id == record.Id))
                {
                    project.Diagrams.Add(record);
                }

                record.IsDeleted = false;
                record.DeletedOnUtc = null;
                record.Name = NormalizeText(diagram.Name, "PFD 1");
                record.TypeCode = NormalizeText(diagram.TypeCode, "PFD");
                record.DiagramNumber = string.IsNullOrWhiteSpace(diagram.DiagramNumber) ? null : diagram.DiagramNumber.Trim();
                record.CanvasStateJson = NormalizeJson(diagram.CanvasStateJson);
                record.Order = diagram.Order;
            }
        }

        private static bool CanEdit(ProjectRecord project, string userId)
        {
            return project.Collaborators.Any(collaborator =>
                collaborator.UserId == userId &&
                collaborator.Role is ProjectCollaboratorRole.Owner or ProjectCollaboratorRole.Editor);
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

        private static ProjectSummaryDto ToSummaryDto(ProjectRecord project)
        {
            return new ProjectSummaryDto
            {
                Id = project.Id,
                Name = project.Name,
                OwnerUserId = project.OwnerUserId,
                Version = project.Version,
                CreatedOn = project.CreatedOn,
                UpdatedOnUtc = project.UpdatedOnUtc
            };
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
                    .Select(diagram => new ProjectDiagramDto
                    {
                        Id = diagram.Id,
                        Name = diagram.Name,
                        TypeCode = diagram.TypeCode,
                        DiagramNumber = diagram.DiagramNumber,
                        Order = diagram.Order,
                        CanvasStateJson = diagram.CanvasStateJson
                    })
                    .ToList(),
                Collaborators = project.Collaborators
                    .Select(collaborator => new ProjectCollaboratorDto
                    {
                        UserId = collaborator.UserId,
                        DisplayName = collaborator.User?.FullName ?? string.Empty,
                        Role = collaborator.Role.ToString()
                    })
                    .ToList()
            };
        }
    }
}
