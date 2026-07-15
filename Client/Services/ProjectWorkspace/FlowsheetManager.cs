using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.ProcessFlowDiagram.Streams;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using Shared.WorkSpaceManagers;
using System.Threading;
using UnitSystem;
using Distillator.Domain.Factories;
using Distillator.Domain.Models;
using Distillator.Domain.Policies;
using Distillator.Domain.Services;

namespace Client.Services.ProjectWorkspace;

/// <summary>
/// Nuevo gestor de dominio para un diagrama (Flowsheet) dentro de un proyecto.
/// Reemplaza las responsabilidades de WorkspaceManager pero trabaja directamente
/// sobre Project, Flowsheet y EquipmentRegistry de Distillator.Domain.
/// </summary>
public class FlowsheetManager
{
    private readonly ICameraService _cameraService;
    private readonly IPlacementRules _placementRules;
    private readonly IEquipmentNamingService _namingService;
    private readonly FlowsheetCanvasLayoutService _canvasLayout;
    private readonly FlowsheetStyleService _styleService;
    private readonly EquipmentDragService _drag;

    private Project? _project;
    private IFlowsheet? _flowsheet;
    private IConnectionService? _connectionService;
    private IMainSolver? _subscribedSolver;
    private Action? _solverCompletionHandler;
    private long _solverSubscriptionVersion;
    private List<IVisualElement> _elements = new();
    private List<PipeVisualElement> _pipes = new();

    private bool _isPanning;
    private int _runningSimulations;
    private bool _visualSavePendingAfterSimulation;
    private readonly Dictionary<Guid, IFlowsheet> _pendingVisualSaveFlowsheets = new();
    private readonly object _visualSaveSync = new();
    private double _lastPanMouseX;
    private double _lastPanMouseY;

    public FlowsheetManager(
        ICameraService cameraService,
        IPlacementRules placementRules,
        IEquipmentNamingService namingService,
        FlowsheetCanvasLayoutService canvasLayout,
        FlowsheetStyleService styleService,
        EquipmentDragService drag)
    {
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _placementRules = placementRules ?? throw new ArgumentNullException(nameof(placementRules));
        _namingService = namingService ?? throw new ArgumentNullException(nameof(namingService));
        _canvasLayout = canvasLayout ?? throw new ArgumentNullException(nameof(canvasLayout));
        _styleService = styleService ?? throw new ArgumentNullException(nameof(styleService));
        _drag = drag ?? throw new ArgumentNullException(nameof(drag));
    }

    // ==============================================================================
    // ESTADO PÚBLICO
    // ==============================================================================
    public Project? CurrentProject => _project;
    public IFlowsheet? CurrentFlowsheet => _flowsheet;
    public IVisualElement? SelectedElement { get; private set; }
    public List<IVisualElement> Elements => _elements;
    public List<PipeVisualElement> Pipes => _pipes;

    public double Zoom => _flowsheet?.Zoom ?? 1.0;
    public double PanX => _flowsheet?.PanX ?? 0;
    public double PanY => _flowsheet?.PanY ?? 0;
    public double GlobalScale => _flowsheet?.GlobalScale ?? 0.7;
    public bool IsPanning => _isPanning;
    public string CameraTransform => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"translate({Math.Round(PanX)}px, {Math.Round(PanY)}px) scale({Zoom}) scale({GlobalScale})");

    public string WorkspaceCssClass => _styleService.GetWorkspaceCssClass(_isPanning);
    public string WorkspaceBackgroundStyle => _styleService.GetWorkspaceBackgroundStyle(PanX, PanY, Zoom);
    public string PaperStyle => _canvasLayout.GetPaperStyle();
    public double DiagramWidth => _canvasLayout.DiagramWidth;
    public double DiagramHeight => _canvasLayout.DiagramHeight;
    public double Snap(double val) => _canvasLayout.Snap(val);
    public bool IsMovingAny => _drag.IsMovingAny;
    public bool IsMoving(IVisualElement el) => _drag.IsMoving(el);

    public bool IsConnectionModeActive { get; private set; }
    public PipeVisualElement? CurrentDraftPipe { get; private set; }
    public double DraftMouseLogicalX { get; private set; }
    public double DraftMouseLogicalY { get; private set; }

    public Action? OnNotifyUI;
    public Func<IReadOnlyCollection<IFlowsheet>, Task>? OnVisualStatesChangedAsync;

    // ==============================================================================
    // CARGA DE PROYECTO / FLOWSHEET
    // ==============================================================================
    public void LoadProject(Project project)
    {
        _project = project;
        _namingService.SetConfiguration(project.Configuration.NamingConfig);
        SubscribeToSolver(project.SimulationService.Solver);
    }

    public void LoadFlowsheet(IFlowsheet flowsheet)
    {
        _flowsheet = flowsheet;
        _elements.Clear();
        _pipes.Clear();
        SelectedElement = null;
        CurrentDraftPipe = null;
        IsConnectionModeActive = false;
        _isPanning = false;

        _connectionService = CreateConnectionService();
        _canvasLayout.SetDimensions(flowsheet.DiagramWidth, flowsheet.DiagramHeight);
        _canvasLayout.SetGridSize(flowsheet.GridSize);

        // Hidratar elementos desde las referencias del dominio
        foreach (var reference in flowsheet.Elements)
        {
            var element = _project?.EquipmentRegistry.GetById(reference.ElementId);
            if (element == null) continue;

            element.X = reference.X;
            element.Y = reference.Y;
            element.RotationAngle = reference.RotationAngle;
            element.ZIndex = reference.ZIndex;
            element.IsFlippedHorizontal = reference.IsFlippedHorizontal;
            element.IsFlippedVertical = reference.IsFlippedVertical;

            if (element is OffPageConnectorElement opc && reference is IOffPageConnectorReference opcReference)
            {
                opc.IsOutlet = opcReference.IsOutlet;
                opc.TargetAreaId = opcReference.TargetFlowsheetId;
                opc.TargetConnectorId = opcReference.TargetConnectorId;
                opc.TargetAreaName = opcReference.TargetFlowsheetName;
                opc.ConnectedEquipmentName = opcReference.ConnectedEquipmentName;
                opc.RefreshPorts();
            }

            _elements.Add(element);
        }

        // Hidratar tuberías
        foreach (var pipeRef in flowsheet.Pipes)
        {
            var pipe = new PipeVisualElement
            {
                Id = pipeRef.Id,
                SourceElementId = pipeRef.SourceElementId,
                SourcePortName = pipeRef.SourcePortName,
                TargetElementId = pipeRef.TargetElementId,
                TargetPortName = pipeRef.TargetPortName,
                SourceElement = GetElementById(pipeRef.SourceElementId),
                TargetElement = GetElementById(pipeRef.TargetElementId),
                ShowTechnicalLabel = false
            };
            _pipes.Add(pipe);
        }

        UpdateDiagramSize();
        NotifyStateChanged();
    }

    public void LoadProjectAndFlowsheet(Project project, IFlowsheet? flowsheet = null)
    {
        LoadProject(project);
        var target = flowsheet ?? project.Flowsheets.FirstOrDefault();
        if (target != null)
        {
            LoadFlowsheet(target);
        }
        else
        {
            // Si el proyecto no tiene diagramas, se crea uno PFD por defecto
            var pfd = project.CreateFlowsheet("PFD 1", "PFD");
            LoadFlowsheet(pfd);
        }
    }

    public void Clear()
    {
        _flowsheet = null;
        _elements.Clear();
        _pipes.Clear();
        SelectedElement = null;
        CurrentDraftPipe = null;
        IsConnectionModeActive = false;
        _isPanning = false;
        _canvasLayout.SetDimensions(0, 0);
        NotifyStateChanged();
    }

    // ==============================================================================
    // EQUIPOS
    // ==============================================================================
    public void AddFromToolbox(EquipmentType type, double offsetX, double offsetY)
    {
        if (_project == null || _flowsheet == null) return;
        if (type == EquipmentType.None) return;

        var factory = GetEquipmentFactory();
        var element = factory.Create(type, offsetX, offsetY, _canvasLayout.Snap);
        if (element == null) return;

        SetUniqueElementName(element);
        RegisterElementInSolver(element);

        // Resolver colisiones usando el dominio
        var (resolvedX, resolvedY) = _placementRules.ResolvePosition(
            element,
            _elements,
            _flowsheet.GridSize);
        element.X = resolvedX;
        element.Y = resolvedY;

        // Mantener el equipo dentro de la región cuadriculada (canvas).
        ClampElementPosition(element);

        _project.AddEquipment(element);
        _flowsheet.AddElementReference(new FlowsheetElementReference(element.Id, element.X, element.Y));
        _elements.Add(element);

        UpdateDiagramSize();
        RunSimulation();
        NotifyStateChanged();
        QueueVisualStateChanged();
    }

    public void SelectElement(IVisualElement? element)
    {
        SelectedElement = element;
        NotifyStateChanged();
    }

    public void StartMove(IVisualElement el, MouseEventArgs e)
    {
        _drag.StartMove(el, e, IsConnectionModeActive, SelectElement);
    }

    public void Move(MouseEventArgs e)
    {
        _drag.Move(e, Zoom, GlobalScale, _canvasLayout.DiagramWidth, _canvasLayout.DiagramHeight);
        NotifyStateChanged();
    }

    public void EndMove()
    {
        var moved = _drag.EndMove(
            _elements,
            _flowsheet?.GridSize ?? _canvasLayout.GridSize,
            _canvasLayout.DiagramWidth,
            _canvasLayout.DiagramHeight);

        if (moved)
        {
            SyncElementReferences();
            UpdateDiagramSize();
            NotifyStateChanged();
            QueueVisualStateChanged();
        }
    }

    public void CancelMove()
    {
        _drag.CancelMove();
        SyncElementReferences();
        UpdateDiagramSize();
        NotifyStateChanged();
    }

    public void RotateElement(IVisualElement element)
    {
        element.Rotate90();
        SyncElementReference(element);
        NotifyStateChanged();
        QueueVisualStateChanged();
    }

    public void FlipElementHorizontal(IVisualElement element)
    {
        element.ToggleFlipHorizontal();
        SyncElementReference(element);
        NotifyStateChanged();
        QueueVisualStateChanged();
    }

    public void FlipElementVertical(IVisualElement element)
    {
        element.ToggleFlipVertical();
        SyncElementReference(element);
        NotifyStateChanged();
        QueueVisualStateChanged();
    }

    public void DeleteElement(IVisualElement element)
    {
        if (_project == null || _flowsheet == null || _connectionService == null) return;

        // Desconectar todos los pipes conectados al elemento antes de eliminarlo.
        var connectedPipeIds = _flowsheet.Pipes
            .Where(p => p.SourceElementId == element.Id || p.TargetElementId == element.Id)
            .Select(p => p.Id)
            .ToList();

        foreach (var pipeId in connectedPipeIds)
        {
            _connectionService.Disconnect(_flowsheet, pipeId);
        }

        _project.RemoveEquipment(element.Id);
        _flowsheet.RemoveElementReference(element.Id);
        _elements.Remove(element);
        if (SelectedElement?.Id == element.Id) SelectedElement = null;

        RebuildPipes();
        UpdateDiagramSize();
        RunSimulation();
        NotifyStateChanged();
        QueueVisualStateChanged();
    }

    // ==============================================================================
    // CÁMARA
    // ==============================================================================
    public void StartPan(MouseEventArgs e)
    {
        if (!IsMovingAny && !IsConnectionModeActive && (e.Button == 0 || e.Button == 1))
        {
            _isPanning = true;
            _lastPanMouseX = e.ClientX;
            _lastPanMouseY = e.ClientY;
            NotifyStateChanged();
        }
    }

    public void Pan(MouseEventArgs e)
    {
        if (!_isPanning || _flowsheet == null) return;

        var deltaX = e.ClientX - _lastPanMouseX;
        var deltaY = e.ClientY - _lastPanMouseY;
        _lastPanMouseX = e.ClientX;
        _lastPanMouseY = e.ClientY;

        _cameraService.Pan(_flowsheet, deltaX, deltaY);
        PersistCamera();
        NotifyStateChanged();
    }

    public void EndPan()
    {
        _isPanning = false;
        NotifyStateChanged();
    }

    public void ZoomAt(double dY, double pX, double pY)
    {
        if (_flowsheet == null) return;
        _cameraService.ZoomAt(_flowsheet, dY, pX, pY);
        PersistCamera();
        NotifyStateChanged();
    }

    public void ZoomToFit(double screenWidth, double screenHeight)
    {
        if (_flowsheet == null) return;
        _cameraService.ZoomToFit(_flowsheet, screenWidth, screenHeight);
        PersistCamera();
        NotifyStateChanged();
    }

    public void SetContainerDimensions(double width, double height)
    {
        _canvasLayout.SetContainerDimensions(width, height);
        UpdateDiagramSize();
    }

    // ==============================================================================
    // CONEXIONES
    // ==============================================================================
    public void SetConnectionMode(bool isActive)
    {
        if (IsConnectionModeActive == isActive) return;
        IsConnectionModeActive = isActive;
        if (!isActive) CancelConnectionDraft();
        NotifyStateChanged();
    }

    public void StartConnectionDraft(IVisualElement source, string portName)
    {
        var portCoords = source.GetAbsolutePortCoordinates(portName);
        DraftMouseLogicalX = portCoords.X;
        DraftMouseLogicalY = portCoords.Y;
        CurrentDraftPipe = new PipeVisualElement
        {
            Id = Guid.NewGuid(),
            SourceElementId = source.Id,
            SourcePortName = portName,
            SourceElement = source,
            Label = "Draft...",
            ShowTechnicalLabel = false
        };
        NotifyStateChanged();
    }

    public void UpdateConnectionDraft(double clientX, double clientY)
    {
        if (CurrentDraftPipe == null) return;
        DraftMouseLogicalX = clientX;
        DraftMouseLogicalY = clientY;
        NotifyStateChanged();
    }

    public void CancelConnectionDraft()
    {
        CurrentDraftPipe = null;
        NotifyStateChanged();
    }

    public void CompleteConnection(IVisualElement? target, string? targetPortName, double dropX, double dropY)
    {
        if (_project == null || _flowsheet == null || _connectionService == null || CurrentDraftPipe?.SourceElement == null) return;

        var source = CurrentDraftPipe.SourceElement;
        var sourcePortName = CurrentDraftPipe.SourcePortName;

        CancelConnectionDraft();
        SetConnectionMode(false);

        var pipe = _connectionService.Connect(
            _flowsheet,
            source,
            sourcePortName,
            target,
            targetPortName,
            dropX,
            dropY);

        if (pipe != null)
        {
            EnsureElementInVisualList(pipe.SourceElementId);
            EnsureElementInVisualList(pipe.TargetElementId);
            RebuildPipes();
        }

        RunSimulation();
        NotifyStateChanged();
        QueueVisualStateChanged();
    }

    public bool IsValidTarget(IVisualElement target, string targetPortName)
    {
        if (_flowsheet == null || _connectionService == null || CurrentDraftPipe?.SourceElement == null) return false;
        var source = CurrentDraftPipe.SourceElement;
        var sourcePortName = CurrentDraftPipe.SourcePortName;
        return _connectionService.CanConnect(_flowsheet, source, sourcePortName, target, targetPortName);
    }

    // ==============================================================================
    // SIMULACIÓN
    // ==============================================================================
    public void RunSimulation()
    {
        if (_project == null) return;

        _visualSavePendingAfterSimulation = true;
        Interlocked.Increment(ref _runningSimulations);
        _project.RunSimulation();
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnNotifyUI?.Invoke();

    private void QueueVisualStateChanged(params IFlowsheet[] changedFlowsheets)
    {
        if (_flowsheet == null || OnVisualStatesChangedAsync == null) return;
        var flowsheets = changedFlowsheets.Length == 0 ? new[] { _flowsheet } : changedFlowsheets;

        if (Interlocked.CompareExchange(ref _runningSimulations, 0, 0) > 0)
        {
            _visualSavePendingAfterSimulation = true;
            lock (_visualSaveSync)
            {
                foreach (var flowsheet in flowsheets)
                {
                    _pendingVisualSaveFlowsheets[flowsheet.Id] = flowsheet;
                }
            }
            return;
        }

        var changed = flowsheets
            .DistinctBy(candidate => candidate.Id)
            .ToArray();
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (Func<IReadOnlyCollection<IFlowsheet>, Task> handler in
                         OnVisualStatesChangedAsync.GetInvocationList())
                {
                    await handler(changed);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Visual autosave failed: {ex.Message}");
            }
        });
    }

    private void SubscribeToSolver(IMainSolver solver)
    {
        if (_subscribedSolver == solver) return;

        if (_subscribedSolver != null && _solverCompletionHandler != null)
        {
            _subscribedSolver.OnSimulationCompleted -= _solverCompletionHandler;
        }

        Interlocked.Exchange(ref _runningSimulations, 0);
        _visualSavePendingAfterSimulation = false;
        lock (_visualSaveSync)
        {
            _pendingVisualSaveFlowsheets.Clear();
        }

        _subscribedSolver = solver;
        var subscriptionVersion = Interlocked.Increment(ref _solverSubscriptionVersion);
        _solverCompletionHandler = () => OnSolverSimulationCompleted(solver, subscriptionVersion);
        _subscribedSolver.OnSimulationCompleted += _solverCompletionHandler;
    }

    private void OnSolverSimulationCompleted(IMainSolver solver, long subscriptionVersion)
    {
        if (!ReferenceEquals(_subscribedSolver, solver) ||
            subscriptionVersion != Interlocked.Read(ref _solverSubscriptionVersion))
        {
            return;
        }

        var runningSimulations = Interlocked.Decrement(ref _runningSimulations);
        if (runningSimulations < 0)
        {
            Interlocked.Exchange(ref _runningSimulations, 0);
            runningSimulations = 0;
        }

        if (runningSimulations == 0 && _visualSavePendingAfterSimulation)
        {
            _visualSavePendingAfterSimulation = false;
            IFlowsheet[] pendingFlowsheets;
            lock (_visualSaveSync)
            {
                pendingFlowsheets = _pendingVisualSaveFlowsheets.Values.ToArray();
                _pendingVisualSaveFlowsheets.Clear();
            }

            QueueVisualStateChanged(pendingFlowsheets);
        }

        NotifyStateChanged();
    }

    // ==============================================================================
    // NAMING (pasarela al IEquipmentNamingService del dominio)
    // ==============================================================================
    public string GenerateNextName(string equipmentTypeCode)
    {
        if (_project == null) return string.Empty;
        return _namingService.GenerateNextName(equipmentTypeCode, _project, _flowsheet);
    }

    public string? SuggestName(string equipmentTypeCode)
    {
        if (_project == null) return null;
        return _namingService.SuggestName(equipmentTypeCode, _project, _flowsheet);
    }

    public bool IsNameAvailable(string name)
    {
        if (_project == null) return false;
        return _namingService.IsNameAvailable(name, _project);
    }

    // ==============================================================================
    // HELPERS PARA DIÁLOGOS DE EQUIPOS (reemplazan WM)
    // ==============================================================================

    /// <summary>
    /// Crea un StreamVisualElement programáticamente, lo registra en el proyecto
    /// y en el flowsheet actual, y lo añade a la lista visual.
    /// </summary>
    public StreamVisualElement? CreateStreamProgrammatically(string name)
    {
        if (_project == null || _flowsheet == null) return null;

        var factory = GetEquipmentFactory();
        var element = factory.Create(EquipmentType.MaterialStream, 0, 0, v => _canvasLayout.Snap(v));
        if (element is not StreamVisualElement stream) return null;

        stream.Name = name;
        stream.Label = name;
        if (stream.Facade != null)
        {
            stream.Facade.Name = name;
        }

        _project.AddEquipment(stream);
        _flowsheet.AddElementReference(new FlowsheetElementReference(stream.Id, stream.X, stream.Y));
        _elements.Add(stream);

        if (stream.Facade is IFacadeStream facadeStream)
        {
            var solver = GetSolver();
            solver.AddStream(facadeStream);
        }

        return stream;
    }

    public StreamVisualElement? CreateAndConnectStreamFromPort(IVisualElement equipment, string portName, string streamName)
    {
        if (_project == null || _flowsheet == null) return null;

        var port = equipment.Ports.FirstOrDefault(item => item.Name == portName);
        if (port == null) return null;

        var stream = CreateStreamProgrammatically(streamName);
        if (stream == null) return null;

        PositionStreamFromPort(equipment, port, stream);
        SyncElementReference(stream);
        ConnectEquipmentToProjectStream(equipment, portName, stream);
        return stream;
    }

    /// <summary>
    /// Conecta un equipo a un stream existente usando el SimulationService del dominio.
    /// </summary>
    public void ConnectEquipmentToStream(IVisualElement equipment, string portName, IVisualElement stream)
    {
        if (_project == null || _flowsheet == null) return;
        _project.SimulationService.ConnectEquipmentToStream(_project, _flowsheet, equipment, portName, stream);
        RebuildPipes();
        RunSimulation();
        NotifyStateChanged();
        QueueVisualStateChanged();
    }

    /// <summary>
    /// Conecta un equipo al stream indicado, creando OPCs si el stream vive en otro flowsheet.
    /// </summary>
    public void ConnectEquipmentToProjectStream(IVisualElement equipment, string portName, IVisualElement stream)
    {
        if (_project == null || _flowsheet == null) return;

        var streamFlowsheet = FindFlowsheetForElement(stream.Id);
        if (streamFlowsheet == null) return;

        if (streamFlowsheet.Id == _flowsheet.Id)
        {
            ConnectEquipmentToStream(equipment, portName, stream);
            return;
        }

        if (equipment.Ports.FirstOrDefault(p => p.Name == portName)?.ConnectedElementId.HasValue == true)
        {
            _project.SimulationService.DisconnectPort(_project, _flowsheet, equipment, portName);
        }

        var interFlowsheetService = new InterFlowsheetConnectionService(
            _placementRules,
            _namingService,
            _project.SimulationService);

        var connection = interFlowsheetService.CreateInterFlowsheetConnection(
            _project,
            _flowsheet,
            equipment,
            portName,
            streamFlowsheet,
            stream);

        if (connection == null) return;

        RebuildPipes();
        UpdateDiagramSize();
        RunSimulation();
        NotifyStateChanged();
        QueueVisualStateChanged(_flowsheet, streamFlowsheet);
    }

    private void PositionStreamFromPort(IVisualElement equipment, EquipmentPort port, StreamVisualElement stream)
    {
        if (_flowsheet == null) return;

        var absCoords = equipment.GetAbsolutePortCoordinates(port.Name);
        const double spawnDistance = 80;
        double dx = 0;
        double dy = 0;

        switch (port.Direction)
        {
            case PortDirection.Top: dy = -spawnDistance; break;
            case PortDirection.Bottom: dy = spawnDistance; break;
            case PortDirection.Left: dx = -spawnDistance; break;
            case PortDirection.Right: dx = spawnDistance; break;
        }

        var isInlet = port.Type == PortType.Inlet;
        stream.RotationAngle = port.Direction switch
        {
            PortDirection.Left => isInlet ? 0 : 180,
            PortDirection.Right => isInlet ? 180 : 0,
            PortDirection.Top => isInlet ? 90 : 270,
            PortDirection.Bottom => isInlet ? 270 : 90,
            _ => stream.RotationAngle
        };

        var streamPortName = isInlet ? "Outlet" : "Inlet";
        var (streamOffsetX, streamOffsetY, _) = stream.GetTransformedPort(streamPortName);

        stream.X = _canvasLayout.Snap((absCoords.X + dx) - streamOffsetX);
        stream.Y = _canvasLayout.Snap((absCoords.Y + dy) - streamOffsetY);
        ClampElementPosition(stream);
    }

    /// <summary>
    /// Devuelve streams libres de todo el proyecto según el lado requerido por el puerto del equipo.
    /// </summary>
    public IEnumerable<IVisualElement> GetAvailableStreamsForPort(EquipmentPort port)
    {
        if (_project == null) return Enumerable.Empty<IVisualElement>();

        var requiredStreamPortName = port.Type == PortType.Inlet ? "Outlet" : "Inlet";

        return _project.Flowsheets
            .SelectMany(flowsheet => flowsheet.Elements)
            .Select(reference => _project.EquipmentRegistry.GetById(reference.ElementId))
            .OfType<StreamVisualElement>()
            .Where(stream => IsStreamPortFree(stream.Id, requiredStreamPortName))
            .OrderBy(stream => stream.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Desconecta un puerto de equipo usando el SimulationService del dominio.
    /// </summary>
    public void DisconnectEquipmentPort(IVisualElement equipment, string portName)
    {
        if (_project == null || _flowsheet == null) return;
        _project.SimulationService.DisconnectPort(_project, _flowsheet, equipment, portName);
        RebuildPipes();
        RunSimulation();
        NotifyStateChanged();
        QueueVisualStateChanged();
    }

    /// <summary>
    /// Resuelve el IVisualElement conectado a un puerto de equipo, incluyendo OPCs inter-flowsheet.
    /// </summary>
    public IVisualElement? GetConnectedElementForPort(IVisualElement parentEquipment, string portName)
    {
        if (_project == null || _flowsheet == null) return null;

        // Buscar pipe local
        var pipe = _pipes.FirstOrDefault(p =>
            (p.SourceElementId == parentEquipment.Id && p.SourcePortName == portName) ||
            (p.TargetElementId == parentEquipment.Id && p.TargetPortName == portName));

        if (pipe == null) return null;

        var connectedElementId = pipe.SourceElementId == parentEquipment.Id ? pipe.TargetElementId : pipe.SourceElementId;
        var connectedElement = _elements.FirstOrDefault(x => x.Id == connectedElementId)
            ?? _project.EquipmentRegistry.GetById(connectedElementId);

        // Si es un OPC, buscar el stream real en el flowsheet remoto
        if (connectedElement is OffPageConnectorElement opc && opc.TargetAreaId.HasValue)
        {
            var remoteFlowsheet = _project.GetFlowsheet(opc.TargetAreaId.Value);
            if (remoteFlowsheet != null)
            {
                var twinOpcRef = remoteFlowsheet.Elements
                    .OfType<IOffPageConnectorReference>()
                    .FirstOrDefault(e => e.ElementId == opc.TargetConnectorId);

                if (twinOpcRef != null)
                {
                    var twinPipe = remoteFlowsheet.Pipes.FirstOrDefault(p =>
                        p.SourceElementId == twinOpcRef.ElementId || p.TargetElementId == twinOpcRef.ElementId);

                    if (twinPipe != null)
                    {
                        var realStreamId = twinPipe.SourceElementId == twinOpcRef.ElementId
                            ? twinPipe.TargetElementId
                            : twinPipe.SourceElementId;
                        return _project.EquipmentRegistry.GetById(realStreamId);
                    }
                }
            }
        }

        return connectedElement;
    }

    /// <summary>
    /// Obtiene el IFacadeStream conectado a un puerto de equipo (versión conveniente).
    /// Resuelve pipes y OPCs inter-flowsheet.
    /// </summary>
    public IFacadeStream? GetFacadeForPort(IVisualElement parentEquipment, string portName)
    {
        var connected = GetConnectedElementForPort(parentEquipment, portName);
        return connected?.Facade as IFacadeStream;
    }

    /// <summary>
    /// Obtiene el IFacadeStream a partir de un ConnectedElementId (para contextos sin equipo padre).
    /// Primero busca en Elements locales, luego en el EquipmentRegistry del proyecto.
    /// </summary>
    public IFacadeStream? GetFacadeForConnectedId(Guid connectedElementId)
    {
        var element = _elements.FirstOrDefault(e => e.Id == connectedElementId)
            ?? _project?.EquipmentRegistry.GetById(connectedElementId);
        element = ResolveOffPageConnectorTarget(element);
        return element?.Facade as IFacadeStream;
    }

    // ==============================================================================
    // PRIVADOS
    // ==============================================================================
    private IVisualElement? GetElementById(Guid id)
    {
        return _elements.FirstOrDefault(e => e.Id == id)
            ?? _project?.EquipmentRegistry.GetById(id);
    }

    private IVisualElement? ResolveOffPageConnectorTarget(IVisualElement? element)
    {
        if (_project == null || element is not OffPageConnectorElement opc || !opc.TargetAreaId.HasValue)
        {
            return element;
        }

        var remoteFlowsheet = _project.GetFlowsheet(opc.TargetAreaId.Value);
        if (remoteFlowsheet == null) return element;

        var remoteConnectorId = opc.TargetConnectorId;
        if (!remoteConnectorId.HasValue) return element;

        var remotePipe = remoteFlowsheet.Pipes.FirstOrDefault(pipe =>
            pipe.SourceElementId == remoteConnectorId.Value ||
            pipe.TargetElementId == remoteConnectorId.Value);

        if (remotePipe == null) return element;

        var remoteElementId = remotePipe.SourceElementId == remoteConnectorId.Value
            ? remotePipe.TargetElementId
            : remotePipe.SourceElementId;

        return _project.EquipmentRegistry.GetById(remoteElementId) ?? element;
    }

    private IFlowsheet? FindFlowsheetForElement(Guid elementId)
    {
        return _project?.Flowsheets.FirstOrDefault(flowsheet =>
            flowsheet.Elements.Any(reference => reference.ElementId == elementId));
    }

    private bool IsStreamPortFree(Guid streamId, string streamPortName)
    {
        if (_project == null) return false;

        return !_project.Flowsheets
            .SelectMany(flowsheet => flowsheet.Pipes)
            .Any(pipe =>
                (pipe.SourceElementId == streamId && pipe.SourcePortName == streamPortName) ||
                (pipe.TargetElementId == streamId && pipe.TargetPortName == streamPortName));
    }

    private IEquipmentFactory GetEquipmentFactory()
    {
        if (_flowsheet != null) return _flowsheet.TypeDefinition.EquipmentFactory;
        return new PfdEquipmentFactory();
    }

    private IMainSolver GetSolver()
    {
        return _project?.SimulationService.Solver ?? new MainSolver();
    }

    private void SetUniqueElementName(IVisualElement element)
    {
        if (_project == null || _namingService == null) return;
        var typeCode = GetEquipmentTypeCode(element);

        // Usamos el nuevo método detallado
        var names = _namingService.GenerateNextNameDetails(typeCode, _project, _flowsheet);

        // Asignamos correctamente según tu regla de negocio
        element.Name = names.FullName;  // Ej: "100-P-101" (Para el sistema y reportes)
        element.Label = names.Label;    // Ej: "P-101" (Limpio para el canvas)

        if (element.Facade != null)
        {
            element.Facade.Name = names.FullName;
        }
    }

    private void RegisterElementInSolver(IVisualElement element)
    {
        var solver = GetSolver();
        if (element.Facade is IFacadeStream stream) solver.AddStream(stream);
        else if (element.Facade is ISolverEquipment eq) solver.AddEquipment(eq);
    }

    private static string GetEquipmentTypeCode(IVisualElement element)
    {
        return element.Type switch
        {
            EquipmentType.Pump => "Pump",
            EquipmentType.MaterialStream => "Stream",
            EquipmentType.Column => "Column",
            EquipmentType.FlashDrum => "FlashDrum",
            EquipmentType.Exchanger => "HeatExchanger",
            EquipmentType.PlateExchanger => "HeatExchanger",
            EquipmentType.Reboiler => "HeatExchanger",
            EquipmentType.ControlValve => "ControlValve",
            EquipmentType.Splitter => "Splitter",
            EquipmentType.Mixer => "Mixer",
            EquipmentType.Tank => "Tank",
            EquipmentType.Instrument => "Instrument",
            EquipmentType.OffPageConnector => "OffPageConnector",
            _ => element.Prefix
        };
    }

    private void SyncElementReferences()
    {
        if (_flowsheet == null) return;
        foreach (var element in _elements)
        {
            SyncElementReference(element);
        }
    }

    private void SyncElementReference(IVisualElement element)
    {
        if (_flowsheet == null) return;
        var reference = _flowsheet.GetElementReference(element.Id);
        if (reference == null) return;
        reference.X = element.X;
        reference.Y = element.Y;
        reference.RotationAngle = element.RotationAngle;
        reference.ZIndex = element.ZIndex;
        reference.IsFlippedHorizontal = element.IsFlippedHorizontal;
        reference.IsFlippedVertical = element.IsFlippedVertical;
    }

    private void UpdateDiagramSize()
    {
        _canvasLayout.UpdateDiagramSize(_elements, GlobalScale);
        if (_flowsheet != null)
        {
            _flowsheet.DiagramWidth = _canvasLayout.DiagramWidth;
            _flowsheet.DiagramHeight = _canvasLayout.DiagramHeight;
        }
    }

    private void ClampElementPosition(IVisualElement element)
    {
        var paperWidth = _canvasLayout.DiagramWidth;
        var paperHeight = _canvasLayout.DiagramHeight;
        if (paperWidth <= 0 || paperHeight <= 0) return;

        var maxX = Math.Max(0, paperWidth - element.Width);
        var maxY = Math.Max(0, paperHeight - element.Height);

        element.X = Math.Clamp(element.X, 0, maxX);
        element.Y = Math.Clamp(element.Y, 0, maxY);
    }

    private void PersistCamera()
    {
        if (_flowsheet == null) return;
        // El servicio de dominio ya actualiza flowsheet.Zoom/PanX/PanY.
        // GlobalScale no se modifica por el servicio de dominio; se mantiene en el flowsheet.
    }

    private void RebuildPipes()
    {
        _pipes.Clear();
        if (_flowsheet == null) return;
        foreach (var pipeRef in _flowsheet.Pipes)
        {
            EnsureElementInVisualList(pipeRef.SourceElementId);
            EnsureElementInVisualList(pipeRef.TargetElementId);
            _pipes.Add(new PipeVisualElement
            {
                Id = pipeRef.Id,
                SourceElementId = pipeRef.SourceElementId,
                SourcePortName = pipeRef.SourcePortName,
                TargetElementId = pipeRef.TargetElementId,
                TargetPortName = pipeRef.TargetPortName,
                SourceElement = GetElementById(pipeRef.SourceElementId),
                TargetElement = GetElementById(pipeRef.TargetElementId),
                ShowTechnicalLabel = false
            });
        }
    }

    private void EnsureElementInVisualList(Guid elementId)
    {
        if (_project == null || _flowsheet == null || _elements.Any(e => e.Id == elementId)) return;

        var element = _project.EquipmentRegistry.GetById(elementId);
        if (element == null) return;

        var reference = _flowsheet.GetElementReference(elementId);
        if (reference != null)
        {
            element.X = reference.X;
            element.Y = reference.Y;
            element.RotationAngle = reference.RotationAngle;
            element.ZIndex = reference.ZIndex;
            element.IsFlippedHorizontal = reference.IsFlippedHorizontal;
            element.IsFlippedVertical = reference.IsFlippedVertical;
        }

        _elements.Add(element);
    }

    private IConnectionService? CreateConnectionService()
    {
        if (_project == null || _flowsheet == null) return null;

        return new ConnectionService(
            _flowsheet.TypeDefinition.ConnectionRules,
            _placementRules,
            _namingService,
            _flowsheet.TypeDefinition.EquipmentFactory,
            _project.SimulationService);
    }

    /// <summary>
    /// Placeholder que evita NRE si aún no hay proyecto cargado.
    /// No ejecuta nada; el solver real se obtiene del proyecto.
    /// </summary>
    //private class NullSolver : IMainSolver
    //{
    //    public List<IFacadeStream> Streams { get; } = new();
    //    public List<ISolverEquipment> Equipments { get; } = new();
    //    public Length Altitude { get; set; } = new(0, LengthUnits.Meter);
    //    public Pressure AtmosphericPressure { get; set; } = new(101325, PressureUnits.Pascala);
    //    public ThermodynamicMethodFullDto ThermoMethod { get; set; } = null!;
    //    public event Action? OnSimulationCompleted;
    //    public void AddStream(IFacadeStream stream) { }
    //    public void AddEquipment(ISolverEquipment equipment) { }
    //    public void ClearOrphanStream(IFacadeStream stream) { }
    //    public void RunSimulation() { }
    //    public void RemoveStream(IFacadeStream stream) { }
    //    public void RemoveEquipment(ISolverEquipment equipment) { }
    //}
}
