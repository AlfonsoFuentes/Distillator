using Shared.SolverQwen.Stream;

namespace Shared.SolverConsecutive.Equipments
{
    public enum VesselStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }
    public class SolverVessel : SolverEquipmentBase
    {
  

        public override List<ISolverEquation> Equations => GetEquations().ToList();

        public SolverVessel(string name)
        {
            Name = name;
        }

        public void AddOutlet(IFacadeStream stream)
        {
            if (Outlets.Contains(stream)) return;
            Outlets.Add(stream);
            stream.EquipmentInlet = this;
        }

        public void AddInlet(IFacadeStream stream)
        {
            if (Inlets.Contains(stream)) return;
            Inlets.Add(stream);
            stream.EquipmentOutlet = this;
        }

        public void RemoveInlet(IFacadeStream stream)
        {
            if (!Inlets.Contains(stream)) return;
            Inlets.Remove(stream);
            stream.EquipmentOutlet = null!;
        }
        public void RemoveOulet(IFacadeStream stream)
        {
            if (!Outlets.Contains(stream)) return;
            Outlets.Remove(stream);
            stream.EquipmentInlet = null!;
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
        
            yield return new EquipmentMassBalanceEquation(this);
            yield return new EquipmentMassEnergyBalanceEquation(this);
            yield return new EquipmentComponentMassBalanceEquation(this);

        }
        public override Task PostSolveAsync()
        {
            // El Splitter no tiene KPIs post-convergencia (como Power en la Bomba)
            // Pero dejamos el método para seguir el patrón de SolverEquipmentBase
            return Task.CompletedTask;
        }
       
    }


}
