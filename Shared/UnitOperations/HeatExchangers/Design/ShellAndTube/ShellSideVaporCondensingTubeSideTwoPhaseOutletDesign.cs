namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class ShellSideVaporCondensingTubeSideTwoPhaseOutletDesign : ShellSideLiquidCoolingTubeSideTwoPhaseOutletDesign
{
    public ShellSideVaporCondensingTubeSideTwoPhaseOutletDesign(HeatExchangerDesignRequest request)
        : base(request)
    {
    }

    protected override string DesignType => nameof(ShellSideVaporCondensingTubeSideTwoPhaseOutletDesign);

    protected override void CalculateShellSideHeatTransferCoefficient() => CalculateMixtureCondensingShellSideHeatTransferCoefficient();

    protected override void CalculateShellSidePressureDrop() => CalculateCondensingShellSidePressureDrop();
}
