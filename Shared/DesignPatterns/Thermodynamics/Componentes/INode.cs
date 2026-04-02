using Shared.DesignPatterns.NewFolder;

namespace Shared.DesignPatterns.Thermodynamics
{
    public interface INode
    {
        Guid Id { get; }
        string Name { get; }
        PureComponentData PureComponentData { get; }
    }
}
