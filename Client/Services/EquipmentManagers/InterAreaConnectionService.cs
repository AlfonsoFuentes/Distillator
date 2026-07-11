
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.ProcessFlowDiagram.Streams;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using Shared.WorkSpaceManagers;

namespace Client.Services.EquipmentManagers
{
    // 🎛️ Configuración centralizada para futuro panel UI
    //public static class InterAreaConnectionDefaults
    //{
    //    public static double ArrowOffset { get; set; } = 60;
    //}

    //public class InterAreaConnectionService
    //{
    //    private readonly IEquipmentFactory _factory;
    //    private readonly IMainSolver _solver;

    //    public InterAreaConnectionService(IEquipmentFactory factory, IMainSolver solver)
    //    {
    //        _factory = factory;
    //        _solver = solver;
    //    }

    //    /// <summary>
    //    /// Ancho fijo coordinado de 200px para OPCs.
    //    /// Nombres largos son cubiertos por el tooltip.
    //    /// </summary>
    //    private double CalculateOpcWidth(string areaName)
    //    {
    //        return 200;
    //    }

    //    // Callbacks para operaciones que requieren contexto del WorkspaceManager
    //    public Func<DiagramArea>? GetActiveArea { get; set; }
    //    public Func<double, double>? SnapFunc { get; set; }
    //    public Action? UpdateDiagramSizeAction { get; set; }
    //    public Action? RunSimulationAction { get; set; }
    //    public Action? NotifyStateChangedAction { get; set; }

    //    // 🔄 B7: Creación de streams extraída de WorkspaceManager.
    //    public StreamVisualElement? CreateStreamProgrammatically(string name)
    //    {
    //        var activeArea = GetActiveArea?.Invoke();
    //        if (activeArea == null || SnapFunc == null) return null;

    //        // 1. Usamos la fábrica oficial para que construya la corriente y su Facade
    //        var stream = _factory.Create(EquipmentType.MaterialStream, 0, 0, SnapFunc) as StreamVisualElement;

    //        if (stream != null)
    //        {
    //            // 2. Le asignamos el nombre ("S-101", etc.)
    //            stream.Name = name;
    //            stream.Label = name;

    //            if (stream.Facade != null)
    //            {
    //                stream.Facade.Name = name; // Que el cerebro también sepa cómo se llama

    //                // 🚩 3. ¡LA MAGIA! Registramos la corriente en el Solver
    //                if (stream.Facade is IFacadeStream s)
    //                {
    //                    _solver.AddStream(s);
    //                }
    //            }

    //            // 4. Lo añadimos al lienzo
    //            activeArea.Elements.Add(stream);
    //        }

    //        return stream;
    //    }

    //    // 🔄 B7: Conexión inter-área extraída de WorkspaceManager.
    //    public void CreateInterAreaConnection(IVisualElement localEquip, string localPortName, StreamVisualElement remStream, DiagramArea remArea)
    //    {
    //        var activeArea = GetActiveArea?.Invoke();
    //        if (activeArea == null || SnapFunc == null) return;

    //        var lPort = localEquip.Ports.FirstOrDefault(p => p.Name == localPortName);

    //        // 👉 TRUE = flujo entra al área actual (Succión)
    //        bool isFlowEnteringArea = lPort?.Type == PortType.Inlet;

    //        // =========================================================
    //        // OPC LOCAL Y REMOTO (Visuales)
    //        // =========================================================
    //        // 🚩 UX: Posicionar OPCs como banderas en los bordes del área (estilo P&ID)
    //        // Lógica simétrica: mismo offset desde el borde en ambos lados.
    //        // Izquierdo: X = 0 + arrowOffset
    //        // Derecho:  X = DiagramWidth - opcWidth - arrowOffset
    //        double arrowOffset = SnapFunc?.Invoke(InterAreaConnectionDefaults.ArrowOffset)
    //                             ?? InterAreaConnectionDefaults.ArrowOffset;

    //        // Ancho dinámico según nombre del área que muestra cada OPC
    //        double lOpcWidth = CalculateOpcWidth(remArea.Name);      // lOpc muestra nombre del área remota
    //        double rOpcWidth = CalculateOpcWidth(activeArea.Name); // rOpc muestra nombre del área actual

    //        // Izquierdo: desde x=0 sumamos el offset
    //        double leftEdgeX = arrowOffset;

    //        // 🚩 Ancho efectivo del área: nunca 0. Si DiagramWidth no está cacheado, calcular desde elementos.
    //        double activeEffectiveWidth = Math.Max(
    //            activeArea.DiagramWidth,
    //            activeArea.Elements.Count > 0
    //                ? activeArea.Elements.Max(e => e.X + e.Width) + 100
    //                : 600);

    //        double remEffectiveWidth = Math.Max(
    //            remArea.DiagramWidth,
    //            remArea.Elements.Count > 0
    //                ? remArea.Elements.Max(e => e.X + e.Width) + 100
    //                : 600);

    //        // Derecho del área LOCAL (coloca lOpc)
    //        double activeRightEdge = (activeEffectiveWidth > lOpcWidth + arrowOffset * 2)
    //            ? activeEffectiveWidth - lOpcWidth - arrowOffset
    //            : SnapFunc?.Invoke(leftEdgeX + lOpcWidth + arrowOffset) ?? (leftEdgeX + lOpcWidth + arrowOffset);

    //        // Derecho del área REMOTA (coloca rOpc)
    //        double remRightEdge = (remEffectiveWidth > rOpcWidth + arrowOffset * 2)
    //            ? remEffectiveWidth - rOpcWidth - arrowOffset
    //            : SnapFunc?.Invoke(leftEdgeX + rOpcWidth + arrowOffset) ?? (leftEdgeX + rOpcWidth + arrowOffset);

    //        var lOpc = new OffPageConnectorElement(isFlowEnteringArea ? false : true)
    //        {
    //            Width = lOpcWidth,
    //            TargetAreaId = remArea.Id,
    //            TargetConnectorId = Guid.NewGuid(),
    //            Label = remArea.Name,
    //            TargetAreaName = remArea.Name,
    //            ConnectedEquipmentName = localEquip.Label,
    //            X = isFlowEnteringArea ? leftEdgeX : activeRightEdge,
    //            Y = SnapFunc?.Invoke(localEquip.Y) ?? localEquip.Y
    //        };
    //        lOpc.RefreshPorts(); // ← Recalcular geometría con el ancho real asignado

    //        var rOpc = new OffPageConnectorElement(isFlowEnteringArea)
    //        {
    //            Width = rOpcWidth,
    //            TargetAreaId = activeArea.Id,
    //            TargetConnectorId = lOpc.Id,
    //            Id = lOpc.TargetConnectorId.Value,
    //            Label = activeArea.Name,
    //            TargetAreaName = activeArea.Name,
    //            ConnectedEquipmentName = remStream.Label,
    //            X = isFlowEnteringArea ? remRightEdge : leftEdgeX,
    //            Y = SnapFunc?.Invoke(remStream.Y) ?? remStream.Y
    //        };
    //        rOpc.RefreshPorts(); // ← Recalcular geometría con el ancho real asignado

    //        activeArea.Elements.Add(lOpc);
    //        remArea.Elements.Add(rOpc);

    //        // =========================================================
    //        // 🚩 LA MAGIA DEL "WORMHOLE" (Agujero de Gusano Lógico)
    //        // =========================================================
    //        string remPortName = isFlowEnteringArea ? "Outlet" : "Inlet";

    //        // 1. Ocupar los puertos en la Interfaz Gráfica para que no se puedan reutilizar y se pongan grises
    //        if (lPort != null) lPort.ConnectedElementId = lOpc.Id;
    //        lOpc.Ports.First(p => p.Name == "Transfer").ConnectedElementId = localEquip.Id;

    //        var rPort = remStream.Ports.FirstOrDefault(p => p.Name == remPortName);
    //        if (rPort != null) rPort.ConnectedElementId = rOpc.Id;
    //        rOpc.Ports.First(p => p.Name == "Transfer").ConnectedElementId = remStream.Id;

    //        // 2. Conectar los Cerebros Termodinámicos DIRECTAMENTE
    //        if (localEquip.Facade is IEquipmentFacade localEquipment && remStream.Facade is IFacadeStream remoteStream)
    //        {
    //            localEquip.AttachConnection(localPortName, remoteStream);
    //        }
    //        else if (remStream.Facade is IEquipmentFacade remoteEquipment && localEquip.Facade is IFacadeStream localStream)
    //        {
    //            localEquip.AttachConnection(remPortName, localStream);
    //        }

    //        // =========================================================
    //        // PIPES VISUALES
    //        // =========================================================
    //        var pLocal = new PipeVisualElement { Id = Guid.NewGuid(), ShowTechnicalLabel = false };
    //        if (isFlowEnteringArea)
    //        {
    //            pLocal.SourceElement = lOpc;
    //            pLocal.TargetElement = localEquip;
    //            pLocal.SourcePortName = "Transfer";
    //            pLocal.TargetPortName = localPortName;
    //        }
    //        else
    //        {
    //            pLocal.SourceElement = localEquip;
    //            pLocal.TargetElement = lOpc;
    //            pLocal.SourcePortName = localPortName;
    //            pLocal.TargetPortName = "Transfer";
    //        }
    //        pLocal.SourceElementId = pLocal.SourceElement.Id;
    //        pLocal.TargetElementId = pLocal.TargetElement.Id;
    //        activeArea.Pipes.Add(pLocal);

    //        var pRem = new PipeVisualElement { Id = Guid.NewGuid(), ShowTechnicalLabel = false };
    //        if (isFlowEnteringArea)
    //        {
    //            pRem.SourceElement = remStream;
    //            pRem.TargetElement = rOpc;
    //            pRem.SourcePortName = remPortName;
    //            pRem.TargetPortName = "Transfer";
    //        }
    //        else
    //        {
    //            pRem.SourceElement = rOpc;
    //            pRem.TargetElement = remStream;
    //            pRem.SourcePortName = "Transfer";
    //            pRem.TargetPortName = remPortName;
    //        }
    //        pRem.SourceElementId = pRem.SourceElement.Id;
    //        pRem.TargetElementId = pRem.TargetElement.Id;
    //        remArea.Pipes.Add(pRem);

    //        UpdateDiagramSizeAction?.Invoke();
    //        RunSimulationAction?.Invoke();
    //        NotifyStateChangedAction?.Invoke();
    //    }
    //}
}