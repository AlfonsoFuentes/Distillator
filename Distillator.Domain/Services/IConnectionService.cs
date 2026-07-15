using Distillator.Domain.Events;
using Distillator.Domain.Models;
using Distillator.Domain.Policies;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Streams;
using Shared.SolverQwen.Stream;
using Shared.WorkSpaceManagers;

namespace Distillator.Domain.Services;

/// <summary>
/// Servicio de conexiones entre equipos dentro de un mismo Flowsheet.
/// </summary>
public interface IConnectionService
{
    IPipeReference? Connect(
        IFlowsheet flowsheet,
        IVisualElement source,
        string sourcePortName,
        IVisualElement? target,
        string? targetPortName,
        double dropX,
        double dropY);

    void Disconnect(IFlowsheet flowsheet, Guid pipeId);
    bool CanConnect(IFlowsheet flowsheet, IVisualElement source, string sourcePortName, IVisualElement target, string targetPortName);
}

public class ConnectionService : IConnectionService
{
    private readonly IConnectionRules _rules;
    private readonly IPlacementRules _placementRules;
    private readonly IEquipmentNamingService _namingService;
    private readonly IEquipmentFactory _factory;
    private readonly ISimulationService _simulationService;

    public ConnectionService(
        IConnectionRules rules,
        IPlacementRules placementRules,
        IEquipmentNamingService namingService,
        IEquipmentFactory factory,
        ISimulationService simulationService)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _placementRules = placementRules ?? throw new ArgumentNullException(nameof(placementRules));
        _namingService = namingService ?? throw new ArgumentNullException(nameof(namingService));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _simulationService = simulationService ?? throw new ArgumentNullException(nameof(simulationService));
    }

    public bool CanConnect(IFlowsheet flowsheet, IVisualElement source, string sourcePortName, IVisualElement target, string targetPortName)
    {
        return _rules.CanConnect(source, sourcePortName, target, targetPortName, flowsheet);
    }

    public IPipeReference? Connect(
        IFlowsheet flowsheet,
        IVisualElement source,
        string sourcePortName,
        IVisualElement? target,
        string? targetPortName,
        double dropX,
        double dropY)
    {
        if (!_rules.CanConnect(source, sourcePortName, target, targetPortName, flowsheet))
            return null;

        if (target != null && !string.IsNullOrWhiteSpace(targetPortName))
        {
            if (_rules.RequiresIntermediateStream(source, target))
            {
                return ConnectEquipmentToEquipment(flowsheet, source, sourcePortName, target, targetPortName);
            }

            return ConnectDirect(flowsheet, source, sourcePortName, target, targetPortName);
        }

        if (target == null && !string.IsNullOrWhiteSpace(sourcePortName))
        {
            return ConnectToEmptySpace(flowsheet, source, sourcePortName, dropX, dropY);
        }

        return null;
    }

    public void Disconnect(IFlowsheet flowsheet, Guid pipeId)
    {
        var pipe = flowsheet.GetPipe(pipeId);
        if (pipe == null) return;

        var source = flowsheet.Project.EquipmentRegistry.GetById(pipe.SourceElementId);
        var target = flowsheet.Project.EquipmentRegistry.GetById(pipe.TargetElementId);

        source?.Disconnect(pipe.SourcePortName);
        target?.Disconnect(pipe.TargetPortName);

        flowsheet.RemovePipe(pipeId);
    }

    private IPipeReference? ConnectDirect(
        IFlowsheet flowsheet,
        IVisualElement source,
        string sourcePortName,
        IVisualElement target,
        string targetPortName)
    {
        if (!source.Connect(sourcePortName, target, targetPortName))
            return null;

        var pipe = new PipeReference(source.Id, target.Id, sourcePortName, targetPortName);
        flowsheet.AddPipe(pipe);

        if (target.Facade is IFacadeStream streamFacade)
        {
            _simulationService.ConnectEquipmentToStream(flowsheet.Project, flowsheet, source, sourcePortName, target);
        }

        return pipe;
    }

    private IPipeReference? ConnectEquipmentToEquipment(
        IFlowsheet flowsheet,
        IVisualElement source,
        string sourcePortName,
        IVisualElement target,
        string targetPortName)
    {
        var sourcePort = source.Ports.FirstOrDefault(p => p.Name == sourcePortName);
        var targetPort = target.Ports.FirstOrDefault(p => p.Name == targetPortName);
        if (sourcePort == null || targetPort == null || sourcePort.Type == targetPort.Type)
            return null;

        var placement = CalculateIntermediateStreamPlacement(source, sourcePort, target, targetPort, flowsheet.GridSize);
        var newStream = CreateStream(flowsheet, source, sourcePort, placement.X, placement.Y, placement.RotationAngle);
        if (newStream == null) return null;

        var streamPortForSource = sourcePort.Type == PortType.Inlet ? "Outlet" : "Inlet";
        var streamPortForTarget = targetPort.Type == PortType.Inlet ? "Outlet" : "Inlet";

        if (!source.Connect(sourcePortName, newStream, streamPortForSource))
        {
            RemoveStream(flowsheet, newStream);
            return null;
        }

        if (!newStream.Connect(streamPortForTarget, target, targetPortName))
        {
            source.Disconnect(sourcePortName);
            RemoveStream(flowsheet, newStream);
            return null;
        }

        var pipe1 = new PipeReference(source.Id, newStream.Id, sourcePortName, streamPortForSource);
        var pipe2 = new PipeReference(newStream.Id, target.Id, streamPortForTarget, targetPortName);

        flowsheet.AddPipe(pipe1);
        flowsheet.AddPipe(pipe2);

        _simulationService.ConnectEquipmentToStream(flowsheet.Project, flowsheet, source, sourcePortName, newStream);
        _simulationService.ConnectEquipmentToStream(flowsheet.Project, flowsheet, target, targetPortName, newStream);

        return pipe2;
    }

    private IPipeReference? ConnectToEmptySpace(
        IFlowsheet flowsheet,
        IVisualElement source,
        string sourcePortName,
        double dropX,
        double dropY)
    {
        var sourcePort = source.Ports.FirstOrDefault(p => p.Name == sourcePortName);
        if (sourcePort == null) return null;

        var newStream = CreateStream(flowsheet, source, sourcePort, dropX, dropY);
        if (newStream == null) return null;

        var isSourceInlet = sourcePort.Type == PortType.Inlet;
        var streamPortName = isSourceInlet ? "Outlet" : "Inlet";

        if (!source.Connect(sourcePortName, newStream, streamPortName))
        {
            RemoveStream(flowsheet, newStream);
            return null;
        }

        var pipe = isSourceInlet
            ? new PipeReference(newStream.Id, source.Id, streamPortName, sourcePortName)
            : new PipeReference(source.Id, newStream.Id, sourcePortName, streamPortName);

        flowsheet.AddPipe(pipe);

        _simulationService.ConnectEquipmentToStream(flowsheet.Project, flowsheet, source, sourcePortName, newStream);

        return pipe;
    }

    private StreamVisualElement? CreateStream(
        IFlowsheet flowsheet,
        IVisualElement source,
        EquipmentPort sourcePort,
        double? overrideX = null,
        double? overrideY = null,
        int? rotationAngle = null)
    {
        var element = _factory.Create(EquipmentType.MaterialStream, 0, 0, v => _placementRules.Snap(v, flowsheet.GridSize));
        if (element is not StreamVisualElement stream) return null;

        var name = _namingService.GenerateNextName("Stream", flowsheet.Project, flowsheet);
        stream.Name = name;
        stream.Label = name;
        if (stream.Facade != null)
        {
            stream.Facade.Name = name;
        }

        if (overrideX.HasValue && overrideY.HasValue)
        {
            stream.X = _placementRules.Snap(overrideX.Value, flowsheet.GridSize);
            stream.Y = _placementRules.Snap(overrideY.Value, flowsheet.GridSize);
            if (rotationAngle.HasValue)
            {
                stream.RotationAngle = NormalizeRotation(rotationAngle.Value);
            }
        }
        else
        {
            var portCoords = source.GetAbsolutePortCoordinates(sourcePort.Name);
            double offset = 180;
            double x = portCoords.X;
            double y = portCoords.Y;

            switch (portCoords.Direction)
            {
                case PortDirection.Right: x += offset; break;
                case PortDirection.Left: x -= offset; break;
                case PortDirection.Top: y -= offset; break;
                case PortDirection.Bottom: y += offset; break;
            }

            stream.X = _placementRules.Snap(x - (stream.Width / 2.0), flowsheet.GridSize);
            stream.Y = _placementRules.Snap(y - (stream.Height / 2.0), flowsheet.GridSize);
        }

        var reference = new FlowsheetElementReference(stream.Id, stream.X, stream.Y);
        flowsheet.AddElementReference(reference);
        flowsheet.Project.AddEquipment(stream);

        // La conexión al solver la hacen los callers (ConnectToEmptySpace, ConnectEquipmentToEquipment)
        // después de que source.Connect() tenga éxito, para evitar doble conexión del puerto.

        return stream;
    }

    private StreamPlacement CalculateIntermediateStreamPlacement(
        IVisualElement source,
        EquipmentPort sourcePort,
        IVisualElement target,
        EquipmentPort targetPort,
        double gridSize)
    {
        var sourcePoint = source.GetAbsolutePortCoordinates(sourcePort.Name);
        var targetPoint = target.GetAbsolutePortCoordinates(targetPort.Name);

        var upstreamPoint = sourcePort.Type == PortType.Outlet ? sourcePoint : targetPoint;
        var downstreamPoint = sourcePort.Type == PortType.Outlet ? targetPoint : sourcePoint;

        const double nozzleClearance = 72;
        var upstreamLane = ProjectFromPort(upstreamPoint, nozzleClearance);
        var downstreamLane = ProjectFromPort(downstreamPoint, nozzleClearance);

        var dx = downstreamLane.X - upstreamLane.X;
        var dy = downstreamLane.Y - upstreamLane.Y;
        var rotationAngle = Math.Abs(dx) >= Math.Abs(dy)
            ? (dx >= 0 ? 0 : 180)
            : (dy >= 0 ? 90 : 270);

        var centerX = (upstreamLane.X + downstreamLane.X) / 2.0;
        var centerY = (upstreamLane.Y + downstreamLane.Y) / 2.0;

        if (rotationAngle is 0 or 180)
        {
            centerY = ChooseLane(upstreamPoint.Y, downstreamPoint.Y, upstreamPoint.Direction, downstreamPoint.Direction, true);
        }
        else
        {
            centerX = ChooseLane(upstreamPoint.X, downstreamPoint.X, upstreamPoint.Direction, downstreamPoint.Direction, false);
        }

        var x = centerX - (60 / 2.0);
        var y = centerY - (30 / 2.0);

        return new StreamPlacement(
            _placementRules.Snap(x, gridSize),
            _placementRules.Snap(y, gridSize),
            rotationAngle);
    }

    private static double ChooseLane(
        double upstreamCoordinate,
        double downstreamCoordinate,
        PortDirection upstreamDirection,
        PortDirection downstreamDirection,
        bool horizontalStream)
    {
        var upstreamAligned = horizontalStream
            ? upstreamDirection is PortDirection.Left or PortDirection.Right
            : upstreamDirection is PortDirection.Top or PortDirection.Bottom;
        var downstreamAligned = horizontalStream
            ? downstreamDirection is PortDirection.Left or PortDirection.Right
            : downstreamDirection is PortDirection.Top or PortDirection.Bottom;

        if (upstreamAligned && !downstreamAligned) return upstreamCoordinate;
        if (!upstreamAligned && downstreamAligned) return downstreamCoordinate;
        return (upstreamCoordinate + downstreamCoordinate) / 2.0;
    }

    private static (double X, double Y) ProjectFromPort(AbsoluteCoordinates point, double distance)
    {
        var (nx, ny) = DirectionVector(point.Direction);
        return (point.X + nx * distance, point.Y + ny * distance);
    }

    private static (double X, double Y) DirectionVector(PortDirection direction) => direction switch
    {
        PortDirection.Right => (1, 0),
        PortDirection.Left => (-1, 0),
        PortDirection.Bottom => (0, 1),
        PortDirection.Top => (0, -1),
        _ => (0, 0)
    };

    private static int NormalizeRotation(int angle)
    {
        var normalized = angle % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private sealed record StreamPlacement(double X, double Y, int RotationAngle);

    private void RemoveStream(IFlowsheet flowsheet, StreamVisualElement stream)
    {
        flowsheet.RemoveElementReference(stream.Id);
        flowsheet.Project.RemoveEquipment(stream.Id);
    }
}
