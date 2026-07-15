# Design — Shared/Results: Inmutabilidad y contrato limpio

## Archivo afectado
`Shared/Results/Result.cs`

## Cambios de interfaz

```csharp
// ANTES:
public interface IResult
{
    List<string> Messages { get; set; }
    string Message { get; }
    bool Succeeded { get; set; }
}

// DESPUÉS:
public interface IResult
{
    IReadOnlyList<string> Messages { get; }
    string Message { get; }
    bool Succeeded { get; }
}
```

## Cambios en la clase `Result`

- `Messages` cambia de `List<string> { get; set; }` a `List<string> { get; private set; }`
  (o `init`). La implementación explícita de `IResult.Messages` retorna la lista como
  `IReadOnlyList<string>`.
- `Succeeded` cambia de `{ get; set; }` a `{ get; private set; }` (o `init`).
- Los factory methods usan inicializadores de objeto con `init` o constructores internos.

## Cambio en `EndPointResult<T>`

```csharp
// ANTES:
public static IResult<T> EndPointResult<T>(T data, ...) where T : class

// DESPUÉS:
public static IResult<T> EndPointResult<T>(T data, ...)
```

## Estrategia de compatibilidad

1. Buscar con `rg "\.Messages\s*="` todos los usos externos.
2. Buscar con `rg "\.Succeeded\s*="` todos los usos externos.
3. Si hay usos externos fuera de `Result.cs`, corregirlos para usar factory methods.
4. La mayoría del código solo lee `result.Succeeded` y `result.Messages` — no se rompe.
