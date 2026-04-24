using Shared.ProcessFlowDiagram;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Basiss
{
    public interface IFacade
    {
        Guid Id { get; set; }
        string Name { get; set; }

        string StatusText { get; }
        string StatusColor { get; }

        // Diccionario mágico para el Tooltip genérico
        List<ToolTipLegend> GetToolTipLegend();

        void AttachConnection(string portName, IFacade connectedFacade);

        // Desconecta lo que sea que esté en ese puerto
        void DetachConnection(string portName);
        Action<IFacade>? OnExecuteSolver { get; set; }
        void Calculate();
        void ResetCalculatedVariable();
    }
   
}
