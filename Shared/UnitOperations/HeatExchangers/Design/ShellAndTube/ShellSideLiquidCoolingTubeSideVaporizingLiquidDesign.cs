using Shared.SolverConsecutive;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public class ShellSideLiquidCoolingTubeSideVaporizingLiquidDesign : HeatExchangerDesign
{
    public ShellSideLiquidCoolingTubeSideVaporizingLiquidDesign(HeatExchangerDesignRequest request)
        : base(request)
    {
    }

    protected override string DesignType => nameof(ShellSideLiquidCoolingTubeSideVaporizingLiquidDesign);

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

    protected override bool TryCalculateTubePasses() => CalculateTubePassesUsingInletVolumetricFlow();

    protected override void CalculateActualGeometry() => MarkRequired();

    protected override void VerifyTubeVelocity() => VerifyShellAndTubeVelocity();

    private bool CalculateTubePassesUsingInletVolumetricFlow()
    {
        if (!Variables.RequiredTubeCount.IsDefined ||
            !Variables.TubeFlowArea.IsDefined ||
            !Variables.MinimumTubeVelocity.IsDefined ||
            !Request.TubeSideInlet.Stream.VolumetricFlow.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate tube passes because tube side design inputs are incomplete.");
        }

        var requiredTubeCount = Variables.RequiredTubeCount.Value.GetValue(UnitLessUnits.None);
        var tubeFlowAreaSquareFeet = Variables.TubeFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var minimumTubeVelocityFeetPerSecond = Variables.MinimumTubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond);
        var tubeSideVolumetricFlow = Request.TubeSideInlet.Stream.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg);
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
}
