# Tasks — Shared/Results: Inmutabilidad y contrato limpio

## Task 1 — Buscar usos externos de las propiedades mutables
- [ ] Ejecutar `rg "\.Messages\s*=" --include="*.cs"` en el workspace y registrar resultados.
- [ ] Ejecutar `rg "\.Succeeded\s*=" --include="*.cs"` en el workspace y registrar resultados.
- [ ] Si hay usos fuera de `Result.cs`, listarlos antes de continuar.

## Task 2 — Cambiar `IResult.Messages` a solo lectura
- [ ] Abrir `Shared/Results/Result.cs`.
- [ ] En `IResult`, cambiar `List<string> Messages { get; set; }` por `IReadOnlyList<string> Messages { get; }`.
- [ ] En la clase `Result`, cambiar `public List<string> Messages { get; set; }` por `public List<string> Messages { get; private set; }`.
- [ ] Verificar que los factory methods `Fail(List<string> messages)` y `Success(...)` asignan la lista directamente en el inicializador.

## Task 3 — Cambiar `IResult.Succeeded` a solo lectura
- [ ] En `IResult`, cambiar `bool Succeeded { get; set; }` por `bool Succeeded { get; }`.
- [ ] En la clase `Result`, cambiar `public bool Succeeded { get; set; }` por `public bool Succeeded { get; private set; }`.
- [ ] Verificar que todos los factory methods asignan `Succeeded` mediante inicializador de objeto o constructor.

## Task 4 — Eliminar restricción `where T : class`
- [ ] Localizar el método `EndPointResult<T>` en la clase `Result`.
- [ ] Eliminar `where T : class` de la firma.
- [ ] Verificar que la lógica interna no asume que `T` es referencia (no hay comparación con `null` que dependa de ello).

## Task 5 — Corregir usos externos rotos (si los hay)
- [ ] Para cada uso externo encontrado en Task 1, reemplazarlo por el factory method correcto.
- [ ] Ejemplo: si hay `result.Succeeded = true`, cambiarlo por `result = Result.Success()`.

## Task 6 — Verificar compilación
- [ ] Ejecutar `dotnet build Shared/Shared.csproj` y confirmar 0 errores.
- [ ] Ejecutar `dotnet build` en la solución completa y confirmar 0 errores nuevos.
