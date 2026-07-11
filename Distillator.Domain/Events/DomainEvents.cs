using Distillator.Domain.Core;

namespace Distillator.Domain.Events;

public abstract class DomainEventBase : IDomainEvent
{
    public DateTime OccurredAt { get; }

    protected DomainEventBase()
    {
        OccurredAt = DateTime.UtcNow;
    }
}

public class ProjectCreatedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public string ProjectName { get; }

    public ProjectCreatedEvent(Guid projectId, string projectName)
    {
        ProjectId = projectId;
        ProjectName = projectName;
    }
}

public class FlowsheetAddedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public Guid FlowsheetId { get; }
    public string FlowsheetName { get; }
    public string FlowsheetTypeCode { get; }

    public FlowsheetAddedEvent(Guid projectId, Guid flowsheetId, string flowsheetName, string flowsheetTypeCode)
    {
        ProjectId = projectId;
        FlowsheetId = flowsheetId;
        FlowsheetName = flowsheetName;
        FlowsheetTypeCode = flowsheetTypeCode;
    }
}

public class FlowsheetRemovedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public Guid FlowsheetId { get; }

    public FlowsheetRemovedEvent(Guid projectId, Guid flowsheetId)
    {
        ProjectId = projectId;
        FlowsheetId = flowsheetId;
    }
}

public class EquipmentAddedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public Guid FlowsheetId { get; }
    public Guid ElementId { get; }
    public string EquipmentTypeCode { get; }

    public EquipmentAddedEvent(Guid projectId, Guid flowsheetId, Guid elementId, string equipmentTypeCode)
    {
        ProjectId = projectId;
        FlowsheetId = flowsheetId;
        ElementId = elementId;
        EquipmentTypeCode = equipmentTypeCode;
    }
}

public class EquipmentRemovedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public Guid FlowsheetId { get; }
    public Guid ElementId { get; }

    public EquipmentRemovedEvent(Guid projectId, Guid flowsheetId, Guid elementId)
    {
        ProjectId = projectId;
        FlowsheetId = flowsheetId;
        ElementId = elementId;
    }
}

public class EquipmentMovedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public Guid FlowsheetId { get; }
    public Guid ElementId { get; }
    public double OldX { get; }
    public double OldY { get; }
    public double NewX { get; }
    public double NewY { get; }

    public EquipmentMovedEvent(Guid projectId, Guid flowsheetId, Guid elementId, double oldX, double oldY, double newX, double newY)
    {
        ProjectId = projectId;
        FlowsheetId = flowsheetId;
        ElementId = elementId;
        OldX = oldX;
        OldY = oldY;
        NewX = newX;
        NewY = newY;
    }
}

public class ConnectionCreatedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public Guid FlowsheetId { get; }
    public Guid PipeId { get; }

    public ConnectionCreatedEvent(Guid projectId, Guid flowsheetId, Guid pipeId)
    {
        ProjectId = projectId;
        FlowsheetId = flowsheetId;
        PipeId = pipeId;
    }
}

public class ConnectionRemovedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public Guid FlowsheetId { get; }
    public Guid PipeId { get; }

    public ConnectionRemovedEvent(Guid projectId, Guid flowsheetId, Guid pipeId)
    {
        ProjectId = projectId;
        FlowsheetId = flowsheetId;
        PipeId = pipeId;
    }
}

public class SimulationStartedEvent : DomainEventBase
{
    public Guid ProjectId { get; }

    public SimulationStartedEvent(Guid projectId)
    {
        ProjectId = projectId;
    }
}

public class SimulationCompletedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public TimeSpan ExecutionTime { get; }
    public bool Converged { get; }

    public SimulationCompletedEvent(Guid projectId, TimeSpan executionTime, bool converged)
    {
        ProjectId = projectId;
        ExecutionTime = executionTime;
        Converged = converged;
    }
}

public class SimulationFailedEvent : DomainEventBase
{
    public Guid ProjectId { get; }
    public string ErrorMessage { get; }

    public SimulationFailedEvent(Guid projectId, string errorMessage)
    {
        ProjectId = projectId;
        ErrorMessage = errorMessage;
    }
}

public class ProjectConfigurationUpdatedEvent : DomainEventBase
{
    public Guid ProjectId { get; }

    public ProjectConfigurationUpdatedEvent(Guid projectId)
    {
        ProjectId = projectId;
    }
}
