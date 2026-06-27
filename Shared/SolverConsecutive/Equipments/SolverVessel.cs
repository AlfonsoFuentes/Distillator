using Shared.SolverQwen.Stream;

namespace Shared.SolverConsecutive.Equipments
{
    public enum VesselStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }
    public class SolverVessel : SolverEquipmentBase
    {
        public List<IFacadeStream> Outlets { get; set; } = new();
   
        public List<IFacadeStream> Inlets { get; set; } = new();

        public override List<ISolverEquation> Equations => GetEquations().ToList();

        public SolverVessel(string name)
        {
            Name = name;
        }

        public void AddOutlet(IFacadeStream stream)
        {
            Outlets.Add(stream);
        }

        public void AddInlet(IFacadeStream stream)
        {
            Inlets.Add(stream);
        }

        public void RemoveInlet(IFacadeStream stream)
        {
            Inlets.Remove(stream);
        }
        public void RemoveOulet(IFacadeStream stream)
        {
            Outlets.Remove(stream);
        }

        // ====================================================================
        // ESTADO DEL EQUIPO
        // ====================================================================
        public VesselStateType State => GetState();

        private VesselStateType GetState()
        {
            // 1. Topología: Verificar conexiones mínimas
            if (Outlets.Count == 0 || Inlets.Count == 0)
                return VesselStateType.Created;

            // 2. Verificar si el Inlet tiene flujo definido
           

            // 3. Verificar si TODOS los Outlets tienen flujo calculado
            bool allOutletsCalculated = Inlets.All(o => o.MassFlow.IsDefined);

            if (allOutletsCalculated)
                return VesselStateType.Solved;

            return VesselStateType.ReadyToCalculate;
        }

        // ====================================================================
        // GENERADOR DE ECUACIONES
        // ====================================================================
        private IEnumerable<ISolverEquation> GetEquations()
        {
            yield return null!;

        }
        public override Task PostSolveAsync()
        {
            // El Splitter no tiene KPIs post-convergencia (como Power en la Bomba)
            // Pero dejamos el método para seguir el patrón de SolverEquipmentBase
            return Task.CompletedTask;
        }
    }


}
