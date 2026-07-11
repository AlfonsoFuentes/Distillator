
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Pipes;

//namespace Client.Services.EquipmentManagers
//{


//    // Extensión para limpiar la lógica de "Es corriente o no"
//    public static class VisualElementExtensions
//    {
//        public static bool IsStream(this IVisualElement el) =>
//            el.Type == EquipmentType.MaterialStream || el.Type == EquipmentType.EnergyStream;
//    }

//    public interface IConnectionStrategy
//    {
//        bool CanHandle(IVisualElement source, string sourcePortName, IVisualElement? target, string? targetPortName);
//        void Execute(WorkspaceManager wm, IVisualElement source, string sourcePortName, IVisualElement? target, string? targetPortName, double dropX, double dropY);
//    }

//    public class ConnectionOrchestrator
//    {
//        private readonly List<IConnectionStrategy> _strategies = new()
//        {
//            new DirectConnectionStrategy(),
//            new EquipmentToEquipmentStrategy(),
//            new DropToEmptySpaceStrategy()
//        };

//        public void ProcessConnection(WorkspaceManager wm, IVisualElement source, string sourcePortName, IVisualElement? target, string? targetPortName, double dropX, double dropY)
//        {
//            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(source, sourcePortName, target, targetPortName));
//            strategy?.Execute(wm, source, sourcePortName, target, targetPortName, dropX, dropY);
//        }
//    }

//    // 1. Directo: Equipo -> Corriente (o viceversa)
//    public class DirectConnectionStrategy : IConnectionStrategy
//    {
//        public bool CanHandle(IVisualElement s, string sP, IVisualElement? t, string? tP) =>
//            t != null && s.IsStream() != t.IsStream();

//        public void Execute(WorkspaceManager wm, IVisualElement s, string sP, IVisualElement? t, string? tP, double dx, double dy)
//        {
//            if (t != null && s.Connect(sP, t, tP!))
//            {
//                var pipe = new PipeVisualElement
//                {
//                    Id = Guid.NewGuid(),
//                    // Usamos los elementos y sus IDs correctamente
//                    SourceElement = s,
//                    SourceElementId = s.Id,
//                    SourcePortName = sP, // Nombre real del puerto (ej: "Suction")
//                    TargetElement = t,
//                    TargetElementId = t.Id,
//                    TargetPortName = tP!, // Nombre real del puerto destino
//                    ShowTechnicalLabel = false
//                };

//                wm.Pipes.Add(pipe);

//                // Actualizamos los puertos para que el diálogo los reconozca
//                var sPort = s.Ports.FirstOrDefault(p => p.Name == sP);
//                var tPort = t.Ports.FirstOrDefault(p => p.Name == tP);

//                if (sPort != null) sPort.ConnectedElementId = pipe.Id;
//                if (tPort != null) tPort.ConnectedElementId = pipe.Id;

//                wm.RunSimulation();
//                wm.NotifyStateChanged();
//            }
//        }
//    }

//    // 2. Equipo -> Equipo: Crea la corriente en el medio
//    public class EquipmentToEquipmentStrategy : IConnectionStrategy
//    {
//        public bool CanHandle(IVisualElement s, string sP, IVisualElement? t, string? tP) =>
//            t != null && !s.IsStream() && !t.IsStream();

//        public void Execute(WorkspaceManager wm, IVisualElement s, string sP, IVisualElement? t, string? tP, double dx, double dy)
//        {
//            var sPort = s.Ports.FirstOrDefault(p => p.Name == sP);
//            var tPort = t!.Ports.FirstOrDefault(p => p.Name == tP);
//            if (sPort == null || tPort == null || sPort.Type == tPort.Type) return;

//            var newStream = wm.CreateStreamProgrammatically(GetUniqueStreamName(wm));
//            if (newStream == null) return;

//            // 1. POSICIÓN PRECISA (Aumentamos el gap a 80 para mayor aire)
//            var srcAbs = s.GetAbsolutePortCoordinates(sP);
//            int offset = 180; // <--- Incrementado para una separación visual más profesional

//            double streamX = srcAbs.X;
//            double streamY = srcAbs.Y;

//            switch (srcAbs.Direction)
//            {
//                case PortDirection.Right: streamX += offset; break;
//                case PortDirection.Left: streamX -= offset; break;
//                case PortDirection.Top: streamY -= offset; break;
//                case PortDirection.Bottom: streamY += offset; break;
//            }

//            // Centramos el objeto restando la mitad de sus dimensiones propias
//            newStream.X = wm.Snap(streamX - (newStream.Width / 2.0));
//            newStream.Y = wm.Snap(streamY - (newStream.Height / 2.0));

//            // 2. ROTACIÓN UNIFICADA
//            newStream.RotationAngle = GetRotationForPort(sPort);

//            // 3. CONEXIONES
//            string streamPortForSrc = sPort.Type == PortType.Inlet ? "Outlet" : "Inlet";
//            string streamPortForTgt = tPort.Type == PortType.Inlet ? "Outlet" : "Inlet";

//            // Conexión Fuente -> Corriente
//            if (s.Connect(sP, newStream, streamPortForSrc))
//            {
//                var pipe1 = new PipeVisualElement
//                {
//                    Id = Guid.NewGuid(),
//                    SourceElement = s,
//                    SourceElementId = s.Id,
//                    SourcePortName = sP,
//                    TargetElement = newStream,
//                    TargetElementId = newStream.Id,
//                    TargetPortName = streamPortForSrc,
//                    ShowTechnicalLabel = false
//                };
//                wm.Pipes.Add(pipe1);
//                sPort.ConnectedElementId = pipe1.Id;
//                newStream.Ports.FirstOrDefault(p => p.Name == streamPortForSrc)!.ConnectedElementId = pipe1.Id;
//            }

//            // Conexión Corriente -> Destino
//            if (newStream.Connect(streamPortForTgt, t, tP!))
//            {
//                var pipe2 = new PipeVisualElement
//                {
//                    Id = Guid.NewGuid(),
//                    SourceElement = newStream,
//                    SourceElementId = newStream.Id,
//                    SourcePortName = streamPortForTgt,
//                    TargetElement = t,
//                    TargetElementId = t.Id,
//                    TargetPortName = tP!,
//                    ShowTechnicalLabel = false
//                };
//                wm.Pipes.Add(pipe2);
//                newStream.Ports.FirstOrDefault(p => p.Name == streamPortForTgt)!.ConnectedElementId = pipe2.Id;
//                tPort.ConnectedElementId = pipe2.Id;
//            }

//            wm.RunSimulation();
//            wm.NotifyStateChanged();
//        }

//        private int GetRotationForPort(EquipmentPort port)
//        {
//            bool isOutlet = port.Type == PortType.Outlet;
//            return port.Direction switch
//            {
//                PortDirection.Top => isOutlet ? 270 : 90,
//                PortDirection.Bottom => isOutlet ? 90 : 270,
//                PortDirection.Left => isOutlet ? 180 : 0,
//                PortDirection.Right => isOutlet ? 0 : 180,
//                _ => 0
//            };
//        }

//        private string GetUniqueStreamName(WorkspaceManager wm)
//        {
//            string name = wm.NamingService.GenerateNextName("S");
//            while (wm.Areas.SelectMany(a => a.Elements).Any(el => string.Equals(el.Name, name, StringComparison.OrdinalIgnoreCase)))
//            {
//                name = wm.NamingService.GenerateNextName("S");
//            }
//            return name;
//        }
//    }

//    // 3. Equipo -> Lienzo Vacío
//    public class DropToEmptySpaceStrategy : IConnectionStrategy
//    {
//        public bool CanHandle(IVisualElement s, string sP, IVisualElement? t, string? tP) => t == null && !s.IsStream();

//        public void Execute(WorkspaceManager wm, IVisualElement s, string sP, IVisualElement? t, string? tP, double dx, double dy)
//        {
//            var sPort = s.Ports.FirstOrDefault(p => p.Name == sP);
//            if (sPort == null) return;

//            var newStream = wm.CreateStreamProgrammatically(GetUniqueStreamName(wm));
//            if (newStream == null) return;

//            newStream.RotationAngle = GetRotationForPort(sPort);
//            newStream.X = wm.Snap(dx);
//            newStream.Y = wm.Snap(dy);

//            bool isSourceInlet = sPort.Type == PortType.Inlet;
//            string streamPort = isSourceInlet ? "Outlet" : "Inlet";

//            PipeVisualElement pipe;

//            if (isSourceInlet)
//            {
//                // Equipo es Inlet (Destino), Stream es Source
//                if (newStream.Connect(streamPort, s, sP))
//                {
//                    pipe = new PipeVisualElement
//                    {
//                        Id = Guid.NewGuid(),
//                        SourceElement = newStream,
//                        SourceElementId = newStream.Id,
//                        SourcePortName = streamPort,
//                        TargetElement = s,
//                        TargetElementId = s.Id,
//                        TargetPortName = sP, // Usamos el nombre real del puerto del equipo
//                        ShowTechnicalLabel = false
//                    };
//                    wm.Pipes.Add(pipe);
//                    sPort.ConnectedElementId = pipe.Id;
//                }
//            }
//            else
//            {
//                // Equipo es Outlet (Origen), Stream es Target
//                if (s.Connect(sP, newStream, streamPort))
//                {
//                    pipe = new PipeVisualElement
//                    {
//                        Id = Guid.NewGuid(),
//                        SourceElement = s,
//                        SourceElementId = s.Id,
//                        SourcePortName = sP, // Usamos el nombre real del puerto del equipo
//                        TargetElement = newStream,
//                        TargetElementId = newStream.Id,
//                        TargetPortName = streamPort,
//                        ShowTechnicalLabel = false
//                    };
//                    wm.Pipes.Add(pipe);
//                    sPort.ConnectedElementId = pipe.Id;
//                }
//            }

//            wm.RunSimulation();
//            wm.NotifyStateChanged();
//        }

//        private int GetRotationForPort(EquipmentPort port)
//        {
//            bool isOutlet = port.Type == PortType.Outlet;
//            return port.Direction switch
//            {
//                PortDirection.Top => isOutlet ? 270 : 90,
//                PortDirection.Bottom => isOutlet ? 90 : 270,
//                PortDirection.Left => isOutlet ? 180 : 0,
//                PortDirection.Right => isOutlet ? 0 : 180,
//                _ => 0
//            };
//        }

//        private string GetUniqueStreamName(WorkspaceManager wm)
//        {
//            string name = wm.NamingService.GenerateNextName("S");
//            while (wm.Areas.SelectMany(a => a.Elements).Any(el => string.Equals(el.Name, name, StringComparison.OrdinalIgnoreCase)))
//            {
//                name = wm.NamingService.GenerateNextName("S");
//            }
//            return name;
//        }
//    }

//}