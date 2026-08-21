using Distillator.Domain.Models;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;

namespace Distillator.Core.Tests.Topology;

public sealed class FlowsheetEquipmentEditServiceTests
{
    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void TryAddEquipment_ShouldRegisterRegistrySolverAndFlowsheetReferenceOnce()
    {
        var project = CreateProject();
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var service = new FlowsheetEquipmentEditService();
        var pump = new PumpVisualElement { X = 120, Y = 80, Name = "P-101" };

        var firstResult = service.TryAddEquipment(project, flowsheet, pump);
        var secondResult = service.TryAddEquipment(project, flowsheet, pump);

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.Same(pump, project.EquipmentRegistry.GetById(pump.Id));
        Assert.Single(project.EquipmentRegistry.AllEquipments);
        Assert.Single(project.SimulationService.Solver.Equipments);
        Assert.Same(pump.Facade, project.SimulationService.Solver.Equipments[0]);

        var reference = Assert.Single(flowsheet.Elements);
        Assert.Equal(pump.Id, reference.ElementId);
        Assert.Equal(120, reference.X);
        Assert.Equal(80, reference.Y);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void TryAddEquipment_WhenElementAlreadyHasReferenceWithoutRegistry_ShouldNotRegisterPartialEquipment()
    {
        var project = CreateProject();
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var service = new FlowsheetEquipmentEditService();
        var pump = new PumpVisualElement { X = 120, Y = 80, Name = "P-101" };
        flowsheet.AddElementReference(new FlowsheetElementReference(pump.Id, pump.X, pump.Y));

        var result = service.TryAddEquipment(project, flowsheet, pump);

        Assert.False(result);
        Assert.Null(project.EquipmentRegistry.GetById(pump.Id));
        Assert.Empty(project.EquipmentRegistry.AllEquipments);
        Assert.Empty(project.SimulationService.Solver.Equipments);
        Assert.Single(flowsheet.Elements);
    }

    [Fact]
    [Trait("Spec", "04")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void TryDeleteEquipment_ShouldDisconnectPipesAndRemoveRegistryReferenceAndSolverEntry()
    {
        var project = CreateProject();
        var flowsheet = project.CreateFlowsheet("PFD 1", "PFD");
        var service = new FlowsheetEquipmentEditService();
        var pump = new PumpVisualElement { X = 120, Y = 80, Name = "P-101" };
        var stream = new StreamVisualElement { X = 220, Y = 80, Name = "S-101" };
        service.TryAddEquipment(project, flowsheet, pump);
        service.TryAddEquipment(project, flowsheet, stream);
        var pipe = new PipeReference(pump.Id, stream.Id, "Discharge", "Inlet");
        flowsheet.AddPipe(pipe);

        var result = service.TryDeleteEquipment(project, flowsheet, pump, out var affectedFlowsheets);

        Assert.True(result);
        Assert.Equal(new[] { flowsheet.Id }, affectedFlowsheets.Select(candidate => candidate.Id));
        Assert.Null(project.EquipmentRegistry.GetById(pump.Id));
        Assert.DoesNotContain(project.SimulationService.Solver.Equipments, equipment => ReferenceEquals(equipment, pump.Facade));
        Assert.Null(flowsheet.GetElementReference(pump.Id));
        Assert.DoesNotContain(flowsheet.Pipes, candidate => candidate.SourceElementId == pump.Id || candidate.TargetElementId == pump.Id);

        Assert.Same(stream, project.EquipmentRegistry.GetById(stream.Id));
        Assert.NotNull(flowsheet.GetElementReference(stream.Id));
        Assert.Empty(project.SimulationService.Solver.Streams);
    }

    private static Project CreateProject()
    {
        var owner = new User(Guid.Parse("e90df25b-b57f-49d5-9dfa-71978799814e"), "test@example.com", "Test", "User", false);
        return new Project("Topology edit test", owner);
    }

}
