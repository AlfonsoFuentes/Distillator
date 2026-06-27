using Client.Templates.Graphs;
using Client.Templates.Graphs.ChartBases;
using Microsoft.AspNetCore.Components;
using Shared.SolverConsecutive.Equipments.Columns;
using UnitSystem;

namespace Client.Pages.UnitOperations.Columns.ColumnProfiles.ColumnPlates
{
    public partial class PressureProfileChart
    {
        [Parameter] public SolverColumn? Column { get; set; }
        [Parameter] public bool IsLoading { get; set; } = false;

        private List<ChartSeries> _series = new();
        private List<ChartMarker> _markers = new();
        private ChartViewState _viewState = new();
        private double _maxStage = 1;
        private double _minPressure = 0;
        private double _maxPressure = 100;
        private string _chartTitle = "Pressure Profile";
        private string _chartSubtitle = string.Empty;
        private UnitMeasure _currentUnit = PressureUnits.Bara;
        private List<UnitMeasure> _Units = new();

        private string _yAxisTitle = "Temperature (K)";
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            BuildSeries();
        }

        private void BuildSeries()
        {
            _series.Clear();
            _markers.Clear();
            _chartTitle = "Pressure Profile";
            _chartSubtitle = string.Empty;

            if (Column?.CalculationResult == null || !Column.CalculationResult.Success)
                return;

            var stages = Column.CalculationResult.Stages;
            if (stages == null || !stages.Any()) return;

            if (!_Units.Any())
            {
                var sampleTemp = stages.First().Liquid.Pressure;
                _Units = sampleTemp.UnitsList;
            }
            _yAxisTitle = $"Pressure ({_currentUnit.Symbol})";
            _maxStage = stages.Max(s => s.StageNumber);

            // ==========================================
            // PRESIÓN DEL PLATO (LÍQUIDO = VAPOR en equilibrio)
            // ==========================================
            var pressurePoints = new List<ChartPoint>();
            double minPressure = double.MaxValue;
            double maxPressure = double.MinValue;

            foreach (var stage in stages.OrderBy(s => s.StageNumber))
            {
                // En equilibrio, P_liquid = P_vapor
                // Usamos la presión del líquido
                double pressure = stage.Liquid.Pressure.GetValue(_currentUnit);
                minPressure = Math.Min(minPressure, pressure);
                maxPressure = Math.Max(maxPressure, pressure);

                pressurePoints.Add(new ChartPoint
                {
                    X = stage.StageNumber,
                    Y = pressure,
                    TooltipData = new Dictionary<string, string>
                {
                    { "Stage", stage.StageNumber.ToString() },
                    { "Pressure", $"{pressure:F2} {_currentUnit.Symbol}" }
                }
                });

                // Marcar platos especiales
                if (stage.IsFeedStage)
                {
                    _markers.Add(new ChartMarker
                    {
                        X = stage.StageNumber,
                        Y = pressure,
                        Label = "F",
                        Description = "Feed Stage",
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Feed Stage" },
                        { "Pressure", $"{pressure:F2} {_currentUnit.Symbol}" }
                    }
                    });
                }
                else if (stage.IsCondenser)
                {
                    _markers.Add(new ChartMarker
                    {
                        X = stage.StageNumber,
                        Y = pressure,
                        Label = "C",
                        Description = "Condenser",
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Condenser" },
                        { "Pressure", $"{pressure:F2} {_currentUnit.Symbol}" }
                    }
                    });
                }
                else if (stage.IsReboiler)
                {
                    _markers.Add(new ChartMarker
                    {
                        X = stage.StageNumber,
                        Y = pressure,
                        Label = "R",
                        Description = "Reboiler",
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Reboiler" },
                        { "Pressure", $"{pressure:F2} {_currentUnit.Symbol}" }
                    }
                    });
                }
            }

            // Agregar márgenes del 5%
            double pressureRange = maxPressure - minPressure;
            _minPressure = minPressure - (pressureRange * 0.05);
            _maxPressure = maxPressure + (pressureRange * 0.05);

            _series.Add(new ChartSeries
            {
                Name = "Pressure",
                SeriesType = ChartSeriesType.Both,
                Color = "#10b981",
                StrokeWidth = 2,
                PointRadius = 1,
                IsVisible = true,
                ShowInLegend = true,
                Points = pressurePoints
            });

            _chartSubtitle = $"Stages: {stages.Count}";
        }
        private void OnUnitChanged(UnitMeasure newUnit)
        {
            _currentUnit = newUnit;
            BuildSeries();
            StateHasChanged();
        }
    }
}
