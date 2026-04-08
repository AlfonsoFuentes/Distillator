using Shared.Thermodynamics.PureComponents;

namespace Shared.Thermodynamics.Componentes
{
    public interface INode
    {
        Guid Id { get; }
        string Name { get; }
        PureComponentData PureComponentData { get; }
    }
}
