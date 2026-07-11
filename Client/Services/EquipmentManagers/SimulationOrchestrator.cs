
using Microsoft.AspNetCore.Components;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;
using Shared.ProcessFlowDiagram.Streams;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;

namespace Client.Services.EquipmentManagers
{
    //public class SimulationOrchestrator
    //{
    //    private readonly IMainSolver _solver;

    //    public SimulationOrchestrator(IMainSolver solver)
    //    {
    //        _solver = solver;
    //    }

    //    // Callbacks para acceder al estado del WorkspaceManager sin acoplamiento directo
    //    public Func<List<PipeVisualElement>>? GetPipes { get; set; }
    //    public Func<List<DiagramArea>>? GetAreas { get; set; }
    //    public Func<DiagramArea>? GetActiveArea { get; set; }
    //    public Action? RunSimulationAction { get; set; }
    //    public Action? NotifyStateChangedAction { get; set; }

    //    public void DisconnectEquipmentPort(IVisualElement equipment, string portName)
    //    {
    //        var pipes = GetPipes?.Invoke();
    //        var activeArea = GetActiveArea?.Invoke();
    //        var areas = GetAreas?.Invoke();

    //        if (pipes == null || activeArea == null) return;

    //        var pipe = pipes.FirstOrDefault(p =>
    //            (p.SourceElementId == equipment.Id && p.SourcePortName == portName) ||
    //            (p.TargetElementId == equipment.Id && p.TargetPortName == portName));

    //        if (pipe == null) return;

    //        var otherEl = pipe.SourceElementId == equipment.Id ? pipe.TargetElement : pipe.SourceElement;

    //        // 🚩 B7 FIX: Detectar desconexión inter-área (wormhole via OPC)
    //        if (otherEl is OffPageConnectorElement localOpc)
    //        {
    //            var remoteOpc = areas?
    //                .SelectMany(a => a.Elements)
    //                .OfType<OffPageConnectorElement>()
    //                .FirstOrDefault(opc => opc.Id == localOpc.TargetConnectorId);

    //            if (remoteOpc != null)
    //            {
    //                var remoteArea = areas?.FirstOrDefault(a => a.Elements.Contains(remoteOpc));
    //                var remotePipe = remoteArea?.Pipes.FirstOrDefault(p =>
    //                    p.SourceElementId == remoteOpc.Id || p.TargetElementId == remoteOpc.Id);

    //                if (remotePipe != null)
    //                {
    //                    var remoteStreamEl = remotePipe.SourceElementId == remoteOpc.Id
    //                        ? remotePipe.TargetElement : remotePipe.SourceElement;
    //                    var remoteStreamPortName = remotePipe.SourceElementId == remoteOpc.Id
    //                        ? remotePipe.TargetPortName : remotePipe.SourcePortName;

    //                    if (remoteStreamEl?.Facade is IFacadeStream rs)
    //                    {
    //                        remoteStreamEl.Disconnect(remoteStreamPortName);
    //                        bool isRemoteOrphan = remoteStreamEl.Ports.All(p => p.ConnectedElementId == null);
    //                        if (isRemoteOrphan)
    //                        {
    //                            _solver.ClearOrphanStream(rs);
    //                        }
    //                    }

    //                    remoteArea?.Pipes.Remove(remotePipe);
    //                }

    //                remoteArea?.Elements.Remove(remoteOpc);
    //            }

    //            foreach (var p in localOpc.Ports) p.ConnectedElementId = null;
    //            activeArea.Elements.Remove(localOpc);
    //        }
    //        else
    //        {
    //            IFacadeStream? streamToClean = null;
    //            IVisualElement? streamVisualElement = null;

    //            if (pipe.SourceElement?.Facade is IFacadeStream sourceStream)
    //            {
    //                streamToClean = sourceStream;
    //                streamVisualElement = pipe.SourceElement;
    //            }
    //            else if (pipe.TargetElement?.Facade is IFacadeStream targetStream)
    //            {
    //                streamToClean = targetStream;
    //                streamVisualElement = pipe.TargetElement;
    //            }

    //            var otherPortName = pipe.SourceElementId == equipment.Id ? pipe.TargetPortName : pipe.SourcePortName;
    //            otherEl?.Disconnect(otherPortName);

    //            if (streamToClean != null && streamVisualElement != null)
    //            {
    //                bool isTotallyOrphan = streamVisualElement.Ports.All(p => p.ConnectedElementId == null);
    //                if (isTotallyOrphan)
    //                {
    //                    _solver.ClearOrphanStream(streamToClean);
    //                }
    //            }
    //        }

    //        pipes.Remove(pipe);
    //        equipment.Disconnect(portName);

    //        RunSimulationAction?.Invoke();
    //        NotifyStateChangedAction?.Invoke();
    //    }

    //    public void ConnectEquipmentToStream(IVisualElement equipment, string equipmentPortName, IVisualElement stream)
    //    {
    //        var pipes = GetPipes?.Invoke();
    //        if (pipes == null) return;

    //        var existingPort = equipment.Ports.FirstOrDefault(p => p.Name == equipmentPortName);
    //        if (existingPort != null && existingPort.ConnectedElementId.HasValue)
    //        {
    //            DisconnectEquipmentPort(equipment, equipmentPortName);
    //        }

    //        bool isEquipmentInlet = existingPort?.Type == PortType.Inlet;
    //        string streamPortName = isEquipmentInlet ? "Outlet" : "Inlet";

    //        equipment.Connect(equipmentPortName, stream, streamPortName);

    //        var pipe = new PipeVisualElement { Id = Guid.NewGuid(), Label = stream.Label, ShowTechnicalLabel = false };

    //        if (isEquipmentInlet)
    //        {
    //            pipe.SourceElementId = stream.Id; pipe.SourcePortName = streamPortName; pipe.SourceElement = stream;
    //            pipe.TargetElementId = equipment.Id; pipe.TargetPortName = equipmentPortName; pipe.TargetElement = equipment;
    //        }
    //        else
    //        {
    //            pipe.SourceElementId = equipment.Id; pipe.SourcePortName = equipmentPortName; pipe.SourceElement = equipment;
    //            pipe.TargetElementId = stream.Id; pipe.TargetPortName = streamPortName; pipe.TargetElement = stream;
    //        }

    //        pipes.Add(pipe);

    //        RunSimulationAction?.Invoke();
    //        NotifyStateChangedAction?.Invoke();
    //    }
    //}
}