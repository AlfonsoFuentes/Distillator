using Shared.SolverConsecutive;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public class ShellSideLiquidCoolingTubeSideVaporDesign : HeatExchangerDesign
{
    public ShellSideLiquidCoolingTubeSideVaporDesign(HeatExchangerDesignRequest request)
        : base(request)
    {
    }

    protected override string DesignType => nameof(ShellSideLiquidCoolingTubeSideVaporDesign);

    private ShellAndTubeDesignVariables Variables => Request.Variables;

    protected override void InitializeValues()
    {
        SetIfUndefined(Variables.TubeGauge, new UnitLess(16));
        SetIfUndefined(Variables.TubeNominalDiameter, new Diameter(1, DiameterUnits.Inch));
        SetIfUndefined(Variables.TubeOuterDiameter, new Diameter(1, DiameterUnits.Inch));
        SetIfUndefined(Variables.TubeInnerDiameter, new Diameter(0.87, DiameterUnits.Inch));
        SetIfUndefined(Variables.TubeLength, new Length(3, LengthUnits.Meter));
        SetIfUndefined(Variables.BaffleSpacing, new Length(10, LengthUnits.Inch));
        SetIfUndefined(Variables.TubePitch, new Diameter(1.25, DiameterUnits.Inch));
        SetIfUndefined(Variables.ShellPasses, new UnitLess(1));
        SetIfUndefined(Variables.TubePasses, new UnitLess(-1));
        SetIfUndefined(Variables.AllowedFoulingResistance, new UnitLess(0.001));
        SetIfUndefined(Variables.MinimumTubeVelocity, new Velocity(100, VelocityUnits.FeetPerSecond));
    }

    protected override void CalculateTubeSideHeatTransferCoefficient() => MarkRequired();

    protected override void CalculateTubeSidePressureDrop() => MarkRequired();

    protected override void CalculateShellSideHeatTransferCoefficient() => CalculateSensibleShellSideHeatTransferCoefficient();

    protected override void CalculateShellSidePressureDrop() => CalculateLiquidShellSidePressureDrop();

    protected override void CalculateInitialAssumedDirtyOverallCoefficient() =>
        InitializeAssumedDirtyOverallCoefficientFromPreviousCalculation(1000);

    protected override bool TryCalculateTubePasses() => MarkTubePassCalculationRequired();

    protected override void CalculateActualGeometry() => MarkRequired();

    protected override void VerifyTubeVelocity() => VerifyShellAndTubeVelocity();

    private bool MarkTubePassCalculationRequired()
    {
        MarkRequired(nameof(TryCalculateTubePasses));
        return false;
    }
}
