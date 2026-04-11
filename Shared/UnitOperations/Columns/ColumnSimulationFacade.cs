using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Columns
{
    public class ColumnSimulationFacade : IEquipmentFacade
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#CBD5E0";
        public void AttachConnection(string port, IEquipmentFacade target) { }
        public void DetachConnection(string port) { }

        public string StatusText => "Text";

        public Dictionary<string, string> GetQuickViewData()
        {
           
            return new Dictionary<string, string>();
        }

        public Action? OnTopologyChanged { get; set; }
    }
}
