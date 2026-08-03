using Shared.ProcessFlowDiagram.Designs;
using Shared.SolverConsecutive;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public abstract class HeatExchangerDesign : IHeatExchangerDesign
{
    private const int MaximumDesignIterations = 50;
    private const double DefaultAllowedTubePressureDropPsi = 5d;

    protected HeatExchangerDesign(HeatExchangerDesignRequest request)
    {
        Request = request;
    }

    protected HeatExchangerDesignRequest Request { get; }

    protected HeatExchangerDesignState State { get; } = new();

    public IDesignResult Calculate()
    {
        InitializeValues();
        CalculateAllowedFoulingResistance();
        CalculateTubeInnerDiameter();
        CalculateTubeClearance();
        CalculateTubeSurfaceArea();
        CalculateHeatDuty();
        CalculateLogMeanTemperatureDifference();

        ResetPreviousCalculatedFoulingResistance();
        CalculateInitialAssumedDirtyOverallCoefficient();
        ClearMessage();

        do
        {
            State.DesignIterationCount++;

            CalculateAssumedArea();
            CalculateRequiredTubeCount();

            do
            {
                if (!TryCalculateTubePasses())
                {
                    RequestStop();
                    break;
                }

                SelectActualTubeCount();
                ValidateShellDiameter();
                CalculateActualGeometry();
                VerifyTubeVelocity();
            }
            while (ShouldContinueTubePassIteration() && !State.StopRequested);

            CalculateShellFlowArea();
            CalculateShellEquivalentDiameter();

            CalculateTubeSideHeatTransferCoefficient();
            CalculateTubeSidePressureDrop();

            CalculateShellSideHeatTransferCoefficient();
            CalculateCleanOverallCoefficient();
            CalculateFoulingResistance();
            CalculateShellSidePressureDrop();

            VerifyAssumedDirtyOverallCoefficient();
            VerifyTubeSidePressureDrop();
            StopIfMessageExists();
        }
        while (ShouldContinueDesignIteration());

        return BuildResult();
    }

    protected abstract string DesignType { get; }

    protected abstract void CalculateTubeSideHeatTransferCoefficient();

    protected abstract void CalculateTubeSidePressureDrop();

    protected abstract void CalculateInitialAssumedDirtyOverallCoefficient();

    protected virtual void InitializeValues() => MarkRequired();

    protected virtual void CalculateAllowedFoulingResistance()
    {
        if (!Request.Variables.TubeSideAllowedFoulingResistance.IsDefined ||
            !Request.Variables.ShellSideAllowedFoulingResistance.IsDefined)
        {
            return;
        }

        var tubeSideAllowedFouling = Request.Variables.TubeSideAllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);
        var shellSideAllowedFouling = Request.Variables.ShellSideAllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);

        Request.Variables.AllowedFoulingResistance.SetValue(
            new UnitLess(tubeSideAllowedFouling + shellSideAllowedFouling),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateTubeInnerDiameter()
    {
        var tubeInnerDiameterInches = Request.Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeFlowAreaSquareInches = Math.PI * tubeInnerDiameterInches * tubeInnerDiameterInches / 4d;

        Request.Variables.TubeFlowArea.SetValue(
            new Area(tubeFlowAreaSquareInches, SurfaceUnits.inch2),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateTubeClearance()
    {
        var tubePitchInches = GetTubePitchInches();
        var tubeOuterDiameterInches = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeClearanceInches = tubePitchInches - tubeOuterDiameterInches;

        Request.Variables.TubeClearance.SetValue(
            new Length(tubeClearanceInches, LengthUnits.Inch),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateTubeSurfaceArea()
    {
        var tubeOuterDiameterFeet = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var tubeLengthFeet = Request.Variables.TubeLength.Value.GetValue(LengthUnits.Foot);
        var tubeSurfaceAreaSquareFeet = Math.PI * tubeOuterDiameterFeet * tubeLengthFeet;

        Request.Variables.TubeSurfaceArea.SetValue(
            new Area(tubeSurfaceAreaSquareFeet, SurfaceUnits.Foot2),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateHeatDuty()
    {
        if (TryCalculateSideHeatDuty(Request.TubeSideInlet, Request.TubeSideOutlet, out var tubeSideDutyBtuPerHour))
        {
            SetHeatDuty(tubeSideDutyBtuPerHour);
            return;
        }

        if (TryCalculateSideHeatDuty(Request.ShellSideInlet, Request.ShellSideOutlet, out var shellSideDutyBtuPerHour))
        {
            SetHeatDuty(shellSideDutyBtuPerHour);
            return;
        }

        throw new InvalidOperationException(
            "Cannot calculate heat duty because tube side and shell side enthalpy flows are not defined.");
    }

    private static bool TryCalculateSideHeatDuty(
        HeatExchangerStreamSnapshot inlet,
        HeatExchangerStreamSnapshot outlet,
        out double heatDutyBtuPerHour)
    {
        heatDutyBtuPerHour = 0d;

        if (!inlet.Stream.EnthalpyFlow.IsDefined || !outlet.Stream.EnthalpyFlow.IsDefined)
        {
            return false;
        }

        var inletEnthalpyFlow = inlet.Stream.EnthalpyFlow.Value.GetValue(EnergyFlowUnits.BTUhr);
        var outletEnthalpyFlow = outlet.Stream.EnthalpyFlow.Value.GetValue(EnergyFlowUnits.BTUhr);
        heatDutyBtuPerHour = Math.Abs(outletEnthalpyFlow - inletEnthalpyFlow);

        return true;
    }

    private void SetHeatDuty(double heatDutyBtuPerHour)
    {
        Request.Variables.HeatDuty.SetValue(
            new EnergyFlow(heatDutyBtuPerHour, EnergyFlowUnits.BTUhr),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateLogMeanTemperatureDifference()
    {
        if (Request.Variables.LogMeanTemperatureDifference.IsDefined)
        {
            return;
        }

        EnsureStreamTemperatureDefined(Request.ShellSideInlet, "shell side inlet");
        EnsureStreamTemperatureDefined(Request.ShellSideOutlet, "shell side outlet");
        EnsureStreamTemperatureDefined(Request.TubeSideInlet, "tube side inlet");
        EnsureStreamTemperatureDefined(Request.TubeSideOutlet, "tube side outlet");

        var shellInletTemperature = ReadTemperatureFahrenheit(Request.ShellSideInlet.Stream.Temperature.Value);
        var shellOutletTemperature = ReadTemperatureFahrenheit(Request.ShellSideOutlet.Stream.Temperature.Value);
        var tubeInletTemperature = ReadTemperatureFahrenheit(Request.TubeSideInlet.Stream.Temperature.Value);
        var tubeOutletTemperature = ReadTemperatureFahrenheit(Request.TubeSideOutlet.Stream.Temperature.Value);
        var shellAverageTemperature = (shellInletTemperature + shellOutletTemperature) / 2d;
        var tubeAverageTemperature = (tubeInletTemperature + tubeOutletTemperature) / 2d;

        var terminalDifferenceA = shellAverageTemperature >= tubeAverageTemperature
            ? shellInletTemperature - tubeOutletTemperature
            : tubeInletTemperature - shellOutletTemperature;
        var terminalDifferenceB = shellAverageTemperature >= tubeAverageTemperature
            ? shellOutletTemperature - tubeInletTemperature
            : tubeOutletTemperature - shellInletTemperature;
        var lmtd = CalculateLogMeanTemperatureDifference(terminalDifferenceA, terminalDifferenceB);

        Request.Variables.LogMeanTemperatureDifference.SetValue(
            new Temperature(lmtd, TemperatureUnits.DegreeFahrenheit),
            VariableDefinedBy.Equipment);
    }

    private static void EnsureStreamTemperatureDefined(HeatExchangerStreamSnapshot snapshot, string sideName)
    {
        if (!snapshot.Stream.Temperature.IsDefined)
        {
            throw new InvalidOperationException($"Cannot calculate LMTD because {sideName} temperature is not defined.");
        }
    }

    private static double CalculateLogMeanTemperatureDifference(double terminalDifferenceA, double terminalDifferenceB)
    {
        if (terminalDifferenceA <= 0d || terminalDifferenceB <= 0d)
        {
            throw new InvalidOperationException("Cannot calculate LMTD because terminal temperature differences must be positive.");
        }

        if (Math.Abs(terminalDifferenceA - terminalDifferenceB) < 1e-9)
        {
            return terminalDifferenceA;
        }

        return (terminalDifferenceA - terminalDifferenceB) / Math.Log(terminalDifferenceA / terminalDifferenceB);
    }

    private static double ReadTemperatureFahrenheit(Temperature temperature)
    {
        try
        {
            return temperature.GetValue(TemperatureUnits.DegreeFahrenheit);
        }
        catch (UnitConversionException)
        {
            if (ReferenceEquals(temperature.Unit, TemperatureUnits.DegreeCelcius) ||
                string.Equals(temperature.Unit?.Name, TemperatureUnits.DegreeCelcius.Name, StringComparison.OrdinalIgnoreCase))
            {
                return temperature.Value * 9d / 5d + 32d;
            }

            if (ReferenceEquals(temperature.Unit, TemperatureUnits.Kelvin) ||
                string.Equals(temperature.Unit?.Name, TemperatureUnits.Kelvin.Name, StringComparison.OrdinalIgnoreCase))
            {
                return (temperature.Value - 273.15d) * 9d / 5d + 32d;
            }

            throw;
        }
    }

    protected virtual void ResetPreviousCalculatedFoulingResistance()
    {
        State.PreviousCalculatedFoulingResistance = 0d;
    }

    protected virtual void ClearMessage()
    {
        State.Message = string.Empty;
    }

    protected virtual void CalculateAssumedArea()
    {
        if (!Request.Variables.HeatDuty.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate assumed area because heat duty is not defined.");
        }

        if (!Request.Variables.AssumedDirtyOverallCoefficient.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate assumed area because assumed dirty overall coefficient is not defined.");
        }

        if (!Request.Variables.LogMeanTemperatureDifference.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate assumed area because LMTD is not defined.");
        }

        var heatDutyBtuPerHour = Math.Abs(Request.Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr));
        var assumedDirtyOverallCoefficient = Request.Variables.AssumedDirtyOverallCoefficient.Value
            .GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);
        var lmtdFahrenheit = Request.Variables.LogMeanTemperatureDifference.Value
            .GetValue(TemperatureUnits.DegreeFahrenheit);

        if (Math.Abs(assumedDirtyOverallCoefficient) < 1e-12 || Math.Abs(lmtdFahrenheit) < 1e-12)
        {
            throw new InvalidOperationException("Cannot calculate assumed area because Ud or LMTD is zero.");
        }

        var assumedAreaSquareFeet = heatDutyBtuPerHour / (assumedDirtyOverallCoefficient * lmtdFahrenheit);

        Request.Variables.AssumedArea.SetValue(
            new Area(assumedAreaSquareFeet, SurfaceUnits.Foot2),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateRequiredTubeCount()
    {
        if (!Request.Variables.AssumedArea.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate required tube count because assumed area is not defined.");
        }

        if (!Request.Variables.TubeSurfaceArea.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate required tube count because tube surface area is not defined.");
        }

        var assumedAreaSquareFeet = Request.Variables.AssumedArea.Value.GetValue(SurfaceUnits.Foot2);
        var tubeSurfaceAreaSquareFeet = Request.Variables.TubeSurfaceArea.Value.GetValue(SurfaceUnits.Foot2);

        if (Math.Abs(tubeSurfaceAreaSquareFeet) < 1e-12)
        {
            throw new InvalidOperationException("Cannot calculate required tube count because tube surface area is zero.");
        }

        var requiredTubeCount = (int)Math.Ceiling(assumedAreaSquareFeet / tubeSurfaceAreaSquareFeet);

        Request.Variables.RequiredTubeCount.SetValue(
            new UnitLess(requiredTubeCount),
            VariableDefinedBy.Equipment);
    }

    protected virtual bool TryCalculateTubePasses()
    {
        MarkRequired();
        return true;
    }

    protected virtual void SelectActualTubeCount()
    {
        var requestedTubeCount = GetRequestedTubeCount();
        var tubePasses = GetTubePasses();
        var tubeOuterDiameterInches = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubePitchInches = GetTubePitchInches();
        var layoutService = new KernShellAndTubeLayoutCatalogService();

        if (IsUserDefined(Request.Variables.ShellInsideDiameter))
        {
            var capacity = layoutService.EstimateTubeCapacity(new ShellAndTubeLayoutCapacityRequest
            {
                ShellInsideDiameterInches = Request.Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch),
                TubeOuterDiameterInches = tubeOuterDiameterInches,
                TubePitchInches = tubePitchInches,
                TubePasses = tubePasses,
                TubeLayout = Request.Variables.TubeLayout
            });

            Request.Variables.MaximumTubeCount.SetValue(new UnitLess(capacity.MaximumTubeCount), VariableDefinedBy.Equipment);

            if (!IsUserDefined(Request.Variables.ActualTubeCount))
            {
                Request.Variables.ActualTubeCount.SetValue(new UnitLess(requestedTubeCount), VariableDefinedBy.Equipment);
            }

            return;
        }

        var selection = layoutService.SelectShellForTubeCount(new ShellAndTubeLayoutRequest
        {
            TubeCount = requestedTubeCount,
            TubeOuterDiameterInches = tubeOuterDiameterInches,
            TubePitchInches = tubePitchInches,
            TubePasses = tubePasses,
            TubeLayout = Request.Variables.TubeLayout
        });

        Request.Variables.ShellInsideDiameter.SetValue(
            new Diameter(selection.ShellInsideDiameterInches, DiameterUnits.Inch),
            VariableDefinedBy.Equipment);

        Request.Variables.MaximumTubeCount.SetValue(
            new UnitLess(selection.MaximumTubeCount),
            VariableDefinedBy.Equipment);

        if (!IsUserDefined(Request.Variables.ActualTubeCount))
        {
            Request.Variables.ActualTubeCount.SetValue(new UnitLess(requestedTubeCount), VariableDefinedBy.Equipment);
        }
    }

    private double GetRequestedTubeCount()
    {
        if (IsUserDefined(Request.Variables.ActualTubeCount))
        {
            return Request.Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None);
        }

        if (!Request.Variables.RequiredTubeCount.IsDefined)
        {
            throw new InvalidOperationException("Cannot select actual tube count because required tube count is not defined.");
        }

        var requiredTubeCount = Request.Variables.RequiredTubeCount.Value.GetValue(UnitLessUnits.None);

        return Math.Max(requiredTubeCount, State.MinimumTubeCountRequiredByPressureDrop);
    }

    private int GetTubePasses()
    {
        if (!Request.Variables.TubePasses.IsDefined)
        {
            throw new InvalidOperationException("Cannot select actual tube count because tube passes are not defined.");
        }

        return (int)Request.Variables.TubePasses.Value.GetValue(UnitLessUnits.None);
    }

    protected static bool IsUserDefined<T>(Variable<T> variable)
        where T : Amount
    {
        return variable.IsDefined &&
               (variable.DataProcedence == VariableDefinedBy.UserInput ||
                variable.DataProcedence == VariableDefinedBy.Specification);
    }

    protected virtual void ValidateShellDiameter()
    {
        if (!Request.Variables.ShellInsideDiameter.IsDefined)
        {
            return;
        }

        if (!Request.Variables.TubePitch.IsDefined)
        {
            throw new InvalidOperationException("Cannot validate shell diameter because tube pitch is not defined.");
        }

        if (!Request.Variables.TubeOuterDiameter.IsDefined)
        {
            throw new InvalidOperationException("Cannot validate shell diameter because tube outside diameter is not defined.");
        }

        if (!Request.Variables.TubePasses.IsDefined)
        {
            throw new InvalidOperationException("Cannot validate shell diameter because tube passes are not defined.");
        }

        if (!Request.Variables.ActualTubeCount.IsDefined)
        {
            throw new InvalidOperationException("Cannot validate shell diameter because actual tube count is not defined.");
        }

        if (!Request.Variables.RequiredTubeCount.IsDefined)
        {
            throw new InvalidOperationException("Cannot validate shell diameter because required tube count is not defined.");
        }

        var shellInsideDiameterInches = Request.Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubePitchInches = GetTubePitchInches();
        var tubePasses = (int)Request.Variables.TubePasses.Value.GetValue(UnitLessUnits.None);
        var tubeOuterDiameterInches = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var layoutService = new KernShellAndTubeLayoutCatalogService();
        var capacity = layoutService.EstimateTubeCapacity(new ShellAndTubeLayoutCapacityRequest
        {
            ShellInsideDiameterInches = shellInsideDiameterInches,
            TubeOuterDiameterInches = tubeOuterDiameterInches,
            TubePitchInches = tubePitchInches,
            TubePasses = tubePasses,
            TubeLayout = Request.Variables.TubeLayout
        });
        var maximumTubeCount = capacity.MaximumTubeCount;

        Request.Variables.MaximumTubeCount.SetValue(
            new UnitLess(maximumTubeCount),
            VariableDefinedBy.Equipment);

        if (!IsUserDefined(Request.Variables.ActualTubeCount))
        {
            return;
        }

        var actualTubeCount = Request.Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None);
        var requiredTubeCount = Request.Variables.RequiredTubeCount.Value.GetValue(UnitLessUnits.None);

        if (actualTubeCount > maximumTubeCount)
        {
            State.Message = $"The selected shell diameter cannot contain the selected {actualTubeCount:0} tubes.";
            return;
        }

        if (actualTubeCount < requiredTubeCount)
        {
            State.Message = "The selected tube count is lower than the required tube count for this design.";
        }
    }

    protected virtual void CalculateActualGeometry() => MarkRequired();

    protected void CalculateShellAndTubeActualGeometry()
    {
        if (!Request.Variables.TubeSurfaceArea.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because tube surface area is not defined.");
        }

        if (!Request.Variables.ActualTubeCount.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because actual tube count is not defined.");
        }

        if (!Request.Variables.TubeFlowArea.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because tube flow area is not defined.");
        }

        if (!Request.Variables.TubePasses.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because tube passes are not defined.");
        }

        if (!Request.TubeSideInlet.Stream.VolumetricFlow.IsDefined || !Request.TubeSideOutlet.Stream.VolumetricFlow.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because tube side volumetric flow is not defined.");
        }

        if (!Request.Variables.HeatDuty.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because heat duty is not defined.");
        }

        if (!Request.Variables.LogMeanTemperatureDifference.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because LMTD is not defined.");
        }

        var tubeSurfaceAreaSquareFeet = Request.Variables.TubeSurfaceArea.Value.GetValue(SurfaceUnits.Foot2);
        var actualTubeCount = Request.Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None);
        var actualAreaSquareFeet = actualTubeCount * tubeSurfaceAreaSquareFeet;

        Request.Variables.ActualArea.SetValue(
            new Area(actualAreaSquareFeet, SurfaceUnits.Foot2),
            VariableDefinedBy.Equipment);

        var tubeFlowAreaSquareFeet = Request.Variables.TubeFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var tubePasses = Request.Variables.TubePasses.Value.GetValue(UnitLessUnits.None);

        if (Math.Abs(tubePasses) < 1e-12)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because tube passes are zero.");
        }

        var actualTubeFlowAreaSquareFeet = actualTubeCount * tubeFlowAreaSquareFeet / tubePasses;

        Request.Variables.ActualTubeFlowArea.SetValue(
            new Area(actualTubeFlowAreaSquareFeet, SurfaceUnits.Foot2),
            VariableDefinedBy.Equipment);

        if (Math.Abs(actualTubeFlowAreaSquareFeet) < 1e-12)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because actual tube flow area is zero.");
        }

        var tubeSideVolumetricFlow = CalculateLiquidTubeSideVolumetricFlow();
        var tubeVelocity = tubeSideVolumetricFlow / actualTubeFlowAreaSquareFeet;

        Request.Variables.TubeVelocity.SetValue(
            new Velocity(tubeVelocity, VelocityUnits.FeetPerSecond),
            VariableDefinedBy.Equipment);

        var heatDutyBtuPerHour = Math.Abs(Request.Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr));
        var lmtdFahrenheit = Request.Variables.LogMeanTemperatureDifference.Value.GetValue(TemperatureUnits.DegreeFahrenheit);

        if (Math.Abs(actualAreaSquareFeet) < 1e-12 || Math.Abs(lmtdFahrenheit) < 1e-12)
        {
            throw new InvalidOperationException("Cannot calculate actual geometry because actual area or LMTD is zero.");
        }

        var calculatedDirtyOverallCoefficient = heatDutyBtuPerHour / (actualAreaSquareFeet * lmtdFahrenheit);

        Request.Variables.CalculatedDirtyOverallCoefficient.SetValue(
            new HeatTransferCoefficient(calculatedDirtyOverallCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);

        Request.Variables.ActualOverallCoefficient.SetValue(
            new HeatTransferCoefficient(calculatedDirtyOverallCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);

        Request.Variables.LastCalculatedDirtyOverallCoefficient.SetValue(
            new HeatTransferCoefficient(calculatedDirtyOverallCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }

    private double CalculateLiquidTubeSideVolumetricFlow()
    {
        var tubeSideInletFlow = Request.TubeSideInlet.Stream.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg);
        var tubeSideOutletFlow = Request.TubeSideOutlet.Stream.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg);

        return (tubeSideInletFlow + tubeSideOutletFlow) / 2d;
    }

    protected virtual void VerifyTubeVelocity() => MarkRequired();

    protected void VerifyShellAndTubeVelocity()
    {
        State.ContinueTubePassIteration = false;

        if (!Request.Variables.MinimumTubeVelocity.IsDefined)
        {
            throw new InvalidOperationException("Cannot verify tube velocity because minimum tube velocity is not defined.");
        }

        if (!Request.Variables.TubeVelocity.IsDefined)
        {
            throw new InvalidOperationException("Cannot verify tube velocity because real tube velocity is not defined.");
        }

        if (!Request.Variables.TubePasses.IsDefined)
        {
            throw new InvalidOperationException("Cannot verify tube velocity because tube passes are not defined.");
        }

        if (IsUserDefined(Request.Variables.TubePasses))
        {
            return;
        }

        var requiredTubeVelocity = Request.Variables.MinimumTubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond);
        var actualTubeVelocity = Request.Variables.TubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond);

        if (actualTubeVelocity >= requiredTubeVelocity)
        {
            return;
        }

        var tubePasses = (int)Request.Variables.TubePasses.Value.GetValue(UnitLessUnits.None);

        if (tubePasses == 8)
        {
            State.Message = "Could not calculate the number of tube passes.";
            return;
        }

        var nextTubePasses = tubePasses == 1
            ? 2
            : tubePasses + 2;

        Request.Variables.TubePasses.SetValue(
            new UnitLess(nextTubePasses),
            VariableDefinedBy.Equipment);

        State.ContinueTubePassIteration = true;
    }

    protected virtual bool ShouldContinueTubePassIteration()
    {
        var continueIteration = State.ContinueTubePassIteration;
        State.ContinueTubePassIteration = false;

        return continueIteration;
    }


    protected virtual void CalculateShellFlowArea()
    {
        if (!Request.Variables.ShellInsideDiameter.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell flow area because shell inside diameter is not defined.");
        }

        if (!Request.Variables.TubeOuterDiameter.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell flow area because tube outside diameter is not defined.");
        }

        if (!Request.Variables.TubeClearance.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell flow area because tube clearance is not defined.");
        }

        if (!Request.Variables.BaffleSpacing.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell flow area because baffle spacing is not defined.");
        }

        if (!Request.Variables.TubePitch.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell flow area because tube pitch is not defined.");
        }

        if (!Request.Variables.TubePasses.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell flow area because tube passes are not defined.");
        }

        var shellInsideDiameterFeet = Request.Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var tubeOuterDiameterFeet = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var tubeClearanceFeet = Request.Variables.TubeClearance.Value.GetValue(LengthUnits.Foot);
        var baffleSpacingFeet = Request.Variables.BaffleSpacing.Value.GetValue(LengthUnits.Foot);
        var tubePitchFeet = GetTubePitchFeet();
        var tubePasses = Request.Variables.TubePasses.Value.GetValue(UnitLessUnits.None);

        double shellFlowAreaSquareFeet;

        if (Math.Abs(baffleSpacingFeet) < 1e-12)
        {
            shellFlowAreaSquareFeet = Math.PI / 4d *
                                      (Math.Pow(shellInsideDiameterFeet, 2d) -
                                       Math.Pow(tubeOuterDiameterFeet, 2d) * tubePasses);
        }
        else
        {
            if (Math.Abs(tubePitchFeet) < 1e-12)
            {
                throw new InvalidOperationException("Cannot calculate shell flow area because tube pitch is zero.");
            }

            shellFlowAreaSquareFeet = shellInsideDiameterFeet * tubeClearanceFeet * baffleSpacingFeet / tubePitchFeet;
        }

        Request.Variables.ShellFlowArea.SetValue(
            new Area(shellFlowAreaSquareFeet, SurfaceUnits.Foot2),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateShellEquivalentDiameter()
    {
        if (!Request.Variables.BaffleSpacing.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell equivalent diameter because baffle spacing is not defined.");
        }

        var baffleSpacingFeet = Request.Variables.BaffleSpacing.Value.GetValue(LengthUnits.Foot);

        if (Math.Abs(baffleSpacingFeet) >= 1e-12)
        {
            CalculateShellEquivalentDiameterFromTubeLayout();
            return;
        }

        if (!Request.Variables.ShellFlowArea.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell equivalent diameter because shell flow area is not defined.");
        }

        if (!Request.Variables.ActualTubeCount.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell equivalent diameter because actual tube count is not defined.");
        }

        if (!Request.Variables.TubeOuterDiameter.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell equivalent diameter because tube outside diameter is not defined.");
        }

        var shellFlowAreaSquareFeet = Request.Variables.ShellFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var actualTubeCount = Request.Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None);
        var tubeOuterDiameterFeet = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;

        if (Math.Abs(actualTubeCount) < 1e-12 || Math.Abs(tubeOuterDiameterFeet) < 1e-12)
        {
            throw new InvalidOperationException("Cannot calculate shell equivalent diameter because tube count or outside diameter is zero.");
        }

        var equivalentDiameterFeet = 4d * shellFlowAreaSquareFeet / (actualTubeCount * Math.PI * tubeOuterDiameterFeet);

        Request.Variables.ShellEquivalentDiameter.SetValue(
            new Diameter(equivalentDiameterFeet * 12d, DiameterUnits.Inch),
            VariableDefinedBy.Equipment);
    }

    private void CalculateShellEquivalentDiameterFromTubeLayout()
    {
        if (!Request.Variables.TubePitch.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell equivalent diameter because tube pitch is not defined.");
        }

        if (!Request.Variables.TubeOuterDiameter.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell equivalent diameter because tube outside diameter is not defined.");
        }

        var pitchFeet = GetTubePitchFeet();
        var tubeOuterDiameterFeet = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;

        if (tubeOuterDiameterFeet >= pitchFeet)
        {
            throw new InvalidOperationException("Cannot calculate shell equivalent diameter because tube pitch must be greater than tube outside diameter.");
        }

        var equivalentDiameterFeet =
            4d * (pitchFeet * pitchFeet * 0.86d - Math.PI * tubeOuterDiameterFeet * tubeOuterDiameterFeet / 4d) /
            (Math.PI * tubeOuterDiameterFeet);

        Request.Variables.ShellEquivalentDiameter.SetValue(
            new Diameter(equivalentDiameterFeet * 12d, DiameterUnits.Inch),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateShellSideHeatTransferCoefficient() => MarkRequired();

    protected void CalculateSensibleShellSideHeatTransferCoefficient()
    {
        var properties = ReadShellSideAverageProperties();
        var shellEquivalentDiameterFeet = GetShellEquivalentDiameterFeet();
        var shellFlowAreaSquareFeet = Request.Variables.ShellFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var massFlowLbPerHour = Request.ShellSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var massVelocity = massFlowLbPerHour / shellFlowAreaSquareFeet;
        var shellVelocity = massVelocity / properties.AverageMassDensityLbFt3 / 3600d;
        var reynolds = shellEquivalentDiameterFeet * massVelocity / properties.AverageViscosityLbFtHr;
        var jh = JhFig28(reynolds);
        var propertyFactor = properties.AverageThermalConductivityBtuHrFtF *
                             Math.Pow(properties.AverageMassCpBtuLbF * properties.AverageViscosityLbFtHr /
                                      properties.AverageThermalConductivityBtuHrFtF, 1d / 3d);
        var heatTransferCoefficient = jh / shellEquivalentDiameterFeet * propertyFactor;

        Request.Variables.ShellSideVelocity.SetValue(
            new Velocity(shellVelocity, VelocityUnits.FeetPerSecond),
            VariableDefinedBy.Equipment);
        Request.Variables.ShellSideReynoldsNumber.SetValue(new UnitLess(reynolds), VariableDefinedBy.Equipment);
        Request.Variables.ShellSideHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(heatTransferCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }

    protected void CalculatePureWaterCondensingShellSideHeatTransferCoefficient()
    {
        CalculateCondensingShellSideHydraulics();

        Request.Variables.ShellSideHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(1500d, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }

    private void CalculateShellSideHydraulics()
    {
        var properties = ReadShellSideAverageProperties();
        var shellEquivalentDiameterFeet = GetShellEquivalentDiameterFeet();
        var shellFlowAreaSquareFeet = Request.Variables.ShellFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var massFlowLbPerHour = Request.ShellSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var massVelocity = massFlowLbPerHour / shellFlowAreaSquareFeet;
        var shellVelocity = massVelocity / properties.AverageMassDensityLbFt3 / 3600d;
        var reynolds = shellEquivalentDiameterFeet * massVelocity / properties.AverageViscosityLbFtHr;

        Request.Variables.ShellSideVelocity.SetValue(
            new Velocity(shellVelocity, VelocityUnits.FeetPerSecond),
            VariableDefinedBy.Equipment);
        Request.Variables.ShellSideReynoldsNumber.SetValue(new UnitLess(reynolds), VariableDefinedBy.Equipment);
    }

    protected void CalculateMixtureCondensingShellSideHeatTransferCoefficient()
    {
        var properties = ReadShellSideAverageProperties();
        var inletProperties = ReadShellSideInletProperties();
        var shellEquivalentDiameterFeet = GetShellEquivalentDiameterFeet();
        var shellFlowAreaSquareFeet = Request.Variables.ShellFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var massFlowLbPerHour = Request.ShellSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var massVelocity = massFlowLbPerHour / shellFlowAreaSquareFeet;
        var shellVelocity = massVelocity / inletProperties.MassDensityLbFt3 / 3600d;
        var reynolds = shellEquivalentDiameterFeet * massVelocity / inletProperties.ViscosityLbFtHr;
        var jh = JhFig28(reynolds);
        var propertyFactor = properties.AverageThermalConductivityBtuHrFtF *
                             Math.Pow(properties.AverageMassCpBtuLbF * properties.AverageViscosityLbFtHr /
                                      properties.AverageThermalConductivityBtuHrFtF, 1d / 3d);
        var sensibleCoefficient = jh / shellEquivalentDiameterFeet * propertyFactor;
        var condensingCoefficient = EstimateMixtureCondensingShellSideCoefficient();

        Request.Variables.ShellSideVelocity.SetValue(
            new Velocity(shellVelocity, VelocityUnits.FeetPerSecond),
            VariableDefinedBy.Equipment);
        Request.Variables.ShellSideReynoldsNumber.SetValue(new UnitLess(reynolds), VariableDefinedBy.Equipment);
        Request.Variables.ShellSideHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(Math.Max(sensibleCoefficient, condensingCoefficient), HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }

    private void CalculateCondensingShellSideHydraulics()
    {
        var properties = ReadShellSideInletProperties();
        var shellEquivalentDiameterFeet = GetShellEquivalentDiameterFeet();
        var shellFlowAreaSquareFeet = Request.Variables.ShellFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var massFlowLbPerHour = Request.ShellSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var massVelocity = massFlowLbPerHour / shellFlowAreaSquareFeet;
        var shellVelocity = massVelocity / properties.MassDensityLbFt3 / 3600d;
        var reynolds = shellEquivalentDiameterFeet * massVelocity / properties.ViscosityLbFtHr;

        Request.Variables.ShellSideVelocity.SetValue(
            new Velocity(shellVelocity, VelocityUnits.FeetPerSecond),
            VariableDefinedBy.Equipment);
        Request.Variables.ShellSideReynoldsNumber.SetValue(new UnitLess(reynolds), VariableDefinedBy.Equipment);
    }

    private double EstimateMixtureCondensingShellSideCoefficient()
    {
        if (!Request.Variables.TubeSideHeatTransferCoefficient.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side condensation coefficient because tube side coefficient is not defined.");
        }

        var properties = ReadShellSideAverageProperties();
        var tubeOuterDiameterFeet = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var tubeLengthFeet = Request.Variables.TubeLength.Value.GetValue(LengthUnits.Foot);
        var actualTubeCount = Request.Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None);
        var massFlowLbPerHour = Request.ShellSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var g2Prime = massFlowLbPerHour / (Math.PI * actualTubeCount * tubeOuterDiameterFeet);
        var hio = Request.Variables.TubeSideHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);
        var tubeInletTemperature = Request.TubeSideInlet.Stream.Temperature.Value.GetValue(TemperatureUnits.DegreeFahrenheit);
        var tubeOutletTemperature = Request.TubeSideOutlet.Stream.Temperature.Value.GetValue(TemperatureUnits.DegreeFahrenheit);
        var shellVaporTemperature = Request.ShellSideInlet.Stream.Temperature.Value.GetValue(TemperatureUnits.DegreeFahrenheit);
        var averageTubeTemperature = (tubeInletTemperature + tubeOutletTemperature) / 2d;
        var ho = 200d;

        for (var i = 0; i < 20; i++)
        {
            var wallTemperature = averageTubeTemperature + ho / (hio + ho) * (shellVaporTemperature - averageTubeTemperature);
            _ = (shellVaporTemperature + wallTemperature) / 2d;
            var nextHo = 1.5d * Math.Pow(g2Prime * 4d / properties.AverageViscosityLbFtHr, -1d / 3d) /
                         Math.Pow(
                             Math.Pow(properties.AverageViscosityLbFtHr, 2d) /
                             (Math.Pow(properties.AverageThermalConductivityBtuHrFtF, 3d) *
                              Math.Pow(properties.AverageMassDensityLbFt3, 2d) *
                              417118110.23622d),
                             1d / 3d);

            if (Math.Abs(ho - nextHo) < 1e-2)
            {
                return ho;
            }

            ho = nextHo;
        }

        return ho;
    }

    private double GetShellEquivalentDiameterFeet()
    {
        if (Request.Variables.ShellEquivalentDiameter.IsDefined)
        {
            return Request.Variables.ShellEquivalentDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        }

        var pitchFeet = GetTubePitchFeet();
        var tubeOuterDiameterFeet = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;

        return 4d * (pitchFeet * pitchFeet * 0.86d - Math.PI * tubeOuterDiameterFeet * tubeOuterDiameterFeet / 4d) /
               (Math.PI * tubeOuterDiameterFeet);
    }

    private ShellSideAverageProperties ReadShellSideAverageProperties()
    {
        var inlet = Request.ShellSideInlet.Stream;
        var outlet = Request.ShellSideOutlet.Stream;

        EnsureDefined(inlet.Viscosity.IsDefined && outlet.Viscosity.IsDefined, "shell side viscosity");
        EnsureDefined(inlet.MassCp.IsDefined && outlet.MassCp.IsDefined, "shell side mass heat capacity");
        EnsureDefined(inlet.ThermalConductivity.IsDefined && outlet.ThermalConductivity.IsDefined, "shell side thermal conductivity");
        EnsureDefined(inlet.MassDensity.IsDefined && outlet.MassDensity.IsDefined, "shell side mass density");
        EnsureDefined(inlet.MassFlow.IsDefined, "shell side mass flow");

        var inletViscosity = inlet.Viscosity.Value.GetValue(ViscosityUnits.lb_ft_hr);
        var outletViscosity = outlet.Viscosity.Value.GetValue(ViscosityUnits.lb_ft_hr);
        var inletMassCp = inlet.MassCp.Value.GetValue(MassEntropyUnits.BTU_lb_F);
        var outletMassCp = outlet.MassCp.Value.GetValue(MassEntropyUnits.BTU_lb_F);
        var inletThermalConductivity = inlet.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.BTU_ft_hr_ft2_m_F);
        var outletThermalConductivity = outlet.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.BTU_ft_hr_ft2_m_F);
        var inletDensity = inlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);
        var outletDensity = outlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);

        return new ShellSideAverageProperties(
            (inletViscosity + outletViscosity) / 2d,
            (inletMassCp + outletMassCp) / 2d,
            (inletThermalConductivity + outletThermalConductivity) / 2d,
            (inletDensity + outletDensity) / 2d);
    }

    private ShellSideSingleProperties ReadShellSideInletProperties()
    {
        var inlet = Request.ShellSideInlet.Stream;

        EnsureDefined(inlet.Viscosity.IsDefined, "shell side inlet viscosity");
        EnsureDefined(inlet.MassDensity.IsDefined, "shell side inlet mass density");

        return new ShellSideSingleProperties(
            inlet.Viscosity.Value.GetValue(ViscosityUnits.lb_ft_hr),
            inlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3));
    }

    private double JhFig28(double reynolds)
    {
        var baffleSpacingFeet = Request.Variables.BaffleSpacing.Value.GetValue(LengthUnits.Foot);

        return Math.Abs(baffleSpacingFeet) < 1e-12
            ? 6.51287624556527d * Math.Pow(reynolds / 1000d, 0.801675663907211d)
            : 0.50885407315d * Math.Pow(reynolds, 0.517921335094d);
    }

    private sealed record ShellSideAverageProperties(
        double AverageViscosityLbFtHr,
        double AverageMassCpBtuLbF,
        double AverageThermalConductivityBtuHrFtF,
        double AverageMassDensityLbFt3);

    private sealed record ShellSideSingleProperties(
        double ViscosityLbFtHr,
        double MassDensityLbFt3);

    private double GetTubePitchInches() =>
        Request.Variables.TubePitch.Value.GetValue(DiameterUnits.Inch);

    private double GetTubePitchFeet() =>
        GetTubePitchInches() / 12d;

    protected void CalculateLiquidWaterTubeSideHeatTransferCoefficient()
    {
        var properties = ReadTubeSideAverageProperties();
        var tubeInnerDiameterFeet = Request.Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var tubeOuterDiameterFeet = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var tubeVelocityFeetPerSecond = Request.Variables.TubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond);
        var reynolds = CalculateTubeSideReynoldsNumber(properties.AverageViscosityLbFtHr);

        var heatTransferCoefficient =
            (1.7492d * properties.AverageTemperatureFahrenheit + 157.2111d) *
            Math.Pow(tubeVelocityFeetPerSecond, 0.806585770079556d);
        var correctionFactor = 0.906395121079d * Math.Pow(tubeInnerDiameterFeet * 12d, -0.202330180988d);
        heatTransferCoefficient *= correctionFactor;
        heatTransferCoefficient *= tubeInnerDiameterFeet / tubeOuterDiameterFeet;

        Request.Variables.TubeSideReynoldsNumber.SetValue(new UnitLess(reynolds), VariableDefinedBy.Equipment);
        Request.Variables.TubeSideHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(heatTransferCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }

    protected void CalculateLiquidMixtureTubeSideHeatTransferCoefficient()
    {
        var properties = ReadTubeSideAverageProperties();
        var tubeInnerDiameterFeet = Request.Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var tubeOuterDiameterFeet = Request.Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var reynolds = CalculateTubeSideReynoldsNumber(properties.AverageViscosityLbFtHr);
        var jh = 6.51287624556527d * Math.Pow(reynolds / 1000d, 0.801675663907211d);
        var propertyFactor = properties.AverageThermalConductivityBtuHrFtF *
                             Math.Pow(properties.AverageMassCpBtuLbF * properties.AverageViscosityLbFtHr /
                                      properties.AverageThermalConductivityBtuHrFtF, 1d / 3d);
        var heatTransferCoefficient = propertyFactor * jh / tubeInnerDiameterFeet;
        heatTransferCoefficient *= tubeInnerDiameterFeet / tubeOuterDiameterFeet;

        Request.Variables.TubeSideReynoldsNumber.SetValue(new UnitLess(reynolds), VariableDefinedBy.Equipment);
        Request.Variables.TubeSideHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(heatTransferCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }

    private double CalculateTubeSideReynoldsNumber(double averageViscosityLbFtHr)
    {
        var massFlowLbPerHour = Request.TubeSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var actualTubeFlowAreaSquareFeet = Request.Variables.ActualTubeFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var tubeInnerDiameterFeet = Request.Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var massVelocity = massFlowLbPerHour / actualTubeFlowAreaSquareFeet;

        return tubeInnerDiameterFeet * massVelocity / averageViscosityLbFtHr;
    }

    protected void CalculateLiquidTubeSidePressureDrop()
    {
        var properties = ReadTubeSideAverageProperties();
        CalculateLiquidTubeSidePressureDrop(properties.AverageSpecificGravity);
    }

    private void CalculateLiquidTubeSidePressureDrop(double averageSpecificGravity)
    {
        var reynolds = Request.Variables.TubeSideReynoldsNumber.Value.GetValue(UnitLessUnits.None);
        var frictionFactor = 47.618905632321d * Math.Pow(reynolds / 1000d, -0.252212058238d) / 100000d;
        var massFlowLbPerHour = Request.TubeSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var actualTubeFlowAreaSquareFeet = Request.Variables.ActualTubeFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var massVelocity = massFlowLbPerHour / actualTubeFlowAreaSquareFeet;
        var tubeInnerDiameterFeet = Request.Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var tubeLengthFeet = Request.Variables.TubeLength.Value.GetValue(LengthUnits.Foot);
        var tubePasses = Request.Variables.TubePasses.Value.GetValue(UnitLessUnits.None);
        var tubePressureDrop = frictionFactor * Math.Pow(massVelocity, 2d) * tubeLengthFeet * tubePasses /
                               (5.22e10d * tubeInnerDiameterFeet * averageSpecificGravity);
        var returnVelocityHead = 0.001529180217d * Math.Pow(massVelocity / 1000d, 1.982454700288d) / 10000d;
        var returnPressureDrop = returnVelocityHead * 4d * tubePasses / averageSpecificGravity;

        Request.Variables.TubeSideFrictionFactor.SetValue(new UnitLess(frictionFactor), VariableDefinedBy.Equipment);
        Request.Variables.TubeSidePressureDrop.SetValue(
            new PressureDrop(tubePressureDrop + returnPressureDrop, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
    }

    private TubeSideAverageProperties ReadTubeSideAverageProperties()
    {
        var inlet = Request.TubeSideInlet.Stream;
        var outlet = Request.TubeSideOutlet.Stream;

        EnsureDefined(inlet.Viscosity.IsDefined && outlet.Viscosity.IsDefined, "viscosity");
        EnsureDefined(inlet.MassCp.IsDefined && outlet.MassCp.IsDefined, "mass heat capacity");
        EnsureDefined(inlet.ThermalConductivity.IsDefined && outlet.ThermalConductivity.IsDefined, "thermal conductivity");
        EnsureDefined(inlet.Temperature.IsDefined && outlet.Temperature.IsDefined, "temperature");
        EnsureDefined(inlet.MassDensity.IsDefined && outlet.MassDensity.IsDefined, "mass density");
        EnsureDefined(inlet.MassFlow.IsDefined, "mass flow");

        var inletViscosity = inlet.Viscosity.Value.GetValue(ViscosityUnits.lb_ft_hr);
        var outletViscosity = outlet.Viscosity.Value.GetValue(ViscosityUnits.lb_ft_hr);
        var inletMassCp = inlet.MassCp.Value.GetValue(MassEntropyUnits.BTU_lb_F);
        var outletMassCp = outlet.MassCp.Value.GetValue(MassEntropyUnits.BTU_lb_F);
        var inletThermalConductivity = inlet.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.BTU_ft_hr_ft2_m_F);
        var outletThermalConductivity = outlet.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.BTU_ft_hr_ft2_m_F);
        var inletTemperature = inlet.Temperature.Value.GetValue(TemperatureUnits.DegreeFahrenheit);
        var outletTemperature = outlet.Temperature.Value.GetValue(TemperatureUnits.DegreeFahrenheit);
        var inletDensity = inlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);
        var outletDensity = outlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);

        return new TubeSideAverageProperties(
            (inletViscosity + outletViscosity) / 2d,
            (inletMassCp + outletMassCp) / 2d,
            (inletThermalConductivity + outletThermalConductivity) / 2d,
            (inletTemperature + outletTemperature) / 2d,
            (inletDensity + outletDensity) / 2d / 62.4d);
    }

    private static void EnsureDefined(bool isDefined, string propertyName)
    {
        if (!isDefined)
        {
            throw new InvalidOperationException($"Cannot calculate tube side because tube side {propertyName} is not defined.");
        }
    }

    private sealed record TubeSideAverageProperties(
        double AverageViscosityLbFtHr,
        double AverageMassCpBtuLbF,
        double AverageThermalConductivityBtuHrFtF,
        double AverageTemperatureFahrenheit,
        double AverageSpecificGravity);

    protected virtual void CalculateCleanOverallCoefficient()
    {
        if (!Request.Variables.TubeSideHeatTransferCoefficient.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate clean overall coefficient because tube side heat transfer coefficient is not defined.");
        }

        if (!Request.Variables.ShellSideHeatTransferCoefficient.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate clean overall coefficient because shell side heat transfer coefficient is not defined.");
        }

        var tubeSideCoefficient = Request.Variables.TubeSideHeatTransferCoefficient.Value
            .GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);
        var shellSideCoefficient = Request.Variables.ShellSideHeatTransferCoefficient.Value
            .GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);

        if (Math.Abs(tubeSideCoefficient + shellSideCoefficient) < 1e-12)
        {
            throw new InvalidOperationException("Cannot calculate clean overall coefficient because the coefficient sum is zero.");
        }

        var cleanOverallCoefficient =
            tubeSideCoefficient * shellSideCoefficient / (tubeSideCoefficient + shellSideCoefficient);

        Request.Variables.CleanOverallCoefficient.SetValue(
            new HeatTransferCoefficient(cleanOverallCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateFoulingResistance()
    {
        if (!Request.Variables.CleanOverallCoefficient.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate fouling resistance because clean overall coefficient is not defined.");
        }

        if (!Request.Variables.CalculatedDirtyOverallCoefficient.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate fouling resistance because calculated dirty overall coefficient is not defined.");
        }

        var cleanOverallCoefficient = Request.Variables.CleanOverallCoefficient.Value
            .GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);
        var calculatedDirtyOverallCoefficient = Request.Variables.CalculatedDirtyOverallCoefficient.Value
            .GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);

        if (Math.Abs(cleanOverallCoefficient * calculatedDirtyOverallCoefficient) < 1e-12)
        {
            throw new InvalidOperationException("Cannot calculate fouling resistance because Uc or Ud is zero.");
        }

        var calculatedFoulingResistance =
            (cleanOverallCoefficient - calculatedDirtyOverallCoefficient) /
            (cleanOverallCoefficient * calculatedDirtyOverallCoefficient);

        Request.Variables.CalculatedFoulingResistance.SetValue(
            new UnitLess(calculatedFoulingResistance),
            VariableDefinedBy.Equipment);
    }

    protected virtual void CalculateShellSidePressureDrop() => MarkRequired();

    protected void CalculateLiquidShellSidePressureDrop()
    {
        if (!Request.Variables.BaffleSpacing.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because baffle spacing is not defined.");
        }

        if (!Request.Variables.TubeLength.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because tube length is not defined.");
        }

        if (!Request.Variables.ShellInsideDiameter.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell inside diameter is not defined.");
        }

        if (!Request.Variables.ShellFlowArea.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell flow area is not defined.");
        }

        if (!Request.Variables.ShellEquivalentDiameter.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell equivalent diameter is not defined.");
        }

        if (!Request.Variables.ShellPasses.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell passes are not defined.");
        }

        if (!Request.Variables.ShellSideReynoldsNumber.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell side Reynolds number is not defined.");
        }

        if (!Request.ShellSideInlet.Stream.MassFlow.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell side mass flow is not defined.");
        }

        if (!Request.ShellSideInlet.Stream.MassDensity.IsDefined || !Request.ShellSideOutlet.Stream.MassDensity.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell side density is not defined.");
        }

        var baffleSpacingFeet = Request.Variables.BaffleSpacing.Value.GetValue(LengthUnits.Foot);
        var tubeLengthFeet = Request.Variables.TubeLength.Value.GetValue(LengthUnits.Foot);
        var shellInsideDiameterFeet = Request.Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var shellFlowAreaSquareFeet = Request.Variables.ShellFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var massFlowLbPerHour = Request.ShellSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var inletDensityLbFt3 = Request.ShellSideInlet.Stream.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);
        var outletDensityLbFt3 = Request.ShellSideOutlet.Stream.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);
        var averageSpecificGravity = (inletDensityLbFt3 + outletDensityLbFt3) / 2d / 62.4d;
        var shellPasses = Request.Variables.ShellPasses.Value.GetValue(UnitLessUnits.None);
        var reynolds = Request.Variables.ShellSideReynoldsNumber.Value.GetValue(UnitLessUnits.None);
        var frictionFactor = FrictionFig29(reynolds);
        var massVelocity = massFlowLbPerHour / shellFlowAreaSquareFeet;
        var equivalentDiameterFeet = Request.Variables.ShellEquivalentDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var crossflowSections = Math.Abs(baffleSpacingFeet) < 1e-12
            ? 0d
            : tubeLengthFeet / baffleSpacingFeet;

        double shellPressureDrop;

        if (Math.Abs(baffleSpacingFeet) < 1e-12)
        {
            shellPressureDrop = frictionFactor * Math.Pow(massVelocity, 2d) * tubeLengthFeet * shellPasses /
                                (52200000000d * equivalentDiameterFeet * averageSpecificGravity);
        }
        else
        {
            shellPressureDrop = frictionFactor * Math.Pow(massVelocity, 2d) * shellInsideDiameterFeet * crossflowSections /
                                (52200000000d * equivalentDiameterFeet * averageSpecificGravity);
        }

        Request.Variables.ShellSideFrictionFactor.SetValue(new UnitLess(frictionFactor), VariableDefinedBy.Equipment);
        Request.Variables.ShellSideCrossflowSections.SetValue(new UnitLess(crossflowSections), VariableDefinedBy.Equipment);
        Request.Variables.ShellSidePressureDrop.SetValue(
            new PressureDrop(shellPressureDrop, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
    }

    protected void CalculateCondensingShellSidePressureDrop()
    {
        if (!Request.Variables.BaffleSpacing.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because baffle spacing is not defined.");
        }

        if (!Request.Variables.TubeLength.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because tube length is not defined.");
        }

        if (!Request.Variables.ShellInsideDiameter.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell inside diameter is not defined.");
        }

        if (!Request.Variables.ShellFlowArea.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell flow area is not defined.");
        }

        if (!Request.Variables.ShellEquivalentDiameter.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell equivalent diameter is not defined.");
        }

        if (!Request.Variables.ShellPasses.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell passes are not defined.");
        }

        if (!Request.Variables.ShellSideReynoldsNumber.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell side Reynolds number is not defined.");
        }

        if (!Request.ShellSideInlet.Stream.MassFlow.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell side mass flow is not defined.");
        }

        if (!Request.ShellSideInlet.Stream.MassDensity.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate shell side pressure drop because shell side inlet density is not defined.");
        }

        var baffleSpacingFeet = Request.Variables.BaffleSpacing.Value.GetValue(LengthUnits.Foot);
        var tubeLengthFeet = Request.Variables.TubeLength.Value.GetValue(LengthUnits.Foot);
        var shellInsideDiameterFeet = Request.Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var shellFlowAreaSquareFeet = Request.Variables.ShellFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var massFlowLbPerHour = Request.ShellSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var inletDensityLbFt3 = Request.ShellSideInlet.Stream.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);
        var inletSpecificGravity = inletDensityLbFt3 / 62.4d;
        var shellPasses = Request.Variables.ShellPasses.Value.GetValue(UnitLessUnits.None);
        var reynolds = Request.Variables.ShellSideReynoldsNumber.Value.GetValue(UnitLessUnits.None);
        var frictionFactor = FrictionFig29(reynolds);
        var massVelocity = massFlowLbPerHour / shellFlowAreaSquareFeet;
        var equivalentDiameterFeet = Request.Variables.ShellEquivalentDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var crossflowSections = Math.Abs(baffleSpacingFeet) < 1e-12
            ? 0d
            : tubeLengthFeet / baffleSpacingFeet;

        double shellPressureDrop;

        if (Math.Abs(baffleSpacingFeet) < 1e-12)
        {
            shellPressureDrop = frictionFactor * Math.Pow(massVelocity, 2d) * tubeLengthFeet * shellPasses /
                                (52200000000d * equivalentDiameterFeet * inletSpecificGravity);
        }
        else
        {
            shellPressureDrop = frictionFactor * Math.Pow(massVelocity, 2d) * shellInsideDiameterFeet * crossflowSections /
                                (52200000000d * equivalentDiameterFeet * inletSpecificGravity);
        }

        shellPressureDrop /= 2d;

        Request.Variables.ShellSideFrictionFactor.SetValue(new UnitLess(frictionFactor), VariableDefinedBy.Equipment);
        Request.Variables.ShellSideCrossflowSections.SetValue(new UnitLess(crossflowSections), VariableDefinedBy.Equipment);
        Request.Variables.ShellSidePressureDrop.SetValue(
            new PressureDrop(shellPressureDrop, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
    }

    private double FrictionFig29(double reynolds)
    {
        var baffleSpacingFeet = Request.Variables.BaffleSpacing.Value.GetValue(LengthUnits.Foot);

        if (Math.Abs(baffleSpacingFeet) < 1e-12)
        {
            return 47.618905632321d * Math.Pow(reynolds / 1000d, -0.252212058238d) / 100000d;
        }

        double result;

        if (reynolds < 51d)
        {
            result = 3914.78115052459d * Math.Pow(reynolds, -0.955368922774d);
        }
        else if (reynolds < 400d)
        {
            result = 0.000000013788d * Math.Pow(reynolds, 4d)
                     - 0.000016250029d * Math.Pow(reynolds, 3d)
                     + 0.006846455076d * Math.Pow(reynolds, 2d)
                     - 1.289957589873d * reynolds
                     + 137.822190837813d;
        }
        else
        {
            result = 102.097227258065d * Math.Pow(reynolds, -0.17634947761d);
        }

        return result / 10000d;
    }

    protected virtual void VerifyAssumedDirtyOverallCoefficient()
    {
        State.ContinueDesignIteration = false;

        if (!Request.Variables.CleanOverallCoefficient.IsDefined)
        {
            throw new InvalidOperationException("Cannot verify dirty overall coefficient because clean overall coefficient is not defined.");
        }

        if (!Request.Variables.AllowedFoulingResistance.IsDefined)
        {
            throw new InvalidOperationException("Cannot verify dirty overall coefficient because allowed fouling resistance is not defined.");
        }

        if (!Request.Variables.CalculatedFoulingResistance.IsDefined)
        {
            throw new InvalidOperationException("Cannot verify dirty overall coefficient because calculated fouling resistance is not defined.");
        }

        var allowedFoulingResistance = Request.Variables.AllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);
        var calculatedFoulingResistance = Request.Variables.CalculatedFoulingResistance.Value.GetValue(UnitLessUnits.None);
        var currentAssumedDirtyOverallCoefficient = Request.Variables.AssumedDirtyOverallCoefficient.Value
            .GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);
        var difference = calculatedFoulingResistance - allowedFoulingResistance;

        if (calculatedFoulingResistance < 0d)
        {
            State.DirtyOverallCoefficientIsDecreasing = false;
        }

        if (difference > 0d)
        {
            return;
        }

        if (IsUserDefined(Request.Variables.AssumedDirtyOverallCoefficient) ||
            IsUserDefined(Request.Variables.ActualTubeCount))
        {
            State.Message = "Calculated fouling resistance is lower than the required fouling resistance.";
            return;
        }

        var cleanOverallCoefficient = Request.Variables.CleanOverallCoefficient.Value
            .GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);
        var requiredDirtyOverallCoefficient = cleanOverallCoefficient / (allowedFoulingResistance * cleanOverallCoefficient + 1d);

        if (Math.Abs(requiredDirtyOverallCoefficient - currentAssumedDirtyOverallCoefficient) < 1e-8 ||
            requiredDirtyOverallCoefficient < 0d)
        {
            return;
        }

        Request.Variables.AssumedDirtyOverallCoefficient.SetValue(
            new HeatTransferCoefficient(requiredDirtyOverallCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);

        CalculateRequiredAreaFromDirtyOverallCoefficient(requiredDirtyOverallCoefficient);
        State.ContinueDesignIteration = true;
    }

    protected virtual void VerifyTubeSidePressureDrop()
    {
        if (!Request.Variables.TubeSidePressureDrop.IsDefined)
        {
            throw new InvalidOperationException("Cannot verify tube pressure drop because tube pressure drop is not defined.");
        }

        if (!Request.Variables.ActualTubeCount.IsDefined)
        {
            throw new InvalidOperationException("Cannot verify tube pressure drop because actual tube count is not defined.");
        }

        var tubePressureDropPsi = Request.Variables.TubeSidePressureDrop.Value.GetValue(PressureDropUnits.psi);

        if (tubePressureDropPsi <= DefaultAllowedTubePressureDropPsi)
        {
            return;
        }

        if (IsUserDefined(Request.Variables.ActualTubeCount))
        {
            State.Message = "Tube side pressure drop exceeds the allowed pressure drop for the selected tube count.";
            return;
        }

        var actualTubeCount = Request.Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None);
        var estimatedTubeCount = Math.Ceiling(actualTubeCount * Math.Sqrt(tubePressureDropPsi / DefaultAllowedTubePressureDropPsi) * 1.02d);

        if (Request.Variables.TubeVelocity.IsDefined && Request.Variables.MinimumTubeVelocity.IsDefined)
        {
            var tubeVelocity = Request.Variables.TubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond);
            var minimumTubeVelocity = Request.Variables.MinimumTubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond);

            if (minimumTubeVelocity > 0d && tubeVelocity > 0d)
            {
                var maximumTubeCountFromVelocity = Math.Floor(actualTubeCount * tubeVelocity / minimumTubeVelocity);
                estimatedTubeCount = Math.Min(estimatedTubeCount, Math.Max(actualTubeCount + 1d, maximumTubeCountFromVelocity));
            }
        }

        if (estimatedTubeCount <= actualTubeCount)
        {
            State.Message = "Tube side pressure drop exceeds the allowed pressure drop, but no automatic tube count increase is available without violating velocity constraints.";
            return;
        }

        State.MinimumTubeCountRequiredByPressureDrop = Math.Max(
            State.MinimumTubeCountRequiredByPressureDrop,
            estimatedTubeCount);

        Request.Variables.ActualTubeCount.SetValue(
            new UnitLess(estimatedTubeCount),
            VariableDefinedBy.Equipment);

        State.ContinueDesignIteration = true;
    }

    private void CalculateRequiredAreaFromDirtyOverallCoefficient(double dirtyOverallCoefficient)
    {
        if (!Request.Variables.HeatDuty.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate required area because heat duty is not defined.");
        }

        if (!Request.Variables.LogMeanTemperatureDifference.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate required area because LMTD is not defined.");
        }

        var heatDutyBtuPerHour = Math.Abs(Request.Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr));
        var lmtdFahrenheit = Request.Variables.LogMeanTemperatureDifference.Value.GetValue(TemperatureUnits.DegreeFahrenheit);

        if (Math.Abs(lmtdFahrenheit * dirtyOverallCoefficient) < 1e-12)
        {
            throw new InvalidOperationException("Cannot calculate required area because LMTD or Ud is zero.");
        }

        var requiredArea = heatDutyBtuPerHour / (lmtdFahrenheit * dirtyOverallCoefficient);

        Request.Variables.RequiredArea.SetValue(
            new Area(requiredArea, SurfaceUnits.Foot2),
            VariableDefinedBy.Equipment);
    }

    protected virtual bool ShouldContinueDesignIteration()
    {
        var continueIteration = State.ContinueDesignIteration;
        State.ContinueDesignIteration = false;

        if (State.StopRequested || !string.IsNullOrWhiteSpace(State.Message))
        {
            return false;
        }

        if (!continueIteration)
        {
            return false;
        }

        if (State.DesignIterationCount >= MaximumDesignIterations)
        {
            State.Message = "The heat exchanger design did not converge within the maximum iteration count.";
            RequestStop();
            return false;
        }

        return true;
    }

    protected virtual HeatExchangerDesignResult BuildResult()
    {
        return new HeatExchangerDesignResult
        {
            DesignType = DesignType,
            Message = State.Message,
            RequiredMethodImplementations = State.RequiredMethodImplementations.Distinct().ToArray()
        };
    }

    protected void RequestStop()
    {
        State.StopRequested = true;
    }

    protected void StopIfMessageExists()
    {
        if (!string.IsNullOrWhiteSpace(State.Message))
        {
            State.ContinueDesignIteration = false;
            RequestStop();
        }
    }

    protected void MarkRequired([System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
    {
        State.RequiredMethodImplementations.Add($"{GetType().Name}.{methodName}");
    }

    protected static void SetIfUndefined<T>(Variable<T> variable, T value)
        where T : Amount
    {
        if (!variable.IsDefined)
        {
            variable.SetValue(value, VariableDefinedBy.Equipment);
        }
    }

    protected void InitializeAssumedDirtyOverallCoefficientFromPreviousCalculation(double fallbackValue)
    {
        if (Request.Variables.AssumedDirtyOverallCoefficient.IsDefined)
        {
            return;
        }

        if (Request.Variables.LastCalculatedDirtyOverallCoefficient.IsDefined)
        {
            Request.Variables.AssumedDirtyOverallCoefficient.SetValue(
                Request.Variables.LastCalculatedDirtyOverallCoefficient.Value,
                VariableDefinedBy.Equipment);
            return;
        }

        Request.Variables.AssumedDirtyOverallCoefficient.SetValue(
            new HeatTransferCoefficient(fallbackValue, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }
}
