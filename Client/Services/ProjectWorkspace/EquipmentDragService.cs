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
    private readonly List<IVisualElement> _movingElements = new();
    private readonly Dictionary<Guid, (double X, double Y)> _originalPositions = new();
    private double _lastMouseX;
    private double _lastMouseY;
    private double _originalDragX;
    private double _originalDragY;

    public EquipmentDragService(IPlacementRules placementRules)
    {
        _placementRules = placementRules ?? throw new ArgumentNullException(nameof(placementRules));
    }

    public bool IsMovingAny => _movingElement != null;

    public bool IsMoving(IVisualElement el) => _movingElements.Any(element => element.Id == el.Id);

    public void StartMove(
        IVisualElement el,
        MouseEventArgs e,
        bool isConnectionModeActive,
        Action<IVisualElement> onSelectElement,
        IReadOnlyCollection<IVisualElement>? selectedElements = null)
    {
        if (isConnectionModeActive || e.Button != 0) return;

        _movingElement = el;
        _movingElements.Clear();
        _originalPositions.Clear();
        _lastMouseX = e.ClientX;
        _lastMouseY = e.ClientY;
        _originalDragX = el.X;
        _originalDragY = el.Y;

        var group = selectedElements?.Any(selected => selected.Id == el.Id) == true
            ? selectedElements
            : new[] { el };

        foreach (var element in group)
        {
            _movingElements.Add(element);
            _originalPositions[element.Id] = (element.X, element.Y);
        }

        if (_movingElements.Count == 1)
        {
            onSelectElement(el);
        }
    }

    public void Move(MouseEventArgs e, double zoom, double globalScale, double paperWidth, double paperHeight)
    {
        if (_movingElement == null) return;

        double effectiveScale = zoom * globalScale;
        if (effectiveScale <= 0) effectiveScale = 0.001;

        var deltaX = (e.ClientX - _lastMouseX) / effectiveScale;
        var deltaY = (e.ClientY - _lastMouseY) / effectiveScale;

        foreach (var element in _movingElements)
        {
            if (element.AllowFreeDragX)
                element.X += deltaX;
            if (element.AllowFreeDragY)
                element.Y += deltaY;

            ClampToPaper(element, paperWidth, paperHeight);
        }

        _lastMouseX = e.ClientX;
        _lastMouseY = e.ClientY;
    }

    public bool EndMove(List<IVisualElement> elements, double gridSize, double paperWidth, double paperHeight)
    {
        if (_movingElement == null) return false;

        var movingIds = _movingElements.Select(element => element.Id).ToHashSet();
        var collisionCandidates = elements
            .Where(element => !movingIds.Contains(element.Id))
            .ToList();

        if (_movingElements.Any(element => _placementRules.HasCollision(element, element.X, element.Y, collisionCandidates)))
        {
            RestoreOriginalPositions();
        }

        foreach (var element in _movingElements)
        {
            ClampToPaper(element, paperWidth, paperHeight);

            if (element.AllowFreeDragX)
                element.X = _placementRules.Snap(element.X, gridSize);
            if (element.AllowFreeDragY)
                element.Y = _placementRules.Snap(element.Y, gridSize);

            ClampToPaper(element, paperWidth, paperHeight);
        }

        _movingElement = null;
        _movingElements.Clear();
        _originalPositions.Clear();
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

        RestoreOriginalPositions();
        _movingElement = null;
        _movingElements.Clear();
        _originalPositions.Clear();
    }

    private void RestoreOriginalPositions()
    {
        foreach (var element in _movingElements)
        {
            if (!_originalPositions.TryGetValue(element.Id, out var position)) continue;
            element.X = position.X;
            element.Y = position.Y;
        }
    }
}
