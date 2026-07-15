# Refactorización Distillator — Índice General

Orden de ejecución: de lo más general a lo más particular.
Cada spec es independiente y verificable antes de pasar a la siguiente.

| # | Spec | Capa | Riesgo | Estado |
|---|------|------|--------|--------|
| 01 | [Shared/Results — Inmutabilidad y contrato limpio](./01-shared-results.md) | Shared | Bajo | Pendiente |
| 02 | [SolverConsecutive — Unificar Newton + limpiar legacy](./02-solver-consecutive.md) | Shared | Medio | Pendiente |
| 03 | [Domain — Walker de grafo + eventos + factories](./03-domain.md) | Domain | Medio | Pendiente |
| 04 | [ProcessFlowDiagram — Connect/Disconnect + código muerto](./04-process-flow-diagram.md) | Shared | Medio | Pendiente |
| 05 | [SolverQwen — Sacar tests de producción + sellar IFacadeStream](./05-solver-qwen.md) | Shared | Bajo | Pendiente |
| 06 | [UnitOperations — Definir o eliminar la capa](./06-unit-operations.md) | Shared | Bajo/Alto* | Pendiente |
| 07 | [Server — Dividir ProjectEndPoint + Hub query](./07-server.md) | Server | Medio | Pendiente |
| 08 | [Client — Limpiar legacy + dividir ProjectSessionService + DI solver](./08-client.md) | Client | Alto | Pendiente |

> *Spec 06: Bajo riesgo si se elige Opción C (limpiar carpetas vacías). Alto si se elige
> Opción A o B (reorganización arquitectural completa).

---

## Resumen de impacto por spec

| # | Líneas eliminadas aprox. | Deuda resuelta |
|---|---|---|
| 01 | ~5 líneas cambiadas | Interfaz inmutable, contratos confiables |
| 02 | ~300 líneas eliminadas | DRY en solver Newton, legacy limpio |
| 03 | ~200 líneas refactorizadas | Walker único, evento de dominio correcto |
| 04 | ~100 líneas eliminadas | Connect/Disconnect consistentes |
| 05 | ~700 líneas sacadas de producción | Assembly WASM más liviano |
| 06 | ~0-3 carpetas vacías | Claridad arquitectural |
| 07 | ~400 líneas reorganizadas | SRP en endpoints, Hub optimizado |
| 08 | ~2000 líneas eliminadas | Migración legacy cerrada, DI limpio |
