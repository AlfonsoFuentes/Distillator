using Distillator.Domain.Models;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;

namespace Distillator.Core.Tests.Hydration;

public sealed class ProjectPipeHydrationServiceTests
{
    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void TryRestore_WhenPipeIsValid_ShouldConnectPortsAndAddPipe()
    {
        var project = CreateProject();
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var registry = new ProjectEquipmentHydrationRegistry();
        var source = new StreamVisualElement();
        var target = new PumpVisualElement();
        registry.TryRegister(project, source);
        registry.TryRegister(project, target);

        var pipeId = Guid.NewGuid();
        var result = new ProjectPipeHydrationService().TryRestore(
            project,
            flowsheet,
            new PipeHydrationSnapshot(
                pipeId,
                source.Id,
                target.Id,
                "Outlet",
                PumpVisualElement.PortSuctionName));

        Assert.True(result);
        var pipe = Assert.Single(flowsheet.Pipes);
        Assert.Equal(pipeId, pipe.Id);
        Assert.Equal(target.Id, source.Ports.Single(port => port.Name == "Outlet").ConnectedElementId);
        Assert.Equal(source.Id, target.SuctionPort.ConnectedElementId);
    }

    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    public void TryRestore_WhenPortIsInvalid_ShouldNotAddPipe()
    {
        var project = CreateProject();
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var registry = new ProjectEquipmentHydrationRegistry();
        var source = new StreamVisualElement();
        var target = new PumpVisualElement();
        registry.TryRegister(project, source);
        registry.TryRegister(project, target);

        var result = new ProjectPipeHydrationService().TryRestore(
            project,
            flowsheet,
            new PipeHydrationSnapshot(
                Guid.NewGuid(),
                source.Id,
                target.Id,
                "Missing",
                PumpVisualElement.PortSuctionName));

        Assert.False(result);
        Assert.Empty(flowsheet.Pipes);
        Assert.Null(target.SuctionPort.ConnectedElementId);
    }

    private static Project CreateProject()
    {
        var owner = new User(Guid.Parse("e90df25b-b57f-49d5-9dfa-71978799814e"), "test@example.com", "Test", "User", false);
        return new Project("Pipe hydration test", owner);
    }
}
