using System.Globalization;

namespace Client.Services.ProjectWorkspace;

/// <summary>
/// Servicio de UI que genera las clases CSS y los estilos del workspace.
/// No contiene lógica de dominio; solo convierte el estado de cámara en strings CSS.
/// </summary>
public class FlowsheetStyleService
{
    public string GetWorkspaceCssClass(bool isPanning) =>
        isPanning ? "pfd-workspace is-panning" : "pfd-workspace";

    public string GetWorkspaceBackgroundStyle(double panX, double panY, double zoom) => string.Create(
        CultureInfo.InvariantCulture,
        $"background-position: {Math.Round(panX)}px {Math.Round(panY)}px; background-size: {100 * zoom}px {100 * zoom}px, {100 * zoom}px {100 * zoom}px, {20 * zoom}px {20 * zoom}px, {20 * zoom}px {20 * zoom}px;");
}
