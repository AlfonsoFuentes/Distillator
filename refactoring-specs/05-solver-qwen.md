# Spec 05 — SolverQwen/Stream: Sacar Tests de Producción + Sellar IFacadeStream

## Estado
Pendiente

## Archivos afectados
- `Shared/SolverQwen/StreamIntegrationTest.cs` — mover fuera de producción
- `Shared/SolverQwen/StreamMixerBalanceRegressionTest.cs` — mover fuera de producción
- `Shared/SolverQwen/Stream/IFacadeStream.cs` — sellar setter de `Composition`
- `Shared/SolverQwen/Stream/FacadeStream.cs` — corregir `SetThermodynamicMethod`

---

## Contexto

`StreamIntegrationTest` es un script manual de ~700 líneas con `Console.WriteLine` y secuencias
de pasos manuales para una columna de destilación completa. Actualmente vive en
`Shared/SolverQwen/` y se compila dentro del assembly `Shared.dll`, que se carga en WASM.
Esto incrementa el tamaño del bundle sin aportar nada en producción.

`StreamMixerBalanceRegressionTest` es un test de regresión válido con aserciones numéricas,
pero también vive en producción.

---

## Problemas a resolver

### 1. [ALTA] `StreamIntegrationTest.cs` vive en producción — contamina WASM

~700 líneas de script exploratorio con `Console.WriteLine` en el assembly `Shared.dll`
que se descarga en el navegador. No pertenece aquí.

### 2. [ALTA] `StreamMixerBalanceRegressionTest.cs` vive en producción

Un test de regresión con aserciones numéricas (`Assert`) está mezclado con código de
producción. Debe vivir en un proyecto de tests.

### 3. [MEDIA] `IFacadeStream.Composition` tiene setter público en la interfaz

```csharp
// Actual — permite reemplazar el orquestador externamente:
CompositionOrchestrator Composition { get; set; }
```

Si código externo reemplaza `Composition` directamente, las subscripciones a
`OnCompositionChanged` configuradas en `SetThermodynamicMethod` se pierden. El setter
debe ser interno o eliminado de la interfaz.

### 4. [MEDIA] `FacadeStream.SetThermodynamicMethod` crea lista local innecesariamente

```csharp
List<ComponentFacade> _componentList = new();
_componentList.Clear(); // ← lista recién creada, el Clear es redundante
```

### 5. [MEDIA] `FacadeStream.ExecuteEquilibrium()` y `ExecuteFlows()` son públicos

Estos métodos disparan recálculos termodinámicos. Ser públicos permite que código
externo fuerce recálculos sin pasar por las subscripciones de variables, lo que puede
generar estados inconsistentes.

---

## Cambios requeridos

### Paso 1 — Crear proyecto de tests o carpeta de scripts

Evaluar si ya existe un proyecto `*.Tests` en la solución. Si no:
- Opción A: Crear `Shared.Tests/` como proyecto de tests xUnit referenciando `Shared`.
- Opción B: Mover `StreamIntegrationTest` a `docs/` o `scripts/` como archivo de referencia
  (no compilado), si no se quiere el overhead de un proyecto nuevo.

El `StreamMixerBalanceRegressionTest` sí debe ir a un proyecto xUnit para poder ejecutarse
en CI.

### Paso 2 — Mover `StreamIntegrationTest.cs` fuera de `Shared/`

```
Shared/SolverQwen/StreamIntegrationTest.cs  →  eliminar del proyecto Shared
```

Si se crea proyecto de tests: moverlo allí. Si no: eliminarlo o archivarlo en `docs/`.

### Paso 3 — Mover `StreamMixerBalanceRegressionTest.cs` al proyecto de tests

```
Shared/SolverQwen/StreamMixerBalanceRegressionTest.cs  →  Shared.Tests/SolverQwen/
```

Adaptar si usa directivas de assert propias para que funcione con xUnit.

### Paso 4 — Cambiar `Composition` en `IFacadeStream` a solo getter

```csharp
// Antes:
CompositionOrchestrator Composition { get; set; }

// Después:
CompositionOrchestrator Composition { get; }
```

En `FacadeStream`, `Composition` se asigna solo en `SetThermodynamicMethod`. Si hay
código que actualmente usa el setter externo, canalizarlo por `SetThermodynamicMethod`.

### Paso 5 — Corregir `SetThermodynamicMethod` en `FacadeStream`

```csharp
// Antes:
List<ComponentFacade> _componentList = new();
_componentList.Clear(); // redundante

// Después:
var componentList = new List<ComponentFacade>();
// ... foreach usando componentList ...
Composition = new CompositionOrchestrator(componentList);
```

### Paso 6 — Cambiar `ExecuteEquilibrium` y `ExecuteFlows` a `private`

Si no hay código externo que los llame directamente, hacerlos `private`.
Si hay casos legítimos de llamada externa, documentarlos claramente y dejarlos `internal`.

---

## Verificación

1. `dotnet build` sin errores — en particular que `Shared.dll` compila sin las clases de test.
2. `rg "StreamIntegrationTest\|StreamMixerBalanceRegressionTest"` no debe aparecer en
   `Shared/SolverQwen/`.
3. Si se creó el proyecto de tests, ejecutar `dotnet test` — el test de regresión del mixer
   debe pasar.
4. `rg "\.Composition\s*="` fuera de `FacadeStream` — no debe haber asignaciones externas
   del orquestador.

---

## Riesgo
**Bajo-Medio.** Mover los archivos es bajo riesgo. Cambiar el setter de `Composition`
requiere verificar que no haya código externo que lo use directamente.

---

## Dependencias previas
Ninguna. Independiente de las specs anteriores.
