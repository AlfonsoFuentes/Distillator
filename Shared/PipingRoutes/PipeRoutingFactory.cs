using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;

namespace Shared.PipingRoutes
{
   
    /// </summary>
    public static class PipeRoutingFactory
    {
        private const double SAFE_MARGIN = 30.0;
        private static readonly ICollisionDetector _collision = new CollisionDetector();
        private static readonly ISafePointCalculator _safeCalc = new SafePointCalculator();

        public static SvgRenderData GetRoute(PipeVisualElement pipe, bool isDraft, 
            double DraftMouseLogicalX, double DraftMouseLogicalY, List<IVisualElement> _Elements, 
            List<PipeVisualElement> _Pipes)
        {
            if (pipe.SourceElement == null)
                return new SvgRenderData("");

            // ─────────────────────────────────────────────────────────
            // 1. Extraer coordenadas absolutas de puertos y cajas
            // ─────────────────────────────────────────────────────────
            var sourceCoords = pipe.SourceElement.GetAbsolutePortCoordinates(pipe.SourcePortName);
            var sourcePoint = new CanvasPoint(sourceCoords.X, sourceCoords.Y);

            CanvasPoint targetPoint;
            PortDirection targetDir;
            ScreenBoundingBox targetBox;

            if (isDraft)
            {
                targetPoint = new CanvasPoint(DraftMouseLogicalX, DraftMouseLogicalY);
                targetDir = PortDirection.Left;
                targetBox = new ScreenBoundingBox(targetPoint.X, targetPoint.Y, 0, 0);
            }
            else
            {
                if (pipe.TargetElement == null)
                    return new SvgRenderData("");

                var targetCoords = pipe.TargetElement.GetAbsolutePortCoordinates(pipe.TargetPortName);
                targetPoint = new CanvasPoint(targetCoords.X, targetCoords.Y);
                targetDir = targetCoords.Direction;
                targetBox = GetScreenBoundingBox(pipe.TargetElement);
            }

            var sourceBox = GetScreenBoundingBox(pipe.SourceElement);

            // ─────────────────────────────────────────────────────────
            // 2. Normalizar A/B (A siempre a la izquierda) y Obstáculos
            // ─────────────────────────────────────────────────────────
            var sourceNode = (sourcePoint, sourceCoords.Direction, sourceBox);
            var targetNode = (targetPoint, targetDir, targetBox);

            // Obstáculos separados inteligentemente
            var equipmentObstacles = _Elements
                .Where(e => e.Id != pipe.SourceElement.Id &&
                       (pipe.TargetElement == null || e.Id != pipe.TargetElement.Id) &&
                       !(e is PipeVisualElement)); // Asegurarnos de que no entren tuberías aquí

            var pipeObstacles = _Pipes.Where(p => p.Id != pipe.Id);

            var ctx = CreateContext(sourceNode, targetNode, equipmentObstacles, pipeObstacles, out bool wasSwapped);

            ctx = ctx with
            {
                SafeStart = CalculateSafePoint(ctx.A, ctx.ADir),
                SafeEnd = CalculateSafePoint(ctx.B, ctx.BDir)
            };

            // ─────────────────────────────────────────────────────────
            // 3. Ejecutar TryAvoidRouter
            // ─────────────────────────────────────────────────────────
            var _router = new TryAvoidRouter(ctx, _collision, _safeCalc);
            var routePoints = _router.CalculateRoute(ctx);

            // ─────────────────────────────────────────────────────────
            // 4. Ensamblar ruta completa (Solución al "Salto Cuántico")
            // ─────────────────────────────────────────────────────────
            var fullPath = new List<CanvasPoint>();

            if (wasSwapped)
            {
                routePoints.Reverse(); // CRÍTICO: Voltear la ruta intermedia calculada
                fullPath.Add(ctx.B);   // Iniciar en puerto real (Source)
                fullPath.AddRange(routePoints);
                fullPath.Add(ctx.A);   // Terminar en puerto real (Target)
            }
            else
            {
                fullPath.Add(ctx.A);
                fullPath.AddRange(routePoints);
                fullPath.Add(ctx.B);
            }

            // CRÍTICO: Almacenar la ruta COMPLETA en memoria para que otras tuberías la vean
            if (!isDraft)
            {
                pipe.CalculatedRoute = new List<CanvasPoint>(fullPath);
            }

            // ─────────────────────────────────────────────────────────
            // 5. Aplicar cortes visuales (Fase 1.1) y Renderizar
            // ─────────────────────────────────────────────────────────
            if (isDraft)
            {
                // Si estamos arrastrando, no hacemos cortes pesados
                return SvgRouteFormatter.FormatSinglePath(fullPath);
            }

            // Extraer TODOS los segmentos horizontales de las otras tuberías
            var horizontalSegments = new List<GeometryHelper.Segment>();
            var otherPipes = _Pipes
     .Where(p => p.Id != pipe.Id && p.CalculatedRoute != null && p.CalculatedRoute.Any());

            foreach (var otherPipe in otherPipes)
            {
                for (int i = 0; i < otherPipe.CalculatedRoute.Count - 1; i++)
                {
                    var seg = new GeometryHelper.Segment(otherPipe.CalculatedRoute[i], otherPipe.CalculatedRoute[i + 1]);
                    if (seg.IsHorizontal)
                    {
                        horizontalSegments.Add(seg);
                    }
                }
            }
            var equipmentBoxes = equipmentObstacles
               .Select(e => {
                   var box = GetScreenBoundingBox(e);
                   return (box.X, box.Y, box.Width, box.Height);
               }).ToList();
            // CRÍTICO: Usar el formateador que aplica los cortes estéticos
            return SvgRouteFormatter.FormatWithColisionBreaks(fullPath, horizontalSegments, equipmentBoxes);
        }

        private static PipeRoutingContext CreateContext(
            (CanvasPoint Point, PortDirection Dir, ScreenBoundingBox Box) source,
            (CanvasPoint Point, PortDirection Dir, ScreenBoundingBox Box) target,
            IEnumerable<IVisualElement> equipmentObstacles,
            IEnumerable<PipeVisualElement> pipeObstacles,
            out bool wasSwapped)
        {
            wasSwapped = source.Point.X > target.Point.X;

            if (!wasSwapped)
            {
                return new PipeRoutingContext(
                    source.Point, source.Dir,
                    new CanvasPoint(source.Box.X, source.Box.Y), source.Box.Width, source.Box.Height,
                    target.Point, target.Dir,
                    new CanvasPoint(target.Box.X, target.Box.Y), target.Box.Width, target.Box.Height,
                    new CanvasPoint(0, 0), new CanvasPoint(0, 0),
                    equipmentObstacles,
                    pipeObstacles);
            }
            else
            {
                return new PipeRoutingContext(
                    target.Point, target.Dir,
                    new CanvasPoint(target.Box.X, target.Box.Y), target.Box.Width, target.Box.Height,
                    source.Point, source.Dir,
                    new CanvasPoint(source.Box.X, source.Box.Y), source.Box.Width, source.Box.Height,
                    new CanvasPoint(0, 0), new CanvasPoint(0, 0),
                    equipmentObstacles,
                    pipeObstacles);
            }
        }

        private static ScreenBoundingBox GetScreenBoundingBox(IVisualElement el)
        {
            double w = el.Width;
            double h = el.Height;
            int rot = el.RotationAngle % 360;

            // Si rota 90° o 270°, Width y Height se intercambian
            double screenW = (rot == 90 || rot == 270) ? h : w;
            double screenH = (rot == 90 || rot == 270) ? w : h;

            // El CSS rota alrededor del centro, pero left/top es la esquina.
            // Si W != H y rota, la esquina visual se desplaza.
            double dx = (w - screenW) / 2.0;
            double dy = (h - screenH) / 2.0;

            return new ScreenBoundingBox(el.X + dx, el.Y + dy, screenW, screenH);
        }

        private static CanvasPoint CalculateSafePoint(CanvasPoint port, PortDirection dir)
        {
            return dir switch
            {
                PortDirection.Top => new CanvasPoint(port.X, port.Y - SAFE_MARGIN),
                PortDirection.Bottom => new CanvasPoint(port.X, port.Y + SAFE_MARGIN),
                PortDirection.Left => new CanvasPoint(port.X - SAFE_MARGIN, port.Y),
                PortDirection.Right => new CanvasPoint(port.X + SAFE_MARGIN, port.Y),
                _ => port
            };
        }
    }
   
    public record PipeRoutingContext(
    // Equipo A (Siempre Izquierda)
    CanvasPoint A, PortDirection ADir, CanvasPoint AEquipPos, double AWidth, double AHeight,

    // Equipo B (Siempre Derecha)
    CanvasPoint B, PortDirection BDir, CanvasPoint BEquipPos, double BWidth, double BHeight,

    // Puntos Seguros
    CanvasPoint SafeStart, CanvasPoint SafeEnd,

    // 👇 Obstáculos separados inteligentemente
    IEnumerable<IVisualElement> EquipmentObstacles,
    IEnumerable<PipeVisualElement> PipeObstacles)
    {
        public bool IsVerticalPrimary => ADir is PortDirection.Top or PortDirection.Bottom;
        public bool IsHorizontalPrimary => ADir is PortDirection.Left or PortDirection.Right;
    }
  
    public record ScreenBoundingBox(double X, double Y, double Width, double Height);
}
