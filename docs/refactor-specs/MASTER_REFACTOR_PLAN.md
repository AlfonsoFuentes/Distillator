# Plan Maestro Del Refactor

Estado: Activo

Ultima actualizacion: 2026-07-14

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
- Foco actual: M02, definir proyecto de pruebas y convenciones.
- Primera unidad ejecutable prevista: M04, finalizacion real del solver.

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
- `Manual`: comprobacion manual y resultado esperado confirmados por Alfonso.
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
| M02 | Definir proyecto de pruebas y convenciones | TEST | Active | Done | N/A | Pending | N/A | Pending | N/A |
| M03 | Crear infraestructura minima de pruebas | TEST | Pending | N/A | Pending | Pending | Pending | Pending | Pending |

Resultado de fase:

- vocabulario aprobado;
- resultados esperados iniciales aprobados;
- suite ejecutable disponible;
- linea base del solver reproducible.

### Fase 1 - Ciclo De Simulacion

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M04 | S1: finalizacion real incluye post-calculos | 01 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M05 | S2: una sola simulacion activa por proyecto | 01 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M06 | S3: coalescencia de solicitudes | 01 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M07 | S4-S6: no convergencia, excepcion y revision atrasada | 01,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M08 | Integrar estado de simulacion con hidratacion y UI | 01,03 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M09 | Limpieza final de rutas legacy de simulacion | 01 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- contrato asincrono unico;
- solver no solapado;
- resultado correlacionado por ejecucion y revision;
- UI observa el final real.

### Fase 2 - Inputs, Formulas Y Autosave

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M10 | Comando de input para `Variable<T>` | 02 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M11 | Validacion, unidades y auditoria de inputs | 02,09 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M12 | Composicion completa, incompleta y limpieza | 02,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M13 | Crear, editar y eliminar formulas | 10 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M14 | Tres niveles de intento de specifications | 10,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M15 | Estado `Dirty` y autosave serializado | 02,06 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M16 | Fallo de solver y fallo HTTP sin perdida de input | 02,06 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M17 | Limpieza legacy de inputs, formulas y autosave | 02,10 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- componentes expresan intencion;
- componentes no llaman solver ni HTTP;
- inputs y formulas sobreviven fallos;
- guardados no se aplican fuera de orden.

### Fase 3 - Carga, Hidratacion Y Sesion

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M18 | Extraer mapeo de configuracion y documentos | 03,09 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M19 | Reconstruir equipos, variables y registry | 03,11 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M20 | Reconstruir pipes, formulas y solver | 03,04,10 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M21 | Hidratacion cancelable y publicacion atomica | 03 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M22 | Seleccion explicita de proyecto y diagrama | 08 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M23 | Logout, sesion expirada y cambio rapido A -> B | 03,08 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M24 | Limpieza legacy de hidratacion y seleccion | 03,08 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- proyecto construido en aislamiento;
- carga espera recalculo real;
- respuestas atrasadas se descartan;
- render no produce efectos de sesion.

### Fase 4 - Canvas Y Topologia Local

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M25 | Cambios visuales sin solver | 04 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M26 | Crear y borrar equipos atomicamente | 04,11 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M27 | Conexion directa y desconexion | 04 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M28 | Conexion equipo-equipo con stream intermedio | 04 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M29 | Mixer y splitter: puertos dinamicos | 04,11 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M30 | Vessel y column: puertos dinamicos | 04,11 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M31 | Persistir orden, pan, zoom y dimensiones | 04,09 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M32 | Limpieza legacy de canvas y topologia local | 04 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- comandos topologicos atomicos;
- registry, pipes, puertos y solver consistentes;
- cambios visuales independientes del solver.

### Fase 5 - Conexiones Interdiagrama Y Eliminacion

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M33 | Crear conexion interdiagrama atomica | 05 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M34 | Hidratar conexion interdiagrama | 03,05 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M35 | Desconectar desde cualquiera de los extremos | 05 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M36 | Borrar diagrama y limpiar sobrevivientes | 05 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M37 | Persistencia por lote de ambos extremos | 05,06 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M38 | Limpieza legacy interdiagrama | 05 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- OPC reciprocos;
- un solo enlace logico de solver;
- ambos diagramas guardados o rechazados juntos;
- eliminaciones sin artefactos huerfanos.

### Fase 6 - Versionado, Conflictos Y Realtime

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M39 | `ExpectedVersion` y rechazo de escritura atrasada | 06 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M40 | Confirmacion HTTP actualiza revision local | 06 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M41 | Conflicto conserva intencion local | 06,07 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M42 | Auditoria ligera por tipo de cambio | 06 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M43 | Idempotencia y timeout incierto | 06 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M44 | Realtime limpio, duplicado y fuera de orden | 07 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M45 | Realtime con estado local `Dirty` | 06,07 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M46 | Reconexion, eventos perdidos y presencia | 07 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M47 | Limpieza legacy de persistencia y realtime | 06,07 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- conflictos detectados;
- ningun overwrite silencioso por version atrasada;
- SignalR notifica commits y HTTP mantiene autoridad;
- reconexion recupera versiones perdidas.

### Fase 7 - Permisos Y Configuracion

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M48 | Owner, Editor y Viewer en comandos y endpoints | 08 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M49 | Cambio de rol y retiro de acceso en vivo | 07,08 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M50 | Configuracion como copia temporal y commit unico | 09 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M51 | Metodo termodinamico y elevacion del solver real | 09,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M52 | Defaults de unidades y overrides | 09 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M53 | Naming y migracion atomica | 09 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M54 | Limpieza legacy de permisos y configuracion | 08,09 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- permisos coherentes en UI y servidor;
- configuracion aplicada una vez;
- un solo solver por proyecto;
- unidades y nombres sobreviven recarga.

### Fase 8 - Matriz De Equipos Y Regresion Numerica

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M55 | Material Stream | 11,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M56 | Pump y Control Valve | 11,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M57 | Heat, Plate Exchanger y Reboiler | 11,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M58 | Flash Tank: Feed, Vapor y Liquid | 11,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M59 | Mixer, Splitter y Vessel | 11,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M60 | Column y estrategias asociadas | 11,12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M61 | Casos fisicos limite y no convergencia | 12 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M62 | Limpieza legacy por equipos | 11,12 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- contrato probado por equipo;
- balances con tolerancias explicitas;
- flujo cero y limites fisicos cubiertos;
- no convergencia distinguible de error.

### Fase 9 - Funciones De Soporte

| ID | Unidad | Spec | Estado | Baseline | Legacy | Implementation | Auto | Manual | Cleanup |
|---|---|---|---|---|---|---|---|---|---|
| M63 | CRUD de componentes y correlaciones | 13 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M64 | CRUD de metodos y parametros binarios | 13 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M65 | Usuarios, roles globales y passwords | 08,13 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M66 | Exportaciones Excel | 13 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M67 | Phase envelope y graficas | 12,13 | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| M68 | Limpieza legacy de funciones de soporte | 13 | Pending | N/A | Pending | N/A | Pending | Pending | Pending |

Resultado de fase:

- CRUD y roles verificados;
- exportaciones reproducibles;
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
Pendientes: iniciar M02 con autorizacion independiente.
```

## Bitacora Del Plan

| Fecha | Cambio | Evidencia |
|---|---|---|
| 2026-07-14 | Barrido funcional y arquitectonico completado | Revision estatica del repositorio |
| 2026-07-14 | Specs 00-13 creadas como borradores | `docs/refactor-specs/` |
| 2026-07-14 | Plan incremental de pruebas creado | `TEST_PLAN.md` |
| 2026-07-14 | Regla de preservacion legacy incorporada | `README.md` y `TEST_PLAN.md` |
| 2026-07-14 | Plan maestro secuencial creado | Este documento |
| 2026-07-14 | M01 activada y decisiones funcionales consolidadas | Specs 00, 01 y 02 |
| 2026-07-14 | M01 aprobada y cerrada | Confirmacion de Alfonso; specs 00, 01 y 02 |
| 2026-07-15 | M02 activada e inventario de pruebas registrado | `TEST_CONVENTIONS.md` |

## Siguiente Accion

Obtener confirmacion de Alfonso sobre las convenciones propuestas en M02. Despues se
cierra M02 y se solicita autorizacion independiente para crear la infraestructura
minima de M03.
