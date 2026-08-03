using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Shared.ProcessFlowDiagram.Vessels
{
    public enum VesselOrientation { Vertical, Horizontal }
    public enum VesselHeadType { Flat, Torispherical, Conical }

    public class VesselVisualElement : VisualElementBase
    {
        public const string PortMainOutletName = "Outlet_1";

        private SolverVessel Vessel => Facade as SolverVessel ?? throw new InvalidOperationException("Facade must be SolverVessel");
        private readonly Dictionary<string, IFacadeStream> _inletStreamsByPortName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IFacadeStream> _outletStreamsByPortName = new(StringComparer.OrdinalIgnoreCase);
        public override EquipmentType Type => EquipmentType.Tank;
        public override string Prefix => "V";

        public VesselOrientation Orientation { get; set; } = VesselOrientation.Vertical;
        public VesselHeadType TopHead { get; set; } = VesselHeadType.Torispherical;
        public VesselHeadType BottomHead { get; set; } = VesselHeadType.Torispherical;
        public EquipmentPort MainOutletPort => Ports.First(port => port.Name == PortMainOutletName);

        public VesselVisualElement()
        {
            Width = 80;
            Height = 120; // Altura inicial base

            // 1. Iniciamos con la configuración core: 1 Entrada (L) y 1 Salida (Fondo)
            // 🚩 Ajustado a X = 0 para que empiece desde afuera del tanque
            AddPort("Inlet_1", PortType.Inlet, 0, 40, PortDirection.Left);

            // 🚩 Ajustado a Height + 10 para que dibuje hacia arriba y quede a ras del fondo
            AddPort(PortMainOutletName, PortType.Outlet, Width / 2, Height + 10, PortDirection.Bottom);

            RefreshDynamicPorts();
            Facade = new SolverVessel("V-102")
            {
                Id = this.Id
            };
        }

        public void RefreshDynamicPorts()
        {
            // 1. GESTIÓN DE SPROUTING
            ManageInletSprouting();
            ManageOutletSprouting();

            // 2. RECALCULAR DIMENSIONES
            int leftCount = Ports.Count(p => p.Direction == PortDirection.Left);
            int rightCount = Ports.Count(p => p.Direction == PortDirection.Right);
            int maxSidePorts = Math.Max(leftCount, rightCount);

            double spacing = 35;
            double newHeight = Math.Max(120, (maxSidePorts * spacing) + 60);
            Height = newHeight;

            // 3. POSICIONAMIENTO DE ENTRADAS (Lado Izquierdo)
            var inlets = Ports.Where(p => p.Direction == PortDirection.Left)
                              .OrderBy(p => GetPortIndex(p.Name)).ToList();
            for (int i = 0; i < inlets.Count; i++)
            {
                inlets[i].OffsetX = 0; // 🚩 Pegado al borde absoluto (X = 0)
                inlets[i].OffsetY = 40 + (i * spacing);
            }

            // 4. POSICIONAMIENTO DE SALIDAS (Fondo + Derecha)
            var bottomPort = Ports.FirstOrDefault(p => p.Name == "Outlet_1");
            if (bottomPort != null)
            {
                bottomPort.OffsetX = Width / 2;
                bottomPort.OffsetY = Height + 10; // 🚩 Pegado al fondo absoluto
                bottomPort.Direction = PortDirection.Bottom;
            }

            var rightOutlets = Ports.Where(p => p.Direction == PortDirection.Right)
                                    .OrderBy(p => GetPortIndex(p.Name)).ToList();
            for (int i = 0; i < rightOutlets.Count; i++)
            {
                rightOutlets[i].OffsetX = Width; // 🚩 Pegado al borde absoluto (X = Width)
                rightOutlets[i].OffsetY = (Height - 40) - (i * spacing);
            }
        }

        private void ManageInletSprouting()
        {
            var inlets = Ports.Where(p => p.Name.StartsWith("Inlet")).ToList();
            if (!inlets.Any(p => p.ConnectedElementId == null))
            {
                int nextIndex = inlets.Select(p => ExtractPortNumber(p.Name, "Inlet_")).DefaultIfEmpty(0).Max() + 1;
                // 🚩 Nace en 0
                AddPort($"Inlet_{nextIndex}", PortType.Inlet, 0, 0, PortDirection.Left);
            }
            else if (inlets.Count(p => p.ConnectedElementId == null) > 1)
            {
                var lastFree = inlets
                    .Where(p => p.ConnectedElementId == null)
                    .OrderByDescending(p => ExtractPortNumber(p.Name, "Inlet_"))
                    .First();
                foreach (var port in inlets.Where(p => p.ConnectedElementId == null && p != lastFree).ToList())
                {
                    Ports.Remove(port);
                }
            }
        }

        private void ManageOutletSprouting()
        {
            var outlets = Ports.Where(p => p.Name.StartsWith("Outlet")).ToList();
            if (!outlets.Any(p => p.ConnectedElementId == null))
            {
                int nextIndex = outlets.Select(p => ExtractPortNumber(p.Name, "Outlet_")).DefaultIfEmpty(0).Max() + 1;
                // 🚩 Nace en Width
                AddPort($"Outlet_{nextIndex}", PortType.Outlet, Width, 0, PortDirection.Right);
            }
        }

        public void EnsureDynamicPort(string portName)
        {
            if (!portName.StartsWith("Inlet_", StringComparison.OrdinalIgnoreCase) &&
                !portName.StartsWith("Outlet_", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Ports.Any(port => string.Equals(port.Name, portName, StringComparison.OrdinalIgnoreCase))) return;

            if (portName.StartsWith("Inlet_", StringComparison.OrdinalIgnoreCase))
            {
                AddPort(portName, PortType.Inlet, 0, 0, PortDirection.Left);
            }
            else
            {
                AddPort(portName, PortType.Outlet, Width, 0, PortDirection.Right);
            }

            RefreshDynamicPorts();
        }

        private static int ExtractPortNumber(string name, string prefix)
        {
            return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                   int.TryParse(name[prefix.Length..], out var number)
                ? number
                : 0;
        }

        private int GetPortIndex(string name)
        {
            var parts = name.Split('_');
            return parts.Length > 1 && int.TryParse(parts[1], out int index) ? index : 0;
        }

        public override string StatusColor => Vessel.State switch
        {
            VesselStateType.Created => "#CBD5E0",
            VesselStateType.PartiallyConnected => "#F6AD55",
            VesselStateType.ReadyToCalculate => "#63B3ED",
            VesselStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => Vessel.State switch
        {
            VesselStateType.Created => "Ready",
            VesselStateType.PartiallyConnected => "Underspecified",
            VesselStateType.ReadyToCalculate => "Ready to Solve",
            VesselStateType.Solved => "Converged",
            _ => "Unknown"
        };
        // ==============================================================================
        // IMPLEMENTACIÓN DE IEquipmentFacade
        // ==============================================================================

        public override IEnumerable<string> GetPortNames()
        {
            // Retornamos todos los nombres de los puertos dinámicos actuales
            return Ports.Select(p => p.Name);
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            // Entradas
            if (portName.StartsWith("Inlet_"))
            {
                if (_inletStreamsByPortName.TryGetValue(portName, out var mappedStream))
                {
                    return mappedStream;
                }

                int index = ExtractPortIndex(portName, "Inlet_");
                if (index >= 0 && index < Vessel.Inlets.Count)
                    return Vessel.Inlets[index];
            }
            // Salidas
            else if (portName.StartsWith("Outlet_"))
            {
                if (_outletStreamsByPortName.TryGetValue(portName, out var mappedStream))
                {
                    return mappedStream;
                }

                int index = ExtractPortIndex(portName, "Outlet_");
                if (index >= 0 && index < Vessel.Outlets.Count)
                    return Vessel.Outlets[index];
            }

            return null;
        }

        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            if (portName.StartsWith("Inlet_"))
            {
                if (!_inletStreamsByPortName.ContainsKey(portName))
                {
                    _inletStreamsByPortName[portName] = connectedFacade;
                    Vessel.AddInlet(connectedFacade);
                }
                RefreshDynamicPorts();
            }
            else if (portName.StartsWith("Outlet_"))
            {
                if (!_outletStreamsByPortName.ContainsKey(portName))
                {
                    _outletStreamsByPortName[portName] = connectedFacade;
                    Vessel.AddOutlet(connectedFacade);
                }
                RefreshDynamicPorts();
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName.StartsWith("Inlet_"))
            {
                if (_inletStreamsByPortName.Remove(portName, out var mappedStream))
                {
                    Vessel.RemoveInlet(mappedStream);
                }
                else
                {
                    int index = ExtractPortIndex(portName, "Inlet_");
                    if (index >= 0 && index < Vessel.Inlets.Count)
                    {
                        Vessel.RemoveInlet(Vessel.Inlets[index]);
                    }
                }
                RefreshDynamicPorts();
            }
            else if (portName.StartsWith("Outlet_"))
            {
                if (_outletStreamsByPortName.Remove(portName, out var mappedStream))
                {
                    Vessel.RemoveOulet(mappedStream);
                }
                else
                {
                    int index = ExtractPortIndex(portName, "Outlet_");
                    if (index >= 0 && index < Vessel.Outlets.Count)
                    {
                        // Nota: Usando el nombre exacto de tu método (RemoveOulet)
                        Vessel.RemoveOulet(Vessel.Outlets[index]);
                    }
                }
                RefreshDynamicPorts();
            }
        }

        // Método auxiliar para obtener el índice del string (ej. "Inlet_2" -> índice 1)
        private int ExtractPortIndex(string portName, string prefix)
        {
            if (portName.StartsWith(prefix) && int.TryParse(portName.Substring(prefix.Length), out int index))
            {
                return index - 1; // Convertir de 1-based a 0-based
            }
            return -1;
        }

        // ==============================================================================
        // TOOLTIP Y ESTADO
        // ==============================================================================

        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            new("Inlets", Vessel.Inlets.Count.ToString()),
            new("Outlets", Vessel.Outlets.Count.ToString())
        };
        }
    }
}
