# Tasks — SolverConsecutive: Unificar Newton Solvers + Limpiar Legacy

## Task 1 — Localizar usos de `ColumnPlateNewtonSolver`
- [ ] Ejecutar `rg "ColumnPlateNewtonSolver" --include="*.cs"` y registrar todos los archivos que la instancian.
- [ ] Leer cada archivo que la instancia para entender el contexto de uso.

## Task 2 — Agregar `NewtonVariableScope` a `SolverNewtonSolver.cs`
- [ ] Abrir `Shared/SolverConsecutive/SolverNewtonSolver.cs`.
- [ ] Agregar el enum `NewtonVariableScope { AdjustableOnly, All }` en el mismo namespace.
- [ ] Agregar campo `private readonly NewtonVariableScope _variableScope` en `NewtonSolver`.
- [ ] Agregar constructor `public NewtonSolver(NewtonVariableScope scope = NewtonVariableScope.AdjustableOnly)`.

## Task 3 — Hacer `NewtonSolver.Solve()` stateless
- [ ] Convertir el campo `ISolverEquation equation` en parámetro local (ya es parámetro del método — eliminar el campo de instancia).
- [ ] Convertir `List<IVariable> _adjustableVariables` en variable local dentro de `Solve()`.
- [ ] Convertir `double Alpha` en variable local `double currentAlpha = alpha`.
- [ ] Actualizar `CalculateJacobian` para recibir `List<IVariable> adjustableVariables` como parámetro.
- [ ] Actualizar `ApplyDampedStep` para recibir `List<IVariable> adjustableVariables` como parámetro.
- [ ] Actualizar `CheckInitialValues` para recibir `List<IVariable> adjustableVariables` como parámetro.
- [ ] En `Solve()`, usar `_variableScope` para determinar qué variables usar: `AdjustableOnly` → `equation.AdjustableVariables().ToList()`, `All` → `equation.Variables`.

## Task 4 — Reemplazar `ColumnPlateNewtonSolver` por `NewtonSolver(NewtonVariableScope.All)`
- [ ] En cada archivo encontrado en Task 1, reemplazar `new ColumnPlateNewtonSolver()` por `new NewtonSolver(NewtonVariableScope.All)`.
- [ ] Verificar que el tipo de campo/propiedad es `INewtonSolver` (no `ColumnPlateNewtonSolver` concreto).

## Task 5 — Eliminar `ColumnPlateNewtonSolver.cs`
- [ ] Eliminar el archivo `Shared/SolverConsecutive/ColumnPlateNewtonSolver.cs`.
- [ ] Ejecutar `dotnet build Shared/Shared.csproj` — confirmar 0 errores.

## Task 6 — Limpiar `IMainSolver.cs`
- [ ] Abrir `Shared/SolverConsecutive/IMainSolver.cs`.
- [ ] Eliminar todo el bloque comentado de `MainSolverLegacy` (~600 líneas).
- [ ] Dejar solo la interfaz `IMainSolver` y los using necesarios.
- [ ] Ejecutar `dotnet build` — confirmar 0 errores.

## Task 7 — Separar clases de ecuaciones de `ISolverEquipment.cs`
- [ ] Leer `Shared/SolverConsecutive/Equipments/ISolverEquipment.cs` para identificar las 4 clases.
- [ ] Crear `Shared/SolverConsecutive/Equipments/EquipmentMassBalanceEquation.cs` con la clase `EquipmentMassBalanceEquation` y el namespace `Shared.SolverConsecutive.Equipments`.
- [ ] Crear `Shared/SolverConsecutive/Equipments/EquipmentMassEnergyBalanceEquation.cs` con la clase correspondiente.
- [ ] Crear `Shared/SolverConsecutive/Equipments/EquipmentMassEnergyBalanceWithComponentsEquation.cs` con la clase correspondiente.
- [ ] Crear `Shared/SolverConsecutive/Equipments/EquipmentComponentMassBalanceEquation.cs` con la clase correspondiente.
- [ ] Eliminar las 4 clases de `ISolverEquipment.cs`, dejando solo la interfaz y la clase base.

## Task 8 — Verificación final
- [ ] Ejecutar `dotnet build` en la solución completa — confirmar 0 errores.
- [ ] Ejecutar `rg "ColumnPlateNewtonSolver" --include="*.cs"` — debe retornar 0 resultados.
- [ ] Ejecutar `rg "MainSolverLegacy" --include="*.cs"` — debe retornar 0 resultados.
- [ ] Confirmar manualmente que el solver de columna sigue instanciando `NewtonSolver(NewtonVariableScope.All)`.
