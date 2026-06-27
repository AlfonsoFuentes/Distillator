using Client.Templates.Graphs;
using Client.Templates.Graphs.ChartBases;
using Microsoft.AspNetCore.Components;
using Shared.SolverConsecutive.Equipments.Columns;
using UnitSystem;

namespace Client.Pages.UnitOperations.Columns.ColumnProfiles.ColumnPlates
{
    public partial class EnthalpyProfileChart
    {
        [Parameter] public SolverColumn? Column { get; set; }
        [Parameter] public bool IsLoading { get; set; } = false;

        private List<ChartSeries> _series = new();
        private List<ChartMarker> _markers = new();
        private ChartViewState _viewState = new();
        private double _maxStage = 1;
        private double _minEnthalpy = 0;
        private double _maxEnthalpy = 100;
        private string _chartTitle = "Enthalpy Profile";
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
            _chartTitle = "Enthalpy Profile";
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
            _maxStage = stages.Max(s => s.StageNumber);

            // ==========================================
            // 1. ENTALPÍA DEL LÍQUIDO
            // ==========================================
            var liquidEnthalpyPoints = new List<ChartPoint>();
            double minEnthalpy = double.MaxValue;
            double maxEnthalpy = double.MinValue;

            foreach (var stage in stages.OrderBy(s => s.StageNumber))
            {
                double enthalpy = stage.Liquid.Enthalpy.GetValue(_currentUnit);
                minEnthalpy = Math.Min(minEnthalpy, enthalpy);
                maxEnthalpy = Math.Max(maxEnthalpy, enthalpy);

                liquidEnthalpyPoints.Add(new ChartPoint
                {
                    X = stage.StageNumber,
                    Y = enthalpy,
                    TooltipData = new Dictionary<string, string>
                {
                    { "Stage", stage.StageNumber.ToString() },
                    { "Type", "Liquid" },
                    { "Enthalpy", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                }
                });

                // Marcar platos especiales
                if (stage.IsFeedStage)
                {
                    _markers.Add(new ChartMarker
                    {
                        X = stage.StageNumber,
                        Y = enthalpy,
                        Label = "F",
                        Description = "Feed Stage",
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Feed Stage" },
                        { "Enthalpy (L)", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                    }
                    });
                }
                else if (stage.IsCondenser)
                {
                    _markers.Add(new ChartMarker
                    {
                        X = stage.StageNumber,
                        Y = enthalpy,
                        Label = "C",
                        Description = "Condenser",
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Condenser" },
                        { "Enthalpy (L)", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                    }
                    });
                }
                else if (stage.IsReboiler)
                {
                    _markers.Add(new ChartMarker
                    {
                        X = stage.StageNumber,
                        Y = enthalpy,
                        Label = "R",
                        Description = "Reboiler",
                        TooltipData = new Dictionary<string, string>
                    {
                        { "Stage", stage.StageNumber.ToString() },
                        { "Type", "Reboiler" },
                        { "Enthalpy (L)", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                    }
                    });
                }
            }

            // ==========================================
            // 2. ENTALPÍA DEL VAPOR
            // ==========================================
            var vaporEnthalpyPoints = new List<ChartPoint>();
            double maxVaporEnthalpy = double.MinValue;

            foreach (var stage in stages.OrderBy(s => s.StageNumber))
            {
                double enthalpy = stage.Vapor.Enthalpy.GetValue(_currentUnit);
                maxVaporEnthalpy = Math.Max(maxVaporEnthalpy, enthalpy);

                vaporEnthalpyPoints.Add(new ChartPoint
                {
                    X = stage.StageNumber,
                    Y = enthalpy,
                    TooltipData = new Dictionary<string, string>
                {
                    { "Stage", stage.StageNumber.ToString() },
                    { "Type", "Vapor" },
                    { "Enthalpy", $"{enthalpy:F2} {_currentUnit.Symbol}" }
                }
                });
            }

            // Configurar escala
            double enthalpyRange = maxVaporEnthalpy - minEnthalpy;
            _minEnthalpy = minEnthalpy - (enthalpyRange * 0.05);
            _maxEnthalpy = maxVaporEnthalpy + (enthalpyRange * 0.05);

            _series.Add(new ChartSeries
            {
                Name = "Liquid Enthalpy",
                SeriesType = ChartSeriesType.Both,
                Color = "#3b82f6",
                StrokeWidth = 2,
                PointRadius =1,
                IsVisible = true,
                ShowInLegend = true,
                Points = liquidEnthalpyPoints
            });

            _series.Add(new ChartSeries
            {
                Name = "Vapor Enthalpy",
                SeriesType = ChartSeriesType.Both,
                Color = "#f59e0b",
                StrokeWidth = 2,
                PointRadius = 1,
                IsVisible = true,
                ShowInLegend = true,
                Points = vaporEnthalpyPoints
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
