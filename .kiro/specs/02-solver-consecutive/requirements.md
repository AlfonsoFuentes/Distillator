# Requirements — SolverConsecutive: Unificar Newton Solvers + Limpiar Legacy

## Problema
`NewtonSolver` y `ColumnPlateNewtonSolver` son clases casi idénticas (~280 líneas duplicadas).
Cualquier corrección al algoritmo debe aplicarse en dos lugares. Además, `IMainSolver.cs`
contiene ~600 líneas de `MainSolverLegacy` completamente comentado que contamina el archivo
con historia que ya vive en git.

## Requisitos

### REQ-01 — Un único `NewtonSolver` cubre ambos comportamientos
- `NewtonSolver` acepta un parámetro de construcción que controla si usa
  `AdjustableVariables()` o `Variables` al resolver.
- `ColumnPlateNewtonSolver` es eliminado.
- El solver de columna sigue funcionando igual que antes del cambio.

### REQ-02 — `NewtonSolver` es thread-safe en `Solve()`
- Los campos `equation`, `_adjustableVariables` y `Alpha` son variables locales
  dentro de `Solve()`, no campos de instancia.
- La instancia de `NewtonSolver` puede reutilizarse sin riesgo de race condition.

### REQ-03 — `IMainSolver.cs` solo contiene la interfaz
- El bloque de `MainSolverLegacy` comentado (~600 líneas) es eliminado.
- El archivo queda limpio con solo `IMainSolver` y los tipos de soporte mínimos.

### REQ-04 — Las 4 clases de ecuaciones viven en archivos propios
- `EquipmentMassBalanceEquation`, `EquipmentMassEnergyBalanceEquation`,
  `EquipmentMassEnergyBalanceWithComponentsEquation`, `EquipmentComponentMassBalanceEquation`
  tienen cada una su propio archivo `.cs`.
- `ISolverEquipment.cs` solo contiene la interfaz `ISolverEquipment` y la clase base.

### REQ-05 — El test de regresión del mixer sigue pasando
- `StreamMixerBalanceRegressionTest` ejecuta sin errores tras el cambio.

## Criterios de aceptación
- `dotnet build` sin errores.
- `rg "ColumnPlateNewtonSolver"` no encuentra ningún resultado en el proyecto.
- `rg "MainSolverLegacy"` no encuentra ningún resultado en el proyecto.
- El solver de columna resuelve ecuaciones correctamente (prueba manual o test).
