using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Shared.ProcessFlowDiagram.Helpers
{
    public class StreamMixerVisualElement : VisualElementBase
    {
        public const string PortOutletName = "Outlet";

        private SolverStreamMixer StreamMixer => Facade as SolverStreamMixer ?? throw new InvalidOperationException("Facade must be SolverStreamMixer");
        private readonly Dictionary<string, IFacadeStream> _inletStreamsByPortName = new(StringComparer.OrdinalIgnoreCase);
        public override EquipmentType Type => EquipmentType.Mixer;
        public override string Prefix => "MIX";

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;
        public EquipmentPort OutletPort => Ports.First(port => port.Name == PortOutletName);

        public StreamMixerVisualElement()
        {
            Width = 40;
            Height = 60;

            // 1. Salida estática (Punta del triángulo a la derecha). 
            // 🚩 Se dibuja desde Width - 10 hasta Width
            AddPort(PortOutletName, PortType.Outlet, Width, Height / 2, PortDirection.Right);

            // 2. Primera entrada dinámica (Base plana a la izquierda)
            // 🚩 Se dibuja desde 0 hasta 10
            AddPort("Inlet_1", PortType.Inlet, 0, Height / 2, PortDirection.Left);

            RefreshDynamicPorts();
            Facade = new SolverStreamMixer("MX-101")
            {
                Id = this.Id
            };
        }

        // ==============================================================================
        // LÓGICA DE PUERTOS AUTO-GENERABLES Y ALTURA DINÁMICA
        // ==============================================================================
        public void RefreshDynamicPorts()
        {
            var targetPorts = Ports
                .Where(p => p.Name.StartsWith("Inlet"))
                .OrderBy(p => ExtractPortIndex(p.Name, "Inlet_"))
                .ToList();
            var freePorts = targetPorts.Where(p => p.ConnectedElementId == null).ToList();

            if (freePorts.Count == 0)
            {
                int nextNum = targetPorts.Select(port => ExtractPortIndex(port.Name, "Inlet_")).DefaultIfEmpty(-1).Max() + 2;
                AddPort($"Inlet_{nextNum}", PortType.Inlet, 0, 0, PortDirection.Left);
            }
            else if (freePorts.Count > 1)
            {
                var lastFreePort = freePorts
                    .OrderByDescending(port => ExtractPortIndex(port.Name, "Inlet_"))
                    .First();
                var toRemove = freePorts.Where(port => port != lastFreePort).ToList();
                foreach (var p in toRemove) Ports.Remove(p);
            }

            targetPorts = Ports
                .Where(p => p.Name.StartsWith("Inlet"))
                .OrderBy(p => ExtractPortIndex(p.Name, "Inlet_"))
                .ToList();

            double spacing = 30;
            Height = Math.Max(60, (targetPorts.Count - 1) * spacing + 60);
            Width = Math.Max(50, Height * 0.8);

            double centerY = Height / 2;

            // 🚩 CORRECCIÓN BUG 1: Aquí decía p.Name == "Inlet" en lugar de "Outlet"
            var outlet = Ports.FirstOrDefault(p => p.Name == "Outlet");
            if (outlet != null)
            {
                outlet.OffsetX = Width; // Pegado al borde derecho absoluto
                outlet.OffsetY = centerY;
            }

            double startY = centerY - ((targetPorts.Count - 1) * spacing) / 2;
            for (int i = 0; i < targetPorts.Count; i++)
            {
                targetPorts[i].OffsetX = 0; // Pegado al borde izquierdo absoluto
                targetPorts[i].OffsetY = startY + (i * spacing);
            }
        }

        public void EnsureDynamicInletPort(string portName)
        {
            if (!portName.StartsWith("Inlet_", StringComparison.OrdinalIgnoreCase)) return;
            if (Ports.Any(port => string.Equals(port.Name, portName, StringComparison.OrdinalIgnoreCase))) return;

            AddPort(portName, PortType.Inlet, 0, 0, PortDirection.Left);
            RefreshDynamicPorts();
        }

        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            if (portName == "Outlet" && StreamMixer.Outlet == null)
            {
                StreamMixer.SetOutlet(connectedFacade);
            }
            else if (portName.StartsWith("Inlet_"))
            {
                if (!_inletStreamsByPortName.ContainsKey(portName))
                {
                    _inletStreamsByPortName[portName] = connectedFacade;
                    StreamMixer.AddInlet(connectedFacade);
                }
                RefreshDynamicPorts();
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Outlet")
            {
                StreamMixer.UnSetOutlet();
            }
            else if (portName.StartsWith("Inlet_"))
            {
                if (_inletStreamsByPortName.Remove(portName, out var mappedStream))
                {
                    StreamMixer.RemoveInlet(mappedStream);
                }
                else
                {
                    int index = ExtractPortIndex(portName, "Inlet_");
                    if (index >= 0 && index < StreamMixer.Inlets.Count)
                    {
                        StreamMixer.RemoveInlet(StreamMixer.Inlets[index]);
                    }
                }
                RefreshDynamicPorts();
            }
        }

        private int ExtractPortIndex(string portName, string prefix)
        {
            if (portName.StartsWith(prefix) && int.TryParse(portName.Substring(prefix.Length), out int index))
            {
                return index - 1;
            }
            return -1;
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            if (portName == "Outlet") return StreamMixer.Outlet;

            // 🚩 CORRECCIÓN BUG 2: Aquí decía StartsWith("Outlet_")
            if (portName.StartsWith("Inlet_"))
            {
                if (_inletStreamsByPortName.TryGetValue(portName, out var mappedStream))
                {
                    return mappedStream;
                }

                int index = ExtractPortIndex(portName, "Inlet_");
                if (index >= 0 && index < StreamMixer.Inlets.Count)
                    return StreamMixer.Inlets[index];
            }

            return null;
        }

        public override string StatusColor => StreamMixer.State switch
        {
            StreamMixerStateType.Created => "#CBD5E0",
            StreamMixerStateType.PartiallyConnected => "#F6AD55",
            StreamMixerStateType.ReadyToCalculate => "#63B3ED",
            StreamMixerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => StreamMixer.State switch
        {
            StreamMixerStateType.Created => "Ready",
            StreamMixerStateType.PartiallyConnected => "Underspecified",
            StreamMixerStateType.ReadyToCalculate => "Ready to Solve",
            StreamMixerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            // 🚩 CORRECCIÓN BUG 3: Las leyendas estaban invertidas
            new("Outlet", StreamMixer.Outlet?.Name ?? "Not connected"),
            new("Inlets", StreamMixer.Inlets.Count.ToString())
        };
        }
    }
}
