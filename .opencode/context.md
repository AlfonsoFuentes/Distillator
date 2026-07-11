# Contexto Actual: Refactorización Fase D Completada

## Estado actual

- `Distillator.Domain` compila y tiene **93 tests pasando**.
- `Client` compila con **0 errores** y advertencias conocidas.
- Todas las fases principales completadas:
  - **Fase A y B**: `FlowsheetManager` usa `ICameraService`, `IPlacementRules` y servicios UI. Conexiones migradas a `IConnectionService` del dominio.
  - **Fase C**: `WorkspaceManager` y sus sub-servicios comentados. `PfdCanvas`, `OldHome` comentados/reducidos.
  - **Fase D**: Diálogos de equipos migrados a `FlowsheetManager` + servicios de dominio.
- **04/07/2026**: Migración WM → FSM completada. `EquipmentFactory` (Shared) ya no depende de `INamingService` viejo; el naming se hace exclusivamente con `IEquipmentNamingService` del dominio.

## Advertencias actuales conocidas

- `_Imports.razor(77,26)` — `FSM` inyectado globalmente oculta el miembro heredado en componentes base (CS0108 x4).
- `FlowsheetManager.cs(773,30)` — evento `NullSolver.OnSimulationCompleted` no se usa (placeholder, CS0067).
- `WorkspaceManager.cs` — clase entera comentada; advertencia de NULL dereference ya no aplica.

---

## Fase D — Completada

### D.1 Configuración de proyecto (parcial)
| # | Tarea | Estado |
|---|-------|--------|
| 1 | Hacer `ProjectConfiguration` editable | ✅ |
| 2 | Agregar `IUnitConfiguration` al diálogo | ⏳ |
| 3 | Agregar `ICameraConfiguration` al diálogo | ⏳ |
| 4 | Agregar `INamingConfiguration` al diálogo | ⏳ |
| 5 | Agregar `IReportConfiguration` al diálogo | ⏳ |
| 6 | Agregar `IEquipmentDesignConfiguration` al diálogo | ⏳ |
| 7 | Persistir cambios en `Project` y notificar | ⏳ |

### D.2 Configurador de diagrama particular (parcial)
| # | Tarea | Estado |
|---|-------|--------|
| 1-3 | Propiedades visuales expuestas y funcionales | ✅ |
| 4 | Botón "Reset to defaults" | ⏳ |
| 5 | Opción "Auto" para dimensiones | ⏳ |

### D.3 NamingService ✅
`IEquipmentNamingService` del dominio: registrado como Scoped, inyectado en `FlowsheetManager`, `IConnectionService`, `IInterFlowsheetConnectionService`. Opera sobre `Project.EquipmentRegistry`. 93 tests de dominio pasando.

### D.4 Migrar diálogos de equipos ✅ (04/07/2026)
- `EquipmentPortConnector` — reescrito de WM → FSM + `IEquipmentNamingService`.
- `EquipmentBaseStreams` — `GetFacadeForPort` → `FSM.GetFacadeForConnectedId`.
- `EquipmentBaseSpecifications` — Areas/Pipes/RunSimulation → `FSM.Elements`/`FSM.Pipes`/`FSM.RunSimulation`.
- `EquipmentBaseCompositionGrid` — Areas/RunSimulation → FSM.
- `EquipmentBaseStreamFacade` — `WM.Elements` + `NamingService` viejo → FSM.
- `MaterialStreamDialog` / `MaterialStreamMainData` — `WM.OnNotifyUI` → `FSM.OnNotifyUI`.
- `CanvasEquipmentWrapper` — modo conexión → FSM.
- `CanvasPipeWrapper` — draft params → FSM.
- `OffPageConnectorUI` — areas → flowsheets + `SessionService`.
- `OldHome.razor` — placeholder (dependía enteramente de WM).
- `EquipmentFactory` (Shared) — eliminada dependencia de `INamingService`. El naming ahora es responsabilidad exclusiva del nuevo `IEquipmentNamingService` del dominio, invocado desde `FlowsheetManager.SetUniqueElementName()` o desde el dominio `ConnectionService.CreateStream()`.

### Flujo de creación con nombres (nuevo)

```
Toolbox → FSM.AddFromToolbox()
  → factory.Create(type, x, y, snap)      // Shared: crea elemento sin nombre
  → FSM.SetUniqueElementName()            // Client: asigna nombre vía IEquipmentNamingService
  → _project.AddEquipment()               // Registry global
  → _flowsheet.AddElementReference()       // Referencia posicional

Conexión → ConnectionService.Connect()
  → CreateStream()
    → factory.Create(MaterialStream, ...)  // Shared: crea sin nombre
    → _namingService.GenerateNextName()    // Dominio: asigna nombre
    → stream.Name = "S-101"
```

### DI actual (Program.cs)

```csharp
// --- Legacy (comentado) ---
// builder.Services.AddSingleton<INamingService, ...>();
// builder.Services.AddScoped<WorkspaceManager>();

// --- Dominio ---
builder.Services.AddScoped<ICameraService, Distillator.Domain.Services.CameraService>();
builder.Services.AddScoped<IPlacementRules, PlacementRules>();
builder.Services.AddScoped<IEquipmentNamingService, Distillator.Domain.Policies.EquipmentNamingService>();

// --- UI ---
builder.Services.AddScoped<FlowsheetManager>();
builder.Services.AddScoped<FlowsheetCanvasLayoutService>();
builder.Services.AddScoped<FlowsheetStyleService>();
builder.Services.AddScoped<EquipmentDragService>();
```

---

## Pruebas de la última refactorización

### Pruebas Fase D.4 — Migración WM → FSM (04/07/2026)

| ID | Nombre | Tipo | Resultado |
|----|--------|------|-----------|
| TL-11 | Client compila sin WM | Automatizado | ✅ 0 errores |
| TL-12 | Dominio compila | Automatizado | ✅ 0 errores |
| TL-13 | 93 tests de dominio | Automatizado | ✅ 93/93 |
| TL-14 | FSM expone 8 métodos puente | Inspección | ✅ |
| TL-15 | DI sin WM/NamingService viejo | Inspección | ✅ |
| TL-16 | StreamFacade usa FSM | Inspección | ✅ |
| TL-17 | Streams resuelve facades vía FSM | Inspección | ✅ |
| TL-18 | Specifications usa FSM | Inspección | ✅ |
| TL-19 | CompositionGrid usa FSM | Inspección | ✅ |
| TL-20 | MaterialStreamDialog suscrito a FSM | Inspección | ✅ |
| TL-21 | CanvasEquipmentWrapper usa FSM | Inspección | ✅ |
| TL-22 | CanvasPipeWrapper usa FSM | Inspección | ✅ |
| TL-23 | OffPageConnectorUI usa flowsheets | Inspección | ✅ |
| TL-24 | PortConnector conectar/desconectar/autogenerar | Funcional | ✅ |
| TL-25 | PumpDialog todas las pestañas | Funcional | ✅ |
| TL-26 | MaterialStreamDialog refresco | Funcional | ✅ |

### Pruebas Fase C — Limpieza

| ID | Nombre | Resultado |
|----|--------|-----------|
| FC-001 | `WorkspaceManager` comentado | ✅ |
| FC-002 | `PfdCanvas` y `OldHome` comentados | ✅ |
| FC-004 | Diálogos de equipos sin `WM` | ✅ |
| FD-009 | Diálogos migrados funcionan sin WM | ✅ |
| FD-010 | Client compila sin WM | ✅ |

---

## Archivos modificados en la migración WM → FSM

| Archivo | Cambio |
|---------|--------|
| `FlowsheetManager.cs` | +8 métodos puente (GenerateNextName, CreateStreamProgrammatically, ConnectEquipmentToStream, etc.) |
| `EquipmentPortConnector.razor` | WM → FSM completo |
| `EquipmentBaseStreams.razor` | GetFacade → FSM.GetFacadeForConnectedId |
| `EquipmentBaseSpecifications.razor` | Areas/Pipes/RunSimulation → FSM |
| `EquipmentBaseCompositionGrid.razor.cs` | Areas/RunSimulation → FSM |
| `EquipmentBaseStreamFacade.razor` | WM.Elements + NamingService → FSM |
| `MaterialStreamDialog.razor` | WM.OnNotifyUI → FSM.OnNotifyUI |
| `MaterialStreamMainData.razor` | WM.Elements + NamingService → FSM |
| `CanvasEquipmentWrapper.razor` | Modo conexión → FSM |
| `CanvasPipeWrapper.razor` | Draft params → FSM |
| `OffPageConnectorUI.razor` | Areas → flowsheets + SessionService |
| `_Imports.razor` | Comentado @inject WM y @inject NamingService |
| `Program.cs` | Comentado AddScoped<WM> y AddSingleton<INamingService> |
| `OldHome.razor` | Reducido a placeholder |
| `IEquipmentFactory.cs` (Shared) | EquipmentFactory sin dependencia de INamingService |

---

*Última actualización: 04/07/2026*
