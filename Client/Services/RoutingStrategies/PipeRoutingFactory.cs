using Client.Services.EquipmentManagers;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Services.RoutingStrategies
{
    public record PipeRoutingContext(
         // Equipo A (Siempre Izquierda)
         CanvasPoint A, PortDirection ADir, CanvasPoint AEquipPos, double AWidth, double AHeight,

         // Equipo B (Siempre Derecha)
         CanvasPoint B, PortDirection BDir, CanvasPoint BEquipPos, double BWidth, double BHeight,

         // Puntos Seguros (Calculados por el Factory)
         CanvasPoint SafeStart, CanvasPoint SafeEnd,

         // Obstáculos
         IEnumerable<IVisualElement> Obstacles)
    {
        public bool IsVerticalPrimary => ADir is PortDirection.Top or PortDirection.Bottom;
        public bool IsHorizontalPrimary => ADir is PortDirection.Left or PortDirection.Right;
    }
    /// <summary>
    /// Factory que orquesta el ruteo en 4 pasos:
    /// 1. Normalizar A/B
    /// 2. Calcular salidas seguras (30px)
    /// 3. Ejecutar TryAvoidRouter
    /// 4. Ensamblar y formatear resultado
    /// </summary>
    public static class PipeRoutingFactory
    {
        private const double SAFE_MARGIN = 30.0;
        private static readonly ICollisionDetector _collision = new CollisionDetector();
        private static readonly ISafePointCalculator _safeCalc = new SafePointCalculator();
       

        public static SvgRenderData GetRoute(PipeVisualElement pipe, bool isDraft, WorkspaceManager wm)
        {
            if (pipe.SourceElement == null)
                return new SvgRenderData("");
           
            // ─────────────────────────────────────────────────────────
            // 0. Extraer coordenadas absolutas de puertos
            // GetAbsolutePortCoordinates YA retorna dirección transformada
            // ─────────────────────────────────────────────────────────
            var sourceCoords = pipe.SourceElement.GetAbsolutePortCoordinates(pipe.SourcePortName);
            var sourcePoint = new CanvasPoint(sourceCoords.X, sourceCoords.Y);

            CanvasPoint targetPoint;
            PortDirection targetDir;
            ScreenBoundingBox targetBox;

            if (isDraft)
            {
                targetPoint = new CanvasPoint(wm.DraftMouseLogicalX, wm.DraftMouseLogicalY);
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

                // 👇 CLAVE: Calcular caja real en pantalla con rotación
                targetBox = GetScreenBoundingBox(pipe.TargetElement);
            }

            // 👇 Caja real de Source
            var sourceBox = GetScreenBoundingBox(pipe.SourceElement);

            // ─────────────────────────────────────────────────────────
            // 1. Normalizar A/B (A siempre izquierda)
            // ─────────────────────────────────────────────────────────
            var sourceNode = (sourcePoint, sourceCoords.Direction, sourceBox);
            var targetNode = (targetPoint, targetDir, targetBox);

            var obstacles = wm.Elements
                .Where(e => e.Id != pipe.SourceElement.Id &&
                       (pipe.TargetElement == null || e.Id != pipe.TargetElement.Id));

            var ctx = CreateContext(sourceNode, targetNode, obstacles, out bool wasSwapped);

            // ─────────────────────────────────────────────────────────
            // 2. Calcular puntos seguros (30px)
            // ─────────────────────────────────────────────────────────
            ctx = ctx with
            {
                SafeStart = CalculateSafePoint(ctx.A, ctx.ADir),
                SafeEnd = CalculateSafePoint(ctx.B, ctx.BDir)
            };

            var _router = new TryAvoidRouter(ctx, _collision, _safeCalc);
          
            var routePoints = _router.CalculateRoute(ctx);

            // ─────────────────────────────────────────────────────────
            // 4. Ensamblar ruta completa
            // ─────────────────────────────────────────────────────────
            var fullPath = new List<CanvasPoint>();
        

            if (wasSwapped)
            {
                // routePoints fue calculado de A (Target) hacia B (Source).
                // Lo invertimos para que la línea fluya visualmente desde Source hacia Target.
                routePoints.Reverse();

                fullPath.Add(ctx.B);            // Iniciamos en el puerto Source real
                fullPath.AddRange(routePoints); // Agregamos la ruta ya enderezada
                fullPath.Add(ctx.A);            // Terminamos en el puerto Target real
            }
            else
            {
                fullPath.Add(ctx.A);            // Iniciamos en el puerto Source real
                fullPath.AddRange(routePoints); // Agregamos la ruta
                fullPath.Add(ctx.B);            // Terminamos en el puerto Target real
            }

            return SvgRouteFormatter.FormatSinglePath(fullPath);
        }

        // 👇 NUEVO: Calcula la caja real en pantalla considerando rotación
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

        private static PipeRoutingContext CreateContext(
            (CanvasPoint Point, PortDirection Dir, ScreenBoundingBox Box) source,
            (CanvasPoint Point, PortDirection Dir, ScreenBoundingBox Box) target,
            IEnumerable<IVisualElement> obstacles,
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
                    obstacles);
            }
            else
            {
                return new PipeRoutingContext(
                    target.Point, target.Dir,
                    new CanvasPoint(target.Box.X, target.Box.Y), target.Box.Width, target.Box.Height,
                    source.Point, source.Dir,
                    new CanvasPoint(source.Box.X, source.Box.Y), source.Box.Width, source.Box.Height,
                    new CanvasPoint(0, 0), new CanvasPoint(0, 0),
                    obstacles);
            }
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

    // 👇 Helper para claridad
    public record ScreenBoundingBox(double X, double Y, double Width, double Height);
}