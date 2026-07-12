# Codex Context - Distillator

Este archivo consolida la memoria de trabajo encontrada para el proyecto Distillator.

Ruta principal del proyecto: C:\Programas\Distillator\Distillator

---

## Contexto Reciente

# Contexto Actual: Refactorización Fase D Completada

## Estado actual

- `Distillator.Domain` compila y tiene **93 tests pasando**.
- `Client` compila con **0 errores** y advertencias conocidas.
- Todas las fases principales completadas:
  - **Fase A y B**: `FlowsheetManager` usa `ICameraService`, `IPlacementRules` y servicios UI. Conexiones migradas a `IConnectionService` del dominio.
  - **Fase C**: `WorkspaceManager` y sus sub-servicios comentados. `PfdCanvas`, `OldHome` comentados/reducidos.
  - **Fase D**: Diálogos de equipos migrados a `FlowsheetManager` + servicios de dominio.
- **04/07/2026**: Migración WM → FSM completada. `EquipmentFactory` (Shared) ya no depende de `INamingService` viejo; el naming se hace exclusivamente con `IEquipmentNamingService` del dominio.
- **06/07/2026**: se inicio la prueba real del solver nuevo. `MainSolverInDevelopment` paso a ser `MainSolver`; el solver anterior quedo como `MainSolverLegacy`.
- **06/07/2026**: durante pruebas reales de especificaciones se definio la filosofia de "intencion" para specs. El solver ahora arma tres intentos ordenados: especificacion suelta, especificacion + equipo dueño, y especificacion + equipos conectados. El segundo nivel cubre casos como splitter con inlet definido y dos outlets acoplados por una spec (`MassBalance - SP-101` + `Spec`), sin depender del cluster amplio.

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


---

## Resumen Inicial Encontrado En Escritorio

# RESUMEN DE ANALISIS - PROYECTO DISTILLATOR
## Fecha: 28/06/2026
## Ruta del proyecto: C:\Programas\Distillator\Distillator

---

## 1. IDENTIDAD DEL PROYECTO

**Nombre:** Distillator System PRO
**Tipo:** Simulador de Procesos Quimicos / Ingenieria Quimica
**Descripcion:** Es un simulador de procesos quimicos tipo Aspen HYSYS o ChemCAD, construido como aplicacion web interactiva. Permite disenar Diagramas de Flujo de Procesos (PFD), definir corrientes de materiales, seleccionar metodos termodinamicos (NRTL, Wilson, SRK, etc.), y resolver balances de masa y energia con solvers numericos propios.

---

## 2. STACK TECNOLOGICO

| Capa | Tecnologia |
|------|------------|
| Frontend | Blazor WebAssembly (.NET 10) |
| UI Framework | MudBlazor 9.5 + MudBlazor.ThemeManager 4.0 |
| Backend | ASP.NET Core Minimal APIs (.NET 10) |
| Base de Datos | PostgreSQL |
| ORM | Entity Framework Core 10.0.8 |
| Autenticacion | ASP.NET Core Identity + JWT Bearer |
| Exportacion | EPPlus 8.6.0 (Excel) |
| Diagramas | SVG nativo + Canvas Interop (JavaScript) |
| Librerias adicionales | BlazorDownloadFile, SvgPathProperties, Toolbelt.Blazor.HttpClientInterceptor |

---

## 3. ESTRUCTURA DE PROYECTOS (SOLUCION)

Archivo de solucion: `Distillator.slnx`
Proyectos incluidos:
1. **Client** (Blazor WASM) - Interfaz grafica, PFD interactivo, dialogos de equipos.
2. **Server** (ASP.NET Core Web) - API, base de datos, seeders, autenticacion.
3. **Shared** (.NET Class Library) - Logica de negocio, termodinamica, solvers, modelos de dominio.
4. **UnitSystem** (.NET Class Library) - Sistema de unidades propio con conversiones tipadas.

**NOTA:** Existe un proyecto `Numerics` con `Vector.cs` y `NumericsTest.cs` que NO esta incluido en la solucion (huerfano).

---

## 4. ARQUITECTURA FUNCIONAL

### 4.1 Cliente (Frontend)
- **Layout:** MainLayout.razor (tema industrial personalizado), NavMenu.razor
- **Diagramas:** PfdCanvas.razor, paleta de equipos (EquipmentPalette), renderizado SVG de equipos (bombas, intercambiadores, columnas, valvulas, etc.)
- **Paginas:**
  - Home.razor (area de trabajo con pestanas drag & drop)
  - Login.razor, Register.razor, Users.razor (gestion de usuarios)
  - Components.razor (banco de componentes quimicos)
  - ThermodynamicMethodList.razor (metodos termodinamicos)
  - UnitOperations/Columns/ColumnDialog.razor (dialogos de equipos)
  - UnitOperations/HeatExchangers, Pumps, Mixers, Splitters, Valves, Vessels
- **Servicios:**
  - WorkspaceManager.cs (monolitico ~90KB, maneja canvas, equipos, conexiones)
  - EquipmentRegistry.cs, ConnectionOrchestrator.cs
  - DragStateService.cs
  - CustomAuthenticationStateProvider.cs
  - HttpServices (comunicacion con API)

### 4.2 Servidor (Backend)
- **Base de Datos:** AppDbContext (PostgreSQL), migraciones iniciales existentes.
- **Entidades:**
  - Thermodynamics/Components: ChemicalComponent, CorrelationCoefficients
  - Thermodynamics/Methods: ThermodynamicMethod, BinaryInteractionParameter, MethodComponent
  - UserManagement: ApplicationUser (con roles: Administrator, Developer, Creator, Viewer)
- **Endpoints (Minimal APIs):**
  - LoginEndpoint, LogoutEndpoint, RegisterEndpoint
  - CreateUserEndpoint, GetUsersEndpoint, ToggleUserStatusEndpoint
  - ChangeInitialPasswordEndpoint, ValidateDeveloperPasswordEndpoint
  - ThermodynamicMethodEndPoint, ComponentEndPoint
- **Servicios:** DatabaseSeeder, ErrorHandlerMiddleware, SoftDeleteInterceptor, AppConfiguration

### 4.3 Shared (Nucleo de Calculo)
- **Termodinamica:**
  - Phases: MaterialStream, LiquidPhaseMixture, VaporPhaseMixture
  - PureComponents: Evaluadores DIPPR, Antoine Extendido, Propiedades de Agua (gas y liquido)
  - Strategies: Flash PT, PH, PVF, TVF, equilibrio liquido-vapor (VLE)
  - Ecuaciones de Estado: SRK (Soave-Redlich-Kwong)
  - Modelos de actividad: NRTL, Wilson, Ideal
- **Solvers Numericos:**
  - SolverNewtonSolver.cs (Newton-Raphson con jacobianas numericas, damping, manejo de singularidades)
  - BisectionSolver, SecantSolver, CubicSolver
  - LinearSystemSolver (resolucion de sistemas lineales para NR)
- **Diagrama de Flujo (PFD):**
  - IVisualElement, DiagramArea
  - Equipos visuales: ColumnVisualElement, HeatExchangerVisualElement, PumpVisualElement, VesselVisualElement, etc.
  - Tuberias: PipeVisualElement, PipeRoutingFactory, GeometryHelper, CanvasPoint
- **Operaciones de Unidad (Simulacion):**
  - SolverColumn (columnas de destilacion plate-a-plate)
  - SolverHeatExchanger, SolverPump, SolverSplitter, SolverStreamMixer, SolverValve, SolverVessel
  - McCabeThieleBuilder, VLECurveCalculator, FUGCalculationService
  - PlateByPlateCalculator (calculo riguroso de columnas)
- **Sistema de Unidades:**
  - StoredAmount, Amount, ConversionUnit, UnitManager
  - Unidades SI tipadas: Temperature, Pressure, MassFlow, MolarFlow, EnergyFlow, Viscosity, etc.

---

## 5. FUNCIONALIDADES CLAVE DETECTADAS

1. **PFD Interactivo:** Canvas con pan, zoom, arrastre de equipos desde paleta, conexion de tuberias con rutas automaticas.
2. **Banco de Componentes:** Base de datos de componentes quimicos con propiedades criticas y coeficientes de correlacion (DIPPR).
3. **Metodos Termodinamicos:** Configuracion de metodos (Ideal, NRTL, Wilson, SRK) con parametros de interaccion binaria (BIP).
4. **Corrientes de Material:** Definicion de composicion, estado termodinamico (T, P, flujo), calculo de propiedades del bulk y flash.
5. **Equipos de Proceso:** Dialogos especializados para bombas, intercambiadores, columnas de destilacion (con perfiles de composicion, T, P, entalpia), flash tanks, valvulas de control, mezcladores, separadores.
6. **Solvers:**
   - Consecutivo (secuencial equipo por equipo)
   - Newton-Raphson global (solver completo)
   - Ecuaciones de estado y modelos de actividad integrados
7. **Graficos:** Diagramas de McCabe-Thiele, envolventes de fase, perfiles de columna. Usan SVG nativo.
8. **Autenticacion y Autorizacion:** Roles diferenciados. Solo Admin/Developer ven el menu lateral. Creator/Viewer ven la paleta de equipos.
9. **Exportacion:** Reportes a Excel (EPPlus).
10. **Sistema de Unidades:** Manejo robusto de unidades con conversiones implicitas.

---

## 6. EVALUACION PROFESIONAL (Por el Asistente AI)

### 6.1 Puntuacion General
| Criterio | Nota | Comentario |
|----------|------|------------|
| Tecnologia / Stack | A+ | .NET 10, MudBlazor, PostgreSQL. Muy moderno y bien elegido. |
| Complejidad Tecnica | A+ | Termodinamica real, solvers numericos, PFD interactivo. Es un producto serio. |
| Arquitectura Base | B+ | Separacion Client/Server/Shared clara. Minimal APIs bien usadas. |
| Calidad de Codigo | B | Funciona, pero hay clases monoliticas y codigo de prueba mezclado. |
| Mantenibilidad | C+ | WorkspaceManager es un monstruo. Hay carpetas vacias y codigo muerto. |
| Consistencia de Idioma | C | Mezcla espantosa de espanol e ingles en nombres de carpetas y clases. |

### 6.2 Puntos Fuertes
- Implementacion de termodinamica de fluidos completa y funcional.
- Solver Newton-Raphson propio con manejo de singularidades y damping.
- Interfaz visual industrial atractiva y profesional (tema MudBlazor personalizado).
- Arquitectura de roles y autenticacion bien pensada.
- Sistema de unidades propio robusto (`StoredAmount`, `UnitManager`).
- Base de datos seedeada con componentes y metodos maestros via CSV.

### 6.3 Debilidades y Deuda Tecnica
1. **Mezcla de idiomas (CRITICO):** Carpetas como `Liquido`, `Componentes`, `SolverConsecutive` junto a `HeatExchangers`, `PureComponents`. El codigo debe estandarizarse al ingles.
2. **WorkspaceManager.cs monolitico (CRITICO):** Pesa ~90KB. Mezcla logica de canvas, camara, registro de equipos, conexiones de tuberias y notificaciones UI. Debe dividirse en servicios especializados.
3. **Carpetas vacias y codigo muerto:** `FlowsheetSolvers`, `Final Solver`, `MatrixSolvers`, `SolverQwen\Equipments` estan vacias. `SolverQwen` parece nombre temporal de prueba.
4. **Numerics.csproj huerfano:** No esta en la solucion. Incluirlo o eliminarlo.
5. **Codigo comentado de pruebas en produccion:** En `Home.razor` y `Server/Program.cs` hay bloques comentados de tests de integracion.
6. **Inconsistencia en modelos:** `ChemicalComponent` usa `double` y `StoredAmount` sin criterio claro para propiedades similares.
7. **Hardcoding en solver:** Parametros de tolerancia e iteraciones del NewtonSolver sin configuracion externa.
8. **Logging:** Uso de `Console.WriteLine` con emojis en DEBUG, pero sin `ILogger` estructurado.

---

## 7. RECOMENDACIONES TOP 3 (Prioridad)

1. **Refactorizar WorkspaceManager:** Dividirlo en servicios pequenos y especializados (CanvasCameraService, ConnectionManager, ProjectStateService, etc.). Es la mayor fuente de deuda tecnica.
2. **Estandarizar idioma del codigo a Ingles:** Renombrar carpetas, namespaces y clases. Los comentarios pueden quedar en espanol si el equipo es hispanohablante, pero el codigo debe ser monolingue.
3. **Limpiar el proyecto Shared:** Eliminar carpetas vacias, resolver la dualidad `SolverConsecutive` vs `SolverQwen` vs `Final Solver`, y definir una sola arquitectura de solver clara.

---

## 8. NOTAS PARA LA PROXIMA SESION

- El usuario estaba "redondeando una idea en su cabeza" antes de solicitar la siguiente tarea.
- Este proyecto es de alta complejidad. Cualquier cambio debe hacerse con cuidado, especialmente en el nucleo de termodinamica y solvers.
- El codigo compila y funciona (hay builds de Debug y Release en `Client/bin`).
- Tecnologia objetivo actual: .NET 10 (preview o reciente). MudBlazor 9.5.
- Base de datos: PostgreSQL. Contexto: `AppDbContext`. Migracion inicial: `20260608210304_InitialPostgres`.

---

## 9. ARCHIVOS CLAVE A RECORDAR

| Archivo | Proposito |
|---------|-----------|
| `Client/Pages/Home.razor` | Pagina principal con pestanas de areas de trabajo |
| `Client/Services/EquipmentManagers/WorkspaceManager.cs` | El monolito principal (~90KB) |
| `Client/Layout/MainLayout.razor` | Layout raiz con tema industrial y autorizacion |
| `Server/Program.cs` | Punto de entrada backend, seeding de DB |
| `Server/Entities/Thermodynamics/Components/ChemicalComponent.cs` | Modelo de componente quimico |
| `Shared/Thermodynamics/Phases/MaterialStream.cs` | Corriente de material (1167 lineas) |
| `Shared/SolverConsecutive/SolverNewtonSolver.cs` | Solver Newton-Raphson |
| `Shared/SolverConsecutive/Equipments/Columns/SolverColumn.cs` | Simulacion de columnas |
| `UnitSystem/UnitManager.cs` | Registro y conversion de unidades |
| `Distillator.slnx` | Solucion actual (NO incluye Numerics) |

---

## 10. PALETA DE EQUIPOS DETECTADA EN EL PFD

Bombas (Pump), Intercambiadores de Calor (HeatExchanger, PlateExchanger, Reboiler, ShellTube), Columnas de Destilacion (Column), Tanques Flash (FlashTank), Vessel generico, Valvulas de Control (ControlValve), Mezcladores (StreamMixer), Separadores (Splitter), Conectores Off-Page, Instrumentos.

Todos tienen su correspondiente:
- `*VisualElement.cs` (en Shared)
- `*UI.razor` (renderizado SVG en Client)
- `*Dialog.razor` (formulario de propiedades en Client)
- `Solver*.cs` (modelo de simulacion en Shared)

---

Fin del resumen.

---

## Decision De Trabajo - MainSolver Y Specifications (06/07/2026)

### Contexto

Se revisaron las clases actuales:

- `Shared/SolverConsecutive/IMainSolver.cs`
- `Shared/SolverConsecutive/Equipments/Specification.cs`

El usuario identifico correctamente que el codigo actual fue evolucionando por ajustes sucesivos hasta quedar funcional, pero con forma de prototipo: versiones `V2`, `V3`, metodos antiguos comentados como backup, varias responsabilidades en `MainSolver` y demasiadas piezas asociadas a `Specification` para existir hoy una sola especificacion real (`MultiplierSpecification`).

### Lectura actual

`MainSolver` hoy cumple demasiadas responsabilidades:

- registra `Streams` y `Equipments`;
- ejecuta `RunSimulation`;
- limpia variables calculadas por solver;
- agrupa ecuaciones por tipo;
- construye clusters especiales para specifications con `BuildSpecificationClustersV3`;
- ejecuta `NewtonSolver`;
- ejecuta `PostSolveAsync`;
- notifica a UI con `OnSimulationCompleted`.

`Specification.cs` hoy mezcla demasiados conceptos en un solo archivo:

- contrato `ISpecification`;
- enum `SpecificationType`;
- base `StreamSpecificationBase`;
- `MultiplierSpecification`;
- adaptador `SpecificationEquation`;
- composiciones/clusters de ecuaciones (`CompositeEquation`, `CompositeEquationEquipmentList`).

### Decision

No se va a borrar ni modificar de entrada el solver actual. La estrategia sera crear implementaciones nuevas con apellido `InDevelopment`, manteniendo lo actual como referencia funcional hasta tener pruebas suficientes.

Plan acordado:

1. Crear `MainSolverInDevelopment` con codigo mas claro y mejor separado, preservando funcionalidad.
2. Crear una nueva estructura de specifications `InDevelopment`, mas simple y extensible.
3. Mantener el `MainSolver` y `Specification` actuales como referencia mientras se desarrolla.
4. Cuando el nuevo flujo funcione y este probado, renombrar lo viejo como `Legacy`.
5. Comparar resultados nuevo vs legacy.
6. Solo despues retirar codigo viejo.

### Objetivo De Diseno

El objetivo principal del solver sigue siendo resolver balances de masa y energia de equipos, respetando specifications definidas por el usuario.

Principios que deben guiar el rediseño:

- SOLID: separar orquestacion, construccion de ecuaciones, construction de clusters y ejecucion numerica.
- KISS: empezar con una version limpia que resuelva todo como hoy antes de optimizar.
- YAGNI: no introducir un motor complejo antes de tener pruebas y casos reales.
- DRY: evitar duplicar logica entre `V2`, `V3`, `Old` y variantes similares.

### Specifications

Se quiere que exista polimorfismo real en specifications. Hoy solo existe `MultiplierSpecification`, por ejemplo:

```text
Destination = Source * Multiplier
```

Se propone agregar una segunda especificacion, tipo formula, para representar relaciones evaluables definidas por usuario o por UI, por ejemplo:

```text
Reflux.MassFlow = 2.5 * Distillate.MassFlow
```

o en forma residual:

```text
Reflux.MassFlow - 2.5 * Distillate.MassFlow = 0
```

Esto permitiria validar el diseno de specifications con polimorfismo real, sin sobrearquitecturar.

### Fase Posterior: Solver Incremental Por Dependencias

El usuario planteo una mejora futura importante: el solver actual es reactivo, pero ante un cambio de UI repasa muchas ecuaciones/equipos. Para flowsheets grandes, seria mejor que el solver conozca dependencias y resuelva solo lo afectado.

Idea futura:

```text
Variable cambiada
-> detectar ecuaciones dependientes
-> propagar impacto por equipos/corrientes afectados
-> construir SolvePlan
-> resolver solo subgrafo afectado
```

Conceptos posibles para una fase posterior:

- `DependencyGraphBuilder`
- `SolverDependencyGraph`
- `DirtyPropagationService`
- `IncrementalSolvePlanner`
- `SolvePlan`
- `SolveScope` (`Equipment`, `LocalCluster`, `Downstream`, `ConnectedSubgraph`, `FullFlowsheet`)

Decision actual: no empezar por el solver incremental. Primero se hara un `MainSolverInDevelopment` limpio que conserve la funcionalidad actual. Despues, sobre esa base mas clara, se agregara el grafo de dependencias.

### Avance Implementado (06/07/2026)

Se creo una primera version paralela:

- `Shared/SolverConsecutive/MainSolver.cs`

Esta clase implementa `IMainSolver` y mantiene la estrategia actual de resolver todo el flowsheet, pero organiza el flujo en etapas mas claras:

1. limpiar variables calculadas por solver;
2. construir un `SolvePlan` completo;
3. crear ecuaciones regulares por tipo;
4. agregar clusters de specifications usando la misma logica funcional de `BuildSpecificationClustersV3`;
5. resolver ecuaciones/clusters con `NewtonSolver`;
6. ejecutar `PostSolveAsync`;
7. notificar `OnSimulationCompleted`.

En la primera etapa no se reemplazo el `MainSolver` actual ni se modifico `Specification.cs`.

Verificacion ejecutada:

```text
dotnet build C:\Programas\Distillator\Distillator\Shared\Shared.csproj
```

Resultado:

```text
Compilacion correcta.
0 Advertencia(s)
0 Errores
```

### Inicio De Prueba Real - MainSolver Nuevo Como Principal (06/07/2026)

Se inicio formalmente la travesia de cambiar el solver principal para probarlo con casos reales.

Cambio aplicado:

- `MainSolver` actual fue renombrado a `MainSolverLegacy`.
- `MainSolverInDevelopment` fue renombrado a `MainSolver`.
- `IMainSolver` sigue siendo el contrato publico usado por la UI y el dominio.
- El registro actual de DI en `Client/Program.cs` sigue resolviendo `IMainSolver` como `MainSolver`, por lo tanto ahora apunta al solver nuevo.
- `Project.CreateDefaultSimulationService()` sigue creando `new MainSolver()`, por lo tanto tambien apunta al solver nuevo.
- El archivo fisico del solver nuevo quedo como `Shared/SolverConsecutive/MainSolver.cs`.

Objetivo inmediato:

1. Probar un caso real simple desde la UI.
2. Comparar resultados esperados contra el comportamiento conocido del solver legacy.
3. Mantener `MainSolverLegacy` disponible como referencia temporal hasta tener suficiente confianza.

Decision importante:

- No se elimino el solver anterior.
- No se cambio aun la estructura de `Specification.cs`.
- La prioridad es verificar comportamiento real antes de seguir extrayendo servicios o redisenando specifications.

Pendiente:

- comparar comportamiento contra `MainSolverLegacy` con un caso simple;
- probar el nuevo `MainSolver` conectado a la UI con un caso real;
- despues redisenar `SpecificationInDevelopment` y agregar una specification por formula.

### Avance Implementado - EquationClusterInDevelopment (06/07/2026)

Se creo una clase nueva para reemplazar en el solver nuevo las dos estrategias previas de ecuaciones compuestas:

- `CompositeEquation`
- `CompositeEquationEquipmentList`

Nueva clase:

- `Shared/SolverConsecutive/EquationClusterInDevelopment.cs`

El nuevo `MainSolver` ahora usa `EquationClusterInDevelopment` tanto para clusters fisicos como para clusters de specifications. La clase permite declarar explicitamente:

- `EquationType`
- `EquationTypeModifer`
- lista interna de ecuaciones
- union de residuos
- union de variables sin duplicados

Esto evita que un cluster fisico tenga que fingir ser una specification, y evita mantener dos clases con comportamiento casi duplicado.

Las clases antiguas se mantienen intactas porque todavia son referencia del solver actual.

Verificacion ejecutada:

```text
dotnet build C:\Programas\Distillator\Distillator\Shared\Shared.csproj
```

Resultado:

```text
Compilacion correcta.
0 Advertencia(s)
0 Errores
```

Verificacion adicional despues de promover el solver nuevo:

```text
dotnet build C:\Programas\Distillator\Distillator\Shared\Shared.csproj
dotnet build C:\Programas\Distillator\Distillator\Client\Client.csproj
dotnet test C:\Programas\Distillator\Distillator\Distillator.Domain.Tests\Distillator.Domain.Tests.csproj
```

Resultado:

```text
Shared: compilacion correcta, 0 advertencias, 0 errores.
Client: compilacion correcta, 0 advertencias, 0 errores.
Distillator.Domain.Tests: 93/93 tests pasando.
```

### Ajuste De Intencion Para Specifications (06/07/2026)

Durante la primera prueba real con columna + condenser + splitter, se confirmo que la specification del splitter si entraba al `SolvePlan`, pero solo como cluster acoplado. El cluster arrastraba ecuaciones de `SP-101`, `E-101` y `C-101`, quedando con mas variables ajustables que residuos y Newton lo rechazaba por no ser cuadrado.

Decision de diseno:

- Mantener tres formas de ecuacion para specifications:
  1. `SpecificationEquation` suelta, para capturar la intencion algebraica directa del usuario.
  2. `EquationClusterInDevelopment` con la specification + el equipo donde reside la specification. Ejemplo: `MassBalance - SP-101` + `Spec`.
  3. `EquationClusterInDevelopment` con la specification + equipos conectados inmediatos a las corrientes involucradas.
- La filosofia del solver es intentar ecuaciones en orden, no exigir que todo el sistema global este listo desde el inicio.

Cambio aplicado:

- `MainSolver.BuildFullSolvePlan()` ahora agrega primero las `SpecificationEquation` sueltas, despues los clusters specification + equipo dueño, y finalmente los clusters de specifications con equipos conectados.
- Se evaluo cambiar `NewtonSolver.Solve()` para revisar residual antes de validar si el sistema era cuadrado, pero se descarto porque podia retirar clusters pendientes antes de que otras ecuaciones movieran variables compartidas. `NewtonSolver.Solve()` quedo con su orden original.

Verificacion ejecutada:

```text
dotnet build C:\Programas\Distillator\Distillator\Shared\Shared.csproj
dotnet build C:\Programas\Distillator\Distillator\Client\Client.csproj
dotnet test C:\Programas\Distillator\Distillator\Distillator.Domain.Tests\Distillator.Domain.Tests.csproj
```

Resultado:

```text
Shared: compilacion correcta, 0 advertencias, 0 errores.
Client: compilacion correcta, 1 advertencia conocida de NullSolver, 0 errores.
Distillator.Domain.Tests: 93/93 tests pasando.
```

Correccion posterior:

- Se revirtio el cambio temporal en `NewtonSolver.Solve()`.
- La modificacion funcional vigente de esta fase es que `MainSolver` incluye las `SpecificationEquation` sueltas, el cluster local con el equipo dueño, y el cluster amplio con equipos conectados inmediatos.

Pruebas manuales reales superadas con columna + condenser + splitter:

- Modo 1, specification suelta: definiendo `S-103 = 1000 kg/hr`, la spec `S-104 = 5 * S-103` calcula `S-104 = 5000 kg/hr`.
- Modo 2, specification + equipo dueño: definiendo `S-102 = 6000 kg/hr` en el splitter, calcula `S-103 = 1000 kg/hr` y `S-104 = 5000 kg/hr`.
- Modo 3, cluster amplio: definiendo variables de la columna como `Feed_1`, `ReboilerReturn`, `Bottoms` u `Overhead/Vapor`, el sistema converge y mantiene el splitter en `S-102 = 6000`, `S-103 = 1000`, `S-104 = 5000`.
- La prueba reina, definiendo vapor/overhead `S-101 = 6000 kg/hr`, converge y mantiene el balance del splitter y la spec.

Hoja de ruta siguiente:

1. Completar o corregir las ecuaciones necesarias para que `SolverStreamMixer` y el tanque/vessel se resuelvan con el nuevo `MainSolver`.
2. Crear una nueva specification por formula para que el usuario escriba ecuaciones visuales/naturales, por ejemplo `S-104.MassFlow = 5 * S-103.MassFlow`.
3. Integrar la specification por formula con la misma filosofia de intencion: spec suelta, spec + equipo dueño, y spec + equipos conectados inmediatos.
4. Agregar pruebas de regresion enfocadas para splitter/specs y luego para mixer/tanque.

### Roadmap Maestro Del Producto (06/07/2026)

Despues de estabilizar el solver nuevo, `StreamMixer`, tanque/vessel y `FormulaSpecification`, la ruta de trabajo acordada es:

1. Mejorar las configuraciones de proyecto y configuraciones por diagrama.
2. Mejorar el servicio de nombrado.
3. Diseñar como se va a persistir en base de datos el modelo completo creado: proyecto, diagramas, equipos, corrientes, conexiones, specifications, configuraciones y resultados relevantes.
4. Implementar y probar guardado/carga desde base de datos.
5. Probar una planta completa de destilacion con muchas columnas y muchos diagramas.
6. Validar calculos de propiedades de transporte.
7. Diseñar equipos.
8. Crear reportes.

Secuencia de criterio: primero asegurar solver y experiencia de modelado, despues configuracion/naming, luego persistencia, despues pruebas grandes de planta, validacion fisica, diseño de equipos y finalmente reporteria.

### Pendientes Naming Service Y Project Configuration UI (11/07/2026)

Estado actual:

- El tab `Naming` de `ProjectFormDialog` ya usa un builder visual tipo lego.
- La configuracion por defecto debe ser la mas basica: consecutivo por proyecto.
- El usuario no debe ver nombres internos de enums; debe ver comportamiento y ejemplos.

Decisiones de diseno:

1. Quitar la regla `Diagram Number Range`; por ahora son suficientes:
   - `Project`
   - `Equipment Type`
   - `Diagram`
   - `Diagram + Equipment Type`
2. La regla de equipo principal/paquete se implementara despues como una regla aparte.
3. Si `Diagram Prefix` esta activo, tacitamente la numeracion debe estar asociada a diagrama:
   - `Diagram`
   - `Diagram + Equipment Type`
4. `Diagram Prefix` se define al crear/configurar cada diagrama.
5. `Diagram Prefix` no debe mostrarse grande en el canvas; se usara en reportes/listados, por ejemplo listado de bombas o equipos por area.
6. Si un diagrama tiene prefijo/base `200` y la secuencia es por diagrama, el primer equipo debe iniciar como `201`.

Pendientes UI:

1. Mejorar visualmente el panel de prefijos de equipos; actualmente nombre e input se ven muy separados y con poca relacion visual.
2. Hacer que las explicaciones hablen solo de la configuracion activa o de la pieza seleccionada.
3. Los ejemplos deben mostrarse como arbol por diagramas:
   - `Diagram 100`
   - `Diagram 200`
   - equipos y corrientes debajo.
4. Cuando `Diagram Prefix` esta activo, los ejemplos deben mostrar nombres tipo `100-P-101` o `200-P-201`, segun la regla de secuencia.
5. Mejorar la proporcion general del dialogo; actualmente se ve ancho y desbalanceado.

Refactor pendiente:

1. Separar `ProjectFormDialog` por componentes:
   - General tab
   - Units tab
   - Camera tab
   - Reports tab
   - Equipment Design tab
   - Naming tab
2. Cada componente debe tener su propio `.razor.css` scoped.
3. `ProjectFormDialog` tambien debe tener su propio CSS scoped.
4. Evitar estilos grandes embebidos dentro del `.razor`.

Pruebas pendientes:

1. Crear diagramas con distintas reglas de naming.
2. Crear equipos y corrientes en varios diagramas.
3. Verificar consecutivo por proyecto.
4. Verificar consecutivo por tipo de equipo.
5. Verificar consecutivo por diagrama.
6. Verificar consecutivo por diagrama + tipo de equipo.
7. Verificar que `Diagram Prefix` aparezca en reportes/listados, pero no como rotulo grande en canvas.

### Diseño Persistencia Y Colaboración En Tiempo Real (11/07/2026)

Se creó la hoja de diseño:

```text
docs/PERSISTENCE_REALTIME_DESIGN.md
```

Decisiones funcionales acordadas:

- Persistencia por fases:
  1. Configuración básica del proyecto sin sistemas de unidades detallados, equipos ni corrientes.
  2. Sistemas de unidades del proyecto.
  3. Diagramas, equipos, corrientes, conexiones y specifications.
- Guardado automático sin botón de guardar.
- Colaboración por proyecto completo.
- Permisos por usuario: solo edita quien tenga permiso.
- Conflictos: último cambio gana.
- Mostrar usuarios conectados y presencia por diagrama.
- Guardar auditoría futura: quién cambió qué dato y cuándo.
- Los resultados calculados del solver se recalculan al abrir; se guardan los datos definidos por usuario.

Diseño técnico inicial:

- Blazor WASM Client + ASP.NET Core Server + PostgreSQL.
- HTTP para carga inicial.
- SignalR para cambios en tiempo real.
- Cambios granulares y versionados.
- `ProjectStateService` en cliente como estado local.
- Hub delgado: valida y delega en servicios de aplicación.

Avance implementado:

- Se agregó el esqueleto EF Core para persistencia colaborativa en `Server.Entities.Projects`.
- `ApplicationDbContext` ahora expone:
  - `Projects`
  - `ProjectCollaborators`
  - `ProjectDiagrams`
  - `ProjectChangeLogs`
- Las entidades son tenanted mediante `TenantId`.
- `Projects` guarda configuración del proyecto en columnas `jsonb` para naming, unidades, cámara, reportes y diseño de equipos.
- `ProjectCollaborators` prepara permisos `Owner`, `Editor`, `Viewer`.
- `ProjectChangeLogs` prepara auditoría/versionado para autosave y colaboración.
- Se agregó la migración EF `20260712024215_AddProjectPersistence`.
- La migración fue aplicada a PostgreSQL con `dotnet-ef database update`.
- Se instaló `dotnet-ef` como herramienta local del repo mediante `dotnet-tools.json`.
- Se agregaron contratos mínimos en `Shared/Projects/ProjectPersistenceDtos.cs`.
- Se agregó `ProjectEndPoint` con endpoints HTTP iniciales:
  - `/Projects/GetUserProjects`
  - `/Projects/GetProject`
  - `/Projects/CreateProject`
  - `/Projects/UpdateProjectConfiguration`
- Se agregó un puente inicial de autosave en cliente:
  - `HttpService` resuelve requests de `Shared.Projects` hacia `/Projects/...`.
  - `ProjectSessionService` persiste creación de proyecto y cambios de configuración básica.
  - El proyecto local conserva su `Guid` al crearse en base de datos.
- Aún no se creó SignalR Hub.
- Aún no se reemplazó la carga/listado de proyectos en memoria por carga desde PostgreSQL.
- Aún no se persisten diagramas, equipos, corrientes ni conexiones.

Verificación:

```text
dotnet build .\Distillator.slnx --nologo
dotnet tool run dotnet-ef database update --project Server\Server.csproj --startup-project Server\Server.csproj --context ApplicationDbContext
```

Resultado:

```text
Compilación correcta.
1 advertencia existente no relacionada: FlowsheetManager.NullSolver.OnSimulationCompleted nunca se usa.
0 errores.
Migración aplicada correctamente.
```

