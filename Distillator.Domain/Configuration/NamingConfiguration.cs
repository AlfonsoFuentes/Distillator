namespace Distillator.Domain.Configuration;

public class NamingConfiguration : INamingConfiguration
{
    public NamingMode Mode { get; set; }
    public string Pattern { get; set; }
    public int StartingNumber { get; set; }
    public string BaseNumber { get; set; }
    public string AreaPrefix { get; set; }
    public NamingCounterScope CounterScope { get; set; }
    public IList<NamingPatternPart> PatternParts { get; set; }
    public IDictionary<string, string> PrefixesByEquipmentType { get; set; }

    public NamingConfiguration(
        NamingMode mode = NamingMode.ProjectSequential,
        string? pattern = null,
        int startingNumber = 101,
        string? baseNumber = null,
        string? areaPrefix = null,
        NamingCounterScope? counterScope = null,
        IList<NamingPatternPart>? patternParts = null,
        IDictionary<string, string>? prefixesByEquipmentType = null)
    {
        Mode = mode;
        Pattern = !string.IsNullOrWhiteSpace(pattern) ? pattern : "{Prefix}-{Number:D3}";
        StartingNumber = startingNumber > 0 ? startingNumber : 101;
        BaseNumber = !string.IsNullOrWhiteSpace(baseNumber) ? baseNumber : "1151";
        AreaPrefix = areaPrefix ?? string.Empty;
        CounterScope = counterScope ?? GetDefaultScope(mode);
        PatternParts = patternParts != null && patternParts.Count > 0
            ? patternParts.Select(part => new NamingPatternPart(part.Kind, part.Value)).ToList()
            : GetDefaultPatternParts(mode);

        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Pump"] = "P",
            ["Stream"] = "S",
            ["Column"] = "C",
            ["HeatExchanger"] = "E",
            ["FlashDrum"] = "F",
            ["Tank"] = "T",
            ["ControlValve"] = "V",
            ["Mixer"] = "M",
            ["Splitter"] = "SP",
            ["OffPageConnector"] = "OPC",
            ["Instrument"] = "I"
        };

        if (prefixesByEquipmentType != null)
        {
            foreach (var kvp in prefixesByEquipmentType)
            {
                defaults[kvp.Key] = kvp.Value;
            }
        }

        PrefixesByEquipmentType = defaults;
    }

    public static NamingConfiguration Clone(INamingConfiguration source)
    {
        return new NamingConfiguration(
            mode: source.Mode,
            pattern: source.Pattern,
            startingNumber: source.StartingNumber,
            baseNumber: source.BaseNumber,
            areaPrefix: source.AreaPrefix,
            counterScope: source.CounterScope,
            patternParts: source.PatternParts,
            prefixesByEquipmentType: source.PrefixesByEquipmentType);
    }

    public static IList<NamingPatternPart> GetDefaultPatternParts(NamingMode mode)
    {
        return mode switch
        {
            NamingMode.MainEquipmentPackageSequential => new List<NamingPatternPart>
            {
                new(NamingPatternPartKind.EquipmentPrefix),
                new(NamingPatternPartKind.Literal, "-"),
                new(NamingPatternPartKind.BaseNumber),
                new(NamingPatternPartKind.Literal, "_"),
                new(NamingPatternPartKind.Sequence)
            },
            NamingMode.DiagramSequentialWithAreaPrefix => new List<NamingPatternPart>
            {
                new(NamingPatternPartKind.AreaPrefix),
                new(NamingPatternPartKind.Literal, "-"),
                new(NamingPatternPartKind.EquipmentPrefix),
                new(NamingPatternPartKind.Literal, "-"),
                new(NamingPatternPartKind.Number)
            },
            _ => new List<NamingPatternPart>
            {
                new(NamingPatternPartKind.EquipmentPrefix),
                new(NamingPatternPartKind.Literal, "-"),
                new(NamingPatternPartKind.Number)
            }
        };
    }

    public static NamingCounterScope GetDefaultScope(NamingMode mode)
    {
        return mode switch
        {
            NamingMode.ProjectSequential => NamingCounterScope.Project,
            NamingMode.ProjectSequentialByType => NamingCounterScope.EquipmentType,
            NamingMode.DiagramSequentialWithAreaPrefix => NamingCounterScope.Diagram,
            NamingMode.DiagramSequentialByType => NamingCounterScope.DiagramAndType,
            NamingMode.MainEquipmentPackageSequential => NamingCounterScope.MainEquipmentPackage,
            NamingMode.DiagramNumberRangeSequential => NamingCounterScope.DiagramNumberRange,
            _ => NamingCounterScope.EquipmentType
        };
    }
}
