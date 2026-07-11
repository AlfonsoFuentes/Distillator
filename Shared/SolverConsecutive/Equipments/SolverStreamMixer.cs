using Shared.SolverQwen.Stream;

namespace Shared.SolverConsecutive.Equipments
{
    public enum StreamMixerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }
    public class SolverStreamMixer : SolverEquipmentBase
    {
        public IFacadeStream Outlet { get; set; } = null!;

        public override List<ISolverEquation> Equations => GetEquations().ToList();

        public SolverStreamMixer(string name)
        {
            Name = name;
        }

        public void SetOutlet(IFacadeStream outlet)
        {
            if (outlet != null)
            {
                Outlets.Add(outlet);
                Outlet = outlet;
                Outlet.EquipmentInlet = this;
            }
        }

        public void UnSetOutlet()
        {
            if (Outlet == null) return;
            Outlets.Remove(Outlet);
            Outlet.EquipmentInlet = null!;
            Outlet = null!;
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

        public StreamMixerStateType State => GetState();

        private StreamMixerStateType GetState()
        {
            if (Outlet == null || Inlets.Count == 0)
                return StreamMixerStateType.PartiallyConnected;

            if (!Outlet.MassFlow.IsDefined)
                return StreamMixerStateType.ReadyToCalculate;

            bool allOutletsCalculated = Inlets.All(o => o.MassFlow.IsDefined);

            if (allOutletsCalculated)
                return StreamMixerStateType.Solved;

            return StreamMixerStateType.ReadyToCalculate;
        }

        private IEnumerable<ISolverEquation> GetEquations()
        {
            yield return new MixerPressureEquation(this);
            yield return new EquipmentMassBalanceEquation(this);
            yield return new EquipmentMassEnergyBalanceEquation(this);
            yield return new EquipmentComponentMassBalanceEquation(this);
            yield return new EquipmentMassEnergyBalanceWithComponentsEquation(this);

        }

        public override Task PostSolveAsync()
        {
            return Task.CompletedTask;
        }
    }
    public class MixerPressureEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        private readonly SolverStreamMixer mixer;

        public MixerPressureEquation(SolverStreamMixer mixer)
        {
            this.mixer = mixer;
        }

        public string Name => $"{EquationType} - {mixer.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        private List<double> GetResiduals()
        {
            var residuals = new List<double>();
            if (mixer.Outlet == null || mixer.Inlets.Count == 0)
            {
                return residuals;
            }

            var minimumInletPressure = mixer.Inlets.Min(inlet => inlet.Pressure.GetSolverValue());
            residuals.Add(mixer.Outlet.Pressure.GetSolverValue() - minimumInletPressure);
            return residuals;
        }

        private List<IVariable> GetVariables()
        {
            var variables = new List<IVariable>();
            if (mixer.Outlet == null || mixer.Inlets.Count == 0)
            {
                return variables;
            }

            variables.Add(mixer.Outlet.Pressure);
            foreach (var inlet in mixer.Inlets)
            {
                variables.Add(inlet.Pressure);
            }

            return variables;
        }
    }


}
