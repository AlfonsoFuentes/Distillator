using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.WorkSpaceManagers;
namespace Client.Services.EquipmentManagers
{
    public class WorkspaceManager
    {
        private readonly IEquipmentFactory _factory;

        // ==============================================================================
        // ESTADO DEL LIENZO Y COLECCIONES
        // ==============================================================================
        public List<DiagramArea> Areas { get; } = new();
        public DiagramArea ActiveArea { get; private set; }

        public List<IVisualElement> Elements => ActiveArea.Elements;
        public List<PipeVisualElement> Pipes => ActiveArea.Pipes;
        public IVisualElement? SelectedElement { get; private set; }

        // ==============================================================================
        // TRANSFORMACIONES DE VISTA (Pan & Zoom)
        // ==============================================================================
        public double Zoom { get; set; } = 1.0;
        public double PanX { get; set; } = 0;
        public double PanY { get; set; } = 0;

        public bool IsPanning => _isPanning;

        public string WorkspaceCssClass
        {
            get
            {
                var css = "pfd-workspace";
                if (IsPanning) css += " is-panning";
                return css;
            }
        }

        public string CameraTransform =>
            string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"translate({Math.Round(PanX)}px, {Math.Round(PanY)}px) scale({Zoom})");

        public string WorkspaceBackgroundStyle =>
            string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"background-position: {Math.Round(PanX)}px {Math.Round(PanY)}px; " +
            $"background-size: {100 * Zoom}px {100 * Zoom}px, {100 * Zoom}px {100 * Zoom}px, {20 * Zoom}px {20 * Zoom}px, {20 * Zoom}px {20 * Zoom}px;");

        public string PaperStyle =>
            string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"width: {Math.Round(DiagramWidth)}px; height: {Math.Round(DiagramHeight)}px;");

        public const int GridSize = 20;

        private bool _isPanning;
        private double _lastPanMouseX;
        private double _lastPanMouseY;

        public event Action? OnNotifyUI;

        // ==============================================================================
        // ESTADO INTERNO DE MOVIMIENTO
        // ==============================================================================
        private IVisualElement? _movingElement;
        private double _lastMouseX, _lastMouseY;

        // 🚩 Esta propiedad le dice a la UI si estamos agarrando un equipo
        public bool IsMovingAny => _movingElement != null;
        public bool IsMoving(IVisualElement el) => _movingElement != null && _movingElement.Id == el.Id;

        // ==============================================================================
        // ESTADO DE MODO CONEXIÓN
        // ==============================================================================
        public bool IsConnectionModeActive { get; private set; }
        public PipeVisualElement? CurrentDraftPipe { get; private set; }
        public double DraftMouseLogicalX { get; private set; }
        public double DraftMouseLogicalY { get; private set; }
        private double _originalDragX;
        private double _originalDragY;

        public WorkspaceManager(IEquipmentFactory factory)
        {
            _factory = factory;

            var defaultArea = new DiagramArea { Name = "Main Area" };
            Areas.Add(defaultArea);
            ActiveArea = defaultArea;

            DiagramWidth = 3000;
            DiagramHeight = 2000;
        }

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

            PanX = 0;
            PanY = 0;
            Zoom = 1.0;

            UpdateDiagramSize();
            NotifyStateChanged();
        }

        public void RenameArea(DiagramArea area, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName) || area == null) return;
            area.Name = newName;
            NotifyStateChanged();
        }

        public void DeleteArea(DiagramArea area)
        {
            if (area == null || Areas.Count <= 1) return;

            Areas.Remove(area);
            if (ActiveArea.Id == area.Id) SwitchToArea(Areas.First());
            else NotifyStateChanged();
        }

        public void ReorderArea(DiagramArea areaToMove, DiagramArea targetArea)
        {
            if (areaToMove == null || targetArea == null || areaToMove.Id == targetArea.Id) return;

            int oldIndex = Areas.IndexOf(areaToMove);
            int newIndex = Areas.IndexOf(targetArea);

            if (oldIndex == -1 || newIndex == -1) return;

            Areas.RemoveAt(oldIndex);
            Areas.Insert(newIndex, areaToMove);
            NotifyStateChanged();
        }

        public void AddFromToolbox(EquipmentType type, double offsetX, double offsetY)
        {
            if (type == EquipmentType.None) return;

            double logicalX = (offsetX - PanX) / Zoom;
            double logicalY = (offsetY - PanY) / Zoom;

            var el = _factory.Create(type, logicalX, logicalY, Snap);

            if (el != null)
            {
                if (HasEquipmentCollision(el, el.X, el.Y)) return;
                Elements.Add(el);
                UpdateDiagramSize();
                NotifyStateChanged();
            }
        }

        public void SelectElement(IVisualElement element)
        {
            SelectedElement = element;
            NotifyStateChanged();
        }

        // ==============================================================================
        // LÓGICA DE MOVIMIENTO DE EQUIPOS
        // ==============================================================================
        public void StartMove(IVisualElement el, MouseEventArgs e)
        {
            if (IsConnectionModeActive || e.Button != 0) return;

            _movingElement = el;
            _lastMouseX = e.ClientX;
            _lastMouseY = e.ClientY;
            _originalDragX = el.X;
            _originalDragY = el.Y;

            SelectElement(el);
        }

        public void Move(MouseEventArgs e)
        {
            if (_movingElement == null) return;

            _movingElement.X += (e.ClientX - _lastMouseX) / Zoom;
            _movingElement.Y += (e.ClientY - _lastMouseY) / Zoom;

            _lastMouseX = e.ClientX;
            _lastMouseY = e.ClientY;

            NotifyStateChanged();
        }

        public void EndMove()
        {
            if (_movingElement != null)
            {
                if (HasEquipmentCollision(_movingElement, _movingElement.X, _movingElement.Y))
                {
                    _movingElement.X = _originalDragX;
                    _movingElement.Y = _originalDragY;
                }

                _movingElement.X = Snap(_movingElement.X);
                _movingElement.Y = Snap(_movingElement.Y);
                _movingElement = null; // Liberamos el equipo
                UpdateDiagramSize();
                NotifyStateChanged();
            }
        }

        // ==============================================================================
        // 🚩 LÓGICA DE NAVEGACIÓN (PAN) CORREGIDA
        // ==============================================================================
        public void StartPan(MouseEventArgs e)
        {
            // Si acabas de hacer clic en un equipo, no inicies el movimiento de cámara
            if (IsMovingAny || IsConnectionModeActive) return;

            // 🚩 Solución final: Botón 0 (Izquierdo) o Botón 1 (Rueda) sobre el fondo gris
            if (e.Button == 0 || e.Button == 1)
            {
                _isPanning = true;
                _lastPanMouseX = e.ClientX;
                _lastPanMouseY = e.ClientY;
                NotifyStateChanged();
            }
        }

        public void Pan(MouseEventArgs e)
        {
            if (!_isPanning) return;

            PanX += (e.ClientX - _lastPanMouseX);
            PanY += (e.ClientY - _lastPanMouseY);

            _lastPanMouseX = e.ClientX;
            _lastPanMouseY = e.ClientY;

            NotifyStateChanged();
        }

        public void EndPan()
        {
            _isPanning = false;
            NotifyStateChanged();
        }

        public void ZoomAt(double deltaY, double pointerX, double pointerY)
        {
            double zoomFactor = deltaY > 0 ? 0.9 : 1.1;
            double newZoom = Zoom * zoomFactor;

            if (newZoom < 0.2) newZoom = 0.2;
            if (newZoom > 3.0) newZoom = 3.0;

            double logicalX = (pointerX - PanX) / Zoom;
            double logicalY = (pointerY - PanY) / Zoom;

            Zoom = newZoom;
            PanX = pointerX - (logicalX * Zoom);
            PanY = pointerY - (logicalY * Zoom);

            NotifyStateChanged();
        }

        public void ZoomToFit(double screenWidth, double screenHeight)
        {
            if (Elements.Count == 0)
            {
                PanX = 0; PanY = 0; Zoom = 1.0;
                NotifyStateChanged();
                return;
            }

            double minX = Elements.Min(e => e.X);
            double maxX = Elements.Max(e => e.X + e.Width);
            double minY = Elements.Min(e => e.Y);
            double maxY = Elements.Max(e => e.Y + e.Height);

            double contentWidth = (maxX - minX) * 1.2;
            double contentHeight = (maxY - minY) * 1.2;

            double zoomX = screenWidth / contentWidth;
            double zoomY = screenHeight / contentHeight;

            Zoom = Math.Min(zoomX, zoomY);
            if (Zoom > 1.5) Zoom = 1.5;
            if (Zoom < 0.2) Zoom = 0.2;

            double centerX = minX + ((maxX - minX) / 2.0);
            double centerY = minY + ((maxY - minY) / 2.0);

            PanX = (screenWidth / 2.0) - (centerX * Zoom);
            PanY = (screenHeight / 2.0) - (centerY * Zoom);

            NotifyStateChanged();
        }

        public double DiagramWidth { get; private set; } = 3000;
        public double DiagramHeight { get; private set; } = 2000;

        private void UpdateDiagramSize()
        {
            if (Elements.Count == 0)
            {
                DiagramWidth = 3000;
                DiagramHeight = 2000;
                return;
            }

            double maxX = Elements.Max(e => e.X + e.Width);
            double maxY = Elements.Max(e => e.Y + e.Height);

            double newWidth = Snap(maxX + 200);
            double newHeight = Snap(maxY + 200);

            DiagramWidth = Math.Max(3000, newWidth);
            DiagramHeight = Math.Max(2000, newHeight);
        }

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
            DraftMouseLogicalX = (clientX - PanX) / Zoom;
            DraftMouseLogicalY = (clientY - PanY) / Zoom;
            NotifyStateChanged();
        }

        public void CancelConnectionDraft()
        {
            CurrentDraftPipe = null;
            NotifyStateChanged();
        }
        public void CompleteConnection2(IVisualElement target, string targetPortName)
        {
            if (CurrentDraftPipe == null || CurrentDraftPipe.SourceElement == null) return;

            // ✅ VALIDACIÓN DEFENSIVA
            if (!CurrentDraftPipe.SourceElement.CanConnect(
                    CurrentDraftPipe.SourcePortName, target, targetPortName))
            {
                CancelConnectionDraft(); // Rechazar conexión inválida
                return;
            }

            if (CurrentDraftPipe.SourceElementId == target.Id)
            {
                CancelConnectionDraft();
                return;
            }

            // ... resto del código igual ...
        }
        public void CompleteConnection(IVisualElement target, string targetPortName)
        {
            if (CurrentDraftPipe == null || CurrentDraftPipe.SourceElement == null) return;

            if (!CurrentDraftPipe.SourceElement.CanConnect(
                    CurrentDraftPipe.SourcePortName, target, targetPortName))
            {
                CancelConnectionDraft();
                return;
            }
            if (CurrentDraftPipe.SourceElementId == target.Id)
            {
                CancelConnectionDraft();
                return;
            }
            CurrentDraftPipe.TargetElementId = target.Id;
            CurrentDraftPipe.TargetPortName = targetPortName;
            CurrentDraftPipe.TargetElement = target;

            var sourcePort = CurrentDraftPipe.SourceElement.Ports.FirstOrDefault(p => p.Name == CurrentDraftPipe.SourcePortName);
            var targetPort = target.Ports.FirstOrDefault(p => p.Name == targetPortName);

            if (sourcePort != null) sourcePort.ConnectedElementId = CurrentDraftPipe.Id;
            if (targetPort != null) targetPort.ConnectedElementId = CurrentDraftPipe.Id;

            Pipes.Add(CurrentDraftPipe);
            CurrentDraftPipe = null;
            SetConnectionMode(false);
            NotifyStateChanged();
        }

        public double Snap(double val) => Math.Round(val / GridSize) * GridSize;
        private void NotifyStateChanged() => OnNotifyUI?.Invoke();

        private bool HasEquipmentCollision(IVisualElement movingElement, double checkX, double checkY)
        {
            var box1 = GetBoundingBox(movingElement, checkX, checkY);
            foreach (var other in Elements)
            {
                if (other.Id == movingElement.Id) continue;
                var box2 = GetBoundingBox(other, other.X, other.Y);
                if (box1.X < box2.X + box2.Width && box1.X + box1.Width > box2.X &&
                    box1.Y < box2.Y + box2.Height && box1.Y + box1.Height > box2.Y) return true;
            }
            return false;
        }

        private (double X, double Y, double Width, double Height) GetBoundingBox(IVisualElement el, double posX, double posY)
        {
            double w = el.Width; double h = el.Height; int rot = el.RotationAngle % 360;
            double screenW = (rot == 90 || rot == 270) ? h : w;
            double screenH = (rot == 90 || rot == 270) ? w : h;
            double dx = (w - screenW) / 2.0; double dy = (h - screenH) / 2.0;
            return (posX + dx + 2, posY + dy + 2, screenW - 4, screenH - 4);
        }
        public bool IsValidTarget(IVisualElement targetElement, string targetPortName)
        {
            // Si no hay draft, no hay origen → no hay objetivos válidos
            if (CurrentDraftPipe?.SourceElement == null)
                return false;

            var sourceElement = CurrentDraftPipe.SourceElement;
            var sourcePortName = CurrentDraftPipe.SourcePortName;

            // Delegamos a la lógica de negocio del elemento origen
            return sourceElement.CanConnect(sourcePortName, targetElement, targetPortName);
        }
    }


}