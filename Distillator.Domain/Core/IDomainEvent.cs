namespace Distillator.Domain.Core;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
