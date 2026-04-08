using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pumps;
using Shared.ProcessFlowDiagram.Streams;

namespace Client.Services.EquipmentManagers
{
    public interface INamingService
    {
        string GenerateNextName(string prefix);
    }

    public class EquipmentNamingService : INamingService
    {
        private readonly Dictionary<string, int> _counters = new();

        public string GenerateNextName(string prefix)
        {
            if (!_counters.ContainsKey(prefix)) _counters[prefix] = 101;
            return $"{prefix}-{_counters[prefix]++}";
        }
    }
    public interface IEquipmentFactory
    {
        IVisualElement? Create(string type, double x, double y, Func<double, double> snap);
        void Register(string type, Func<IVisualElement> factory);
    }

    public class EquipmentFactory : IEquipmentFactory
    {
        private readonly INamingService _naming;
        private readonly Dictionary<string, Func<IVisualElement>> _registry = new();

        public EquipmentFactory(INamingService naming)
        {
            _naming = naming;
            // Registro inicial
            Register("CentrifugalPump", () => new PumpVisualElement());
            Register("MaterialStream", () => new StreamVisualElement());
        }

        public void Register(string type, Func<IVisualElement> factory) => _registry[type] = factory;

        public IVisualElement? Create(string type, double x, double y, Func<double, double> snap)
        {
            if (!_registry.TryGetValue(type, out var factory)) return null;

            var element = factory();
            element.SetDropPosition(x, y, snap);

            var name = _naming.GenerateNextName(element.Prefix);
            element.Facade.Name = name;
            element.Label = name;

            return element;
        }
    }
   
}
