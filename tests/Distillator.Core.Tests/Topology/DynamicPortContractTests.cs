using Shared.ProcessFlowDiagram.Helpers;
using Shared.ProcessFlowDiagram.Streams;
using Distillator.Domain.Models;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram.Columns;
using Shared.ProcessFlowDiagram.Vessels;

namespace Distillator.Core.Tests.Topology;

public sealed class DynamicPortContractTests
{
    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void Mixer_WhenFirstDynamicInletIsDisconnected_ShouldKeepSecondInletMappedToItsStream()
    {
        var mixer = new StreamMixerVisualElement();
        var stream1 = new StreamVisualElement { Name = "S-101" };
        var stream2 = new StreamVisualElement { Name = "S-102" };
        stream1.Connect("Outlet", mixer, "Inlet_1");
        stream2.Connect("Outlet", mixer, "Inlet_2");

        mixer.Disconnect("Inlet_1");

        Assert.Same(stream2.Facade, mixer.GetConnectedStream("Inlet_2"));
        Assert.Equal("Inlet_3", mixer.Ports.Where(port => port.Type == Shared.ProcessFlowDiagram.PortType.Inlet).Last().Name);

        mixer.Disconnect("Inlet_2");

        Assert.Null(mixer.GetConnectedStream("Inlet_2"));
        Assert.Empty(((Shared.SolverConsecutive.Equipments.SolverStreamMixer)mixer.Facade).Inlets);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void Splitter_WhenFirstDynamicOutletIsDisconnected_ShouldKeepSecondOutletMappedToItsStream()
    {
        var splitter = new SplitterVisualElement();
        var stream1 = new StreamVisualElement { Name = "S-101" };
        var stream2 = new StreamVisualElement { Name = "S-102" };
        splitter.Connect("Outlet_1", stream1, "Inlet");
        splitter.Connect("Outlet_2", stream2, "Inlet");

        splitter.Disconnect("Outlet_1");

        Assert.Same(stream2.Facade, splitter.GetConnectedStream("Outlet_2"));
        Assert.Equal("Outlet_3", splitter.Ports.Where(port => port.Type == Shared.ProcessFlowDiagram.PortType.Outlet).Last().Name);

        splitter.Disconnect("Outlet_2");

        Assert.Null(splitter.GetConnectedStream("Outlet_2"));
        Assert.Empty(((Shared.SolverConsecutive.Equipments.SolverSplitter)splitter.Facade).Outlets);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void PipeHydration_WhenMixerPipeTargetsSecondInlet_ShouldCreatePortBeforeConnecting()
    {
        var project = CreateProject();
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var mixer = new StreamMixerVisualElement { Name = "MX-101" };
        var stream = new StreamVisualElement { Name = "S-101" };
        AddEquipment(project, flowsheet, mixer);
        AddEquipment(project, flowsheet, stream);
        var service = new ProjectPipeHydrationService();

        var restored = service.TryRestore(project, flowsheet, new PipeHydrationSnapshot(
            Guid.NewGuid(),
            stream.Id,
            mixer.Id,
            "Outlet",
            "Inlet_2"));

        Assert.True(restored);
        Assert.Contains(mixer.Ports, port => port.Name == "Inlet_2");
        Assert.Same(stream.Facade, mixer.GetConnectedStream("Inlet_2"));
        Assert.NotEqual(0, mixer.Ports.Single(port => port.Name == "Inlet_2").OffsetY);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void PipeHydration_WhenSplitterPipeUsesSecondOutlet_ShouldCreatePortBeforeConnecting()
    {
        var project = CreateProject();
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var splitter = new SplitterVisualElement { Name = "SP-101" };
        var stream = new StreamVisualElement { Name = "S-101" };
        AddEquipment(project, flowsheet, splitter);
        AddEquipment(project, flowsheet, stream);
        var service = new ProjectPipeHydrationService();

        var restored = service.TryRestore(project, flowsheet, new PipeHydrationSnapshot(
            Guid.NewGuid(),
            splitter.Id,
            stream.Id,
            "Outlet_2",
            "Inlet"));

        Assert.True(restored);
        Assert.Contains(splitter.Ports, port => port.Name == "Outlet_2");
        Assert.Same(stream.Facade, splitter.GetConnectedStream("Outlet_2"));
        Assert.NotEqual(0, splitter.Ports.Single(port => port.Name == "Outlet_2").OffsetY);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void Vessel_WhenFirstDynamicInletIsDisconnected_ShouldKeepSecondInletMappedToItsStream()
    {
        var vessel = new VesselVisualElement();
        var stream1 = new StreamVisualElement { Name = "S-101" };
        var stream2 = new StreamVisualElement { Name = "S-102" };
        stream1.Connect("Outlet", vessel, "Inlet_1");
        stream2.Connect("Outlet", vessel, "Inlet_2");

        vessel.Disconnect("Inlet_1");

        Assert.Same(stream2.Facade, vessel.GetConnectedStream("Inlet_2"));

        vessel.Disconnect("Inlet_2");

        Assert.Null(vessel.GetConnectedStream("Inlet_2"));
        Assert.Empty(((Shared.SolverConsecutive.Equipments.SolverVessel)vessel.Facade).Inlets);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void Column_WhenFirstDynamicFeedIsDisconnected_ShouldKeepSecondFeedMappedToItsStream()
    {
        var column = new ColumnVisualElement();
        var stream1 = new StreamVisualElement { Name = "S-101" };
        var stream2 = new StreamVisualElement { Name = "S-102" };
        stream1.Connect("Outlet", column, "Feed_1");
        stream2.Connect("Outlet", column, "Feed_2");

        column.Disconnect("Feed_1");

        Assert.Same(stream2.Facade, column.GetConnectedStream("Feed_2"));

        column.Disconnect("Feed_2");

        Assert.Null(column.GetConnectedStream("Feed_2"));
        Assert.Empty(((Shared.SolverConsecutive.Equipments.Columns.SolverColumn)column.Facade).Feeds);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void PipeHydration_WhenVesselPipeTargetsSecondInlet_ShouldCreatePortBeforeConnecting()
    {
        var project = CreateProject();
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var vessel = new VesselVisualElement { Name = "V-101" };
        var stream = new StreamVisualElement { Name = "S-101" };
        AddEquipment(project, flowsheet, vessel);
        AddEquipment(project, flowsheet, stream);
        var service = new ProjectPipeHydrationService();

        var restored = service.TryRestore(project, flowsheet, new PipeHydrationSnapshot(
            Guid.NewGuid(),
            stream.Id,
            vessel.Id,
            "Outlet",
            "Inlet_2"));

        Assert.True(restored);
        Assert.Contains(vessel.Ports, port => port.Name == "Inlet_2");
        Assert.Same(stream.Facade, vessel.GetConnectedStream("Inlet_2"));
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void PipeHydration_WhenColumnPipeTargetsSecondFeed_ShouldCreatePortBeforeConnecting()
    {
        var project = CreateProject();
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var column = new ColumnVisualElement { Name = "T-101" };
        var stream = new StreamVisualElement { Name = "S-101" };
        AddEquipment(project, flowsheet, column);
        AddEquipment(project, flowsheet, stream);
        var service = new ProjectPipeHydrationService();

        var restored = service.TryRestore(project, flowsheet, new PipeHydrationSnapshot(
            Guid.NewGuid(),
            stream.Id,
            column.Id,
            "Outlet",
            "Feed_2"));

        Assert.True(restored);
        Assert.Contains(column.Ports, port => port.Name == "Feed_2");
        Assert.Same(stream.Facade, column.GetConnectedStream("Feed_2"));
    }

    private static Project CreateProject()
    {
        var owner = new User(Guid.Parse("e90df25b-b57f-49d5-9dfa-71978799814e"), "test@example.com", "Test", "User", false);
        return new Project("Dynamic port hydration test", owner);
    }

    private static void AddEquipment(Project project, IFlowsheet flowsheet, Shared.ProcessFlowDiagram.IVisualElement element)
    {
        project.AddEquipment(element);
        flowsheet.AddElementReference(new FlowsheetElementReference(element.Id, element.X, element.Y));
    }
}
