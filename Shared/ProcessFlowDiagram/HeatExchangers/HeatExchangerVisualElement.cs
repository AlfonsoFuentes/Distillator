using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Shared.ProcessFlowDiagram.HeatExchangers
{
    public class HeatExchangerVisualElement : VisualElementBase
    {
        private SolverHeatExchanger HX => Facade as SolverHeatExchanger ?? throw new InvalidOperationException("Facade must be SolverHeatExchanger");

        public override EquipmentType Type => EquipmentType.Exchanger;
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


        public HeatExchangerVisualElement()
        {
            Width = 160;
            Height = 60;

            // LADO DE LOS TUBOS (Izquierda - 2 Pasos)
            // La X en 0 ya está perfecta (a ras del borde izquierdo que empieza en 10).
            AddPort(PortTubeInName, PortType.Inlet, 0, 15, PortDirection.Left);

            // 🚩 AJUSTE DE SIMETRÍA: Cambié Y de 45 a 35. 
            // Como el tanque va de Y=5 a Y=55 (centro 30), los puertos en 15 y 35 se ven mucho más simétricos.
            AddPort(PortTubeOutName, PortType.Outlet, 0, 35, PortDirection.Left);

            // LADO DE LA CORAZA (Shell)
            // 🚩 AJUSTE: Empujamos 5 píxeles hacia afuera (-5 arriba, 65 abajo) para que muerdan el borde exacto
            AddPort(PortShellInName, PortType.Inlet, 30, -5, PortDirection.Top);
            AddPort(PortCondensateOutName, PortType.Outlet, 130, 65, PortDirection.Bottom);


            Facade = new SolverHeatExchanger("E-101")
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
                PortTubeInName => HX.ColdInlet,
                PortTubeOutName => HX.ColdOutlet,
                PortShellInName => HX.HotInlet,
                PortCondensateOutName => HX.HotOutlet,
                // VaporVent no tiene correspondencia directa en el solver
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
            // VaporVent: puerto visual sin lógica de solver por ahora
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
            // VaporVent: puerto visual sin lógica de solver por ahora
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