namespace Shared.UnitOperations.HeatExchangers.Design;

public static class Dp09dShellAndTubeCorrelations
{
    public const string TubeSideWaterCoefficientSource =
        "DP09D no-change-of-phase procedure, page 13, step 8.l(2) tube-side water hio equation";

    public const string TubeSideWaterPressureDropSource =
        "DP09D no-change-of-phase procedure, page 14, step 8.m water tube-side pressure-drop equation";

    public const string Figure14FrictionAndJFactorSource =
        "DP09D no-change-of-phase procedure, Figure 1.4, shell-side friction factor f and heat-transfer j factor";

    public static double NormalCrossflowFraction(
        double tubeBundleDiameterInches,
        double passPartitionRatio,
        Dp09dRearHeadType rearHeadType)
    {
        var curves = rearHeadType switch
        {
            Dp09dRearHeadType.FixedTubesheet => FixedTubesheetCrossflowFractionCurves,
            Dp09dRearHeadType.SplitRingFloatingHead => SplitRingFloatingHeadCrossflowFractionCurves,
            Dp09dRearHeadType.PullThroughFloatingHead => PullThroughFloatingHeadCrossflowFractionCurves,
            _ => FixedTubesheetCrossflowFractionCurves
        };
        var clampedRatio = Math.Clamp(passPartitionRatio, curves[0].Ratio, curves[^1].Ratio);
        var clampedBundleDiameter = Math.Clamp(tubeBundleDiameterInches, 10d, rearHeadType == Dp09dRearHeadType.PullThroughFloatingHead ? 60d : 50d);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedRatio > right.Ratio)
            {
                continue;
            }

            var leftValue = InterpolateLinear(left.Points, clampedBundleDiameter);
            var rightValue = InterpolateLinear(right.Points, clampedBundleDiameter);
            var fraction = (clampedRatio - left.Ratio) / (right.Ratio - left.Ratio);
            return leftValue + fraction * (rightValue - leftValue);
        }

        return InterpolateLinear(curves[^1].Points, clampedBundleDiameter);
    }

    public static double TubeSideIsothermalFrictionFactor(double reynolds)
    {
        if (reynolds <= 2000d)
        {
            return 16d / Math.Max(reynolds, 1d);
        }

        if (reynolds < 3800d)
        {
            return 1e-4d * Math.Pow(reynolds, 0.575d);
        }

        return 0.0035d + 0.264d * Math.Pow(reynolds, -0.42d);
    }

    public static double TubeSideWaterCoefficientCorrectedToOutsideArea(
        double velocityFeetPerSecond,
        double tubeInsideDiameterInches,
        double tubeOutsideDiameterInches,
        double tubeBulkTemperatureFahrenheit)
    {
        if (velocityFeetPerSecond <= 0d ||
            tubeInsideDiameterInches <= 0d ||
            tubeOutsideDiameterInches <= 0d ||
            tubeBulkTemperatureFahrenheit <= 0d)
        {
            return 0d;
        }

        return 368d / tubeOutsideDiameterInches *
               Math.Pow(velocityFeetPerSecond * tubeInsideDiameterInches, 0.7d) *
               Math.Pow(tubeBulkTemperatureFahrenheit / 100d, 0.26d);
    }

    public static double TubeSideViscosityGradientCorrectionFactor(double reynolds, double bulkToWallViscosityRatio)
    {
        var exponent = reynolds switch
        {
            >= 6000d => 0.135d,
            >= 3000d => InterpolateLinear([new(3000d, 0.165d), new(6000d, 0.135d)], reynolds),
            >= 2000d => InterpolateLinear([new(2000d, 0.200d), new(3000d, 0.165d)], reynolds),
            _ => 0.220d
        };
        var clampedRatio = Math.Clamp(bulkToWallViscosityRatio, 0.01d, 100d);

        return Math.Clamp(Math.Pow(clampedRatio, -exponent), 0.30d, 4.0d);
    }

    public static double TubeSidePressureDropNaturalConvectionCorrectionFactor(
        double reynolds,
        double grashofPrandtlViscosityRatio)
    {
        if (grashofPrandtlViscosityRatio <= 1e5d)
        {
            return 1d;
        }

        var curves = new[]
        {
            new CorrectionCurve(2000d, [new(1e5d, 1.00d), new(1e6d, 1.08d), new(1e7d, 1.32d), new(1e8d, 1.55d), new(1e9d, 1.75d)]),
            new CorrectionCurve(3000d, [new(1e5d, 1.00d), new(1e6d, 1.07d), new(1e7d, 1.26d), new(1e8d, 1.45d), new(1e9d, 1.62d)]),
            new CorrectionCurve(4000d, [new(1e5d, 1.00d), new(1e6d, 1.05d), new(1e7d, 1.19d), new(1e8d, 1.32d), new(1e9d, 1.43d)]),
            new CorrectionCurve(5000d, [new(1e5d, 1.00d), new(1e6d, 1.03d), new(1e7d, 1.10d), new(1e8d, 1.18d), new(1e9d, 1.24d)]),
            new CorrectionCurve(6000d, [new(1e5d, 1.00d), new(1e6d, 1.02d), new(1e7d, 1.04d), new(1e8d, 1.07d), new(1e9d, 1.10d)]),
            new CorrectionCurve(8000d, [new(1e5d, 1.00d), new(1e6d, 1.00d), new(1e7d, 1.02d), new(1e8d, 1.05d), new(1e9d, 1.08d)])
        };

        var clampedReynolds = Math.Clamp(reynolds, curves[0].Reynolds, curves[^1].Reynolds);
        var clampedGroup = Math.Clamp(grashofPrandtlViscosityRatio, 1e5d, 1e9d);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedReynolds > right.Reynolds)
            {
                continue;
            }

            var leftValue = InterpolateLogLog(left.Points, clampedGroup);
            var rightValue = InterpolateLogLog(right.Points, clampedGroup);
            var fraction = (clampedReynolds - left.Reynolds) / (right.Reynolds - left.Reynolds);

            return leftValue + fraction * (rightValue - leftValue);
        }

        return InterpolateLogLog(curves[^1].Points, clampedGroup);
    }

    public static double DP_j_ShellSideHeatTransferFactor(
        double DP_Rexh_HeatTransferCrossflowReynoldsNumber,
        ShellAndTubeTubeLayout layout)
    {
        // DP09D Figure 1.4: j is read from the lower heat-transfer family using Re_xh and tube layout.
        var points = layout == ShellAndTubeTubeLayout.Square
            ? SquareJFactorPoints
            : TriangularJFactorPoints;

        return InterpolateLogLog(points, DP_Rexh_HeatTransferCrossflowReynoldsNumber);
    }

    public static double DP_f_ShellSideFrictionFactor(
        double DP_Rexp_PressureDropCrossflowReynoldsNumber,
        ShellAndTubeTubeLayout layout)
    {
        var DP_PR_PitchRatio = layout == ShellAndTubeTubeLayout.Square ? 1.25d : 1.33d;
        return DP_f_ShellSideFrictionFactor(
            DP_Rexp_PressureDropCrossflowReynoldsNumber,
            layout,
            DP_PR_PitchRatio);
    }

    public static double DP_f_ShellSideFrictionFactor(
        double DP_Rexp_PressureDropCrossflowReynoldsNumber,
        ShellAndTubeTubeLayout layout,
        double DP_PR_PitchRatio)
    {
        // DP09D Figure 1.4: f is read from the upper friction family using Re_xp, PR, and tube layout.
        var points = layout == ShellAndTubeTubeLayout.Square
            ? InterpolateFigure14FrictionCurve(SquareFrictionFactorCurves, DP_PR_PitchRatio)
            : InterpolateFigure14FrictionCurve(TriangularFrictionFactorCurves, DP_PR_PitchRatio);

        return InterpolateLogLog(points, DP_Rexp_PressureDropCrossflowReynoldsNumber);
    }

    public static double DP_SC_BaffleSpacingCorrection(
        double DP_LBCC_BaffleSpacingInches,
        double DP_DOTL_TubeBundleDiameterInches)
    {
        // DP09D Figure 1.2, page 42: SC as a function of n = LBCC/DOTL.
        var DP_N_BaffleSpacingToBundleDiameterRatio =
            DP_LBCC_BaffleSpacingInches / Math.Max(DP_DOTL_TubeBundleDiameterInches, 1e-12);
        var points = new[]
        {
            new ChartPoint(0.20d, 0.65d),
            new ChartPoint(0.30d, 0.82d),
            new ChartPoint(0.40d, 0.96d),
            new ChartPoint(0.50d, 1.06d),
            new ChartPoint(0.60d, 1.13d),
            new ChartPoint(0.80d, 1.24d),
            new ChartPoint(1.00d, 1.30d)
        };

        return InterpolateLinear(points, Math.Clamp(DP_N_BaffleSpacingToBundleDiameterRatio, points[0].X, points[^1].X));
    }

    public static double DP_RC_ReynoldsNumberCorrection(double DP_Rext_TotalFlowReynoldsNumber)
    {
        // DP09D Figure 1.3, page 42: RC low-Reynolds correction from Re_xt.
        // Current digitization uses the lower Figure 1.3 family; final audit should split curves 1/2/3 by rear-head and DOTL basis.
        var points = new[]
        {
            new ChartPoint(50d, 0.35d),
            new ChartPoint(100d, 0.55d),
            new ChartPoint(200d, 0.72d),
            new ChartPoint(400d, 0.88d),
            new ChartPoint(600d, 0.95d),
            new ChartPoint(1000d, 1.00d)
        };

        if (DP_Rext_TotalFlowReynoldsNumber >= 1000d)
        {
            return 1d;
        }

        return InterpolateLinear(points, Math.Clamp(DP_Rext_TotalFlowReynoldsNumber, points[0].X, points[^1].X));
    }

    public static double LowPrandtlNumberCorrection(double prandtl)
    {
        var points = new[]
        {
            new ChartPoint(0d, 0.30d),
            new ChartPoint(2d, 0.40d),
            new ChartPoint(4d, 0.58d),
            new ChartPoint(6d, 0.73d),
            new ChartPoint(8d, 0.83d),
            new ChartPoint(10d, 0.90d),
            new ChartPoint(12d, 0.95d),
            new ChartPoint(14d, 0.98d),
            new ChartPoint(16d, 1.00d),
            new ChartPoint(24d, 1.00d)
        };

        return InterpolateLinear(points, Math.Clamp(prandtl, points[0].X, points[^1].X));
    }

    public static double ShortTubeCorrectionFactor(double reynolds, double lengthToInsideDiameter)
    {
        var reynoldsCurves = new[]
        {
            new CorrectionCurve(100d, [new(10d, 0.021d), new(20d, 0.018d), new(30d, 0.014d), new(40d, 0.010d), new(50d, 0.004d), new(60d, 0.000d)]),
            new CorrectionCurve(300d, [new(10d, 0.034d), new(20d, 0.030d), new(30d, 0.023d), new(40d, 0.012d), new(50d, 0.003d), new(60d, 0.000d)]),
            new CorrectionCurve(500d, [new(10d, 0.065d), new(20d, 0.056d), new(30d, 0.030d), new(40d, 0.014d), new(50d, 0.005d), new(60d, 0.000d)]),
            new CorrectionCurve(1000d, [new(10d, 0.106d), new(20d, 0.096d), new(30d, 0.062d), new(40d, 0.018d), new(50d, 0.006d), new(60d, 0.000d)]),
            new CorrectionCurve(2000d, [new(10d, 0.182d), new(20d, 0.164d), new(30d, 0.110d), new(40d, 0.030d), new(50d, 0.008d), new(60d, 0.000d)])
        };
        var clampedReynolds = Math.Clamp(reynolds, reynoldsCurves[0].Reynolds, reynoldsCurves[^1].Reynolds);
        var clampedLengthRatio = Math.Clamp(lengthToInsideDiameter, 10d, 60d);

        for (var i = 0; i < reynoldsCurves.Length - 1; i++)
        {
            var left = reynoldsCurves[i];
            var right = reynoldsCurves[i + 1];
            if (clampedReynolds > right.Reynolds)
            {
                continue;
            }

            var leftValue = InterpolateLinear(left.Points, clampedLengthRatio);
            var rightValue = InterpolateLinear(right.Points, clampedLengthRatio);
            var fraction = (clampedReynolds - left.Reynolds) / (right.Reynolds - left.Reynolds);
            return leftValue + fraction * (rightValue - leftValue);
        }

        return InterpolateLinear(reynoldsCurves[^1].Points, clampedLengthRatio);
    }

    public static double NaturalConvectionFactor(
        double grashofNumber,
        Dp09dTubeOrientation orientation,
        double lengthToInsideDiameter = 100d)
    {
        if (orientation == Dp09dTubeOrientation.Horizontal)
        {
            return InterpolateLogLog(HorizontalNaturalConvectionPoints, grashofNumber);
        }

        var clampedLengthRatio = Math.Clamp(lengthToInsideDiameter, VerticalNaturalConvectionCurves[0].LengthToDiameter, VerticalNaturalConvectionCurves[^1].LengthToDiameter);
        var clampedGrashofNumber = Math.Clamp(grashofNumber, VerticalNaturalConvectionCurves[0].Points[0].X, VerticalNaturalConvectionCurves[0].Points[^1].X);

        for (var i = 0; i < VerticalNaturalConvectionCurves.Length - 1; i++)
        {
            var left = VerticalNaturalConvectionCurves[i];
            var right = VerticalNaturalConvectionCurves[i + 1];
            if (clampedLengthRatio > right.LengthToDiameter)
            {
                continue;
            }

            var leftValue = InterpolateLogLog(left.Points, clampedGrashofNumber);
            var rightValue = InterpolateLogLog(right.Points, clampedGrashofNumber);
            var fraction = (Math.Log(clampedLengthRatio) - Math.Log(left.LengthToDiameter)) /
                           (Math.Log(right.LengthToDiameter) - Math.Log(left.LengthToDiameter));
            return Math.Exp(Math.Log(leftValue) + fraction * (Math.Log(rightValue) - Math.Log(leftValue)));
        }

        return InterpolateLogLog(VerticalNaturalConvectionCurves[^1].Points, clampedGrashofNumber);
    }

    public static double OneShellPassTemperatureCorrectionFactor(
        double hotInletTemperatureF,
        double hotOutletTemperatureF,
        double coldInletTemperatureF,
        double coldOutletTemperatureF)
    {
        return TemperatureCorrectionFactor(
            shellPasses: 1d,
            hotInletTemperatureF,
            hotOutletTemperatureF,
            coldInletTemperatureF,
            coldOutletTemperatureF);
    }

    public static double TemperatureCorrectionFactor(
        double shellPasses,
        double hotInletTemperatureF,
        double hotOutletTemperatureF,
        double coldInletTemperatureF,
        double coldOutletTemperatureF)
    {
        var hotRange = hotInletTemperatureF - hotOutletTemperatureF;
        var coldRange = coldOutletTemperatureF - coldInletTemperatureF;
        var approach = hotInletTemperatureF - coldInletTemperatureF;

        if (hotRange <= 0d || coldRange <= 0d || approach <= 0d)
        {
            return 1d;
        }

        var r = hotRange / Math.Max(coldRange, 1e-12);
        var p = coldRange / Math.Max(approach, 1e-12);
        var shellCount = Math.Max(1d, Math.Round(shellPasses));

        if (p <= 0d || p >= 1d)
        {
            return 1d;
        }

        if (shellCount > 1d)
        {
            p = EquivalentOneShellPassP(p, r, shellCount);
        }

        return OneShellPassTemperatureCorrectionFactorFromPr(p, r);
    }

    private static double OneShellPassTemperatureCorrectionFactorFromPr(double p, double r)
    {
        var root = Math.Sqrt(r * r + 1d);
        if (Math.Abs(r - 1d) < 1e-9)
        {
            var numerator = Math.Sqrt(2d) * p / Math.Max(1d - p, 1e-12);
            var denominator = Math.Log((2d - p * (2d - Math.Sqrt(2d))) /
                                       Math.Max(2d - p * (2d + Math.Sqrt(2d)), 1e-12));

            return ClampCorrectionFactor(numerator / Math.Max(denominator, 1e-12));
        }

        var firstTerm = root / (r - 1d);
        var firstLog = Math.Log((1d - p) / Math.Max(1d - r * p, 1e-12));
        var secondLog = Math.Log((2d - p * (r + 1d - root)) /
                                 Math.Max(2d - p * (r + 1d + root), 1e-12));

        return ClampCorrectionFactor(firstTerm * firstLog / Math.Max(secondLog, 1e-12));
    }

    private static double EquivalentOneShellPassP(double p, double r, double shellPasses)
    {
        if (Math.Abs(r - 1d) < 1e-9)
        {
            return p / Math.Max(shellPasses - (shellPasses - 1d) * p, 1e-12);
        }

        var ratio = (1d - p * r) / Math.Max(1d - p, 1e-12);
        if (ratio <= 0d)
        {
            return p;
        }

        var equivalentTemperatureRatio = Math.Pow(ratio, 1d / shellPasses);

        return (1d - equivalentTemperatureRatio) / Math.Max(r - equivalentTemperatureRatio, 1e-12);
    }

    private static double InterpolateLogLog(IReadOnlyList<ChartPoint> points, double x)
    {
        var clamped = Math.Clamp(x, points[0].X, points[^1].X);
        for (var i = 0; i < points.Count - 1; i++)
        {
            var left = points[i];
            var right = points[i + 1];
            if (clamped > right.X)
            {
                continue;
            }

            var fraction = (Math.Log(clamped) - Math.Log(left.X)) / (Math.Log(right.X) - Math.Log(left.X));
            return Math.Exp(Math.Log(left.Y) + fraction * (Math.Log(right.Y) - Math.Log(left.Y)));
        }

        return points[^1].Y;
    }

    private static double InterpolateLinear(IReadOnlyList<ChartPoint> points, double x)
    {
        for (var i = 0; i < points.Count - 1; i++)
        {
            var left = points[i];
            var right = points[i + 1];
            if (x > right.X)
            {
                continue;
            }

            var fraction = (x - left.X) / (right.X - left.X);
            return left.Y + fraction * (right.Y - left.Y);
        }

        return points[^1].Y;
    }

    private static double ClampCorrectionFactor(double correctionFactor)
    {
        if (double.IsNaN(correctionFactor) || double.IsInfinity(correctionFactor))
        {
            return 1d;
        }

        return Math.Clamp(correctionFactor, 0.01d, 1d);
    }

    private static ChartPoint[] InterpolateFigure14FrictionCurve(Figure14FrictionCurve[] curves, double pitchRatio)
    {
        var clampedPitchRatio = Math.Clamp(pitchRatio, curves[0].PitchRatio, curves[^1].PitchRatio);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedPitchRatio > right.PitchRatio)
            {
                continue;
            }

            return InterpolateFigure14FrictionPoints(left, right, clampedPitchRatio);
        }

        return curves[^1].Points.ToArray();
    }

    private static ChartPoint[] InterpolateFigure14FrictionPoints(
        Figure14FrictionCurve left,
        Figure14FrictionCurve right,
        double pitchRatio)
    {
        var fraction = (pitchRatio - left.PitchRatio) / (right.PitchRatio - left.PitchRatio);
        return left.Points
            .Zip(
                right.Points,
                (leftPoint, rightPoint) =>
                {
                    var value = Math.Exp(
                        Math.Log(leftPoint.Y) +
                        fraction * (Math.Log(rightPoint.Y) - Math.Log(leftPoint.Y)));
                    return new ChartPoint(leftPoint.X, value);
                })
            .ToArray();
    }

    private static readonly ChartPoint[] SquareJFactorPoints =
    [
        new(100d, 0.12d),
        new(300d, 0.074d),
        new(1_000d, 0.043d),
        new(3_000d, 0.025d),
        new(10_000d, 0.014d),
        new(30_000d, 0.0085d),
        new(100_000d, 0.0048d)
    ];

    private static readonly ChartPoint[] TriangularJFactorPoints =
    [
        new(100d, 0.15d),
        new(300d, 0.091d),
        new(1_000d, 0.052d),
        new(3_000d, 0.031d),
        new(10_000d, 0.018d),
        new(30_000d, 0.011d),
        new(100_000d, 0.006d)
    ];

    private static readonly Figure14FrictionCurve[] SquareFrictionFactorCurves =
    [
        new(1.25d,
        [
            new(100d, 1.30d),
            new(300d, 0.88d),
            new(1_000d, 0.52d),
            new(3_000d, 0.30d),
            new(10_000d, 0.16d),
            new(30_000d, 0.12d),
            new(100_000d, 0.10d),
            new(300_000d, 0.085d),
            new(1_000_000d, 0.070d)
        ]),
        new(1.33d,
        [
            new(100d, 0.95d),
            new(300d, 0.64d),
            new(1_000d, 0.39d),
            new(3_000d, 0.23d),
            new(10_000d, 0.13d),
            new(30_000d, 0.105d),
            new(100_000d, 0.090d),
            new(300_000d, 0.076d),
            new(1_000_000d, 0.062d)
        ])
    ];

    private static readonly Figure14FrictionCurve[] TriangularFrictionFactorCurves =
    [
        new(1.25d,
        [
            new(100d, 2.80d),
            new(300d, 1.55d),
            new(1_000d, 0.74d),
            new(3_000d, 0.39d),
            new(10_000d, 0.22d),
            new(30_000d, 0.16d),
            new(100_000d, 0.13d),
            new(300_000d, 0.105d),
            new(1_000_000d, 0.085d)
        ]),
        new(1.33d,
        [
            new(100d, 2.10d),
            new(300d, 1.15d),
            new(1_000d, 0.56d),
            new(3_000d, 0.31d),
            new(10_000d, 0.18d),
            new(30_000d, 0.135d),
            new(100_000d, 0.11d),
            new(300_000d, 0.092d),
            new(1_000_000d, 0.075d)
        ])
    ];

    private static readonly ChartPoint[] HorizontalNaturalConvectionPoints =
    [
        new(4d, 0.1d),
        new(5d, 0.7d),
        new(10d, 2.0d),
        new(100d, 8.0d),
        new(1_000d, 25d),
        new(10_000d, 80d),
        new(100_000d, 250d),
        new(1_000_000d, 800d),
        new(10_000_000d, 2_500d)
    ];

    private static readonly NaturalConvectionCurve[] VerticalNaturalConvectionCurves =
    [
        new(20d, [new(4d, 0.1d), new(10d, 3.5d), new(100d, 16d), new(1_000d, 80d), new(10_000d, 300d), new(100_000d, 1_200d), new(1_000_000d, 6_000d), new(10_000_000d, 25_000d)]),
        new(50d, [new(4d, 0.1d), new(10d, 2.5d), new(100d, 11d), new(1_000d, 45d), new(10_000d, 160d), new(100_000d, 650d), new(1_000_000d, 3_200d), new(10_000_000d, 13_000d)]),
        new(100d, [new(4d, 0.1d), new(10d, 1.8d), new(100d, 8d), new(1_000d, 32d), new(10_000d, 110d), new(100_000d, 450d), new(1_000_000d, 2_000d), new(10_000_000d, 8_000d)]),
        new(200d, [new(4d, 0.1d), new(10d, 1.3d), new(100d, 5d), new(1_000d, 22d), new(10_000d, 80d), new(100_000d, 320d), new(1_000_000d, 1_500d), new(10_000_000d, 6_000d)]),
        new(400d, [new(4d, 0.1d), new(10d, 0.8d), new(100d, 3d), new(1_000d, 12d), new(10_000d, 45d), new(100_000d, 180d), new(1_000_000d, 800d), new(10_000_000d, 3_000d)])
    ];

    private static readonly CrossflowFractionCurve[] FixedTubesheetCrossflowFractionCurves =
    [
        new(0.200d, [new(10d, 0.56d), new(20d, 0.61d), new(30d, 0.64d), new(40d, 0.66d), new(50d, 0.67d)]),
        new(0.225d, [new(10d, 0.59d), new(20d, 0.64d), new(30d, 0.66d), new(40d, 0.68d), new(50d, 0.70d)]),
        new(0.250d, [new(10d, 0.62d), new(20d, 0.68d), new(30d, 0.71d), new(40d, 0.73d), new(50d, 0.75d)]),
        new(0.275d, [new(10d, 0.66d), new(20d, 0.70d), new(30d, 0.73d), new(40d, 0.75d), new(50d, 0.77d)])
    ];

    private static readonly CrossflowFractionCurve[] SplitRingFloatingHeadCrossflowFractionCurves =
    [
        new(0.200d, [new(10d, 0.42d), new(20d, 0.48d), new(30d, 0.51d), new(40d, 0.52d), new(50d, 0.53d)]),
        new(0.225d, [new(10d, 0.45d), new(20d, 0.50d), new(30d, 0.53d), new(40d, 0.55d), new(50d, 0.55d)]),
        new(0.250d, [new(10d, 0.48d), new(20d, 0.53d), new(30d, 0.56d), new(40d, 0.57d), new(50d, 0.57d)]),
        new(0.275d, [new(10d, 0.51d), new(20d, 0.55d), new(30d, 0.58d), new(40d, 0.59d), new(50d, 0.60d)])
    ];

    private static readonly CrossflowFractionCurve[] PullThroughFloatingHeadCrossflowFractionCurves =
    [
        new(0.200d, [new(10d, 0.34d), new(20d, 0.39d), new(30d, 0.45d), new(40d, 0.50d), new(50d, 0.53d), new(60d, 0.55d)]),
        new(0.225d, [new(10d, 0.36d), new(20d, 0.42d), new(30d, 0.48d), new(40d, 0.53d), new(50d, 0.55d), new(60d, 0.57d)]),
        new(0.250d, [new(10d, 0.39d), new(20d, 0.45d), new(30d, 0.53d), new(40d, 0.57d), new(50d, 0.59d), new(60d, 0.60d)]),
        new(0.275d, [new(10d, 0.44d), new(20d, 0.52d), new(30d, 0.57d), new(40d, 0.60d), new(50d, 0.63d), new(60d, 0.65d)])
    ];

    private sealed record ChartPoint(double X, double Y);

    private sealed record CorrectionCurve(double Reynolds, IReadOnlyList<ChartPoint> Points);

    private sealed record CrossflowFractionCurve(double Ratio, IReadOnlyList<ChartPoint> Points);

    private sealed record Figure14FrictionCurve(double PitchRatio, IReadOnlyList<ChartPoint> Points);

    private sealed record NaturalConvectionCurve(double LengthToDiameter, IReadOnlyList<ChartPoint> Points);
}

public enum Dp09dRearHeadType
{
    FixedTubesheet,
    SplitRingFloatingHead,
    PullThroughFloatingHead
}

public enum Dp09dTubeOrientation
{
    Horizontal,
    Vertical
}
