using Distillator.Domain.Events;
using Distillator.Domain.Configuration;
using Distillator.Domain.Inputs;
using Distillator.Domain.Models;
using Distillator.Domain.Services;
using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using UnitSystem;

namespace Distillator.Core.Tests.Simulation;

public sealed class SimulationServiceTests
{
    [Fact]
    [Trait("Spec", "09")]
    [Trait("Spec", "12")]
    [Trait("Level", "Unit")]
    public void ApplyProjectConfiguration_ShouldUpdateSolverAltitudeAndAtmosphericPressure()
    {
        var solver = new MainSolver();
        var service = new SimulationService(solver);
        var project = CreateProject(service);
        var elevation = new Length(1500, LengthUnits.Meter);

        project.UpdateConfiguration(new ProjectConfiguration(plantElevation: elevation));

        Assert.Equal(1500, solver.Altitude.GetValue(LengthUnits.Meter), 6);
        Assert.True(solver.AtmosphericPressure.GetValue(PressureUnits.Pascala) < 101325);
    }

    [Fact]
    [Trait("Spec", "51")]
    [Trait("Level", "Unit")]
    public void ApplyProjectConfiguration_WhenElevationChanges_ShouldUpdateGaugePressureConversionReference()
    {
        UnitManager.ResetAtmosphericPressureReference();
        var solver = new MainSolver();
        var service = new SimulationService(solver);
        var project = CreateProject(service);
        var pressure = new Pressure(25, PressureUnits.Psig);

        project.UpdateConfiguration(new ProjectConfiguration(
            plantElevation: new Length(0, LengthUnits.Meter)));
        var seaLevelAbsolutePsia = pressure.GetValue(PressureUnits.Psia);

        project.UpdateConfiguration(new ProjectConfiguration(
            plantElevation: new Length(1500, LengthUnits.Meter)));
        var elevatedAbsolutePsia = pressure.GetValue(PressureUnits.Psia);

        Assert.Equal(39.6959, seaLevelAbsolutePsia, 3);
        Assert.True(elevatedAbsolutePsia < seaLevelAbsolutePsia);
        Assert.True(UnitManager.GetAtmosphericPressureReference().GetValue(PressureUnits.Psia) < 14.7);
    }

    [Fact]
    [Trait("Spec", "51")]
    [Trait("Level", "Unit")]
    public void MainSolverConstructor_ShouldNotResetActiveAtmosphericPressureReference()
    {
        UnitManager.ResetAtmosphericPressureReference();
        var solver = new MainSolver();
        var service = new SimulationService(solver);
        var project = CreateProject(service);
        var pressure = new Pressure(25, PressureUnits.Psig);

        project.UpdateConfiguration(new ProjectConfiguration(
            plantElevation: new Length(1500, LengthUnits.Meter)));
        var elevatedAbsolutePsia = pressure.GetValue(PressureUnits.Psia);

        _ = new MainSolver();

        Assert.Equal(elevatedAbsolutePsia, pressure.GetValue(PressureUnits.Psia), 6);
    }

    [Fact]
    [Trait("Spec", "09")]
    [Trait("Spec", "12")]
    [Trait("Level", "Unit")]
    public void ApplyProjectConfiguration_ShouldPropagateThermodynamicMethodToProjectSolverStreams()
    {
        var solver = new MainSolver();
        var stream = new FacadeStream("S-101");
        solver.AddStream(stream);
        var service = new SimulationService(solver);
        var project = CreateProject(service);
        var method = CreateThermodynamicMethod();

        project.UpdateConfiguration(new ProjectConfiguration(
            thermodynamicMethodId: method.Id,
            thermodynamicMethod: method));

        Assert.Same(method, solver.ThermoMethod);
        Assert.Same(method, stream.ThermoMethod);
    }

    [Fact]
    [Trait("Spec", "01")]
    [Trait("Spec", "03")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenSolverPostSolveIsRunning_ShouldPublishCompletionAfterSolverFinishes()
    {
        var solver = new MainSolver();
        var equipment = new BlockingPostSolveEquipment();
        solver.AddEquipment(equipment);

        var service = new SimulationService(solver);
        var project = CreateProject(service);

        var simulation = service.RunSimulationAsync(project);
        await equipment.WaitUntilPostSolveStartedAsync();

        Assert.Contains(service.RecentEvents, domainEvent => domainEvent is SimulationStartedEvent);
        Assert.DoesNotContain(service.RecentEvents, domainEvent => domainEvent is SimulationCompletedEvent);
        Assert.Null(service.LastSimulationResult);

        equipment.ReleasePostSolve();
        var result = await simulation.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(SimulationRunStatus.Completed, result.Status);
        Assert.Same(result, service.LastSimulationResult);
        Assert.Contains(service.RecentEvents, domainEvent => domainEvent is SimulationCompletedEvent);
    }

    [Fact]
    [Trait("Spec", "01")]
    [Trait("Spec", "03")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenSolverFails_ShouldPublishFailureAndReleaseObservableState()
    {
        var solver = new MainSolver();
        solver.AddEquipment(new ThrowingPostSolveEquipment());

        var service = new SimulationService(solver);
        var project = CreateProject(service);

        var result = await service.RunSimulationAsync(project);

        Assert.Equal(SimulationRunStatus.Failed, result.Status);
        Assert.Same(result, service.LastSimulationResult);
        Assert.NotNull(service.LastError);
        Assert.Contains(service.RecentEvents, domainEvent => domainEvent is SimulationFailedEvent);
        Assert.Contains(service.RecentEvents, domainEvent => domainEvent is SimulationCompletedEvent);
    }

    [Fact]
    [Trait("Spec", "02")]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    public async Task RunSimulationAsync_WhenSolverFails_ShouldKeepValidatedInput()
    {
        var solver = new MainSolver();
        solver.AddEquipment(new ThrowingPostSolveEquipment());

        var service = new SimulationService(solver);
        var project = CreateProject(service);
        var temperature = new Variable<Temperature>(
            new Temperature(298.15, TemperatureUnits.Kelvin),
            TemperatureUnits.DegreeCelcius,
            25);
        var inputResult = new VariableInputCommandHandler().Apply(
            new SetVariableInputCommand<Temperature>(
                temperature,
                55,
                TemperatureUnits.DegreeCelcius,
                "user-1",
                "Alfonso"));

        var simulationResult = await service.RunSimulationAsync(project);

        Assert.Equal(VariableInputCommandStatus.Applied, inputResult.Status);
        Assert.Equal(SimulationRunStatus.Failed, simulationResult.Status);
        Assert.True(temperature.IsDefinedByUI);
        Assert.Equal(55, temperature.GetDisplayValue(), 6);
        Assert.Equal("user-1", temperature.DefinedByUserId);
        Assert.NotNull(service.LastError);
    }

    private static Project CreateProject(ISimulationService simulationService)
    {
        var owner = new User(Guid.Parse("e90df25b-b57f-49d5-9dfa-71978799814e"), "test@example.com", "Test", "User", false);
        return new Project("Simulation service test", owner, simulationService: simulationService);
    }

    private static ThermodynamicMethodFullDto CreateThermodynamicMethod()
    {
        return new ThermodynamicMethodFullDto
        {
            Id = Guid.Parse("6a28cb5e-6c6d-4a67-9070-03d1cc1449a1"),
            Name = "M51 Ethanol Water",
            LiquidModel = LiquidPhaseModel.IdealLiquid,
            VaporModel = VaporPhaseModel.IdealGas,
            Components =
            [
                CreateComponent(Guid.Parse("b148ce7e-a0c2-439a-aa17-e916324bce61"), "Ethanol", "C2H6O", 46.07, 0),
                CreateComponent(Guid.Parse("b595e4f2-54db-4058-a9c2-d36b7f88962c"), "Water", "H2O", 18.015, 1)
            ]
        };
    }

    private static MethodComponentFullDto CreateComponent(Guid id, string name, string formula, double molecularWeight, int matrixIndex)
    {
        return new MethodComponentFullDto
        {
            ComponentId = id,
            ComponentName = name,
            MatrixIndex = matrixIndex,
            FullData = new ChemicalComponentDto
            {
                Id = id,
                Name = name,
                Formula = formula,
                MolecularWeight = molecularWeight,
                CriticalTemperature = new Temperature(500, TemperatureUnits.Kelvin),
                CriticalPressure = new Pressure(50, PressureUnits.Bara),
                BoilingPoint = new Temperature(350, TemperatureUnits.Kelvin),
                MeltingPoint = new Temperature(250, TemperatureUnits.Kelvin)
            }
        };
    }

    private sealed class BlockingPostSolveEquipment : SolverEquipmentBase
    {
        private readonly TaskCompletionSource _postSolveStarted = NewTaskCompletionSource();
        private readonly TaskCompletionSource _postSolveReleased = NewTaskCompletionSource();

        public override List<ISolverEquation> Equations { get; } = [];

        public override async Task PostSolveAsync()
        {
            _postSolveStarted.TrySetResult();
            await _postSolveReleased.Task;
        }

        public Task WaitUntilPostSolveStartedAsync()
        {
            return _postSolveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        public void ReleasePostSolve()
        {
            _postSolveReleased.TrySetResult();
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
            throw new InvalidOperationException("Controlled simulation service failure");
        }
    }
}
