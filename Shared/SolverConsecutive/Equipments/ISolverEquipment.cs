using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;

namespace Shared.SolverConsecutive.Equipments
{
    // ============================================================================
    // INTERFACES Y CLASE BASE
    // ============================================================================

    public interface ISolverEquipment : IFacade
    {
        List<ISolverEquation> Equations { get; }
        IReadOnlyList<ISpecification> Specifications { get; }
        List<IFacadeStream> Inlets { get; }
        List<IFacadeStream> Outlets { get; }
        IEnumerable<ISolverEquipment> GetDownstreamEquipments();
        IEnumerable<ISolverEquipment> GetUpstreamEquipments();
        List<IFacadeStream> AllStreams { get; }
    }

    public abstract class SolverEquipmentBase : ISolverEquipment, IEquipmentFacade
    {
        public List<IFacadeStream> AllStreams => Inlets.Concat(Outlets).ToList();
        public string Name { get; set; } = string.Empty;
        public abstract List<ISolverEquation> Equations { get; }

        public List<IFacadeStream> Inlets { get; private set; } = new();
        public List<IFacadeStream> Outlets { get; private set; } = new();

        public virtual IEnumerable<ISolverEquipment> GetUpstreamEquipments()
        {
            foreach (var stream in Inlets)
            {
                if (stream.EquipmentInlet != null)
                {
                    yield return stream.EquipmentInlet;
                }
            }
        }

        public virtual IEnumerable<ISolverEquipment> GetDownstreamEquipments()
        {
            foreach (var stream in Outlets)
            {
                if (stream.EquipmentOutlet != null)
                {
                    yield return stream.EquipmentOutlet;
                }
            }
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        private readonly List<ISpecification> _specifications = new();
        public IReadOnlyList<ISpecification> Specifications => _specifications.AsReadOnly();

        public SolverEquipmentBase()
        {
        }

        public virtual Task PostSolveAsync()
        {
            return Task.CompletedTask;
        }

        public void AddSpec(ISpecification spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (_specifications.Any(s => s.Id == spec.Id))
                throw new InvalidOperationException($"Spec {spec.Id} ya existe en {Name}");

            _specifications.Add(spec);
        }

        public void RemoveSpec(ISpecification spec)
        {
            if (spec != null) _specifications.Remove(spec);
        }

        public void ClearSpecs()
        {
            _specifications.Clear();
        }


    }


    public class EquipmentMassBalanceEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public EquipmentMassBalanceEquation(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return r;
            double massin = eq.Inlets.Sum(i => i.MassFlow.GetSolverValue());
            double massout = eq.Outlets.Sum(o => o.MassFlow.GetSolverValue());
            r.Add(massin - massout);


            return r;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return v;

            foreach (var stream in eq.AllStreams)
            {
                v.Add(stream.MassFlow);
            }

            return v;
        }
    }

    public class EquipmentMassEnergyBalanceEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public EquipmentMassEnergyBalanceEquation(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return r;

            double energyIn = eq.Inlets.Sum(i =>
                i.MassFlow.GetSolverValue() * i.MassEnthalpy.GetSolverValue());

            double energyOut = eq.Outlets.Sum(o =>
                o.MassFlow.GetSolverValue() * o.MassEnthalpy.GetSolverValue());

            r.Add(energyIn - energyOut);

            double massin = eq.Inlets.Sum(i => i.MassFlow.GetSolverValue());
            double massout = eq.Outlets.Sum(o => o.MassFlow.GetSolverValue());
            r.Add(massin - massout);
            //int ncomponents = GetComponentCount();
            //if (ncomponents <= 1) return r;
            //if (!CanUseComponents(ncomponents)) return r;

            //for (int componentIndex = 0; componentIndex < ncomponents; componentIndex++)
            //{
            //    int i = componentIndex;

            //    double componentIn = eq.Inlets.Sum(s =>
            //        s.MassFlow.GetSolverValue() *
            //        s.Composition.Components[i].MassFraction.GetSolverValue());

            //    double componentOut = eq.Outlets.Sum(s =>
            //        s.MassFlow.GetSolverValue() *
            //        s.Composition.Components[i].MassFraction.GetSolverValue());

            //    r.Add(componentIn - componentOut);
            //}

            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return v;

            foreach (var stream in eq.AllStreams)
            {
                v.Add(stream.MassFlow);
                v.Add(stream.MassEnthalpy);
            }
            //int ncomponents = GetComponentCount();
            //if (ncomponents <= 1) return v;
            //if (!CanUseComponents(ncomponents)) return v;

            //foreach (var stream in eq.AllStreams)
            //{
            //    v.Add(stream.MassFlow);

            //    for (int componentIndex = 0; componentIndex < ncomponents; componentIndex++)
            //    {
            //        v.Add(stream.Composition.Components[componentIndex].MassFraction);
            //    }
            //}

            return v;
        }
        private int GetComponentCount()
        {
            return eq.AllStreams
                .FirstOrDefault(s => s.Composition != null)
                ?.Composition.Components.Count ?? 0;
        }

        private bool CanUseComponents(int ncomponents)
        {
            return eq.AllStreams.All(stream =>
                stream.Composition != null &&
                stream.Composition.Components.Count == ncomponents);
        }
    }
    public class EquipmentMassEnergyBalanceWithComponentsEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public EquipmentMassEnergyBalanceWithComponentsEquation(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return r;

            double energyIn = eq.Inlets.Sum(i =>
                i.MassFlow.GetSolverValue() * i.MassEnthalpy.GetSolverValue());

            double energyOut = eq.Outlets.Sum(o =>
                o.MassFlow.GetSolverValue() * o.MassEnthalpy.GetSolverValue());

            r.Add(energyIn - energyOut);

            double massin = eq.Inlets.Sum(i => i.MassFlow.GetSolverValue());
            double massout = eq.Outlets.Sum(o => o.MassFlow.GetSolverValue());
            r.Add(massin - massout);
            int ncomponents = GetComponentCount();
            if (ncomponents <= 1) return r;
            if (!CanUseComponents(ncomponents)) return r;

            for (int componentIndex = 0; componentIndex < ncomponents; componentIndex++)
            {
                int i = componentIndex;

                double componentIn = eq.Inlets.Sum(s =>
                    s.MassFlow.GetSolverValue() *
                    s.Composition.Components[i].MassFraction.GetSolverValue());

                double componentOut = eq.Outlets.Sum(s =>
                    s.MassFlow.GetSolverValue() *
                    s.Composition.Components[i].MassFraction.GetSolverValue());

                r.Add(componentIn - componentOut);
            }

            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return v;

            foreach (var stream in eq.AllStreams)
            {
                v.Add(stream.MassFlow);
                v.Add(stream.MassEnthalpy);
            }
            int ncomponents = GetComponentCount();
            if (ncomponents <= 1) return v;
            if (!CanUseComponents(ncomponents)) return v;

            foreach (var stream in eq.AllStreams)
            {
                v.Add(stream.MassFlow);

                for (int componentIndex = 0; componentIndex < ncomponents; componentIndex++)
                {
                    v.Add(stream.Composition.Components[componentIndex].MassFraction);
                }
            }

            return v;
        }
        private int GetComponentCount()
        {
            return eq.AllStreams
                .FirstOrDefault(s => s.Composition != null)
                ?.Composition.Components.Count ?? 0;
        }

        private bool CanUseComponents(int ncomponents)
        {
            return eq.AllStreams.All(stream =>
                stream.Composition != null &&
                stream.Composition.Components.Count == ncomponents);
        }
    }
    public class EquipmentComponentMassBalanceEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;

        private readonly ISolverEquipment eq;

        public EquipmentComponentMassBalanceEquation(ISolverEquipment _eq)
        {
            eq = _eq;
        }

        public string Name => $"{EquationType} Components - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;

        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        private List<double> GetResiduals()
        {
            List<double> r = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return r;

            int ncomponents = GetComponentCount();
            if (ncomponents <= 1) return r;
            if (!CanUseComponents(ncomponents)) return r;

            for (int componentIndex = 0; componentIndex < ncomponents; componentIndex++)
            {
                int i = componentIndex;

                double componentIn = eq.Inlets.Sum(s =>
                    s.MassFlow.GetSolverValue() *
                    s.Composition.Components[i].MassFraction.GetSolverValue());

                double componentOut = eq.Outlets.Sum(s =>
                    s.MassFlow.GetSolverValue() *
                    s.Composition.Components[i].MassFraction.GetSolverValue());

                r.Add(componentIn - componentOut);
            }

            return r;
        }

        private List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return v;

            int ncomponents = GetComponentCount();
            if (ncomponents <= 1) return v;
            if (!CanUseComponents(ncomponents)) return v;

            foreach (var stream in eq.AllStreams)
            {
                v.Add(stream.MassFlow);

                for (int componentIndex = 0; componentIndex < ncomponents; componentIndex++)
                {
                    v.Add(stream.Composition.Components[componentIndex].MassFraction);
                }
            }

            return v;
        }

        private int GetComponentCount()
        {
            return eq.AllStreams
                .FirstOrDefault(s => s.Composition != null)
                ?.Composition.Components.Count ?? 0;
        }

        private bool CanUseComponents(int ncomponents)
        {
            return eq.AllStreams.All(stream =>
                stream.Composition != null &&
                stream.Composition.Components.Count == ncomponents);
        }
    }
}

