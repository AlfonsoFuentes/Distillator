# 00 - Fundamentos Y Vocabulario

Estado: Aprobada

## Contexto

Distillator combina edicion de procesos, simulacion reactiva, persistencia silenciosa
y colaboracion multiusuario. Actualmente varias capas interpretan de forma diferente
cuando un cambio empieza, cuando una simulacion termina y cuando un proyecto queda
guardado.

Esta spec establece el lenguaje comun que usaran las demas especificaciones.

## Principios Funcionales

### Intencion Sobre Resultado

La verdad persistida principal es lo que el usuario definio:

- valores ingresados por UI;
- formulas y specifications;
- unidades seleccionadas;
- equipos, puertos y conexiones;
- nombres y configuracion;
- estado visual necesario para reconstruir el flowsheet.

Los valores derivados por el solver se recalculan al hidratar el proyecto. Solo se
persistiran resultados calculados si una spec futura define un snapshot explicito y
separado de la intencion.

### Una Autoridad Por Responsabilidad

- La UI expresa una intencion y presenta estado.
- El dominio valida y aplica cambios validos.
- El coordinador de simulacion controla la ejecucion del solver.
- El coordinador de autosave controla el guardado.
- HTTP confirma el estado compartido persistido.
- SignalR notifica que el estado persistido cambio.

### Errores Explicitos

Los errores esperados deben representarse con resultados explicitos. Una operacion no
se considera exitosa solo porque no lanzo una excepcion.

## Vocabulario

### User Intent

Cambio solicitado directamente por el usuario. Ejemplos: definir temperatura, mover
un equipo, crear una formula o conectar un stream.

### Domain Change

Mutacion valida aplicada al modelo en memoria como consecuencia de una intencion.

### Change Kind

Clasificacion funcional de un cambio:

| Tipo | Requiere solver | Requiere persistencia |
|---|---:|---:|
| `Input` | Si | Si |
| `Specification` | Si | Si |
| `Topology` | Si | Si |
| `Visual` | No | Si |
| `Configuration` | Depende del campo | Si |
| `WorkspacePreference` | No | Si, por usuario |
| `CalculatedResult` | No aplica | No, por defecto |

### Dirty Revision

Revision local que contiene intenciones validas aun no confirmadas por el backend.

### Simulation Run

Una ejecucion identificable del solver sobre una revision conocida del proyecto.

### Persisted Revision

Revision aceptada por el backend y asociada a una version monotona del proyecto.

### Realtime Event

Notificacion de que existe una nueva version persistida. No transporta por si sola la
verdad completa del proyecto.

## Estados Compartidos

### Estado De Simulacion

```text
Idle -> Queued -> Running -> Completed
                         -> Failed
```

`Completed` y `Failed` son resultados de una ejecucion concreta. Despues de procesar
el resultado, el coordinador vuelve a `Idle` o inicia la siguiente revision pendiente.

### Estado De Persistencia

```text
Clean -> Dirty -> Saving -> Saved
                    |       |
                    +-> SaveFailed
```

`Saved` significa que el backend confirmo la revision. Un cambio nuevo durante
`Saving` mantiene el proyecto `Dirty` aunque la peticion anterior termine bien.

## Invariantes Globales

1. Un proyecto tiene como maximo una ejecucion activa del solver.
2. Una finalizacion siempre corresponde a una ejecucion identificada.
3. Un resultado atrasado no puede sobrescribir el estado de una revision mas nueva.
4. Un autosave nunca se interpreta como exitoso sin confirmacion del backend.
5. SignalR solo se emite despues de persistencia exitosa.
6. Una recarga realtime no puede descartar silenciosamente una revision local sucia.
7. Los permisos se validan en UI para experiencia y en servidor para autoridad.
8. Viewer nunca produce cambios persistidos del proyecto.
9. Los callbacks asincronos que ejecutan trabajo devuelven `Task`.
10. El estado visible de carga, simulacion y guardado refleja trabajo real pendiente.
11. La implementacion anterior permanece como referencia temporal hasta verificar su
    reemplazo y recibir confirmacion funcional.

## Decisiones Aprobadas M01

Paquete consolidado aprobado en M01:

1. La version y la serializacion que protegen la autoridad compartida se controlan
   inicialmente por proyecto. El debounce puede agruparse por diagrama, pero no crea
   autoridades de version independientes.
2. Si llega realtime mientras existe una revision local `Dirty`, no se reemplaza el
   modelo local. Se conserva la intencion y se inicia el flujo de conflicto definido
   posteriormente en la spec 06.
3. Una intencion valida se guarda aunque el solver no converja o falle. El guardado
   espera el resultado final de la simulacion y usa un snapshot inmutable de intencion
   capturado antes de ejecutar el solver.
4. No se implementan snapshots de resultados calculados durante este refactor. Una
   necesidad futura de reportes se especificara como almacenamiento separado.

Decisiones aprobadas por Alfonso en M01 el 2026-07-14.

## Criterios De Aceptacion

1. Todas las specs funcionales usan el mismo significado de intencion, revision,
   simulacion y persistencia.
2. Cada cambio puede clasificarse sin que la UI decida directamente solver y HTTP.
3. Los estados de simulacion y persistencia pueden evolucionar independientemente.
4. Ninguna spec trata resultados calculados como verdad principal persistida.
5. Los criterios de aceptacion posteriores se pueden relacionar con al menos una
   invariante global.

## Pruebas Requeridas

- Clasificacion de cada tipo de cambio conocido.
- Transiciones validas e invalidas de simulacion.
- Transiciones validas e invalidas de persistencia.
- Revision nueva durante simulacion y durante guardado.
- Verificacion de que Viewer no produce cambio compartido.
- Verificacion de que resultados atrasados no reemplazan revisiones nuevas.

## Fuera De Alcance

- Definir todavia clases, interfaces o carpetas finales.
- Elegir infraestructura antes de que una spec funcional la necesite.
- Sustituir las reglas especificas de cada flujo por principios generales.
