# 02 - Input De Usuario, Solver Y Autosave

Estado: Aprobada

## Contexto

Una edicion de variable debe actualizar la simulacion y guardarse silenciosamente. En
el flujo actual, el componente muta directamente una `Variable<T>`, dispara el solver
y el guardado ocurre indirectamente cuando `FlowsheetManager` interpreta que todas las
simulaciones terminaron.

## Problema Actual

- La UI conoce y dispara el solver.
- No existe un resultado unico que describa validacion, aplicacion y guardado.
- El autosave depende de contadores de simulacion dentro del gestor del canvas.
- Fallos HTTP pueden dejar el estado local distinto al persistido.
- El guardado reemplaza el documento completo del diagrama.
- Un cambio remoto puede recargar el proyecto mientras hay una intencion local sin
  confirmar.

## Comportamiento Deseado

La UI entrega una intencion tipada. Un unico flujo valida permisos y datos, aplica el
cambio, solicita simulacion cuando corresponde, crea un snapshot persistible de la
intencion y confirma el guardado.

## Flujo Normal

```text
UI Commit
  -> Validate Permission
  -> Validate Value and Unit
  -> Apply User Intent
  -> Mark Revision Dirty
  -> Capture Immutable Intent Snapshot
  -> Request Simulation
  -> Await Final Simulation Result
  -> Enqueue Latest Intent Snapshot For Autosave
  -> Backend Confirms Version
  -> SignalR Notification
  -> Revision Becomes Clean
```

El snapshot se captura antes de ejecutar el solver y no copia resultados calculados.
No se serializa el modelo de dominio mutable mientras el solver esta trabajando. El
autosave se encola despues del resultado final, tanto si converge como si no converge
o falla. Si durante la simulacion aparece una revision mas reciente, la cola conserva
la ultima intencion pendiente y no confirma como limpia una revision anterior.

## Invariantes

1. Solo un commit explicito cambia el modelo; escribir texto temporal no lo hace.
2. Un valor invalido no cambia dominio, no ejecuta solver y no guarda.
3. La unidad se valida antes de convertir el valor interno.
4. La auditoria de input conserva usuario y fecha del commit.
5. Un input valido queda `Dirty` hasta confirmacion HTTP.
6. El solver no convierte por si solo una revision en `Saved`.
7. Un fallo del solver no elimina el input valido.
8. Un fallo de guardado mantiene la revision `Dirty` y permite reintento.
9. Una respuesta de guardado antigua no limpia cambios posteriores.
10. Viewer no puede aplicar el cambio ni provocar simulacion o autosave.

## Politica De Autosave Propuesta

### Inputs Y Specifications

- Debounce corto para agrupar commits cercanos.
- Guardado serializado por proyecto.
- Si llega un cambio durante `Saving`, se guarda despues la revision mas reciente.
- No se ejecutan peticiones paralelas que puedan sobrescribirse fuera de orden.

### Cambios Visuales

- Debounce independiente y mayor que el de inputs.
- No requieren solver.
- Pueden combinarse con una revision funcional pendiente del mismo diagrama.

### Topologia

- Se considera una operacion atomica de dominio.
- Si afecta dos diagramas, ambos se incluyen en la misma persistencia logica.
- El guardado no se emite hasta que ambos extremos sean reconstruibles.

## Solver Fallido: Decision Aprobada M01

La intencion de usuario validada debe guardarse aunque la simulacion no converja o
falle. El estado de simulacion se presenta por separado y se recalcula al cargar.

Motivo: no guardar el input por un fallo numerico puede hacer perder trabajo y mezcla
persistencia de intencion con validez del resultado calculado.

Esta decision fue aprobada funcionalmente en M01.

## Decisiones Aprobadas M01

1. Los cambios funcionales nunca serializan el modelo mutable mientras el solver esta
   en estado `Running`.
2. El snapshot inmutable conserva exclusivamente intencion y se captura antes de la
   simulacion correspondiente.
3. Al finalizar la simulacion, el snapshot se guarda aunque el resultado sea no
   convergente o fallido.
4. Si ya existe una revision local posterior, una respuesta o snapshot anterior no la
   marca como `Clean` ni la sobrescribe.
5. Los cambios visuales mantienen su debounce independiente y no esperan solver.

Decisiones aprobadas por Alfonso en M01 el 2026-07-14.

## Conflicto Realtime

Si llega una version remota mientras existen cambios locales `Dirty`, el cliente no
debe reemplazar silenciosamente el proyecto en memoria.

La estrategia exacta se definira en la spec de concurrencia. Hasta entonces se exige:

- detectar la condicion;
- conservar la intencion local;
- no afirmar que la recarga fue aplicada correctamente;
- no enviar un documento atrasado sin comprobar version.

## Errores

| Falla | Resultado esperado |
|---|---|
| Permiso insuficiente | Rechazar antes de mutar |
| Valor o unidad invalida | Mostrar validacion y conservar valor anterior |
| Solver no converge | Conservar y guardar intencion; mostrar no convergencia |
| Solver lanza excepcion | Conservar intencion; mostrar fallo; permitir reintento |
| HTTP falla | Mantener `Dirty`; reintentar con politica limitada |
| Servidor rechaza version | No sobrescribir; iniciar resolucion de conflicto |
| Usuario cambia de proyecto | Finalizar, cancelar o conservar explicitamente el pendiente |

## Criterios De Aceptacion

1. Confirmar una temperatura valida aplica exactamente un cambio de dominio.
2. El componente que edita la temperatura no llama directamente al solver ni a HTTP.
3. Dos inputs rapidos no producen guardados paralelos fuera de orden.
4. Un autosave confirmado devuelve y registra la nueva version del proyecto.
5. Un fallo HTTP no muestra el proyecto como guardado.
6. Un input valido permanece despues de una simulacion no convergente.
7. Cambiar unidad de visualizacion se persiste sin convertirlo en resultado calculado.
8. Editar composicion incompleta no ejecuta solver hasta cumplir la regla funcional
   definida para composiciones.
9. Un cambio de formula conserva formula, autor y fecha.
10. Un cambio remoto no elimina silenciosamente un input local pendiente.

## Pruebas Requeridas

- Commit valido de `Variable<T>`.
- Texto invalido y unidad incompatible.
- Limpiar un input previamente definido.
- Cambio de unidad de visualizacion.
- Composicion completa e incompleta.
- Specification creada, editada y eliminada.
- Solver convergente, no convergente y fallido.
- Guardado exitoso, timeout y rechazo de version.
- Cambio nuevo mientras el guardado anterior esta activo.
- Realtime recibido con revision limpia y con revision sucia.
- Usuario Viewer intentando editar.

## Objetivos De Refactor Posteriores

- Componentes `UIVariable*`.
- Grillas de composicion.
- `EquipmentBaseFormulaSpecifications`.
- `FlowsheetManager.RunSimulation` y su autosave asociado.
- Persistencia visual dentro de `ProjectSessionService`.

## Fuera De Alcance

- Resolver todavia la estrategia completa de merge multiusuario.
- Persistir resultados calculados del solver.
- Cambiar el formato visual de los editores.
