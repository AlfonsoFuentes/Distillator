using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.SolverConsecutive.Equipments.Columns
{
    // StoppingLevel.cs
    public enum StopLevel
    {
        Continue,      // Seguir calculando
        Success,       // Convergencia exitosa
        Warning,       // Advertencia (monitorear)
        HardStop       // Parada inmediata
    }

    // StoppingResult.cs
    public class StoppingResult
    {
        public bool ShouldStop { get; set; }
        public StopLevel Level { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();

        public static StoppingResult Continue() =>
            new() { ShouldStop = false, Level = StopLevel.Continue };

        public static StoppingResult Success(string reason) =>
            new() { ShouldStop = true, Level = StopLevel.Success, Reason = reason };

        public static StoppingResult Warning(string reason) =>
            new() { ShouldStop = false, Level = StopLevel.Warning, Reason = reason };

        public static StoppingResult HardStop(string reason) =>
            new() { ShouldStop = true, Level = StopLevel.HardStop, Reason = reason };
    }

    // PlateContext.cs - Datos del plato actual para evaluación
    public class PlateContext
    {
        public int PlateNumber { get; set; }
        public double CurrentComposition { get; set; }
        public double PreviousComposition { get; set; }
        public double TargetComposition { get; set; }
        public double RelativeVolatility { get; set; }
        public bool IsValidComposition { get; set; }
        public bool IsThermodynamicallyValid { get; set; }
        public List<double> CompositionHistory { get; set; } = new();
    }

    // IColumnStoppingCriterion.cs
    public interface IColumnStoppingCriterion
    {
        string Name { get; }
        StopLevel Priority { get; }
        StoppingResult Evaluate(PlateContext context);
    }// E1: TargetConcentrationCriterion.cs
    public class TargetConcentrationCriterion : IColumnStoppingCriterion
    {
        public string Name => "Target Concentration";
        public StopLevel Priority => StopLevel.Success;

        private readonly double _tolerance;

        public TargetConcentrationCriterion(double tolerance = 0.0001)
        {
            _tolerance = tolerance; // 1% de tolerancia absoluta
        }

        public StoppingResult Evaluate(PlateContext context)
        {
            double difference = Math.Abs(context.CurrentComposition - context.TargetComposition);

            if (difference < _tolerance)
            {
                return StoppingResult.Success(
                    $"Convergencia alcanzada: |{context.CurrentComposition:P2} - {context.TargetComposition:P2}| = {difference:P2} < {_tolerance:P2}");
            }

            return StoppingResult.Continue();
        }
    }

    // E2: CompositionInversionCriterion.cs
    public class CompositionInversionCriterion : IColumnStoppingCriterion
    {
        public string Name => "Composition Inversion";
        public StopLevel Priority => StopLevel.HardStop;

        private readonly double _margin;

        public CompositionInversionCriterion(double margin = 0.001)
        {
            _margin = margin; // 1% de margen para detectar inversión
        }

        public StoppingResult Evaluate(PlateContext context)
        {
            // En columna normal, la composición del componente pesado debe AUMENTAR hacia abajo
            // Si disminuye significativamente, hay inversión (posible azeótropo o error)
            if (context.PreviousComposition > 0 &&
                context.CurrentComposition < context.PreviousComposition - _margin)
            {
                return StoppingResult.HardStop(
                    $"Inversión de composición detectada: {context.PreviousComposition:P2} → {context.CurrentComposition:P2} (margen: {_margin:P2})");
            }

            return StoppingResult.Continue();
        }
    }

    // E3: InvalidCompositionCriterion.cs
    public class InvalidCompositionCriterion : IColumnStoppingCriterion
    {
        public string Name => "Invalid Composition";
        public StopLevel Priority => StopLevel.HardStop;

        public StoppingResult Evaluate(PlateContext context)
        {
            if (!context.IsValidComposition)
            {
                return StoppingResult.HardStop(
                    $"Composición inválida: {context.CurrentComposition:P2} (fuera de rango 0-1)");
            }

            return StoppingResult.Continue();
        }
    }

    // E4: ThermodynamicErrorCriterion.cs
    public class ThermodynamicErrorCriterion : IColumnStoppingCriterion
    {
        public string Name => "Thermodynamic Error";
        public StopLevel Priority => StopLevel.HardStop;

        public StoppingResult Evaluate(PlateContext context)
        {
            if (!context.IsThermodynamicallyValid)
            {
                return StoppingResult.HardStop("Error termodinámico: fase no calculada correctamente");
            }

            return StoppingResult.Continue();
        }
    }

    // E5: PinchPointCriterion.cs (con StateMachine interna)
    public class PinchPointCriterion : IColumnStoppingCriterion
    {
        public string Name => "Pinch Point Detection";
        public StopLevel Priority => StopLevel.HardStop;

        private enum DetectorState { Normal, Warning, Confirmed }

        private DetectorState _state;
        private int _warningCount;
        private int _confirmedCount;

        private const double WARNING_THRESHOLD = 0.001;      // 3er decimal
        private const double CONFIRMED_THRESHOLD = 1e-5;     // 5to decimal
        private const int WARNING_PLATES_REQUIRED = 3;
        private const int CONFIRMED_PLATES_REQUIRED = 5;

        public PinchPointCriterion()
        {
            // Inicializar estado en el constructor para mayor claridad
            _state = DetectorState.Normal;
            _warningCount = 0;
            _confirmedCount = 0;
        }

        public StoppingResult Evaluate(PlateContext context)
        {
            double deltaX = Math.Abs(context.CurrentComposition - context.PreviousComposition);

            switch (_state)
            {
                case DetectorState.Normal:
                    if (deltaX < WARNING_THRESHOLD)
                    {
                        _warningCount++;
                        if (_warningCount >= WARNING_PLATES_REQUIRED)
                        {
                            _state = DetectorState.Warning;
                            return StoppingResult.Warning(
                                $"Pinch Point sospechoso: Δx = {deltaX:E2} < {WARNING_THRESHOLD:E2} por {WARNING_PLATES_REQUIRED} platos. Activando monitoreo estricto.");
                        }
                    }
                    else
                    {
                        _warningCount = 0; // Reset si el cambio vuelve a ser significativo
                    }
                    break;

                case DetectorState.Warning:
                    if (deltaX < CONFIRMED_THRESHOLD)
                    {
                        _confirmedCount++;
                        if (_confirmedCount >= CONFIRMED_PLATES_REQUIRED)
                        {
                            _state = DetectorState.Confirmed;
                            return StoppingResult.HardStop(
                                $"Pinch Point confirmado: Δx = {deltaX:E2} < {CONFIRMED_THRESHOLD:E2} por {CONFIRMED_PLATES_REQUIRED} platos consecutivos. Separación imposible.");
                        }
                    }
                    else if (deltaX >= WARNING_THRESHOLD)
                    {
                        // Falso positivo: el cambio volvió a ser significativo, recuperar estado normal
                        _state = DetectorState.Normal;
                        _warningCount = 0;
                        _confirmedCount = 0;
                    }
                    // Si deltaX está entre CONFIRMED_THRESHOLD y WARNING_THRESHOLD,
                    // simplemente mantenemos el estado Warning sin incrementar ni resetear
                    break;
            }

            return StoppingResult.Continue();
        }
    }

    // W2: RelativeVolatilityCriterion.cs
    public class RelativeVolatilityCriterion : IColumnStoppingCriterion
    {
        public string Name => "Relative Volatility";
        public StopLevel Priority => StopLevel.Warning;

        private readonly double _minAlpha;

        public RelativeVolatilityCriterion(double minAlpha = 1.05)
        {
            _minAlpha = minAlpha; // α < 1.05 indica separación muy difícil (cerca del azeótropo)
        }

        public StoppingResult Evaluate(PlateContext context)
        {
            if (context.RelativeVolatility > 0 && context.RelativeVolatility < _minAlpha)
            {
                return StoppingResult.Warning(
                    $"Volatilidad relativa crítica: α = {context.RelativeVolatility:F3} < {_minAlpha:F2}. Separación muy difícil.");
            }

            return StoppingResult.Continue();
        }
    }

    // W3: OscillationCriterion.cs
    public class OscillationCriterion : IColumnStoppingCriterion
    {
        public string Name => "Composition Oscillation";
        public StopLevel Priority => StopLevel.Warning;

        private readonly int _oscillationPlates;
        private readonly double _minMagnitude;

        public OscillationCriterion(int oscillationPlates = 3, double minMagnitude = 0.001)
        {
            _oscillationPlates = oscillationPlates; // Número de platos para detectar oscilación
            _minMagnitude = minMagnitude; // Magnitud mínima para considerar oscilación real
        }

        public StoppingResult Evaluate(PlateContext context)
        {
            if (context.CompositionHistory.Count < _oscillationPlates + 1)
                return StoppingResult.Continue();

            // Detectar si alterna signo de cambio con magnitud significativa
            bool isOscillating = true;
            double maxMagnitude = 0;

            for (int i = context.CompositionHistory.Count - _oscillationPlates;
                 i < context.CompositionHistory.Count - 1; i++)
            {
                double change1 = context.CompositionHistory[i] - context.CompositionHistory[i - 1];
                double change2 = context.CompositionHistory[i + 1] - context.CompositionHistory[i];

                // Rastrear la magnitud máxima de los cambios
                maxMagnitude = Math.Max(maxMagnitude, Math.Max(Math.Abs(change1), Math.Abs(change2)));

                // Si tienen el mismo signo, no hay oscilación
                if (change1 * change2 >= 0)
                {
                    isOscillating = false;
                    break;
                }
            }

            // Solo reportar oscilación si la magnitud es significativa
            if (isOscillating && maxMagnitude >= _minMagnitude)
            {
                return StoppingResult.Warning(
                    $"Oscilación de composición detectada en los últimos {_oscillationPlates} platos " +
                    $"(magnitud máx: {maxMagnitude:E2}). Posible inestabilidad numérica.");
            }

            return StoppingResult.Continue();
        }
    }

    // R1: MaxUsefulPlatesCriterion.cs (solo referencia)
    public class MaxUsefulPlatesCriterion : IColumnStoppingCriterion
    {
        public string Name => "Max Useful Plates Reference";
        public StopLevel Priority => StopLevel.Continue; // Nunca para, solo log

        private readonly double _minStages;
        private readonly double _factor;
        private bool _warned = false;

        public MaxUsefulPlatesCriterion(double minStages, double factor = 3.0)
        {
            _minStages = minStages;
            _factor = factor;
        }

        public StoppingResult Evaluate(PlateContext context)
        {
            double maxReference = _minStages * _factor;

            if (!_warned && context.PlateNumber > maxReference)
            {
                _warned = true;
                return StoppingResult.Warning(
                    $"Referencia: Superados {maxReference:F0} platos (N_min × {_factor:F1}). Verificar especificaciones.");
            }

            return StoppingResult.Continue();
        }
    }// StoppingCriteriaEvaluator.cs
    public class StoppingCriteriaEvaluator
    {
        private readonly List<IColumnStoppingCriterion> _criteria;

        public StoppingCriteriaEvaluator(IEnumerable<IColumnStoppingCriterion> criteria)
        {
            // Ordenar por prioridad: HardStop > Success > Warning > Continue
            _criteria = criteria
                .OrderByDescending(c => c.Priority)
                .ToList();
        }

        public StoppingResult EvaluateAll(PlateContext context)
        {
            StoppingResult finalResult = StoppingResult.Continue();

            foreach (var criterion in _criteria)
            {
                var result = criterion.Evaluate(context);

                // Si encontramos un HardStop o Success, retornar inmediatamente
                if (result.Level == StopLevel.HardStop || result.Level == StopLevel.Success)
                {
                    return result;
                }

                // Acumular warnings
                if (result.Level == StopLevel.Warning && finalResult.Level != StopLevel.Warning)
                {
                    finalResult = result;
                }
            }

            return finalResult;
        }
    }
}
