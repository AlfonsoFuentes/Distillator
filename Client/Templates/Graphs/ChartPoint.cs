using System.Globalization;

namespace Client.Templates.Graphs
{
    public enum ChartSeriesType
    {
        Line,        // Solo línea continua (ej: curva VLE)
        Points,      // Solo puntos discretos (ej: platos individuales)
        Both,        // Línea + puntos
        StepLine     // Línea escalonada (para perfiles por plato)
    }
    public class ChartMarker
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> TooltipData { get; set; } = new();
    }
    public class ChartPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        // Metadata para el tooltip (ej: "Stage: 5", "Temp: 85°C")
        public Dictionary<string, string> TooltipData { get; set; } = new();
    }
    public class ChartSeries
    {
        public string Name { get; set; } = string.Empty;
        public List<ChartPoint> Points { get; set; } = new();
        public ChartSeriesType SeriesType { get; set; } = ChartSeriesType.Line;
        public string Color { get; set; } = "#90CAF9"; // Default pale blue
        public double StrokeWidth { get; set; } = 1.2;  // ✅ Más delgado
        public string StrokeDashArray { get; set; } = "";
        public double PointRadius { get; set; } = 1.0;  // También más pequeño
        public bool ShowPoints { get; set; } = false;

        // Metadata adicional (ej: si es una línea de operación, de equilibrio, etc.)
        public Dictionary<string, object> Metadata { get; set; } = new();
        public bool IsVisible { get; set; } = true;           // Para ocultar/mostrar (doble click)
        public bool IsHighlighted { get; set; } = false;      // Para resaltar (click simple)
        public bool ShowInLegend { get; set; } = true;  // Controla si aparece en el panel de leyendas
    }
    public class ChartViewState
    {
        public double BaseWidth { get; set; } = 800;
        public double BaseHeight { get; set; } = 600;

        // 🔥 NUEVO: Padding individual por lado
        public double PaddingLeft { get; set; } = 60;    // Espacio para etiquetas Y
        public double PaddingRight { get; set; } = 20;   // Mínimo espacio
        public double PaddingTop { get; set; } = 15;     // Mínimo espacio
        public double PaddingBottom { get; set; } = 60;  // Espacio para etiquetas X

        // 🔥 Obsoleto pero mantenido para compatibilidad
        

        public double ZoomLevel { get; set; } = 1.0;
        public double PanX { get; set; } = 0;
        public double PanY { get; set; } = 0;

        // 🔥 NUEVO: Cálculos con padding asimétrico
        public double InnerWidth => BaseWidth - PaddingLeft - PaddingRight;
        public double InnerHeight => BaseHeight - PaddingTop - PaddingBottom;

        public string ViewBox
        {
            get
            {
                double width = BaseWidth / ZoomLevel;
                double height = BaseHeight / ZoomLevel;
                return $"{PanX.ToString("0.##", CultureInfo.InvariantCulture)} {PanY.ToString("0.##", CultureInfo.InvariantCulture)} {width.ToString("0.##", CultureInfo.InvariantCulture)} {height.ToString("0.##", CultureInfo.InvariantCulture)}";
            }
        }

        public void Reset()
        {
            ZoomLevel = 1.0;
            PanX = 0;
            PanY = 0;
        }
    }
    public class ChartTooltip
    {
        public double ScreenX { get; set; }
        public double ScreenY { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<TooltipRow> Rows { get; set; } = new();
        public bool IsVisible { get; set; } = false;
        public string PositionClass { get; set; } = "tooltip-right";  // 🔥 NUEVO
    }

    public class TooltipRow
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty; // Indicador visual
    }
    public static class ChartColorPalette
    {
        // Paleta pálida estilo Industrial Minimalista
        public static readonly string[] Palette = new[]
        {
            "#90CAF9", // Pale Blue
            "#F48FB1", // Pale Pink
            "#A5D6A7", // Pale Green
            "#FFCC80", // Pale Orange
            "#CE93D8", // Pale Purple
            "#80DEEA", // Pale Cyan
            "#FFF59D", // Pale Yellow
            "#BCAAA4", // Pale Brown
            "#B0BEC5", // Pale Blue Grey
            "#E6EE9C"  // Pale Lime
        };

        public static string GetColor(int index)
        {
            if (index < 0) return Palette[0];
            return Palette[index % Palette.Length];
        }
    }
}
