using Shared.ProcessFlowDiagram;
using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Basiss;

namespace Shared.SolverConsecutive.Equipments
{
    public interface ISolverEquipment :IFacade
    {
       
        List<ISolverEquation> Equations { get; }
        IReadOnlyList<ISpecification> Specifications { get; }
    }


    public abstract class SolverEquipmentBase : ISolverEquipment , IEquipmentFacade
    {
        public string Name { get; set; } = string.Empty;
        public abstract List<ISolverEquation> Equations { get; }
        public Guid Id { get; set; } = Guid.NewGuid();

        private readonly List<ISpecification> _specifications = new();
        public IReadOnlyList<ISpecification> Specifications => _specifications.AsReadOnly();

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
}
