using Distillator.Domain.Models;
using Shared.ProcessFlowDiagram.Columns;
using Shared.ProcessFlowDiagram.Helpers;
using Shared.ProcessFlowDiagram.Vessels;

namespace Distillator.Domain.Services;

public sealed record PipeHydrationSnapshot(
    Guid Id,
    Guid SourceElementId,
    Guid TargetElementId,
    string SourcePortName,
    string TargetPortName);

public sealed class ProjectPipeHydrationService
{
    public bool TryRestore(Project project, IFlowsheet flowsheet, PipeHydrationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(flowsheet);

        var source = project.EquipmentRegistry.GetById(snapshot.SourceElementId);
        var target = project.EquipmentRegistry.GetById(snapshot.TargetElementId);
        if (source == null || target == null) return false;
        if (string.IsNullOrWhiteSpace(snapshot.SourcePortName) ||
            string.IsNullOrWhiteSpace(snapshot.TargetPortName))
        {
            return false;
        }

        EnsureDynamicPortForHydration(source, snapshot.SourcePortName);
        EnsureDynamicPortForHydration(target, snapshot.TargetPortName);

        if (!source.Connect(snapshot.SourcePortName, target, snapshot.TargetPortName))
        {
            return false;
        }

        flowsheet.AddPipe(new PipeReference(
            snapshot.SourceElementId,
            snapshot.TargetElementId,
            snapshot.SourcePortName,
            snapshot.TargetPortName,
            snapshot.Id == Guid.Empty ? null : snapshot.Id));
        return true;
    }

    private static void EnsureDynamicPortForHydration(Shared.ProcessFlowDiagram.IVisualElement element, string portName)
    {
        switch (element)
        {
            case StreamMixerVisualElement mixer:
                mixer.EnsureDynamicInletPort(portName);
                break;
            case SplitterVisualElement splitter:
                splitter.EnsureDynamicOutletPort(portName);
                break;
            case VesselVisualElement vessel:
                vessel.EnsureDynamicPort(portName);
                break;
            case ColumnVisualElement column:
                column.EnsureDynamicPort(portName);
                break;
        }
    }
}
