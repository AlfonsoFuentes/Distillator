using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed record DesignPracticesCondensationZone(
    string Name,
    double DutyBtuPerHour,
    double CondensingInletTemperatureF,
    double CondensingOutletTemperatureF,
    double CoolingInletTemperatureF,
    double CoolingOutletTemperatureF,
    double LogMeanTemperatureDifferenceF);

public sealed record DesignPracticesCondensationZoneArea(
    string Name,
    double AreaSquareFeet,
    double AreaFraction);

public sealed record DesignPracticesCondensingAreaIterationResult(
    double CondensingCoefficientBtuHrFt2F,
    double LiquidCoolingCoefficientBtuHrFt2F,
    double DripCoolingCoefficientBtuHrFt2F,
    double DutyWeightedCoefficientBtuHrFt2F,
    double RequiredAreaSquareFeet,
    double VaporFreeAreaFraction,
    double VaporMassVelocityLbSecFt2,
    int Iterations);

public static class Dp09fCondensationZoneModel
{
    private const double TraceVaporFractionPercent = 1d;
    private const double VaporPhaseFractionPercent = 99d;

    public static IReadOnlyList<DesignPracticesCondensationZone> BuildPreliminaryZones(
        HeatExchangerDesignRequest request,
        DesignPracticesProcessRegime processRegime,
        double totalDutyBtuPerHour)
    {
        var condensingInlet = processRegime == DesignPracticesProcessRegime.ShellSideCondensation
            ? request.ShellSideInlet.Stream
            : request.TubeSideInlet.Stream;
        var condensingOutlet = processRegime == DesignPracticesProcessRegime.ShellSideCondensation
            ? request.ShellSideOutlet.Stream
            : request.TubeSideOutlet.Stream;
        var coolingInlet = processRegime == DesignPracticesProcessRegime.ShellSideCondensation
            ? request.TubeSideInlet.Stream
            : request.ShellSideInlet.Stream;
        var coolingOutlet = processRegime == DesignPracticesProcessRegime.ShellSideCondensation
            ? request.TubeSideOutlet.Stream
            : request.ShellSideOutlet.Stream;

        var condensingInletTemperature = ReadTemperatureF(condensingInlet);
        var condensingOutletTemperature = ReadTemperatureF(condensingOutlet);
        var coolingInletTemperature = ReadTemperatureF(coolingInlet);
        var coolingOutletTemperature = ReadTemperatureF(coolingOutlet);
        var condensingInletVaporFraction = ReadVaporFractionPercent(condensingInlet);
        var condensingOutletVaporFraction = ReadVaporFractionPercent(condensingOutlet);

        var sensibleDutyZones = BuildSensibleDutyZones(
            condensingInlet,
            condensingOutlet,
            totalDutyBtuPerHour,
            condensingInletTemperature,
            condensingOutletTemperature,
            coolingInletTemperature,
            coolingOutletTemperature,
            condensingInletVaporFraction,
            condensingOutletVaporFraction);

        if (sensibleDutyZones.Count > 0)
        {
            return sensibleDutyZones;
        }

        var inferredZones = BuildInferredZones(
            totalDutyBtuPerHour,
            condensingInletTemperature,
            condensingOutletTemperature,
            coolingInletTemperature,
            coolingOutletTemperature,
            condensingInletVaporFraction,
            condensingOutletVaporFraction);

        if (inferredZones.Count > 0)
        {
            return inferredZones;
        }

        var lmtd = CalculateCounterCurrentLmtd(
            condensingInletTemperature - coolingOutletTemperature,
            condensingOutletTemperature - coolingInletTemperature);

        return
        [
            new DesignPracticesCondensationZone(
                "Preliminary condensation zone",
                totalDutyBtuPerHour,
                condensingInletTemperature,
                condensingOutletTemperature,
                coolingInletTemperature,
                coolingOutletTemperature,
                lmtd)
        ];
    }

    public static double CalculateWeightedEffectiveLmtd(IReadOnlyList<DesignPracticesCondensationZone> zones)
    {
        var totalDuty = zones.Sum(zone => zone.DutyBtuPerHour);
        var sumDutyOverLmtd = zones.Sum(zone =>
            zone.DutyBtuPerHour / Math.Max(zone.LogMeanTemperatureDifferenceF, 1e-12));

        return totalDuty / Math.Max(sumDutyOverLmtd, 1e-12);
    }

    public static IReadOnlyList<DesignPracticesCondensationZoneArea> CalculateZoneAreas(
        IReadOnlyList<DesignPracticesCondensationZone> zones,
        double zoneOverallCoefficientBtuHrFt2F)
    {
        var areas = zones
            .Select(zone => new
            {
                zone.Name,
                Area = zone.DutyBtuPerHour /
                       Math.Max(zoneOverallCoefficientBtuHrFt2F * zone.LogMeanTemperatureDifferenceF, 1e-12)
            })
            .ToArray();
        var totalArea = areas.Sum(zone => zone.Area);

        return areas
            .Select(zone => new DesignPracticesCondensationZoneArea(
                zone.Name,
                zone.Area,
                zone.Area / Math.Max(totalArea, 1e-12)))
            .ToArray();
    }

    public static double CalculateAreaWeightedPressureDrop(
        IReadOnlyList<double> zonePressureDropsPsi,
        IReadOnlyList<DesignPracticesCondensationZoneArea> zoneAreas)
    {
        if (zonePressureDropsPsi.Count != zoneAreas.Count)
        {
            throw new ArgumentException("The number of zone pressure drops must match the number of DP09F zone areas.");
        }

        var pressureDrop = 0d;
        for (var i = 0; i < zonePressureDropsPsi.Count; i++)
        {
            pressureDrop += zonePressureDropsPsi[i] * zoneAreas[i].AreaFraction;
        }

        return pressureDrop;
    }

    public static IReadOnlyList<double> CalculatePreliminaryZonePressureDrops(
        IReadOnlyList<DesignPracticesCondensationZone> zones,
        double basePressureDropPsi,
        double twoPhaseDensityCorrectionFactor)
    {
        return zones
            .Select(zone => basePressureDropPsi * SelectPreliminaryZonePressureDropFactor(
                zone.Name,
                twoPhaseDensityCorrectionFactor))
            .ToArray();
    }

    public static IReadOnlyList<double> CalculatePreliminaryZonePressureDrops(
        IReadOnlyList<DesignPracticesCondensationZone> zones,
        double basePressureDropPsi,
        IReadOnlyList<double> zoneDensityCorrectionFactors)
    {
        if (zones.Count != zoneDensityCorrectionFactors.Count)
        {
            throw new ArgumentException("The number of DP09F zone density corrections must match the number of zones.");
        }

        return zones
            .Select((zone, index) =>
                basePressureDropPsi *
                SelectPreliminaryZoneTypePressureDropMultiplier(zone.Name) *
                Math.Clamp(zoneDensityCorrectionFactors[index], 0.10d, 10d))
            .ToArray();
    }

    public static IReadOnlyList<double> CalculatePreliminaryZonePressureDropContributions(
        IReadOnlyList<DesignPracticesCondensationZone> zones,
        IReadOnlyList<DesignPracticesCondensationZoneArea> zoneAreas,
        double fullLengthBasePressureDropPsi,
        IReadOnlyList<double> zoneDensityCorrectionFactors)
    {
        if (zones.Count != zoneAreas.Count ||
            zones.Count != zoneDensityCorrectionFactors.Count)
        {
            throw new ArgumentException("The number of DP09F zones, areas, and density corrections must match.");
        }

        return zones
            .Select((zone, index) =>
                fullLengthBasePressureDropPsi *
                Math.Clamp(zoneAreas[index].AreaFraction, 0d, 1d) *
                SelectPreliminaryZoneTypePressureDropMultiplier(zone.Name) *
                Math.Clamp(zoneDensityCorrectionFactors[index], 0.10d, 10d))
            .ToArray();
    }

    public static IReadOnlyList<double> CalculatePreliminaryZoneDensityCorrectionFactors(
        IReadOnlyList<DesignPracticesCondensationZone> zones,
        double baseDensityLbFt3,
        double vaporDensityLbFt3,
        double twoPhaseDensityLbFt3,
        double liquidDensityLbFt3)
    {
        return zones
            .Select(zone => baseDensityLbFt3 / Math.Max(SelectPreliminaryZoneDensity(
                zone.Name,
                vaporDensityLbFt3,
                twoPhaseDensityLbFt3,
                liquidDensityLbFt3), 1e-12))
            .Select(correction => Math.Clamp(correction, 0.10d, 10d))
            .ToArray();
    }

    public static IReadOnlyList<double> CalculateZoneAverageDensities(
        IReadOnlyList<DesignPracticesCondensationZone> zones,
        double totalMassFlowLbPerHour,
        double inletVaporFraction,
        double outletVaporFraction,
        double vaporDensityLbFt3,
        double liquidDensityLbFt3)
    {
        if (zones.Count == 0)
        {
            return [];
        }

        var vaporFractionIn = Math.Clamp(inletVaporFraction, 0d, 1d);
        var vaporFractionOut = Math.Clamp(outletVaporFraction, 0d, 1d);
        if (vaporFractionOut > vaporFractionIn)
        {
            (vaporFractionIn, vaporFractionOut) = (vaporFractionOut, vaporFractionIn);
        }

        var totalCondensingDuty = zones
            .Where(zone => IsCondensingZone(zone.Name))
            .Sum(zone => zone.DutyBtuPerHour);
        var consumedCondensingDuty = 0d;
        var densities = new List<double>(zones.Count);

        foreach (var zone in zones)
        {
            var (zoneInletVaporFraction, zoneOutletVaporFraction) = SelectZoneVaporFractionProfile(
                zone,
                vaporFractionIn,
                vaporFractionOut,
                totalCondensingDuty,
                consumedCondensingDuty);

            if (IsCondensingZone(zone.Name))
            {
                consumedCondensingDuty += zone.DutyBtuPerHour;
            }

            densities.Add(CalculateEndpointAverageDensity(
                totalMassFlowLbPerHour,
                zoneInletVaporFraction,
                zoneOutletVaporFraction,
                vaporDensityLbFt3,
                liquidDensityLbFt3));
        }

        return densities;
    }

    public static double CalculateCondensingZoneVaporFreeAreaFraction(
        IReadOnlyList<DesignPracticesCondensationZone> zones,
        double inletVaporFraction,
        double outletVaporFraction,
        double vaporDensityLbFt3,
        double liquidDensityLbFt3)
    {
        if (zones.Count == 0)
        {
            return CalculateEndpointAverageVaporVolumeFraction(
                inletVaporFraction,
                outletVaporFraction,
                vaporDensityLbFt3,
                liquidDensityLbFt3);
        }

        var vaporFractionIn = Math.Clamp(inletVaporFraction, 0d, 1d);
        var vaporFractionOut = Math.Clamp(outletVaporFraction, 0d, 1d);
        if (vaporFractionOut > vaporFractionIn)
        {
            (vaporFractionIn, vaporFractionOut) = (vaporFractionOut, vaporFractionIn);
        }

        var totalCondensingDuty = zones
            .Where(zone => IsCondensingZone(zone.Name))
            .Sum(zone => zone.DutyBtuPerHour);
        var consumedCondensingDuty = 0d;
        var weightedVaporFraction = 0d;

        foreach (var zone in zones)
        {
            var (zoneInletVaporFraction, zoneOutletVaporFraction) = SelectZoneVaporFractionProfile(
                zone,
                vaporFractionIn,
                vaporFractionOut,
                totalCondensingDuty,
                consumedCondensingDuty);

            if (!IsCondensingZone(zone.Name))
            {
                continue;
            }

            var zoneVaporVolumeFraction = CalculateEndpointAverageVaporVolumeFraction(
                zoneInletVaporFraction,
                zoneOutletVaporFraction,
                vaporDensityLbFt3,
                liquidDensityLbFt3);
            weightedVaporFraction += zoneVaporVolumeFraction *
                                     zone.DutyBtuPerHour /
                                     Math.Max(totalCondensingDuty, 1e-12);
            consumedCondensingDuty += zone.DutyBtuPerHour;
        }

        if (totalCondensingDuty <= 0d)
        {
            return CalculateEndpointAverageVaporVolumeFraction(
                vaporFractionIn,
                vaporFractionOut,
                vaporDensityLbFt3,
                liquidDensityLbFt3);
        }

        return Math.Clamp(weightedVaporFraction, 0.05d, 1d);
    }

    private static double SelectPreliminaryZonePressureDropFactor(
        string zoneName,
        double twoPhaseDensityCorrectionFactor)
    {
        if (zoneName.Contains("desuper", StringComparison.OrdinalIgnoreCase) ||
            zoneName.Contains("vapor", StringComparison.OrdinalIgnoreCase))
        {
            return 1.15d;
        }

        if (zoneName.Contains("subcool", StringComparison.OrdinalIgnoreCase) ||
            zoneName.Contains("liquid", StringComparison.OrdinalIgnoreCase))
        {
            return 0.65d;
        }

        return Math.Clamp(twoPhaseDensityCorrectionFactor, 0.10d, 10d);
    }

    private static double SelectPreliminaryZoneTypePressureDropMultiplier(string zoneName)
    {
        if (zoneName.Contains("desuper", StringComparison.OrdinalIgnoreCase) ||
            zoneName.Contains("vapor", StringComparison.OrdinalIgnoreCase))
        {
            return 1.15d;
        }

        if (zoneName.Contains("subcool", StringComparison.OrdinalIgnoreCase) ||
            zoneName.Contains("liquid", StringComparison.OrdinalIgnoreCase))
        {
            return 0.65d;
        }

        return 1d;
    }

    private static double SelectPreliminaryZoneDensity(
        string zoneName,
        double vaporDensityLbFt3,
        double twoPhaseDensityLbFt3,
        double liquidDensityLbFt3)
    {
        if (zoneName.Contains("desuper", StringComparison.OrdinalIgnoreCase) ||
            zoneName.Contains("vapor", StringComparison.OrdinalIgnoreCase))
        {
            return vaporDensityLbFt3;
        }

        if (zoneName.Contains("subcool", StringComparison.OrdinalIgnoreCase) ||
            zoneName.Contains("liquid", StringComparison.OrdinalIgnoreCase))
        {
            return liquidDensityLbFt3;
        }

        return twoPhaseDensityLbFt3;
    }

    private static (double InletVaporFraction, double OutletVaporFraction) SelectZoneVaporFractionProfile(
        DesignPracticesCondensationZone zone,
        double inletVaporFraction,
        double outletVaporFraction,
        double totalCondensingDuty,
        double consumedCondensingDuty)
    {
        if (IsVaporCoolingZone(zone.Name))
        {
            return (inletVaporFraction, inletVaporFraction);
        }

        if (IsLiquidCoolingZone(zone.Name))
        {
            return (outletVaporFraction, outletVaporFraction);
        }

        if (totalCondensingDuty <= 0d)
        {
            return (inletVaporFraction, outletVaporFraction);
        }

        var vaporFractionRange = inletVaporFraction - outletVaporFraction;
        var zoneInletFraction = inletVaporFraction -
                                vaporFractionRange * consumedCondensingDuty / totalCondensingDuty;
        var zoneOutletFraction = inletVaporFraction -
                                 vaporFractionRange * (consumedCondensingDuty + zone.DutyBtuPerHour) / totalCondensingDuty;

        return (
            Math.Clamp(zoneInletFraction, outletVaporFraction, inletVaporFraction),
            Math.Clamp(zoneOutletFraction, outletVaporFraction, inletVaporFraction));
    }

    private static double CalculateEndpointAverageDensity(
        double totalMassFlowLbPerHour,
        double inletVaporFraction,
        double outletVaporFraction,
        double vaporDensityLbFt3,
        double liquidDensityLbFt3)
    {
        var inletVolumetricFlowFt3Hr = CalculatePhaseVolumetricFlow(
            totalMassFlowLbPerHour,
            inletVaporFraction,
            vaporDensityLbFt3,
            liquidDensityLbFt3);
        var outletVolumetricFlowFt3Hr = CalculatePhaseVolumetricFlow(
            totalMassFlowLbPerHour,
            outletVaporFraction,
            vaporDensityLbFt3,
            liquidDensityLbFt3);

        return 2d * totalMassFlowLbPerHour /
               Math.Max(inletVolumetricFlowFt3Hr + outletVolumetricFlowFt3Hr, 1e-12);
    }

    private static double CalculatePhaseVolumetricFlow(
        double totalMassFlowLbPerHour,
        double vaporFraction,
        double vaporDensityLbFt3,
        double liquidDensityLbFt3)
    {
        var clampedVaporFraction = Math.Clamp(vaporFraction, 0d, 1d);
        var vaporMassFlow = totalMassFlowLbPerHour * clampedVaporFraction;
        var liquidMassFlow = totalMassFlowLbPerHour - vaporMassFlow;

        return vaporMassFlow / Math.Max(vaporDensityLbFt3, 1e-12) +
               liquidMassFlow / Math.Max(liquidDensityLbFt3, 1e-12);
    }

    private static double CalculateEndpointAverageVaporVolumeFraction(
        double inletVaporFraction,
        double outletVaporFraction,
        double vaporDensityLbFt3,
        double liquidDensityLbFt3)
    {
        return (
            CalculateVaporVolumeFraction(inletVaporFraction, vaporDensityLbFt3, liquidDensityLbFt3) +
            CalculateVaporVolumeFraction(outletVaporFraction, vaporDensityLbFt3, liquidDensityLbFt3)) / 2d;
    }

    private static double CalculateVaporVolumeFraction(
        double vaporFraction,
        double vaporDensityLbFt3,
        double liquidDensityLbFt3)
    {
        var clampedVaporFraction = Math.Clamp(vaporFraction, 0d, 1d);
        var vaporVolumeBasis = clampedVaporFraction / Math.Max(vaporDensityLbFt3, 1e-12);
        var liquidVolumeBasis = (1d - clampedVaporFraction) / Math.Max(liquidDensityLbFt3, 1e-12);

        return vaporVolumeBasis / Math.Max(vaporVolumeBasis + liquidVolumeBasis, 1e-12);
    }

    private static bool IsVaporCoolingZone(string zoneName)
    {
        return zoneName.Contains("desuper", StringComparison.OrdinalIgnoreCase) ||
               zoneName.Contains("vapor", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLiquidCoolingZone(string zoneName)
    {
        return zoneName.Contains("subcool", StringComparison.OrdinalIgnoreCase) ||
               zoneName.Contains("liquid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCondensingZone(string zoneName)
    {
        return !IsVaporCoolingZone(zoneName) && !IsLiquidCoolingZone(zoneName);
    }

    public static double CalculateHorizontalBundleCondensateStreams(
        double tubeCount,
        ShellAndTubeTubeLayout layout)
    {
        var (factor, exponent) = layout == ShellAndTubeTubeLayout.Square
            ? (1.29d, 0.480d)
            : (1.02d, 0.519d);

        return factor * Math.Pow(Math.Max(tubeCount, 1d), exponent);
    }

    public static double CalculateHorizontalBundleCondensingCoefficient(
        double condensateMassFlowLbPerHour,
        double condensingLengthFeet,
        double condensateStreams,
        DesignPracticesFluidProperties filmProperties,
        double vaporMassVelocityLbSecFt2,
        bool applyVaporMassVelocityCorrection = true)
    {
        var condensateLoading = condensateMassFlowLbPerHour /
                                Math.Max(condensingLengthFeet * condensateStreams, 1e-12);
        var uncorrectedCoefficient =
            8.3d *
            filmProperties.ThermalConductivityBtuHrFtF *
            Math.Pow(
                Math.Max(Math.Pow(filmProperties.DensityLbFt3 / 62.4d, 2d) /
                         Math.Pow(filmProperties.ViscosityLbFtHr, 2d), 1e-12),
                1d / 3d) /
            Math.Pow(Math.Max(condensateLoading, 1e-12), 1d / 3d);
        if (!applyVaporMassVelocityCorrection)
        {
            return uncorrectedCoefficient;
        }

        var velocityMultiplier = Math.Pow(Math.Max(vaporMassVelocityLbSecFt2, 1e-12) / 5d, 0.70d);

        return Math.Min(uncorrectedCoefficient * velocityMultiplier, 2d * uncorrectedCoefficient);
    }

    public static DesignPracticesCondensingAreaIterationResult IterateHorizontalBundleCondensingArea(
        IReadOnlyList<DesignPracticesCondensationZone> zones,
        double totalDutyBtuPerHour,
        double installedAreaSquareFeet,
        double shellFreeAreaSquareFeet,
        double baseVaporFreeAreaFraction,
        double vaporMassFlowLbPerHour,
        double condensateMassFlowLbPerHour,
        double condensingLengthFeet,
        double condensateStreams,
        DesignPracticesFluidProperties filmProperties,
        double vaporCoolingCoefficient,
        double bottomFlowLiquidCoolingCoefficient,
        bool applyVaporMassVelocityCorrection,
        int maximumIterations = 12,
        double tolerance = 0.005d)
    {
        var requiredArea = Math.Max(installedAreaSquareFeet, 1e-12);
        var condensingCoefficient = CalculateHorizontalBundleCondensingCoefficient(
            condensateMassFlowLbPerHour,
            condensingLengthFeet,
            condensateStreams,
            filmProperties,
            vaporMassVelocityLbSecFt2: 0d,
            applyVaporMassVelocityCorrection: false);
        var liquidCoolingCoefficient = bottomFlowLiquidCoolingCoefficient;
        var dripCoolingCoefficient = 1.5d * condensingCoefficient;
        var dutyWeightedCoefficient = CalculateDutyWeightedZoneCoefficient(
            zones,
            vaporCoolingCoefficient,
            condensingCoefficient,
            liquidCoolingCoefficient);
        var vaporFreeAreaFraction = Math.Clamp(baseVaporFreeAreaFraction, 0.05d, 1d);
        var vaporMassVelocity = vaporMassFlowLbPerHour /
                                Math.Max(shellFreeAreaSquareFeet * vaporFreeAreaFraction * 3600d, 1e-12);
        var iterations = 0;

        for (var i = 0; i < maximumIterations; i++)
        {
            iterations = i + 1;
            var areaRatio = Math.Clamp(requiredArea / Math.Max(installedAreaSquareFeet, 1e-12), 0.10d, 1d);
            vaporFreeAreaFraction = Math.Clamp(baseVaporFreeAreaFraction * areaRatio, 0.05d, 1d);
            vaporMassVelocity = vaporMassFlowLbPerHour /
                                Math.Max(shellFreeAreaSquareFeet * vaporFreeAreaFraction * 3600d, 1e-12);
            condensingCoefficient = CalculateHorizontalBundleCondensingCoefficient(
                condensateMassFlowLbPerHour,
                condensingLengthFeet,
                condensateStreams,
                filmProperties,
                vaporMassVelocity,
                applyVaporMassVelocityCorrection);
            dripCoolingCoefficient = 1.5d * condensingCoefficient;
            liquidCoolingCoefficient = CalculateLiquidCoolingCoefficient(
                bottomFlowLiquidCoolingCoefficient,
                dripCoolingCoefficient);
            dutyWeightedCoefficient = CalculateDutyWeightedZoneCoefficient(
                zones,
                vaporCoolingCoefficient,
                condensingCoefficient,
                liquidCoolingCoefficient);
            var weightedLmtd = CalculateWeightedEffectiveLmtd(zones);
            var updatedRequiredArea = totalDutyBtuPerHour /
                                      Math.Max(dutyWeightedCoefficient * weightedLmtd, 1e-12);
            var relativeChange = Math.Abs(updatedRequiredArea - requiredArea) /
                                 Math.Max(requiredArea, 1e-12);
            requiredArea = updatedRequiredArea;

            if (relativeChange <= tolerance)
            {
                break;
            }
        }

        return new DesignPracticesCondensingAreaIterationResult(
            condensingCoefficient,
            liquidCoolingCoefficient,
            dripCoolingCoefficient,
            dutyWeightedCoefficient,
            requiredArea,
            vaporFreeAreaFraction,
            vaporMassVelocity,
            iterations);
    }

    public static double CalculateDutyWeightedZoneCoefficient(
        IReadOnlyList<DesignPracticesCondensationZone> zones,
        double vaporCoolingCoefficient,
        double condensingCoefficient,
        double liquidCoolingCoefficient)
    {
        if (zones.Count == 0)
        {
            return condensingCoefficient;
        }

        var totalDuty = zones.Sum(zone => zone.DutyBtuPerHour);
        var dutyOverCoefficient = zones.Sum(zone =>
            zone.DutyBtuPerHour / Math.Max(SelectZoneCoefficient(
                zone.Name,
                vaporCoolingCoefficient,
                condensingCoefficient,
                liquidCoolingCoefficient), 1e-12));

        return totalDuty / Math.Max(dutyOverCoefficient, 1e-12);
    }

    public static double CalculateLiquidCoolingCoefficient(
        double bottomFlowLiquidCoolingCoefficient,
        double dripCoolingCoefficient,
        double dripDutyFraction = 0.50d)
    {
        var clampedDripFraction = Math.Clamp(dripDutyFraction, 0d, 1d);
        var bottomFlowFraction = 1d - clampedDripFraction;

        return 1d / (
            bottomFlowFraction / Math.Max(bottomFlowLiquidCoolingCoefficient, 1e-12) +
            clampedDripFraction / Math.Max(dripCoolingCoefficient, 1e-12));
    }

    public static double CalculateInsideTubeCondensingCoefficient(
        double equivalentLiquidMassVelocityLbHrFt2,
        double tubeInsideDiameterFeet,
        DesignPracticesFluidProperties filmProperties)
    {
        var reynolds = tubeInsideDiameterFeet *
                       Math.Max(equivalentLiquidMassVelocityLbHrFt2, 1e-12) /
                       Math.Max(filmProperties.ViscosityLbFtHr, 1e-12);
        var prandtl = filmProperties.CpBtuLbF *
                      filmProperties.ViscosityLbFtHr /
                      Math.Max(filmProperties.ThermalConductivityBtuHrFtF, 1e-12);
        var conductivityOverDiameter = filmProperties.ThermalConductivityBtuHrFtF /
                                       Math.Max(tubeInsideDiameterFeet, 1e-12);

        return reynolds < 50_000d
            ? 5.03d * Math.Pow(Math.Max(reynolds, 1d), 1d / 3d) *
              Math.Pow(Math.Max(prandtl, 1e-6d), 1d / 3d) *
              conductivityOverDiameter
            : 0.0265d * Math.Pow(reynolds, 0.8d) *
              Math.Pow(Math.Max(prandtl, 1e-6d), 1d / 3d) *
              conductivityOverDiameter;
    }

    public static double CalculateEquivalentLiquidMassVelocity(
        double averageLiquidMassVelocityLbHrFt2,
        double averageVaporMassVelocityLbHrFt2,
        double liquidDensityLbFt3,
        double vaporDensityLbFt3)
    {
        return averageLiquidMassVelocityLbHrFt2 +
               averageVaporMassVelocityLbHrFt2 *
               Math.Sqrt(Math.Max(liquidDensityLbFt3, 1e-12) / Math.Max(vaporDensityLbFt3, 1e-12));
    }

    private static double SelectZoneCoefficient(
        string zoneName,
        double vaporCoolingCoefficient,
        double condensingCoefficient,
        double liquidCoolingCoefficient)
    {
        if (zoneName.Contains("desuperheating", StringComparison.OrdinalIgnoreCase) ||
            zoneName.Contains("vapor", StringComparison.OrdinalIgnoreCase))
        {
            return vaporCoolingCoefficient;
        }

        if (zoneName.Contains("subcooling", StringComparison.OrdinalIgnoreCase) ||
            zoneName.Contains("liquid", StringComparison.OrdinalIgnoreCase))
        {
            return liquidCoolingCoefficient;
        }

        return condensingCoefficient;
    }

    private static IReadOnlyList<DesignPracticesCondensationZone> BuildInferredZones(
        double totalDutyBtuPerHour,
        double condensingInletTemperatureF,
        double condensingOutletTemperatureF,
        double coolingInletTemperatureF,
        double coolingOutletTemperatureF,
        double condensingInletVaporFractionPercent,
        double condensingOutletVaporFractionPercent)
    {
        var condensingTemperatureDrop = condensingInletTemperatureF - condensingOutletTemperatureF;
        var coolingTemperatureRise = coolingOutletTemperatureF - coolingInletTemperatureF;

        if (totalDutyBtuPerHour <= 0d || condensingTemperatureDrop <= 2d || coolingTemperatureRise <= 0d)
        {
            return [];
        }

        var fractions = SelectInferredZoneFractions(
            condensingInletVaporFractionPercent,
            condensingOutletVaporFractionPercent);
        if (fractions.Count == 0)
        {
            return [];
        }

        var zones = new List<DesignPracticesCondensationZone>(fractions.Count);
        var coolingZoneIn = coolingInletTemperatureF;
        var condensingZoneIn = condensingInletTemperatureF;
        var consumedCondensingDrop = 0d;

        foreach (var zoneFraction in fractions)
        {
            var dutyFraction = zoneFraction.DutyFraction;
            var condensingDrop = zoneFraction.TemperatureDropFraction * condensingTemperatureDrop;
            var coolingRise = dutyFraction * coolingTemperatureRise;
            var condensingZoneOut = zones.Count == fractions.Count - 1
                ? condensingOutletTemperatureF
                : condensingInletTemperatureF - consumedCondensingDrop - condensingDrop;
            var coolingZoneOut = zones.Count == fractions.Count - 1
                ? coolingOutletTemperatureF
                : coolingZoneIn + coolingRise;

            if (!TryCreateZone(
                    zoneFraction.Name,
                    totalDutyBtuPerHour * dutyFraction,
                    condensingZoneIn,
                    condensingZoneOut,
                    coolingZoneIn,
                    coolingZoneOut,
                    out var zone))
            {
                return [];
            }

            zones.Add(zone);
            coolingZoneIn = coolingZoneOut;
            condensingZoneIn = condensingZoneOut;
            consumedCondensingDrop += condensingDrop;
        }

        return zones;
    }

    private static IReadOnlyList<DesignPracticesCondensationZone> BuildSensibleDutyZones(
        IFacadeStream condensingInlet,
        IFacadeStream condensingOutlet,
        double totalDutyBtuPerHour,
        double condensingInletTemperatureF,
        double condensingOutletTemperatureF,
        double coolingInletTemperatureF,
        double coolingOutletTemperatureF,
        double condensingInletVaporFractionPercent,
        double condensingOutletVaporFractionPercent)
    {
        var condensingTemperatureDrop = condensingInletTemperatureF - condensingOutletTemperatureF;
        var coolingTemperatureRise = coolingOutletTemperatureF - coolingInletTemperatureF;
        if (totalDutyBtuPerHour <= 0d || condensingTemperatureDrop <= 2d || coolingTemperatureRise <= 0d)
        {
            return [];
        }

        if (!TryReadMassFlowLbPerHour(condensingInlet, condensingOutlet, out var massFlowLbPerHour) ||
            !TryReadCpBtuLbF(condensingInlet, out var inletCp) ||
            !TryReadCpBtuLbF(condensingOutlet, out var outletCp))
        {
            return [];
        }

        var hasInletVapor = condensingInletVaporFractionPercent >= VaporPhaseFractionPercent;
        var hasOutletLiquid = condensingOutletVaporFractionPercent <= TraceVaporFractionPercent;
        var hasPartialInlet = condensingInletVaporFractionPercent is > TraceVaporFractionPercent and < VaporPhaseFractionPercent;
        var hasPartialOutlet = condensingOutletVaporFractionPercent is > TraceVaporFractionPercent and < VaporPhaseFractionPercent;
        if ((!hasInletVapor && !hasPartialInlet) || (!hasOutletLiquid && !hasPartialOutlet))
        {
            return [];
        }

        var rawDesuperheatingDuty = 0d;
        var rawSubcoolingDuty = 0d;
        var desuperheatingTemperatureDrop = 0d;
        var subcoolingTemperatureDrop = 0d;

        if (hasInletVapor && hasOutletLiquid)
        {
            rawDesuperheatingDuty = massFlowLbPerHour * inletCp * condensingTemperatureDrop / 2d;
            rawSubcoolingDuty = massFlowLbPerHour * outletCp * condensingTemperatureDrop / 2d;
            var rawSensibleDuty = rawDesuperheatingDuty + rawSubcoolingDuty;
            if (rawSensibleDuty > 0d)
            {
                desuperheatingTemperatureDrop = condensingTemperatureDrop * rawDesuperheatingDuty / rawSensibleDuty;
                subcoolingTemperatureDrop = condensingTemperatureDrop - desuperheatingTemperatureDrop;
            }
        }
        else if (hasInletVapor && hasPartialOutlet)
        {
            rawDesuperheatingDuty = massFlowLbPerHour * inletCp * condensingTemperatureDrop;
            desuperheatingTemperatureDrop = condensingTemperatureDrop;
        }
        else if (hasPartialInlet && hasOutletLiquid)
        {
            rawSubcoolingDuty = massFlowLbPerHour * outletCp * condensingTemperatureDrop;
            subcoolingTemperatureDrop = condensingTemperatureDrop;
        }
        else
        {
            return [];
        }

        var (desuperheatingDuty, subcoolingDuty) = LimitSensibleDuties(
            rawDesuperheatingDuty,
            rawSubcoolingDuty,
            totalDutyBtuPerHour);
        var condensationDuty = totalDutyBtuPerHour - desuperheatingDuty - subcoolingDuty;
        if (condensationDuty <= totalDutyBtuPerHour * 0.10d)
        {
            return [];
        }

        var saturationTemperatureF = condensingInletTemperatureF - desuperheatingTemperatureDrop;
        var specs = new List<CondensationZoneSpec>(3);
        if (desuperheatingDuty > 0d)
        {
            specs.Add(new CondensationZoneSpec(
                "Sensible desuperheating zone",
                desuperheatingDuty,
                condensingInletTemperatureF,
                saturationTemperatureF));
        }

        specs.Add(new CondensationZoneSpec(
            "Sensible-balance condensation zone",
            condensationDuty,
            saturationTemperatureF,
            saturationTemperatureF));

        if (subcoolingDuty > 0d)
        {
            specs.Add(new CondensationZoneSpec(
                "Sensible subcooling zone",
                subcoolingDuty,
                saturationTemperatureF,
                condensingOutletTemperatureF));
        }

        return TryCreateSequentialZones(
            specs,
            totalDutyBtuPerHour,
            coolingInletTemperatureF,
            coolingOutletTemperatureF);
    }

    private static (double DesuperheatingDuty, double SubcoolingDuty) LimitSensibleDuties(
        double desuperheatingDuty,
        double subcoolingDuty,
        double totalDutyBtuPerHour)
    {
        var limitedDesuperheatingDuty = Math.Min(Math.Max(desuperheatingDuty, 0d), totalDutyBtuPerHour * 0.35d);
        var limitedSubcoolingDuty = Math.Min(Math.Max(subcoolingDuty, 0d), totalDutyBtuPerHour * 0.35d);
        var combinedSensibleDuty = limitedDesuperheatingDuty + limitedSubcoolingDuty;
        var maximumSensibleDuty = totalDutyBtuPerHour * 0.80d;

        if (combinedSensibleDuty <= maximumSensibleDuty || combinedSensibleDuty <= 0d)
        {
            return (limitedDesuperheatingDuty, limitedSubcoolingDuty);
        }

        var scale = maximumSensibleDuty / combinedSensibleDuty;
        return (limitedDesuperheatingDuty * scale, limitedSubcoolingDuty * scale);
    }

    private static IReadOnlyList<DesignPracticesCondensationZone> TryCreateSequentialZones(
        IReadOnlyList<CondensationZoneSpec> specs,
        double totalDutyBtuPerHour,
        double coolingInletTemperatureF,
        double coolingOutletTemperatureF)
    {
        var coolingTemperatureRise = coolingOutletTemperatureF - coolingInletTemperatureF;
        var zones = new List<DesignPracticesCondensationZone>(specs.Count);
        var coolingZoneIn = coolingInletTemperatureF;

        foreach (var spec in specs)
        {
            var isLast = zones.Count == specs.Count - 1;
            var coolingZoneOut = isLast
                ? coolingOutletTemperatureF
                : coolingZoneIn + coolingTemperatureRise * spec.DutyBtuPerHour / Math.Max(totalDutyBtuPerHour, 1e-12);

            if (!TryCreateZone(
                    spec.Name,
                    spec.DutyBtuPerHour,
                    spec.CondensingInletTemperatureF,
                    spec.CondensingOutletTemperatureF,
                    coolingZoneIn,
                    coolingZoneOut,
                    out var zone))
            {
                return [];
            }

            zones.Add(zone);
            coolingZoneIn = coolingZoneOut;
        }

        return zones;
    }

    private static IReadOnlyList<InferredCondensationZoneFraction> SelectInferredZoneFractions(
        double inletVaporFractionPercent,
        double outletVaporFractionPercent)
    {
        if (inletVaporFractionPercent >= VaporPhaseFractionPercent &&
            outletVaporFractionPercent <= TraceVaporFractionPercent)
        {
            return
            [
                new("Inferred desuperheating zone", 0.10d, 0.20d),
                new("Inferred condensation zone", 0.80d, 0.60d),
                new("Inferred subcooling zone", 0.10d, 0.20d)
            ];
        }

        if (inletVaporFractionPercent >= VaporPhaseFractionPercent &&
            outletVaporFractionPercent is > TraceVaporFractionPercent and < VaporPhaseFractionPercent)
        {
            return
            [
                new("Inferred desuperheating zone", 0.15d, 0.25d),
                new("Inferred partial condensation zone", 0.85d, 0.75d)
            ];
        }

        if (inletVaporFractionPercent is > TraceVaporFractionPercent and < VaporPhaseFractionPercent &&
            outletVaporFractionPercent <= TraceVaporFractionPercent)
        {
            return
            [
                new("Inferred final condensation zone", 0.85d, 0.75d),
                new("Inferred subcooling zone", 0.15d, 0.25d)
            ];
        }

        return [];
    }

    private static bool TryCreateZone(
        string name,
        double dutyBtuPerHour,
        double condensingInletTemperatureF,
        double condensingOutletTemperatureF,
        double coolingInletTemperatureF,
        double coolingOutletTemperatureF,
        out DesignPracticesCondensationZone zone)
    {
        zone = default!;
        var terminalDifferenceA = condensingInletTemperatureF - coolingOutletTemperatureF;
        var terminalDifferenceB = condensingOutletTemperatureF - coolingInletTemperatureF;

        if (terminalDifferenceA <= 0d || terminalDifferenceB <= 0d)
        {
            return false;
        }

        zone = new DesignPracticesCondensationZone(
            name,
            dutyBtuPerHour,
            condensingInletTemperatureF,
            condensingOutletTemperatureF,
            coolingInletTemperatureF,
            coolingOutletTemperatureF,
            CalculateCounterCurrentLmtd(terminalDifferenceA, terminalDifferenceB));
        return true;
    }

    private static double ReadTemperatureF(IFacadeStream stream)
    {
        if (!stream.Temperature.IsDefined)
        {
            throw new InvalidOperationException("Cannot build DP09F condensation zones because stream temperature is not defined.");
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

    private static double ReadVaporFractionPercent(IFacadeStream stream)
    {
        return stream.VaporFraction.IsDefined
            ? stream.VaporFraction.Value.GetValue(PercentageUnits.Percentage)
            : 0d;
    }

    private static bool TryReadMassFlowLbPerHour(
        IFacadeStream inlet,
        IFacadeStream outlet,
        out double massFlowLbPerHour)
    {
        if (inlet.MassFlow.IsDefined)
        {
            massFlowLbPerHour = inlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
            return massFlowLbPerHour > 0d;
        }

        if (outlet.MassFlow.IsDefined)
        {
            massFlowLbPerHour = outlet.MassFlow.Value.GetValue(MassFlowUnits.lb_hr);
            return massFlowLbPerHour > 0d;
        }

        massFlowLbPerHour = 0d;
        return false;
    }

    private static bool TryReadCpBtuLbF(IFacadeStream stream, out double cpBtuLbF)
    {
        if (stream.MassCp.IsDefined)
        {
            cpBtuLbF = stream.MassCp.Value.GetValue(MassEntropyUnits.BTU_lb_F);
            return cpBtuLbF > 0d;
        }

        cpBtuLbF = 0d;
        return false;
    }

    private static double CalculateCounterCurrentLmtd(double terminalDifferenceA, double terminalDifferenceB)
    {
        if (terminalDifferenceA <= 0d || terminalDifferenceB <= 0d)
        {
            throw new InvalidOperationException("Cannot build DP09F condensation zones because terminal temperature differences must be positive.");
        }

        return Math.Abs(terminalDifferenceA - terminalDifferenceB) < 1e-9
            ? terminalDifferenceA
            : (terminalDifferenceA - terminalDifferenceB) / Math.Log(terminalDifferenceA / terminalDifferenceB);
    }

    private sealed record InferredCondensationZoneFraction(
        string Name,
        double DutyFraction,
        double TemperatureDropFraction);

    private sealed record CondensationZoneSpec(
        string Name,
        double DutyBtuPerHour,
        double CondensingInletTemperatureF,
        double CondensingOutletTemperatureF);
}
