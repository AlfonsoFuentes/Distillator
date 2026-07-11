using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Repositories;

public interface IEquipmentRepository
{
    Task<IVisualElement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(IVisualElement equipment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
