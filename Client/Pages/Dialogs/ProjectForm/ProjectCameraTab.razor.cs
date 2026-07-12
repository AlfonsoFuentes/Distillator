using Microsoft.AspNetCore.Components;

namespace Client.Pages.Dialogs.ProjectForm;

public partial class ProjectCameraTab
{
    [Parameter] public double DefaultZoom { get; set; }
    [Parameter] public EventCallback<double> DefaultZoomChanged { get; set; }
    [Parameter] public double GlobalScale { get; set; }
    [Parameter] public EventCallback<double> GlobalScaleChanged { get; set; }
    [Parameter] public double DefaultPanX { get; set; }
    [Parameter] public EventCallback<double> DefaultPanXChanged { get; set; }
    [Parameter] public double DefaultPanY { get; set; }
    [Parameter] public EventCallback<double> DefaultPanYChanged { get; set; }
    [Parameter] public double GridSize { get; set; }
    [Parameter] public EventCallback<double> GridSizeChanged { get; set; }
    [Parameter] public double MinZoom { get; set; }
    [Parameter] public EventCallback<double> MinZoomChanged { get; set; }
    [Parameter] public double MaxZoom { get; set; }
    [Parameter] public EventCallback<double> MaxZoomChanged { get; set; }
}
