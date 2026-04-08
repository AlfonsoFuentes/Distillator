//namespace Client.Services.RoutingStrategies
//{
//    public interface ISourceExitStrategy
//    {

//        PhaseResult Calculate(RoutingRequest req);
//    }
//    public class SourceExitLeft : ISourceExitStrategy
//    {

//        public PhaseResult Calculate(RoutingRequest req)
//        {
//            var points = new List<CanvasPoint> { req.A };

//            // 1. Stub: 30px a la izquierda del puerto
//            var s1 = new CanvasPoint(req.A.X - 30, req.A.Y);
//            points.Add(s1);

//            // 2. ¿A dónde vamos? 
//            // Si B está abajo (B.Y > A.Y), rodeamos por ABAJO.
//            // Si B está arriba (B.Y < A.Y), rodeamos por ARRIBA.
//            double safeY = (req.B.Y > req.A.Y)
//                ? req.AEquipPos.Y + req.AHeight + 30  // Borde inferior + margen
//                : req.AEquipPos.Y - 30;               // Borde superior - margen

//            var handover = new CanvasPoint(s1.X, safeY);
//            points.Add(handover);

//            return new PhaseResult(points, handover);
//        }
//    }
//    public class SourceExitTop : ISourceExitStrategy
//    {
//        public PhaseResult Calculate(RoutingRequest req)
//        {
//            var points = new List<CanvasPoint> { req.A };

//            // Solo subimos 30px (Y disminuye)
//            var handover = new CanvasPoint(req.A.X, req.A.Y - 30);
//            points.Add(handover);

//            return new PhaseResult(points, handover);
//        }
//    }
//    public class SourceExitBottom : ISourceExitStrategy
//    {
//        public PhaseResult Calculate(RoutingRequest req)
//        {
//            var points = new List<CanvasPoint> { req.A };

//            // Solo bajamos 30px (Y aumenta hacia abajo en el canvas)
//            var handover = new CanvasPoint(req.A.X, req.A.Y + 30);
//            points.Add(handover);

//            return new PhaseResult(points, handover);
//        }
//    }
//    public class SourceExitRight : ISourceExitStrategy
//    {
//        public PhaseResult Calculate(RoutingRequest req)
//        {
//            var points = new List<CanvasPoint> { req.A };

//            // 1. Stub: 30px hacia la derecha (Y se mantiene, X aumenta)
//            var handover = new CanvasPoint(req.A.X + 30, req.A.Y);
//            points.Add(handover);

//            return new PhaseResult(points, handover);
//        }
//    }
//}
//public class SafePointCalculator2 : ISafePointCalculator
//{
//    private const double MARGIN = 30.0;

//    public double CalculateSafeY(PipeRoutingContext ctx)
//    {
//        // REGLA: SafeY debe estar del mismo lado que SafeEnd
//        double bTop = ctx.BEquipPos.Y;
//        double bBottom = ctx.BEquipPos.Y + ctx.BHeight;

//        bool safeEndAbove = ctx.SafeEnd.Y < bTop;
//        bool safeEndBelow = ctx.SafeEnd.Y > bBottom;

//        bool goAbove = safeEndAbove || (!safeEndBelow && ctx.SafeStart.Y < bTop + ctx.BHeight / 2);

//        return goAbove ? bTop - MARGIN : bBottom + MARGIN;
//    }

//    public double CalculateSafeX(PipeRoutingContext ctx)
//    {
//        double bLeft = ctx.BEquipPos.X;
//        double bRight = ctx.BEquipPos.X + ctx.BWidth;

//        bool safeEndLeft = ctx.SafeEnd.X < bLeft;
//        bool safeEndRight = ctx.SafeEnd.X > bRight;

//        bool goRight = safeEndRight || (!safeEndLeft && ctx.SafeStart.X < bLeft + ctx.BWidth / 2);

//        return goRight ? bRight + MARGIN : bLeft - MARGIN;
//    }
//}
//public class TryLRouteHandler : RoutingHandlerBase
//{
//    private readonly ICollisionDetector _collision;

//    public TryLRouteHandler(ICollisionDetector collision) => _collision = collision;

//    public override List<CanvasPoint>? Handle(PipeRoutingContext ctx)
//    {
//        // Intentar L-route según eje primario
//        var route = ctx.IsVerticalPrimary
//            ? TryVerticalThenHorizontal(ctx)
//            : TryHorizontalThenVertical(ctx);

//        return route ?? PassToNext(ctx);
//    }

//    private List<CanvasPoint>? TryVerticalThenHorizontal(PipeRoutingContext ctx)
//    {
//        var via = new CanvasPoint(ctx.SafeStart.X, ctx.SafeEnd.Y);
//        if (!_collision.HasCollision(ctx.SafeStart, via, ctx) &&
//            !_collision.HasCollision(via, ctx.SafeEnd, ctx))
//        {
//            return new List<CanvasPoint> { ctx.SafeStart, via, ctx.SafeEnd };
//        }
//        return null;
//    }

//    private List<CanvasPoint>? TryHorizontalThenVertical(PipeRoutingContext ctx)
//    {
//        var via = new CanvasPoint(ctx.SafeEnd.X, ctx.SafeStart.Y);
//        if (!_collision.HasCollision(ctx.SafeStart, via, ctx) &&
//            !_collision.HasCollision(via, ctx.SafeEnd, ctx))
//        {
//            return new List<CanvasPoint> { ctx.SafeStart, via, ctx.SafeEnd };
//        }
//        return null;
//    }
//}
//public class AvoidHandler : RoutingHandlerBase
//{
//    private readonly ICollisionDetector _collision;
//    private readonly ISafePointCalculator _safeCalc;

//    public AvoidHandler(ICollisionDetector collision, ISafePointCalculator safeCalc)
//    {
//        _collision = collision;
//        _safeCalc = safeCalc;
//    }

//    public override List<CanvasPoint>? Handle(PipeRoutingContext ctx)
//    {
//        // Intentar ruta preferida según eje primario
//        var route = ctx.IsVerticalPrimary
//            ? TryBuildVerticalAvoidRoute(ctx)
//            : TryBuildHorizontalAvoidRoute(ctx);

//        if (route != null)
//            return route;

//        // Fallback: intentar eje secundario
//        route = ctx.IsVerticalPrimary
//            ? TryBuildHorizontalAvoidRoute(ctx)
//            : TryBuildVerticalAvoidRoute(ctx);

//        if (route != null)
//            return route;

//        // Último recurso: ruta Z con ambos SafeX y SafeY
//        return BuildZAvoidRoute(ctx);
//    }

//    private List<CanvasPoint>? TryBuildHorizontalAvoidRoute(PipeRoutingContext ctx)
//    {
//        double safeX = _safeCalc.CalculateSafeX(ctx);
//        var via1 = new CanvasPoint(safeX, ctx.SafeStart.Y);
//        var via2 = new CanvasPoint(safeX, ctx.SafeEnd.Y);

//        // Validar todos los segmentos
//        if (_collision.HasCollision(ctx.SafeStart, via1, ctx)) return null;
//        if (_collision.HasCollision(via1, via2, ctx)) return null;
//        if (_collision.HasCollision(via2, ctx.SafeEnd, ctx)) return null;

//        return new List<CanvasPoint> { ctx.SafeStart, via1, via2, ctx.SafeEnd };
//    }

//    private List<CanvasPoint>? TryBuildVerticalAvoidRoute(PipeRoutingContext ctx)
//    {
//        double safeY = _safeCalc.CalculateSafeY(ctx);
//        var via1 = new CanvasPoint(ctx.SafeStart.X, safeY);
//        var via2 = new CanvasPoint(ctx.SafeEnd.X, safeY);

//        // Validar todos los segmentos
//        if (_collision.HasCollision(ctx.SafeStart, via1, ctx)) return null;
//        if (_collision.HasCollision(via1, via2, ctx)) return null;
//        if (_collision.HasCollision(via2, ctx.SafeEnd, ctx)) return null;

//        return new List<CanvasPoint> { ctx.SafeStart, via1, via2, ctx.SafeEnd };
//    }

//    private List<CanvasPoint> BuildZAvoidRoute(PipeRoutingContext ctx)
//    {
//        // Ruta que escapa en ambos ejes: SafeStart → (SafeStart.X, SafeY) → (SafeX, SafeY) → (SafeX, SafeEnd.Y) → SafeEnd
//        double safeY = _safeCalc.CalculateSafeY(ctx);
//        double safeX = _safeCalc.CalculateSafeX(ctx);

//        var via1 = new CanvasPoint(ctx.SafeStart.X, safeY);
//        var via2 = new CanvasPoint(safeX, safeY);
//        var via3 = new CanvasPoint(safeX, ctx.SafeEnd.Y);

//        // Esta ruta debería ser siempre válida porque SafeX/SafeY están fuera de ambos equipos
//        return new List<CanvasPoint> { ctx.SafeStart, via1, via2, via3, ctx.SafeEnd };
//    }
//}
//public class AvoidHandler2 : RoutingHandlerBase
//{
//    private readonly ICollisionDetector _collision;
//    private readonly ISafePointCalculator _safeCalc;

//    public AvoidHandler2(ICollisionDetector collision, ISafePointCalculator safeCalc)
//    {
//        _collision = collision;
//        _safeCalc = safeCalc;
//    }

//    public override List<CanvasPoint>? Handle(PipeRoutingContext ctx)
//    {
//        // Último recurso: usar SafeX/SafeY
//        var route = ctx.IsVerticalPrimary
//         ? BuildHorizontalAvoidRoute(ctx) // Usa SafeX para rodear por los lados
//         : BuildVerticalAvoidRoute(ctx);  // Usa SafeY para rodear por arriba/abajo

//        return route;


//    }

//    private List<CanvasPoint> BuildVerticalAvoidRoute(PipeRoutingContext ctx)
//    {
//        double safeY = _safeCalc.CalculateSafeY(ctx);
//        var via1 = new CanvasPoint(ctx.SafeStart.X, safeY);
//        var via2 = new CanvasPoint(ctx.SafeEnd.X, safeY);
//        return new List<CanvasPoint> { ctx.SafeStart, via1, via2, ctx.SafeEnd };
//    }

//    private List<CanvasPoint> BuildHorizontalAvoidRoute(PipeRoutingContext ctx)
//    {
//        double safeX = _safeCalc.CalculateSafeX(ctx);
//        var via1 = new CanvasPoint(safeX, ctx.SafeStart.Y);
//        var via2 = new CanvasPoint(safeX, ctx.SafeEnd.Y);
//        return new List<CanvasPoint> { ctx.SafeStart, via1, via2, ctx.SafeEnd };
//    }
//}