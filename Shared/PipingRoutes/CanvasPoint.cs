using Shared.ProcessFlowDiagram;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.PipingRoutes
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

    public static class SvgRouteFormatter
    {
        public static SvgRenderData FormatSinglePath(List<CanvasPoint> points)
        {
            if (points == null || points.Count < 2) return new SvgRenderData("");

            var sb = new StringBuilder();
            sb.Append($"M {Pt(points[0].X)} {Pt(points[0].Y)}");
            for (int i = 1; i < points.Count; i++)
            {
                sb.Append($" L {Pt(points[i].X)} {Pt(points[i].Y)}");
            }

            var midIdx = points.Count / 2;
            return new SvgRenderData(sb.ToString(), points[midIdx].X, points[midIdx].Y);
        }
        public static SvgRenderData FormatWithColisionBreaks(
           List<CanvasPoint> points,
           List<GeometryHelper.Segment> horizontalSegments,
           List<(double X, double Y, double Width, double Height)> equipmentBoxes)
        {
            if (points == null || points.Count < 2) return new SvgRenderData("", 0, 0, false);

            var sb = new StringBuilder();
            double PIPE_GAP = 4.0;  // Hueco para salto de tuberías
            double EQUIP_GAP = 5.0; // Hueco antes y después de los equipos

            CanvasPoint currentPoint = points[0];
            sb.Append($"M {Pt(currentPoint.X)} {Pt(currentPoint.Y)} ");

            for (int i = 1; i < points.Count; i++)
            {
                CanvasPoint nextPoint = points[i];
                bool isVert = Math.Abs(currentPoint.X - nextPoint.X) < 0.1;

                double startVal = isVert ? currentPoint.Y : currentPoint.X;
                double endVal = isVert ? nextPoint.Y : nextPoint.X;
                double fixedVal = isVert ? currentPoint.X : currentPoint.Y;
                double sign = endVal > startVal ? 1.0 : -1.0;

                // Recopilar todos los cortes necesarios en este segmento
                List<(double min, double max)> cuts = new();

                if (isVert)
                {
                    // Cortes por Tuberías
                    var seg = new GeometryHelper.Segment(currentPoint, nextPoint);
                    var pCuts = horizontalSegments.Where(h => seg.IntersectsHorizontal(h)).Select(h => (h.Y1 + h.Y2) / 2.0);
                    foreach (var y in pCuts) cuts.Add((y - PIPE_GAP, y + PIPE_GAP));

                    // Cortes por Equipos (Vertical)
                    foreach (var box in equipmentBoxes)
                    {
                        if (fixedVal > box.X + 1 && fixedVal < box.X + box.Width - 1) // Pasa por dentro del equipo
                        {
                            if (Math.Max(startVal, endVal) > box.Y && Math.Min(startVal, endVal) < box.Y + box.Height)
                                cuts.Add((box.Y - EQUIP_GAP, box.Y + box.Height + EQUIP_GAP));
                        }
                    }
                }
                else
                {
                    // Cortes por Equipos (Horizontal)
                    foreach (var box in equipmentBoxes)
                    {
                        if (fixedVal > box.Y + 1 && fixedVal < box.Y + box.Height - 1) // Pasa por dentro del equipo
                        {
                            if (Math.Max(startVal, endVal) > box.X && Math.Min(startVal, endVal) < box.X + box.Width)
                                cuts.Add((box.X - EQUIP_GAP, box.X + box.Width + EQUIP_GAP));
                        }
                    }
                }

                // Ordenar y procesar los cortes en la dirección en la que viajamos
                var validCuts = cuts
                    .Where(c => c.max > Math.Min(startVal, endVal) && c.min < Math.Max(startVal, endVal))
                    .OrderBy(c => sign == 1 ? c.min : -c.max)
                    .ToList();

                double currentDrawVal = startVal;

                foreach (var cut in validCuts)
                {
                    double cutEntry = sign == 1 ? cut.min : cut.max;
                    double cutExit = sign == 1 ? cut.max : cut.min;

                    // Si el corte empezó antes de donde estamos, ajustamos
                    if ((sign == 1 && cutEntry < currentDrawVal) || (sign == -1 && cutEntry > currentDrawVal)) cutEntry = currentDrawVal;

                    // 1. Dibujar línea hasta el borde del hueco
                    if ((sign == 1 && cutEntry > currentDrawVal) || (sign == -1 && cutEntry < currentDrawVal))
                    {
                        if (isVert) sb.Append($"L {Pt(fixedVal)} {Pt(cutEntry)} ");
                        else sb.Append($"L {Pt(cutEntry)} {Pt(fixedVal)} ");
                    }

                    // 2. Saltar el hueco (Mover el lápiz virtual)
                    currentDrawVal = sign == 1 ? Math.Max(currentDrawVal, cutExit) : Math.Min(currentDrawVal, cutExit);

                    if ((sign == 1 && currentDrawVal > endVal) || (sign == -1 && currentDrawVal < endVal)) currentDrawVal = endVal;

                    if (isVert) sb.Append($"M {Pt(fixedVal)} {Pt(currentDrawVal)} ");
                    else sb.Append($"M {Pt(currentDrawVal)} {Pt(fixedVal)} ");
                }

                // 3. Dibujar el tramo final del segmento si quedó algo después de los cortes
                if (Math.Abs(currentDrawVal - endVal) > 0.1)
                {
                    if (isVert) sb.Append($"L {Pt(fixedVal)} {Pt(endVal)} ");
                    else sb.Append($"L {Pt(endVal)} {Pt(fixedVal)} ");
                }

                currentPoint = nextPoint;
            }

            var midIdx = points.Count / 2;
            return new SvgRenderData(sb.ToString(), points[midIdx].X, points[midIdx].Y, false);
        }
        public static SvgRenderData FormatWithColisionBreaks2(List<CanvasPoint> points, List<GeometryHelper.Segment> horizontalSegments)
        {
            if (points == null || points.Count < 2) return new SvgRenderData("");

            var sb = new StringBuilder();
            double BREAK_SIZE = 8.0;
            double HALF_BREAK = BREAK_SIZE / 2.0;

            CanvasPoint currentPoint = points[0];
            sb.Append($"M {Pt(currentPoint.X)} {Pt(currentPoint.Y)} ");

            for (int i = 1; i < points.Count; i++)
            {
                CanvasPoint nextPoint = points[i];
                var currentSegment = new GeometryHelper.Segment(currentPoint, nextPoint);

                if (currentSegment.IsVertical)
                {
                    var intersections = horizontalSegments
                        .Where(h => currentSegment.IntersectsHorizontal(h))
                        .Select(h => (h.Y1 + h.Y2) / 2.0) // 🔥 Promedio para mayor seguridad
                        .OrderBy(y => Math.Abs(y - currentPoint.Y))
                        .ToList();

                    foreach (var intY in intersections)
                    {
                        double sign = nextPoint.Y > currentPoint.Y ? 1.0 : -1.0;

                        // Dibujar hasta el hueco
                        sb.Append($"L {Pt(nextPoint.X)} {Pt(intY - (HALF_BREAK * sign))} ");
                        // Saltar el hueco
                        sb.Append($"M {Pt(nextPoint.X)} {Pt(intY + (HALF_BREAK * sign))} ");
                    }
                }

                // Terminar segmento
                sb.Append($"L {Pt(nextPoint.X)} {Pt(nextPoint.Y)} ");
                currentPoint = nextPoint;
            }

            var midIdx = points.Count / 2;
            return new SvgRenderData(sb.ToString(), points[midIdx].X, points[midIdx].Y);
        }

        private static string Pt(double val) => Math.Round(val, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
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
