# 07 - Realtime Y Presencia

Estado: Borrador

## Contexto

SignalR coordina usuarios conectados a un proyecto. HTTP sigue siendo la fuente de
verdad. Realtime debe informar versiones persistidas, presencia y diagrama activo sin
convertirse en una segunda ruta de datos.

## Responsabilidades

### SignalR Si Hace

- unir y abandonar grupos de proyecto;
- notificar una nueva version persistida;
- comunicar tipo general y autor del cambio;
- publicar presencia y diagrama activo;
- reconectar y volver a unir el grupo apropiado.

### SignalR No Hace

- persistir proyectos;
- transportar el documento completo como autoridad;
- ejecutar solver;
- decidir merges;
- marcar un autosave como confirmado;
- autorizar una operacion que HTTP rechazaria.

## Evento De Cambio

Debe contener como minimo:

```text
ProjectId
Version
ChangeType
EntityType
EntityId
ChangedByUserId
OccurredOnUtc
```

El receptor usa el evento para decidir si ya conoce la version, debe recargar o debe
iniciar resolucion de conflicto.

## Flujo De Recepcion

1. Validar que el evento pertenece al proyecto relevante.
2. Descartar versiones ya aplicadas.
3. Si el autor es el cliente actual, reconciliar la version con la respuesta HTTP sin
   recargar innecesariamente.
4. Si el estado local esta `Clean`, obtener por HTTP la version mas reciente.
5. Hidratar en aislamiento y aplicar solo si sigue siendo relevante.
6. Si el estado local esta `Dirty`, no reemplazarlo; activar la politica de conflicto.
7. Actualizar UI y presencia sin ejecutar callbacks `async void`.

## Orden Y Coalescencia

- Los eventos pueden llegar duplicados o fuera de orden.
- La version, no la hora del cliente, determina orden compartido.
- Si llegan N, N+1 y N+2 antes de recargar, basta obtener la version mas reciente.
- Una recarga activa puede quedar superada por otro evento; su resultado se descarta o
  se sigue inmediatamente con la version mas nueva.
- Las recargas de un proyecto se serializan con el estado local de persistencia.

## Reconexion

- Reconectar no implica que se recibieron los eventos perdidos.
- Al reconectar se vuelve a unir el proyecto actual.
- Se actualiza diagrama activo.
- Se compara la version local con HTTP.
- La perdida de SignalR no bloquea edicion HTTP, pero la UI puede mostrar estado de
  colaboracion degradado.

## Presencia

- Se muestra por usuario, no por conexion duplicada.
- Incluye proyecto y diagrama activo.
- Al cambiar proyecto o cerrar conexion se retira la presencia anterior.
- Presencia es efimera y no altera version ni auditoria del proyecto.
- Multiples pestanas del mismo usuario siguen una politica determinista de
  consolidacion.

## Escalamiento

La presencia en memoria del servidor es aceptable mientras exista una sola instancia.
Antes de usar multiples instancias se requerira backplane o almacenamiento compartido.
No se introduce esa infraestructura anticipadamente.

## Invariantes

1. Todo evento de cambio corresponde a un commit exitoso.
2. HTTP determina el documento que se aplica.
3. Una version menor o igual a la aplicada se ignora.
4. Una recarga atrasada no reemplaza una version mas nueva.
5. Estado local `Dirty` no se descarta silenciosamente.
6. Reconexion verifica posibles eventos perdidos.
7. Presencia no modifica el proyecto.
8. Handlers asincronos devuelven `Task` y sus fallos se observan.
9. La ausencia de SignalR no convierte HTTP exitoso en fallo de persistencia.

## Criterios De Aceptacion

1. A guarda y B aplica por HTTP la nueva version sin F5.
2. Eventos duplicados producen una sola aplicacion.
3. Eventos fuera de orden terminan en la version mayor.
4. Una recarga lenta anterior no reemplaza la mas nueva.
5. B con cambios `Dirty` no pierde su trabajo al recibir el evento de A.
6. Reconexion detecta un cambio ocurrido durante la desconexion.
7. Cambiar de proyecto actualiza correctamente grupos y presencia.
8. Cerrar una sesion retira su presencia.
9. Un fallo SignalR despues del commit no repite la escritura HTTP.

## Pruebas Requeridas

- A -> B con proyecto y diagrama activos.
- Autor recibe su propio evento.
- Duplicados y orden invertido.
- Eventos agrupados durante recarga.
- Realtime con estado `Clean` y `Dirty`.
- Desconexion, reconexion y evento perdido.
- Cambio rapido de proyecto.
- Multiples pestanas del mismo usuario.
- Viewer recibiendo cambios.
- SignalR no disponible con HTTP operativo.

## Objetivos De Refactor Posteriores

- `ProjectRealtimeService`.
- Manejo realtime dentro de `ProjectSessionService`.
- `ProjectCollaborationHub`.
- Emision desde endpoints despues del commit.

## Fuera De Alcance

- Enviar cada mutacion por SignalR como fuente de verdad.
- Presencia durable historica.
- Infraestructura multi-instancia antes de requerirla.

