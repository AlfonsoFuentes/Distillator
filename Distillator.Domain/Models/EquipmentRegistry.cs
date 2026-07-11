using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Models
{
    public class EquipmentRegistry : IEquipmentRegistry
    {
        private readonly Dictionary<Guid, IVisualElement> _byId = new();
        private readonly Dictionary<string, IVisualElement> _byName = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<IVisualElement> AllEquipments => _byId.Values.ToList().AsReadOnly();

        public IVisualElement? GetById(Guid id) => _byId.GetValueOrDefault(id);

        public IVisualElement? GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return _byName.GetValueOrDefault(name);
        }

        public void Register(IVisualElement equipment)
        {
            if (equipment == null) throw new ArgumentNullException(nameof(equipment));
            _byId[equipment.Id] = equipment;
            _byName[equipment.Name] = equipment;
        }

        public void Unregister(Guid id)
        {
            if (_byId.TryGetValue(id, out var eq) && eq != null)
            {
                _byId.Remove(id);
                if (_byName.TryGetValue(eq.Name, out var namedEq) && namedEq != null && namedEq.Id == id)
                {
                    _byName.Remove(eq.Name);
                }
            }
        }

        public IEnumerable<IVisualElement> GetByType(EquipmentType type)
        {
            return _byId.Values.Where(e => e.Type == type).ToList();
        }

        public IEnumerable<IVisualElement> GetByPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return Array.Empty<IVisualElement>();
            return _byName
                .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Value)
                .ToList();
        }
    }
}
