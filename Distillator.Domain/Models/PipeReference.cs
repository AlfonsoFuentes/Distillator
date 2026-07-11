namespace Distillator.Domain.Models;

public class PipeReference : IPipeReference
{
    public Guid Id { get; }
    public Guid SourceElementId { get; }
    public Guid TargetElementId { get; }
    public string SourcePortName { get; }
    public string TargetPortName { get; }

    public PipeReference(Guid sourceElementId, Guid targetElementId, string sourcePortName, string targetPortName)
    {
        Id = Guid.NewGuid();
        SourceElementId = sourceElementId;
        TargetElementId = targetElementId;
        SourcePortName = sourcePortName ?? throw new ArgumentNullException(nameof(sourcePortName));
        TargetPortName = targetPortName ?? throw new ArgumentNullException(nameof(targetPortName));
    }
}
