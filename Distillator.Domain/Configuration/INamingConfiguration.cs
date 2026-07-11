namespace Distillator.Domain.Configuration;

/// <summary>
/// Configuración del servicio de nombrado de equipos.
/// Define cómo se generan los nombres únicos de equipos dentro de un proyecto.
/// </summary>
public interface INamingConfiguration
{
    /// <summary>Modo principal de nombrado automático.</summary>
    NamingMode Mode { get; set; }

    /// <summary>Patrón de nombrado (ej: "{Prefix}-{Number:D3}").</summary>
    string Pattern { get; set; }

    /// <summary>Número inicial para cada prefijo (ej: 101).</summary>
    int StartingNumber { get; set; }

    /// <summary>Número base usado por modos de paquete o equipo principal.</summary>
    string BaseNumber { get; set; }

    /// <summary>Prefijo opcional del área o diagrama.</summary>
    string AreaPrefix { get; set; }

    /// <summary>Alcance del contador usado para generar el siguiente nombre.</summary>
    NamingCounterScope CounterScope { get; set; }

    /// <summary>Partes ordenadas del patrón visual de nombrado.</summary>
    IList<NamingPatternPart> PatternParts { get; set; }

    /// <summary>Prefijos por tipo de equipo (ej: Pump -> "P", Stream -> "S").</summary>
    IDictionary<string, string> PrefixesByEquipmentType { get; set; }
}

public enum NamingMode
{
    ProjectSequential,
    ProjectSequentialByType,
    DiagramSequentialWithAreaPrefix,
    DiagramSequentialByType,
    MainEquipmentPackageSequential,
    DiagramNumberRangeSequential
}

public enum NamingCounterScope
{
    Project,
    EquipmentType,
    Diagram,
    DiagramAndType,
    MainEquipmentPackage,
    DiagramNumberRange
}

public enum NamingPatternPartKind
{
    EquipmentPrefix,
    AreaPrefix,
    BaseNumber,
    Number,
    Sequence,
    Literal
}

public class NamingPatternPart
{
    public NamingPatternPartKind Kind { get; set; }
    public string Value { get; set; }

    public NamingPatternPart(NamingPatternPartKind kind = NamingPatternPartKind.Literal, string? value = null)
    {
        Kind = kind;
        Value = value ?? string.Empty;
    }
}
