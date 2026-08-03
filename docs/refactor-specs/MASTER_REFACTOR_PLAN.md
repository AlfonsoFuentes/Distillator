# Plan Maestro Del Refactor

Estado: Activo

Ultima actualizacion: 2026-07-16

## Objetivo

Mantener una fuente de verdad unica y secuencial para el refactor completo de
Distillator. El plan registra que se definio, implemento, probo, confirmo y limpio en
cada porcion funcional.

Actualizar este documento forma parte obligatoria de cada unidad de trabajo.

## Estado Actual

- Aplicacion: apagada en la ultima confirmacion del usuario.
- Cambios de codigo del refactor: ninguno.
- Barrido arquitectonico y funcional: completado.
- Specs 00, 01 y 02: aprobadas en M01.
- Specs 03-13: creadas, en estado Borrador.
- Plan incremental de pruebas: creado.
- Foco actual: M31, persistir orden, pan, zoom y dimensiones.
- Primera unidad ejecutable completada: M04, finalizacion real del solver.

## Estados Generales

| Estado | Significado |
|---|---|
| `Pending` | Aun no iniciada |
| `Review` | Comportamiento o decisiones en revision |
| `Ready` | Spec y pruebas suficientes para comenzar |
| `Active` | Implementacion o verificacion en curso |
| `Blocked` | Existe un impedimento identificado |
| `Verified` | Paso pruebas, confirmacion y limpieza requerida |
| `N/A` | La columna no aplica a esa unidad |

## Estados De Evidencia

Cada unidad usa estas columnas:

- `Baseline`: comportamiento anterior reproducido y registrado.
- `Legacy`: implementacion anterior preservada como `LEGACY - TEMPORARY`.
- `Implementation`: nueva ruta terminada.
- `Auto`: pruebas automaticas requeridas aprobadas.
- `Manual`: comprobacion manual visible confirmada por Alfonso, o `N/A` cuando la unidad solo tiene evidencia automatica.
- `Cleanup`: legacy eliminado y regresiones repetidas.

Valores permitidos en esas columnas:

- `Pending`;
- `Done`;
- `Failed`;
- `N/A`.

`Verified` solo se usa cuando todas las columnas aplicables estan en `Done`.

## Reglas De Actualizacion

1. Actualizar `Ultima actualizacion` cuando cambie cualquier estado.
2. Mantener una sola unidad `Active`, salvo pruebas independientes expresamente
   documentadas.
3. No marcar `Implementation = Done` por tener codigo parcial o solo compilar.
4. No marcar `Auto = Done` sin registrar comandos y resultados.
5. No marcar `Manual = Done` sin comparar resultado esperado y obtenido.
6. No marcar `Cleanup = Done` antes de la confirmacion de Alfonso.
7. El legacy se elimina en una unidad de limpieza separada y se repiten regresiones.
8. Un fallo actualiza la unidad a `Active` o `Blocked`, conserva evidencia y no se
   oculta.
9. Si aparece una funcionalidad no contemplada, se agrega una unidad nueva en la
   posicion logica antes de implementarla.
10. Ningun cambio de codigo empieza sin confirmar que la app esta apagada y obtener
    autorizacion.

## Secuencia Maestra

### Fase 0 - Especificacion Y Preparacion

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M00 | Barrido, catalogo de specs y plan de pruebas | 00-13 | Verified | Done | N/A | Done | Done | N/A | N/A |
| M01 | Revisar y aprobar fundamentos, simulacion e inputs | 00-02 | Verified | N/A | N/A | Done | N/A | Done | N/A |
| M02 | Definir proyecto de pruebas y convenciones | TEST | Verified | Done | N/A | Done | N/A | Done | N/A |
| M03 | Crear infraestructura minima de pruebas | TEST | Verified | Done | Done | Done | Done | Done | N/A |

Resultado de fase:

- vocabulario aprobado;
- resultados esperados iniciales aprobados;
- suite ejecutable disponible;
- linea base del solver reproducible.

### Fase 1 - Ciclo De Simulacion

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M04 | S1: finalizacion real incluye post-calculos | 01 | Verified | Done | Done | Done | Done | N/A | Done |
| M05 | S2: una sola simulacion activa por proyecto | 01 | Verified | Done | Done | Done | Done | N/A | Done |
| M06 | S3: coalescencia de solicitudes | 01 | Verified | Done | Done | Done | Done | N/A | Done |
| M07 | S4-S6: no convergencia, excepcion y revision atrasada | 01,12 | Verified | Done | Done | Done | Done | N/A | Done |
| M08 | Integrar estado de simulacion con hidratacion y UI | 01,03 | Verified | Done | Done | Done | Done | N/A | Done |
| M09 | Limpieza final de rutas legacy de simulacion | 01 | Verified | N/A | Done | N/A | Done | N/A | Done |

Resultado de fase:

- contrato asincrono unico;
- solver no solapado;
- resultado correlacionado por ejecucion y revision;
- UI observa el final real.

### Fase 2 - Inputs, Formulas Y Autosave

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M10 | Comando de input para `Variable<T>` | 02 | Verified | Done | Done | Done | Done | Done | Done |
| M11 | Validacion, unidades y auditoria de inputs | 02,09 | Verified | Done | Done | Done | Done | Done | Done |
| M12 | Composicion completa, incompleta y limpieza | 02,12 | Verified | Done | Done | Done | Done | Done | Done |
| M13 | Crear, editar y eliminar formulas | 10 | Verified | Done | Done | Done | Done | Done | Done |
| M14 | Tres niveles de intento de specifications | 10,12 | Verified | Done | N/A | Done | Done | N/A | N/A |
| M15 | Estado `Dirty` y autosave serializado | 02,06 | Verified | Done | Done | Done | Done | Done | Done |
| M16 | Fallo de solver y fallo HTTP sin perdida de input | 02,06 | Verified | Done | N/A | Done | Done | N/A | N/A |
| M17 | Limpieza legacy de inputs, formulas y autosave | 02,10 | Verified | N/A | Done | N/A | Done | N/A | Done |

Resultado de fase:

- componentes expresan intencion;
- componentes no llaman solver ni HTTP;
- inputs y formulas sobreviven fallos;
- guardados no se aplican fuera de orden.

### Fase 3 - Carga, Hidratacion Y Sesion

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M18 | Extraer mapeo de configuracion y documentos | 03,09 | Verified | Done | Done | Done | Done | N/A | Done |
| M19 | Reconstruir equipos, variables y registry | 03,11 | Verified | Done | Done | Done | Done | N/A | Done |
| M20 | Reconstruir pipes, formulas y solver | 03,04,10 | Verified | Done | Done | Done | Done | N/A | Done |
| M21 | Hidratacion cancelable y publicacion atomica | 03 | Verified | Done | Done | Done | Done | Done | Done |
| M22 | Seleccion explicita de proyecto y diagrama | 08 | Verified | Done | Done | Done | Done | N/A | Done |
| M23 | Logout, sesion expirada y cambio rapido A -> B | 03,08 | Verified | Done | Done | Done | Done | Done | Done |
| M24 | Limpieza legacy de hidratacion y seleccion | 03,08 | Verified | N/A | Done | N/A | Done | N/A | Done |

Resultado de fase:

- proyecto construido en aislamiento;
- carga espera recalculo real;
- respuestas atrasadas se descartan;
- render no produce efectos de sesion.

### Fase 4 - Canvas Y Topologia Local

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M25 | Cambios visuales sin solver | 04 | Verified | Done | Done | Done | Done | Done | Done |
| M26 | Crear y borrar equipos atomicamente | 04,11 | Verified | Done | Done | Done | Done | Done | Done |
| M27 | Conexion directa y desconexion | 04 | Verified | Done | Done | Done | Done | Done | Done |
| M28 | Conexion equipo-equipo con stream intermedio | 04 | Verified | Done | N/A | Done | Done | Done | Done |
| M29 | Mixer y splitter: puertos dinamicos | 04,11 | Verified | Done | Done | Done | Done | Done | Done |
| M30 | Vessel y column: puertos dinamicos | 04,11 | Verified | Done | Done | Done | Done | Done | Done |
| M31 | Persistir orden, pan, zoom y dimensiones | 04,09 | Verified | Done | Done | Done | Done | Done | Done |
| M32 | Limpieza legacy de canvas y topologia local | 04 | Verified | N/A | Done | N/A | Done | N/A | Done |

Resultado de fase:

- comandos topologicos atomicos;
- registry, pipes, puertos y solver consistentes;
- cambios visuales independientes del solver.

### Fase 5 - Conexiones Interdiagrama Y Eliminacion

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M33 | Crear conexion interdiagrama atomica | 05 | Verified | Done | Done | Done | Done | Done | Done |
| M34 | Hidratar conexion interdiagrama | 03,05 | Verified | Done | Done | Done | Done | Done | Done |
| M35 | Desconectar desde cualquiera de los extremos | 05 | Verified | Done | Done | Done | Done | Done | Done |
| M36 | Borrar diagrama y limpiar sobrevivientes | 05 | Verified | Done | Done | Done | Done | Done | Done |
| M37 | Persistencia por lote de ambos extremos | 05,06 | Verified | Done | Done | Done | Done | Done | Done |
| M38 | Limpieza legacy interdiagrama | 05 | Verified | N/A | Done | N/A | Done | N/A | Done |

Resultado de fase:

- OPC reciprocos;
- un solo enlace logico de solver;
- ambos diagramas guardados o rechazados juntos;
- eliminaciones sin artefactos huerfanos.

### Fase 6 - Versionado, Conflictos Y Realtime

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M39 | `ExpectedVersion` y rechazo de escritura atrasada | 06 | Verified | Done | Done | Done | Done | Done | Done |
| M40 | Confirmacion HTTP actualiza revision local | 06 | Verified | Done | Done | Done | Done | N/A | Done |
| M41 | Conflicto conserva intencion local | 06,07 | Verified | Done | Done | Done | Done | Done | Done |
| M42 | Auditoria ligera por tipo de cambio | 06 | Verified | Done | Done | Done | Done | N/A | Done |
| M43 | Idempotencia y timeout incierto | 06 | Verified | Done | Done | Done | Done | N/A | Done |
| M44 | Realtime limpio, duplicado y fuera de orden | 07 | Verified | Done | Done | Done | Done | N/A | Done |
| M45 | Realtime con estado local `Dirty` | 06,07 | Verified | Done | Done | Done | Done | Done | Done |
| M46 | Reconexion, eventos perdidos y presencia | 07 | Verified | Done | Done | Done | Done | Done | Done |
| M47 | Limpieza legacy de persistencia y realtime | 06,07 | Verified | N/A | Done | N/A | Done | N/A | Done |

Resultado de fase:

- conflictos detectados;
- ningun overwrite silencioso por version atrasada;
- SignalR notifica commits y HTTP mantiene autoridad;
- reconexion recupera versiones perdidas.

### Fase 7 - Permisos Y Configuracion

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M48 | Owner, Editor y Viewer en comandos y endpoints | 08 | Verified | Done | N/A | Done | Done | Done | N/A |
| M49 | Cambio de rol y retiro de acceso en vivo | 07,08 | Verified | Done | Done | Done | Done | Done | Done |
| M50 | Configuracion como copia temporal y commit unico | 09 | Verified | Done | Done | Done | Done | Done | Done |
| M51 | Metodo termodinamico y elevacion del solver real | 09,12 | Verified | Done | Done | Done | Done | Done | Done |
| M52 | Defaults de unidades y overrides | 09 | Verified | Done | N/A | Done | Done | Done | Done |
| M53 | Naming y migracion atomica | 09 | Verified | Done | Done | Done | Done | Done | Done |
| M54 | Limpieza legacy de permisos y configuracion | 08,09 | Verified | N/A | Done | N/A | Done | N/A | Done |

Resultado de fase:

- permisos coherentes en UI y servidor;
- configuracion aplicada una vez;
- un solo solver por proyecto;
- unidades y nombres sobreviven recarga.

### Fase 8 - Matriz De Equipos Y Regresion Numerica

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M55 | Material Stream | 11,12 | Manual Verified | Done | Pending | Done | Pending | Done | Pending |
| M56 | Pump y Control Valve | 11,12 | Manual Verified | Done | Pending | Done | Pending | Done | Pending |
| M57 | Heat, Plate Exchanger y Reboiler | 11,12 | Manual Verified | Done | Pending | Done | Pending | Done | Pending |
| M58 | Flash Tank: Feed, Vapor y Liquid | 11,12 | Manual Verified | Done | Pending | Done | Pending | Done | Pending |
| M59 | Mixer, Splitter y Vessel | 11,12 | Manual Verified | Done | Pending | Done | Pending | Done | Pending |
| M60 | Column y estrategias asociadas | 11,12 | Manual Verified | Done | Pending | Done | Pending | Done | Pending |
| M61 | Casos fisicos limite y no convergencia | 12 | Manual Verified | Done | Pending | Done | N/A | Done | Pending |
| M62 | Limpieza legacy por equipos | 11,12 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |
| M62A | Auditoria de recalculos innecesarios en UI y comandos | 01,02,10 | Manual Verified | Done | N/A | Done | N/A | Done | Done |

Resultado de fase:

- contrato probado por equipo;
- balances con tolerancias explicitas;
- flujo cero y limites fisicos cubiertos;
- no convergencia distinguible de error.

Nota 2026-07-20: Alfonso confirmo cierre funcional manual de solver de equipos,
`SolverStreamMixer`, persistencia multiusuario, specifications, realtime/autosave y UX
de conexion. Quedan como pendientes de fase las regresiones automaticas rentables y
la limpieza legacy que se decida ejecutar despues.

Validado manualmente 2026-07-21: Alfonso confirmo que cambiar tabs sin edicion no
recalcula, provocar `blur` con el mismo valor no recalcula, y cambiar un valor real
sin presionar Enter antes de cambiar de tab si recalcula. Se aplico idempotencia en
`VariableInputCommandHandler`, `CompositionInputCommandHandler`, caso etanol/agua por
`GL`, cambio de unidad visual y confirmacion de formula sin cambios. Se retiraron los
logs temporales `[SIM-FSM]`, `[SIM-SERVICE]` y `[SIM-MAIN]` usados para validar el
flujo.

### Fase 9 - Funciones De Soporte

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M63 | CRUD de componentes y correlaciones | 13 | Pending | Done | Pending | Done | N/A | Pending | Pending |
| M64 | CRUD de metodos y parametros binarios | 13 | Pending | Done | Pending | Done | N/A | Pending | Pending |
| M65 | Usuarios, roles globales y passwords | 08,13 | Manual Verified | Done | Pending | Done | N/A | Done | Pending |
| M66 | Exportaciones Excel | 13 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M67 | Phase envelope y graficas | 12,13 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M68 | Limpieza legacy de funciones de soporte | 13 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- CRUD y roles implementados;
- componentes/metodos requieren validacion futura con sustancias distintas a agua/etanol;
- exportaciones Excel se dejan para el cierre;
- calculos visuales cancelables y sin mutaciones laterales.

### Fase 10 - Cierre General

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M69 | Eliminar legacy global confirmado y codigo muerto autorizado | Todas | Pending | N/A | Pending | N/A | Pending | Pending | Pending |
| M70 | Build y suite completa sin warnings nuevos | Todas | Pending | N/A | N/A | N/A | Pending | N/A | N/A |
| M71 | Regresion manual de usuario unico | Todas | Pending | N/A | N/A | N/A | N/A | Pending | N/A |
| M72 | Regresion manual multiusuario A <-> B | 05-08 | Pending | N/A | N/A | N/A | N/A | Pending | N/A |
| M73 | Validacion publicada sin F5 | Todas | Pending | N/A | N/A | N/A | N/A | Pending | N/A |
| M74 | Documentacion final y cierre de specs | Todas | Pending | N/A | N/A | Pending | N/A | Pending | N/A |

Resultado de fase:

- suite y build aprobados;
- pruebas manuales confirmadas;
- aplicacion publicada validada;
- specs marcadas `Verificada` con evidencia.

## Registro De Evidencia

Cada unidad activa agrega una entrada resumida:

```text
ID:
Fecha:
Spec:
Objetivo:
Resultado esperado:
Baseline:
Referencia legacy:
Archivos modificados:
Pruebas automaticas:
Resultado automatico:
Prueba manual:
Resultado obtenido:
Confirmacion de Alfonso:
Legacy eliminado:
Regresiones repetidas:
Pendientes:
```

### M01 - Fundamentos, Simulacion E Inputs

```text
ID: M01
Fecha: 2026-07-14
Spec: 00, 01 y 02
Objetivo: aprobar vocabulario y decisiones funcionales previas al refactor.
Resultado esperado: contratos sin ambiguedad para simulacion, autosave y revisiones.
Baseline: contexto y diseno vigente revisados; no aplica ejecucion de la app.
Referencia legacy: N/A, unidad exclusivamente documental.
Archivos modificados: specs 00, 01 y 02; plan maestro.
Pruebas automaticas: N/A.
Resultado automatico: estructura documental, estados y enlaces verificados.
Prueba manual: revision funcional de las decisiones por Alfonso.
Resultado obtenido: paquete de diez decisiones aprobado.
Confirmacion de Alfonso: aprobado el 2026-07-14.
Legacy eliminado: N/A.
Regresiones repetidas: N/A.
Pendientes: M02 cerrada; continuar con M03 bajo autorizacion independiente.
```

### M02 - Definir proyecto de pruebas y convenciones

```text
Fecha: 2026-07-15.
Spec relacionada: TEST_PLAN.md y TEST_CONVENTIONS.md.
Objetivo: definir estructura incremental, convenciones y alcance inicial de pruebas antes de crear infraestructura.
Linea base: solucion .NET 10 sin proyectos de pruebas; escenarios legacy identificados en Shared/SolverQwen.
Legacy conservado: N/A en documentacion; escenarios actuales quedan intactos para migracion gradual.
Implementacion: TEST_CONVENTIONS.md aprobado; referencias agregadas desde README.md y TEST_PLAN.md.
Pruebas automaticas: N/A por alcance documental.
Revision documental: links internos, unidad activa y caracteres no ASCII revisados.
Prueba manual: aprobacion de Alfonso el 2026-07-15.
Cleanup: N/A.
Pendientes: solicitar autorizacion independiente para M03, que crea el primer proyecto de pruebas.
```

### M03 - Crear infraestructura minima de pruebas

```text
ID: M03
Fecha: 2026-07-15
Spec: TEST_PLAN.md y TEST_CONVENTIONS.md
Objetivo: crear el primer proyecto de pruebas ejecutable y migrar una caracterizacion pequena.
Resultado esperado: `dotnet test` descubre y ejecuta pruebas xUnit en `Distillator.Core.Tests`.
Baseline:
- `dotnet --version`: 10.0.301.
- `dotnet test Distillator.slnx --no-restore`: exit 0, sin pruebas descubiertas.
- `Distillator.slnx`: Client, Server, Shared, Distillator.Domain y UnitSystem.
Referencia legacy:
- `Shared/SolverQwen/StreamMixerBalanceRegressionTest.cs` conservado sin cambios.
- Escenario migrado: Case 1, mixer con dos entradas de masa y una salida.
Archivos modificados:
- `Distillator.slnx`.
- `tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj`.
- `tests/Distillator.Core.Tests/Infrastructure/DiscoveryTests.cs`.
- `tests/Distillator.Core.Tests/Solver/StreamMixerBalanceTests.cs`.
- `docs/refactor-specs/TEST_CONVENTIONS.md`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj`.
- `dotnet test Distillator.slnx`.
Resultado automatico:
- Primer intento fallo: salida del mixer 3600 kg/hr porque `RunSimulation()` retorna antes de terminar.
- Correccion dentro de M03: esperar `OnSimulationCompleted` con `TaskCompletionSource` y timeout de 5 s solo anti-bloqueo.
- Segundo intento: 2 pruebas correctas, 0 fallidas.
- Solucion completa: 2 pruebas correctas, 0 fallidas.
Prueba manual: confirmacion de Alfonso; no requiere UI.
Resultado obtenido: infraestructura minima lista y regresion del mixer ejecutable.
Confirmacion de Alfonso: confirmado el 2026-07-15.
Legacy eliminado: no; legacy conservado segun regla `LEGACY - TEMPORARY`.
Regresiones repetidas: `dotnet test Distillator.slnx` correcto.
Pendientes: solicitar autorizacion independiente para M04.
```

### M04 - Finalizacion real incluye post-calculos

```text
ID: M04
Fecha: 2026-07-15
Spec: 01
Objetivo: permitir esperar la simulacion hasta que terminen ecuaciones y post-calculos.
Resultado esperado: `RunSimulationAsync()` no completa mientras un post-calculo siga activo.
Baseline:
- `MainSolver.RunSimulation()` devolvia `void`.
- La unica espera posible era indirecta mediante `OnSimulationCompleted`.
- Prueba inicial fallo con CS1061: `MainSolver` no contenia `RunSimulationAsync`.
Referencia legacy:
- `RunSimulation()` se conserva como wrapper fire-and-forget.
- Implementacion anterior documentada como comentario `LEGACY - TEMPORARY M04`.
Archivos modificados:
- `Shared/SolverConsecutive/IMainSolver.cs`.
- `Shared/SolverConsecutive/MainSolver.cs`.
- `tests/Distillator.Core.Tests/Solver/MainSolverLifecycleTests.cs`.
- `tests/Distillator.Core.Tests/Solver/StreamMixerBalanceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 3 correctas, 0 fallidas.
- Solucion: 3 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; unidad sin comportamiento visible en UI.
Resultado obtenido:
- `RunSimulationAsync()` devuelve una tarea esperable.
- La tarea permanece incompleta mientras `PostSolveAsync()` esta bloqueado.
- Al liberar post-calculo, la tarea completa correctamente y el post-calculo se ejecuta una vez.
Confirmacion de Alfonso: no aplica como confirmacion de exito; se cierra por evidencia automatica segun regla operativa acordada el 2026-07-15.
Legacy eliminado: no; `RunSimulation()` queda para consumidores existentes.
Regresiones repetidas: mixer y discovery tests pasan.
Pendientes: solicitar autorizacion independiente para M05.
```

### M05 - Una sola simulacion activa por proyecto

```text
ID: M05
Fecha: 2026-07-15
Spec: 01
Objetivo: evitar que dos solicitudes entren simultaneamente al solver para la misma instancia/proyecto.
Resultado esperado: una segunda llamada a `RunSimulationAsync()` espera hasta que la primera termine.
Baseline:
- Prueba inicial fallo: al pedir una segunda simulacion con la primera bloqueada, `PostSolveCallCount` llego a 2.
- Evidencia del fallo: se observaban dos post-calculos activos antes de liberar el primero.
Referencia legacy:
- `RunSimulationAsync()` anterior iniciaba `Task.Run(ExecuteSimulationAsync)` por cada llamada.
- Referencia conservada en comentario `LEGACY - TEMPORARY M05`.
Archivos modificados:
- `Shared/SolverConsecutive/MainSolver.cs`.
- `tests/Distillator.Core.Tests/Solver/MainSolverLifecycleTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 4 correctas, 0 fallidas.
- Solucion: 4 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; unidad sin comportamiento visible en UI.
Resultado obtenido:
- `SemaphoreSlim` serializa `RunSimulationAsync()` por instancia/proyecto.
- La segunda simulacion queda pendiente mientras el post-calculo de la primera esta bloqueado.
- No hay mas de un `PostSolveAsync()` activo al mismo tiempo.
Confirmacion de Alfonso: no aplica como confirmacion de exito; se cierra por evidencia automatica segun regla operativa acordada el 2026-07-15.
Legacy eliminado: no; queda para limpieza posterior.
Regresiones repetidas: ciclo de vida, mixer y discovery tests pasan.
Pendientes: solicitar autorizacion independiente para M06.
```

### M06 - Coalescencia de solicitudes

```text
ID: M06
Fecha: 2026-07-15
Spec: 01
Objetivo: compactar varias solicitudes recibidas durante `Running` en una sola repeticion.
Resultado esperado: diez solicitudes durante la primera ejecucion producen dos ejecuciones en total.
Baseline:
- Prueba inicial fallo por timeout.
- Comportamiento observado: M05 serializaba solicitudes, pero podia dejar varias ejecuciones pendientes una por una.
Referencia legacy:
- La ruta M05 con serializacion lineal queda documentada como `LEGACY - TEMPORARY M06`.
Archivos modificados:
- `Shared/SolverConsecutive/MainSolver.cs`.
- `tests/Distillator.Core.Tests/Solver/MainSolverLifecycleTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 5 correctas, 0 fallidas.
- Solucion: 5 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; unidad sin comportamiento visible en UI.
Resultado obtenido:
- La primera solicitud completa al terminar su propia ejecucion.
- Las solicitudes recibidas durante `Running` comparten una unica repeticion pendiente.
- Diez solicitudes adicionales producen solo una segunda ejecucion.
- No hay solapamiento de `PostSolveAsync()`.
Confirmacion de Alfonso: no aplica como confirmacion de exito; se cierra por evidencia automatica segun regla operativa acordada el 2026-07-15.
Legacy eliminado: no; queda para limpieza posterior.
Regresiones repetidas: ciclo de vida, no solapamiento, mixer y discovery tests pasan.
Pendientes: solicitar autorizacion independiente para M07.
```

### M07 - No convergencia, excepcion y revision atrasada

```text
ID: M07
Fecha: 2026-07-15
Spec: 01, 12
Objetivo: devolver un resultado explicito para distinguir ejecucion completada, fallo y ejecucion superada.
Resultado esperado: no convergencia no es excepcion; excepcion produce `Failed`; una ejecucion atrasada queda `Superseded`.
Baseline:
- Prueba inicial fallo en compilacion: `RunSimulationAsync()` devolvia `Task` sin resultado.
- No existia `SimulationRunStatus`.
Referencia legacy:
- Contrato anterior `Task RunSimulationAsync()` conservado como referencia en comentario `LEGACY - TEMPORARY M07`.
Archivos modificados:
- `Shared/SolverConsecutive/SimulationRunResult.cs`.
- `Shared/SolverConsecutive/IMainSolver.cs`.
- `Shared/SolverConsecutive/MainSolver.cs`.
- `tests/Distillator.Core.Tests/Solver/MainSolverLifecycleTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 8 correctas, 0 fallidas.
- Solucion: 8 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; unidad sin comportamiento visible en UI.
Resultado obtenido:
- `SimulationRunResult` distingue `Completed`, `Failed` y `Superseded`.
- `Converged=false` puede existir con `Status=Completed`.
- Fallos de post-calculo producen `Status=Failed` con diagnostico.
- Si llega una solicitud mas nueva durante `Running`, el resultado anterior queda `Superseded`.
Confirmacion de Alfonso: no aplica como confirmacion de exito; se cierra por evidencia automatica segun regla operativa acordada el 2026-07-15.
Legacy eliminado: no; queda para limpieza posterior.
Regresiones repetidas: ciclo de vida, no solapamiento, coalescencia, mixer y discovery tests pasan.
Pendientes: solicitar autorizacion independiente para M08.
```

### M08 - Integrar estado de simulacion con hidratacion y UI

```text
ID: M08
Fecha: 2026-07-15
Spec: 01, 03
Objetivo: evitar que dominio, hidratacion y estado visible publiquen finalizacion antes del resultado real del solver.
Resultado esperado: `SimulationService.RunSimulationAsync` espera el solver y expone `LastSimulationResult`.
Baseline:
- Prueba inicial fallo en compilacion: no existian `RunSimulationAsync` ni `LastSimulationResult` en `SimulationService`.
- `ProjectSessionService` llamaba `project.RunSimulation()` durante hidratacion sin esperarlo.
- `FlowsheetManager` cerraba estado visual mediante evento global, incompatible con solicitudes coalescidas.
Referencia legacy:
- `SimulationService.RunSimulation` queda como wrapper `LEGACY - TEMPORARY M08`.
- Linea anterior `project.RunSimulation()` queda comentada en hidratacion.
- `OnSimulationCompleted` queda como refresco liviano; cierre visual migra a la tarea solicitada.
Archivos modificados:
- `Distillator.Domain/Services/ISimulationService.cs`.
- `Distillator.Domain/Models/IProject.cs`.
- `Distillator.Domain/Models/Project.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`.
- `tests/Distillator.Core.Tests/Simulation/SimulationServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 10 correctas, 0 fallidas.
- Solucion: 10 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; no se levanto la app en esta unidad.
Resultado obtenido:
- `SimulationCompletedEvent` se publica despues de que termina el solver.
- `LastSimulationResult` refleja el resultado observable.
- Fallo del solver publica `SimulationFailedEvent`, conserva diagnostico y tambien produce cierre observable.
- Hidratacion espera `project.RunSimulationAsync()`.
- FlowsheetManager cierra estado visual segun la tarea real iniciada por UI, no por conteo de eventos globales.
Confirmacion de Alfonso: no aplica como confirmacion de exito; se cierra por evidencia automatica segun regla operativa acordada el 2026-07-15.
Legacy eliminado: no; queda para M09.
Regresiones repetidas: ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: solicitar autorizacion independiente para M09.
```

### M09 - Limpieza final de rutas legacy de simulacion

```text
ID: M09
Fecha: 2026-07-18
Spec: 01
Objetivo: eliminar legacy temporal de simulacion M04-M08 despues de validacion funcional.
Baseline:
- M04-M08 estaban verificados y conservaban comentarios legacy temporales en `MainSolver`, `SimulationService`, `FlowsheetManager` y la hidratacion de proyecto.
- `RunSimulation()` sigue siendo una API publica wrapper; no se elimina para no romper consumidores fuera del alcance.
Referencia legacy:
- Eliminada para M04-M08.
Archivos modificados:
- `Shared/SolverConsecutive/MainSolver.cs`.
- `Distillator.Domain/Services/ISimulationService.cs`.
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `rg -n "LEGACY - TEMPORARY M0[4-8]" Client Server Shared Distillator.Domain -g "*.cs" -g "*.razor"` -> sin coincidencias.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "MainSolverLifecycleTests|SimulationServiceTests|StreamMixerBalanceTests|MainSolverSpecificationPlanTests" --no-restore /p:UseSharedCompilation=false` -> OK, 15/15.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 147/147.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual: N/A; M09 solo elimina legacy temporal ya validado en M04-M08.
Cleanup: Done.
Pendientes: siguiente limpieza en orden, M17.
```

### M10 - Comando de input para `Variable<T>`

```text
ID: M10
Fecha: 2026-07-15
Spec: 02
Objetivo: introducir una ruta unica y testeable para aplicar o limpiar inputs de `Variable<T>`.
Resultado esperado: los editores de variables delegan la mutacion de dominio a un comando y conservan la solicitud de simulacion.
Baseline:
- `UIVariableBase<T>`, `UIVariablePercentageBase` y `UIVariableUnitLessBase` mutaban `Variable<T>` directamente.
- La UI modificaba la instancia existente de `Amount` antes de llamar `SetValueFromUI`.
- La UI limpiaba con `ClearFromUI` y disparaba `FlowsheetManager.RunSimulation()` dentro del componente.
Referencia legacy:
- Las rutas anteriores de set y clear quedan comentadas como `LEGACY - TEMPORARY M10` en los tres editores.
Archivos modificados:
- `Distillator.Domain/Inputs/VariableInputCommand.cs`.
- `Client/Program.cs`.
- `Client/Templates/Units/UIVariable/UIVariableBase.cs`.
- `Client/Templates/Units/UIVariablePercentage/UIVariablePercentageBase.cs`.
- `Client/Templates/Units/UIVariableUnitLess/UIVariableUnitLessBase.cs`.
- `tests/Distillator.Core.Tests/Inputs/VariableInputCommandTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 13 correctas, 0 fallidas.
- Solucion: 13 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso edito varios valores en una columna completa y luego valores en una corriente suelta.
Resultado obtenido:
- `VariableInputCommandHandler` aplica valores tipados sin mutar previamente la instancia vieja de `Amount`.
- El clear captura si debe simular antes de dejar la variable `Undefined`.
- Los editores usan el comando para aplicar o limpiar inputs.
- Se corrigio el input adimensional para usar `UnitLessUnits.None`.
Confirmacion de Alfonso: OK; funciono bien, sin comportamientos raros ni avisos raros. Prueba posterior de `Delete` tambien OK.
Legacy eliminado: no; queda para M17 despues de validacion manual.
Regresiones repetidas: comando de input, ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: limpieza legacy de inputs en M17.
```

### M11 - Validacion, unidades y auditoria de inputs

```text
ID: M11
Fecha: 2026-07-15
Spec: 02, 09
Objetivo: rechazar valores numericos invalidos y unidades incompatibles antes de mutar `Variable<T>`.
Resultado esperado: un input invalido no cambia dominio, no solicita simulacion y conserva auditoria anterior.
Baseline:
- El comando de M10 aceptaba cualquier `double` y unidad que llegara desde UI.
- La validacion de texto invalido vivia en el parseo del componente.
- No existia resultado explicito de rechazo en el comando de input.
Referencia legacy:
- La ruta anterior de aplicar sin validar queda comentada como `LEGACY - TEMPORARY M11` en `VariableInputCommandHandler`.
Archivos modificados:
- `Distillator.Domain/Inputs/VariableInputCommand.cs`.
- `tests/Distillator.Core.Tests/Inputs/VariableInputCommandTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 16 correctas, 0 fallidas.
- Solucion: 16 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso introdujo texto invalido en varias variables; el valor no cambio.
- Luego introdujo valores validos y funcionaron correctamente.
Resultado obtenido:
- `VariableInputCommandResult` distingue `Rejected` y expone `ErrorMessage`.
- `NaN`/infinito se rechazan sin mutar variable ni pedir simulacion.
- Unidades incompatibles se rechazan sin mutar variable ni pedir simulacion.
- Unidades validas por conversion o familia de magnitud se aceptan, incluyendo temperatura.
Confirmacion de Alfonso: OK; no vio errores raros.
Legacy eliminado: no; queda para M17 despues de validacion manual.
Regresiones repetidas: comando de input, ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: limpieza legacy de inputs en M17.
```

### M12 - Composicion completa, incompleta y limpieza

```text
ID: M12
Fecha: 2026-07-15
Spec: 02, 12
Objetivo: centralizar la logica de composicion completa, incompleta y limpieza.
Resultado esperado: composicion parcial aplica la intencion pero no solicita simulacion; composicion completa solicita simulacion; valores invalidos no mutan; limpiar composicion recalcula si habia input.
Baseline:
- `UICompositionGridBase` y `EquipmentBaseCompositionGrid` duplicaban set/clear de fracciones.
- La decision de simular usaba suma 99-101 sin exigir que todos los componentes estuvieran definidos.
- La ruta de °GL mutaba etanol/agua directamente y simulaba siempre.
Referencia legacy:
- Las rutas anteriores de set/clear quedan comentadas como `LEGACY - TEMPORARY M12` en las grillas.
Archivos modificados:
- `Distillator.Domain/Inputs/CompositionInputCommand.cs`.
- `Client/Program.cs`.
- `Client/Pages/UnitOperations/MaterialStreams/CompositionGrids/UICompositionGridBase.cs`.
- `Client/Pages/UnitOperations/MaterialStreams/CompositionGrids/CompositionGrid.razor`.
- `Client/Pages/UnitOperations/DialogBase/EquipmentBaseCompositionGrid.razor.cs`.
- `tests/Distillator.Core.Tests/Inputs/CompositionInputCommandTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 23 correctas, 0 fallidas.
- Solucion: 23 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso valido composicion completa, composicion parcial, ocultamiento de base opuesta vieja y recalculo equivalente posterior.
Resultado obtenido:
- `CompositionInputCommandHandler` aplica fracciones masicas/molares con auditoria via `VariableInputCommandHandler`.
- Una composicion parcial no solicita simulacion aunque la suma accidental pueda ser 100.
- Una composicion completa exige todos los componentes definidos y suma 100 con tolerancia.
- Valores fuera de rango se rechazan sin mutar.
- °GL etanol/agua usa la misma ruta y queda completa cuando define ambos extremos.
- Limpiar composicion borra fracciones/flujos via `CompositionOrchestrator.Clear()` y solicita simulacion si habia input.
- Correccion posterior revisada: no se borra la base opuesta del dominio; se oculta visualmente mientras la composicion activa esta incompleta.
- Cuando la composicion queda completa, se conserva la base opuesta para que el solver refresque y muestre el calculo equivalente.
Confirmacion de Alfonso: OK; funciono perfecto despues de corregir la regla visual/dominio.
Legacy eliminado: no; queda para M17 despues de validacion manual.
Regresiones repetidas: composicion, input variable, ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: limpieza legacy de inputs/composicion en M17.
```

### M13 - Crear, editar y eliminar formulas

```text
ID: M13
Fecha: 2026-07-15
Spec: 10
Objetivo: centralizar crear, editar y eliminar formulas en un comando testeable.
Resultado esperado: crear agrega una specification, editar conserva identidad, eliminar retira por identidad y cada cambio solicita simulacion una vez.
Baseline:
- `EquipmentBaseFormulaSpecifications` removia/agregaba `FormulaSpecification` directamente.
- La UI asignaba auditoria y llamaba `FSM.RunSimulation()`.
- No existia resultado unico para crear/editar/eliminar formula.
Referencia legacy:
- Las rutas anteriores de remove/add quedan comentadas como `LEGACY - TEMPORARY M13` en la UI.
Archivos modificados:
- `Distillator.Domain/Inputs/FormulaSpecificationCommand.cs`.
- `Client/Program.cs`.
- `Client/Pages/UnitOperations/DialogBase/EquipmentBaseFormulaSpecifications.razor`.
- `tests/Distillator.Core.Tests/Inputs/FormulaSpecificationCommandTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 29 correctas, 0 fallidas.
- Solucion: 29 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso creo una formula valida, la edito sin duplicados, la elimino y verifico que una formula invalida no se agrega.
Resultado obtenido:
- `FormulaSpecificationCommandHandler` crea formulas con auditoria.
- Editar conserva `Id` y reemplaza una sola specification.
- Eliminar retira la formula existente por identidad.
- Intentar eliminar una formula inexistente se rechaza sin solicitar simulacion.
Confirmacion de Alfonso: OK; no vio errores raros.
Legacy eliminado: no; queda para M17 despues de validacion manual.
Regresiones repetidas: formulas, composicion, input variable, ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: limpieza legacy de formulas en M17.
```

### M14 - Tres niveles de intento de specifications

```text
ID: M14
Fecha: 2026-07-15
Spec: 10, 12
Objetivo: proteger por prueba el orden de los tres intentos de specifications en el solver.
Resultado esperado: el plan contiene specification suelta, specification + equipo semilla y specification + equipos conectados inmediatos, en ese orden.
Baseline:
- `MainSolver.BuildFullSolvePlan()` ya llamaba los tres niveles, pero no existia prueba que protegiera orden ni presencia.
Referencia legacy:
- N/A; no se cambio logica productiva del solver.
Archivos modificados:
- `tests/Distillator.Core.Tests/Solver/MainSolverSpecificationPlanTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Proyecto de pruebas: 30 correctas, 0 fallidas.
- Solucion: 30 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; unidad sin comportamiento visible nuevo en UI.
Resultado obtenido:
- El primer intento es `SpecificationEquation` suelta.
- El segundo intento es cluster con ecuaciones del equipo semilla y la specification.
- El tercer intento es cluster con ecuaciones del equipo semilla, equipos conectados inmediatos y la specification.
Confirmacion de Alfonso: no aplica como confirmacion de exito; se cierra por evidencia automatica segun regla operativa acordada el 2026-07-15.
Legacy eliminado: N/A.
Regresiones repetidas: plan de specifications, formulas, composicion, input variable, ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: solicitar autorizacion independiente para M15.
```

### M15 - Estado Dirty y autosave serializado

```text
ID: M15
Fecha: 2026-07-15
Spec: 02, 06
Objetivo: introducir estado Dirty/Saving/Clean y serializar autosaves para que una respuesta vieja no limpie cambios posteriores.
Resultado esperado:
- Un cambio valido marca una revision Dirty antes del HTTP.
- Existe como maximo un guardado activo por cliente/proyecto.
- Si llega un cambio durante Saving, se persiste luego la revision mas reciente.
- Un fallo HTTP deja la revision Dirty para reintento.
Baseline:
- `PersistDiagramUpdatedAsync` enviaba `UpdateDiagramRequest` directamente.
- `PersistDiagramVisualStateAsync` y `PersistDiagramVisualStatesAsync` serializaban con `_visualPersistenceLock`, pero no tenian revision Dirty/Clean ni proteccion explicita contra confirmar una revision vieja como limpia.
Referencia legacy:
- Rutas anteriores preservadas como comentarios `LEGACY - TEMPORARY M15` en `ProjectSessionService`.
- `_visualPersistenceLock` se conserva temporalmente como respaldo alrededor del HTTP hasta la limpieza M17.
Archivos modificados:
- `Distillator.Domain/Inputs/ProjectAutosaveCoordinator.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Inputs/ProjectAutosaveCoordinatorTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter ProjectAutosaveCoordinatorTests`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M15: 3 correctas, 0 fallidas.
- Proyecto de pruebas: 33 correctas, 0 fallidas.
- Solucion: 33 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso edito variables rapido, refresco/reabrio y valido persistencia del ultimo valor.
- Alfonso valido cambio visual y persistencia posterior.
Resultado obtenido:
- `ProjectAutosaveCoordinator<TPayload>` conserva `LatestRevision`, `CleanRevision`, `SavingRevision` y `State`.
- Guardados concurrentes usan el mismo drenaje activo; no se inicia un segundo autosave paralelo.
- Una falla conserva Dirty.
- `ProjectSessionService` crea snapshots DTO antes del HTTP y los encola por revision.
Confirmacion de Alfonso: OK; no volvieron valores viejos ni aparecieron errores raros.
Legacy eliminado: no; queda para M17 despues de validacion manual.
Regresiones repetidas: autosave, formulas, composicion, input variable, ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: limpieza legacy en M17.
```

### M17 - Limpieza legacy de inputs, formulas y autosave

```text
ID: M17
Fecha: 2026-07-18
Spec: 02, 10
Objetivo: eliminar legacy temporal de inputs, composicion, formulas y autosave M10-M16 despues de validacion funcional.
Baseline:
- M10-M16 estaban verificados y conservaban comentarios/rutas legacy temporales en comandos de dominio, editores UI, grillas de composicion, formulas y autosave visual.
- `_visualPersistenceLock` seguia envolviendo el HTTP del autosave visual, aunque `ProjectAutosaveCoordinator.SaveLatestAsync` ya serializa el drenaje por `_drainTask`.
Referencia legacy:
- Eliminada para M10-M16.
Archivos modificados:
- `Distillator.Domain/Inputs/VariableInputCommand.cs`.
- `Distillator.Domain/Inputs/CompositionInputCommand.cs`.
- `Distillator.Domain/Inputs/FormulaSpecificationCommand.cs`.
- `Client/Templates/Units/UIVariable/UIVariableBase.cs`.
- `Client/Templates/Units/UIVariablePercentage/UIVariablePercentageBase.cs`.
- `Client/Templates/Units/UIVariableUnitLess/UIVariableUnitLessBase.cs`.
- `Client/Pages/UnitOperations/MaterialStreams/CompositionGrids/UICompositionGridBase.cs`.
- `Client/Pages/UnitOperations/MaterialStreams/CompositionGrids/CompositionGrid.razor`.
- `Client/Pages/UnitOperations/DialogBase/EquipmentBaseCompositionGrid.razor.cs`.
- `Client/Pages/UnitOperations/DialogBase/EquipmentBaseFormulaSpecifications.razor`.
- `Client/Services/ProjectSessionService.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `rg -n "LEGACY - TEMPORARY M1[0-6]|_visualPersistenceLock" Client Server Shared Distillator.Domain -g "*.cs" -g "*.razor"` -> sin coincidencias.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "VariableInputCommandTests|CompositionInputCommandTests|FormulaSpecificationCommandTests|ProjectAutosaveCoordinatorTests|SimulationServiceTests|MainSolverLifecycleTests" --no-restore /p:UseSharedCompilation=false` -> OK, 39/39.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 147/147.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual: N/A; M17 solo elimina legacy temporal ya validado en M10-M16.
Cleanup: Done.
Pendientes: siguiente limpieza en orden, M24.
```

### M16 - Fallo de solver y fallo HTTP sin perdida de input

```text
ID: M16
Fecha: 2026-07-16
Spec: 02, 06
Objetivo: proteger que una intencion validada no se pierda por fallo del solver o por fallo HTTP/autosave.
Resultado esperado:
- Un input valido permanece aplicado si el solver falla.
- Un fallo HTTP conserva revision Dirty.
- Un autosave fallido puede reintentarse y limpiar la misma revision cuando el backend confirma.
Baseline:
- M07 ya entregaba `SimulationRunResult.Failed` sin lanzar excepcion al flujo superior.
- M15 ya dejaba Dirty cuando el persistidor devolvia `AutosavePersistenceResult.Failure`.
- Faltaban pruebas explicitas de input retenido y reintento despues de fallo/exception del autosave.
Referencia legacy:
- N/A; no se cambio logica productiva en esta unidad, se agregaron pruebas de contrato.
Archivos modificados:
- `tests/Distillator.Core.Tests/Simulation/SimulationServiceTests.cs`.
- `tests/Distillator.Core.Tests/Inputs/ProjectAutosaveCoordinatorTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter "ProjectAutosaveCoordinatorTests|SimulationServiceTests"`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M16 enfocadas: 8 correctas, 0 fallidas.
- Proyecto de pruebas: 36 correctas, 0 fallidas.
- Solucion: 36 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; unidad de contrato sin comportamiento visible nuevo.
Resultado obtenido:
- `RunSimulationAsync_WhenSolverFails_ShouldKeepValidatedInput` confirma que temperatura, auditoria y estado definido por UI sobreviven a fallo controlado del solver.
- `SaveLatestAsync_AfterFailureCanRetrySameDirtyRevision` confirma reintento exitoso de la misma revision Dirty.
- `SaveLatestAsync_WhenPersistenceThrows_ShouldRemainDirtyForRetry` confirma que una excepcion de persistencia no limpia la revision y permite reintento.
Confirmacion de Alfonso: N/A por evidencia automatica.
Legacy eliminado: N/A.
Regresiones repetidas: autosave, simulacion, formulas, composicion, input variable, ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: M17 limpia rutas legacy de inputs, formulas y autosave con regresiones.
```

### M18 - Extraer mapeo de configuracion y documentos

```text
ID: M18
Fecha: 2026-07-16
Spec: 03, 09
Objetivo: separar el mapeo de configuracion/documentos de `ProjectSessionService` sin cambiar todavia la hidratacion profunda.
Resultado esperado:
- La configuracion de proyecto se serializa/deserializa desde una clase dedicada.
- Los DTO de diagramas y `CanvasStateJson` se construyen desde un mapper dedicado.
- `ProjectSessionService` conserva orquestacion de sesion, HTTP e hidratacion.
Baseline:
- `ProjectSessionService` contenia mapeo de `ProjectBasicConfigurationDto`, snapshots de unidades/configuracion y construccion de `ProjectDiagramDto`.
- El mapeo aceptaba JSON parcial pero podia depender de defaults implicitos dentro del servicio.
Referencia legacy:
- Rutas anteriores preservadas como `LEGACY - TEMPORARY M18` en los wrappers de `ProjectSessionService`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Persistence/ProjectConfigurationPersistenceMapper.cs`.
- `Client/Services/ProjectDiagramDocumentMapper.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Persistence/ProjectConfigurationPersistenceMapperTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter ProjectConfigurationPersistenceMapperTests`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M18 enfocadas: 2 correctas, 0 fallidas.
- Proyecto de pruebas: 38 correctas, 0 fallidas.
- Solucion: 38 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; unidad de refactor interno sin comportamiento visual nuevo.
Resultado obtenido:
- `ProjectConfigurationPersistenceMapper` roundtrip de configuracion, unidades, camara, naming y elevacion.
- Unidad de elevacion desconocida usa fallback `Meter` probado.
- `ProjectDiagramDocumentMapper` concentra serializacion de `ProjectDiagramDto` y canvas snapshot para persistencia.
- `ProjectSessionService` delega mapeo y conserva hidratacion/restauracion para M19-M20.
Confirmacion de Alfonso: N/A por evidencia automatica.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Regresiones repetidas: configuracion, autosave, simulacion, formulas, composicion, input variable, ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: M19 reconstruye equipos, variables y registry.
```

### M19 - Reconstruir equipos, variables y registry

```text
ID: M19
Fecha: 2026-07-16
Spec: 03, 11
Objetivo: centralizar el registro de equipos hidratados en registry y solver, evitando duplicados.
Resultado esperado:
- Un elemento hidratado entra una sola vez al `EquipmentRegistry`.
- Streams se agregan una sola vez a `Solver.Streams`.
- Equipos calculables se agregan una sola vez a `Solver.Equipments`.
- `ProjectSessionService` no decide directamente como registrar facades en solver.
Baseline:
- `ApplyCanvasState` llamaba `RegisterCanvasElementInSolver(project, element)` y luego `project.AddEquipment(element)`.
- El registro en solver estaba dentro de `ProjectSessionService`.
- `MainSolver.AddStream/AddEquipment` no impide duplicados por si mismo.
Referencia legacy:
- Las llamadas anteriores quedan comentadas como `LEGACY - TEMPORARY M19`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Services/ProjectEquipmentHydrationRegistry.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Hydration/ProjectEquipmentHydrationRegistryTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter ProjectEquipmentHydrationRegistryTests`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M19 enfocadas: 2 correctas, 0 fallidas.
- Proyecto de pruebas: 40 correctas, 0 fallidas.
- Solucion: 40 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; unidad interna de hidratacion parcial. La prueba manual de carga completa queda para M20/M21.
Resultado obtenido:
- `ProjectEquipmentHydrationRegistry.TryRegister` registra streams y equipos en registry/solver de forma idempotente.
- Si el mismo elemento aparece dos veces en la hidratacion, el segundo intento se rechaza y no duplica solver.
- `ApplyCanvasState` delega el registro y conserva el resto de restauracion para M20.
Confirmacion de Alfonso: N/A por evidencia automatica.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Regresiones repetidas: hidratacion registry, configuracion, autosave, simulacion, formulas, composicion, input variable, ciclo de simulacion, servicio de simulacion, mixer y discovery tests pasan.
Pendientes: M20 reconstruye pipes, formulas y solver con reglas mas explicitas.
```

### M20 - Reconstruir pipes, formulas y solver

```text
ID: M20
Fecha: 2026-07-16
Spec: 03, 04, 10
Objetivo: centralizar la reconstruccion de pipes locales y formulas durante hidratacion.
Resultado esperado:
- Un pipe hidratado solo se agrega si ambos extremos existen, los puertos son validos y `Connect` acepta la conexion.
- Un pipe huerfano o con puerto invalido no queda como conexion visual falsa.
- Una formula hidratada se agrega solo si parsea contra streams ya registrados.
- Auditoria de formulas persistidas se conserva.
Baseline:
- `ApplyCanvasState` llamaba `source.Connect(...)` pero agregaba `PipeReference` sin revisar si `Connect` retorno `false`.
- `ProjectSessionService` parseaba y agregaba formulas directamente.
Referencia legacy:
- Rutas anteriores preservadas como comentarios `LEGACY - TEMPORARY M20`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Services/ProjectPipeHydrationService.cs`.
- `Distillator.Domain/Services/ProjectFormulaHydrationService.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Hydration/ProjectPipeHydrationServiceTests.cs`.
- `tests/Distillator.Core.Tests/Hydration/ProjectFormulaHydrationServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter "ProjectPipeHydrationServiceTests|ProjectFormulaHydrationServiceTests|ProjectEquipmentHydrationRegistryTests"`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M20 enfocadas: 6 correctas, 0 fallidas.
- Proyecto de pruebas: 44 correctas, 0 fallidas.
- Solucion: 44 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; unidad interna de hidratacion parcial. La prueba manual completa queda para M21.
Resultado obtenido:
- `ProjectPipeHydrationService.TryRestore` protege endpoints, puertos y resultado de conexion antes de agregar pipe.
- `ProjectFormulaHydrationService.Restore` restaura formulas validas, conserva auditoria y omite formulas invalidas.
- `ApplyCanvasState` delega pipes y formulas a servicios dedicados.
Confirmacion de Alfonso: N/A por evidencia automatica.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Regresiones repetidas: pipes, formulas, registry, configuracion, autosave, simulacion, composicion, input variable, mixer y discovery tests pasan.
Pendientes: M21 hidratacion cancelable y publicacion atomica.
```

### M21 - Hidratacion cancelable y publicacion atomica

```text
ID: M21
Fecha: 2026-07-16
Spec: 03
Objetivo: evitar que una hidratacion atrasada publique un proyecto viejo y centralizar la publicacion de proyecto/flowsheet activo.
Resultado esperado:
- Una carga mas nueva invalida publicaciones anteriores.
- Una hidratacion cuyo proyecto no coincide con la solicitud no se publica.
- `CurrentProject` y `ActiveFlowsheet` se actualizan juntos antes de notificar UI.
Baseline:
- `HandleRealtimeProjectChangedAsync` asignaba `CurrentProject` y `ActiveFlowsheet` directamente despues de hidratar.
- `SetCurrentProjectAsync` tenia su propia logica de publicacion.
Referencia legacy:
- Rutas anteriores preservadas como comentarios `LEGACY - TEMPORARY M21`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Services/ProjectHydrationPublicationGate.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Hydration/ProjectHydrationPublicationGateTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter ProjectHydrationPublicationGateTests`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M21 enfocadas: 3 correctas, 0 fallidas.
- Proyecto de pruebas: 47 correctas, 0 fallidas.
- Solucion: 47 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso creo un segundo proyecto, cambio entre proyectos y valido que no se mezclan elementos.
- En el proyecto con equipos observo delay visible con mensaje `Recalculating simulation` antes de cargar.
Resultado obtenido:
- `ProjectHydrationPublicationGate` permite publicar solo la solicitud vigente y del proyecto correcto.
- Realtime usa compuerta antes de publicar documento recargado o estado de proyecto removido.
- `PublishHydratedProject` centraliza la actualizacion de `CurrentProject` y `ActiveFlowsheet`.
Confirmacion de Alfonso: OK funcional; no aparecen elementos de un proyecto en otro.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Regresiones repetidas: hidratacion, pipes, formulas, registry, configuracion, autosave, simulacion, composicion, input variable, mixer y discovery tests pasan.
Pendientes: evaluar experiencia/performance del recalculo inicial en unidades posteriores de hidratacion/sesion.
```

### M22 - Seleccion explicita de proyecto y diagrama

```text
ID: M22
Fecha: 2026-07-16
Spec: 08
Objetivo: extraer la seleccion inicial y explicita de proyecto/diagrama a una regla testeable.
Resultado esperado:
- Cero proyectos produce sesion sin proyecto activo.
- El ultimo proyecto autorizado se selecciona si existe.
- Si el ultimo proyecto ya no esta disponible, se usa fallback determinista.
- El diagrama activo debe pertenecer al proyecto seleccionado; si no, se usa el primero del proyecto.
Baseline:
- `InitializeFromProjectsAsync` decidia proyecto y diagrama con ramas locales.
- `SetCurrentProjectAsync` elegia el primer flowsheet directamente.
- `SetActiveFlowsheetAsync` asignaba el parametro directo despues de una validacion inline.
Referencia legacy:
- Rutas anteriores preservadas como comentarios `LEGACY - TEMPORARY M22`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Session/ProjectWorkspaceSelectionService.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Session/ProjectWorkspaceSelectionServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter ProjectWorkspaceSelectionServiceTests`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M22 enfocadas: 4 correctas, 0 fallidas.
- Proyecto de pruebas: 51 correctas, 0 fallidas.
- Solucion: 51 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual: N/A; la prueba visible de cambio de proyecto ya fue realizada en M21.
Resultado obtenido:
- `ProjectWorkspaceSelectionService` centraliza seleccion inicial y seleccion por proyecto/diagrama.
- `ProjectSessionService` delega la regla y mantiene persistencia/realtime en el servicio de sesion.
Confirmacion de Alfonso: N/A por evidencia automatica.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Regresiones repetidas: seleccion de sesion, hidratacion, pipes, formulas, registry, configuracion, autosave, simulacion, composicion, input variable, mixer y discovery tests pasan.
Pendientes: M23 cubre logout, sesion expirada y cambio rapido A -> B.
```

### M23 - Logout, sesion expirada y cambio rapido A -> B

```text
ID: M23
Fecha: 2026-07-16
Spec: 03, 08
Objetivo: limpiar estado sensible de sesion al logout/sesion expirada y proteger cambio rapido A -> B.
Resultado esperado:
- Logout limpia proyecto activo, diagrama activo, roles locales y presencia.
- El servicio realtime abandona el proyecto actual.
- Si AuthProvider queda sin usuario, ProjectSessionService limpia estado local.
- Una hidratacion vieja no publica encima de una seleccion mas nueva.
Baseline:
- `MainLayout` limpiaba solo AuthProvider y navegaba a `/login`.
- `ProjectRealtimeService` no exponia salida explicita del proyecto activo.
- `CustomAuthenticationStateProvider.ClearUserInfo()` no notificaba cambio de usuario.
Referencia legacy:
- Ruta anterior preservada como comentario `LEGACY - TEMPORARY M23`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Session/ProjectSessionSnapshot.cs`.
- `Client/Services/ProjectRealtimeService.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `Client/Services/Security/CustomAuthenticationStateProvider.cs`.
- `Client/Layout/MainLayout.razor`.
- `tests/Distillator.Core.Tests/Session/ProjectSessionSnapshotRulesTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter "ProjectSessionSnapshotRulesTests|ProjectHydrationPublicationGateTests"`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M23 enfocadas: 5 correctas, 0 fallidas.
- Proyecto de pruebas: 53 correctas, 0 fallidas.
- Solucion: 53 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso cambio rapido entre proyectos, hizo logout y volvio a entrar.
Resultado obtenido:
- `ClearCurrentSessionAsync` limpia proyecto, diagrama, workspace state, roles locales y presencia.
- Logout llama limpieza de sesion antes de limpiar usuario y navegar a login.
- `ProjectRealtimeService.LeaveCurrentProjectAsync` abandona proyecto y limpia presencia local.
- `ClearUserInfo()` notifica cambio de usuario para limpiar estado local tambien ante sesion expirada.
Confirmacion de Alfonso: OK.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Regresiones repetidas: sesion, seleccion, hidratacion, pipes, formulas, registry, configuracion, autosave, simulacion, composicion, input variable, mixer y discovery tests pasan.
Pendientes: M24 sigue pendiente, pero la limpieza legacy queda bloqueada por decision de Alfonso hasta el final del refactor.
```

### M24 - Limpieza legacy de hidratacion y seleccion

```text
ID: M24
Fecha: 2026-07-18
Spec: 03, 08
Objetivo: eliminar legacy temporal de hidratacion, mapeo y seleccion M18-M23 despues de validacion funcional.
Baseline:
- M18-M23 estaban verificados y conservaban comentarios legacy temporales en `ProjectSessionService`.
- Existian bloques inalcanzables de mapeo M18 detras de `return` y helpers privados de configuracion/canvas sin uso real en la ruta actual.
- Los snapshots de canvas usados por `ApplyCanvasState` siguen siendo necesarios para hidratar diagramas y no se eliminan.
Referencia legacy:
- Eliminada para M18-M23.
Archivos modificados:
- `Client/Services/ProjectSessionService.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Implementacion:
- Se retiraron comentarios `LEGACY - TEMPORARY M18-M23`.
- Se eliminaron bloques inalcanzables M18 de mapeo manual.
- Se eliminaron helpers privados muertos de configuracion/unidades que ya viven en `ProjectConfigurationPersistenceMapper`.
- Se elimino `ToCanvasState` local muerto; la serializacion vigente queda en `ProjectDiagramDocumentMapper`.
- Se conservaron los snapshots de canvas y la ruta vigente de hidratacion.
Pruebas automaticas:
- `rg -n "LEGACY - TEMPORARY M(18|19|20|21|22|23)" Client Server Shared Distillator.Domain -g "*.cs" -g "*.razor"` -> sin coincidencias.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectConfigurationPersistenceMapperTests|ProjectEquipmentHydrationRegistryTests|ProjectPipeHydrationServiceTests|ProjectFormulaHydrationServiceTests|ProjectHydrationPublicationGateTests|ProjectWorkspaceSelectionServiceTests|ProjectSessionSnapshotRulesTests" --no-restore /p:UseSharedCompilation=false` -> OK, 17/17.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 147/147.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual: N/A; M24 solo elimina legacy temporal ya validado en M18-M23.
Cleanup: Done.
Pendientes: siguiente limpieza en orden, M32.
```

### M25 - Cambios visuales sin solver

Estado: Verified.

Linea base registrada:
- Los cambios visuales aceptados en `FlowsheetManager` mezclaban notificacion de UI, guardado visual y, en algunos flujos, decisiones locales sin una politica explicita.
- Las operaciones topologicas ya seguian disparando simulacion mediante `RunSimulation()`.
Referencia legacy:
- Ruta anterior preservada como comentario `LEGACY - TEMPORARY M25`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Policies/FlowsheetEditChangePolicy.cs`.
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`.
- `tests/Distillator.Core.Tests/Topology/FlowsheetEditChangePolicyTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter FlowsheetEditChangePolicyTests`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M25 enfocadas: 4 correctas, 0 fallidas.
- Proyecto de pruebas: 57 correctas, 0 fallidas.
- Solucion: 57 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso valido mover, rotar, pan, zoom y cambios visuales sin recalculo visible ni errores.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M26 cubre crear y borrar equipos atomicamente.

### M26 - Crear y borrar equipos atomicamente

Estado: Verified.

Linea base registrada:
- `FlowsheetManager.AddFromToolbox` creaba elemento, registraba solver, registraba proyecto, creaba referencia visual, simulaba y guardaba desde el mismo metodo.
- `FlowsheetManager.DeleteElement` desconectaba pipes, removia registry, removia referencia visual, reconstruia pipes, simulaba y guardaba paso a paso.
Referencia legacy:
- Rutas anteriores preservadas como comentarios `LEGACY - TEMPORARY M26`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Services/FlowsheetEquipmentEditService.cs`.
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`.
- `tests/Distillator.Core.Tests/Topology/FlowsheetEquipmentEditServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter "FlowsheetEquipmentEditServiceTests|FlowsheetEditChangePolicyTests"`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M26 enfocadas: 7 correctas, 0 fallidas.
- Proyecto de pruebas: 60 correctas, 0 fallidas.
- Solucion: 60 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso valido crear y borrar equipos, incluyendo equipos conectados, sin errores visibles.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M27 cubre conexion directa y desconexion.

### M27 - Conexion directa y desconexion

Estado: Verified.

Linea base registrada:
- `FlowsheetManager.CompleteConnection` llamaba `Connect(...)`, pero simulaba, notificaba y guardaba aunque la conexion retornara `null`.
- `FlowsheetManager.DisconnectEquipmentPort` llamaba `DisconnectPort(...)`, simulaba y guardaba aunque no existiera pipe para desconectar.
Referencia legacy:
- Rutas anteriores preservadas como comentarios `LEGACY - TEMPORARY M27`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Services/FlowsheetConnectionEditService.cs`.
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`.
- `tests/Distillator.Core.Tests/Topology/FlowsheetConnectionEditServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter "FlowsheetConnectionEditServiceTests|FlowsheetEquipmentEditServiceTests|FlowsheetEditChangePolicyTests"`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M27 enfocadas: 11 correctas, 0 fallidas.
- Proyecto de pruebas: 64 correctas, 0 fallidas.
- Solucion: 64 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso valido conexion directa, desconexion e intento invalido sin errores visibles.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M28 cubre conexion equipo-equipo con stream intermedio.

### M28 - Conexion equipo-equipo con stream intermedio

Estado: Verified.

Linea base registrada:
- `ConnectionService.ConnectEquipmentToEquipment` ya contenia la ruta de stream intermedio, pero no existia prueba enfocada que protegiera creacion de stream, dos pipes, registry y solver.
- Un segundo intento sobre los mismos puertos debia quedar rechazado sin duplicar stream intermedio.
Referencia legacy:
- N/A; no fue necesario cambiar codigo de produccion en esta unidad.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `tests/Distillator.Core.Tests/Topology/FlowsheetConnectionEditServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter FlowsheetConnectionEditServiceTests`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M28 enfocadas: 6 correctas, 0 fallidas.
- Proyecto de pruebas: 66 correctas, 0 fallidas.
- Solucion: 66 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso valido conexion equipo-equipo con stream intermedio y recarga sin errores visibles.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M29 cubre mixer y splitter con puertos dinamicos.

### M29 - Mixer y splitter: puertos dinamicos

Estado: Verified.

Linea base registrada:
- `StreamMixerVisualElement` y `SplitterVisualElement` usaban el numero del puerto dinamico (`Inlet_N`/`Outlet_N`) como indice directo de la lista del solver.
- Al desconectar un puerto anterior mientras uno posterior seguia conectado, el puerto visual podia quedar desalineado del facade.
- En la primera validacion manual, el puerto libre quedaba en primer lugar; la regla visual esperada es que el puerto disponible quede siempre de ultimo.
- En la segunda validacion manual, al cerrar y volver a abrir los puertos salian descuadrados por hidratacion incompleta de `Inlet_N`/`Outlet_N`.
Referencia legacy:
- Rutas anteriores preservadas como comentarios `LEGACY - TEMPORARY M29`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Shared/ProcessFlowDiagram/Helpers/StreamMixerVisualElement.cs`.
- `Shared/ProcessFlowDiagram/Helpers/SplitterVisualElement.cs`.
- `Distillator.Domain/Services/ProjectPipeHydrationService.cs`.
- `tests/Distillator.Core.Tests/Topology/DynamicPortContractTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter "DynamicPortContractTests|FlowsheetConnectionEditServiceTests"`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M29 enfocadas: 4 correctas, 0 fallidas.
- Proyecto de pruebas: 70 correctas, 0 fallidas.
- Solucion: 70 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Primera validacion de Alfonso: paso a medias; puerto libre quedaba en primer lugar.
- Segunda validacion de Alfonso: interaccion en vivo paso, pero al reabrir los puertos salian descuadrados.
- Correccion de hidratacion/recarga aplicada.
- Alfonso confirmo que funciono perfecto.
- Alfonso observo lentitud extrema al conectar/desconectar; queda registrado para plan futuro de metricas/performance, fuera de M29.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M30 cubre vessel y column con puertos dinamicos.

### M30 - Vessel y column: puertos dinamicos

Estado: Verified.

Linea base registrada:
- `VesselVisualElement` y `ColumnVisualElement` usaban el numero del puerto dinamico como indice directo de listas del solver.
- La hidratacion podia recibir pipes hacia `Inlet_N`, `Outlet_N`, `Feed_N` o `SideDraw_N` antes de que el puerto dinamico existiera en el equipo recien construido.
Referencia legacy:
- Rutas anteriores preservadas como comentarios `LEGACY - TEMPORARY M30`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Shared/ProcessFlowDiagram/Vessels/VesselVisualElement.cs`.
- `Shared/ProcessFlowDiagram/Columns/ColumnVisualElement.cs`.
- `Distillator.Domain/Services/ProjectPipeHydrationService.cs`.
- `tests/Distillator.Core.Tests/Topology/DynamicPortContractTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore --filter DynamicPortContractTests`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M30 enfocadas: 8 correctas, 0 fallidas.
- Proyecto de pruebas: 74 correctas, 0 fallidas.
- Solucion: 74 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso valido Vessel y Column con puertos dinamicos; funciono perfecto.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M31 cubre persistir orden, pan, zoom y dimensiones.

### M31 - Persistir orden, pan, zoom y dimensiones

Estado: Verified.

Linea base registrada:
- Pan y zoom ya pasaban por `MarkVisualStateChanged`, por tanto guardaban estado sin ejecutar solver.
- `SetContainerDimensions` solo actualizaba layout en memoria y no encolaba persistencia visual.
- `ProcessFlowsheetCanvas` no media dimensiones del contenedor al primer render.
- `ProjectSessionService.ReorderFlowsheet` solo reordenaba diagramas en memoria y disparaba `ProjectChanged`, sin persistir `Order`.
Referencia legacy:
- Ruta anterior de reordenamiento preservada como comentario `LEGACY - TEMPORARY M31`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Client/Services/ProjectWorkspace/FlowsheetCanvasLayoutService.cs`.
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`.
- `Client/Layout/Diagrams/Project/ProcessFlowsheetCanvas.razor`.
- `Client/Services/ProjectSessionService.cs`.
- `Client/Templates/Panels/FlowsheetExplorer.razor`.
- `tests/Distillator.Core.Tests/Topology/ProjectFlowsheetOrderTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "Spec=04|Spec=09"`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas Specs 04/09: 26 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Primera validacion de Alfonso: pan, zoom y dimensiones OK; arrastrar diagramas no funciono.
- Correccion aplicada: `draggable` explicito, destino visual de drop y refresco inmediato antes del autosave.
- Alfonso revalido arrastrar diagramas; funciono OK.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M32 queda bloqueada hasta limpieza legacy final o decision explicita.

### M32 - Limpieza legacy de canvas y topologia local

Estado: Verified.

Linea base registrada:
- M25-M31 estaban verificados y conservaban comentarios legacy temporales en `FlowsheetManager`, `FlowsheetConnectionEditService`, elementos con puertos dinamicos y reordenamiento de diagramas.
- No habia rutas ejecutables viejas que reemplazar; la deuda era documental/codigo comentado.
Referencia legacy:
- Eliminada para M25-M31.
- No se tocaron legacies interdiagrama M33-M38; quedan para M38.
Archivos modificados:
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `Distillator.Domain/Services/FlowsheetConnectionEditService.cs`.
- `Shared/ProcessFlowDiagram/Helpers/StreamMixerVisualElement.cs`.
- `Shared/ProcessFlowDiagram/Helpers/SplitterVisualElement.cs`.
- `Shared/ProcessFlowDiagram/Vessels/VesselVisualElement.cs`.
- `Shared/ProcessFlowDiagram/Columns/ColumnVisualElement.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Implementacion:
- Se retiraron comentarios `LEGACY - TEMPORARY M25-M31`.
- Se mantuvo intacta la logica vigente de cambios visuales, topologia aceptada, puertos dinamicos, reordenamiento y persistencia visual.
Pruebas automaticas:
- `rg -n "LEGACY - TEMPORARY M(25|26|27|28|29|30|31)" Client Server Shared Distillator.Domain -g "*.cs" -g "*.razor"` -> sin coincidencias.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "FlowsheetEditChangePolicyTests|FlowsheetEquipmentEditServiceTests|FlowsheetConnectionEditServiceTests|DynamicPortContractTests|ProjectFlowsheetOrderTests" --no-restore /p:UseSharedCompilation=false` -> OK, 22/22.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 147/147.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual: N/A; M32 solo elimina legacy temporal ya validado en M25-M31.
Cleanup: Done.
Pendientes: siguiente limpieza en orden, M38.

### M33 - Crear conexion interdiagrama atomica

Estado: Verified.

Linea base registrada:
- `InterFlowsheetConnectionService.CreateInterFlowsheetConnection` creaba OPCs, referencias, pipes, ocupacion de puertos, registro en solver y conexion logica en una secuencia de mutaciones.
- La ruta no validaba de entrada que el puerto local estuviera libre, que el stream remoto estuviera en el flowsheet remoto ni que el puerto remoto equivalente estuviera libre.
- Un fallo inesperado durante la secuencia podia dejar artefactos parciales.
Referencia legacy:
- Ruta anterior preservada como comentario `LEGACY - TEMPORARY M33`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Services/IInterFlowsheetConnectionService.cs`.
- `tests/Distillator.Core.Tests/Topology/InterFlowsheetConnectionServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "InterFlowsheetConnectionServiceTests"`.
- `dotnet build Distillator.slnx --no-restore`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
Resultado automatico:
- Pruebas M33 enfocadas: 3 correctas, 0 fallidas.
- Proyecto de pruebas: 78 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso creo conexion interdiagrama; corrientes entre diagramas funcionaron bien.
- Falla adicional detectada fuera de M33: al desconectar, los OPC persistieron despues de cerrar y abrir; queda registrada para M35.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M34 cubre hidratacion de conexion interdiagrama.

### M34 - Hidratar conexion interdiagrama

Estado: Verified.

Linea base registrada:
- `ProjectSessionService` restauraba conexiones interdiagrama con metodos privados.
- La restauracion aceptaba el gemelo por `TargetConnectorId`, pero no exigia reciprocidad completa.
- No habia prueba enfocada para orden inverso, gemelo faltante, gemelo no reciproco o doble restauracion.
Referencia legacy:
- Ruta anterior preservada como comentario `LEGACY - TEMPORARY M34`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Services/ProjectInterFlowsheetConnectionHydrationService.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Hydration/ProjectInterFlowsheetConnectionHydrationServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectInterFlowsheetConnectionHydrationServiceTests"`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M34 enfocadas: 4 correctas, 0 fallidas.
- Proyecto de pruebas: 82 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso confirmo M34 OK.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M35 cubre desconectar desde cualquiera de los extremos y debe corregir los OPC persistidos reportados por Alfonso.

### M35 - Desconectar desde cualquiera de los extremos

Estado: Verified.

Linea base registrada:
- Al desconectar una conexion interdiagrama, los OPC podian quedar en registry/persistencia y reaparecer al cerrar y abrir.
- La desconexion desde el extremo remoto no liberaba el puerto del equipo del diagrama opuesto.
- `FlowsheetManager` guardaba solo el diagrama activo aunque la desconexion afectara dos diagramas.
Referencia legacy:
- Ruta anterior preservada por comentarios `LEGACY - TEMPORARY` de M27/M35 en servicios de conexion.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Services/FlowsheetConnectionEditService.cs`.
- `Distillator.Domain/Services/ISimulationService.cs`.
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`.
- `tests/Distillator.Core.Tests/Topology/InterFlowsheetConnectionServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "InterFlowsheetConnectionServiceTests" --no-restore`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas interdiagrama enfocadas: 5 correctas, 0 fallidas.
- Proyecto de pruebas: 84 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Primera validacion de Alfonso: al desconectar, el OPC quedaba visible sin flecha hasta cambiar de diagrama.
- Correccion visual aplicada: `RebuildPipes` sincroniza `_elements` con las referencias reales del flowsheet y elimina elementos huerfanos.
- Alfonso confirmo que funciono perfecto.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M36 cubre borrar diagrama y limpiar sobrevivientes.

### M36 - Borrar diagrama y limpiar sobrevivientes

Estado: Verified.

Linea base registrada:
- `Project.RemoveFlowsheet` ya intentaba limpiar conexiones interdiagrama antes de borrar.
- `ProjectSessionService.DeleteFlowsheetAsync` persistia los sobrevivientes afectados uno por uno.
- No habia prueba enfocada que garantizara que al borrar cualquiera de los dos diagramas el sobreviviente queda sin OPC, pipes ni puerto ocupado.
Referencia legacy:
- Ruta anterior preservada como comentario `LEGACY - TEMPORARY M36`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Topology/InterFlowsheetConnectionServiceTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "InterFlowsheetConnectionServiceTests" --no-restore`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas interdiagrama enfocadas: 7 correctas, 0 fallidas.
- Proyecto de pruebas: 86 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso confirmo que funciono perfecto.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M37 cubre persistencia por lote de ambos extremos.

### M37 - Persistencia por lote de ambos extremos

Estado: Verified.

Linea base registrada:
- La creacion interdiagrama ya llamaba `QueueVisualStateChanged(_flowsheet, streamFlowsheet)`.
- La desconexion interdiagrama ya reportaba ambos diagramas afectados para persistencia.
- El borrado de diagrama ya persistia sobrevivientes afectados mediante `PersistDiagramVisualStatesAsync`.
- El endpoint `UpdateDiagramsRequest` ya existia como operacion batch.
Referencia legacy:
- Rutas anteriores preservadas por comentarios `LEGACY - TEMPORARY` de M15/M36.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `tests/Distillator.Core.Tests/Inputs/ProjectAutosaveCoordinatorTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectAutosaveCoordinatorTests" --no-restore`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore`.
- `dotnet build Distillator.slnx --no-restore`.
Resultado automatico:
- Pruebas M37 enfocadas: 7 correctas, 0 fallidas.
- Proyecto de pruebas: 88 correctas, 0 fallidas.
- Build completo: correcto, 0 warnings, 0 errores.
- Nota: una primera ejecucion paralela produjo bloqueo temporal del DLL de pruebas por Windows; se repitio secuencial y paso limpia.
Prueba manual:
- Alfonso confirmo que funciono perfecto.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M38 queda bloqueada hasta limpieza legacy final.

### M38 - Limpieza legacy interdiagrama

Estado: Verified.

Linea base registrada:
- M33-M37 estaban verificados y conservaban comentarios legacy temporales en creacion, hidratacion, desconexion, eliminacion de diagramas y persistencia por lote.
- `ProjectSessionService.RestoreInterFlowsheetConnections` contenia un bloque inalcanzable M34 detras de `return`; la ruta vigente ya delegaba en `ProjectInterFlowsheetConnectionHydrationService`.
Referencia legacy:
- Eliminada para M33-M37.
Archivos modificados:
- `Distillator.Domain/Services/IInterFlowsheetConnectionService.cs`.
- `Distillator.Domain/Services/ISimulationService.cs`.
- `Distillator.Domain/Services/FlowsheetConnectionEditService.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `Client/Services/ProjectWorkspace/FlowsheetManager.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Implementacion:
- Se retiraron comentarios `LEGACY - TEMPORARY M33-M37`.
- Se elimino el bloque inalcanzable M34 de hidratacion anterior en `ProjectSessionService`.
- Se eliminaron helpers privados muertos usados solo por ese bloque inalcanzable.
- Se mantuvo intacta la ruta vigente de creacion atomica, hidratacion reciproca, desconexion, limpieza de OPC huerfanos y persistencia por lote.
Pruebas automaticas:
- `rg -n "LEGACY - TEMPORARY M(33|34|35|36|37)" Client Server Shared Distillator.Domain -g "*.cs" -g "*.razor"` -> sin coincidencias.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "InterFlowsheetConnectionServiceTests|ProjectInterFlowsheetConnectionHydrationServiceTests|ProjectAutosaveCoordinatorTests|ProjectPipeHydrationServiceTests" --no-restore /p:UseSharedCompilation=false` -> OK, 20/20.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 147/147.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual: N/A; M38 solo elimina legacy temporal ya validado en M33-M37.
Cleanup: Done.
Pendientes: validacion futura de componentes/metodos con sustancias distintas a agua/etanol y Excel al cierre.

### M39 - `ExpectedVersion` y rechazo de escritura atrasada

Estado: Verified.

Linea base registrada:
- Los endpoints de persistencia ya incrementaban `Project.Version` al guardar.
- Las confirmaciones HTTP ya devolvian `ProjectDocumentDto` con la version persistida.
- Antes de M39 no se enviaba version esperada en las escrituras, por lo que una escritura atrasada podia llegar al servidor sin contrato explicito de conflicto.
Referencia legacy:
- Se conserva compatibilidad temporal aceptando `ExpectedVersion = null`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Shared/Projects/ProjectPersistenceDtos.cs`.
- `Shared/Projects/ProjectVersionConcurrency.cs`.
- `Server/Entities/Projects/EndPoints/ProjectEndPoint.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Persistence/ProjectVersionConcurrencyTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectVersionConcurrencyTests" --no-restore /p:UseSharedCompilation=false`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false /p:BuildProjectReferences=false /p:OutDir="<TEMP>\\distillator-m39-server-build\\"`.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false /p:OutDir="<TEMP>\\distillator-m39-client-build-full\\"`.
- Correccion post-manual:
  - `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectVersionConcurrencyTests" --no-restore /p:UseSharedCompilation=false`.
  - `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
  - `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false /p:OutDir="<TEMP>\\distillator-m39-client-build-fix\\"`.
- Correccion de actualizacion post-conflicto:
  - `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectVersionConcurrencyTests" --no-restore /p:UseSharedCompilation=false`.
  - `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
  - `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false /p:OutDir="<TEMP>\\distillator-m39-client-build-reload-conflict\\"`.
Resultado automatico:
- Pruebas M39 enfocadas: 5 correctas, 0 fallidas.
- Proyecto de pruebas: 93 correctas, 0 fallidas.
- Build Server temporal: correcto, 0 warnings, 0 errores.
- Build Client temporal: correcto, 0 warnings, 0 errores.
- Nota: `dotnet build Server/Server.csproj --no-restore` contra `bin/Debug` quedo bloqueado por `Microsoft Visual Studio Insiders (9892)` y `Server (21188)` usando DLLs existentes; se valido con salida temporal para no tocar esos procesos.
- Correccion post-manual: pruebas M39 enfocadas 9 correctas; proyecto de pruebas 97 correctas; build Client temporal correcto.
- Correccion de actualizacion post-conflicto: pruebas M39 enfocadas 9 correctas; proyecto de pruebas 97 correctas; build Client temporal correcto.
Prueba manual:
- Primera prueba de Alfonso: el servidor rechazo correctamente la version atrasada, pero el cliente mostro error global por registrar el conflicto esperado con `Console.Error`.
- Segunda prueba de Alfonso: ya no mostro error global, pero las sesiones no actualizaron el estado visible despues de cambios concurrentes.
- Alfonso confirmo que funciono OK: el conflicto recarga el estado mas reciente, no pisa silenciosamente la version mas reciente y no muestra error rojo global.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M40 cubre que la confirmacion HTTP mantenga la revision local actualizada de forma mas completa.

### M40 - Confirmacion HTTP actualiza revision local

Estado: Verified.

Linea base registrada:
- M39 actualizaba `_currentProjectVersion` con la respuesta HTTP exitosa.
- La version usada para filtrar eventos realtime (`_lastAppliedRealtimeVersion`) solo se actualizaba al aplicar una recarga realtime.
- Si el autor recibia luego su propio evento o eventos ya conocidos, no existia una politica explicita y testeable para tratarlos como version ya confirmada por HTTP.
Referencia legacy:
- Ruta anterior preservada por comentario `LEGACY - TEMPORARY M40` en `UpdateConfirmedProjectVersion`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Shared/Projects/ProjectVersionConfirmation.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Persistence/ProjectVersionConfirmationTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectVersionConfirmationTests|ProjectVersionConcurrencyTests" --no-restore /p:UseSharedCompilation=false`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false /p:OutDir="<TEMP>\\distillator-m40-client-build\\"`.
Resultado automatico:
- Pruebas enfocadas de versionado: 13 correctas, 0 fallidas.
- Proyecto de pruebas: 101 correctas, 0 fallidas.
- Build Client temporal: correcto, 0 warnings, 0 errores.
Prueba manual:
- N/A. M40 no introduce flujo visible nuevo; protege la contabilidad local de revision confirmada por HTTP.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M41 cubre conservar intencion local en conflictos de forma mas completa.

### M41 - Conflicto conserva intencion local

Estado: Verified.

Linea base registrada:
- M39 recargaba el proyecto autoritativo cuando el servidor rechazaba un autosave visual por version atrasada.
- Esa recarga protegia el dato remoto, pero descartaba la intencion visual local rechazada.
- La spec permite reaplicar incrementalmente cambios visuales independientes, dejando merge general de topologia e inputs para unidades posteriores.
Referencia legacy:
- Ruta anterior preservada por comentario `LEGACY - TEMPORARY M39/M41`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Shared/Projects/ProjectVisualIntentPolicy.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Persistence/ProjectVisualIntentPolicyTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectVisualIntentPolicyTests|ProjectVersionConfirmationTests|ProjectVersionConcurrencyTests" --no-restore /p:UseSharedCompilation=false`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false /p:OutDir="<TEMP>\\distillator-m41-client-build\\"`.
Resultado automatico:
- Pruebas enfocadas de versionado/conflicto visual: 16 correctas, 0 fallidas.
- Proyecto de pruebas: 104 correctas, 0 fallidas.
- Build Client temporal: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso confirmo que funciono perfecto.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M42 cubre auditoria ligera por tipo de cambio; M45 cubre realtime con estado local Dirty mas amplio.

### M42 - Auditoria ligera por tipo de cambio

Estado: Verified.

Linea base registrada:
- `UpdateDiagramRequest` y `UpdateDiagramsRequest` guardaban `ProjectDiagramDto` completo en `OldValueJson` y `NewValueJson`.
- Para cambios visuales pequeños, eso copiaba `CanvasStateJson` completo en auditoria.
- La spec exige que mover un equipo no copie todo el proyecto/diagrama como auditoria desproporcionada.
Referencia legacy:
- Ruta anterior preservada conceptualmente por el estado Git y por el uso intacto de `ToDiagramDto` para create/delete.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Shared/Projects/ProjectDiagramAuditSummary.cs`.
- `Server/Entities/Projects/EndPoints/ProjectEndPoint.cs`.
- `tests/Distillator.Core.Tests/Persistence/ProjectDiagramAuditTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectDiagramAuditTests" --no-restore /p:UseSharedCompilation=false`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false /p:BuildProjectReferences=false /p:OutDir="<TEMP>\\distillator-m42-server-build\\"`.
Resultado automatico:
- Pruebas M42 enfocadas: 2 correctas, 0 fallidas.
- Proyecto de pruebas: 106 correctas, 0 fallidas.
- Build Server temporal: correcto, 0 warnings, 0 errores.
Prueba manual:
- N/A. M42 modifica auditoria interna de servidor; no introduce flujo visible nuevo.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M43 cubre idempotencia y timeout incierto.

### M43 - Idempotencia y timeout incierto

Estado: Verified.

Linea base registrada:
- Las escrituras de proyecto/diagrama no enviaban `OperationId`.
- Si el servidor guardaba pero el cliente perdia la respuesta por timeout, el reintento podia llegar con `ExpectedVersion` vieja y terminar como conflicto o intentar aplicar otra operacion.
- `ProjectChangeLog` no tenia una clave de operacion para reconocer reintentos ya aplicados.
Referencia legacy:
- Compatibilidad temporal: `OperationId = null` sigue permitido para clientes viejos, pero no tiene garantia de idempotencia.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Shared/Projects/ProjectPersistenceDtos.cs`.
- `Shared/Projects/ProjectOperationId.cs`.
- `Server/Entities/Projects/ProjectChangeLog.cs`.
- `Server/Entities/Projects/Configurations/ProjectChangeLogConfiguration.cs`.
- `Server/Entities/Projects/EndPoints/ProjectEndPoint.cs`.
- `Server/Migrations/20260717215418_AddProjectChangeLogOperationId.cs`.
- `Server/Migrations/20260717215418_AddProjectChangeLogOperationId.Designer.cs`.
- `Server/Migrations/ApplicationDbContextModelSnapshot.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Persistence/ProjectOperationIdTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectOperationIdTests|ProjectVersionConfirmationTests|ProjectVersionConcurrencyTests" --no-restore /p:UseSharedCompilation=false`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false /p:BuildProjectReferences=false /p:OutDir="<TEMP>\\distillator-m43-server-build\\"`.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false /p:OutDir="<TEMP>\\distillator-m43-client-build\\"`.
- `dotnet ef database update --project Server/Server.csproj --startup-project Server/Server.csproj`.
Resultado automatico:
- Pruebas M43 enfocadas: 15 correctas, 0 fallidas.
- Proyecto de pruebas: 108 correctas, 0 fallidas.
- Build Server temporal: correcto, 0 warnings, 0 errores.
- Build Client temporal: correcto, 0 warnings, 0 errores.
- Migracion local aplicada: `20260717215418_AddProjectChangeLogOperationId`.
Prueba manual:
- N/A. M43 protege reintentos por timeout mediante contrato y persistencia; la simulacion manual de timeout queda como escenario de observabilidad futura.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M44 cubre realtime limpio, duplicado y fuera de orden.

### M44 - Realtime limpio, duplicado y fuera de orden

Estado: Verified.

Linea base registrada:
- `HandleRealtimeProjectChangedAsync` ya descartaba versiones conocidas antes de cargar por HTTP.
- Faltaba una politica explicita y testeable para validar que el documento cargado realmente cubre el evento recibido y no es una version vieja o ya conocida.
- Una respuesta HTTP atrasada no debe publicar una version menor que el evento realtime ni reemplazar una version local mas nueva.
Referencia legacy:
- Ruta anterior preservada por el filtro existente y por comentario `LEGACY - TEMPORARY M40`; ahora se centraliza la decision en una politica compartida.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Shared/Projects/ProjectRealtimeVersionPolicy.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Persistence/ProjectRealtimeVersionPolicyTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectRealtimeVersionPolicyTests|ProjectVersionConfirmationTests" --no-restore /p:UseSharedCompilation=false`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false /p:OutDir="<TEMP>\\distillator-m44-client-build\\"`.
Resultado automatico:
- Pruebas M44 enfocadas: 9 correctas, 0 fallidas.
- Proyecto de pruebas: 113 correctas, 0 fallidas.
- Build Client temporal: correcto, 0 warnings, 0 errores.
Prueba manual:
- N/A. M44 cubre politica interna de orden realtime; el flujo multiusuario visible fue validado en M39-M41.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M45 cubre realtime cuando existe estado local `Dirty`.

### M45 - Realtime con estado local `Dirty`

Estado: Verified.

Linea base registrada:
- `HandleRealtimeProjectChangedAsync` recargaba por HTTP cuando llegaba una version remota nueva.
- Si habia autosave visual local `Dirty` o `Saving`, esa recarga podia reemplazar el modelo local antes de que terminara la intencion pendiente.
- La spec exige que realtime no descarte silenciosamente trabajo local `Dirty`.
Referencia legacy:
- Ruta anterior preservada por el flujo M44; M45 agrega compuerta antes de la recarga.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Distillator.Domain/Inputs/ProjectRealtimeDirtyPolicy.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `tests/Distillator.Core.Tests/Inputs/ProjectRealtimeDirtyPolicyTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectRealtimeDirtyPolicyTests|ProjectRealtimeVersionPolicyTests|ProjectAutosaveCoordinatorTests" --no-restore /p:UseSharedCompilation=false`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false /p:OutDir="<TEMP>\\distillator-m45-client-build\\"`.
Resultado automatico:
- Pruebas M45 enfocadas: 15 correctas, 0 fallidas.
- Proyecto de pruebas: 116 correctas, 0 fallidas.
- Build Client temporal: correcto, 0 warnings, 0 errores.
Prueba manual:
- Alfonso confirmo que funciono OK.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes: M46 cubre reconexion, eventos perdidos y presencia.

### M46 - Reconexion, eventos perdidos y presencia

Estado: Verified.

Linea base registrada:
- `ProjectRealtimeService` al reconectar volvia a unir el proyecto y actualizar presencia.
- No avisaba a `ProjectSessionService` para comparar la version local con HTTP.
- Si se perdia un evento durante desconexion, la sesion podia quedar en una version vieja hasta otro cambio o recarga manual.
Referencia legacy:
- Ruta anterior preservada por comentario `LEGACY - TEMPORARY M21/M46`.
- No se elimina ningun legacy temporal por decision de Alfonso.
Archivos modificados:
- `Shared/Projects/ProjectReconnectVersionPolicy.cs`.
- `Client/Services/ProjectRealtimeService.cs`.
- `Client/Services/ProjectSessionService.cs`.
- `Client/Services/ProjectAuthoritativeSyncService.cs`.
- `Distillator.Domain/Inputs/ProjectAuthoritativeSyncPolicy.cs`.
- `Client/Templates/Panels/ProjectDiagram.razor`.
- `Client/Templates/Panels/ProjectExplorer.razor`.
- `Client/wwwroot/networkStatusInterop.js`.
- `Client/wwwroot/index.html`.
- `tests/Distillator.Core.Tests/Inputs/ProjectAuthoritativeSyncPolicyTests.cs`.
- `tests/Distillator.Core.Tests/Persistence/ProjectReconnectVersionPolicyTests.cs`.
- `docs/refactor-specs/MASTER_REFACTOR_PLAN.md`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectReconnectVersionPolicyTests|ProjectRealtimeVersionPolicyTests|ProjectRealtimeDirtyPolicyTests" --no-restore /p:UseSharedCompilation=false`.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false`.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false /p:OutDir="<TEMP>\\distillator-m46-client-build\\"`.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false`.
Resultado automatico:
- Pruebas M46 enfocadas: 11 correctas, 0 fallidas.
- Proyecto de pruebas: 119 correctas, 0 fallidas.
- Build Client temporal: correcto, 0 warnings, 0 errores.
- Correccion visual/reconexion M46: pruebas enfocadas 11 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion auto-reconnect Closed M46: pruebas enfocadas 11 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion browser online M46: pruebas enfocadas 11 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion browser online no bloqueante M46: pruebas enfocadas 11 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion recovery watcher M46: pruebas enfocadas 11 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion refresh canvas remoto M46: pruebas enfocadas 11 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion recovery HTTP autoritativo M46: pruebas enfocadas 11 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion polling autoritativo M46: pruebas enfocadas 11 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion background sync condicionado M46: pruebas enfocadas 18 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion version renderizada M46: pruebas enfocadas 18 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
- Correccion no limpiar proyecto por red M46: pruebas enfocadas 18 correctas, 0 fallidas; build Client correcto, 0 warnings, 0 errores.
Prueba manual:
- Fallida: despues de volver online, B quedo sin lista de proyectos y sin proyecto seleccionado.
- Correccion: una falla remota de `GetUserProjectsRequest` ya no reemplaza la lista real por una lista local vacia; al reconectar se solicita refrescar la lista aunque no haya proyecto activo.
- Fallida parcial: despues de volver online, B conservo proyectos pero no actualizo el proyecto activo hasta cambiar de proyecto.
- Correccion: si SignalR queda `Closed`, `ProjectRealtimeService` reintenta conectar, vuelve a unir el proyecto y dispara el catch-up M46.
- Fallida parcial: despues de volver online, B no actualizo automaticamente; al cambiar de diagrama y volver si aparecio el cambio.
- Correccion: `window.online` ahora notifica a .NET, fuerza recuperacion realtime y dispara catch-up HTTP M46.
- Fallida critica: con A y B online, los cambios dejaron de reflejarse entre sesiones.
- Correccion: el registro JS de `window.online` ahora es opcional y no puede bloquear inicio, union ni eventos SignalR.
- Fallida parcial: online-online funciona, pero al volver B de offline no actualiza hasta cambiar de diagrama.
- Correccion: `ProjectRealtimeService` inicia un watcher cuando SignalR entra en `Reconnecting` o `Closed`; al volver `Connected`, re-une el proyecto y dispara catch-up HTTP.
- Fallida parcial: el cambio remoto se ve al cambiar de diagrama, no inmediatamente en el diagrama activo.
- Correccion: `ProjectDiagram.OnProjectChanged` recarga `FlowsheetManager` dentro de `InvokeAsync`, junto con el render de Blazor.
- Fallida parcial: la recuperacion seguia dependiendo de disparadores realtime/browser y el canvas activo podia quedar atrasado.
- Correccion: una falla de conexion marca recuperacion pendiente; un bucle HTTP reintenta cargar el proyecto activo y publica el documento autoritativo incluso si la version parecia conocida.
- Fallida parcial: `networkStatusInterop.js` no aparecio cargado en el navegador y B siguio necesitando cambio de diagrama.
- Correccion: el proyecto activo ahora tiene polling HTTP autoritativo cada 3 segundos; SignalR queda como canal rapido y polling como reconciliacion para eventos perdidos/offline.
- Fallida UX: polling agresivo podia hidratar repetidamente y afectar la edicion.
- Correccion: polling agresivo reemplazado por `ProjectAuthoritativeSyncService` con politica: solo sincroniza con proyecto activo, Clean, sin hidratacion, sin solver corriendo y sin operacion visual activa.
- Fallida parcial: B recibia `GetProjectRequest 200`, pero no actualizaba el canvas.
- Correccion: background sync compara contra ultima version publicada/renderizada, no contra version realtime conocida.
- Fallida critica: B mostro proyecto en header pero paleta `No project selected`.
- Correccion: un fallo de red en `GetProjectRequest` ya no limpia `CurrentProject`; conserva estado, difiere evento y agenda recuperacion.
- Alfonso valido manualmente que online-online sincroniza y que B offline, A cambia, B online recupera sin cambiar de proyecto ni diagrama. Probado dos veces.
Legacy eliminado: no; queda bloqueado hasta limpieza final.
Pendientes:
- Optimizar `ProjectAuthoritativeSyncService`: el polling cada 3 segundos con `GetProjectRequest` funciona y protege offline-online, pero debe medirse y reducirse con backoff/condiciones para evitar carga innecesaria en UI/backend.

### M47 - Limpieza legacy de persistencia y realtime

Estado: Verified.

Linea base registrada:
- M39-M46 estaban funcionalmente verificados y conservaban comentarios/rutas legacy temporales por decision previa de no limpiar hasta tener validacion manual.
- Las marcas `LEGACY - TEMPORARY M39-M46` se concentraban en `ProjectSessionService`.
Referencia legacy:
- Eliminada para M39-M46.
- No se tocaron legacies de otras unidades: inputs, hidratacion, permisos, configuracion, canvas, interdiagrama, solver ni equipos.
Implementacion:
- Se elimino el fallback comentado M46 que documentaba la consulta local de proyectos tras fallo HTTP.
- Se retiraron comentarios temporales M40 y M39/M41 en confirmacion de version y conflicto visual.
- Se retiro la marca temporal M46 del flujo de publicacion de documentos HTTP/realtime ya consolidado.
- Se mantuvo estable el contrato publico de DTOs para no endurecer API en una unidad de limpieza.
Pruebas automaticas:
- `rg -n "LEGACY - TEMPORARY M(39|40|41|42|43|44|45|46)" Client Server Shared Distillator.Domain -g "*.cs" -g "*.razor"` -> sin coincidencias.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectVersionConcurrencyTests|ProjectVersionConfirmationTests|ProjectVisualIntentPolicyTests|ProjectDiagramAuditTests|ProjectOperationIdTests|ProjectRealtimeVersionPolicyTests|ProjectRealtimeDirtyPolicyTests|ProjectReconnectVersionPolicyTests|ProjectAuthoritativeSyncPolicyTests" --no-restore /p:UseSharedCompilation=false` -> OK, 38/38.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 147/147.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> primer intento fallido por bloqueo de `Client.dll` al correr build en paralelo; repetido secuencial -> OK, 0 errores.
Prueba manual:
- N/A. M47 solo elimina legacy temporal ya validado en M39-M46.
Cleanup:
- Done.

### M48 - Owner, Editor y Viewer en comandos y endpoints

Estado: Verified.

Linea base registrada:
- Servidor valida acceso de lectura con `LoadProjectForUserAsync`.
- Servidor usa `CanEdit` en configuracion, creacion/actualizacion/borrado de diagramas y lotes.
- Servidor usa `IsOwner` en sharing y borrado de proyecto.
- UI usa `CanCurrentUserEditProject` para paleta, render de equipos y dialogos, pero aun deben revisarse comandos publicos de sesion/canvas para evitar mutacion local por Viewer.
Referencia legacy:
- No se elimina ningun legacy temporal por decision de Alfonso.
Prueba esperada:
- Viewer no debe mutar dominio compartido, solver ni persistencia por UI ni por comandos de cliente.
- Editor puede editar contenido, no sharing ni borrar proyecto.
- Owner puede compartir y borrar.
Implementacion:
- Se agrego `ProjectPermissionPolicy` para centralizar la regla Owner/Editor/Viewer sin duplicar strings en cliente.
- `ProjectSessionService` usa la politica para bloquear comandos de configuracion, diagramas, reordenamiento, borrado y autosave cuando el usuario no puede editar.
- `FlowsheetManager` recibe un predicado `CanEditCurrentProject` y rechaza mutaciones visuales, topologicas, solver y helpers programaticos para Viewer.
- `ProjectDiagram` conecta el estado de permisos de la sesion con los comandos del canvas.
Legacy:
- N/A para M48: no se reemplazo una ruta funcional por otra equivalente; se agregaron guardas de permisos. Se conserva todo legacy temporal existente.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectPermissionPolicyTests|ProjectAuthoritativeSyncPolicyTests" --no-restore /p:UseSharedCompilation=false` -> OK, 17/17.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual:
- Alfonso confirmo que Owner, Editor y Viewer funcionaron OK.
Cleanup:
- N/A para M48; no se elimina legacy en esta unidad por decision de conservar referencias hasta la limpieza final.

### M49 - Cambio de rol y retiro de acceso en vivo

Estado: Verified.

Linea base registrada:
- `UpdateProjectSharingRequest` persiste cambios de colaboradores y emite `SharingUpdated` al grupo del proyecto y a los usuarios afectados.
- `HandleRealtimeProjectChangedAsync` ya limpia `CurrentProject` si una recarga HTTP del proyecto activo devuelve documento nulo.
- `ApplyLoadedProjectDocumentAsync` refresca el rol del usuario actual al publicar un documento cargado.
- `ReconcileAuthoritativeProjectAsync` ignora `Document == null`, por lo que el background sync no invalida el proyecto activo cuando se retira acceso y no llega/funciona SignalR.
- La UI queda bloqueada para editar si el rol local cambia a Viewer por M48.
Referencia legacy:
- No se elimina ningun legacy temporal por decision de Alfonso.
Prueba esperada:
- Si Owner cambia Editor -> Viewer en vivo, el usuario afectado conserva el proyecto visible pero queda sin comandos de escritura.
- Si Owner retira acceso en vivo, el usuario afectado sale del proyecto activo y refresca lista de proyectos.
- La recuperacion debe funcionar por SignalR y por reconciliacion HTTP autoritativa cuando SignalR no entregue el evento.
Implementacion:
- Se agrego `ProjectAccessFailurePolicy` para clasificar explicitamente perdida de acceso sin confundirla con conexion, timeout o permisos administrativos.
- `TryLoadProjectDocumentAsync` ahora distingue `AccessDenied` y `ConnectionFailed`.
- `HandleRealtimeProjectChangedAsync` limpia el proyecto activo cuando un evento remoto detecta que el usuario ya no tiene acceso.
- `ReconcileAuthoritativeProjectAsync` aplica la misma limpieza cuando el background sync HTTP detecta retiro de acceso aunque SignalR no entregue el evento.
- La degradacion Editor -> Viewer sigue usando `ApplyLoadedProjectDocumentAsync` para refrescar rol y M48 bloquea los comandos de escritura.
Legacy:
- Se conserva todo legacy temporal existente. Se agrego comentario `LEGACY - TEMPORARY M49` en la ruta antigua que solo limpiaba con documento nulo.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectAccessFailurePolicyTests|ProjectPermissionPolicyTests|ProjectAuthoritativeSyncPolicyTests|ProjectRealtimeVersionPolicyTests|ProjectReconnectVersionPolicyTests" --no-restore /p:UseSharedCompilation=false` -> OK, 29/29.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual:
- Alfonso confirmo que cambio Editor -> Viewer y retiro de acceso en vivo funcionaron perfecto.
Cleanup:
- Pendiente por decision de conservar legacy temporal hasta la limpieza final.

### M50 - Configuracion como copia temporal y commit unico

Estado: Verified.

Linea base registrada:
- `ProjectFormDialog` carga valores del proyecto en campos locales y clona sistemas de unidades/naming para editar.
- Al confirmar migracion de naming, el dialogo asigna `flowsheet.DiagramNumber` antes de guardar.
- Al guardar edicion, el dialogo asigna `Project.Name` antes de llamar a `UpdateProjectConfigurationAsync`.
- `UpdateProjectConfigurationAsync` aplica `CurrentProject.UpdateConfiguration(configuration)` antes de saber si HTTP acepto la revision.
- Luego el dialogo llama `Project.UpdateThermodynamicMethod(...)`, tambien fuera de una confirmacion explicita del guardado.
- Los numeros de diagrama requeridos por naming se persisten en una ruta posterior; M53 tratara la migracion atomica completa.
Referencia legacy:
- No se elimina ningun legacy temporal por decision de Alfonso.
Prueba esperada:
- Cancelar el dialogo no cambia proyecto, metodo, unidades, naming ni numeros de diagrama.
- Si el guardado de configuracion falla, no debe quedar aplicada la configuracion nueva en memoria.
- Si el guardado de configuracion tiene exito, se aplica una sola vez y luego se notifican UI/realtime.
Implementacion:
- `ProjectFormDialog` conserva los cambios de numeros de diagrama como draft hasta confirmar el guardado.
- El dialogo ya no asigna `Project.Name` ni llama `Project.UpdateThermodynamicMethod` directamente.
- `ProjectSessionService.UpdateProjectConfigurationAsync` persiste primero el draft de nombre/configuracion y solo aplica dominio local si HTTP confirma.
- La validacion de numeros de diagrama puede evaluar numeros proyectados sin mutar los flowsheets.
- La propagacion del metodo termodinamico queda dentro del commit confirmado para conservar el comportamiento anterior sin mutacion temprana.
Legacy:
- Se conserva todo legacy temporal existente y se agregan comentarios `LEGACY - TEMPORARY M50`.
- La persistencia atomica completa de migracion de naming queda pendiente para M53, como ya estaba definido en el plan.
Pruebas automaticas:
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectConfigurationPersistenceMapperTests|ProjectPermissionPolicyTests|ProjectAccessFailurePolicyTests" --no-restore /p:UseSharedCompilation=false` -> OK, 16/16.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual:
- Alfonso confirmo que cancelar y guardar configuracion desde la UI funciono OK.
Cleanup:
- Pendiente por decision de conservar legacy temporal hasta la limpieza final.

### M51 - Metodo termodinamico y elevacion del solver real

Estado: Verified.

Linea base registrada:
- `Project.UpdateConfiguration` actualiza la configuracion y unidades, pero no aplica elevacion al solver real.
- `Project.UpdateThermodynamicMethod` reconstruye parcialmente la configuracion, propaga metodo y unidades.
- `SimulationService.PropagateThermodynamicMethod` asigna metodo al solver y streams, pero no maneja elevacion ni limpieza cuando no hay metodo.
- `SolverConfigurationDialog` inyecta `IMainSolver` directamente y puede configurar un solver distinto al `Project.SimulationService.Solver`.
- La paleta principal nueva ya usa `SessionService.CurrentProject.Configuration.ThermodynamicMethodId`.
Referencia legacy:
- No se elimina ningun legacy temporal por decision de Alfonso.
Prueba esperada:
- El metodo termodinamico configurado del proyecto actualiza `CurrentProject.SimulationService.Solver` y sus streams.
- Guardar elevacion actualiza `CurrentProject.SimulationService.Solver.Altitude` y `AtmosphericPressure`.
- Cancelar configuracion no toca el solver.
- No se configura un solver DI ajeno al proyecto activo.
Nota de alcance:
- Alfonso aclara que inicialmente no se penso el metodo termodinamico como una funcionalidad de cambio frecuente. M51 no crea una migracion avanzada de metodo; solo garantiza que el metodo configurado use el solver real del proyecto.
Implementacion:
- `SimulationService.ApplyProjectConfiguration` aplica al solver real del proyecto la elevacion y el metodo configurado.
- `Project.UpdateConfiguration` llama a la aplicacion completa de configuracion del solver real.
- `Project.UpdateThermodynamicMethod` conserva compatibilidad, pero redirige a la misma aplicacion completa.
- `ProjectSessionService.UpdateProjectConfigurationAsync` ya no aplica el metodo una segunda vez despues del commit.
- `SolverConfigurationDialog` deja de inyectar/escribir `IMainSolver` directamente y guarda por `ProjectSessionService`.
- Correccion M51: `MainSolver` ya no publica 0 m como referencia atmosferica global desde el constructor. La referencia global del `UnitManager` queda gobernada por la elevacion configurada del proyecto activo.
Legacy:
- Se conserva todo legacy temporal existente y se agregan comentarios `LEGACY - TEMPORARY M51`.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "MainSolverConstructor_ShouldNotResetActiveAtmosphericPressureReference|ApplyProjectConfiguration_WhenElevationChanges_ShouldUpdateGaugePressureConversionReference|ApplyProjectConfiguration_ShouldUpdateSolverAltitudeAndAtmosphericPressure|ApplyProjectConfiguration_ShouldPropagateThermodynamicMethodToProjectSolverStreams" --no-restore /p:UseSharedCompilation=false` -> OK, 4/4.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "SimulationServiceTests|ProjectConfigurationPersistenceMapperTests" --no-restore /p:UseSharedCompilation=false` -> OK, 9/9.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual:
- Alfonso confirmo que guardar elevacion actualiza conversiones de presion gauge a absoluta. Caso validado: 25 psig muestra cerca de 39.7 psia a 0 m y un valor menor al aumentar elevacion.
Cleanup:
- Pendiente por decision de conservar legacy temporal hasta la limpieza final.

### M52 - Defaults de unidades y overrides

Estado: Verified.

Linea base registrada:
- `ProjectUnitSystemApplier.ApplyToProject` recorre facades del proyecto y llama `SetProjectDefaultDisplayUnit` por tipo de `Amount`.
- `Variable<T>.SetProjectDefaultDisplayUnit` cambia la unidad visible solo cuando `HasDisplayUnitOverride` es falso.
- `Variable<T>.SetDisplayUnit` marca override individual.
- `FacadeStateSerializer` persiste `ValueUnitName`, `DisplayUnitName` y `HasDisplayUnitOverride`; al hidratar puede restaurar defaults del proyecto si se solicita `restoreProjectDefaultDisplayUnits`.
Referencia legacy:
- No hay ruta legacy ejecutable que reemplazar en M52; se conserva todo legacy temporal existente por decision de Alfonso.
Prueba esperada:
- Cambiar default de unidades del proyecto actualiza variables sin override sin cambiar valor fisico.
- Una variable con override individual conserva su unidad visible aunque cambie el default del proyecto.
- Al serializar e hidratar, el override sobrevive y las variables sin override adoptan el default activo del proyecto.
Implementacion:
- `FacadeStateSerializer.Apply` restaura overrides visuales de unidad aunque la variable este indefinida y no tenga unidad de valor util.
- `UIVariableBase.SelectUnit` ahora marca estado de facade/visual como cambiado para persistir el override de unidad sin ejecutar solver.
- `FlowsheetManager.MarkFacadeStateChanged` reutiliza la cola visual existente para guardar `FacadeStateJson`.
- Se agregan pruebas de regresion para defaults de proyecto, overrides individuales y restauracion por serializacion/hidratacion.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectUnitDefaultsTests" --no-restore /p:UseSharedCompilation=false` -> OK, 3/3.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectUnitDefaultsTests|ProjectConfigurationPersistenceMapperTests|VariableInputCommandTests" --no-restore /p:UseSharedCompilation=false` -> OK, 11/11.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual:
- Alfonso confirmo que el override de unidad por variable persiste al cerrar y volver a abrir la app.
Cleanup:
- Pendiente por decision de conservar legacy temporal hasta la limpieza final.

### M53 - Naming y migracion atomica

Estado: Verified.

Linea base registrada:
- `ProjectSessionService.UpdateProjectConfigurationAsync` persistia primero la configuracion del proyecto y luego aplicaba numeros de diagrama, renombrado de equipos y guardados de diagramas en pasos separados.
- `/UpdateProjectConfigurationRequest` en servidor solo actualizaba la configuracion del proyecto; los documentos de diagramas se persistian por otra ruta.
- Si la configuracion nueva requeria numeros de diagrama o renombrado existente, podia quedar una migracion parcial: configuracion guardada sin todos los nombres/numeros coherentes.
Referencia legacy:
- Se conserva la ruta separada `PersistDiagramNumbersForNamingAsync` para configuraciones sin migracion atomica y como referencia temporal.
- Se agregan comentarios `LEGACY - TEMPORARY M53` donde la ruta anterior queda como respaldo.
Prueba esperada:
- Guardar configuracion sin migracion mantiene el flujo existente.
- Guardar configuracion que requiere numeros de diagrama o renombrado existente prepara un draft local completo y lo envia al servidor como una sola operacion.
- El servidor valida el batch de diagramas antes de aplicar cambios y rechaza duplicados o diagramas inexistentes sin guardar parcialmente.
- Si falla el commit, el cliente restaura nombre de proyecto, configuracion, numeros de diagrama, nombres visuales, labels y registry.
Implementacion:
- `UpdateProjectConfigurationRequest` acepta `MigratedDiagrams` para enviar documentos de diagrama junto con la configuracion.
- El endpoint de configuracion valida el batch de diagramas y aplica configuracion + diagramas antes de un unico `SaveChangesAsync`.
- `ProjectSessionService.UpdateProjectConfigurationAsync` distingue configuraciones simples de migraciones de naming.
- Para migraciones, el cliente captura snapshot, aplica draft local, serializa los diagramas migrados y revierte si el servidor no confirma.
- La persistencia posterior de numeros de diagrama se omite cuando la migracion ya fue incluida en el commit atomico.
Pruebas automaticas:
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectConfigurationPersistenceMapperTests|ProjectVersionConcurrencyTests|ProjectVisualIntentPolicyTests" --no-restore /p:UseSharedCompilation=false` -> OK, 14/14.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual:
- Alfonso confirmo que naming funciona, que el cambio entre naming services conserva los cambios, y que cerrar/abrir la app y crear elementos despues de la migracion tambien funciona correctamente.
Cleanup:
- Pendiente por decision de conservar legacy temporal hasta la limpieza final.

### M54 - Limpieza legacy de permisos y configuracion

Estado: Verified.

Linea base registrada:
- M48-M53 estaban funcionalmente verificados y conservaban comentarios legacy temporales en permisos, configuracion, solver real, unidades y naming.
- No habia rutas ejecutables viejas dentro de M48-M53 que reemplazar; la deuda era documental/codigo comentado.
Referencia legacy:
- Eliminada para M48-M53.
- No se tocaron legacies de otras unidades: inputs, hidratacion, canvas, interdiagrama, persistencia/realtime ya limpiada en M47, ni equipos.
Implementacion:
- Se retiraron comentarios `LEGACY - TEMPORARY M49-M53` de sesion, dialogos de configuracion, solver/configuracion de dominio, unit overrides y naming migration.
- Se elimino un comentario inline obsoleto en `UIVariableBase.SelectUnit`.
- Se mantuvo intacta la logica vigente y no se endurecieron contratos publicos en esta unidad de limpieza.
Pruebas automaticas:
- `rg -n "LEGACY - TEMPORARY M(48|49|50|51|52|53)" Client Server Shared Distillator.Domain -g "*.cs" -g "*.razor"` -> sin coincidencias.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --filter "ProjectPermissionPolicyTests|ProjectAccessFailurePolicyTests|ProjectConfigurationPersistenceMapperTests|SimulationServiceTests|ProjectUnitDefaultsTests|VariableInputCommandTests|ProjectVersionConcurrencyTests|ProjectVisualIntentPolicyTests" --no-restore /p:UseSharedCompilation=false` -> OK, 44/44.
- `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 147/147.
- `dotnet build Client/Client.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
- `dotnet build Server/Server.csproj --no-restore /p:UseSharedCompilation=false` -> OK, 0 errores.
Prueba manual:
- N/A. M54 solo elimina legacy temporal ya validado en M48-M53.
Cleanup:
- Done.

## Bitacora Del Plan

| Fecha | Cambio | Evidencia |
|---|---|---|
| 2026-07-18 | M32 activada | Autorizacion de Alfonso; app apagada; alcance limitado a legacy temporal de canvas/topologia local M25-M31 |
| 2026-07-18 | M32 verificada | Legacy temporal M25-M31 eliminado; busqueda sin coincidencias; pruebas enfocadas 22/22; suite 147/147; builds Client y Server correctos |
| 2026-07-18 | M24 verificada y cerrada | Legacy temporal M18-M23 eliminado; helpers M18 muertos retirados; tests 147/147; build Client/Server |
| 2026-07-18 | M24 activada | Autorizacion de Alfonso; app apagada; alcance limitado a legacy temporal de hidratacion/seleccion M18-M23 |
| 2026-07-18 | M17 verificada y cerrada | Legacy temporal M10-M16 eliminado; `_visualPersistenceLock` retirado; rg sin coincidencias; tests 147/147; build Client/Server |
| 2026-07-18 | M17 activada | Autorizacion de Alfonso; app apagada; alcance limitado a legacy temporal de inputs, formulas y autosave M10-M16 |
| 2026-07-18 | M09 verificada y cerrada | Legacy temporal M04-M08 eliminado; rg sin coincidencias; tests 147/147; build Client/Server |
| 2026-07-18 | M09 activada | Autorizacion de Alfonso; app apagada; alcance limitado a legacy temporal de simulacion M04-M08 |
| 2026-07-18 | M54 verificada y cerrada | Legacy temporal M48-M53 eliminado; rg sin coincidencias; tests 147/147; build Client/Server |
| 2026-07-18 | M54 activada | Autorizacion de Alfonso; app apagada; alcance limitado a legacy temporal de permisos/configuracion |
| 2026-07-18 | M47 verificada y cerrada | Legacy temporal M39-M46 eliminado; rg sin coincidencias; tests 147/147; build Client/Server |
| 2026-07-18 | M47 activada | Autorizacion de Alfonso; app apagada; alcance limitado a legacy temporal de persistencia/realtime |
| 2026-07-18 | M53 validada manualmente | Alfonso confirmo cambio entre naming services, persistencia tras cerrar/abrir y creacion posterior OK |
| 2026-07-18 | M53 implementada con pruebas automaticas; prueba manual pendiente | Request atomico con `MigratedDiagrams`; validacion servidor; snapshot/rollback cliente; tests 14/14; build Client/Server |
| 2026-07-18 | M53 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-18 | Deuda registrada: optimizar polling autoritativo `GetProjectRequest` | `docs/CODEX_CONTEXT.md`; `docs/PERSISTENCE_REALTIME_DESIGN.md`; M46 mantiene 3s probado funcionalmente |
| 2026-07-18 | M52 validada manualmente | Alfonso confirmo que el override de unidad por variable persiste tras cerrar/abrir |
| 2026-07-18 | M52 corregida para persistir override de unidad por variable | `UIVariableBase.SelectUnit` marca facade state cambiado; tests M52/regresiones; build Client/Server |
| 2026-07-18 | M52 implementada con pruebas automaticas | `ProjectUnitDefaultsTests`; regresiones configuracion/inputs; build Client/Server |
| 2026-07-18 | M52 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-18 | M51 validada manualmente | Alfonso confirmo que la presion absoluta de 25 psig baja al aumentar elevacion |
| 2026-07-18 | M51 corregida para que solvers auxiliares no reseteen la referencia atmosferica global | Regression `MainSolverConstructor_ShouldNotResetActiveAtmosphericPressureReference`; tests M51; build Client/Server |
| 2026-07-18 | M51 implementada con pruebas automaticas | `SimulationServiceTests`; build Client; build Server |
| 2026-07-18 | M51 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M50 validada manualmente | Confirmacion de Alfonso; cancelar y guardar configuracion OK |
| 2026-07-17 | M50 implementada con pruebas automaticas | Build Client; `ProjectConfigurationPersistenceMapperTests`; build Server |
| 2026-07-17 | M50 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M49 validada manualmente | Confirmacion de Alfonso; cambio de rol y retiro de acceso OK |
| 2026-07-17 | M49 implementada con pruebas automaticas | `ProjectAccessFailurePolicyTests`; build Client; build Server |
| 2026-07-17 | M49 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M48 validada manualmente y cerrada | Confirmacion de Alfonso; Owner, Editor y Viewer OK |
| 2026-07-17 | M48 implementada con pruebas automaticas | `ProjectPermissionPolicyTests`; build Client; build Server |
| 2026-07-14 | Barrido funcional y arquitectonico completado | Revision estatica del repositorio |
| 2026-07-14 | Specs 00-13 creadas como borradores | `docs/refactor-specs/` |
| 2026-07-14 | Plan incremental de pruebas creado | `TEST_PLAN.md` |
| 2026-07-14 | Regla de preservacion legacy incorporada | `README.md` y `TEST_PLAN.md` |
| 2026-07-14 | Plan maestro secuencial creado | Este documento |
| 2026-07-14 | M01 activada y decisiones funcionales consolidadas | Specs 00, 01 y 02 |
| 2026-07-14 | M01 aprobada y cerrada | Confirmacion de Alfonso; specs 00, 01 y 02 |
| 2026-07-15 | M02 activada e inventario de pruebas registrado | `TEST_CONVENTIONS.md` |
| 2026-07-15 | M02 aprobada y cerrada | Confirmacion de Alfonso; `TEST_CONVENTIONS.md` |
| 2026-07-15 | M03 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-15 | M03 infraestructura implementada y pruebas automaticas pasan | `dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj`; `dotnet test Distillator.slnx` |
| 2026-07-15 | M03 confirmada y cerrada | Confirmacion de Alfonso; no requiere UI |
| 2026-07-15 | M04 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-15 | M04 implementada y pruebas automaticas pasan | `dotnet test`; `dotnet build` |
| 2026-07-15 | Regla operativa ajustada | Unidades sin UI visible se cierran por evidencia automatica |
| 2026-07-15 | M04 verificada tecnicamente y cerrada | Pruebas y build correctos; manual N/A |
| 2026-07-15 | M05 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-15 | M05 verificada tecnicamente y cerrada | Pruebas y build correctos; manual N/A |
| 2026-07-15 | M06 activada | Autorizacion de Alfonso; app apagada por regla operativa |
| 2026-07-15 | M06 verificada tecnicamente y cerrada | Pruebas y build correctos; manual N/A |
| 2026-07-15 | M07 activada | Autorizacion de Alfonso; app apagada por regla operativa |
| 2026-07-15 | M07 verificada tecnicamente y cerrada | Pruebas y build correctos; manual N/A |
| 2026-07-15 | M08 activada | Autorizacion de Alfonso; app apagada por regla operativa |
| 2026-07-15 | M08 verificada tecnicamente y cerrada | Pruebas y build correctos; manual N/A |
| 2026-07-15 | M09 pospuesta | No borrar legacy hasta prueba manual real de la app por Alfonso |
| 2026-07-15 | M10 inicio | Baseline: editores `UIVariable*` mutan `Variable<T>` y disparan `FlowsheetManager.RunSimulation()` directamente |
| 2026-07-15 | M10 implementacion y auto tests | `dotnet test` 13/13; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-15 | M10 validada manualmente | Alfonso edito variables de columna y corriente suelta sin errores visibles |
| 2026-07-15 | M10 delete validado | Alfonso probo borrar dato definido con `Delete`; todo OK |
| 2026-07-15 | M11 inicio | Baseline: validacion numerica vive en UI y el comando acepta cualquier double/unidad recibida |
| 2026-07-15 | M11 implementacion y auto tests | `dotnet test` 16/16; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-15 | M11 validada manualmente | Texto invalido no cambio variables; valor valido posterior funciono sin errores raros |
| 2026-07-15 | M12 inicio | Baseline: grillas de composicion duplican set/clear y usan suma 99-101 sin exigir todos los componentes definidos |
| 2026-07-15 | M12 implementacion y auto tests | `dotnet test` 21/21; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-15 | M12 bug visual detectado | Fracciones calculadas viejas de la base opuesta quedaban visibles en composicion parcial |
| 2026-07-15 | M12 bug visual corregido | `dotnet test` 23/23; `dotnet build` correcto; revalidacion manual pendiente |
| 2026-07-15 | M12 render visual corregido | `CompositionGrid` muestra `<Not defined>` si la fraccion readonly no esta definida; build y tests correctos |
| 2026-07-15 | M12 render visual homologado | `EquipmentBaseCompositionGrid` usa la misma regla visual; build y tests correctos |
| 2026-07-15 | M12 regla de base opuesta corregida | No borrar dominio; ocultar base opuesta solo en composicion incompleta; `dotnet test` 25/25 y build correctos |
| 2026-07-15 | M12 validada manualmente | Alfonso confirmo que ahora funciona perfecto en app |
| 2026-07-15 | M13 inicio | Baseline: `EquipmentBaseFormulaSpecifications` crea/edita/elimina specs directamente y dispara `FSM.RunSimulation()` |
| 2026-07-15 | M13 implementacion y auto tests | `dotnet test` 29/29; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-15 | M13 validada manualmente | Crear, editar sin duplicar, eliminar e invalida no agregada OK |
| 2026-07-15 | M14 inicio | Baseline: `MainSolver` construye tres niveles de specification sin prueba de orden/presencia |
| 2026-07-15 | M14 verificada tecnicamente | `dotnet test` 30/30; `dotnet build` correcto; manual N/A |
| 2026-07-15 | M15 inicio | Baseline: guardados de diagrama directos o bajo lock visual, sin revision Dirty/Clean |
| 2026-07-15 | M15 implementacion y auto tests | `dotnet test` 33/33; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-16 | M15 validada manualmente | Alfonso confirmo ultimo input/cambio visual persistido sin errores raros |
| 2026-07-16 | M16 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M16 verificada tecnicamente | `dotnet test` 36/36; `dotnet build` correcto; manual N/A |
| 2026-07-16 | M17 bloqueada por decision de producto | Alfonso decide conservar legacy temporal hasta terminar el refactor completo |
| 2026-07-16 | M18 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M18 verificada tecnicamente | `dotnet test` 38/38; `dotnet build` correcto; manual N/A |
| 2026-07-16 | M19 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M19 verificada tecnicamente | `dotnet test` 40/40; `dotnet build` correcto; manual N/A |
| 2026-07-16 | M20 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M20 verificada tecnicamente | `dotnet test` 44/44; `dotnet build` correcto; manual N/A |
| 2026-07-16 | M21 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M21 implementacion y auto tests | `dotnet test` 47/47; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-16 | M21 validada manualmente | Cambio entre proyectos OK; sin mezcla de elementos; delay observado en recalculo inicial |
| 2026-07-16 | M22 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M22 verificada tecnicamente | `dotnet test` 51/51; `dotnet build` correcto; manual N/A |
| 2026-07-16 | M23 implementacion y auto tests | `dotnet test` 53/53; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-16 | M23 validada manualmente | Alfonso confirmo cambio rapido, logout y reingreso OK |
| 2026-07-16 | M24 bloqueada por decision de producto | Alfonso decide conservar legacy temporal hasta terminar el refactor completo |
| 2026-07-16 | M25 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M25 implementacion y auto tests | `dotnet test` 57/57; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-16 | M25 validada manualmente | Alfonso confirmo todas las pruebas OK |
| 2026-07-16 | M26 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M26 implementacion y auto tests | `dotnet test` 60/60; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-16 | M26 validada manualmente | Alfonso confirmo que crear y borrar equipos funciono perfecto |
| 2026-07-16 | M27 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M27 implementacion y auto tests | `dotnet test` 64/64; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-16 | M27 validada manualmente | Alfonso confirmo prueba OK |
| 2026-07-16 | M28 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M28 auto tests | Ruta existente protegida; `dotnet test` 66/66; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-16 | M28 validada manualmente | Alfonso confirmo que funciono perfecto |
| 2026-07-16 | M29 implementacion y auto tests | `dotnet test` 68/68; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-16 | M29 correccion visual | Puerto libre dinamico queda al final; `dotnet test` 68/68; `dotnet build` correcto; revalidacion pendiente |
| 2026-07-16 | M29 correccion de recarga | Hidratacion crea `Inlet_N`/`Outlet_N` antes de pipes; `dotnet test` 70/70; `dotnet build` correcto |
| 2026-07-16 | M29 validada manualmente | Alfonso confirmo funcionamiento perfecto; lentitud registrada para metricas futuras |
| 2026-07-16 | M30 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M30 implementacion y auto tests | `dotnet test` 74/74; `dotnet build` correcto; prueba manual pendiente |
| 2026-07-16 | M30 validada manualmente | Alfonso confirmo que funciono perfecto |
| 2026-07-16 | M31 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M31 implementacion y auto tests | Persistencia de dimensiones y orden por autosave visual; `dotnet test` Specs 04/09 26/26; `dotnet build` correcto |
| 2026-07-16 | M31 falla manual parcial | Alfonso reporto que arrastrar diagramas no funciono |
| 2026-07-16 | M31 correccion drag/drop | `draggable` explicito y refresco inmediato del orden antes de persistir |
| 2026-07-16 | M31 validada manualmente | Alfonso confirmo que arrastrar diagramas funciono OK |
| 2026-07-16 | M32 bloqueada por decision de producto | Alfonso decide conservar legacy temporal hasta terminar el refactor completo |
| 2026-07-16 | M33 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M33 implementacion y auto tests | `dotnet test` InterFlowsheetConnectionServiceTests 3/3; suite 78/78; `dotnet build` correcto |
| 2026-07-16 | M33 validada manualmente | Alfonso conecto corrientes entre diagramas; creacion OK |
| 2026-07-16 | Falla interdiagrama registrada para M35 | Al desconectar, los OPC quedaron visibles y persistidos tras cerrar/abrir |
| 2026-07-16 | M34 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M34 implementacion y auto tests | `dotnet test` ProjectInterFlowsheetConnectionHydrationServiceTests 4/4; suite 82/82; `dotnet build` correcto |
| 2026-07-16 | M34 validada manualmente | Alfonso confirmo M34 OK |
| 2026-07-16 | M35 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M35 implementacion y auto tests | `dotnet test` InterFlowsheetConnectionServiceTests 5/5; suite 84/84; `dotnet build` correcto |
| 2026-07-16 | M35 correccion visual | OPC huerfano se elimina de `_elements` al reconstruir pipes; test interdiagrama 5/5 y build correcto |
| 2026-07-16 | M35 validada manualmente | Alfonso confirmo que funciono perfecto |
| 2026-07-16 | M36 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M36 implementacion y auto tests | `dotnet test` InterFlowsheetConnectionServiceTests 7/7; suite 86/86; `dotnet build` correcto |
| 2026-07-16 | M36 validada manualmente | Alfonso confirmo que funciono perfecto |
| 2026-07-16 | M37 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M37 implementacion y auto tests | `dotnet test` ProjectAutosaveCoordinatorTests 7/7; suite 88/88; `dotnet build` correcto |
| 2026-07-16 | M37 validada manualmente | Alfonso confirmo que funciono perfecto |
| 2026-07-16 | M38 bloqueada | Limpieza legacy interdiagrama aplazada por decision de Alfonso |
| 2026-07-18 | M38 activada | Autorizacion global de limpiezas; app apagada; alcance limitado a legacy temporal interdiagrama M33-M37 |
| 2026-07-18 | M38 verificada | Legacy temporal M33-M37 eliminado; bloque inalcanzable M34 retirado; pruebas enfocadas 20/20; suite 147/147; builds Client y Server correctos |
| 2026-07-16 | M39 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-16 | M39 implementacion y auto tests | `ExpectedVersion` en DTOs, rechazo servidor por version atrasada, cliente envia version confirmada; tests 5/5 y suite 93/93; builds Server/Client correctos con salida temporal |
| 2026-07-17 | M39 falla manual registrada | Dos sesiones detectaron conflicto correctamente, pero el cliente mostro error global por `Console.Error` |
| 2026-07-17 | M39 correccion cliente | Conflicto esperado se registra como informacion controlada; tests 9/9, suite 97/97 y build Client temporal correcto |
| 2026-07-17 | M39 actualizacion post-conflicto | Conflicto recarga proyecto autoritativo y descarta snapshot visual rechazado; tests 9/9, suite 97/97 y build Client temporal correcto |
| 2026-07-17 | M39 validada manualmente | Alfonso confirmo que funciono OK |
| 2026-07-17 | M40 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M40 verificada tecnicamente | Confirmacion HTTP actualiza version local y version realtime conocida; tests 13/13, suite 101/101 y build Client temporal correcto |
| 2026-07-17 | M41 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M41 implementacion y auto tests | Conflicto visual recarga estado autoritativo y reaplica intencion visual segura; tests 16/16, suite 104/104 y build Client temporal correcto |
| 2026-07-17 | M41 validada manualmente | Alfonso confirmo que funciono perfecto |
| 2026-07-17 | M42 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M42 verificada tecnicamente | Auditoria de actualizacion de diagramas usa resumen ligero; tests 2/2, suite 106/106 y build Server temporal correcto |
| 2026-07-17 | M43 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M43 verificada tecnicamente | `OperationId` en escrituras, auditoria e idempotencia de reintento; tests 15/15, suite 108/108, builds Server/Client correctos y migracion local aplicada |
| 2026-07-17 | M44 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M44 verificada tecnicamente | Politica realtime descarta duplicados, documentos atrasados y versiones ya conocidas; tests 9/9, suite 113/113 y build Client temporal correcto |
| 2026-07-17 | M45 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M45 implementacion y auto tests | Realtime remoto se difiere con autosave local Dirty/Saving y se procesa al volver a Clean; tests 15/15, suite 116/116 y build Client temporal correcto |
| 2026-07-17 | M45 validada manualmente | Alfonso confirmo que funciono OK |
| 2026-07-17 | M46 activada | Autorizacion de Alfonso; app apagada |
| 2026-07-17 | M46 implementacion y auto tests | Reconectar dispara catch-up HTTP si existe version perdida; tests 11/11, suite 119/119 y build Client temporal correcto |
| 2026-07-17 | M46 correccion post prueba fallida | Fallo manual: B quedaba sin proyectos tras volver online; correccion conserva lista ante falla HTTP y refresca lista al reconectar; tests 11/11 y build Client correctos |
| 2026-07-17 | M46 correccion auto-reconnect Closed | Fallo parcial: B no actualizaba hasta cambiar de proyecto; `ProjectRealtimeService` reintenta conexion cerrada, re-une proyecto y dispara catch-up; tests 11/11 y build Client correctos |
| 2026-07-17 | M46 correccion browser online | Fallo parcial: B solo veia cambio al cambiar de diagrama; `window.online` dispara recuperacion realtime y catch-up HTTP; tests 11/11 y build Client correctos |
| 2026-07-17 | M46 correccion browser online no bloqueante | Fallo critico: online-online dejo de sincronizar; registro JS queda best-effort para no bloquear SignalR; tests 11/11 y build Client correctos |
| 2026-07-17 | M46 correccion recovery watcher | Fallo parcial: offline-online no actualizaba hasta cambiar diagrama; watcher en `Reconnecting`/`Closed` dispara catch-up al volver `Connected`; tests 11/11 y build Client correctos |
| 2026-07-17 | M46 correccion refresh canvas remoto | Fallo parcial: cambio remoto aparecia al cambiar de diagrama; `ProjectDiagram` recarga FSM dentro de `InvokeAsync`; tests 11/11 y build Client correctos |
| 2026-07-17 | M46 correccion recovery HTTP autoritativo | Fallo parcial persistente: cambio aparecia solo tras interaccion; fallos de conexion ahora disparan loop HTTP que publica documento autoritativo; tests 11/11 y build Client correctos |
| 2026-07-17 | M46 correccion polling autoritativo | Fallo persistente y JS no cargado visible; proyecto activo consulta version HTTP cada 3s y publica versiones nuevas como reconciliacion; tests 11/11 y build Client correctos |
| 2026-07-17 | M46 background sync condicionado | Polling agresivo reemplazado por servicio dedicado con politica Clean/sin hidratacion/sin solver/sin operacion visual; tests 18/18 y build Client correctos |
| 2026-07-17 | M46 correccion version renderizada | B recibia HTTP 200 pero no publicaba; sync compara contra version renderizada, no solo version realtime conocida; tests 18/18 y build Client correctos |
| 2026-07-17 | M46 correccion no limpiar por red | Fallo de red en `GetProjectRequest` se trataba como perdida de proyecto; ahora conserva estado y difiere evento; tests 18/18 y build Client correctos |
| 2026-07-17 | M46 validada manualmente | Alfonso confirmo online-online OK y offline-online recupera sin cambiar proyecto/diagrama, probado dos veces |

## Siguiente Accion

Limpiezas legacy programadas completadas. El foco pendiente no bloqueante es validar componentes/metodos termodinamicos con sustancias distintas a agua/etanol. Las exportaciones Excel quedan para el cierre.
