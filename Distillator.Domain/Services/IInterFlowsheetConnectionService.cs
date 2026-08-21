using Distillator.Domain.Events;
using Distillator.Domain.Models;
using Distillator.Domain.Policies;
using Shared.ProcessFlowDiagram;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;

namespace Distillator.Domain.Services;

/// <summary>
/// Servicio de conexiones entre dos Flowsheets mediante Off-Page Connectors (OPCs).
/// </summary>
public interface IInterFlowsheetConnectionService
{
    IInterFlowsheetConnection? CreateInterFlowsheetConnection(
        IProject project,
        IFlowsheet sourceFlowsheet,
        IVisualElement sourceEquipment,
        string sourcePortName,
        IFlowsheet targetFlowsheet,
        IVisualElement targetStream);

    void RemoveInterFlowsheetConnection(IProject project, Guid connectionId);
}

public class InterFlowsheetConnectionService : IInterFlowsheetConnectionService
{
    private readonly IPlacementRules _placementRules;
    private readonly IEquipmentNamingService _namingService;
    private readonly ISimulationService _simulationService;

    public InterFlowsheetConnectionService(
        IPlacementRules placementRules,
        IEquipmentNamingService namingService,
        ISimulationService simulationService)
    {
        _placementRules = placementRules ?? throw new ArgumentNullException(nameof(placementRules));
        _namingService = namingService ?? throw new ArgumentNullException(nameof(namingService));
        _simulationService = simulationService ?? throw new ArgumentNullException(nameof(simulationService));
    }

    public IInterFlowsheetConnection? CreateInterFlowsheetConnection(
        IProject project,
        IFlowsheet sourceFlowsheet,
        IVisualElement sourceEquipment,
        string sourcePortName,
        IFlowsheet targetFlowsheet,
        IVisualElement targetStream)
    {
        var localPort = sourceEquipment.Ports.FirstOrDefault(p => p.Name == sourcePortName);
        if (localPort == null || localPort.ConnectedElementId.HasValue) return null;
        if (!CanCreateInterFlowsheetConnection(
                project,
                sourceFlowsheet,
                sourceEquipment,
                localPort,
                targetFlowsheet,
                targetStream,
                out var remotePortName,
                out var remoteStreamPort))
        {
            return null;
        }

        bool isFlowEnteringSource = localPort.Type == PortType.Inlet;
        OffPageConnectorElement? localOpc = null;
        OffPageConnectorElement? remoteOpc = null;
        IInterFlowsheetConnection? connection = null;
        var previousLocalConnection = localPort.ConnectedElementId;
        var previousRemoteConnection = remoteStreamPort!.ConnectedElementId;

        try
        {
            double arrowOffset = _placementRules.Snap(60, sourceFlowsheet.GridSize);
            double opcWidth = 200;
            var localAnchorSide = isFlowEnteringSource ? OffPageConnectorPortSide.Left : OffPageConnectorPortSide.Right;
            var remoteAnchorSide = isFlowEnteringSource ? OffPageConnectorPortSide.Right : OffPageConnectorPortSide.Left;
            var localPortSide = GetInwardPortSide(localAnchorSide);
            var remotePortSide = GetInwardPortSide(remoteAnchorSide);

            // Crear OPC local
            double localX = GetConnectorX(project, sourceFlowsheet, arrowOffset);
            double localY = _placementRules.Snap(sourceEquipment.Y, sourceFlowsheet.GridSize);

            localOpc = new OffPageConnectorElement(!isFlowEnteringSource, localPortSide)
            {
                Width = opcWidth,
                TargetAreaId = targetFlowsheet.Id,
                TargetConnectorId = Guid.NewGuid(),
                Label = targetFlowsheet.Name,
                TargetAreaName = targetFlowsheet.Name,
                ConnectedEquipmentName = targetStream.Label,
                X = localX,
                Y = localY
            };
            localOpc.RefreshPorts();

            // Crear OPC remoto
            double remoteX = GetConnectorX(project, targetFlowsheet, arrowOffset);
            double remoteY = _placementRules.Snap(targetStream.Y, targetFlowsheet.GridSize);

            remoteOpc = new OffPageConnectorElement(isFlowEnteringSource, remotePortSide)
            {
                Width = opcWidth,
                TargetAreaId = sourceFlowsheet.Id,
                TargetConnectorId = localOpc.Id,
                Id = localOpc.TargetConnectorId.Value,
                Label = sourceFlowsheet.Name,
                TargetAreaName = sourceFlowsheet.Name,
                ConnectedEquipmentName = sourceEquipment.Label,
                X = remoteX,
                Y = remoteY
            };
            remoteOpc.RefreshPorts();

            // Asignar nombres
            var opcName = _namingService.GenerateNextName("OffPageConnector", project, sourceFlowsheet);
            localOpc.Name = opcName;
            localOpc.Label = opcName;
            remoteOpc.Name = opcName;
            remoteOpc.Label = opcName;

            // Registrar equipos y referencias
            project.AddEquipment(localOpc);
            project.AddEquipment(remoteOpc);
            sourceFlowsheet.AddElementReference(new OffPageConnectorReference(localOpc.Id, localOpc.X, localOpc.Y, !isFlowEnteringSource, localPortSide)
            {
                TargetFlowsheetId = targetFlowsheet.Id,
                TargetConnectorId = remoteOpc.Id,
                TargetFlowsheetName = targetFlowsheet.Name,
                ConnectedEquipmentName = targetStream.Label
            });
            targetFlowsheet.AddElementReference(new OffPageConnectorReference(remoteOpc.Id, remoteOpc.X, remoteOpc.Y, isFlowEnteringSource, remotePortSide)
            {
                TargetFlowsheetId = sourceFlowsheet.Id,
                TargetConnectorId = localOpc.Id,
                TargetFlowsheetName = sourceFlowsheet.Name,
                ConnectedEquipmentName = sourceEquipment.Label
            });

            // Conectar cerebros termodinámicos
            if (sourceEquipment.Facade is IEquipmentFacade localEquipment && targetStream.Facade is IFacadeStream remoteFacade)
            {
                sourceEquipment.AttachConnection(sourcePortName, remoteFacade);
            }
            else if (targetStream.Facade is IEquipmentFacade remoteEquipment && sourceEquipment.Facade is IFacadeStream localStream)
            {
                sourceEquipment.AttachConnection(remotePortName, localStream);
            }

            // Ocupar puertos
            localPort.ConnectedElementId = localOpc.Id;
            localOpc.Ports.First(p => p.Name == "Transfer").ConnectedElementId = sourceEquipment.Id;

            remoteStreamPort.ConnectedElementId = remoteOpc.Id;
            remoteOpc.Ports.First(p => p.Name == "Transfer").ConnectedElementId = targetStream.Id;

            // Crear pipes visuales respetando la dirección real del flujo.
            var localPipe = isFlowEnteringSource
                ? new Models.PipeReference(localOpc.Id, sourceEquipment.Id, "Transfer", sourcePortName)
                : new Models.PipeReference(sourceEquipment.Id, localOpc.Id, sourcePortName, "Transfer");

            var remotePipe = isFlowEnteringSource
                ? new Models.PipeReference(targetStream.Id, remoteOpc.Id, remotePortName, "Transfer")
                : new Models.PipeReference(remoteOpc.Id, targetStream.Id, "Transfer", remotePortName);

            sourceFlowsheet.AddPipe(localPipe);
            targetFlowsheet.AddPipe(remotePipe);

            // Registrar el stream remoto en el solver sin romper los pipes OPC.
            if (targetStream.Facade is IFacadeStream facadeStream)
            {
                if (!_simulationService.Solver.Streams.Contains(facadeStream))
                {
                    _simulationService.Solver.AddStream(facadeStream);
                }
            }

            // Crear conexión inter-flowsheet
            connection = new InterFlowsheetConnection(
                sourceFlowsheet.Id,
                targetFlowsheet.Id,
                localOpc.Id,
                remoteOpc.Id);

            project.AddInterFlowsheetConnection(connection);

            return connection;
        }
        catch
        {
            localPort.ConnectedElementId = previousLocalConnection;
            remoteStreamPort!.ConnectedElementId = previousRemoteConnection;
            RemovePartialConnectionArtifacts(project, sourceFlowsheet, targetFlowsheet, localOpc?.Id, remoteOpc?.Id, connection?.Id);
            throw;
        }
    }

    private static bool CanCreateInterFlowsheetConnection(
        IProject project,
        IFlowsheet sourceFlowsheet,
        IVisualElement sourceEquipment,
        EquipmentPort localPort,
        IFlowsheet targetFlowsheet,
        IVisualElement targetStream,
        out string remotePortName,
        out EquipmentPort? remoteStreamPort)
    {
        var requiredRemotePortName = localPort.Type == PortType.Inlet ? "Outlet" : "Inlet";
        remotePortName = requiredRemotePortName;
        remoteStreamPort = targetStream.Ports.FirstOrDefault(p => p.Name == requiredRemotePortName);

        return sourceFlowsheet.Project.Id == project.Id &&
               targetFlowsheet.Project.Id == project.Id &&
               sourceFlowsheet.Id != targetFlowsheet.Id &&
               sourceFlowsheet.Elements.Any(reference => reference.ElementId == sourceEquipment.Id) &&
               targetFlowsheet.Elements.Any(reference => reference.ElementId == targetStream.Id) &&
               targetStream.Facade is IFacadeStream &&
               remoteStreamPort != null &&
               !remoteStreamPort.ConnectedElementId.HasValue;
    }

    private static void RemovePartialConnectionArtifacts(
        IProject project,
        IFlowsheet sourceFlowsheet,
        IFlowsheet targetFlowsheet,
        Guid? localConnectorId,
        Guid? remoteConnectorId,
        Guid? connectionId)
    {
        if (localConnectorId.HasValue)
        {
            RemoveConnectorArtifacts(project, sourceFlowsheet, localConnectorId.Value);
        }

        if (remoteConnectorId.HasValue)
        {
            RemoveConnectorArtifacts(project, targetFlowsheet, remoteConnectorId.Value);
        }

        if (connectionId.HasValue)
        {
            project.RemoveInterFlowsheetConnection(connectionId.Value);
        }
    }

    private static void RemoveConnectorArtifacts(IProject project, IFlowsheet flowsheet, Guid connectorId)
    {
        foreach (var pipe in flowsheet.Pipes
                     .Where(candidate => candidate.SourceElementId == connectorId || candidate.TargetElementId == connectorId)
                     .ToList())
        {
            flowsheet.RemovePipe(pipe.Id);
        }

        flowsheet.RemoveElementReference(connectorId);
        project.RemoveEquipment(connectorId);
    }

    public void RemoveInterFlowsheetConnection(IProject project, Guid connectionId)
    {
        var connection = project.InterFlowsheetConnections.FirstOrDefault(c => c.Id == connectionId);
        if (connection == null) return;

        var sourceFlowsheet = project.GetFlowsheet(connection.SourceFlowsheetId);
        var targetFlowsheet = project.GetFlowsheet(connection.TargetFlowsheetId);

        if (sourceFlowsheet != null)
        {
            var localPipe = sourceFlowsheet.Pipes.FirstOrDefault(p =>
                p.SourceElementId == connection.SourceConnectorId || p.TargetElementId == connection.SourceConnectorId);
            if (localPipe != null) sourceFlowsheet.RemovePipe(localPipe.Id);
            sourceFlowsheet.RemoveElementReference(connection.SourceConnectorId);
        }

        if (targetFlowsheet != null)
        {
            var remotePipe = targetFlowsheet.Pipes.FirstOrDefault(p =>
                p.SourceElementId == connection.TargetConnectorId || p.TargetElementId == connection.TargetConnectorId);
            if (remotePipe != null) targetFlowsheet.RemovePipe(remotePipe.Id);
            targetFlowsheet.RemoveElementReference(connection.TargetConnectorId);
        }

        project.RemoveEquipment(connection.SourceConnectorId);
        project.RemoveEquipment(connection.TargetConnectorId);
        project.RemoveInterFlowsheetConnection(connection.Id);
    }

    private static double GetConnectorX(IProject project, IFlowsheet flowsheet, double offset)
    {
        var rightMostElement = flowsheet.Elements
            .Where(reference => project.GetEquipment(reference.ElementId) is not OffPageConnectorElement)
            .Select(reference => GetElementRightEdge(project, reference))
            .DefaultIfEmpty(0)
            .Max();

        return Math.Max(offset, Snap(rightMostElement + offset, flowsheet.GridSize));
    }

    private static double GetElementRightEdge(IProject project, IFlowsheetElementReference reference)
    {
        var element = project.GetEquipment(reference.ElementId);
        var width = element?.Width > 0 ? element.Width : 100;
        return reference.X + width;
    }

    private static double Snap(double value, double gridSize)
    {
        if (gridSize <= 0)
        {
            return value;
        }

        return Math.Round(value / gridSize) * gridSize;
    }

    private static OffPageConnectorPortSide GetInwardPortSide(OffPageConnectorPortSide anchorSide) =>
        anchorSide == OffPageConnectorPortSide.Left
            ? OffPageConnectorPortSide.Right
            : OffPageConnectorPortSide.Left;
}
