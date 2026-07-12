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

        var newStream = CreateStream(flowsheet, source, sourcePort);
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

    private StreamVisualElement? CreateStream(IFlowsheet flowsheet, IVisualElement source, EquipmentPort sourcePort, double? overrideX = null, double? overrideY = null)
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

    private void RemoveStream(IFlowsheet flowsheet, StreamVisualElement stream)
    {
        flowsheet.RemoveElementReference(stream.Id);
        flowsheet.Project.RemoveEquipment(stream.Id);
    }
}
