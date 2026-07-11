
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.ProcessFlowDiagram.Streams;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using Shared.WorkSpaceManagers;


namespace Client.Services.EquipmentManagers
{


    
    //public class WorkspaceManager
    //{


    //    public void SelectElement(IVisualElement element)
    //    {
    //        SelectedElement = element;
    //        NotifyStateChanged();
    //    }

    //    private readonly IEquipmentFactory _factory;
    //    private readonly IMainSolver _solver;
    //    private readonly CameraService _camera;
    //    private readonly CanvasSizeService _canvasSize;
    //    private readonly EquipmentMovementService _movement;
    //    private readonly AreaManagementService _areaManagement;
    //    private readonly CssStyleService _cssStyle;
    //    private readonly ConnectionService _connection;
    //    private readonly InterAreaConnectionService _interArea;
    //    private readonly SimulationOrchestrator _simOrchestrator;

    //    public double GlobalScale { get => _camera.GlobalScale; set => _camera.GlobalScale = value; }
    //    public string CameraTransform => _camera.CameraTransform;
    //    public double Zoom { get => _camera.Zoom; set => _camera.Zoom = value; }
    //    public double PanX { get => _camera.PanX; set => _camera.PanX = value; }
    //    public double PanY { get => _camera.PanY; set => _camera.PanY = value; }
    //    public bool IsPanning => _camera.IsPanning;

    //    // ==============================================================================
    //    // ESTADO DEL LIENZO Y COLECCIONES
    //    // ==============================================================================
    //    public IVisualElement? SelectedElement { get; private set; }

    //    public List<DiagramArea> Areas => _areaManagement.Areas;
    //    public DiagramArea ActiveArea => _areaManagement.ActiveArea;
    //    public List<IVisualElement> Elements => ActiveArea.Elements;
    //    public List<PipeVisualElement> Pipes => ActiveArea.Pipes;

    //    public double DiagramWidth => _canvasSize.DiagramWidth;
    //    public double DiagramHeight => _canvasSize.DiagramHeight;
    //    public const int GridSize = 20;
    //    public double Snap(double val) => _canvasSize.Snap(val);

    //    public bool IsMovingAny => _movement.IsMovingAny;
    //    public bool IsMoving(IVisualElement el) => _movement.IsMoving(el);

    //    public Action? OnNotifyUI;

    //    // ?? B6: Estado de conexi�n extra�do a ConnectionService.
    //    public bool IsConnectionModeActive => _connection.IsConnectionModeActive;
    //    public PipeVisualElement? CurrentDraftPipe => _connection.CurrentDraftPipe;
    //    public double DraftMouseLogicalX => _connection.DraftMouseLogicalX;
    //    public double DraftMouseLogicalY => _connection.DraftMouseLogicalY;
    //    public INamingService NamingService { get; }
    //    public WorkspaceManager(IEquipmentFactory factory, IMainSolver solver, INamingService namingService)
    //    {
    //        _factory = factory;
    //        _solver = solver;
    //        _camera = new CameraService();
    //        _camera.OnNotifyUI = () => OnNotifyUI?.Invoke();
    //        _canvasSize = new CanvasSizeService();
    //        _movement = new EquipmentMovementService(factory, solver, _camera, _canvasSize);
    //        _areaManagement = new AreaManagementService();
    //        _areaManagement.Initialize("Main Area");
    //        _cssStyle = new CssStyleService(_camera);
    //        _connection = new ConnectionService();
    //        _interArea = new InterAreaConnectionService(factory, solver);
    //        _simOrchestrator = new SimulationOrchestrator(solver);
    //        _simOrchestrator.GetPipes = () => ActiveArea.Pipes;
    //        _simOrchestrator.GetAreas = () => Areas;
    //        _simOrchestrator.GetActiveArea = () => ActiveArea;
    //        _simOrchestrator.RunSimulationAction = () => RunSimulation();
    //        _simOrchestrator.NotifyStateChangedAction = () => NotifyStateChanged();
    //        _connection.OnNotifyUI = () => OnNotifyUI?.Invoke();
    //        _connection.GetPipes = () => ActiveArea.Pipes;
    //        _connection.GetAreas = () => Areas;
    //        _connection.GetActiveArea = () => ActiveArea;
    //        _connection.GenerateStreamName = (prefix) => NamingService.GenerateNextName(prefix);
    //        _connection.SnapFunc = (val) => _canvasSize.Snap(val);
    //        _connection.CreateStreamFunc = (name) => _interArea.CreateStreamProgrammatically(name);
    //        _connection.RunSimulationAction = () => RunSimulation();
    //        _connection.DisconnectPortAction = (eq, port) => DisconnectEquipmentPort(eq, port);
    //        _interArea.GetActiveArea = () => ActiveArea;
    //        _interArea.SnapFunc = (val) => _canvasSize.Snap(val);
    //        _interArea.UpdateDiagramSizeAction = () => UpdateDiagramSize();
    //        _interArea.RunSimulationAction = () => RunSimulation();
    //        _interArea.NotifyStateChangedAction = () => NotifyStateChanged();
    //        NamingService = namingService;
    //        UpdateDiagramSize();
    //    }

    //    // ==============================================================================
    //    // GESTI�N DE �REAS
    //    // ==============================================================================
    //    public void CreateArea(string name)
    //    {
    //        var newArea = _areaManagement.CreateArea(name);
    //        _areaManagement.ActiveArea = newArea;
    //        SelectedElement = null;
    //        _connection.CancelConnectionDraft();
    //        _camera.Reset();
    //        _canvasSize.UpdateDiagramSize(newArea.Elements, GlobalScale);
    //        newArea.DiagramWidth = _canvasSize.DiagramWidth;
    //        newArea.DiagramHeight = _canvasSize.DiagramHeight;
    //        NotifyStateChanged();
    //    }

    //    public void SwitchToArea(DiagramArea area)
    //    {
    //        if (area == null || _areaManagement.ActiveArea.Id == area.Id) return;

    //        // ?? A2/A3: Guardar estado del �rea que abandonamos
    //        var leaving = _areaManagement.ActiveArea;
    //        leaving.Zoom = _camera.Zoom;
    //        leaving.PanX = _camera.PanX;
    //        leaving.PanY = _camera.PanY;
    //        leaving.DiagramWidth = _canvasSize.DiagramWidth;
    //        leaving.DiagramHeight = _canvasSize.DiagramHeight;

    //        _areaManagement.ActiveArea = area;
    //        SelectedElement = null;
    //        _connection.CancelConnectionDraft();

    //        // ?? A2: Restaurar estado de c�mara del �rea que entramos
    //        _camera.Zoom = area.Zoom;
    //        _camera.PanX = area.PanX;
    //        _camera.PanY = area.PanY;

    //        // ?? A3: Restaurar dimensiones del �rea que entramos
    //        if (area.DiagramWidth > 0 && area.DiagramHeight > 0)
    //        {
    //            _canvasSize.SetDimensions(area.DiagramWidth, area.DiagramHeight);
    //        }
    //        else
    //        {
    //            _canvasSize.UpdateDiagramSize(area.Elements, GlobalScale);
    //            area.DiagramWidth = _canvasSize.DiagramWidth;
    //            area.DiagramHeight = _canvasSize.DiagramHeight;
    //        }

    //        NotifyStateChanged();
    //    }

    //    public void NavigateToOpcTarget(Guid targetAreaId, Guid? targetConnectorId)
    //    {
    //        var targetArea = _areaManagement.Areas.FirstOrDefault(a => a.Id == targetAreaId);
    //        if (targetArea == null || !targetConnectorId.HasValue) return;

    //        SwitchToArea(targetArea);

    //        var twinOpc = targetArea.Elements.FirstOrDefault(e => e.Id == targetConnectorId.Value);
    //        if (twinOpc != null)
    //        {
    //            SelectElement(twinOpc);
    //        }
    //    }

    //    public void RenameArea(DiagramArea area, string newName)
    //    {
    //        _areaManagement.RenameArea(area, newName);

    //        // ?? Actualizar Label y TargetAreaName de todos los OPCs que apuntan a esta �rea
    //        foreach (var a in _areaManagement.Areas)
    //        {
    //            foreach (var opc in a.Elements.OfType<OffPageConnectorElement>())
    //            {
    //                if (opc.TargetAreaId == area.Id)
    //                {
    //                    opc.Label = newName;
    //                    opc.TargetAreaName = newName;
    //                }
    //            }
    //        }

    //        NotifyStateChanged();
    //    }

    //    public void DeleteArea(DiagramArea? area)
    //    {
    //        if (area == null) return;
    //        bool wasActive = (area.Id == _areaManagement.ActiveArea?.Id);
    //        if (!_areaManagement.DeleteArea(area)) return;
    //        if (wasActive) SwitchToArea(Areas.First());
    //        else NotifyStateChanged();
    //    }

    //    public void ReorderArea(DiagramArea move, DiagramArea target)
    //    {
    //        if (_areaManagement.ReorderArea(move, target))
    //            NotifyStateChanged();
    //    }

    //    // ==============================================================================
    //    // L�GICA DE EQUIPOS Y COLISIONES (FIEL A TU BACKUP)
    //    // ==============================================================================
    //    public void AddFromToolbox(EquipmentType type, double offsetX, double offsetY)
    //    {
    //        var el = _movement.AddFromToolbox(type, offsetX, offsetY, Elements);
    //        if (el != null)
    //        {
    //            UpdateDiagramSize();
    //            NotifyStateChanged();
    //        }
    //    }
    //    public void StartMove(IVisualElement el, MouseEventArgs e)
    //    {
    //        _movement.StartMove(el, e, IsConnectionModeActive, SelectElement);
    //    }
    //    public void Move(MouseEventArgs e)
    //    {
    //        _movement.Move(e);
    //        NotifyStateChanged();
    //    }
    //    public void EndMove()
    //    {
    //        if (_movement.EndMove(Elements))
    //        {
    //            UpdateDiagramSize();
    //            NotifyStateChanged();
    //        }
    //    }

    //    public void SetConnectionMode(bool isActive) => _connection.SetConnectionMode(isActive);
    //    public void StartConnectionDraft(IVisualElement source, string portName) => _connection.StartConnectionDraft(source, portName);
    //    public void UpdateConnectionDraft(double clientX, double clientY) => _connection.UpdateConnectionDraft(clientX, clientY);
    //    public void CancelConnectionDraft() => _connection.CancelConnectionDraft();
    //    public void CompleteConnection2(IVisualElement target, string targetPortName) => _connection.CompleteConnection2(target, targetPortName);
    //    public void CompleteConnection(IVisualElement? target, string? targetPortName, double dropX, double dropY) => _connection.CompleteConnection(this, target, targetPortName, dropX, dropY);
    //    public bool IsValidTarget(IVisualElement target, string targetPortName) => _connection.IsValidTarget(target, targetPortName);

    //    public void CreateInterAreaConnection(IVisualElement localEquip, string localPortName, StreamVisualElement remStream, DiagramArea remArea)
    //        => _interArea.CreateInterAreaConnection(localEquip, localPortName, remStream, remArea);
    //    public StreamVisualElement? CreateStreamProgrammatically(string name) => _interArea.CreateStreamProgrammatically(name);
    //    public void StartPan(MouseEventArgs e) => _camera.StartPan(e, IsMovingAny, IsConnectionModeActive);
    //    public void Pan(MouseEventArgs e) => _camera.Pan(e);
    //    public void EndPan() => _camera.EndPan();
    //    public void ZoomAt(double dY, double pX, double pY) => _camera.ZoomAt(dY, pX, pY);
    //    public void ZoomToFit(double screenWidth, double screenHeight)
    //    {
    //        if (Elements.Count == 0) return;
    //        double minX = Elements.Min(e => e.X); double maxX = Elements.Max(e => e.X + e.Width);
    //        double minY = Elements.Min(e => e.Y); double maxY = Elements.Max(e => e.Y + e.Height);
    //        _camera.ZoomToFit(screenWidth, screenHeight, minX, maxX, minY, maxY);
    //    }

    //    public void SetContainerDimensions(double width, double height)
    //    {
    //        _canvasSize.SetContainerDimensions(width, height);
    //        _canvasSize.UpdateDiagramSize(Elements, GlobalScale);
    //    }
    //    private void UpdateDiagramSize()
    //    {
    //        _canvasSize.UpdateDiagramSize(Elements, GlobalScale);
    //        // ?? A3: Persistir dimensiones calculadas en el �rea activa
    //        ActiveArea.DiagramWidth = _canvasSize.DiagramWidth;
    //        ActiveArea.DiagramHeight = _canvasSize.DiagramHeight;
    //    }

    //    public void NotifyStateChanged() => OnNotifyUI?.Invoke();

    //    public string WorkspaceCssClass => _cssStyle.WorkspaceCssClass;
    //    public string WorkspaceBackgroundStyle => _cssStyle.WorkspaceBackgroundStyle;
    //    public string PaperStyle => _canvasSize.PaperStyle;

    //    public void LoadFromDatabase() { }
    //    public void SaveToDatabase() { }

    //    public void RunSimulation()
    //    {
    //        // 1. Ejecutar el solver
    //        _solver.RunSimulation();

    //        // 2. Notificar a la UI que termin�
    //        NotifyStateChanged();
    //    }
    //    // ?? C9: Fachadas delegadas a SimulationOrchestrator. C�digo viejo comentado abajo.
    //    public void DisconnectEquipmentPort(IVisualElement equipment, string portName)
    //        => _simOrchestrator.DisconnectEquipmentPort(equipment, portName);

    //    public void ConnectEquipmentToStream(IVisualElement equipment, string equipmentPortName, IVisualElement stream)
    //        => _simOrchestrator.ConnectEquipmentToStream(equipment, equipmentPortName, stream);


    //}


}