using Shared.SolverConsecutive;
using Shared.UnitOperations.Basiss;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using UnitSystem;

namespace Distillator.Domain.Services;

public static class FacadeStateSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(IFacade? facade, bool includeTransientState = false)
    {
        if (facade == null) return "{}";

        var variables = new List<FacadeVariableState>();
        var properties = new List<FacadePropertyState>();
        CaptureObject(
            facade,
            string.Empty,
            variables,
            properties,
            includeTransientState,
            new HashSet<object>(ReferenceEqualityComparer.Instance));

        return JsonSerializer.Serialize(new FacadeStateSnapshot(1, variables, properties), JsonOptions);
    }

    public static void Apply(IFacade? facade, string? facadeStateJson, bool restoreProjectDefaultDisplayUnits = false)
    {
        if (facade == null || string.IsNullOrWhiteSpace(facadeStateJson)) return;

        FacadeStateSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<FacadeStateSnapshot>(facadeStateJson, JsonOptions);
        }
        catch
        {
            return;
        }

        if (snapshot == null) return;

        var variablesByPath = new Dictionary<string, IVariable>(StringComparer.OrdinalIgnoreCase);
        var propertiesByPath = new Dictionary<string, PropertyTarget>(StringComparer.OrdinalIgnoreCase);
        IndexStateTargets(facade, string.Empty, variablesByPath, propertiesByPath, new HashSet<object>(ReferenceEqualityComparer.Instance));

        foreach (var variableState in snapshot.Variables ?? new List<FacadeVariableState>())
        {
            if (!variablesByPath.TryGetValue(variableState.Path, out var variable)) continue;

            TryRestoreVariableState(variable, variableState, restoreProjectDefaultDisplayUnits);
        }

        foreach (var propertyState in snapshot.Properties ?? new List<FacadePropertyState>())
        {
            if (!propertiesByPath.TryGetValue(propertyState.Path, out var target)) continue;

            try
            {
                if (target.Property.PropertyType.IsEnum)
                {
                    target.Property.SetValue(target.Owner, Enum.Parse(target.Property.PropertyType, propertyState.Value, true));
                }
            }
            catch
            {
                // El estado viejo o incompatible se ignora para mantener compatibilidad.
            }
        }
    }

    public static bool ApplyNewerUserInputStates(
        IFacade? facade,
        string? facadeStateJson,
        string? userId,
        bool restoreProjectDefaultDisplayUnits = false)
    {
        if (facade == null || string.IsNullOrWhiteSpace(facadeStateJson) || string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        FacadeStateSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<FacadeStateSnapshot>(facadeStateJson, JsonOptions);
        }
        catch
        {
            return false;
        }

        if (snapshot?.Variables == null || snapshot.Variables.Count == 0)
        {
            return false;
        }

        var variablesByPath = new Dictionary<string, IVariable>(StringComparer.OrdinalIgnoreCase);
        var propertiesByPath = new Dictionary<string, PropertyTarget>(StringComparer.OrdinalIgnoreCase);
        IndexStateTargets(facade, string.Empty, variablesByPath, propertiesByPath, new HashSet<object>(ReferenceEqualityComparer.Instance));

        var changed = false;
        foreach (var variableState in snapshot.Variables)
        {
            if (!variableState.IsDefined ||
                variableState.DefinedAtUtc == null ||
                !string.Equals(variableState.DefinedByUserId, userId, StringComparison.OrdinalIgnoreCase) ||
                ParseEnum(variableState.DataProcedence, VariableDefinedBy.Undefined) != VariableDefinedBy.UserInput ||
                !variablesByPath.TryGetValue(variableState.Path, out var variable))
            {
                continue;
            }

            if (variable.DefinedAtUtc.HasValue && variable.DefinedAtUtc.Value >= variableState.DefinedAtUtc.Value)
            {
                continue;
            }

            changed |= TryRestoreVariableState(variable, variableState, restoreProjectDefaultDisplayUnits);
        }

        return changed;
    }

    private static void CaptureObject(
        object? value,
        string path,
        List<FacadeVariableState> variables,
        List<FacadePropertyState> properties,
        bool includeTransientState,
        HashSet<object> visited)
    {
        if (value == null || value is string or Amount or UnitMeasure) return;
        var valueType = value.GetType();
        if (valueType.IsValueType || !visited.Add(value)) return;

        if (value is IVariable variable)
        {
            if (includeTransientState || ShouldPersistVariable(variable))
            {
                variables.Add(ToState(path, variable));
            }
            return;
        }

        CaptureSimpleState(value, path, properties);

        if (value is IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                CaptureObject(item, $"{path}[{index}]", variables, properties, includeTransientState, visited);
                index++;
            }

            return;
        }

        if (!ShouldInspectType(valueType)) return;

        foreach (var property in valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!ShouldInspectProperty(property)) continue;

            var propertyValue = ReadProperty(property, value);
            var propertyPath = string.IsNullOrWhiteSpace(path) ? property.Name : $"{path}.{property.Name}";
            CaptureObject(propertyValue, propertyPath, variables, properties, includeTransientState, visited);
        }
    }

    private static void IndexStateTargets(
        object? value,
        string path,
        Dictionary<string, IVariable> variablesByPath,
        Dictionary<string, PropertyTarget> propertiesByPath,
        HashSet<object> visited)
    {
        if (value == null || value is string or Amount or UnitMeasure) return;
        var valueType = value.GetType();
        if (valueType.IsValueType || !visited.Add(value)) return;

        if (value is IVariable variable)
        {
            variablesByPath[path] = variable;
            return;
        }

        IndexSimpleState(value, path, propertiesByPath);

        if (value is IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                IndexStateTargets(item, $"{path}[{index}]", variablesByPath, propertiesByPath, visited);
                index++;
            }

            return;
        }

        if (!ShouldInspectType(valueType)) return;

        foreach (var property in valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!ShouldInspectProperty(property)) continue;

            var propertyValue = ReadProperty(property, value);
            var propertyPath = string.IsNullOrWhiteSpace(path) ? property.Name : $"{path}.{property.Name}";
            IndexStateTargets(propertyValue, propertyPath, variablesByPath, propertiesByPath, visited);
        }
    }

    private static void CaptureSimpleState(object value, string path, List<FacadePropertyState> properties)
    {
        if (value.GetType().FullName != "Shared.SolverQwen.Stream.CompositionOrchestrator") return;

        var inputType = value.GetType().GetProperty("InputType");
        var inputValue = inputType?.GetValue(value)?.ToString();
        if (!string.IsNullOrWhiteSpace(inputValue))
        {
            properties.Add(new FacadePropertyState(
                string.IsNullOrWhiteSpace(path) ? "InputType" : $"{path}.InputType",
                inputValue));
        }
    }

    private static void IndexSimpleState(object value, string path, Dictionary<string, PropertyTarget> propertiesByPath)
    {
        if (value.GetType().FullName != "Shared.SolverQwen.Stream.CompositionOrchestrator") return;

        var inputType = value.GetType().GetProperty("InputType");
        if (inputType == null || !inputType.CanWrite) return;

        propertiesByPath[string.IsNullOrWhiteSpace(path) ? "InputType" : $"{path}.InputType"] =
            new PropertyTarget(value, inputType);
    }

    private static FacadeVariableState ToState(string path, IVariable variable)
    {
        var value = ReadVariableAmount(variable);
        var displayUnit = ReadVariableDisplayUnit(variable);

        return new FacadeVariableState(
            path,
            ReadVariableAmountTypeName(variable),
            variable.IsDefined,
            value?.Value ?? 0,
            UnitName(value?.Unit),
            variable.DataProcedence.ToString(),
            UnitName(displayUnit),
            variable.HasDisplayUnitOverride,
            variable.DefinedByUserId,
            variable.DefinedByUserName,
            variable.DefinedAtUtc);
    }

    private static bool ShouldPersistVariable(IVariable variable)
    {
        return variable.DataProcedence is VariableDefinedBy.UserInput or VariableDefinedBy.Specification
            || variable.HasDisplayUnitOverride;
    }

    private static Amount? ReadVariableAmount(IVariable variable)
    {
        return variable.GetType().GetProperty("Value")?.GetValue(variable) as Amount;
    }

    private static UnitMeasure? ReadVariableDisplayUnit(IVariable variable)
    {
        return variable.GetType().GetProperty("DisplayUnit")?.GetValue(variable) as UnitMeasure;
    }

    private static string ReadVariableAmountTypeName(IVariable variable)
    {
        return variable
            .GetType()
            .GetInterfaces()
            .Append(variable.GetType())
            .Where(type => type.IsGenericType)
            .FirstOrDefault(type => type.GetGenericTypeDefinition() == typeof(IVariable<>))
            ?.GetGenericArguments()[0]
            .Name ?? string.Empty;
    }

    private static bool ShouldInspectType(Type type)
    {
        var namespaceName = type.Namespace ?? string.Empty;

        return namespaceName.StartsWith("Shared.SolverConsecutive", StringComparison.Ordinal)
            || namespaceName.StartsWith("Shared.SolverQwen.Stream", StringComparison.Ordinal);
    }

    private static bool ShouldInspectProperty(PropertyInfo property)
    {
        if (!property.CanRead || property.GetIndexParameters().Length > 0) return false;
        if (property.PropertyType == typeof(string)) return false;
        if (typeof(IFacade).IsAssignableFrom(property.PropertyType)) return false;

        var name = property.Name;
        if (name is "Inlets" or "Outlets" or "AllStreams" or "Equations" or "Specifications")
        {
            return false;
        }

        return true;
    }

    private static object? ReadProperty(PropertyInfo property, object owner)
    {
        try
        {
            return property.GetValue(owner);
        }
        catch
        {
            return null;
        }
    }

    private static UnitMeasure? ResolveUnit(string? unitName)
    {
        if (string.IsNullOrWhiteSpace(unitName)) return null;

        try
        {
            return UnitManager.GetUnitByName(unitName);
        }
        catch
        {
            return null;
        }
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }

    private static bool TryRestoreVariableState(
        IVariable variable,
        FacadeVariableState variableState,
        bool restoreProjectDefaultDisplayUnits)
    {
        var valueUnit = ResolveUnit(variableState.ValueUnitName);
        var displayUnit = ResolveUnit(variableState.DisplayUnitName);
        if (valueUnit == null) return false;

        variable.RestorePersistedState(
            variableState.IsDefined,
            variableState.Value,
            valueUnit,
            ParseEnum(variableState.DataProcedence, VariableDefinedBy.Undefined),
            displayUnit,
            variableState.HasDisplayUnitOverride,
            restoreProjectDefaultDisplayUnits,
            variableState.DefinedByUserId,
            variableState.DefinedByUserName,
            variableState.DefinedAtUtc);

        return true;
    }

    private static string UnitName(UnitMeasure? unit)
    {
        return string.IsNullOrWhiteSpace(unit?.Name) ? UnitMeasure.None.Name : unit.Name;
    }

    private sealed record FacadeStateSnapshot(
        int Version,
        List<FacadeVariableState>? Variables,
        List<FacadePropertyState>? Properties);

    private sealed record FacadeVariableState(
        string Path,
        string AmountType,
        bool IsDefined,
        double Value,
        string ValueUnitName,
        string DataProcedence,
        string DisplayUnitName,
        bool HasDisplayUnitOverride,
        string? DefinedByUserId = null,
        string? DefinedByUserName = null,
        DateTime? DefinedAtUtc = null);

    private sealed record FacadePropertyState(string Path, string Value);

    private sealed record PropertyTarget(object Owner, PropertyInfo Property);
}
