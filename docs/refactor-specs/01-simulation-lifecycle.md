# 01 - Ciclo De Vida De Simulacion

Estado: Aprobada

## Contexto

El solver es reactivo. Distintos cambios pueden solicitar recalculo en intervalos muy
cortos y algunos calculos posteriores son asincronos. El codigo actual dispara trabajo
en segundo plano, pero varios consumidores continuan como si la simulacion hubiese
terminado.

## Problema Actual

- `RunSimulation` no representa la duracion real del calculo.
- El dominio puede publicar finalizacion antes que el solver.
- Varias simulaciones pueden modificar simultaneamente los mismos objetos.
- La UI, el autosave y la hidratacion observan finales diferentes.
- Los errores se registran, pero no forman un resultado consumible y consistente.

## Comportamiento Deseado

La simulacion debe ser una operacion asincrona coordinada, identificable y esperable.
Todos los consumidores deben observar el mismo inicio y el mismo resultado final.

## Contrato Funcional

Entrada conceptual:

```text
SimulationRequest
- ProjectId
- Revision
- Reason
- RequestedAtUtc
```

Salida conceptual:

```text
SimulationResult
- RunId
- ProjectId
- Revision
- Status: Completed | Failed | Superseded
- StartedAtUtc
- CompletedAtUtc
- Converged
- Diagnostics
```

Estos nombres describen el contrato requerido; no obligan todavia a crear tipos con
esa forma exacta.

## Decisiones Aprobadas M01

1. `Completed` significa que terminaron ecuaciones y post-calculos sin una excepcion
   no controlada. La convergencia se informa por separado mediante `Converged`.
2. Cada ejecucion se identifica como minimo con `RunId`, `ProjectId` y `Revision`.
3. Mientras existe una ejecucion activa, se conserva solamente la revision solicitada
   mas reciente. Las solicitudes intermedias quedan `Superseded`.
4. La coordinacion inicial se serializa por proyecto. No se introduce paralelismo por
   diagrama, equipo o cluster durante este refactor.

Decisiones aprobadas por Alfonso en M01 el 2026-07-14.

## Flujo Normal

1. Un cambio clasificado como `Input`, `Specification` o `Topology` solicita
   simulacion.
2. El coordinador registra la revision solicitada.
3. Si no existe una ejecucion activa, inicia el solver para esa revision.
4. El solver limpia solamente resultados calculados que corresponda recalcular.
5. Ejecuta ecuaciones y calculos posteriores.
6. Produce un unico resultado final.
7. El coordinador publica el nuevo estado a UI y autosave.
8. Si aparecio una revision mas nueva durante la ejecucion, inicia una nueva
   simulacion con la revision mas reciente.
9. Si no hay trabajo pendiente, vuelve a `Idle`.

## Coalescencia

Cuando llegan varias solicitudes durante una simulacion activa:

- no se inicia otro solver en paralelo;
- se conserva la revision mas reciente solicitada;
- al terminar la ejecucion actual se realiza como maximo una nueva ejecucion con el
  estado mas reciente;
- solicitudes intermedias pueden quedar `Superseded`.

## Errores Y No Convergencia

- Una excepcion inesperada produce `Failed` y diagnostico.
- Una solucion numerica que termina sin converger es un resultado valido de ejecucion,
  no una excepcion de control de flujo.
- `Completed` no implica necesariamente `Converged`.
- El evento final se produce exactamente una vez, incluso si fallan los calculos
  posteriores.
- Un fallo no deja el coordinador permanentemente en `Running`.

## Relacion Con La UI

- La UI puede mostrar `Queued`, `Running`, `Failed` o `Completed`.
- La hidratacion permanece activa hasta que termine la simulacion inicial que deba
  esperarse.
- Los componentes no llaman directamente al solver.
- Los componentes no cuentan simulaciones ni interpretan eventos del solver.

## Relacion Con Autosave

- El coordinador de simulacion no ejecuta HTTP directamente.
- El autosave puede observar el resultado para decidir cuando tomar un snapshot
  consistente.
- La intencion valida del usuario no se pierde porque el solver falle.
- Los resultados calculados no se incluyen en el documento persistido por defecto.

## Invariantes

1. Existe como maximo una ejecucion activa por proyecto.
2. Cada ejecucion tiene identidad y revision conocidas.
3. Cada inicio produce exactamente un resultado final.
4. La operacion no termina antes de sus post-calculos.
5. Una revision mas nueva no es reemplazada por un resultado anterior.
6. No convergencia y excepcion son resultados distinguibles.
7. Un fallo siempre libera el estado `Running`.
8. Componentes, hidratacion y autosave observan el mismo ciclo de vida.
9. Una solicitud durante `Running` se combina segun la politica de coalescencia.
10. El solver no se ejecuta directamente desde componentes visuales.

## Criterios De Aceptacion

1. Dos cambios consecutivos nunca ejecutan dos solvers simultaneamente para el mismo
   proyecto.
2. Diez cambios durante una ejecucion producen como maximo una ejecucion adicional.
3. Esperar la operacion de simulacion espera tambien los calculos posteriores.
4. Cada inicio tiene exactamente un resultado final correlacionado.
5. La hidratacion no informa finalizacion antes que el solver inicial.
6. Un fallo permite ejecutar nuevamente el solver.
7. Una finalizacion atrasada no marca como actual una revision mas nueva.
8. El solver conserva la filosofia de retirar trabajo pendiente solo cuando realmente
   converge.

## Pruebas Requeridas

- Ejecucion simple convergente.
- Ejecucion no convergente sin excepcion.
- Excepcion durante ecuaciones.
- Excepcion durante post-calculos.
- Multiples solicitudes mientras esta `Running`.
- Solicitud nueva inmediatamente despues de finalizar.
- Cambio de proyecto mientras existe una ejecucion anterior.
- Confirmacion de que `OnSimulationCompleted` no se duplica.

## Objetivos De Refactor Posteriores

- `Shared/SolverConsecutive/IMainSolver.cs`
- `Shared/SolverConsecutive/MainSolver.cs`
- `Distillator.Domain/Services/ISimulationService.cs`
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`
- Consumidores directos de `RunSimulation()` en componentes.

## Fuera De Alcance

- Cambiar ecuaciones o tolerancias numericas sin una prueba de regresion concreta.
- Ejecutar simultaneamente partes independientes del flowsheet.
- Introducir una cola distribuida.
