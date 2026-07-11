namespace Distillator.Domain.Repositories;

public interface IUserSessionStateRepository
{
    Task<Session.IUserSessionState?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveAsync(Session.IUserSessionState sessionState, CancellationToken cancellationToken = default);
}
