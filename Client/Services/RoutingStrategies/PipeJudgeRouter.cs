using Shared.ProcessFlowDiagram;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Services.RoutingStrategies
{
    // --- 1. MODELO DE DATOS Y FACTORY METHOD ---
    //public record RoutingRequest(
    //    CanvasPoint A, PortDirection ADir, CanvasPoint AEquipPos, double AWidth, double AHeight,
    //    CanvasPoint B, PortDirection BDir, CanvasPoint BEquipPos, double BWidth, double BHeight,
    //    IEnumerable<IVisualElement> Obstacles)
    //{
    //    // El Factory Method que limpia la creación y oculta la lógica de "swap"
    //    public static RoutingRequest CreateNormalized(
    //        (CanvasPoint Point, PortDirection Dir, CanvasPoint EquipPos, double W, double H) source,
    //        (CanvasPoint Point, PortDirection Dir, CanvasPoint EquipPos, double W, double H) target,
    //        IEnumerable<IVisualElement> obstacles,
    //        out bool wasSwapped)
    //    {
    //        wasSwapped = source.Point.X > target.Point.X;

    //        return !wasSwapped
    //            ? new RoutingRequest(
    //                source.Point, source.Dir, source.EquipPos, source.W, source.H,
    //                target.Point, target.Dir, target.EquipPos, target.W, target.H,
    //                obstacles)
    //            : new RoutingRequest(
    //                target.Point, target.Dir, target.EquipPos, target.W, target.H,
    //                source.Point, source.Dir, source.EquipPos, source.W, source.H,
    //                obstacles);
    //    }
    //}

    // --- 2. EL MOTOR DE RUTEO (EL JUEZ PRAGMÁTICO) ---
    public class PipeJudgeRouter
    {
        private const double MARGIN = 30.0;

        public List<CanvasPoint> GetBestRoute(RoutingRequest req)
        {
            // 1. TANTEO DE SALIDA (A)
            var startSafe = GetSmartExitPoint(req);

            // 2. TANTEO DE ENTRADA (B)
            var endSafe = GetSmartEntryPoint(req);

            // 3. GENERAR CANDIDATOS PARA EL PUENTE
            var candidates = new List<RouteCandidate>
            {
                GenerateLRoute(startSafe, endSafe, verticalFirst: true),
                GenerateLRoute(startSafe, endSafe, verticalFirst: false),
                GenerateZRoute(startSafe, endSafe)
            };

            // 4. SELECCIÓN
            var best = SelectBest(candidates, req);

            // 5. ENSAMBLAJE FINAL
            var path = new List<CanvasPoint> { req.A };

            foreach (var p in best.Points)
            {
                if (Math.Abs(p.X - path.Last().X) > 0.1 || Math.Abs(p.Y - path.Last().Y) > 0.1)
                    path.Add(p);
            }

            if (Math.Abs(path.Last().X - req.B.X) > 0.1 || Math.Abs(path.Last().Y - req.B.Y) > 0.1)
                path.Add(req.B);

            return path;
        }

        // --- MÉTODOS DE TANTEO ---

        private CanvasPoint GetSmartExitPoint(RoutingRequest req)
        {
            var tryPoint = GetPointInDirection(req.A, req.ADir, MARGIN);

            if (!RoutingMath.IsSegmentIntersectsRectangle(req.A, tryPoint, req.AEquipPos, req.AWidth, req.AHeight))
            {
                return tryPoint;
            }

            PortDirection escapeDir = (req.ADir == PortDirection.Top || req.ADir == PortDirection.Bottom)
                                      ? PortDirection.Right : PortDirection.Bottom;

            return GetPointInDirection(req.A, escapeDir, MARGIN);
        }

        private CanvasPoint GetSmartEntryPoint(RoutingRequest req)
        {
            var tryPoint = GetPointInDirection(req.B, req.BDir, MARGIN);

            if (!RoutingMath.IsSegmentIntersectsRectangle(tryPoint, req.B, req.BEquipPos, req.BWidth, req.BHeight))
            {
                return tryPoint;
            }

            return new CanvasPoint(tryPoint.X + MARGIN, tryPoint.Y + MARGIN);
        }

        private CanvasPoint GetPointInDirection(CanvasPoint p, PortDirection dir, double dist)
        {
            return dir switch
            {
                PortDirection.Top => new CanvasPoint(p.X, p.Y - dist),
                PortDirection.Bottom => new CanvasPoint(p.X, p.Y + dist),
                PortDirection.Left => new CanvasPoint(p.X - dist, p.Y),
                PortDirection.Right => new CanvasPoint(p.X + dist, p.Y),
                _ => p
            };
        }

        // --- GENERADORES Y EVALUACIÓN ---

        private RouteCandidate SelectBest(List<RouteCandidate> candidates, RoutingRequest req)
        {
            RouteCandidate best = candidates[0];
            double minScore = EvaluateRoute(best, req);

            for (int i = 1; i < candidates.Count; i++)
            {
                double score = EvaluateRoute(candidates[i], req);
                if (score < minScore)
                {
                    minScore = score;
                    best = candidates[i];
                }
            }
            return best;
        }

        private double EvaluateRoute(RouteCandidate route, RoutingRequest req)
        {
            double score = route.Points.Count * 100;

            if (IntersectsMainEquipment(route, req))
                score += 1000000;

            return score;
        }

        private bool IntersectsMainEquipment(RouteCandidate route, RoutingRequest req)
        {
            for (int i = 0; i < route.Points.Count - 1; i++)
            {
                if (RoutingMath.IsSegmentIntersectsRectangle(route.Points[i], route.Points[i + 1], req.AEquipPos, req.AWidth, req.AHeight)) return true;
                if (RoutingMath.IsSegmentIntersectsRectangle(route.Points[i], route.Points[i + 1], req.BEquipPos, req.BWidth, req.BHeight)) return true;
            }
            return false;
        }

        private RouteCandidate GenerateLRoute(CanvasPoint start, CanvasPoint end, bool verticalFirst)
        {
            var c = new RouteCandidate();
            c.Points.Add(start);
            if (verticalFirst) c.Points.Add(new CanvasPoint(start.X, end.Y));
            else c.Points.Add(new CanvasPoint(end.X, start.Y));
            c.Points.Add(end);
            return c;
        }

        private RouteCandidate GenerateZRoute(CanvasPoint start, CanvasPoint end)
        {
            var c = new RouteCandidate();
            c.Points.Add(start);
            double midX = start.X + (end.X - start.X) / 2;
            c.Points.Add(new CanvasPoint(midX, start.Y));
            c.Points.Add(new CanvasPoint(midX, end.Y));
            c.Points.Add(end);
            return c;
        }

        public class RouteCandidate { public List<CanvasPoint> Points { get; set; } = new(); }
    }

    // --- 3. MOTOR MATEMÁTICO ---
    public static class RoutingMath
    {
        public static bool IsSegmentIntersectsRectangle(CanvasPoint p1, CanvasPoint p2, CanvasPoint rectPos, double rectW, double rectH)
        {
            double rectMinX = rectPos.X;
            double rectMaxX = rectPos.X + rectW;
            double rectMinY = rectPos.Y;
            double rectMaxY = rectPos.Y + rectH;

            double segMinX = Math.Min(p1.X, p2.X);
            double segMaxX = Math.Max(p1.X, p2.X);
            double segMinY = Math.Min(p1.Y, p2.Y);
            double segMaxY = Math.Max(p1.Y, p2.Y);

            bool overlapX = (segMaxX >= rectMinX && segMinX <= rectMaxX);
            bool overlapY = (segMaxY >= rectMinY && segMinY <= rectMaxY);

            if (!overlapX || !overlapY) return false;

            if (Math.Abs(p1.X - p2.X) < 0.1)
                return p1.X > rectMinX && p1.X < rectMaxX;

            if (Math.Abs(p1.Y - p2.Y) < 0.1)
                return p1.Y > rectMinY && p1.Y < rectMaxY;

            return false;
        }
    }

}