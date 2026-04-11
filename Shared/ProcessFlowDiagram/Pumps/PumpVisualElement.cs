using Shared.ProcessFlowDiagram.Streams;
using Shared.UnitOperations.Pumps;

namespace Shared.ProcessFlowDiagram.Pumps
{
    public class PumpVisualElement : VisualElementBase
    {
        public override EquipmentType Type => EquipmentType.Pump;
        public override string Prefix => "P";
      
        public override bool AllowFreeRotation => true;    // Oculta el botón de rotar 90°
        public override bool AllowFlipHorizontal => true;   // Muestra el botón de espejo
        public override bool AllowFlipVertical => true;    // No tiene sentido volcarla de cabeza
        public override bool IsResizable => false;
        public PumpVisualElement()
        {
            Width = 80;
            Height = 80;
            AddPort("Suction", PortType.Inlet, 12, 40, PortDirection.Left);
            AddPort("Discharge", PortType.Outlet, 40, 10, PortDirection.Top);
            AddPort("Power", PortType.EnergyIn, 40, 62, PortDirection.Bottom);
            Facade = new PumpSimulationFacade
            {
                Id = this.Id,
                Name = "P-101" // El lienzo sobrescribirá esto en el HandleDrop
            };
        }
        // ... (dentro de PumpVisualElement) ...

        public override bool CanConnect(string myPortName, IVisualElement targetElement, string targetPortName)
        {
            // 1. Reglas base (puertos libres, Inlet no conecta con Inlet, etc.)
            if (!base.CanConnect(myPortName, targetElement, targetPortName)) return false;

            // 2. REGLA DE NEGOCIO PARA FLUIDOS: 
            // Las boquillas de proceso SOLO aceptan Corrientes Másicas.
            if (myPortName == "Suction" || myPortName == "Discharge")
            {
                if (!(targetElement is StreamVisualElement)) return false;
            }

            // 3. REGLA DE NEGOCIO PARA ENERGÍA:
            // El eje del motor SOLO acepta Corrientes de Energía.
            if (myPortName == "Power")
            {
                // if (!(targetElement is EnergyStreamVisualElement)) return false;
            }

            return true;
        }
    }
}
