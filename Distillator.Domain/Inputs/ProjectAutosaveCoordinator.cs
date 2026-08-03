namespace Distillator.Domain.Inputs;

public enum AutosaveRevisionState
{
    Clean,
    Dirty,
    Saving
}

public sealed record AutosaveSnapshot<TPayload>(
    long Revision,
    TPayload Payload,
    DateTimeOffset CreatedAt);

public sealed record AutosavePersistenceResult(bool Succeeded, string? ErrorMessage = null)
{
    public static AutosavePersistenceResult Success() => new(true);

    public static AutosavePersistenceResult Failure(string errorMessage) => new(false, errorMessage);
}

public sealed class ProjectAutosaveCoordinator<TPayload>
{
    private readonly object _sync = new();
    private AutosaveSnapshot<TPayload>? _pendingSnapshot;
    private Task? _drainTask;
    private long _latestRevision;
    private long _cleanRevision;
    private long _savingRevision;
    private bool _isSaving;

    public AutosaveRevisionState State
    {
        get
        {
            lock (_sync)
            {
                if (_isSaving) return AutosaveRevisionState.Saving;
                return _cleanRevision >= _latestRevision
                    ? AutosaveRevisionState.Clean
                    : AutosaveRevisionState.Dirty;
            }
        }
    }

    public long LatestRevision
    {
        get
        {
            lock (_sync)
            {
                return _latestRevision;
            }
        }
    }

    public long CleanRevision
    {
        get
        {
            lock (_sync)
            {
                return _cleanRevision;
            }
        }
    }

    public long? SavingRevision
    {
        get
        {
            lock (_sync)
            {
                return _isSaving ? _savingRevision : null;
            }
        }
    }

    public AutosaveSnapshot<TPayload> MarkDirty(TPayload payload, DateTimeOffset? createdAt = null)
    {
        lock (_sync)
        {
            var snapshot = new AutosaveSnapshot<TPayload>(
                ++_latestRevision,
                payload,
                createdAt ?? DateTimeOffset.UtcNow);
            _pendingSnapshot = snapshot;
            return snapshot;
        }
    }

    public Task SaveLatestAsync(
        Func<AutosaveSnapshot<TPayload>, CancellationToken, Task<AutosavePersistenceResult>> persistAsync,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_drainTask is { IsCompleted: false })
            {
                return _drainTask;
            }

            _drainTask = DrainAsync(persistAsync, cancellationToken);
            return _drainTask;
        }
    }

    private async Task DrainAsync(
        Func<AutosaveSnapshot<TPayload>, CancellationToken, Task<AutosavePersistenceResult>> persistAsync,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            AutosaveSnapshot<TPayload>? snapshot;
            lock (_sync)
            {
                snapshot = _pendingSnapshot;
                if (snapshot == null || snapshot.Revision <= _cleanRevision)
                {
                    _isSaving = false;
                    _savingRevision = 0;
                    _drainTask = null;
                    return;
                }

                _isSaving = true;
                _savingRevision = snapshot.Revision;
            }

            AutosavePersistenceResult result;
            try
            {
                result = await persistAsync(snapshot, cancellationToken);
            }
            catch
            {
                lock (_sync)
                {
                    _isSaving = false;
                    _savingRevision = 0;
                    _drainTask = null;
                }

                throw;
            }

            lock (_sync)
            {
                _isSaving = false;
                _savingRevision = 0;

                if (!result.Succeeded)
                {
                    _drainTask = null;
                    return;
                }

                if (snapshot.Revision > _cleanRevision)
                {
                    _cleanRevision = snapshot.Revision;
                }
            }
        }
    }
}
