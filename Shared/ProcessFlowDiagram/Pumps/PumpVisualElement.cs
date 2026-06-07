using Shared.ProcessFlowDiagram.Streams;
using Shared.UnitOperations.Pumps;

namespace Shared.ProcessFlowDiagram.Pumps
{
    public class PumpVisualElement : VisualElementBase
    {
        public override List<ToolTipLegend> GetToolTipData() => Facade.GetToolTipLegend();
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

            // Usamos las constantes al inicializar
            AddPort(PortSuctionName, PortType.Inlet, 12, 40, PortDirection.Left);
            AddPort(PortDischargeName, PortType.Outlet, 40, 10, PortDirection.Top);
    

            //Facade = new PumpSimulationFacade2
            //{
            //    Id = this.Id,
            //    Name = "P-101"
            //};
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
    }
}
