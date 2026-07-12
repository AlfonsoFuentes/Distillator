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
