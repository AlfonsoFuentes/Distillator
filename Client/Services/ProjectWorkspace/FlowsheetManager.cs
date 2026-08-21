using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.ProcessFlowDiagram.Streams;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverConsecutive.SolverRemanufactured;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using Shared.WorkSpaceManagers;
using System.Threading;
using System.Runtime.CompilerServices;
using UnitSystem;
using Client.Services.Diagnostics;
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
    private readonly ProjectActivityLogService _activityLog;
    private readonly FlowsheetEditChangePolicy _editChangePolicy = new();
    private readonly FlowsheetEquipmentEditService _equipmentEditService = new();
    private readonly FlowsheetConnectionEditService _connectionEditService = new();

    private Project? _project;
    private IFlowsheet? _flowsheet;
    private IConnectionService? _connectionService;
    private IMainSolver? _subscribedSolver;
    private Action? _solverCompletionHandler;
    private long _solverSubscriptionVersion;
    private List<IVisualElement> _elements = new();
    private List<PipeVisualElement> _pipes = new();
    private readonly List<IVisualElement> _selectedElements = new();

    private bool _isPanning;
    private int _runningSimulations;
    private bool _visualSavePendingAfterSimulation;
    private readonly Dictionary<Guid, IFlowsheet> _pendingVisualSaveFlowsheets = new();
    private readonly Dictionary<Guid, IFlowsheet> _queuedVisualSaveFlowsheets = new();
    private readonly object _visualSaveSync = new();
    private CancellationTokenSource? _visualStateNotificationDebounce;
    private double _lastPanMouseX;
    private double _lastPanMouseY;
    private long _routeGeometryVersion;
    private const int VisualStateNotificationDebounceMs = 1500;
    private const double OffPageConnectorOffset = 60;

    public FlowsheetManager(
        ICameraService cameraService,
        IPlacementRules placementRules,
        IEquipmentNamingService namingService,
        FlowsheetCanvasLayoutService canvasLayout,
        FlowsheetStyleService styleService,
        EquipmentDragService drag,
        ProjectActivityLogService activityLog)
    {
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _placementRules = placementRules ?? throw new ArgumentNullException(nameof(placementRules));
        _namingService = namingService ?? throw new ArgumentNullException(nameof(namingService));
        _canvasLayout = canvasLayout ?? throw new ArgumentNullException(nameof(canvasLayout));
        _styleService = styleService ?? throw new ArgumentNullException(nameof(styleService));
        _drag = drag ?? throw new ArgumentNullException(nameof(drag));
        _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
    }

    // ==============================================================================
    // ESTADO PÚBLICO
    // ==============================================================================
    public Project? CurrentProject => _project;
    public IFlowsheet? CurrentFlowsheet => _flowsheet;
    public IVisualElement? SelectedElement { get; private set; }
    public IReadOnlyCollection<IVisualElement> SelectedElements => _selectedElements.AsReadOnly();
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
    public bool IsMoving(Guid elementId) => _elements.FirstOrDefault(element => element.Id == elementId) is { } element && _drag.IsMoving(element);
    public bool IsSelected(IVisualElement el) => _selectedElements.Any(selected => selected.Id == el.Id);
    public long RouteGeometryVersion => Interlocked.Read(ref _routeGeometryVersion);
    public bool IsSimulationRunning => Interlocked.CompareExchange(ref _runningSimulations, 0, 0) > 0;
    public bool HasActiveVisualOperation => IsMovingAny || IsPanning || CurrentDraftPipe != null;
    public Func<bool>? CanEditCurrentProject { get; set; }

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
        ResetVisualSaveQueues();
        _project = project;
        project.SimulationService.Solver.TraceSink = _activityLog;
        _namingService.SetConfiguration(project.Configuration.NamingConfig);
        SubscribeToSolver(project.SimulationService.Solver);
    }

    public void LoadFlowsheet(IFlowsheet flowsheet)
    {
        ResetVisualSaveQueues();
        _flowsheet = flowsheet;
        _elements.Clear();
        _pipes.Clear();
        SelectedElement = null;
        _selectedElements.Clear();
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
                opc.PortSide = opcReference.PortSide;
                opc.TargetAreaId = opcReference.TargetFlowsheetId;
                opc.TargetConnectorId = opcReference.TargetConnectorId;
                opc.TargetAreaName = opcReference.TargetFlowsheetName;
                opc.ConnectedEquipmentName = ResolveRemoteConnectorEndpointName(opcReference);
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

        ReflowOffPageConnectors();
        UpdateDiagramSize();
        NotifyRouteGeometryChanged();
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
        ResetVisualSaveQueues();
        _flowsheet = null;
        _elements.Clear();
        _pipes.Clear();
        SelectedElement = null;
        _selectedElements.Clear();
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
        if (!CanEdit()) return;
        if (_project == null || _flowsheet == null) return;
        if (type == EquipmentType.None) return;

        var factory = GetEquipmentFactory();
        var element = factory.Create(type, offsetX, offsetY, _canvasLayout.Snap);
        if (element == null) return;

        SetUniqueElementName(element);
        // Resolver colisiones usando el dominio
        var (resolvedX, resolvedY) = _placementRules.ResolvePosition(
            element,
            _elements,
            _flowsheet.GridSize);
        element.X = resolvedX;
        element.Y = resolvedY;

        // Mantener el equipo dentro de la región cuadriculada (canvas).
        ClampElementPosition(element);

        if (!_equipmentEditService.TryAddEquipment(_project, _flowsheet, element)) return;
        _elements.Add(element);

        ReflowOffPageConnectors();
        UpdateDiagramSize();
        NotifyRouteGeometryChanged();
        MarkTopologicalStateChanged();
    }

    public void SelectElement(IVisualElement? element)
    {
        if (element != null &&
            _selectedElements.Count > 1 &&
            _selectedElements.Any(selected => selected.Id == element.Id))
        {
            SelectedElement = element;
            NotifyStateChanged();
            return;
        }

        SelectedElement = element;
        _selectedElements.Clear();
        if (element != null)
        {
            _selectedElements.Add(element);
        }

        NotifyStateChanged();
    }

    public void ToggleElementSelection(IVisualElement element)
    {
        var existing = _selectedElements.FirstOrDefault(selected => selected.Id == element.Id);
        if (existing != null)
        {
            _selectedElements.Remove(existing);
        }
        else
        {
            _selectedElements.Add(element);
        }

        SelectedElement = _selectedElements.LastOrDefault();
        NotifyStateChanged();
    }

    public void ClearSelection()
    {
        if (SelectedElement == null && _selectedElements.Count == 0) return;

        SelectedElement = null;
        _selectedElements.Clear();
        NotifyStateChanged();
    }

    public void StartMove(IVisualElement el, MouseEventArgs e)
    {
        if (!CanEdit()) return;
        _drag.StartMove(el, e, IsConnectionModeActive, SelectElement, _selectedElements);
    }

    public void Move(MouseEventArgs e)
    {
        if (!CanEdit()) return;
        _drag.Move(e, Zoom, GlobalScale, _canvasLayout.DiagramWidth, _canvasLayout.DiagramHeight);
        NotifyStateChanged();
    }

    public void EndMove()
    {
        if (!CanEdit()) return;
        var moved = _drag.EndMove(
            _elements,
            _flowsheet?.GridSize ?? _canvasLayout.GridSize,
            _canvasLayout.DiagramWidth,
            _canvasLayout.DiagramHeight);

        if (moved)
        {
            ReflowOffPageConnectors();
            SyncElementReferences();
            UpdateDiagramSize();
            NotifyRouteGeometryChanged();
            MarkVisualStateChanged();
        }
    }

    public void CancelMove()
    {
        if (!CanEdit()) return;
        _drag.CancelMove();
        SyncElementReferences();
        UpdateDiagramSize();
        NotifyRouteGeometryChanged();
        NotifyStateChanged();
    }

    public void RotateElement(IVisualElement element)
    {
        if (!CanEdit()) return;
        element.Rotate90();
        SyncElementReference(element);
        NotifyRouteGeometryChanged();
        MarkVisualStateChanged();
    }

    public void FlipElementHorizontal(IVisualElement element)
    {
        if (!CanEdit()) return;
        element.ToggleFlipHorizontal();
        SyncElementReference(element);
        NotifyRouteGeometryChanged();
        MarkVisualStateChanged();
    }

    public void FlipElementVertical(IVisualElement element)
    {
        if (!CanEdit()) return;
        element.ToggleFlipVertical();
        SyncElementReference(element);
        NotifyRouteGeometryChanged();
        MarkVisualStateChanged();
    }

    public void SetOffPageConnectorPortSide(OffPageConnectorElement connector, OffPageConnectorPortSide side)
    {
        if (!CanEdit()) return;
        connector.SetPortSide(side);
        SyncElementReference(connector);
        NotifyRouteGeometryChanged();
        MarkVisualStateChanged();
    }

    public void SetOffPageConnectorAnchorSide(OffPageConnectorElement connector, OffPageConnectorPortSide anchorSide)
    {
        if (!CanEdit()) return;

        var inwardPortSide = anchorSide == OffPageConnectorPortSide.Left
            ? OffPageConnectorPortSide.Right
            : OffPageConnectorPortSide.Left;

        connector.SetPortSide(inwardPortSide);
        ReflowOffPageConnectors();
        SyncElementReference(connector);
        NotifyRouteGeometryChanged();
        MarkVisualStateChanged();
    }

    public void DeleteElement(IVisualElement element)
    {
        if (!CanEdit()) return;
        if (_project == null || _flowsheet == null) return;

        if (!_equipmentEditService.TryDeleteEquipment(_project, _flowsheet, element, out var affectedFlowsheets)) return;
        _elements.Remove(element);
        if (SelectedElement?.Id == element.Id) SelectedElement = null;
        _selectedElements.RemoveAll(selected => selected.Id == element.Id);

        RebuildPipes();
        ReflowOffPageConnectors();
        UpdateDiagramSize();
        NotifyRouteGeometryChanged();
        MarkTopologicalStateChanged(affectedFlowsheets.ToArray());
    }

    // ==============================================================================
    // CÁMARA
    // ==============================================================================
    public void StartPan(MouseEventArgs e)
    {
        if (!CanEdit()) return;

        if (e.ShiftKey)
        {
            return;
        }

        if (e.Button == 0 && _selectedElements.Count > 0)
        {
            return;
        }

        if (!IsMovingAny && !IsConnectionModeActive && e.Button == 1)
        {
            _isPanning = true;
            _lastPanMouseX = e.ClientX;
            _lastPanMouseY = e.ClientY;
            NotifyStateChanged();
        }
    }

    public void Pan(MouseEventArgs e)
    {
        if (!CanEdit()) return;
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
        if (!CanEdit()) return;
        var wasPanning = _isPanning;
        _isPanning = false;
        if (wasPanning)
        {
            MarkVisualStateChanged();
            return;
        }

        NotifyStateChanged();
    }

    public void ZoomAt(double dY, double pX, double pY)
    {
        if (!CanEdit()) return;
        if (_flowsheet == null) return;
        _cameraService.ZoomAt(_flowsheet, dY, pX, pY);
        PersistCamera();
        MarkVisualStateChanged();
    }

    public void ZoomToFit(double screenWidth, double screenHeight)
    {
        if (!CanEdit()) return;
        if (_flowsheet == null) return;
        _cameraService.ZoomToFit(_flowsheet, screenWidth, screenHeight);
        PersistCamera();
        MarkVisualStateChanged();
    }

    public void SetContainerDimensions(double width, double height)
    {
        if (!CanEdit()) return;
        if (!_canvasLayout.SetContainerDimensions(width, height)) return;

        UpdateDiagramSize();

        NotifyStateChanged();
    }

    // ==============================================================================
    // CONEXIONES
    // ==============================================================================
    public void SetConnectionMode(bool isActive)
    {
        if (!CanEdit() && isActive) return;
        if (IsConnectionModeActive == isActive) return;
        IsConnectionModeActive = isActive;
        if (!isActive) CancelConnectionDraft();
        NotifyStateChanged();
    }

    public void StartConnectionDraft(IVisualElement source, string portName)
    {
        if (!CanEdit()) return;
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
        if (!CanEdit()) return;
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
        if (!CanEdit()) return;
        if (_project == null || _flowsheet == null || _connectionService == null || CurrentDraftPipe?.SourceElement == null) return;

        var source = CurrentDraftPipe.SourceElement;
        var sourcePortName = CurrentDraftPipe.SourcePortName;

        CurrentDraftPipe = null;
        IsConnectionModeActive = false;

        var pipe = _connectionEditService.TryConnect(
            _flowsheet,
            _connectionService,
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
            NotifyRouteGeometryChanged();
        }

        if (pipe == null)
        {
            NotifyStateChanged();
            return;
        }

        MarkTopologicalStateChanged();
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
    public void RunSimulation([CallerMemberName] string caller = "")
    {
        if (!CanEdit()) return;
        if (_project == null) return;

        _ = RunSimulationAndUpdateStateAsync(_project);
    }

    private async Task RunSimulationAndUpdateStateAsync(IProject project)
    {
        Interlocked.Increment(ref _runningSimulations);
        NotifyStateChanged();

        try
        {
            await project.RunSimulationAsync();
        }
        finally
        {
            CompleteTrackedSimulation();
        }
    }

    public void NotifyStateChanged() => OnNotifyUI?.Invoke();

    private void NotifyRouteGeometryChanged()
    {
        Interlocked.Increment(ref _routeGeometryVersion);
    }

    public void MarkFacadeStateChanged()
    {
        _activityLog.Add("Facade", "Stream data changed", _flowsheet?.Name);
        MarkVisualStateChanged();
    }

    private bool CanEdit()
    {
        return CanEditCurrentProject?.Invoke() ?? true;
    }

    private void MarkVisualStateChanged()
    {
        NotifyStateChanged();
        if (_editChangePolicy.ShouldPersistVisualState(FlowsheetEditChangeKind.Visual))
        {
            QueueVisualStateChanged();
        }
    }

    private void MarkTopologicalStateChanged(params IFlowsheet[] changedFlowsheets)
    {
        if (_editChangePolicy.ShouldRunSimulation(FlowsheetEditChangeKind.Topological))
        {
            RunSimulation();
        }

        NotifyStateChanged();
        if (_editChangePolicy.ShouldPersistVisualState(FlowsheetEditChangeKind.Topological))
        {
            QueueVisualStateChanged(changedFlowsheets);
        }
    }

    private void QueueVisualStateChanged(params IFlowsheet[] changedFlowsheets)
    {
        if (_flowsheet == null || OnVisualStatesChangedAsync == null) return;
        var flowsheets = changedFlowsheets.Length == 0 ? new[] { _flowsheet } : changedFlowsheets;

        if (Interlocked.CompareExchange(ref _runningSimulations, 0, 0) > 0)
        {
            var shouldLogWaiting = !_visualSavePendingAfterSimulation;
            _visualSavePendingAfterSimulation = true;
            lock (_visualSaveSync)
            {
                foreach (var flowsheet in flowsheets)
                {
                    _pendingVisualSaveFlowsheets[flowsheet.Id] = flowsheet;
                }
            }

            if (shouldLogWaiting)
            {
                _activityLog.Add("Autosave", "Waiting for simulation before save", string.Join(", ", flowsheets.Select(flowsheet => flowsheet.Name)));
            }
            return;
        }

        EnqueueVisualStateNotification(flowsheets);
    }

    private void EnqueueVisualStateNotification(IReadOnlyCollection<IFlowsheet> flowsheets)
    {
        if (flowsheets.Count == 0 || OnVisualStatesChangedAsync == null) return;

        CancellationTokenSource debounce;
        lock (_visualSaveSync)
        {
            foreach (var flowsheet in flowsheets.DistinctBy(candidate => candidate.Id))
            {
                _queuedVisualSaveFlowsheets[flowsheet.Id] = flowsheet;
            }

            _visualStateNotificationDebounce?.Cancel();
            _visualStateNotificationDebounce?.Dispose();
            debounce = new CancellationTokenSource();
            _visualStateNotificationDebounce = debounce;
        }

        _activityLog.StartAutosaveCountdown("Visual state notification", TimeSpan.FromMilliseconds(VisualStateNotificationDebounceMs));
        var token = debounce.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(VisualStateNotificationDebounceMs, token);
                await FlushQueuedVisualStateNotificationAsync(debounce);
            }
            catch (TaskCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                debounce.Dispose();
            }
        });
    }

    private async Task FlushQueuedVisualStateNotificationAsync(CancellationTokenSource debounce)
    {
        IFlowsheet[] changed;
        lock (_visualSaveSync)
        {
            if (!ReferenceEquals(_visualStateNotificationDebounce, debounce))
            {
                return;
            }

            changed = _queuedVisualSaveFlowsheets.Values.ToArray();
            _queuedVisualSaveFlowsheets.Clear();
            _visualStateNotificationDebounce = null;
        }

        if (changed.Length == 0 || OnVisualStatesChangedAsync == null) return;

        try
        {
            _activityLog.Add("Autosave", "Visual state notification sent", string.Join(", ", changed.Select(flowsheet => flowsheet.Name)));
            foreach (Func<IReadOnlyCollection<IFlowsheet>, Task> handler in
                     OnVisualStatesChangedAsync.GetInvocationList())
            {
                await handler(changed);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Visual autosave failed: {ex.Message}");
            _activityLog.CompleteAutosave($"Visual autosave notification failed: {ex.Message}", false);
        }
    }

    private void ResetVisualSaveQueues()
    {
        _visualSavePendingAfterSimulation = false;
        lock (_visualSaveSync)
        {
            _pendingVisualSaveFlowsheets.Clear();
            _queuedVisualSaveFlowsheets.Clear();
            _visualStateNotificationDebounce?.Cancel();
            _visualStateNotificationDebounce?.Dispose();
            _visualStateNotificationDebounce = null;
        }
    }

    private void SubscribeToSolver(IMainSolver solver)
    {
        if (_subscribedSolver == solver) return;

        if (_subscribedSolver != null && _solverCompletionHandler != null)
        {
            _subscribedSolver.OnSimulationCompleted -= _solverCompletionHandler;
        }

        Interlocked.Exchange(ref _runningSimulations, 0);
        ResetVisualSaveQueues();

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

        NotifyStateChanged();
    }

    private void CompleteTrackedSimulation()
    {
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

    public bool RenameStream(IFacadeStream stream, string newName)
    {
        if (!CanEdit()) return false;
        if (_project == null || stream == null || string.IsNullOrWhiteSpace(newName)) return false;

        var element = FindStreamElement(stream);
        if (element == null) return false;
        var facade = element.Facade;
        if (facade == null) return false;

        var trimmedName = newName.Trim();
        var oldElementName = element.Name;
        var oldLabel = element.Label;
        var oldFacadeName = facade.Name;
        if (string.Equals(oldFacadeName, trimmedName, StringComparison.Ordinal))
        {
            return true;
        }

        if (IsNameDuplicated(trimmedName, element.Id))
        {
            return false;
        }

        element.Name = trimmedName;
        element.Label = trimmedName;
        facade.Id = element.Id;
        facade.Name = trimmedName;

        var formulaUpdates = BuildFormulaRenameUpdates(stream);
        if (formulaUpdates == null)
        {
            element.Name = oldElementName;
            element.Label = oldLabel;
            facade.Name = oldFacadeName;
            return false;
        }

        foreach (var update in formulaUpdates)
        {
            update.Equipment.RemoveSpec(update.Original);
            update.Equipment.AddSpec(update.Replacement);
        }

        NotifyStateChanged();
        QueueVisualStateChanged();
        return true;
    }

    public bool IsStreamNameDuplicated(IFacadeStream stream, string name)
    {
        var element = FindStreamElement(stream);
        return element == null
            ? IsNameDuplicated(name, stream.Id)
            : IsNameDuplicated(name, element.Id);
    }

    private StreamVisualElement? FindStreamElement(IFacadeStream stream)
    {
        return _project?.EquipmentRegistry.AllEquipments
            .OfType<StreamVisualElement>()
            .FirstOrDefault(element =>
                ReferenceEquals(element.Facade, stream) ||
                element.Id == stream.Id ||
                element.Facade?.Id == stream.Id);
    }

    private bool IsNameDuplicated(string name, Guid currentElementId)
    {
        if (_project == null || string.IsNullOrWhiteSpace(name)) return false;

        return _project.EquipmentRegistry.AllEquipments.Any(element =>
            element.Id != currentElementId &&
            string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private List<FormulaRenameUpdate>? BuildFormulaRenameUpdates(IFacadeStream stream)
    {
        if (_project == null) return new List<FormulaRenameUpdate>();

        var updates = new List<FormulaRenameUpdate>();
        foreach (var equipment in _project.EquipmentRegistry.AllEquipments
                     .Select(element => element.Facade)
                     .OfType<SolverEquipmentBase>())
        {
            foreach (var specification in equipment.Specifications.OfType<FormulaSpecification>().ToList())
            {
                if (!FormulaUsesStream(specification, stream))
                {
                    continue;
                }

                var renamedFormula = specification.Equation.ToFormulaText();
                if (string.Equals(renamedFormula, specification.Formula, StringComparison.Ordinal))
                {
                    continue;
                }

                updates.Add(new FormulaRenameUpdate(
                    equipment,
                    specification,
                    new FormulaSpecification(renamedFormula, specification.Equation)
                    {
                        Id = specification.Id,
                        DefinedByUserId = specification.DefinedByUserId,
                        DefinedByUserName = specification.DefinedByUserName,
                        DefinedAtUtc = specification.DefinedAtUtc
                    }));
            }
        }

        return updates;
    }

    private static bool FormulaUsesStream(FormulaSpecification specification, IFacadeStream stream)
    {
        return specification.AssociatedStreams.Any(associated =>
            ReferenceEquals(associated, stream) || associated.Id == stream.Id);
    }

    private sealed record FormulaRenameUpdate(
        SolverEquipmentBase Equipment,
        FormulaSpecification Original,
        FormulaSpecification Replacement);

    // ==============================================================================
    // HELPERS PARA DIÁLOGOS DE EQUIPOS (reemplazan WM)
    // ==============================================================================

    /// <summary>
    /// Crea un StreamVisualElement programáticamente, lo registra en el proyecto
    /// y en el flowsheet actual, y lo añade a la lista visual.
    /// </summary>
    public StreamVisualElement? CreateStreamProgrammatically(string name)
    {
        if (!CanEdit()) return null;
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
        if (!CanEdit()) return null;
        if (_project == null || _flowsheet == null) return null;

        var port = equipment.Ports.FirstOrDefault(item => item.Name == portName);
        if (port == null) return null;

        var stream = CreateStreamProgrammatically(streamName);
        if (stream == null) return null;

        PositionStreamFromPort(equipment, port, stream);
        SyncElementReference(stream);
        NotifyRouteGeometryChanged();
        ConnectEquipmentToProjectStream(equipment, portName, stream);
        return stream;
    }

    /// <summary>
    /// Conecta un equipo a un stream existente usando el SimulationService del dominio.
    /// </summary>
    public void ConnectEquipmentToStream(IVisualElement equipment, string portName, IVisualElement stream)
    {
        if (!CanEdit()) return;
        if (_project == null || _flowsheet == null) return;
        _project.SimulationService.ConnectEquipmentToStream(_project, _flowsheet, equipment, portName, stream);
        RebuildPipes();
        NotifyRouteGeometryChanged();
        RunSimulation();
        NotifyStateChanged();
        QueueVisualStateChanged();
    }

    /// <summary>
    /// Conecta un equipo al stream indicado, creando OPCs si el stream vive en otro flowsheet.
    /// </summary>
    public void ConnectEquipmentToProjectStream(IVisualElement equipment, string portName, IVisualElement stream)
    {
        if (!CanEdit()) return;
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
        NotifyRouteGeometryChanged();
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

        switch (absCoords.Direction)
        {
            case PortDirection.Top: dy = -spawnDistance; break;
            case PortDirection.Bottom: dy = spawnDistance; break;
            case PortDirection.Left: dx = -spawnDistance; break;
            case PortDirection.Right: dx = spawnDistance; break;
        }

        var isInlet = port.Type == PortType.Inlet;
        stream.RotationAngle = GetStreamRotationForEquipmentPort(port.Type, absCoords.Direction);

        var streamPortName = isInlet ? "Outlet" : "Inlet";
        var (streamOffsetX, streamOffsetY, _) = stream.GetTransformedPort(streamPortName);

        stream.X = _canvasLayout.Snap((absCoords.X + dx) - streamOffsetX);
        stream.Y = _canvasLayout.Snap((absCoords.Y + dy) - streamOffsetY);
        ClampElementPosition(stream);
    }

    private static int GetStreamRotationForEquipmentPort(PortType portType, PortDirection portDirection)
    {
        var streamDirection = portType == PortType.Inlet
            ? OppositeDirection(portDirection)
            : portDirection;

        return streamDirection switch
        {
            PortDirection.Right => 0,
            PortDirection.Bottom => 90,
            PortDirection.Left => 180,
            PortDirection.Top => 270,
            _ => 0
        };
    }

    private static PortDirection OppositeDirection(PortDirection direction) => direction switch
    {
        PortDirection.Right => PortDirection.Left,
        PortDirection.Left => PortDirection.Right,
        PortDirection.Bottom => PortDirection.Top,
        PortDirection.Top => PortDirection.Bottom,
        _ => direction
    };

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
        if (!CanEdit()) return;
        if (_project == null || _flowsheet == null) return;

        if (!_connectionEditService.TryDisconnectPort(_project, _flowsheet, equipment, portName, out var affectedFlowsheets))
        {
            NotifyStateChanged();
            return;
        }

        RebuildPipes();
        NotifyRouteGeometryChanged();
        MarkTopologicalStateChanged(affectedFlowsheets.ToArray());
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
        return _project?.SimulationService.Solver ?? new MainSolverRemanufactured();
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

        if (element is OffPageConnectorElement connector &&
            reference is OffPageConnectorReference connectorReference)
        {
            connectorReference.IsOutlet = connector.IsOutlet;
            connectorReference.PortSide = connector.PortSide;
            connectorReference.TargetFlowsheetId = connector.TargetAreaId;
            connectorReference.TargetConnectorId = connector.TargetConnectorId;
            connectorReference.TargetFlowsheetName = connector.TargetAreaName;
            connectorReference.ConnectedEquipmentName = connector.ConnectedEquipmentName;
        }
    }

    private bool ReflowOffPageConnectors()
    {
        if (_flowsheet == null) return false;

        var connectors = _elements
            .OfType<OffPageConnectorElement>()
            .ToList();
        if (connectors.Count == 0)
        {
            return false;
        }

        var leftX = _canvasLayout.Snap(OffPageConnectorOffset);
        var rightX = GetRightSideOpcX();
        var changed = false;

        foreach (var connector in connectors)
        {
            var anchorSide = GetOffPageConnectorAnchorSide(connector);
            var targetX = anchorSide == OffPageConnectorPortSide.Left
                ? leftX
                : rightX;

            if (Math.Abs(connector.X - targetX) <= 0.001)
            {
                continue;
            }

            connector.X = targetX;
            SyncElementReference(connector);
            changed = true;
        }

        return changed;
    }

    private double GetRightSideOpcX()
    {
        var rightMostRealElement = _elements
            .Where(element => element is not OffPageConnectorElement)
            .Select(element => element.X + Math.Max(element.Width, 100))
            .DefaultIfEmpty(0)
            .Max();

        return Math.Max(
            _canvasLayout.Snap(OffPageConnectorOffset),
            _canvasLayout.Snap(rightMostRealElement + OffPageConnectorOffset));
    }

    private static OffPageConnectorPortSide GetOffPageConnectorAnchorSide(OffPageConnectorElement connector) =>
        connector.PortSide == OffPageConnectorPortSide.Right
            ? OffPageConnectorPortSide.Left
            : OffPageConnectorPortSide.Right;

    private string ResolveRemoteConnectorEndpointName(IOffPageConnectorReference reference)
    {
        if (_project == null ||
            !reference.TargetFlowsheetId.HasValue ||
            !reference.TargetConnectorId.HasValue)
        {
            return reference.ConnectedEquipmentName;
        }

        var targetFlowsheet = _project.GetFlowsheet(reference.TargetFlowsheetId.Value);
        if (targetFlowsheet == null)
        {
            return reference.ConnectedEquipmentName;
        }

        var pipe = targetFlowsheet.Pipes.FirstOrDefault(candidate =>
            candidate.SourceElementId == reference.TargetConnectorId.Value ||
            candidate.TargetElementId == reference.TargetConnectorId.Value);
        if (pipe == null)
        {
            return reference.ConnectedEquipmentName;
        }

        var remoteElementId = pipe.SourceElementId == reference.TargetConnectorId.Value
            ? pipe.TargetElementId
            : pipe.SourceElementId;
        var remoteElement = _project.EquipmentRegistry.GetById(remoteElementId);

        return string.IsNullOrWhiteSpace(remoteElement?.Label)
            ? reference.ConnectedEquipmentName
            : remoteElement.Label;
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
        RemoveStaleVisualElements();

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

    private void RemoveStaleVisualElements()
    {
        if (_flowsheet == null) return;

        var activeElementIds = _flowsheet.Elements
            .Select(reference => reference.ElementId)
            .ToHashSet();

        _elements.RemoveAll(element => !activeElementIds.Contains(element.Id));
        if (SelectedElement != null && !activeElementIds.Contains(SelectedElement.Id))
        {
            SelectedElement = null;
        }
        _selectedElements.RemoveAll(element => !activeElementIds.Contains(element.Id));
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
