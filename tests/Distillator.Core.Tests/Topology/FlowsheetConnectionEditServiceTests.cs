using Distillator.Domain.Models;
using Distillator.Domain.Policies;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.ControlValves;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;

namespace Distillator.Core.Tests.Topology;

public sealed class FlowsheetConnectionEditServiceTests
{
    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void TryConnect_WhenDirectConnectionIsValid_ShouldCreateOnePipeAndConnectBothPorts()
    {
        var (project, flowsheet, connectionService, editService) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101" };
        var stream = new StreamVisualElement { Name = "S-101" };
        AddEquipment(project, flowsheet, pump);
        AddEquipment(project, flowsheet, stream);

        var pipe = editService.TryConnect(flowsheet, connectionService, pump, "Discharge", stream, "Inlet", 0, 0);

        Assert.NotNull(pipe);
        Assert.Single(flowsheet.Pipes);
        Assert.Equal(pump.Id, pipe.SourceElementId);
        Assert.Equal(stream.Id, pipe.TargetElementId);
        Assert.Equal(stream.Id, pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId);
        Assert.Equal(pump.Id, stream.Ports.Single(port => port.Name == "Inlet").ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void TryConnect_WhenConnectionIsInvalid_ShouldLeaveTopologyUnchanged()
    {
        var (project, flowsheet, connectionService, editService) = CreateContext();
        var streamA = new StreamVisualElement { Name = "S-101" };
        var streamB = new StreamVisualElement { Name = "S-102" };
        AddEquipment(project, flowsheet, streamA);
        AddEquipment(project, flowsheet, streamB);

        var pipe = editService.TryConnect(flowsheet, connectionService, streamA, "Outlet", streamB, "Inlet", 0, 0);

        Assert.Null(pipe);
        Assert.Empty(flowsheet.Pipes);
        Assert.Null(streamA.Ports.Single(port => port.Name == "Outlet").ConnectedElementId);
        Assert.Null(streamB.Ports.Single(port => port.Name == "Inlet").ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void TryDisconnectPort_WhenPipeExists_ShouldRemovePipeAndReleaseBothPorts()
    {
        var (project, flowsheet, connectionService, editService) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101" };
        var stream = new StreamVisualElement { Name = "S-101" };
        AddEquipment(project, flowsheet, pump);
        AddEquipment(project, flowsheet, stream);
        editService.TryConnect(flowsheet, connectionService, pump, "Discharge", stream, "Inlet", 0, 0);

        var disconnected = editService.TryDisconnectPort(project, flowsheet, pump, "Discharge");

        Assert.True(disconnected);
        Assert.Empty(flowsheet.Pipes);
        Assert.Null(pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId);
        Assert.Null(stream.Ports.Single(port => port.Name == "Inlet").ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void TryDisconnectPort_WhenNoPipeExists_ShouldReturnFalseAndLeaveTopologyUnchanged()
    {
        var (project, flowsheet, _, editService) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101" };
        AddEquipment(project, flowsheet, pump);

        var disconnected = editService.TryDisconnectPort(project, flowsheet, pump, "Discharge");

        Assert.False(disconnected);
        Assert.Empty(flowsheet.Pipes);
        Assert.Null(pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void TryConnect_WhenEquipmentToEquipmentIsValid_ShouldCreateIntermediateStreamAndTwoPipes()
    {
        var (project, flowsheet, connectionService, editService) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101", X = 100, Y = 100 };
        var valve = new ControlValveVisualElement { Name = "V-101", X = 300, Y = 100 };
        AddEquipment(project, flowsheet, pump);
        AddEquipment(project, flowsheet, valve);

        var returnedPipe = editService.TryConnect(flowsheet, connectionService, pump, "Discharge", valve, "Inlet", 0, 0);

        Assert.NotNull(returnedPipe);

        var intermediateStream = Assert.Single(project.EquipmentRegistry.AllEquipments.OfType<StreamVisualElement>());
        Assert.Single(project.SimulationService.Solver.Streams);
        Assert.Same(intermediateStream.Facade, project.SimulationService.Solver.Streams[0]);
        Assert.NotNull(flowsheet.GetElementReference(intermediateStream.Id));

        Assert.Equal(2, flowsheet.Pipes.Count);
        Assert.Contains(flowsheet.Pipes, pipe =>
            pipe.SourceElementId == pump.Id &&
            pipe.TargetElementId == intermediateStream.Id &&
            pipe.SourcePortName == "Discharge" &&
            pipe.TargetPortName == "Inlet");
        Assert.Contains(flowsheet.Pipes, pipe =>
            pipe.SourceElementId == intermediateStream.Id &&
            pipe.TargetElementId == valve.Id &&
            pipe.SourcePortName == "Outlet" &&
            pipe.TargetPortName == "Inlet");

        Assert.Equal(intermediateStream.Id, pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId);
        Assert.Equal(valve.Id, intermediateStream.Ports.Single(port => port.Name == "Outlet").ConnectedElementId);
        Assert.Equal(intermediateStream.Id, valve.Ports.Single(port => port.Name == "Inlet").ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void TryConnect_WhenEquipmentToEquipmentIsRequestedAgain_ShouldNotCreateDuplicateIntermediateStream()
    {
        var (project, flowsheet, connectionService, editService) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101", X = 100, Y = 100 };
        var valve = new ControlValveVisualElement { Name = "V-101", X = 300, Y = 100 };
        AddEquipment(project, flowsheet, pump);
        AddEquipment(project, flowsheet, valve);
        editService.TryConnect(flowsheet, connectionService, pump, "Discharge", valve, "Inlet", 0, 0);

        var duplicatePipe = editService.TryConnect(flowsheet, connectionService, pump, "Discharge", valve, "Inlet", 0, 0);

        Assert.Null(duplicatePipe);
        Assert.Single(project.EquipmentRegistry.AllEquipments.OfType<StreamVisualElement>());
        Assert.Single(project.SimulationService.Solver.Streams);
        Assert.Equal(2, flowsheet.Pipes.Count);
    }

    private static (Project Project, IFlowsheet Flowsheet, IConnectionService ConnectionService, FlowsheetConnectionEditService EditService) CreateContext()
    {
        var owner = new User(Guid.Parse("e90df25b-b57f-49d5-9dfa-71978799814e"), "test@example.com", "Test", "User", false);
        var project = new Project("Connection edit test", owner);
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var connectionService = new ConnectionService(
            new PfdConnectionRules(),
            new PlacementRules(),
            new EquipmentNamingService(),
            flowsheet.TypeDefinition.EquipmentFactory,
            project.SimulationService);

        return (project, flowsheet, connectionService, new FlowsheetConnectionEditService());
    }

    private static void AddEquipment(Project project, IFlowsheet flowsheet, IVisualElement element)
    {
        project.AddEquipment(element);
        flowsheet.AddElementReference(new FlowsheetElementReference(element.Id, element.X, element.Y));
    }

}
