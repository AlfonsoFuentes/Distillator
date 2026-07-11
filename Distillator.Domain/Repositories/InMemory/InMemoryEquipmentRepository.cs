using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Repositories.InMemory;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly Dictionary<Guid, IVisualElement> _equipment = new();

    public Task<IVisualElement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _equipment.TryGetValue(id, out var element);
        return Task.FromResult(element);
    }

    public Task SaveAsync(IVisualElement equipment, CancellationToken cancellationToken = default)
    {
        _equipment[equipment.Id] = equipment;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _equipment.Remove(id);
        return Task.CompletedTask;
    }
}
