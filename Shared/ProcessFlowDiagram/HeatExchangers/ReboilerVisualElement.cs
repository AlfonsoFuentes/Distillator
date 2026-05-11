using Shared.UnitOperations.HeatExchangers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.ProcessFlowDiagram.HeatExchangers
{
    public class ReboilerVisualElement : VisualElementBase
    {
         public override List<ToolTipLegend> GetToolTipData() => Facade.GetToolTipLegend();
        public override EquipmentType Type => EquipmentType.Reboiler;
        public override string Prefix => "E"; // Sigue siendo un intercambiador

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        public ReboilerVisualElement()
        {
            Width = 60;    // Más estrecho
            Height = 140;  // Orientación Vertical

            // ==========================================
            // LADO DE LOS TUBOS (1 Paso)
            // ==========================================
            // 1. Entrada de líquido (Fondo, centro)
            AddPort("TubeIn", PortType.Inlet, 30, 140, PortDirection.Bottom);

            // 2. Salida de mezcla/vapor (Arriba, lado izquierdo)
            AddPort("TubeOut", PortType.Outlet, 0, 20, PortDirection.Left);

            // ==========================================
            // LADO DE LA CORAZA (Fluido de calentamiento)
            // ==========================================
            // 3. Entrada de vapor de calentamiento (Arriba, lado derecho)
            AddPort("ShellIn", PortType.Inlet, 60, 20, PortDirection.Right);

            // 4. Salida de condensado (Abajo, lado derecho)
            AddPort("CondensateOut", PortType.Outlet, 60, 120, PortDirection.Right);

            Facade = new ReboilerSimulationFacade2
            {
                Id = this.Id,
                Name = "E-102"
            };
        }
    }
}