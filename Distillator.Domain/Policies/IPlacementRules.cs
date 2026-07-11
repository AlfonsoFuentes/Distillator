using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Policies;

/// <summary>
/// Reglas de colocación de equipos: snap a grilla, colisiones, resolución de posición.
/// </summary>
public interface IPlacementRules
{
    double Snap(double value, double gridSize);
    bool HasCollision(IVisualElement moving, double x, double y, IEnumerable<IVisualElement> others);
    (double X, double Y) ResolvePosition(IVisualElement newElement, IEnumerable<IVisualElement> existing, double gridSize);
}

public class PlacementRules : IPlacementRules
{
    public double Snap(double value, double gridSize)
    {
        if (gridSize <= 0) gridSize = 20.0;
        return Math.Round(value / gridSize) * gridSize;
    }

    public bool HasCollision(IVisualElement moving, double x, double y, IEnumerable<IVisualElement> others)
    {
        var b1 = GetBoundingBox(moving, x, y);
        foreach (var other in others)
        {
            if (other.Id == moving.Id) continue;
            var b2 = GetBoundingBox(other, other.X, other.Y);
            if (b1.X < b2.X + b2.Width && b1.X + b1.Width > b2.X &&
                b1.Y < b2.Y + b2.Height && b1.Y + b1.Height > b2.Y)
                return true;
        }
        return false;
    }

    public (double X, double Y) ResolvePosition(IVisualElement newElement, IEnumerable<IVisualElement> existing, double gridSize)
    {
        var list = existing.ToList();
        var x = newElement.X;
        var y = newElement.Y;

        while (HasCollision(newElement, x, y, list))
        {
            x += newElement.Width + gridSize;
        }

        return (Snap(x, gridSize), Snap(y, gridSize));
    }

    private static (double X, double Y, double Width, double Height) GetBoundingBox(IVisualElement el, double px, double py)
    {
        double w = el.Width;
        double h = el.Height;
        int rot = el.RotationAngle % 360;
        double sw = (rot == 90 || rot == 270) ? h : w;
        double sh = (rot == 90 || rot == 270) ? w : h;
        return (px + (w - sw) / 2.0 + 2, py + (h - sh) / 2.0 + 2, sw - 4, sh - 4);
    }
}
