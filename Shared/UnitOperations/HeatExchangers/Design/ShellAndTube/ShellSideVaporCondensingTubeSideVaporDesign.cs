namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class ShellSideVaporCondensingTubeSideVaporDesign : ShellSideLiquidCoolingTubeSideVaporDesign
{
    public ShellSideVaporCondensingTubeSideVaporDesign(HeatExchangerDesignRequest request)
        : base(request)
    {
    }

    protected override string DesignType => nameof(ShellSideVaporCondensingTubeSideVaporDesign);

    protected override void CalculateShellSideHeatTransferCoefficient() => CalculateMixtureCondensingShellSideHeatTransferCoefficient();

    protected override void CalculateShellSidePressureDrop() => CalculateCondensingShellSidePressureDrop();
}
