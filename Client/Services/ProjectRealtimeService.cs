using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Client.Services.Diagnostics;
using Shared.Projects;

namespace Client.Services
{
    public class ProjectRealtimeService : IAsyncDisposable
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;
        private readonly ProjectActivityLogService _activityLog;
        private HubConnection? _connection;
        private Guid? _joinedProjectId;
        private Guid? _activeDiagramId;
        private string _activeDiagramName = string.Empty;
        private CancellationTokenSource? _closedReconnectCts;
        private DotNetObjectReference<ProjectRealtimeService>? _dotNetReference;
        private bool _browserOnlineListenerRegistered;
        private bool _isRecoveringClosedConnection;

        public event Func<ProjectRealtimeEventDto, Task>? ProjectChangedReceived;
        public event Func<Task>? Reconnected;
        public event Action<IReadOnlyList<ProjectPresenceDto>>? PresenceChangedReceived;
        public event Action? ConnectionStateChanged;
        public IReadOnlyList<ProjectPresenceDto> CurrentPresence { get; private set; } = Array.Empty<ProjectPresenceDto>();

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;
        public bool IsConnecting => _connection?.State is HubConnectionState.Connecting or HubConnectionState.Reconnecting;

        public ProjectRealtimeService(NavigationManager navigationManager, IJSRuntime jsRuntime, ProjectActivityLogService activityLog)
        {
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
            _activityLog = activityLog;
        }

        public async Task JoinProjectAsync(Guid projectId, Guid? activeDiagramId = null, string? activeDiagramName = null)
        {
            await EnsureConnectionAsync();

            if (_connection == null) return;
            _activeDiagramId = activeDiagramId;
            _activeDiagramName = activeDiagramName?.Trim() ?? string.Empty;

            if (_joinedProjectId == projectId && _connection.State == HubConnectionState.Connected)
            {
                _activityLog.Add("SignalR", "Active diagram updated", activeDiagramName);
                await UpdateActiveDiagramAsync(activeDiagramId, activeDiagramName);
                return;
            }

            if (_joinedProjectId.HasValue && _connection.State == HubConnectionState.Connected)
            {
                await _connection.SendAsync("LeaveProject", _joinedProjectId.Value);
            }

            await _connection.SendAsync("JoinProject", projectId, _activeDiagramId, _activeDiagramName);
            _joinedProjectId = projectId;
            _activityLog.Add("SignalR", "Project joined", projectId.ToString());
        }

        public async Task UpdateActiveDiagramAsync(Guid? activeDiagramId, string? activeDiagramName)
        {
            _activeDiagramId = activeDiagramId;
            _activeDiagramName = activeDiagramName?.Trim() ?? string.Empty;

            if (_connection?.State != HubConnectionState.Connected || !_joinedProjectId.HasValue) return;

            await _connection.SendAsync("UpdateActiveDiagram", _joinedProjectId.Value, _activeDiagramId, _activeDiagramName);
            _activityLog.Add("SignalR", "Active diagram sent", _activeDiagramName);
        }

        public async Task LeaveCurrentProjectAsync()
        {
            StopClosedRecovery();

            if (_connection?.State == HubConnectionState.Connected && _joinedProjectId.HasValue)
            {
                await _connection.SendAsync("LeaveProject", _joinedProjectId.Value);
                _activityLog.Add("SignalR", "Project left", _joinedProjectId.Value.ToString());
            }

            _joinedProjectId = null;
            _activeDiagramId = null;
            _activeDiagramName = string.Empty;
            CurrentPresence = Array.Empty<ProjectPresenceDto>();
            PresenceChangedReceived?.Invoke(CurrentPresence);
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
                    _activityLog.Add("SignalR", "Connection reconnecting");
                    ConnectionStateChanged?.Invoke();
                    StartClosedRecovery();
                    return Task.CompletedTask;
                };
                _connection.Reconnected += async _ =>
                {
                    _activityLog.Add("SignalR", "Connection reconnected");
                    ConnectionStateChanged?.Invoke();
                    StopClosedRecovery();
                    if (_joinedProjectId.HasValue)
                    {
                        await _connection.SendAsync("JoinProject", _joinedProjectId.Value, _activeDiagramId, _activeDiagramName);
                    }

                    await NotifyReconnectedAsync();
                };
                _connection.Closed += _ =>
                {
                    _activityLog.Add("SignalR", "Connection closed");
                    ConnectionStateChanged?.Invoke();
                    StartClosedRecovery();
                    return Task.CompletedTask;
                };
            }

            if (_connection.State == HubConnectionState.Disconnected)
            {
                _activityLog.Add("SignalR", "Connecting");
                await _connection.StartAsync();
                _activityLog.Add("SignalR", "Connected");
                ConnectionStateChanged?.Invoke();
            }

            await TryEnsureBrowserOnlineListenerAsync();
        }

        private async Task TryEnsureBrowserOnlineListenerAsync()
        {
            if (_browserOnlineListenerRegistered) return;

            try
            {
                _dotNetReference = DotNetObjectReference.Create(this);
                await _jsRuntime.InvokeVoidAsync("distillatorNetworkStatus.registerOnlineHandler", _dotNetReference);
                _browserOnlineListenerRegistered = true;
            }
            catch
            {
                _dotNetReference?.Dispose();
                _dotNetReference = null;
            }
        }

        [JSInvokable]
        public Task OnBrowserOnlineAsync()
        {
            return RecoverAfterBrowserOnlineAsync();
        }

        private async Task RecoverAfterBrowserOnlineAsync()
        {
            if (!_joinedProjectId.HasValue) return;

            try
            {
                await EnsureConnectionAsync();
                if (_connection?.State != HubConnectionState.Connected) return;

                await _connection.SendAsync("JoinProject", _joinedProjectId.Value, _activeDiagramId, _activeDiagramName);
                await NotifyReconnectedAsync();
            }
            catch
            {
                StartClosedRecovery();
            }
        }

        private void StartClosedRecovery()
        {
            if (!_joinedProjectId.HasValue) return;
            if (_closedReconnectCts is { IsCancellationRequested: false }) return;

            _closedReconnectCts?.Dispose();
            _closedReconnectCts = new CancellationTokenSource();
            _ = RecoverClosedConnectionAsync(_closedReconnectCts.Token);
        }

        private void StopClosedRecovery()
        {
            if (_closedReconnectCts is not { IsCancellationRequested: false }) return;

            _closedReconnectCts.Cancel();
        }

        private async Task RecoverClosedConnectionAsync(CancellationToken cancellationToken)
        {
            if (_isRecoveringClosedConnection) return;

            _isRecoveringClosedConnection = true;
            try
            {
                while (!cancellationToken.IsCancellationRequested && _joinedProjectId.HasValue)
                {
                    try
                    {
                        if (_connection?.State == HubConnectionState.Connected)
                        {
                            await _connection.SendAsync("JoinProject", _joinedProjectId.Value, _activeDiagramId, _activeDiagramName, cancellationToken);
                            await NotifyReconnectedAsync();
                            StopClosedRecovery();
                            return;
                        }

                        if (_connection?.State == HubConnectionState.Disconnected)
                        {
                            await _connection.StartAsync(cancellationToken);
                            ConnectionStateChanged?.Invoke();
                        }

                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
            }
            finally
            {
                _isRecoveringClosedConnection = false;
            }
        }

        private async Task NotifyReconnectedAsync()
        {
            var handlers = Reconnected;
            if (handlers == null) return;

            foreach (Func<Task> handler in handlers.GetInvocationList())
            {
                await handler();
            }
        }

        private async Task OnProjectChangedAsync(ProjectRealtimeEventDto realtimeEvent)
        {
            _activityLog.Add("SignalR", "ProjectChanged event", $"{realtimeEvent.ChangeType} v{realtimeEvent.Version}");
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
            _activityLog.Add("SignalR", "Presence changed", $"{presence.Count} user(s)");
            PresenceChangedReceived?.Invoke(CurrentPresence);
        }

        public async ValueTask DisposeAsync()
        {
            StopClosedRecovery();
            _closedReconnectCts?.Dispose();
            if (_browserOnlineListenerRegistered)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("distillatorNetworkStatus.unregisterOnlineHandler");
                }
                catch
                {
                    // El cierre de la pagina no debe fallar si JS ya no esta disponible.
                }
            }

            _dotNetReference?.Dispose();

            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
