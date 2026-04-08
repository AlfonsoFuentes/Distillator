using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations
{
    public interface IEquipmentFacade
    {
        Guid Id { get; set; }
        string Name { get; set; }

        string StatusText { get; }
        string StatusColor { get; }

        // Diccionario mágico para el Tooltip genérico
        Dictionary<string, string> GetQuickViewData();

        void AttachConnection(string portName, IEquipmentFacade connectedFacade);

        // Desconecta lo que sea que esté en ese puerto
        void DetachConnection(string portName);
        Action? OnTopologyChanged { get; set; }
    }
}
