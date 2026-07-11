namespace Distillator.Domain.Configuration;

public class CameraConfiguration : ICameraConfiguration
{
    public double DefaultZoom { get; set; }
    public double DefaultPanX { get; set; }
    public double DefaultPanY { get; set; }
    public double GlobalScale { get; set; }
    public double GridSize { get; set; }
    public double MinZoom { get; set; }
    public double MaxZoom { get; set; }

    public CameraConfiguration(
        double defaultZoom = 1.0,
        double defaultPanX = 0.0,
        double defaultPanY = 0.0,
        double globalScale = 0.7,
        double gridSize = 20.0,
        double minZoom = 0.2,
        double maxZoom = 3.0)
    {
        DefaultZoom = defaultZoom;
        DefaultPanX = defaultPanX;
        DefaultPanY = defaultPanY;
        GlobalScale = globalScale > 0.01 ? globalScale : 0.7;
        GridSize = gridSize > 0 ? gridSize : 20.0;
        MinZoom = minZoom > 0 ? minZoom : 0.2;
        MaxZoom = maxZoom > minZoom ? maxZoom : 3.0;
    }
}
