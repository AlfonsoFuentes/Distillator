using Shared.UnitOperations.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.ProcessFlowDiagram.Helpers
{
    public class SplitterVisualElement : VisualElementBase
    {
         public override List<ToolTipLegend> GetToolTipData() => Facade.GetToolTipLegend();
        public override EquipmentType Type => EquipmentType.Splitter;
        public override string Prefix => "SP"; // Splitter

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        public SplitterVisualElement()
        {
            Width = 40;  // Delgado
            Height = 60; // Altura base

            // 1. Entrada estática (En la punta del triángulo, a la izquierda)
            AddPort("Inlet", PortType.Inlet, 0, Height / 2, PortDirection.Left);

            // 2. Primera salida dinámica
            AddPort("Outlet_1", PortType.Outlet, 40, Height / 2, PortDirection.Right);

            // Configuramos los puertos y la altura dinámica inicial
            RefreshDynamicPorts();

            Facade = new SplitterSimulationFacade
            {
                Id = this.Id,
                Name = "SP-101"
            };
        }

        // ==============================================================================
        // LÓGICA DE PUERTOS AUTO-GENERABLES Y ALTURA DINÁMICA
        // ==============================================================================
        public void RefreshDynamicPorts()
        {
            var targetPorts = Ports.Where(p => p.Name.StartsWith("Outlet")).OrderBy(p => p.OffsetY).ToList();
            var freePorts = targetPorts.Where(p => p.ConnectedElementId == null).ToList();

            // 1. Lógica de puerto libre
            if (freePorts.Count == 0)
            {
                int nextNum = targetPorts.Count + 1;
                AddPort($"Outlet_{nextNum}", PortType.Outlet, 0, 0, PortDirection.Right);
            }
            else if (freePorts.Count > 1)
            {
                var toRemove = freePorts.Skip(1).ToList();
                foreach (var p in toRemove) Ports.Remove(p);
            }

            targetPorts = Ports.Where(p => p.Name.StartsWith("Outlet")).ToList();

            // 2. CÁLCULO PROPORCIONAL
            double spacing = 30;
            // Altura basada en puertos
            Height = Math.Max(60, (targetPorts.Count - 1) * spacing + 60);

            // 🚩 LA CLAVE: El ancho es el 80% de la altura para mantener la proporción del triángulo
            Width = Math.Max(50, Height * 0.8);

            double centerY = Height / 2;

            // 3. Punta (Inlet) a la izquierda
            var inlet = Ports.FirstOrDefault(p => p.Name == "Inlet");
            if (inlet != null) { inlet.OffsetX = 5; inlet.OffsetY = centerY; }

            // 4. Salidas (Outlets) a la derecha (usando el nuevo Width)
            double startY = centerY - ((targetPorts.Count - 1) * spacing) / 2;
            for (int i = 0; i < targetPorts.Count; i++)
            {
                targetPorts[i].OffsetX = Width - 5; // Siempre al borde derecho
                targetPorts[i].OffsetY = startY + (i * spacing);
            }
        }
    }
}
