using Distillator.Domain.Configuration;
using Distillator.Domain.Core;
using Distillator.Domain.Events;
using Distillator.Domain.Factories;
using Distillator.Domain.Policies;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverConsecutive.SolverRemanufactured;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;

namespace Distillator.Domain.Models;

public class Project : IProject
{
    private readonly List<IFlowsheet> _flowsheets = new();
    private readonly List<ISimulationSnapshot> _snapshots = new();
    private readonly List<IInterFlowsheetConnection> _interFlowsheetConnections = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; }
    public string Name { get; set; }
    public Guid OwnerUserId => Owner.Id;
    public DateTime CreatedAt { get; }
    public IUser Owner { get; }
    public IProjectConfiguration Configuration { get; private set; }
    public IEquipmentRegistry EquipmentRegistry { get; }
    public IFlowsheetTypeRegistry FlowsheetTypes { get; }
    public IReadOnlyCollection<IFlowsheet> Flowsheets => _flowsheets.AsReadOnly();
    public IReadOnlyCollection<ISimulationSnapshot> SimulationSnapshots => _snapshots.AsReadOnly();
    public IReadOnlyCollection<IInterFlowsheetConnection> InterFlowsheetConnections => _interFlowsheetConnections.AsReadOnly();
    public ISimulationService SimulationService { get; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Project(
        string name,
        IUser owner,
        IProjectConfiguration? configuration = null,
        IFlowsheetTypeRegistry? flowsheetTypes = null,
        ISimulationService? simulationService = null,
        Guid? id = null,
        DateTime? createdAt = null)
    {
        Id = id ?? Guid.NewGuid();
        Name = name;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Configuration = configuration ?? new ProjectConfiguration();
        FlowsheetTypes = flowsheetTypes ?? new FlowsheetTypeRegistry();
        EquipmentRegistry = new EquipmentRegistry();
        SimulationService = simulationService ?? CreateDefaultSimulationService();

        Raise(new ProjectCreatedEvent(Id, Name));
    }

    public void UpdateConfiguration(IProjectConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        ProjectUnitSystemApplier.ApplyToProject(this);
        SimulationService.ApplyProjectConfiguration(this);
        Raise(new ProjectConfigurationUpdatedEvent(Id));
    }

    public IFlowsheet CreateFlowsheet(string name, string flowsheetTypeCode, Guid? id = null)
    {
        var factory = new FlowsheetFactory();
        var flowsheet = factory.Create(name, flowsheetTypeCode, this, id);
        _flowsheets.Add(flowsheet);
        Raise(new FlowsheetAddedEvent(Id, flowsheet.Id, flowsheet.Name, flowsheet.TypeCode));
        return flowsheet;
    }

    public void RemoveFlowsheet(Guid flowsheetId)
    {
        var fs = _flowsheets.FirstOrDefault(f => f.Id == flowsheetId);
        if (fs == null) return;

        var interFlowsheetConnections = _interFlowsheetConnections
            .Where(connection =>
                connection.SourceFlowsheetId == flowsheetId ||
                connection.TargetFlowsheetId == flowsheetId)
            .ToList();

        foreach (var connection in interFlowsheetConnections)
        {
            RemoveInterFlowsheetConnectionArtifacts(connection);
        }

        foreach (var elementId in fs.Elements.Select(reference => reference.ElementId).Distinct().ToList())
        {
            RemoveEquipment(elementId);
        }

        _flowsheets.Remove(fs);
        Raise(new FlowsheetRemovedEvent(Id, flowsheetId));
    }

    private void RemoveInterFlowsheetConnectionArtifacts(IInterFlowsheetConnection connection)
    {
        var sourceFlowsheet = GetFlowsheet(connection.SourceFlowsheetId);
        var targetFlowsheet = GetFlowsheet(connection.TargetFlowsheetId);

        if (!TryDisconnectConnector(sourceFlowsheet, connection.SourceConnectorId))
        {
            TryDisconnectConnector(targetFlowsheet, connection.TargetConnectorId);
        }

        RemoveConnectorArtifacts(sourceFlowsheet, connection.SourceConnectorId);
        RemoveConnectorArtifacts(targetFlowsheet, connection.TargetConnectorId);
        RemoveEquipment(connection.SourceConnectorId);
        RemoveEquipment(connection.TargetConnectorId);
        RemoveInterFlowsheetConnection(connection.Id);
    }

    private bool TryDisconnectConnector(IFlowsheet? flowsheet, Guid connectorId)
    {
        if (flowsheet == null) return false;

        var pipe = flowsheet.Pipes.FirstOrDefault(candidate =>
            candidate.SourceElementId == connectorId || candidate.TargetElementId == connectorId);
        if (pipe == null) return false;

        var endpointId = pipe.SourceElementId == connectorId
            ? pipe.TargetElementId
            : pipe.SourceElementId;
        var endpointPortName = pipe.SourceElementId == connectorId
            ? pipe.TargetPortName
            : pipe.SourcePortName;
        var endpoint = EquipmentRegistry.GetById(endpointId);
        if (endpoint == null) return false;

        SimulationService.DisconnectPort(this, flowsheet, endpoint, endpointPortName);
        return true;
    }

    private static void RemoveConnectorArtifacts(IFlowsheet? flowsheet, Guid connectorId)
    {
        if (flowsheet == null) return;

        foreach (var pipe in flowsheet.Pipes
                     .Where(candidate =>
                         candidate.SourceElementId == connectorId ||
                         candidate.TargetElementId == connectorId)
                     .ToList())
        {
            flowsheet.RemovePipe(pipe.Id);
        }

        flowsheet.RemoveElementReference(connectorId);
    }

    public IFlowsheet? GetFlowsheet(Guid id) => _flowsheets.FirstOrDefault(f => f.Id == id);

    public void ReorderFlowsheet(IFlowsheet flowsheet, int newIndex)
    {
        if (flowsheet == null) throw new ArgumentNullException(nameof(flowsheet));
        var currentIndex = _flowsheets.IndexOf(flowsheet);
        if (currentIndex < 0) return;

        newIndex = Math.Clamp(newIndex, 0, _flowsheets.Count - 1);
        _flowsheets.RemoveAt(currentIndex);
        _flowsheets.Insert(newIndex, flowsheet);
    }

    public void AddEquipment(IVisualElement equipment)
    {
        if (equipment == null) throw new ArgumentNullException(nameof(equipment));

        // Asignar el método termodinámico del proyecto a las nuevas corrientes.
        if (Configuration.ThermodynamicMethod != null)
        {
            SimulationService.Solver.ThermoMethod = Configuration.ThermodynamicMethod;
            if (equipment.Facade is IFacadeStream streamFacade)
            {
                streamFacade.SetThermodynamicMethod(Configuration.ThermodynamicMethod);
            }
        }

        ProjectUnitSystemApplier.ApplyToFacade(equipment.Facade, Configuration.UnitDefaults);
        EquipmentRegistry.Register(equipment);
        Raise(new EquipmentAddedEvent(Id, Guid.Empty, equipment.Id, equipment.Type.ToString()));
    }

    public void RemoveEquipment(Guid equipmentId)
    {
        var equipment = EquipmentRegistry.GetById(equipmentId);
        if (equipment == null) return;

        if (equipment.Facade is IFacadeStream stream)
        {
            SimulationService.Solver.RemoveStream(stream);
        }
        else if (equipment.Facade is ISolverEquipment solverEquipment)
        {
            SimulationService.Solver.RemoveEquipment(solverEquipment);
        }

        EquipmentRegistry.Unregister(equipmentId);
        Raise(new EquipmentRemovedEvent(Id, Guid.Empty, equipmentId));
    }

    public IVisualElement? GetEquipment(Guid id) => EquipmentRegistry.GetById(id);

    public void UpdateThermodynamicMethod(Guid thermodynamicMethodId, ThermodynamicMethodFullDto? thermodynamicMethod = null)
    {
        Configuration = new ProjectConfiguration(
            unitDefaults: Configuration.UnitDefaults,
            unitSystems: Configuration.UnitSystems,
            activeUnitSystemName: Configuration.ActiveUnitSystemName,
            cameraDefaults: Configuration.CameraDefaults,
            namingConfig: Configuration.NamingConfig,
            thermodynamicMethodId: thermodynamicMethodId,
            thermodynamicMethod: thermodynamicMethod,
            reportConfig: Configuration.ReportConfig,
            equipmentDesignConfig: Configuration.EquipmentDesignConfig,
            plantElevation: Configuration.PlantElevation);

        SimulationService.ApplyProjectConfiguration(this);
        ProjectUnitSystemApplier.ApplyToProject(this);
    }

    public void AddInterFlowsheetConnection(IInterFlowsheetConnection connection)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        _interFlowsheetConnections.Add(connection);
    }

    public void RemoveInterFlowsheetConnection(Guid connectionId)
    {
        var connection = _interFlowsheetConnections.FirstOrDefault(c => c.Id == connectionId);
        if (connection != null) _interFlowsheetConnections.Remove(connection);
    }

    public void RunSimulation()
    {
        SimulationService.RunSimulation(this);
    }

    public Task<SimulationRunResult> RunSimulationAsync()
    {
        return SimulationService.RunSimulationAsync(this);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    private static ISimulationService CreateDefaultSimulationService()
    {
        var solver = new MainSolverRemanufactured();
        return new SimulationService(solver);
    }
}
