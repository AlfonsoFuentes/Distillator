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
            if (_column.State != ColumnStateType.Solved)
            {
                Console.WriteLine($"⚠️ Platos: Columna no está resuelta, saltando cálculo");
                return;
            }

            // 🔥 Si la topología no cambió, usar caché
            // 🔥 Si la topología no cambió Y FUG no recalculó, usar caché
            if (_column.Orchestrator != null && !_column.Orchestrator.TopologyChanged )
            {
                Console.WriteLine($"✅ Platos: Topología y FUG sin cambios, usando caché");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"📊 Calculando platos...");
                    var plateSolver = new ColumnIdealPlateSolver(_column);
                    plateSolver.Solve();

                    // 🔥 Notificar al orquestador que los platos están listos
                    // El orquestador los acumula vía OnPlateSolved durante el Solve()
                    // Aquí solo necesitamos convertir la lista interna a ImmutableList
                    // y enviarla al orquestador

                    // Como el orquestador ya tiene los platos en _stages (vía OnPlateSolved),
                    // necesitamos un mecanismo para que el orquestador los convierta a ImmutableList
                    // y los guarde en el caché. Esto se hace en el orquestador.

                    Console.WriteLine($"✅ Platos calculados");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error en Platos: {ex.Message}");
                    _column.Orchestrator?.SetStages(ImmutableList<StageResult>.Empty);
                }
            }, cancellationToken);

            // 🔥 Después del solver, notificar al orquestador que los platos están listos
            // El orquestador debe tomar los platos de su lista interna _stages
            _column.Orchestrator?.NotifyPlatesCalculationComplete();
        }
    }
}