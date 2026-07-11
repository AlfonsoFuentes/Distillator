namespace Distillator.Domain.Session;

/// <summary>
/// Estado de sesión del usuario. Permite recordar el último proyecto y flowsheet activos.
/// Diseñado para ser persistido en una etapa posterior.
/// </summary>
public interface IUserSessionState
{
    Guid UserId { get; }
    Guid? LastProjectId { get; set; }
    Guid? LastFlowsheetId { get; set; }
    DateTime LastAccessAt { get; set; }
}

public class UserSessionState : IUserSessionState
{
    public Guid UserId { get; }
    public Guid? LastProjectId { get; set; }
    public Guid? LastFlowsheetId { get; set; }
    public DateTime LastAccessAt { get; set; }

    public UserSessionState(Guid userId)
    {
        UserId = userId;
        LastAccessAt = DateTime.UtcNow;
    }
}
