using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.ProcessFlowDiagram.Helpers
{
    public class SplitterVisualElement : VisualElementBase
    {
        private SolverSplitter Splitter => Facade as SolverSplitter ?? throw new InvalidOperationException("Facade must be SolverSplitter");
        private readonly Dictionary<string, IFacadeStream> _outletStreamsByPortName = new(StringComparer.OrdinalIgnoreCase);

        public override EquipmentType Type => EquipmentType.Splitter;
        public override string Prefix => "SP";

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        // Constante para nombre de puerto estático
        public const string PortInletName = "Inlet";

        // Propiedad fuertemente tipada
        public EquipmentPort InletPort => Ports.First(p => p.Name == PortInletName);

        public SplitterVisualElement()
        {
            Width = 40;
            Height = 60;

            // 1. Entrada estática (En la punta del triángulo, a la izquierda)
            AddPort(PortInletName, PortType.Inlet, 0, Height / 2, PortDirection.Left);

            // 2. Primera salida dinámica
            AddPort("Outlet_1", PortType.Outlet, 40, Height / 2, PortDirection.Right);

            // Configuramos los puertos y la altura dinámica inicial
            RefreshDynamicPorts();

            Facade = new SolverSplitter("SP-101")
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
                .Where(p => p.Name.StartsWith("Outlet"))
                .OrderBy(p => ExtractPortIndex(p.Name, "Outlet_"))
                .ToList();
            var freePorts = targetPorts.Where(p => p.ConnectedElementId == null).ToList();

            // 1. Lógica de puerto libre
            if (freePorts.Count == 0)
            {
                int nextNum = targetPorts.Select(port => ExtractPortIndex(port.Name, "Outlet_")).DefaultIfEmpty(-1).Max() + 2;
                AddPort($"Outlet_{nextNum}", PortType.Outlet, 0, 0, PortDirection.Right);
            }
            else if (freePorts.Count > 1)
            {
                var lastFreePort = freePorts
                    .OrderByDescending(port => ExtractPortIndex(port.Name, "Outlet_"))
                    .First();
                var toRemove = freePorts.Where(port => port != lastFreePort).ToList();
                foreach (var p in toRemove) Ports.Remove(p);
            }

            targetPorts = Ports
                .Where(p => p.Name.StartsWith("Outlet"))
                .OrderBy(p => ExtractPortIndex(p.Name, "Outlet_"))
                .ToList();

            // 2. CÁLCULO PROPORCIONAL
            double spacing = 30;
            Height = Math.Max(60, (targetPorts.Count - 1) * spacing + 60);
            Width = Math.Max(50, Height * 0.8);

            double centerY = Height / 2;

            // 3. Punta (Inlet) a la izquierda
            var inlet = Ports.FirstOrDefault(p => p.Name == PortInletName);
            if (inlet != null) { inlet.OffsetX =0; inlet.OffsetY = centerY; }

            // 4. Salidas (Outlets) a la derecha (usando el nuevo Width)
            double startY = centerY - ((targetPorts.Count - 1) * spacing) / 2;
            for (int i = 0; i < targetPorts.Count; i++)
            {
                targetPorts[i].OffsetX = Width;
                targetPorts[i].OffsetY = startY + (i * spacing);
            }
        }

        public void EnsureDynamicOutletPort(string portName)
        {
            if (!portName.StartsWith("Outlet_", StringComparison.OrdinalIgnoreCase)) return;
            if (Ports.Any(port => string.Equals(port.Name, portName, StringComparison.OrdinalIgnoreCase))) return;

            AddPort(portName, PortType.Outlet, 0, 0, PortDirection.Right);
            RefreshDynamicPorts();
        }

        // ==============================================================================
        // IMPLEMENTACIÓN DE IEquipmentFacade
        // ==============================================================================
        public override IEnumerable<string> GetPortNames()
        {
            yield return PortInletName;

            // Puertos dinámicos
            foreach (var port in Ports.Where(p => p.Name.StartsWith("Outlet")))
            {
                yield return port.Name;
            }
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            // Puerto estático
            if (portName == PortInletName) return Splitter.Inlet;

            // Puertos dinámicos - Outlet
            if (portName.StartsWith("Outlet_"))
            {
                if (_outletStreamsByPortName.TryGetValue(portName, out var mappedStream))
                {
                    return mappedStream;
                }

                int index = ExtractPortIndex(portName, "Outlet_");
                if (index >= 0 && index < Splitter.Outlets.Count)
                    return Splitter.Outlets[index];
            }

            return null;
        }

        private int ExtractPortIndex(string portName, string prefix)
        {
            if (portName.StartsWith(prefix) && int.TryParse(portName.Substring(prefix.Length), out int index))
            {
                return index - 1; // Convertir de 1-based a 0-based
            }
            return -1;
        }

        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            // Puerto estático
            if (portName == PortInletName && Splitter.Inlet == null)
            {
                Splitter.SetInlet(connectedFacade);
            }
            // Puertos dinámicos - Outlet
            else if (portName.StartsWith("Outlet_"))
            {
                if (!_outletStreamsByPortName.ContainsKey(portName))
                {
                    _outletStreamsByPortName[portName] = connectedFacade;
                    Splitter.AddOutlet(connectedFacade);
                }
                RefreshDynamicPorts();
            }
        }

        public override void DetachConnection(string portName)
        {
            // Puerto estático
            if (portName == PortInletName)
            {
                Splitter.UnSetInlet();
            }
            // Puertos dinámicos - Outlet
            else if (portName.StartsWith("Outlet_"))
            {
                if (_outletStreamsByPortName.Remove(portName, out var mappedStream))
                {
                    Splitter.RemoveOutlet(mappedStream);
                }
                else
                {
                    int index = ExtractPortIndex(portName, "Outlet_");
                    if (index >= 0 && index < Splitter.Outlets.Count)
                    {
                        Splitter.RemoveOutlet(Splitter.Outlets[index]);
                    }
                }
                RefreshDynamicPorts();
            }
        }

        public override string StatusColor => Splitter.State switch
        {
            SplitterStateType.Created => "#CBD5E0",
            SplitterStateType.PartiallyConnected => "#F6AD55",
            SplitterStateType.ReadyToCalculate => "#63B3ED",
            SplitterStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => Splitter.State switch
        {
            SplitterStateType.Created => "Ready",
            SplitterStateType.PartiallyConnected => "Underspecified",
            SplitterStateType.ReadyToCalculate => "Ready to Solve",
            SplitterStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            new("Inlet", Splitter.Inlet?.Name ?? "Not connected"),
            new("Outlets", Splitter.Outlets.Count.ToString())
        };
        }
    }
}
