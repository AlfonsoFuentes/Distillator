using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Shared.ProcessFlowDiagram.ControlValves
{
    public class ControlValveVisualElement : VisualElementBase
    {
        private SolverValve Valve => Facade as SolverValve ?? throw new InvalidOperationException("Facade must be SolverValve");

        public override EquipmentType Type => EquipmentType.ControlValve;
        public override string Prefix => "CV";

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        // Constantes para nombres de puertos
        public const string PortInletName = "Inlet";
        public const string PortOutletName = "Outlet";

        // Propiedades fuertemente tipadas
        public EquipmentPort InletPort => Ports.First(p => p.Name == PortInletName);
        public EquipmentPort OutletPort => Ports.First(p => p.Name == PortOutletName);

        public ControlValveVisualElement()
        {
            Width = 60;
            Height = 80;

            // 1. Entrada (Lado izquierdo, centrado en el cuerpo)
            AddPort(PortInletName, PortType.Inlet, 0, 50, PortDirection.Left);

            // 2. Salida (Lado derecho, centrado en el cuerpo)
            AddPort(PortOutletName, PortType.Outlet, 60, 50, PortDirection.Right);

            Facade = new SolverValve("CV-101")
            {
                Id = this.Id
            };
        }

        public override IEnumerable<string> GetPortNames()
        {
            yield return PortInletName;
            yield return PortOutletName;
        }

        public override IFacadeStream? GetConnectedStream(string portName)
        {
            return portName switch
            {
                PortInletName => Valve.Inlet,
                PortOutletName => Valve.Outlet,
                _ => null
            };
        }

        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            if (portName == PortInletName && Valve.Inlet == null)
            {
                Valve.SetInlet(connectedFacade);
            }
            else if (portName == PortOutletName && Valve.Outlet == null)
            {
                Valve.SetOutlet(connectedFacade);
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == PortInletName)
            {
                Valve.UnSetInlet();
            }
            else if (portName == PortOutletName)
            {
                Valve.UnSetOutlet();
            }
        }

        public override string StatusColor => Valve.State switch
        {
            ValveStateType.Created => "#CBD5E0",
            ValveStateType.PartiallyConnected => "#F6AD55",
            ValveStateType.ReadyToCalculate => "#63B3ED",
            ValveStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => Valve.State switch
        {
            ValveStateType.Created => "Ready",
            ValveStateType.PartiallyConnected => "Underspecified",
            ValveStateType.ReadyToCalculate => "Ready to Solve",
            ValveStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            new("ΔP", Valve.DeltaP.ToUiString())
        };
        }
    }
}
