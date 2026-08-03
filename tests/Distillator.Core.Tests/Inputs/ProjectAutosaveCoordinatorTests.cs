using Distillator.Domain.Inputs;

namespace Distillator.Core.Tests.Inputs;

public sealed class ProjectAutosaveCoordinatorTests
{
    [Fact]
    public async Task SaveLatestAsync_SerializesSavesAndPersistsLatestDirtyRevision()
    {
        var coordinator = new ProjectAutosaveCoordinator<string>();
        var firstSaveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var activeSaves = 0;
        var maxActiveSaves = 0;
        var savedRevisions = new List<long>();

        coordinator.MarkDirty("first");
        var saveTask = coordinator.SaveLatestAsync(async (snapshot, _) =>
        {
            var active = Interlocked.Increment(ref activeSaves);
            maxActiveSaves = Math.Max(maxActiveSaves, active);
            savedRevisions.Add(snapshot.Revision);

            if (snapshot.Payload == "first")
            {
                firstSaveStarted.SetResult();
                await releaseFirstSave.Task;
            }

            Interlocked.Decrement(ref activeSaves);
            return AutosavePersistenceResult.Success();
        });

        await firstSaveStarted.Task;
        coordinator.MarkDirty("second");
        var concurrentSaveTask = coordinator.SaveLatestAsync((_, _) =>
            throw new InvalidOperationException("A second drain must not start while saving."));

        releaseFirstSave.SetResult();
        await Task.WhenAll(saveTask, concurrentSaveTask);

        Assert.Equal(new[] { 1L, 2L }, savedRevisions);
        Assert.Equal(1, maxActiveSaves);
        Assert.Equal(2, coordinator.CleanRevision);
        Assert.Equal(AutosaveRevisionState.Clean, coordinator.State);
    }

    [Fact]
    public async Task SaveLatestAsync_FailureLeavesLatestRevisionDirty()
    {
        var coordinator = new ProjectAutosaveCoordinator<string>();

        coordinator.MarkDirty("draft");
        await coordinator.SaveLatestAsync((_, _) =>
            Task.FromResult(AutosavePersistenceResult.Failure("HTTP failed")));

        Assert.Equal(1, coordinator.LatestRevision);
        Assert.Equal(0, coordinator.CleanRevision);
        Assert.Equal(AutosaveRevisionState.Dirty, coordinator.State);
    }

    [Fact]
    public async Task SaveLatestAsync_AfterFailureCanRetrySameDirtyRevision()
    {
        var coordinator = new ProjectAutosaveCoordinator<string>();
        var savedRevisions = new List<long>();

        coordinator.MarkDirty("draft");
        await coordinator.SaveLatestAsync((_, _) =>
            Task.FromResult(AutosavePersistenceResult.Failure("HTTP failed")));

        await coordinator.SaveLatestAsync((snapshot, _) =>
        {
            savedRevisions.Add(snapshot.Revision);
            return Task.FromResult(AutosavePersistenceResult.Success());
        });

        Assert.Equal(new[] { 1L }, savedRevisions);
        Assert.Equal(1, coordinator.CleanRevision);
        Assert.Equal(AutosaveRevisionState.Clean, coordinator.State);
    }

    [Fact]
    public async Task SaveLatestAsync_WhenPersistenceThrows_ShouldRemainDirtyForRetry()
    {
        var coordinator = new ProjectAutosaveCoordinator<string>();

        coordinator.MarkDirty("draft");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.SaveLatestAsync((_, _) =>
                throw new InvalidOperationException("Network failure")));

        Assert.Equal(1, coordinator.LatestRevision);
        Assert.Equal(0, coordinator.CleanRevision);
        Assert.Equal(AutosaveRevisionState.Dirty, coordinator.State);

        await coordinator.SaveLatestAsync((_, _) =>
            Task.FromResult(AutosavePersistenceResult.Success()));

        Assert.Equal(1, coordinator.CleanRevision);
        Assert.Equal(AutosaveRevisionState.Clean, coordinator.State);
    }

    [Fact]
    public async Task SaveLatestAsync_OlderSuccessDoesNotCleanNewerRevision()
    {
        var coordinator = new ProjectAutosaveCoordinator<string>();
        var saveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.MarkDirty("old");
        var saveTask = coordinator.SaveLatestAsync(async (_, _) =>
        {
            saveStarted.TrySetResult();
            await releaseSave.Task;
            return AutosavePersistenceResult.Success();
        });

        await saveStarted.Task;
        coordinator.MarkDirty("new");

        releaseSave.SetResult();
        await saveTask;

        Assert.Equal(2, coordinator.LatestRevision);
        Assert.Equal(2, coordinator.CleanRevision);
        Assert.Equal(AutosaveRevisionState.Clean, coordinator.State);
    }

    [Fact]
    [Trait("Spec", "05")]
    [Trait("Spec", "06")]
    public async Task SaveLatestAsync_WhenPayloadIsDiagramBatch_ShouldPersistBatchAsSingleSnapshot()
    {
        var coordinator = new ProjectAutosaveCoordinator<IReadOnlyList<Guid>>();
        var diagramA = Guid.Parse("7a0c7ad4-7855-46f7-a417-dc2ef77584ef");
        var diagramB = Guid.Parse("0fda6e43-5b3c-403c-9342-828889ea76f2");
        var savedBatches = new List<IReadOnlyList<Guid>>();

        coordinator.MarkDirty(new[] { diagramA, diagramB });
        await coordinator.SaveLatestAsync((snapshot, _) =>
        {
            savedBatches.Add(snapshot.Payload);
            return Task.FromResult(AutosavePersistenceResult.Success());
        });

        var savedBatch = Assert.Single(savedBatches);
        Assert.Equal(new[] { diagramA, diagramB }, savedBatch);
        Assert.Equal(AutosaveRevisionState.Clean, coordinator.State);
    }

    [Fact]
    [Trait("Spec", "05")]
    [Trait("Spec", "06")]
    public async Task SaveLatestAsync_WhenDiagramBatchFails_ShouldKeepWholeBatchDirty()
    {
        var coordinator = new ProjectAutosaveCoordinator<IReadOnlyList<Guid>>();
        var diagramA = Guid.Parse("cdd892f6-e738-4c9f-a757-8a8ab8c1932d");
        var diagramB = Guid.Parse("7a8ba994-3588-441a-bcf7-b74f411661a5");

        coordinator.MarkDirty(new[] { diagramA, diagramB });
        await coordinator.SaveLatestAsync((_, _) =>
            Task.FromResult(AutosavePersistenceResult.Failure("Batch failed")));

        Assert.Equal(1, coordinator.LatestRevision);
        Assert.Equal(0, coordinator.CleanRevision);
        Assert.Equal(AutosaveRevisionState.Dirty, coordinator.State);
    }
}
