# Spec 03 — Distillator.Domain: Walker de grafo + Eventos + Factories

## Estado
Pendiente

## Archivos afectados
- `Distillator.Domain/Services/FacadeStateSerializer.cs`
- `Distillator.Domain/Services/ProjectUnitSystemApplier.cs`
- `Distillator.Domain/Events/DomainEvents.cs`
- `Distillator.Domain/Factories/PfdEquipmentFactory.cs`
- `Distillator.Domain/Policies/IConnectionRules.cs`

---

## Contexto

El dominio está bien estructurado (DDD, repositorios, aggregate root `Project`). Los problemas
son puntuales: duplicación de un algoritmo crítico, un evento de dominio incompleto y
varias clases mezcladas en archivos únicos sin necesidad.

---

## Problemas a resolver

### 1. [ALTA] `FacadeStateSerializer` y `ProjectUnitSystemApplier` duplican el traversal de grafo

Ambas clases recorren el grafo de objetos de un `IFacade` con el mismo algoritmo:
- Mismo check de `null`, `string`, `Amount`, `UnitMeasure`
- Mismo `HashSet<object>` de visitados con `ReferenceEqualityComparer`
- Mismo manejo de `IVariable`, `IEnumerable` y propiedades por reflection
- Misma función `ShouldInspectType` (namespaces `Shared.SolverConsecutive`, `Shared.SolverQwen.Stream`)
- La única diferencia es el callback al encontrar un `IVariable`

Esta duplicación significa que si se cambia la lógica de traversal (por ejemplo, para soportar
nuevos namespaces), hay que cambiarlo en dos lugares — y en el pasado ya divergieron
(`ProjectUnitSystemApplier` también inspecciona `Shared.UnitOperations`, el `Serializer` no).

### 2. [ALTA] `EquipmentAddedEvent` pasa `Guid.Empty` como `FlowsheetId`

En `Project.cs`:
```csharp
Raise(new EquipmentAddedEvent(Id, Guid.Empty, equipment.Id, equipment.Type.ToString()));
```

El evento no tiene el `FlowsheetId` real. Si algún handler necesita saber en qué diagrama
se agregó el equipo (ej. persistencia diferencial, broadcast SignalR por diagrama), el dato
está perdido. El evento de dominio debe ser rico y correcto.

### 3. [MEDIA] `PfdEquipmentFactory.cs` tiene tres factories en un archivo
`PfdEquipmentFactory`, `PandidEquipmentFactory` y `ElectricalEquipmentFactory` en el mismo
archivo. Las dos últimas son stubs con `// TODO`. Un archivo por clase.

### 4. [BAJA] `IConnectionRules.cs` tiene tres implementaciones en el mismo archivo
`PfdConnectionRules`, `PandidConnectionRules`, `ElectricalConnectionRules` en el mismo
archivo que la interfaz. Un archivo por clase.

---

## Cambios requeridos

### Paso 1 — Crear `FacadeObjectGraphWalker`

Extraer el algoritmo de traversal a una clase utilitaria en `Distillator.Domain/Services/`:

```csharp
/// <summary>
/// Recorre el grafo de objetos de un IFacade aplicando un callback en cada IVariable encontrado.
/// Único punto de verdad para la inspección de grafos de facades.
/// </summary>
public static class FacadeObjectGraphWalker
{
    public static void Walk(
        object? root,
        Action<string, IVariable> onVariable,
        Action<object, string>? onSimpleState = null,
        HashSet<string>? extraNamespaces = null)
    {
        Walk(root, string.Empty, onVariable, onSimpleState, extraNamespaces,
             new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    private static void Walk(
        object? value,
        string path,
        Action<string, IVariable> onVariable,
        Action<object, string>? onSimpleState,
        HashSet<string>? extraNamespaces,
        HashSet<object> visited)
    {
        if (value == null || value is string or Amount or UnitMeasure) return;
        var type = value.GetType();
        if (type.IsValueType || !visited.Add(value)) return;

        if (value is IVariable variable)
        {
            onVariable(path, variable);
            return;
        }

        onSimpleState?.Invoke(value, path);

        if (value is IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                Walk(item, $"{path}[{index}]", onVariable, onSimpleState, extraNamespaces, visited);
                index++;
            }
            return;
        }

        if (!ShouldInspectType(type, extraNamespaces)) return;

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!ShouldInspectProperty(property)) continue;
            var propertyValue = SafeGetValue(property, value);
            var propertyPath = string.IsNullOrWhiteSpace(path) ? property.Name : $"{path}.{property.Name}";
            Walk(propertyValue, propertyPath, onVariable, onSimpleState, extraNamespaces, visited);
        }
    }

    private static bool ShouldInspectType(Type type, HashSet<string>? extraNamespaces)
    {
        var ns = type.Namespace ?? string.Empty;
        if (ns.StartsWith("Shared.SolverConsecutive", StringComparison.Ordinal)) return true;
        if (ns.StartsWith("Shared.SolverQwen.Stream", StringComparison.Ordinal)) return true;
        if (extraNamespaces != null)
        {
            foreach (var extra in extraNamespaces)
                if (ns.StartsWith(extra, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static bool ShouldInspectProperty(PropertyInfo property)
    {
        if (!property.CanRead || property.GetIndexParameters().Length > 0) return false;
        if (property.PropertyType == typeof(string)) return false;
        if (typeof(IFacade).IsAssignableFrom(property.PropertyType)) return false;
        var name = property.Name;
        return name is not ("Inlets" or "Outlets" or "AllStreams" or "Equations" or "Specifications");
    }

    private static object? SafeGetValue(PropertyInfo property, object owner)
    {
        try { return property.GetValue(owner); }
        catch { return null; }
    }
}
```

### Paso 2 — Refactorizar `FacadeStateSerializer` para usar el walker

`CaptureObject` e `IndexStateTargets` se reemplazan por:

```csharp
// En Serialize():
FacadeObjectGraphWalker.Walk(
    facade,
    onVariable: (path, variable) =>
    {
        if (includeTransientState || ShouldPersistVariable(variable))
            variables.Add(ToState(path, variable));
    },
    onSimpleState: (obj, path) => CaptureSimpleState(obj, path, properties));

// En Apply():
FacadeObjectGraphWalker.Walk(
    facade,
    onVariable: (path, variable) => variablesByPath[path] = variable,
    onSimpleState: (obj, path) => IndexSimpleState(obj, path, propertiesByPath));
```

### Paso 3 — Refactorizar `ProjectUnitSystemApplier` para usar el walker

```csharp
FacadeObjectGraphWalker.Walk(
    facade,
    onVariable: (_, variable) => ApplyToVariable(variable, units),
    extraNamespaces: new HashSet<string> { "Shared.UnitOperations" });
```

### Paso 4 — Corregir `EquipmentAddedEvent` en `Project.cs`

Localizar la llamada en `Project.AddEquipmentToFlowsheet` (o equivalente) y pasar el
`flowsheetId` real:

```csharp
// Antes:
Raise(new EquipmentAddedEvent(Id, Guid.Empty, equipment.Id, equipment.Type.ToString()));

// Después:
Raise(new EquipmentAddedEvent(Id, flowsheetId, equipment.Id, equipment.Type.ToString()));
```

Verificar que el `flowsheetId` sea accesible en el contexto donde se lanza el evento.

### Paso 5 — Separar las factories en archivos individuales

Crear:
- `Distillator.Domain/Factories/PandidEquipmentFactory.cs`
- `Distillator.Domain/Factories/ElectricalEquipmentFactory.cs`

Dejar `PfdEquipmentFactory.cs` solo con `PfdEquipmentFactory`.

### Paso 6 — Separar las connection rules en archivos individuales

Crear:
- `Distillator.Domain/Policies/PfdConnectionRules.cs`
- `Distillator.Domain/Policies/PandidConnectionRules.cs`
- `Distillator.Domain/Policies/ElectricalConnectionRules.cs`

Dejar `IConnectionRules.cs` solo con la interfaz.

---

## Verificación

1. `dotnet build` sin errores.
2. `rg "CaptureObject\|IndexStateTargets"` no debe aparecer fuera de `FacadeStateSerializer`.
3. Test de serialización: serializar un `SolverDrum` con variables definidas, aplicar
   a otro, confirmar que los valores se restauran correctamente.
4. `EquipmentAddedEvent.FlowsheetId` ya no es `Guid.Empty` en los eventos lanzados.

---

## Riesgo
**Medio.** El walker es lógica crítica — afecta persistencia y unidades del proyecto.
Verificar con prueba manual de guardar/cargar un proyecto antes de cerrar.

---

## Dependencias previas
Specs 01 y 02 (ninguna dependencia directa, pero conviene tenerlas limpias primero).
