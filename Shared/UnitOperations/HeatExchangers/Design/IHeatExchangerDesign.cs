using Shared.ProcessFlowDiagram.Designs;

namespace Shared.UnitOperations.HeatExchangers.Design;

public interface IHeatExchangerDesign
{
    IDesignResult Calculate();
}
