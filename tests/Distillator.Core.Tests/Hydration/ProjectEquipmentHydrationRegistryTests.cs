using Distillator.Domain.Models;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;

namespace Distillator.Core.Tests.Hydration;

public sealed class ProjectEquipmentHydrationRegistryTests
{
    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void TryRegister_WhenElementIsStream_ShouldRegisterRegistryAndSolverOnce()
    {
        var project = CreateProject();
        var registry = new ProjectEquipmentHydrationRegistry();
        var stream = new StreamVisualElement();

        var firstResult = registry.TryRegister(project, stream);
        var secondResult = registry.TryRegister(project, stream);

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.Same(stream, project.EquipmentRegistry.GetById(stream.Id));
        Assert.Single(project.EquipmentRegistry.AllEquipments);
        Assert.Single(project.SimulationService.Solver.Streams);
        Assert.Same(stream.Facade, project.SimulationService.Solver.Streams[0]);
    }

    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "11")]
    [Trait("Level", "Unit")]
    public void TryRegister_WhenElementIsEquipment_ShouldRegisterRegistryAndSolverOnce()
    {
        var project = CreateProject();
        var registry = new ProjectEquipmentHydrationRegistry();
        var pump = new PumpVisualElement();

        var firstResult = registry.TryRegister(project, pump);
        var secondResult = registry.TryRegister(project, pump);

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.Same(pump, project.EquipmentRegistry.GetById(pump.Id));
        Assert.Single(project.EquipmentRegistry.AllEquipments);
        Assert.Single(project.SimulationService.Solver.Equipments);
        Assert.Same(pump.Facade, project.SimulationService.Solver.Equipments[0]);
    }

    private static Project CreateProject()
    {
        var owner = new User(Guid.Parse("e90df25b-b57f-49d5-9dfa-71978799814e"), "test@example.com", "Test", "User", false);
        return new Project("Hydration registry test", owner);
    }
}
