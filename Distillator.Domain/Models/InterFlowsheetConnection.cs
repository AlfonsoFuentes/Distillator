namespace Distillator.Domain.Models;

public class InterFlowsheetConnection : IInterFlowsheetConnection
{
    public Guid Id { get; }
    public Guid SourceFlowsheetId { get; }
    public Guid TargetFlowsheetId { get; }
    public Guid SourceConnectorId { get; }
    public Guid TargetConnectorId { get; }

    public InterFlowsheetConnection(
        Guid sourceFlowsheetId,
        Guid targetFlowsheetId,
        Guid sourceConnectorId,
        Guid targetConnectorId)
    {
        Id = Guid.NewGuid();
        SourceFlowsheetId = sourceFlowsheetId;
        TargetFlowsheetId = targetFlowsheetId;
        SourceConnectorId = sourceConnectorId;
        TargetConnectorId = targetConnectorId;
    }
}
