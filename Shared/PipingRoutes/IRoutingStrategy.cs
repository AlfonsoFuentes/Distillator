using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.PipingRoutes
{
    public interface IRoutingStrategy
    {
        List<CanvasPoint> CalculateRoute(PipeRoutingContext ctx);
    }
    public interface ICollisionDetector
    {
        bool HasCollision(CanvasPoint p1, CanvasPoint p2, PipeRoutingContext ctx);
        bool IntersectsRect(CanvasPoint p1, CanvasPoint p2, CanvasPoint rectPos, double w, double h);
    }
    public class CollisionDetector : ICollisionDetector
    {
        private const double EPSILON = 0.1;
        private const double PIPE_MARGIN = 15.0;
        public bool HasCollision(CanvasPoint p1, CanvasPoint p2, PipeRoutingContext ctx)
        {
            if (IntersectsRect(p1, p2, ctx.AEquipPos, ctx.AWidth, ctx.AHeight))
                return true;

            if (IntersectsRect(p1, p2, ctx.BEquipPos, ctx.BWidth, ctx.BHeight))
                return true;

            foreach (var obstacle in ctx.EquipmentObstacles)
            {
                var box = GetScreenBoundingBox(obstacle);
                if (IntersectsRect(p1, p2, new CanvasPoint(box.X, box.Y), box.Width, box.Height))
                {
                    return true;
                }
            }
            return false;
        }

        // Helper para detectar si un punto es salida de puerto
        private bool IsPortExit(CanvasPoint p, ScreenBoundingBox box)
        {
            double margin = 10.0;
            return (Math.Abs(p.X - box.X) < margin || Math.Abs(p.X - (box.X + box.Width)) < margin ||
                    Math.Abs(p.Y - box.Y) < margin || Math.Abs(p.Y - (box.Y + box.Height)) < margin);
        }
        public bool HasCollision2(CanvasPoint p1, CanvasPoint p2, PipeRoutingContext ctx)
        {
            // 1. Equipos A y B (Origen y Destino)
            if (IntersectsRect(p1, p2, ctx.AEquipPos, ctx.AWidth, ctx.AHeight)) return true;
            if (IntersectsRect(p1, p2, ctx.BEquipPos, ctx.BWidth, ctx.BHeight)) return true;

            // 2. Equipos obstáculos físicos (Bombas, Tanques, etc.)
            foreach (var obstacle in ctx.EquipmentObstacles)
            {
                var box = GetScreenBoundingBox(obstacle);
                if (IntersectsRect(p1, p2, new CanvasPoint(box.X, box.Y), box.Width, box.Height))
                    return true;
            }

            

            return false;
        }

        // 👇 Dejo estos métodos por si en el futuro necesitas validar algo específico 
        // (como evitar que nazcan dos tuberías exactamente pegadas), pero ya no interfieren 
        // con el ruteo principal.
        private bool HasCollisionWithPipe(CanvasPoint p1, CanvasPoint p2, PipeVisualElement pipe)
        {
            if (pipe.CalculatedRoute == null || pipe.CalculatedRoute.Count < 2) return false;

            foreach (var (sStart, sEnd) in pipe.GetSegments())
            {
                // Relajación: Ignorar si comparten el mismo puerto de origen
                if (IsSamePoint(p1, sStart) || IsSamePoint(p1, sEnd) ||
                    IsSamePoint(p2, sStart) || IsSamePoint(p2, sEnd))
                {
                    continue;
                }

                if (SegmentsTooClose(p1, p2, sStart, sEnd, PIPE_MARGIN))
                    return true;
            }
            return false;
        }

        private bool IsSamePoint(CanvasPoint a, CanvasPoint b) => Math.Abs(a.X - b.X) < 0.1 && Math.Abs(a.Y - b.Y) < 0.1;

        private bool SegmentsTooClose(CanvasPoint a1, CanvasPoint a2, CanvasPoint b1, CanvasPoint b2, double margin)
        {
            double dist = SegmentToSegmentDistance(a1, a2, b1, b2);
            return dist < margin;
        }

        private ScreenBoundingBox GetScreenBoundingBox(IVisualElement el)
        {
            double w = el.Width, h = el.Height;
            int rot = el.RotationAngle % 360;

            double screenW = (rot == 90 || rot == 270) ? h : w;
            double screenH = (rot == 90 || rot == 270) ? w : h;

            double dx = (w - screenW) / 2.0;
            double dy = (h - screenH) / 2.0;

            return new ScreenBoundingBox(el.X + dx, el.Y + dy, screenW, screenH);
        }

        public bool IntersectsRect(CanvasPoint p1, CanvasPoint p2, CanvasPoint rectPos, double w, double h)
        {
            double minX = Math.Min(p1.X, p2.X), maxX = Math.Max(p1.X, p2.X);
            double minY = Math.Min(p1.Y, p2.Y), maxY = Math.Max(p1.Y, p2.Y);

            double rectMinX = rectPos.X, rectMaxX = rectPos.X + w;
            double rectMinY = rectPos.Y, rectMaxY = rectPos.Y + h;

            if (maxX < rectMinX || minX > rectMaxX || maxY < rectMinY || minY > rectMaxY)
                return false;

            if (Math.Abs(p1.X - p2.X) < EPSILON)
                return p1.X > rectMinX && p1.X < rectMaxX;

            if (Math.Abs(p1.Y - p2.Y) < EPSILON)
                return p1.Y > rectMinY && p1.Y < rectMaxY;

            return false;
        }

        private double SegmentToSegmentDistance(CanvasPoint a1, CanvasPoint a2, CanvasPoint b1, CanvasPoint b2)
        {
            bool aHoriz = Math.Abs(a1.Y - a2.Y) < EPSILON;
            bool bHoriz = Math.Abs(b1.Y - b2.Y) < EPSILON;

            if (aHoriz && bHoriz)
            {
                if (Math.Abs(a1.Y - b1.Y) >= EPSILON) return Math.Abs(a1.Y - b1.Y);
                double aMinX = Math.Min(a1.X, a2.X), aMaxX = Math.Max(a1.X, a2.X);
                double bMinX = Math.Min(b1.X, b2.X), bMaxX = Math.Max(b1.X, b2.X);
                if (aMaxX < bMinX) return bMinX - aMaxX;
                if (bMaxX < aMinX) return aMinX - bMaxX;
                return 0;
            }
            else if (!aHoriz && !bHoriz)
            {
                if (Math.Abs(a1.X - b1.X) >= EPSILON) return Math.Abs(a1.X - b1.X);
                double aMinY = Math.Min(a1.Y, a2.Y), aMaxY = Math.Max(a1.Y, a2.Y);
                double bMinY = Math.Min(b1.Y, b2.Y), bMaxY = Math.Max(b1.Y, b2.Y);
                if (aMaxY < bMinY) return bMinY - aMaxY;
                if (bMaxY < aMinY) return aMinY - bMaxY;
                return 0;
            }
            else
            {
                double ax = aHoriz ? a1.X : a1.Y;
                double ay = aHoriz ? a1.Y : a1.X;
                double bx = bHoriz ? b1.X : b1.Y;
                double by = bHoriz ? b1.Y : b1.X;

                double aMin = Math.Min(a1.X, a2.X), aMax = Math.Max(a1.X, a2.X);
                double bMin = Math.Min(b1.Y, b2.Y), bMax = Math.Max(b1.Y, b2.Y);

                if (aHoriz)
                {
                    if (bx >= aMin && bx <= aMax && ay >= bMin && ay <= bMax) return 0;
                    double dx = (bx < aMin) ? aMin - bx : (bx > aMax) ? bx - aMax : 0;
                    double dy = (ay < bMin) ? bMin - ay : (ay > bMax) ? ay - bMax : 0;
                    return Math.Sqrt(dx * dx + dy * dy);
                }
                else
                {
                    if (ax >= bMin && ax <= bMax && by >= aMin && by <= aMax) return 0;
                    double dx = (ax < bMin) ? bMin - ax : (ax > bMax) ? ax - bMin : 0;
                    double dy = (by < aMin) ? aMin - by : (by > aMax) ? by - aMax : 0;
                    return Math.Sqrt(dx * dx + dy * dy);
                }
            }
        }
    }
  
    
    public interface ISafePointCalculator
    {
        double CalculateSafeY(PipeRoutingContext ctx);
        double CalculateSafeX(PipeRoutingContext ctx);
    }
    public class SafePointCalculator : ISafePointCalculator
    {
        private const double MARGIN = 30.0;

        public double CalculateSafeY(PipeRoutingContext ctx)
        {
            // 🔥 LÓGICA MINIMALISTA: Para evitar el "escalón", simplemente elegimos 
            // el borde del equipo B por el que pasará la tubería,
            // basándonos en si el puerto SafeEnd está más arriba o más abajo.

            double bTop = ctx.BEquipPos.Y;
            double bBottom = ctx.BEquipPos.Y + ctx.BHeight;

            // Si el puerto de destino está en la mitad superior, rodeamos por arriba.
            // Si está en la inferior, rodeamos por abajo. ¡Cero quiebres intermedios!
            double bCenterY = bTop + ctx.BHeight / 2.0;

            if (Math.Abs(ctx.SafeEnd.Y - bCenterY) > 1.0)
            {
                return ctx.SafeEnd.Y < bCenterY ? bTop - MARGIN : bBottom + MARGIN;
            }
            else // El puerto está centrado. Elegimos el camino más corto según de dónde venimos.
            {
                return ctx.SafeStart.Y < bCenterY ? bTop - MARGIN : bBottom + MARGIN;
            }
        }
        public double CalculateSafeX(PipeRoutingContext ctx)
        {
            double bLeft = ctx.BEquipPos.X;
            double bRight = ctx.BEquipPos.X + ctx.BWidth;
            double bCenterX = bLeft + ctx.BWidth / 2.0;

            // Si el destino está claramente a la izquierda o derecha, respetamos su lado.
            if (Math.Abs(ctx.SafeEnd.X - bCenterX) > 1.0)
            {
                return ctx.SafeEnd.X < bCenterX ? bLeft - MARGIN : bRight + MARGIN;
            }
            // Si está centrado, rodeamos por el pasillo MÁS CERCANO al origen.
            else
            {
                return ctx.SafeStart.X < bCenterX ? bLeft - MARGIN : bRight + MARGIN;
            }
        }
        
    }

    public interface IRoutingHandler
    {
        IRoutingHandler SetNext(IRoutingHandler handler);
        List<CanvasPoint>? Handle(PipeRoutingContext ctx);
    }

    public abstract class RoutingHandlerBase : IRoutingHandler
    {
        private IRoutingHandler? _next;

        public IRoutingHandler SetNext(IRoutingHandler handler)
        {
            _next = handler;
            return handler;
        }

        protected List<CanvasPoint>? PassToNext(PipeRoutingContext ctx) => _next?.Handle(ctx);

        public abstract List<CanvasPoint>? Handle(PipeRoutingContext ctx);
    }
    public class TryDirectHandler : RoutingHandlerBase
    {
        private readonly ICollisionDetector _collision;

        public TryDirectHandler(ICollisionDetector collision) => _collision = collision;

        public override List<CanvasPoint>? Handle(PipeRoutingContext ctx)
        {
            // Solo aplica si puertos están alineados
            bool isAlignedX = Math.Abs(ctx.SafeStart.X - ctx.SafeEnd.X) < 0.1;
            bool isAlignedY = Math.Abs(ctx.SafeStart.Y - ctx.SafeEnd.Y) < 0.1;

            if ((isAlignedX || isAlignedY) && !_collision.HasCollision(ctx.SafeStart, ctx.SafeEnd, ctx))
            {
                return new List<CanvasPoint> { ctx.SafeStart, ctx.SafeEnd };
            }

            return PassToNext(ctx);
        }
    }


    public class TryAvoidRouter : IRoutingStrategy
    {
        private readonly IRoutingHandler _chain;

        public TryAvoidRouter(PipeRoutingContext ctx, ICollisionDetector collision, ISafePointCalculator safeCalc)
        {
            IAxisRoutingStrategy axisStrategy = ctx.IsVerticalPrimary
                ? new VerticalPrimaryStrategy()
                : new HorizontalPrimaryStrategy();

            var tryDirect = new TryDirectHandler(collision);
            var tryLRoute = new TryLRouteHandler(collision, axisStrategy);
            var avoid = new AvoidHandler(safeCalc, axisStrategy, collision); // 👈 Inyectar collision

            tryDirect.SetNext(tryLRoute).SetNext(avoid);
            _chain = tryDirect;
        }

        public List<CanvasPoint> CalculateRoute(PipeRoutingContext ctx)
        {
            return _chain.Handle(ctx) ?? new List<CanvasPoint> { ctx.SafeStart, ctx.SafeEnd };
        }
    }
    public class VerticalPrimaryStrategy : IAxisRoutingStrategy
    {
        public List<CanvasPoint>? TryLRoute(PipeRoutingContext ctx, ICollisionDetector collision)
        {
            // Try 1: Instinto puro (Vertical -> Horizontal)
            var via1 = new CanvasPoint(ctx.SafeStart.X, ctx.SafeEnd.Y);
            if (!collision.HasCollision(ctx.SafeStart, via1, ctx) && !collision.HasCollision(via1, ctx.SafeEnd, ctx))
                return new List<CanvasPoint> { ctx.SafeStart, via1, ctx.SafeEnd };

            // Try 2 (Plan B): Instinto invertido (Horizontal -> Vertical)
            var via2 = new CanvasPoint(ctx.SafeEnd.X, ctx.SafeStart.Y);
            if (!collision.HasCollision(ctx.SafeStart, via2, ctx) && !collision.HasCollision(via2, ctx.SafeEnd, ctx))
                return new List<CanvasPoint> { ctx.SafeStart, via2, ctx.SafeEnd };

            return null; // Solo si ambos chocan, pasamos al Avoid
        }

        public List<CanvasPoint> BuildAvoidRoute(PipeRoutingContext ctx, ISafePointCalculator safeCalc)
        {
            double safeX = safeCalc.CalculateSafeX(ctx);
            return new List<CanvasPoint> { ctx.SafeStart, new CanvasPoint(safeX, ctx.SafeStart.Y), new CanvasPoint(safeX, ctx.SafeEnd.Y), ctx.SafeEnd };
        }
    }
    public interface IAxisRoutingStrategy
    {
        List<CanvasPoint>? TryLRoute(PipeRoutingContext ctx, ICollisionDetector collision);
        List<CanvasPoint> BuildAvoidRoute(PipeRoutingContext ctx, ISafePointCalculator safeCalc);
    }
    public class TryLRouteHandler : RoutingHandlerBase
    {
        private readonly ICollisionDetector _collision;
        private readonly IAxisRoutingStrategy _axisStrategy;

        public TryLRouteHandler(ICollisionDetector collision, IAxisRoutingStrategy axisStrategy)
        {
            _collision = collision;
            _axisStrategy = axisStrategy;
        }

        public override List<CanvasPoint>? Handle(PipeRoutingContext ctx)
        {
            // Ejecuta el instinto primario sin preguntar cuál es
            var primaryRoute = _axisStrategy.TryLRoute(ctx, _collision);

            return primaryRoute ?? PassToNext(ctx);
        }
    }
    public class AvoidHandler : RoutingHandlerBase
    {
        private const double ROUTE_MARGIN = 30.0;
        private readonly ISafePointCalculator _safeCalc;
        private readonly IAxisRoutingStrategy _axisStrategy;
        private readonly ICollisionDetector _collision;

        public AvoidHandler(ISafePointCalculator safeCalc, IAxisRoutingStrategy axisStrategy, ICollisionDetector collision)
        {
            _safeCalc = safeCalc;
            _axisStrategy = axisStrategy;
            _collision = collision;
        }

        public override List<CanvasPoint>? Handle(PipeRoutingContext ctx)
        {
            var validRoute = BuildCandidateRoutes(ctx)
                .Where(route => IsValidRoute(route, ctx))
                .OrderBy(RouteLength)
                .ThenBy(CountBends)
                .FirstOrDefault();

            return validRoute ?? BuildExternalFallbackRoute(ctx);
        }

        private bool IsValidRoute(List<CanvasPoint> route, PipeRoutingContext ctx)
        {
            for (int i = 0; i < route.Count - 1; i++)
            {
                if (_collision.HasCollision(route[i], route[i + 1], ctx))
                    return false;
            }
            return true;
        }

        private IEnumerable<List<CanvasPoint>> BuildCandidateRoutes(PipeRoutingContext ctx)
        {
            var preferredL = ctx.IsVerticalPrimary
                ? BuildVerticalThenHorizontalRoute(ctx)
                : BuildHorizontalThenVerticalRoute(ctx);
            yield return preferredL;

            var alternateL = ctx.IsVerticalPrimary
                ? BuildHorizontalThenVerticalRoute(ctx)
                : BuildVerticalThenHorizontalRoute(ctx);
            yield return alternateL;

            yield return _axisStrategy.BuildAvoidRoute(ctx, _safeCalc);
            yield return BuildZAvoidRoute(ctx);

            foreach (var xLane in BuildXLanes(ctx))
            {
                yield return NormalizeRoute(new List<CanvasPoint>
                {
                    ctx.SafeStart,
                    new CanvasPoint(xLane, ctx.SafeStart.Y),
                    new CanvasPoint(xLane, ctx.SafeEnd.Y),
                    ctx.SafeEnd
                });
            }

            foreach (var yLane in BuildYLanes(ctx))
            {
                yield return NormalizeRoute(new List<CanvasPoint>
                {
                    ctx.SafeStart,
                    new CanvasPoint(ctx.SafeStart.X, yLane),
                    new CanvasPoint(ctx.SafeEnd.X, yLane),
                    ctx.SafeEnd
                });
            }
        }

        private static List<CanvasPoint> BuildHorizontalThenVerticalRoute(PipeRoutingContext ctx)
        {
            return NormalizeRoute(new List<CanvasPoint>
            {
                ctx.SafeStart,
                new CanvasPoint(ctx.SafeEnd.X, ctx.SafeStart.Y),
                ctx.SafeEnd
            });
        }

        private static List<CanvasPoint> BuildVerticalThenHorizontalRoute(PipeRoutingContext ctx)
        {
            return NormalizeRoute(new List<CanvasPoint>
            {
                ctx.SafeStart,
                new CanvasPoint(ctx.SafeStart.X, ctx.SafeEnd.Y),
                ctx.SafeEnd
            });
        }

        private IEnumerable<double> BuildXLanes(PipeRoutingContext ctx)
        {
            foreach (var box in BuildRoutingBoxes(ctx))
            {
                yield return box.X - ROUTE_MARGIN;
                yield return box.X + box.Width + ROUTE_MARGIN;
            }
        }

        private IEnumerable<double> BuildYLanes(PipeRoutingContext ctx)
        {
            foreach (var box in BuildRoutingBoxes(ctx))
            {
                yield return box.Y - ROUTE_MARGIN;
                yield return box.Y + box.Height + ROUTE_MARGIN;
            }
        }

        private static IEnumerable<ScreenBoundingBox> BuildRoutingBoxes(PipeRoutingContext ctx)
        {
            yield return new ScreenBoundingBox(ctx.AEquipPos.X, ctx.AEquipPos.Y, ctx.AWidth, ctx.AHeight);
            yield return new ScreenBoundingBox(ctx.BEquipPos.X, ctx.BEquipPos.Y, ctx.BWidth, ctx.BHeight);

            foreach (var obstacle in ctx.EquipmentObstacles)
            {
                yield return GetScreenBoundingBox(obstacle);
            }
        }

        private static ScreenBoundingBox GetScreenBoundingBox(IVisualElement el)
        {
            double w = el.Width;
            double h = el.Height;
            int rot = el.RotationAngle % 360;

            double screenW = (rot == 90 || rot == 270) ? h : w;
            double screenH = (rot == 90 || rot == 270) ? w : h;

            double dx = (w - screenW) / 2.0;
            double dy = (h - screenH) / 2.0;

            return new ScreenBoundingBox(el.X + dx, el.Y + dy, screenW, screenH);
        }

        private static List<CanvasPoint> NormalizeRoute(List<CanvasPoint> route)
        {
            var normalized = new List<CanvasPoint>();
            foreach (var point in route)
            {
                if (normalized.Count == 0 || !IsSamePoint(normalized[^1], point))
                {
                    normalized.Add(point);
                }
            }

            for (int i = normalized.Count - 2; i > 0; i--)
            {
                var previous = normalized[i - 1];
                var current = normalized[i];
                var next = normalized[i + 1];
                if (AreCollinear(previous, current, next))
                {
                    normalized.RemoveAt(i);
                }
            }

            return normalized;
        }

        private static bool IsSamePoint(CanvasPoint a, CanvasPoint b) =>
            Math.Abs(a.X - b.X) < 0.1 && Math.Abs(a.Y - b.Y) < 0.1;

        private static bool AreCollinear(CanvasPoint a, CanvasPoint b, CanvasPoint c)
        {
            return (Math.Abs(a.X - b.X) < 0.1 && Math.Abs(b.X - c.X) < 0.1) ||
                   (Math.Abs(a.Y - b.Y) < 0.1 && Math.Abs(b.Y - c.Y) < 0.1);
        }

        private List<CanvasPoint> BuildExternalFallbackRoute(PipeRoutingContext ctx)
        {
            var boxes = BuildRoutingBoxes(ctx).ToList();
            var minX = boxes.Min(box => box.X) - ROUTE_MARGIN;
            var maxX = boxes.Max(box => box.X + box.Width) + ROUTE_MARGIN;
            var minY = boxes.Min(box => box.Y) - ROUTE_MARGIN;
            var maxY = boxes.Max(box => box.Y + box.Height) + ROUTE_MARGIN;

            var candidates = new[]
            {
                NormalizeRoute(new List<CanvasPoint>
                {
                    ctx.SafeStart,
                    new CanvasPoint(minX, ctx.SafeStart.Y),
                    new CanvasPoint(minX, ctx.SafeEnd.Y),
                    ctx.SafeEnd
                }),
                NormalizeRoute(new List<CanvasPoint>
                {
                    ctx.SafeStart,
                    new CanvasPoint(maxX, ctx.SafeStart.Y),
                    new CanvasPoint(maxX, ctx.SafeEnd.Y),
                    ctx.SafeEnd
                }),
                NormalizeRoute(new List<CanvasPoint>
                {
                    ctx.SafeStart,
                    new CanvasPoint(ctx.SafeStart.X, minY),
                    new CanvasPoint(ctx.SafeEnd.X, minY),
                    ctx.SafeEnd
                }),
                NormalizeRoute(new List<CanvasPoint>
                {
                    ctx.SafeStart,
                    new CanvasPoint(ctx.SafeStart.X, maxY),
                    new CanvasPoint(ctx.SafeEnd.X, maxY),
                    ctx.SafeEnd
                })
            };

            return candidates
                .Where(route => IsValidRoute(route, ctx))
                .OrderBy(RouteLength)
                .ThenBy(CountBends)
                .FirstOrDefault()
                ?? candidates.OrderBy(RouteLength).ThenBy(CountBends).First();
        }

        private static double RouteLength(List<CanvasPoint> route)
        {
            double length = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                length += Math.Abs(route[i + 1].X - route[i].X) +
                          Math.Abs(route[i + 1].Y - route[i].Y);
            }

            return length;
        }

        private static int CountBends(List<CanvasPoint> route)
        {
            var bends = 0;
            for (int i = 1; i < route.Count - 1; i++)
            {
                if (!AreCollinear(route[i - 1], route[i], route[i + 1]))
                {
                    bends++;
                }
            }

            return bends;
        }

        private List<CanvasPoint> BuildZAvoidRoute(PipeRoutingContext ctx)
        {
            // Ruta que escapa en ambos ejes: garantiza evitar A y B
            double safeY = _safeCalc.CalculateSafeY(ctx);
            double safeX = _safeCalc.CalculateSafeX(ctx);

            return new List<CanvasPoint>
            {
                ctx.SafeStart,
                new CanvasPoint(ctx.SafeStart.X, safeY),
                new CanvasPoint(safeX, safeY),
                new CanvasPoint(safeX, ctx.SafeEnd.Y),
                ctx.SafeEnd
            };
        }

        private List<CanvasPoint> BuildCleanFallbackRoute(PipeRoutingContext ctx)
        {
            if (ctx.IsVerticalPrimary)
            {
                // Si la boquilla apunta a ARRIBA/ABAJO: El primer trazo debe ser VERTICAL.
                // Mantenemos la X (SafeStart.X) y viajamos hasta la Y del destino (SafeEnd.Y)
                return new List<CanvasPoint>
            {
                ctx.SafeStart,
                new CanvasPoint(ctx.SafeStart.X, ctx.SafeEnd.Y),
                ctx.SafeEnd
            };
            }
            else
            {
                // Si la boquilla apunta a IZQ/DER (Como tu S-101): El primer trazo debe ser HORIZONTAL.
                // Mantenemos la Y (SafeStart.Y) y viajamos hasta la X del destino (SafeEnd.X)
                return new List<CanvasPoint>
            {
                ctx.SafeStart,
                new CanvasPoint(ctx.SafeEnd.X, ctx.SafeStart.Y),
                ctx.SafeEnd
            };
            }
        }
       
    }
  
    public class HorizontalPrimaryStrategy : IAxisRoutingStrategy
    {
        public List<CanvasPoint>? TryLRoute(PipeRoutingContext ctx, ICollisionDetector collision)
        {
            // Try 1: Instinto puro (Horizontal -> Vertical)
            var via1 = new CanvasPoint(ctx.SafeEnd.X, ctx.SafeStart.Y);
            if (!collision.HasCollision(ctx.SafeStart, via1, ctx) && !collision.HasCollision(via1, ctx.SafeEnd, ctx))
                return new List<CanvasPoint> { ctx.SafeStart, via1, ctx.SafeEnd };

            // Try 2 (Plan B): Instinto invertido (Vertical -> Horizontal)
            var via2 = new CanvasPoint(ctx.SafeStart.X, ctx.SafeEnd.Y);
            if (!collision.HasCollision(ctx.SafeStart, via2, ctx) && !collision.HasCollision(via2, ctx.SafeEnd, ctx))
                return new List<CanvasPoint> { ctx.SafeStart, via2, ctx.SafeEnd };

            return null; // Solo si ambos chocan, pasamos al Avoid
        }

        public List<CanvasPoint> BuildAvoidRoute(PipeRoutingContext ctx, ISafePointCalculator safeCalc)
        {
            double safeY = safeCalc.CalculateSafeY(ctx);
            return new List<CanvasPoint> { ctx.SafeStart, new CanvasPoint(ctx.SafeStart.X, safeY), new CanvasPoint(ctx.SafeEnd.X, safeY), ctx.SafeEnd };
        }
    }
}
