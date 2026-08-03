using Distillator.Domain.Models;
using Shared.ProcessFlowDiagram;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;

namespace Distillator.Domain.Services;

public sealed class ProjectInterFlowsheetConnectionHydrationService
{
    public int Restore(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var restoredConnectorIds = new HashSet<Guid>();
        var restoredCount = 0;

        foreach (var sourceFlowsheet in project.Flowsheets)
        {
            foreach (var sourceReference in sourceFlowsheet.Elements.OfType<IOffPageConnectorReference>())
            {
                if (!CanUseSourceConnector(sourceReference, restoredConnectorIds))
                {
                    continue;
                }

                var targetFlowsheet = project.GetFlowsheet(sourceReference.TargetFlowsheetId!.Value);
                var targetReference = targetFlowsheet?.Elements
                    .OfType<IOffPageConnectorReference>()
                    .FirstOrDefault(reference => reference.ElementId == sourceReference.TargetConnectorId!.Value);
                if (targetFlowsheet == null ||
                    targetReference == null ||
                    !IsReciprocal(sourceFlowsheet, sourceReference, targetFlowsheet, targetReference))
                {
                    continue;
                }

                restoredConnectorIds.Add(sourceReference.ElementId);
                restoredConnectorIds.Add(targetReference.ElementId);
                if (ConnectionAlreadyExists(project, sourceReference.ElementId, targetReference.ElementId))
                {
                    continue;
                }

                project.AddInterFlowsheetConnection(new InterFlowsheetConnection(
                    sourceFlowsheet.Id,
                    targetFlowsheet.Id,
                    sourceReference.ElementId,
                    targetReference.ElementId));

                RestoreSimulationConnection(
                    project,
                    sourceFlowsheet,
                    sourceReference,
                    targetFlowsheet,
                    targetReference);
                restoredCount++;
            }
        }

        return restoredCount;
    }

    private static bool CanUseSourceConnector(
        IOffPageConnectorReference sourceReference,
        HashSet<Guid> restoredConnectorIds)
    {
        return sourceReference.TargetFlowsheetId.HasValue &&
               sourceReference.TargetConnectorId.HasValue &&
               !restoredConnectorIds.Contains(sourceReference.ElementId);
    }

    private static bool IsReciprocal(
        IFlowsheet sourceFlowsheet,
        IOffPageConnectorReference sourceReference,
        IFlowsheet targetFlowsheet,
        IOffPageConnectorReference targetReference)
    {
        return sourceReference.TargetFlowsheetId == targetFlowsheet.Id &&
               sourceReference.TargetConnectorId == targetReference.ElementId &&
               targetReference.TargetFlowsheetId == sourceFlowsheet.Id &&
               targetReference.TargetConnectorId == sourceReference.ElementId;
    }

    private static bool ConnectionAlreadyExists(Project project, Guid sourceConnectorId, Guid targetConnectorId)
    {
        return project.InterFlowsheetConnections.Any(connection =>
            (connection.SourceConnectorId == sourceConnectorId && connection.TargetConnectorId == targetConnectorId) ||
            (connection.SourceConnectorId == targetConnectorId && connection.TargetConnectorId == sourceConnectorId));
    }

    private static void RestoreSimulationConnection(
        Project project,
        IFlowsheet sourceFlowsheet,
        IOffPageConnectorReference sourceConnector,
        IFlowsheet targetFlowsheet,
        IOffPageConnectorReference targetConnector)
    {
        var sourceEndpoint = GetConnectedEndpoint(project, sourceFlowsheet, sourceConnector.ElementId);
        var targetEndpoint = GetConnectedEndpoint(project, targetFlowsheet, targetConnector.ElementId);
        if (sourceEndpoint.Element == null || targetEndpoint.Element == null) return;

        if (sourceEndpoint.Element.Facade is IEquipmentFacade &&
            targetEndpoint.Element.Facade is IFacadeStream targetStream)
        {
            sourceEndpoint.Element.AttachConnection(sourceEndpoint.PortName, targetStream);
        }
        else if (targetEndpoint.Element.Facade is IEquipmentFacade &&
                 sourceEndpoint.Element.Facade is IFacadeStream sourceStream)
        {
            targetEndpoint.Element.AttachConnection(targetEndpoint.PortName, sourceStream);
        }
    }

    private static (IVisualElement? Element, string PortName) GetConnectedEndpoint(
        Project project,
        IFlowsheet flowsheet,
        Guid connectorId)
    {
        var pipe = flowsheet.Pipes.FirstOrDefault(candidate =>
            candidate.SourceElementId == connectorId || candidate.TargetElementId == connectorId);
        if (pipe == null) return (null, string.Empty);

        return pipe.SourceElementId == connectorId
            ? (project.EquipmentRegistry.GetById(pipe.TargetElementId), pipe.TargetPortName)
            : (project.EquipmentRegistry.GetById(pipe.SourceElementId), pipe.SourcePortName);
    }
}
