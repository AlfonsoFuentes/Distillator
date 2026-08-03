namespace Shared.ProcessFlowDiagram
{
    public enum OffPageConnectorPortSide
    {
        Left,
        Right
    }

    public class OffPageConnectorElement : VisualElementBase
    {
        public override bool ShowNozzles => false;
        public string TargetAreaName { get; set; } = "Unknown";
        public string ConnectedEquipmentName { get; set; } = "Unconnected";
        public override List<ToolTipLegend> GetToolTipData()
        {
            return new List<ToolTipLegend>
        {
            new ToolTipLegend("Target Area", TargetAreaName),
            new ToolTipLegend("Local Node", ConnectedEquipmentName),
            new ToolTipLegend("Navigate", "Double-click to go to area")
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
        // UX: OPCs se comportan como banderas en bordes de área.
        // Solo se pueden mover en Y para alinear con puertos.
        public override bool AllowFreeDragX => false;
        public override bool AllowFreeDragY => true;

        public Guid? TargetAreaId { get; set; }
        public Guid? TargetConnectorId { get; set; }

        public bool IsOutlet { get; set; }
        public OffPageConnectorPortSide PortSide { get; set; }

        public OffPageConnectorElement(bool isOutlet = true, OffPageConnectorPortSide? portSide = null)
        {
            Width = 80;
            Height = 40;
            IsOutlet = isOutlet;
            PortSide = portSide ?? GetDefaultPortSide(isOutlet);

            UpdatePorts();
        }

        public void ToggleDirection()
        {
            IsOutlet = !IsOutlet;
            PortSide = GetDefaultPortSide(IsOutlet);
            UpdatePorts();
        }

        public void SetPortSide(OffPageConnectorPortSide side)
        {
            PortSide = side;
            UpdatePorts();
        }

        /// <summary>
        /// Recalcula puertos según el ancho actual. Llamar después de cambiar Width.
        /// </summary>
        public void RefreshPorts() => UpdatePorts();

        /// <summary>
        /// Cambia el ancho y recalcula geometría interna atómicamente.
        /// </summary>
        public void Resize(double newWidth)
        {
            Width = newWidth;
            RefreshPorts();
        }

        private void UpdatePorts()
        {
            Ports.Clear();

            var portType = IsOutlet ? PortType.Inlet : PortType.Outlet;
            var offsetX = PortSide == OffPageConnectorPortSide.Left ? 5 : Width - 5;
            var direction = PortSide == OffPageConnectorPortSide.Left ? PortDirection.Left : PortDirection.Right;

            AddPort("Transfer", portType, offsetX, 20, direction);
        }

        private static OffPageConnectorPortSide GetDefaultPortSide(bool isOutlet) =>
            isOutlet ? OffPageConnectorPortSide.Left : OffPageConnectorPortSide.Right;
    }
}
