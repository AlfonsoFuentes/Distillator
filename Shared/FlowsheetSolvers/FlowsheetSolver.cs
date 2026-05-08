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
            
        }

        // Registra una corriente (tubo) cuando el usuario lo conecta
        public void AddStream(StreamSimulationFacade stream)
        {

            Streams.Add(stream);

            // Nos suscribimos al evento del tubo también
 

        }

        // El método central que atrapa cualquier cambio en la UI
        private void HandleRecalculationRequest(IFacade sourceNode)
        {
            // 🚨 Le pasamos al Solver: Quién empezó el problema, y el mapa completo (equipos y tubos)
           
        }
    }

    public class FlowsheetSolver
    {
       

   

       
    }
}
