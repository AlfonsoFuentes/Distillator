namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class ShellSideVaporCondensingTubeSideVaporizingLiquidDesign : ShellSideLiquidCoolingTubeSideVaporizingLiquidDesign
{
    public ShellSideVaporCondensingTubeSideVaporizingLiquidDesign(HeatExchangerDesignRequest request)
        : base(request)
    {
    }

    protected override string DesignType => nameof(ShellSideVaporCondensingTubeSideVaporizingLiquidDesign);

    protected override void CalculateShellSideHeatTransferCoefficient() => CalculateMixtureCondensingShellSideHeatTransferCoefficient();

    protected override void CalculateShellSidePressureDrop() => CalculateCondensingShellSidePressureDrop();
}
