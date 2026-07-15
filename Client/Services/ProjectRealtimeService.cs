using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.Projects;

namespace Client.Services
{
    public class ProjectRealtimeService : IAsyncDisposable
    {
        private readonly NavigationManager _navigationManager;
        private HubConnection? _connection;
        private Guid? _joinedProjectId;
        private Guid? _activeDiagramId;
        private string _activeDiagramName = string.Empty;

        public event Func<ProjectRealtimeEventDto, Task>? ProjectChangedReceived;
        public event Action<IReadOnlyList<ProjectPresenceDto>>? PresenceChangedReceived;
        public event Action? ConnectionStateChanged;
        public IReadOnlyList<ProjectPresenceDto> CurrentPresence { get; private set; } = Array.Empty<ProjectPresenceDto>();

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;
        public bool IsConnecting => _connection?.State is HubConnectionState.Connecting or HubConnectionState.Reconnecting;

        public ProjectRealtimeService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public async Task JoinProjectAsync(Guid projectId, Guid? activeDiagramId = null, string? activeDiagramName = null)
        {
            await EnsureConnectionAsync();

            if (_connection == null) return;
            _activeDiagramId = activeDiagramId;
            _activeDiagramName = activeDiagramName?.Trim() ?? string.Empty;

            if (_joinedProjectId == projectId && _connection.State == HubConnectionState.Connected)
            {
                await UpdateActiveDiagramAsync(activeDiagramId, activeDiagramName);
                return;
            }

            if (_joinedProjectId.HasValue && _connection.State == HubConnectionState.Connected)
            {
                await _connection.SendAsync("LeaveProject", _joinedProjectId.Value);
            }

            await _connection.SendAsync("JoinProject", projectId, _activeDiagramId, _activeDiagramName);
            _joinedProjectId = projectId;
        }

        public async Task UpdateActiveDiagramAsync(Guid? activeDiagramId, string? activeDiagramName)
        {
            _activeDiagramId = activeDiagramId;
            _activeDiagramName = activeDiagramName?.Trim() ?? string.Empty;

            if (_connection?.State != HubConnectionState.Connected || !_joinedProjectId.HasValue) return;

            await _connection.SendAsync("UpdateActiveDiagram", _joinedProjectId.Value, _activeDiagramId, _activeDiagramName);
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection == null)
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(_navigationManager.ToAbsoluteUri("/projectCollaborationHub"))
                    .WithAutomaticReconnect()
                    .Build();

                _connection.On<ProjectRealtimeEventDto>("ProjectChanged", OnProjectChangedAsync);
                _connection.On<Guid, List<ProjectPresenceDto>>("ProjectPresenceChanged", OnProjectPresenceChanged);
                _connection.Reconnecting += _ =>
                {
                    ConnectionStateChanged?.Invoke();
                    return Task.CompletedTask;
                };
                _connection.Reconnected += async _ =>
                {
                    ConnectionStateChanged?.Invoke();
                    if (_joinedProjectId.HasValue)
                    {
                        await _connection.SendAsync("JoinProject", _joinedProjectId.Value, _activeDiagramId, _activeDiagramName);
                    }
                };
                _connection.Closed += _ =>
                {
                    ConnectionStateChanged?.Invoke();
                    return Task.CompletedTask;
                };
            }

            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync();
                ConnectionStateChanged?.Invoke();
            }
        }

        private async Task OnProjectChangedAsync(ProjectRealtimeEventDto realtimeEvent)
        {
            var handlers = ProjectChangedReceived;
            if (handlers == null) return;

            foreach (Func<ProjectRealtimeEventDto, Task> handler in handlers.GetInvocationList())
            {
                await handler(realtimeEvent);
            }
        }

        private void OnProjectPresenceChanged(Guid projectId, List<ProjectPresenceDto> presence)
        {
            if (_joinedProjectId != projectId) return;

            CurrentPresence = presence;
            PresenceChangedReceived?.Invoke(CurrentPresence);
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
