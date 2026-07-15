# Requirements — Shared/Results: Inmutabilidad y contrato limpio

## Problema
Las interfaces `IResult` e `IResult<T>` exponen propiedades mutables (`Messages` con setter,
`Succeeded` con setter), lo que permite que código externo modifique el estado de un resultado
después de crearlo. Esto rompe el contrato de un resultado como valor inmutable.

## Requisitos

### REQ-01 — `IResult.Messages` debe ser de solo lectura
- `IResult.Messages` expone `IReadOnlyList<string>` sin setter.
- La clase concreta `Result` mantiene internamente `List<string>` para construcción.
- Código externo no puede asignar ni mutar la lista directamente.

### REQ-02 — `IResult.Succeeded` debe ser de solo lectura
- `IResult.Succeeded` expone solo getter.
- El valor solo se establece en los factory methods (`Fail`, `Success`).

### REQ-03 — `EndPointResult<T>` no debe restringir a tipos de referencia
- Eliminar `where T : class` de `Result.EndPointResult<T>`.
- El método debe aceptar structs y tipos valor además de clases.

### REQ-04 — El proyecto debe compilar sin errores tras el cambio
- Ningún código externo que solo lea resultados debe romperse.
- Los factory methods (`Fail`, `Success`) siguen funcionando igual.

## Criterios de aceptación
- `dotnet build` completa sin errores ni warnings nuevos.
- No existen asignaciones externas a `result.Messages = ...` ni `result.Succeeded = ...`
  fuera de los factory methods internos.
- Los endpoints del servidor siguen retornando resultados correctamente.
