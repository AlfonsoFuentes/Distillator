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

    public bool TryDeleteEquipment(
        Project project,
        IFlowsheet flowsheet,
        IVisualElement element,
        out IReadOnlyCollection<IFlowsheet> affectedFlowsheets)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(flowsheet);
        ArgumentNullException.ThrowIfNull(element);

        affectedFlowsheets = Array.Empty<IFlowsheet>();
        if (project.EquipmentRegistry.GetById(element.Id) == null ||
            flowsheet.GetElementReference(element.Id) == null)
        {
            return false;
        }

        var connectedPipeIds = flowsheet.Pipes
            .Where(pipe => pipe.SourceElementId == element.Id || pipe.TargetElementId == element.Id)
            .Select(pipe => pipe.Id)
            .ToList();

        var affected = new Dictionary<Guid, IFlowsheet>
        {
            [flowsheet.Id] = flowsheet
        };

        foreach (var pipeId in connectedPipeIds)
        {
            var pipe = flowsheet.GetPipe(pipeId);
            if (pipe == null)
            {
                continue;
            }

            foreach (var affectedFlowsheet in GetAffectedFlowsheets(project, flowsheet, pipe))
            {
                affected[affectedFlowsheet.Id] = affectedFlowsheet;
            }

            DisconnectPipe(project, flowsheet, element, pipe);
        }

        if (flowsheet.Pipes.Any(pipe => pipe.SourceElementId == element.Id || pipe.TargetElementId == element.Id))
        {
            return false;
        }

        flowsheet.RemoveElementReference(element.Id);
        project.RemoveEquipment(element.Id);
        affectedFlowsheets = affected.Values.ToArray();
        return true;
    }

    private static void DisconnectPipe(Project project, IFlowsheet flowsheet, IVisualElement element, IPipeReference pipe)
    {
        if (element is OffPageConnectorElement)
        {
            var endpoint = pipe.SourceElementId == element.Id
                ? project.EquipmentRegistry.GetById(pipe.TargetElementId)
                : project.EquipmentRegistry.GetById(pipe.SourceElementId);
            var endpointPortName = pipe.SourceElementId == element.Id
                ? pipe.TargetPortName
                : pipe.SourcePortName;

            if (endpoint != null)
            {
                project.SimulationService.DisconnectPort(project, flowsheet, endpoint, endpointPortName);
                return;
            }
        }

        var elementPortName = pipe.SourceElementId == element.Id
            ? pipe.SourcePortName
            : pipe.TargetPortName;
        project.SimulationService.DisconnectPort(project, flowsheet, element, elementPortName);
    }

    private static IReadOnlyCollection<IFlowsheet> GetAffectedFlowsheets(Project project, IFlowsheet flowsheet, IPipeReference pipe)
    {
        var affected = new Dictionary<Guid, IFlowsheet>
        {
            [flowsheet.Id] = flowsheet
        };

        var sourceElement = project.EquipmentRegistry.GetById(pipe.SourceElementId);
        var targetElement = project.EquipmentRegistry.GetById(pipe.TargetElementId);
        var connector = sourceElement as OffPageConnectorElement ?? targetElement as OffPageConnectorElement;
        if (connector?.TargetAreaId is { } targetFlowsheetId)
        {
            var targetFlowsheet = project.GetFlowsheet(targetFlowsheetId);
            if (targetFlowsheet != null)
            {
                affected[targetFlowsheet.Id] = targetFlowsheet;
            }
        }

        return affected.Values.ToArray();
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
