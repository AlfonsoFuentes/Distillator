using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Simlations;

namespace Distillator.Core.Tests.Solver;

public sealed class MainSolverLifecycleTests
{
    [Fact]
    [Trait("Spec", "01")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenPostSolveIsBlocked_ShouldCompleteAfterPostSolveFinishes()
    {
        var solver = new MainSolver();
        var equipment = new BlockingPostSolveEquipment();
        solver.AddEquipment(equipment);

        var simulationTask = solver.RunSimulationAsync();
        await equipment.WaitUntilPostSolveStartedAsync();

        Assert.False(simulationTask.IsCompleted);

        equipment.ReleasePostSolve();
        await simulationTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(simulationTask.IsCompletedSuccessfully);
        Assert.Equal(1, equipment.PostSolveCallCount);
    }

    [Fact]
    [Trait("Spec", "01")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenCalledWhileRunning_ShouldNotOverlapExecutions()
    {
        var solver = new MainSolver();
        var equipment = new BlockingPostSolveEquipment();
        solver.AddEquipment(equipment);

        var firstSimulation = solver.RunSimulationAsync();
        await equipment.WaitUntilPostSolveStartedAsync();

        var secondSimulation = solver.RunSimulationAsync();
        await Task.Delay(100);

        Assert.Equal(1, equipment.PostSolveCallCount);
        Assert.Equal(1, equipment.MaxConcurrentPostSolveCount);
        Assert.False(secondSimulation.IsCompleted);

        equipment.ReleasePostSolve();
        await firstSimulation.WaitAsync(TimeSpan.FromSeconds(5));
        await equipment.WaitUntilPostSolveStartedAsync(expectedCallCount: 2);

        Assert.Equal(2, equipment.PostSolveCallCount);
        Assert.Equal(1, equipment.MaxConcurrentPostSolveCount);

        equipment.ReleasePostSolve();
        await secondSimulation.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("Spec", "01")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenManyRequestsArriveWhileRunning_ShouldRunOnlyOneAdditionalSimulation()
    {
        var solver = new MainSolver();
        var equipment = new BlockingPostSolveEquipment();
        solver.AddEquipment(equipment);

        var firstSimulation = solver.RunSimulationAsync();
        await equipment.WaitUntilPostSolveStartedAsync();

        var queuedSimulations = Enumerable
            .Range(0, 10)
            .Select(_ => solver.RunSimulationAsync())
            .ToArray();

        await Task.Delay(100);

        Assert.Equal(1, equipment.PostSolveCallCount);

        equipment.ReleasePostSolve();
        await firstSimulation.WaitAsync(TimeSpan.FromSeconds(5));
        await equipment.WaitUntilPostSolveStartedAsync(expectedCallCount: 2);

        Assert.Equal(2, equipment.PostSolveCallCount);

        equipment.ReleasePostSolve();
        await Task.WhenAll(queuedSimulations).WaitAsync(TimeSpan.FromSeconds(5));

        await Task.Delay(100);

        Assert.Equal(2, equipment.PostSolveCallCount);
        Assert.Equal(1, equipment.MaxConcurrentPostSolveCount);
    }

    [Fact]
    [Trait("Spec", "01")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenEquationDoesNotConverge_ShouldCompleteWithConvergedFalse()
    {
        var solver = new MainSolver(new FixedNewtonSolver(converged: false));
        solver.AddEquipment(new EquationEquipment(new TestEquation()));

        var result = await solver.RunSimulationAsync();

        Assert.Equal(SimulationRunStatus.Completed, result.Status);
        Assert.False(result.Converged);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Contains("Convergencia incompleta"));
    }

    [Fact]
    [Trait("Spec", "01")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenEquationHasNoAdjustableVariablesAndResidualIsSolved_ShouldNotCallNewton()
    {
        var newton = new FixedNewtonSolver(converged: false);
        var solver = new MainSolver(newton);
        solver.AddEquipment(new EquationEquipment(new TestEquation(residual: 1e-8)));

        var result = await solver.RunSimulationAsync();

        Assert.Equal(SimulationRunStatus.Completed, result.Status);
        Assert.True(result.Converged);
        Assert.Equal(0, newton.CallCount);
    }

    [Fact]
    [Trait("Spec", "01")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenEquationHasNoAdjustableVariablesAndResidualIsOpen_ShouldNotCallNewton()
    {
        var newton = new FixedNewtonSolver(converged: true);
        var solver = new MainSolver(newton);
        solver.AddEquipment(new EquationEquipment(new TestEquation(residual: 1.0)));

        var result = await solver.RunSimulationAsync();

        Assert.Equal(SimulationRunStatus.Completed, result.Status);
        Assert.False(result.Converged);
        Assert.Equal(0, newton.CallCount);
    }


    [Fact]
    [Trait("Spec", "01")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenPostSolveThrows_ShouldCompleteWithFailedResult()
    {
        var solver = new MainSolver();
        solver.AddEquipment(new ThrowingPostSolveEquipment());

        var result = await solver.RunSimulationAsync();

        Assert.Equal(SimulationRunStatus.Failed, result.Status);
        Assert.False(result.Converged);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Contains("post-calculos"));
    }

    [Fact]
    [Trait("Spec", "01")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenNewRequestArrivesBeforeCompletion_ShouldMarkFirstResultAsSuperseded()
    {
        var solver = new MainSolver();
        var equipment = new BlockingPostSolveEquipment();
        solver.AddEquipment(equipment);

        var firstSimulation = solver.RunSimulationAsync();
        await equipment.WaitUntilPostSolveStartedAsync();

        var latestSimulation = solver.RunSimulationAsync();

        equipment.ReleasePostSolve();
        var firstResult = await firstSimulation.WaitAsync(TimeSpan.FromSeconds(5));
        await equipment.WaitUntilPostSolveStartedAsync(expectedCallCount: 2);

        equipment.ReleasePostSolve();
        var latestResult = await latestSimulation.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(SimulationRunStatus.Superseded, firstResult.Status);
        Assert.Equal(SimulationRunStatus.Completed, latestResult.Status);
    }

    private sealed class BlockingPostSolveEquipment : SolverEquipmentBase
    {
        private readonly object _syncRoot = new();
        private TaskCompletionSource _postSolveStarted = NewTaskCompletionSource();
        private TaskCompletionSource _postSolveReleased = NewTaskCompletionSource();

        public override List<ISolverEquation> Equations { get; } = [];

        public int PostSolveCallCount { get; private set; }
        public int MaxConcurrentPostSolveCount { get; private set; }

        private int ActivePostSolveCount { get; set; }

        public override async Task PostSolveAsync()
        {
            Task releaseTask;

            lock (_syncRoot)
            {
                PostSolveCallCount++;
                ActivePostSolveCount++;
                MaxConcurrentPostSolveCount = Math.Max(MaxConcurrentPostSolveCount, ActivePostSolveCount);
                releaseTask = _postSolveReleased.Task;
                _postSolveStarted.TrySetResult();
            }

            try
            {
                await releaseTask;
            }
            finally
            {
                lock (_syncRoot)
                {
                    ActivePostSolveCount--;
                }
            }
        }

        public async Task WaitUntilPostSolveStartedAsync(int expectedCallCount = 1)
        {
            while (true)
            {
                Task waitTask;
                lock (_syncRoot)
                {
                    if (PostSolveCallCount >= expectedCallCount)
                    {
                        return;
                    }

                    waitTask = _postSolveStarted.Task;
                }

                await waitTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }

        public void ReleasePostSolve()
        {
            lock (_syncRoot)
            {
                _postSolveReleased.TrySetResult();
                _postSolveStarted = NewTaskCompletionSource();
                _postSolveReleased = NewTaskCompletionSource();
            }
        }

        private static TaskCompletionSource NewTaskCompletionSource()
        {
            return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    private sealed class ThrowingPostSolveEquipment : SolverEquipmentBase
    {
        public override List<ISolverEquation> Equations { get; } = [];

        public override Task PostSolveAsync()
        {
            throw new InvalidOperationException("Controlled post solve failure");
        }
    }

    private sealed class EquationEquipment : SolverEquipmentBase
    {
        public EquationEquipment(ISolverEquation equation)
        {
            Equations = [equation];
        }

        public override List<ISolverEquation> Equations { get; }
    }

    private sealed class TestEquation : ISolverEquation
    {
        private readonly double _residual;

        public TestEquation(double residual = 1.0)
        {
            _residual = residual;
        }

        public string Name => "Test equation";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => [_residual];
        public List<IVariable> Variables => [];
        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Regular;
    }

    private sealed class FixedNewtonSolver : INewtonSolver
    {
        private readonly bool _converged;

        public FixedNewtonSolver(bool converged)
        {
            _converged = converged;
        }

        public int CallCount { get; private set; }

        public void Subscribe(INewtonSolverObserver observer)
        {
        }

        public SolverResult Solve(ISolverEquation mainSolver, double _alpha = 1.0)
        {
            CallCount++;
            return new SolverResult(_converged, iterations: 1, finalError: _converged ? 0 : 1);
        }
    }
}
