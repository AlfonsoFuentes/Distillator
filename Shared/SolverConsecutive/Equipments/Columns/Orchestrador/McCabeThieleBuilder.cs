using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments.Columns.Orchestrador
{
    public sealed class McCabeThieleBuilder : IColumnPostSolverCalculation
    {
        public int Order => 4;
        private readonly SolverColumn _column;

        public McCabeThieleBuilder(SolverColumn column)
        {
            _column = column;
        }

        public async Task CalculateAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Trace("McCabeThiele started", $"FUGChanged={_column.Orchestrator?.FUGChanged}; VLEChanged={_column.Orchestrator?.VLEChanged}; PlatesChanged={_column.Orchestrator?.PlatesChanged}");

            if (_column.Orchestrator == null)
            {
                stopwatch.Stop();
                Trace("McCabeThiele skipped", $"reason=no orchestrator; elapsedMs={stopwatch.ElapsedMilliseconds}");
                return;
            }

            // Caché si no hay cambios relevantes
            if (!_column.Orchestrator.FUGChanged &&
                !_column.Orchestrator.VLEChanged &&
                !_column.Orchestrator.PlatesChanged)
            {
                stopwatch.Stop();
                Trace("McCabeThiele skipped", $"reason=no input changed; elapsedMs={stopwatch.ElapsedMilliseconds}");
                return;
            }

            var columnResult = _column.Orchestrator.CurrentResult;
            if (columnResult == null)
            {
                _column.Orchestrator.SetMcCabeThieleData(CreateEmptyData());
                stopwatch.Stop();
                Trace("McCabeThiele skipped", $"reason=no column result; elapsedMs={stopwatch.ElapsedMilliseconds}");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    var diagonalLine = new List<(double x, double y)> { (0, 0), (1, 1) };

                    var vlePoints = columnResult.VLECurve.Points
                        .OrderBy(p => p.x)
                        .Select(p => (p.x, p.y))
                        .ToList();

                    if (!vlePoints.Any())
                    {
                        _column.Orchestrator?.SetMcCabeThieleData(
                            CreatePartialData(diagonalLine, vlePoints, "VLE curve data is not available."));
                        return;
                    }

                    // 1. Obtener parámetros FUG
                    var fug = columnResult.DistillationParameters;
                    double xD = fug?.xD ?? 0;
                    double xB = fug?.xB ?? 0;
                    double R = fug?.RefluxRatio.Value ?? 0;
                    double R_min = fug?.MinRefluxRatio.Value ?? 0;
                    double q = fug?.FeedQuality ?? 1.0;

                    // 2. Obtener zF del componente MÁS LIGERO del feed
                    var feedComposition = _column.Feeds.FirstOrDefault()?.Composition;
                    double zF = 0;

                    if (feedComposition != null && feedComposition.Components.Any())
                    {
                        var sortedComponents = feedComposition.Components.OrderBy(c =>
                        {
                            return c.DataBase.FullData.BoilingPoint?.GetValue(TemperatureUnits.Kelvin) ?? 0;
                        }).ToList();

                        var lightestComponent = sortedComponents.FirstOrDefault();
                        if (lightestComponent != null)
                        {
                            zF = lightestComponent.MolarFraction.GetSolverValue();
                        }
                    }

                    // 3. MAPA TERMODINÁMICO: Derivar x_feed e y_feed explícitos
                    double x_feed = zF;
                    double y_feed = zF;
                    var feedState = _column.Feeds.FirstOrDefault()?.ThermodynamicState;

                    if (feedState == ThermodynamicState.SaturatedVapor ||
                        feedState == ThermodynamicState.SuperheatedVapor)
                    {
                        y_feed = zF;
                        x_feed = FindXForY(vlePoints, zF);
                    }
                    else if (feedState == ThermodynamicState.SubcooledLiquid ||
                             feedState == ThermodynamicState.SaturatedLiquid)
                    {
                        x_feed = zF;
                        y_feed = InterpolateVLE(vlePoints, zF);
                    }

                    // Validación básica
                    if (xD <= 0 || xB < 0 || R <= 0 || zF <= 0)
                    {
                        _column.Orchestrator?.SetMcCabeThieleData(
                            CreatePartialData(
                                diagonalLine,
                                vlePoints,
                                $"Insufficient McCabe-Thiele inputs: xD={xD:F4}, xB={xB:F4}, R={R:F4}, zF={zF:F4}."));
                        return;
                    }

                    // 4. Cálculos geométricos usando zF directamente
                    double x_int_q, y_int_q;
                    FindQLineVLEIntersection(vlePoints, zF, q, out x_int_q, out y_int_q);

                    double x_int, y_int;
                    CalculateOperatingLinesIntersection(xD, xB, R, zF, q, out x_int, out y_int);

                    // Líneas operativas
                    var rectifyingLine = new List<(double x, double y)> { (x_int, y_int), (xD, xD) };
                    var strippingLine = new List<(double x, double y)> { (xB, xB), (x_int, y_int) };

                    var minRefluxRectifyingLine = R_min > 0
                        ? new List<(double x, double y)> { (x_int_q, y_int_q), (xD, xD) }
                        : new List<(double x, double y)>();

                    var minRefluxStrippingLine = R_min > 0
                        ? new List<(double x, double y)> { (xB, xB), (x_int_q, y_int_q) }
                        : new List<(double x, double y)>();

                    // Proyecciones estándar
                    var projectionLinesD = new List<(double x, double y)> { (xD, 0), (xD, xD) };
                    var projectionLinesB = new List<(double x, double y)> { (xB, 0), (xB, xB) };

                    // ✅ TAREA 1: Línea q unificada (con fix para q=1)
                    var projectionLinesF = BuildUnifiedFeedLine(zF, q, x_int, y_int);

                    // ✅ TAREA 2: Líneas punteadas auxiliares
                    var projectionLinesFToX = BuildProjectionFromFToX(zF);
                    var projectionLinesIntersectToY = BuildProjectionFromIntersectToY(x_int, y_int);

                    var markers = BuildMarkers(xD, xB, zF, q, R);
                    var staircaseSteps = BuildStaircaseSteps(vlePoints, xD, xB, R, x_feed, x_int, y_int);

                    var mcCabeData = new McCabeThieleData
                    {
                        DiagonalLine = diagonalLine,
                        VLECurve = vlePoints,
                        RectifyingLine = rectifyingLine,
                        StrippingLine = strippingLine,
                        StaircaseSteps = staircaseSteps,
                        Markers = markers,
                        MinRefluxRectifyingLine = minRefluxRectifyingLine,
                        MinRefluxStrippingLine = minRefluxStrippingLine,
                        ProjectionLinesD = projectionLinesD,
                        ProjectionLinesB = projectionLinesB,
                        ProjectionLinesF = projectionLinesF,
                        ProjectionLinesFToX = projectionLinesFToX,              // ✅ NUEVO
                        ProjectionLinesIntersectToY = projectionLinesIntersectToY, // ✅ NUEVO
                        MinRefluxRatio = R_min,
                        ChartTitle = "McCabe-Thiele Diagram",
                        ChartSubtitle = $"Reflux Ratio R = {R:F2} | Stages  = {columnResult.Stages.Count:F2}"
                    };

                    _column.Orchestrator?.SetMcCabeThieleData(mcCabeData);
                }
                catch (Exception)
                {
                    _column.Orchestrator?.SetMcCabeThieleData(CreateEmptyData());
                }
            }, cancellationToken);

            stopwatch.Stop();
            var mcCabeThiele = _column.Orchestrator.CurrentResult.McCabeThiele;
            Trace("McCabeThiele finished", $"elapsedMs={stopwatch.ElapsedMilliseconds}; vlePoints={mcCabeThiele?.VLECurve.Count ?? 0}; stages={mcCabeThiele?.StaircaseSteps.Count ?? 0}");
        }

        private void Trace(string message, string? detail = null)
        {
            _column.TraceSink?.TraceSolver($"Column {_column.Name}: {message}", detail);
        }

        /// <summary>
        /// Construye la línea de alimentación unificada.
        /// FIX: Para q=1 (líquido saturado), dibuja línea vertical continua sin quiebre.
        /// </summary>
        private List<(double x, double y)> BuildUnifiedFeedLine(double zF, double q, double x_int, double y_int)
        {
            var lines = new List<(double x, double y)>();

            var anchorPoint = (zF, zF);
            var intersectPoint = (x_int, y_int);

            if (q >= 1.0)
            {
                // Líquidos: Eje X → Intersección
                // ✅ FIX: Para q=1, x_int == zF, así que dibujamos directo sin pasar por anchorPoint
                // Esto evita el "quiebre" visual en la diagonal
                lines.Add((zF, 0));

                // Solo agregar anchorPoint si NO está alineado con el segmento (q > 1)
                if (Math.Abs(q - 1.0) > 0.01)
                {
                    lines.Add(anchorPoint);
                }
            }
            else if (q <= 0.0)
            {
                // Vapores: Eje Y → Diagonal → Intersección
                lines.Add((0, zF));
                lines.Add(anchorPoint);

                // Punto intermedio para vapor sobrecalentado (q < 0)
                if (q < 0)
                {
                    double slope = q / (q - 1.0);
                    double x_mid = (zF + x_int) / 2.0;
                    double y_mid = slope * (x_mid - zF) + zF;
                    lines.Add((x_mid, y_mid));
                }
            }
            else
            {
                // Mezcla bifásica: Solo Diagonal → Intersección
                lines.Add(anchorPoint);
            }

            lines.Add(intersectPoint);

            return lines;
        }

        /// <summary>
        /// ✅ NUEVO: Línea vertical punteada desde F (diagonal) hasta el eje X
        /// </summary>
        private List<(double x, double y)> BuildProjectionFromFToX(double zF)
        {
            return new List<(double x, double y)>
        {
            (zF, zF),  // Punto F en diagonal
            (zF, 0)    // Eje X
        };
        }

        /// <summary>
        /// ✅ NUEVO: Línea horizontal punteada desde la intersección hasta el eje Y
        /// </summary>
        private List<(double x, double y)> BuildProjectionFromIntersectToY(double x_int, double y_int)
        {
            return new List<(double x, double y)>
        {
            (x_int, y_int),  // Punto de intersección
            (0, y_int)       // Eje Y
        };
        }

        private void FindQLineVLEIntersection(
            List<(double x, double y)> vlePoints,
            double zF, double q,
            out double x_int, out double y_int)
        {
            if (Math.Abs(q - 1.0) < 0.01)
            {
                x_int = zF;
                y_int = InterpolateVLE(vlePoints, zF);
            }
            else if (Math.Abs(q) < 0.01)
            {
                y_int = zF;
                x_int = FindXForY(vlePoints, zF);
            }
            else
            {
                double slope = q / (q - 1.0);
                double intercept = -zF / (q - 1.0);

                double left = 0, right = 1;
                for (int iter = 0; iter < 50; iter++)
                {
                    double mid = (left + right) / 2.0;
                    double y_q = slope * mid + intercept;
                    double y_vle = InterpolateVLE(vlePoints, mid);

                    if (Math.Abs(y_q - y_vle) < 1e-6)
                    {
                        x_int = mid;
                        y_int = y_vle;
                        return;
                    }

                    double y_q_left = slope * left + intercept;
                    double y_vle_left = InterpolateVLE(vlePoints, left);

                    if ((y_q_left - y_vle_left) * (y_q - y_vle) < 0)
                        right = mid;
                    else
                        left = mid;
                }

                x_int = (left + right) / 2.0;
                y_int = InterpolateVLE(vlePoints, x_int);
            }
        }

        private void CalculateOperatingLinesIntersection(
            double xD, double xB, double R,
            double zF, double q,
            out double x_int, out double y_int)
        {
            if (Math.Abs(q - 1.0) < 1e-6)
            {
                x_int = zF;
                y_int = (R / (R + 1.0)) * zF + xD / (R + 1.0);
            }
            else
            {
                double R_frac = R / (R + 1.0);
                double D_frac = xD / (R + 1.0);
                double Q_frac1 = q / (q - 1.0);
                double Q_intercept = -zF / (q - 1.0);

                x_int = (Q_intercept - D_frac) / (R_frac - Q_frac1);
                y_int = R_frac * x_int + D_frac;
            }
        }

        private List<MarkerPoint> BuildMarkers(double xD, double xB, double zF, double q, double R)
        {
            var markers = new List<MarkerPoint>();

            markers.Add(new MarkerPoint { Label = "D", X = xD, Y = 0, Description = "Distillate", TooltipData = new Dictionary<string, string> { { "x", xD.ToString("F3") }, { "Type", "Distillate" } } });
            markers.Add(new MarkerPoint { Label = "B", X = xB, Y = 0, Description = "Bottoms", TooltipData = new Dictionary<string, string> { { "x", xB.ToString("F3") }, { "Type", "Bottoms" } } });

            // F SIEMPRE en la diagonal (zF, zF)
            markers.Add(new MarkerPoint
            {
                Label = "F",
                X = zF,
                Y = zF,
                Description = $"Feed (q={q:F2})",
                TooltipData = new Dictionary<string, string>
            {
                { "x", zF.ToString("F3") },
                { "y", zF.ToString("F3") },
                { "Type", "Feed" },
                { "Feed Quality (q)", q.ToString("F2") }
            }
            });

            return markers;
        }

        private List<StaircaseStep> BuildStaircaseSteps(List<(double x, double y)> vlePoints, double xD, double xB, double R, double x_feed, double x_int, double y_int)
        {
            var steps = new List<StaircaseStep>();
            if (!vlePoints.Any()) return steps;

            double x_op = xD;
            double y_op = xD;
            int stageNum = 1;
            bool inRectifying = true;

            while (x_op > xB && stageNum <= 150)
            {
                double x_vle = FindXForY(vlePoints, y_op);
                if (x_vle >= x_op) x_vle = x_op - 0.001;

                var point1 = (x_op, y_op);
                var point2 = (x_vle, y_op);

                if (inRectifying && x_vle <= x_int) inRectifying = false;

                double x_op_next = x_vle;
                var strippingDenominator = x_int - xB;
                double y_op_next = inRectifying
                    ? (R / (R + 1.0)) * x_op_next + xD / (R + 1.0)
                    : Math.Abs(strippingDenominator) < 1e-12
                        ? xB
                        : ((y_int - xB) / strippingDenominator) * (x_op_next - xB) + xB;

                var point3 = (x_op_next, y_op_next);
                steps.Add(new StaircaseStep { StageNumber = stageNum, Points = new List<(double x, double y)> { point1, point2, point3 }, StageType = inRectifying ? "Rectifying" : "Stripping" });

                if (x_op_next <= xB || (Math.Abs(y_op - y_op_next) < 1e-6 && Math.Abs(x_op - x_vle) < 1e-6)) break;

                x_op = x_op_next;
                y_op = y_op_next;
                stageNum++;
            }
            return steps;
        }

        private double InterpolateVLE(List<(double x, double y)> vlePoints, double x)
        {
            if (x <= vlePoints.First().x) return vlePoints.First().y;
            if (x >= vlePoints.Last().x) return vlePoints.Last().y;

            int left = 0, right = vlePoints.Count - 1;
            while (left < right - 1)
            {
                int mid = (left + right) / 2;
                if (vlePoints[mid].x <= x) left = mid; else right = mid;
            }

            double diff = vlePoints[right].x - vlePoints[left].x;
            if (Math.Abs(diff) < 1e-10) return vlePoints[left].y;

            return vlePoints[left].y + (vlePoints[right].y - vlePoints[left].y) * (x - vlePoints[left].x) / diff;
        }

        private double FindXForY(List<(double x, double y)> vlePoints, double targetY)
        {
            if (targetY >= vlePoints.Last().y) return vlePoints.Last().x;
            if (targetY <= vlePoints.First().y) return vlePoints.First().x;

            for (int i = 0; i < vlePoints.Count - 1; i++)
            {
                if ((vlePoints[i].y <= targetY && vlePoints[i + 1].y >= targetY) ||
                    (vlePoints[i].y >= targetY && vlePoints[i + 1].y <= targetY))
                {
                    double diff = vlePoints[i + 1].y - vlePoints[i].y;
                    if (Math.Abs(diff) < 1e-10) return vlePoints[i].x;
                    double t = (targetY - vlePoints[i].y) / diff;
                    return vlePoints[i].x + t * (vlePoints[i + 1].x - vlePoints[i].x);
                }
            }
            return targetY;
        }

        private McCabeThieleData CreateEmptyData()
        {
            return CreatePartialData(
                new List<(double x, double y)> { (0, 0), (1, 1) },
                new List<(double x, double y)>(),
                string.Empty);
        }

        private McCabeThieleData CreatePartialData(
            List<(double x, double y)> diagonalLine,
            List<(double x, double y)> vleCurve,
            string chartSubtitle)
        {
            return new McCabeThieleData
            {
                DiagonalLine = diagonalLine,
                VLECurve = vleCurve,
                RectifyingLine = new List<(double x, double y)>(),
                StrippingLine = new List<(double x, double y)>(),
                StaircaseSteps = new List<StaircaseStep>(),
                Markers = new List<MarkerPoint>(),
                MinRefluxRectifyingLine = new List<(double x, double y)>(),
                MinRefluxStrippingLine = new List<(double x, double y)>(),
                ProjectionLinesB = new(),
                ProjectionLinesD = new(),
                ProjectionLinesF = new(),
                ProjectionLinesFToX = new(),              // ✅ NUEVO
                ProjectionLinesIntersectToY = new(),      // ✅ NUEVO
                MinRefluxRatio = 0,
                ChartTitle = "McCabe-Thiele Diagram",
                ChartSubtitle = chartSubtitle
            };
        }
    }

    //public sealed class McCabeThieleBuilder : IColumnPostSolverCalculation
    //{
    //    public int Order => 4;
    //    private readonly SolverColumn _column;

    //    public McCabeThieleBuilder(SolverColumn column)
    //    {
    //        _column = column;
    //    }

    //    public async Task CalculateAsync(CancellationToken cancellationToken = default)
    //    {
    //        if (_column.Orchestrator == null) return;

    //        // Caché si no hay cambios relevantes
    //        if (!_column.Orchestrator.FUGChanged &&
    //            !_column.Orchestrator.VLEChanged &&
    //            !_column.Orchestrator.PlatesChanged)
    //        {
    //            return;
    //        }

    //        var columnResult = _column.Orchestrator.CurrentResult;
    //        if (columnResult == null)
    //        {
    //            _column.Orchestrator.SetMcCabeThieleData(CreateEmptyData());
    //            return;
    //        }

    //        await Task.Run(() =>
    //        {
    //            try
    //            {
    //                var diagonalLine = new List<(double x, double y)> { (0, 0), (1, 1) };

    //                var vlePoints = columnResult.VLECurve.Points
    //                    .OrderBy(p => p.x)
    //                    .Select(p => (p.x, p.y))
    //                    .ToList();

    //                if (!vlePoints.Any())
    //                {
    //                    _column.Orchestrator?.SetMcCabeThieleData(new McCabeThieleData
    //                    {
    //                        DiagonalLine = diagonalLine,
    //                        ChartTitle = "McCabe-Thiele Diagram",
    //                        VLECurve = new List<(double x, double y)>(),
    //                        RectifyingLine = new List<(double x, double y)>(),
    //                        StrippingLine = new List<(double x, double y)>(),
    //                        StaircaseSteps = new List<StaircaseStep>(),
    //                        Markers = new List<MarkerPoint>(),
    //                        MinRefluxRectifyingLine = new List<(double x, double y)>(),
    //                        MinRefluxStrippingLine = new List<(double x, double y)>(),
    //                        ProjectionLinesB = new(),
    //                        ProjectionLinesF = new(),
    //                        ProjectionLinesD = new(),
    //                        MinRefluxRatio = 0,
    //                        ChartSubtitle = string.Empty
    //                    });
    //                    return;
    //                }

    //                // 1. Obtener parámetros FUG
    //                var fug = columnResult.DistillationParameters;
    //                double xD = fug?.xD ?? 0;
    //                double xB = fug?.xB ?? 0;
    //                double R = fug?.RefluxRatio.Value ?? 0;
    //                double R_min = fug?.MinRefluxRatio.Value ?? 0;
    //                double q = fug?.FeedQuality ?? 1.0;

    //                // 2. Obtener zF GLOBAL (composición del componente MÁS LIGERO del feed)
    //                var feedComposition = _column.Feeds.FirstOrDefault()?.Composition;
    //                double zF = 0;

    //                if (feedComposition != null && feedComposition.Components.Any())
    //                {
    //                    // Ordenar por punto de ebullición (menor = más ligero/volátil)
    //                    var sortedComponents = feedComposition.Components.OrderBy(c =>
    //                    {
    //                        return c.DataBase.FullData.BoilingPoint?.GetValue(TemperatureUnits.Kelvin) ?? 0;
    //                    }).ToList();

    //                    // El componente más ligero es el primero (menor punto de ebullición)
    //                    var lightestComponent = sortedComponents.FirstOrDefault();
    //                    if (lightestComponent != null)
    //                    {
    //                        zF = lightestComponent.MolarFraction.GetSolverValue();
    //                    }
    //                }


    //                // 2. Obtener zF GLOBAL (composición real del feed)


    //                // 3. MAPA TERMODINÁMICO: Derivar x_feed e y_feed explícitos
    //                // Esto es SOLO para el marcador F, NO para construir la línea q
    //                double x_feed = zF;
    //                double y_feed = zF;
    //                var feedState = _column.Feeds.FirstOrDefault()?.ThermodynamicState;

    //                if (feedState == ThermodynamicState.SaturatedVapor ||
    //                    feedState == ThermodynamicState.SuperheatedVapor)
    //                {
    //                    // Vapor: yF = zF, xF se obtiene de la curva VLE
    //                    y_feed = zF;
    //                    x_feed = FindXForY(vlePoints, zF);
    //                }
    //                else if (feedState == ThermodynamicState.SubcooledLiquid ||
    //                         feedState == ThermodynamicState.SaturatedLiquid)
    //                {
    //                    // Líquido: xF = zF, yF se obtiene de la curva VLE
    //                    x_feed = zF;
    //                    y_feed = InterpolateVLE(vlePoints, zF);
    //                }

    //                // Validación básica
    //                if (xD <= 0 || xB <= 0 || R <= 0 || zF <= 0)
    //                {
    //                    return;
    //                }

    //                // 4. Cálculos geométricos usando zF directamente (NO x_feed/y_feed)
    //                double x_int_q, y_int_q;
    //                FindQLineVLEIntersection(vlePoints, zF, q, out x_int_q, out y_int_q);

    //                double x_int, y_int;
    //                CalculateOperatingLinesIntersection(xD, xB, R, zF, q, out x_int, out y_int);

    //                // Líneas operativas
    //                var rectifyingLine = new List<(double x, double y)> { (x_int, y_int), (xD, xD) };
    //                var strippingLine = new List<(double x, double y)> { (xB, xB), (x_int, y_int) };

    //                var minRefluxRectifyingLine = R_min > 0
    //                    ? new List<(double x, double y)> { (x_int_q, y_int_q), (xD, xD) }
    //                    : new List<(double x, double y)>();

    //                var minRefluxStrippingLine = R_min > 0
    //                    ? new List<(double x, double y)> { (xB, xB), (x_int_q, y_int_q) }
    //                    : new List<(double x, double y)>();

    //                // Proyecciones y marcadores con la nueva lógica unificada
    //                var projectionLinesD = new List<(double x, double y)> { (xD, 0), (xD, xD) };
    //                var projectionLinesB = new List<(double x, double y)> { (xB, 0), (xB, xB) };

    //                // Aquí aplicamos la lógica conceptual validada: Eje -> Diagonal(zF,zF) -> ROL
    //                var projectionLinesF = BuildUnifiedFeedLine(zF, q, x_int, y_int);

    //                var markers = BuildMarkers(xD, xB, zF, q, R);
    //                var staircaseSteps = BuildStaircaseSteps(vlePoints, xD, xB, R, x_feed, x_int, y_int);

    //                var mcCabeData = new McCabeThieleData
    //                {
    //                    DiagonalLine = diagonalLine,
    //                    VLECurve = vlePoints,
    //                    RectifyingLine = rectifyingLine,
    //                    StrippingLine = strippingLine,
    //                    StaircaseSteps = staircaseSteps,
    //                    Markers = markers,
    //                    MinRefluxRectifyingLine = minRefluxRectifyingLine,
    //                    MinRefluxStrippingLine = minRefluxStrippingLine,
    //                    ProjectionLinesD = projectionLinesD,
    //                    ProjectionLinesB = projectionLinesB,
    //                    ProjectionLinesF = projectionLinesF,
    //                    MinRefluxRatio = R_min,
    //                    ChartTitle = "McCabe-Thiele Diagram",
    //                    ChartSubtitle = $"Reflux Ratio R = {R:F2} | Min Reflux R_min = {R_min:F2}"
    //                };

    //                _column.Orchestrator?.SetMcCabeThieleData(mcCabeData);
    //            }
    //            catch (Exception ex)
    //            {
    //                _column.Orchestrator?.SetMcCabeThieleData(CreateEmptyData());
    //            }
    //        }, cancellationToken);
    //    }

    //    /// <summary>
    //    /// Construye la línea de alimentación unificada siguiendo la regla:
    //    /// Eje (según q) -> Diagonal (zF, zF) -> Intersección con ROL (x_int, y_int)
    //    /// </summary>
    //    private List<(double x, double y)> BuildUnifiedFeedLine(double zF, double q, double x_int, double y_int)
    //    {
    //        var lines = new List<(double x, double y)>();

    //        var anchorPoint = (zF, zF);
    //        var intersectPoint = (x_int, y_int);

    //        if (q >= 1.0)
    //        {
    //            // Líquidos: Eje X → Diagonal → Intersección
    //            lines.Add((zF, 0));
    //            lines.Add(anchorPoint);
    //        }
    //        else if (q <= 0.0)
    //        {
    //            // Vapores: Eje Y → Diagonal → Intersección
    //            // Para q < 0, necesitamos que la línea tenga pendiente positiva
    //            lines.Add((0, zF));
    //            lines.Add(anchorPoint);

    //            // ✅ Agregar punto intermedio para vapor sobrecalentado
    //            // Esto fuerza a que la línea tenga la pendiente correcta
    //            if (q < 0)
    //            {
    //                // Calculamos un punto intermedio usando la ecuación de la línea q
    //                double slope = q / (q - 1.0);
    //                double x_mid = (zF + x_int) / 2.0;  // Punto medio en X
    //                double y_mid = slope * (x_mid - zF) + zF;  // Usando punto-pendiente
    //                lines.Add((x_mid, y_mid));
    //            }
    //        }
    //        else
    //        {
    //            // Mezcla bifásica: Solo Diagonal → Intersección
    //            lines.Add(anchorPoint);
    //        }

    //        lines.Add(intersectPoint);

    //        return lines;
    //    }
    //    private void FindQLineVLEIntersection(
    //        List<(double x, double y)> vlePoints,
    //        double zF, double q,
    //        out double x_int, out double y_int)
    //    {
    //        if (Math.Abs(q - 1.0) < 0.01)
    //        {
    //            x_int = zF;
    //            y_int = InterpolateVLE(vlePoints, zF);
    //        }
    //        else if (Math.Abs(q) < 0.01)
    //        {
    //            y_int = zF;
    //            x_int = FindXForY(vlePoints, zF);
    //        }
    //        else
    //        {
    //            // Línea q: y = (q/(q-1))*x - zF/(q-1)
    //            double slope = q / (q - 1.0);
    //            double intercept = -zF / (q - 1.0);

    //            double left = 0, right = 1;
    //            for (int iter = 0; iter < 50; iter++)
    //            {
    //                double mid = (left + right) / 2.0;
    //                double y_q = slope * mid + intercept;
    //                double y_vle = InterpolateVLE(vlePoints, mid);

    //                if (Math.Abs(y_q - y_vle) < 1e-6)
    //                {
    //                    x_int = mid;
    //                    y_int = y_vle;
    //                    return;
    //                }

    //                double y_q_left = slope * left + intercept;
    //                double y_vle_left = InterpolateVLE(vlePoints, left);

    //                if ((y_q_left - y_vle_left) * (y_q - y_vle) < 0)
    //                    right = mid;
    //                else
    //                    left = mid;
    //            }

    //            x_int = (left + right) / 2.0;
    //            y_int = InterpolateVLE(vlePoints, x_int);
    //        }
    //    }

    //    private void CalculateOperatingLinesIntersection(
    //        double xD, double xB, double R,
    //        double zF, double q,
    //        out double x_int, out double y_int)
    //    {
    //        if (Math.Abs(q - 1.0) < 1e-6)
    //        {
    //            x_int = zF;
    //            y_int = (R / (R + 1.0)) * zF + xD / (R + 1.0);
    //        }
    //        else
    //        {
    //            double R_frac = R / (R + 1.0);
    //            double D_frac = xD / (R + 1.0);
    //            double Q_frac1 = q / (q - 1.0);

    //            // Intercepto correcto de la línea q: -zF/(q-1)
    //            double Q_intercept = -zF / (q - 1.0);

    //            x_int = (Q_intercept - D_frac) / (R_frac - Q_frac1);
    //            y_int = R_frac * x_int + D_frac;
    //        }
    //    }

    //    private List<MarkerPoint> BuildMarkers(double xD, double xB, double zF, double q, double R)
    //    {
    //        var markers = new List<MarkerPoint>();

    //        markers.Add(new MarkerPoint { Label = "D", X = xD, Y = 0, Description = "Distillate", TooltipData = new Dictionary<string, string> { { "x", xD.ToString("F3") }, { "Type", "Distillate" } } });
    //        markers.Add(new MarkerPoint { Label = "B", X = xB, Y = 0, Description = "Bottoms", TooltipData = new Dictionary<string, string> { { "x", xB.ToString("F3") }, { "Type", "Bottoms" } } });

    //        // ✅ F SIEMPRE en la diagonal (zF, zF) - Sin condiciones
    //        markers.Add(new MarkerPoint
    //        {
    //            Label = "F",
    //            X = zF,           // Siempre zF
    //            Y = zF,           // Siempre zF  
    //            Description = $"Feed (q={q:F2})",
    //            TooltipData = new Dictionary<string, string>
    //    {
    //        { "x", zF.ToString("F3") },
    //        { "y", zF.ToString("F3") },
    //        { "Type", "Feed" },
    //        { "Feed Quality (q)", q.ToString("F2") }
    //    }
    //        });

    //        return markers;
    //    }

    //    private List<StaircaseStep> BuildStaircaseSteps(List<(double x, double y)> vlePoints, double xD, double xB, double R, double x_feed, double x_int, double y_int)
    //    {
    //        var steps = new List<StaircaseStep>();
    //        if (!vlePoints.Any()) return steps;

    //        double x_op = xD;
    //        double y_op = xD;
    //        int stageNum = 1;
    //        bool inRectifying = true;

    //        while (x_op > xB && stageNum <= 150)
    //        {
    //            double x_vle = FindXForY(vlePoints, y_op);
    //            if (x_vle >= x_op) x_vle = x_op - 0.001;

    //            var point1 = (x_op, y_op);
    //            var point2 = (x_vle, y_op);

    //            if (inRectifying && x_vle <= x_int) inRectifying = false;

    //            double x_op_next = x_vle;
    //            double y_op_next = inRectifying
    //                ? (R / (R + 1.0)) * x_op_next + xD / (R + 1.0)
    //                : ((y_int - xB) / (x_int - xB)) * (x_op_next - xB) + xB;

    //            var point3 = (x_op_next, y_op_next);
    //            steps.Add(new StaircaseStep { StageNumber = stageNum, Points = new List<(double x, double y)> { point1, point2, point3 }, StageType = inRectifying ? "Rectifying" : "Stripping" });

    //            if (x_op_next <= xB || (Math.Abs(y_op - y_op_next) < 1e-6 && Math.Abs(x_op - x_vle) < 1e-6)) break;

    //            x_op = x_op_next;
    //            y_op = y_op_next;
    //            stageNum++;
    //        }
    //        return steps;
    //    }

    //    private double InterpolateVLE(List<(double x, double y)> vlePoints, double x)
    //    {
    //        if (x <= vlePoints.First().x) return vlePoints.First().y;
    //        if (x >= vlePoints.Last().x) return vlePoints.Last().y;

    //        int left = 0, right = vlePoints.Count - 1;
    //        while (left < right - 1)
    //        {
    //            int mid = (left + right) / 2;
    //            if (vlePoints[mid].x <= x) left = mid; else right = mid;
    //        }

    //        double diff = vlePoints[right].x - vlePoints[left].x;
    //        if (Math.Abs(diff) < 1e-10) return vlePoints[left].y;

    //        return vlePoints[left].y + (vlePoints[right].y - vlePoints[left].y) * (x - vlePoints[left].x) / diff;
    //    }

    //    private double FindXForY(List<(double x, double y)> vlePoints, double targetY)
    //    {
    //        if (targetY >= vlePoints.Last().y) return vlePoints.Last().x;
    //        if (targetY <= vlePoints.First().y) return vlePoints.First().x;

    //        for (int i = 0; i < vlePoints.Count - 1; i++)
    //        {
    //            if ((vlePoints[i].y <= targetY && vlePoints[i + 1].y >= targetY) ||
    //                (vlePoints[i].y >= targetY && vlePoints[i + 1].y <= targetY))
    //            {
    //                double diff = vlePoints[i + 1].y - vlePoints[i].y;
    //                if (Math.Abs(diff) < 1e-10) return vlePoints[i].x;
    //                double t = (targetY - vlePoints[i].y) / diff;
    //                return vlePoints[i].x + t * (vlePoints[i + 1].x - vlePoints[i].x);
    //            }
    //        }
    //        return targetY;
    //    }

    //    private McCabeThieleData CreateEmptyData()
    //    {
    //        return new McCabeThieleData
    //        {
    //            DiagonalLine = new List<(double x, double y)>(),
    //            VLECurve = new List<(double x, double y)>(),
    //            RectifyingLine = new List<(double x, double y)>(),
    //            StrippingLine = new List<(double x, double y)>(),
    //            StaircaseSteps = new List<StaircaseStep>(),
    //            Markers = new List<MarkerPoint>(),
    //            MinRefluxRectifyingLine = new List<(double x, double y)>(),
    //            MinRefluxStrippingLine = new List<(double x, double y)>(),
    //            ProjectionLinesB = new(),
    //            ProjectionLinesD = new(),
    //            ProjectionLinesF = new(),
    //            MinRefluxRatio = 0,
    //            ChartTitle = "McCabe-Thiele Diagram",
    //            ChartSubtitle = string.Empty
    //        };
    //    }
    //}
    //public sealed class McCabeThieleBuilder : IColumnPostSolverCalculation
    //{
    //    public int Order => 4;
    //    private readonly SolverColumn _column;

    //    public McCabeThieleBuilder(SolverColumn column)
    //    {
    //        _column = column;
    //    }

    //    public async Task CalculateAsync(CancellationToken cancellationToken = default)
    //    {
    //        if (_column.Orchestrator == null) return;

    //        // Caché si no hay cambios relevantes
    //        if (!_column.Orchestrator.FUGChanged &&
    //            !_column.Orchestrator.VLEChanged &&
    //            !_column.Orchestrator.PlatesChanged)
    //        {
    //            return;
    //        }

    //        var columnResult = _column.Orchestrator.CurrentResult;
    //        if (columnResult == null)
    //        {
    //            _column.Orchestrator.SetMcCabeThieleData(CreateEmptyData());
    //            return;
    //        }

    //        await Task.Run(() =>
    //        {
    //            try
    //            {
    //                var diagonalLine = new List<(double x, double y)> { (0, 0), (1, 1) };

    //                var vlePoints = columnResult.VLECurve.Points
    //                    .OrderBy(p => p.x)
    //                    .Select(p => (p.x, p.y))
    //                    .ToList();

    //                if (!vlePoints.Any())
    //                {
    //                    _column.Orchestrator?.SetMcCabeThieleData(new McCabeThieleData
    //                    {
    //                        DiagonalLine = diagonalLine,
    //                        ChartTitle = "McCabe-Thiele Diagram",
    //                        VLECurve = new List<(double x, double y)>(),
    //                        RectifyingLine = new List<(double x, double y)>(),
    //                        StrippingLine = new List<(double x, double y)>(),
    //                        //FeedLine = new List<(double x, double y)>(),
    //                        StaircaseSteps = new List<StaircaseStep>(),
    //                        Markers = new List<MarkerPoint>(),
    //                        MinRefluxRectifyingLine = new List<(double x, double y)>(),
    //                        MinRefluxStrippingLine = new List<(double x, double y)>(),
    //                        ProjectionLinesB = new(),
    //                        ProjectionLinesF = new(),
    //                        ProjectionLinesD = new(),

    //                        MinRefluxRatio = 0,

    //                        ChartSubtitle = string.Empty
    //                    });
    //                    return;
    //                }

    //                // 1. Obtener parámetros FUG
    //                var fug = columnResult.DistillationParameters;
    //                double xD = fug?.xD ?? 0;
    //                double xB = fug?.xB ?? 0;
    //                double R = fug?.RefluxRatio.Value ?? 0;
    //                double R_min = fug?.MinRefluxRatio.Value ?? 0;
    //                double q = fug?.FeedQuality ?? 1.0;

    //                // 2. Obtener zF GLOBAL (composición real del feed)
    //                double zF = _column.Feeds.FirstOrDefault()?
    //                    .Composition.Components.FirstOrDefault()?
    //                    .MolarFraction.GetSolverValue() ?? 0;

    //                // 3. MAPA TERMODINÁMICO: Derivar x_feed e y_feed explícitos
    //                double x_feed = zF;
    //                double y_feed = zF;
    //                var feedState = _column.Feeds.FirstOrDefault()?.ThermodynamicState;

    //                if (feedState == ThermodynamicState.SaturatedVapor ||
    //                    feedState == ThermodynamicState.SuperheatedVapor)
    //                {
    //                    // Vapor: yF = zF, xF se obtiene de la curva VLE
    //                    y_feed = zF;
    //                    x_feed = FindXForY(vlePoints, zF);
    //                }
    //                else if (feedState == ThermodynamicState.SubcooledLiquid ||
    //                         feedState == ThermodynamicState.SaturatedLiquid)
    //                {
    //                    // Líquido: xF = zF, yF se obtiene de la curva VLE
    //                    x_feed = zF;
    //                    y_feed = InterpolateVLE(vlePoints, zF);
    //                }
    //                // Para mezcla bifásica (0 < q < 1), x_feed = y_feed = zF es una aproximación válida
    //                // ya que la línea q maneja la transición entre fases.

    //                // Validación básica
    //                if (xD <= 0 || xB <= 0 || R <= 0 || zF <= 0)
    //                {
    //                    return;
    //                }

    //                // 4. Cálculos geométricos usando x_feed / y_feed
    //                double x_int_q, y_int_q;
    //                FindQLineVLEIntersection(vlePoints, x_feed, y_feed, q, out x_int_q, out y_int_q);

    //                double x_int, y_int;
    //                CalculateOperatingLinesIntersection(xD, xB, R, x_feed, y_feed, q, out x_int, out y_int);

    //                // Líneas operativas
    //                var rectifyingLine = new List<(double x, double y)> { (x_int, y_int), (xD, xD) };
    //                var strippingLine = new List<(double x, double y)> { (xB, xB), (x_int, y_int) };

    //                var minRefluxRectifyingLine = R_min > 0
    //                    ? new List<(double x, double y)> { (x_int_q, y_int_q), (xD, xD) }
    //                    : new List<(double x, double y)>();

    //                var minRefluxStrippingLine = R_min > 0
    //                    ? new List<(double x, double y)> { (xB, xB), (x_int_q, y_int_q) }
    //                    : new List<(double x, double y)>();

    //                // Proyecciones y marcadores
    //                var projectionLinesD = new List<(double x, double y)> { (xD, 0), (xD, xD) };
    //                var projectionLinesB = new List<(double x, double y)> { (xB, 0), (xB, xB) };
    //                var projectionLinesF = BuildProjectionLinesF(x_feed, y_feed, q, x_int_q, y_int_q);
    //                var markers = BuildMarkers(xD, xB, x_feed, y_feed, q, R);
    //                var staircaseSteps = BuildStaircaseSteps(vlePoints, xD, xB, R, x_feed, x_int, y_int);

    //                var mcCabeData = new McCabeThieleData
    //                {
    //                    DiagonalLine = diagonalLine,
    //                    VLECurve = vlePoints,
    //                    RectifyingLine = rectifyingLine,
    //                    StrippingLine = strippingLine,
    //                    StaircaseSteps = staircaseSteps,
    //                    Markers = markers,
    //                    MinRefluxRectifyingLine = minRefluxRectifyingLine,
    //                    MinRefluxStrippingLine = minRefluxStrippingLine,
    //                    ProjectionLinesD = projectionLinesD,
    //                    ProjectionLinesB = projectionLinesB,
    //                    ProjectionLinesF = projectionLinesF,
    //                    MinRefluxRatio = R_min,
    //                    ChartTitle = "McCabe-Thiele Diagram",
    //                    ChartSubtitle = $"Reflux Ratio R = {R:F2} | Min Reflux R_min = {R_min:F2}" ,
    //                    //FeedLine=new(),

    //                };

    //                _column.Orchestrator?.SetMcCabeThieleData(mcCabeData);
    //            }
    //            catch (Exception ex)
    //            {
    //                _column.Orchestrator?.SetMcCabeThieleData(CreateEmptyData());
    //            }
    //        }, cancellationToken);
    //    }

    //    // 🔥 Nuevo método con firma actualizada para usar x_feed e y_feed
    //    private void FindQLineVLEIntersection(
    //        List<(double x, double y)> vlePoints,
    //        double x_feed, double y_feed, double q,
    //        out double x_int, out double y_int)
    //    {
    //        if (Math.Abs(q - 1.0) < 0.01)
    //        {
    //            x_int = x_feed;
    //            y_int = InterpolateVLE(vlePoints, x_feed);
    //        }
    //        else if (Math.Abs(q) < 0.01)
    //        {
    //            y_int = y_feed;
    //            x_int = FindXForY(vlePoints, y_feed);
    //        }
    //        else
    //        {
    //            // Pendiente q/(q-1) pasa por (x_feed, y_feed)
    //            double slope = q / (q - 1.0);
    //            double intercept = y_feed - slope * x_feed;

    //            double left = 0, right = 1;
    //            for (int iter = 0; iter < 50; iter++)
    //            {
    //                double mid = (left + right) / 2.0;
    //                double y_q = slope * mid + intercept;
    //                double y_vle = InterpolateVLE(vlePoints, mid);

    //                if (Math.Abs(y_q - y_vle) < 1e-6)
    //                {
    //                    x_int = mid;
    //                    y_int = y_vle;
    //                    return;
    //                }

    //                double y_q_left = slope * left + intercept;
    //                double y_vle_left = InterpolateVLE(vlePoints, left);

    //                if ((y_q_left - y_vle_left) * (y_q - y_vle) < 0)
    //                    right = mid;
    //                else
    //                    left = mid;
    //            }

    //            x_int = (left + right) / 2.0;
    //            y_int = InterpolateVLE(vlePoints, x_int);
    //        }
    //    }

    //    private void CalculateOperatingLinesIntersection(
    //        double xD, double xB, double R,
    //        double x_feed, double y_feed, double q,
    //        out double x_int, out double y_int)
    //    {
    //        if (Math.Abs(q - 1.0) < 1e-6)
    //        {
    //            x_int = x_feed;
    //            y_int = (R / (R + 1.0)) * x_feed + xD / (R + 1.0);
    //        }
    //        else
    //        {
    //            double R_frac = R / (R + 1.0);
    //            double D_frac = xD / (R + 1.0);
    //            double Q_frac1 = q / (q - 1.0);

    //            // Línea q: y = Q_frac1 * x + (y_feed - Q_frac1 * x_feed)
    //            double Q_intercept = y_feed - Q_frac1 * x_feed;

    //            x_int = (Q_intercept - D_frac) / (R_frac - Q_frac1);
    //            y_int = R_frac * x_int + D_frac;
    //        }
    //    }

    //    private List<MarkerPoint> BuildMarkers(double xD, double xB, double x_feed, double y_feed, double q, double R)
    //    {
    //        var markers = new List<MarkerPoint>();

    //        markers.Add(new MarkerPoint { Label = "D", X = xD, Y = 0, Description = "Distillate", TooltipData = new Dictionary<string, string> { { "x", xD.ToString("F3") }, { "Type", "Distillate" } } });
    //        markers.Add(new MarkerPoint { Label = "B", X = xB, Y = 0, Description = "Bottoms", TooltipData = new Dictionary<string, string> { { "x", xB.ToString("F3") }, { "Type", "Bottoms" } } });

    //        double markerX_F, markerY_F;
    //        if (q > 1) { markerX_F = x_feed; markerY_F = 0; }
    //        else if (Math.Abs(q - 1.0) < 0.01 || Math.Abs(q) < 0.01) { markerX_F = x_feed; markerY_F = y_feed; }
    //        else if (q < 0) { markerX_F = 0; markerY_F = y_feed; }
    //        else { markerX_F = x_feed; markerY_F = y_feed; }

    //        markers.Add(new MarkerPoint
    //        {
    //            Label = "F",
    //            X = markerX_F,
    //            Y = markerY_F,
    //            Description = $"Feed (q={q:F2})",
    //            TooltipData = new Dictionary<string, string>
    //        {
    //            { "x", markerX_F.ToString("F3") },
    //            { "y", markerY_F.ToString("F3") },
    //            { "Type", "Feed" },
    //            { "Feed Quality (q)", q.ToString("F2") }
    //        }
    //        });

    //        return markers;
    //    }
    //    private List<(double x, double y)> BuildProjectionLinesF(double x_feed, double y_feed, double q, double x_int_q, double y_int_q)
    //    {
    //        var lines = new List<(double x, double y)>();

    //        // 1. Vertical desde eje X hasta punto F en diagonal
    //        lines.Add((x_feed, 0));
    //        lines.Add((x_feed, y_feed));

    //        // 2. Línea q desde punto F hasta intersección con VLE
    //        lines.Add((x_feed, y_feed));
    //        lines.Add((x_int_q, y_int_q));

    //        // 3. Horizontal desde curva VLE hasta eje Y
    //        lines.Add((x_int_q, y_int_q));
    //        lines.Add((0, y_int_q));

    //        return lines;
    //    }
    //    private List<(double x, double y)> BuildProjectionLinesF2(double x_feed, double y_feed, double q, double x_int_q, double y_int_q)
    //    {
    //        var lines = new List<(double x, double y)>();

    //        // Serie 1: Vertical desde eje X hasta punto de feed
    //        lines.Add((x_feed, 0));
    //        lines.Add((x_feed, y_feed)); // Nota: para q=1, y_feed está en diagonal; para q=0, y_feed=zF

    //        // Serie 2: Línea q hasta curva VLE
    //        lines.Add((x_feed, y_feed));
    //        lines.Add((x_int_q, y_int_q));

    //        // Serie 3: Horizontal desde curva VLE hasta eje Y
    //        lines.Add((x_int_q, y_int_q));
    //        lines.Add((0, y_int_q));

    //        return lines;
    //    }

    //    // ... Resto de métodos auxiliares (BuildStaircaseSteps, InterpolateVLE, FindXForY, CreateEmptyData) 
    //    // permanecen idénticos a tu versión anterior ...
    //    private McCabeThieleData CreateEmptyData()
    //    {
    //        return new McCabeThieleData
    //        {
    //            DiagonalLine = new List<(double x, double y)>(),
    //            VLECurve = new List<(double x, double y)>(),
    //            RectifyingLine = new List<(double x, double y)>(),
    //            StrippingLine = new List<(double x, double y)>(),
    //            //FeedLine = new List<(double x, double y)>(),
    //            StaircaseSteps = new List<StaircaseStep>(),
    //            Markers = new List<MarkerPoint>(),
    //            MinRefluxRectifyingLine = new List<(double x, double y)>(),
    //            MinRefluxStrippingLine = new List<(double x, double y)>(),
    //            ProjectionLinesB = new(),
    //            ProjectionLinesD = new(),
    //            ProjectionLinesF = new(),
    //            MinRefluxRatio = 0,
    //            ChartTitle = "McCabe-Thiele Diagram",
    //            ChartSubtitle = string.Empty
    //        };
    //    }
    //    //private double InterpolateVLE(List<(double x, double y)> vlePoints, double x)
    //    //{
    //    //    if (x <= vlePoints.First().x) return vlePoints.First().y;
    //    //    if (x >= vlePoints.Last().x) return vlePoints.Last().y;

    //    //    int left = 0, right = vlePoints.Count - 1;
    //    //    while (left < right - 1)
    //    //    {
    //    //        int mid = (left + right) / 2;
    //    //        if (vlePoints[mid].x <= x)
    //    //            left = mid;
    //    //        else
    //    //            right = mid;
    //    //    }

    //    //    double x1 = vlePoints[left].x, y1 = vlePoints[left].y;
    //    //    double x2 = vlePoints[right].x, y2 = vlePoints[right].y;
    //    //    double diff = x2 - x1;

    //    //    if (Math.Abs(diff) < 1e-10) return y1;

    //    //    return y1 + (y2 - y1) * (x - x1) / diff;
    //    //}
    //    private List<StaircaseStep> BuildStaircaseSteps(List<(double x, double y)> vlePoints, double xD, double xB, double R, double x_feed, double x_int, double y_int)
    //    {
    //        var steps = new List<StaircaseStep>();
    //        if (!vlePoints.Any()) return steps;

    //        double x_op = xD;
    //        double y_op = xD;
    //        int stageNum = 1;
    //        bool inRectifying = true;

    //        while (x_op > xB && stageNum <= 150)
    //        {
    //            double x_vle = FindXForY(vlePoints, y_op);
    //            if (x_vle >= x_op) x_vle = x_op - 0.001;

    //            var point1 = (x_op, y_op);
    //            var point2 = (x_vle, y_op);

    //            if (inRectifying && x_vle <= x_int) inRectifying = false;

    //            double x_op_next = x_vle;
    //            double y_op_next = inRectifying
    //                ? (R / (R + 1.0)) * x_op_next + xD / (R + 1.0)
    //                : ((y_int - xB) / (x_int - xB)) * (x_op_next - xB) + xB;

    //            var point3 = (x_op_next, y_op_next);
    //            steps.Add(new StaircaseStep { StageNumber = stageNum, Points = new List<(double x, double y)> { point1, point2, point3 }, StageType = inRectifying ? "Rectifying" : "Stripping" });

    //            if (x_op_next <= xB || (Math.Abs(y_op - y_op_next) < 1e-6 && Math.Abs(x_op - x_vle) < 1e-6)) break;

    //            x_op = x_op_next;
    //            y_op = y_op_next;
    //            stageNum++;
    //        }
    //        return steps;
    //    }

    //    private double InterpolateVLE(List<(double x, double y)> vlePoints, double x)
    //    {
    //        if (x <= vlePoints.First().x) return vlePoints.First().y;
    //        if (x >= vlePoints.Last().x) return vlePoints.Last().y;

    //        int left = 0, right = vlePoints.Count - 1;
    //        while (left < right - 1)
    //        {
    //            int mid = (left + right) / 2;
    //            if (vlePoints[mid].x <= x) left = mid; else right = mid;
    //        }

    //        double diff = vlePoints[right].x - vlePoints[left].x;
    //        if (Math.Abs(diff) < 1e-10) return vlePoints[left].y;

    //        return vlePoints[left].y + (vlePoints[right].y - vlePoints[left].y) * (x - vlePoints[left].x) / diff;
    //    }

    //    private double FindXForY(List<(double x, double y)> vlePoints, double targetY)
    //    {
    //        if (targetY >= vlePoints.Last().y) return vlePoints.Last().x;
    //        if (targetY <= vlePoints.First().y) return vlePoints.First().x;

    //        for (int i = 0; i < vlePoints.Count - 1; i++)
    //        {
    //            if ((vlePoints[i].y <= targetY && vlePoints[i + 1].y >= targetY) ||
    //                (vlePoints[i].y >= targetY && vlePoints[i + 1].y <= targetY))
    //            {
    //                double diff = vlePoints[i + 1].y - vlePoints[i].y;
    //                if (Math.Abs(diff) < 1e-10) return vlePoints[i].x;
    //                double t = (targetY - vlePoints[i].y) / diff;
    //                return vlePoints[i].x + t * (vlePoints[i + 1].x - vlePoints[i].x);
    //            }
    //        }
    //        return targetY;
    //    }
    //    //private List<StaircaseStep> BuildStaircaseSteps(
    //    //    List<(double x, double y)> vlePoints,
    //    //    double xD, double xB, double R, double zF,
    //    //    double x_int, double y_int)
    //    //{
    //    //    var steps = new List<StaircaseStep>();

    //    //    if (!vlePoints.Any()) return steps;

    //    //    double x_op = xD;
    //    //    double y_op = xD;
    //    //    int stageNum = 1;
    //    //    bool inRectifying = true;

    //    //    while (x_op > xB && stageNum <= 150)
    //    //    {
    //    //        double x_vle = FindXForY(vlePoints, y_op);

    //    //        if (x_vle >= x_op)
    //    //        {
    //    //            x_vle = x_op - 0.001;
    //    //        }

    //    //        var point1 = (x_op, y_op);
    //    //        var point2 = (x_vle, y_op);

    //    //        if (inRectifying && x_vle <= x_int)
    //    //        {
    //    //            inRectifying = false;
    //    //        }

    //    //        double x_op_next = x_vle;
    //    //        double y_op_next;

    //    //        if (inRectifying)
    //    //        {
    //    //            y_op_next = (R / (R + 1.0)) * x_op_next + xD / (R + 1.0);
    //    //        }
    //    //        else
    //    //        {
    //    //            double slope = (y_int - xB) / (x_int - xB);
    //    //            y_op_next = slope * (x_op_next - xB) + xB;
    //    //        }

    //    //        var point3 = (x_op_next, y_op_next);

    //    //        steps.Add(new StaircaseStep
    //    //        {
    //    //            StageNumber = stageNum,
    //    //            Points = new List<(double x, double y)> { point1, point2, point3 },
    //    //            StageType = inRectifying ? "Rectifying" : "Stripping"
    //    //        });

    //    //        if (x_op_next <= xB) break;

    //    //        if (Math.Abs(y_op - y_op_next) < 1e-6 && Math.Abs(x_op - x_vle) < 1e-6) break;

    //    //        x_op = x_op_next;
    //    //        y_op = y_op_next;
    //    //        stageNum++;
    //    //    }

    //    //    return steps;
    //    //}

    //}

    //public sealed class McCabeThieleBuilder2 : IColumnPostSolverCalculation
    //{
    //    public int Order => 4;

    //    private readonly SolverColumn _column;

    //    public McCabeThieleBuilder2(SolverColumn column)
    //    {
    //        _column = column;
    //    }

    //    public async Task CalculateAsync(CancellationToken cancellationToken = default)
    //    {
    //        if (_column.Orchestrator == null)
    //        {
    //            return;
    //        }

    //        // 🔥 Si FUG, VLE y Platos no cambiaron, usar caché
    //        if (!_column.Orchestrator.FUGChanged &&
    //            !_column.Orchestrator.VLEChanged &&
    //            !_column.Orchestrator.PlatesChanged)
    //        {
    //            return;
    //        }

    //        // 🔥 Obtener el resultado actual del orquestador (ya tiene FUG, VLE, Platos)
    //        var columnResult = _column.Orchestrator.CurrentResult;

    //        if (columnResult == null)
    //        {
    //            _column.Orchestrator.SetMcCabeThieleData(CreateEmptyData());
    //            return;
    //        }

    //        await Task.Run(() =>
    //        {
    //            try
    //            {
    //                // 🔥 1. Siempre graficar la diagonal (independiente de todo)
    //                var diagonalLine = new List<(double x, double y)> { (0, 0), (1, 1) };

    //                // 🔥 2. Graficar VLE si está disponible
    //                var vlePoints = columnResult.VLECurve.Points
    //                    .OrderBy(p => p.x)
    //                    .Select(p => (p.x, p.y))
    //                    .ToList();

    //                if (!vlePoints.Any())
    //                {
    //                    _column.Orchestrator?.SetMcCabeThieleData(new McCabeThieleData
    //                    {
    //                        DiagonalLine = diagonalLine,
    //                        VLECurve = new List<(double x, double y)>(),
    //                        RectifyingLine = new List<(double x, double y)>(),
    //                        StrippingLine = new List<(double x, double y)>(),
    //                        FeedLine = new List<(double x, double y)>(),
    //                        StaircaseSteps = new List<StaircaseStep>(),
    //                        Markers = new List<MarkerPoint>(),
    //                        MinRefluxRectifyingLine = new List<(double x, double y)>(),
    //                        MinRefluxStrippingLine = new List<(double x, double y)>(),
    //                        ProjectionLinesB = new(),
    //                        ProjectionLinesF = new(),
    //                        ProjectionLinesD = new(),

    //                        MinRefluxRatio = 0,
    //                        ChartTitle = "McCabe-Thiele Diagram",
    //                        ChartSubtitle = string.Empty
    //                    });
    //                    return;
    //                }

    //                // 🔥 3. Intentar obtener datos de FUG (opcionales)
    //                var fug = columnResult.DistillationParameters;
    //                double xD = fug?.xD ?? 0;
    //                double xB = fug?.xB ?? 0;
    //                double R = fug?.RefluxRatio.Value ?? 0;
    //                double R_min = fug?.MinRefluxRatio.Value ?? 0;
    //                double q = fug?.FeedQuality ?? 1.0;

    //                // 🔥 4. Intentar obtener zF del feed (opcional)
    //                double zF = 0;
    //                var feedStage = columnResult.Stages?.FirstOrDefault(s => s.IsFeedStage);
    //                zF = _column.Feeds.FirstOrDefault()?.Composition.Components.FirstOrDefault()?.MolarFraction.GetSolverValue() ?? 0;

    //                if (_column.Feeds.FirstOrDefault()?.ThermodynamicState != ThermodynamicState.SubcooledLiquid &&
    //                _column.Feeds.FirstOrDefault()?.ThermodynamicState != ThermodynamicState.SaturatedLiquid)
    //                {
    //                    zF = _column.Feeds.FirstOrDefault()?.LiquidPhase.Components.FirstOrDefault()?.MolarFraction ?? 0;
    //                }


    //                // 🔥 5. Si tenemos FUG válido, graficar líneas de operación
    //                var rectifyingLine = new List<(double x, double y)>();
    //                var strippingLine = new List<(double x, double y)>();
    //                var feedLine = new List<(double x, double y)>();
    //                var minRefluxRectifyingLine = new List<(double x, double y)>();
    //                var minRefluxStrippingLine = new List<(double x, double y)>();

    //                var markers = new List<MarkerPoint>();
    //                var staircaseSteps = new List<StaircaseStep>();
    //                string chartSubtitle = string.Empty;
    //                List<(double x, double y)> projectionLinesD = new();
    //                List<(double x, double y)> projectionLinesB = new();
    //                List<(double x, double y)> projectionLinesF = new();

    //                if (xD > 0 && xB > 0 && R > 0 && zF > 0)
    //                {
    //                    // Encontrar intersección de línea q con curva VLE
    //                    double x_int_q, y_int_q;
    //                    FindQLineVLEIntersection(vlePoints, zF, q, out x_int_q, out y_int_q);

    //                    // Calcular intersección de líneas de operación (R actual)
    //                    double x_int, y_int;
    //                    CalculateOperatingLinesIntersection(xD, xB, R, zF, q, out x_int, out y_int);

    //                    // Línea de Rectificación
    //                    rectifyingLine = new List<(double x, double y)> { (x_int, y_int), (xD, xD) };

    //                    // Línea de Agotamiento
    //                    strippingLine = new List<(double x, double y)> { (xB, xB), (x_int, y_int) };

    //                    // Líneas en R_min
    //                    if (R_min > 0)
    //                    {
    //                        minRefluxRectifyingLine = new List<(double x, double y)> { (x_int_q, y_int_q), (xD, xD) };
    //                        minRefluxStrippingLine = new List<(double x, double y)> { (xB, xB), (x_int_q, y_int_q) };
    //                    }

    //                    // Línea q
    //                    //feedLine = new List<(double x, double y)> { (zF, zF), (x_int_q, y_int_q) };

    //                    // 🔥 6. Líneas de proyección vertical (D y B hacia la diagonal)
    //                    projectionLinesD = new List<(double x, double y)>
    //                    {
    //                        (xD, 0),      // Desde eje X
    //                        (xD, xD)      // Hasta la diagonal
    //                    };

    //                    // B (Bottoms - líquido): solo línea vertical desde eje X hasta diagonal
    //                    projectionLinesB = new List<(double x, double y)>
    //                    {
    //                        (xB, 0),      // Desde eje X
    //                        (xB, xB)      // Hasta la diagonal
    //                    };

    //                    // 🔥 Líneas de proyección de F según estado del feed (q)
    //                    projectionLinesF = BuildProjectionLinesF(zF, q, x_int_q, y_int_q);

    //                    // Escalones
    //                    staircaseSteps = BuildStaircaseSteps(vlePoints, xD, xB, R, zF, x_int, y_int);

    //                    // 🔥 7. Marcadores con coordenadas en TooltipData
    //                    markers = BuildMarkers(xD, xB, zF, q, R);

    //                    // 🔥 Subtítulo con información de reflujo
    //                    chartSubtitle = $"Reflux Ratio R = {R:F2} | Min Reflux R_min = {R_min:F2}";

    //                }
    //                else
    //                {
    //                }

    //                var mcCabeData = new McCabeThieleData
    //                {
    //                    DiagonalLine = diagonalLine,
    //                    VLECurve = vlePoints,
    //                    RectifyingLine = rectifyingLine,
    //                    StrippingLine = strippingLine,
    //                    FeedLine = feedLine,
    //                    StaircaseSteps = staircaseSteps,
    //                    Markers = markers,
    //                    MinRefluxRectifyingLine = minRefluxRectifyingLine,
    //                    MinRefluxStrippingLine = minRefluxStrippingLine,
    //                    ProjectionLinesD = projectionLinesD,
    //                    ProjectionLinesB = projectionLinesB,
    //                    ProjectionLinesF = projectionLinesF,
    //                    MinRefluxRatio = R_min,
    //                    ChartTitle = "McCabe-Thiele Diagram",
    //                    ChartSubtitle = chartSubtitle
    //                };

    //                _column.Orchestrator?.SetMcCabeThieleData(mcCabeData);
    //            }
    //            catch (Exception ex)
    //            {
    //                _column.Orchestrator?.SetMcCabeThieleData(CreateEmptyData());
    //            }
    //        }, cancellationToken);
    //    }

    //    // 🔥 TAREA 7: Método para construir marcadores B, F, D según estado del feed (q)
    //    private List<MarkerPoint> BuildMarkers(double xD, double xB, double zF, double q, double R)
    //    {
    //        var markers = new List<MarkerPoint>();

    //        // B y D siempre en el eje X (Y = 0)
    //        markers.Add(new MarkerPoint
    //        {
    //            Label = "D",
    //            X = xD,
    //            Y = 0,
    //            Description = "Distillate",
    //            TooltipData = new Dictionary<string, string>
    //    {
    //        { "x", xD.ToString("F3") },
    //        { "y", "0.000" },
    //        { "Type", "Distillate" },
    //        { "Reflux Ratio (R)", R.ToString("F2") }
    //    }
    //        });

    //        markers.Add(new MarkerPoint
    //        {
    //            Label = "B",
    //            X = xB,
    //            Y = 0,
    //            Description = "Bottoms",
    //            TooltipData = new Dictionary<string, string>
    //    {
    //        { "x", xB.ToString("F3") },
    //        { "y", "0.000" },
    //        { "Type", "Bottoms" }
    //    }
    //        });

    //        // F depende del estado del feed (q)
    //        double markerX_F = 0;
    //        double markerY_F = 0;

    //        if (q > 1) // Líquido subenfriado
    //        {
    //            markerX_F = zF;
    //            markerY_F = 0; // Solo eje X
    //        }
    //        else if (Math.Abs(q - 1.0) < 0.01 || Math.Abs(q) < 0.01) // Líquido saturado (q=1) o Vapor saturado (q=0)
    //        {
    //            markerX_F = zF;
    //            markerY_F = zF; // En la diagonal (eje X y Y)
    //        }
    //        else if (q < 0) // Vapor sobrecalentado
    //        {
    //            markerX_F = 0; // Solo eje Y
    //            markerY_F = zF;
    //        }

    //        markers.Add(new MarkerPoint
    //        {
    //            Label = "F",
    //            X = markerX_F,
    //            Y = markerY_F,
    //            Description = $"Feed (q={q:F2})",
    //            TooltipData = new Dictionary<string, string>
    //    {
    //        { "x", markerX_F.ToString("F3") },
    //        { "y", markerY_F.ToString("F3") },
    //        { "Type", "Feed" },
    //        { "Feed Quality (q)", q.ToString("F2") }
    //    }
    //        });

    //        return markers;
    //    }    // 🔥 Método para construir líneas de proyección de F según estado del feed (q)
    //         // 🔥 Método para construir líneas de proyección de F según estado del feed (q)
    //         // 🔥 Método para construir líneas de proyección de F según estado del feed (q)
    //         // 🔥 Método para construir líneas de proyección de F según estado del feed (q)
    //         // 🔥 Método para construir 3 series de proyección de F (siempre)
    //    private List<(double x, double y)> BuildProjectionLinesF(double zF, double q, double x_int_q, double y_int_q)
    //    {
    //        var projectionLinesF = new List<(double x, double y)>();

    //        // Serie 1: Vertical desde eje X hasta diagonal
    //        projectionLinesF.Add((zF, 0));
    //        projectionLinesF.Add((zF, zF));

    //        // Serie 2: Línea q desde diagonal hasta curva VLE
    //        projectionLinesF.Add((zF, zF));
    //        projectionLinesF.Add((x_int_q, y_int_q));

    //        // Serie 3: Horizontal desde curva VLE hasta eje Y
    //        projectionLinesF.Add((x_int_q, y_int_q));
    //        projectionLinesF.Add((0, y_int_q));

    //        return projectionLinesF;
    //    }
    //    private List<StaircaseStep> BuildStaircaseSteps(
    //        List<(double x, double y)> vlePoints,
    //        double xD, double xB, double R, double zF,
    //        double x_int, double y_int)
    //    {
    //        var steps = new List<StaircaseStep>();

    //        if (!vlePoints.Any()) return steps;

    //        double x_op = xD;
    //        double y_op = xD;
    //        int stageNum = 1;
    //        bool inRectifying = true;

    //        while (x_op > xB && stageNum <= 150)
    //        {
    //            double x_vle = FindXForY(vlePoints, y_op);

    //            if (x_vle >= x_op)
    //            {
    //                x_vle = x_op - 0.001;
    //            }

    //            var point1 = (x_op, y_op);
    //            var point2 = (x_vle, y_op);

    //            if (inRectifying && x_vle <= x_int)
    //            {
    //                inRectifying = false;
    //            }

    //            double x_op_next = x_vle;
    //            double y_op_next;

    //            if (inRectifying)
    //            {
    //                y_op_next = (R / (R + 1.0)) * x_op_next + xD / (R + 1.0);
    //            }
    //            else
    //            {
    //                double slope = (y_int - xB) / (x_int - xB);
    //                y_op_next = slope * (x_op_next - xB) + xB;
    //            }

    //            var point3 = (x_op_next, y_op_next);

    //            steps.Add(new StaircaseStep
    //            {
    //                StageNumber = stageNum,
    //                Points = new List<(double x, double y)> { point1, point2, point3 },
    //                StageType = inRectifying ? "Rectifying" : "Stripping"
    //            });

    //            if (x_op_next <= xB) break;

    //            if (Math.Abs(y_op - y_op_next) < 1e-6 && Math.Abs(x_op - x_vle) < 1e-6) break;

    //            x_op = x_op_next;
    //            y_op = y_op_next;
    //            stageNum++;
    //        }

    //        return steps;
    //    }

    //    private double InterpolateVLE(List<(double x, double y)> vlePoints, double x)
    //    {
    //        if (x <= vlePoints.First().x) return vlePoints.First().y;
    //        if (x >= vlePoints.Last().x) return vlePoints.Last().y;

    //        int left = 0, right = vlePoints.Count - 1;
    //        while (left < right - 1)
    //        {
    //            int mid = (left + right) / 2;
    //            if (vlePoints[mid].x <= x)
    //                left = mid;
    //            else
    //                right = mid;
    //        }

    //        double x1 = vlePoints[left].x, y1 = vlePoints[left].y;
    //        double x2 = vlePoints[right].x, y2 = vlePoints[right].y;
    //        double diff = x2 - x1;

    //        if (Math.Abs(diff) < 1e-10) return y1;

    //        return y1 + (y2 - y1) * (x - x1) / diff;
    //    }

    //    private double FindXForY(List<(double x, double y)> vlePoints, double targetY)
    //    {
    //        if (targetY >= vlePoints.Last().y) return vlePoints.Last().x;
    //        if (targetY <= vlePoints.First().y) return vlePoints.First().x;

    //        for (int i = 0; i < vlePoints.Count - 1; i++)
    //        {
    //            if ((vlePoints[i].y <= targetY && vlePoints[i + 1].y >= targetY) ||
    //                (vlePoints[i].y >= targetY && vlePoints[i + 1].y <= targetY))
    //            {
    //                double diff = vlePoints[i + 1].y - vlePoints[i].y;
    //                if (Math.Abs(diff) < 1e-10) return vlePoints[i].x;

    //                double t = (targetY - vlePoints[i].y) / diff;
    //                return vlePoints[i].x + t * (vlePoints[i + 1].x - vlePoints[i].x);
    //            }
    //        }
    //        return targetY;
    //    }

    //    private void CalculateOperatingLinesIntersection(
    //        double xD, double xB, double R, double zF, double q,
    //        out double x_int, out double y_int)
    //    {
    //        if (Math.Abs(q - 1.0) < 1e-6)
    //        {
    //            x_int = zF;
    //            y_int = (R / (R + 1.0)) * zF + xD / (R + 1.0);
    //        }
    //        else
    //        {
    //            double R_frac = R / (R + 1.0);
    //            double D_frac = xD / (R + 1.0);
    //            double Q_frac1 = q / (q - 1.0);
    //            double Q_frac2 = -zF / (q - 1.0);

    //            x_int = (Q_frac2 - D_frac) / (R_frac - Q_frac1);
    //            y_int = R_frac * x_int + D_frac;
    //        }
    //    }

    //    private void FindQLineVLEIntersection(
    //        List<(double x, double y)> vlePoints,
    //        double zF, double q,
    //        out double x_int, out double y_int)
    //    {
    //        if (Math.Abs(q - 1.0) < 0.01)
    //        {
    //            x_int = zF;
    //            y_int = InterpolateVLE(vlePoints, zF);
    //        }
    //        else if (Math.Abs(q) < 0.01)
    //        {
    //            y_int = zF;
    //            x_int = FindXForY(vlePoints, zF);
    //        }
    //        else
    //        {
    //            double slope = q / (q - 1.0);
    //            double intercept = -zF / (q - 1.0);

    //            double left = 0, right = 1;
    //            for (int iter = 0; iter < 50; iter++)
    //            {
    //                double mid = (left + right) / 2.0;
    //                double y_q = slope * mid + intercept;
    //                double y_vle = InterpolateVLE(vlePoints, mid);

    //                if (Math.Abs(y_q - y_vle) < 1e-6)
    //                {
    //                    x_int = mid;
    //                    y_int = y_vle;
    //                    return;
    //                }

    //                double y_q_left = slope * left + intercept;
    //                double y_vle_left = InterpolateVLE(vlePoints, left);

    //                if ((y_q_left - y_vle_left) * (y_q - y_vle) < 0)
    //                    right = mid;
    //                else
    //                    left = mid;
    //            }

    //            x_int = (left + right) / 2.0;
    //            y_int = InterpolateVLE(vlePoints, x_int);
    //        }
    //    }

    //    private McCabeThieleData CreateEmptyData()
    //    {
    //        return new McCabeThieleData
    //        {
    //            DiagonalLine = new List<(double x, double y)>(),
    //            VLECurve = new List<(double x, double y)>(),
    //            RectifyingLine = new List<(double x, double y)>(),
    //            StrippingLine = new List<(double x, double y)>(),
    //            FeedLine = new List<(double x, double y)>(),
    //            StaircaseSteps = new List<StaircaseStep>(),
    //            Markers = new List<MarkerPoint>(),
    //            MinRefluxRectifyingLine = new List<(double x, double y)>(),
    //            MinRefluxStrippingLine = new List<(double x, double y)>(),
    //            ProjectionLinesB = new(),
    //            ProjectionLinesD = new(),
    //            ProjectionLinesF = new(),
    //            MinRefluxRatio = 0,
    //            ChartTitle = "McCabe-Thiele Diagram",
    //            ChartSubtitle = string.Empty
    //        };
    //    }

    //    // 🔥 TAREA 7: Método para construir marcadores B, F, D según estado del feed (q)

    //}

}
