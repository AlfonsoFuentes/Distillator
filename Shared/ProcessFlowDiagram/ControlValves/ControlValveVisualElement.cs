using Shared.UnitOperations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.ProcessFlowDiagram.ControlValves
{
    public class ControlValveVisualElement : VisualElementBase
    {
        public override EquipmentType Type => EquipmentType.ControlValve;
        public override string Prefix => "CV"; // Control Valve

        public override bool AllowFreeRotation => true;
        public override bool AllowFlipHorizontal => true;
        public override bool AllowFlipVertical => true;
        public override bool IsResizable => false;

        public ControlValveVisualElement()
        {
            Width = 60;
            Height = 80;

            // 1. Entrada (Lado izquierdo, centrado en el cuerpo)
            AddPort("Inlet", PortType.Inlet, 0, 50, PortDirection.Left);

            // 2. Salida (Lado derecho, centrado en el cuerpo)
            AddPort("Outlet", PortType.Outlet, 60, 50, PortDirection.Right);

            Facade = new ControlValveSimulationFacade
            {
                Id = this.Id,
                Name = "CV-101"
            };
        }
    }
}
