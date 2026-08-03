# Persistence And Real-Time Collaboration Design - Distillator

Fecha: 11/07/2026

Este documento define el diseño inicial para persistencia en base de datos y colaboración en tiempo real de Distillator.

El objetivo es lograr una experiencia tipo Google Sheets: sin botón de guardar, con autosave, colaboración multiusuario, presencia visible y control de cambios. El diseño se implementará por fases para reducir riesgo.

---

## Objetivo Funcional

Distillator debe permitir que varios usuarios trabajen sobre un mismo proyecto al mismo tiempo, según permisos.

Comportamiento esperado:

- Guardado automático de cambios, sin botón de guardar.
- Colaboración por proyecto completo.
- Usuarios conectados visibles.
- Presencia visible: quién está editando o trabajando en un diagrama.
- Último cambio gana cuando dos usuarios cambian el mismo dato.
- Historial futuro: quién cambió qué dato y cuándo.
- Los resultados calculados del solver no se guardan como verdad principal; se recalculan al abrir.
- Se guardan los datos definidos por usuario y las configuraciones necesarias para reconstruir el proyecto.

---

## Decisiones Base

| Tema | Decisión |
|---|---|
| Técnica inicial | Autosave + SignalR + cambios granulares + versión |
| OT/CRDT | No implementar inicialmente; evaluar más adelante si aparece una necesidad real |
| Conflictos | Last write wins |
| Permisos | Owner / Editor / Viewer o equivalente |
| Colaboración | Por proyecto |
| Resultados del solver | Recalcular al abrir |
| Auditoría | Guardar quién cambió qué dato |
| Botón guardar | No usar |

---

## Modo De Trabajo Eficiente

Para esta línea de persistencia y colaboración, Codex debe trabajar con máxima relación calidad/costo:

- Leer solo archivos directamente relacionados con el flujo que se está corrigiendo.
- Evitar salidas largas como `git diff` completo salvo cuando ayuden a detectar cambios accidentales.
- Preferir búsquedas puntuales con `rg` y lecturas por fragmento.
- Aplicar cambios mínimos que corrijan la intención del endpoint o servicio.
- No agregar SignalR, colas, CRDT, servicios nuevos o refactors grandes hasta que la vertical HTTP esté estable.
- Mantener verificación proporcional: `dotnet build` y pruebas manuales dirigidas por flujo.
- Documentar reglas aprendidas solo cuando eviten repetir errores, como la regla EF Core de agregar entidades hijas nuevas por `DbSet` y FK explícita.

El objetivo es reducir ruido y consumo de tokens sin bajar calidad técnica.

---

## Fases De Implementación

### Fase 1 - Configuración Básica Del Proyecto

Guardar solo la configuración principal del proyecto.

Incluye:

- Proyecto:
  - Id
  - Nombre
  - Owner/UserId
  - Fecha de creación
  - Fecha de última modificación
  - Versión de cambio
- Método termodinámico seleccionado.
- Configuración de cámara/canvas por defecto.
- Configuración de reportes.
- Configuración de diseño de equipos.
- Configuración de naming.
- Diagramas básicos:
  - Id
  - Nombre
  - Tipo de diagrama
  - Número/prefijo de diagrama cuando aplique
  - Orden
  - Configuración visual básica.

No incluye todavía:

- Sistemas de unidades personalizados.
- Equipos.
- Corrientes.
- Conexiones.
- Resultados del solver.

Estado actual:

- Entidades EF, configuraciones, migración y endpoints HTTP iniciales creados.
- Base de datos actualizada con tablas de proyectos, diagramas, colaboradores y cambios.
- Cliente conectado parcialmente: creación de proyecto, listado por usuario y carga básica desde PostgreSQL funcionan.
- Persistencia de configuración y diagramas todavía requiere corrección de flujo y separación de responsabilidades.

### Estado Probado En UI

Validado:

- Usuario nuevo puede registrarse.
- Usuario existente puede iniciar sesión.
- Crear proyecto nuevo persiste en PostgreSQL.
- Cerrar y abrir la app conserva proyectos creados.
- Cambiar entre usuarios mantiene aislamiento de proyectos.
- `GetUserProjectsRequest` lista proyectos por usuario correctamente.
- `GetProjectRequest` carga el proyecto seleccionado.

Problemas detectados:

- Usuario nuevo recibe `Main Project` por defecto automáticamente. A futuro, si no tiene proyectos, debe ver estado vacío y decidir crear uno.
- `Delete Project` no persiste en base de datos; actualmente borra de memoria/UI porque no existe endpoint real de borrado.
- Crear diagramas no persiste correctamente.
- Al crear diagramas se está llamando `UpdateProjectConfigurationRequest`, aunque la intención real no es actualizar configuración del proyecto.
- El error `DbUpdateConcurrencyException` visto durante creación de diagramas se considera un síntoma de flujo incorrecto: se envía una actualización donde no corresponde, o sin una entidad/versión válida para ese caso.
- La configuración del proyecto no persiste completa: `ThermodynamicMethod` parece guardar, pero valores como `CameraZoom` / `MaxZoom` vuelven al default al cerrar y abrir.
- Login/Register necesitan revisión visual y de navegación: falta CSS y después de login el usuario queda autenticado, pero no siempre redirige a la app sin refrescar.

Decisión importante:

No tratar `DbUpdateConcurrencyException` como una corrección aislada. Primero corregir la intención del flujo: no llamar `UpdateProjectConfigurationRequest` desde operaciones de creación/edición de diagramas ni desde carga/inicialización si no hay cambio real de configuración.

### Corrección Arquitectónica Pendiente

Separar endpoints y servicios por intención:

- `CreateProjectRequest`: solo crea proyecto. No debe crear diagramas ni guardar configuración mezclada salvo defaults mínimos del proyecto.
- `UpdateProjectConfigurationRequest`: solo actualiza configuración del proyecto cuando el usuario realmente cambia configuración.
- `CreateDiagramRequest`: crea diagrama.
- `UpdateDiagramRequest`: actualiza nombre, tipo, número/prefijo y metadata visual del diagrama.
- `DeleteDiagramRequest`: elimina diagrama.
- `DeleteProjectRequest`: elimina o archiva proyecto en base de datos.

Regla:

No seguir agregando llamadas sueltas a `PersistCurrentProjectConfigurationAsync` para resolver síntomas. Primero revisar `ProjectSessionService`, `ProjectExplorer`, `ProjectFormDialog` y los servicios de diagramas para que cada acción llame al request correcto.

### Fase 2 - Sistemas De Unidades Del Proyecto

Agregar persistencia de unidades.

Incluye:

- Sistemas incorporados:
  - SI
  - English
- Sistemas creados por usuario.
- Sistema activo del proyecto.
- Unidad seleccionada por magnitud:
  - Pressure
  - Temperature
  - Mass Flow
  - Molar Flow
  - Energy
  - Power
  - Length
  - Density
  - Viscosity
  - Thermal Conductivity
  - etc.

Reglas:

- SI y English son base del proyecto.
- El usuario puede crear un sistema nuevo desde el sistema actualmente seleccionado.
- El sistema activo aplica a nuevas entradas/visualizaciones.
- Cambios específicos de unidades deben poder persistirse.

### Fase 3 - Modelo De Simulación

Agregar persistencia del flowsheet completo.

Incluye:

- Equipos:
  - Id
  - Tag/Name
  - Tipo
  - Posición en diagrama
  - Configuración de diseño
  - Datos definidos por usuario
- Corrientes:
  - Id
  - Tag/Name
  - Tipo
  - Datos definidos por usuario
  - Composición definida por usuario
  - Condiciones definidas por usuario
- Conexiones:
  - Source equipment/port
  - Target equipment/port
  - Stream asociada
  - Referencias visuales del pipe
- Specifications:
  - Tipo
  - Equipo dueño
  - Source/destination
  - Variable
  - Valor/fórmula
- Diagramas:
  - Elementos visibles
  - Posiciones
  - Pipes
  - Off-page connectors

No guardar como verdad principal:

- Variables calculadas por solver.
- Perfiles calculados.
- Resultados transitorios.

Estos resultados se recalculan al abrir o ejecutar simulación.

---

## Arquitectura Propuesta

```text
Client Blazor WebAssembly
    -> ProjectStateService
    -> ProjectRealtimeService
    -> SignalR Hub
    -> Server Application Service
    -> EF Core
    -> PostgreSQL
    -> SignalR broadcast
    -> Other clients
```

### Shared

Responsabilidad:

- DTOs.
- Commands de cambios.
- Events de cambios aceptados.
- Enums compartidos.
- Contratos simples que viajan entre Client y Server.

No debe contener:

- DbContext.
- Acceso a PostgreSQL.
- Lógica de SignalR.

### Server

Responsabilidad:

- EF Core.
- PostgreSQL.
- Hubs SignalR.
- Controllers REST para carga inicial.
- Validación de permisos.
- Aplicación de cambios.
- Auditoría.

Regla:

El Hub debe ser delgado. No debe contener lógica compleja de dominio ni simulación.

Regla EF Core para endpoints de persistencia:

- Cuando un endpoint modifica un proyecto ya cargado y necesita crear entidades dependientes nuevas, como `ProjectChangeLog`, `ProjectDiagramRecord` u otras tablas hijas futuras, no agregarlas mediante navegación (`project.ChangeLogs.Add(...)`, `project.Diagrams.Add(...)`) sobre un grafo ya trackeado.
- Agregar entidades nuevas con el `DbSet` correspondiente (`context.ProjectChangeLogs.Add(...)`, `context.ProjectDiagrams.Add(...)`) y asignar claves foráneas explícitas (`ProjectId = project.Id`, `TenantId = project.TenantId`).
- Esta regla evita que EF Core trate entidades nuevas con `Guid` ya asignado como `Modified` y ejecute `UPDATE` en vez de `INSERT`, lo que produce `DbUpdateConcurrencyException` con `0 rows affected`.
- Las navegaciones pueden usarse para lectura y composición de DTOs, pero las mutaciones persistentes deben expresar claramente si la intención es `Add`, `Update` o `Delete`.

### Client

Responsabilidad:

- UI.
- Estado local reactivo.
- Conexión SignalR.
- Cola de cambios pendientes.
- Indicadores visuales de sincronización/presencia.

Regla:

Los componentes no deben hablar directamente con SignalR si la interacción empieza a crecer. Deben usar servicios como `ProjectRealtimeService` y `ProjectStateService`.

---

## Carga Inicial Y Sincronización

Al abrir un proyecto:

1. Cliente hace `GET` HTTP al servidor para traer la última versión completa necesaria.
2. Cliente inicializa `ProjectStateService`.
3. Cliente abre conexión SignalR.
4. Cliente ejecuta `JoinProject(projectId)`.
5. Servidor agrega la conexión al grupo del proyecto.
6. Servidor notifica presencia a otros usuarios.

SignalR no reemplaza la carga inicial. SignalR solo sincroniza cambios posteriores.

---

## Modelo De Cambios

Cada acción del usuario debe expresarse como un cambio granular.

Ejemplos:

```text
ProjectNameChanged
ThermodynamicMethodChanged
NamingConfigurationChanged
DiagramCreated
DiagramRenamed
DiagramNumberChanged
CameraConfigurationChanged
UnitSystemCreated
ActiveUnitSystemChanged
EquipmentCreated
EquipmentMoved
EquipmentRenamed
StreamCreated
StreamConnected
StreamUserVariableChanged
SpecificationCreated
SpecificationUpdated
SpecificationDeleted
```

Cada cambio debe contener:

- `ProjectId`
- `ChangeId`
- `ChangeType`
- `EntityId`
- `Payload`
- `ClientVersion`
- `UserId`
- `TimestampUtc`

El servidor responde con:

- Cambio aceptado.
- Nueva versión del proyecto.
- Evento para otros usuarios.

---

## Concurrencia

Decisión actual: último cambio gana.

Modelo recomendado:

- Cada proyecto tiene `Version`.
- Cada cambio aceptado incrementa `Version`.
- El servidor guarda auditoría.
- Si dos usuarios cambian el mismo campo, gana el último cambio aceptado por el servidor.
- Los clientes reciben el evento y actualizan su estado local.

No bloquear inicialmente:

- No usar locks para cada campo.
- No usar edición exclusiva de proyecto.

Excepción futura:

Para operaciones críticas, como borrar equipos conectados o cambiar estructura compleja, se puede pedir confirmación o aplicar reglas especiales.

---

## Auditoría

Se debe preparar desde el inicio una tabla/event log.

Objetivo:

- Saber quién cambió qué dato.
- Permitir historial futuro.
- Facilitar debugging de colaboración.
- Preparar versiones anteriores o undo futuro.

Registro sugerido:

- Id
- ProjectId
- UserId
- UserDisplayName
- ChangeType
- EntityType
- EntityId
- OldValueJson
- NewValueJson
- ProjectVersion
- CreatedAtUtc

Inicialmente no necesitamos construir UI de historial, pero sí conviene guardar los datos.

---

## Presencia

La presencia es en tiempo real y no necesariamente persistente.

Debe mostrar:

- Usuarios conectados al proyecto.
- Diagrama activo de cada usuario.
- Posible estado: viewing, editing, solving, disconnected.

Eventos SignalR sugeridos:

```text
UserJoinedProject
UserLeftProject
UserChangedActiveDiagram
UserStartedEditing
UserStoppedEditing
```

El servidor puede mantener presencia en memoria por conexión.

Más adelante, si hay múltiples instancias del servidor, se puede usar Redis/backplane.

---

## Autosave

No hay botón de guardar.

Reglas:

- Inputs de texto/número usan debounce.
- Cambios estructurales se envían inmediatamente.
- Movimientos de equipos pueden agruparse:
  - mover en vivo local;
  - enviar al soltar;
  - opcional: enviar presencia de movimiento sin persistir cada pixel.

Delay sugerido:

- Inputs simples: 500 ms.
- Cambios de configuración compleja: al confirmar dialog.
- Movimiento de equipos: al terminar drag.

---

## Permisos

Cada cambio debe validar permiso en servidor.

Roles funcionales iniciales:

- Owner: puede administrar proyecto, usuarios y permisos.
- Editor: puede modificar proyecto.
- Viewer: puede ver, pero no modificar.

Regla:

El cliente puede ocultar controles, pero la seguridad real debe estar en Server.

---

## SignalR

Hub inicial sugerido:

```text
ProjectCollaborationHub
```

Métodos iniciales:

```text
JoinProject(projectId)
LeaveProject(projectId)
SubmitChange(changeDto)
UpdatePresence(presenceDto)
```

Eventos enviados al cliente:

```text
ChangeAccepted
ProjectChanged
PresenceChanged
ForceProjectReload
ChangeRejected
```

Regla:

El Hub recibe el cambio, valida usuario, llama un servicio de aplicación, y emite eventos. El Hub no debe saber cómo renombrar equipos, cómo resolver el solver ni cómo validar reglas profundas.

### Deuda De Optimizacion - Reconciliacion Autoritativa

Estado actual probado:

- `ProjectAuthoritativeSyncService` usa `GetProjectRequest` cada 3 segundos como respaldo autoritativo.
- Este polling resolvio el caso offline-online donde SignalR o el navegador podian perder eventos.
- La regla actual es funcional y fue validada manualmente, pero produce muchas llamadas HTTP visibles en consola.

Riesgo:

- Si se mantiene siempre activo, puede generar ruido, carga innecesaria en backend y potencial impacto de UX en proyectos grandes.

Tarea futura:

- Medir frecuencia, duracion y peso de `GetProjectRequest`.
- Reducir el polling con backoff, ventanas de recuperacion post-desconexion, version hints o activacion solo cuando SignalR este inestable.
- Mantener la garantia funcional: B debe recuperar cambios perdidos sin cambiar de proyecto/diagrama ni refrescar la pagina.

---

## Servicios Propuestos

### Server

```text
IProjectPersistenceService
IProjectChangeService
IProjectAccessService
IProjectAuditService
IProjectSnapshotService
IProjectPresenceService
```

### Client

```text
ProjectStateService
ProjectRealtimeService
ProjectAutosaveService
ProjectPresenceClientService
PendingChangeQueue
```

---

## Base De Datos - Borrador Conceptual

Fase 1:

```text
Projects
ProjectDiagrams
ProjectChangeLogs
ProjectCollaborators
```

Implementación inicial creada:

- `Projects`: raíz persistente del proyecto, tenanted, versionada, con configuración del proyecto en JSONB.
- `ProjectDiagrams`: diagramas básicos del proyecto, con nombre, tipo, número/prefijo y estado visual JSONB.
- `ProjectCollaborators`: permisos por usuario (`Owner`, `Editor`, `Viewer`).
- `ProjectChangeLogs`: auditoría de cambios granulares para autosave, historial futuro y colaboración.
- `ProjectEndPoint`: primera API HTTP para crear, listar, cargar y actualizar configuración básica del proyecto.
- `Shared/Projects/ProjectPersistenceDtos.cs`: contratos iniciales cliente/servidor para fase 1.

Se decidió no separar todavía `ProjectConfigurations` en una tabla propia. En la fase inicial queda dentro de `Projects` usando columnas JSONB para mantener bajo el riesgo mientras se estabilizan los DTOs.

Migración aplicada:

```text
20260712024215_AddProjectPersistence
```

Nota técnica:

`TenantId` inicia como el `OwnerUserId` del proyecto. Esto mantiene la fase inicial simple y deja abierta la evolución futura hacia organizaciones/empresas.

Fase 2:

```text
ProjectUnitSystems
ProjectUnitSelections
```

Fase 3:

```text
ProjectEquipments
ProjectStreams
ProjectConnections
ProjectSpecifications
FlowsheetElements
FlowsheetPipes
```

Nota:

La estructura exacta se debe diseñar revisando los modelos actuales de `Distillator.Domain`, `Shared`, `Client` y `Server`.

---

## Manejo Del Solver

Los resultados calculados no son la verdad persistida.

Al abrir un proyecto:

1. Se cargan datos definidos por usuario.
2. Se reconstruye el modelo en memoria.
3. El usuario ejecuta o el sistema recalcula si corresponde.

Esto evita guardar estados calculados obsoletos o inconsistentes.

Excepción futura:

Podrían guardarse snapshots de resultados para reportes o comparación, pero no como fuente primaria del modelo.

---

## UI Esperada

La UI debe mostrar estado sin interrumpir.

Indicadores:

- Conectado.
- Reconectando.
- Sin conexión.
- Guardando.
- Guardado.
- Cambios pendientes.
- Usuario conectado.
- Usuario editando diagrama.

No usar popups para cada guardado.

Los conflictos bajo "último cambio gana" no deben molestar al usuario salvo que afecten una operación crítica.

---

## Riesgos Y Decisiones Pendientes

1. Definir si el cambio log guarda `OldValueJson` desde fase 1 o se agrega después.
2. Definir si los cambios se guardan como eventos además de tablas normalizadas.
3. Diseñar la conversión entre modelo de dominio en memoria y modelo persistente EF Core.
4. Definir qué propiedades de cada equipo/corriente son datos de usuario y cuáles son calculadas.
5. Definir estrategia para cambios offline o reconexión prolongada.
6. Definir si una operación compleja se guarda como un solo change set o como varios cambios pequeños.

---

## Primera Implementación Recomendada

Primero construir una vertical mínima:

1. Guardar proyecto básico en PostgreSQL.
2. Cargar proyecto por HTTP.
3. Cambiar nombre del proyecto con autosave.
4. Persistir cambio.
5. Emitir cambio por SignalR.
6. Otro cliente ve el cambio.
7. Registrar auditoría mínima.

Después ampliar a:

1. Configuración de naming.
2. Diagramas.
3. Configuración de unidades.
4. Equipos/corrientes.

Esta vertical mínima prueba toda la cadena antes de persistir el modelo grande.

---

## Plan Activo - Realtime Fase 1

Objetivo:

Agregar actualización en vivo acotada sobre la persistencia ya probada, sin cambiar todavía el modelo de guardado ni introducir CRDT/OT.

Alcance inicial:

- Proyectos compartidos.
- Configuración de proyecto.
- Creación, edición y eliminación de diagramas.
- Cambios de sharing/permisos.
- Recarga automática del proyecto activo en otros navegadores cuando otro usuario guarda cambios.

Decisiones:

- HTTP sigue siendo la fuente de guardado y validación.
- SignalR solo notifica cambios aceptados por el servidor.
- Al recibir un cambio externo, el cliente recarga el proyecto por HTTP (`GetProjectRequest`) en vez de aplicar patches granulares.
- El cliente ignora eventos originados por el mismo usuario.
- La primera fase no incluye CRDT, OT, cola offline, cursores, locks ni presencia visual avanzada.

Implementación fase 1:

1. Crear DTO compartido `ProjectRealtimeEventDto`.
2. Crear `ProjectCollaborationHub` en Server.
3. Registrar SignalR y mapear `/projectCollaborationHub`.
4. Validar acceso del usuario en `JoinProject`.
5. Emitir evento desde endpoints ya probados después de guardar correctamente.
6. Crear `ProjectRealtimeService` en Client con reconexión automática.
7. Integrar `ProjectSessionService` para unirse al proyecto activo y recargar el proyecto al recibir eventos externos.
8. Probar con dos navegadores: Owner/Editor/Viewer.

Prueba esperada de fase 1:

- Usuario A y usuario B abren el mismo proyecto compartido en navegadores distintos.
- Usuario A crea un diagrama.
- Usuario B ve el nuevo diagrama sin refrescar.
- Usuario B cambia configuración si es Editor.
- Usuario A ve la configuración actualizada sin refrescar.
- Viewer recibe actualizaciones, pero no puede generarlas.

## Plan Activo - Realtime Fase 2

Objetivo:

Agregar presencia mínima para saber quién está conectado al proyecto y qué diagrama está viendo, sin persistir ese estado ni introducir locks, cursores o edición colaborativa por campo.

Decisiones:

- La presencia es estado efímero en memoria del Hub.
- No requiere migración ni tabla nueva.
- `ProjectSessionService` sigue siendo el orquestador del cliente; los componentes no hablan directamente con SignalR.
- La UI muestra otros usuarios conectados al proyecto activo y el diagrama que están viendo.

Implementación fase 2:

1. Agregar `ProjectPresenceDto`.
2. Extender `ProjectCollaborationHub` con presencia por `ConnectionId`.
3. Enviar presencia al hacer `JoinProject`, `LeaveProject`, desconexión y cambio de diagrama activo.
4. Extender `ProjectRealtimeService` con evento `ProjectPresenceChanged`.
5. Integrar `ProjectSessionService` para publicar el diagrama activo.
6. Mostrar presencia compacta en el header del proyecto.

Prueba esperada de fase 2:

- A y B abren el mismo proyecto compartido en navegadores distintos.
- A ve a B conectado y B ve a A conectado sin refrescar.
- A cambia de diagrama; B ve actualizado el nombre del diagrama activo de A.
- B cambia de proyecto; A deja de verlo en el proyecto anterior.
- B cierra sesión o pestaña; A deja de verlo después de la desconexión.

## Plan Activo - Realtime Fase 3

Objetivo:

Persistir y sincronizar el estado visual de equipos y corrientes sin botón de guardar, manteniendo la UI fluida.

Decisiones:

- Separar visual y simulación.
- La fase visual guarda equipos, referencias, pipes y cámara dentro de `ProjectDiagram.CanvasStateJson`.
- La fase visual no guarda todavía datos internos de Facade, especificaciones, resultados ni variables de simulación.
- Las acciones del canvas actualizan primero el estado en memoria y la UI; el guardado se dispara en segundo plano.
- Crear, mover al soltar, rotar, flip, conectar, desconectar y borrar disparan autosave visual.
- Movimiento continuo, pan y zoom no deben guardar por cada pixel; se usarán debounce o eventos finales.
- SignalR sigue notificando solo cambios aceptados por el servidor.

Implementación inicial fase 3:

1. Serializar snapshot visual del diagrama en `CanvasStateJson`.
2. Rehidratar equipos visuales y pipes desde `CanvasStateJson` al cargar proyecto.
3. Conectar `FlowsheetManager` con un evento de autosave visual no bloqueante.
4. Encolar guardado con debounce por diagrama desde `ProjectSessionService`.
5. Reutilizar `UpdateDiagramRequest` para persistir y disparar realtime.

Prueba esperada fase 3:

- A crea un equipo desde paleta; B lo ve sin refrescar.
- A mueve un equipo y al soltarlo B ve la nueva posición.
- A rota o hace flip; B ve el cambio.
- A crea una corriente/conexión; B la ve.
- A borra un equipo; B deja de verlo.
- Al cerrar y abrir la app, el estado visual permanece.

## Avance - Persistencia Facade Y Unidades 2026-07-13

Decisión principal:

- Persistir intención de usuario, no resultados calculados.
- El server/PostgreSQL conserva estado mínimo; `Shared`/dominio reconstruyen el grafo y recalculan.
- No crear columnas ni migraciones por cada nueva variable de equipo.
- Cualquier `Variable<T>` nueva debe ser detectable por el serializer/aplicador sin tocar EF Core.

Diseño implementado:

- `ProjectUnitSystemApplier` aplica defaults de unidades por tipo genérico `Variable<T>`.
- `FacadeStateSerializer` serializa estado de Facade dentro del `CanvasStateJson` del elemento.
- Persistencia mínima:
  - `UserInput`;
  - `Specification`;
  - overrides manuales de unidades;
  - propiedades simples explícitamente soportadas, hoy `CompositionOrchestrator.InputType`.
- Refresco transitorio para diálogos abiertos:
  - usa estado completo en memoria;
  - no aumenta el JSON persistido;
  - permite que diálogos abiertos vean clears, resultados calculados y cambios de unidades.

Reglas de carga:

1. Crear visual/equipo/corriente con su constructor normal.
2. Registrar en solver.
3. Aplicar método termodinámico completo del proyecto.
4. Aplicar sistema de unidades activo del proyecto.
5. Restaurar inputs/overrides de Facade.
6. Reconectar pipes.
7. Ejecutar simulación una vez para regenerar resultados.

Reglas de UI:

- Viewer no puede editar variables, composición, nombres ni conexiones desde diálogos.
- Owner/Editor sí pueden editar.
- La UI debe mostrar cambios en vivo incluso si el diálogo está abierto.
- Los valores definidos por usuario se muestran en azul.
- `°GL` se muestra azul si la composición fue definida por usuario, aunque el usuario actual sea Viewer.

Estado validado:

- Corriente:
  - valores definidos por UI persisten;
  - cambio de unidades persiste;
  - cambio de sistema de unidades del proyecto se refleja en diálogo abierto;
  - clears se reflejan en diálogo abierto;
  - composición definida por UI conserva `InputType`;
  - Viewer no puede editar.
- Bomba:
  - diálogo abre correctamente;
  - parámetros propios persisten al cerrar/reabrir;
  - cálculo funciona con las variables restauradas.

Estado validado adicional:

- Corrientes y equipos visuales:
  - creación, conexión, movimiento, rotación, flip y eliminación se persisten;
  - los cambios se propagan en vivo a otros usuarios;
  - la carga inicial reconstruye equipos, corrientes, pipes y estado visual.
- Método termodinámico:
  - el proyecto carga el método completo por Id;
  - el solver recibe `ThermodynamicMethodFullDto`;
  - corrientes existentes cargan composición y método correctamente.
- Autosave:
  - se ejecuta en segundo plano;
  - no muestra snackbar por cada guardado;
  - se difiere mientras el solver está resolviendo.
- Solver:
  - limpia variables calculadas por ecuaciones antes de reintentar;
  - no conserva residuos viejos como `DeltaP` de bomba cuando desaparece una especificación/condición requerida.

Validación manual final 2026-07-20:

- Persistencia multiusuario confirmada por Alfonso:
  - otro usuario abre el proyecto y ve topología, inputs UI, specifications y resultados esperados;
  - autosave/realtime no pisa valores definidos por UI.
- Reconexion de corrientes confirmada:
  - `FacadeStream.SetThermodynamicMethod` se hizo idempotente para no reconstruir
    `Composition` cuando el método efectivo ya es el mismo;
  - `S-106` conserva composición UI al desconectar/reconectar.
- Solver de equipos, `SolverStreamMixer`, specifications, realtime/autosave y UX de
  conexión quedaron cerrados funcionalmente por prueba manual en UI.

## Avance - Specifications Por Formula 2026-07-13

Decisión principal:

- Mantener el componente anterior de specifications, pero dejar la UI activa basada en formulas.
- No mezclar la UI vieja con la nueva.
- La nueva specification se define por expresión tipada, por ejemplo:
  - `S-102.MassFlow = 5 * S-103.MassFlow`
  - `S-103.Component.Ethanol.MassFlow = 0.2 * S-101.Component.Ethanol.MassFlow`
- Inicialmente se soportan flujos másicos globales y flujos másicos por componente.

Diseño implementado:

- `FormulaSpecification` e infraestructura de parser tipado.
- Validación de:
  - sintaxis;
  - paréntesis;
  - corriente existente;
  - componente existente;
  - dimensiones;
  - división por cero o valor no evaluable.
- La fórmula puede quedar `Pending` si faltan datos requeridos; no debe lanzar excepción ni confundir al solver.
- La UI tiene editor con autocomplete dentro del textarea:
  - antes del `=` solo sugiere corrientes conectadas al equipo;
  - después del `=` sugiere corrientes del flowsheet;
  - usa nombres reales generados por `NamingConfiguration`, no prefijos hardcodeados.
- Se permiten varias formulas por equipo.
- Las formulas se confirman con botón `Confirm`; escribir o validar no dispara solver.

Persistencia:

- Las formulas se guardan dentro del `CanvasStateJson` del equipo.
- Se persiste solo:
  - Id;
  - texto de formula;
  - usuario que la definió;
  - fecha UTC.
- Al cargar:
  - primero se reconstruyen equipos, variables y conexiones;
  - después se parsean/restauran formulas;
  - finalmente se ejecuta simulación una vez.
- No requiere migración ni columnas nuevas.

Estado validado:

- Formula global de flujo másico funciona.
- Formula por componente funciona.
- Dos usuarios pueden crear/editar specifications en vivo.
- Varias specifications en un mismo equipo funcionan.
- Las specifications persisten al cerrar y abrir.
- Al reabrir, el sistema recalcula y el equipo aparece calculado.

## Avance - Auditoría Ligera De Inputs 2026-07-13

Objetivo:

- En una app multiusuario, distinguir visualmente:
  - valores calculados por stream/solver/equipment;
  - valores definidos por usuario;
  - usuario y fecha de definición.

Diseño implementado:

- `Variable<T>` conserva:
  - `DefinedByUserId`;
  - `DefinedByUserName`;
  - `DefinedAtUtc`.
- `FacadeStateSerializer` persiste esa metadata junto al valor definido por usuario.
- Las entradas UI estampan el usuario actual al definir:
  - variables normales;
  - porcentajes;
  - unitless;
  - composición;
  - `°GL`.
- `FormulaSpecification` también conserva usuario y fecha.
- Tooltips:
  - calculado: `Calculated by: Stream/Solver/Equipment`;
  - definido: dos líneas, `Defined by: User` y fecha.

Regla:

- Esta auditoría vive por ahora en el estado persistido del canvas/facade.
- No reemplaza un change log histórico completo en servidor.

## Avance - Routing Inicial De Corriente Intermedia 2026-07-13

Problema:

- Al conectar equipo-equipo se crea una corriente intermedia de forma programática.
- La ubicación anterior estaba hardcodeada respecto al puerto origen.
- Eso producía rutas visuales feas o absurdas en algunos casos.

Diseño implementado:

- `ConnectionService` calcula la ubicación de la corriente intermedia con:
  - boquilla origen;
  - boquilla destino;
  - dirección visual de cada boquilla;
  - puerto upstream/downstream real;
  - proyección fuera de cada boquilla;
  - orientación dominante horizontal/vertical.
- La corriente se crea con rotación automática:
  - derecha;
  - izquierda;
  - abajo;
  - arriba.
- La conexión a espacio vacío conserva su comportamiento anterior.

Estado:

- Compila.
- Pendiente de prueba visual con distintas combinaciones de equipos y posiciones.

## Pendientes Reales Actuales

1. Ejecutar matriz de pruebas equipo por equipo:
   - columna;
   - bomba;
   - válvula;
   - mixer;
   - splitter;
   - flash/vessel;
   - intercambiadores.
2. Confirmar que cada diálogo respeta modo Viewer/Editor y refresco en vivo con diálogo abierto.
3. Ajustar fino del routing de corriente intermedia si las pruebas visuales muestran rutas todavía feas.
4. Mejorar formulas ante renombrado de corrientes:
   - hoy las formulas referencian nombres;
   - futuro recomendado: referencia estable por Id o migración automática de texto al renombrar.
5. Agregar pruebas de regresión del solver para:
   - limpieza de variables calculadas;
   - bomba `DeltaP`;
   - formulas globales;
   - formulas por componente;
   - division por cero/no evaluable.
6. Definir más adelante si la auditoría ligera debe convertirse en change log histórico completo en servidor.
7. Mantener regla de persistencia mínima:
   - guardar intención de usuario;
   - recalcular resultados;
   - no persistir resultados transitorios como verdad principal.

## Endurecimiento Realtime Y Conexiones Interdiagrama 2026-07-14

Problema observado en producción:

- La presencia y el diagrama activo se propagaban, pero algunos cambios de contenido no llegaban hasta presionar F5.
- La conexión SignalR estaba activa; el fallo estaba en el recorrido cliente de `ProjectChanged`.
- El callback se manejaba con `async void`, permitiendo recargas solapadas y errores no esperables por SignalR.

Reglas implementadas:

- Los handlers de cambios realtime deben devolver `Task` y ser esperados.
- Las recargas del proyecto se serializan con un único lock asíncrono.
- Una versión anterior no puede reemplazar un documento más reciente del mismo proyecto.
- Los autosaves de diagramas se envían secuencialmente.
- HTTP continúa siendo la fuente de verdad; SignalR solo notifica que debe recargarse.

Persistencia interdiagrama:

- Un OPC persiste:
  - flowsheet destino;
  - conector gemelo;
  - nombre del flowsheet;
  - equipo conectado;
  - dirección de flujo.
- Crear una conexión entre diagramas guarda ambos `CanvasStateJson` mediante `UpdateDiagramsRequest`, un único `SaveChanges` y una sola notificación realtime.
- No lanzar tareas independientes por cada extremo ni acceder concurrentemente al diccionario de debounce.
- La hidratación restaura primero diagramas/equipos/pipes y después reconstruye conexiones inter-flowsheet y enlaces del solver.
- Borrar un flowsheet es una operación de agregado, no un simple `List.Remove`:
  - desconectar ambos extremos;
  - retirar pipes y OPC;
  - limpiar puertos;
  - retirar equipos/corrientes del registry y solver;
  - guardar los diagramas sobrevivientes;
  - finalmente borrar el registro del diagrama.

## Contrato De Puertos En UI 2026-07-14

- Los diálogos deben consumir propiedades tipadas (`FeedPort`, `VaporPort`, `LiquidPort`, etc.) para puertos fijos.
- No usar `Ports.First(...)` con nombres literales en `EquipmentPortConnector`.
- `OnConnChanged` no es requisito general de persistencia: el conector ya llama al manager, solver y autosave.
- Usar callbacks de topología únicamente para equipos con sprouting de puertos dinámicos.
- Flash Tank tiene exactamente `Feed`, `Vapor` y `Liquid`; su UI no debe presentarlos como listas dinámicas.

## Pruebas Pendientes Inmediatas

1. Producción con dos usuarios:
   - A crea/mueve/elimina y B ve sin F5;
   - B crea/mueve/elimina y A ve sin F5.
2. Conexión interdiagrama:
   - crear y comprobar ambos extremos en vivo;
   - borrar uno de los diagramas;
   - confirmar que desaparece el OPC sobreviviente;
   - cerrar y abrir para confirmar persistencia.
3. Flash Tank:
   - abrir diálogo;
   - validar layout;
   - conectar `Feed`, `Vapor` y `Liquid`;
   - comprobar solver, autosave y realtime.
4. Continuar matriz equipo por equipo y pruebas de regresión del solver.
