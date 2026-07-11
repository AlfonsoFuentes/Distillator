namespace Distillator.Domain.Repositories.InMemory;

public class InMemoryUserSessionStateRepository : IUserSessionStateRepository
{
    private readonly Dictionary<Guid, Session.IUserSessionState> _sessions = new();

    public Task<Session.IUserSessionState?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(userId, out var session);
        return Task.FromResult(session);
    }

    public Task SaveAsync(Session.IUserSessionState sessionState, CancellationToken cancellationToken = default)
    {
        _sessions[sessionState.UserId] = sessionState;
        return Task.CompletedTask;
    }
}
