using Shared.SolverConsecutive.Equipments.Columns;
using Shared.SolverQwen.Stream;

namespace Shared.ProcessFlowDiagram.Columns
{
    public class ColumnVisualElement : VisualElementBase
    {
        private SolverColumn Column => Facade as SolverColumn ?? throw new InvalidOperationException("Facade must be SolverColumn");
        private readonly Dictionary<string, IFacadeStream> _feedStreamsByPortName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IFacadeStream> _sideDrawStreamsByPortName = new(StringComparer.OrdinalIgnoreCase);

        public override EquipmentType Type => EquipmentType.Column;
        public override string Prefix => "T";
        public override bool AllowFreeRotation => false;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => false;
        public override bool IsResizable => false;

        // Constantes para nombres de puertos estáticos
        public const string PortOverheadName = "Overhead";
        public const string PortBottomsName = "Bottoms";
        public const string PortRefluxName = "Reflux";
        public const string PortReboilerReturnName = "ReboilerReturn";

        // Propiedades fuertemente tipadas para puertos estáticos
        public EquipmentPort OverheadPort => Ports.First(p => p.Name == PortOverheadName);
        public EquipmentPort BottomsPort => Ports.First(p => p.Name == PortBottomsName);
        public EquipmentPort RefluxPort => Ports.First(p => p.Name == PortRefluxName);
        public EquipmentPort ReboilerReturnPort => Ports.First(p => p.Name == PortReboilerReturnName);

        public ColumnVisualElement()
        {
            Width = 80;
            Height = 320;

            // 1. Puertos Estáticos Principales
            // 1. Puertos Estáticos Principales
            AddPort(PortOverheadName, PortType.Outlet, 40, 10, PortDirection.Top);
            AddPort(PortBottomsName, PortType.Outlet, 40, 290, PortDirection.Bottom); // <-- Ajustado aquí (280 a 290)
            AddPort(PortRefluxName, PortType.Inlet, 70, 60, PortDirection.Right);
            AddPort(PortReboilerReturnName, PortType.Inlet, 70, 240, PortDirection.Right);

            // 2. Nacen los puertos dinámicos por primera vez
            RefreshDynamicPorts();

            Facade = new SolverColumn("T-101")
            {
                Id = this.Id
            };
        }

        // ==============================================================================
        // LÓGICA DE PUERTOS AUTO-GENERABLES (Acordeón y Altura Dinámica)
        // ==============================================================================
        public void RefreshDynamicPorts()
        {
            double startY = 100;
            double spacingY = 30;

            // 1. Gestionamos los puertos como lo hacíamos antes
            ManageDynamicPortGroup("Feed", PortType.Inlet, PortDirection.Left, 10, startY, spacingY);
            ManageDynamicPortGroup("SideDraw", PortType.Outlet, PortDirection.Right, 70, startY, spacingY);

            // 2. CALCULAR LA ALTURA DINÁMICA
            int maxSidePorts = Math.Max(
                Ports.Count(p => p.Name.StartsWith("Feed")),
                Ports.Count(p => p.Name.StartsWith("SideDraw"))
            );

            double requiredHeight = startY + (maxSidePorts * spacingY) + 100;
            double newHeight = Math.Max(320, requiredHeight);

            if (Height != newHeight)
            {
                Height = newHeight;

                var bottomsPort = Ports.FirstOrDefault(p => p.Name == PortBottomsName);
                if (bottomsPort != null) bottomsPort.OffsetY = Height - 30;

                var reboilerPort = Ports.FirstOrDefault(p => p.Name == PortReboilerReturnName);
                if (reboilerPort != null) reboilerPort.OffsetY = Height - 80;
            }
        }

        private void ManageDynamicPortGroup(string prefix, PortType type, PortDirection dir, double xOffset, double startY, double spacingY)
        {
            var targetPorts = Ports
                .Where(p => p.Name.StartsWith(prefix))
                .OrderBy(p => ExtractPortNumber(p.Name, $"{prefix}_"))
                .ToList();
            var freePorts = targetPorts.Where(p => p.ConnectedElementId == null).ToList();

            if (freePorts.Count == 0)
            {
                int nextNum = targetPorts.Select(p => ExtractPortNumber(p.Name, $"{prefix}_")).DefaultIfEmpty(0).Max() + 1;
                AddPort($"{prefix}_{nextNum}", type, xOffset, startY + (targetPorts.Count * spacingY), dir);
            }
            else if (freePorts.Count > 1)
            {
                var lastFreePort = freePorts
                    .OrderByDescending(p => ExtractPortNumber(p.Name, $"{prefix}_"))
                    .First();
                foreach (var port in freePorts.Where(p => p != lastFreePort).ToList())
                {
                    Ports.Remove(port);
                }
            }

            targetPorts = Ports
                .Where(p => p.Name.StartsWith(prefix))
                .OrderBy(p => ExtractPortNumber(p.Name, $"{prefix}_"))
                .ToList();
            for (int i = 0; i < targetPorts.Count; i++)
            {
                targetPorts[i].OffsetY = startY + (i * spacingY);
            }
        }

        public void EnsureDynamicPort(string portName)
        {
            if (!portName.StartsWith("Feed_", StringComparison.OrdinalIgnoreCase) &&
                !portName.StartsWith("SideDraw_", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Ports.Any(port => string.Equals(port.Name, portName, StringComparison.OrdinalIgnoreCase))) return;

            if (portName.StartsWith("Feed_", StringComparison.OrdinalIgnoreCase))
            {
                AddPort(portName, PortType.Inlet, 10, 0, PortDirection.Left);
            }
            else
            {
                AddPort(portName, PortType.Outlet, 70, 0, PortDirection.Right);
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

        // ==============================================================================
        // IMPLEMENTACIÓN DE IEquipmentFacade
        // ==============================================================================
        public override IEnumerable<string> GetPortNames()
        {
            yield return PortOverheadName;
            yield return PortBottomsName;
            yield return PortRefluxName;
            yield return PortReboilerReturnName;

            // Puertos dinámicos
            foreach (var port in Ports.Where(p => p.Name.StartsWith("Feed") || p.Name.StartsWith("SideDraw")))
            {
                yield return port.Name;
            }
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            // Puertos estáticos
            if (portName == PortOverheadName) return Column.VaporOutlet;
            if (portName == PortBottomsName) return Column.BottomOutlet;
            if (portName == PortRefluxName) return Column.RefluxInlet;
            if (portName == PortReboilerReturnName) return Column.VaporInlet;

            // Puertos dinámicos - Feed
            if (portName.StartsWith("Feed_"))
            {
                if (_feedStreamsByPortName.TryGetValue(portName, out var mappedStream))
                {
                    return mappedStream;
                }

                int index = ExtractPortIndex(portName, "Feed_");
                if (index >= 0 && index < Column.Feeds.Count)
                    return Column.Feeds[index];
            }

            // Puertos dinámicos - SideDraw
            if (portName.StartsWith("SideDraw_"))
            {
                if (_sideDrawStreamsByPortName.TryGetValue(portName, out var mappedStream))
                {
                    return mappedStream;
                }

                int index = ExtractPortIndex(portName, "SideDraw_");
                if (index >= 0 && index < Column.SideDraws.Count)
                    return Column.SideDraws[index];
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
            // Puertos estáticos
            if (portName == PortOverheadName && Column.VaporOutlet == null)
            {
                Column.SetTopVaporOutlet(connectedFacade);
            }
            else if (portName == PortBottomsName && Column.BottomOutlet == null)
            {
                Column.SetBottomOutlet(connectedFacade);
            }
            else if (portName == PortRefluxName && Column.RefluxInlet == null)
            {
                Column.SetRefluxInlet(connectedFacade);
            }
            else if (portName == PortReboilerReturnName && Column.VaporInlet == null)
            {
                Column.SetVaporInlet(connectedFacade);
            }
            // Puertos dinámicos - Feed
            else if (portName.StartsWith("Feed_"))
            {
                if (!_feedStreamsByPortName.ContainsKey(portName))
                {
                    _feedStreamsByPortName[portName] = connectedFacade;
                    Column.AddFeed(connectedFacade);
                }
                RefreshDynamicPorts();
            }
            // Puertos dinámicos - SideDraw
            else if (portName.StartsWith("SideDraw_"))
            {
                if (!_sideDrawStreamsByPortName.ContainsKey(portName))
                {
                    _sideDrawStreamsByPortName[portName] = connectedFacade;
                    Column.AddSideDraw(connectedFacade);
                }
                RefreshDynamicPorts();
            }
        }

        public override void DetachConnection(string portName)
        {
            // Puertos estáticos
            if (portName == PortOverheadName)
            {
                Column.UnSetTopVaporOutlet();
            }
            else if (portName == PortBottomsName)
            {
                Column.UnSetBottomOutlet();
            }
            else if (portName == PortRefluxName)
            {
                Column.UnSetRefluxInlet();
            }
            else if (portName == PortReboilerReturnName)
            {
                Column.UnSetVaporInlet();
            }
            // Puertos dinámicos - Feed
            else if (portName.StartsWith("Feed_"))
            {
                if (_feedStreamsByPortName.Remove(portName, out var mappedStream))
                {
                    Column.RemoveFeed(mappedStream);
                }
                else
                {
                    int index = ExtractPortIndex(portName, "Feed_");
                    if (index >= 0 && index < Column.Feeds.Count)
                    {
                        Column.RemoveFeed(Column.Feeds[index]);
                    }
                }
                RefreshDynamicPorts();
            }
            // Puertos dinámicos - SideDraw
            else if (portName.StartsWith("SideDraw_"))
            {
                if (_sideDrawStreamsByPortName.Remove(portName, out var mappedStream))
                {
                    Column.RemoveSideDraw(mappedStream);
                }
                else
                {
                    int index = ExtractPortIndex(portName, "SideDraw_");
                    if (index >= 0 && index < Column.SideDraws.Count)
                    {
                        Column.RemoveSideDraw(Column.SideDraws[index]);
                    }
                }
                RefreshDynamicPorts();
            }
        }

        public override string StatusColor => Column.State switch
        {
            ColumnStateType.Created => "#CBD5E0",
            ColumnStateType.PartiallyConnected => "#F6AD55",
            ColumnStateType.ReadyToCalculate => "#63B3ED",
            ColumnStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => Column.State switch
        {
            ColumnStateType.Created => "Ready",
            ColumnStateType.PartiallyConnected => "Underspecified",
            ColumnStateType.ReadyToCalculate => "Ready to Solve",
            ColumnStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            new("Top Pressure", Column.TopPressure.ToUiString()),
            new("ΔP", Column.DeltaP.ToUiString()),
            new("Bottom Pressure", Column.BottomPressure.ToUiString()),
            
        };
        }
    }
}
