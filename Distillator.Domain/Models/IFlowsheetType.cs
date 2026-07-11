using Distillator.Domain.Policies;
using Shared.ProcessFlowDiagram;
using Shared.WorkSpaceManagers;

namespace Distillator.Domain.Models;

/// <summary>
/// Tipo de diagrama (PFD, P&ID, Electrical...).
/// Extensible sin enum: cada implementación registra su código único.
/// </summary>
public interface IFlowsheetType
{
    string Code { get; }                     // "PFD", "PAndID", "Electrical"
    string DisplayName { get; }              // "Process Flow Diagram"
    bool SupportsSimulation { get; }         // ¿Tiene solver?
    IEnumerable<EquipmentType> AllowedEquipmentTypes { get; } // Tipos de equipo permitidos
    string ConnectionTypeCode { get; }       // "MaterialPipe", "Signal", "Cable"

    /// <summary>Fábrica de equipos asociada a este tipo de diagrama.</summary>
    IEquipmentFactory EquipmentFactory { get; }

    /// <summary>Reglas de conexión asociadas a este tipo de diagrama.</summary>
    IConnectionRules ConnectionRules { get; }
}

/// <summary>
/// Registro de tipos de diagrama. Permite agregar nuevos sin recompilar lógica central.
/// </summary>
public interface IFlowsheetTypeRegistry
{
    IFlowsheetType? GetByCode(string code);
    IEnumerable<IFlowsheetType> AllTypes { get; }
}
