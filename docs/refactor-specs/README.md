# Distillator Refactor Specifications

Estado: Borrador activo

## Proposito

Estas especificaciones definen el comportamiento esperado de Distillator antes de
refactorizar su implementacion. El codigo existente se considera una fuente de
conocimiento, pero no la definicion final del comportamiento correcto.

El refactor debe conservar las funcionalidades utiles, corregir contradicciones de
flujo y reducir responsabilidades mezcladas aplicando SOLID, KISS, DRY y YAGNI.

## Reglas De Trabajo

1. Cada spec describe comportamiento observable, invariantes y criterios de
   aceptacion.
2. Una decision de arquitectura solo se agrega cuando sea necesaria para cumplir una
   invariante.
3. No se refactoriza un flujo hasta que su spec tenga las decisiones funcionales
   suficientes.
4. Los resultados calculados por el solver no son la verdad persistida principal.
5. HTTP es la fuente de verdad compartida; SignalR comunica que esa verdad cambio.
6. Los cambios deben poder verificarse con pruebas enfocadas y escenarios de
   regresion.
7. Durante una sustitucion, la implementacion anterior permanece disponible como
   referencia temporal hasta verificar la nueva funcionalidad.

La estrategia incremental y las puertas de aceptacion se definen en
[TEST_PLAN.md](TEST_PLAN.md).

La estructura, herramientas y convenciones propuestas para la suite se definen en
[TEST_CONVENTIONS.md](TEST_CONVENTIONS.md).

La secuencia completa y el estado vivo del refactor se mantienen en
[MASTER_REFACTOR_PLAN.md](MASTER_REFACTOR_PLAN.md). Su actualizacion es obligatoria al
iniciar y cerrar cada unidad.

## Regla De Preservacion Legacy

El objetivo es no perder detalles de una funcionalidad que servia parcialmente antes
de comprobar su reemplazo.

Para cada porcion refactorizada:

1. Registrar el comportamiento actual y su evidencia antes de cambiarlo.
2. Conservar la implementacion anterior comentada y marcada con:

   ```text
   LEGACY - TEMPORARY
   Spec: NN
   Replacement: nombre de la nueva unidad
   Remove after: pruebas y confirmacion de aceptacion
   ```

3. Implementar la nueva ruta sin ejecutar simultaneamente logica legacy y nueva.
4. Comparar resultados esperados, nueva implementacion y referencia anterior.
5. Mantener el bloque legacy mientras la unidad no este `Verificada`.
6. Eliminarlo en una limpieza explicita despues de las pruebas y la confirmacion de
   Alfonso.

Git conserva el historial completo y es la referencia definitiva. El bloque comentado
es una ayuda temporal de consulta durante la transicion, no una segunda
implementacion permanente.

Restricciones:

- no copiar secretos, datos de produccion ni configuracion sensible;
- no dejar bloques legacy sin spec y condicion de eliminacion;
- no mezclar partes de ambas rutas en una misma ejecucion;
- no eliminar la referencia anterior en la misma unidad que introduce el reemplazo;
- si el bloque es demasiado grande para mantener legibilidad, dividir primero la
  sustitucion en unidades mas pequenas.

## Orden De Las Specs

| Orden | Spec | Estado |
|---|---|---|
| 00 | [Fundamentos y vocabulario](00-foundations.md) | Borrador |
| 01 | [Ciclo de vida de simulacion](01-simulation-lifecycle.md) | Borrador |
| 02 | [Input de usuario, solver y autosave](02-user-input-autosave.md) | Borrador |
| 03 | [Carga e hidratacion de proyecto](03-project-loading-hydration.md) | Borrador |
| 04 | [Edicion de flowsheet y topologia](04-flowsheet-editing-topology.md) | Borrador |
| 05 | [Conexiones interdiagrama](05-interdiagram-connections.md) | Borrador |
| 06 | [Persistencia y concurrencia](06-persistence-concurrency.md) | Borrador |
| 07 | [Realtime y presencia](07-realtime-presence.md) | Borrador |
| 08 | [Sesion, permisos y errores](08-session-permissions-errors.md) | Borrador |
| 09 | [Configuracion, unidades y naming](09-project-configuration-units-naming.md) | Borrador |
| 10 | [Formulas y specifications](10-formula-specifications.md) | Borrador |
| 11 | [Contratos de equipos](11-equipment-contracts.md) | Borrador |
| 12 | [Termodinamica y comportamiento numerico](12-thermodynamics-numerical-behavior.md) | Borrador |
| 13 | [Catalogos, administracion y reportes](13-catalogs-administration-reports.md) | Borrador |

## Dependencias De Trabajo

```text
00 Foundations
  -> 01 Simulation
       -> 02 Input and Autosave
       -> 03 Loading and Hydration
       -> 10 Formula Specifications
       -> 11 Equipment Contracts
       -> 12 Numerical Behavior
  -> 04 Flowsheet Editing
       -> 05 Interdiagram Connections
  -> 06 Persistence and Concurrency
       -> 07 Realtime and Presence
  -> 08 Session and Permissions
  -> 09 Configuration, Units and Naming
  -> 13 Catalogs, Administration and Reports
```

Las dependencias indican que reglas deben estar acordadas antes de implementar otra
spec. No obligan a completar todo el programa antes de iniciar una porcion vertical.

## Estados De Una Spec

- `Borrador`: comportamiento propuesto, pendiente de decisiones funcionales o
  aprobacion.
- `Aprobada`: invariantes y criterios suficientes para implementar.
- `En implementacion`: existe una unidad pequena en desarrollo con pruebas definidas.
- `Verificada`: implementacion y evidencia cumplen la puerta de aceptacion.

## Flujo Rector

```text
User Intent
  -> Validate
  -> Apply to Domain
  -> Classify Change
  -> Schedule Simulation when required
  -> Persist User Intent
  -> Publish Realtime Notification
  -> Other Clients Reload or Reconcile
```

La simulacion y la persistencia pertenecen al mismo flujo coordinado, pero son
responsabilidades diferentes. Un fallo del solver no debe borrar una intencion de
usuario valida, y un guardado no debe publicar resultados transitorios como verdad
principal.

## Fuera De Alcance Inicial

- Reescribir algoritmos termodinamicos que no esten relacionados con un bug
  reproducible.
- Cambiar la experiencia visual sin una necesidad funcional.
- Eliminar codigo legado antes de confirmar que no tiene consumidores activos.
- Introducir mensajeria distribuida, CQRS completo o infraestructura adicional sin
  una necesidad demostrada.
