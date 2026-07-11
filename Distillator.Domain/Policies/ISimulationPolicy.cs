using Distillator.Domain.Core;

namespace Distillator.Domain.Policies;

/// <summary>
/// Política que decide si debe ejecutarse el solver ante un evento de dominio.
/// </summary>
public interface ISimulationPolicy
{
    bool ShouldRunSimulation(IDomainEvent domainEvent);
}

public class SimulationPolicy : ISimulationPolicy
{
    public bool ShouldRunSimulation(IDomainEvent domainEvent)
    {
        return domainEvent is Events.ConnectionCreatedEvent
            || domainEvent is Events.ConnectionRemovedEvent
            || domainEvent is Events.EquipmentRemovedEvent;
    }
}
