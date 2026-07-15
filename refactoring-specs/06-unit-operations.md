# Spec 06 — Shared/UnitOperations: Definir o Eliminar la Capa

## Estado
Pendiente — requiere decisión de arquitectura antes de implementar

## Archivos afectados
- `Shared/UnitOperations/` — toda la carpeta
- `Shared/UnitOperations/Basiss/IFacade.cs`
- `Shared/UnitOperations/Basiss/IEquipmentFacade.cs`
- `Shared/UnitOperations/Helpers/MixerSimulationFacade.cs`
- `Shared/UnitOperations/Instruments/InstrumentSimulationFacade.cs`
- `Shared/UnitOperations/Pipes/PipeDesignFacade.cs`
- `Shared/UnitOperations/Streams/ThermodynamicState.cs`

---

## Contexto

`Shared/UnitOperations` tiene la intención de ser la capa de operaciones unitarias (facades
de equipos). Sin embargo, el estado actual es inconsistente:

- `Basiss/IFacade.cs` y `IEquipmentFacade.cs` — contratos base usados en todo el proyecto ✅
- `Helpers/MixerSimulationFacade.cs` — facade del mixer ✅
- `Instruments/InstrumentSimulationFacade.cs` — facade de instrumento ✅
- `Pipes/PipeDesignFacade.cs` — facade de tuberías ✅
- `Streams/ThermodynamicState.cs` — estado termodinámico de streams ✅
- `HeatExchangers/` — **vacía**
- `Pumps/` — **vacía**
- `Vessels/` — **vacía**

Los equipos de mayor complejidad (intercambiadores, bombas, tanques) **no tienen facade aquí**.
Sus modelos viven en `Shared/SolverConsecutive/Equipments/` (`SolverHeatExchanger`,
`SolverPump`, `SolverVessel`, etc.) que implementan `IEquipmentFacade` directamente.

Esto genera ambigüedad: ¿dónde deben vivir los facades? ¿En `UnitOperations` o en
`SolverConsecutive`?

---

## Decisión de arquitectura requerida

Antes de implementar, confirmar con Alfonso cuál es el modelo deseado:

**Opción A — `UnitOperations` como capa completa de facades**
- Mover todos los `Solver*` que implementan `IEquipmentFacade` desde `SolverConsecutive/Equipments/`
  a `UnitOperations/`.
- `SolverConsecutive` solo contiene el motor Newton, ecuaciones e interfaces del solver.
- Impacto: alto — mover ~10 clases y actualizar referencias.

**Opción B — `SolverConsecutive` como capa única de lógica de simulación**
- Eliminar las carpetas vacías de `UnitOperations` (`HeatExchangers/`, `Pumps/`, `Vessels/`).
- Mover `MixerSimulationFacade`, `InstrumentSimulationFacade` a `SolverConsecutive/Equipments/`
  para consolidar.
- `UnitOperations` queda solo con los contratos base (`IFacade`, `IEquipmentFacade`),
  `PipeDesignFacade`, `ThermodynamicState` y opcionalmente se renombra a `Shared/Contracts/`.
- Impacto: bajo — no se mueve lógica crítica.

**Opción C — Mantener como está, solo limpiar carpetas vacías**
- Eliminar `HeatExchangers/`, `Pumps/`, `Vessels/` vacías.
- Dejar el resto sin cambios.
- Impacto: mínimo. Resuelve la confusión visual sin resolver la ambigüedad arquitectural.
- Conveniente si P&ID y Electrical tienen equipos futuros que usarán estas carpetas.

---

## Problemas a resolver (independientemente de la opción)

### 1. [ALTA] Carpetas `HeatExchangers/`, `Pumps/`, `Vessels/` están vacías
Confunden sobre el estado de implementación de la capa. Se eliminan en cualquier opción.

### 2. [MEDIA] Ambigüedad sobre dónde viven los facades de equipos
`MixerSimulationFacade` está en `UnitOperations/Helpers` pero `SolverStreamMixer` está en
`SolverConsecutive/Equipments`. ¿Son lo mismo? ¿Tienen roles diferentes?
Definir claramente la separación.

---

## Cambios mínimos (Opción C — pendiente decisión)

### Paso 1 — Eliminar carpetas vacías
```
Shared/UnitOperations/HeatExchangers/  →  eliminar
Shared/UnitOperations/Pumps/           →  eliminar
Shared/UnitOperations/Vessels/         →  eliminar
```

### Paso 2 — Documentar la convención vigente
Agregar un `README.md` breve en `Shared/UnitOperations/` explicando:
- `Basiss/` — contratos base para todos los facades
- `Helpers/`, `Instruments/`, `Pipes/` — facades de equipos simples
- Facades de equipos complejos viven en `SolverConsecutive/Equipments/` por ahora

---

## Verificación

1. `dotnet build` sin errores.
2. Las carpetas eliminadas no aparecen en el explorador de solución.
3. Sin cambio de comportamiento en runtime.

---

## Riesgo
**Bajo** para Opción C.
**Alto** para Opciones A/B — requieren mover clases y actualizar referencias en todo el proyecto.

---

## Dependencias previas
Ninguna. Pero conviene tener la decisión tomada antes de la Spec 07 (Server) y Spec 08 (Client).
