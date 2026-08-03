using System.Text.Json;

namespace Shared.Projects;

public sealed record ProjectDiagramAuditSummary(
    Guid Id,
    string Name,
    string TypeCode,
    string? DiagramNumber,
    int Order,
    int ElementCount,
    int PipeCount,
    int CanvasJsonLength);

public static class ProjectDiagramAudit
{
    public static ProjectDiagramAuditSummary Summarize(ProjectDiagramDto diagram)
    {
        var (elementCount, pipeCount) = CountCanvasItems(diagram.CanvasStateJson);
        return new ProjectDiagramAuditSummary(
            diagram.Id,
            diagram.Name,
            diagram.TypeCode,
            diagram.DiagramNumber,
            diagram.Order,
            elementCount,
            pipeCount,
            diagram.CanvasStateJson?.Length ?? 0);
    }

    private static (int ElementCount, int PipeCount) CountCanvasItems(string? canvasStateJson)
    {
        if (string.IsNullOrWhiteSpace(canvasStateJson))
        {
            return (0, 0);
        }

        try
        {
            using var document = JsonDocument.Parse(canvasStateJson);
            var root = document.RootElement;
            return (
                CountArray(root, "elements"),
                CountArray(root, "pipes"));
        }
        catch (JsonException)
        {
            return (0, 0);
        }
    }

    private static int CountArray(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Array
            ? property.GetArrayLength()
            : 0;
    }
}
