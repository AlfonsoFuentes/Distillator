using Client.Templates.Graphs;
using Client.Templates.Graphs.ChartBases;
using Microsoft.AspNetCore.Components;
using Shared.SolverConsecutive.Equipments.Columns;
using Shared.SolverConsecutive.Equipments.Columns.Orchestrador;
using UnitSystem;

namespace Client.Pages.UnitOperations.Columns.ColumnProfiles.ColumnComposition
{
    public partial class TemperatureCompositionChart
    {
        [Parameter] public SolverColumn? Column { get; set; }
        [Parameter] public bool IsLoading { get; set; } = false;

        private List<ChartSeries> _series = new();
        private List<ChartMarker> _markers = new();
        private ChartViewState _viewState = new();
        private double _minTemp = 0;
        private double _maxTemp = 400;
        private string _chartTitle = "Temperature-Composition Profile";
        private string _chartSubtitle = string.Empty;
        private UnitMeasure _currentUnit = TemperatureUnits.Kelvin;
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
            _chartTitle = "Temperature-Composition Profile";
            _chartSubtitle = string.Empty;

            if (Column?.CalculationResult == null || !Column.CalculationResult.Success)
                return;

            var stages = Column.CalculationResult.Stages;
            if (stages == null || !stages.Any()) return;
            if (!_Units.Any())
            {
                var sampleTemp = stages.First().Liquid.Temperature;
                _Units = sampleTemp.UnitsList;
            }
            _yAxisTitle = $"Temperature ({_currentUnit.Symbol})";
            var feed = Column.Feeds.FirstOrDefault();
            if (feed == null) return;

            var lightestComponent = feed.Composition.Components
                .OrderBy(c => c.DataBase.FullData.BoilingPoint?.GetValue(TemperatureUnits.Kelvin) ?? 0)
                .FirstOrDefault();

            if (lightestComponent == null) return;

            var componentId = lightestComponent.Id;

            // ==========================================
            // PERFIL DE COMPOSICIÓN vs TEMPERATURA
            // Eje X = Composición, Eje Y = Temperatura
            // ==========================================
            var compositionPoints = new List<ChartPoint>();
            double minTemp = double.MaxValue;
            double maxTemp = double.MinValue;

            foreach (var stage in stages.OrderBy(s => s.StageNumber))
            {
                if (stage.Liquid.MolarComposition.TryGetValue(componentId, out StageStreamComponentResult? composition))
                {
                    double temp = stage.Liquid.Temperature.GetValue(_currentUnit);
                    minTemp = Math.Min(minTemp, temp);
                    maxTemp = Math.Max(maxTemp, temp);

                    compositionPoints.Add(new ChartPoint
                    {
                        X = composition.MolarComposition,  // Eje X = Composición
                        Y = temp,         // Eje Y = Temperatura
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Component", composition.ComponentName },
                        { "Stage", stage.StageNumber.ToString() },
                        { "Composition", composition.MolarComposition.ToString("F3") },
                        { "Temperature", $"{temp:F2} {_currentUnit.Symbol}" }
                    }
                    });

                    // Marcar platos especiales
                    if (stage.IsFeedStage)
                    {
                        _markers.Add(new ChartMarker
                        {
                            X = composition.MolarComposition,
                            Y = temp,
                            Label = "F",
                            Description = "Feed Stage",
                            TooltipData = new Dictionary<string, string>
                        {
                            { "Stage", stage.StageNumber.ToString() },
                            { "Type", "Feed Stage" },
                            { "Composition", composition.MolarComposition.ToString("F3") },
                            { "Temperature", $"{temp:F2} {_currentUnit.Symbol}" }
                        }
                        });
                    }
                    else if (stage.IsCondenser)
                    {
                        _markers.Add(new ChartMarker
                        {
                            X = composition.MolarComposition,
                            Y = temp,
                            Label = "C",
                            Description = "Condenser",
                            TooltipData = new Dictionary<string, string>
                        {
                            { "Stage", stage.StageNumber.ToString() },
                            { "Type", "Condenser" },
                            { "Composition", composition.MolarComposition.ToString("F3") },
                            { "Temperature", $"{temp:F2} {_currentUnit.Symbol}" }
                        }
                        });
                    }
                    else if (stage.IsReboiler)
                    {
                        _markers.Add(new ChartMarker
                        {
                            X = composition.MolarComposition,
                            Y = temp,
                            Label = "R",
                            Description = "Reboiler",
                            TooltipData = new Dictionary<string, string>
                        {
                            { "Stage", stage.StageNumber.ToString() },
                            { "Type", "Reboiler" },
                            { "Composition", composition.MolarComposition.ToString("F3") },
                            { "Temperature", $"{temp:F2} {_currentUnit.Symbol}" }
                        }
                        });
                    }
                }
            }

            _minTemp = minTemp - 5;
            _maxTemp = maxTemp + 5;

            _series.Add(new ChartSeries
            {
                Name = $"{lightestComponent.Name} Composition",
                SeriesType = ChartSeriesType.Both,
                Color = "#3b82f6",
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
