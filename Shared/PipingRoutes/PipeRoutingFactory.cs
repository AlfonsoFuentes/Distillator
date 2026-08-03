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
            if (isDraft && Math.Abs(DraftMouseLogicalX) < 0.1 && Math.Abs(DraftMouseLogicalY) < 0.1)
            {
                // Si el código entra aquí cuando NO debería, sabemos que el error 
                // viene de las variables de estado que llegan al Factory.
            }
            if (isDraft)
            {
                var mousePt = new CanvasPoint(DraftMouseLogicalX, DraftMouseLogicalY);
                var draftRoute = OrthogonalPipeRouter.RouteDraft(sourcePoint, sourceCoords.Direction, mousePt);
                return SvgRouteFormatter.FormatSinglePath(draftRoute);
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
            var sourcePort = GetPortOrDefault(pipe.SourceElement, pipe.SourcePortName, PortType.Outlet);
            var targetPort = pipe.TargetElement is null
                ? null
                : GetPortOrDefault(pipe.TargetElement, pipe.TargetPortName, PortType.Inlet);

            // ─────────────────────────────────────────────────────────
            // 2. Normalizar A/B y Obstáculos
            // ─────────────────────────────────────────────────────────
            var sourceNode = (sourcePoint, sourceCoords.Direction, sourceBox);
            var targetNode = (targetPoint, targetDir, targetBox);

            // Obstáculos: Excluir explícitamente el equipo de origen y destino
            var equipmentObstacles = _Elements
                .Where(e => e.Id != pipe.SourceElement.Id &&
                            (pipe.TargetElement == null || e.Id != pipe.TargetElement.Id) &&
                            !(e is PipeVisualElement));

            //var equipmentObstacles = _Elements
            //    .Where(e => e.Id != pipe.SourceElement.Id &&
            //           (pipe.TargetElement == null || e.Id != pipe.TargetElement.Id) &&
            //           !(e is PipeVisualElement));

            var pipeObstacles = _Pipes.Where(p => p.Id != pipe.Id);

            var ctx = CreateContext(sourceNode, targetNode, equipmentObstacles, pipeObstacles, out bool wasSwapped);

            ctx = ctx with
            {
                SafeStart = CalculateSafePoint(ctx.A, ctx.ADir),
                SafeEnd = CalculateSafePoint(ctx.B, ctx.BDir)
            };

            if (isDraft)
            {
                if (wasSwapped)
                    ctx = ctx with { SafeStart = ctx.A }; // El ratón quedó en A
                else
                    ctx = ctx with { SafeEnd = ctx.B };   // El ratón quedó en B
            }
            var obstacleBoxes = equipmentObstacles
                .Select(GetScreenBoundingBox)
                .ToList();

            var sourceEndpoint = new OrthogonalRoutingEndpoint(
                ctx.A,
                ctx.ADir,
                new ScreenBoundingBox(ctx.AEquipPos.X, ctx.AEquipPos.Y, ctx.AWidth, ctx.AHeight),
                sourcePort.Type,
                GetEndpointOwnerKind(pipe.SourceElement));

            var targetEndpoint = new OrthogonalRoutingEndpoint(
                ctx.B,
                ctx.BDir,
                new ScreenBoundingBox(ctx.BEquipPos.X, ctx.BEquipPos.Y, ctx.BWidth, ctx.BHeight),
                targetPort?.Type ?? PortType.Inlet,
                GetEndpointOwnerKind(pipe.TargetElement));

            var routeSource = sourceEndpoint;
            var routeTarget = targetEndpoint;
            if (sourceEndpoint.PortType == PortType.Inlet && targetEndpoint.PortType == PortType.Outlet)
            {
                routeSource = targetEndpoint;
                routeTarget = sourceEndpoint;
            }

            var fullPath = TryBuildSameSideFlowTemplate(routeSource, routeTarget, obstacleBoxes)
                ?? OrthogonalPipeRouter.Route(new OrthogonalRoutingRequest(
                    routeSource,
                    routeTarget,
                    obstacleBoxes));

            if (!isDraft)
            {
                pipe.CalculatedRoute = new List<CanvasPoint>(fullPath);
            }

            // ─────────────────────────────────────────────────────────
            // 5. Aplicar cortes visuales y Renderizar
            // ─────────────────────────────────────────────────────────
            if (isDraft)
            {
                return SvgRouteFormatter.FormatSinglePath(fullPath);
            }

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
            return SvgRouteFormatter.FormatWithColisionBreaks(fullPath, horizontalSegments, new List<(double X, double Y, double Width, double Height)>());
        }
        public static SvgRenderData GetRoute2(PipeVisualElement pipe, bool isDraft, 
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
            wasSwapped = false;

            return new PipeRoutingContext(
                source.Point, source.Dir,
                new CanvasPoint(source.Box.X, source.Box.Y), source.Box.Width, source.Box.Height,
                target.Point, target.Dir,
                new CanvasPoint(target.Box.X, target.Box.Y), target.Box.Width, target.Box.Height,
                new CanvasPoint(0, 0), new CanvasPoint(0, 0),
                equipmentObstacles,
                pipeObstacles);
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

            // 🔥 LA PIEZA FINAL DEL PUZLE:
            // 1. Recortamos el espacio de la etiqueta (LABEL_SPACE) para que el router
            //    no la vea como un obstáculo.
            // 2. Aplicamos un INSET (margen pequeño) para que la tubería pueda "nacer" 
            //    ligeramente dentro del equipo sin detectar choque propio.
            double LABEL_SPACE = 28.0;
            double INSET = 2.0;

            return new ScreenBoundingBox(
                el.X + dx + INSET,
                el.Y + dy + INSET,
                screenW - (INSET * 2),
                screenH - LABEL_SPACE - (INSET * 2)
            );
        }

        private static EquipmentPort GetPortOrDefault(IVisualElement element, string portName, PortType fallbackType)
        {
            return element.Ports.FirstOrDefault(port => string.Equals(port.Name, portName, StringComparison.OrdinalIgnoreCase))
                ?? new EquipmentPort
                {
                    Name = portName,
                    Type = fallbackType,
                    Direction = PortDirection.Right
                };
        }

        private static OrthogonalEndpointOwnerKind GetEndpointOwnerKind(IVisualElement? element)
        {
            return element?.Type switch
            {
                EquipmentType.MaterialStream or EquipmentType.EnergyStream => OrthogonalEndpointOwnerKind.Stream,
                EquipmentType.OffPageConnector => OrthogonalEndpointOwnerKind.OffPageConnector,
                EquipmentType.None or null => OrthogonalEndpointOwnerKind.Other,
                _ => OrthogonalEndpointOwnerKind.Equipment
            };
        }

        private static List<CanvasPoint>? TryBuildSameSideFlowTemplate(
            OrthogonalRoutingEndpoint source,
            OrthogonalRoutingEndpoint target,
            IReadOnlyCollection<ScreenBoundingBox> obstacleBoxes)
        {
            if (source.PortType != PortType.Outlet ||
                target.PortType != PortType.Inlet ||
                source.Direction != target.Direction ||
                source.OwnerKind == OrthogonalEndpointOwnerKind.Other ||
                target.OwnerKind == OrthogonalEndpointOwnerKind.Other)
            {
                return null;
            }

            var sourceEscape = CalculateSafePoint(source.Port, source.Direction);
            var targetApproach = CalculateSafePoint(target.Port, target.Direction);
            const double laneMargin = SAFE_MARGIN;

            var route = source.Direction switch
            {
                PortDirection.Right => BuildSameSideHorizontalTemplate(
                    source.Port,
                    sourceEscape,
                    targetApproach,
                    target.Port,
                    Math.Max(sourceEscape.X, targetApproach.X) + laneMargin),

                PortDirection.Left => BuildSameSideHorizontalTemplate(
                    source.Port,
                    sourceEscape,
                    targetApproach,
                    target.Port,
                    Math.Min(sourceEscape.X, targetApproach.X) - laneMargin),

                PortDirection.Bottom => BuildSameSideVerticalTemplate(
                    source.Port,
                    sourceEscape,
                    targetApproach,
                    target.Port,
                    Math.Max(sourceEscape.Y, targetApproach.Y) + laneMargin),

                PortDirection.Top => BuildSameSideVerticalTemplate(
                    source.Port,
                    sourceEscape,
                    targetApproach,
                    target.Port,
                    Math.Min(sourceEscape.Y, targetApproach.Y) - laneMargin),

                _ => null
            };

            if (route is null)
            {
                return null;
            }

            var normalized = NormalizeRoute(route);
            return IsRouteClear(normalized, obstacleBoxes) ? normalized : null;
        }

        private static List<CanvasPoint> BuildSameSideHorizontalTemplate(
            CanvasPoint source,
            CanvasPoint sourceEscape,
            CanvasPoint targetApproach,
            CanvasPoint target,
            double laneX)
        {
            return new List<CanvasPoint>
            {
                source,
                sourceEscape,
                new(laneX, sourceEscape.Y),
                new(laneX, targetApproach.Y),
                targetApproach,
                target
            };
        }

        private static List<CanvasPoint> BuildSameSideVerticalTemplate(
            CanvasPoint source,
            CanvasPoint sourceEscape,
            CanvasPoint targetApproach,
            CanvasPoint target,
            double laneY)
        {
            return new List<CanvasPoint>
            {
                source,
                sourceEscape,
                new(sourceEscape.X, laneY),
                new(targetApproach.X, laneY),
                targetApproach,
                target
            };
        }

        private static List<CanvasPoint> NormalizeRoute(List<CanvasPoint> route)
        {
            var result = new List<CanvasPoint>();
            foreach (var point in route)
            {
                if (result.Count == 0 || !IsSamePoint(result[^1], point))
                {
                    result.Add(point);
                }
            }

            for (var i = result.Count - 2; i > 0; i--)
            {
                if (IsRedundantCollinearPoint(result[i - 1], result[i], result[i + 1]))
                {
                    result.RemoveAt(i);
                }
            }

            return result;
        }

        private static bool IsSamePoint(CanvasPoint a, CanvasPoint b) =>
            Math.Abs(a.X - b.X) < 0.1 && Math.Abs(a.Y - b.Y) < 0.1;

        private static bool IsRouteClear(List<CanvasPoint> route, IReadOnlyCollection<ScreenBoundingBox> obstacleBoxes)
        {
            for (var i = 0; i < route.Count - 1; i++)
            {
                if (obstacleBoxes.Any(box => SegmentIntersectsBoxInterior(route[i], route[i + 1], box)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SegmentIntersectsBoxInterior(CanvasPoint start, CanvasPoint end, ScreenBoundingBox box)
        {
            var minX = Math.Min(start.X, end.X);
            var maxX = Math.Max(start.X, end.X);
            var minY = Math.Min(start.Y, end.Y);
            var maxY = Math.Max(start.Y, end.Y);
            var boxMaxX = box.X + box.Width;
            var boxMaxY = box.Y + box.Height;

            if (maxX <= box.X || minX >= boxMaxX || maxY <= box.Y || minY >= boxMaxY)
            {
                return false;
            }

            if (Math.Abs(start.X - end.X) < 0.1)
            {
                return start.X > box.X && start.X < boxMaxX;
            }

            if (Math.Abs(start.Y - end.Y) < 0.1)
            {
                return start.Y > box.Y && start.Y < boxMaxY;
            }

            return true;
        }

        private static bool IsRedundantCollinearPoint(CanvasPoint a, CanvasPoint b, CanvasPoint c)
        {
            var sameX = Math.Abs(a.X - b.X) < 0.1 && Math.Abs(b.X - c.X) < 0.1;
            var sameY = Math.Abs(a.Y - b.Y) < 0.1 && Math.Abs(b.Y - c.Y) < 0.1;
            if (!sameX && !sameY)
            {
                return false;
            }

            var betweenX = b.X >= Math.Min(a.X, c.X) - 0.1 &&
                           b.X <= Math.Max(a.X, c.X) + 0.1;
            var betweenY = b.Y >= Math.Min(a.Y, c.Y) - 0.1 &&
                           b.Y <= Math.Max(a.Y, c.Y) + 0.1;

            return betweenX && betweenY;
        }

        private static ScreenBoundingBox GetScreenBoundingBox2(IVisualElement el)
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
