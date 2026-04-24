using Shared.UnitOperations.Columns;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.ProcessFlowDiagram.Columns
{
    public class ColumnVisualElement : VisualElementBase
    {
         public override List<ToolTipLegend> GetToolTipData() => Facade.GetToolTipLegend();
        public override EquipmentType Type => EquipmentType.Column;
        public override string Prefix => "T";
        public override bool AllowFreeRotation => false;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => false;
        public override bool IsResizable => false;

        public ColumnVisualElement()
        {
            Width = 80;
            Height = 320;

            // 1. Puertos Estáticos Principales (Los inamovibles)
            AddPort("Overhead", PortType.Outlet, 40, 10, PortDirection.Top);
            AddPort("Bottoms", PortType.Outlet, 40, 280, PortDirection.Bottom);
            AddPort("Reflux", PortType.Inlet, 70, 60, PortDirection.Right);
            AddPort("ReboilerReturn", PortType.Inlet, 70, 240, PortDirection.Right);

            // 2. Nacen los puertos dinámicos por primera vez
            RefreshDynamicPorts();

            Facade = new ColumnSimulationFacade { Id = this.Id, Name = "T-101" };
        }

        // ==============================================================================
        // LÓGICA DE PUERTOS AUTO-GENERABLES (Acordeón)
        // ==============================================================================
        // ==============================================================================
        // LÓGICA DE PUERTOS AUTO-GENERABLES (Acordeón y Altura Dinámica)
        // ==============================================================================
        public void RefreshDynamicPorts()
        {
            double startY = 100;
            double spacingY = 30;

            // 1. Gestionamos los puertos como lo hacíamos antes
            ManageDynamicPortGroup("Feed", PortType.Inlet, PortDirection.Left, 10, startY, spacingY);
            ManageDynamicPortGroup("SideDraw", PortType.Outlet, PortDirection.Right, 70, startY, spacingY);

            // 2. 🚩 NUEVO: CALCULAR LA ALTURA DINÁMICA
            // Averiguamos cuál lado tiene más puertos (izquierdo o derecho)
            int maxSidePorts = Math.Max(
                Ports.Count(p => p.Name.StartsWith("Feed")),
                Ports.Count(p => p.Name.StartsWith("SideDraw"))
            );

            // Calculamos la altura requerida (El último puerto + 100px de margen inferior para la base)
            double requiredHeight = startY + (maxSidePorts * spacingY) + 100;

            // La altura nunca puede ser menor a nuestra altura base (320)
            double newHeight = Math.Max(320, requiredHeight);

            // Si la altura cambió, actualizamos la torre y movemos los puertos inferiores
            if (Height != newHeight)
            {
                Height = newHeight;

                // Movemos la salida de fondos para que siempre esté pegada a la base (Altura - 40px)
                var bottomsPort = Ports.FirstOrDefault(p => p.Name == "Bottoms");
                if (bottomsPort != null) bottomsPort.OffsetY = Height - 40;

                // Movemos el retorno del rehervidor (Altura - 80px)
                var reboilerPort = Ports.FirstOrDefault(p => p.Name == "ReboilerReturn");
                if (reboilerPort != null) reboilerPort.OffsetY = Height - 80;
            }
        }

        private void ManageDynamicPortGroup(string prefix, PortType type, PortDirection dir, double xOffset, double startY, double spacingY)
        {
            // 1. Filtramos los puertos de esta familia ("Feed_1", "Feed_2", etc)
            var targetPorts = Ports.Where(p => p.Name.StartsWith(prefix)).OrderBy(p => p.OffsetY).ToList();

            // 2. Averiguamos cuántos están libres (sin tubería)
            var freePorts = targetPorts.Where(p => p.ConnectedElementId == null).ToList();

            // 🚩 REGLA 1 y 2: Si no hay libres (todos tienen tubería), "Damos a luz" a uno nuevo
            if (freePorts.Count == 0)
            {
                int nextNum = targetPorts.Count + 1;
                AddPort($"{prefix}_{nextNum}", type, xOffset, startY + (targetPorts.Count * spacingY), dir);
            }
            // 🚩 REGLA 3 (Limpieza): Si hay más de 1 libre, borramos los que sobran (dejamos solo el primero)
            else if (freePorts.Count > 1)
            {
                for (int i = 1; i < freePorts.Count; i++)
                {
                    Ports.Remove(freePorts[i]);
                }
            }

            // 4. Re-calculamos la posición "Y" de los que sobrevivieron para que no queden huecos feos
            targetPorts = Ports.Where(p => p.Name.StartsWith(prefix)).OrderBy(p => p.OffsetY).ToList();
            for (int i = 0; i < targetPorts.Count; i++)
            {
                targetPorts[i].OffsetY = startY + (i * spacingY);
            }
        }
    }
}
