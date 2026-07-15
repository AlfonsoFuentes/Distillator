# Spec 01 — Shared/Results: Inmutabilidad y contrato limpio

## Estado
Pendiente

## Archivo afectado
`Shared/Results/Result.cs`

---

## Contexto

El patrón Railway (`Result<T>`) está bien implementado y es usado en todo el proyecto
(servidor, dominio, endpoints). El problema es que las interfaces `IResult` e `IResult<T>`
exponen propiedades mutables (`Messages`, `Succeeded` con setter público), lo que permite
que el caller modifique el resultado después de construirlo. Esto rompe el contrato de
un resultado como valor inmutable.

---

## Problemas a resolver

### 1. [MEDIA] `IResult.Messages` es mutable
```csharp
// Actual — permite mutación externa:
List<string> Messages { get; set; }

// Objetivo — solo lectura desde la interfaz:
IReadOnlyList<string> Messages { get; }
```

### 2. [MEDIA] `IResult.Succeeded` tiene setter público en la interfaz
```csharp
// Actual:
bool Succeeded { get; set; }

// Objetivo:
bool Succeeded { get; }
```

### 3. [BAJA] `EndPointResult<T>` restringe innecesariamente a `where T : class`
```csharp
// Actual — prohíbe structs y tipos valor:
public static IResult<T> EndPointResult<T>(T data, ...) where T : class

// Objetivo — sin restricción:
public static IResult<T> EndPointResult<T>(T data, ...)
```

### 4. [BAJA] DRY menor — `Result<T>` repite campos de construcción del padre
Los métodos `Fail()`/`Success()` en `Result<T>` duplican la lógica de `Result`.
Se pueden simplificar para que `Result<T>` delegue al padre.

---

## Cambios requeridos

### Interfaces
```csharp
public interface IResult
{
    IReadOnlyList<string> Messages { get; }
    string Message { get; }
    bool Succeeded { get; }
}

public interface IResult<out T> : IResult
{
    T Data { get; }
}
```

### Clase Result
- Cambiar `List<string> Messages { get; set; }` a `List<string> Messages { get; }` con init privado.
- Cambiar `bool Succeeded { get; set; }` a `bool Succeeded { get; }` con init privado (o `init`).
- Exponer `IReadOnlyList<string>` en la interfaz pero mantener `List<string>` interno para construcción.
- Quitar `where T : class` de `EndPointResult<T>`.

### Compatibilidad
- Los usos actuales que acceden a `result.Messages` solo para leer no se rompen.
- Los usos que asignan `result.Messages = ...` o `result.Succeeded = ...` desde fuera deben
  identificarse y corregirse (se espera que sean pocos o ninguno fuera de los factory methods).

---

## Verificación

1. El proyecto compila sin errores tras el cambio.
2. Buscar con `rg "\.Messages\s*="` y `rg "\.Succeeded\s*="` para confirmar que no hay
   asignaciones externas que se rompan.
3. Los endpoints del servidor siguen retornando los resultados correctamente.

---

## Riesgo
**Bajo.** El cambio es en interfaces y propiedades — si solo se leen desde fuera (que es el
contrato esperado), no hay ruptura. La búsqueda de asignaciones externas confirma el riesgo
real antes de aplicar.

---

## Dependencias previas
Ninguna. Esta es la primera spec.
