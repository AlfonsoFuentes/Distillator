using Shared.ProcessFlowDiagram.Streams;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Shared.ProcessFlowDiagram.Pumps
{

    public class PumpVisualElement : VisualElementBase
    {


        private SolverPump Pump => Facade as SolverPump ?? throw new InvalidOperationException("Facade must be SolverPump");

        public override EquipmentType Type => EquipmentType.Pump;
        public override string Prefix => "P";

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        // 1. Centralizamos los nombres en CONSTANTES para evitar errores de tipeo
        public const string PortSuctionName = "Suction";
        public const string PortDischargeName = "Discharge";


        // 2. Propiedades FUERTEMENTE TIPADAS para consumirlas desde Blazor
        public EquipmentPort SuctionPort => Ports.First(p => p.Name == PortSuctionName);
        public EquipmentPort DischargePort => Ports.First(p => p.Name == PortDischargeName);


        public PumpVisualElement()
        {
            Width = 80;
            Height = 80;

            // La voluta izquierda empieza en X=10. Para que el cuadrito muerda a ras, lo ponemos en 10.
            AddPort(PortSuctionName, PortType.Inlet, 0, 40, PortDirection.Left);

            // El cuello de descarga superior termina en Y=20 (centro en X=30). 
            AddPort(PortDischargeName, PortType.Outlet, 30, 10, PortDirection.Top);

            Facade = new SolverPump
            {
                Id = this.Id,
                Name = "P-101"
            };
        }

        public override bool CanConnect(string myPortName, IVisualElement targetElement, string targetPortName)
        {
            if (!base.CanConnect(myPortName, targetElement, targetPortName)) return false;

            // 3. Usamos las constantes en la lógica de negocio también
            if (myPortName == PortSuctionName || myPortName == PortDischargeName)
            {
                if (!(targetElement is StreamVisualElement)) return false;
            }



            return true;
        }
        public override IEnumerable<string> GetPortNames()
        {
            yield return PortSuctionName;
            yield return PortDischargeName;
        }
        public override IFacadeStream? GetConnectedStream(string portName)
        {
            return portName switch
            {
                PortSuctionName => Pump.Inlet,
                PortDischargeName => Pump.Outlet,
                _ => null
            };
        }
        public override void AttachConnection(string portName, IFacadeStream connectedFacade)
        {
            if (portName == "Suction" && Pump.Inlet == null)
            {
                Pump.SetInlet(connectedFacade);

            }
            else if (portName == "Discharge" && Pump.Outlet == null)
            {
                Pump.SetOutlet(connectedFacade);

            }
        }
        public override void DetachConnection(string portName)
        {
            if (portName == "Suction")
            {
                Pump.SetInlet(null!);
            }
            else if (portName == "Discharge")
            {
                Pump.SetOutlet(null!);
            }
        }

        public override bool ShowLabel { get; set; } = true;
        public override string StatusColor => Pump.State switch
        {
            PumpStateType.Created => "#CBD5E0",
            PumpStateType.PartiallyConnected => "#F6AD55",
            PumpStateType.ReadyToCalculate => "#63B3ED",
            PumpStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };
        public override string StatusText => Pump.State switch
        {
            PumpStateType.Created => "Ready",
            PumpStateType.PartiallyConnected => "Underspecified",
            PumpStateType.ReadyToCalculate => "Ready to Solve",
            PumpStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
             {
                 new ("ΔP", Pump.DeltaP.ToUiString()),
                 new ("%Efficiency", Pump.Efficiency.ToUiString()),
                 new ("Power", Pump.Power.ToUiString())
             };
        }
    }
}
