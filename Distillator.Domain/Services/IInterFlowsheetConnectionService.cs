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
        if (localPort == null) return null;

        bool isFlowEnteringSource = localPort.Type == PortType.Inlet;

        // Calcular dimensiones efectivas
        double sourceEffectiveWidth = GetEffectiveWidth(sourceFlowsheet);
        double targetEffectiveWidth = GetEffectiveWidth(targetFlowsheet);
        double arrowOffset = _placementRules.Snap(60, sourceFlowsheet.GridSize);
        double opcWidth = 200;

        // Crear OPC local
        double localX = isFlowEnteringSource ? arrowOffset : sourceEffectiveWidth - opcWidth - arrowOffset;
        double localY = _placementRules.Snap(sourceEquipment.Y, sourceFlowsheet.GridSize);

        var localOpc = new OffPageConnectorElement(!isFlowEnteringSource)
        {
            Width = opcWidth,
            TargetAreaId = targetFlowsheet.Id,
            TargetConnectorId = Guid.NewGuid(),
            Label = targetFlowsheet.Name,
            TargetAreaName = targetFlowsheet.Name,
            ConnectedEquipmentName = sourceEquipment.Label,
            X = localX,
            Y = localY
        };
        localOpc.RefreshPorts();

        // Crear OPC remoto
        double remoteX = isFlowEnteringSource ? targetEffectiveWidth - opcWidth - arrowOffset : arrowOffset;
        double remoteY = _placementRules.Snap(targetStream.Y, targetFlowsheet.GridSize);

        var remoteOpc = new OffPageConnectorElement(isFlowEnteringSource)
        {
            Width = opcWidth,
            TargetAreaId = sourceFlowsheet.Id,
            TargetConnectorId = localOpc.Id,
            Id = localOpc.TargetConnectorId.Value,
            Label = sourceFlowsheet.Name,
            TargetAreaName = sourceFlowsheet.Name,
            ConnectedEquipmentName = targetStream.Label,
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
        sourceFlowsheet.AddElementReference(new OffPageConnectorReference(localOpc.Id, localOpc.X, localOpc.Y, !isFlowEnteringSource)
        {
            TargetFlowsheetId = targetFlowsheet.Id,
            TargetConnectorId = remoteOpc.Id,
            TargetFlowsheetName = targetFlowsheet.Name,
            ConnectedEquipmentName = sourceEquipment.Label
        });
        targetFlowsheet.AddElementReference(new OffPageConnectorReference(remoteOpc.Id, remoteOpc.X, remoteOpc.Y, isFlowEnteringSource)
        {
            TargetFlowsheetId = sourceFlowsheet.Id,
            TargetConnectorId = localOpc.Id,
            TargetFlowsheetName = sourceFlowsheet.Name,
            ConnectedEquipmentName = targetStream.Label
        });

        // Conectar cerebros termodinámicos
        string remotePortName = isFlowEnteringSource ? "Outlet" : "Inlet";
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

        var remoteStreamPort = targetStream.Ports.FirstOrDefault(p => p.Name == remotePortName);
        if (remoteStreamPort != null) remoteStreamPort.ConnectedElementId = remoteOpc.Id;
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
        var connection = new InterFlowsheetConnection(
            sourceFlowsheet.Id,
            targetFlowsheet.Id,
            localOpc.Id,
            remoteOpc.Id);

        project.AddInterFlowsheetConnection(connection);

        return connection;
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

    private static double GetEffectiveWidth(IFlowsheet flowsheet)
    {
        return Math.Max(
            flowsheet.DiagramWidth,
            flowsheet.Elements.Count > 0
                ? flowsheet.Elements.Max(e => e.X + 100)
                : 600);
    }
}
