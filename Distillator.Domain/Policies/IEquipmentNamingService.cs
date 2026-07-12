using System.Text.RegularExpressions;
using Distillator.Domain.Configuration;
using Distillator.Domain.Models;
using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Policies;

/// <summary>
/// Servicio de generación de nombres únicos para equipos dentro de un proyecto.
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
    string GenerateNextName(string equipmentTypeCode, IProject project, IFlowsheet? flowsheet = null);

    /// <summary>
    /// Sugiere un nombre sin reservarlo. Útil para mostrar en UI antes de confirmar.
    /// </summary>
    string SuggestName(string equipmentTypeCode, IProject project, IFlowsheet? flowsheet = null);

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

    /// <summary>
    /// Genera el siguiente nombre único y devuelve el nombre completo (para reportes/sistema) 
    /// y el nombre corto (para la etiqueta visual en el canvas).
    /// </summary>
    (string FullName, string Label) GenerateNextNameDetails(string equipmentTypeCode, IProject project, IFlowsheet? flowsheet = null);
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

    public string SuggestName(string equipmentTypeCode, IProject project, IFlowsheet? flowsheet = null)
    {
        if (string.IsNullOrWhiteSpace(equipmentTypeCode))
            throw new ArgumentException("Equipment type code cannot be empty", nameof(equipmentTypeCode));

        var prefix = GetPrefix(equipmentTypeCode);
        var counterKey = GetCounterKey(equipmentTypeCode, prefix, flowsheet);
        var number = _counters.TryGetValue(counterKey, out var current)
            ? current
            : GetInitialCounter(flowsheet);

        string name;
        do
        {
            // Usamos BuildNames y extraemos el FullName para evaluar la disponibilidad
            name = BuildNames(equipmentTypeCode, prefix, number++, flowsheet).FullName;
        }
        while (!IsCandidateAvailable(name, number - 1, project, flowsheet));

        return name;
    }

    public bool IsNameAvailable(string name, IProject project)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return project.EquipmentRegistry.GetByName(name) == null;
    }

    private bool IsCandidateAvailable(string name, int number, IProject project, IFlowsheet? flowsheet)
    {
        if (!IsNameAvailable(name, project))
        {
            return false;
        }

        return Configuration.CounterScope switch
        {
            NamingCounterScope.Project => !IsNumberUsed(project.EquipmentRegistry.AllEquipments, number),
            NamingCounterScope.Diagram => !IsNumberUsed(GetElementsInFlowsheet(project, flowsheet), number),
            NamingCounterScope.MainEquipmentPackage => !IsNumberUsed(project.EquipmentRegistry.AllEquipments, number),
            NamingCounterScope.DiagramNumberRange => !IsNumberUsed(GetElementsInFlowsheet(project, flowsheet), number),
            _ => true
        };
    }

    private static IEnumerable<IVisualElement> GetElementsInFlowsheet(IProject project, IFlowsheet? flowsheet)
    {
        if (flowsheet == null)
        {
            return project.EquipmentRegistry.AllEquipments;
        }

        return flowsheet.Elements
            .Select(reference => project.EquipmentRegistry.GetById(reference.ElementId))
            .OfType<IVisualElement>()
            .ToList();
    }

    private static bool IsNumberUsed(IEnumerable<IVisualElement> elements, int number)
    {
        return elements.Any(element => TryGetLastNumber(element.Name, out var usedNumber) && usedNumber == number);
    }

    private static bool TryGetLastNumber(string? name, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var match = Regex.Match(name, @"(\d+)(?!.*\d)");
        return match.Success && int.TryParse(match.Value, out number);
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

    private string GetCounterKey(string equipmentTypeCode, string prefix, IFlowsheet? flowsheet)
    {
        var diagramNumber = GetDiagramNumber(flowsheet);

        return Configuration.CounterScope switch
        {
            NamingCounterScope.Project => "Project",
            NamingCounterScope.EquipmentType => $"Type:{prefix}",
            NamingCounterScope.Diagram => $"Diagram:{diagramNumber}",
            NamingCounterScope.DiagramAndType => $"Diagram:{diagramNumber}:Type:{prefix}",
            NamingCounterScope.MainEquipmentPackage => $"Package:{Configuration.BaseNumber}",
            NamingCounterScope.DiagramNumberRange => $"Range:{diagramNumber}",
            _ => $"Type:{equipmentTypeCode}"
        };
    }

    public (string FullName, string Label) GenerateNextNameDetails(string equipmentTypeCode, IProject project, IFlowsheet? flowsheet = null)
    {
        if (string.IsNullOrWhiteSpace(equipmentTypeCode))
            throw new ArgumentException("Equipment type code cannot be empty", nameof(equipmentTypeCode));

        var prefix = GetPrefix(equipmentTypeCode);
        var counterKey = GetCounterKey(equipmentTypeCode, prefix, flowsheet);
        if (!_counters.ContainsKey(counterKey))
        {
            _counters[counterKey] = GetInitialCounter(flowsheet);
        }

        string fullName;
        string label;
        int number;
        do
        {
            number = _counters[counterKey]++;
            var names = BuildNames(equipmentTypeCode, prefix, number, flowsheet);
            fullName = names.FullName;
            label = names.Label;
        }
        while (!IsCandidateAvailable(fullName, number, project, flowsheet));

        return (fullName, label);
    }

    public string GenerateNextName(string equipmentTypeCode, IProject project, IFlowsheet? flowsheet = null)
    {
        return GenerateNextNameDetails(equipmentTypeCode, project, flowsheet).FullName;
    }

    private (string FullName, string Label) BuildNames(string equipmentTypeCode, string prefix, int number, IFlowsheet? flowsheet)
    {
        if (Configuration.PatternParts.Count > 0)
        {
            var diagramPrefix = GetDiagramPrefix(flowsheet);

            var fullName = string.Concat(Configuration.PatternParts.Select(part => ResolvePart(part, equipmentTypeCode, prefix, number, diagramPrefix)));

            var labelParts = new List<string>();
            bool skipNextLiteral = false;

            foreach (var part in Configuration.PatternParts)
            {
                if (part.Kind == NamingPatternPartKind.AreaPrefix)
                {
                    skipNextLiteral = true;
                    continue;
                }
                if (skipNextLiteral && part.Kind == NamingPatternPartKind.Literal)
                {
                    skipNextLiteral = false;
                    continue;
                }

                skipNextLiteral = false;
                labelParts.Add(ResolvePart(part, equipmentTypeCode, prefix, number, string.Empty));
            }

            var label = string.Concat(labelParts);
            return (fullName, label);
        }

        var legacy = BuildLegacyName(prefix, number);
        return (legacy, legacy);
    }

    private string ResolvePart(NamingPatternPart part, string equipmentTypeCode, string prefix, int number, string diagramPrefix)
    {
        return part.Kind switch
        {
            NamingPatternPartKind.EquipmentPrefix => prefix,
            NamingPatternPartKind.AreaPrefix => diagramPrefix,
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

    private static string GetDiagramNumber(IFlowsheet? flowsheet)
    {
        return string.IsNullOrWhiteSpace(flowsheet?.DiagramNumber)
            ? "Project"
            : flowsheet.DiagramNumber.Trim();
    }

    private static string GetDiagramPrefix(IFlowsheet? flowsheet)
    {
        var diagramNumber = flowsheet?.DiagramNumber?.Trim();
        if (string.IsNullOrWhiteSpace(diagramNumber))
        {
            return string.Empty;
        }

        return int.TryParse(diagramNumber, out var number)
            ? $"{number}00"
            : diagramNumber;
    }

    private int GetInitialCounter(IFlowsheet? flowsheet)
    {
        if (Configuration.CounterScope is NamingCounterScope.Diagram or NamingCounterScope.DiagramAndType or NamingCounterScope.DiagramNumberRange &&
            int.TryParse(flowsheet?.DiagramNumber?.Trim(), out var diagramNumber))
        {
            return diagramNumber * 100 + 1;
        }

        return Configuration.StartingNumber;
    }

    private static readonly Regex NumberPlaceholderRegex = new(@"\{Number(?::([^}]+))?\}", RegexOptions.Compiled);
}
