using Distillator.Domain.Models;
using Shared.ProcessFlowDiagram;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Distillator.Domain.Services;

public sealed class FlowsheetEquipmentEditService
{
    public bool TryAddEquipment(Project project, IFlowsheet flowsheet, IVisualElement element)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(flowsheet);
        ArgumentNullException.ThrowIfNull(element);

        if (project.EquipmentRegistry.GetById(element.Id) != null ||
            flowsheet.GetElementReference(element.Id) != null)
        {
            return false;
        }

        project.AddEquipment(element);
        RegisterFacadeInSolver(project, element);

        try
        {
            flowsheet.AddElementReference(new FlowsheetElementReference(element.Id, element.X, element.Y));
            return true;
        }
        catch
        {
            project.RemoveEquipment(element.Id);
            throw;
        }
    }

    public bool TryDeleteEquipment(Project project, IFlowsheet flowsheet, IVisualElement element, IConnectionService? connectionService)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(flowsheet);
        ArgumentNullException.ThrowIfNull(element);

        if (project.EquipmentRegistry.GetById(element.Id) == null ||
            flowsheet.GetElementReference(element.Id) == null)
        {
            return false;
        }

        var connectedPipeIds = flowsheet.Pipes
            .Where(pipe => pipe.SourceElementId == element.Id || pipe.TargetElementId == element.Id)
            .Select(pipe => pipe.Id)
            .ToList();

        if (connectedPipeIds.Count > 0 && connectionService == null)
        {
            return false;
        }

        foreach (var pipeId in connectedPipeIds)
        {
            connectionService!.Disconnect(flowsheet, pipeId);
        }

        if (flowsheet.Pipes.Any(pipe => pipe.SourceElementId == element.Id || pipe.TargetElementId == element.Id))
        {
            return false;
        }

        flowsheet.RemoveElementReference(element.Id);
        project.RemoveEquipment(element.Id);
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
