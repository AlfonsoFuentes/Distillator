using Client.Templates.Graphs;
using Client.Templates.Graphs.ChartBases;
using Microsoft.AspNetCore.Components;
using Shared.SolverConsecutive.Equipments.Columns;
using Shared.SolverConsecutive.Equipments.Columns.Orchestrador;
using UnitSystem;

namespace Client.Pages.UnitOperations.Columns.ColumnProfiles.ColumnComposition
{

    public partial class PressureCompositionChart
    {
        [Parameter] public SolverColumn? Column { get; set; }
        [Parameter] public bool IsLoading { get; set; } = false;

        private List<ChartSeries> _series = new();
        private List<ChartMarker> _markers = new();
        private ChartViewState _viewState = new();
        private double _minPressure = 0;
        private double _maxPressure = 10;
        private string _chartTitle = "Pressure-Composition Profile";
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
            _chartTitle = "Pressure-Composition Profile";
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
            var feed = Column.Feeds.FirstOrDefault();
            if (feed == null) return;

            var lightestComponent = feed.Composition.Components
                .OrderBy(c => c.DataBase.FullData.BoilingPoint?.GetValue(TemperatureUnits.Kelvin) ?? 0)
                .FirstOrDefault();

            if (lightestComponent == null) return;

            var componentId = lightestComponent.Id;

            var compositionPoints = new List<ChartPoint>();
            double minPressure = double.MaxValue;
            double maxPressure = double.MinValue;

            foreach (var stage in stages.OrderBy(s => s.StageNumber))
            {
                if (stage.Liquid.MolarComposition.TryGetValue(componentId, out StageStreamComponentResult? composition))
                {
                    double pressure = stage.Liquid.Pressure.GetValue(_currentUnit);
                    minPressure = Math.Min(minPressure, pressure);
                    maxPressure = Math.Max(maxPressure, pressure);

                    compositionPoints.Add(new ChartPoint
                    {
                        X = composition.MolarComposition,
                        Y = pressure,
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Component", composition.ComponentName },
                        { "Stage", stage.StageNumber.ToString() },
                        { "Composition", composition.MolarComposition.ToString("F3") },
                        { "Pressure", $"{pressure:F2} {_currentUnit.Symbol}" }
                    }
                    });

                    if (stage.IsFeedStage)
                    {
                        _markers.Add(new ChartMarker
                        {
                            X = composition.MolarComposition,
                            Y = pressure,
                            Label = "F",
                            Description = "Feed Stage",
                            TooltipData = new Dictionary<string, string>
                        {
                            { "Stage", stage.StageNumber.ToString() },
                            { "Type", "Feed Stage" },
                            { "Composition", composition.MolarComposition.ToString("F3") },
                            { "Pressure", $"{pressure:F2} {_currentUnit.Symbol}" }
                        }
                        });
                    }
                    else if (stage.IsCondenser)
                    {
                        _markers.Add(new ChartMarker
                        {
                            X = composition.MolarComposition,
                            Y = pressure,
                            Label = "C",
                            Description = "Condenser",
                            TooltipData = new Dictionary<string, string>
                        {
                            { "Stage", stage.StageNumber.ToString() },
                            { "Type", "Condenser" },
                            { "Composition", composition.MolarComposition.ToString("F3") },
                            { "Pressure", $"{pressure:F2} {_currentUnit.Symbol}" }
                        }
                        });
                    }
                    else if (stage.IsReboiler)
                    {
                        _markers.Add(new ChartMarker
                        {
                            X = composition.MolarComposition,
                            Y = pressure,
                            Label = "R",
                            Description = "Reboiler",
                            TooltipData = new Dictionary<string, string>
                        {
                            { "Stage", stage.StageNumber.ToString() },
                            { "Type", "Reboiler" },
                            { "Composition", composition.MolarComposition.ToString("F3") },
                            { "Pressure", $"{pressure:F2} {_currentUnit.Symbol}" }
                        }
                        });
                    }
                }
            }

            double pressureRange = maxPressure - minPressure;
            _minPressure = minPressure - (pressureRange * 0.05);
            _maxPressure = maxPressure + (pressureRange * 0.05);

            _series.Add(new ChartSeries
            {
                Name = $"{lightestComponent.Name} Composition",
                SeriesType = ChartSeriesType.Both,
                Color = "#10b981",
                StrokeWidth = 2,
                PointRadius = 1,
                IsVisible = true,
                ShowInLegend = true,
                Points = compositionPoints
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
