using Shared.UnitOperations.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.ProcessFlowDiagram.Helpers
{
    public class MixerVisualElement : VisualElementBase
    {
        public override EquipmentType Type => EquipmentType.Mixer;
        public override string Prefix => "MIX"; // Mixer

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        public MixerVisualElement()
        {
            Width = 40;
            Height = 60;

            // 1. Salida estática (En la punta del triángulo, a la derecha)
            AddPort("Outlet", PortType.Outlet, 30, Height / 2, PortDirection.Right);

            // 2. Primera entrada dinámica (En la base plana, a la izquierda)
            AddPort("Inlet_1", PortType.Inlet, 0, Height / 2, PortDirection.Left);

            RefreshDynamicPorts();

            Facade = new MixerSimulationFacade
            {
                Id = this.Id,
                Name = "MIX-101"
            };
        }

        // ==============================================================================
        // LÓGICA DE PUERTOS AUTO-GENERABLES Y ALTURA DINÁMICA
        // ==============================================================================
        public void RefreshDynamicPorts()
        {
            // 1. Identificar puertos de entrada (Inlets)
            var targetPorts = Ports.Where(p => p.Name.StartsWith("Inlet")).OrderBy(p => p.OffsetY).ToList();
            var freePorts = targetPorts.Where(p => p.ConnectedElementId == null).ToList();

            // 2. Regla de Oro: Siempre un solo puerto libre al final
            if (freePorts.Count == 0)
            {
                int nextNum = targetPorts.Count + 1;
                AddPort($"Inlet_{nextNum}", PortType.Inlet, 0, 0, PortDirection.Left);
            }
            else if (freePorts.Count > 1)
            {
                var toRemove = freePorts.Skip(1).ToList();
                foreach (var p in toRemove) Ports.Remove(p);
            }

            targetPorts = Ports.Where(p => p.Name.StartsWith("Inlet")).ToList();

            // 3. CÁLCULO DE PROPORCIÓN (Aspect Ratio 0.8)
            double spacing = 30;
            // Altura basada en la cantidad de entradas
            Height = Math.Max(60, (targetPorts.Count - 1) * spacing + 60);

            // 🚩 Ancho proporcional a la altura para que el triángulo sea congruente
            Width = Math.Max(50, Height * 0.8);

            double centerY = Height / 2;

            // 4. Posicionar la SALIDA (Outlet) en la punta derecha central
            var outlet = Ports.FirstOrDefault(p => p.Name == "Outlet");
            if (outlet != null)
            {
                outlet.OffsetX = Width - 5; // Pegado al borde derecho
                outlet.OffsetY = centerY;
            }

            // 5. Distribuir las ENTRADAS (Inlets) simétricamente en la base izquierda
            double startY = centerY - ((targetPorts.Count - 1) * spacing) / 2;
            for (int i = 0; i < targetPorts.Count; i++)
            {
                targetPorts[i].OffsetX = 5; // Pegado al borde izquierdo
                targetPorts[i].OffsetY = startY + (i * spacing);
            }
        }
    }
}
