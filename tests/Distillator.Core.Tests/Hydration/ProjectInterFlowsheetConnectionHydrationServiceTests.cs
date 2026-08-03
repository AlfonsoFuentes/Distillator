using Distillator.Domain.Models;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;

namespace Distillator.Core.Tests.Hydration;

public sealed class ProjectInterFlowsheetConnectionHydrationServiceTests
{
    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void Restore_WhenReciprocalOpcPairExists_ShouldCreateOneLogicalConnectionAndAttachFacade()
    {
        var context = CreateHydratedConnectionContext();
        context.Project.ReorderFlowsheet(context.TargetFlowsheet, 0);

        var restored = new ProjectInterFlowsheetConnectionHydrationService().Restore(context.Project);

        Assert.Equal(1, restored);
        var connection = Assert.Single(context.Project.InterFlowsheetConnections);
        Assert.Contains(context.SourceOpc.Id, new[] { connection.SourceConnectorId, connection.TargetConnectorId });
        Assert.Contains(context.TargetOpc.Id, new[] { connection.SourceConnectorId, connection.TargetConnectorId });
        Assert.Same(context.RemoteStream.Facade, context.Pump.GetConnectedStream(PumpVisualElement.PortDischargeName));
    }

    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void Restore_WhenCalledTwice_ShouldNotDuplicateLogicalConnection()
    {
        var context = CreateHydratedConnectionContext();
        var service = new ProjectInterFlowsheetConnectionHydrationService();

        service.Restore(context.Project);
        var restoredAgain = service.Restore(context.Project);

        Assert.Equal(0, restoredAgain);
        Assert.Single(context.Project.InterFlowsheetConnections);
    }

    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void Restore_WhenTwinConnectorIsMissing_ShouldNotCreatePartialConnection()
    {
        var context = CreateHydratedConnectionContext(addTargetConnectorReference: false);

        var restored = new ProjectInterFlowsheetConnectionHydrationService().Restore(context.Project);

        Assert.Equal(0, restored);
        Assert.Empty(context.Project.InterFlowsheetConnections);
        Assert.Null(context.Pump.GetConnectedStream(PumpVisualElement.PortDischargeName));
    }

    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "05")]
    [Trait("Level", "Unit")]
    public void Restore_WhenConnectorsAreNotReciprocal_ShouldNotCreatePartialConnection()
    {
        var context = CreateHydratedConnectionContext(reciprocalTarget: false);

        var restored = new ProjectInterFlowsheetConnectionHydrationService().Restore(context.Project);

        Assert.Equal(0, restored);
        Assert.Empty(context.Project.InterFlowsheetConnections);
        Assert.Null(context.Pump.GetConnectedStream(PumpVisualElement.PortDischargeName));
    }

    private static HydratedConnectionContext CreateHydratedConnectionContext(
        bool addTargetConnectorReference = true,
        bool reciprocalTarget = true)
    {
        var owner = new User(Guid.Parse("042d20a4-a7bb-47c4-953b-f27063773f89"), "test@example.com", "Test", "User", false);
        var project = new Project("Interdiagram hydration test", owner);
        var sourceFlowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var targetFlowsheet = project.CreateFlowsheet("PFD 2", "PFD");
        var pump = new PumpVisualElement { Name = "P-101" };
        var remoteStream = new StreamVisualElement { Name = "S-201" };
        var sourceOpc = new OffPageConnectorElement(true) { Name = "OPC-1" };
        var targetOpc = new OffPageConnectorElement(false) { Name = "OPC-1" };

        AddEquipment(project, sourceFlowsheet, pump);
        AddEquipment(project, targetFlowsheet, remoteStream);
        AddEquipment(project, sourceFlowsheet, sourceOpc, new OffPageConnectorReference(sourceOpc.Id, 60, 80, true)
        {
            TargetFlowsheetId = targetFlowsheet.Id,
            TargetConnectorId = targetOpc.Id,
            TargetFlowsheetName = targetFlowsheet.Name,
            ConnectedEquipmentName = pump.Name
        });

        if (addTargetConnectorReference)
        {
            AddEquipment(project, targetFlowsheet, targetOpc, new OffPageConnectorReference(targetOpc.Id, 60, 140, false)
            {
                TargetFlowsheetId = sourceFlowsheet.Id,
                TargetConnectorId = reciprocalTarget ? sourceOpc.Id : Guid.NewGuid(),
                TargetFlowsheetName = sourceFlowsheet.Name,
                ConnectedEquipmentName = remoteStream.Name
            });
        }
        else
        {
            project.AddEquipment(targetOpc);
        }

        sourceFlowsheet.AddPipe(new PipeReference(
            pump.Id,
            sourceOpc.Id,
            PumpVisualElement.PortDischargeName,
            "Transfer"));
        targetFlowsheet.AddPipe(new PipeReference(
            targetOpc.Id,
            remoteStream.Id,
            "Transfer",
            "Inlet"));

        return new HydratedConnectionContext(
            project,
            sourceFlowsheet,
            targetFlowsheet,
            pump,
            remoteStream,
            sourceOpc,
            targetOpc);
    }

    private static void AddEquipment(Project project, IFlowsheet flowsheet, IVisualElement element, IFlowsheetElementReference? reference = null)
    {
        project.AddEquipment(element);
        flowsheet.AddElementReference(reference ?? new FlowsheetElementReference(element.Id, element.X, element.Y));
    }

    private sealed record HydratedConnectionContext(
        Project Project,
        IFlowsheet SourceFlowsheet,
        IFlowsheet TargetFlowsheet,
        PumpVisualElement Pump,
        StreamVisualElement RemoteStream,
        OffPageConnectorElement SourceOpc,
        OffPageConnectorElement TargetOpc);
}
