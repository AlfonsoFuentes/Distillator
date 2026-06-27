using Client.Templates.Graphs;
using Client.Templates.Graphs.ChartBases;
using Microsoft.AspNetCore.Components;
using Shared.SolverConsecutive.Equipments.Columns;
using UnitSystem;

namespace Client.Pages.UnitOperations.Columns.ColumnProfiles.ColumnPlates
{
    public partial class TemperatureProfileChart
    {
        [Parameter] public SolverColumn? Column { get; set; }

        [Parameter] public bool IsLoading { get; set; } = false;

        private List<ChartSeries> _series = new();
        private List<ChartMarker> _markers = new();
        private ChartViewState _viewState = new();
        private double _maxStage = 1;
        private double _minTemp = 0;
        private double _maxTemp = 100;
        private UnitMeasure _currentUnit = TemperatureUnits.Kelvin;
        private List<UnitMeasure> _Units = new();
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            BuildSeries();
        }


        private string _chartTitle = "Temperature Profile";
        private string _chartSubtitle = string.Empty;

        private string _yAxisTitle = "Temperature (K)";

        private void BuildSeries()
        {
            _series.Clear();
            _markers.Clear();
            _chartTitle = "Temperature Profile";
            _chartSubtitle = string.Empty;

            if (Column?.CalculationResult == null || !Column.CalculationResult.Success)
                return;

            var stages = Column.CalculationResult.Stages;
            if (stages == null || !stages.Any()) return;

            _maxStage = stages.Max(s => s.StageNumber);
            // Obtener lista de unidades disponibles del primer dato
            if (!_Units.Any())
            {
                var sampleTemp = stages.First().Liquid.Temperature;
                _Units = sampleTemp.UnitsList;
            }
            _yAxisTitle = $"Temperature ({_currentUnit.Symbol})";
            // ==========================================
            // 1. TEMPERATURA DEL LÍQUIDO
            // ==========================================
            var liquidTempPoints = new List<ChartPoint>();
            double minTemp = double.MaxValue;
            double maxTemp = double.MinValue;

            foreach (var stage in stages.OrderBy(s => s.StageNumber))
            {
                double temp = stage.Liquid.Temperature.GetValue(_currentUnit);
                minTemp = Math.Min(minTemp, temp);
                maxTemp = Math.Max(maxTemp, temp);

                liquidTempPoints.Add(new ChartPoint
                {
                    X = stage.StageNumber,
                    Y = temp,
                    TooltipData = new Dictionary<string, string>
                {
                    { "Stage", stage.StageNumber.ToString() },
                    { "Type", "Liquid" },
                    { "Temperature", $"{temp:F2} {_currentUnit.Symbol}"}
                }
                });

                // Marcar platos especiales
                if (stage.IsFeedStage)
                {
                    _markers.Add(new ChartMarker
                    {
                        X = stage.StageNumber,
                        Y = temp,
                        Label = "F",
                        Description = "Feed Stage",
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Feed Stage" },
                        { "Temperature",  $"{temp:F2} {_currentUnit.Symbol}"}
                    }
                    });
                }
                else if (stage.IsCondenser)
                {
                    _markers.Add(new ChartMarker
                    {
                        X = stage.StageNumber,
                        Y = temp,
                        Label = "C",
                        Description = "Condenser",
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Condenser" },
                        { "Temperature",  $"{temp:F2} {_currentUnit.Symbol}" }
                    }
                    });
                }
                else if (stage.IsReboiler)
                {
                    _markers.Add(new ChartMarker
                    {
                        X = stage.StageNumber,
                        Y = temp,
                        Label = "R",
                        Description = "Reboiler",
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Reboiler" },
                        { "Temperature",  $"{temp:F2} {_currentUnit.Symbol}" }
                    }
                    });
                }
            }

            _minTemp = minTemp - 5;
            _maxTemp = maxTemp + 5;

            _series.Add(new ChartSeries
            {
                Name = "Temperature",
                SeriesType = ChartSeriesType.Both,
                Color = "#3b82f6",
                StrokeWidth = 2,
                PointRadius = 1,
                IsVisible = true,
                ShowInLegend = true,
                Points = liquidTempPoints
            });

            // ==========================================
            // 2. TEMPERATURA DEL VAPOR
            // ==========================================


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
