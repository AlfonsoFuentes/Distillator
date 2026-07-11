namespace Distillator.Domain.Repositories.InMemory;

public class InMemoryFlowsheetRepository : IFlowsheetRepository
{
    private readonly Dictionary<Guid, Models.IFlowsheet> _flowsheets = new();

    public Task<Models.IFlowsheet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _flowsheets.TryGetValue(id, out var flowsheet);
        return Task.FromResult(flowsheet);
    }

    public Task SaveAsync(Models.IFlowsheet flowsheet, CancellationToken cancellationToken = default)
    {
        _flowsheets[flowsheet.Id] = flowsheet;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _flowsheets.Remove(id);
        return Task.CompletedTask;
    }
}
