using System.Text.Json;
using Distillator.Domain.Models;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram;
using Shared.Projects;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;

namespace Client.Services;

internal static class ProjectDiagramDocumentMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static List<ProjectDiagramDto> ToDiagramDtos(Project project)
    {
        return project.Flowsheets
            .Select(ToDiagramDto)
            .ToList();
    }

    public static ProjectDiagramDto ToDiagramDto(IFlowsheet flowsheet, int index)
    {
        return new ProjectDiagramDto
        {
            Id = flowsheet.Id,
            Name = flowsheet.Name,
            TypeCode = flowsheet.TypeCode,
            DiagramNumber = string.IsNullOrWhiteSpace(flowsheet.DiagramNumber) ? null : flowsheet.DiagramNumber,
            Order = index < 0 ? 0 : index,
            CanvasStateJson = Serialize(ToCanvasState(flowsheet.Project, flowsheet))
        };
    }

    private static DiagramCanvasStateSnapshot ToCanvasState(IProject project, IFlowsheet flowsheet)
    {
        return new DiagramCanvasStateSnapshot(
            new DiagramCameraSnapshot(
                flowsheet.Zoom,
                flowsheet.PanX,
                flowsheet.PanY,
                flowsheet.DiagramWidth,
                flowsheet.DiagramHeight,
                flowsheet.GridSize,
                flowsheet.GlobalScale),
            flowsheet.Elements
                .Select(reference =>
                {
                    var element = project.EquipmentRegistry.GetById(reference.ElementId);
                    return element == null
                        ? null
                        : new DiagramElementSnapshot(
                            element.Id,
                            element.Type.ToString(),
                            element.Name,
                            element.Label,
                            FacadeStateSerializer.Serialize(
                                element.Facade,
                                excludedVariables: GetFormulaSpecificationTargetVariables(element.Facade)),
                            ToFormulaSpecificationSnapshots(element.Facade),
                            reference.X,
                            reference.Y,
                            element.Width,
                            element.Height,
                            reference.RotationAngle,
                            reference.ZIndex,
                            reference.IsFlippedHorizontal,
                            reference.IsFlippedVertical,
                            element.ShowLabel,
                            element.IsLocked,
                            ToOffPageConnectorSnapshot(reference, element));
                })
                .Where(snapshot => snapshot != null)
                .Cast<DiagramElementSnapshot>()
                .ToList(),
            flowsheet.Pipes
                .Select(pipe => new DiagramPipeSnapshot(
                    pipe.Id,
                    pipe.SourceElementId,
                    pipe.TargetElementId,
                    pipe.SourcePortName,
                    pipe.TargetPortName))
                .ToList());
    }

    private static OffPageConnectorSnapshot? ToOffPageConnectorSnapshot(
        IFlowsheetElementReference reference,
        IVisualElement element)
    {
        if (reference is IOffPageConnectorReference connectorReference)
        {
            return new OffPageConnectorSnapshot(
                connectorReference.TargetFlowsheetId,
                connectorReference.TargetConnectorId,
                connectorReference.TargetFlowsheetName,
                connectorReference.ConnectedEquipmentName,
                connectorReference.IsOutlet,
                connectorReference.PortSide);
        }

        return element is OffPageConnectorElement connector
            ? new OffPageConnectorSnapshot(
                connector.TargetAreaId,
                connector.TargetConnectorId,
                connector.TargetAreaName,
                connector.ConnectedEquipmentName,
                connector.IsOutlet,
                connector.PortSide)
            : null;
    }

    private static List<FormulaSpecificationSnapshot> ToFormulaSpecificationSnapshots(IFacade? facade)
    {
        return facade is not SolverEquipmentBase equipment
            ? new List<FormulaSpecificationSnapshot>()
            : equipment.Specifications
                .OfType<FormulaSpecification>()
                .Select(specification => new FormulaSpecificationSnapshot(
                    specification.Id,
                    specification.Formula,
                    specification.DefinedByUserId,
                    specification.DefinedByUserName,
                    specification.DefinedAtUtc))
                .ToList();
    }

    private static IReadOnlyCollection<IVariable> GetFormulaSpecificationTargetVariables(IFacade? facade)
    {
        return facade is not SolverEquipmentBase equipment
            ? Array.Empty<IVariable>()
            : equipment.Specifications
                .OfType<FormulaSpecification>()
                .SelectMany(specification => specification.GetTargetVariables())
                .Distinct()
                .ToList();
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private sealed record DiagramCanvasStateSnapshot(
        DiagramCameraSnapshot Camera,
        List<DiagramElementSnapshot> Elements,
        List<DiagramPipeSnapshot> Pipes);

    private sealed record DiagramCameraSnapshot(
        double Zoom,
        double PanX,
        double PanY,
        double DiagramWidth,
        double DiagramHeight,
        double GridSize,
        double GlobalScale);

    private sealed record DiagramElementSnapshot(
        Guid Id,
        string Type,
        string Name,
        string Label,
        string? FacadeStateJson,
        List<FormulaSpecificationSnapshot>? FormulaSpecifications,
        double X,
        double Y,
        double Width,
        double Height,
        int RotationAngle,
        int ZIndex,
        bool IsFlippedHorizontal,
        bool IsFlippedVertical,
        bool ShowLabel,
        bool IsLocked,
        OffPageConnectorSnapshot? OffPageConnector = null);

    private sealed record OffPageConnectorSnapshot(
        Guid? TargetFlowsheetId,
        Guid? TargetConnectorId,
        string TargetFlowsheetName,
        string ConnectedEquipmentName,
        bool IsOutlet,
        OffPageConnectorPortSide? PortSide = null);

    private sealed record FormulaSpecificationSnapshot(
        Guid Id,
        string Formula,
        string? DefinedByUserId = null,
        string? DefinedByUserName = null,
        DateTime? DefinedAtUtc = null);

    private sealed record DiagramPipeSnapshot(
        Guid Id,
        Guid SourceElementId,
        Guid TargetElementId,
        string SourcePortName,
        string TargetPortName);
}
