using Microsoft.AspNetCore.Components.Web;
using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.ProcessFlowDiagram.Streams;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using Shared.WorkSpaceManagers;


namespace Client.Services.EquipmentManagers
{


    public class EquipmentNamingService : INamingService
    {
        private readonly Dictionary<string, int> _counters = new();

        public string GenerateNextName(string prefix)
        {
            if (!_counters.ContainsKey(prefix)) _counters[prefix] = 101;
            return $"{prefix}-{_counters[prefix]++}";
        }
    }
    public class WorkspaceManager
    {
        public void SelectElement(IVisualElement element)
        {
            SelectedElement = element;
            NotifyStateChanged();
        }

        private readonly IEquipmentFactory _factory;
        //private readonly SolverMatrixManager2 _plantManager;

        // ==============================================================================
        // ESTADO INTERNO (RECUPERADO DE TU BACKUP)
        // ==============================================================================
        private bool _isPanning;
        private double _lastPanMouseX;
        private double _lastPanMouseY;
        private IVisualElement? _movingElement;
        private double _lastMouseX, _lastMouseY;
        private double _originalDragX;
        private double _originalDragY;

        // ==============================================================================
        // ESTADO DEL LIENZO Y COLECCIONES
        // ==============================================================================
        public List<DiagramArea> Areas { get; } = new();
        public DiagramArea ActiveArea { get; private set; }

        public List<IVisualElement> Elements => ActiveArea.Elements;
        public List<PipeVisualElement> Pipes => ActiveArea.Pipes;
        public IVisualElement? SelectedElement { get; private set; }

        public double Zoom { get; set; } = 1.0;
        public double PanX { get; set; } = 0;
        public double PanY { get; set; } = 0;
        public double DiagramWidth { get; private set; } = 3000;
        public double DiagramHeight { get; private set; } = 2000;
        public const int GridSize = 20;

        public bool IsPanning => _isPanning;
        public bool IsMovingAny => _movingElement != null;
        public bool IsMoving(IVisualElement el) => _movingElement != null && _movingElement.Id == el.Id;

        public Action? OnNotifyUI;

        // ESTADO DE MODO CONEXIÓN
        public bool IsConnectionModeActive { get; private set; }
        public PipeVisualElement? CurrentDraftPipe { get; private set; }
        public double DraftMouseLogicalX { get; private set; }
        public double DraftMouseLogicalY { get; private set; }

        public WorkspaceManager(IEquipmentFactory factory /*,SolverMatrixManager2 plantManager*/)
        {
            _factory = factory;
            //_plantManager = plantManager;

            var defaultArea = new DiagramArea { Name = "Main Area" };
            Areas.Add(defaultArea);
            ActiveArea = defaultArea;

            DiagramWidth = 3000;
            DiagramHeight = 2000;
        }

        // ==============================================================================
        // GESTIÓN DE ÁREAS
        // ==============================================================================
        public void CreateArea(string name)
        {
            var newArea = new DiagramArea { Name = name };
            Areas.Add(newArea);
            SwitchToArea(newArea);
        }

        public void SwitchToArea(DiagramArea area)
        {
            if (area == null || ActiveArea.Id == area.Id) return;
            ActiveArea = area;
            SelectedElement = null;
            CurrentDraftPipe = null;
            PanX = 0; PanY = 0; Zoom = 1.0;
            UpdateDiagramSize();
            NotifyStateChanged();
        }

        public void RenameArea(DiagramArea area, string newName)
        {
            if (area != null && !string.IsNullOrWhiteSpace(newName)) { area.Name = newName; NotifyStateChanged(); }
        }

        public void DeleteArea(DiagramArea area)
        {
            if (area == null || Areas.Count <= 1) return;
            Areas.Remove(area);
            if (ActiveArea.Id == area.Id) SwitchToArea(Areas.First());
            else NotifyStateChanged();
        }

        public void ReorderArea(DiagramArea move, DiagramArea target)
        {
            int oldIdx = Areas.IndexOf(move); int newIdx = Areas.IndexOf(target);
            if (oldIdx == -1 || newIdx == -1) return;
            Areas.RemoveAt(oldIdx); Areas.Insert(newIdx, move); NotifyStateChanged();
        }

        // ==============================================================================
        // LÓGICA DE EQUIPOS Y COLISIONES (FIEL A TU BACKUP)
        // ==============================================================================
        public void AddFromToolbox(EquipmentType type, double offsetX, double offsetY)
        {
            if (type == EquipmentType.None) return;
            double lx = (offsetX - PanX) / Zoom; double ly = (offsetY - PanY) / Zoom;
            var el = _factory.Create(type, lx, ly, Snap);
            if (el != null)
            {
                if (HasEquipmentCollision(el, el.X, el.Y)) return;
                Elements.Add(el);
                if (el.Facade != null)
                {
                    //if (el.Facade is IStreamFacade2 s) _plantManager.RegisterStream(s);
                    //else if (el.Facade is IEquipmentFacade2 e) _plantManager.RegisterEquipment(e);
                }
                UpdateDiagramSize();
                NotifyStateChanged();
            }
        }
        public void StartMove(IVisualElement el, MouseEventArgs e)
        {
            if (IsConnectionModeActive || e.Button != 0) return;

            // 🚩 REQUISITO: El OPC es estático
            if (el.Type == EquipmentType.OffPageConnector)
            {
                SelectElement(el); // 🚩 AÑADIR ESTO: Lo seleccionamos aunque no lo movamos
                return;
            }

            _movingElement = el;
            _lastMouseX = e.ClientX; _lastMouseY = e.ClientY;
            _originalDragX = el.X; _originalDragY = el.Y;
            SelectElement(el);
        }


        public void Move(MouseEventArgs e)
        {
            if (_movingElement == null) return;
            _movingElement.X += (e.ClientX - _lastMouseX) / Zoom;
            _movingElement.Y += (e.ClientY - _lastMouseY) / Zoom;
            _lastMouseX = e.ClientX; _lastMouseY = e.ClientY;
            NotifyStateChanged();
        }

        public void EndMove()
        {
            if (_movingElement != null)
            {
                if (HasEquipmentCollision(_movingElement, _movingElement.X, _movingElement.Y))
                {
                    _movingElement.X = _originalDragX; _movingElement.Y = _originalDragY;
                }
                _movingElement.X = Snap(_movingElement.X);
                _movingElement.Y = Snap(_movingElement.Y);
                _movingElement = null;
                UpdateDiagramSize();
                NotifyStateChanged();
            }
        }

        private bool HasEquipmentCollision(IVisualElement moving, double cx, double cy)
        {
            var b1 = GetBoundingBox(moving, cx, cy);
            foreach (var o in Elements)
            {
                if (o.Id == moving.Id) continue;
                var b2 = GetBoundingBox(o, o.X, o.Y);
                if (b1.X < b2.X + b2.Width && b1.X + b1.Width > b2.X && b1.Y < b2.Y + b2.Height && b1.Y + b1.Height > b2.Y) return true;
            }
            return false;
        }

        private (double X, double Y, double Width, double Height) GetBoundingBox(IVisualElement el, double px, double py)
        {
            double w = el.Width; double h = el.Height; int rot = el.RotationAngle % 360;
            double sw = (rot == 90 || rot == 270) ? h : w; double sh = (rot == 90 || rot == 270) ? w : h;
            return (px + (w - sw) / 2.0 + 2, py + (h - sh) / 2.0 + 2, sw - 4, sh - 4);
        }

        // ==============================================================================
        // GESTIÓN DE CONEXIONES (PIPES)
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
            DraftMouseLogicalX = portCoords.X; DraftMouseLogicalY = portCoords.Y;
            CurrentDraftPipe = new PipeVisualElement { Id = Guid.NewGuid(), SourceElementId = source.Id, SourcePortName = portName, SourceElement = source, Label = "Draft...", ShowTechnicalLabel = false };
            NotifyStateChanged();
        }

        public void UpdateConnectionDraft(double clientX, double clientY)
        {
            if (CurrentDraftPipe == null) return;
            DraftMouseLogicalX = (clientX - PanX) / Zoom; DraftMouseLogicalY = (clientY - PanY) / Zoom;
            NotifyStateChanged();
        }

        public void CancelConnectionDraft() { CurrentDraftPipe = null; NotifyStateChanged(); }
       
        public void CompleteConnection(IVisualElement target, string targetPortName)
        {
            if (CurrentDraftPipe == null || CurrentDraftPipe.SourceElement == null) return;
            if (!CurrentDraftPipe.SourceElement.CanConnect(CurrentDraftPipe.SourcePortName, target, targetPortName)) { CancelConnectionDraft(); return; }
            if (CurrentDraftPipe.SourceElementId == target.Id) { CancelConnectionDraft(); return; }

            CurrentDraftPipe.TargetElementId = target.Id;
            CurrentDraftPipe.TargetPortName = targetPortName;
            CurrentDraftPipe.TargetElement = target;

            var sP = CurrentDraftPipe.SourceElement.Ports.FirstOrDefault(p => p.Name == CurrentDraftPipe.SourcePortName);
            var tP = target.Ports.FirstOrDefault(p => p.Name == targetPortName);
            if (sP != null) sP.ConnectedElementId = CurrentDraftPipe.Id;
            if (tP != null) tP.ConnectedElementId = CurrentDraftPipe.Id;

            Pipes.Add(CurrentDraftPipe);

            // 🔥 NUEVA LÓGICA: Solo equipos llaman AttachConnection
            //if (CurrentDraftPipe.SourceElement.Facade is IEquipmentFacade2 sourceEquipment && target.Facade is IStreamFacade2 targetStream)
            //{
            //    sourceEquipment.AttachConnection(CurrentDraftPipe.SourcePortName, targetStream);
            //}
            //else if (target.Facade is IEquipmentFacade2 targetEquipment && CurrentDraftPipe.SourceElement.Facade is IStreamFacade2 sourceStream)
            //{
            //    targetEquipment.AttachConnection(targetPortName, sourceStream);
            //}
            // CanConnect ya garantiza que uno es equipo y otro stream, así que uno de los dos if se ejecutará

            CurrentDraftPipe = null;
            SetConnectionMode(false);
            NotifyStateChanged();
        }

        public bool IsValidTarget(IVisualElement target, string targetPortName)
        {
            if (CurrentDraftPipe?.SourceElement == null) return false;
            return CurrentDraftPipe.SourceElement.CanConnect(CurrentDraftPipe.SourcePortName, target, targetPortName);
        }
        public void CreateInterAreaConnection(IVisualElement localEquip, string localPortName, StreamVisualElement remStream, DiagramArea remArea)
        {
            var lPort = localEquip.Ports.FirstOrDefault(p => p.Name == localPortName);

            // 👉 TRUE = flujo entra al área actual (Succión)
            bool isFlowEnteringArea = lPort?.Type == PortType.Inlet;

            // Borde derecho actual
            double currentMaxX = Elements.Count > 0 ? Elements.Max(e => e.X + e.Width) : localEquip.X;

            // =========================================================
            // OPC LOCAL Y REMOTO (Visuales)
            // =========================================================
            var lOpc = new OffPageConnectorElement(isFlowEnteringArea ? false : true)
            {
                TargetAreaId = remArea.Id,
                TargetConnectorId = Guid.NewGuid(),
                Label = remArea.Name,

                // 🚩 AÑADIR ESTAS DOS LÍNEAS
                TargetAreaName = remArea.Name,
                ConnectedEquipmentName = localEquip.Label,

                X = isFlowEnteringArea ? 220 : Snap(currentMaxX + 400),
                Y = Snap(localEquip.Y)
            };

            var rOpc = new OffPageConnectorElement(isFlowEnteringArea)
            {
                TargetAreaId = ActiveArea.Id,
                TargetConnectorId = lOpc.Id,
                Id = lOpc.TargetConnectorId.Value,
                Label = ActiveArea.Name,

                // 🚩 AÑADIR ESTAS DOS LÍNEAS
                TargetAreaName = ActiveArea.Name,
                ConnectedEquipmentName = remStream.Label,

                X = isFlowEnteringArea ? Snap(currentMaxX + 400) : 220,
                Y = Snap(remStream.Y)
            };

            Elements.Add(lOpc);
            remArea.Elements.Add(rOpc);

            // =========================================================
            // 🚩 LA MAGIA DEL "WORMHOLE" (Agujero de Gusano Lógico)
            // =========================================================
            string remPortName = isFlowEnteringArea ? "Outlet" : "Inlet";

            // 1. Ocupar los puertos en la Interfaz Gráfica para que no se puedan reutilizar y se pongan grises
            if (lPort != null) lPort.ConnectedElementId = lOpc.Id;
            lOpc.Ports.First(p => p.Name == "Transfer").ConnectedElementId = localEquip.Id;

            var rPort = remStream.Ports.FirstOrDefault(p => p.Name == remPortName);
            if (rPort != null) rPort.ConnectedElementId = rOpc.Id;
            rOpc.Ports.First(p => p.Name == "Transfer").ConnectedElementId = remStream.Id;

            // 2. Conectar los Cerebros Termodinámicos DIRECTAMENTE
            //if (localEquip.Facade is IEquipmentFacade2 localEquipment && remStream.Facade is IStreamFacade2 remoteStream)
            //{
            //    localEquipment.AttachConnection(localPortName, remoteStream);
            //}
            //else if (remStream.Facade is IEquipmentFacade2 remoteEquipment && localEquip.Facade is IStreamFacade2 localStream)
            //{
            //    remoteEquipment.AttachConnection(remPortName, localStream);
            //}

            // =========================================================
            // PIPES VISUALES
            // =========================================================
            var pLocal = new PipeVisualElement { Id = Guid.NewGuid(), ShowTechnicalLabel = false };
            if (isFlowEnteringArea)
            {
                pLocal.SourceElement = lOpc;
                pLocal.TargetElement = localEquip;
                pLocal.SourcePortName = "Transfer";
                pLocal.TargetPortName = localPortName;
            }
            else
            {
                pLocal.SourceElement = localEquip;
                pLocal.TargetElement = lOpc;
                pLocal.SourcePortName = localPortName;
                pLocal.TargetPortName = "Transfer";
            }
            pLocal.SourceElementId = pLocal.SourceElement.Id;
            pLocal.TargetElementId = pLocal.TargetElement.Id;
            Pipes.Add(pLocal);

            var pRem = new PipeVisualElement { Id = Guid.NewGuid(), ShowTechnicalLabel = false };
            if (isFlowEnteringArea)
            {
                pRem.SourceElement = remStream;
                pRem.TargetElement = rOpc;
                pRem.SourcePortName = remPortName;
                pRem.TargetPortName = "Transfer";
            }
            else
            {
                pRem.SourceElement = rOpc;
                pRem.TargetElement = remStream;
                pRem.SourcePortName = "Transfer";
                pRem.TargetPortName = remPortName;
            }
            pRem.SourceElementId = pRem.SourceElement.Id;
            pRem.TargetElementId = pRem.TargetElement.Id;
            remArea.Pipes.Add(pRem);

            UpdateDiagramSize();
            NotifyStateChanged();
        }
        public StreamVisualElement? CreateStreamProgrammatically(string name)
        {
            // 1. Usamos la fábrica oficial para que construya la corriente y su Facade
            var stream = _factory.Create(EquipmentType.MaterialStream, 0, 0, Snap) as StreamVisualElement;

            if (stream != null)
            {
                // 2. Le asignamos el nombre ("S-101", etc.)
                stream.Name = name;
                stream.Label = name;

                if (stream.Facade != null)
                {
                    stream.Facade.Name = name; // Que el cerebro también sepa cómo se llama

                    // 🚩 3. ¡LA MAGIA! Registramos la corriente en el Solver
                    //if (stream.Facade is IStreamFacade2 s)
                    //{
                    //    _plantManager.RegisterStream(s);
                    //}
                }

                // 4. Lo añadimos al lienzo
                Elements.Add(stream);
            }

            return stream;
        }
        // ==============================================================================
        public void StartPan(MouseEventArgs e) { if (!IsMovingAny && !IsConnectionModeActive && (e.Button == 0 || e.Button == 1)) { _isPanning = true; _lastPanMouseX = e.ClientX; _lastPanMouseY = e.ClientY; NotifyStateChanged(); } }
        public void Pan(MouseEventArgs e) { if (_isPanning) { PanX += (e.ClientX - _lastPanMouseX); PanY += (e.ClientY - _lastPanMouseY); _lastPanMouseX = e.ClientX; _lastPanMouseY = e.ClientY; NotifyStateChanged(); } }
        public void EndPan() { _isPanning = false; NotifyStateChanged(); }
        public void ZoomAt(double dY, double pX, double pY) { double zF = dY > 0 ? 0.9 : 1.1; double nZ = Zoom * zF; if (nZ < 0.2) nZ = 0.2; if (nZ > 3.0) nZ = 3.0; double lX = (pX - PanX) / Zoom; double lY = (pY - PanY) / Zoom; Zoom = nZ; PanX = pX - (lX * Zoom); PanY = pY - (lY * Zoom); NotifyStateChanged(); }

        public void ZoomToFit(double screenWidth, double screenHeight)
        {
            if (Elements.Count == 0) { PanX = 0; PanY = 0; Zoom = 1.0; NotifyStateChanged(); return; }
            double minX = Elements.Min(e => e.X); double maxX = Elements.Max(e => e.X + e.Width);
            double minY = Elements.Min(e => e.Y); double maxY = Elements.Max(e => e.Y + e.Height);
            double cW = (maxX - minX) * 1.2; double cH = (maxY - minY) * 1.2;
            Zoom = Math.Min(screenWidth / cW, screenHeight / cH);
            if (Zoom > 1.5) Zoom = 1.5; if (Zoom < 0.2) Zoom = 0.2;

            // 🚩 Añadimos + 90 al final para que el centro visual considere la paleta
            PanX = (screenWidth / 2.0) - ((minX + (maxX - minX) / 2.0) * Zoom) + 90;
            PanY = (screenHeight / 2.0) - ((minY + (maxY - minY) / 2.0) * Zoom);
            NotifyStateChanged();
        }

        private void UpdateDiagramSize()
        {
            if (Elements.Count == 0) { DiagramWidth = 3000; DiagramHeight = 2000; return; }
            double maxX = Elements.Max(e => e.X + e.Width); double maxY = Elements.Max(e => e.Y + e.Height);
            DiagramWidth = Math.Max(3000, Snap(maxX + 400)); DiagramHeight = Math.Max(2000, Snap(maxY + 200));
        }

        public double Snap(double val) => Math.Round(val / GridSize) * GridSize;
        public void NotifyStateChanged() => OnNotifyUI?.Invoke();

        public string WorkspaceCssClass => _isPanning ? "pfd-workspace is-panning" : "pfd-workspace";
        public string CameraTransform => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"translate({Math.Round(PanX)}px, {Math.Round(PanY)}px) scale({Zoom})");
        public string WorkspaceBackgroundStyle => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"background-position: {Math.Round(PanX)}px {Math.Round(PanY)}px; background-size: {100 * Zoom}px {100 * Zoom}px, {100 * Zoom}px {100 * Zoom}px, {20 * Zoom}px {20 * Zoom}px, {20 * Zoom}px {20 * Zoom}px;");
        public string PaperStyle => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"width: {Math.Round(DiagramWidth)}px; height: {Math.Round(DiagramHeight)}px;");

        public void LoadFromDatabase() { }
        public void SaveToDatabase() { }
    }


}


