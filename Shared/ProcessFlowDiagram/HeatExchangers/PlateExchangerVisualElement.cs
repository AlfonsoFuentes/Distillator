using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Shared.ProcessFlowDiagram.HeatExchangers
{
    public class PlateExchangerVisualElement : VisualElementBase
    {
        private SolverHeatExchanger HX => Facade as SolverHeatExchanger ?? throw new InvalidOperationException("Facade must be SolverHeatExchanger");
        private IFacadeStream? SideAInlet => HX.HotInlet;
        private IFacadeStream? SideAOutlet => HX.HotOutlet;
        private IFacadeStream? SideBInlet => HX.ColdInlet;
        private IFacadeStream? SideBOutlet => HX.ColdOutlet;

        public override EquipmentType Type => EquipmentType.PlateExchanger;
        public override string Prefix => "E";

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        // Constantes para nombres de puertos.
        public const string PortSideAInName = "SideAIn";
        public const string PortSideAOutName = "SideAOut";
        public const string PortSideBInName = "SideBIn";
        public const string PortSideBOutName = "SideBOut";

        // Propiedades fuertemente tipadas
        public EquipmentPort SideAInPort => Ports.First(p => p.Name == PortSideAInName);
        public EquipmentPort SideAOutPort => Ports.First(p => p.Name == PortSideAOutName);
        public EquipmentPort SideBInPort => Ports.First(p => p.Name == PortSideBInName);
        public EquipmentPort SideBOutPort => Ports.First(p => p.Name == PortSideBOutName);

        public PlateExchangerVisualElement()
        {
            Width = 80;
            Height = 80;

            // Lado A.
            AddPort(PortSideAInName, PortType.Inlet, 0, 20, PortDirection.Left);
            AddPort(PortSideAOutName, PortType.Outlet, 80, 60, PortDirection.Right);

            // Lado B.
            AddPort(PortSideBInName, PortType.Inlet, 0, 60, PortDirection.Left);
            AddPort(PortSideBOutName, PortType.Outlet, 80, 20, PortDirection.Right);

            Facade = new SolverHeatExchanger("E-103")
            {
                Id = this.Id
            };
        }

        // ==============================================================================
        // IMPLEMENTACIÓN DE IEquipmentFacade
        // ==============================================================================
        public override IEnumerable<string> GetPortNames()
        {
            yield return PortSideAInName;
            yield return PortSideAOutName;
            yield return PortSideBInName;
            yield return PortSideBOutName;
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            return portName switch
            {
                PortSideAInName => SideAInlet,
                PortSideAOutName => SideAOutlet,
                PortSideBInName => SideBInlet,
                PortSideBOutName => SideBOutlet,
                _ => null
            };
        }

        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            if (portName == PortSideAInName && HX.HotInlet == null)
            {
                HX.SetHotInlet(connectedFacade);
            }
            else if (portName == PortSideAOutName && HX.HotOutlet == null)
            {
                HX.SetHotOutlet(connectedFacade);
            }
            else if (portName == PortSideBInName && HX.ColdInlet == null)
            {
                HX.SetColdInlet(connectedFacade);
            }
            else if (portName == PortSideBOutName && HX.ColdOutlet == null)
            {
                HX.SetColdOutlet(connectedFacade);
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == PortSideAInName)
            {
                HX.UnSetHotInlet();
            }
            else if (portName == PortSideAOutName)
            {
                HX.UnSetHotOutlet();
            }
            else if (portName == PortSideBInName)
            {
                HX.UnSetColdInlet();
            }
            else if (portName == PortSideBOutName)
            {
                HX.UnSetColdOutlet();
            }
        }

        public override string StatusColor => HX.State switch
        {
            HeatExchangerStateType.Created => "#CBD5E0",
            HeatExchangerStateType.PartiallyConnected => "#F6AD55",
            HeatExchangerStateType.ReadyToCalculate => "#63B3ED",
            HeatExchangerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => HX.State switch
        {
            HeatExchangerStateType.Created => "Ready",
            HeatExchangerStateType.PartiallyConnected => "Underspecified",
            HeatExchangerStateType.ReadyToCalculate => "Ready to Solve",
            HeatExchangerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            new("ΔP Side A", HX.DeltaPHot.ToUiString()),
            new("ΔP Side B", HX.DeltaPCold.ToUiString()),
            new("Q Transfer", HX.TransferHeat.ToUiString())
        };
        }
    }
}
