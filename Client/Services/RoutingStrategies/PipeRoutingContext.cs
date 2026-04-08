using Shared.ProcessFlowDiagram;

namespace Client.Services.RoutingStrategies
{
    
    //public class TryAvoidRouter2
    //{
    //    private const double MARGIN = 30.0;
    //    private const double EPSILON = 0.1;

    //    /// <summary>
    //    /// Calcula la ruta óptima Try/Avoid entre SafeStart y SafeEnd.
    //    /// </summary>
    //    public List<CanvasPoint> CalculateRoute(PipeRoutingContext ctx)
    //    {
    //        var route = new List<CanvasPoint> { ctx.SafeStart };

    //        // Determinar eje primario según puerto A
    //        if (ctx.IsVerticalPrimary)
    //        {
    //            // Puerto Vertical: Intentar Vertical primero
    //            if (!TryVerticalThenHorizontal(ctx, route))
    //            {
    //                // Fallback: Horizontal primero con SafeX
    //                TryHorizontalThenVerticalWithAvoid(ctx, route);
    //            }
    //        }
    //        else
    //        {
    //            // Puerto Horizontal: Intentar Horizontal primero
    //            if (!TryHorizontalThenVertical(ctx, route))
    //            {
    //                // Fallback: Vertical primero con SafeY
    //                TryVerticalThenHorizontalWithAvoid(ctx, route);
    //            }
    //        }

    //        // Asegurar que termina en SafeEnd
    //        AddPointIfDifferent(route, ctx.SafeEnd);

    //        return route;
    //    }

    //    // ─────────────────────────────────────────────────────────
    //    // ESTRATEGIAS TRY
    //    // ─────────────────────────────────────────────────────────

    //    private bool TryVerticalThenHorizontal(PipeRoutingContext ctx, List<CanvasPoint> route)
    //    {
    //        var current = route.Last();
    //        var via = new CanvasPoint(current.X, ctx.SafeEnd.Y);
    //        var target = ctx.SafeEnd;

    //        // Try 1: Vertical hasta Y de destino
    //        if (HasCollision(current, via, ctx)) return false;

    //        route.Add(via);

    //        // Try 2: Horizontal hasta X de destino
    //        if (HasCollision(via, target, ctx))
    //        {
    //            route.RemoveAt(route.Count - 1); // Backtrack
    //            return false;
    //        }

    //        route.Add(target);
    //        return true;
    //    }

    //    private bool TryHorizontalThenVertical(PipeRoutingContext ctx, List<CanvasPoint> route)
    //    {
    //        var current = route.Last();
    //        var via = new CanvasPoint(ctx.SafeEnd.X, current.Y);
    //        var target = ctx.SafeEnd;

    //        // Try 1: Horizontal hasta X de destino
    //        if (HasCollision(current, via, ctx)) return false;

    //        route.Add(via);

    //        // Try 2: Vertical hasta Y de destino
    //        if (HasCollision(via, target, ctx))
    //        {
    //            route.RemoveAt(route.Count - 1); // Backtrack
    //            return false;
    //        }

    //        route.Add(target);
    //        return true;
    //    }

    //    // ─────────────────────────────────────────────────────────
    //    // ESTRATEGIAS AVOID + RETRY
    //    // ─────────────────────────────────────────────────────────

    //    private void TryVerticalThenHorizontalWithAvoid(PipeRoutingContext ctx, List<CanvasPoint> route)
    //    {
    //        var current = route.Last();

    //        // Calcular SafeY fuera de B + margen
    //        double safeY = CalculateSafeY(ctx);
    //        var via1 = new CanvasPoint(current.X, safeY);

    //        route.Add(via1);

    //        // Horizontal en SafeY
    //        var via2 = new CanvasPoint(ctx.SafeEnd.X, safeY);
    //        route.Add(via2);

    //        // Vertical final a SafeEnd
    //        route.Add(ctx.SafeEnd);
    //    }

    //    private void TryHorizontalThenVerticalWithAvoid(PipeRoutingContext ctx, List<CanvasPoint> route)
    //    {
    //        var current = route.Last();

    //        // Calcular SafeX fuera de B + margen
    //        double safeX = CalculateSafeX(ctx);
    //        var via1 = new CanvasPoint(safeX, current.Y);

    //        route.Add(via1);

    //        // Vertical en SafeX
    //        var via2 = new CanvasPoint(safeX, ctx.SafeEnd.Y);
    //        route.Add(via2);

    //        // Horizontal final a SafeEnd
    //        route.Add(ctx.SafeEnd);
    //    }

    //    // ─────────────────────────────────────────────────────────
    //    // CÁLCULO DE COORDENADAS SEGURAS
    //    // ─────────────────────────────────────────────────────────

    //    private double CalculateSafeY(PipeRoutingContext ctx)
    //    {
    //        // ─────────────────────────────────────────────────────────
    //        // REGLA CRÍTICA: SafeY debe estar del mismo lado que SafeEnd.
    //        // Si SafeEnd está arriba de B, SafeY debe ir arriba.
    //        // Si SafeEnd está abajo de B, SafeY debe ir abajo.
    //        // Esto evita que el tramo final vertical cruce el equipo B.
    //        // ─────────────────────────────────────────────────────────

    //        double bTop = ctx.BEquipPos.Y;
    //        double bBottom = ctx.BEquipPos.Y + ctx.BHeight;

    //        bool safeEndAbove = ctx.SafeEnd.Y < bTop;
    //        bool safeEndBelow = ctx.SafeEnd.Y > bBottom;

    //        bool goAbove;

    //        if (safeEndAbove)
    //        {
    //            // SafeEnd está claramente arriba → forzar arriba
    //            goAbove = true;
    //        }
    //        else if (safeEndBelow)
    //        {
    //            // SafeEnd está claramente abajo → forzar abajo
    //            goAbove = false;
    //        }
    //        else
    //        {
    //            // SafeEnd está alineado verticalmente con B (raro para SafeEnd, pero fallback)
    //            // Usar posición de SafeStart como desempate
    //            goAbove = ctx.SafeStart.Y < ctx.BEquipPos.Y + ctx.BHeight / 2;
    //        }

    //        if (goAbove)
    //        {
    //            return bTop - MARGIN;
    //        }
    //        else
    //        {
    //            return bBottom + MARGIN;
    //        }
    //    }

    //    private double CalculateSafeX(PipeRoutingContext ctx)
    //    {
    //        // Para B, siempre ir por la derecha (B está a la derecha)
    //        return ctx.BEquipPos.X + ctx.BWidth + MARGIN;
    //    }

    //    // ─────────────────────────────────────────────────────────
    //    // DETECCIÓN DE COLISIONES
    //    // ─────────────────────────────────────────────────────────

    //    private bool HasCollision(CanvasPoint p1, CanvasPoint p2, PipeRoutingContext ctx)
    //    {
    //        // Verificar colisión con A
    //        if (IntersectsRect(p1, p2, ctx.AEquipPos, ctx.AWidth, ctx.AHeight))
    //            return true;

    //        // Verificar colisión con B
    //        if (IntersectsRect(p1, p2, ctx.BEquipPos, ctx.BWidth, ctx.BHeight))
    //            return true;

    //        return false;
    //    }

    //    private bool IntersectsRect(CanvasPoint p1, CanvasPoint p2, CanvasPoint rectPos, double w, double h)
    //    {
    //        double minX = Math.Min(p1.X, p2.X);
    //        double maxX = Math.Max(p1.X, p2.X);
    //        double minY = Math.Min(p1.Y, p2.Y);
    //        double maxY = Math.Max(p1.Y, p2.Y);

    //        double rectMinX = rectPos.X;
    //        double rectMaxX = rectPos.X + w;
    //        double rectMinY = rectPos.Y;
    //        double rectMaxY = rectPos.Y + h;

    //        // Verificar solapamiento de bounding boxes
    //        if (maxX < rectMinX || minX > rectMaxX || maxY < rectMinY || minY > rectMaxY)
    //            return false;

    //        // Segmento vertical
    //        if (Math.Abs(p1.X - p2.X) < EPSILON)
    //        {
    //            return p1.X > rectMinX && p1.X < rectMaxX;
    //        }

    //        // Segmento horizontal
    //        if (Math.Abs(p1.Y - p2.Y) < EPSILON)
    //        {
    //            return p1.Y > rectMinY && p1.Y < rectMaxY;
    //        }

    //        return false;
    //    }

    //    private void AddPointIfDifferent(List<CanvasPoint> route, CanvasPoint pt)
    //    {
    //        var last = route.Last();
    //        if (Math.Abs(last.X - pt.X) > EPSILON || Math.Abs(last.Y - pt.Y) > EPSILON)
    //        {
    //            route.Add(pt);
    //        }
    //    }
    //}
}
