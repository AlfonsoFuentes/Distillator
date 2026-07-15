using Distillator.Domain.Core;
using Distillator.Domain.Events;
using Distillator.Domain.Models;
using Distillator.Domain.Policies;
using Shared.ProcessFlowDiagram;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;

namespace Distillator.Domain.Services;

/// <summary>
/// Orquesta la ejecución del solver para un proyecto.
/// </summary>
public interface ISimulationService
{
    IMainSolver Solver { get; }
    IReadOnlyCollection<IDomainEvent> RecentEvents { get; }
    string? LastError { get; }

    void RunSimulation(IProject project);
    void PropagateThermodynamicMethod(IProject project);
    void ClearOrphanStream(IProject project, IFacadeStream stream);
    void DisconnectPort(IProject project, IFlowsheet flowsheet, IVisualElement equipment, string portName);
    void ConnectEquipmentToStream(IProject project, IFlowsheet flowsheet, IVisualElement equipment, string equipmentPortName, IVisualElement stream);
}

public class SimulationService : ISimulationService
{
    private readonly ISimulationPolicy _policy;
    private readonly List<IDomainEvent> _recentEvents = new();

    public IMainSolver Solver { get; }
    public IReadOnlyCollection<IDomainEvent> RecentEvents => _recentEvents.AsReadOnly();
    public string? LastError { get; private set; }

    public SimulationService(IMainSolver solver, ISimulationPolicy? policy = null)
    {
        Solver = solver ?? throw new ArgumentNullException(nameof(solver));
        _policy = policy ?? new SimulationPolicy();
    }

    public void RunSimulation(IProject project)
    {
        LastError = null;
        Raise(new SimulationStartedEvent(project.Id));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            Solver.RunSimulation();
            stopwatch.Stop();
            var successSnapshot = new SimulationSnapshot(Guid.NewGuid(), project.Id, DateTime.UtcNow, true, stopwatch.Elapsed, "{}");
            Raise(new SimulationCompletedEvent(project.Id, stopwatch.Elapsed, true));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LastError = ex.Message;
            var failedSnapshot = new SimulationSnapshot(Guid.NewGuid(), project.Id, DateTime.UtcNow, false, stopwatch.Elapsed, "{\"error\":\"" + ex.Message + "\"}");
            Raise(new SimulationFailedEvent(project.Id, ex.Message));
            Raise(new SimulationCompletedEvent(project.Id, stopwatch.Elapsed, false));
        }
    }

    public void PropagateThermodynamicMethod(IProject project)
    {
        var method = project.Configuration.ThermodynamicMethod;
        if (method == null) return;

        Solver.ThermoMethod = method;
        foreach (var stream in Solver.Streams)
        {
            stream.SetThermodynamicMethod(method);
        }
    }

    public void ClearOrphanStream(IProject project, IFacadeStream stream)
    {
        Solver.ClearOrphanStream(stream);
    }

    public void DisconnectPort(IProject project, IFlowsheet flowsheet, IVisualElement equipment, string portName)
    {
        var pipe = flowsheet.Pipes.FirstOrDefault(p =>
            (p.SourceElementId == equipment.Id && p.SourcePortName == portName) ||
            (p.TargetElementId == equipment.Id && p.TargetPortName == portName));

        if (pipe == null) return;

        var otherElement = pipe.SourceElementId == equipment.Id
            ? project.EquipmentRegistry.GetById(pipe.TargetElementId)
            : project.EquipmentRegistry.GetById(pipe.SourceElementId);

        if (otherElement is OffPageConnectorElement localOpc)
        {
            DisconnectOffPageConnector(project, flowsheet, localOpc, pipe);
        }
        else
        {
            DisconnectNormalPipe(project, flowsheet, equipment, pipe, otherElement);
        }

        flowsheet.RemovePipe(pipe.Id);
        equipment.Disconnect(portName);

        Raise(new ConnectionRemovedEvent(project.Id, flowsheet.Id, pipe.Id));
    }

    public void ConnectEquipmentToStream(IProject project, IFlowsheet flowsheet, IVisualElement equipment, string equipmentPortName, IVisualElement stream)
    {
        if (equipment.Ports.FirstOrDefault(p => p.Name == equipmentPortName)?.ConnectedElementId.HasValue == true)
        {
            DisconnectPort(project, flowsheet, equipment, equipmentPortName);
        }

        var equipmentPort = equipment.Ports.FirstOrDefault(p => p.Name == equipmentPortName);
        if (equipmentPort == null) return;

        var isEquipmentInlet = equipmentPort.Type == PortType.Inlet;
        var streamPortName = isEquipmentInlet ? "Outlet" : "Inlet";

        if (!equipment.Connect(equipmentPortName, stream, streamPortName)) return;

        var pipe = isEquipmentInlet
            ? new PipeReference(stream.Id, equipment.Id, streamPortName, equipmentPortName)
            : new PipeReference(equipment.Id, stream.Id, equipmentPortName, streamPortName);

        flowsheet.AddPipe(pipe);

        if (stream.Facade is IFacadeStream facadeStream)
        {
            if (project.Configuration.ThermodynamicMethod != null)
            {
                facadeStream.SetThermodynamicMethod(project.Configuration.ThermodynamicMethod);
                Solver.ThermoMethod = project.Configuration.ThermodynamicMethod;
            }

            if (!Solver.Streams.Contains(facadeStream))
            {
                Solver.AddStream(facadeStream);
            }
        }

        Raise(new ConnectionCreatedEvent(project.Id, flowsheet.Id, pipe.Id));
    }

    public bool ShouldRunSimulation(IDomainEvent domainEvent)
    {
        return _policy.ShouldRunSimulation(domainEvent);
    }

    private void DisconnectNormalPipe(IProject project, IFlowsheet flowsheet, IVisualElement equipment, IPipeReference pipe, IVisualElement? otherElement)
    {
        var sourceElement = project.EquipmentRegistry.GetById(pipe.SourceElementId);
        var targetElement = project.EquipmentRegistry.GetById(pipe.TargetElementId);

        IFacadeStream? streamToClean = null;
        IVisualElement? streamVisualElement = null;

        if (sourceElement?.Facade is IFacadeStream sourceStream)
        {
            streamToClean = sourceStream;
            streamVisualElement = sourceElement;
        }
        else if (targetElement?.Facade is IFacadeStream targetStream)
        {
            streamToClean = targetStream;
            streamVisualElement = targetElement;
        }

        if (streamVisualElement != null && otherElement != null)
        {
            var otherPortName = pipe.SourceElementId == equipment.Id ? pipe.TargetPortName : pipe.SourcePortName;
            otherElement.Disconnect(otherPortName);

            if (streamVisualElement.Ports.All(p => p.ConnectedElementId == null))
            {
                Solver.ClearOrphanStream(streamToClean!);
            }
        }
    }

    private void DisconnectOffPageConnector(IProject project, IFlowsheet flowsheet, OffPageConnectorElement localOpc, IPipeReference pipe)
    {
        if (localOpc.TargetConnectorId == null || localOpc.TargetAreaId == null) return;

        var remoteFlowsheet = project.GetFlowsheet(localOpc.TargetAreaId.Value);
        var remoteOpc = remoteFlowsheet?.Elements
            .OfType<IOffPageConnectorReference>()
            .FirstOrDefault(e => e.ElementId == localOpc.TargetConnectorId.Value);

        if (remoteFlowsheet == null || remoteOpc == null) return;

        var remotePipe = remoteFlowsheet.Pipes.FirstOrDefault(p =>
            p.SourceElementId == remoteOpc.ElementId || p.TargetElementId == remoteOpc.ElementId);

        if (remotePipe != null)
        {
            var remoteStreamElement = remotePipe.SourceElementId == remoteOpc.ElementId
                ? project.EquipmentRegistry.GetById(remotePipe.TargetElementId)
                : project.EquipmentRegistry.GetById(remotePipe.SourceElementId);

            var remoteStreamPortName = remotePipe.SourceElementId == remoteOpc.ElementId
                ? remotePipe.TargetPortName
                : remotePipe.SourcePortName;

            if (remoteStreamElement?.Facade is IFacadeStream remoteFacadeStream)
            {
                remoteStreamElement.Disconnect(remoteStreamPortName);
                if (remoteStreamElement.Ports.All(p => p.ConnectedElementId == null))
                {
                    Solver.ClearOrphanStream(remoteFacadeStream);
                }
            }

            remoteFlowsheet.RemovePipe(remotePipe.Id);
            Raise(new ConnectionRemovedEvent(project.Id, remoteFlowsheet.Id, remotePipe.Id));
        }

        remoteFlowsheet.RemoveElementReference(remoteOpc.ElementId);
        flowsheet.RemoveElementReference(localOpc.Id);

        var interConnection = project.InterFlowsheetConnections.FirstOrDefault(c =>
            c.SourceConnectorId == localOpc.Id || c.TargetConnectorId == localOpc.Id);
        if (interConnection != null)
        {
            project.RemoveInterFlowsheetConnection(interConnection.Id);
        }
    }



    private void Raise(IDomainEvent domainEvent)
    {
        _recentEvents.Add(domainEvent);
    }
}
