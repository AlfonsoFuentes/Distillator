using Client.Templates.Graphs;
using Client.Templates.Graphs.ChartBases;
using Microsoft.AspNetCore.Components;
using Shared.SolverConsecutive.Equipments.Columns;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Client.Pages.UnitOperations.Columns.ColumnProfiles.ColumnComposition
{


    public partial class McCabeThieleChart
    {
        [Parameter] public SolverColumn? Column { get; set; }
        [Parameter] public bool IsLoading { get; set; } = false;
        [Parameter] public bool ShowLegends { get; set; } = true;
        private List<ChartSeries> _series = new();
        private ChartViewState _viewState = new();
        private List<ChartMarker> _markers = new();

        // 🔥 NUEVO: Título y subtítulo
        private string _chartTitle = "McCabe-Thiele Diagram";
        private string _chartSubtitle = string.Empty;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            BuildSeries();
        }

        private void BuildSeries()
        {
            _series.Clear();
            _markers.Clear();
            _chartTitle = "McCabe-Thiele Diagram";
            _chartSubtitle = string.Empty;

            if (Column?.CalculationResult == null || !Column.CalculationResult.Success)
                return;

            var data = Column.CalculationResult.McCabeThiele;
            if (data == null)
                return;

            // 🔥 Título y subtítulo
            _chartTitle = data.ChartTitle;
            _chartSubtitle = data.ChartSubtitle;

            // ==========================================
            // 1. LÍNEA DIAGONAL DE REFERENCIA (y = x)
            // ==========================================
            _series.Add(new ChartSeries
            {
                Name = "y = x",
                SeriesType = ChartSeriesType.Line,
                Color = "#94a3b8",
                StrokeWidth = 0.8,
                IsVisible = true,
                ShowInLegend = true,
                Points = MapPoints(data.DiagonalLine, "Reference", "y = x")
            });

            // ==========================================
            // 2. CURVA VLE
            // ==========================================
            // ==========================================
            // 2. CURVA VLE
            // ==========================================
            _series.Add(new ChartSeries
            {
                Name = "VLE Curve",
                SeriesType = ChartSeriesType.Line,
                Color = "#3b82f6",
                StrokeWidth = 0.8,
                IsVisible = true,
                ShowInLegend = true,
                
                Points = data.VLECurve.Select(p => new ChartPoint
                {
                    X = p.x,
                    Y = p.y,
                    TooltipData = new Dictionary<string, string>
                {
                    { "x (liquid)", p.x.ToString("F3") },
                    { "y (vapor)", p.y.ToString("F3") }
                }
                }).ToList()
            });

            // ==========================================
            // 3. LÍNEA DE RECTIFICACIÓN (R actual)
            // ==========================================
            _series.Add(new ChartSeries
            {
                Name = "Rectifying",
                SeriesType = ChartSeriesType.Line,
                Color = "#f59e0b",
                StrokeWidth = 0.8,
                IsVisible = true,
                ShowInLegend = true,
                Points = MapPoints(data.RectifyingLine, "Section", "Rectifying")
            });

            // ==========================================
            // 4. LÍNEA DE AGOTAMIENTO (R actual)
            // ==========================================
            _series.Add(new ChartSeries
            {
                Name = "Stripping",
                SeriesType = ChartSeriesType.Line,
                Color = "#06b6d4",
                StrokeWidth = 0.8,
                IsVisible = true,
                ShowInLegend = true,
                Points = MapPoints(data.StrippingLine, "Section", "Stripping")
            });

            

            // ==========================================
            // 6. 🔥 LÍNEAS DE OPERACIÓN EN R_min (punteadas)
            // ==========================================
            //if (data.MinRefluxRatio > 0 && data.MinRefluxRectifyingLine.Any())
            //{
            //    _series.Add(new ChartSeries
            //    {
            //        Name = $"Rectifying at R_min ({data.MinRefluxRatio:F2})",
            //        SeriesType = ChartSeriesType.Line,
            //        Color = "#f59e0b",
            //        StrokeWidth = 0.8,
            //        StrokeDashArray = "5,5",
            //        IsVisible = false,
            //        ShowInLegend = true,
            //        Points = MapPoints(data.MinRefluxRectifyingLine, "Section", "Rectifying at R_min")
            //    });
            //}

            //if (data.MinRefluxRatio > 0 && data.MinRefluxStrippingLine.Any())
            //{
            //    _series.Add(new ChartSeries
            //    {
            //        Name = $"Stripping at R_min ({data.MinRefluxRatio:F2})",
            //        SeriesType = ChartSeriesType.Line,
            //        Color = "#06b6d4",
            //        StrokeWidth = 0.8,
            //        StrokeDashArray = "5,5",
            //        IsVisible = false,
            //        ShowInLegend = true,
            //        Points = MapPoints(data.MinRefluxStrippingLine, "Section", "Stripping at R_min")
            //    });
            //}

            // ==========================================
            // 7. 🔥 LÍNEAS DE PROYECCIÓN (D y B hacia diagonal)
            // ==========================================
            if (data.ProjectionLinesD.Any())
            {
                _series.Add(new ChartSeries
                {
                    Name = "Projection D",
                    SeriesType = ChartSeriesType.Line,
                    Color = "#1e293b",
                    StrokeWidth = 0.8,
                    StrokeDashArray = "2,4",
                    IsVisible = true,
                    ShowInLegend = false,
                    ShowPoints = false,
                    Points = MapPoints(data.ProjectionLinesD, "Type", "Projection D")
                });
            }

            if (data.ProjectionLinesB.Any())
            {
                _series.Add(new ChartSeries
                {
                    Name = "Projection B",
                    SeriesType = ChartSeriesType.Line,
                    Color = "#1e293b",
                    StrokeWidth = 0.8,
                    StrokeDashArray = "2,4",
                    IsVisible = true,
                    ShowInLegend = false,
                    ShowPoints = false,
                    Points = MapPoints(data.ProjectionLinesB, "Type", "Projection B")
                });
            }

            if (data.ProjectionLinesF.Any())
            {
                _series.Add(new ChartSeries
                {
                    Name = "Projection F",
                    SeriesType = ChartSeriesType.Line,
                    Color = "#1e293b",
                    StrokeWidth = 0.8,
                    StrokeDashArray = "2,4",
                    IsVisible = true,
                    ShowInLegend = false,
                    ShowPoints = false,
                    Points = MapPoints(data.ProjectionLinesF, "Type", "Projection F")
                });
            }

            // ==========================================
            // ✅ NUEVO: Proyección desde F (diagonal) hasta eje X
            // ==========================================
            if (data.ProjectionLinesFToX.Any())
            {
                _series.Add(new ChartSeries
                {
                    Name = "Feed Projection (F to X)",
                    SeriesType = ChartSeriesType.Line,
                    Color = "#1e293b",
                    StrokeWidth = 0.8,
                    StrokeDashArray = "2,4",
                    IsVisible = true,
                    ShowInLegend = false,
                    ShowPoints = false,
                    Points = MapPoints(data.ProjectionLinesFToX, "Type", "Projection F to X")
                });
            }

            // ==========================================
            // ✅ NUEVO: Proyección desde intersección hasta eje Y
            // ==========================================
            if (data.ProjectionLinesIntersectToY.Any())
            {
                _series.Add(new ChartSeries
                {
                    Name = "Intersection Projection (to Y)",
                    SeriesType = ChartSeriesType.Line,
                    Color = "#1e293b",
                    StrokeWidth = 0.8,
                    StrokeDashArray = "2,4",
                    IsVisible = true,
                    ShowInLegend = false,
                    ShowPoints = false,
                    Points = MapPoints(data.ProjectionLinesIntersectToY, "Type", "Projection to Y")
                });
            }

            // ==========================================
            // 8. ESCALONES DE PLATOS (StepLine)
            // ==========================================

            // ==========================================
            // 8. ESCALONES DE PLATOS (StepLine)
            // ==========================================
            if (data.StaircaseSteps.Any())
            {
                var staircasePoints = new List<ChartPoint>();
                foreach (var step in data.StaircaseSteps)
                {
                    foreach (var point in step.Points)
                    {
                        staircasePoints.Add(new ChartPoint
                        {
                            X = point.x,
                            Y = point.y,
                            TooltipData = new Dictionary<string, string>
                        {
                            { "Stage", step.StageNumber.ToString() },
                            { "Type", step.StageType },
                            { "x (liquid)", point.x.ToString("F3") },
                            { "y (vapor)", point.y.ToString("F3") }
                        }
                        });
                    }
                }

                _series.Add(new ChartSeries
                {
                    Name = "Stages",
                    SeriesType = ChartSeriesType.StepLine,
                    Color = "#10b981",
                    StrokeWidth = 0.8,
                    IsVisible = true,
                    ShowInLegend = true,
                    Points = staircasePoints
                });
            }

            // ==========================================
            // 9. MARCADORES D, B, F
            // ==========================================
            foreach (var marker in data.Markers)
            {
                _markers.Add(new ChartMarker
                {
                    X = marker.X,
                    Y = marker.Y,
                    Label = marker.Label,
                    Description = marker.Description,
                    TooltipData = marker.TooltipData  // 🔥 Usar el TooltipData del backend
                });
            }
        }

        // 🔥 Helper para mapear tuplas a ChartPoint con tooltip genérico
        private List<ChartPoint> MapPoints(List<(double x, double y)> points, string tooltipKey, string tooltipValue)
        {
            return points.Select(p => new ChartPoint
            {
                X = p.x,
                Y = p.y,
                TooltipData = new Dictionary<string, string>
            {
                { tooltipKey, tooltipValue },
                { "x", p.x.ToString("F3") },
                { "y", p.y.ToString("F3") }
            }
            }).ToList();
        }
    }
}