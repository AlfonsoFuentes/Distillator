using Client.Services.EquipmentManagers;
using OfficeOpenXml.Packaging;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;

namespace Client.Services.RoutingStrategies
{
    public static class RoutingRequestFactory
    {
        public static SvgRenderData GetRoute(PipeVisualElement pipe, bool isDraft, WorkspaceManager wm)
        {
            if (pipe.SourceElement == null) return new SvgRenderData("");

            // 1. EXTRACCIÓN DE DATOS (Lo mismo de antes)
            var sCoords = pipe.SourceElement.GetAbsolutePortCoordinates(pipe.SourcePortName);
            var sPoint = new CanvasPoint(sCoords.X, sCoords.Y);

            CanvasPoint tPoint; PortDirection tDir; double tW = 0, tH = 0; CanvasPoint tEquipPos;

            if (isDraft)
            {
                tPoint = new CanvasPoint(wm.DraftMouseLogicalX, wm.DraftMouseLogicalY);
                tDir = PortDirection.Left; tEquipPos = tPoint;
            }
            else
            {
                if (pipe.TargetElement == null) return new SvgRenderData("");
                var eCoords = pipe.TargetElement.GetAbsolutePortCoordinates(pipe.TargetPortName);
                tPoint = new CanvasPoint(eCoords.X, eCoords.Y); tDir = eCoords.Direction;
                tW = pipe.TargetElement.Width; tH = pipe.TargetElement.Height;
                tEquipPos = new CanvasPoint(pipe.TargetElement.X, pipe.TargetElement.Y);
            }

            // 2. NORMALIZACIÓN (A la izquierda de B)
            //bool swap = sPoint.X > tPoint.X;
            var obstacles = ((IEnumerable<IVisualElement>)wm.Elements)
                .Where(e => e.Id != pipe.SourceElement.Id && (pipe.TargetElement == null || e.Id != pipe.TargetElement.Id));

         

            // 1. Empaquetamos los datos en Tuplas (Limpio y legible)
            var sourceNode = (sPoint, sCoords.Direction, new CanvasPoint(pipe.SourceElement.X, pipe.SourceElement.Y), pipe.SourceElement.Width, pipe.SourceElement.Height);
            var targetNode = (tPoint, tDir, tEquipPos, tW, tH);
            var req = RoutingRequest.CreateNormalized(sourceNode, targetNode, obstacles, out bool swap);

            // 3. EJECUCIÓN DE LA ESTRATEGIA "TRY AND LEARN"
            var router = new PipeJudgeRouter();
            var fullPath = router.GetBestRoute(req);

            // 4. CORRECCIÓN DE FLUJO
            // Si hubo swap, invertimos para que el dibujo empiece en Source y termine en Target
            if (swap) fullPath.Reverse();

            // 5. FORMATEO ÚNICO
            return SvgRouteFormatter.FormatSinglePath(fullPath);
        }
    }

    //public static class RoutingRequestFactory2
    //{
    //    public static SvgRenderData GetRoute(PipeVisualElement pipe, bool isDraft, WorkspaceManager wm)
    //    {
    //        if (pipe.SourceElement == null) return new SvgRenderData("", "", "", "", 0, 0, false);

    //        // 1. EXTRAER DATOS (NORMALIZACIÓN)
    //        var sCoords = pipe.SourceElement.GetAbsolutePortCoordinates(pipe.SourcePortName);
    //        var sPoint = new CanvasPoint(sCoords.X, sCoords.Y);

    //        CanvasPoint tPoint; PortDirection tDir; double tW = 0, tH = 0; CanvasPoint tEquipPos;
    //        if (isDraft)
    //        {
    //            tPoint = new CanvasPoint(wm.DraftMouseLogicalX, wm.DraftMouseLogicalY);
    //            tDir = PortDirection.Left; tEquipPos = tPoint;
    //        }
    //        else
    //        {
    //            if (pipe.TargetElement == null) return new SvgRenderData("", "", "", "", 0, 0, false);
    //            var eCoords = pipe.TargetElement.GetAbsolutePortCoordinates(pipe.TargetPortName);
    //            tPoint = new CanvasPoint(eCoords.X, eCoords.Y); tDir = eCoords.Direction;
    //            tW = pipe.TargetElement.Width; tH = pipe.TargetElement.Height;
    //            tEquipPos = new CanvasPoint(pipe.TargetElement.X, pipe.TargetElement.Y);
    //        }

    //        bool swap = sPoint.X > tPoint.X;
    //        var obstacles = ((IEnumerable<IVisualElement>)wm.Elements).Where(e => e.Id != pipe.SourceElement.Id && (pipe.TargetElement == null || e.Id != pipe.TargetElement.Id));

    //        // Creamos el Request Geométrico (A siempre Izquierda)
    //        var req = !swap
    //            ? new RoutingRequest(sPoint, sCoords.Direction, new CanvasPoint(pipe.SourceElement.X, pipe.SourceElement.Y), pipe.SourceElement.Width, pipe.SourceElement.Height, tPoint, tDir, tEquipPos, tW, tH, obstacles)
    //            : new RoutingRequest(tPoint, tDir, tEquipPos, tW, tH, sPoint, sCoords.Direction, new CanvasPoint(pipe.SourceElement.X, pipe.SourceElement.Y), pipe.SourceElement.Width, pipe.SourceElement.Height, obstacles);

    //        // 2. EJECUCIÓN DINÁMICA (Sin Inyección de Dependencias)

    //        // El Factory elige las estrategias aquí mismo
    //        var stratA = GetSourceStrategy(req.ADir);
    //        var stratB = GetTargetStrategy(req.BDir);

    //        var resA = stratA.Calculate(req);
    //        var resB = stratB.Calculate(req);

    //        // El puente se calcula directamente con los puntos que entregaron A y B
    //        // El puente se calcula directamente con los puntos que entregaron A y B
    //        // ¡Agregamos 'req' como tercer parámetro!
    //        var bridgePoints = BridgeFactory.Connect(resA.TerminalPoint, resB.TerminalPoint, req);

    //        // 3. ENSAMBLAJE
    //        var bPoints = new List<CanvasPoint>(resB.Points);
    //        bPoints.Reverse();

    //        return SvgRouteFormatter.Format(resA.Points, bridgePoints, bPoints);
    //    }

    //    // --- RESOLVERS INTERNOS (Mecanismo de selección) ---

    //    private static ISourceExitStrategy GetSourceStrategy(PortDirection dir) => dir switch
    //    {
    //        PortDirection.Left => new SourceExitLeft(),
    //        PortDirection.Top => new SourceExitTop(),
    //        PortDirection.Bottom => new SourceExitBottom(),
    //        PortDirection.Right => new SourceExitRight(),
    //        _ =>new SourceExitLeft() // Por ahora solo implementamos esta, pero se pueden agregar más
    //        //
           
         
    //        //_ => new SourceExitRight() // Default seguro
    //    };

    //    private static ITargetEntryStrategy GetTargetStrategy(PortDirection dir) => dir switch
    //    {
    //        PortDirection.Left => new TargetEntryLeft(),
    //        PortDirection.Right => new TargetEntryRight(),
    //        PortDirection.Top => new TargetEntryTop(),
    //        PortDirection.Bottom => new TargetEntryBottom(),
    //        _ => new TargetEntryRight() // Default seguro
    //    };
    //}
    //public static class BridgeFactory
    //{
    //    public static List<CanvasPoint> Connect(CanvasPoint h, CanvasPoint r)
    //    {
    //        // 1. Si ya están alineados en X o en Y, es una línea recta perfecta (0 quiebres)
    //        if (Math.Abs(h.X - r.X) < 1 || Math.Abs(h.Y - r.Y) < 1)
    //        {
    //            return new List<CanvasPoint> { h, r };
    //        }

    //        // 2. Si no están alineados, hacemos una simple "L" (1 solo quiebre)
    //        // Codo: Avanzamos todo en X primero, y de ahí caemos/subimos en Y
    //        var corner = new CanvasPoint(r.X, h.Y);

    //        return new List<CanvasPoint> { h, corner, r };
    //    }
    //}

}