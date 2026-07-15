# Design — Domain: Walker de grafo + Eventos + Factories

## Archivos afectados
- `Distillator.Domain/Services/FacadeObjectGraphWalker.cs` — nuevo
- `Distillator.Domain/Services/FacadeStateSerializer.cs` — refactorizar
- `Distillator.Domain/Services/ProjectUnitSystemApplier.cs` — refactorizar
- `Distillator.Domain/Models/Project.cs` — corregir evento
- `Distillator.Domain/Factories/PfdEquipmentFactory.cs` — extraer factories
- `Distillator.Domain/Factories/PandidEquipmentFactory.cs` — nuevo
- `Distillator.Domain/Factories/ElectricalEquipmentFactory.cs` — nuevo
- `Distillator.Domain/Policies/IConnectionRules.cs` — extraer clases
- `Distillator.Domain/Policies/PfdConnectionRules.cs` — nuevo
- `Distillator.Domain/Policies/PandidConnectionRules.cs` — nuevo
- `Distillator.Domain/Policies/ElectricalConnectionRules.cs` — nuevo

## Diseño de `FacadeObjectGraphWalker`

```csharp
namespace Distillator.Domain.Services;

public static class FacadeObjectGraphWalker
{
    // Recorre el grafo de objetos a partir de root.
    // onVariable: callback llamado con (path, variable) al encontrar IVariable.
    // onSimpleState: callback opcional para objetos especiales (ej. CompositionOrchestrator).
    // extraNamespaces: namespaces adicionales a inspeccionar además de los defaults.
    public static void Walk(
        object? root,
        Action<string, IVariable> onVariable,
        Action<object, string>? onSimpleState = null,
        IReadOnlyCollection<string>? extraNamespaces = null)
    { ... }
}
```

## Uso en `FacadeStateSerializer`

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

// En Apply() — indexar targets:
FacadeObjectGraphWalker.Walk(
    facade,
    onVariable: (path, variable) => variablesByPath[path] = variable,
    onSimpleState: (obj, path) => IndexSimpleState(obj, path, propertiesByPath));
```

## Uso en `ProjectUnitSystemApplier`

```csharp
FacadeObjectGraphWalker.Walk(
    facade,
    onVariable: (_, variable) => ApplyToVariable(variable, units),
    extraNamespaces: new[] { "Shared.UnitOperations" });
```

## Corrección del evento en `Project.cs`

Localizar el método que agrega equipos (probable `AddEquipmentToFlowsheet` o similar).
El `flowsheetId` debe estar disponible en ese contexto y pasarse al evento:

```csharp
// Antes:
Raise(new EquipmentAddedEvent(Id, Guid.Empty, equipment.Id, equipment.Type.ToString()));

// Después:
Raise(new EquipmentAddedEvent(Id, flowsheetId, equipment.Id, equipment.Type.ToString()));
```
