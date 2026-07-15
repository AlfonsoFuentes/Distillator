# Spec 02 — SolverConsecutive: Unificar Newton Solvers + Limpiar Legacy

## Estado
Pendiente

## Archivos afectados
- `Shared/SolverConsecutive/SolverNewtonSolver.cs` — `NewtonSolver`
- `Shared/SolverConsecutive/ColumnPlateNewtonSolver.cs` — eliminar
- `Shared/SolverConsecutive/IMainSolver.cs` — limpiar legacy comentado
- `Shared/SolverConsecutive/Equipments/ISolverEquipment.cs` — separar clases de ecuaciones

---

## Contexto

`NewtonSolver` y `ColumnPlateNewtonSolver` son prácticamente idénticos. La única diferencia
funcional es una línea en `Solve()`:

```csharp
// NewtonSolver:
_adjustableVariables = equation.AdjustableVariables().ToList();

// ColumnPlateNewtonSolver:
_adjustableVariables = equation.Variables;   // ← usa TODOS los variables, no solo ajustables
```

Todo el resto — `Solve`, `CheckInitialValues`, `CalculateJacobian`, `ApplyDampedStep`,
`GetNorm` — es código idéntico. Cualquier corrección al solver debe aplicarse en dos lugares.

Además, `IMainSolver.cs` tiene ~600 líneas de `MainSolverLegacy` completamente comentado.

---

## Problemas a resolver

### 1. [ALTA] `ColumnPlateNewtonSolver` duplica `NewtonSolver` — DRY grave
~280 líneas idénticas. Un bug en el Jacobiano, damping o convergencia debe corregirse en dos
archivos. Riesgo real de que divergan silenciosamente.

### 2. [ALTA] `IMainSolver.cs` tiene ~600 líneas de código legacy comentado
`MainSolverLegacy`, `ClusterEquations`, `BuildSpecificationClustersV2`, `BuildSpecificationClustersV3`,
`MapSpecificationDependencyClusters_Old`, `CreateEquationsByTypeV2`, `CreateEquationsByType_Old`.
Git ya preserva este historial — no hay razón para tenerlo en producción.

### 3. [MEDIA] `ISolverEquipment.cs` mezcla interfaz + 4 clases de ecuaciones concretas
`EquipmentMassBalanceEquation`, `EquipmentMassEnergyBalanceEquation`,
`EquipmentMassEnergyBalanceWithComponentsEquation`, `EquipmentComponentMassBalanceEquation`
viven en el mismo archivo que `ISolverEquipment`. Las ecuaciones concretas deben vivir en
archivos propios.

### 4. [MEDIA] `NewtonSolver` tiene campos de instancia mutables — no es thread-safe
`equation`, `_adjustableVariables` y `Alpha` son campos de instancia asignados en `Solve()`.
Si la misma instancia se reutiliza concurrentemente (posible con `Task.Run`), hay race condition.

---

## Cambios requeridos

### Paso 1 — Unificar en `NewtonSolver` con parámetro `useAllVariables`

Agregar un enum o bool al constructor de `NewtonSolver`:

```csharp
public enum NewtonVariableScope
{
    AdjustableOnly,  // comportamiento default (NewtonSolver actual)
    All              // comportamiento de ColumnPlateNewtonSolver
}

public class NewtonSolver : INewtonSolver
{
    private readonly NewtonVariableScope _variableScope;

    public NewtonSolver(NewtonVariableScope scope = NewtonVariableScope.AdjustableOnly)
    {
        _variableScope = scope;
    }

    // En Solve():
    _adjustableVariables = _variableScope == NewtonVariableScope.All
        ? equation.Variables
        : equation.AdjustableVariables().ToList();
}
```

Donde se usaba `ColumnPlateNewtonSolver`, reemplazar por:
```csharp
new NewtonSolver(NewtonVariableScope.All)
```

### Paso 2 — Eliminar `ColumnPlateNewtonSolver.cs`
Una vez que `NewtonSolver` cubre ambos comportamientos, eliminar el archivo.

### Paso 3 — Hacer `NewtonSolver` stateless en `Solve()`
Mover `equation`, `_adjustableVariables` y `Alpha` de campos de instancia a variables locales
dentro de `Solve()`. Esto elimina la posibilidad de race condition y hace el solver reusable.

```csharp
// Antes (campos de instancia):
ISolverEquation equation = null!;
List<IVariable> _adjustableVariables = null!;
double Alpha = 1;

// Después (locales en Solve):
public SolverResult Solve(ISolverEquation equation, double alpha = 1.0)
{
    var adjustableVariables = /* según scope */
    // usar locales en todo el método
}
```

### Paso 4 — Limpiar `IMainSolver.cs`
Eliminar todo el bloque de `MainSolverLegacy` comentado (~600 líneas).
Dejar solo la interfaz `IMainSolver` y los tipos de soporte necesarios.

### Paso 5 — Separar las 4 clases de ecuaciones de `ISolverEquipment.cs`
Moverlas a archivos individuales en `Shared/SolverConsecutive/Equipments/`:
- `EquipmentMassBalanceEquation.cs`
- `EquipmentMassEnergyBalanceEquation.cs`
- `EquipmentMassEnergyBalanceWithComponentsEquation.cs`
- `EquipmentComponentMassBalanceEquation.cs`

---

## Verificación

1. `dotnet build` sin errores ni warnings nuevos.
2. Buscar con `rg "ColumnPlateNewtonSolver"` — no debe encontrar ningún uso.
3. Los tests de regresión existentes (`StreamMixerBalanceRegressionTest`) siguen pasando.
4. El solver de columna (`SolverColumn`) sigue creando su solver con `NewtonVariableScope.All`.

---

## Riesgo
**Medio.** El cambio en el scope de variables es la parte crítica. Verificar con el test
de regresión existente y una prueba manual de columna antes de cerrar.

---

## Dependencias previas
Spec 01 (opcional — no hay dependencia directa).
