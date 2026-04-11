using Shared.UnitOperations.HeatExchangers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.ProcessFlowDiagram.HeatExchangers
{
    public class PlateExchangerVisualElement : VisualElementBase
    {
        public override EquipmentType Type => EquipmentType.PlateExchanger;
        public override string Prefix => "E"; // Intercambiador

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        public PlateExchangerVisualElement()
        {
            Width = 80;   // Cuadrado
            Height = 80;

            // ==========================================
            // LADO CALIENTE (Flujo descendente típico)
            // ==========================================
            // 1. Entrada Caliente (Arriba, Izquierda)
            AddPort("HotIn", PortType.Inlet, 0, 20, PortDirection.Left);

            // 2. Salida Caliente (Abajo, Derecha)
            AddPort("HotOut", PortType.Outlet, 80, 60, PortDirection.Right);

            // ==========================================
            // LADO FRÍO (Flujo ascendente en contracorriente)
            // ==========================================
            // 3. Entrada Fría (Abajo, Izquierda)
            AddPort("ColdIn", PortType.Inlet, 0, 60, PortDirection.Left);

            // 4. Salida Fría (Arriba, Derecha)
            AddPort("ColdOut", PortType.Outlet, 80, 20, PortDirection.Right);

            Facade = new PlateExchangerSimulationFacade
            {
                Id = this.Id,
                Name = "E-103"
            };
        }
    }
}
