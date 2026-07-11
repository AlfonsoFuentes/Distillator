using Distillator.Domain.Configuration;
using Shared.ProcessFlowDiagram;

namespace Distillator.Domain.Models;

/// <summary>
/// Un diagrama (Flowsheet) dentro de un proyecto.
/// Contiene referencias a equipos del EquipmentRegistry del proyecto,
/// más su propio estado visual (cámara, pipes).
/// Es abstracto porque cada tipo de diagrama (PFD, P&ID, Electrical) tiene comportamiento propio.
/// </summary>
public interface IFlowsheet
{
    Guid Id { get; }
    string Name { get; set; }
    string TypeCode { get; }
    IFlowsheetType TypeDefinition { get; }

    // Estado visual propio de ESTE diagrama
    double Zoom { get; set; }
    double PanX { get; set; }
    double PanY { get; set; }
    double DiagramWidth { get; set; }
    double DiagramHeight { get; set; }
    double GridSize { get; set; }
    double GlobalScale { get; set; }

    // Referencias a equipos (del registry del proyecto)
    // Guarda solo: ElementId + posición + rotación + z-index + flip
    IReadOnlyCollection<IFlowsheetElementReference> Elements { get; }
    IReadOnlyCollection<IPipeReference> Pipes { get; }

    // Referencia ascendente
    IProject Project { get; }

    void AddElementReference(IFlowsheetElementReference reference);
    void RemoveElementReference(Guid elementId);
    IFlowsheetElementReference? GetElementReference(Guid elementId);

    void AddPipe(IPipeReference pipe);
    void RemovePipe(Guid pipeId);
    IPipeReference? GetPipe(Guid pipeId);

    void ResetCameraToDefaults();
}

/// <summary>
/// Referencia posicional de un equipo en un diagrama.
/// El equipo real vive en IEquipmentRegistry del proyecto.
/// </summary>
public interface IFlowsheetElementReference
{
    Guid ElementId { get; }
    double X { get; set; }
    double Y { get; set; }
    int RotationAngle { get; set; }
    int ZIndex { get; set; }
    bool IsFlippedHorizontal { get; set; }
    bool IsFlippedVertical { get; set; }
}

/// <summary>
/// Referencia a una conexión entre dos equipos en un diagrama.
/// </summary>
public interface IPipeReference
{
    Guid Id { get; }
    Guid SourceElementId { get; }
    Guid TargetElementId { get; }
    string SourcePortName { get; }
    string TargetPortName { get; }
}

/// <summary>
/// Referencia a un Off-Page Connector (OPC) en un diagrama.
/// Representa un punto de entrada/salida hacia otro Flowsheet.
/// </summary>
public interface IOffPageConnectorReference : IFlowsheetElementReference
{
    Guid? TargetFlowsheetId { get; }
    Guid? TargetConnectorId { get; }
    string TargetFlowsheetName { get; }
    string ConnectedEquipmentName { get; }
    bool IsOutlet { get; }
}

/// <summary>
/// Conexión lógica entre dos Flowsheets mediante OPCs.
/// </summary>
public interface IInterFlowsheetConnection
{
    Guid Id { get; }
    Guid SourceFlowsheetId { get; }
    Guid TargetFlowsheetId { get; }
    Guid SourceConnectorId { get; }
    Guid TargetConnectorId { get; }
}
