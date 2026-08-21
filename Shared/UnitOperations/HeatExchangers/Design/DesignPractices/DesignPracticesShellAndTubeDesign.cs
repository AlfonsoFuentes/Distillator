using Shared.ProcessFlowDiagram.Designs;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class DesignPracticesShellAndTubeDesign : IHeatExchangerDesign
{
    private const int MaximumIterations = 40;
    private const double PreliminaryTotalNozzleLossCoefficient = 2d;

    private readonly HeatExchangerDesignRequest request;
    private readonly DesignPracticesProcessRegime processRegime;
    private readonly List<string> recommendations = [];
    private readonly List<string> requiredMethodImplementations = [];
    private IReadOnlyList<DesignPracticesCondensationZone> condensationZones = [];

    public DesignPracticesShellAndTubeDesign(
        HeatExchangerDesignRequest request,
        DesignPracticesProcessRegime processRegime)
    {
        this.request = request;
        this.processRegime = processRegime;
    }

    private ShellAndTubeDesignVariables Variables => request.Variables;

    public IDesignResult Calculate()
    {
        recommendations.Clear();
        requiredMethodImplementations.Clear();

        ApplyDp09GeneralDefaults();
        ApplyDp09ShellAndTubeDefaults();
        CalculateTubeGeometry();
        CalculateHeatDuty();
        CalculateEffectiveTemperatureDifference();
        CalculateInitialArea();
        CalculateTubeCountAndShellSize();
        CalculateHydraulicsAndCoefficients();
        CalculateOverallCoefficients();
        CalculateDp09dNozzleReview();
        AddDp09dOptimizationReview();
        AddDp09ConstructionAndMaintenanceReview();
        AddDp09aEnhancedHeatTransferReview();
        AddDp09cShellTypeReview();
        AddDp09bCondenserArrangementReview();
        AddDp09fCondensationServiceReview();
        AddDp09bCoolingWaterTemperatureReview();

        return new HeatExchangerDesignResult
        {
            DesignType = $"DesignPracticesShellAndTubeDesign.{processRegime}",
            CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices,
            Message = string.Empty,
            RequiredMethodImplementations = requiredMethodImplementations.Distinct().ToArray(),
            Recommendations = recommendations.Distinct().ToArray()
        };
    }

    private void ApplyDp09GeneralDefaults()
    {
        SetIfUndefined(Variables.ShellPasses, new UnitLess(1));
        // Default DP original: SetIfUndefined(Variables.TubePasses, new UnitLess(-1));
        SetIfUndefined(Variables.TubePasses, new UnitLess(2));

        if (!Variables.TubeSideAllowedFoulingResistance.IsDefined)
        {
            // Default DP original: new UnitLess(0.001d)
            Variables.TubeSideAllowedFoulingResistance.SetValue(new UnitLess(0.00010d), VariableDefinedBy.Equipment);
        }

        if (!Variables.ShellSideAllowedFoulingResistance.IsDefined)
        {
            // Default DP original: new UnitLess(0.001d)
            Variables.ShellSideAllowedFoulingResistance.SetValue(new UnitLess(0.00025d), VariableDefinedBy.Equipment);
        }

        var fouling = Variables.TubeSideAllowedFoulingResistance.Value.GetValue(UnitLessUnits.None) +
                      Variables.ShellSideAllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);
        Variables.AllowedFoulingResistance.SetValue(new UnitLess(fouling), VariableDefinedBy.Equipment);
    }

    private void ApplyDp09ShellAndTubeDefaults()
    {
        var shellFouling = Variables.ShellSideAllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);
        var tubeFouling = Variables.TubeSideAllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);
        var selectedTubeOd = tubeFouling > 0.002d || shellFouling > 0.002d ? 1d : 0.75d;
        var selectedGauge = selectedTubeOd < 1d ? 16 : 14;
        var selectedWallThickness = Dp09cShellAndTubeCatalog.GetTubeWallThicknessInches(selectedGauge);

        // Banco temporal Enerquip 65168: restaurar los defaults DP originales al cerrar la auditoría.
        // Defaults DP originales:
        // SetIfUndefined(Variables.TubeGauge, new UnitLess(selectedGauge));
        // SetIfUndefined(Variables.TubeNominalDiameter, new Diameter(selectedTubeOd, DiameterUnits.Inch));
        // SetIfUndefined(Variables.TubeOuterDiameter, new Diameter(selectedTubeOd, DiameterUnits.Inch));
        // SetIfUndefined(Variables.TubeInnerDiameter, new Diameter(selectedTubeOd - 2d * selectedWallThickness, DiameterUnits.Inch));
        // SetIfUndefined(Variables.TubeLength, new Length(20d, LengthUnits.Foot));
        // SetIfUndefined(Variables.TubePitch, new Diameter(selectedTubeOd * 1.25d, DiameterUnits.Inch));
        // SetIfUndefined(Variables.BaffleSpacing, new Length(12d, LengthUnits.Inch));
        // SetIfUndefined(Variables.BaffleCutPercent, new UnitLess(25d));
        SetIfUndefined(Variables.TubeGauge, new UnitLess(20d));
        SetIfUndefined(Variables.TubeNominalDiameter, new Diameter(0.625d, DiameterUnits.Inch));
        SetIfUndefined(Variables.TubeOuterDiameter, new Diameter(0.625d, DiameterUnits.Inch));
        SetIfUndefined(Variables.TubeInnerDiameter, new Diameter(0.555d, DiameterUnits.Inch));
        SetIfUndefined(Variables.TubeLength, new Length(4d, LengthUnits.Foot));
        SetIfUndefined(Variables.TubePitch, new Diameter(0.7812d, DiameterUnits.Inch));
        SetIfUndefined(Variables.BaffleSpacing, new Length(9.5d, LengthUnits.Inch));
        SetIfUndefined(Variables.BaffleCutPercent, new UnitLess(25d));
        SetIfUndefined(Variables.ShellInsideDiameter, new Diameter(6.407d, DiameterUnits.Inch));
        SetIfUndefined(Variables.ActualTubeCount, new UnitLess(32d));
        SetIfUndefined(Variables.MinimumTubeVelocity, new Velocity(3d, VelocityUnits.FeetPerSecond));
        ApplyDp09cRearHeadDefault();

        if (shellFouling > 0.002d)
        {
            Variables.TubeLayout = ShellAndTubeTubeLayout.Square;
            recommendations.Add("DP09C: square layout is selected/recommended when shell-side fouling or mechanical cleaning controls.");
        }
        else
        {
            Variables.TubeLayout = ShellAndTubeTubeLayout.Triangular;
        }

        if (processRegime == DesignPracticesProcessRegime.ShellSideCondensation)
        {
            recommendations.Add("DP09C: condensing process vapors are normally allocated to the shell side.");
            recommendations.Add("DP09F: final condenser design requires zone-by-zone heat release data when desuperheating, subcooling, wide-cut condensation, steam, or noncondensables are present.");

            if (IsPureWater(request.ShellSideInlet.Stream))
            {
                recommendations.Add("DP09C: condensing steam is normally allocated to the tube side; shell-side steam condensation should be reviewed.");
            }
        }

        if (processRegime == DesignPracticesProcessRegime.TubeSideCondensation)
        {
            recommendations.Add("DP09F: tube-side condensation is uncommon; use the DP09F tube-side condensing path and flag for engineering review.");
        }
    }

    private void CalculateTubeGeometry()
    {
        var outerDiameterInches = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var innerDiameterInches = Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeLengthFeet = Variables.TubeLength.Value.GetValue(LengthUnits.Foot);

        if (outerDiameterInches <= 0d || innerDiameterInches <= 0d || innerDiameterInches >= outerDiameterInches)
        {
            throw new InvalidOperationException("Cannot calculate Design Practices geometry because tube OD/ID are not physically valid.");
        }

        Variables.TubeFlowArea.SetValue(
            new Area(Math.PI * innerDiameterInches * innerDiameterInches / 4d, SurfaceUnits.inch2),
            VariableDefinedBy.Equipment);
        Variables.TubeClearance.SetValue(
            new Length(Variables.TubePitch.Value.GetValue(DiameterUnits.Inch) - outerDiameterInches, LengthUnits.Inch),
            VariableDefinedBy.Equipment);
        Variables.TubeSurfaceArea.SetValue(
            new Area(Math.PI * outerDiameterInches / 12d * tubeLengthFeet, SurfaceUnits.Foot2),
            VariableDefinedBy.Equipment);
    }

    private void CalculateHeatDuty()
    {
        if (TryCalculateSideHeatDuty(request.TubeSideInlet, request.TubeSideOutlet, out var tubeDuty) ||
            TryCalculateSideHeatDuty(request.ShellSideInlet, request.ShellSideOutlet, out tubeDuty))
        {
            Variables.HeatDuty.SetValue(new EnergyFlow(tubeDuty, EnergyFlowUnits.BTUhr), VariableDefinedBy.Equipment);
            return;
        }

        throw new InvalidOperationException("Cannot calculate Design Practices heat duty because stream enthalpy flows are not defined.");
    }

    private void CalculateEffectiveTemperatureDifference()
    {
        if (IsCondensationRegime())
        {
            CalculateDp09fCondensationEffectiveTemperatureDifference();
            return;
        }

        CalculateDp09dSinglePhaseEffectiveTemperatureDifference();
    }

    private bool IsCondensationRegime()
    {
        return processRegime is DesignPracticesProcessRegime.ShellSideCondensation or DesignPracticesProcessRegime.TubeSideCondensation;
    }

    private void CalculateDp09fCondensationEffectiveTemperatureDifference()
    {
        var duty = Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr);
        condensationZones = Dp09fCondensationZoneModel.BuildPreliminaryZones(request, processRegime, duty);
        var uncorrectedWeightedLmtd = Dp09fCondensationZoneModel.CalculateWeightedEffectiveLmtd(condensationZones);
        condensationZones = ApplyDp09fZoneTemperatureCorrectionFactors(condensationZones);
        var weightedLmtd = Dp09fCondensationZoneModel.CalculateWeightedEffectiveLmtd(condensationZones);
        var effectiveCorrectionFactor = weightedLmtd / Math.Max(uncorrectedWeightedLmtd, 1e-12);

        Variables.LogMeanTemperatureCorrectionFactor.SetValue(new UnitLess(effectiveCorrectionFactor), VariableDefinedBy.Equipment);
        Variables.LogMeanTemperatureDifference.SetValue(
            new Temperature(weightedLmtd, TemperatureUnits.DegreeFahrenheit),
            VariableDefinedBy.Equipment);

        AddDp09fCondensationZoningRecommendations(effectiveCorrectionFactor);
    }

    private void AddDp09fCondensationZoningRecommendations(double effectiveCorrectionFactor)
    {
        var zoningBasis = condensationZones.Any(zone => zone.Name.Contains("sensible", StringComparison.OrdinalIgnoreCase))
            ? "preliminary sensible-duty"
            : "preliminary/inferred";

        recommendations.Add($"DP09F: condenser zoning is active with {condensationZones.Count} {zoningBasis} zone(s); full T-Q zone splitting remains required for definitive wide-cut designs.");
        if (effectiveCorrectionFactor < 0.99d)
        {
            recommendations.Add($"DP09F: zone LMTDs were corrected with an effective temperature factor of {effectiveCorrectionFactor:0.###}.");
        }
    }

    private void CalculateDp09dSinglePhaseEffectiveTemperatureDifference()
    {
        var shellIn = ReadTemperatureF(request.ShellSideInlet.Stream);
        var shellOut = ReadTemperatureF(request.ShellSideOutlet.Stream);
        var tubeIn = ReadTemperatureF(request.TubeSideInlet.Stream);
        var tubeOut = ReadTemperatureF(request.TubeSideOutlet.Stream);
        var shellAverage = (shellIn + shellOut) / 2d;
        var tubeAverage = (tubeIn + tubeOut) / 2d;
        var dt1 = shellAverage >= tubeAverage ? shellIn - tubeOut : tubeIn - shellOut;
        var dt2 = shellAverage >= tubeAverage ? shellOut - tubeIn : tubeOut - shellIn;
        var lmtd = CalculateLmtd(dt1, dt2);
        var correctedLmtd = ApplyDp09dTemperatureCorrectionFactor(lmtd, shellIn, shellOut, tubeIn, tubeOut);

        Variables.LogMeanTemperatureDifference.SetValue(new Temperature(correctedLmtd, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.Equipment);
    }

    private IReadOnlyList<DesignPracticesCondensationZone> ApplyDp09fZoneTemperatureCorrectionFactors(
        IReadOnlyList<DesignPracticesCondensationZone> zones)
    {
        var shellPasses = Variables.ShellPasses.Value.GetValue(UnitLessUnits.None);
        var tubePasses = Variables.TubePasses.Value.GetValue(UnitLessUnits.None);
        if (shellPasses <= 1d && tubePasses <= 1d)
        {
            return zones;
        }

        return zones
            .Select(zone =>
            {
                var correctionFactor = Dp09dShellAndTubeCorrelations.TemperatureCorrectionFactor(
                    Math.Max(shellPasses, 1d),
                    zone.CondensingInletTemperatureF,
                    zone.CondensingOutletTemperatureF,
                    zone.CoolingInletTemperatureF,
                    zone.CoolingOutletTemperatureF);

                return zone with
                {
                    LogMeanTemperatureDifferenceF = zone.LogMeanTemperatureDifferenceF * correctionFactor
                };
            })
            .ToArray();
    }

    private void CalculateInitialArea()
    {
        var duty = Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr);
        var lmtd = Variables.LogMeanTemperatureDifference.Value.GetValue(TemperatureUnits.DegreeFahrenheit);
        var typicalU = GetDp09InitialOverallCoefficientRange();
        var assumedU = typicalU.MidpointBtuPerHourSquareFootFahrenheit;
        var assumedArea = duty / (assumedU * lmtd);

        SetTypicalOverallCoefficientRange(typicalU);
        EnsureAssumedDirtyOverallCoefficient(assumedU);
        SetPreliminaryArea(assumedArea);
        recommendations.Add($"DP09B: initial area uses {typicalU.Basis}; assumed U is {assumedU:0.##} Btu/hr-ft2-F.");
        CalculateDp09fCondensationZoneAreaFractions(assumedU);
    }

    private void SetTypicalOverallCoefficientRange(DesignPracticesOverallCoefficientRange typicalU)
    {
        Variables.TypicalOverallCoefficientMinimum.SetValue(
            new HeatTransferCoefficient(typicalU.MinimumBtuPerHourSquareFootFahrenheit, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        Variables.TypicalOverallCoefficientMaximum.SetValue(
            new HeatTransferCoefficient(typicalU.MaximumBtuPerHourSquareFootFahrenheit, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }

    private void EnsureAssumedDirtyOverallCoefficient(double assumedU)
    {
        if (Variables.AssumedDirtyOverallCoefficient.IsDefined)
        {
            return;
        }

        Variables.AssumedDirtyOverallCoefficient.SetValue(
            new HeatTransferCoefficient(assumedU, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
    }

    private void SetPreliminaryArea(double assumedArea)
    {
        Variables.AssumedArea.SetValue(new Area(assumedArea, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
        Variables.RequiredArea.SetValue(new Area(assumedArea, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
    }

    private void CalculateDp09fCondensationZoneAreaFractions(double assumedOverallCoefficient)
    {
        if (processRegime is not (DesignPracticesProcessRegime.ShellSideCondensation or DesignPracticesProcessRegime.TubeSideCondensation) ||
            condensationZones.Count == 0)
        {
            return;
        }

        var zoneAreas = Dp09fCondensationZoneModel.CalculateZoneAreas(condensationZones, assumedOverallCoefficient);
        var desuperheatingFraction = zoneAreas
            .Where(zone => zone.Name.Contains("desuper", StringComparison.OrdinalIgnoreCase))
            .Sum(zone => zone.AreaFraction);
        var subcoolingFraction = zoneAreas
            .Where(zone => zone.Name.Contains("subcool", StringComparison.OrdinalIgnoreCase))
            .Sum(zone => zone.AreaFraction);
        var condensationFraction = zoneAreas
            .Where(zone => zone.Name.Contains("condensation", StringComparison.OrdinalIgnoreCase))
            .Sum(zone => zone.AreaFraction);
        var totalDuty = condensationZones.Sum(zone => zone.DutyBtuPerHour);
        var desuperheatingDutyFraction = condensationZones
            .Where(zone => zone.Name.Contains("desuper", StringComparison.OrdinalIgnoreCase))
            .Sum(zone => zone.DutyBtuPerHour) / Math.Max(totalDuty, 1e-12);
        var subcoolingDutyFraction = condensationZones
            .Where(zone => zone.Name.Contains("subcool", StringComparison.OrdinalIgnoreCase))
            .Sum(zone => zone.DutyBtuPerHour) / Math.Max(totalDuty, 1e-12);
        var condensationDutyFraction = condensationZones
            .Where(zone => zone.Name.Contains("condensation", StringComparison.OrdinalIgnoreCase))
            .Sum(zone => zone.DutyBtuPerHour) / Math.Max(totalDuty, 1e-12);

        Variables.CondensationZoneCount.SetValue(new UnitLess(zoneAreas.Count), VariableDefinedBy.Equipment);
        Variables.DesuperheatingDutyFraction.SetValue(new UnitLess(desuperheatingDutyFraction), VariableDefinedBy.Equipment);
        Variables.CondensationDutyFraction.SetValue(new UnitLess(condensationDutyFraction), VariableDefinedBy.Equipment);
        Variables.SubcoolingDutyFraction.SetValue(new UnitLess(subcoolingDutyFraction), VariableDefinedBy.Equipment);
        Variables.DesuperheatingAreaFraction.SetValue(new UnitLess(desuperheatingFraction), VariableDefinedBy.Equipment);
        Variables.CondensationAreaFraction.SetValue(new UnitLess(condensationFraction), VariableDefinedBy.Equipment);
        Variables.SubcoolingAreaFraction.SetValue(new UnitLess(subcoolingFraction), VariableDefinedBy.Equipment);

        recommendations.Add($"DP09F: preliminary condenser duty split is {desuperheatingDutyFraction:0%} desuperheating, {condensationDutyFraction:0%} condensation, and {subcoolingDutyFraction:0%} subcooling.");
        recommendations.Add($"DP09F: preliminary condenser area split is {desuperheatingFraction:0%} desuperheating, {condensationFraction:0%} condensation, and {subcoolingFraction:0%} subcooling.");
    }

    private void CalculateTubeCountAndShellSize()
    {
        var requiredArea = Variables.RequiredArea.Value.GetValue(SurfaceUnits.Foot2);
        var areaPerTube = Variables.TubeSurfaceArea.Value.GetValue(SurfaceUnits.Foot2);
        var rawRequiredTubeCount = Math.Ceiling(requiredArea / areaPerTube);
        var tubePasses = CalculateTubePasses(rawRequiredTubeCount);
        var requiredTubeCount = RoundUpToTubePassMultiple(rawRequiredTubeCount, tubePasses);
        Variables.RequiredTubeCount.SetValue(new UnitLess(requiredTubeCount), VariableDefinedBy.Equipment);

        if (!IsUserDefined(Variables.TubePasses))
        {
            Variables.TubePasses.SetValue(new UnitLess(tubePasses), VariableDefinedBy.Equipment);
        }

        var requestedShellId = Variables.ShellInsideDiameter.IsDefined
            ? Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch)
            : 0d;
        var shellId = SelectShellInsideDiameter(requiredTubeCount, requestedShellId, tubePasses);
        Variables.ShellInsideDiameter.SetValue(new Diameter(shellId, DiameterUnits.Inch), VariableDefinedBy.Equipment);
        AddDp09cTubePassConstructionReview(shellId, tubePasses);

        var maximumTubeCount = RoundDownToTubePassMultiple(EstimateDp09cTubeCount(shellId, tubePasses), tubePasses);
        Variables.MaximumTubeCount.SetValue(new UnitLess(maximumTubeCount), VariableDefinedBy.Equipment);

        var actualTubeCount = IsUserDefined(Variables.ActualTubeCount)
            ? RoundUpToTubePassMultiple(Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None), tubePasses)
            : Math.Min(maximumTubeCount, Math.Max(requiredTubeCount, 1d));
        Variables.ActualTubeCount.SetValue(new UnitLess(actualTubeCount), IsUserDefined(Variables.ActualTubeCount) ? VariableDefinedBy.UserInput : VariableDefinedBy.Equipment);

        Variables.ActualArea.SetValue(new Area(actualTubeCount * areaPerTube, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
    }

    private static double RoundUpToTubePassMultiple(double tubeCount, int tubePasses)
    {
        var passCount = Math.Max(tubePasses, 1);
        return Math.Ceiling(Math.Max(tubeCount, 1d) / passCount) * passCount;
    }

    private static double RoundDownToTubePassMultiple(double tubeCount, int tubePasses)
    {
        var passCount = Math.Max(tubePasses, 1);
        return Math.Max(Math.Floor(Math.Max(tubeCount, 1d) / passCount) * passCount, passCount);
    }

    private void AddDp09cTubePassConstructionReview(double shellInsideDiameterInches, int tubePasses)
    {
        var recommendedMaximumPasses = Dp09cShellAndTubeCatalog.GetRecommendedMaximumTubePasses(shellInsideDiameterInches);
        if (tubePasses > recommendedMaximumPasses)
        {
            recommendations.Add($"DP09C: {tubePasses} tube passes exceed the recommended maximum of {recommendedMaximumPasses} for a {shellInsideDiameterInches:0.#} in shell ID.");
        }

        if (Variables.TubeConstruction == ShellAndTubeTubeConstruction.UTube && tubePasses > 6)
        {
            recommendations.Add("DP09C: U-tube exchangers can use even tube-pass counts, but the normally recommended maximum is six because of construction considerations.");
        }

        if (Variables.RearHeadType is Dp09dRearHeadType.PullThroughFloatingHead or Dp09dRearHeadType.SplitRingFloatingHead &&
            tubePasses == 1)
        {
            recommendations.Add("DP09C: single-pass pull-through or split-ring floating-head designs require special expansion-joint review and are not generally used.");
        }
    }

    private int CalculateTubePasses(double requiredTubeCount)
    {
        var requestedPasses = Variables.TubePasses.IsDefined
            ? (int)Variables.TubePasses.Value.GetValue(UnitLessUnits.None)
            : -1;
        if (requestedPasses > 0)
        {
            return requestedPasses;
        }

        if (!TryReadAverageVolumetricFlow(request.TubeSideInlet.Stream, request.TubeSideOutlet.Stream, out var flowFt3s))
        {
            return 2;
        }

        var tubeFlowAreaFt2 = Variables.TubeFlowArea.Value.GetValue(SurfaceUnits.inch2) / 144d;
        var targetVelocity = 6d;

        foreach (var passes in new[] { 1, 2, 4, 6, 8, 10, 12 })
        {
            var velocity = flowFt3s / Math.Max(requiredTubeCount * tubeFlowAreaFt2 / passes, 1e-12);
            if (velocity >= targetVelocity)
            {
                return passes;
            }
        }

        recommendations.Add("DP09D/DP09C: tube-side pressure-drop utilization may require more passes than the preliminary selector can justify.");
        return 12;
    }

    private double SelectShellInsideDiameter(double requiredTubeCount, double requestedShellId, int tubePasses)
    {
        if (requestedShellId > 0d && EstimateDp09cTubeCount(requestedShellId, tubePasses) >= requiredTubeCount)
        {
            return requestedShellId;
        }

        for (var shellId = 6d; shellId <= 120d; shellId += shellId < 24d ? 2d : 1d)
        {
            if (EstimateDp09cTubeCount(shellId, tubePasses) >= requiredTubeCount)
            {
                return shellId;
            }
        }

        throw new InvalidOperationException("Cannot select a Design Practices shell diameter that fits the required tube count.");
    }

    private double EstimateDp09cTubeCount(double shellInsideDiameterInches, int tubePasses)
    {
        var tubeOuterDiameter = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubePitch = Variables.TubePitch.Value.GetValue(DiameterUnits.Inch);
        var outerTubeLimit = CalculateDp09cOuterTubeLimit(shellInsideDiameterInches);
        var safeCircleDiameter = Math.Max(outerTubeLimit - tubeOuterDiameter, 0d);
        var tubePassCorrection = GetDp09cTubePassCorrection(tubePasses);
        var passPartitionDistance = Dp09cShellAndTubeCatalog.GetPassPartitionDistanceInches(
            shellInsideDiameterInches,
            tubeOuterDiameter,
            tubePitch,
            Variables.TubeLayout);
        var passPartitionBase = Variables.TubeLayout == ShellAndTubeTubeLayout.Square
            ? tubePitch
            : 0.84d * tubePitch;
        var layoutFactor = Variables.TubeLayout == ShellAndTubeTubeLayout.Square ? 0.785d : 0.907d;
        var rawCount = layoutFactor * safeCircleDiameter * safeCircleDiameter / (tubePitch * tubePitch) -
                       tubePassCorrection * Math.Max(passPartitionDistance - passPartitionBase, 0d) * safeCircleDiameter / Math.Max(tubePitch * tubePitch, 1e-12);
        var correctedCount = rawCount * CalculateDp09cTubeCountCorrectionFactor(
            shellInsideDiameterInches,
            outerTubeLimit,
            tubeOuterDiameter);

        return Math.Max(Math.Floor(correctedCount), 1d);
    }

    private double CalculateDp09cTubeCountCorrectionFactor(
        double shellInsideDiameterInches,
        double outerTubeLimitInches,
        double tubeOuterDiameterInches)
    {
        var uTubeLossFraction = Variables.TubeConstruction == ShellAndTubeTubeConstruction.UTube ? 0.06d : 0d;
        var requiresShellInletProtection = processRegime is DesignPracticesProcessRegime.ShellSideCondensation or DesignPracticesProcessRegime.ShellSideVaporization;
        var impingementLossFraction = 0d;
        var shellNozzleLossFraction = requiresShellInletProtection
            ? CalculateDp09cFigure8ShellNozzleTubeLossFraction(
                shellInsideDiameterInches,
                outerTubeLimitInches,
                tubeOuterDiameterInches)
            : 0d;
        var correctionFactor = Math.Clamp(1d - uTubeLossFraction - impingementLossFraction - shellNozzleLossFraction, 0.75d, 1d);

        Variables.UTubeBendTubeLossFraction.SetValue(new UnitLess(uTubeLossFraction), VariableDefinedBy.Equipment);
        Variables.ImpingementTubeLossFraction.SetValue(new UnitLess(impingementLossFraction), VariableDefinedBy.Equipment);
        Variables.ShellNozzleTubeLossFraction.SetValue(new UnitLess(shellNozzleLossFraction), VariableDefinedBy.Equipment);
        Variables.TubeCountCorrectionFactor.SetValue(new UnitLess(correctionFactor), VariableDefinedBy.Equipment);

        return correctionFactor;
    }

    private double CalculateDp09cFigure8ShellNozzleTubeLossFraction(
        double shellInsideDiameterInches,
        double outerTubeLimitInches,
        double tubeOuterDiameterInches)
    {
        var shellNozzleDiameter = Variables.ShellSideNozzleDiameter.IsDefined &&
                                  Variables.ShellSideNozzleDiameter.Value.GetValue(DiameterUnits.Inch) > 0d
            ? Variables.ShellSideNozzleDiameter.Value.GetValue(DiameterUnits.Inch)
            : Math.Max(shellInsideDiameterInches / 4d, 1d);
        var figure8CorrectionFactor = Dp09cShellAndTubeCatalog.GetShellNozzleCorrectionFactor(
            shellInsideDiameterInches,
            outerTubeLimitInches,
            tubeOuterDiameterInches,
            shellNozzleDiameter);

        return Math.Clamp(1d - figure8CorrectionFactor, 0d, 0.25d);
    }

    private double CalculateDp09cOuterTubeLimit(double shellInsideDiameterInches)
    {
        var isTableBased = Dp09cShellAndTubeCatalog.TryGetTableOuterTubeLimitInches(
            shellInsideDiameterInches,
            Variables.TubeConstruction,
            Variables.RearHeadType,
            out var outerTubeLimit);

        if (!isTableBased)
        {
            var designPressurePsig = EstimateDp09cBundleDesignPressurePsig();
            var figure7OuterTubeLimit = Dp09cShellAndTubeCatalog.GetPullThroughFloatingHeadOuterTubeLimitInches(
                shellInsideDiameterInches,
                designPressurePsig);
            recommendations.Add($"DP09C: pull-through floating-head OTL uses digitized Figure 7 with {designPressurePsig:0.#} psig preliminary design-pressure basis.");

            if (shellInsideDiameterInches > 84d)
            {
                requiredMethodImplementations.Add("DP09C Figure 7 pull-through floating-head OTL review above chart range");
                recommendations.Add("DP09C: pull-through floating-head OTL is beyond the digitized Figure 7 range; consult mechanical design before final tube count.");
            }

            return Math.Max(figure7OuterTubeLimit, 0d);
        }

        return Math.Max(outerTubeLimit, 0d);
    }

    private double EstimateDp09cBundleDesignPressurePsig()
    {
        var maximumPressurePsia = new[]
            {
                request.ShellSideInlet.Stream.Pressure,
                request.ShellSideOutlet.Stream.Pressure,
                request.TubeSideInlet.Stream.Pressure,
                request.TubeSideOutlet.Stream.Pressure
            }
            .Where(pressure => pressure.IsDefined)
            .Select(pressure => pressure.Value.GetValue(PressureUnits.Psia))
            .DefaultIfEmpty(164.7d)
            .Max();

        return Math.Max(maximumPressurePsia - 14.7d, 150d);
    }

    private void CalculateHydraulicsAndCoefficients()
    {
        CalculateDp09bPressureDropAllowances();
        CalculateTubeSideHydraulicsAndCoefficient();
        CalculateShellSideHydraulicsAndCoefficient();
    }

    private void CalculateDp09bPressureDropAllowances()
    {
        var tubeSideAllowance = SelectDp09bPressureDropAllowancePsi(
            request.TubeSideInlet.Stream,
            request.TubeSideOutlet.Stream);
        var shellSideAllowance = SelectDp09bPressureDropAllowancePsi(
            request.ShellSideInlet.Stream,
            request.ShellSideOutlet.Stream);

        if (Variables.ShellType == ShellAndTubeShellType.TwoPass)
        {
            shellSideAllowance = Math.Min(shellSideAllowance, 7.5d);
            recommendations.Add("DP09B Table 11: TEMA F shell-side pressure drop is limited to the 5-10 psi maximum range.");
        }

        Variables.TubeSideAllowedPressureDrop.SetValue(
            new PressureDrop(tubeSideAllowance, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
        Variables.ShellSideAllowedPressureDrop.SetValue(
            new PressureDrop(shellSideAllowance, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
        recommendations.Add($"DP09B Table 11: preliminary pressure-drop allowances are {tubeSideAllowance:0.##} psi tube side and {shellSideAllowance:0.##} psi shell side.");
    }

    private void CalculateTubeSideHydraulicsAndCoefficient()
    {
        var properties = ReadAverageProperties(request.TubeSideInlet.Stream, request.TubeSideOutlet.Stream, "tube side");
        var tubePasses = Math.Max(Variables.TubePasses.Value.GetValue(UnitLessUnits.None), 1d);
        var tubeCount = Math.Max(Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None), 1d);
        var flowAreaFt2 = Variables.TubeFlowArea.Value.GetValue(SurfaceUnits.inch2) / 144d * tubeCount / tubePasses;

        if (!TryReadAverageVolumetricFlow(request.TubeSideInlet.Stream, request.TubeSideOutlet.Stream, out var flowFt3s))
        {
            flowFt3s = request.TubeSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr) / properties.DensityLbFt3 / 3600d;
        }

        var velocity = flowFt3s / Math.Max(flowAreaFt2, 1e-12);
        var diameterFeet = Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var massVelocity = properties.DensityLbFt3 * velocity * 3600d;
        var reynolds = diameterFeet * massVelocity / properties.ViscosityLbFtHr;
        var prandtl = properties.CpBtuLbF * properties.ViscosityLbFtHr / properties.ThermalConductivityBtuHrFtF;
        var lengthFeet = Variables.TubeLength.Value.GetValue(LengthUnits.Foot);
        var isTubeSideWaterLike = IsTubeSideWaterLikeService();
        var usesDp09fWaterTubeSide = IsShellSideCondensationWithTubeSideWaterLikeService();
        var sensibleTubeCoefficient = CalculateDp09dTubeSideCoefficient(
            reynolds,
            prandtl,
            properties,
            diameterFeet,
            lengthFeet,
            velocity,
            isTubeSideWaterLike);
        var coefficient = SelectTubeSideHeatTransferCoefficient(
            properties,
            flowAreaFt2,
            diameterFeet,
            velocity,
            sensibleTubeCoefficient,
            usesDp09fWaterTubeSide);
        var isothermalFriction = Dp09dShellAndTubeCorrelations.TubeSideIsothermalFrictionFactor(reynolds);
        var tubeWallTemperatureF = EstimateDp09dTubeWallTemperatureF(sensibleTubeCoefficient);
        var viscosityRatio = 1d;
        var viscosityGradientCorrection = 1d;
        var naturalConvectionPressureDropCorrection = 1d;
        if (!isTubeSideWaterLike)
        {
            var wallViscosity = EstimateTubeSideViscosityAtTemperature(tubeWallTemperatureF, properties.ViscosityLbFtHr);
            viscosityRatio = properties.ViscosityLbFtHr / Math.Max(wallViscosity, 1e-12);
            viscosityGradientCorrection = Dp09dShellAndTubeCorrelations.TubeSideViscosityGradientCorrectionFactor(reynolds, viscosityRatio);
            var grashofNumber = CalculateDp09dTubeSideGrashofNumber(properties, diameterFeet, tubeWallTemperatureF);
            naturalConvectionPressureDropCorrection =
                Dp09dShellAndTubeCorrelations.TubeSidePressureDropNaturalConvectionCorrectionFactor(
                    reynolds,
                    grashofNumber * prandtl * viscosityRatio);
        }

        var friction = isothermalFriction * viscosityGradientCorrection * naturalConvectionPressureDropCorrection;
        var pressureDrop = CalculateDp09dTubeSidePressureDrop(
            properties,
            velocity,
            massVelocity,
            diameterFeet,
            lengthFeet,
            tubePasses,
            friction,
            isTubeSideWaterLike);
        if (usesDp09fWaterTubeSide)
        {
            pressureDrop = CalculateDp09fTubeSideWaterPressureDrop(velocity, lengthFeet, tubePasses);
        }
        else if (processRegime == DesignPracticesProcessRegime.TubeSideCondensation)
        {
            pressureDrop = CalculateDp09fZoneWeightedPressureDrop(
                pressureDrop,
                request.TubeSideInlet.Stream,
                request.TubeSideOutlet.Stream,
                properties.DensityLbFt3);
            recommendations.Add("DP09F: tube-side condensing pressure drop is area-weighted across the current condensation zone model using preliminary zone pressure-drop and density factors.");
        }

        Variables.TubeVelocity.SetValue(new Velocity(velocity, VelocityUnits.FeetPerSecond), VariableDefinedBy.Equipment);
        Variables.AssumedTubeFlowArea.SetValue(new Area(flowAreaFt2, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
        Variables.ActualTubeFlowArea.SetValue(new Area(flowAreaFt2, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
        Variables.TubeSideReynoldsNumber.SetValue(new UnitLess(reynolds), VariableDefinedBy.Equipment);
        Variables.TubeSidePrandtlNumber.SetValue(new UnitLess(prandtl), VariableDefinedBy.Equipment);
        Variables.TubeSideFrictionFactor.SetValue(new UnitLess(friction), VariableDefinedBy.Equipment);
        Variables.TubeSideViscosityGradientCorrectionFactor.SetValue(new UnitLess(viscosityGradientCorrection), VariableDefinedBy.Equipment);
        Variables.TubeSidePressureDropNaturalConvectionCorrectionFactor.SetValue(new UnitLess(naturalConvectionPressureDropCorrection), VariableDefinedBy.Equipment);
        Variables.TubeSideHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(coefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        Variables.TubeSidePressureDrop.SetValue(new PressureDrop(pressureDrop, PressureDropUnits.psi), VariableDefinedBy.Equipment);

        var lowPrandtlCorrection = Dp09dShellAndTubeCorrelations.LowPrandtlNumberCorrection(prandtl);
        if (lowPrandtlCorrection < 0.99d)
        {
            recommendations.Add($"DP09D: tube-side coefficient uses Figure 1.7 low-Prandtl correction factor {lowPrandtlCorrection:0.###}.");
        }

        AddDp09dShortTubeLaminarReview(reynolds, lengthFeet / Math.Max(diameterFeet, 1e-12));
        if (isTubeSideWaterLike)
        {
            recommendations.Add("DP09D: tube-side water/aqueous pressure drop follows the water equation; Figure 1.9 and Figure 1.10 non-water friction corrections are not applied.");
        }
        else
        {
            AddDp09dTubePressureDropCorrectionReview(
                isothermalFriction,
                viscosityGradientCorrection,
                naturalConvectionPressureDropCorrection,
                tubeWallTemperatureF,
                viscosityRatio);
        }

        if (pressureDrop > GetTubeSideAllowedPressureDropPsi())
        {
            recommendations.Add("DP09D: tube-side pressure drop exceeds the preliminary allowance; increase tube count, diameter, or reduce passes.");
        }
    }

    private double SelectTubeSideHeatTransferCoefficient(
        DesignPracticesFluidProperties properties,
        double flowAreaFt2,
        double tubeInsideDiameterFeet,
        double velocityFeetPerSecond,
        double sensibleTubeCoefficient,
        bool usesDp09fWaterTubeSide)
    {
        if (processRegime == DesignPracticesProcessRegime.TubeSideCondensation)
        {
            return CalculateDp09fTubeSideCondensingCoefficient(properties, flowAreaFt2, tubeInsideDiameterFeet, sensibleTubeCoefficient);
        }

        if (usesDp09fWaterTubeSide)
        {
            var coefficient = CalculateDp09fTubeSideWaterHeatingCoefficient(velocityFeetPerSecond);
            recommendations.Add($"DP09F: tube-side water/aqueous coefficient uses {Dp09fTubeSideWaterCorrelation.WaterHeatingCoefficientSource}.");
            return coefficient;
        }

        return sensibleTubeCoefficient;
    }

    private bool IsShellSideCondensationWithTubeSideWaterLikeService()
    {
        return processRegime == DesignPracticesProcessRegime.ShellSideCondensation &&
               IsTubeSideWaterLikeService();
    }

    private bool IsTubeSideWaterLikeService()
    {
        var tubeService = DesignPracticesServiceClassifier.Classify(
            request.TubeSideInlet.Stream,
            request.TubeSideOutlet.Stream);

        return tubeService.Kind is DesignPracticesServiceKind.Water or DesignPracticesServiceKind.AqueousSolution;
    }

    private double CalculateDp09fTubeSideWaterHeatingCoefficient(double velocityFeetPerSecond)
    {
        var tubeInsideDiameterInches = Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeOutsideDiameterInches = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeBulkTemperatureF = (
            ReadTemperatureF(request.TubeSideInlet.Stream) +
            ReadTemperatureF(request.TubeSideOutlet.Stream)) / 2d;

        return Dp09fTubeSideWaterCorrelation.CalculateInsideCoefficientCorrectedToOutsideArea(
            velocityFeetPerSecond,
            tubeInsideDiameterInches,
            tubeOutsideDiameterInches,
            tubeBulkTemperatureF);
    }

    private double CalculateDp09dTubeSideWaterCoefficient(double velocityFeetPerSecond)
    {
        var tubeInsideDiameterInches = Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeOutsideDiameterInches = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeBulkTemperatureF = (
            ReadTemperatureF(request.TubeSideInlet.Stream) +
            ReadTemperatureF(request.TubeSideOutlet.Stream)) / 2d;

        recommendations.Add($"DP09D: tube-side water/aqueous coefficient uses {Dp09dShellAndTubeCorrelations.TubeSideWaterCoefficientSource}.");
        return Dp09dShellAndTubeCorrelations.TubeSideWaterCoefficientCorrectedToOutsideArea(
            velocityFeetPerSecond,
            tubeInsideDiameterInches,
            tubeOutsideDiameterInches,
            tubeBulkTemperatureF);
    }

    private double CalculateDp09fTubeSideWaterPressureDrop(double velocityFeetPerSecond, double lengthFeet, double tubePasses)
    {
        var tubeInsideDiameterInches = Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeOutsideDiameterInches = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var shellPasses = Math.Max(Variables.ShellPasses.Value.GetValue(UnitLessUnits.None), 1d);
        var pressureDropFoulingFactor = Dp09fTubeSideWaterCorrelation.EstimatePressureDropFoulingFactor(
            Variables.TubeMaterial,
            tubeOutsideDiameterInches,
            tubeInsideDiameterInches);

        recommendations.Add($"DP09F: tube-side water/aqueous pressure drop uses {Dp09fTubeSideWaterCorrelation.WaterPressureDropSource}; Ft={pressureDropFoulingFactor.Value:0.###} from {pressureDropFoulingFactor.Source}.");
        if (pressureDropFoulingFactor.RequiresMaterialReview)
        {
            recommendations.Add($"DP09D: tube material {Variables.TubeMaterial} needs a verified alloy Ft lookup before final DP09F tube-side water pressure-drop rating.");
        }

        return Dp09fTubeSideWaterCorrelation.CalculateTubeSidePressureDropPsi(
            velocityFeetPerSecond,
            tubeInsideDiameterInches,
            lengthFeet,
            shellPasses,
            tubePasses,
            pressureDropFoulingFactor.Value);
    }

    private double CalculateDp09fTubeSideCondensingCoefficient(
        DesignPracticesFluidProperties properties,
        double flowAreaFt2,
        double tubeInsideDiameterFeet,
        double sensibleCoefficient)
    {
        var inlet = request.TubeSideInlet.Stream;
        var outlet = request.TubeSideOutlet.Stream;
        var inletMassFlow = inlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var outletMassFlow = outlet.MassFlow.IsDefined
            ? outlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr)
            : inletMassFlow;
        var inletVaporFraction = inlet.VaporFraction.IsDefined
            ? inlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : 1d;
        var outletVaporFraction = outlet.VaporFraction.IsDefined
            ? outlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : inletVaporFraction;
        var averageVaporMassFlow = ((inletMassFlow * inletVaporFraction) + (outletMassFlow * outletVaporFraction)) / 2d;
        var averageLiquidMassFlow = ((inletMassFlow * (1d - inletVaporFraction)) + (outletMassFlow * (1d - outletVaporFraction))) / 2d;
        var vaporDensity = inlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);
        var liquidDensity = outlet.MassDensity.IsDefined
            ? outlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3)
            : properties.DensityLbFt3;
        var vaporMassVelocity = averageVaporMassFlow / Math.Max(flowAreaFt2, 1e-12);
        var liquidMassVelocity = averageLiquidMassFlow / Math.Max(flowAreaFt2, 1e-12);
        var equivalentMassVelocity = Dp09fCondensationZoneModel.CalculateEquivalentLiquidMassVelocity(
            liquidMassVelocity,
            vaporMassVelocity,
            liquidDensity,
            vaporDensity);
        var coefficient = Dp09fCondensationZoneModel.CalculateInsideTubeCondensingCoefficient(
            equivalentMassVelocity,
            tubeInsideDiameterFeet,
            properties);
        var dutyWeightedCoefficient = Dp09fCondensationZoneModel.CalculateDutyWeightedZoneCoefficient(
            condensationZones,
            sensibleCoefficient,
            coefficient,
            coefficient);

        Variables.CondensingZoneHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(coefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        Variables.VaporCoolingZoneHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(sensibleCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        Variables.LiquidCoolingZoneHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(coefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        Variables.DutyWeightedCondensingSideHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(dutyWeightedCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);

        recommendations.Add($"DP09F: tube-side condensation uses Akers-Deans-Crosser equivalent liquid mass velocity {equivalentMassVelocity:0.###} lb/hr-ft2 and inside-tube condensing coefficient {coefficient:0.##} Btu/hr-ft2-F.");
        recommendations.Add("DP09F: tube-side wide-cut condensation still needs independent vapor-cooling zone properties; liquid cooling is preliminarily assigned the condensing coefficient.");

        return dutyWeightedCoefficient;
    }

    private double CalculateDp09dTubeSideCoefficient(
        double reynolds,
        double prandtl,
        DesignPracticesFluidProperties properties,
        double tubeInsideDiameterFeet,
        double tubeLengthFeet,
        double velocityFeetPerSecond,
        bool isTubeSideWaterLike)
    {
        if (isTubeSideWaterLike)
        {
            return CalculateDp09dTubeSideWaterCoefficient(velocityFeetPerSecond);
        }

        if (reynolds >= 10_000d)
        {
            return CalculateDp09dTurbulentTubeSideCoefficient(reynolds, prandtl, properties.ThermalConductivityBtuHrFtF, tubeInsideDiameterFeet);
        }

        if (reynolds <= 2_000d)
        {
            return CalculateDp09dLaminarTubeSideCoefficient(reynolds, prandtl, properties, tubeInsideDiameterFeet, tubeLengthFeet);
        }

        var laminarAtBoundary = CalculateDp09dLaminarTubeSideCoefficient(2_000d, prandtl, properties, tubeInsideDiameterFeet, tubeLengthFeet);
        var turbulentAtBoundary = CalculateDp09dTurbulentTubeSideCoefficient(10_000d, prandtl, properties.ThermalConductivityBtuHrFtF, tubeInsideDiameterFeet);
        var laminarWeight = 1.25d - reynolds / 8_000d;
        var coefficient = laminarWeight * laminarAtBoundary + (1d - laminarWeight) * turbulentAtBoundary;

        recommendations.Add($"DP09D: tube-side coefficient is interpolated through the transition range with laminar weight {laminarWeight:0.###}.");
        return coefficient;
    }

    private double CalculateDp09dTubeSidePressureDrop(
        DesignPracticesFluidProperties properties,
        double velocityFeetPerSecond,
        double massVelocityLbFt2Hr,
        double tubeInsideDiameterFeet,
        double tubeLengthFeet,
        double tubePasses,
        double friction,
        bool isTubeSideWaterLike)
    {
        if (isTubeSideWaterLike)
        {
            return CalculateDp09dTubeSideWaterPressureDrop(velocityFeetPerSecond, tubeLengthFeet, tubePasses);
        }

        var specificGravity = properties.DensityLbFt3 / 62.4d;
        var pressureDrop = friction * tubeLengthFeet * tubePasses * Math.Pow(massVelocityLbFt2Hr, 2d) /
                           (5.22e10d * Math.Max(tubeInsideDiameterFeet, 1e-12) * Math.Max(specificGravity, 1e-12));

        return pressureDrop + 4d * tubePasses * velocityFeetPerSecond * velocityFeetPerSecond /
            (2d * 32.174d) * properties.DensityLbFt3 / 144d;
    }

    private double CalculateDp09dTubeSideWaterPressureDrop(
        double velocityFeetPerSecond,
        double lengthFeet,
        double tubePasses)
    {
        var tubeInsideDiameterInches = Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeOutsideDiameterInches = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var shellPasses = Math.Max(Variables.ShellPasses.Value.GetValue(UnitLessUnits.None), 1d);
        var pressureDropFoulingFactor = Dp09fTubeSideWaterCorrelation.EstimatePressureDropFoulingFactor(
            Variables.TubeMaterial,
            tubeOutsideDiameterInches,
            tubeInsideDiameterInches);

        recommendations.Add($"DP09D: tube-side water/aqueous pressure drop uses {Dp09dShellAndTubeCorrelations.TubeSideWaterPressureDropSource}; Ft={pressureDropFoulingFactor.Value:0.###} from {pressureDropFoulingFactor.Source}.");
        if (pressureDropFoulingFactor.RequiresMaterialReview)
        {
            recommendations.Add($"DP09D: tube material {Variables.TubeMaterial} needs a verified alloy Ft lookup before final DP09D tube-side water pressure-drop rating.");
        }

        return Dp09fTubeSideWaterCorrelation.CalculateTubeSidePressureDropPsi(
            velocityFeetPerSecond,
            tubeInsideDiameterInches,
            lengthFeet,
            shellPasses,
            tubePasses,
            pressureDropFoulingFactor.Value);
    }

    private double CalculateDp09dTurbulentTubeSideCoefficient(
        double reynolds,
        double prandtl,
        double thermalConductivityBtuHrFtF,
        double tubeInsideDiameterFeet)
    {
        return 0.023d * Math.Pow(Math.Max(reynolds, 1d), 0.8d) *
               Math.Pow(Math.Max(prandtl, 1e-6), 1d / 3d) *
               thermalConductivityBtuHrFtF / Math.Max(tubeInsideDiameterFeet, 1e-12) *
               Dp09dShellAndTubeCorrelations.LowPrandtlNumberCorrection(prandtl);
    }

    private double CalculateDp09dLaminarTubeSideCoefficient(
        double reynolds,
        double prandtl,
        DesignPracticesFluidProperties properties,
        double tubeInsideDiameterFeet,
        double tubeLengthFeet)
    {
        var lengthToInsideDiameter = tubeLengthFeet / Math.Max(tubeInsideDiameterFeet, 1e-12);
        var grashofNumber = CalculateDp09dTubeSideGrashofNumber(
            properties,
            tubeInsideDiameterFeet,
            EstimateDp09dTubeWallTemperatureF(0d));
        var naturalConvectionFactor = Dp09dShellAndTubeCorrelations.NaturalConvectionFactor(
            grashofNumber,
            Dp09dTubeOrientation.Horizontal,
            lengthToInsideDiameter);
        var shortTubeCorrection = Dp09dShellAndTubeCorrelations.ShortTubeCorrectionFactor(reynolds, lengthToInsideDiameter);
        var lambda = tubeInsideDiameterFeet / Math.Max(tubeLengthFeet, 1e-12) + shortTubeCorrection;
        var lowPrandtlCorrection = Dp09dShellAndTubeCorrelations.LowPrandtlNumberCorrection(prandtl);
        var tubeInsideDiameterInches = tubeInsideDiameterFeet * 12d;
        var coefficient = 12d * properties.ThermalConductivityBtuHrFtF / Math.Max(tubeInsideDiameterInches, 1e-12) *
                          (2.5d + 4.5d * Math.Pow(Math.Max((reynolds + naturalConvectionFactor) * lambda, 1e-12), 0.37d) *
                           Math.Pow(Math.Max(prandtl, 1e-6), 0.17d)) *
                          lowPrandtlCorrection;

        Variables.TubeSideNaturalConvectionFactor.SetValue(new UnitLess(naturalConvectionFactor), VariableDefinedBy.Equipment);
        Variables.TubeSideShortTubeCorrectionFactor.SetValue(new UnitLess(shortTubeCorrection), VariableDefinedBy.Equipment);
        Variables.TubeSideLaminarLengthFactor.SetValue(new UnitLess(lambda), VariableDefinedBy.Equipment);
        recommendations.Add($"DP09D: tube-side laminar coefficient uses Figure 1.5 natural-convection factor {naturalConvectionFactor:0.###}, Figure 1.6 short-tube correction {shortTubeCorrection:0.###}, and lambda {lambda:0.###}.");
        recommendations.Add("DP09D: laminar natural-convection Grashof number uses the current estimated tube-wall temperature; final rating should use film-property values.");

        return coefficient;
    }

    private double CalculateDp09dTubeSideGrashofNumber(
        DesignPracticesFluidProperties properties,
        double tubeInsideDiameterFeet,
        double tubeWallTemperatureF)
    {
        var tubeBulkTemperatureF = (
            ReadTemperatureF(request.TubeSideInlet.Stream) +
            ReadTemperatureF(request.TubeSideOutlet.Stream)) / 2d;
        var filmTemperatureF = (tubeBulkTemperatureF + tubeWallTemperatureF) / 2d;
        var temperatureDifferenceF = Math.Max(Math.Abs(tubeWallTemperatureF - tubeBulkTemperatureF), 1e-6d);
        var thermalExpansionPerF = 1d / Math.Max(filmTemperatureF + 460d, 1e-6d);
        var tubeInsideDiameterInches = tubeInsideDiameterFeet * 12d;

        return 413d * 10_000d *
               Math.Pow(Math.Max(tubeInsideDiameterInches, 1e-12), 3d) *
               Math.Pow(Math.Max(properties.DensityLbFt3, 1e-12), 2d) *
               thermalExpansionPerF *
               temperatureDifferenceF /
               Math.Pow(Math.Max(properties.ViscosityLbFtHr, 1e-12), 2d);
    }

    private double EstimateDp09dTubeWallTemperatureF(double tubeSideCoefficientBtuHrFt2F)
    {
        var tubeBulkTemperatureF = (
            ReadTemperatureF(request.TubeSideInlet.Stream) +
            ReadTemperatureF(request.TubeSideOutlet.Stream)) / 2d;
        var shellBulkTemperatureF = (
            ReadTemperatureF(request.ShellSideInlet.Stream) +
            ReadTemperatureF(request.ShellSideOutlet.Stream)) / 2d;
        var heatFlux = Variables.HeatFlux.IsDefined
            ? Variables.HeatFlux.Value.GetValue(HeatSurfaceFlowUnits.BTU_hr_ft2)
            : Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr) /
              Math.Max(Variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2), 1e-12);
        var wallApproach = tubeSideCoefficientBtuHrFt2F > 0d
            ? Math.Abs(heatFlux) / Math.Max(tubeSideCoefficientBtuHrFt2F, 1e-12)
            : Math.Abs(shellBulkTemperatureF - tubeBulkTemperatureF) * 0.5d;
        var direction = Math.Sign(shellBulkTemperatureF - tubeBulkTemperatureF);
        var estimatedWallTemperature = tubeBulkTemperatureF + direction * wallApproach;

        return ClampBetween(estimatedWallTemperature, tubeBulkTemperatureF, shellBulkTemperatureF);
    }

    private double EstimateTubeSideViscosityAtTemperature(double temperatureF, double fallbackViscosityLbFtHr)
    {
        var inletTemperatureF = ReadTemperatureF(request.TubeSideInlet.Stream);
        var outletTemperatureF = ReadTemperatureF(request.TubeSideOutlet.Stream);
        var inletViscosity = request.TubeSideInlet.Stream.Viscosity.Value.GetValue(ViscosityUnits.lb_ft_hr);
        var outletViscosity = request.TubeSideOutlet.Stream.Viscosity.Value.GetValue(ViscosityUnits.lb_ft_hr);

        if (inletViscosity <= 0d || outletViscosity <= 0d || Math.Abs(outletTemperatureF - inletTemperatureF) < 1e-9d)
        {
            return fallbackViscosityLbFtHr;
        }

        var fraction = (temperatureF - inletTemperatureF) / (outletTemperatureF - inletTemperatureF);
        var logViscosity = Math.Log(inletViscosity) + fraction * (Math.Log(outletViscosity) - Math.Log(inletViscosity));

        return Math.Exp(logViscosity);
    }

    private static double ClampBetween(double value, double boundaryA, double boundaryB)
    {
        return Math.Clamp(value, Math.Min(boundaryA, boundaryB), Math.Max(boundaryA, boundaryB));
    }

    private void CalculateShellSideHydraulicsAndCoefficient()
    {
        var properties = ReadAverageProperties(request.ShellSideInlet.Stream, request.ShellSideOutlet.Stream, "shell side");
        var shellIdFeet = Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var baffleSpacingFeet = Variables.BaffleSpacing.Value.GetValue(LengthUnits.Foot);
        var pitchInches = Variables.TubePitch.Value.GetValue(DiameterUnits.Inch);
        var tubeOdInches = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var freeArea = shellIdFeet * baffleSpacingFeet * Math.Max(pitchInches - tubeOdInches, 1e-6d) / pitchInches;
        var massFlow = request.ShellSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var massVelocity = massFlow / Math.Max(freeArea, 1e-12);
        var velocity = massVelocity / properties.DensityLbFt3 / 3600d;
        var equivalentDiameterFeet = CalculateDp09EquivalentDiameterFeet();
        var reynolds = equivalentDiameterFeet * massVelocity / properties.ViscosityLbFtHr;
        var prandtl = properties.CpBtuLbF * properties.ViscosityLbFtHr / properties.ThermalConductivityBtuHrFtF;
        var crossflowFractions = CalculateDp09dShellSideCrossflowFractions(reynolds);
        var heatTransferReynolds = reynolds * crossflowFractions.HeatTransferFraction;
        var pressureDropReynolds = reynolds * crossflowFractions.PressureDropFraction;
        var pressureDropMassVelocity = massVelocity * crossflowFractions.PressureDropFraction;
        var coefficient = CalculateDp09ShellSideCoefficient(heatTransferReynolds, prandtl, properties.ThermalConductivityBtuHrFtF, equivalentDiameterFeet);
        var pitchRatio = pitchInches / Math.Max(tubeOdInches, 1e-12);
        var friction = Dp09dShellAndTubeCorrelations.DP_f_ShellSideFrictionFactor(
            pressureDropReynolds,
            Variables.TubeLayout,
            pitchRatio);
        var shellPressureDrop = friction * Math.Pow(pressureDropMassVelocity, 2d) * shellIdFeet *
                                Math.Max(Variables.TubeLength.Value.GetValue(LengthUnits.Foot) / Math.Max(baffleSpacingFeet, 1e-12), 1d) /
                                (5.22e10d * Math.Max(equivalentDiameterFeet, 1e-12) * Math.Max(properties.DensityLbFt3 / 62.4d, 1e-12));

        if (processRegime == DesignPracticesProcessRegime.ShellSideCondensation)
        {
            coefficient = CalculateDp09fCondensingCoefficient(coefficient, freeArea);
            shellPressureDrop = CalculateDp09fZoneWeightedPressureDrop(
                shellPressureDrop,
                request.ShellSideInlet.Stream,
                request.ShellSideOutlet.Stream,
                properties.DensityLbFt3);
            recommendations.Add("DP09F: shell-side condensing pressure drop is area-weighted across the current condensation zone model using preliminary zone pressure-drop and density factors.");
        }

        Variables.ShellFlowArea.SetValue(new Area(freeArea, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
        Variables.ShellEquivalentDiameter.SetValue(new Diameter(equivalentDiameterFeet * 12d, DiameterUnits.Inch), VariableDefinedBy.Equipment);
        Variables.ShellSideVelocity.SetValue(new Velocity(velocity, VelocityUnits.FeetPerSecond), VariableDefinedBy.Equipment);
        Variables.ShellSideReynoldsNumber.SetValue(new UnitLess(reynolds), VariableDefinedBy.Equipment);
        Variables.ShellSidePrandtlNumber.SetValue(new UnitLess(prandtl), VariableDefinedBy.Equipment);
        Variables.ShellSideFrictionFactor.SetValue(new UnitLess(friction), VariableDefinedBy.Equipment);
        Variables.ShellSideNominalCrossflowFraction.SetValue(new UnitLess(crossflowFractions.NominalFraction), VariableDefinedBy.Equipment);
        Variables.ShellSidePressureDropCrossflowFraction.SetValue(new UnitLess(crossflowFractions.PressureDropFraction), VariableDefinedBy.Equipment);
        Variables.ShellSideHeatTransferCrossflowFraction.SetValue(new UnitLess(crossflowFractions.HeatTransferFraction), VariableDefinedBy.Equipment);
        Variables.ShellSideCrossflowSections.SetValue(
            new UnitLess(Variables.TubeLength.Value.GetValue(LengthUnits.Foot) / Math.Max(baffleSpacingFeet, 1e-12)),
            VariableDefinedBy.Equipment);
        Variables.ShellSideHeatTransferCoefficient.SetValue(
            new HeatTransferCoefficient(coefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        Variables.ShellSidePressureDrop.SetValue(new PressureDrop(shellPressureDrop, PressureDropUnits.psi), VariableDefinedBy.Equipment);

        if (shellPressureDrop > GetShellSideAllowedPressureDropPsi())
        {
            recommendations.Add("DP09D/DP09C: shell-side pressure drop exceeds the preliminary allowance; increase shell diameter, baffle spacing, or use a lower-pressure-drop shell arrangement.");
        }
    }

    private void CalculateOverallCoefficients()
    {
        var shellH = Variables.ShellSideHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);
        var tubeH = Variables.TubeSideHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);
        var tubeOd = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeId = Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch);
        var wallResistance = CalculateDp09cTubeWallResistance();
        var tubeResistanceOutsideArea = tubeOd / Math.Max(tubeId * tubeH, 1e-12);
        var cleanResistance = 1d / Math.Max(shellH, 1e-12) + tubeResistanceOutsideArea + wallResistance;
        var allowedFouling = Variables.AllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);
        var cleanU = 1d / cleanResistance;
        var totalServiceResistance = cleanResistance + allowedFouling;
        var dirtyU = 1d / totalServiceResistance;
        var foulingResistanceFraction = allowedFouling / Math.Max(totalServiceResistance, 1e-12);
        var actualArea = Variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2);
        var duty = Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr);
        var lmtd = Variables.LogMeanTemperatureDifference.Value.GetValue(TemperatureUnits.DegreeFahrenheit);
        var actualU = duty / Math.Max(actualArea * lmtd, 1e-12);

        Variables.CleanOverallCoefficient.SetValue(new HeatTransferCoefficient(cleanU, HeatTransferCoefficientUnits.BTU_hr_ft2_F), VariableDefinedBy.Equipment);
        Variables.CalculatedDirtyOverallCoefficient.SetValue(new HeatTransferCoefficient(dirtyU, HeatTransferCoefficientUnits.BTU_hr_ft2_F), VariableDefinedBy.Equipment);
        Variables.LastCalculatedDirtyOverallCoefficient.SetValue(new HeatTransferCoefficient(dirtyU, HeatTransferCoefficientUnits.BTU_hr_ft2_F), VariableDefinedBy.Equipment);
        Variables.ActualOverallCoefficient.SetValue(new HeatTransferCoefficient(actualU, HeatTransferCoefficientUnits.BTU_hr_ft2_F), VariableDefinedBy.Equipment);
        Variables.CalculatedFoulingResistance.SetValue(new UnitLess((1d / Math.Max(actualU, 1e-12)) - cleanResistance), VariableDefinedBy.Equipment);
        Variables.FoulingResistanceFraction.SetValue(new UnitLess(foulingResistanceFraction), VariableDefinedBy.Equipment);
        AddDp09bFoulingDominanceReview(foulingResistanceFraction);
    }

    private void AddDp09dOptimizationReview()
    {
        var requiredArea = Variables.RequiredArea.Value.GetValue(SurfaceUnits.Foot2);
        var actualArea = Variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2);
        var overdesignPercent = (actualArea / Math.Max(requiredArea, 1e-12) - 1d) * 100d;
        var tubePressureDrop = Variables.TubeSidePressureDrop.Value.GetValue(PressureDropUnits.psi);
        var shellPressureDrop = Variables.ShellSidePressureDrop.Value.GetValue(PressureDropUnits.psi);
        var tubeUtilization = tubePressureDrop / GetTubeSideAllowedPressureDropPsi();
        var shellUtilization = shellPressureDrop / GetShellSideAllowedPressureDropPsi();

        Variables.AreaOverdesignPercent.SetValue(new UnitLess(overdesignPercent), VariableDefinedBy.Equipment);
        Variables.TubeSidePressureDropUtilization.SetValue(new UnitLess(tubeUtilization), VariableDefinedBy.Equipment);
        Variables.ShellSidePressureDropUtilization.SetValue(new UnitLess(shellUtilization), VariableDefinedBy.Equipment);

        if (overdesignPercent < 0d)
        {
            recommendations.Add($"DP09D: installed area is {Math.Abs(overdesignPercent):0.#}% below required area; increase area or revise U/MTD assumptions.");
        }
        else if (overdesignPercent > 25d)
        {
            recommendations.Add($"DP09D: area overdesign is {overdesignPercent:0.#}%; review tube count, shell size, length, or U assumptions during optimization.");
        }

        AddDp09dPressureDropUtilizationReview("tube-side", tubeUtilization);
        AddDp09dPressureDropUtilizationReview("shell-side", shellUtilization);
    }

    private void CalculateDp09dNozzleReview()
    {
        var tubeProperties = ReadAverageProperties(request.TubeSideInlet.Stream, request.TubeSideOutlet.Stream, "tube-side nozzle");
        var shellProperties = ReadAverageProperties(request.ShellSideInlet.Stream, request.ShellSideOutlet.Stream, "shell-side nozzle");
        var tubeFlow = ReadAverageVolumetricFlowOrMassDensityFallback(request.TubeSideInlet.Stream, request.TubeSideOutlet.Stream, tubeProperties);
        var shellFlow = ReadAverageVolumetricFlowOrMassDensityFallback(request.ShellSideInlet.Stream, request.ShellSideOutlet.Stream, shellProperties);

        CalculateDp09dSideNozzle(
            sideLabel: "tube-side",
            inlet: request.TubeSideInlet.Stream,
            outlet: request.TubeSideOutlet.Stream,
            nozzleDiameter: Variables.TubeSideNozzleDiameter,
            nozzleVelocity: Variables.TubeSideNozzleVelocity,
            nozzlePressureDrop: Variables.TubeSideNozzlePressureDrop,
            sidePressureDropPsi: Variables.TubeSidePressureDrop.Value.GetValue(PressureDropUnits.psi),
            flowFt3PerSecond: tubeFlow,
            densityLbFt3: tubeProperties.DensityLbFt3,
            allowableFraction: Variables.TubePasses.Value.GetValue(UnitLessUnits.None) <= 1d ? 0.40d : 0.35d);

        var shellNozzleAllowance = IsShellSideGasOrCondensingVapor() ? 0.35d : 0.15d;
        CalculateDp09dSideNozzle(
            sideLabel: "shell-side",
            inlet: request.ShellSideInlet.Stream,
            outlet: request.ShellSideOutlet.Stream,
            nozzleDiameter: Variables.ShellSideNozzleDiameter,
            nozzleVelocity: Variables.ShellSideNozzleVelocity,
            nozzlePressureDrop: Variables.ShellSideNozzlePressureDrop,
            sidePressureDropPsi: Variables.ShellSidePressureDrop.Value.GetValue(PressureDropUnits.psi),
            flowFt3PerSecond: shellFlow,
            densityLbFt3: shellProperties.DensityLbFt3,
            allowableFraction: shellNozzleAllowance);
    }

    private void CalculateDp09dSideNozzle(
        string sideLabel,
        IFacadeStream inlet,
        IFacadeStream outlet,
        Variable<Diameter> nozzleDiameter,
        Variable<Velocity> nozzleVelocity,
        Variable<PressureDrop> nozzlePressureDrop,
        double sidePressureDropPsi,
        double flowFt3PerSecond,
        double densityLbFt3,
        double allowableFraction)
    {
        if (!IsUserDefined(nozzleDiameter) || nozzleDiameter.Value.GetValue(DiameterUnits.Inch) <= 0d)
        {
            var targetVelocity = IsGasOrVaporService(inlet, outlet) ? 100d : 8d;
            var selectedDiameter = SelectPreliminaryNozzleDiameterInches(flowFt3PerSecond, targetVelocity);
            if (sideLabel.Equals("shell-side", StringComparison.OrdinalIgnoreCase))
            {
                selectedDiameter = Math.Min(
                    selectedDiameter,
                    Math.Min(Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch) / 2d, 20d));
            }

            nozzleDiameter.SetValue(new Diameter(Math.Max(selectedDiameter, 0.5d), DiameterUnits.Inch), VariableDefinedBy.Equipment);
        }

        var diameterFeet = nozzleDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;
        var flowArea = Math.PI * diameterFeet * diameterFeet / 4d;
        var velocity = flowFt3PerSecond / Math.Max(flowArea, 1e-12);
        var pressureDropPsi = PreliminaryTotalNozzleLossCoefficient * densityLbFt3 * velocity * velocity /
                              (2d * 32.174d * 144d);
        var allowableNozzlePressureDrop = allowableFraction * sidePressureDropPsi;

        nozzleVelocity.SetValue(new Velocity(velocity, VelocityUnits.FeetPerSecond), VariableDefinedBy.Equipment);
        nozzlePressureDrop.SetValue(new PressureDrop(pressureDropPsi, PressureDropUnits.psi), VariableDefinedBy.Equipment);

        if (pressureDropPsi > allowableNozzlePressureDrop)
        {
            recommendations.Add($"DP09D: {sideLabel} nozzle pressure drop is {pressureDropPsi:0.###} psi, above the {allowableFraction:0%} preliminary allowance; increase nozzle size or revisit line sizing.");
            return;
        }

        recommendations.Add($"DP09D: {sideLabel} nozzle pressure drop is within the {allowableFraction:0%} preliminary allowance using velocity-head estimate.");
    }

    private void AddDp09dPressureDropUtilizationReview(string sideLabel, double utilization)
    {
        if (utilization > 1d)
        {
            recommendations.Add($"DP09D: {sideLabel} pressure-drop utilization is {utilization:0%}; reduce pressure drop before finalizing the geometry.");
            return;
        }

        if (utilization < 0.35d)
        {
            recommendations.Add($"DP09D: {sideLabel} pressure-drop utilization is only {utilization:0%}; consider whether allowable pressure drop can be used to improve heat transfer or reduce area.");
        }
    }

    private void AddDp09ConstructionAndMaintenanceReview()
    {
        var pitchRatio = Variables.TubePitch.Value.GetValue(DiameterUnits.Inch) /
                         Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        if (pitchRatio < 1.25d)
        {
            recommendations.Add("DP09C/TEMA: tube pitch should be at least 1.25 times tube OD for shell-and-tube construction.");
        }

        if (Variables.BaffleSpacing.Value.GetValue(LengthUnits.Inch) > Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch))
        {
            recommendations.Add("DP09D: for no-change-of-phase shell-side flow, baffle spacing should not exceed shell diameter.");
        }

        var bundleDiameter = CalculateDp09cOuterTubeLimit(Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch));
        var bafflePitchToBundleDiameter = Variables.BaffleSpacing.Value.GetValue(LengthUnits.Inch) /
                                          Math.Max(bundleDiameter, 1e-12);
        if (bafflePitchToBundleDiameter is < 0.25d or > 0.80d)
        {
            recommendations.Add($"DP09D: baffle pitch/bundle diameter ratio is {bafflePitchToBundleDiameter:0.###}; keep the preliminary design between 0.25 and 0.80.");
        }

        AddDp09cBaffleReview();

        var tubeSideAllowedFouling = Variables.TubeSideAllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);
        if (Variables.TubeConstruction == ShellAndTubeTubeConstruction.UTube && tubeSideAllowedFouling > 0.002d)
        {
            recommendations.Add("DP09C: U-tube construction is not recommended when tube-side fouling exceeds 0.002 hr-ft2-F/Btu.");
            if (!IsMechanicalCleaning(Variables.TubeSideCleaningMethod))
            {
                recommendations.Add("DP09C Table 3: high tube-side fouling requires mechanical tube-side cleaning or a removable-bundle review.");
            }

            if (!IsPureWater(request.TubeSideInlet.Stream))
            {
                recommendations.Add("DP09C Table 3: U-tube construction with high tube-side fouling should be limited to cooling-water services cleaned by high-pressure jetting; otherwise use removable-bundle construction.");
            }
        }

        var shellSideAllowedFouling = Variables.ShellSideAllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);
        if (shellSideAllowedFouling > 0.002d &&
            Variables.RearHeadType == Dp09dRearHeadType.FixedTubesheet)
        {
            recommendations.Add("DP09C Table 3: high shell-side fouling should use removable-bundle rear-head construction for shell-side mechanical cleaning; fixed tubesheet rear head needs review.");
        }
        else if (Variables.TubeConstruction == ShellAndTubeTubeConstruction.Straight && shellSideAllowedFouling > 0.002d)
        {
            recommendations.Add("DP09C Table 3: high shell-side fouling favors removable-bundle construction for shell-side mechanical cleaning.");
        }

        if (shellSideAllowedFouling > 0.002d && !IsMechanicalCleaning(Variables.ShellSideCleaningMethod))
        {
            recommendations.Add("DP09C Table 3: high shell-side fouling requires shell-side mechanical cleaning access; chemical-only cleaning needs maintenance review.");
        }

        AddDp09cTemaHeadSelectionBoundaryReview();

        AddDp09cThermalExpansionReview();

        if (processRegime is DesignPracticesProcessRegime.ShellSideCondensation or DesignPracticesProcessRegime.ShellSideVaporization)
        {
            requiredMethodImplementations.Add("DP09C flow-induced vibration analysis");
            recommendations.Add("DP09C: specify shell inlet impingement protection for condensing or vaporizing services.");
            recommendations.Add("DP09C: perform flow-induced vibration review after mechanical geometry is finalized.");
        }

        AddDp09cTubeCountCorrectionReview();

        if (processRegime is DesignPracticesProcessRegime.ShellSideVaporization or DesignPracticesProcessRegime.TubeSideVaporization)
        {
            AddDp09eVaporizationReview();
            recommendations.Add("DP09E: vaporization design must check heat flux, vapor blanketing, choke flow, and reboiler-specific circulation before final release.");
        }
    }

    private void AddDp09cShellTypeReview()
    {
        if (Variables.ShellType == ShellAndTubeShellType.OnePass)
        {
            if (Variables.ShellSidePressureDropUtilization.Value.GetValue(UnitLessUnits.None) > 1d &&
                IsShellSideGasOrCondensingVapor())
            {
                recommendations.Add("DP09C/DP09D: shell-side gas/vapor pressure drop is high; review TEMA X cross-flow shell or parallel shells before final design.");
            }

            return;
        }

        var shellTypeLetter = GetTemaShellTypeLetter(Variables.ShellType);
        requiredMethodImplementations.Add($"DP09C/DP09D shell-side method for TEMA {shellTypeLetter} shell");
        recommendations.Add($"DP09C: selected TEMA shell type {shellTypeLetter} requires the matching shell-side correction method; current preliminary hydraulic calculation remains based on E-shell assumptions.");
    }

    private void AddDp09cTemaHeadSelectionBoundaryReview()
    {
        if (Variables.TubeSideCleaningMethod == ShellAndTubeCleaningMethod.NotSpecified ||
            Variables.ShellSideCleaningMethod == ShellAndTubeCleaningMethod.NotSpecified)
        {
            requiredMethodImplementations.Add("DP09C TEMA cleaning-method selection");
            recommendations.Add("DP09C Table 3: final TEMA selection requires tube-side and shell-side cleaning methods; current DP review applies only the cleaning methods already specified.");
        }

        if (Variables.FrontHeadType == ShellAndTubeFrontHeadType.Bonnet &&
            IsMechanicalCleaning(Variables.TubeSideCleaningMethod))
        {
            recommendations.Add("DP09C Table 3: mechanical tube-side cleaning normally favors a removable-channel front head instead of a bonnet.");
        }

        if (Variables.FrontHeadType == ShellAndTubeFrontHeadType.IntegralTubesheetRemovableCover &&
            Variables.RearHeadType != Dp09dRearHeadType.FixedTubesheet)
        {
            recommendations.Add("DP09C Table 3: C front heads are primarily associated with fixed-tubesheet construction; review the selected removable rear head.");
        }

        AddDp09cCorrosionAllowanceFrontHeadReview();
    }

    private static bool IsMechanicalCleaning(ShellAndTubeCleaningMethod cleaningMethod) =>
        cleaningMethod is ShellAndTubeCleaningMethod.Mechanical or ShellAndTubeCleaningMethod.ChemicalOrMechanical;

    private void AddDp09cCorrosionAllowanceFrontHeadReview()
    {
        if (!Variables.TubeSideCorrosionAllowance.IsDefined ||
            !Variables.ShellSideCorrosionAllowance.IsDefined)
        {
            requiredMethodImplementations.Add("DP09C corrosion-allowance front-head review");
            recommendations.Add("DP09C Table 3: front-head preference also depends on tube-side and shell-side corrosion allowance.");
            return;
        }

        var tubeSideCorrosionAllowance = Variables.TubeSideCorrosionAllowance.Value.GetValue(LengthUnits.Inch);
        var shellSideCorrosionAllowance = Variables.ShellSideCorrosionAllowance.Value.GetValue(LengthUnits.Inch);
        var maximumCorrosionAllowance = Math.Max(tubeSideCorrosionAllowance, shellSideCorrosionAllowance);

        if (maximumCorrosionAllowance >= 0.125d &&
            Variables.FrontHeadType != ShellAndTubeFrontHeadType.RemovableChannelAndCover)
        {
            recommendations.Add("DP09C Table 3: corrosion allowance of 1/8 in or more favors an A front head with removable channel and cover.");
        }
    }

    private void AddDp09cTubeCountCorrectionReview()
    {
        var correctionFactor = Variables.TubeCountCorrectionFactor.Value.GetValue(UnitLessUnits.None);
        if (correctionFactor >= 0.999d)
        {
            return;
        }

        var uTubeLoss = Variables.UTubeBendTubeLossFraction.Value.GetValue(UnitLessUnits.None);
        var impingementLoss = Variables.ImpingementTubeLossFraction.Value.GetValue(UnitLessUnits.None);
        var shellNozzleLoss = Variables.ShellNozzleTubeLossFraction.Value.GetValue(UnitLessUnits.None);
        recommendations.Add($"DP09C: tube-count capacity applies a preliminary {correctionFactor:0%} correction for U-bend ({uTubeLoss:0%}) and Figure 8 shell-nozzle/impingement factor ({shellNozzleLoss:0%}); final mechanical layout should verify actual removed tubes.");
    }

    private void AddDp09aEnhancedHeatTransferReview()
    {
        switch (Variables.EnhancedHeatTransferType)
        {
            case ShellAndTubeEnhancedHeatTransferType.IntegralFinnedTubes:
                if (processRegime is DesignPracticesProcessRegime.ShellSideVaporization or DesignPracticesProcessRegime.TubeSideVaporization)
                {
                    recommendations.Add("DP09A/DP09E: integral finned tubes use preliminary Figures A10/A11/A12 boiling correction; final area basis and vendor fin geometry should still be confirmed.");
                }
                else
                {
                    requiredMethodImplementations.Add("DP09A integral-finned tube heat-transfer method");
                    recommendations.Add("DP09A: integral finned tubes require fin efficiency and outside-area-basis review; current preliminary U and area remain on a smooth-tube basis.");
                }
                break;
            case ShellAndTubeEnhancedHeatTransferType.NucleateBoilingTubes:
                requiredMethodImplementations.Add("DP09E nucleate-boiling tube method");
                recommendations.Add("DP09A/DP09E: nucleate boiling tubes require dedicated boiling-curve and critical-heat-flux review; current heat-flux check remains a smooth-tube preliminary screen.");
                break;
            case ShellAndTubeEnhancedHeatTransferType.TurbulencePromoters:
                requiredMethodImplementations.Add("DP09D tube-side turbulence-promoter method");
                recommendations.Add("DP09A/DP09D: turbulence promoters require tube-side heat-transfer and friction correction; current tube-side coefficient and pressure drop remain on a plain-tube basis.");
                break;
            case ShellAndTubeEnhancedHeatTransferType.OnlineMechanicalCleaning:
                recommendations.Add("DP09A/DP09B: online mechanical cleaning should be checked against tube passes, nozzles, water quality, and fouling strategy before reducing fouling allowance.");
                break;
            case ShellAndTubeEnhancedHeatTransferType.RodBaffles:
                requiredMethodImplementations.Add("DP09A/DP09C/DP09D rod-baffle shell-side method");
                recommendations.Add("DP09A/DP09C/DP09D: rod-baffle exchangers require a dedicated shell-side vibration, pressure-drop, and heat-transfer method; current shell-side calculation remains single-segmental preliminary.");
                break;
            case ShellAndTubeEnhancedHeatTransferType.HelicalBaffles:
                requiredMethodImplementations.Add("DP09A/DP09C/DP09D helical-baffle shell-side method");
                recommendations.Add("DP09A/DP09C/DP09D: helical baffles require a dedicated shell-side pressure-drop and heat-transfer method; current shell-side calculation remains single-segmental preliminary.");
                break;
            case ShellAndTubeEnhancedHeatTransferType.TwistedTubes:
                requiredMethodImplementations.Add("DP09A twisted-tube heat-transfer method");
                recommendations.Add("DP09A: twisted-tube exchangers require vendor or validated proprietary-style correlations before replacing the smooth-tube preliminary basis.");
                break;
        }
    }

    private void AddDp09bCondenserArrangementReview()
    {
        if (processRegime is not (DesignPracticesProcessRegime.ShellSideCondensation or DesignPracticesProcessRegime.TubeSideCondensation))
        {
            return;
        }

        switch (Variables.CondenserArrangement)
        {
            case ShellAndTubeCondenserArrangement.NotSpecified:
                recommendations.Add("DP09B/DP09F: condenser arrangement is not specified; final design should confirm location, drainage, venting, receiver connection, and available pressure drop.");
                break;
            case ShellAndTubeCondenserArrangement.ConventionalWithAccumulator:
                recommendations.Add("DP09B/DP09F: conventional condenser with accumulator should verify gravity drainage, vapor disengagement, noncondensable venting, and liquid seal requirements.");
                break;
            case ShellAndTubeCondenserArrangement.ElevatedAboveReceiver:
                recommendations.Add("DP09B/DP09F: elevated condenser arrangement should include static head, floodback risk, receiver pressure control, and subcooling review before finalizing pressure drop.");
                break;
            case ShellAndTubeCondenserArrangement.DrumlessCondenser:
                ApplyDp09fDrumlessCondenserCriteria();
                break;
            case ShellAndTubeCondenserArrangement.SurfaceCondenser:
                requiredMethodImplementations.Add("HEI steam surface condenser sizing");
                recommendations.Add("DP09F: surface condenser service should be checked against HEI-style vacuum, air-removal, and cooling-water allocation requirements in addition to this preliminary DP shell-and-tube sizing.");
                break;
        }
    }

    private void AddDp09fCondensationServiceReview()
    {
        if (processRegime is not (DesignPracticesProcessRegime.ShellSideCondensation or DesignPracticesProcessRegime.TubeSideCondensation))
        {
            return;
        }

        var condensingInlet = processRegime == DesignPracticesProcessRegime.ShellSideCondensation
            ? request.ShellSideInlet.Stream
            : request.TubeSideInlet.Stream;
        var condensingOutlet = processRegime == DesignPracticesProcessRegime.ShellSideCondensation
            ? request.ShellSideOutlet.Stream
            : request.TubeSideOutlet.Stream;

        if (IsPureWater(condensingInlet) && IsPureWater(condensingOutlet))
        {
            recommendations.Add("DP09F: pure steam/water condensation is treated as pure-component condensation; the Eq. 9 vapor mass-velocity correction is not applied.");
            return;
        }

        if (HasWaterAndNonWaterComponents(condensingInlet) || HasWaterAndNonWaterComponents(condensingOutlet))
        {
            requiredMethodImplementations.Add("DP09F hydrocarbon/steam dew-point and T-Q zone split");
            recommendations.Add("DP09F: hydrocarbon condensation with steam requires hydrocarbon dew point, steam dew point, and T-Q zone splitting; in mixed condensing zones the steam condensing coefficient follows the hydrocarbon condensate film coefficient.");
        }
    }

    private void ApplyDp09fDrumlessCondenserCriteria()
    {
        const double surfaceAllowanceFactor = 1.10d;
        const double ventDiameterInches = 2d;
        const double separatorPotMinimumLengthFeet = 3d;
        const double separatorPotMaximumLengthFeet = 5d;
        const double minimumElevationFeet = 20d;

        var requiredSurface = Variables.RequiredArea.Value.GetValue(SurfaceUnits.Foot2);
        var drumlessSurface = requiredSurface * surfaceAllowanceFactor;
        var condensingOutlet = GetCondensingOutletStream();
        var liquidFlowFt3s = ReadOutletLiquidVolumetricFlow(condensingOutlet);
        var separatorPotDiameter = SelectDp09fDrumlessSeparatorPotDiameterInches(liquidFlowFt3s);

        Variables.DrumlessCondenserSurfaceAllowanceFactor.SetValue(new UnitLess(surfaceAllowanceFactor), VariableDefinedBy.Equipment);
        Variables.DrumlessCondenserRequiredSurface.SetValue(new Area(drumlessSurface, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
        Variables.DrumlessCondenserVentDiameter.SetValue(new Diameter(ventDiameterInches, DiameterUnits.Inch), VariableDefinedBy.Equipment);
        Variables.DrumlessCondenserSeparatorPotDiameter.SetValue(new Diameter(separatorPotDiameter, DiameterUnits.Inch), VariableDefinedBy.Equipment);
        Variables.DrumlessCondenserSeparatorPotMinimumLength.SetValue(new Length(separatorPotMinimumLengthFeet, LengthUnits.Foot), VariableDefinedBy.Equipment);
        Variables.DrumlessCondenserSeparatorPotMaximumLength.SetValue(new Length(separatorPotMaximumLengthFeet, LengthUnits.Foot), VariableDefinedBy.Equipment);
        Variables.DrumlessCondenserMinimumElevation.SetValue(new Length(minimumElevationFeet, LengthUnits.Foot), VariableDefinedBy.Equipment);

        recommendations.Add($"DP09F: drumless condenser surface is increased to {surfaceAllowanceFactor:0%} of condensing surface, giving {drumlessSurface:0.##} ft2 preliminary required surface.");
        recommendations.Add($"DP09F: drumless condenser outlet pot is preliminarily sized at {separatorPotDiameter:0.#} in diameter for liquid-vapor separation, with {separatorPotMinimumLengthFeet:0.#}-{separatorPotMaximumLengthFeet:0.#} ft length, 2 in vent near the liquid outlet, full-diameter gauge glass, anti-vortex baffle, and minimum {minimumElevationFeet:0.#} ft shell-bottom elevation.");
        recommendations.Add("DP09F: drumless condenser pump suction piping should slope continuously to the pump, with suction-line velocity limits checked separately against the selected line size.");
    }

    private IFacadeStream GetCondensingOutletStream() =>
        processRegime == DesignPracticesProcessRegime.ShellSideCondensation
            ? request.ShellSideOutlet.Stream
            : request.TubeSideOutlet.Stream;

    private static double ReadOutletLiquidVolumetricFlow(IFacadeStream outlet)
    {
        if (outlet.VolumetricFlow.IsDefined)
        {
            return outlet.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg);
        }

        EnsureDefined(outlet.MassFlow.IsDefined, outlet.Name, "mass flow");
        EnsureDefined(outlet.MassDensity.IsDefined, outlet.Name, "mass density");

        return outlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr) /
               Math.Max(outlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3), 1e-12) /
               3600d;
    }

    private static double SelectDp09fDrumlessSeparatorPotDiameterInches(double liquidFlowFt3s)
    {
        var diameterForOneFeetPerSecond = CalculateDiameterForVelocityInches(liquidFlowFt3s, 1d);
        if (diameterForOneFeetPerSecond <= 14d)
        {
            return RoundUpToNearestEvenInch(Math.Max(diameterForOneFeetPerSecond, 2d));
        }

        return Math.Max(16d, RoundUpToNearestEvenInch(CalculateDiameterForVelocityInches(liquidFlowFt3s, 1.5d)));
    }

    private static double CalculateDiameterForVelocityInches(double flowFt3s, double velocityFeetPerSecond)
    {
        return Math.Sqrt(4d * Math.Max(flowFt3s, 0d) / (Math.PI * Math.Max(velocityFeetPerSecond, 1e-12))) * 12d;
    }

    private static double RoundUpToNearestEvenInch(double diameterInches)
    {
        return Math.Ceiling(diameterInches / 2d) * 2d;
    }

    private void AddDp09cBaffleReview()
    {
        var baffleCut = Variables.BaffleCutPercent.Value.GetValue(UnitLessUnits.None);
        if (Variables.BaffleType != ShellAndTubeBaffleType.SingleSegmental)
        {
            requiredMethodImplementations.Add($"DP09C/DP09D shell-side method for {GetBaffleTypeLabel(Variables.BaffleType)} baffles");
            recommendations.Add($"DP09C/DP09D: selected baffle type {GetBaffleTypeLabel(Variables.BaffleType)} requires a dedicated shell-side correction method; current preliminary hydraulic calculation assumes 25% cut single-segmental baffles.");
        }

        if (baffleCut is < 15d or > 45d)
        {
            recommendations.Add($"DP09C/DP09D: baffle cut is {baffleCut:0.#}%; review against mechanical clearance, shell-side pressure drop, and vibration before final design.");
        }
        else if (Math.Abs(baffleCut - 25d) > 1e-9)
        {
            recommendations.Add($"DP09D: baffle cut is {baffleCut:0.#}%; preliminary DP estimation starts from 25% cut single-segmental baffles, so shell-side correction should be reviewed.");
        }
    }

    private double ApplyDp09dTemperatureCorrectionFactor(
        double counterCurrentLmtd,
        double shellInTemperatureF,
        double shellOutTemperatureF,
        double tubeInTemperatureF,
        double tubeOutTemperatureF)
    {
        var shellPasses = Variables.ShellPasses.Value.GetValue(UnitLessUnits.None);
        var tubePasses = Variables.TubePasses.Value.GetValue(UnitLessUnits.None);

        if (shellPasses <= 1d && tubePasses <= 1d)
        {
            Variables.LogMeanTemperatureCorrectionFactor.SetValue(new UnitLess(1d), VariableDefinedBy.Equipment);
            return counterCurrentLmtd;
        }

        var shellAverage = (shellInTemperatureF + shellOutTemperatureF) / 2d;
        var tubeAverage = (tubeInTemperatureF + tubeOutTemperatureF) / 2d;
        var hotIn = shellAverage >= tubeAverage ? shellInTemperatureF : tubeInTemperatureF;
        var hotOut = shellAverage >= tubeAverage ? shellOutTemperatureF : tubeOutTemperatureF;
        var coldIn = shellAverage >= tubeAverage ? tubeInTemperatureF : shellInTemperatureF;
        var coldOut = shellAverage >= tubeAverage ? tubeOutTemperatureF : shellOutTemperatureF;
        var correctionFactor = Dp09dShellAndTubeCorrelations.TemperatureCorrectionFactor(
            Math.Max(shellPasses, 1d),
            hotIn,
            hotOut,
            coldIn,
            coldOut);
        Variables.LogMeanTemperatureCorrectionFactor.SetValue(new UnitLess(correctionFactor), VariableDefinedBy.Equipment);

        if (correctionFactor < 0.8d)
        {
            recommendations.Add($"DP09D: LMTD correction factor is {correctionFactor:0.###}; revise terminal temperatures or shell arrangement when F is below 0.80.");
        }
        else if (correctionFactor < 0.99d)
        {
            recommendations.Add(shellPasses > 1d
                ? $"DP09D: LMTD was corrected with a {shellPasses:0.#}-shell-pass temperature factor of {correctionFactor:0.###}."
                : $"DP09D: LMTD was corrected with a one-shell-pass temperature factor of {correctionFactor:0.###}.");
        }

        return counterCurrentLmtd * correctionFactor;
    }

    private void AddDp09eVaporizationReview()
    {
        if (!Variables.ActualArea.IsDefined || Variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2) <= 0d)
        {
            return;
        }

        var heatFlux = Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr) /
                       Math.Max(Variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2), 1e-12);
        Variables.HeatFlux.SetValue(new HeatSurfaceFlow(heatFlux, HeatSurfaceFlowUnits.BTU_hr_ft2), VariableDefinedBy.Equipment);
        var vaporizedFraction = CalculateDp09eVaporizedFraction();
        Variables.VaporizedFraction.SetValue(new UnitLess(vaporizedFraction), VariableDefinedBy.Equipment);
        TryCalculateDp09eMaximumAllowableHeatFlux();
        TryCalculateDp09eVerticalThermosiphonChokeFlowLimit(heatFlux);
        TryCalculateDp09eNucleateBoilingReferenceCoefficient(heatFlux);
        TryCalculateDp09eReboilerHydraulicPrecheck(heatFlux, vaporizedFraction);
        AddDp09eHeatFluxLimitReview(heatFlux);
        AddDp09eReboilerTypeReview(heatFlux, vaporizedFraction);
        ApplyDp09eReboilerElevationAndCirculationCriteria();
        AddDp09eVaporizingSidePressureDropReview(vaporizedFraction);
        recommendations.Add($"DP09E: preliminary heat flux is {heatFlux:0.##} Btu/hr-ft2 and vaporized fraction is {vaporizedFraction:0.###}.");
    }

    private void AddDp09bFoulingDominanceReview(double foulingResistanceFraction)
    {
        if (foulingResistanceFraction >= 0.50d)
        {
            recommendations.Add($"DP09B: fouling represents {foulingResistanceFraction:0%} of the service thermal resistance; plant data, cleaning strategy, and fouling allocation should control the design.");
            return;
        }

        if (foulingResistanceFraction <= 0.05d)
        {
            recommendations.Add($"DP09B: fouling represents only {foulingResistanceFraction:0%} of service thermal resistance; sensitivity to fouling factor is likely small.");
        }
    }

    private void AddDp09bCoolingWaterTemperatureReview()
    {
        if (Variables.CoolingWaterType == ShellAndTubeCoolingWaterType.None)
        {
            return;
        }

        var bulkLimitF = Variables.CoolingWaterType switch
        {
            ShellAndTubeCoolingWaterType.SaltWater => 120d,
            ShellAndTubeCoolingWaterType.BrackishWater => 125d,
            _ => 130d
        };
        var filmLimitF = Variables.CoolingWaterType is ShellAndTubeCoolingWaterType.SaltWater or ShellAndTubeCoolingWaterType.BrackishWater ? 140d : 150d;
        Variables.CoolingWaterOutletBulkTemperatureLimit.SetValue(
            new Temperature(bulkLimitF, TemperatureUnits.DegreeFahrenheit),
            VariableDefinedBy.Equipment);
        Variables.CoolingWaterFilmTemperatureLimit.SetValue(
            new Temperature(filmLimitF, TemperatureUnits.DegreeFahrenheit),
            VariableDefinedBy.Equipment);
        AddDp09dCoolingWaterTubeVelocityReview();

        var shellAverage = (ReadTemperatureF(request.ShellSideInlet.Stream) + ReadTemperatureF(request.ShellSideOutlet.Stream)) / 2d;
        var tubeAverage = (ReadTemperatureF(request.TubeSideInlet.Stream) + ReadTemperatureF(request.TubeSideOutlet.Stream)) / 2d;
        var coolingWaterOutletF = shellAverage <= tubeAverage
            ? ReadTemperatureF(request.ShellSideOutlet.Stream)
            : ReadTemperatureF(request.TubeSideOutlet.Stream);
        var waterLabel = Variables.CoolingWaterType switch
        {
            ShellAndTubeCoolingWaterType.SaltWater => "salt water",
            ShellAndTubeCoolingWaterType.BrackishWater => "brackish water",
            _ => "fresh water"
        };

        if (coolingWaterOutletF > bulkLimitF)
        {
            recommendations.Add($"DP09B: cooling water outlet bulk temperature is {coolingWaterOutletF:0.#} F, above the {bulkLimitF:0.#} F {waterLabel} limit; increase water flow or revise the cooling-water temperature rise.");
        }
        else
        {
            recommendations.Add($"DP09B: cooling water outlet bulk temperature is within the {bulkLimitF:0.#} F {waterLabel} limit.");
        }

        AddDp09bCoolingWaterFilmTemperatureReview(shellAverage, tubeAverage, coolingWaterOutletF, filmLimitF, waterLabel);
    }

    private void AddDp09bCoolingWaterFilmTemperatureReview(
        double shellAverageTemperatureF,
        double tubeAverageTemperatureF,
        double coolingWaterOutletF,
        double filmLimitF,
        string waterLabel)
    {
        var heatFlux = Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr) /
                       Math.Max(Variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2), 1e-12);
        var coolingWaterCoefficient = tubeAverageTemperatureF <= shellAverageTemperatureF
            ? Variables.TubeSideHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F)
            : Variables.ShellSideHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);

        if (coolingWaterCoefficient <= 0d)
        {
            recommendations.Add($"DP09B: {waterLabel} film temperature limit is {filmLimitF:0.#} F; film-temperature review requires a positive cooling-side heat-transfer coefficient.");
            return;
        }

        var wallTemperatureF = coolingWaterOutletF + heatFlux / Math.Max(coolingWaterCoefficient, 1e-12);
        var filmTemperatureF = (coolingWaterOutletF + wallTemperatureF) / 2d;
        Variables.CoolingWaterEstimatedTubeWallTemperature.SetValue(
            new Temperature(wallTemperatureF, TemperatureUnits.DegreeFahrenheit),
            VariableDefinedBy.Equipment);
        Variables.CoolingWaterEstimatedFilmTemperature.SetValue(
            new Temperature(filmTemperatureF, TemperatureUnits.DegreeFahrenheit),
            VariableDefinedBy.Equipment);

        if (tubeAverageTemperatureF > shellAverageTemperatureF)
        {
            recommendations.Add("DP09B: cooling-water film temperature was estimated on the apparent shell-side cold stream; confirm the selected cooling-water type matches the actual side allocation.");
        }

        if (filmTemperatureF > filmLimitF)
        {
            recommendations.Add($"DP09B: estimated {waterLabel} film temperature is {filmTemperatureF:0.#} F, above the {filmLimitF:0.#} F limit; increase water rate, reduce heat flux, or revise side allocation.");
            return;
        }

        recommendations.Add($"DP09B: estimated {waterLabel} film temperature is {filmTemperatureF:0.#} F, within the {filmLimitF:0.#} F limit.");
    }

    private void AddDp09dCoolingWaterTubeVelocityReview()
    {
        var range = Dp09cShellAndTubeCatalog.GetCoolingWaterTubeVelocityRange(Variables.TubeMaterial, Variables.CoolingWaterType);
        if (range is null)
        {
            recommendations.Add("DP09D: cooling-water tube velocity range is not available for the selected tube material/water combination; review Table 4 manually.");
            return;
        }

        Variables.CoolingWaterMinimumTubeVelocity.SetValue(
            new Velocity(range.MinimumFeetPerSecond, VelocityUnits.FeetPerSecond),
            VariableDefinedBy.Equipment);
        Variables.CoolingWaterMaximumTubeVelocity.SetValue(
            new Velocity(range.MaximumFeetPerSecond, VelocityUnits.FeetPerSecond),
            VariableDefinedBy.Equipment);

        var shellAverage = (ReadTemperatureF(request.ShellSideInlet.Stream) + ReadTemperatureF(request.ShellSideOutlet.Stream)) / 2d;
        var tubeAverage = (ReadTemperatureF(request.TubeSideInlet.Stream) + ReadTemperatureF(request.TubeSideOutlet.Stream)) / 2d;
        if (tubeAverage > shellAverage)
        {
            recommendations.Add("DP09D: cooling-water velocity range applies to water inside tubes; the current colder side appears to be shell side, so tube-side velocity should be reviewed against the actual side allocation.");
        }

        var tubeVelocity = Variables.TubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond);
        if (tubeVelocity < range.MinimumFeetPerSecond || tubeVelocity > range.MaximumFeetPerSecond)
        {
            recommendations.Add($"DP09D: tube-side cooling-water velocity is {tubeVelocity:0.##} ft/s, outside the {range.MinimumFeetPerSecond:0.##}-{range.MaximumFeetPerSecond:0.##} ft/s range for {range.Basis}.");
            return;
        }

        recommendations.Add($"DP09D: tube-side cooling-water velocity is within the {range.MinimumFeetPerSecond:0.##}-{range.MaximumFeetPerSecond:0.##} ft/s range for {range.Basis}.");
    }

    private double CalculateDp09eVaporizedFraction()
    {
        var inlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideInlet.Stream
            : request.TubeSideInlet.Stream;
        var outlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideOutlet.Stream
            : request.TubeSideOutlet.Stream;
        var inletVapor = inlet.VaporFraction.IsDefined
            ? inlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : 0d;
        var outletVapor = outlet.VaporFraction.IsDefined
            ? outlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : inletVapor;

        return Math.Clamp(outletVapor - inletVapor, 0d, 1d);
    }

    private void TryCalculateDp09eMaximumAllowableHeatFlux()
    {
        if (!Variables.VaporizingSideCriticalPressure.IsDefined ||
            Variables.VaporizingSideCriticalPressure.Value.GetValue(PressureUnits.Psia) <= 0d)
        {
            return;
        }

        var vaporizingInlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideInlet.Stream
            : request.TubeSideInlet.Stream;
        if (!vaporizingInlet.Pressure.IsDefined)
        {
            recommendations.Add("DP09E: vaporizing-side pressure is required to calculate Figure A3 maximum heat flux.");
            return;
        }

        var criticalPressure = Variables.VaporizingSideCriticalPressure.Value.GetValue(PressureUnits.Psia);
        var operatingPressure = vaporizingInlet.Pressure.Value.GetValue(PressureUnits.Psia);
        var singleTubeMaximumHeatFlux = Dp09eVaporizationCorrelations.SingleTubeMaximumHeatFlux(criticalPressure, operatingPressure);
        var pitchRatio = Variables.TubePitch.Value.GetValue(DiameterUnits.Inch) /
                         Math.Max(Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch), 1e-12);
        var tubeCount = Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None);
        var bundleCorrectionFactor = Dp09eVaporizationCorrelations.BundleCorrectionFactor(tubeCount, pitchRatio);
        var bundleMaximumHeatFlux = singleTubeMaximumHeatFlux * bundleCorrectionFactor;

        Variables.SingleTubeMaximumHeatFlux.SetValue(new HeatSurfaceFlow(singleTubeMaximumHeatFlux, HeatSurfaceFlowUnits.BTU_hr_ft2), VariableDefinedBy.Equipment);
        Variables.BundleHeatFluxCorrectionFactor.SetValue(new UnitLess(bundleCorrectionFactor), VariableDefinedBy.Equipment);
        Variables.BundleMaximumHeatFlux.SetValue(new HeatSurfaceFlow(bundleMaximumHeatFlux, HeatSurfaceFlowUnits.BTU_hr_ft2), VariableDefinedBy.Equipment);

        if (!IsUserDefined(Variables.MaximumAllowableHeatFlux))
        {
            Variables.MaximumAllowableHeatFlux.SetValue(new HeatSurfaceFlow(bundleMaximumHeatFlux, HeatSurfaceFlowUnits.BTU_hr_ft2), VariableDefinedBy.Equipment);
        }

        recommendations.Add($"DP09E: maximum allowable heat flux was estimated from Figures A3/A4 using critical pressure {criticalPressure:0.##} psia and pitch ratio {pitchRatio:0.###}.");
    }

    private void TryCalculateDp09eVerticalThermosiphonChokeFlowLimit(double heatFlux)
    {
        if (Variables.ReboilerType != ShellAndTubeReboilerType.VerticalThermosiphon)
        {
            return;
        }

        if (!Variables.VaporizingSideCriticalPressure.IsDefined ||
            Variables.VaporizingSideCriticalPressure.Value.GetValue(PressureUnits.Psia) <= 0d)
        {
            recommendations.Add("DP09E: vertical thermosiphon choke-flow review requires vaporizing-side critical pressure for Figure A14.");
            return;
        }

        var vaporizingInlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideInlet.Stream
            : request.TubeSideInlet.Stream;
        if (!vaporizingInlet.Pressure.IsDefined)
        {
            recommendations.Add("DP09E: vertical thermosiphon choke-flow review requires vaporizing-side operating pressure for Figure A14.");
            return;
        }

        var tubeInsideDiameterInches = Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeLengthFeet = Variables.TubeLength.Value.GetValue(LengthUnits.Foot);
        if (tubeInsideDiameterInches <= 0d || tubeLengthFeet <= 0d)
        {
            recommendations.Add("DP09E: vertical thermosiphon choke-flow review requires positive tube inside diameter and tube length for Figure A15.");
            return;
        }

        var criticalPressure = Variables.VaporizingSideCriticalPressure.Value.GetValue(PressureUnits.Psia);
        var operatingPressure = vaporizingInlet.Pressure.Value.GetValue(PressureUnits.Psia);
        var referenceMaximumHeatFlux = Dp09eVaporizationCorrelations.VerticalThermosiphonChokeReferenceMaximumHeatFlux(
            criticalPressure,
            operatingPressure);
        var tubeGeometryCorrection = Dp09eVaporizationCorrelations.VerticalThermosiphonTubeGeometryCorrectionFactor(
            tubeInsideDiameterInches,
            tubeLengthFeet);
        var chokeMaximumHeatFlux = 0.70d * referenceMaximumHeatFlux * tubeGeometryCorrection;
        var utilization = heatFlux / Math.Max(chokeMaximumHeatFlux, 1e-12);

        Variables.VerticalThermosiphonChokeReferenceMaximumHeatFlux.SetValue(
            new HeatSurfaceFlow(referenceMaximumHeatFlux, HeatSurfaceFlowUnits.BTU_hr_ft2),
            VariableDefinedBy.Equipment);
        Variables.VerticalThermosiphonTubeGeometryCorrectionFactor.SetValue(
            new UnitLess(tubeGeometryCorrection),
            VariableDefinedBy.Equipment);
        Variables.VerticalThermosiphonChokeMaximumHeatFlux.SetValue(
            new HeatSurfaceFlow(chokeMaximumHeatFlux, HeatSurfaceFlowUnits.BTU_hr_ft2),
            VariableDefinedBy.Equipment);
        Variables.VerticalThermosiphonChokeHeatFluxUtilization.SetValue(new UnitLess(utilization), VariableDefinedBy.Equipment);

        if (heatFlux > chokeMaximumHeatFlux)
        {
            recommendations.Add($"DP09E: vertical thermosiphon choke-flow utilization is {utilization:0%}; it exceeds the Figure A14/A15 limit with the 70% design factor.");
            return;
        }

        recommendations.Add($"DP09E: vertical thermosiphon choke-flow utilization is {utilization:0%}; it is within the Figure A14/A15 limit with the 70% design factor.");
    }

    private void TryCalculateDp09eNucleateBoilingReferenceCoefficient(double heatFlux)
    {
        if (!Variables.VaporizingSideCriticalPressure.IsDefined ||
            Variables.VaporizingSideCriticalPressure.Value.GetValue(PressureUnits.Psia) <= 0d ||
            heatFlux <= 0d)
        {
            return;
        }

        var criticalPressure = Variables.VaporizingSideCriticalPressure.Value.GetValue(PressureUnits.Psia);
        var referenceCoefficient = Dp09eVaporizationCorrelations.SingleTubeNucleateBoilingReferenceCoefficient(
            criticalPressure,
            Math.Abs(heatFlux));

        Variables.SingleTubeNucleateBoilingReferenceCoefficient.SetValue(
            new HeatTransferCoefficient(referenceCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);

        var vaporizingInlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideInlet.Stream
            : request.TubeSideInlet.Stream;
        if (!vaporizingInlet.Pressure.IsDefined)
        {
            recommendations.Add($"DP09E: single-tube nucleate boiling reference coefficient was estimated from Figure A5 as {referenceCoefficient:0.##} Btu/hr-ft2-F; enter vaporizing-side pressure to apply Figure A6 pressure correction.");
            return;
        }

        var operatingPressure = vaporizingInlet.Pressure.Value.GetValue(PressureUnits.Psia);
        var pressureCorrectionFactor = Dp09eVaporizationCorrelations.NucleateBoilingPressureCorrectionFactor(
            criticalPressure,
            operatingPressure);
        var pressureCorrectedCoefficient = referenceCoefficient * pressureCorrectionFactor;

        Variables.NucleateBoilingPressureCorrectionFactor.SetValue(new UnitLess(pressureCorrectionFactor), VariableDefinedBy.Equipment);
        recommendations.Add($"DP09E: single-tube nucleate boiling coefficient is {pressureCorrectedCoefficient:0.##} Btu/hr-ft2-F after Figure A6 pressure correction Fp={pressureCorrectionFactor:0.###}.");

        if (!TryApplyDp09eMixtureCorrection(
            heatFlux,
            vaporizingInlet,
            processRegime == DesignPracticesProcessRegime.ShellSideVaporization
                ? request.ShellSideOutlet.Stream
                : request.TubeSideOutlet.Stream,
            pressureCorrectedCoefficient))
        {
            var correctedCoefficient = ApplyDp09eVerticalThermosiphonNaturalConvectionCorrection(
                pressureCorrectedCoefficient,
                heatFlux);
            Variables.SingleTubeNucleateBoilingCoefficient.SetValue(
                new HeatTransferCoefficient(correctedCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
                VariableDefinedBy.Equipment);
            ApplyDp09eBundleBoilingCorrection(correctedCoefficient);
        }
    }

    private bool TryApplyDp09eMixtureCorrection(
        double heatFlux,
        IFacadeStream vaporizingInlet,
        IFacadeStream vaporizingOutlet,
        double pressureCorrectedCoefficient)
    {
        if (!Variables.VaporizingSideBoilingRange.IsDefined ||
            Variables.VaporizingSideBoilingRange.Value.GetValue(TemperatureUnits.DegreeFahrenheit) <= 0d)
        {
            recommendations.Add("DP09E: enter vaporizing-side boiling range to apply Figures A7/A8 mixture correction.");
            return false;
        }

        if (!TryCalculateDp09eVaporToLiquidDensityRatio(vaporizingInlet, vaporizingOutlet, out var densityRatio))
        {
            recommendations.Add("DP09E: vaporizing-side inlet/outlet densities are required to estimate Figure A7 effective temperature range and Figure A8 mixture correction.");
            return false;
        }

        var boilingRangeF = Variables.VaporizingSideBoilingRange.Value.GetValue(TemperatureUnits.DegreeFahrenheit);
        var effectiveMinimumHeatFlux = Dp09eVaporizationCorrelations.NucleateBoilingEffectiveMinimumHeatFlux(boilingRangeF);
        var effectiveTemperatureRange = Dp09eVaporizationCorrelations.EffectiveTemperatureRange(densityRatio);
        var boilingRangeParameter = boilingRangeF / Math.Max(effectiveTemperatureRange, 1e-12);
        var heatFluxRatio = Math.Abs(heatFlux) / Math.Max(effectiveMinimumHeatFlux, 1e-12);
        var mixtureCorrectionFactor = Dp09eVaporizationCorrelations.MixtureCorrectionFactor(
            heatFluxRatio,
            boilingRangeParameter);
        var mixtureCorrectedCoefficient = ApplyDp09eVerticalThermosiphonNaturalConvectionCorrection(
            pressureCorrectedCoefficient * Math.Pow(mixtureCorrectionFactor, 0.67d),
            heatFlux);

        Variables.NucleateBoilingVaporToLiquidDensityRatio.SetValue(new UnitLess(densityRatio), VariableDefinedBy.Equipment);
        Variables.NucleateBoilingEffectiveMinimumHeatFlux.SetValue(
            new HeatSurfaceFlow(effectiveMinimumHeatFlux, HeatSurfaceFlowUnits.BTU_hr_ft2),
            VariableDefinedBy.Equipment);
        Variables.NucleateBoilingEffectiveTemperatureRange.SetValue(
            new Temperature(effectiveTemperatureRange, TemperatureUnits.DegreeFahrenheit),
            VariableDefinedBy.Equipment);
        Variables.NucleateBoilingRangeParameter.SetValue(new UnitLess(boilingRangeParameter), VariableDefinedBy.Equipment);
        Variables.NucleateBoilingMixtureCorrectionFactor.SetValue(new UnitLess(mixtureCorrectionFactor), VariableDefinedBy.Equipment);
        Variables.SingleTubeNucleateBoilingCoefficient.SetValue(
            new HeatTransferCoefficient(mixtureCorrectedCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        recommendations.Add($"DP09E: Figure A8 mixture correction Fc={mixtureCorrectionFactor:0.###} was applied using BR/DR={boilingRangeParameter:0.###}; corrected nucleate boiling coefficient is {mixtureCorrectedCoefficient:0.##} Btu/hr-ft2-F.");
        ApplyDp09eBundleBoilingCorrection(mixtureCorrectedCoefficient);
        return true;
    }

    private double ApplyDp09eVerticalThermosiphonNaturalConvectionCorrection(double boilingCoefficient, double heatFlux)
    {
        if (Variables.ReboilerType != ShellAndTubeReboilerType.VerticalThermosiphon ||
            !Variables.VaporizingSideCriticalPressure.IsDefined ||
            Variables.VaporizingSideCriticalPressure.Value.GetValue(PressureUnits.Psia) <= 0d ||
            heatFlux <= 0d)
        {
            return boilingCoefficient;
        }

        var criticalPressure = Variables.VaporizingSideCriticalPressure.Value.GetValue(PressureUnits.Psia);
        var naturalConvectionCoefficient = Dp09eVaporizationCorrelations.VerticalThermosiphonNaturalConvectionCoefficient(
            criticalPressure,
            Math.Abs(heatFlux));
        Variables.VerticalThermosiphonNaturalConvectionCoefficient.SetValue(
            new HeatTransferCoefficient(naturalConvectionCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        recommendations.Add($"DP09E: Figure A16 natural convection coefficient hnc={naturalConvectionCoefficient:0.##} Btu/hr-ft2-F was added for vertical thermosiphon boiling.");
        return boilingCoefficient + naturalConvectionCoefficient;
    }

    private void ApplyDp09eBundleBoilingCorrection(double singleTubeCoefficient)
    {
        var pitchRatio = Variables.TubePitch.Value.GetValue(DiameterUnits.Inch) /
                         Math.Max(Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch), 1e-12);
        var tubeCount = Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None);
        if (tubeCount <= 0d)
        {
            return;
        }

        var bundleCorrectionFactor = Dp09eVaporizationCorrelations.BundleNucleateBoilingCorrectionFactor(
            tubeCount,
            pitchRatio);
        var plainBundleCoefficient = singleTubeCoefficient * bundleCorrectionFactor;
        Variables.BundleNucleateBoilingCorrectionFactor.SetValue(new UnitLess(bundleCorrectionFactor), VariableDefinedBy.Equipment);
        var finnedTubeCorrectionFactor = TryCalculateDp09eFinnedTubeCorrectionFactor(singleTubeCoefficient, out var correctionFactor)
            ? correctionFactor
            : 1d;
        var bundleCoefficient = plainBundleCoefficient * finnedTubeCorrectionFactor;

        Variables.BundleNucleateBoilingCoefficient.SetValue(
            new HeatTransferCoefficient(bundleCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        recommendations.Add(finnedTubeCorrectionFactor > 1.0001d || finnedTubeCorrectionFactor < 0.9999d
            ? $"DP09E: Figure A9 bundle boiling correction Fb={bundleCorrectionFactor:0.###} and finned-tube correction Ff={finnedTubeCorrectionFactor:0.###} give a preliminary bundle nucleate boiling coefficient of {bundleCoefficient:0.##} Btu/hr-ft2-F."
            : $"DP09E: Figure A9 bundle boiling correction Fb={bundleCorrectionFactor:0.###} gives a preliminary bundle nucleate boiling coefficient of {bundleCoefficient:0.##} Btu/hr-ft2-F.");
    }

    private bool TryCalculateDp09eFinnedTubeCorrectionFactor(double plainSurfaceNucleateBoilingCoefficient, out double correctionFactor)
    {
        correctionFactor = 1d;
        if (Variables.EnhancedHeatTransferType != ShellAndTubeEnhancedHeatTransferType.IntegralFinnedTubes)
        {
            return false;
        }

        if (plainSurfaceNucleateBoilingCoefficient <= 0d ||
            !Variables.HeatFlux.IsDefined ||
            Variables.HeatFlux.Value.GetValue(HeatSurfaceFlowUnits.BTU_hr_ft2) <= 0d)
        {
            recommendations.Add("DP09E: integral finned tube correction requires positive plain-surface boiling coefficient and heat flux.");
            return false;
        }

        var heatFlux = Variables.HeatFlux.Value.GetValue(HeatSurfaceFlowUnits.BTU_hr_ft2);
        var materialConductivity = Dp09cShellAndTubeCatalog.GetTubeMaterialThermalConductivityBtuHrFtF(Variables.TubeMaterial);
        var finEfficiencyFactor = Dp09eVaporizationCorrelations.FinEfficiencyFactor(materialConductivity, heatFlux);
        var vaporizingInlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideInlet.Stream
            : request.TubeSideInlet.Stream;
        var boilingRangeF = Variables.VaporizingSideBoilingRange.IsDefined
            ? Variables.VaporizingSideBoilingRange.Value.GetValue(TemperatureUnits.DegreeFahrenheit)
            : 0d;

        double surfaceFactor;
        if (boilingRangeF > 0d)
        {
            surfaceFactor = Dp09eVaporizationCorrelations.FinnedTubeSurfaceFactorForMixedHydrocarbon(
                boilingRangeF,
                plainSurfaceNucleateBoilingCoefficient);
            recommendations.Add($"DP09E: Figure A11 mixed-hydrocarbon finned-tube surface factor Fs={surfaceFactor:0.###} was estimated from boiling range {boilingRangeF:0.##} F.");
        }
        else if (vaporizingInlet.MolecularWeight.IsDefined &&
                 vaporizingInlet.MolecularWeight.Value.GetValue(UnitLessUnits.None) > 0d)
        {
            var molecularWeight = vaporizingInlet.MolecularWeight.Value.GetValue(UnitLessUnits.None);
            surfaceFactor = Dp09eVaporizationCorrelations.FinnedTubeSurfaceFactorForPureHydrocarbon(
                molecularWeight,
                plainSurfaceNucleateBoilingCoefficient);
            recommendations.Add($"DP09E: Figure A10 pure-hydrocarbon finned-tube surface factor Fs={surfaceFactor:0.###} was estimated from molecular weight {molecularWeight:0.##}.");
        }
        else
        {
            requiredMethodImplementations.Add("DP09E integral-finned tube surface factor basis");
            recommendations.Add("DP09E: integral finned tube correction requires either vaporizing boiling range for Figure A11 or molecular weight for Figure A10.");
            return false;
        }

        correctionFactor = surfaceFactor * finEfficiencyFactor;
        Variables.FinnedTubeSurfaceFactor.SetValue(new UnitLess(surfaceFactor), VariableDefinedBy.Equipment);
        Variables.FinEfficiencyFactor.SetValue(new UnitLess(finEfficiencyFactor), VariableDefinedBy.Equipment);
        Variables.FinnedTubeCorrectionFactor.SetValue(new UnitLess(correctionFactor), VariableDefinedBy.Equipment);
        Variables.FinnedTubeCorrectedBundleBoilingCoefficient.SetValue(
            new HeatTransferCoefficient(
                Variables.BundleNucleateBoilingCorrectionFactor.Value.GetValue(UnitLessUnits.None) *
                plainSurfaceNucleateBoilingCoefficient *
                correctionFactor,
                HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            VariableDefinedBy.Equipment);
        recommendations.Add($"DP09E: Figure A12 fin efficiency Fe={finEfficiencyFactor:0.###} gives finned-tube correction Ff={correctionFactor:0.###}.");
        return true;
    }

    private void TryCalculateDp09eReboilerHydraulicPrecheck(double heatFlux, double vaporizedFraction)
    {
        var vaporizingInlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideInlet.Stream
            : request.TubeSideInlet.Stream;
        var vaporizingOutlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideOutlet.Stream
            : request.TubeSideOutlet.Stream;

        if (!vaporizingInlet.MassFlow.IsDefined ||
            !vaporizingInlet.MassDensity.IsDefined ||
            !vaporizingOutlet.MassDensity.IsDefined)
        {
            return;
        }

        var massFlowLbPerHour = vaporizingInlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var liquidDensityLbFt3 = Math.Max(
            vaporizingInlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3),
            vaporizingOutlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3));
        if (massFlowLbPerHour <= 0d || liquidDensityLbFt3 <= 0d)
        {
            return;
        }

        var targetVelocityFeetPerSecond = Variables.ReboilerType is ShellAndTubeReboilerType.VerticalThermosiphon or ShellAndTubeReboilerType.HorizontalThermosiphon
            ? 4d
            : 5d;
        var volumetricFlowFt3PerSecond = massFlowLbPerHour / liquidDensityLbFt3 / 3600d;
        var recommendedPipeDiameterInches = Math.Sqrt(4d * volumetricFlowFt3PerSecond / Math.PI / targetVelocityFeetPerSecond) * 12d;
        Variables.ReboilerLiquidLineRecommendedDiameter.SetValue(
            new Diameter(recommendedPipeDiameterInches, DiameterUnits.Inch),
            VariableDefinedBy.Equipment);

        if (Variables.ReboilerType != ShellAndTubeReboilerType.VerticalThermosiphon ||
            vaporizedFraction <= 0d ||
            heatFlux <= 0d ||
            !Variables.TubeFlowArea.IsDefined ||
            Variables.TubeFlowArea.Value.GetValue(SurfaceUnits.Foot2) <= 0d)
        {
            recommendations.Add($"DP09E: Figure A18 preliminary reboiler liquid-line diameter is {recommendedPipeDiameterInches:0.##} in at about {targetVelocityFeetPerSecond:0.#} ft/s liquid velocity.");
            return;
        }

        var latentHeatBtuPerPound = Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr) /
                                    Math.Max(massFlowLbPerHour * vaporizedFraction, 1e-12);
        if (latentHeatBtuPerPound <= 0d)
        {
            return;
        }

        var tubeFlowAreaFt2 = Variables.TubeFlowArea.Value.GetValue(SurfaceUnits.Foot2);
        var inletVelocityFeetPerSecond = volumetricFlowFt3PerSecond / Math.Max(tubeFlowAreaFt2, 1e-12);
        var reducedHeatFlux = heatFlux / Math.Max(latentHeatBtuPerPound * liquidDensityLbFt3, 1e-12);
        var outletVaporFractionLimit = Dp09eVaporizationCorrelations.VerticalThermosiphonFigureA17OutletVaporFractionLimit(
            reducedHeatFlux,
            inletVelocityFeetPerSecond);
        if (outletVaporFractionLimit <= 0d)
        {
            return;
        }

        var utilization = vaporizedFraction / Math.Max(outletVaporFractionLimit, 1e-12);
        Variables.VerticalThermosiphonReducedHeatFlux.SetValue(new UnitLess(reducedHeatFlux), VariableDefinedBy.Equipment);
        Variables.VerticalThermosiphonInletVelocity.SetValue(new Velocity(inletVelocityFeetPerSecond, VelocityUnits.FeetPerSecond), VariableDefinedBy.Equipment);
        Variables.VerticalThermosiphonFigureA17OutletVaporFractionLimit.SetValue(new UnitLess(outletVaporFractionLimit), VariableDefinedBy.Equipment);
        Variables.VerticalThermosiphonFigureA17OutletVaporFractionUtilization.SetValue(new UnitLess(utilization), VariableDefinedBy.Equipment);

        if (vaporizedFraction > outletVaporFractionLimit)
        {
            recommendations.Add($"DP09E: vertical thermosiphon outlet vapor fraction is above the preliminary Figure A17 good-operation limit; reduce heat flux, increase circulation, or revise geometry.");
            return;
        }

        recommendations.Add($"DP09E: vertical thermosiphon outlet vapor fraction is within the preliminary Figure A17 good-operation limit.");
    }

    private static bool TryCalculateDp09eVaporToLiquidDensityRatio(
        IFacadeStream vaporizingInlet,
        IFacadeStream vaporizingOutlet,
        out double densityRatio)
    {
        densityRatio = 0d;
        if (!vaporizingInlet.MassDensity.IsDefined || !vaporizingOutlet.MassDensity.IsDefined)
        {
            return false;
        }

        var inletDensity = vaporizingInlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);
        var outletDensity = vaporizingOutlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);
        var vaporDensity = Math.Min(inletDensity, outletDensity);
        var liquidDensity = Math.Max(inletDensity, outletDensity);
        if (vaporDensity <= 0d || liquidDensity <= 0d)
        {
            return false;
        }

        densityRatio = Math.Min(vaporDensity / liquidDensity, 0.2d);
        return true;
    }

    private void AddDp09eHeatFluxLimitReview(double heatFlux)
    {
        if (!Variables.MaximumAllowableHeatFlux.IsDefined ||
            Variables.MaximumAllowableHeatFlux.Value.GetValue(HeatSurfaceFlowUnits.BTU_hr_ft2) <= 0d)
        {
            recommendations.Add("DP09E: enter maximum allowable heat flux from the applicable DP09E boiling/choke-flow figure to activate the design heat-flux limit check.");
            return;
        }

        var maximumAllowableHeatFlux = Variables.MaximumAllowableHeatFlux.Value.GetValue(HeatSurfaceFlowUnits.BTU_hr_ft2);
        var designFactor = Variables.ReboilerType == ShellAndTubeReboilerType.VerticalThermosiphon
            ? 0.60d
            : 0.70d;
        var designLimit = designFactor * maximumAllowableHeatFlux;
        Variables.DesignHeatFluxLimit.SetValue(new HeatSurfaceFlow(designLimit, HeatSurfaceFlowUnits.BTU_hr_ft2), VariableDefinedBy.Equipment);
        var heatFluxUtilization = heatFlux / Math.Max(designLimit, 1e-12);
        Variables.HeatFluxUtilization.SetValue(new UnitLess(heatFluxUtilization), VariableDefinedBy.Equipment);

        if (heatFlux > designLimit)
        {
            recommendations.Add($"DP09E: heat flux utilization is {heatFluxUtilization:0%}; it exceeds the {designFactor:0%} design limit based on maximum allowable heat flux. Increase area or revise service type.");
        }
        else
        {
            recommendations.Add($"DP09E: heat flux utilization is {heatFluxUtilization:0%}; it is within the {designFactor:0%} design limit based on maximum allowable heat flux.");
        }
    }

    private void ApplyDp09eReboilerElevationAndCirculationCriteria()
    {
        switch (Variables.ReboilerType)
        {
            case ShellAndTubeReboilerType.Kettle:
            case ShellAndTubeReboilerType.Internal:
                Variables.ReboilerMinimumTowerElevation.SetValue(new Length(6d, LengthUnits.Foot), VariableDefinedBy.Equipment);
                Variables.ReboilerMaximumTowerElevation.SetValue(new Length(10d, LengthUnits.Foot), VariableDefinedBy.Equipment);
                Variables.PumpThroughRecommendedCirculationRatio.SetValue(new UnitLess(0d), VariableDefinedBy.Equipment);
                recommendations.Add("DP09E: kettle/internal reboiler tower elevation rough guide is 6-10 ft between column liquid level and reboiler reference elevation; final value requires hydraulic balance.");
                break;
            case ShellAndTubeReboilerType.VerticalThermosiphon:
            case ShellAndTubeReboilerType.HorizontalThermosiphon:
                Variables.ReboilerMinimumTowerElevation.SetValue(new Length(8d, LengthUnits.Foot), VariableDefinedBy.Equipment);
                Variables.ReboilerMaximumTowerElevation.SetValue(new Length(20d, LengthUnits.Foot), VariableDefinedBy.Equipment);
                Variables.PumpThroughRecommendedCirculationRatio.SetValue(new UnitLess(0d), VariableDefinedBy.Equipment);
                requiredMethodImplementations.Add("DP09E thermosiphon reboiler hydraulic balance");
                recommendations.Add("DP09E: thermosiphon reboiler tower elevation rough guide is 8-20 ft; final elevation must come from reboiler-circuit hydraulic balance.");
                break;
            case ShellAndTubeReboilerType.PumpThrough:
                Variables.ReboilerMinimumTowerElevation.SetValue(new Length(15d, LengthUnits.Foot), VariableDefinedBy.Equipment);
                Variables.ReboilerMaximumTowerElevation.SetValue(new Length(15d, LengthUnits.Foot), VariableDefinedBy.Equipment);
                Variables.PumpThroughRecommendedCirculationRatio.SetValue(new UnitLess(10d), VariableDefinedBy.Equipment);
                requiredMethodImplementations.Add("DP09E pump-through reboiler circulation and NPSH balance");
                recommendations.Add("DP09E: forced-circulation pump-through reboilers normally use about 15 ft tower elevation for pump NPSH and about 10:1 circulation ratio for heat-sensitive services.");
                break;
        }
    }

    private void AddDp09eReboilerTypeReview(double heatFlux, double vaporizedFraction)
    {
        var vaporizedFractionLimit = SelectDp09eVaporizedFractionLimit();
        if (vaporizedFractionLimit > 0d)
        {
            var vaporizedFractionUtilization = vaporizedFraction / vaporizedFractionLimit;
            Variables.VaporizedFractionLimit.SetValue(new UnitLess(vaporizedFractionLimit), VariableDefinedBy.Equipment);
            Variables.VaporizedFractionUtilization.SetValue(new UnitLess(vaporizedFractionUtilization), VariableDefinedBy.Equipment);
            recommendations.Add($"DP09E: vaporized-fraction utilization is {vaporizedFractionUtilization:0%} against the preliminary {vaporizedFractionLimit:0%} limit for {GetReboilerTypeLabel(Variables.ReboilerType)}.");
        }

        switch (Variables.ReboilerType)
        {
            case ShellAndTubeReboilerType.VerticalThermosiphon:
                if (vaporizedFraction > 0.50d)
                {
                    recommendations.Add("DP09E: vertical thermosiphon vaporization should be limited to 50%; common industrial practice is about 30%.");
                }

                if (heatFlux < 2_000d)
                {
                    recommendations.Add("DP09E: vertical tubeside thermosiphons should maintain at least 2000 Btu/hr-ft2 heat flux for good circulation.");
                }

                break;
            case ShellAndTubeReboilerType.HorizontalThermosiphon:
                var fouling = Variables.AllowedFoulingResistance.Value.GetValue(UnitLessUnits.None);
                var vaporizedLimit = fouling > 0.002d ? 0.25d : 0.50d;
                if (vaporizedFraction > vaporizedLimit)
                {
                    recommendations.Add($"DP09E: horizontal thermosiphon vaporization exceeds the preliminary {vaporizedLimit:0%} limit for the current fouling level.");
                }

                break;
            case ShellAndTubeReboilerType.Kettle:
            case ShellAndTubeReboilerType.Internal:
                if (vaporizedFraction > 0.50d)
                {
                    recommendations.Add("DP09E: high vaporization is better suited to clean kettle/internal services; confirm fouling and disengagement space.");
                }

                break;
            case ShellAndTubeReboilerType.PumpThrough:
                requiredMethodImplementations.Add("DP09E pump-through reboiler circulation and NPSH balance");
                recommendations.Add("DP09E: pump-through reboiler design requires forced-circulation rate and hydraulic balance checks.");
                break;
        }
    }

    private double SelectDp09eVaporizedFractionLimit()
    {
        return Variables.ReboilerType switch
        {
            ShellAndTubeReboilerType.VerticalThermosiphon => 0.50d,
            ShellAndTubeReboilerType.HorizontalThermosiphon =>
                Variables.AllowedFoulingResistance.Value.GetValue(UnitLessUnits.None) > 0.002d ? 0.25d : 0.50d,
            ShellAndTubeReboilerType.Kettle or ShellAndTubeReboilerType.Internal => 0.50d,
            _ => 0d
        };
    }

    private static string GetReboilerTypeLabel(ShellAndTubeReboilerType reboilerType) =>
        reboilerType switch
        {
            ShellAndTubeReboilerType.VerticalThermosiphon => "vertical thermosiphon",
            ShellAndTubeReboilerType.HorizontalThermosiphon => "horizontal thermosiphon",
            ShellAndTubeReboilerType.PumpThrough => "pump-through",
            ShellAndTubeReboilerType.Internal => "internal",
            _ => "kettle"
        };

    private void AddDp09eVaporizingSidePressureDropReview(double vaporizedFraction)
    {
        var pressureDropPsi = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? Variables.ShellSidePressureDrop.Value.GetValue(PressureDropUnits.psi)
            : Variables.TubeSidePressureDrop.Value.GetValue(PressureDropUnits.psi);
        Variables.VaporizingSidePressureDrop.SetValue(new PressureDrop(pressureDropPsi, PressureDropUnits.psi), VariableDefinedBy.Equipment);
        TryCalculateDp09ePreliminaryReboilerHydraulicBalance(pressureDropPsi);

        recommendations.Add($"DP09E: vaporizing-side exchanger pressure drop is {pressureDropPsi:0.###} psi; final reboiler design must include inlet/outlet piping, static head, and two-phase hydraulic balance.");

        if (Variables.ReboilerType is ShellAndTubeReboilerType.VerticalThermosiphon or ShellAndTubeReboilerType.HorizontalThermosiphon)
        {
            requiredMethodImplementations.Add("DP09E thermosiphon reboiler hydraulic balance");
            recommendations.Add("DP09E: thermosiphon circulation is not proven by exchanger pressure drop alone; available head must exceed exchanger, inlet-line, outlet-line, and distributor losses.");
        }

        if (Variables.ReboilerType == ShellAndTubeReboilerType.PumpThrough)
        {
            requiredMethodImplementations.Add("DP09E pump-through reboiler circulation and NPSH balance");
            recommendations.Add("DP09E: pump-through reboiler pressure drop should be checked against pump head, NPSH, and the required circulation ratio.");
        }

        if (vaporizedFraction > 0.50d && Variables.ReboilerType != ShellAndTubeReboilerType.PumpThrough)
        {
            recommendations.Add("DP09E: high vaporized fraction makes two-phase pressure drop and flow-regime checks controlling for natural-circulation reboilers.");
        }
    }

    private void TryCalculateDp09ePreliminaryReboilerHydraulicBalance(double exchangerPressureDropPsi)
    {
        if (Variables.ReboilerType is not (ShellAndTubeReboilerType.VerticalThermosiphon or ShellAndTubeReboilerType.HorizontalThermosiphon) ||
            exchangerPressureDropPsi <= 0d)
        {
            return;
        }

        var vaporizingInlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideInlet.Stream
            : request.TubeSideInlet.Stream;
        var vaporizingOutlet = processRegime == DesignPracticesProcessRegime.ShellSideVaporization
            ? request.ShellSideOutlet.Stream
            : request.TubeSideOutlet.Stream;

        if (!vaporizingInlet.MassDensity.IsDefined || !vaporizingOutlet.MassDensity.IsDefined)
        {
            recommendations.Add("DP09E: preliminary thermosiphon static-head balance requires vaporizing-side liquid density.");
            return;
        }

        var liquidDensityLbFt3 = Math.Max(
            vaporizingInlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3),
            vaporizingOutlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3));
        if (liquidDensityLbFt3 <= 0d)
        {
            return;
        }

        var vaporDensityLbFt3 = Math.Min(
            vaporizingInlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3),
            vaporizingOutlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3));
        var explicitCircuitDropPsi = TryCalculateDp09eReboilerCircuitLinePressureDrops(
            vaporizingInlet,
            liquidDensityLbFt3,
            vaporDensityLbFt3,
            out var liquidLineDropPsi,
            out var vaporLineDropPsi);
        const double preliminaryCircuitMargin = 1.35d;
        var requiredStaticHeadPsi = explicitCircuitDropPsi is > 0d
            ? exchangerPressureDropPsi + explicitCircuitDropPsi.Value
            : exchangerPressureDropPsi * preliminaryCircuitMargin;
        var requiredTowerElevationFeet = requiredStaticHeadPsi * 144d / Math.Max(liquidDensityLbFt3, 1e-12);

        Variables.ReboilerRequiredStaticHead.SetValue(
            new PressureDrop(requiredStaticHeadPsi, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
        Variables.ReboilerRequiredTowerElevation.SetValue(
            new Length(requiredTowerElevationFeet, LengthUnits.Foot),
            VariableDefinedBy.Equipment);

        if (explicitCircuitDropPsi is > 0d)
        {
            recommendations.Add($"DP09E: preliminary thermosiphon circuit balance requires about {requiredTowerElevationFeet:0.##} ft tower elevation using exchanger drop {exchangerPressureDropPsi:0.###} psi, liquid-line drop {liquidLineDropPsi:0.###} psi, and vapor-line drop {vaporLineDropPsi:0.###} psi.");
        }
        else
        {
            recommendations.Add($"DP09E: preliminary thermosiphon static-head balance requires about {requiredTowerElevationFeet:0.##} ft tower elevation using exchanger pressure drop plus a {preliminaryCircuitMargin:0.##} circuit allowance.");
        }

        if (!Variables.ReboilerAvailableTowerElevation.IsDefined ||
            Variables.ReboilerAvailableTowerElevation.Value.GetValue(LengthUnits.Foot) <= 0d)
        {
            recommendations.Add("DP09E: enter available reboiler tower elevation to compare static head against the preliminary thermosiphon requirement.");
            return;
        }

        var availableTowerElevationFeet = Variables.ReboilerAvailableTowerElevation.Value.GetValue(LengthUnits.Foot);
        var availableStaticHeadPsi = liquidDensityLbFt3 * availableTowerElevationFeet / 144d;
        var staticHeadMargin = (availableStaticHeadPsi - requiredStaticHeadPsi) / Math.Max(requiredStaticHeadPsi, 1e-12);

        Variables.ReboilerAvailableStaticHead.SetValue(
            new PressureDrop(availableStaticHeadPsi, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
        Variables.ReboilerStaticHeadMargin.SetValue(new UnitLess(staticHeadMargin), VariableDefinedBy.Equipment);

        if (staticHeadMargin < 0d)
        {
            recommendations.Add($"DP09E: available thermosiphon static head is short by {Math.Abs(staticHeadMargin):0%}; increase elevation, reduce pressure drop, or revise circulation geometry.");
            return;
        }

        recommendations.Add($"DP09E: available thermosiphon static head has a preliminary {staticHeadMargin:0%} margin over the required circuit static head.");
    }

    private double? TryCalculateDp09eReboilerCircuitLinePressureDrops(
        IFacadeStream vaporizingInlet,
        double liquidDensityLbFt3,
        double vaporDensityLbFt3,
        out double liquidLineDropPsi,
        out double vaporLineDropPsi)
    {
        liquidLineDropPsi = 0d;
        vaporLineDropPsi = 0d;
        if (!vaporizingInlet.MassFlow.IsDefined ||
            liquidDensityLbFt3 <= 0d ||
            vaporDensityLbFt3 <= 0d)
        {
            return null;
        }

        var massFlowLbPerHour = vaporizingInlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        if (massFlowLbPerHour <= 0d)
        {
            return null;
        }

        liquidLineDropPsi = CalculateDp09eLinePressureDropPsi(
            massFlowLbPerHour,
            liquidDensityLbFt3,
            Variables.ReboilerLiquidLineDiameter.Value.GetValue(DiameterUnits.Inch),
            Variables.ReboilerLiquidLineResistanceCoefficient.Value.GetValue(UnitLessUnits.None));
        vaporLineDropPsi = CalculateDp09eLinePressureDropPsi(
            massFlowLbPerHour * Math.Max(Variables.VaporizedFraction.Value.GetValue(UnitLessUnits.None), 0.05d),
            vaporDensityLbFt3,
            Variables.ReboilerVaporLineDiameter.Value.GetValue(DiameterUnits.Inch),
            Variables.ReboilerVaporLineResistanceCoefficient.Value.GetValue(UnitLessUnits.None));

        if (liquidLineDropPsi <= 0d && vaporLineDropPsi <= 0d)
        {
            return null;
        }

        var circuitDropPsi = liquidLineDropPsi + vaporLineDropPsi;
        Variables.ReboilerLiquidLinePressureDrop.SetValue(
            new PressureDrop(liquidLineDropPsi, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
        Variables.ReboilerVaporLinePressureDrop.SetValue(
            new PressureDrop(vaporLineDropPsi, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
        Variables.ReboilerCircuitPressureDrop.SetValue(
            new PressureDrop(circuitDropPsi, PressureDropUnits.psi),
            VariableDefinedBy.Equipment);
        return circuitDropPsi;
    }

    private static double CalculateDp09eLinePressureDropPsi(
        double massFlowLbPerHour,
        double densityLbFt3,
        double lineDiameterInches,
        double resistanceCoefficient)
    {
        if (massFlowLbPerHour <= 0d ||
            densityLbFt3 <= 0d ||
            lineDiameterInches <= 0d ||
            resistanceCoefficient <= 0d)
        {
            return 0d;
        }

        var diameterFeet = lineDiameterInches / 12d;
        var areaFt2 = Math.PI * diameterFeet * diameterFeet / 4d;
        var volumetricFlowFt3Second = massFlowLbPerHour / densityLbFt3 / 3600d;
        var velocityFeetSecond = volumetricFlowFt3Second / Math.Max(areaFt2, 1e-12);
        return resistanceCoefficient * densityLbFt3 * velocityFeetSecond * velocityFeetSecond / (2d * 32.174d * 144d);
    }

    private void AddDp09dShortTubeLaminarReview(double reynolds, double lengthToInsideDiameter)
    {
        if (reynolds > 2_000d || lengthToInsideDiameter >= 60d)
        {
            return;
        }

        var correctionFactor = Dp09dShellAndTubeCorrelations.ShortTubeCorrectionFactor(reynolds, lengthToInsideDiameter);
        recommendations.Add($"DP09D: Figure 1.6 short-tube laminar correction epsilon is {correctionFactor:0.###}; complete laminar tube-side coefficient review is required.");
    }

    private void AddDp09dTubePressureDropCorrectionReview(
        double isothermalFriction,
        double viscosityGradientCorrection,
        double naturalConvectionPressureDropCorrection,
        double tubeWallTemperatureF,
        double bulkToWallViscosityRatio)
    {
        recommendations.Add($"DP09D: tube-side pressure drop uses Figure 1.8 isothermal friction {isothermalFriction:0.#####}, Figure 1.9 viscosity-gradient factor {viscosityGradientCorrection:0.###}, and Figure 1.10 natural-convection pressure-drop factor {naturalConvectionPressureDropCorrection:0.###}.");
        recommendations.Add($"DP09D: Figure 1.9 viscosity-gradient factor uses estimated tube-wall temperature {tubeWallTemperatureF:0.##} °F and bulk/wall viscosity ratio {bulkToWallViscosityRatio:0.###}.");
    }

    private void AddDp09cThermalExpansionReview()
    {
        var shellAverageTemperature = (ReadTemperatureF(request.ShellSideInlet.Stream) + ReadTemperatureF(request.ShellSideOutlet.Stream)) / 2d;
        var tubeAverageTemperature = (ReadTemperatureF(request.TubeSideInlet.Stream) + ReadTemperatureF(request.TubeSideOutlet.Stream)) / 2d;
        var averageTemperatureDifference = Math.Abs(shellAverageTemperature - tubeAverageTemperature);

        if (Variables.TubeConstruction == ShellAndTubeTubeConstruction.Straight && averageTemperatureDifference > 150d)
        {
            recommendations.Add("DP09C: fixed tubesheet construction needs thermal expansion review when the shell/tube average temperature difference is large.");
        }
    }

    private DesignPracticesOverallCoefficientRange GetDp09InitialOverallCoefficientRange()
    {
        var shellAverageTemperature = (ReadTemperatureF(request.ShellSideInlet.Stream) + ReadTemperatureF(request.ShellSideOutlet.Stream)) / 2d;
        var tubeAverageTemperature = (ReadTemperatureF(request.TubeSideInlet.Stream) + ReadTemperatureF(request.TubeSideOutlet.Stream)) / 2d;
        var shellSideIsHotter = shellAverageTemperature >= tubeAverageTemperature;
        var cooledService = shellSideIsHotter
            ? DesignPracticesServiceClassifier.Classify(request.ShellSideInlet.Stream, request.ShellSideOutlet.Stream)
            : DesignPracticesServiceClassifier.Classify(request.TubeSideInlet.Stream, request.TubeSideOutlet.Stream);
        var heatedService = shellSideIsHotter
            ? DesignPracticesServiceClassifier.Classify(request.TubeSideInlet.Stream, request.TubeSideOutlet.Stream)
            : DesignPracticesServiceClassifier.Classify(request.ShellSideInlet.Stream, request.ShellSideOutlet.Stream);
        var cooledFluidName = shellSideIsHotter
            ? $"{request.ShellSideInlet.Stream.Name} {request.ShellSideOutlet.Stream.Name}"
            : $"{request.TubeSideInlet.Stream.Name} {request.TubeSideOutlet.Stream.Name}";
        var heatedFluidName = shellSideIsHotter
            ? $"{request.TubeSideInlet.Stream.Name} {request.TubeSideOutlet.Stream.Name}"
            : $"{request.ShellSideInlet.Stream.Name} {request.ShellSideOutlet.Stream.Name}";

        return Dp09bHeatExchangerCatalog.GetTypicalShellAndTubeOverallCoefficientRange(
            processRegime,
            cooledService,
            heatedService,
            cooledFluidName,
            heatedFluidName);
    }

    private double CalculateDp09EquivalentDiameterFeet()
    {
        var pitchFeet = Variables.TubePitch.Value.GetValue(DiameterUnits.Inch) / 12d;
        var tubeOdFeet = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch) / 12d;

        if (Variables.TubeLayout == ShellAndTubeTubeLayout.Square)
        {
            return 4d * (pitchFeet * pitchFeet - Math.PI * tubeOdFeet * tubeOdFeet / 4d) / (Math.PI * tubeOdFeet);
        }

        return 4d * (0.5d * pitchFeet * 0.8660254037844386d * pitchFeet - Math.PI * tubeOdFeet * tubeOdFeet / 8d) /
               (Math.PI * tubeOdFeet / 2d);
    }

    private double CalculateDp09ShellSideCoefficient(double reynolds, double prandtl, double thermalConductivity, double equivalentDiameterFeet)
    {
        var j = Dp09dShellAndTubeCorrelations.DP_j_ShellSideHeatTransferFactor(
            reynolds,
            Variables.TubeLayout);

        return j * Math.Max(reynolds, 1d) * Math.Pow(Math.Max(prandtl, 1e-6), 1d / 3d) *
               thermalConductivity / Math.Max(equivalentDiameterFeet, 1e-12);
    }

    private DesignPracticesShellSideCrossflowFractions CalculateDp09dShellSideCrossflowFractions(double DP_Rext_TotalFlowReynoldsNumber)
    {
        var DP_DOTL_TubeBundleDiameter = CalculateDp09cOuterTubeLimit(Variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch));
        var partitionRatio = EstimateDp09dPassPartitionRatio();
        var nominalFraction = Dp09dShellAndTubeCorrelations.NormalCrossflowFraction(
            DP_DOTL_TubeBundleDiameter,
            partitionRatio,
            Variables.RearHeadType);
        var DP_LBCC_BaffleSpacing = Variables.BaffleSpacing.Value.GetValue(LengthUnits.Inch);
        var DP_SC_BaffleSpacingCorrection = Dp09dShellAndTubeCorrelations.DP_SC_BaffleSpacingCorrection(
            DP_LBCC_BaffleSpacing,
            DP_DOTL_TubeBundleDiameter);
        var DP_RC_ReynoldsNumberCorrection =
            Dp09dShellAndTubeCorrelations.DP_RC_ReynoldsNumberCorrection(DP_Rext_TotalFlowReynoldsNumber);
        var pressureDropFraction = Math.Min(nominalFraction * DP_SC_BaffleSpacingCorrection * DP_RC_ReynoldsNumberCorrection, 1d);
        var heatTransferFraction = Math.Min(pressureDropFraction + 0.125d, 1d);

        recommendations.Add($"DP09D: shell-side Figure 1.1 normal crossflow fraction is {nominalFraction:0.###}; heat-transfer fraction is {heatTransferFraction:0.###}.");

        if (Variables.TubeConstruction == ShellAndTubeTubeConstruction.UTube &&
            Variables.RearHeadType != Dp09dRearHeadType.PullThroughFloatingHead)
        {
            recommendations.Add("DP09C/DP09D: U-tube construction with a non pull-through rear-head curve should be reviewed for TEMA consistency.");
        }

        return new DesignPracticesShellSideCrossflowFractions(
            nominalFraction,
            pressureDropFraction,
            heatTransferFraction);
    }

    private double EstimateDp09dPassPartitionRatio()
    {
        var tubePasses = Variables.TubePasses.Value.GetValue(UnitLessUnits.None);
        return tubePasses switch
        {
            <= 1d => 0.200d,
            <= 2d => 0.225d,
            <= 4d => 0.250d,
            _ => 0.275d
        };
    }

    private void ApplyDp09cRearHeadDefault()
    {
        if (Variables.TubeConstruction == ShellAndTubeTubeConstruction.UTube &&
            Variables.RearHeadType == Dp09dRearHeadType.FixedTubesheet)
        {
            Variables.RearHeadType = Dp09dRearHeadType.PullThroughFloatingHead;
        }

    }

    private double CalculateDp09fCondensingCoefficient(double sensibleShellCoefficient, double shellFreeAreaSquareFeet)
    {
        var actualTubeCount = Variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None);
        var condensateStreams = Dp09fCondensationZoneModel.CalculateHorizontalBundleCondensateStreams(actualTubeCount, Variables.TubeLayout);
        var tubeLength = Variables.TubeLength.Value.GetValue(LengthUnits.Foot);
        var properties = ReadAverageProperties(request.ShellSideInlet.Stream, request.ShellSideOutlet.Stream, "shell side condensate");
        var vaporFreeAreaFraction = CalculateDp09fCondensingVaporFreeAreaFraction();
        var vaporMassFlow = CalculateDp09fAverageVaporMassFlowLbHr(request.ShellSideInlet.Stream, request.ShellSideOutlet.Stream);
        var applyVaporMassVelocityCorrection = !IsPureCondensingComponent(
            request.ShellSideInlet.Stream,
            request.ShellSideOutlet.Stream);

        if (processRegime == DesignPracticesProcessRegime.ShellSideCondensation)
        {
            var vaporCoolingCoefficient = sensibleShellCoefficient;
            var bottomFlowLiquidCoolingCoefficient = sensibleShellCoefficient;
            var iterationResult = Dp09fCondensationZoneModel.IterateHorizontalBundleCondensingArea(
                condensationZones,
                Variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr),
                Variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2),
                shellFreeAreaSquareFeet,
                vaporFreeAreaFraction,
                vaporMassFlow,
                request.ShellSideInlet.Stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr),
                tubeLength,
                condensateStreams,
                properties,
                vaporCoolingCoefficient,
                bottomFlowLiquidCoolingCoefficient,
                applyVaporMassVelocityCorrection);

            Variables.CondensingZoneHeatTransferCoefficient.SetValue(
                new HeatTransferCoefficient(iterationResult.CondensingCoefficientBtuHrFt2F, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
                VariableDefinedBy.Equipment);
            Variables.VaporCoolingZoneHeatTransferCoefficient.SetValue(
                new HeatTransferCoefficient(vaporCoolingCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
                VariableDefinedBy.Equipment);
            Variables.LiquidCoolingZoneHeatTransferCoefficient.SetValue(
                new HeatTransferCoefficient(iterationResult.LiquidCoolingCoefficientBtuHrFt2F, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
                VariableDefinedBy.Equipment);
            Variables.BottomFlowLiquidCoolingHeatTransferCoefficient.SetValue(
                new HeatTransferCoefficient(bottomFlowLiquidCoolingCoefficient, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
                VariableDefinedBy.Equipment);
            Variables.DripCoolingHeatTransferCoefficient.SetValue(
                new HeatTransferCoefficient(iterationResult.DripCoolingCoefficientBtuHrFt2F, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
                VariableDefinedBy.Equipment);
            Variables.CondensingVaporFreeAreaFraction.SetValue(new UnitLess(iterationResult.VaporFreeAreaFraction), VariableDefinedBy.Equipment);
            Variables.CondensingVaporMassVelocity.SetValue(new UnitLess(iterationResult.VaporMassVelocityLbSecFt2), VariableDefinedBy.Equipment);
            Variables.CondensingAreaIterationCount.SetValue(new UnitLess(iterationResult.Iterations), VariableDefinedBy.Equipment);
            Variables.CondensingIteratedRequiredArea.SetValue(new Area(iterationResult.RequiredAreaSquareFeet, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
            Variables.DutyWeightedCondensingSideHeatTransferCoefficient.SetValue(
                new HeatTransferCoefficient(iterationResult.DutyWeightedCoefficientBtuHrFt2F, HeatTransferCoefficientUnits.BTU_hr_ft2_F),
                VariableDefinedBy.Equipment);
            Variables.RequiredArea.SetValue(new Area(iterationResult.RequiredAreaSquareFeet, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);

            recommendations.Add($"DP09F: shell-side condensation area iteration converged in {iterationResult.Iterations} pass(es), with required area {iterationResult.RequiredAreaSquareFeet:0.##} ft2 and duty-weighted zone coefficient {iterationResult.DutyWeightedCoefficientBtuHrFt2F:0.##} Btu/hr-ft2-F.");
            recommendations.Add(applyVaporMassVelocityCorrection
                ? $"DP09F: vapor velocity correction uses iterated vapor free-area fraction {iterationResult.VaporFreeAreaFraction:0.###} and vapor mass velocity {iterationResult.VaporMassVelocityLbSecFt2:0.###} lb/s-ft2."
                : $"DP09F: vapor mass-velocity correction is not applied because the service is detected as pure-component condensation; iterated vapor free-area fraction is {iterationResult.VaporFreeAreaFraction:0.###} and vapor mass velocity is {iterationResult.VaporMassVelocityLbSecFt2:0.###} lb/s-ft2.");
            recommendations.Add($"DP09F: liquid cooling uses a preliminary 50/50 bottom-flow/drip split; drip cooling coefficient is 1.5 times condensing coefficient ({iterationResult.DripCoolingCoefficientBtuHrFt2F:0.##} Btu/hr-ft2-F).");
            recommendations.Add("DP09F: vapor cooling and bottom-flow liquid cooling coefficients currently use the sensible shell-side coefficient until independent zone properties are available.");

            return iterationResult.DutyWeightedCoefficientBtuHrFt2F;
        }

        return sensibleShellCoefficient;
    }

    private double CalculateDp09fCondensingVaporFreeAreaFraction()
    {
        var inletVaporFraction = request.ShellSideInlet.Stream.VaporFraction.IsDefined
            ? request.ShellSideInlet.Stream.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : 1d;
        var outletVaporFraction = request.ShellSideOutlet.Stream.VaporFraction.IsDefined
            ? request.ShellSideOutlet.Stream.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : inletVaporFraction;
        var vaporDensity = ReadDensityOrVolumetricFlowFallback(request.ShellSideInlet.Stream);
        var liquidDensity = ReadDensityOrVolumetricFlowFallback(request.ShellSideOutlet.Stream);

        return Dp09fCondensationZoneModel.CalculateCondensingZoneVaporFreeAreaFraction(
            condensationZones,
            inletVaporFraction,
            outletVaporFraction,
            vaporDensity,
            liquidDensity);
    }

    private static double CalculateDp09fAverageVaporMassFlowLbHr(IFacadeStream inlet, IFacadeStream outlet)
    {
        var inletMassFlow = inlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var outletMassFlow = outlet.MassFlow.IsDefined
            ? outlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr)
            : inletMassFlow;
        var inletVaporFraction = inlet.VaporFraction.IsDefined
            ? inlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : 1d;
        var outletVaporFraction = outlet.VaporFraction.IsDefined
            ? outlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : inletVaporFraction;

        return ((inletMassFlow * inletVaporFraction) + (outletMassFlow * outletVaporFraction)) / 2d;
    }

    private double CalculateDp09fZoneWeightedPressureDrop(
        double basePressureDropPsi,
        IFacadeStream condensingInlet,
        IFacadeStream condensingOutlet,
        double baseDensityLbFt3)
    {
        if (condensationZones.Count == 0)
        {
            return basePressureDropPsi;
        }

        var vaporDensity = ReadDensityOrVolumetricFlowFallback(condensingInlet);
        var liquidDensity = ReadDensityOrVolumetricFlowFallback(condensingOutlet);
        var inletMassFlow = condensingInlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var outletMassFlow = condensingOutlet.MassFlow.IsDefined
            ? condensingOutlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr)
            : inletMassFlow;
        var averageMassFlow = (inletMassFlow + outletMassFlow) / 2d;
        var inletVaporFraction = condensingInlet.VaporFraction.IsDefined
            ? condensingInlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : 1d;
        var outletVaporFraction = condensingOutlet.VaporFraction.IsDefined
            ? condensingOutlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage) / 100d
            : 0d;
        var assumedU = Variables.AssumedDirtyOverallCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F);
        var zoneAreas = Dp09fCondensationZoneModel.CalculateZoneAreas(condensationZones, assumedU);
        var zoneDensities = Dp09fCondensationZoneModel.CalculateZoneAverageDensities(
            condensationZones,
            averageMassFlow,
            inletVaporFraction,
            outletVaporFraction,
            vaporDensity,
            liquidDensity);
        var zoneDensityCorrectionFactors = zoneDensities
            .Select(zoneDensity => Math.Clamp(baseDensityLbFt3 / Math.Max(zoneDensity, 1e-12), 0.10d, 10d))
            .ToArray();
        var zonePressureDropContributions = Dp09fCondensationZoneModel.CalculatePreliminaryZonePressureDropContributions(
            condensationZones,
            zoneAreas,
            basePressureDropPsi,
            zoneDensityCorrectionFactors);
        var areaWeightedZoneDensity = zoneDensities
            .Zip(zoneAreas, (density, zoneArea) => density * zoneArea.AreaFraction)
            .Sum();
        var densityCorrectionFactor = zoneDensityCorrectionFactors
            .Zip(zoneAreas, (correctionFactor, zoneArea) => correctionFactor * zoneArea.AreaFraction)
            .Sum();

        Variables.CondensingTwoPhaseAverageDensity.SetValue(new UnitLess(areaWeightedZoneDensity), VariableDefinedBy.Equipment);
        Variables.CondensingPressureDropDensityCorrectionFactor.SetValue(new UnitLess(densityCorrectionFactor), VariableDefinedBy.Equipment);
        return zonePressureDropContributions.Sum();
    }

    private static double CalculateDp09fAverageTwoPhaseDensityLbFt3(IFacadeStream inlet, IFacadeStream outlet)
    {
        var inletMassFlow = inlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
        var outletMassFlow = outlet.MassFlow.IsDefined
            ? outlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr)
            : inletMassFlow;
        var averageMassFlow = (inletMassFlow + outletMassFlow) / 2d;
        var inletVolumetricFlow = ReadVolumetricFlowOrMassDensityFallback(inlet);
        var outletVolumetricFlow = ReadVolumetricFlowOrMassDensityFallback(outlet);

        return 2d * averageMassFlow /
               Math.Max((inletVolumetricFlow + outletVolumetricFlow) * 3600d, 1e-12);
    }

    private static double ReadVolumetricFlowOrMassDensityFallback(IFacadeStream stream)
    {
        if (stream.VolumetricFlow.IsDefined)
        {
            return stream.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg);
        }

        EnsureDefined(stream.MassFlow.IsDefined, stream.Name, "mass flow");
        EnsureDefined(stream.MassDensity.IsDefined, stream.Name, "mass density");

        return stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr) /
               Math.Max(stream.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3), 1e-12) /
               3600d;
    }

    private static double ReadDensityOrVolumetricFlowFallback(IFacadeStream stream)
    {
        if (stream.MassDensity.IsDefined)
        {
            return stream.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3);
        }

        EnsureDefined(stream.MassFlow.IsDefined, stream.Name, "mass flow");
        EnsureDefined(stream.VolumetricFlow.IsDefined, stream.Name, "volumetric flow");

        return stream.MassFlow.Value.GetValue(MassFlowUnits.lb_hr) /
               Math.Max(stream.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg) * 3600d, 1e-12);
    }

    private double CalculateDp09cTubeWallResistance()
    {
        var tubeOd = Variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch);
        var tubeId = Variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch);
        var metalConductivity = Dp09cShellAndTubeCatalog.GetTubeMaterialThermalConductivityBtuHrFtF(Variables.TubeMaterial);

        recommendations.Add($"DP09C: tube wall resistance uses {Variables.TubeMaterial} thermal conductivity from DP09C Table 2.");

        return tubeOd * Math.Log(tubeOd / tubeId) / (2d * metalConductivity * 12d);
    }

    private static double GetDp09cTubePassCorrection(int tubePasses)
    {
        return tubePasses switch
        {
            <= 1 => 0d,
            2 => 1d,
            4 => 2.9d,
            6 => 3.5d,
            8 => 4.3d,
            10 => 5.3d,
            12 => 6.2d,
            14 => 6.9d,
            _ => 7.5d
        };
    }

    private static string GetTemaShellTypeLetter(ShellAndTubeShellType shellType)
    {
        return shellType switch
        {
            ShellAndTubeShellType.TwoPass => "F",
            ShellAndTubeShellType.SplitFlow => "G",
            ShellAndTubeShellType.DoubleSplitFlow => "H",
            ShellAndTubeShellType.DividedFlow => "J",
            ShellAndTubeShellType.CrossFlow => "X",
            _ => "E"
        };
    }

    private static string GetBaffleTypeLabel(ShellAndTubeBaffleType baffleType)
    {
        return baffleType switch
        {
            ShellAndTubeBaffleType.DoubleSegmental => "double-segmental",
            ShellAndTubeBaffleType.RodBaffle => "rod-baffle",
            ShellAndTubeBaffleType.HelicalBaffle => "helical",
            ShellAndTubeBaffleType.NoTubesInWindow => "no-tubes-in-window",
            _ => "single-segmental"
        };
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

        heatDutyBtuPerHour = Math.Abs(
            outlet.Stream.EnthalpyFlow.Value.GetValue(EnergyFlowUnits.BTUhr) -
            inlet.Stream.EnthalpyFlow.Value.GetValue(EnergyFlowUnits.BTUhr));

        return true;
    }

    private static double ReadTemperatureF(IFacadeStream stream)
    {
        if (!stream.Temperature.IsDefined)
        {
            throw new InvalidOperationException("Cannot calculate Design Practices LMTD because stream temperature is not defined.");
        }

        try
        {
            return stream.Temperature.Value.GetValue(TemperatureUnits.DegreeFahrenheit);
        }
        catch (UnitConversionException)
        {
            var temperature = stream.Temperature.Value;
            if (temperature.Unit == TemperatureUnits.Kelvin)
            {
                return (temperature.Value - 273.15d) * 9d / 5d + 32d;
            }

            if (temperature.Unit == TemperatureUnits.DegreeCelcius)
            {
                return temperature.Value * 9d / 5d + 32d;
            }

            throw;
        }
    }

    private static double CalculateLmtd(double terminalDifferenceA, double terminalDifferenceB)
    {
        if (terminalDifferenceA <= 0d || terminalDifferenceB <= 0d)
        {
            throw new InvalidOperationException("Cannot calculate Design Practices LMTD because terminal temperature differences must be positive.");
        }

        return Math.Abs(terminalDifferenceA - terminalDifferenceB) < 1e-9
            ? terminalDifferenceA
            : (terminalDifferenceA - terminalDifferenceB) / Math.Log(terminalDifferenceA / terminalDifferenceB);
    }

    private static bool TryReadAverageVolumetricFlow(IFacadeStream inlet, IFacadeStream outlet, out double flowFt3s)
    {
        flowFt3s = 0d;

        if (!inlet.VolumetricFlow.IsDefined || !outlet.VolumetricFlow.IsDefined)
        {
            return false;
        }

        flowFt3s = (inlet.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg) +
                    outlet.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.ft3_sg)) / 2d;
        return true;
    }

    private static double ReadAverageVolumetricFlowOrMassDensityFallback(
        IFacadeStream inlet,
        IFacadeStream outlet,
        DesignPracticesFluidProperties properties)
    {
        if (TryReadAverageVolumetricFlow(inlet, outlet, out var flowFt3s))
        {
            return flowFt3s;
        }

        return inlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr) / Math.Max(properties.DensityLbFt3, 1e-12) / 3600d;
    }

    private static double SelectPreliminaryNozzleDiameterInches(double flowFt3PerSecond, double targetVelocityFeetPerSecond)
    {
        var requiredDiameterInches = Math.Sqrt(4d * flowFt3PerSecond / (Math.PI * Math.Max(targetVelocityFeetPerSecond, 1e-12))) * 12d;
        return Math.Ceiling(Math.Max(requiredDiameterInches, 0.5d) * 2d) / 2d;
    }

    private bool IsShellSideGasOrCondensingVapor()
    {
        return processRegime == DesignPracticesProcessRegime.ShellSideCondensation ||
               IsGasOrVaporService(request.ShellSideInlet.Stream, request.ShellSideOutlet.Stream);
    }

    private static double SelectDp09bPressureDropAllowancePsi(IFacadeStream inlet, IFacadeStream outlet)
    {
        if (!IsGasOrVaporService(inlet, outlet))
        {
            return 17.5d;
        }

        var averagePressurePsia = GetAveragePressurePsia(inlet, outlet);
        return averagePressurePsia switch
        {
            < 14.7d => 0.5d,
            <= 25d => 1.25d,
            <= 100d => 3.5d,
            _ => 7.5d
        };
    }

    private double GetTubeSideAllowedPressureDropPsi() =>
        GetAllowedPressureDropPsi(Variables.TubeSideAllowedPressureDrop);

    private double GetShellSideAllowedPressureDropPsi() =>
        GetAllowedPressureDropPsi(Variables.ShellSideAllowedPressureDrop);

    private static double GetAllowedPressureDropPsi(Variable<PressureDrop> allowedPressureDrop)
    {
        return allowedPressureDrop.IsDefined && allowedPressureDrop.Value.GetValue(PressureDropUnits.psi) > 0d
            ? allowedPressureDrop.Value.GetValue(PressureDropUnits.psi)
            : 5d;
    }

    private static bool IsGasOrVaporService(IFacadeStream inlet, IFacadeStream outlet)
    {
        var inletVaporFraction = inlet.VaporFraction.IsDefined
            ? inlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage)
            : 0d;
        var outletVaporFraction = outlet.VaporFraction.IsDefined
            ? outlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage)
            : 0d;

        return Math.Max(inletVaporFraction, outletVaporFraction) > 50d ||
               (inlet.MassDensity.IsDefined && inlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3) < 5d);
    }

    private static double GetAveragePressurePsia(IFacadeStream inlet, IFacadeStream outlet)
    {
        var pressures = new[] { inlet.Pressure, outlet.Pressure }
            .Where(pressure => pressure.IsDefined)
            .Select(pressure => pressure.Value.GetValue(PressureUnits.Psia))
            .ToArray();

        return pressures.Length == 0 ? 14.7d : pressures.Average();
    }

    private static DesignPracticesFluidProperties ReadAverageProperties(IFacadeStream inlet, IFacadeStream outlet, string sideName)
    {
        EnsureDefined(inlet.Viscosity.IsDefined && outlet.Viscosity.IsDefined, sideName, "viscosity");
        EnsureDefined(inlet.MassCp.IsDefined && outlet.MassCp.IsDefined, sideName, "mass heat capacity");
        EnsureDefined(inlet.ThermalConductivity.IsDefined && outlet.ThermalConductivity.IsDefined, sideName, "thermal conductivity");
        EnsureDefined(inlet.MassDensity.IsDefined && outlet.MassDensity.IsDefined, sideName, "mass density");
        EnsureDefined(inlet.MassFlow.IsDefined, sideName, "mass flow");

        return new DesignPracticesFluidProperties(
            (inlet.Viscosity.Value.GetValue(ViscosityUnits.lb_ft_hr) + outlet.Viscosity.Value.GetValue(ViscosityUnits.lb_ft_hr)) / 2d,
            (inlet.MassCp.Value.GetValue(MassEntropyUnits.BTU_lb_F) + outlet.MassCp.Value.GetValue(MassEntropyUnits.BTU_lb_F)) / 2d,
            (inlet.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.BTU_ft_hr_ft2_m_F) + outlet.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.BTU_ft_hr_ft2_m_F)) / 2d,
            (inlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3) + outlet.MassDensity.Value.GetValue(MassDensityUnits.lb_ft3)) / 2d);
    }

    private static void EnsureDefined(bool isDefined, string sideName, string propertyName)
    {
        if (!isDefined)
        {
            throw new InvalidOperationException($"Cannot calculate Design Practices design because {sideName} {propertyName} is not defined.");
        }
    }

    private static bool IsPureWater(IFacadeStream stream)
    {
        if (stream.Composition is null)
        {
            return IsNamedWaterService(stream);
        }

        var activeComponents = stream.Composition.Components.Where(component =>
            component.MolarFraction.IsDefined && component.MolarFraction.Value.GetValue(PercentageUnits.Percentage) > 1e-6d ||
            component.MassFraction.IsDefined && component.MassFraction.Value.GetValue(PercentageUnits.Percentage) > 1e-6d).ToArray();

        if (activeComponents.Length == 0)
        {
            return IsNamedWaterService(stream);
        }

        return activeComponents.Length == 1 &&
               (string.Equals(activeComponents[0].Formula, "H2O", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(activeComponents[0].Name, "Water", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(activeComponents[0].Name, "Agua", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPureCondensingComponent(IFacadeStream inlet, IFacadeStream outlet)
    {
        if (IsPureWater(inlet) && IsPureWater(outlet))
        {
            return true;
        }

        if (TryGetSingleActiveComponentIdentity(inlet, out var inletIdentity) &&
            TryGetSingleActiveComponentIdentity(outlet, out var outletIdentity))
        {
            return string.Equals(inletIdentity, outletIdentity, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool TryGetSingleActiveComponentIdentity(IFacadeStream stream, out string identity)
    {
        identity = string.Empty;
        if (stream.Composition is null)
        {
            return false;
        }

        var activeComponents = stream.Composition.Components.Where(component =>
            component.MolarFraction.IsDefined && component.MolarFraction.Value.GetValue(PercentageUnits.Percentage) > 1e-6d ||
            component.MassFraction.IsDefined && component.MassFraction.Value.GetValue(PercentageUnits.Percentage) > 1e-6d).ToArray();

        if (activeComponents.Length != 1)
        {
            return false;
        }

        identity = string.IsNullOrWhiteSpace(activeComponents[0].Formula)
            ? activeComponents[0].Name
            : activeComponents[0].Formula;
        return !string.IsNullOrWhiteSpace(identity);
    }

    private static bool HasWaterAndNonWaterComponents(IFacadeStream stream)
    {
        if (stream.Composition is null)
        {
            return false;
        }

        var activeComponents = stream.Composition.Components.Where(component =>
            component.MolarFraction.IsDefined && component.MolarFraction.Value.GetValue(PercentageUnits.Percentage) > 1e-6d ||
            component.MassFraction.IsDefined && component.MassFraction.Value.GetValue(PercentageUnits.Percentage) > 1e-6d).ToArray();

        return activeComponents.Any(IsWaterComponent) && activeComponents.Any(component => !IsWaterComponent(component));
    }

    private static bool IsWaterComponent(ComponentFacade component)
    {
        return string.Equals(component.Formula, "H2O", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(component.Name, "Water", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(component.Name, "Agua", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(component.Name, "Steam", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNamedWaterService(IFacadeStream stream)
    {
        return stream.Name.Contains("water", StringComparison.OrdinalIgnoreCase) ||
               stream.Name.Contains("steam", StringComparison.OrdinalIgnoreCase) ||
               stream.Name.Contains("condensate", StringComparison.OrdinalIgnoreCase) ||
               stream.Name.Contains("agua", StringComparison.OrdinalIgnoreCase) ||
               stream.Name.Contains("vapor de agua", StringComparison.OrdinalIgnoreCase) ||
               stream.Name.Contains("condensado", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUserDefined<T>(Variable<T> variable)
        where T : Amount =>
        variable.IsDefined && variable.DataProcedence == VariableDefinedBy.UserInput;

    private static void SetIfUndefined<T>(Variable<T> variable, T value)
        where T : Amount
    {
        if (!variable.IsDefined)
        {
            variable.SetValue(value, VariableDefinedBy.Equipment);
        }
    }

}

internal sealed record DesignPracticesShellSideCrossflowFractions(
    double NominalFraction,
    double PressureDropFraction,
    double HeatTransferFraction);
