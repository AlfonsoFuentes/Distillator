using Distillator.Domain.Configuration;
using Distillator.Domain.Core;
using Distillator.Domain.Events;
using Distillator.Domain.Factories;
using Distillator.Domain.Policies;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;

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

        _flowsheets.Remove(fs);
        Raise(new FlowsheetRemovedEvent(Id, flowsheetId));
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
        if (equipment.Facade is IFacadeStream streamFacade && Configuration.ThermodynamicMethod != null)
        {
            streamFacade.SetThermodynamicMethod(Configuration.ThermodynamicMethod);
        }

        EquipmentRegistry.Register(equipment);
        Raise(new EquipmentAddedEvent(Id, Guid.Empty, equipment.Id, equipment.Type.ToString()));
    }

    public void RemoveEquipment(Guid equipmentId)
    {
        var equipment = EquipmentRegistry.GetById(equipmentId);
        if (equipment == null) return;

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

        SimulationService.PropagateThermodynamicMethod(this);
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
        var solver = new MainSolver();
        return new SimulationService(solver);
    }
}
