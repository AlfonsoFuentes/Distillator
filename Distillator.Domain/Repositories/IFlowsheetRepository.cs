namespace Distillator.Domain.Repositories;

public interface IFlowsheetRepository
{
    Task<Models.IFlowsheet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(Models.IFlowsheet flowsheet, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
