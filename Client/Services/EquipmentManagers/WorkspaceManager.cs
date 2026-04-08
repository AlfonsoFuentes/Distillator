using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
namespace Client.Services.EquipmentManagers
{



    public class WorkspaceManager
    {
        private readonly IEquipmentFactory _factory;

        // ==============================================================================
        // ESTADO DEL LIENZO Y COLECCIONES
        // ==============================================================================
        public List<IVisualElement> Elements { get; } = new();

        // 🚩 NUEVO: Separamos las tuberías de los equipos para renderizarlas en la capa SVG de fondo
        public List<PipeVisualElement> Pipes { get; } = new();

        public IVisualElement? SelectedElement { get; private set; }

        // ==============================================================================
        // TRANSFORMACIONES DE VISTA (Pan & Zoom)
        // ==============================================================================
        public double Zoom { get; set; } = 1.0;
        public double PanX { get; set; } = 0;
        public double PanY { get; set; } = 0;
        public const int GridSize = 20;

        // Notificación a la UI (Blazor)
        public event Action? OnNotifyUI;

        // ==============================================================================
        // ESTADO INTERNO DE MOVIMIENTO
        // ==============================================================================
        private IVisualElement? _movingElement;
        private double _lastMouseX, _lastMouseY;

        // ==============================================================================
        // ESTADO DE MODO CONEXIÓN (Fase 2)
        // ==============================================================================
        public bool IsConnectionModeActive { get; private set; }
        public PipeVisualElement? CurrentDraftPipe { get; private set; }

        // Coordenadas lógicas del mouse para dibujar la línea fantasma
        public double DraftMouseLogicalX { get; private set; }
        public double DraftMouseLogicalY { get; private set; }


        public WorkspaceManager(IEquipmentFactory factory) => _factory = factory;

        /// <summary>
        /// Informa a la UI si un equipo específico se está moviendo.
        /// </summary>
        public bool IsMoving(IVisualElement el) => _movingElement != null && _movingElement.Id == el.Id;

        /// <summary>
        /// Traduce coordenadas de pantalla a lógica y crea el equipo vía Factory
        /// </summary>
        public void AddFromToolbox(string type, double offsetX, double offsetY)
        {
            if (string.IsNullOrEmpty(type)) return;

            double logicalX = (offsetX - PanX) / Zoom;
            double logicalY = (offsetY - PanY) / Zoom;

            var el = _factory.Create(type, logicalX, logicalY, Snap);

            if (el != null)
            {
                Elements.Add(el);
                NotifyStateChanged();
            }
        }

        public void SelectElement(IVisualElement element)
        {
            if(element == null)  return;
            SelectedElement = element;
            NotifyStateChanged();
        }

        // ==============================================================================
        // LÓGICA DE MOVIMIENTO DE EQUIPOS
        // ==============================================================================

        public void StartMove(IVisualElement el, MouseEventArgs e)
        {
            // Si estamos en modo conexión, ignoramos el inicio de arrastre de equipos
            if (IsConnectionModeActive || e.Button != 0) return;

            _movingElement = el;
            _lastMouseX = e.ClientX;
            _lastMouseY = e.ClientY;

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
                _movingElement.X = Snap(_movingElement.X);
                _movingElement.Y = Snap(_movingElement.Y);

                _movingElement = null;
                NotifyStateChanged();
            }
        }

        // ==============================================================================
        // LÓGICA DE CONEXIÓN DE TUBERÍAS (Fase 2)
        // ==============================================================================

        /// <summary>
        /// Activa o desactiva el modo de conexión (gatillado por la tecla Ctrl)
        /// </summary>
        public void SetConnectionMode(bool isActive)
        {
            if (IsConnectionModeActive == isActive) return;

            IsConnectionModeActive = isActive;

            // Si el usuario suelta el Ctrl a mitad de un trazado, cancelamos la línea
            if (!isActive) CancelConnectionDraft();

            NotifyStateChanged();
        }

        public void StartConnectionDraft(IVisualElement source, string portName)
        {
            // 1. Obtenemos las coordenadas REALES del puerto en el mundo del canvas
            var portCoords = source.GetAbsolutePortCoordinates(portName);

            // 2. IMPORTANTÍSIMO: Sincronizamos el "ratón virtual" con el puerto ANTES de crear nada
            DraftMouseLogicalX = portCoords.X;
            DraftMouseLogicalY = portCoords.Y;

            // 3. Ahora sí, creamos el cable. Al nacer, su punta ya está en el puerto, no en 0,0.
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

        /// <summary>
        /// Actualiza la punta de la línea fantasma mientras el usuario mueve el mouse
        /// </summary>
        public void UpdateConnectionDraft(double clientX, double clientY)
        {
            if (CurrentDraftPipe == null) return;

            // Convertimos a coordenadas lógicas del plano
            DraftMouseLogicalX = (clientX - PanX) / Zoom;
            DraftMouseLogicalY = (clientY - PanY) / Zoom;

            NotifyStateChanged();
        }

        /// <summary>
        /// Cancela el borrador actual sin guardar nada
        /// </summary>
        public void CancelConnectionDraft()
        {
            CurrentDraftPipe = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Consuma la conexión cuando el usuario suelta el clic sobre un puerto destino válido
        /// </summary>
        public void CompleteConnection(IVisualElement target, string targetPortName)
        {
            if (CurrentDraftPipe == null || CurrentDraftPipe.SourceElement == null) return;

            // Evitar conectar al mismo equipo
            if (CurrentDraftPipe.SourceElementId == target.Id)
            {
                CancelConnectionDraft();
                return;
            }

            CurrentDraftPipe.TargetElementId = target.Id;
            CurrentDraftPipe.TargetPortName = targetPortName;
            CurrentDraftPipe.TargetElement = target;

            // ==============================================================
            // 🚩 CRÍTICO: MARCAR LOS PUERTOS COMO OCUPADOS
            // Le asignamos el ID de la tubería a ambos puertos para "bloquearlos"
            // ==============================================================
            var sourcePort = CurrentDraftPipe.SourceElement.Ports.FirstOrDefault(p => p.Name == CurrentDraftPipe.SourcePortName);
            var targetPort = target.Ports.FirstOrDefault(p => p.Name == targetPortName);

            if (sourcePort != null) sourcePort.ConnectedElementId = CurrentDraftPipe.Id;
            if (targetPort != null) targetPort.ConnectedElementId = CurrentDraftPipe.Id;

            // La añadimos a la colección permanente
            Pipes.Add(CurrentDraftPipe);

            // Limpiamos y salimos del modo conexión
            CurrentDraftPipe = null;
            SetConnectionMode(false);

            NotifyStateChanged();
        }

        // ==============================================================================
        // UTILIDADES
        // ==============================================================================

        public double Snap(double val) => Math.Round(val / GridSize) * GridSize;

        private void NotifyStateChanged() => OnNotifyUI?.Invoke();
    }
}