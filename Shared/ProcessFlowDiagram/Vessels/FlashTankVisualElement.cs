using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Vessels;

namespace Shared.ProcessFlowDiagram.Vessels
{
    public class FlashTankVisualElement : VisualElementBase
    {
        public override List<ToolTipLegend> GetToolTipData() => Facade.GetToolTipLegend();
        public override EquipmentType Type => EquipmentType.FlashDrum;
        public override string Prefix => "V"; // O "S" de Separador

        public override bool AllowFreeRotation => true;

        public FlashTankVisualElement()
        {
            Width = 150; // Más alargado
            Height = 70;

            // 1. Entrada de Alimento (Lateral Izquierda - En la mitad)
            AddPort("Feed", PortType.Inlet, 0, Height / 2, PortDirection.Left);

            // 2. Salida de Vapor (Top - Centro)
            AddPort("Vapor", PortType.Outlet, Width / 2, 0, PortDirection.Top);

            // 3. Salida Líquida 1 / Pesados (Bottom - Izquierda)
            AddPort("Liquid_1", PortType.Outlet, Width * 0.4, Height, PortDirection.Bottom);

            // 4. Salida Líquida 2 / Livianos (Bottom - Derecha)
            AddPort("Liquid_2", PortType.Outlet, Width * 0.7, Height, PortDirection.Bottom);

            Facade = new FlashTankSimulationFacade2 { Id = this.Id, Name = "V-102" };
        }

        public void RefreshDynamicPorts()
        {
            // --- LÓGICA DE AUTO-GENERACIÓN (Sprouting) ---
            ManageZone("ExtraFeed", PortType.Inlet, PortDirection.Left);
            ManageZone("ExtraProduct", PortType.Outlet, PortDirection.Right);

            // --- RECALCULAR ANCHO (Width) Y POSICIONES ---
            // Este equipo crece horizontalmente
            int leftCount = Ports.Count(p => p.Direction == PortDirection.Left);
            int rightCount = Ports.Count(p => p.Direction == PortDirection.Right);
            int maxSidePorts = Math.Max(leftCount, rightCount);

            double spacing = 25;
            // El ancho es proporcional a la cantidad de puertos laterales
            Width = Math.Max(120, (maxSidePorts * spacing) + 60);

            // Re-centrar puertos fijos de Top/Bottom
            var topPort = Ports.FirstOrDefault(p => p.Direction == PortDirection.Top);
            if (topPort != null) topPort.OffsetX = Width / 2;

            var bottomPort = Ports.FirstOrDefault(p => p.Direction == PortDirection.Bottom);
            if (bottomPort != null) bottomPort.OffsetX = Width / 2;

            // Posicionar laterales ordenadamente
            var leftPorts = Ports.Where(p => p.Direction == PortDirection.Left).OrderBy(p => p.OffsetY).ToList();
            double centerY = Height / 2;
            double startY = centerY - ((leftPorts.Count - 1) * spacing) / 2;
            for (int i = 0; i < leftPorts.Count; i++) leftPorts[i].OffsetY = startY + (i * spacing);

            var rightPorts = Ports.Where(p => p.Direction == PortDirection.Right).OrderBy(p => p.OffsetY).ToList();
            double startY_R = centerY - ((rightPorts.Count - 1) * spacing) / 2;
            for (int i = 0; i < rightPorts.Count; i++)
            {
                rightPorts[i].OffsetX = Width;
                rightPorts[i].OffsetY = startY_R + (i * spacing);
            }
        }

        private void ManageZone(string prefix, PortType type, PortDirection dir)
        {
            var zonePorts = Ports.Where(p => p.Name.StartsWith(prefix)).ToList();
            if (!zonePorts.Any(p => p.ConnectedElementId == null))
            {
                AddPort($"{prefix}_{zonePorts.Count + 1}", type, 0, Height / 2, dir);
            }
        }
    }
}