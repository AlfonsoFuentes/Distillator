# Requirements — Domain: Walker de grafo + Eventos + Factories

## Problema
`FacadeStateSerializer` y `ProjectUnitSystemApplier` duplican el mismo algoritmo de traversal
de grafo de objetos. Un `EquipmentAddedEvent` se lanza con `Guid.Empty` como `FlowsheetId`.
Varias clases están agrupadas en archivos que deberían tener una clase cada uno.

## Requisitos

### REQ-01 — Un único `FacadeObjectGraphWalker` recorre el grafo
- Existe una clase `FacadeObjectGraphWalker` en `Distillator.Domain/Services/`.
- Implementa el algoritmo de traversal una sola vez.
- Acepta callbacks para lo que se hace al encontrar un `IVariable` o un objeto simple.
- `FacadeStateSerializer` y `ProjectUnitSystemApplier` delegan en el walker.

### REQ-02 — `EquipmentAddedEvent` tiene `FlowsheetId` correcto
- El evento `EquipmentAddedEvent` nunca se lanza con `Guid.Empty` como `FlowsheetId`.
- El `FlowsheetId` real se pasa en el punto donde se agrega el equipo al proyecto.

### REQ-03 — Una clase por archivo en Factories
- `PandidEquipmentFactory` vive en `Distillator.Domain/Factories/PandidEquipmentFactory.cs`.
- `ElectricalEquipmentFactory` vive en `Distillator.Domain/Factories/ElectricalEquipmentFactory.cs`.
- `PfdEquipmentFactory.cs` solo contiene `PfdEquipmentFactory`.

### REQ-04 — Una clase por archivo en Policies
- `PfdConnectionRules` vive en `Distillator.Domain/Policies/PfdConnectionRules.cs`.
- `PandidConnectionRules` vive en `Distillator.Domain/Policies/PandidConnectionRules.cs`.
- `ElectricalConnectionRules` vive en `Distillator.Domain/Policies/ElectricalConnectionRules.cs`.
- `IConnectionRules.cs` solo contiene la interfaz.

## Criterios de aceptación
- `dotnet build` sin errores.
- Guardar y recargar un proyecto recupera todos los valores de variables correctamente.
- `rg "Guid\.Empty" Distillator.Domain/` no aparece en llamadas a `EquipmentAddedEvent`.
- `rg "PandidEquipmentFactory\|ElectricalEquipmentFactory" Distillator.Domain/Factories/PfdEquipmentFactory.cs` no retorna resultados.
