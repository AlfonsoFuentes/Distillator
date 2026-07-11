using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;
using Distillator.Domain.Policies;

namespace Client.Services.ProjectWorkspace;

/// <summary>
/// Servicio de UI que gestiona el arrastre de equipos con el ratón.
/// Usa IPlacementRules del dominio para snap y colisiones.
/// </summary>
public class EquipmentDragService
{
    private readonly IPlacementRules _placementRules;

    private IVisualElement? _movingElement;
    private double _lastMouseX;
    private double _lastMouseY;
    private double _originalDragX;
    private double _originalDragY;

    public EquipmentDragService(IPlacementRules placementRules)
    {
        _placementRules = placementRules ?? throw new ArgumentNullException(nameof(placementRules));
    }

    public bool IsMovingAny => _movingElement != null;

    public bool IsMoving(IVisualElement el) => _movingElement != null && _movingElement.Id == el.Id;

    public void StartMove(IVisualElement el, MouseEventArgs e, bool isConnectionModeActive, Action<IVisualElement> onSelectElement)
    {
        if (isConnectionModeActive || e.Button != 0) return;

        _movingElement = el;
        _lastMouseX = e.ClientX;
        _lastMouseY = e.ClientY;
        _originalDragX = el.X;
        _originalDragY = el.Y;
        onSelectElement(el);
    }

    public void Move(MouseEventArgs e, double zoom, double globalScale, double paperWidth, double paperHeight)
    {
        if (_movingElement == null) return;

        double effectiveScale = zoom * globalScale;
        if (effectiveScale <= 0) effectiveScale = 0.001;

        if (_movingElement.AllowFreeDragX)
            _movingElement.X += (e.ClientX - _lastMouseX) / effectiveScale;
        if (_movingElement.AllowFreeDragY)
            _movingElement.Y += (e.ClientY - _lastMouseY) / effectiveScale;

        ClampToPaper(_movingElement, paperWidth, paperHeight);

        _lastMouseX = e.ClientX;
        _lastMouseY = e.ClientY;
    }

    public bool EndMove(List<IVisualElement> elements, double gridSize, double paperWidth, double paperHeight)
    {
        if (_movingElement == null) return false;

        if (_placementRules.HasCollision(_movingElement, _movingElement.X, _movingElement.Y, elements))
        {
            _movingElement.X = _originalDragX;
            _movingElement.Y = _originalDragY;
        }

        ClampToPaper(_movingElement, paperWidth, paperHeight);

        if (_movingElement.AllowFreeDragX)
            _movingElement.X = _placementRules.Snap(_movingElement.X, gridSize);
        if (_movingElement.AllowFreeDragY)
            _movingElement.Y = _placementRules.Snap(_movingElement.Y, gridSize);

        ClampToPaper(_movingElement, paperWidth, paperHeight);

        _movingElement = null;
        return true;
    }

    private static void ClampToPaper(IVisualElement element, double paperWidth, double paperHeight)
    {
        if (paperWidth <= 0 || paperHeight <= 0) return;

        var maxX = Math.Max(0, paperWidth - element.Width);
        var maxY = Math.Max(0, paperHeight - element.Height);

        element.X = Math.Clamp(element.X, 0, maxX);
        element.Y = Math.Clamp(element.Y, 0, maxY);
    }

    public void CancelMove()
    {
        if (_movingElement == null) return;

        _movingElement.X = _originalDragX;
        _movingElement.Y = _originalDragY;
        _movingElement = null;
    }
}
