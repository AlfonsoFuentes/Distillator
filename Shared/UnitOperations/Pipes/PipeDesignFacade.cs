using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Pipes
{
    public class PipeDesignFacade : IEquipmentFacade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string StatusText => "Flowing";
        public string StatusColor => "#10B981";

        // Propiedades de ingeniería sincronizadas
        public double Diameter { get; set; } = 4.0;
        public string FluidName { get; set; } = "Water";
        public string Material { get; set; } = "CS"; // Agregamos esta que faltaba
        public double Schedule { get; set; } = 40;

        public Action? OnTopologyChanged { get; set; }

        public Dictionary<string, string> GetQuickViewData()
        {
            return new Dictionary<string, string>
        {
            { "Size", $"{Diameter}\"" },
            { "Fluid", FluidName },
            { "Material", Material },
            { "Sch", Schedule.ToString() }
        };
        }

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade) { }
        public void DetachConnection(string portName) { }
    }
}
