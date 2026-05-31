using Shared.ProcessFlowDiagram;

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


        Action? OnExecuteSolver { get; set; }
   

    }
 
}
