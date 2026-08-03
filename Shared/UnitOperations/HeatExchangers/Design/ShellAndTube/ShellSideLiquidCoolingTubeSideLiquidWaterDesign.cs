using Shared.SolverConsecutive;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class ShellSideLiquidCoolingTubeSideLiquidWaterDesign : HeatExchangerDesign
{
    public ShellSideLiquidCoolingTubeSideLiquidWaterDesign(HeatExchangerDesignRequest request)
        : base(request)
    {
    }

    protected override string DesignType => nameof(ShellSideLiquidCoolingTubeSideLiquidWaterDesign);

    private ShellAndTubeDesignVariables Variables => Request.Variables;

    protected override void InitializeValues()
    {
        SetIfUndefined(Variables.TubeGauge, new UnitSystem.UnitLess(16));
        SetIfUndefined(Variables.TubeNominalDiameter, new UnitSystem.Diameter(1, UnitSystem.DiameterUnits.Inch));
        SetIfUndefined(Variables.TubeOuterDiameter, new UnitSystem.Diameter(1, UnitSystem.DiameterUnits.Inch));
        SetIfUndefined(Variables.TubeInnerDiameter, new UnitSystem.Diameter(0.87, UnitSystem.DiameterUnits.Inch));
        SetIfUndefined(Variables.TubeLength, new UnitSystem.Length(3, UnitSystem.LengthUnits.Meter));
        SetIfUndefined(Variables.BaffleSpacing, new UnitSystem.Length(10, UnitSystem.LengthUnits.Inch));
        SetIfUndefined(Variables.TubePitch, new UnitSystem.Diameter(1.25, UnitSystem.DiameterUnits.Inch));
        SetIfUndefined(Variables.ShellPasses, new UnitSystem.UnitLess(1));
        SetIfUndefined(Variables.TubePasses, new UnitSystem.UnitLess(-1));
        SetIfUndefined(Variables.AllowedFoulingResistance, new UnitSystem.UnitLess(0.001));
        SetIfUndefined(Variables.MinimumTubeVelocity, new UnitSystem.Velocity(4.5, UnitSystem.VelocityUnits.FeetPerSecond));
    }

    protected override void CalculateTubeSideHeatTransferCoefficient() => CalculateLiquidWaterTubeSideHeatTransferCoefficient();

    protected override void CalculateTubeSidePressureDrop() => CalculateLiquidTubeSidePressureDrop();

    protected override void CalculateShellSideHeatTransferCoefficient() =>
        CalculateSensibleShellSideHeatTransferCoefficient();

    protected override void CalculateShellSidePressureDrop() => CalculateLiquidShellSidePressureDrop();

    protected override void CalculateInitialAssumedDirtyOverallCoefficient()
    {
        InitializeAssumedDirtyOverallCoefficientFromPreviousCalculation(1000);
    }

    protected override bool TryCalculateTubePasses()
    {
        if (!Variables.RequiredTubeCount.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate tube passes because required tube count is not defined.");
        }

        if (!Variables.TubeFlowArea.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate tube passes because tube flow area is not defined.");
        }

        if (!Variables.MinimumTubeVelocity.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate tube passes because minimum tube velocity is not defined.");
        }

        if (!Request.TubeSideInlet.Stream.VolumetricFlow.IsDefined || !Request.TubeSideOutlet.Stream.VolumetricFlow.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate tube passes because tube side volumetric flow is not defined.");
        }

        var requiredTubeCount = Variables.RequiredTubeCount.Value.GetValue(UnitLessUnits.None);
        var tubeFlowAreaSquareFeet = Variables.TubeFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var minimumTubeVelocityFeetPerSecond = Variables.MinimumTubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond);
        var tubeSideInletFlow = Request.TubeSideInlet.Stream.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg);
        var tubeSideOutletFlow = Request.TubeSideOutlet.Stream.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg);
        var tubeSideVolumetricFlow = (tubeSideInletFlow + tubeSideOutletFlow) / 2d;
        var tubePassesDefined = IsUserDefined(Variables.TubePasses);
        var tubePasses = Variables.TubePasses.IsDefined
            ? (int)Variables.TubePasses.Value.GetValue(UnitLessUnits.None)
            : -1;

        if (tubePasses == -1)
        {
            tubePasses = 1;
        }

        while (true)
        {
            var assumedTubeFlowArea = requiredTubeCount * tubeFlowAreaSquareFeet / tubePasses;
            var tubeVelocity = tubeSideVolumetricFlow / assumedTubeFlowArea;

            Variables.AssumedTubeFlowArea.SetValue(new Area(assumedTubeFlowArea, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
            Variables.TubeVelocity.SetValue(new Velocity(tubeVelocity, VelocityUnits.FeetPerSecond), VariableDefinedBy.Equipment);
            if (!tubePassesDefined)
            {
                Variables.TubePasses.SetValue(new UnitLess(tubePasses), VariableDefinedBy.Equipment);
            }

            if (tubeVelocity > minimumTubeVelocityFeetPerSecond || tubePassesDefined)
            {
                return true;
            }

            if (tubePasses == 8)
            {
                State.Message = "Could not calculate the number of tube passes.";
                return false;
            }

            tubePasses = tubePasses == 1 ? 2 : tubePasses + 2;
        }
    }

    protected override void CalculateActualGeometry() => CalculateShellAndTubeActualGeometry();

    protected override void VerifyTubeVelocity() => VerifyShellAndTubeVelocity();
}
