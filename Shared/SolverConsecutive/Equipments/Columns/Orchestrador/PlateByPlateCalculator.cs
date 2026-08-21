using Shared.SolverQwen.Stream;
using System.Collections.Immutable;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments.Columns.Orchestrador
{


    public sealed class PlateByPlateCalculator : IColumnPostSolverCalculation
    {
        public int Order => 3;

        private readonly SolverColumn _column;

        public PlateByPlateCalculator(SolverColumn column)
        {
            _column = column;
        }

        public async Task CalculateAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Trace("PlateByPlate started", $"state={_column.State}; topologyChanged={_column.Orchestrator?.TopologyChanged}");

            if (_column.State != ColumnStateType.Solved)
            {
                stopwatch.Stop();
                Trace("PlateByPlate skipped", $"reason=column not solved; elapsedMs={stopwatch.ElapsedMilliseconds}");
                return;
            }

            // 🔥 Si la topología no cambió, usar caché
            // 🔥 Si la topología no cambió Y FUG no recalculó, usar caché
            if (_column.Orchestrator != null && !_column.Orchestrator.TopologyChanged )
            {
                stopwatch.Stop();
                Trace("PlateByPlate skipped", $"reason=topology unchanged; elapsedMs={stopwatch.ElapsedMilliseconds}");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    var plateSolver = new ColumnIdealPlateSolver(_column);
                    plateSolver.Solve();

                    // 🔥 Notificar al orquestador que los platos están listos
                    // El orquestador los acumula vía OnPlateSolved durante el Solve()
                    // Aquí solo necesitamos convertir la lista interna a ImmutableList
                    // y enviarla al orquestador

                    // Como el orquestador ya tiene los platos en _stages (vía OnPlateSolved),
                    // necesitamos un mecanismo para que el orquestador los convierta a ImmutableList
                    // y los guarde en el caché. Esto se hace en el orquestador.

                }
                catch (Exception)
                {
                    _column.Orchestrator?.SetStages(ImmutableList<StageResult>.Empty);
                }
            }, cancellationToken);

            // 🔥 Después del solver, notificar al orquestador que los platos están listos
            // El orquestador debe tomar los platos de su lista interna _stages
            _column.Orchestrator?.NotifyPlatesCalculationComplete();
            stopwatch.Stop();
            Trace("PlateByPlate finished", $"elapsedMs={stopwatch.ElapsedMilliseconds}");
        }

        private void Trace(string message, string? detail = null)
        {
            _column.TraceSink?.TraceSolver($"Column {_column.Name}: {message}", detail);
        }
    }
}
