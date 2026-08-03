using Distillator.Domain.Models;
using Shared.ProcessFlowDiagram;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Distillator.Domain.Services;

public sealed class ProjectEquipmentHydrationRegistry
{
    public bool TryRegister(Project project, IVisualElement element)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(element);

        if (project.EquipmentRegistry.GetById(element.Id) != null)
        {
            return false;
        }

        project.AddEquipment(element);
        RegisterFacadeInSolver(project, element);
        return true;
    }

    private static void RegisterFacadeInSolver(Project project, IVisualElement element)
    {
        if (element.Facade is IFacadeStream stream)
        {
            if (!project.SimulationService.Solver.Streams.Contains(stream))
            {
                project.SimulationService.Solver.AddStream(stream);
            }
        }
        else if (element.Facade is ISolverEquipment equipment &&
                 !project.SimulationService.Solver.Equipments.Contains(equipment))
        {
            project.SimulationService.Solver.AddEquipment(equipment);
        }
    }
}
