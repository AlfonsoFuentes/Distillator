using Distillator.Domain.Models;

namespace Distillator.Domain.Services;

/// <summary>
/// Servicio de lógica de cámara. No renderiza; solo opera sobre el estado de un Flowsheet.
/// </summary>
public interface ICameraService
{
    void ZoomAt(IFlowsheet flowsheet, double deltaY, double pointerX, double pointerY);
    void Pan(IFlowsheet flowsheet, double deltaX, double deltaY);
    void ZoomToFit(IFlowsheet flowsheet, double screenWidth, double screenHeight);
    void Reset(IFlowsheet flowsheet);
}

public class CameraService : ICameraService
{
    public void ZoomAt(IFlowsheet flowsheet, double deltaY, double pointerX, double pointerY)
    {
        double factor = deltaY > 0 ? 0.9 : 1.1;
        double newZoom = flowsheet.Zoom * factor;
        newZoom = Math.Clamp(newZoom, flowsheet.Project.Configuration.CameraDefaults.MinZoom, flowsheet.Project.Configuration.CameraDefaults.MaxZoom);

        double logicalX = (pointerX - flowsheet.PanX) / flowsheet.Zoom;
        double logicalY = (pointerY - flowsheet.PanY) / flowsheet.Zoom;

        flowsheet.Zoom = newZoom;
        flowsheet.PanX = pointerX - (logicalX * newZoom);
        flowsheet.PanY = pointerY - (logicalY * newZoom);
    }

    public void Pan(IFlowsheet flowsheet, double deltaX, double deltaY)
    {
        flowsheet.PanX += deltaX;
        flowsheet.PanY += deltaY;
    }

    public void ZoomToFit(IFlowsheet flowsheet, double screenWidth, double screenHeight)
    {
        var elements = flowsheet.Elements.ToList();
        if (elements.Count == 0) return;

        double minX = elements.Min(e => e.X);
        double maxX = elements.Max(e => e.X + GetElementWidth(e, flowsheet.Project));
        double minY = elements.Min(e => e.Y);
        double maxY = elements.Max(e => e.Y + GetElementHeight(e, flowsheet.Project));

        double contentWidth = maxX - minX;
        double contentHeight = maxY - minY;
        double padding = 100;

        double scaleX = (screenWidth - padding) / contentWidth;
        double scaleY = (screenHeight - padding) / contentHeight;
        double newZoom = Math.Min(scaleX, scaleY);
        newZoom = Math.Clamp(newZoom, 0.5, 1.2);

        flowsheet.Zoom = newZoom;
        double effectiveScale = newZoom * flowsheet.GlobalScale;
        flowsheet.PanX = (screenWidth - (contentWidth * effectiveScale)) / 2 - (minX * effectiveScale);
        flowsheet.PanY = (screenHeight - (contentHeight * effectiveScale)) / 2 - (minY * effectiveScale);
    }

    public void Reset(IFlowsheet flowsheet)
    {
        flowsheet.ResetCameraToDefaults();
    }

    private static double GetElementWidth(IFlowsheetElementReference reference, IProject project)
    {
        var element = project.EquipmentRegistry.GetById(reference.ElementId);
        return element?.Width ?? 0;
    }

    private static double GetElementHeight(IFlowsheetElementReference reference, IProject project)
    {
        var element = project.EquipmentRegistry.GetById(reference.ElementId);
        return element?.Height ?? 0;
    }
}
