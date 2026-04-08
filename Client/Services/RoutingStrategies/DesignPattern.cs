namespace Client.Services.RoutingStrategies
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

        public bool HasCollision(CanvasPoint p1, CanvasPoint p2, PipeRoutingContext ctx)
        {
            return IntersectsRect(p1, p2, ctx.AEquipPos, ctx.AWidth, ctx.AHeight) ||
                   IntersectsRect(p1, p2, ctx.BEquipPos, ctx.BWidth, ctx.BHeight);
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
        public double CalculateSafeX2(PipeRoutingContext ctx)
        {
            // Lógica minimalista idéntica para rodeos laterales (Eje X)
            double bLeft = ctx.BEquipPos.X;
            double bRight = ctx.BEquipPos.X + ctx.BWidth;

            // Si el destino está a la izquierda, rodeamos por el pasillo izquierdo.
            // Si está a la derecha, por el pasillo derecho.
            if (ctx.SafeEnd.X < bLeft + ctx.BWidth / 2.0)
            {
                return bLeft - MARGIN; // Pasillo Izquierdo Despejado
            }
            else
            {
                return bRight + MARGIN; // Pasillo Derecho Despejado
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
        private readonly ISafePointCalculator _safeCalc;
        private readonly IAxisRoutingStrategy _axisStrategy;
        private readonly ICollisionDetector _collision; // 👈 Agregar detector

        public AvoidHandler(ISafePointCalculator safeCalc, IAxisRoutingStrategy axisStrategy, ICollisionDetector collision)
        {
            _safeCalc = safeCalc;
            _axisStrategy = axisStrategy;
            _collision = collision;
        }

        public override List<CanvasPoint>? Handle(PipeRoutingContext ctx)
        {
            // 1. Intentar ruta de la estrategia primaria
            var route = _axisStrategy.BuildAvoidRoute(ctx, _safeCalc);
            if (IsValidRoute(route, ctx))
                return route;

            // 2. Fallback: Ruta Z con ambos SafeX y SafeY (máxima seguridad)
            route = BuildZAvoidRoute(ctx);
            if (IsValidRoute(route, ctx))
                return route;

            // 3. Último recurso: Retornar ruta Z aunque tenga colisión (mejor que null)
            return route;
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
