# Spec 08 — Client: Limpiar Legacy + Dividir ProjectSessionService + DI Solver

## Estado
Pendiente

## Archivos afectados
- `Client/Services/ProjectSessionService.cs` — demasiado grande, múltiples responsabilidades
- `Client/Services/EquipmentManagers/WorkspaceManager.cs` — 100% comentado
- `Client/Services/EquipmentManagers/SimulationOrchestrator.cs` — 100% comentado
- `Client/Services/EquipmentManagers/ConnectionService.cs` — 100% comentado
- `Client/Layout/Diagrams/PfdCanvas.razor` — 300+ líneas comentadas
- `Client/Layout/Diagrams/PfdCanvas.razor.cs` (si existe) — verificar
- `Client/Services/EquipmentManagers/ConnectionOrchestrator.cs` — métodos `_Old` comentados
- `Client/Program.cs` — registro de `IMainSolver` conflictivo + comentarios legacy

---

## Contexto

El cliente está en transición del sistema legacy (`WorkspaceManager`) al nuevo sistema
(`FlowsheetManager` + `EquipmentPortConnector`). La migración está funcionando, pero el
código viejo no se limpió. Esto genera ~2000 líneas de ruido en el proyecto activo,
y hay un conflicto potencial entre el `IMainSolver` registrado en DI y el solver que
`Project` crea internamente.

---

## Problemas a resolver

### 1. [ALTA] ~2000 líneas de código comentado en producción

Tres clases 100% comentadas:
- `WorkspaceManager.cs` — ~500 líneas
- `SimulationOrchestrator.cs` — ~200 líneas
- `ConnectionService.cs` — ~200 líneas

Y código adicional comentado en:
- `PfdCanvas.razor` — 300+ líneas del canvas antiguo
- `ConnectionOrchestrator.cs` — métodos `_Old`, `_Old2` (~100 líneas)
- `Client/Program.cs` — registros legacy comentados

### 2. [ALTA] `IMainSolver` en DI conflictúa con el solver de `Project`

```csharp
// Client/Program.cs:
builder.Services.AddScoped<IMainSolver, MainSolver>();
```

`Project` ya crea su propio `MainSolver` internamente:
```csharp
private static ISimulationService CreateDefaultSimulationService()
{
    var solver = new MainSolver();
    return new SimulationService(solver);
}
```

Resultado: hay dos instancias de `MainSolver` en tiempo de ejecución. El registrado en DI
no conoce los streams/equipos del proyecto activo. El del `Project` sí. Si algún componente
inyecta `IMainSolver` del DI directamente y llama `RunSimulation()`, opera sobre un solver
vacío sin conexión al proyecto.

### 3. [ALTA] `ProjectSessionService` tiene demasiadas responsabilidades

El servicio maneja actualmente:
- Sesión de usuario (proyecto activo, flowsheet activo, workspace state)
- Persistencia de proyecto (crear, cargar, actualizar, eliminar)
- Serialización/deserialización de DTOs de persistencia
- Persistencia de diagramas (crear, actualizar, eliminar diagrama)
- Realtime (recibir eventos SignalR, aplicar cambios, versioning)
- Nombres de flowsheets únicos y validación de numeración
- Autosave con debounce y semáforos de concurrencia

Un servicio con 7 responsabilidades es difícil de testear y de mantener.

### 4. [MEDIA] `EquipmentBaseDialog` accede a `FSM.Elements` directamente

```csharp
var current = FSM.Elements.FirstOrDefault(element => element.Id == Equipment.Id);
```

El diálogo usa la lista global de elementos para sincronizar su facade. El `FlowsheetManager`
debería exponer un método `GetElementById(Guid)` en lugar de exponer la lista completa.

---

## Cambios requeridos

### Paso 1 — Eliminar archivos legacy 100% comentados

Antes de eliminar, confirmar con `rg` que no hay ninguna referencia activa:

```powershell
rg "WorkspaceManager\b" --include="*.cs" --include="*.razor"
rg "SimulationOrchestrator\b" --include="*.cs" --include="*.razor"
rg "ConnectionService\b" --include="*.cs" --include="*.razor"
```

Si no hay referencias activas, eliminar:
- `Client/Services/EquipmentManagers/WorkspaceManager.cs`
- `Client/Services/EquipmentManagers/SimulationOrchestrator.cs`
- `Client/Services/EquipmentManagers/ConnectionService.cs`

### Paso 2 — Limpiar `PfdCanvas.razor`

Eliminar el bloque de código comentado de 300+ líneas. Dejar solo el código activo.

### Paso 3 — Limpiar `ConnectionOrchestrator.cs`

Eliminar los métodos con sufijo `_Old` y `_Old2`. Son:
- `GetConnectedStream_Old`
- `GetAvailableStreams_Old`
- `ProcessConnectionRequest_Old`
- `CreatePipeAndConnect_Old`
- `CreatePipeAndConnect2_Old`
- `DisconnectCurrent_Old`

### Paso 4 — Limpiar `Client/Program.cs`

Eliminar comentarios legacy:
```csharp
// builder.Services.AddSingleton<INamingService, ...>(); // --- LEGACY
// builder.Services.AddScoped<WorkspaceManager>(); // --- LEGACY
```

### Paso 5 — Resolver el conflicto de `IMainSolver` en DI

Opciones:

**Opción A — Eliminar el registro de `IMainSolver` en DI (recomendada)**
El solver del proyecto activo se obtiene a través de `ProjectSessionService.CurrentProject`.
Ningún componente debería inyectar `IMainSolver` directamente; deben pasar por el servicio
que tiene el contexto correcto.

Verificar con `rg "IMainSolver" --include="*.razor" --include="*.razor.cs"` si algún
componente lo inyecta. Si sí, canalizarlos a través de `ProjectSessionService` o
`FlowsheetManager`.

**Opción B — Mantener el registro pero aclararlo**
Si hay componentes que legítimamente necesitan un solver desacoplado del proyecto activo
(por ejemplo, para cálculos standalone), documentarlo explícitamente y nombrarlo diferente.

### Paso 6 — Dividir `ProjectSessionService`

Separar en tres servicios con responsabilidades claras:

**`ProjectSessionService`** — solo estado de sesión UI:
- `CurrentUser`, `CurrentProject`, `ActiveFlowsheet`
- `SetCurrentProjectAsync`, `SetActiveFlowsheetAsync`
- `IsProjectExplorerCollapsed`, etc.
- Eventos: `ProjectChanged`, `ProjectReloaded`

**`ProjectPersistenceService`** — persistencia HTTP:
- `LoadUserProjectsAsync`, `LoadProjectAsync`
- `PersistProjectCreatedAsync`, `PersistDiagramCreatedAsync`, etc.
- `FromPersistenceDtoAsync`, `ToPersistenceDto`

**`ProjectRealtimeCoordinator`** — manejo de eventos SignalR:
- `HandleRealtimeProjectChangedAsync`
- Versioning (`_lastAppliedRealtimeVersion`)
- Debounce de autosave visual

Los tres se registran en DI y se inyectan entre sí donde sea necesario.

### Paso 7 — Agregar `GetElementById` a `FlowsheetManager`

```csharp
// En FlowsheetManager:
public IVisualElement? GetElementById(Guid elementId)
    => Elements.FirstOrDefault(e => e.Id == elementId);
```

Reemplazar en `EquipmentBaseDialog`:
```csharp
// Antes:
var current = FSM.Elements.FirstOrDefault(element => element.Id == Equipment.Id);

// Después:
var current = FSM.GetElementById(Equipment.Id);
```

---

## Orden de ejecución recomendado

1. Paso 1-4 (limpiar código comentado) — bajo riesgo, hacerlos primero
2. Paso 5 (conflicto DI) — verificar antes de eliminar
3. Paso 7 (GetElementById) — cambio puntual, bajo riesgo
4. Paso 6 (dividir ProjectSessionService) — el más costoso, hacerlo al final

---

## Verificación

1. `dotnet build` sin errores.
2. `rg "WorkspaceManager\b\|SimulationOrchestrator\b\|ConnectionService\b"` — sin referencias
   activas después de eliminar.
3. Prueba funcional completa: login → cargar proyecto → editar diagrama → guardar → recargar.
4. Prueba de realtime: abrir el mismo proyecto desde dos usuarios, hacer un cambio,
   confirmar que se propaga.
5. El solver del proyecto activo sigue resolviendo correctamente al crear conexiones.

---

## Riesgo
**Alto** para el paso 6 (dividir `ProjectSessionService`) — es el cambio de mayor impacto.
Hacer último, con prueba completa del flujo de usuario antes y después.
**Bajo** para los pasos 1-4 y 7 — son limpieza y mejoras puntuales.

---

## Dependencias previas
Spec 07 (Server) recomendada antes. Los pasos 1-4 son independientes y pueden hacerse
en cualquier momento.
