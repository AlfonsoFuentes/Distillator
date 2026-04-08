using Shared.ProcessFlowDiagram;
using System.Text;

namespace Client.Services.RoutingStrategies
{
    public class Offset { public double X { get; set; } public double Y { get; set; } }
    public record CanvasPoint(double X, double Y);

    public record PhaseResult(List<CanvasPoint> Points, CanvasPoint TerminalPoint);
    public record SvgRenderData(
        string MainPath,
        double LabelX = 0,
        double LabelY = 0,
        bool LabelIsVertical = false
    );

    // 2. El formateador
    public static class SvgRouteFormatter
    {
        public static SvgRenderData FormatSinglePath(List<CanvasPoint> points)
        {
            if (points == null || points.Count < 2)
                return new SvgRenderData("");

            var sb = new StringBuilder();
            sb.Append($"M {points[0].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[0].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

            for (int i = 1; i < points.Count; i++)
            {
                sb.Append($" L {points[i].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[i].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }

            string pathD = sb.ToString();

            // Punto medio para la etiqueta
            var midIdx = points.Count / 2;
            var labelPt = points[midIdx];

            return new SvgRenderData(pathD, labelPt.X, labelPt.Y);
        }
    }

    // El Request ahora es PURA GEOMETRÍA: A siempre es el de la IZQUIERDA.
    public record RoutingRequest(
        CanvasPoint A, PortDirection ADir, CanvasPoint AEquipPos, double AWidth, double AHeight,
        CanvasPoint B, PortDirection BDir, CanvasPoint BEquipPos, double BWidth, double BHeight,
        IEnumerable<IVisualElement> Obstacles)
    {
        // El Factory Method que limpia la creación y oculta la lógica de "swap"
        public static RoutingRequest CreateNormalized(
            (CanvasPoint Point, PortDirection Dir, CanvasPoint EquipPos, double W, double H) source,
            (CanvasPoint Point, PortDirection Dir, CanvasPoint EquipPos, double W, double H) target,
            IEnumerable<IVisualElement> obstacles,
            out bool wasSwapped)
        {
            wasSwapped = source.Point.X > target.Point.X;

            return !wasSwapped
                ? new RoutingRequest(
                    source.Point, source.Dir, source.EquipPos, source.W, source.H,
                    target.Point, target.Dir, target.EquipPos, target.W, target.H,
                    obstacles)
                : new RoutingRequest(
                    target.Point, target.Dir, target.EquipPos, target.W, target.H,
                    source.Point, source.Dir, source.EquipPos, source.W, source.H,
                    obstacles);
        }
    }
}
