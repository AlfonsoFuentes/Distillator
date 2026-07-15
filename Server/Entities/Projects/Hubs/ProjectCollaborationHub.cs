using System.Security.Claims;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Databases;
using Shared.Projects;

namespace Server.Entities.Projects.Hubs
{
    [Authorize]
    public class ProjectCollaborationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, PresenceConnection> PresenceByConnection = new();
        private readonly ApplicationDbContext _context;

        public ProjectCollaborationHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinProject(Guid projectId, Guid? activeDiagramId = null, string? activeDiagramName = null)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException("Invalid session.");
            }

            var hasAccess = await _context.ProjectCollaborators
                .AsNoTracking()
                .AnyAsync(collaborator => collaborator.ProjectId == projectId && collaborator.UserId == userId);

            if (!hasAccess)
            {
                throw new HubException("Project access denied.");
            }

            if (PresenceByConnection.TryGetValue(Context.ConnectionId, out var previousPresence) &&
                previousPresence.ProjectId != projectId)
            {
                PresenceByConnection.TryRemove(Context.ConnectionId, out _);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(previousPresence.ProjectId));
                await BroadcastPresenceAsync(previousPresence.ProjectId);
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(applicationUser => applicationUser.Id == userId)
                .Select(applicationUser => new
                {
                    applicationUser.FirstName,
                    applicationUser.LastName,
                    applicationUser.Email
                })
                .FirstOrDefaultAsync();

            var displayName = string.Join(' ', new[] { user?.FirstName, user?.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
                .Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = user?.Email ?? "User";
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
            PresenceByConnection[Context.ConnectionId] = new PresenceConnection(
                projectId,
                userId,
                displayName,
                activeDiagramId,
                activeDiagramName?.Trim() ?? string.Empty,
                DateTime.UtcNow);

            await BroadcastPresenceAsync(projectId);
        }

        public async Task LeaveProject(Guid projectId)
        {
            PresenceByConnection.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
            await BroadcastPresenceAsync(projectId);
        }

        public async Task UpdateActiveDiagram(Guid projectId, Guid? activeDiagramId, string? activeDiagramName)
        {
            if (!PresenceByConnection.TryGetValue(Context.ConnectionId, out var presence) ||
                presence.ProjectId != projectId)
            {
                return;
            }

            PresenceByConnection[Context.ConnectionId] = presence with
            {
                ActiveDiagramId = activeDiagramId,
                ActiveDiagramName = activeDiagramName?.Trim() ?? string.Empty
            };

            await BroadcastPresenceAsync(projectId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (PresenceByConnection.TryRemove(Context.ConnectionId, out var presence))
            {
                await BroadcastPresenceAsync(presence.ProjectId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static string ProjectGroup(Guid projectId) => $"project:{projectId}";

        private Task BroadcastPresenceAsync(Guid projectId)
        {
            var presence = PresenceByConnection.Values
                .Where(connection => connection.ProjectId == projectId)
                .GroupBy(connection => connection.UserId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(connection => connection.ConnectedAtUtc).First())
                .OrderBy(connection => connection.DisplayName)
                .Select(connection => new ProjectPresenceDto
                {
                    ProjectId = connection.ProjectId,
                    UserId = connection.UserId,
                    DisplayName = connection.DisplayName,
                    ActiveDiagramId = connection.ActiveDiagramId,
                    ActiveDiagramName = connection.ActiveDiagramName,
                    ConnectedAtUtc = connection.ConnectedAtUtc
                })
                .ToList();

            return Clients
                .Group(ProjectGroup(projectId))
                .SendAsync("ProjectPresenceChanged", projectId, presence);
        }

        private sealed record PresenceConnection(
            Guid ProjectId,
            string UserId,
            string DisplayName,
            Guid? ActiveDiagramId,
            string ActiveDiagramName,
            DateTime ConnectedAtUtc);
    }
}
