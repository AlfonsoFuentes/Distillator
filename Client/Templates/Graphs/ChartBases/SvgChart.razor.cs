using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Globalization;
using UnitSystem;

namespace Client.Templates.Graphs.ChartBases
{
    public partial class SvgChart : IAsyncDisposable
    {    // --- NUEVOS PARÁMETROS PARA UNIDADES DINÁMICAS ---
        [Parameter] public List<UnitMeasure>? UnitOptions { get; set; }
        [Parameter] public UnitMeasure? SelectedUnit { get; set; }
        [Parameter] public EventCallback<UnitMeasure> UnitChanged { get; set; }
        [Parameter] public string ChartTitle { get; set; } = string.Empty;
        [Parameter] public string ChartSubtitle { get; set; } = string.Empty;
        [Parameter] public List<ChartSeries> Series { get; set; } = new();
        [Parameter] public string XAxisTitle { get; set; } = "X Axis";
        [Parameter] public string YAxisTitle { get; set; } = "Y Axis";
        [Parameter] public double XMin { get; set; } = 0;
        [Parameter] public double XMax { get; set; } = 1;
        [Parameter] public double YMin { get; set; } = 0;
        [Parameter] public double YMax { get; set; } = 1;
        [Parameter] public ChartViewState ViewState { get; set; } = new();
        [Parameter] public bool IsLoading { get; set; } = false;
        [Parameter] public string LoadingText { get; set; } = "Calculating...";
        [Parameter] public List<ChartMarker> Markers { get; set; } = new();
        [Parameter] public bool ShowLegends { get; set; } = true;
        [Parameter] public double? XTickStep { get; set; }  // ✅ NUEVO
        [Parameter] public double? YTickStep { get; set; }  // ✅ NUEVO

        private const double _tooltipWidth = 220;
        private const double _tooltipHeight = 150;
        private const double _tooltipOffset = 15;
        public double PublicMapX(double value) => MapX(value);
        public double PublicMapY(double value) => MapY(value);

        private ElementReference chartContainerRef;
        private ChartTooltip? CurrentTooltip { get; set; }
        private bool _isPanning = false;
        private double _lastMouseX = 0;
        private double _lastMouseY = 0;

        private List<double> XTicks => GenerateTicks(XMin, XMax, XTickStep, 10);
        private List<double> YTicks => GenerateTicks(YMin, YMax, YTickStep, 8);  // ✅ Reducido de 10 a 8

        private List<double> GenerateTicks(double min, double max, double? step, int maxCount)
        {
            var ticks = new List<double>();
            if (max <= min) return ticks;

            double range = max - min;

            if (step.HasValue)
            {
                // Generar ticks con el step definido
                double actualStep = step.Value;
                double start = Math.Ceiling(min / actualStep) * actualStep;
                for (double value = start; value <= max + actualStep / 2; value += actualStep)
                {
                    ticks.Add(value);
                }
            }
            else
            {
                // Calcular step automático para no exceder maxCount ticks
                double desiredStep = range / maxCount;

                // Redondear a un número "bonito" (1, 2, 5, 10, 20, 50, 100...)
                double magnitude = Math.Pow(10, Math.Floor(Math.Log10(desiredStep)));
                double residual = desiredStep / magnitude;
                double niceStep;

                if (residual <= 1.5) niceStep = 1 * magnitude;
                else if (residual <= 3) niceStep = 2 * magnitude;
                else if (residual <= 7) niceStep = 5 * magnitude;
                else niceStep = 10 * magnitude;

                double start = Math.Ceiling(min / niceStep) * niceStep;
                for (double value = start; value <= max + niceStep / 2; value += niceStep)
                {
                    ticks.Add(value);
                }
            }

            return ticks;
        }

        private double MapX(double value)
        {
            if (XMax == XMin) return ViewState.PaddingLeft;

            var result = ViewState.PaddingLeft + ((value - XMin) / (XMax - XMin)) * ViewState.InnerWidth;
            return Math.Round(result);
        }

        private double MapY(double value)
        {
            if (YMax == YMin) return ViewState.BaseHeight - ViewState.PaddingBottom;

            var result = (ViewState.BaseHeight - ViewState.PaddingBottom) - ((value - YMin) / (YMax - YMin)) * ViewState.InnerHeight;
            return Math.Round(result);
        }

        private string FormatNumber(double value, bool isXAxis = false)
        {
            if (Math.Abs(value) < 0.001 && value != 0)
                return value.ToString("E1", CultureInfo.InvariantCulture);

            // ✅ Si es eje X y hay un step definido, formatear como entero
            if (isXAxis && XTickStep.HasValue)
                return value.ToString("0", CultureInfo.InvariantCulture);

            // Formato para valores entre 0 y 1 (como fracciones molares)
            if (value > 0 && value < 1)
                return value.ToString("0.00", CultureInfo.InvariantCulture);

            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("canvasInterop.preventScroll", chartContainerRef);
            }
            
        }

        public async ValueTask DisposeAsync() { }
        private RenderFragment RenderSeries2(ChartSeries series)
        {
            return builder =>
            {
                if (!series.IsVisible) return;
                if (series.Points == null || !series.Points.Any()) return;

                // 🔥 Calcular grosor y opacidad
                double adjustedStrokeWidth = series.StrokeWidth;
                double opacity = 1.0;

                // Verificar si hay alguna serie resaltada
                bool anySeriesHighlighted = Series.Any(s => s.IsHighlighted && s.IsVisible);

                if (series.IsHighlighted)
                {
                    // 🔥 Serie resaltada: más gruesa y opaca
                    adjustedStrokeWidth = 4.0; // Valor fijo más grueso
                    opacity = 1.0;
                }
                else if (anySeriesHighlighted)
                {
                    // 🔥 Otras series: más delgadas y transparentes
                    adjustedStrokeWidth = 0.8; // Más delgado
                    opacity = 0.3; // Más visible que 0.2
                }

                // Ajustar por zoom si es necesario
                if (ViewState.ZoomLevel > 2.0)
                {
                    adjustedStrokeWidth = Math.Max(adjustedStrokeWidth / ViewState.ZoomLevel, 0.5);
                }

                bool hasDashArray = !string.IsNullOrEmpty(series.StrokeDashArray);

                // Línea continua
                if (series.SeriesType == ChartSeriesType.Line || series.SeriesType == ChartSeriesType.Both)
                {
                    string pointsStr = string.Join(" ", series.Points.Select(p =>
                        $"{MapX(p.X).ToString("0.##", CultureInfo.InvariantCulture)},{MapY(p.Y).ToString("0.##", CultureInfo.InvariantCulture)}"));
                    builder.OpenElement(0, "polyline");
                    builder.AddAttribute(1, "points", pointsStr);
                    builder.AddAttribute(2, "fill", "none");
                    builder.AddAttribute(3, "stroke", series.Color);
                    builder.AddAttribute(4, "stroke-width", adjustedStrokeWidth);
                    builder.AddAttribute(5, "opacity", opacity.ToString(CultureInfo.InvariantCulture));
                    if (hasDashArray)
                    {
                        builder.AddAttribute(6, "stroke-dasharray", series.StrokeDashArray);
                    }
                    builder.CloseElement();
                }

                // StepLine (escalones)
                if (series.SeriesType == ChartSeriesType.StepLine)
                {
                    var pathData = new System.Text.StringBuilder();
                    for (int i = 0; i < series.Points.Count; i++)
                    {
                        var point = series.Points[i];
                        double x = MapX(point.X);
                        double y = MapY(point.Y);

                        if (i == 0)
                        {
                            pathData.Append($"M {x.ToString("0.##", CultureInfo.InvariantCulture)} {y.ToString("0.##", CultureInfo.InvariantCulture)}");
                        }
                        else
                        {
                            pathData.Append($" H {x.ToString("0.##", CultureInfo.InvariantCulture)}");
                            pathData.Append($" V {y.ToString("0.##", CultureInfo.InvariantCulture)}");
                        }
                    }

                    builder.OpenElement(0, "path");
                    builder.AddAttribute(1, "d", pathData.ToString());
                    builder.AddAttribute(2, "fill", "none");
                    builder.AddAttribute(3, "stroke", series.Color);
                    builder.AddAttribute(4, "stroke-width", adjustedStrokeWidth);
                    builder.AddAttribute(5, "opacity", opacity.ToString(CultureInfo.InvariantCulture));
                    if (hasDashArray)
                    {
                        builder.AddAttribute(6, "stroke-dasharray", series.StrokeDashArray);
                    }
                    builder.CloseElement();
                }

                // Puntos
                if (series.Points.Any())
                {
                    // 🔥 Calcular radio ajustado según zoom (escalado inverso)
                    double adjustedRadius = series.PointRadius / Math.Sqrt(ViewState.ZoomLevel);
                    adjustedRadius = Math.Max(adjustedRadius, 0.5); // Mínimo 0.5 para que sigan siendo clickeables

                    foreach (var point in series.Points)
                    {
                        bool isVisible = series.SeriesType == ChartSeriesType.Points ||
                                          series.SeriesType == ChartSeriesType.Both ||
                                          series.ShowPoints;

                        builder.OpenElement(0, "circle");
                        builder.AddAttribute(1, "cx", MapX(point.X).ToString("0.##", CultureInfo.InvariantCulture));
                        builder.AddAttribute(2, "cy", MapY(point.Y).ToString("0.##", CultureInfo.InvariantCulture));
                        builder.AddAttribute(3, "r", isVisible ? adjustedRadius.ToString("0.##", CultureInfo.InvariantCulture) : "1");
                        builder.AddAttribute(4, "fill", series.Color);
                        builder.AddAttribute(5, "opacity", isVisible ? opacity.ToString(CultureInfo.InvariantCulture) : "0");
                        builder.AddAttribute(6, "stroke", "#ffffff");
                        builder.AddAttribute(7, "stroke-width", "1.5");
                        builder.AddAttribute(8, "style", "cursor: pointer;");
                        builder.AddAttribute(9, "onmouseenter", (MouseEventArgs e) => HandlePointHover(point, series, e));
                        builder.AddAttribute(10, "onmouseleave", HandlePointLeave);
                        builder.AddAttribute(11, "ondblclick", () => HandlePointDoubleClick(series));
                        builder.CloseElement();
                    }
                }
            };
        }
        private RenderFragment RenderSeries(ChartSeries series)
        {
            return builder =>
            {
                if (!series.IsVisible) return;
                if (series.Points == null || !series.Points.Any()) return;

                // 🔥 Calcular grosor y opacidad
                double adjustedStrokeWidth = series.StrokeWidth;
                double opacity = 1.0;

                // Verificar si hay alguna serie resaltada
                bool anySeriesHighlighted = Series.Any(s => s.IsHighlighted && s.IsVisible);

                if (series.IsHighlighted)
                {
                    // 🔥 Serie resaltada: más gruesa y opaca
                    adjustedStrokeWidth = 4.0; // Valor fijo más grueso
                    opacity = 1.0;
                }
                else if (anySeriesHighlighted)
                {
                    // 🔥 Otras series: más delgadas y transparentes
                    adjustedStrokeWidth = 0.8; // Más delgado
                    opacity = 0.3; // Más visible que 0.2
                }

                // ✅ Ajustar por zoom (fórmula mejorada)
                if (ViewState.ZoomLevel > 1.0)
                {
                    // Fórmula más agresiva: dividir por sqrt(zoom) en lugar de zoom directo
                    adjustedStrokeWidth = adjustedStrokeWidth / Math.Sqrt(ViewState.ZoomLevel);
                    adjustedStrokeWidth = Math.Max(adjustedStrokeWidth, 0.3);  // Mínimo más bajo
                }
                else if (ViewState.ZoomLevel < 1.0)
                {
                    // Cuando hay zoom out, aumentar ligeramente
                    adjustedStrokeWidth = adjustedStrokeWidth * Math.Sqrt(1.0 / ViewState.ZoomLevel);
                    adjustedStrokeWidth = Math.Min(adjustedStrokeWidth, 4.0);  // Máximo para no saturar
                }

                bool hasDashArray = !string.IsNullOrEmpty(series.StrokeDashArray);

                // Línea continua
                if (series.SeriesType == ChartSeriesType.Line || series.SeriesType == ChartSeriesType.Both)
                {
                    string pointsStr = string.Join(" ", series.Points.Select(p =>
                        $"{MapX(p.X).ToString("0.##", CultureInfo.InvariantCulture)},{MapY(p.Y).ToString("0.##", CultureInfo.InvariantCulture)}"));
                    builder.OpenElement(0, "polyline");
                    builder.AddAttribute(1, "points", pointsStr);
                    builder.AddAttribute(2, "fill", "none");
                    builder.AddAttribute(3, "stroke", series.Color);
                    builder.AddAttribute(4, "stroke-width", adjustedStrokeWidth);
                    builder.AddAttribute(5, "opacity", opacity.ToString(CultureInfo.InvariantCulture));
                    if (hasDashArray)
                    {
                        builder.AddAttribute(6, "stroke-dasharray", series.StrokeDashArray);
                    }
                    builder.CloseElement();
                }

                // StepLine (escalones)
                if (series.SeriesType == ChartSeriesType.StepLine)
                {
                    var pathData = new System.Text.StringBuilder();
                    for (int i = 0; i < series.Points.Count; i++)
                    {
                        var point = series.Points[i];
                        double x = MapX(point.X);
                        double y = MapY(point.Y);

                        if (i == 0)
                        {
                            pathData.Append($"M {x.ToString("0.##", CultureInfo.InvariantCulture)} {y.ToString("0.##", CultureInfo.InvariantCulture)}");
                        }
                        else
                        {
                            pathData.Append($" H {x.ToString("0.##", CultureInfo.InvariantCulture)}");
                            pathData.Append($" V {y.ToString("0.##", CultureInfo.InvariantCulture)}");
                        }
                    }

                    builder.OpenElement(0, "path");
                    builder.AddAttribute(1, "d", pathData.ToString());
                    builder.AddAttribute(2, "fill", "none");
                    builder.AddAttribute(3, "stroke", series.Color);
                    builder.AddAttribute(4, "stroke-width", adjustedStrokeWidth);
                    builder.AddAttribute(5, "opacity", opacity.ToString(CultureInfo.InvariantCulture));
                    if (hasDashArray)
                    {
                        builder.AddAttribute(6, "stroke-dasharray", series.StrokeDashArray);
                    }
                    builder.CloseElement();
                }

                // Puntos
                if (series.Points.Any())
                {
                    // 🔥 Calcular radio ajustado según zoom (escalado inverso)
                    double adjustedRadius = series.PointRadius / Math.Sqrt(ViewState.ZoomLevel);
                    adjustedRadius = Math.Max(adjustedRadius, 0.5); // Mínimo 0.5 para que sigan siendo clickeables

                    foreach (var point in series.Points)
                    {
                        bool isVisible = series.SeriesType == ChartSeriesType.Points ||
                                          series.SeriesType == ChartSeriesType.Both ||
                                          series.ShowPoints;

                        builder.OpenElement(0, "circle");
                        builder.AddAttribute(1, "cx", MapX(point.X).ToString("0.##", CultureInfo.InvariantCulture));
                        builder.AddAttribute(2, "cy", MapY(point.Y).ToString("0.##", CultureInfo.InvariantCulture));
                        builder.AddAttribute(3, "r", isVisible ? adjustedRadius.ToString("0.##", CultureInfo.InvariantCulture) : "1");
                        builder.AddAttribute(4, "fill", series.Color);
                        builder.AddAttribute(5, "opacity", isVisible ? opacity.ToString(CultureInfo.InvariantCulture) : "0");
                        builder.AddAttribute(6, "stroke", "#ffffff");
                        builder.AddAttribute(7, "stroke-width", "1.5");
                        builder.AddAttribute(8, "style", "cursor: pointer;");
                        builder.AddAttribute(9, "onmouseenter", (MouseEventArgs e) => HandlePointHover(point, series, e));
                        builder.AddAttribute(10, "onmouseleave", HandlePointLeave);
                        builder.AddAttribute(11, "ondblclick", () => HandlePointDoubleClick(series));
                        builder.CloseElement();
                    }
                }
            };
        }

        // --- Zoom & Pan Logic ---
        private void HandleWheel(WheelEventArgs e)
        {
            double factor = e.DeltaY > 0 ? 0.9 : 1.1;
            ViewState.ZoomLevel = Math.Clamp(ViewState.ZoomLevel * factor, 0.1, 10);
            StateHasChanged();
        }

        private void HandleMouseDown(MouseEventArgs e)
        {
            _isPanning = true;
            _lastMouseX = e.ClientX;
            _lastMouseY = e.ClientY;
        }

        private void HandleMouseMove(MouseEventArgs e)
        {
            if (!_isPanning) return;

            double dx = (e.ClientX - _lastMouseX) / ViewState.ZoomLevel;
            double dy = (e.ClientY - _lastMouseY) / ViewState.ZoomLevel;

            ViewState.PanX -= dx;
            ViewState.PanY -= dy;

            _lastMouseX = e.ClientX;
            _lastMouseY = e.ClientY;
            StateHasChanged();
        }

        private void HandleMouseUp(MouseEventArgs e)
        {
            _isPanning = false;
        }

        private void ResetZoom()
        {
            ViewState.Reset();
            StateHasChanged();
        }

        // --- Tooltip Logic (puntos de series) ---
        // --- Tooltip Logic (puntos de series) ---
        private void HandlePointHover(ChartPoint point, ChartSeries series, MouseEventArgs e)
        {
            // 🔥 Obtener el bounding rect del contenedor del gráfico
            // Esto lo haremos pasando las coordenadas relativas desde el evento

            CurrentTooltip = new ChartTooltip
            {
                // 🔥 Usar offsetX/offsetY que son relativos al elemento
                ScreenX = e.OffsetX,
                ScreenY = e.OffsetY,
                Title = $"Point ({point.X:F3}, {point.Y:F3})",
                IsVisible = true
            };

            if (point.TooltipData != null)
            {
                foreach (var kvp in point.TooltipData)
                {
                    CurrentTooltip.Rows.Add(new TooltipRow
                    {
                        Label = kvp.Key,
                        Value = kvp.Value,
                        Color = series.Color
                    });
                }
            }
            StateHasChanged();
        }

        // --- Tooltip Logic (marcadores D, B, F) ---
        // --- Tooltip Logic (marcadores D, B, F) ---
        private void HandleMarkerHover(ChartMarker marker, MouseEventArgs e)
        {
            CurrentTooltip = new ChartTooltip
            {
                ScreenX = e.OffsetX,
                ScreenY = e.OffsetY,
                Title = marker.Label,
                IsVisible = true
            };

            if (!marker.TooltipData.ContainsKey("x") && !marker.TooltipData.ContainsKey("y"))
            {
                CurrentTooltip.Rows.Add(new TooltipRow
                {
                    Label = "Point",
                    Value = $"({marker.X:F3}, {marker.Y:F3})",
                    Color = "#1e293b"
                });
            }

            if (marker.TooltipData != null)
            {
                foreach (var kvp in marker.TooltipData)
                {
                    CurrentTooltip.Rows.Add(new TooltipRow
                    {
                        Label = kvp.Key,
                        Value = kvp.Value,
                        Color = "#1e293b"
                    });
                }
            }
            StateHasChanged();
        }

        private string CalculateTooltipPosition(double screenX, double screenY)
        {
            const double windowWidth = 1920;
            const double windowHeight = 1080;
            const double tooltipWidth = 250;
            const double tooltipHeight = 150;

            bool hasSpaceRight = (windowWidth - screenX) > tooltipWidth;
            bool hasSpaceLeft = screenX > tooltipWidth;
            bool hasSpaceBottom = (windowHeight - screenY) > tooltipHeight;
            bool hasSpaceTop = screenY > tooltipHeight;

            if (hasSpaceRight) return "tooltip-right";
            if (hasSpaceLeft) return "tooltip-left";
            if (hasSpaceBottom) return "tooltip-bottom";
            if (hasSpaceTop) return "tooltip-top";

            return "tooltip-right";
        }

        private void HandlePointLeave()
        {
            CurrentTooltip = null;
            StateHasChanged();
        }

        // 🔥 TAREA 5: Doble click en puntos para ocultar/mostrar puntos de la serie
        private void HandlePointDoubleClick(ChartSeries series)
        {
            series.ShowPoints = !series.ShowPoints;
            StateHasChanged();
        }
        // --- Leyenda Interacción ---
        private System.Timers.Timer? _clickTimer;
        private ChartSeries? _lastClickSeries;



        private void HandleLegendClick(ChartSeries series)
        {
            if (_clickTimer == null)
            {
                _clickTimer = new System.Timers.Timer(300);
                _clickTimer.Elapsed += (s, e) =>
                {
                    _clickTimer?.Stop();
                    _clickTimer = null;

                    if (_lastClickSeries == series)
                    {
                        ToggleHighlight(series);
                    }
                    _lastClickSeries = null;
                };
                _clickTimer.AutoReset = false;
            }

            _lastClickSeries = series;
            _clickTimer.Start();
        }

        private void HandleLegendDoubleClick(ChartSeries series)
        {
            _clickTimer?.Stop();
            _clickTimer = null;
            _lastClickSeries = null;

            ToggleVisibility(series);
        }

        private void ToggleHighlight(ChartSeries clickedSeries)
        {
            // 🔥 Si la serie está oculta, hacerla visible (normal) y salir
            if (!clickedSeries.IsVisible)
            {
                clickedSeries.IsVisible = true;
                clickedSeries.IsHighlighted = false; // Aparece normal, no resaltada
                StateHasChanged();
                return;
            }

            // Si ya está resaltada, desresaltar TODAS
            if (clickedSeries.IsHighlighted)
            {
                foreach (var s in Series)
                {
                    s.IsHighlighted = false;
                }
            }
            else
            {
                // Si no está resaltada, desresaltar todas y resaltar solo esta
                foreach (var s in Series)
                {
                    s.IsHighlighted = (s == clickedSeries);
                }
            }

            StateHasChanged();
        }

        private void ToggleVisibility(ChartSeries series)
        {
            series.IsVisible = !series.IsVisible;

            if (!series.IsVisible)
            {
                series.IsHighlighted = false;
            }

            StateHasChanged();
        }
    }


   
}