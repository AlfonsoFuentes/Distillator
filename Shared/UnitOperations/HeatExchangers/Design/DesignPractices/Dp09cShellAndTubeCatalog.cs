namespace Shared.UnitOperations.HeatExchangers.Design;

public static class Dp09cShellAndTubeCatalog
{
    public static bool TryGetTableOuterTubeLimitInches(
        double shellInsideDiameterInches,
        ShellAndTubeTubeConstruction tubeConstruction,
        Dp09dRearHeadType rearHeadType,
        out double outerTubeLimitInches)
    {
        if (tubeConstruction == ShellAndTubeTubeConstruction.UTube ||
            rearHeadType == Dp09dRearHeadType.FixedTubesheet)
        {
            outerTubeLimitInches = shellInsideDiameterInches - GetFixedTubesheetOrUTubeBundleClearance(shellInsideDiameterInches);
            return shellInsideDiameterInches <= 75d;
        }

        if (rearHeadType == Dp09dRearHeadType.SplitRingFloatingHead)
        {
            outerTubeLimitInches = shellInsideDiameterInches - GetSplitRingFloatingHeadBundleClearance(shellInsideDiameterInches);
            return shellInsideDiameterInches <= 60d;
        }

        outerTubeLimitInches = shellInsideDiameterInches <= 50d
            ? shellInsideDiameterInches - 0.5d
            : shellInsideDiameterInches - 0.625d;
        return false;
    }

    public static double GetPullThroughFloatingHeadOuterTubeLimitInches(
        double shellInsideDiameterInches,
        double designPressurePsig)
    {
        var lower = 1d;
        var upper = Math.Min(80d, Math.Max(shellInsideDiameterInches, 1d));

        for (var iteration = 0; iteration < 40; iteration++)
        {
            var candidate = (lower + upper) / 2d;
            var calculatedShellId = candidate + GetPullThroughFloatingHeadDiametralClearanceInches(candidate, designPressurePsig);

            if (calculatedShellId > shellInsideDiameterInches)
            {
                upper = candidate;
            }
            else
            {
                lower = candidate;
            }
        }

        return (lower + upper) / 2d;
    }

    public static double GetPullThroughFloatingHeadDiametralClearanceInches(
        double outerTubeLimitInches,
        double designPressurePsig)
    {
        var pressure = Math.Clamp(designPressurePsig, 150d, 600d);
        var lowerCurve = PullThroughFigure7Curves[0];
        var upperCurve = PullThroughFigure7Curves[^1];

        foreach (var curve in PullThroughFigure7Curves)
        {
            if (curve.DesignPressurePsig <= pressure)
            {
                lowerCurve = curve;
            }

            if (curve.DesignPressurePsig >= pressure)
            {
                upperCurve = curve;
                break;
            }
        }

        var lowerClearance = InterpolateFigure7Curve(lowerCurve, outerTubeLimitInches);
        var upperClearance = InterpolateFigure7Curve(upperCurve, outerTubeLimitInches);

        if (Math.Abs(upperCurve.DesignPressurePsig - lowerCurve.DesignPressurePsig) < 1e-9)
        {
            return lowerClearance;
        }

        var fraction = (pressure - lowerCurve.DesignPressurePsig) /
                       (upperCurve.DesignPressurePsig - lowerCurve.DesignPressurePsig);
        return lowerClearance + fraction * (upperClearance - lowerClearance);
    }

    public static double GetPassPartitionDistanceInches(
        double shellInsideDiameterInches,
        double tubeOuterDiameterInches,
        double tubePitchInches,
        ShellAndTubeTubeLayout tubeLayout)
    {
        if (tubeLayout == ShellAndTubeTubeLayout.Square)
        {
            return tubeOuterDiameterInches + (shellInsideDiameterInches <= 24d ? 0.5d : 0.625d);
        }

        return Math.Max(tubeOuterDiameterInches + (shellInsideDiameterInches <= 24d ? 0.5d : 0.625d), 0.84d * tubePitchInches);
    }

    public static int GetRecommendedMaximumTubePasses(double shellInsideDiameterInches) =>
        shellInsideDiameterInches switch
        {
            < 10d => 4,
            < 20d => 6,
            < 30d => 8,
            < 40d => 10,
            < 50d => 12,
            < 60d => 14,
            _ => 16
        };

    public static double GetShellNozzleCorrectionFactor(
        double shellInsideDiameterInches,
        double outerTubeLimitInches,
        double tubeOuterDiameterInches,
        double shellNozzleInsideDiameterInches)
    {
        var bundleEntranceRatio = (outerTubeLimitInches - tubeOuterDiameterInches) /
                                  Math.Max(shellInsideDiameterInches, 1e-12);
        var nozzleRatio = (shellNozzleInsideDiameterInches + 1d) /
                          Math.Max(shellInsideDiameterInches, 1e-12);
        var clampedNozzleRatio = Math.Clamp(
            nozzleRatio,
            Figure8ShellNozzleCurves[0].NozzleRatio,
            Figure8ShellNozzleCurves[^1].NozzleRatio);

        for (var index = 1; index < Figure8ShellNozzleCurves.Count; index++)
        {
            var previous = Figure8ShellNozzleCurves[index - 1];
            var next = Figure8ShellNozzleCurves[index];
            if (clampedNozzleRatio > next.NozzleRatio)
            {
                continue;
            }

            var previousCorrection = InterpolateFigure8Curve(previous, bundleEntranceRatio);
            var nextCorrection = InterpolateFigure8Curve(next, bundleEntranceRatio);
            if (Math.Abs(next.NozzleRatio - previous.NozzleRatio) < 1e-12)
            {
                return previousCorrection;
            }

            var fraction = (clampedNozzleRatio - previous.NozzleRatio) /
                           (next.NozzleRatio - previous.NozzleRatio);
            return previousCorrection + fraction * (nextCorrection - previousCorrection);
        }

        return InterpolateFigure8Curve(Figure8ShellNozzleCurves[^1], bundleEntranceRatio);
    }

    public static double GetTubeMaterialThermalConductivityBtuHrFtF(ShellAndTubeTubeMaterial material)
    {
        return material switch
        {
            ShellAndTubeTubeMaterial.Admiralty => 64d,
            ShellAndTubeTubeMaterial.Type316StainlessSteel => 9d,
            ShellAndTubeTubeMaterial.Type304StainlessSteel => 9d,
            ShellAndTubeTubeMaterial.Brass => 57d,
            ShellAndTubeTubeMaterial.RedBrass => 92d,
            ShellAndTubeTubeMaterial.AluminumBrass => 58d,
            ShellAndTubeTubeMaterial.CuproNickel9010 => 41d,
            ShellAndTubeTubeMaterial.CuproNickel7030 => 17d,
            ShellAndTubeTubeMaterial.Monel => 15d,
            ShellAndTubeTubeMaterial.Inconel => 11d,
            ShellAndTubeTubeMaterial.Aluminum => 117d,
            ShellAndTubeTubeMaterial.CarbonSteel => 26d,
            ShellAndTubeTubeMaterial.CarbonMoly => 25d,
            ShellAndTubeTubeMaterial.Copper => 223d,
            ShellAndTubeTubeMaterial.Lead => 20d,
            ShellAndTubeTubeMaterial.Nickel => 36d,
            ShellAndTubeTubeMaterial.Titanium => 11d,
            ShellAndTubeTubeMaterial.ChromeMoly1CrHalfMo => 24d,
            ShellAndTubeTubeMaterial.ChromeMoly214CrHalfMo => 22d,
            ShellAndTubeTubeMaterial.ChromeMoly5CrHalfMo => 20d,
            ShellAndTubeTubeMaterial.ChromeMoly12Cr1Mo => 16d,
            _ => 26d
        };
    }

    public static double GetTubeWallThicknessInches(int bwg)
    {
        return bwg switch
        {
            8 => 0.165d,
            9 => 0.148d,
            10 => 0.134d,
            11 => 0.120d,
            12 => 0.109d,
            13 => 0.095d,
            14 => 0.083d,
            15 => 0.072d,
            16 => 0.065d,
            17 => 0.058d,
            18 => 0.049d,
            19 => 0.042d,
            20 => 0.035d,
            22 => 0.028d,
            _ => throw new ArgumentOutOfRangeException(nameof(bwg), bwg, "Unsupported DP09C BWG value.")
        };
    }

    public static DesignPracticesVelocityRange? GetCoolingWaterTubeVelocityRange(
        ShellAndTubeTubeMaterial material,
        ShellAndTubeCoolingWaterType waterType)
    {
        if (waterType == ShellAndTubeCoolingWaterType.None)
        {
            return null;
        }

        var isFresh = waterType == ShellAndTubeCoolingWaterType.FreshWater;
        var isSaltOrBrackish = waterType is ShellAndTubeCoolingWaterType.SaltWater or ShellAndTubeCoolingWaterType.BrackishWater;

        return material switch
        {
            ShellAndTubeTubeMaterial.CarbonSteel when isFresh => new DesignPracticesVelocityRange(3d, 6d, "carbon steel / fresh noninhibited water"),
            ShellAndTubeTubeMaterial.RedBrass => new DesignPracticesVelocityRange(3d, 4d, "red brass / all cooling-water types"),
            ShellAndTubeTubeMaterial.Admiralty when isFresh => new DesignPracticesVelocityRange(3d, 10d, "admiralty / fresh water"),
            ShellAndTubeTubeMaterial.Admiralty when isSaltOrBrackish => new DesignPracticesVelocityRange(3d, 6d, "admiralty / salt or brackish water"),
            ShellAndTubeTubeMaterial.AluminumBrass when isFresh => new DesignPracticesVelocityRange(3d, 10d, "aluminum brass / fresh water"),
            ShellAndTubeTubeMaterial.AluminumBrass when isSaltOrBrackish => new DesignPracticesVelocityRange(3d, 8d, "aluminum brass / salt or brackish water"),
            ShellAndTubeTubeMaterial.CuproNickel7030 => new DesignPracticesVelocityRange(3d, 12d, "70-30 cupronickel / all cooling-water types"),
            ShellAndTubeTubeMaterial.CuproNickel9010 => new DesignPracticesVelocityRange(3d, 12d, "90-10 cupronickel / all cooling-water types"),
            ShellAndTubeTubeMaterial.Monel when isFresh => new DesignPracticesVelocityRange(3d, 12d, "monel / fresh water"),
            _ => null
        };
    }

    private static double GetFixedTubesheetOrUTubeBundleClearance(double shellInsideDiameterInches) =>
        shellInsideDiameterInches switch
        {
            <= 50d => 0.5d,
            <= 62d => 0.625d,
            <= 75d => 0.75d,
            _ => 0.75d
        };

    private static double GetSplitRingFloatingHeadBundleClearance(double shellInsideDiameterInches) =>
        shellInsideDiameterInches switch
        {
            <= 22d => 1.25d,
            <= 24d => 1.375d,
            <= 31d => 1.625d,
            <= 42d => 1.75d,
            <= 60d => 1.875d,
            _ => 1.875d
        };

    private static double InterpolateFigure7Curve(PullThroughFigure7Curve curve, double outerTubeLimitInches)
    {
        var x = Math.Clamp(outerTubeLimitInches, curve.Points[0].OuterTubeLimitInches, curve.Points[^1].OuterTubeLimitInches);

        for (var index = 1; index < curve.Points.Count; index++)
        {
            var previous = curve.Points[index - 1];
            var next = curve.Points[index];
            if (x > next.OuterTubeLimitInches)
            {
                continue;
            }

            var fraction = (x - previous.OuterTubeLimitInches) / (next.OuterTubeLimitInches - previous.OuterTubeLimitInches);
            return previous.ClearanceInches + fraction * (next.ClearanceInches - previous.ClearanceInches);
        }

        return curve.Points[^1].ClearanceInches;
    }

    private static double InterpolateFigure8Curve(Figure8ShellNozzleCurve curve, double bundleEntranceRatio)
    {
        var x = Math.Clamp(
            bundleEntranceRatio,
            curve.Points[0].BundleEntranceRatio,
            curve.Points[^1].BundleEntranceRatio);

        for (var index = 1; index < curve.Points.Count; index++)
        {
            var previous = curve.Points[index - 1];
            var next = curve.Points[index];
            if (x > next.BundleEntranceRatio)
            {
                continue;
            }

            var fraction = (x - previous.BundleEntranceRatio) /
                           (next.BundleEntranceRatio - previous.BundleEntranceRatio);
            return previous.CorrectionFactor + fraction * (next.CorrectionFactor - previous.CorrectionFactor);
        }

        return curve.Points[^1].CorrectionFactor;
    }

    private static readonly IReadOnlyList<PullThroughFigure7Curve> PullThroughFigure7Curves =
    [
        new(150d,
        [
            new(5d, 2.55d),
            new(10d, 2.65d),
            new(20d, 2.95d),
            new(30d, 3.22d),
            new(40d, 3.32d),
            new(50d, 3.36d),
            new(60d, 3.40d),
            new(70d, 3.43d),
            new(80d, 3.48d)
        ]),
        new(300d,
        [
            new(5d, 2.65d),
            new(10d, 2.75d),
            new(20d, 3.05d),
            new(30d, 3.40d),
            new(40d, 3.70d),
            new(50d, 3.80d),
            new(60d, 3.87d),
            new(70d, 3.92d),
            new(80d, 3.96d)
        ]),
        new(450d,
        [
            new(5d, 2.85d),
            new(10d, 3.00d),
            new(20d, 3.30d),
            new(30d, 3.75d),
            new(40d, 4.15d),
            new(50d, 4.35d),
            new(60d, 4.48d),
            new(70d, 4.55d),
            new(80d, 4.60d)
        ]),
        new(600d,
        [
            new(5d, 3.05d),
            new(10d, 3.15d),
            new(20d, 3.45d),
            new(30d, 4.05d),
            new(40d, 4.65d),
            new(50d, 5.05d),
            new(60d, 5.35d),
            new(70d, 5.55d),
            new(80d, 5.70d)
        ])
    ];

    private static readonly IReadOnlyList<Figure8ShellNozzleCurve> Figure8ShellNozzleCurves =
    [
        new(0.10d, [new(0.40d, 1.000d), new(0.70d, 1.000d), new(1.00d, 0.990d)]),
        new(0.20d, [new(0.40d, 1.000d), new(0.75d, 1.000d), new(0.90d, 0.995d), new(1.00d, 0.980d)]),
        new(0.30d, [new(0.40d, 1.000d), new(0.65d, 1.000d), new(0.80d, 0.985d), new(0.90d, 0.975d), new(1.00d, 0.955d)]),
        new(0.40d, [new(0.40d, 1.000d), new(0.72d, 1.000d), new(0.82d, 0.985d), new(0.90d, 0.960d), new(1.00d, 0.925d)]),
        new(0.50d, [new(0.40d, 1.000d), new(0.62d, 1.000d), new(0.75d, 0.965d), new(0.90d, 0.925d), new(1.00d, 0.895d)]),
        new(0.60d, [new(0.40d, 1.000d), new(0.52d, 1.000d), new(0.70d, 0.980d), new(0.80d, 0.940d), new(0.90d, 0.900d), new(1.00d, 0.860d)]),
        new(0.70d, [new(0.40d, 1.000d), new(0.52d, 1.000d), new(0.62d, 0.955d), new(0.70d, 0.920d), new(0.80d, 0.875d), new(0.90d, 0.840d), new(1.00d, 0.805d)])
    ];

    private sealed record PullThroughFigure7Curve(double DesignPressurePsig, IReadOnlyList<PullThroughFigure7Point> Points);

    private sealed record PullThroughFigure7Point(double OuterTubeLimitInches, double ClearanceInches);

    private sealed record Figure8ShellNozzleCurve(double NozzleRatio, IReadOnlyList<Figure8ShellNozzlePoint> Points);

    private sealed record Figure8ShellNozzlePoint(double BundleEntranceRatio, double CorrectionFactor);
}

public sealed record DesignPracticesVelocityRange(double MinimumFeetPerSecond, double MaximumFeetPerSecond, string Basis);

public enum ShellAndTubeTubeMaterial
{
    CarbonSteel,
    Admiralty,
    Type316StainlessSteel,
    Type304StainlessSteel,
    Brass,
    RedBrass,
    AluminumBrass,
    CuproNickel9010,
    CuproNickel7030,
    Monel,
    Inconel,
    Aluminum,
    CarbonMoly,
    Copper,
    Lead,
    Nickel,
    Titanium,
    ChromeMoly1CrHalfMo,
    ChromeMoly214CrHalfMo,
    ChromeMoly5CrHalfMo,
    ChromeMoly12Cr1Mo
}

public enum ShellAndTubeTubeConstruction
{
    Straight,
    UTube
}

public enum ShellAndTubeFrontHeadType
{
    RemovableChannelAndCover,
    Bonnet,
    IntegralTubesheetRemovableCover
}

public enum ShellAndTubeCleaningMethod
{
    NotSpecified,
    Chemical,
    Mechanical,
    ChemicalOrMechanical
}

public enum ShellAndTubeReboilerType
{
    Kettle,
    Internal,
    VerticalThermosiphon,
    HorizontalThermosiphon,
    PumpThrough
}

public enum ShellAndTubeShellType
{
    OnePass,
    TwoPass,
    SplitFlow,
    DoubleSplitFlow,
    DividedFlow,
    CrossFlow
}

public enum ShellAndTubeBaffleType
{
    SingleSegmental,
    DoubleSegmental,
    RodBaffle,
    HelicalBaffle,
    NoTubesInWindow
}

public enum ShellAndTubeCoolingWaterType
{
    None,
    FreshWater,
    BrackishWater,
    SaltWater
}

public enum ShellAndTubeEnhancedHeatTransferType
{
    None,
    IntegralFinnedTubes,
    NucleateBoilingTubes,
    TurbulencePromoters,
    OnlineMechanicalCleaning,
    RodBaffles,
    HelicalBaffles,
    TwistedTubes
}

public enum ShellAndTubeCondenserArrangement
{
    NotSpecified,
    ConventionalWithAccumulator,
    ElevatedAboveReceiver,
    DrumlessCondenser,
    SurfaceCondenser
}
