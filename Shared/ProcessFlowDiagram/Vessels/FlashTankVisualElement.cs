using Shared.ProcessFlowDiagram;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Vessels;

namespace Shared.ProcessFlowDiagram.Vessels
{
    public class FlashTankVisualElement : VisualElementBase
    {
        private SolverDrum Drum => Facade as SolverDrum ?? throw new InvalidOperationException("Facade must be SolverDrum");

        public override EquipmentType Type => EquipmentType.FlashDrum;
        public override string Prefix => "V";

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        // Constantes para nombres de puertos
        public const string PortFeedName = "Feed";
        public const string PortVaporName = "Vapor";
        public const string PortLiquidName = "Liquid";

        // Propiedades fuertemente tipadas
        public EquipmentPort FeedPort => Ports.First(p => p.Name == PortFeedName);
        public EquipmentPort VaporPort => Ports.First(p => p.Name == PortVaporName);
        public EquipmentPort LiquidPort => Ports.First(p => p.Name == PortLiquidName);

        public FlashTankVisualElement()
        {
            Width = 150;
            Height = 70;

            // 1. Entrada de Alimento (Lateral Izquierda - En la mitad)
            AddPort(PortFeedName, PortType.Inlet, 0, Height / 2, PortDirection.Left);

            // 2. Salida de Vapor (Top - Centro)
            AddPort(PortVaporName, PortType.Outlet, Width / 2, 0, PortDirection.Top);

            // 3. Salida Líquida (Bottom - Centro)
            AddPort(PortLiquidName, PortType.Outlet, Width / 2, Height, PortDirection.Bottom);

            Facade = new SolverDrum("V-102")
            {
                Id = this.Id
            };
        }

        // ==============================================================================
        // IMPLEMENTACIÓN DE IEquipmentFacade
        // ==============================================================================
        public override IEnumerable<string> GetPortNames()
        {
            yield return PortFeedName;
            yield return PortVaporName;
            yield return PortLiquidName;
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            return portName switch
            {
                PortFeedName => Drum.Feed,
                PortVaporName => Drum.VaporOutlet,
                PortLiquidName => Drum.LiquidOutlet,
                _ => null
            };
        }

        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            if (portName == PortFeedName && Drum.Feed == null)
            {
                Drum.SetFeed(connectedFacade);
            }
            else if (portName == PortVaporName && Drum.VaporOutlet == null)
            {
                Drum.SetVaporOutlet(connectedFacade);
            }
            else if (portName == PortLiquidName && Drum.LiquidOutlet == null)
            {
                Drum.SetLiquidOutlet(connectedFacade);
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == PortFeedName)
            {
                Drum.SetFeed(null!);
            }
            else if (portName == PortVaporName)
            {
                Drum.SetVaporOutlet(null!);
            }
            else if (portName == PortLiquidName)
            {
                Drum.SetLiquidOutlet(null!);
            }
        }

        public override string StatusColor => Drum.State switch
        {
            FlashTankStateType.Created => "#CBD5E0",
            FlashTankStateType.PartiallyConnected => "#F6AD55",
            FlashTankStateType.ReadyToCalculate => "#63B3ED",
            FlashTankStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => Drum.State switch
        {
            FlashTankStateType.Created => "Ready",
            FlashTankStateType.PartiallyConnected => "Underspecified",
            FlashTankStateType.ReadyToCalculate => "Ready to Solve",
            FlashTankStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            new("Feed", Drum.Feed?.Name ?? "Not connected"),
            new("Vapor", Drum.VaporOutlet?.Name ?? "Not connected"),
            new("Liquid", Drum.LiquidOutlet?.Name ?? "Not connected")
        };
        }
        public void RefreshDynamicPorts()
        {
            // --- LÓGICA DE AUTO-GENERACIÓN (Sprouting) ---
            //ManageZone("ExtraFeed", PortType.Inlet, PortDirection.Left);
            //ManageZone("ExtraProduct", PortType.Outlet, PortDirection.Right);

            //// --- RECALCULAR ANCHO (Width) Y POSICIONES ---
            //// Este equipo crece horizontalmente
            //int leftCount = Ports.Count(p => p.Direction == PortDirection.Left);
            //int rightCount = Ports.Count(p => p.Direction == PortDirection.Right);
            //int maxSidePorts = Math.Max(leftCount, rightCount);

            //double spacing = 25;
            //// El ancho es proporcional a la cantidad de puertos laterales
            //Width = Math.Max(120, (maxSidePorts * spacing) + 60);

            //// Re-centrar puertos fijos de Top/Bottom
            //var topPort = Ports.FirstOrDefault(p => p.Direction == PortDirection.Top);
            //if (topPort != null) topPort.OffsetX = Width / 2;

            //var bottomPort = Ports.FirstOrDefault(p => p.Direction == PortDirection.Bottom);
            //if (bottomPort != null) bottomPort.OffsetX = Width / 2;

            //// Posicionar laterales ordenadamente
            //var leftPorts = Ports.Where(p => p.Direction == PortDirection.Left).OrderBy(p => p.OffsetY).ToList();
            //double centerY = Height / 2;
            //double startY = centerY - ((leftPorts.Count - 1) * spacing) / 2;
            //for (int i = 0; i < leftPorts.Count; i++) leftPorts[i].OffsetY = startY + (i * spacing);

            //var rightPorts = Ports.Where(p => p.Direction == PortDirection.Right).OrderBy(p => p.OffsetY).ToList();
            //double startY_R = centerY - ((rightPorts.Count - 1) * spacing) / 2;
            //for (int i = 0; i < rightPorts.Count; i++)
            //{
            //    rightPorts[i].OffsetX = Width;
            //    rightPorts[i].OffsetY = startY_R + (i * spacing);
            //}
        }
    }
}