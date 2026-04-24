namespace Shared.ProcessFlowDiagram
{
    public class OffPageConnectorElement : VisualElementBase
    {
        public string TargetAreaName { get; set; } = "Unknown";
        public string ConnectedEquipmentName { get; set; } = "Unconnected";
        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            
            new ToolTipLegend("Target Area", TargetAreaName),
            new ToolTipLegend("Local Node", ConnectedEquipmentName)
        };
        }
        public override bool ShowLabel { get; set; } = false;
        public override EquipmentType Type => EquipmentType.OffPageConnector;
        public override string Prefix => "OPC";

        // Bloqueado para rotación y cambio de tamaño
        public override bool AllowFreeRotation => false;
        public override bool AllowFlipHorizontal => false;
        public override bool AllowFlipVertical => false;
        public override bool IsResizable => false;

        public Guid? TargetAreaId { get; set; }
        public Guid? TargetConnectorId { get; set; }

        public bool IsOutlet { get; set; }

        public OffPageConnectorElement(bool isOutlet = true)
        {
            Width = 80;  // Ancho industrial para nombres largos
            Height = 40;
            IsOutlet = isOutlet;

            UpdatePorts();
        }

        public void ToggleDirection()
        {
            IsOutlet = !IsOutlet;
            UpdatePorts();
        }

        private void UpdatePorts()
        {
            Ports.Clear();
            if (IsOutlet)
            {
                // Si el flujo SALE, el tubo entra por la izquierda
                AddPort("Transfer", PortType.Inlet, 0, 20, PortDirection.Left);
            }
            else
            {
                // Si el flujo ENTRA, el tubo sale por la derecha
                AddPort("Transfer", PortType.Outlet, 80, 20, PortDirection.Right);
            }
        }
    }
}
