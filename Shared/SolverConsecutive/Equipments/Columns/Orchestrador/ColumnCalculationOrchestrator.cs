using Shared.SolverQwen.Stream;
using System.Collections.Immutable;
using System.Data.Common;
using UnitSystem;
using static UnitSystem.Amount;

namespace Shared.SolverConsecutive.Equipments.Columns.Orchestrador
{

    public interface IColumnPostSolverCalculation
    {
        int Order { get; }
        Task CalculateAsync(CancellationToken cancellationToken = default!);
    }

    public interface IColumnCalculationOrchestrator
    {
        bool TopologyChanged { get; }
        bool ColumnPressureChanged { get; }
        bool FUGChanged { get; }
        bool VLEChanged { get; }
        bool PlatesChanged { get; }
        bool McCabeThieleChanged { get; }
        ColumnResult CurrentResult { get; }

        Task<ColumnResult> CalculateAsync(CancellationToken cancellationToken = default);

        void OnPlateSolved(IColumnPlate plate, int nPlate, bool isFeedStage, bool isCondenser, bool isReboiler);
        void SetDistillationParameters(DistillationParameters parameters);
        void SetMcCabeThieleData(McCabeThieleData mcCabeThieleData);
        void SetVLECurveResult(VLECurveResult curveResult);
        void SetStages(ImmutableList<StageResult> stages);
        void NotifyPlatesCalculationComplete();
    }

    public sealed class ColumnCalculationOrchestrator : IColumnCalculationOrchestrator
    {
        private readonly List<IColumnPostSolverCalculation> _calculators;
        private readonly SolverColumn _column;

        // 🔥 CACHÉ DE TODOS LOS DATOS
        private ColumnSnapshot? _lastSnapshot;
        private DistillationParameters? _cachedDistillationParams;
        private VLECurveResult? _cachedVLECurve;
        private ImmutableList<StageResult>? _cachedStages;
        private McCabeThieleData? _cachedMcCabeThiele;

        // Lista temporal para acumular platos durante el cálculo
        private List<StageResult> _stages = new();

        public bool TopologyChanged { get; private set; }
        public bool ColumnPressureChanged { get; private set; }

        // 🔥 NUEVO: Banderas individuales por servicio
        public bool FUGChanged { get; private set; }
        public bool VLEChanged { get; private set; }
        public bool PlatesChanged { get; private set; }
        public bool McCabeThieleChanged { get; private set; }

        // 🔥 RESULTADO ACTUAL (progresivo, se va llenando con los Set*)
        private ColumnResult _currentResult;
        public ColumnResult CurrentResult => _currentResult;

        public ColumnCalculationOrchestrator(SolverColumn column)
        {
            _column = column;
            _currentResult = CreateEmptyColumnResult();

            var calculators = new List<IColumnPostSolverCalculation>
            {
                new FUGCalculationService(_column),
                new VLECurveCalculator(_column),
                new PlateByPlateCalculator(_column),
                new McCabeThieleBuilder(_column)
            };
            _calculators = calculators.OrderBy(x => x.Order).ToList();
        }

        // ═══════════════════════════════════════════════════════════════════
        // 🔥 MÉTODOS SET (llamados por los calculadores para reportar resultados)
        // ═══════════════════════════════════════════════════════════════════

        public void SetDistillationParameters(DistillationParameters parameters)
        {
            _cachedDistillationParams = parameters;
            _currentResult = _currentResult with { DistillationParameters = parameters };
            FUGChanged = true;
            Console.WriteLine($"📥 Orquestador recibió DistillationParameters (FUGChanged=true)");
        }

        public void SetVLECurveResult(VLECurveResult curveResult)
        {
            _cachedVLECurve = curveResult;
            _currentResult = _currentResult with { VLECurve = curveResult };
            VLEChanged = true;
            Console.WriteLine($"📥 Orquestador recibió VLECurveResult (VLEChanged=true)");
        }

        public void SetStages(ImmutableList<StageResult> stages)
        {
            _cachedStages = stages;
            _currentResult = _currentResult with { Stages = stages };
            PlatesChanged = true;
            Console.WriteLine($"📥 Orquestador recibió {stages.Count} Stages (PlatesChanged=true)");
        }

        public void SetMcCabeThieleData(McCabeThieleData mcCabeThieleData)
        {
            _cachedMcCabeThiele = mcCabeThieleData;
            _currentResult = _currentResult with { McCabeThiele = mcCabeThieleData };
            McCabeThieleChanged = true;
            Console.WriteLine($"📥 Orquestador recibió McCabeThieleData (McCabeThieleChanged=true)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 🔥 ACUMULACIÓN DE PLATOS AL VUELO
        // ═══════════════════════════════════════════════════════════════════

        public void OnPlateSolved(IColumnPlate plate, int nPlate, bool isFeedStage, bool isCondenser, bool isReboiler)
        {
            var existingIndex = _stages.FindIndex(s => s.StageNumber == nPlate);

            var liquidResult = CreateStageStreamResult(plate.LiquidOutlet);
            var vaporResult = plate.VaporOutlet?.State == StreamStateType.Calculated
                ? CreateStageStreamResult(plate.VaporOutlet)
                : liquidResult;

            var stageResult = new StageResult
            {
                StageNumber = nPlate,
                IsFeedStage = isFeedStage,
                IsCondenser = isCondenser,
                IsReboiler = isReboiler,
                Liquid = liquidResult,
                Vapor = vaporResult
            };

            if (existingIndex >= 0)
            {
                _stages[existingIndex] = stageResult;
            }
            else
            {
                _stages.Add(stageResult);
            }
        }

        private static StageStreamResult CreateStageStreamResult(IFacadeStream stream)
        {
            var composition = new Dictionary<Guid, StageStreamComponentResult>();

            if (stream.Composition?.Components != null)
            {
                foreach (var comp in stream.Composition.Components)
                {
                    string compName = comp.Name ?? $"Comp_{comp.Id}";
                    composition[comp.Id] = new StageStreamComponentResult()
                    {
                        MolarComposition = comp.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0,
                        ComponentName = compName,
                    };
                }
            }

            return new StageStreamResult
            {
                Temperature = stream.Temperature.Value,
                Pressure = stream.Pressure.Value,
                MassFlow = stream.MassFlow.Value,
                MolarFlow = stream.MolarFlow.Value,
                Enthalpy = stream.MassEnthalpy.Value,
                Density = stream.MassDensity.Value,
                MolarComposition = composition.ToImmutableDictionary()
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // 🔥 CALCULAR TODO
        // ═══════════════════════════════════════════════════════════════════
        public async Task<ColumnResult> CalculateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"🚀 Iniciando cálculo completo de {_column.Name}");

                // 🔥 Detectar cambios ANTES de resetear nada
                var currentSnapshot = CreateSnapshot(_column);
                TopologyChanged = _lastSnapshot == null || !_lastSnapshot.Equals(currentSnapshot);

                // 🔥 Validar que la presión actual sea válida antes de marcar cambio
                bool pressureIsValid = currentSnapshot.TopPressure > 0;
                ColumnPressureChanged = pressureIsValid && (_lastSnapshot == null ||
                    Math.Abs(_lastSnapshot.TopPressure - currentSnapshot.TopPressure) > 1e-6);

                if (!pressureIsValid)
                {
                    Console.WriteLine($"⚠️ Orquestador: Presión inválida ({currentSnapshot.TopPressure}), no se calculará VLE");
                }

                // 🔥 NUEVO: Banderas individuales por servicio
                FUGChanged = false;
                VLEChanged = false;
                PlatesChanged = false;
                McCabeThieleChanged = false;

                // 🔥 Si NADA cambió, retornar el caché directamente
                if (!TopologyChanged && !ColumnPressureChanged && _cachedDistillationParams != null)
                {
                    Console.WriteLine($"✅ Todo en caché, retornando resultado anterior");
                    return _currentResult;
                }

                // 🔥 Solo resetear si algo cambió
                _stages.Clear();

                if (TopologyChanged)
                    Console.WriteLine($"🔄 Topología cambió, recalculando todo");
                else
                    Console.WriteLine($"✅ Topología sin cambios");

                // 🔥 Ejecutar calculadores en orden
                // 🔥 Ejecutar FUG, VLE y Platos en PARALELO (son independientes)
                var independentCalculators = _calculators.Where(c => c.Order <= 3).ToList();
                var parallelTasks = independentCalculators.Select(c => c.CalculateAsync(cancellationToken));

                await Task.WhenAll(parallelTasks);

                // 🔥 Ejecutar McCabeThiele DESPUÉS (depende de los 3 anteriores)
                var mcCabeCalculator = _calculators.FirstOrDefault(c => c.Order == 4);
                if (mcCabeCalculator != null)
                {
                    await mcCabeCalculator.CalculateAsync(cancellationToken);
                }

                // 🔥 Finalizar resultado (preservando datos del caché si no se recalculó)
                _currentResult = _currentResult with
                {
                    ColumnName = _column.Name,
                    FeedStage = _cachedStages?.FirstOrDefault(s => s.IsFeedStage)?.StageNumber ?? 1,
                    Success = true,
                    ErrorMessage = string.Empty
                };

                _lastSnapshot = currentSnapshot;

                Console.WriteLine($"✅ Cálculo completo de {_column.Name} finalizado");

                return _currentResult;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error crítico en orquestador: {ex.Message}");
                _currentResult = new ColumnResult
                {
                    ColumnName = _column.Name,
                    DistillationParameters = CreateEmptyDistillationParameters(),
                    VLECurve = CreateEmptyVLECurve(),
                    Stages = ImmutableList<StageResult>.Empty,
                    McCabeThiele = CreateEmptyMcCabeThieleData(),
                    FeedStage = 1,
                    Success = false,
                    ErrorMessage = ex.Message
                };
                return _currentResult;
            }
        }
        public async Task<ColumnResult> CalculateAsync2(CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"🚀 Iniciando cálculo completo de {_column.Name}");

                // 🔥 Resetear resultado actual
                _currentResult = CreateEmptyColumnResult();
                _stages.Clear();

                // 🔥 Detectar cambios
                var currentSnapshot = CreateSnapshot(_column);
                TopologyChanged = _lastSnapshot == null || !_lastSnapshot.Equals(currentSnapshot);
                ColumnPressureChanged = _lastSnapshot == null ||
                    Math.Abs(_lastSnapshot.TopPressure - currentSnapshot.TopPressure) > 1e-6;

                if (TopologyChanged)
                    Console.WriteLine($"🔄 Topología cambió, recalculando todo");
                else
                    Console.WriteLine($"✅ Topología sin cambios, usando caché");

                // 🔥 Ejecutar calculadores en orden
                foreach (var calculator in _calculators)
                {
                    await calculator.CalculateAsync(cancellationToken);
                }

                // 🔥 Finalizar resultado
                _currentResult = _currentResult with
                {
                    ColumnName = _column.Name,
                    FeedStage = _cachedStages?.FirstOrDefault(s => s.IsFeedStage)?.StageNumber ?? 1,
                    Success = true,
                    ErrorMessage = string.Empty
                };

                _lastSnapshot = currentSnapshot;

                Console.WriteLine($"✅ Cálculo completo de {_column.Name} finalizado");

                return _currentResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error crítico en orquestador: {ex.Message}");
                _currentResult = new ColumnResult
                {
                    ColumnName = _column.Name,
                    DistillationParameters = CreateEmptyDistillationParameters(),
                    VLECurve = CreateEmptyVLECurve(),
                    Stages = ImmutableList<StageResult>.Empty,
                    McCabeThiele = CreateEmptyMcCabeThieleData(),
                    FeedStage = 1,
                    Success = false,
                    ErrorMessage = ex.Message
                };
                return _currentResult;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 🔥 SNAPSHOT Y HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private static ColumnSnapshot CreateSnapshot(SolverColumn column)
        {
            // 🔥 Validar si la presión está definida (no es Undefined)
            bool isPressureDefined = column.TopPressure.IsDefined;
            double pressureValue = isPressureDefined ? column.TopPressure.GetSolverValue() : 0;

            return new ColumnSnapshot
            {
                TopPressure = pressureValue,
                DeltaP = column.DeltaP.GetSolverValue(),
                Feeds = column.Feeds.Select(CreateStreamSnapshot).ToList(),
                RefluxInlet = column.RefluxInlet != null ? CreateStreamSnapshot(column.RefluxInlet) : null,
                VaporOutlet = column.VaporOutlet != null ? CreateStreamSnapshot(column.VaporOutlet) : null,
                BottomOutlet = column.BottomOutlet != null ? CreateStreamSnapshot(column.BottomOutlet) : null,
                SideDraws = column.SideDraws.Select(CreateStreamSnapshot).ToList(),
            };
        }

        private static StreamSnapshot CreateStreamSnapshot(IFacadeStream stream)
        {
            var composition = new Dictionary<string, double>();

            if (stream.Composition?.Components != null)
            {
                foreach (var comp in stream.Composition.Components)
                {
                    composition[comp.Name] = comp.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
                }
            }

            return new StreamSnapshot
            {
                Temperature = stream.Temperature.GetSolverValue(),
                Pressure = stream.Pressure.GetSolverValue(),
                MassFlow = stream.MassFlow.GetSolverValue(),
                VaporFraction = stream.VaporFraction.GetSolverValue(),
                Composition = composition
            };
        }

        private ColumnResult CreateEmptyColumnResult()
        {
            return new ColumnResult
            {
                ColumnName = _column.Name,
                DistillationParameters = CreateEmptyDistillationParameters(),
                VLECurve = CreateEmptyVLECurve(),
                Stages = ImmutableList<StageResult>.Empty,
                McCabeThiele = CreateEmptyMcCabeThieleData(),
                FeedStage = 1,
                Success = false,
                ErrorMessage = string.Empty
            };
        }

        private static DistillationParameters CreateEmptyDistillationParameters()
        {
            return new DistillationParameters
            {
                RefluxRatio = new UnitLess(0),
                MinRefluxRatio = new UnitLess(0),
                RefluxExcess = new Percentage(0, PercentageUnits.Percentage),
                MinStages = new UnitLess(0),
                TheoreticalStages = new UnitLess(0),
                xD = 0,
                xB = 0,
                FeedQuality = 0,
                RelativeVolatilities = ImmutableList<double>.Empty,
                LightKeyIndex = 0,
                HeavyKeyIndex = 1
            };
        }

        private static VLECurveResult CreateEmptyVLECurve()
        {
            return new VLECurveResult
            {
                Points = ImmutableList<VLEPointResult>.Empty,
                Pressure = new Pressure(0, PressureUnits.Bara)
            };
        }

        private static McCabeThieleData CreateEmptyMcCabeThieleData()
        {
            return new McCabeThieleData
            {
                DiagonalLine = new List<(double x, double y)>(),
                VLECurve = new List<(double x, double y)>(),
                RectifyingLine = new List<(double x, double y)>(),
                StrippingLine = new List<(double x, double y)>(),
                //FeedLine = new List<(double x, double y)>(),
                StaircaseSteps = new List<StaircaseStep>(),
                Markers = new List<MarkerPoint>(),
                MinRefluxRectifyingLine = new List<(double x, double y)>(),
                MinRefluxStrippingLine = new List<(double x, double y)>(),
                MinRefluxRatio = 0,
                ProjectionLinesB = new(),
                ProjectionLinesD = new(),
                ProjectionLinesF = new(),

            };
        }
        public void NotifyPlatesCalculationComplete()
        {
            var stages = _stages.ToImmutableList();
            SetStages(stages);
            Console.WriteLine($"📥 Orquestador notificó que los platos están listos: {stages.Count} etapas");
        }
    }


  
}