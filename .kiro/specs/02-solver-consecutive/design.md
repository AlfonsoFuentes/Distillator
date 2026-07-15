# Design — SolverConsecutive: Unificar Newton Solvers + Limpiar Legacy

## Archivos afectados
- `Shared/SolverConsecutive/SolverNewtonSolver.cs` — agregar parámetro de scope, hacer stateless
- `Shared/SolverConsecutive/ColumnPlateNewtonSolver.cs` — eliminar
- `Shared/SolverConsecutive/IMainSolver.cs` — eliminar legacy comentado
- `Shared/SolverConsecutive/Equipments/ISolverEquipment.cs` — extraer clases de ecuaciones
- `Shared/SolverConsecutive/Equipments/` — 4 archivos nuevos para las ecuaciones

## Diseño del parámetro de scope en `NewtonSolver`

```csharp
public enum NewtonVariableScope
{
    AdjustableOnly,  // default — usa equation.AdjustableVariables()
    All              // columnas — usa equation.Variables
}

public class NewtonSolver : INewtonSolver
{
    private readonly NewtonVariableScope _variableScope;

    public NewtonSolver(NewtonVariableScope scope = NewtonVariableScope.AdjustableOnly)
    {
        _variableScope = scope;
    }

    public SolverResult Solve(ISolverEquation equation, double alpha = 1.0)
    {
        // Variables locales — no campos de instancia
        var adjustableVariables = _variableScope == NewtonVariableScope.All
            ? equation.Variables
            : equation.AdjustableVariables().ToList();
        double currentAlpha = alpha;
        // ... resto del algoritmo usando las locales ...
    }
}
```

## Localizar dónde se instancia `ColumnPlateNewtonSolver`

Buscar con `rg "ColumnPlateNewtonSolver"` — típicamente en `SolverColumn` o similar.
Reemplazar:
```csharp
// Antes:
INewtonSolver solver = new ColumnPlateNewtonSolver();

// Después:
INewtonSolver solver = new NewtonSolver(NewtonVariableScope.All);
```

## Archivos nuevos para ecuaciones

```
Shared/SolverConsecutive/Equipments/EquipmentMassBalanceEquation.cs
Shared/SolverConsecutive/Equipments/EquipmentMassEnergyBalanceEquation.cs
Shared/SolverConsecutive/Equipments/EquipmentMassEnergyBalanceWithComponentsEquation.cs
Shared/SolverConsecutive/Equipments/EquipmentComponentMassBalanceEquation.cs
```

Cada archivo contiene exactamente una clase, con el mismo namespace `Shared.SolverConsecutive.Equipments`.

## Campos que pasan de instancia a locales en `Solve()`

| Campo instancia actual | Local en Solve() |
|---|---|
| `ISolverEquation equation` | parámetro del método |
| `List<IVariable> _adjustableVariables` | `var adjustableVariables` |
| `double Alpha` | `double currentAlpha = alpha` |

Los métodos privados `CalculateJacobian`, `ApplyDampedStep`, `CheckInitialValues` reciben
`adjustableVariables` como parámetro en lugar de acceder al campo de instancia.
