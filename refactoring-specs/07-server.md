# Spec 07 — Server: Dividir ProjectEndPoint + Hub Query + Limpiar RegisterServices

## Estado
Pendiente

## Archivos afectados
- `Server/Entities/Projects/` — `ProjectEndPoint.cs` (endpoint monolítico)
- `Server/Entities/Projects/Hubs/ProjectCollaborationHub.cs` — doble query en `JoinProject`
- `Server/Services/RegisterServices.cs` — métodos vacíos comentados
- `Server/Services/ApplicationBuilderExtensions.cs` — `Debugger.Break()` en catch

---

## Contexto

El servidor tiene buenas bases: Minimal API, SignalR, EF Core con soft-delete y
`SaveResultAsync<T>` que retorna `Result<T>`. El problema principal es que el endpoint
de proyecto creció hasta ser un archivo de ~700 líneas con todo en un solo `MapEndPoint`,
y hay dos problemas puntuales de infraestructura.

---

## Problemas a resolver

### 1. [ALTA] `ProjectEndPoint` es monolítico — viola SRP

Un único archivo maneja: obtener proyectos, crear proyecto, actualizar, eliminar, compartir,
crear diagrama, actualizar diagrama, eliminar diagrama, workspace state, workspace visual state,
y broadcasting SignalR. Todo en un único método `MapEndPoint` de ~700 líneas.

Esto hace que:
- Agregar un endpoint nuevo requiere navegar todo el archivo
- El contexto de error en las llamadas es difícil de rastrear
- Los permisos y la lógica de negocio se mezclan con el routing

### 2. [ALTA] `ProjectCollaborationHub.JoinProject` hace dos queries a DB en secuencia

```csharp
var hasAccess = await _context.ProjectCollaborators...AnyAsync(...); // query 1
// ...
var user = await _context.Users...FirstOrDefaultAsync(...); // query 2
```

En un hub de alta frecuencia (usuarios entrando/saliendo al abrir diagramas), esto
es dos round-trips innecesarios. Puede resolverse con un JOIN o proyección única.

### 3. [MEDIA] `RegisterServices.AddApplicationServices` y `AddRepositories` están vacíos

```csharp
internal static IServiceCollection AddApplicationServices(...) 
{
    // Todo comentado — no registra nada
    return services;
}
public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    // Todo comentado — no registra nada
    return services;
}
```

Son métodos que no hacen nada y no está claro si deben hacer algo o eliminarse.

### 4. [MEDIA] `Debugger.Break()` en catch de DEBUG puede colgar CI

```csharp
#if DEBUG
    System.Diagnostics.Debugger.Break(); // ← bloquea si no hay debugger en CI
    Console.WriteLine($"Error mapping ...");
#endif
```

Si un build de CI corre en modo Debug y el endpoint falla al registrarse, el proceso
se bloquea esperando un debugger que nunca llega.

---

## Cambios requeridos

### Paso 1 — Dividir `ProjectEndPoint` en grupos de endpoints por responsabilidad

Estructura propuesta en `Server/Entities/Projects/`:

```
ProjectEndPoint.cs              ← solo el método MapEndPoint principal que orquesta
ProjectQueryEndpoints.cs        ← GetProjects, GetProject
ProjectCommandEndpoints.cs      ← CreateProject, UpdateProject, DeleteProject
ProjectSharingEndpoints.cs      ← GetSharing, UpdateSharing
ProjectDiagramEndpoints.cs      ← CreateDiagram, UpdateDiagram, DeleteDiagram
ProjectWorkspaceEndpoints.cs    ← GetWorkspaceState, SaveWorkspaceState, SaveVisualState
```

Cada archivo expone un método de extensión estático `MapXxxEndpoints(RouteGroupBuilder group)`
que `ProjectEndPoint.MapEndPoint` llama:

```csharp
public void MapEndPoint(WebApplication app)
{
    var group = app.MapGroup("/api/project").RequireAuthorization();
    group.MapProjectQueryEndpoints();
    group.MapProjectCommandEndpoints();
    group.MapProjectSharingEndpoints();
    group.MapProjectDiagramEndpoints();
    group.MapProjectWorkspaceEndpoints();
}
```

### Paso 2 — Optimizar `JoinProject` en el Hub

Reemplazar las dos queries con una proyección única:

```csharp
var projectAccess = await _context.Projects
    .Where(p => p.Id == projectId)
    .Select(p => new
    {
        HasAccess = p.OwnerUserId == userId ||
                    p.Collaborators.Any(c => c.UserId == userId),
        // Datos del usuario desde la misma query si están en la misma DB
    })
    .FirstOrDefaultAsync();
```

Si `User` y `Project` están en el mismo contexto, un JOIN o include elimina el segundo
round-trip. Si no, al menos paralelizar con `Task.WhenAll`.

### Paso 3 — Limpiar `RegisterServices`

**Opción A — Eliminar los métodos vacíos:**
Si `AddApplicationServices` y `AddRepositories` no registran nada y no van a registrar nada
en el futuro cercano, eliminarlos de `AddServerServices`.

**Opción B — Documentarlos como placeholders:**
Si se anticipa que registrarán servicios pronto, agregar un comentario explícito:
```csharp
internal static IServiceCollection AddApplicationServices(...)
{
    // Placeholder para servicios de aplicación futuros (auditoría, notificaciones, etc.)
    return services;
}
```

### Paso 4 — Reemplazar `Debugger.Break()` por log estructurado

```csharp
#if DEBUG
    // Antes:
    System.Diagnostics.Debugger.Break();
    
    // Después:
    Console.Error.WriteLine($"[ERROR] Failed to map endpoint {endpoint.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
#endif
```

Si hay un `ILogger` disponible en ese contexto, usarlo en lugar de `Console.Error`.

---

## Verificación

1. `dotnet build` sin errores ni warnings nuevos.
2. Todos los endpoints de proyecto responden correctamente con las mismas rutas.
3. El Hub de SignalR sigue funcionando — usuarios pueden unirse y recibir eventos.
4. `rg "Debugger\.Break"` no debe aparecer en código de producción (solo en Debug
   condicional y reemplazado por log).
5. Prueba de carga básica: varios usuarios abriendo el mismo proyecto simultáneamente.

---

## Riesgo
**Medio.** La división de endpoints es principalmente una reorganización de código sin cambio
de comportamiento. El riesgo real está en el Hub — verificar que la query optimizada
retorna los mismos resultados de acceso que las dos queries originales.

---

## Dependencias previas
Specs 01-03 recomendadas antes, pero no hay dependencia técnica directa.
