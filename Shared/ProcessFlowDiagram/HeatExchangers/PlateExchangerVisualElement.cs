using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Shared.ProcessFlowDiagram.HeatExchangers
{
    public class PlateExchangerVisualElement : VisualElementBase
    {
        private SolverHeatExchanger HX => Facade as SolverHeatExchanger ?? throw new InvalidOperationException("Facade must be SolverHeatExchanger");

        public override EquipmentType Type => EquipmentType.PlateExchanger;
        public override string Prefix => "E";

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        // Constantes para nombres de puertos
        public const string PortHotInName = "HotIn";
        public const string PortHotOutName = "HotOut";
        public const string PortColdInName = "ColdIn";
        public const string PortColdOutName = "ColdOut";

        // Propiedades fuertemente tipadas
        public EquipmentPort HotInPort => Ports.First(p => p.Name == PortHotInName);
        public EquipmentPort HotOutPort => Ports.First(p => p.Name == PortHotOutName);
        public EquipmentPort ColdInPort => Ports.First(p => p.Name == PortColdInName);
        public EquipmentPort ColdOutPort => Ports.First(p => p.Name == PortColdOutName);

        public PlateExchangerVisualElement()
        {
            Width = 80;
            Height = 80;

            // LADO CALIENTE (Flujo descendente típico)
            AddPort(PortHotInName, PortType.Inlet, 0, 20, PortDirection.Left);
            AddPort(PortHotOutName, PortType.Outlet, 80, 60, PortDirection.Right);

            // LADO FRÍO (Flujo ascendente en contracorriente)
            AddPort(PortColdInName, PortType.Inlet, 0, 60, PortDirection.Left);
            AddPort(PortColdOutName, PortType.Outlet, 80, 20, PortDirection.Right);

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
            yield return PortHotInName;
            yield return PortHotOutName;
            yield return PortColdInName;
            yield return PortColdOutName;
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            return portName switch
            {
                PortHotInName => HX.HotInlet,
                PortHotOutName => HX.HotOutlet,
                PortColdInName => HX.ColdInlet,
                PortColdOutName => HX.ColdOutlet,
                _ => null
            };
        }

        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            if (portName == PortHotInName && HX.HotInlet == null)
            {
                HX.SetHotInlet(connectedFacade);
            }
            else if (portName == PortHotOutName && HX.HotOutlet == null)
            {
                HX.SetHotOutlet(connectedFacade);
            }
            else if (portName == PortColdInName && HX.ColdInlet == null)
            {
                HX.SetColdInlet(connectedFacade);
            }
            else if (portName == PortColdOutName && HX.ColdOutlet == null)
            {
                HX.SetColdOutlet(connectedFacade);
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == PortHotInName)
            {
                HX.UnSetHotInlet();
            }
            else if (portName == PortHotOutName)
            {
                HX.UnSetHotOutlet();
            }
            else if (portName == PortColdInName)
            {
                HX.UnSetColdInlet();
            }
            else if (portName == PortColdOutName)
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
            new("ΔP Hot", HX.DeltaPHot.ToUiString()),
            new("ΔP Cold", HX.DeltaPCold.ToUiString()),
            new("Q Transfer", HX.TransferHeat.ToUiString())
        };
        }
    }
}
