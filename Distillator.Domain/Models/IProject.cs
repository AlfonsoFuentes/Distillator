using Distillator.Domain.Configuration;
using Distillator.Domain.Core;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram;
using Shared.PropertiesDtos.Methods;

namespace Distillator.Domain.Models;

/// <summary>
/// Proyecto: contenedor raíz de un proceso químico.
/// Un usuario tiene N proyectos. Cada proyecto tiene configuración, registry de equipos,
/// N flowsheets (diagramas), y snapshots de simulación.
/// </summary>
public interface IProject
{
    Guid Id { get; }
    string Name { get; set; }
    Guid OwnerUserId { get; }
    DateTime CreatedAt { get; }

    /// <summary>Referencia ascendente al usuario propietario.</summary>
    IUser Owner { get; }

    /// <summary>Configuración global del proyecto (unidades, naming, método termo, reportes).</summary>
    IProjectConfiguration Configuration { get; }

    /// <summary>Catálogo global de equipos del proyecto (todos los flowsheets comparten).</summary>
    IEquipmentRegistry EquipmentRegistry { get; }

    /// <summary>Registro de tipos de diagrama disponibles.</summary>
    IFlowsheetTypeRegistry FlowsheetTypes { get; }

    /// <summary>Diagramas del proyecto (PFD, P&ID, Electrical...).</summary>
    IReadOnlyCollection<IFlowsheet> Flowsheets { get; }

    /// <summary>Historial de ejecuciones del solver.</summary>
    IReadOnlyCollection<ISimulationSnapshot> SimulationSnapshots { get; }

    /// <summary>Conexiones entre flowsheets.</summary>
    IReadOnlyCollection<IInterFlowsheetConnection> InterFlowsheetConnections { get; }

    /// <summary>Servicio de simulación del proyecto.</summary>
    ISimulationService SimulationService { get; }

    /// <summary>Eventos de dominio recientes del proyecto.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    // CRUD Flowsheets
    IFlowsheet CreateFlowsheet(string name, string flowsheetTypeCode);
    void RemoveFlowsheet(Guid flowsheetId);
    IFlowsheet? GetFlowsheet(Guid id);
    void ReorderFlowsheet(IFlowsheet flowsheet, int newIndex);

    // Equipos
    void AddEquipment(IVisualElement equipment);
    void RemoveEquipment(Guid equipmentId);
    IVisualElement? GetEquipment(Guid id);

    // Configuración
    void UpdateConfiguration(IProjectConfiguration newConfig);
    void UpdateThermodynamicMethod(Guid thermodynamicMethodId, ThermodynamicMethodFullDto? thermodynamicMethod = null);

    // Conexiones inter-flowsheet
    void AddInterFlowsheetConnection(IInterFlowsheetConnection connection);
    void RemoveInterFlowsheetConnection(Guid connectionId);

    // Simulación
    void RunSimulation();
    void ClearDomainEvents();
}

public interface ISimulationSnapshot
{
    Guid Id { get; }
    Guid ProjectId { get; }
    DateTime CreatedAt { get; }
    bool Converged { get; }
    TimeSpan ExecutionTime { get; }
    string ResultsJson { get; } // JSONB
}
