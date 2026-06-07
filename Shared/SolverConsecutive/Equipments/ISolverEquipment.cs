using Shared.SolverQwen.Variables;

namespace Shared.SolverConsecutive.Equipments
{
    public interface ISolverEquipment
    {
        Guid Id { get; }
        string Name { get; }
        List<ISolverEquation> Equations { get; }
        IReadOnlyList<Specification> Specifications { get; }
    }


    public abstract class SolverEquipmentBase : ISolverEquipment
    {
        public abstract string Name { get; }
        public abstract List<ISolverEquation> Equations { get; }
        public Guid Id { get; set; } = Guid.NewGuid();

        private readonly List<Specification> _specifications = new();
        public IReadOnlyList<Specification> Specifications => _specifications.AsReadOnly();

        public void AddSpec(Specification spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (_specifications.Any(s => s.Id == spec.Id))
                throw new InvalidOperationException($"Spec {spec.Id} ya existe en {Name}");

            _specifications.Add(spec);
        }

        public void RemoveSpec(Specification spec)
        {
            if (spec != null) _specifications.Remove(spec);
        }

        public void ClearSpecs()
        {
            _specifications.Clear();
        }
    }
}
