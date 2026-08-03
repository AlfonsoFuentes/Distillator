using Distillator.Domain.Models;
using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Services;

public sealed class FlowsheetConnectionEditService
{
    public IPipeReference? TryConnect(
        IFlowsheet flowsheet,
        IConnectionService connectionService,
        IVisualElement source,
        string sourcePortName,
        IVisualElement? target,
        string? targetPortName,
        double dropX,
        double dropY)
    {
        ArgumentNullException.ThrowIfNull(flowsheet);
        ArgumentNullException.ThrowIfNull(connectionService);
        ArgumentNullException.ThrowIfNull(source);

        return connectionService.Connect(
            flowsheet,
            source,
            sourcePortName,
            target,
            targetPortName,
            dropX,
            dropY);
    }

    public bool TryDisconnectPort(Project project, IFlowsheet flowsheet, IVisualElement equipment, string portName)
    {
        return TryDisconnectPort(project, flowsheet, equipment, portName, out _);
    }

    public bool TryDisconnectPort(
        Project project,
        IFlowsheet flowsheet,
        IVisualElement equipment,
        string portName,
        out IReadOnlyCollection<IFlowsheet> affectedFlowsheets)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(flowsheet);
        ArgumentNullException.ThrowIfNull(equipment);

        affectedFlowsheets = Array.Empty<IFlowsheet>();
        var pipe = flowsheet.Pipes.FirstOrDefault(candidate =>
            (candidate.SourceElementId == equipment.Id && candidate.SourcePortName == portName) ||
            (candidate.TargetElementId == equipment.Id && candidate.TargetPortName == portName));

        if (pipe == null)
        {
            return false;
        }

        affectedFlowsheets = GetAffectedFlowsheets(project, flowsheet, pipe);

        project.SimulationService.DisconnectPort(project, flowsheet, equipment, portName);
        return flowsheet.GetPipe(pipe.Id) == null;
    }

    private static IReadOnlyCollection<IFlowsheet> GetAffectedFlowsheets(Project project, IFlowsheet flowsheet, IPipeReference pipe)
    {
        var affectedFlowsheets = new Dictionary<Guid, IFlowsheet>
        {
            [flowsheet.Id] = flowsheet
        };

        var sourceElement = project.EquipmentRegistry.GetById(pipe.SourceElementId);
        var targetElement = project.EquipmentRegistry.GetById(pipe.TargetElementId);
        var connector = sourceElement as OffPageConnectorElement ?? targetElement as OffPageConnectorElement;
        if (connector?.TargetAreaId is { } targetFlowsheetId)
        {
            var targetFlowsheet = project.GetFlowsheet(targetFlowsheetId);
            if (targetFlowsheet != null)
            {
                affectedFlowsheets[targetFlowsheet.Id] = targetFlowsheet;
            }
        }

        return affectedFlowsheets.Values.ToArray();
    }
}
