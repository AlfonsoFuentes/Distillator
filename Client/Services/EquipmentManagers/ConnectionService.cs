
using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.ProcessFlowDiagram.Streams;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;

//namespace Client.Services.EquipmentManagers
//{
//    public class ConnectionService
//    {
//        // 🔄 B6: Estado de modo conexión extraído de WorkspaceManager
//        public bool IsConnectionModeActive { get; private set; }
//        public PipeVisualElement? CurrentDraftPipe { get; private set; }
//        public double DraftMouseLogicalX { get; private set; }
//        public double DraftMouseLogicalY { get; private set; }

//        public Action? OnNotifyUI;

//        // Callbacks para operaciones que requieren contexto del WorkspaceManager
//        public Func<List<PipeVisualElement>>? GetPipes { get; set; }
//        public Func<List<DiagramArea>>? GetAreas { get; set; }
//        public Func<DiagramArea>? GetActiveArea { get; set; }
//        public Func<string, string>? GenerateStreamName { get; set; }
//        public Func<double, double>? SnapFunc { get; set; }
//        public Func<string, StreamVisualElement?>? CreateStreamFunc { get; set; }
//        public Action? RunSimulationAction { get; set; }
//        public Action<IVisualElement, string>? DisconnectPortAction { get; set; }

//        private readonly ConnectionOrchestrator _connectionOrchestrator = new();

//        public void SetConnectionMode(bool isActive)
//        {
//            if (IsConnectionModeActive == isActive) return;
//            IsConnectionModeActive = isActive;
//            if (!isActive) CancelConnectionDraft();
//            OnNotifyUI?.Invoke();
//        }

//        public void StartConnectionDraft(IVisualElement source, string portName)
//        {
//            var portCoords = source.GetAbsolutePortCoordinates(portName);
//            DraftMouseLogicalX = portCoords.X;
//            DraftMouseLogicalY = portCoords.Y;
//            CurrentDraftPipe = new PipeVisualElement
//            {
//                Id = Guid.NewGuid(),
//                SourceElementId = source.Id,
//                SourcePortName = portName,
//                SourceElement = source,
//                Label = "Draft...",
//                ShowTechnicalLabel = false
//            };
//            OnNotifyUI?.Invoke();
//        }

//        public void UpdateConnectionDraft(double clientX, double clientY)
//        {
//            if (CurrentDraftPipe == null) return;
//            DraftMouseLogicalX = clientX;
//            DraftMouseLogicalY = clientY;
//            OnNotifyUI?.Invoke();
//        }

//        public void CancelConnectionDraft()
//        {
//            CurrentDraftPipe = null;
//            OnNotifyUI?.Invoke();
//        }

//        public void CompleteConnection2(IVisualElement target, string targetPortName)
//        {
//            if (CurrentDraftPipe == null || CurrentDraftPipe.SourceElement == null) return;
//            if (!CurrentDraftPipe.SourceElement.CanConnect(CurrentDraftPipe.SourcePortName, target, targetPortName))
//            {
//                CancelConnectionDraft(); return;
//            }
//            if (CurrentDraftPipe.SourceElementId == target.Id) { CancelConnectionDraft(); return; }

//            CurrentDraftPipe.TargetElementId = target.Id;
//            CurrentDraftPipe.TargetPortName = targetPortName;
//            CurrentDraftPipe.TargetElement = target;

//            var sP = CurrentDraftPipe.SourceElement.Ports.FirstOrDefault(p => p.Name == CurrentDraftPipe.SourcePortName);
//            var tP = target.Ports.FirstOrDefault(p => p.Name == targetPortName);
//            if (sP != null) sP.ConnectedElementId = CurrentDraftPipe.Id;
//            if (tP != null) tP.ConnectedElementId = CurrentDraftPipe.Id;

//            GetPipes?.Invoke().Add(CurrentDraftPipe);

//            // 🔥 NUEVA LÓGICA: Solo equipos llaman AttachConnection
//            if (CurrentDraftPipe.SourceElement.Facade is IEquipmentFacade sourceEquipment && target.Facade is IFacadeStream targetStream)
//            {
//                CurrentDraftPipe.SourceElement.AttachConnection(CurrentDraftPipe.SourcePortName, targetStream);
//            }
//            else if (target.Facade is IEquipmentFacade targetEquipment && CurrentDraftPipe.SourceElement.Facade is IFacadeStream sourceStream)
//            {
//                target.AttachConnection(targetPortName, sourceStream);
//            }

//            CurrentDraftPipe = null;
//            SetConnectionMode(false);
//            RunSimulationAction?.Invoke();
//            OnNotifyUI?.Invoke();
//        }

//        // 🔄 B6: Temporalmente delega a ConnectionOrchestrator con referencia a WorkspaceManager.
//        // En B7 se refactorizará ConnectionOrchestrator para que use ConnectionService directamente.
//        public void CompleteConnection(WorkspaceManager wm, IVisualElement? target, string? targetPortName, double dropX, double dropY)
//        {
//            if (CurrentDraftPipe == null || CurrentDraftPipe.SourceElement == null) return;
//            var source = CurrentDraftPipe.SourceElement;
//            var sourcePort = CurrentDraftPipe.SourcePortName;
//            _connectionOrchestrator.ProcessConnection(wm, source, sourcePort, target, targetPortName, dropX, dropY);
//            CancelConnectionDraft();
//            SetConnectionMode(false);
//        }

//        public bool IsValidTarget(IVisualElement target, string targetPortName)
//        {
//            if (CurrentDraftPipe?.SourceElement == null) return false;
//            return CurrentDraftPipe.SourceElement.CanConnect(CurrentDraftPipe.SourcePortName, target, targetPortName);
//        }

//        public void ConnectEquipmentToStream(IVisualElement equipment, string equipmentPortName, IVisualElement stream)
//        {
//            // 1. Si el puerto ya tenía algo conectado, lo desconectamos
//            var existingPort = equipment.Ports.FirstOrDefault(p => p.Name == equipmentPortName);
//            if (existingPort != null && existingPort.ConnectedElementId.HasValue)
//            {
//                DisconnectPortAction?.Invoke(equipment, equipmentPortName);
//            }

//            // 2. Determinamos los nombres de los puertos
//            bool isEquipmentInlet = existingPort?.Type == PortType.Inlet;
//            string streamPortName = isEquipmentInlet ? "Outlet" : "Inlet";

//            // 3. Conectamos los objetos lógicamente
//            equipment.Connect(equipmentPortName, stream, streamPortName);

//            // 4. Creamos el tubo visual
//            var pipe = new PipeVisualElement { Id = Guid.NewGuid(), Label = stream.Label, ShowTechnicalLabel = false };

//            if (isEquipmentInlet)
//            {
//                pipe.SourceElementId = stream.Id; pipe.SourcePortName = streamPortName; pipe.SourceElement = stream;
//                pipe.TargetElementId = equipment.Id; pipe.TargetPortName = equipmentPortName; pipe.TargetElement = equipment;
//            }
//            else
//            {
//                pipe.SourceElementId = equipment.Id; pipe.SourcePortName = equipmentPortName; pipe.SourceElement = equipment;
//                pipe.TargetElementId = stream.Id; pipe.TargetPortName = streamPortName; pipe.TargetElement = stream;
//            }

//            GetPipes?.Invoke().Add(pipe);

//            // 5. Arrancamos el motor
//            RunSimulationAction?.Invoke();
//            OnNotifyUI?.Invoke();
//        }
//    }
//}