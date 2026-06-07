using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.HeatExchangers;

namespace Shared.ProcessFlowDiagram.HeatExchangers
{
    public class HeatExchangerVisualElement : VisualElementBase
    {
         public override List<ToolTipLegend> GetToolTipData() => Facade.GetToolTipLegend();
        public override EquipmentType Type => EquipmentType.Exchanger;
        public override string Prefix => "E"; // "E" por Exchanger

        // Permitimos rotar o hacer espejos para adaptar el P&ID
        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        public HeatExchangerVisualElement()
        {
            Width = 160;   // Más alargado que una bomba
            Height = 60;   // Altura estándar

            // ==========================================
            // LADO DE LOS TUBOS (Izquierda - 2 Pasos)
            // ==========================================
            // 1. Entrada a los tubos (Arriba a la izquierda)
            AddPort("TubeIn", PortType.Inlet, 0, 15, PortDirection.Left);

            // 2. Salida de los tubos (Abajo a la izquierda, misma cara)
            AddPort("TubeOut", PortType.Outlet, 0, 45, PortDirection.Left);

            // ==========================================
            // LADO DE LA CORAZA (Shell)
            // ==========================================
            // 3. Entrada a la coraza (Arriba, pegado a la izquierda)
            AddPort("ShellIn", PortType.Inlet, 30, 0, PortDirection.Top);

            // 4. Salida de condensado (Abajo, pegado a la derecha)
            AddPort("CondensateOut", PortType.Outlet, 130, 60, PortDirection.Bottom);

            // 5. Venteo de vapor no condensado (Lateral derecho, sobre el condensado)
            AddPort("VaporVent", PortType.Outlet, 130, 0, PortDirection.Top);

            // TODO: Crear luego la clase HeatExchangerSimulationFacade
            //Facade = new HeatExchangerSimulationFacade2
            //{
            //    Id = this.Id,
            //    Name = "E-101"
            //};
        }
    }
}