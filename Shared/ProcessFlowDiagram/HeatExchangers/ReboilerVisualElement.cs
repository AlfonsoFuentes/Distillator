using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Shared.ProcessFlowDiagram.HeatExchangers
{
    public class ReboilerVisualElement : VisualElementBase
    {
        private SolverHeatExchanger HX => Facade as SolverHeatExchanger ?? throw new InvalidOperationException("Facade must be SolverHeatExchanger");
        private IFacadeStream? TubeSideInlet => HX.ColdInlet;
        private IFacadeStream? TubeSideOutlet => HX.ColdOutlet;
        private IFacadeStream? ShellSideInlet => HX.HotInlet;
        private IFacadeStream? ShellSideOutlet => HX.HotOutlet;

        public override EquipmentType Type => EquipmentType.Reboiler;
        public override string Prefix => "E";

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        // Constantes para nombres de puertos
        public const string PortTubeInName = "TubeIn";
        public const string PortTubeOutName = "TubeOut";
        public const string PortShellInName = "ShellIn";
        public const string PortCondensateOutName = "CondensateOut";

        // Propiedades fuertemente tipadas
        public EquipmentPort TubeInPort => Ports.First(p => p.Name == PortTubeInName);
        public EquipmentPort TubeOutPort => Ports.First(p => p.Name == PortTubeOutName);
        public EquipmentPort ShellInPort => Ports.First(p => p.Name == PortShellInName);
        public EquipmentPort CondensateOutPort => Ports.First(p => p.Name == PortCondensateOutName);

        public ReboilerVisualElement()
        {
            Width = 60;
            Height = 140;

            // Lado de tubos.
            AddPort(PortTubeInName, PortType.Inlet, 30, 145, PortDirection.Bottom);
            AddPort(PortTubeOutName, PortType.Outlet, 0, 20, PortDirection.Left);

            // Lado de coraza.
            AddPort(PortShellInName, PortType.Inlet, 60, 20, PortDirection.Right);
            AddPort(PortCondensateOutName, PortType.Outlet, 60, 120, PortDirection.Right);

            Facade = new SolverHeatExchanger("E-102")
            {
                Id = this.Id
            };
        }

        // ==============================================================================
        // IMPLEMENTACIÓN DE IEquipmentFacade
        // ==============================================================================
        public override IEnumerable<string> GetPortNames()
        {
            yield return PortTubeInName;
            yield return PortTubeOutName;
            yield return PortShellInName;
            yield return PortCondensateOutName;
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            return portName switch
            {
                PortTubeInName => TubeSideInlet,
                PortTubeOutName => TubeSideOutlet,
                PortShellInName => ShellSideInlet,
                PortCondensateOutName => ShellSideOutlet,
                _ => null
            };
        }

        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            if (portName == PortTubeInName && HX.ColdInlet == null)
            {
                HX.SetColdInlet(connectedFacade);
            }
            else if (portName == PortTubeOutName && HX.ColdOutlet == null)
            {
                HX.SetColdOutlet(connectedFacade);
            }
            else if (portName == PortShellInName && HX.HotInlet == null)
            {
                HX.SetHotInlet(connectedFacade);
            }
            else if (portName == PortCondensateOutName && HX.HotOutlet == null)
            {
                HX.SetHotOutlet(connectedFacade);
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == PortTubeInName)
            {
                HX.UnSetColdInlet();
            }
            else if (portName == PortTubeOutName)
            {
                HX.UnSetColdOutlet();
            }
            else if (portName == PortShellInName)
            {
                HX.UnSetHotInlet();
            }
            else if (portName == PortCondensateOutName)
            {
                HX.UnSetHotOutlet();
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
            new("ΔP Tube Side", HX.DeltaPCold.ToUiString()),
            new("ΔP Shell Side", HX.DeltaPHot.ToUiString()),
            new("Q Duty", HX.TransferHeat.ToUiString())
        };
        }
    }
}
