using Shared.SolverQwen.Stream;

namespace Shared.SolverConsecutive.Equipments
{
    public enum StreamMixerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }
    public class SolverStreamMixer : SolverEquipmentBase
    {
        public IFacadeStream Outlet { get; set; } = null!;
        public List<IFacadeStream> Inlets { get; set; } = new();

        public override List<ISolverEquation> Equations => GetEquations().ToList();

        public SolverStreamMixer(string name)
        {
            Name = name;
        }

        public void SetOutlet(IFacadeStream stream)
        {
            Outlet = stream;
        }

        public void AddInlet(IFacadeStream stream)
        {
            Inlets.Add(stream);
        }

        public void RemoveInlet(IFacadeStream stream)
        {
            Inlets.Remove(stream);
        }

        // ====================================================================
        // ESTADO DEL EQUIPO
        // ====================================================================
        public StreamMixerStateType State => GetState();

        private StreamMixerStateType GetState()
        {
            // 1. Topología: Verificar conexiones mínimas
            if (Outlet == null || Inlets.Count == 0)
                return StreamMixerStateType.PartiallyConnected;

            // 2. Verificar si el Inlet tiene flujo definido
            if (!Outlet.MassFlow.IsDefined)
                return StreamMixerStateType.ReadyToCalculate;

            // 3. Verificar si TODOS los Outlets tienen flujo calculado
            bool allOutletsCalculated = Inlets.All(o => o.MassFlow.IsDefined);

            if (allOutletsCalculated)
                return StreamMixerStateType.Solved;

            return StreamMixerStateType.ReadyToCalculate;
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
