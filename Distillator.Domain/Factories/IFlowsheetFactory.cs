using Distillator.Domain.Models;
using Shared.ProcessFlowDiagram;
using Shared.WorkSpaceManagers;

namespace Distillator.Domain.Factories;

/// <summary>
/// Fábrica específica para equipos de diagramas PFD.
/// </summary>
public interface IPfdEquipmentFactory : IEquipmentFactory
{
}

/// <summary>
/// Fábrica específica para equipos de diagramas P&ID.
/// </summary>
public interface IPandidEquipmentFactory : IEquipmentFactory
{
}

/// <summary>
/// Fábrica específica para equipos de diagramas eléctricos.
/// </summary>
public interface IElectricalEquipmentFactory : IEquipmentFactory
{
}

/// <summary>
/// Fábrica de Flowsheets. Crea la instancia correcta según el tipo de diagrama.
/// </summary>
public interface IFlowsheetFactory
{
    IFlowsheet Create(string name, string flowsheetTypeCode, IProject project, Guid? id = null);
}

public class FlowsheetFactory : IFlowsheetFactory
{
    public IFlowsheet Create(string name, string flowsheetTypeCode, IProject project, Guid? id = null)
    {
        var type = project.FlowsheetTypes.GetByCode(flowsheetTypeCode)
            ?? throw new ArgumentException($"Unknown flowsheet type code: {flowsheetTypeCode}", nameof(flowsheetTypeCode));

        return flowsheetTypeCode.ToUpperInvariant() switch
        {
            "PFD" => new PfdFlowsheet(name, type, project, id),
            "PANDID" => new PandidFlowsheet(name, type, project, id),
            "ELECTRICAL" => new ElectricalFlowsheet(name, type, project, id),
            _ => throw new NotSupportedException($"Flowsheet type not supported: {flowsheetTypeCode}")
        };
    }
}
