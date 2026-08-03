using Distillator.Domain.Models;
using Distillator.Domain.Policies;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;

namespace Distillator.Core.Tests.Topology;

public sealed class InterFlowsheetConnectionServiceTests
{
    [Fact]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void CreateInterFlowsheetConnection_WhenValid_ShouldCreateReciprocalOpcArtifacts()
    {
        var (project, sourceFlowsheet, targetFlowsheet, service) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101", X = 120, Y = 80 };
        var remoteStream = new StreamVisualElement { Name = "S-201", X = 240, Y = 140 };
        AddEquipment(project, sourceFlowsheet, pump);
        AddEquipment(project, targetFlowsheet, remoteStream);

        var connection = service.CreateInterFlowsheetConnection(
            project,
            sourceFlowsheet,
            pump,
            "Discharge",
            targetFlowsheet,
            remoteStream);

        Assert.NotNull(connection);
        Assert.Single(project.InterFlowsheetConnections);
        Assert.Equal(2, project.EquipmentRegistry.AllEquipments.OfType<OffPageConnectorElement>().Count());
        Assert.Single(sourceFlowsheet.Elements.OfType<IOffPageConnectorReference>());
        Assert.Single(targetFlowsheet.Elements.OfType<IOffPageConnectorReference>());
        Assert.Equal(OffPageConnectorPortSide.Left, sourceFlowsheet.Elements.OfType<IOffPageConnectorReference>().Single().PortSide);
        Assert.Equal(OffPageConnectorPortSide.Right, targetFlowsheet.Elements.OfType<IOffPageConnectorReference>().Single().PortSide);
        Assert.Single(sourceFlowsheet.Pipes);
        Assert.Single(targetFlowsheet.Pipes);
        Assert.Equal(connection.SourceConnectorId, pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId);
        Assert.Equal(connection.TargetConnectorId, remoteStream.Ports.Single(port => port.Name == "Inlet").ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void CreateInterFlowsheetConnection_WhenLocalPortIsOccupied_ShouldLeaveBothFlowsheetsUnchanged()
    {
        var (project, sourceFlowsheet, targetFlowsheet, service) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101" };
        var remoteStream = new StreamVisualElement { Name = "S-201" };
        var existingElementId = Guid.Parse("b3411de1-bdfa-43c0-8236-1ef53fe9d34b");
        AddEquipment(project, sourceFlowsheet, pump);
        AddEquipment(project, targetFlowsheet, remoteStream);
        pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId = existingElementId;

        var connection = service.CreateInterFlowsheetConnection(
            project,
            sourceFlowsheet,
            pump,
            "Discharge",
            targetFlowsheet,
            remoteStream);

        Assert.Null(connection);
        Assert.Empty(project.InterFlowsheetConnections);
        Assert.Empty(project.EquipmentRegistry.AllEquipments.OfType<OffPageConnectorElement>());
        Assert.DoesNotContain(sourceFlowsheet.Elements, reference => reference is IOffPageConnectorReference);
        Assert.DoesNotContain(targetFlowsheet.Elements, reference => reference is IOffPageConnectorReference);
        Assert.Empty(sourceFlowsheet.Pipes);
        Assert.Empty(targetFlowsheet.Pipes);
        Assert.Equal(existingElementId, pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId);
        Assert.Null(remoteStream.Ports.Single(port => port.Name == "Inlet").ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void CreateInterFlowsheetConnection_WhenRemoteStreamPortIsOccupied_ShouldLeaveBothFlowsheetsUnchanged()
    {
        var (project, sourceFlowsheet, targetFlowsheet, service) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101" };
        var remoteStream = new StreamVisualElement { Name = "S-201" };
        var existingElementId = Guid.Parse("3e7354a7-5276-4bec-8418-5fe990b57382");
        AddEquipment(project, sourceFlowsheet, pump);
        AddEquipment(project, targetFlowsheet, remoteStream);
        remoteStream.Ports.Single(port => port.Name == "Inlet").ConnectedElementId = existingElementId;

        var connection = service.CreateInterFlowsheetConnection(
            project,
            sourceFlowsheet,
            pump,
            "Discharge",
            targetFlowsheet,
            remoteStream);

        Assert.Null(connection);
        Assert.Empty(project.InterFlowsheetConnections);
        Assert.Empty(project.EquipmentRegistry.AllEquipments.OfType<OffPageConnectorElement>());
        Assert.DoesNotContain(sourceFlowsheet.Elements, reference => reference is IOffPageConnectorReference);
        Assert.DoesNotContain(targetFlowsheet.Elements, reference => reference is IOffPageConnectorReference);
        Assert.Empty(sourceFlowsheet.Pipes);
        Assert.Empty(targetFlowsheet.Pipes);
        Assert.Null(pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId);
        Assert.Equal(existingElementId, remoteStream.Ports.Single(port => port.Name == "Inlet").ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void TryDisconnectPort_WhenDisconnectedFromSourceEnd_ShouldRemoveBothOpcEnds()
    {
        var (project, sourceFlowsheet, targetFlowsheet, service) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101" };
        var remoteStream = new StreamVisualElement { Name = "S-201" };
        AddEquipment(project, sourceFlowsheet, pump);
        AddEquipment(project, targetFlowsheet, remoteStream);
        service.CreateInterFlowsheetConnection(project, sourceFlowsheet, pump, "Discharge", targetFlowsheet, remoteStream);

        var disconnected = new FlowsheetConnectionEditService().TryDisconnectPort(
            project,
            sourceFlowsheet,
            pump,
            "Discharge",
            out var affectedFlowsheets);

        Assert.True(disconnected);
        AssertInterFlowsheetConnectionRemoved(project, sourceFlowsheet, targetFlowsheet, pump, remoteStream);
        Assert.Equal(
            new[] { sourceFlowsheet.Id, targetFlowsheet.Id }.OrderBy(id => id),
            affectedFlowsheets.Select(flowsheet => flowsheet.Id).OrderBy(id => id));
    }

    [Fact]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void TryDisconnectPort_WhenDisconnectedFromRemoteEnd_ShouldRemoveBothOpcEnds()
    {
        var (project, sourceFlowsheet, targetFlowsheet, service) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101" };
        var remoteStream = new StreamVisualElement { Name = "S-201" };
        AddEquipment(project, sourceFlowsheet, pump);
        AddEquipment(project, targetFlowsheet, remoteStream);
        service.CreateInterFlowsheetConnection(project, sourceFlowsheet, pump, "Discharge", targetFlowsheet, remoteStream);

        var disconnected = new FlowsheetConnectionEditService().TryDisconnectPort(
            project,
            targetFlowsheet,
            remoteStream,
            "Inlet",
            out var affectedFlowsheets);

        Assert.True(disconnected);
        AssertInterFlowsheetConnectionRemoved(project, sourceFlowsheet, targetFlowsheet, pump, remoteStream);
        Assert.Equal(
            new[] { sourceFlowsheet.Id, targetFlowsheet.Id }.OrderBy(id => id),
            affectedFlowsheets.Select(flowsheet => flowsheet.Id).OrderBy(id => id));
    }

    [Fact]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void RemoveFlowsheet_WhenSourceDiagramIsDeleted_ShouldCleanSurvivingDiagram()
    {
        var (project, sourceFlowsheet, targetFlowsheet, service) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101" };
        var remoteStream = new StreamVisualElement { Name = "S-201" };
        AddEquipment(project, sourceFlowsheet, pump);
        AddEquipment(project, targetFlowsheet, remoteStream);
        service.CreateInterFlowsheetConnection(project, sourceFlowsheet, pump, "Discharge", targetFlowsheet, remoteStream);

        project.RemoveFlowsheet(sourceFlowsheet.Id);

        Assert.DoesNotContain(project.Flowsheets, flowsheet => flowsheet.Id == sourceFlowsheet.Id);
        Assert.Empty(project.InterFlowsheetConnections);
        Assert.Empty(project.EquipmentRegistry.AllEquipments.OfType<OffPageConnectorElement>());
        Assert.DoesNotContain(targetFlowsheet.Elements, reference => reference is IOffPageConnectorReference);
        Assert.Empty(targetFlowsheet.Pipes);
        Assert.Null(remoteStream.Ports.Single(port => port.Name == "Inlet").ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void RemoveFlowsheet_WhenTargetDiagramIsDeleted_ShouldCleanSurvivingDiagram()
    {
        var (project, sourceFlowsheet, targetFlowsheet, service) = CreateContext();
        var pump = new PumpVisualElement { Name = "P-101" };
        var remoteStream = new StreamVisualElement { Name = "S-201" };
        AddEquipment(project, sourceFlowsheet, pump);
        AddEquipment(project, targetFlowsheet, remoteStream);
        service.CreateInterFlowsheetConnection(project, sourceFlowsheet, pump, "Discharge", targetFlowsheet, remoteStream);

        project.RemoveFlowsheet(targetFlowsheet.Id);

        Assert.DoesNotContain(project.Flowsheets, flowsheet => flowsheet.Id == targetFlowsheet.Id);
        Assert.Empty(project.InterFlowsheetConnections);
        Assert.Empty(project.EquipmentRegistry.AllEquipments.OfType<OffPageConnectorElement>());
        Assert.DoesNotContain(sourceFlowsheet.Elements, reference => reference is IOffPageConnectorReference);
        Assert.Empty(sourceFlowsheet.Pipes);
        Assert.Null(pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId);
        Assert.Null(pump.GetConnectedStream(PumpVisualElement.PortDischargeName));
    }

    private static (Project Project, IFlowsheet SourceFlowsheet, IFlowsheet TargetFlowsheet, InterFlowsheetConnectionService Service) CreateContext()
    {
        var owner = new User(Guid.Parse("409d5830-f873-48be-b72a-6cd7ef3b66dc"), "test@example.com", "Test", "User", false);
        var project = new Project("Interdiagram connection test", owner);
        var sourceFlowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var targetFlowsheet = project.CreateFlowsheet("PFD 2", "PFD");
        var service = new InterFlowsheetConnectionService(
            new PlacementRules(),
            new EquipmentNamingService(),
            project.SimulationService);

        return (project, sourceFlowsheet, targetFlowsheet, service);
    }

    private static void AddEquipment(Project project, IFlowsheet flowsheet, IVisualElement element)
    {
        project.AddEquipment(element);
        flowsheet.AddElementReference(new FlowsheetElementReference(element.Id, element.X, element.Y));
    }

    private static void AssertInterFlowsheetConnectionRemoved(
        Project project,
        IFlowsheet sourceFlowsheet,
        IFlowsheet targetFlowsheet,
        PumpVisualElement pump,
        StreamVisualElement remoteStream)
    {
        Assert.Empty(project.InterFlowsheetConnections);
        Assert.Empty(project.EquipmentRegistry.AllEquipments.OfType<OffPageConnectorElement>());
        Assert.DoesNotContain(sourceFlowsheet.Elements, reference => reference is IOffPageConnectorReference);
        Assert.DoesNotContain(targetFlowsheet.Elements, reference => reference is IOffPageConnectorReference);
        Assert.Empty(sourceFlowsheet.Pipes);
        Assert.Empty(targetFlowsheet.Pipes);
        Assert.Null(pump.Ports.Single(port => port.Name == "Discharge").ConnectedElementId);
        Assert.Null(remoteStream.Ports.Single(port => port.Name == "Inlet").ConnectedElementId);
        Assert.Null(pump.GetConnectedStream(PumpVisualElement.PortDischargeName));
    }
}
