# 06 - Persistencia Y Concurrencia

Estado: Borrador

## Contexto

Distillator persiste configuracion de proyecto, documentos de diagrama, preferencias
por usuario y auditoria. Varios usuarios pueden editar un proyecto y cada autosave
actualiza una version monotona.

## Problema Actual

- La version se devuelve y se notifica, pero no se exige como precondicion al guardar.
- Un diagrama se reemplaza como documento completo.
- Dos peticiones pueden terminar fuera del orden en que fueron creadas.
- Un error HTTP suele dejar la mutacion local aplicada sin estado `Dirty` visible.
- La auditoria puede registrar documentos completos para cambios pequenos.
- Guardados y recargas utilizan coordinacion independiente.

## Fuente De Verdad

- PostgreSQL contiene la revision compartida confirmada.
- El cliente contiene una copia local con una version base y posibles cambios `Dirty`.
- Una respuesta exitosa del backend entrega la nueva version confirmada.
- SignalR anuncia esa version, pero no reemplaza la confirmacion HTTP del autor.

## Contrato De Escritura

Toda escritura de proyecto debe incluir conceptualmente:

```text
ProjectId
ExpectedVersion
OperationId
ChangeKind
Payload
```

El backend acepta la operacion solo si la version esperada satisface la politica de
concurrencia de esa operacion. La forma final del contrato se decidira al implementar.

## Granularidad Propuesta

- Configuracion: recurso proyecto.
- Diagrama: recurso diagrama con version global de proyecto inicialmente.
- Conexion interdiagrama: lote atomico de diagramas.
- Preferencias de workspace: recurso independiente por usuario.
- Sharing: recurso proyecto, solo Owner.

Se empieza con version global de proyecto por simplicidad. Una version por diagrama se
considerara solo si los conflictos legitimos entre diagramas distintos resultan
frecuentes y medibles.

## Autosave Serializado

1. Un cambio valido incrementa una revision local.
2. El coordinador conserva el snapshot persistible mas reciente.
3. Existe como maximo una escritura activa por proyecto.
4. Los cambios recibidos durante `Saving` quedan pendientes.
5. Una respuesta solo confirma la revision que envio.
6. Si quedan cambios, se guarda despues el snapshot mas reciente.
7. Una respuesta atrasada nunca limpia una revision posterior.

## Conflicto De Version

Cuando `ExpectedVersion` no coincide:

- el servidor rechaza sin aplicar el payload;
- devuelve version actual y datos suficientes para iniciar reconciliacion;
- no crea auditoria de cambio aplicado;
- no emite evento de cambio como si el guardado hubiese sido exitoso;
- el cliente conserva su intencion local y deja de reintentar a ciegas.

La estrategia de merge por tipo de cambio se definira de forma incremental:

- cambios visuales independientes pueden ser reaplicables;
- inputs sobre variables diferentes pueden ser combinables en el futuro;
- cambios sobre la misma variable requieren una decision visible o una regla
  determinista aprobada;
- topologia y eliminaciones no se mezclan automaticamente sin validacion completa.

## Atomicidad

Son una sola transaccion:

- actualizar configuracion y renombrar equipos cuando la operacion lo exija;
- crear o eliminar una conexion interdiagrama y guardar ambos extremos;
- eliminar un diagrama y actualizar diagramas sobrevivientes afectados;
- actualizar sharing y su auditoria.

## Idempotencia

Una operacion con el mismo `OperationId` no debe aplicarse dos veces si el cliente
reintenta despues de perder la respuesta. La necesidad de persistir identificadores de
operacion se confirmara al implementar reintentos de red.

## Auditoria

- Registra usuario, fecha, operacion, entidad, revision y cambio significativo.
- Inputs y formulas conservan autoria en su propio estado persistido.
- Mover un equipo no necesita copiar todo el proyecto en auditoria.
- No registra resultados calculados como si fueran intenciones.
- Los valores sensibles no se escriben en mensajes ni logs.

## Invariantes

1. Una version de proyecto aumenta una vez por transaccion aceptada.
2. Una operacion rechazada no cambia datos ni version.
3. Existe como maximo un autosave activo por proyecto y cliente.
4. La confirmacion de revision N no limpia N+1.
5. Un lote es completamente aplicado o completamente rechazado.
6. Un Viewer no produce escritura ni auditoria de cambio.
7. Workspace state de un usuario no modifica la version compartida del proyecto.
8. SignalR se emite despues del commit.
9. Un fallo conserva estado local `Dirty` y diagnostico.
10. No hay reintentos infinitos.

## Criterios De Aceptacion

1. Dos escrituras del mismo cliente se aplican en orden.
2. Una respuesta lenta no sobrescribe una revision posterior.
3. Dos clientes con la misma version base producen un guardado y un conflicto.
4. Un conflicto conserva la intencion del cliente rechazado.
5. Un lote interdiagrama no puede guardar solo un extremo.
6. Un timeout permite determinar si la operacion fue aplicada antes de reintentar.
7. La version confirmada se actualiza en el cliente autor.
8. La auditoria identifica el cambio sin crecimiento desproporcionado.

## Pruebas Requeridas

- Guardado unico y secuencia de guardados.
- Cambio durante una peticion activa.
- Respuestas artificialmente fuera de orden.
- Conflicto entre A y B.
- Configuracion, diagrama, lote, sharing y workspace state.
- Timeout antes y despues del commit.
- Reintento con mismo identificador.
- Fallo de base de datos y rollback.
- Auditoria de input, formula, topologia y cambio visual.

## Objetivos De Refactor Posteriores

- DTOs de persistencia.
- Endpoints de proyecto.
- Coordinacion de autosave en Client.
- `ProjectRecord.Version` y configuracion EF.
- `ProjectChangeLog`.

## Fuera De Alcance

- Edicion colaborativa caracter por caracter.
- CRDT u operational transformation sin necesidad demostrada.
- Merge automatico general de topologia.

