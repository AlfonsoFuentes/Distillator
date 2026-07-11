using System.Text.RegularExpressions;
using Distillator.Domain.Configuration;
using Distillator.Domain.Models;
using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Policies;

/// <summary>
/// Servicio de generación de nombres únicos para equipos dentro de un proyecto.
/// </summary>
public interface IEquipmentNamingService
{
    /// <summary>
    /// Configuración del servicio de nombrado.
    /// </summary>
    INamingConfiguration Configuration { get; }

    /// <summary>
    /// Actualiza la configuración de nombrado (por ejemplo, al cambiar de proyecto).
    /// </summary>
    void SetConfiguration(INamingConfiguration configuration);

    /// <summary>
    /// Genera el siguiente nombre único para un tipo de equipo.
    /// </summary>
    /// <param name="equipmentTypeCode">Código del tipo de equipo (ej: "Pump", "Stream").</param>
    /// <param name="project">Proyecto donde se debe garantizar unicidad.</param>
    /// <returns>Nombre único generado (ej: "P-101").</returns>
    string GenerateNextName(string equipmentTypeCode, IProject project);

    /// <summary>
    /// Sugiere un nombre sin reservarlo. Útil para mostrar en UI antes de confirmar.
    /// </summary>
    string SuggestName(string equipmentTypeCode, IProject project);

    /// <summary>
    /// Indica si un nombre está disponible en el proyecto.
    /// </summary>
    bool IsNameAvailable(string name, IProject project);

    /// <summary>
    /// Renombra un elemento validando unicidad. Devuelve true si tuvo éxito.
    /// </summary>
    bool Rename(IVisualElement element, string newName, IProject project);

    /// <summary>
    /// Obtiene el prefijo configurado para un tipo de equipo.
    /// </summary>
    string GetPrefix(string equipmentTypeCode);
}

public class EquipmentNamingService : IEquipmentNamingService
{
    private readonly Dictionary<string, int> _counters = new(StringComparer.OrdinalIgnoreCase);

    public INamingConfiguration Configuration { get; private set; }

    public EquipmentNamingService(INamingConfiguration? configuration = null)
    {
        Configuration = configuration ?? new NamingConfiguration();
    }

    public void SetConfiguration(INamingConfiguration configuration)
    {
        Configuration = configuration ?? new NamingConfiguration();
        _counters.Clear();
    }

    public string GenerateNextName(string equipmentTypeCode, IProject project)
    {
        if (string.IsNullOrWhiteSpace(equipmentTypeCode))
            throw new ArgumentException("Equipment type code cannot be empty", nameof(equipmentTypeCode));

        var prefix = GetPrefix(equipmentTypeCode);
        var counterKey = GetCounterKey(equipmentTypeCode, prefix);
        if (!_counters.ContainsKey(counterKey))
        {
            _counters[counterKey] = Configuration.StartingNumber;
        }

        string name;
        do
        {
            var number = _counters[counterKey]++;
            name = BuildName(equipmentTypeCode, prefix, number);
        }
        while (!IsNameAvailable(name, project));

        return name;
    }

    public string SuggestName(string equipmentTypeCode, IProject project)
    {
        if (string.IsNullOrWhiteSpace(equipmentTypeCode))
            throw new ArgumentException("Equipment type code cannot be empty", nameof(equipmentTypeCode));

        var prefix = GetPrefix(equipmentTypeCode);
        var counterKey = GetCounterKey(equipmentTypeCode, prefix);
        var number = _counters.TryGetValue(counterKey, out var current)
            ? current
            : Configuration.StartingNumber;

        string name;
        do
        {
            name = BuildName(equipmentTypeCode, prefix, number++);
        }
        while (!IsNameAvailable(name, project));

        return name;
    }

    public bool IsNameAvailable(string name, IProject project)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return project.EquipmentRegistry.GetByName(name) == null;
    }

    public bool Rename(IVisualElement element, string newName, IProject project)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (string.IsNullOrWhiteSpace(newName)) return false;
        if (!IsNameAvailable(newName, project) && !string.Equals(element.Name, newName, StringComparison.OrdinalIgnoreCase))
            return false;

        element.Name = newName;
        element.Label = newName;
        if (element.Facade != null)
        {
            element.Facade.Name = newName;
        }

        return true;
    }

    public string GetPrefix(string equipmentTypeCode)
    {
        if (string.IsNullOrWhiteSpace(equipmentTypeCode))
            throw new ArgumentException("Equipment type code cannot be empty", nameof(equipmentTypeCode));

        if (Configuration.PrefixesByEquipmentType.TryGetValue(equipmentTypeCode, out var prefix))
        {
            return prefix;
        }

        return equipmentTypeCode.Substring(0, 1).ToUpperInvariant();
    }

    private string GetCounterKey(string equipmentTypeCode, string prefix)
    {
        return Configuration.CounterScope switch
        {
            NamingCounterScope.Project => "Project",
            NamingCounterScope.EquipmentType => $"Type:{prefix}",
            NamingCounterScope.Diagram => $"Diagram:{Configuration.AreaPrefix}",
            NamingCounterScope.DiagramAndType => $"Diagram:{Configuration.AreaPrefix}:Type:{prefix}",
            NamingCounterScope.MainEquipmentPackage => $"Package:{Configuration.BaseNumber}",
            NamingCounterScope.DiagramNumberRange => $"Range:{Configuration.AreaPrefix}",
            _ => $"Type:{equipmentTypeCode}"
        };
    }

    private string BuildName(string equipmentTypeCode, string prefix, int number)
    {
        if (Configuration.PatternParts.Count > 0)
        {
            return string.Concat(Configuration.PatternParts.Select(part => ResolvePart(part, equipmentTypeCode, prefix, number)));
        }

        return BuildLegacyName(prefix, number);
    }

    private string ResolvePart(NamingPatternPart part, string equipmentTypeCode, string prefix, int number)
    {
        return part.Kind switch
        {
            NamingPatternPartKind.EquipmentPrefix => prefix,
            NamingPatternPartKind.AreaPrefix => Configuration.AreaPrefix,
            NamingPatternPartKind.BaseNumber => Configuration.BaseNumber,
            NamingPatternPartKind.Number => FormatNumber(number),
            NamingPatternPartKind.Sequence => FormatSequence(number),
            NamingPatternPartKind.Literal => part.Value,
            _ => string.Empty
        };
    }

    private string BuildLegacyName(string prefix, int number)
    {
        var result = Configuration.Pattern.Replace("{Prefix}", prefix);
        result = NumberPlaceholderRegex.Replace(result, m =>
        {
            var format = m.Groups[1].Success ? m.Groups[1].Value : string.Empty;
            return string.IsNullOrEmpty(format) ? number.ToString() : number.ToString(format);
        });
        return result;
    }

    private static string FormatNumber(int number)
    {
        return number.ToString("D3");
    }

    private static string FormatSequence(int number)
    {
        return number.ToString();
    }

    private static readonly Regex NumberPlaceholderRegex = new(@"\{Number(?::([^}]+))?\}", RegexOptions.Compiled);
}
