using Shared.ProcessFlowDiagram;
using Shared.UnitOperations.Vessels;

namespace Shared.ProcessFlowDiagram.Vessels
{
    public enum VesselOrientation { Vertical, Horizontal }
    public enum VesselHeadType { Flat, Torispherical, Conical }

    public class VesselVisualElement : VisualElementBase
    {
        public override List<ToolTipLegend> GetToolTipData() => Facade.GetToolTipLegend();
        public override EquipmentType Type => EquipmentType.Tank;
        public override string Prefix => "V";

        public VesselOrientation Orientation { get; set; } = VesselOrientation.Vertical;
        public VesselHeadType TopHead { get; set; } = VesselHeadType.Torispherical;
        public VesselHeadType BottomHead { get; set; } = VesselHeadType.Torispherical;

        public VesselVisualElement()
        {
            Width = 80;
            Height = 120; // Altura inicial base

            // Iniciamos con la configuración core: 1 Entrada (L) y 1 Salida (Fondo)
            AddPort("Inlet_1", PortType.Inlet, 10, 40, PortDirection.Left);
            AddPort("Outlet_1", PortType.Outlet, Width / 2, Height, PortDirection.Bottom);

            RefreshDynamicPorts();

            Facade = new VesselSimulationFacade2 { Id = this.Id, Name = "V-101" };
        }

        /// <summary>
        /// Re-calcula la geometría y la ubicación de los puertos basándose en las conexiones actuales.
        /// </summary>
        public void RefreshDynamicPorts()
        {
            // 1. GESTIÓN DE SPPROUTING (Crear nuevos puertos si los actuales están ocupados)
            ManageInletSprouting();
            ManageOutletSprouting();

            // 2. RECALCULAR DIMENSIONES
            // Contamos cuántos puertos hay en cada lateral para estirar el tanque
            int leftCount = Ports.Count(p => p.Direction == PortDirection.Left);
            int rightCount = Ports.Count(p => p.Direction == PortDirection.Right);
            int maxSidePorts = Math.Max(leftCount, rightCount);

            double spacing = 35;
            // El tanque crece para dar 35px de espacio a cada boquilla lateral
            double newHeight = Math.Max(120, (maxSidePorts * spacing) + 60);
            Height = newHeight;

            // 3. POSICIONAMIENTO DE ENTRADAS (Lado Izquierdo)
            // Van de arriba hacia abajo
            var inlets = Ports.Where(p => p.Direction == PortDirection.Left)
                              .OrderBy(p => GetPortIndex(p.Name)).ToList();
            for (int i = 0; i < inlets.Count; i++)
            {
                inlets[i].OffsetX = 10; // Pegado al cuerpo
                inlets[i].OffsetY = 40 + (i * spacing);
            }

            // 4. POSICIONAMIENTO DE SALIDAS (Fondo + Derecha)
            // Outlet_1: Siempre al fondo
            var bottomPort = Ports.FirstOrDefault(p => p.Name == "Outlet_1");
            if (bottomPort != null)
            {
                bottomPort.OffsetX = Width / 2;
                bottomPort.OffsetY = Height;
                bottomPort.Direction = PortDirection.Bottom;
            }

            // Outlet_2, 3, etc: Lado derecho, de abajo hacia arriba (estilo punto rojo)
            var rightOutlets = Ports.Where(p => p.Direction == PortDirection.Right)
                                    .OrderBy(p => GetPortIndex(p.Name)).ToList();
            for (int i = 0; i < rightOutlets.Count; i++)
            {
                rightOutlets[i].OffsetX = Width - 10;
                // Empezamos desde el fondo (Height - 40) y subimos
                rightOutlets[i].OffsetY = (Height - 40) - (i * spacing);
            }
        }

        private void ManageInletSprouting()
        {
            var inlets = Ports.Where(p => p.Name.StartsWith("Inlet")).ToList();
            if (!inlets.Any(p => p.ConnectedElementId == null))
            {
                int nextIndex = inlets.Count + 1;
                AddPort($"Inlet_{nextIndex}", PortType.Inlet, 10, 0, PortDirection.Left);
            }
        }

        private void ManageOutletSprouting()
        {
            var outlets = Ports.Where(p => p.Name.StartsWith("Outlet")).ToList();
            if (!outlets.Any(p => p.ConnectedElementId == null))
            {
                int nextIndex = outlets.Count + 1;
                // El primero fue Bottom, los siguientes son Right
                AddPort($"Outlet_{nextIndex}", PortType.Outlet, Width - 10, 0, PortDirection.Right);
            }
        }

        private int GetPortIndex(string name)
        {
            var parts = name.Split('_');
            return parts.Length > 1 && int.TryParse(parts[1], out int index) ? index : 0;
        }
    }
}