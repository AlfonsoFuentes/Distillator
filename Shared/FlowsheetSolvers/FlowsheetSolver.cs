using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Pumps;
using Shared.UnitOperations.Streams;

namespace Shared.FlowsheetSolvers
{
    public class PlantManager
    {
        private readonly FlowsheetSolver _solver;

        public List<IFacade> Equipments { get; } = new();
        public List<StreamSimulationFacade> Streams { get; } = new();

        public PlantManager()
        {
            _solver = new FlowsheetSolver();
        }

        // Registra un equipo cuando el usuario lo pone en el lienzo
        public void AddEquipment(IFacade equipment)
        {
            Equipments.Add(equipment);

            // Nos suscribimos al evento: "Si este equipo grita, llamamos al Solver"
            equipment.OnExecuteSolver = HandleRecalculationRequest;
        }

        // Registra una corriente (tubo) cuando el usuario lo conecta
        public void AddStream(StreamSimulationFacade stream)
        {

            Streams.Add(stream);

            // Nos suscribimos al evento del tubo también
            stream.OnExecuteSolver = HandleRecalculationRequest;

        }

        // El método central que atrapa cualquier cambio en la UI
        private void HandleRecalculationRequest(IFacade sourceNode)
        {
            // 🚨 Le pasamos al Solver: Quién empezó el problema, y el mapa completo (equipos y tubos)
            _solver.SolveTwoPass(sourceNode, Equipments, Streams);
        }
    }

    public class FlowsheetSolver
    {
        public void SolveTwoPass(IFacade triggerNode, List<IFacade> equipments, List<StreamSimulationFacade> streams)
        {
            if (equipments.Count == 0) return;

            // --- PASE 1 y 2: BUCLE DE CONVERGENCIA ---
            int maxIterations = 5;
            for (int i = 0; i < maxIterations; i++)
            {
                int varsBefore = CountTotalDefinedVariables(streams);

                PropagateForward(triggerNode, streams);
                PropagateBackward(triggerNode, streams);

                int varsAfter = CountTotalDefinedVariables(streams);

                // 🛑 EL FRENO DE MANO
                if (varsBefore == varsAfter)
                {
                    break;
                }
            }
        }

        private int CountTotalDefinedVariables(List<StreamSimulationFacade> streams)
        {
            int count = 0;
            foreach (var s in streams)
            {
                if (s.Temperature.IsDefined) count++;
                if (s.Pressure.IsDefined) count++;
                if (s.VaporFraction.IsDefined) count++;
                if (s.MolarEnthalpy.IsDefined) count++;
                if (s.MassFlow.IsDefined) count++;
                if (s.MolarFlow.IsDefined) count++;
                if (s.StreamComposition.IsDefined) count++;
            }
            return count;
        }

        private void PropagateForward(IFacade triggerNode, List<StreamSimulationFacade> streams)
        {
            var queue = new Queue<IFacade>();
            var visited = new HashSet<Guid>();

            // 1. Iniciamos exactamente donde se hizo el cambio (Sea equipo o corriente)
            queue.Enqueue(triggerNode);

            // 2. Propagación Segura Alternada
            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();

                if (visited.Contains(currentNode.Id)) continue;
                visited.Add(currentNode.Id);

                // ⚙️ MAGIA SEGURA: Aquí se calcula el nodo actual (Si es bomba hace balance, si es corriente hace Flash)
                currentNode.Calculate();

                // 🔀 ENRUTAMIENTO: ¿A dónde vamos ahora?
                if (currentNode is StreamSimulationFacade stream)
                {
                    // Si acabamos de calcular una corriente, el siguiente es el EQUIPO al que entra
                    if (stream.TargetEquipment != null)
                    {
                        queue.Enqueue(stream.TargetEquipment);
                    }
                }
                else
                {
                    // Si acabamos de calcular un Equipo, buscamos TODAS sus CORRIENTES de salida
                    var outputStreams = streams.Where(s => s.SourceEquipment?.Id == currentNode.Id);
                    foreach (var nextStream in outputStreams)
                    {
                        queue.Enqueue(nextStream);
                    }
                }
            }
        }

        private void PropagateBackward(IFacade triggerNode, List<StreamSimulationFacade> streams)
        {
            var queue = new Queue<IFacade>();
            var visited = new HashSet<Guid>();

            // 1. Iniciamos exactamente donde se hizo el cambio
            queue.Enqueue(triggerNode);

            // 2. Propagación Segura hacia atrás
            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();

                if (visited.Contains(currentNode.Id)) continue;
                visited.Add(currentNode.Id);

                // ⚙️ Cálculo seguro del nodo
                currentNode.Calculate();

                // 🔀 ENRUTAMIENTO HACIA ATRÁS:
                if (currentNode is StreamSimulationFacade stream)
                {
                    // Si estoy en una corriente, voy hacia el EQUIPO que la generó
                    if (stream.SourceEquipment != null)
                    {
                        queue.Enqueue(stream.SourceEquipment);
                    }
                }
                else
                {
                    // Si estoy en un Equipo, voy hacia TODAS sus CORRIENTES de entrada
                    var inputStreams = streams.Where(s => s.TargetEquipment?.Id == currentNode.Id);
                    foreach (var prevStream in inputStreams)
                    {
                        queue.Enqueue(prevStream);
                    }
                }
            }
        }
    }
}
