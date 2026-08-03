namespace Shared.UnitOperations.HeatExchangers.Design;

public interface IShellAndTubeDesignFactory
{
    IHeatExchangerDesign Create(HeatExchangerDesignRequest request);
}
