namespace Shared.UnitOperations.HeatExchangers.Design;

public static class Dp09eVaporizationCorrelations
{
    public static double SingleTubeMaximumHeatFlux(double criticalPressurePsia, double operatingPressurePsia)
    {
        var reducedPressure = operatingPressurePsia / Math.Max(criticalPressurePsia, 1e-12);
        var curves = SingleTubeMaximumHeatFluxCurves;
        var clampedCriticalPressure = Math.Clamp(criticalPressurePsia, curves[0].CriticalPressurePsia, curves[^1].CriticalPressurePsia);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedCriticalPressure > right.CriticalPressurePsia)
            {
                continue;
            }

            var leftValue = InterpolateLogLog(left.Points, reducedPressure);
            var rightValue = InterpolateLogLog(right.Points, reducedPressure);
            var fraction = (Math.Log(clampedCriticalPressure) - Math.Log(left.CriticalPressurePsia)) /
                           (Math.Log(right.CriticalPressurePsia) - Math.Log(left.CriticalPressurePsia));
            return Math.Exp(Math.Log(leftValue) + fraction * (Math.Log(rightValue) - Math.Log(leftValue)));
        }

        return InterpolateLogLog(curves[^1].Points, reducedPressure);
    }

    public static double BundleCorrectionFactor(double tubeCount, double pitchRatio)
    {
        var curves = BundleCorrectionCurves;
        var clampedPitchRatio = Math.Clamp(pitchRatio, curves[0].PitchRatio, curves[^1].PitchRatio);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedPitchRatio > right.PitchRatio)
            {
                continue;
            }

            var leftValue = InterpolateLogLog(left.Points, tubeCount);
            var rightValue = InterpolateLogLog(right.Points, tubeCount);
            var fraction = (clampedPitchRatio - left.PitchRatio) / (right.PitchRatio - left.PitchRatio);
            return leftValue + fraction * (rightValue - leftValue);
        }

        return InterpolateLogLog(curves[^1].Points, tubeCount);
    }

    public static double BundleNucleateBoilingCorrectionFactor(double tubeCount, double pitchRatio)
    {
        var curves = BundleNucleateBoilingCorrectionCurves;
        var clampedPitchRatio = Math.Clamp(pitchRatio, curves[0].PitchRatio, curves[^1].PitchRatio);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedPitchRatio > right.PitchRatio)
            {
                continue;
            }

            var leftValue = InterpolateLogXLinearY(left.Points, tubeCount);
            var rightValue = InterpolateLogXLinearY(right.Points, tubeCount);
            var fraction = (clampedPitchRatio - left.PitchRatio) / (right.PitchRatio - left.PitchRatio);
            return Math.Min(leftValue + fraction * (rightValue - leftValue), 2.5d);
        }

        return Math.Min(InterpolateLogXLinearY(curves[^1].Points, tubeCount), 2.5d);
    }

    public static double VerticalThermosiphonChokeReferenceMaximumHeatFlux(double criticalPressurePsia, double operatingPressurePsia)
    {
        var reducedPressure = operatingPressurePsia / Math.Max(criticalPressurePsia, 1e-12);
        var curves = VerticalThermosiphonChokeReferenceHeatFluxCurves;
        var clampedCriticalPressure = Math.Clamp(criticalPressurePsia, curves[0].CriticalPressurePsia, curves[^1].CriticalPressurePsia);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedCriticalPressure > right.CriticalPressurePsia)
            {
                continue;
            }

            var leftValue = InterpolateLogLog(left.Points, reducedPressure);
            var rightValue = InterpolateLogLog(right.Points, reducedPressure);
            var fraction = (Math.Log(clampedCriticalPressure) - Math.Log(left.CriticalPressurePsia)) /
                           (Math.Log(right.CriticalPressurePsia) - Math.Log(left.CriticalPressurePsia));
            return Math.Exp(Math.Log(leftValue) + fraction * (Math.Log(rightValue) - Math.Log(leftValue)));
        }

        return InterpolateLogLog(curves[^1].Points, reducedPressure);
    }

    public static double VerticalThermosiphonTubeGeometryCorrectionFactor(double tubeInsideDiameterInches, double tubeLengthFeet)
    {
        var curves = VerticalThermosiphonTubeGeometryCorrectionCurves;
        var clampedLength = Math.Clamp(tubeLengthFeet, curves[0].TubeLengthFeet, curves[^1].TubeLengthFeet);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedLength > right.TubeLengthFeet)
            {
                continue;
            }

            var leftValue = InterpolateLogXLinearY(left.Points, tubeInsideDiameterInches);
            var rightValue = InterpolateLogXLinearY(right.Points, tubeInsideDiameterInches);
            var fraction = (clampedLength - left.TubeLengthFeet) / (right.TubeLengthFeet - left.TubeLengthFeet);
            return Math.Max(leftValue + fraction * (rightValue - leftValue), 0.05d);
        }

        return Math.Max(InterpolateLogXLinearY(curves[^1].Points, tubeInsideDiameterInches), 0.05d);
    }

    public static double VerticalThermosiphonNaturalConvectionCoefficient(
        double criticalPressurePsia,
        double heatFluxBtuHrFt2)
    {
        var curves = VerticalThermosiphonNaturalConvectionCoefficientCurves;
        var clampedCriticalPressure = Math.Clamp(criticalPressurePsia, curves[0].CriticalPressurePsia, curves[^1].CriticalPressurePsia);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedCriticalPressure > right.CriticalPressurePsia)
            {
                continue;
            }

            var leftValue = InterpolateLinear(left.Points, heatFluxBtuHrFt2);
            var rightValue = InterpolateLinear(right.Points, heatFluxBtuHrFt2);
            var fraction = (Math.Log(clampedCriticalPressure) - Math.Log(left.CriticalPressurePsia)) /
                           (Math.Log(right.CriticalPressurePsia) - Math.Log(left.CriticalPressurePsia));
            return leftValue + fraction * (rightValue - leftValue);
        }

        return InterpolateLinear(curves[^1].Points, heatFluxBtuHrFt2);
    }

    public static double VerticalThermosiphonFigureA17OutletVaporFractionLimit(
        double reducedHeatFluxFtHr,
        double inletVelocityFeetPerSecond)
    {
        if (reducedHeatFluxFtHr <= 0d || inletVelocityFeetPerSecond <= 0d)
        {
            return 0d;
        }

        var ratio = reducedHeatFluxFtHr / inletVelocityFeetPerSecond;
        return Math.Clamp(0.18d * ratio, 0.02d, 0.50d);
    }

    public static double FinnedTubeSurfaceFactorForPureHydrocarbon(
        double molecularWeight,
        double plainSurfaceNucleateBoilingCoefficient)
    {
        var curves = PureHydrocarbonFinnedTubeSurfaceFactorCurves;
        var clampedMolecularWeight = Math.Clamp(molecularWeight, curves[0].Basis, curves[^1].Basis);
        return InterpolateCurveFamily(
            curves,
            clampedMolecularWeight,
            plainSurfaceNucleateBoilingCoefficient,
            useLogBasis: false,
            useLogY: false);
    }

    public static double FinnedTubeSurfaceFactorForMixedHydrocarbon(
        double boilingRangeF,
        double plainSurfaceNucleateBoilingCoefficient)
    {
        var curves = MixedHydrocarbonFinnedTubeSurfaceFactorCurves;
        var clampedBoilingRange = Math.Clamp(boilingRangeF, curves[0].Basis, curves[^1].Basis);
        return InterpolateCurveFamily(
            curves,
            clampedBoilingRange,
            plainSurfaceNucleateBoilingCoefficient,
            useLogBasis: false,
            useLogY: false);
    }

    public static double FinEfficiencyFactor(
        double tubeMaterialThermalConductivityBtuHrFtF,
        double heatFluxBtuHrFt2)
    {
        var curves = FinEfficiencyCurves;
        var clampedConductivity = Math.Clamp(tubeMaterialThermalConductivityBtuHrFtF, curves[0].Basis, curves[^1].Basis);
        return InterpolateCurveFamily(
            curves,
            clampedConductivity,
            heatFluxBtuHrFt2,
            useLogBasis: true,
            useLogY: false);
    }

    public static double SingleTubeNucleateBoilingReferenceCoefficient(
        double criticalPressurePsia,
        double heatFluxBtuHrFt2)
    {
        var curves = SingleTubeNucleateBoilingCoefficientCurves;
        var clampedCriticalPressure = Math.Clamp(criticalPressurePsia, curves[0].CriticalPressurePsia, curves[^1].CriticalPressurePsia);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedCriticalPressure > right.CriticalPressurePsia)
            {
                continue;
            }

            var leftValue = InterpolateLogLog(left.Points, heatFluxBtuHrFt2);
            var rightValue = InterpolateLogLog(right.Points, heatFluxBtuHrFt2);
            var fraction = (Math.Log(clampedCriticalPressure) - Math.Log(left.CriticalPressurePsia)) /
                           (Math.Log(right.CriticalPressurePsia) - Math.Log(left.CriticalPressurePsia));
            return Math.Exp(Math.Log(leftValue) + fraction * (Math.Log(rightValue) - Math.Log(leftValue)));
        }

        return InterpolateLogLog(curves[^1].Points, heatFluxBtuHrFt2);
    }

    public static double NucleateBoilingPressureCorrectionFactor(double criticalPressurePsia, double operatingPressurePsia)
    {
        var reducedPressure = operatingPressurePsia / Math.Max(criticalPressurePsia, 1e-12);
        return InterpolateLogLog(NucleateBoilingPressureCorrectionPoints, reducedPressure);
    }

    public static double NucleateBoilingEffectiveMinimumHeatFlux(double boilingRangeF)
    {
        if (boilingRangeF <= 0d)
        {
            return 1_000d;
        }

        return Math.Min(24_858d / Math.Pow(boilingRangeF, 0.8d), 1_000d);
    }

    public static double EffectiveTemperatureRange(double vaporToLiquidDensityRatio)
    {
        var clampedRatio = Math.Clamp(vaporToLiquidDensityRatio, 0.01d, 0.20d);
        return InterpolateLinear(EffectiveTemperatureRangePoints, clampedRatio);
    }

    public static double MixtureCorrectionFactor(double heatFluxRatio, double boilingRangeParameter)
    {
        var curves = MixtureCorrectionCurves;
        var clampedHeatFluxRatio = Math.Clamp(heatFluxRatio, curves[0].HeatFluxRatio, curves[^1].HeatFluxRatio);

        for (var i = 0; i < curves.Length - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (clampedHeatFluxRatio > right.HeatFluxRatio)
            {
                continue;
            }

            var leftValue = InterpolateLinear(left.Points, boilingRangeParameter);
            var rightValue = InterpolateLinear(right.Points, boilingRangeParameter);
            var fraction = (Math.Log(clampedHeatFluxRatio) - Math.Log(left.HeatFluxRatio)) /
                           (Math.Log(right.HeatFluxRatio) - Math.Log(left.HeatFluxRatio));
            return leftValue + fraction * (rightValue - leftValue);
        }

        return InterpolateLinear(curves[^1].Points, boilingRangeParameter);
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
        var clamped = Math.Clamp(x, points[0].X, points[^1].X);
        for (var i = 0; i < points.Count - 1; i++)
        {
            var left = points[i];
            var right = points[i + 1];
            if (clamped > right.X)
            {
                continue;
            }

            var fraction = (clamped - left.X) / (right.X - left.X);
            return left.Y + fraction * (right.Y - left.Y);
        }

        return points[^1].Y;
    }

    private static double InterpolateLogXLinearY(IReadOnlyList<ChartPoint> points, double x)
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
            return left.Y + fraction * (right.Y - left.Y);
        }

        return points[^1].Y;
    }

    private static double InterpolateCurveFamily(
        IReadOnlyList<CurveFamily> curves,
        double basis,
        double x,
        bool useLogBasis,
        bool useLogY)
    {
        for (var i = 0; i < curves.Count - 1; i++)
        {
            var left = curves[i];
            var right = curves[i + 1];
            if (basis > right.Basis)
            {
                continue;
            }

            var leftValue = useLogY ? InterpolateLogLog(left.Points, x) : InterpolateLinear(left.Points, x);
            var rightValue = useLogY ? InterpolateLogLog(right.Points, x) : InterpolateLinear(right.Points, x);
            var fraction = useLogBasis
                ? (Math.Log(basis) - Math.Log(left.Basis)) / (Math.Log(right.Basis) - Math.Log(left.Basis))
                : (basis - left.Basis) / (right.Basis - left.Basis);
            return leftValue + fraction * (rightValue - leftValue);
        }

        return useLogY ? InterpolateLogLog(curves[^1].Points, x) : InterpolateLinear(curves[^1].Points, x);
    }

    private static readonly HeatFluxCurve[] SingleTubeMaximumHeatFluxCurves =
    [
        new(200d, [new(0.001d, 13_000d), new(0.003d, 24_000d), new(0.01d, 34_000d), new(0.03d, 45_000d), new(0.10d, 56_000d), new(0.30d, 70_000d), new(0.60d, 60_000d), new(0.80d, 35_000d), new(1.0d, 10_000d)]),
        new(400d, [new(0.001d, 30_000d), new(0.003d, 50_000d), new(0.01d, 75_000d), new(0.03d, 100_000d), new(0.10d, 130_000d), new(0.30d, 160_000d), new(0.60d, 140_000d), new(0.80d, 80_000d), new(1.0d, 10_000d)]),
        new(600d, [new(0.001d, 50_000d), new(0.003d, 80_000d), new(0.01d, 110_000d), new(0.03d, 150_000d), new(0.10d, 190_000d), new(0.30d, 230_000d), new(0.60d, 200_000d), new(0.80d, 105_000d), new(1.0d, 10_000d)]),
        new(1_000d, [new(0.001d, 70_000d), new(0.003d, 110_000d), new(0.01d, 160_000d), new(0.03d, 220_000d), new(0.10d, 280_000d), new(0.30d, 340_000d), new(0.60d, 300_000d), new(0.80d, 140_000d), new(1.0d, 10_000d)]),
        new(1_400d, [new(0.001d, 95_000d), new(0.003d, 150_000d), new(0.01d, 220_000d), new(0.03d, 300_000d), new(0.10d, 380_000d), new(0.30d, 470_000d), new(0.60d, 420_000d), new(0.80d, 170_000d), new(1.0d, 10_000d)]),
        new(2_000d, [new(0.001d, 130_000d), new(0.003d, 210_000d), new(0.01d, 320_000d), new(0.03d, 420_000d), new(0.10d, 520_000d), new(0.30d, 650_000d), new(0.60d, 590_000d), new(0.80d, 220_000d), new(1.0d, 10_000d)]),
        new(3_200d, [new(0.001d, 190_000d), new(0.003d, 320_000d), new(0.01d, 500_000d), new(0.03d, 650_000d), new(0.10d, 800_000d), new(0.30d, 1_000_000d), new(0.60d, 900_000d), new(0.80d, 300_000d), new(1.0d, 10_000d)])
    ];

    private static readonly BundleCorrectionCurve[] BundleCorrectionCurves =
    [
        new(1.10d, [new(2d, 1.0d), new(10d, 0.72d), new(100d, 0.42d), new(1_000d, 0.19d), new(10_000d, 0.015d)]),
        new(1.33d, [new(2d, 1.0d), new(10d, 0.74d), new(100d, 0.47d), new(1_000d, 0.24d), new(10_000d, 0.025d)]),
        new(1.50d, [new(2d, 1.0d), new(10d, 0.76d), new(100d, 0.52d), new(1_000d, 0.29d), new(10_000d, 0.04d)]),
        new(2.00d, [new(2d, 1.0d), new(10d, 0.82d), new(100d, 0.60d), new(1_000d, 0.38d), new(10_000d, 0.08d)]),
        new(3.00d, [new(2d, 1.0d), new(10d, 0.90d), new(100d, 0.70d), new(1_000d, 0.50d), new(10_000d, 0.10d)])
    ];

    private static readonly BundleCorrectionCurve[] BundleNucleateBoilingCorrectionCurves =
    [
        new(1.10d, [new(1d, 1.0d), new(10d, 1.20d), new(100d, 1.60d), new(300d, 2.10d), new(600d, 2.50d), new(1_000d, 2.50d)]),
        new(1.20d, [new(1d, 1.0d), new(10d, 1.17d), new(100d, 1.55d), new(400d, 2.15d), new(800d, 2.50d), new(1_500d, 2.50d)]),
        new(1.50d, [new(1d, 1.0d), new(10d, 1.12d), new(100d, 1.45d), new(700d, 2.10d), new(1_500d, 2.50d), new(2_500d, 2.50d)]),
        new(2.00d, [new(1d, 1.0d), new(10d, 1.08d), new(100d, 1.32d), new(1_000d, 1.85d), new(3_000d, 2.50d), new(5_000d, 2.50d)]),
        new(3.00d, [new(1d, 1.0d), new(10d, 1.04d), new(100d, 1.20d), new(1_000d, 1.60d), new(3_000d, 2.00d), new(10_000d, 2.50d)])
    ];

    private static readonly HeatFluxCurve[] VerticalThermosiphonChokeReferenceHeatFluxCurves =
    [
        new(200d, [new(0.001d, 6_000d), new(0.003d, 7_500d), new(0.01d, 9_500d), new(0.03d, 12_000d), new(0.10d, 15_000d), new(0.20d, 15_500d), new(0.40d, 13_500d), new(0.60d, 9_000d), new(0.80d, 4_000d), new(1.0d, 1_000d)]),
        new(300d, [new(0.001d, 8_000d), new(0.003d, 10_000d), new(0.01d, 13_000d), new(0.03d, 16_000d), new(0.10d, 20_000d), new(0.20d, 22_000d), new(0.40d, 20_000d), new(0.60d, 13_000d), new(0.80d, 5_000d), new(1.0d, 1_000d)]),
        new(400d, [new(0.001d, 10_000d), new(0.003d, 13_000d), new(0.01d, 16_000d), new(0.03d, 20_000d), new(0.10d, 25_000d), new(0.20d, 27_000d), new(0.40d, 24_000d), new(0.60d, 15_000d), new(0.80d, 6_000d), new(1.0d, 1_000d)]),
        new(500d, [new(0.001d, 12_000d), new(0.003d, 15_000d), new(0.01d, 19_000d), new(0.03d, 24_000d), new(0.10d, 30_000d), new(0.20d, 32_000d), new(0.40d, 28_000d), new(0.60d, 17_000d), new(0.80d, 7_000d), new(1.0d, 1_000d)]),
        new(700d, [new(0.001d, 15_000d), new(0.003d, 19_000d), new(0.01d, 24_000d), new(0.03d, 30_000d), new(0.10d, 38_000d), new(0.20d, 42_000d), new(0.40d, 37_000d), new(0.60d, 22_000d), new(0.80d, 9_000d), new(1.0d, 1_000d)]),
        new(1_000d, [new(0.001d, 18_000d), new(0.003d, 23_000d), new(0.01d, 30_000d), new(0.03d, 38_000d), new(0.10d, 50_000d), new(0.20d, 54_000d), new(0.40d, 48_000d), new(0.60d, 28_000d), new(0.80d, 11_000d), new(1.0d, 1_000d)]),
        new(2_000d, [new(0.001d, 28_000d), new(0.003d, 36_000d), new(0.01d, 45_000d), new(0.03d, 58_000d), new(0.10d, 70_000d), new(0.20d, 75_000d), new(0.40d, 65_000d), new(0.60d, 40_000d), new(0.80d, 15_000d), new(1.0d, 1_000d)]),
        new(3_200d, [new(0.001d, 35_000d), new(0.003d, 45_000d), new(0.01d, 60_000d), new(0.03d, 75_000d), new(0.10d, 90_000d), new(0.20d, 100_000d), new(0.40d, 85_000d), new(0.60d, 52_000d), new(0.80d, 20_000d), new(1.0d, 1_000d)])
    ];

    private static readonly TubeGeometryCorrectionCurve[] VerticalThermosiphonTubeGeometryCorrectionCurves =
    [
        new(6d, [new(0.35d, 0.40d), new(0.45d, 0.65d), new(0.60d, 1.00d), new(0.80d, 1.50d), new(1.00d, 2.00d)]),
        new(8d, [new(0.35d, 0.35d), new(0.50d, 0.65d), new(0.70d, 1.00d), new(0.95d, 1.50d), new(1.20d, 2.00d)]),
        new(10d, [new(0.35d, 0.30d), new(0.55d, 0.65d), new(0.80d, 1.00d), new(1.10d, 1.50d), new(1.40d, 2.00d)]),
        new(12d, [new(0.35d, 0.25d), new(0.60d, 0.65d), new(0.90d, 1.00d), new(1.25d, 1.50d), new(1.60d, 2.00d)]),
        new(16d, [new(0.35d, 0.22d), new(0.70d, 0.65d), new(1.05d, 1.00d), new(1.50d, 1.50d), new(1.90d, 2.00d)]),
        new(20d, [new(0.35d, 0.20d), new(0.80d, 0.65d), new(1.20d, 1.00d), new(1.75d, 1.50d), new(2.20d, 2.00d)])
    ];

    private static readonly HeatFluxCurve[] VerticalThermosiphonNaturalConvectionCoefficientCurves =
    [
        new(300d, [new(0d, 30d), new(5_000d, 43d), new(10_000d, 50d), new(25_000d, 58d), new(50_000d, 66d), new(75_000d, 70d), new(100_000d, 73d)]),
        new(400d, [new(0d, 35d), new(5_000d, 50d), new(10_000d, 58d), new(25_000d, 70d), new(50_000d, 82d), new(75_000d, 87d), new(100_000d, 90d)]),
        new(600d, [new(0d, 45d), new(5_000d, 67d), new(10_000d, 78d), new(25_000d, 95d), new(50_000d, 108d), new(75_000d, 115d), new(100_000d, 120d)]),
        new(800d, [new(0d, 55d), new(5_000d, 80d), new(10_000d, 95d), new(25_000d, 115d), new(50_000d, 135d), new(75_000d, 145d), new(100_000d, 150d)]),
        new(1_000d, [new(0d, 65d), new(5_000d, 95d), new(10_000d, 110d), new(25_000d, 140d), new(50_000d, 165d), new(75_000d, 178d), new(100_000d, 185d)]),
        new(1_500d, [new(0d, 95d), new(5_000d, 145d), new(10_000d, 165d), new(25_000d, 200d), new(50_000d, 225d), new(75_000d, 240d), new(100_000d, 255d)])
    ];

    private static readonly HeatFluxCurve[] SingleTubeNucleateBoilingCoefficientCurves =
    [
        new(200d, [new(1_000d, 55d), new(2_000d, 85d), new(5_000d, 170d), new(10_000d, 280d), new(20_000d, 470d), new(50_000d, 820d), new(100_000d, 1_150d)]),
        new(300d, [new(1_000d, 70d), new(2_000d, 105d), new(5_000d, 210d), new(10_000d, 340d), new(20_000d, 560d), new(50_000d, 1_050d), new(100_000d, 1_500d)]),
        new(400d, [new(1_000d, 82d), new(2_000d, 125d), new(5_000d, 260d), new(10_000d, 420d), new(20_000d, 690d), new(50_000d, 1_230d), new(100_000d, 1_750d)]),
        new(600d, [new(1_000d, 98d), new(2_000d, 150d), new(5_000d, 310d), new(10_000d, 500d), new(20_000d, 820d), new(50_000d, 1_500d), new(100_000d, 2_050d)]),
        new(800d, [new(1_000d, 110d), new(2_000d, 175d), new(5_000d, 360d), new(10_000d, 580d), new(20_000d, 950d), new(50_000d, 1_750d), new(100_000d, 2_350d)]),
        new(1_000d, [new(1_000d, 125d), new(2_000d, 200d), new(5_000d, 420d), new(10_000d, 670d), new(20_000d, 1_050d), new(50_000d, 1_950d), new(100_000d, 2_600d)]),
        new(3_200d, [new(2_000d, 310d), new(5_000d, 600d), new(10_000d, 950d), new(20_000d, 1_600d), new(50_000d, 3_000d), new(100_000d, 4_900d)])
    ];

    private static readonly CurveFamily[] PureHydrocarbonFinnedTubeSurfaceFactorCurves =
    [
        new(50d, [new(35d, 3.6d), new(50d, 3.0d), new(100d, 2.2d), new(150d, 1.8d), new(250d, 1.55d), new(500d, 1.35d)]),
        new(70d, [new(35d, 3.0d), new(50d, 2.55d), new(100d, 1.85d), new(150d, 1.55d), new(250d, 1.38d), new(500d, 1.22d)]),
        new(100d, [new(35d, 2.55d), new(50d, 2.15d), new(100d, 1.60d), new(150d, 1.35d), new(250d, 1.22d), new(500d, 1.12d)]),
        new(150d, [new(35d, 2.20d), new(50d, 1.85d), new(100d, 1.42d), new(150d, 1.23d), new(250d, 1.12d), new(500d, 1.04d)]),
        new(200d, [new(35d, 1.85d), new(50d, 1.60d), new(100d, 1.28d), new(150d, 1.14d), new(250d, 1.05d), new(500d, 0.98d)])
    ];

    private static readonly CurveFamily[] MixedHydrocarbonFinnedTubeSurfaceFactorCurves =
    [
        new(20d, [new(35d, 2.55d), new(50d, 2.30d), new(100d, 1.95d), new(150d, 1.72d), new(250d, 1.58d), new(500d, 1.55d)]),
        new(50d, [new(35d, 1.80d), new(50d, 1.68d), new(100d, 1.50d), new(150d, 1.35d), new(250d, 1.28d), new(500d, 1.25d)]),
        new(100d, [new(35d, 1.25d), new(50d, 1.20d), new(100d, 1.12d), new(150d, 1.08d), new(250d, 1.06d), new(500d, 1.05d)]),
        new(200d, [new(35d, 0.78d), new(50d, 0.80d), new(100d, 0.86d), new(150d, 0.90d), new(250d, 0.92d), new(500d, 0.93d)])
    ];

    private static readonly CurveFamily[] FinEfficiencyCurves =
    [
        new(9d, [new(1_000d, 0.63d), new(2_000d, 0.55d), new(5_000d, 0.47d), new(10_000d, 0.41d), new(20_000d, 0.34d)]),
        new(28d, [new(1_000d, 0.84d), new(2_000d, 0.77d), new(5_000d, 0.66d), new(10_000d, 0.58d), new(20_000d, 0.49d)]),
        new(65d, [new(1_000d, 1.00d), new(2_000d, 0.94d), new(5_000d, 0.82d), new(10_000d, 0.73d), new(20_000d, 0.65d)])
    ];

    private static readonly ChartPoint[] NucleateBoilingPressureCorrectionPoints =
    [
        new(0.001d, 0.48d),
        new(0.003d, 0.58d),
        new(0.01d, 0.70d),
        new(0.03d, 0.90d),
        new(0.05d, 1.05d),
        new(0.10d, 1.32d),
        new(0.20d, 1.80d),
        new(0.40d, 2.55d),
        new(0.60d, 3.45d),
        new(0.80d, 5.00d),
        new(1.00d, 9.00d)
    ];

    private static readonly ChartPoint[] EffectiveTemperatureRangePoints =
    [
        new(0.01d, 250d),
        new(0.02d, 195d),
        new(0.03d, 158d),
        new(0.04d, 130d),
        new(0.05d, 108d),
        new(0.06d, 90d),
        new(0.07d, 73d),
        new(0.08d, 60d),
        new(0.09d, 50d),
        new(0.10d, 41d),
        new(0.12d, 30d),
        new(0.14d, 22d),
        new(0.16d, 16d),
        new(0.18d, 12d),
        new(0.20d, 13d)
    ];

    private static readonly MixtureCorrectionCurve[] MixtureCorrectionCurves =
    [
        new(1.1d, [new(0d, 1.0d), new(0.10d, 0.78d), new(0.20d, 0.66d), new(0.40d, 0.53d), new(0.60d, 0.46d), new(0.80d, 0.40d), new(1.00d, 0.36d), new(1.20d, 0.33d)]),
        new(2.0d, [new(0d, 1.0d), new(0.10d, 0.70d), new(0.20d, 0.55d), new(0.40d, 0.41d), new(0.60d, 0.35d), new(0.80d, 0.30d), new(1.00d, 0.26d), new(1.20d, 0.24d)]),
        new(10.0d, [new(0d, 1.0d), new(0.10d, 0.61d), new(0.20d, 0.43d), new(0.40d, 0.30d), new(0.60d, 0.25d), new(0.80d, 0.21d), new(1.00d, 0.19d), new(1.20d, 0.17d)]),
        new(50.0d, [new(0d, 1.0d), new(0.10d, 0.55d), new(0.20d, 0.37d), new(0.40d, 0.25d), new(0.60d, 0.20d), new(0.80d, 0.16d), new(1.00d, 0.14d), new(1.20d, 0.12d)])
    ];

    private sealed record ChartPoint(double X, double Y);

    private sealed record HeatFluxCurve(double CriticalPressurePsia, IReadOnlyList<ChartPoint> Points);

    private sealed record BundleCorrectionCurve(double PitchRatio, IReadOnlyList<ChartPoint> Points);

    private sealed record TubeGeometryCorrectionCurve(double TubeLengthFeet, IReadOnlyList<ChartPoint> Points);

    private sealed record CurveFamily(double Basis, IReadOnlyList<ChartPoint> Points);

    private sealed record MixtureCorrectionCurve(double HeatFluxRatio, IReadOnlyList<ChartPoint> Points);
}
