
using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using Shared.WorkSpaceManagers;

//namespace Client.Services.EquipmentManagers
//{
//    public class EquipmentMovementService
//    {
//        private readonly IEquipmentFactory _factory;
//        private readonly IMainSolver _solver;
//        private readonly CameraService _camera;
//        private readonly CanvasSizeService _canvasSize;

//        public EquipmentMovementService(IEquipmentFactory factory, IMainSolver solver, CameraService camera, CanvasSizeService canvasSize)
//        {
//            _factory = factory;
//            _solver = solver;
//            _camera = camera;
//            _canvasSize = canvasSize;
//        }

//        private IVisualElement? _movingElement;
//        private double _lastMouseX, _lastMouseY;
//        private double _originalDragX, _originalDragY;

//        public bool IsMovingAny => _movingElement != null;
//        public bool IsMoving(IVisualElement el) => _movingElement != null && _movingElement.Id == el.Id;

//        public void StartMove(IVisualElement el, MouseEventArgs e, bool isConnectionModeActive, Action<IVisualElement> onSelectElement)
//        {
//            if (isConnectionModeActive || e.Button != 0) return;

//            _movingElement = el;
//            _lastMouseX = e.ClientX;
//            _lastMouseY = e.ClientY;
//            _originalDragX = el.X;
//            _originalDragY = el.Y;
//            onSelectElement(el);
//        }

//        public void Move(MouseEventArgs e)
//        {
//            if (_movingElement == null) return;
//            double effectiveScale = _camera.Zoom * _camera.GlobalScale;
//            if (_movingElement.AllowFreeDragX)
//                _movingElement.X += (e.ClientX - _lastMouseX) / effectiveScale;
//            if (_movingElement.AllowFreeDragY)
//                _movingElement.Y += (e.ClientY - _lastMouseY) / effectiveScale;
//            _lastMouseX = e.ClientX;
//            _lastMouseY = e.ClientY;
//        }

//        public bool EndMove(List<IVisualElement> elements)
//        {
//            if (_movingElement != null)
//            {
//                if (HasEquipmentCollision(_movingElement, _movingElement.X, _movingElement.Y, elements))
//                {
//                    _movingElement.X = _originalDragX;
//                    _movingElement.Y = _originalDragY;
//                }
//                if (_movingElement.AllowFreeDragX)
//                    _movingElement.X = _canvasSize.Snap(_movingElement.X);
//                if (_movingElement.AllowFreeDragY)
//                    _movingElement.Y = _canvasSize.Snap(_movingElement.Y);
//                _movingElement = null;
//                return true;
//            }
//            return false;
//        }

//        public IVisualElement? AddFromToolbox(EquipmentType type, double offsetX, double offsetY, List<IVisualElement> elements)
//        {
//            if (type == EquipmentType.None) return null;
//            // 🔄 B5 FIX: offsetX/offsetY ya son coordenadas lógicas del mundo
//            // (PfdCanvas.GetRelativeMousePosition ya aplicó: (Client - canvas - Pan) / (Zoom * GlobalScale))
//            // NO aplicar transformaciones de cámara de nuevo para evitar desfase acumulativo.
//            double lx = offsetX;
//            double ly = offsetY;
//            var el = _factory.Create(type, lx, ly, _canvasSize.Snap);
//            if (el != null)
//            {
//                // 🔄 B5 UX: Si colisiona, desplazar automáticamente a la derecha hasta encontrar espacio libre.
//                // El canvas crece automáticamente (no hay tamaño máximo), así que siempre habrá espacio.
//                while (HasEquipmentCollision(el, el.X, el.Y, elements))
//                {
//                    el.X += el.Width + CanvasSizeService.GridSize; // Ancho del equipo + separación de 20px
//                }

//                elements.Add(el);
//                if (el.Facade != null)
//                {
//                    if (el.Facade is IFacadeStream s) _solver.AddStream(s);
//                    else if (el.Facade is ISolverEquipment eq) _solver.AddEquipment(eq);
//                }
//            }
//            return el;
//        }

//        private bool HasEquipmentCollision(IVisualElement moving, double cx, double cy, List<IVisualElement> elements)
//        {
//            var b1 = GetBoundingBox(moving, cx, cy);
//            foreach (var o in elements)
//            {
//                if (o.Id == moving.Id) continue;
//                var b2 = GetBoundingBox(o, o.X, o.Y);
//                if (b1.X < b2.X + b2.Width && b1.X + b1.Width > b2.X && b1.Y < b2.Y + b2.Height && b1.Y + b1.Height > b2.Y)
//                    return true;
//            }
//            return false;
//        }

//        private (double X, double Y, double Width, double Height) GetBoundingBox(IVisualElement el, double px, double py)
//        {
//            double w = el.Width; double h = el.Height;
//            int rot = el.RotationAngle % 360;
//            double sw = (rot == 90 || rot == 270) ? h : w;
//            double sh = (rot == 90 || rot == 270) ? w : h;
//            return (px + (w - sw) / 2.0 + 2, py + (h - sh) / 2.0 + 2, sw - 4, sh - 4);
//        }
//    }
//}