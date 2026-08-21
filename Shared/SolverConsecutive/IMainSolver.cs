using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using UnitSystem;

namespace Shared.SolverConsecutive
{
    public interface IMainSolver
    {
        List<IFacadeStream> Streams { get; }
        List<ISolverEquipment> Equipments { get; }
        void AddStream(IFacadeStream stream);
        void RemoveStream(IFacadeStream stream);
        void RemoveEquipment(ISolverEquipment equipment);
        void AddEquipment(ISolverEquipment equipment);

        Length Altitude { get; set; }
        Pressure AtmosphericPressure { get; set; }
        ThermodynamicMethodFullDto ThermoMethod { get; set; }
        ISolverTraceSink? TraceSink { get; set; }

        Task<SimulationRunResult> RunSimulationAsync();

        event Action? OnSimulationCompleted;
        void ClearOrphanStream(IFacadeStream stream);

    }

}
