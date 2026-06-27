using Client.Templates.Graphs;
using Client.Templates.Graphs.ChartBases;
using Microsoft.AspNetCore.Components;
using Shared.SolverConsecutive.Equipments.Columns;
using Shared.SolverConsecutive.Equipments.Columns.Orchestrador;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Client.Pages.UnitOperations.Columns.ColumnProfiles.ColumnComposition
{

    public partial class EnthalpyCompositionChart
    {
        [Parameter] public SolverColumn? Column { get; set; }
        [Parameter] public bool IsLoading { get; set; } = false;

        private List<ChartSeries> _series = new();
        private List<ChartMarker> _markers = new();
        private ChartViewState _viewState = new();
        private double _minEnthalpy = 0;
        private double _maxEnthalpy = 1000;
        private string _chartTitle = "Enthalpy-Composition Profile";
        private string _chartSubtitle = string.Empty;
        private UnitMeasure _currentUnit = MassEnergyUnits.Kcal_Kg;
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
            _chartTitle = "Enthalpy-Composition Profile";
            _chartSubtitle = string.Empty;

            if (Column?.CalculationResult == null || !Column.CalculationResult.Success)
                return;

            var stages = Column.CalculationResult.Stages;
            if (stages == null || !stages.Any()) return;
            if (!_Units.Any())
            {
                var sampleTemp = stages.First().Liquid.Enthalpy;
                _Units = sampleTemp.UnitsList;
            }
            _yAxisTitle = $"Mass Enthalpy ({_currentUnit.Symbol})";
            var feed = Column.Feeds.FirstOrDefault();
            if (feed == null) return;

            var lightestComponent = feed.Composition.Components
                .OrderBy(c => c.DataBase.FullData.BoilingPoint?.GetValue(TemperatureUnits.Kelvin) ?? 0)
                .FirstOrDefault();

            if (lightestComponent == null) return;

            var componentId = lightestComponent.Id;

            // ==========================================
            // 1. ENTALPÍA DEL LÍQUIDO
            // ==========================================
            var liquidPoints = new List<ChartPoint>();
            double minEnthalpy = double.MaxValue;
            double maxEnthalpy = double.MinValue;

            foreach (var stage in stages.OrderBy(s => s.StageNumber))
            {
                if (stage.Liquid.MolarComposition.TryGetValue(componentId, out StageStreamComponentResult? composition))
                {
                    double enthalpy = stage.Liquid.Enthalpy.GetValue(_currentUnit);
                    minEnthalpy = Math.Min(minEnthalpy, enthalpy);
                    maxEnthalpy = Math.Max(maxEnthalpy, enthalpy);

                    liquidPoints.Add(new ChartPoint
                    {
                        X = composition.MolarComposition,
                        Y = enthalpy,
                        TooltipData = new Dictionary<string, string>
                {
                    { "Phase", "Liquid" },
                    { "Component", composition.ComponentName },
                    { "Stage", stage.StageNumber.ToString() },
                    { "Composition", composition.MolarComposition.ToString("F3") },
                    { "Enthalpy", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                }
                    });

                    // Marcar platos especiales (solo en serie del líquido para no duplicar)
                    if (stage.IsFeedStage)
                    {
                        _markers.Add(new ChartMarker
                        {
                            X = composition.MolarComposition,
                            Y = enthalpy,
                            Label = "F",
                            Description = "Feed Stage",
                            TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Feed Stage" },
                        { "Composition", composition.MolarComposition.ToString("F3") },
                        { "Enthalpy (L)", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                    }
                        });
                    }
                    else if (stage.IsCondenser)
                    {
                        _markers.Add(new ChartMarker
                        {
                            X = composition.MolarComposition,
                            Y = enthalpy,
                            Label = "C",
                            Description = "Condenser",
                            TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Condenser" },
                        { "Composition", composition.MolarComposition.ToString("F3") },
                        { "Enthalpy (L)", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                    }
                        });
                    }
                    else if (stage.IsReboiler)
                    {
                        _markers.Add(new ChartMarker
                        {
                            X = composition.MolarComposition,
                            Y = enthalpy,
                            Label = "R",
                            Description = "Reboiler",
                            TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Reboiler" },
                        { "Composition", composition.MolarComposition.ToString("F3") },
                        { "Enthalpy (L)", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                    }
                        });
                    }
                }
            }

            // ==========================================
            // 2. ENTALPÍA DEL VAPOR (NUEVO)
            // ==========================================
            var vaporPoints = new List<ChartPoint>();
            double maxVaporEnthalpy = double.MinValue;

            foreach (var stage in stages.OrderBy(s => s.StageNumber))
            {
                if (stage.Vapor.MolarComposition.TryGetValue(componentId, out StageStreamComponentResult? composition))
                {
                    double enthalpy = stage.Vapor.Enthalpy.GetValue(_currentUnit);
                    maxVaporEnthalpy = Math.Max(maxVaporEnthalpy, enthalpy);

                    vaporPoints.Add(new ChartPoint
                    {
                        X = composition.MolarComposition,
                        Y = enthalpy,
                        TooltipData = new Dictionary<string, string>
                {
                    { "Phase", "Vapor" },
                    { "Component", composition.ComponentName },
                    { "Stage", stage.StageNumber.ToString() },
                    { "Composition", composition.MolarComposition.ToString("F3") },
                    { "Enthalpy", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                }
                    });
                }
            }

            // ==========================================
            // 3. CONFIGURAR ESCALA DEL EJE Y
            // ==========================================
            // Considerar el máximo de ambas series (vapor será mayor)
            double overallMax = Math.Max(maxEnthalpy, maxVaporEnthalpy);
            double enthalpyRange = overallMax - minEnthalpy;
            _minEnthalpy = minEnthalpy - (enthalpyRange * 0.05);
            _maxEnthalpy = overallMax + (enthalpyRange * 0.05);

            // ==========================================
            // 4. AGREGAR SERIES AL GRÁFICO
            // ==========================================
            _series.Add(new ChartSeries
            {
                Name = $"Liquid - {lightestComponent.Name}",
                SeriesType = ChartSeriesType.Both,
                Color = "#3b82f6",  // Azul
                StrokeWidth = 2,
                PointRadius = 1,
                IsVisible = true,
                ShowInLegend = true,
                Points = liquidPoints
            });

            _series.Add(new ChartSeries
            {
                Name = $"Vapor - {lightestComponent.Name}",
                SeriesType = ChartSeriesType.Both,
                Color = "#f59e0b",  // Naranja
                StrokeWidth = 2,
                PointRadius = 1,
                IsVisible = true,
                ShowInLegend = true,
                Points = vaporPoints
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