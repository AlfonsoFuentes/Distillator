using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.HeatExchangers.Design;
using UnitSystem;

namespace Distillator.HeatExchangerDesign.Tests;

public sealed class DesignPracticesShellAndTubeCorrelationTests
{
    [Fact]
    public void Dp09dTubeSideWaterCoefficient_FollowsPage13WaterEquation()
    {
        var coefficient = Dp09dShellAndTubeCorrelations.TubeSideWaterCoefficientCorrectedToOutsideArea(
            velocityFeetPerSecond: 4.313d,
            tubeInsideDiameterInches: 0.305d,
            tubeOutsideDiameterInches: 0.375d,
            tubeBulkTemperatureFahrenheit: 95d);

        Assert.Equal(1173.22d, coefficient, precision: 2);
    }

    [Fact]
    public void Dp09fTubeSideWaterCoefficient_FollowsTable2Page24Equation()
    {
        var coefficient = Dp09fTubeSideWaterCorrelation.CalculateInsideCoefficientCorrectedToOutsideArea(
            velocityFeetPerSecond: 6.5865052301248541d,
            tubeInsideDiameterInches: 0.555d,
            tubeOutsideDiameterInches: 0.625d,
            tubeBulkTemperatureFahrenheit: 158d);

        Assert.Equal(1643.169106d, coefficient, precision: 6);
    }

    [Fact]
    public void Dp09fTubeSideWaterPressureDrop_FollowsTable2Page24Equation()
    {
        var pressureDropFoulingFactor = Dp09fTubeSideWaterCorrelation.EstimatePlainSteelPressureDropFoulingFactor(
            tubeOutsideDiameterInches: 0.625d,
            tubeInsideDiameterInches: 0.555d);
        var pressureDrop = Dp09fTubeSideWaterCorrelation.CalculateTubeSidePressureDropPsi(
            velocityFeetPerSecond: 6.5865052301248541d,
            tubeInsideDiameterInches: 0.555d,
            tubeLengthFeet: 4d,
            shellPasses: 1d,
            tubePasses: 2d,
            pressureDropFoulingFactor);

        Assert.Equal(1.291375714d, pressureDropFoulingFactor, precision: 6);
        Assert.Equal(4.039154188d, pressureDrop, precision: 6);
    }

    [Fact]
    public void Dp09fTubeSideWaterPressureDropFoulingFactor_SelectsSteelRouteForCarbonSteel()
    {
        var factor = Dp09fTubeSideWaterCorrelation.EstimatePressureDropFoulingFactor(
            ShellAndTubeTubeMaterial.CarbonSteel,
            tubeOutsideDiameterInches: 0.625d,
            tubeInsideDiameterInches: 0.555d);

        Assert.False(factor.RequiresMaterialReview);
        Assert.Equal(1.291375714d, factor.Value, precision: 6);
        Assert.Contains("plain steel", factor.Source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dp09fTubeSideWaterPressureDropFoulingFactor_FlagsAlloyRouteForVerifiedLookup()
    {
        var factor = Dp09fTubeSideWaterCorrelation.EstimatePressureDropFoulingFactor(
            ShellAndTubeTubeMaterial.Type316StainlessSteel,
            tubeOutsideDiameterInches: 0.625d,
            tubeInsideDiameterInches: 0.555d);

        Assert.True(factor.RequiresMaterialReview);
        Assert.Equal(1d, factor.Value, precision: 6);
        Assert.Contains("nonferrous alloy", factor.Source, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1000d, 0.016d)]
    [InlineData(3000d, 0.009984923d)]
    [InlineData(10000d, 0.009015742d)]
    public void Dp09dTubeFrictionFactor_FollowsFigure18RangeEquations(double reynolds, double expected)
    {
        var friction = Dp09dShellAndTubeCorrelations.TubeSideIsothermalFrictionFactor(reynolds);

        Assert.Equal(expected, friction, precision: 5);
    }

    [Theory]
    [InlineData(10_000d, 0.01d, 1.86d)]
    [InlineData(10_000d, 1d, 1.00d)]
    [InlineData(10_000d, 100d, 0.54d)]
    [InlineData(2_000d, 0.01d, 2.51d)]
    public void Dp09dViscosityGradientCorrection_FollowsFigure19Trend(
        double reynolds,
        double viscosityRatio,
        double expected)
    {
        var correction = Dp09dShellAndTubeCorrelations.TubeSideViscosityGradientCorrectionFactor(
            reynolds,
            viscosityRatio);

        Assert.Equal(expected, correction, precision: 2);
    }

    [Theory]
    [InlineData(2_000d, 1e5d, 1.00d)]
    [InlineData(2_000d, 1e9d, 1.75d)]
    [InlineData(8_000d, 1e9d, 1.08d)]
    [InlineData(4_000d, 1e8d, 1.32d)]
    public void Dp09dPressureDropNaturalConvectionCorrection_FollowsFigure110Trend(
        double reynolds,
        double grashofPrandtlViscosityRatio,
        double expected)
    {
        var correction = Dp09dShellAndTubeCorrelations.TubeSidePressureDropNaturalConvectionCorrectionFactor(
            reynolds,
            grashofPrandtlViscosityRatio);

        Assert.Equal(expected, correction, precision: 2);
    }

    [Fact]
    public void Dp09dPressureDropNaturalConvectionCorrection_ReturnsOneBelowFigure110Range()
    {
        var correction = Dp09dShellAndTubeCorrelations.TubeSidePressureDropNaturalConvectionCorrectionFactor(
            reynolds: 13_165d,
            grashofPrandtlViscosityRatio: 27_584d);

        Assert.Equal(1d, correction, precision: 6);
    }

    [Fact]
    public void Dp09dFigure14ShellSideHeatTransferJFactor_InterpolatesLayoutCurves()
    {
        var square = Dp09dShellAndTubeCorrelations.DP_j_ShellSideHeatTransferFactor(
            10_000d,
            ShellAndTubeTubeLayout.Square);
        var triangular = Dp09dShellAndTubeCorrelations.DP_j_ShellSideHeatTransferFactor(
            10_000d,
            ShellAndTubeTubeLayout.Triangular);

        Assert.True(triangular > square);
        Assert.Equal(0.014d, square, precision: 3);
        Assert.Equal(0.018d, triangular, precision: 3);
    }

    [Fact]
    public void Dp09dBaffleSpacingCorrection_FollowsFigure12BundleDiameterRatio()
    {
        var correction = Dp09dShellAndTubeCorrelations.DP_SC_BaffleSpacingCorrection(
            DP_LBCC_BaffleSpacingInches: 6d,
            DP_DOTL_TubeBundleDiameterInches: 7.829d);

        Assert.Equal(1.22d, correction, precision: 2);
    }

    [Theory]
    [InlineData(400d, 0.88d)]
    [InlineData(6_355d, 1.00d)]
    public void Dp09dReynoldsNumberCorrection_FollowsFigure13(
        double DP_Rext_TotalFlowReynoldsNumber,
        double expected)
    {
        var correction = Dp09dShellAndTubeCorrelations.DP_RC_ReynoldsNumberCorrection(
            DP_Rext_TotalFlowReynoldsNumber);

        Assert.Equal(expected, correction, precision: 2);
    }

    [Fact]
    public void Dp09dFigure14ShellSideFrictionFactor_FollowsPitchRatioTrend()
    {
        var tightPitch = Dp09dShellAndTubeCorrelations.DP_f_ShellSideFrictionFactor(
            10_000d,
            ShellAndTubeTubeLayout.Triangular,
            1.25d);
        var widerPitch = Dp09dShellAndTubeCorrelations.DP_f_ShellSideFrictionFactor(
            10_000d,
            ShellAndTubeTubeLayout.Triangular,
            1.33d);

        Assert.Equal(0.22d, tightPitch, precision: 2);
        Assert.Equal(0.18d, widerPitch, precision: 2);
        Assert.True(tightPitch > widerPitch);
    }

    [Fact]
    public void Dp09dFigure14ShellSideFrictionFactor_FollowsLayoutTrend()
    {
        var square = Dp09dShellAndTubeCorrelations.DP_f_ShellSideFrictionFactor(
            10_000d,
            ShellAndTubeTubeLayout.Square,
            1.25d);
        var triangular = Dp09dShellAndTubeCorrelations.DP_f_ShellSideFrictionFactor(
            10_000d,
            ShellAndTubeTubeLayout.Triangular,
            1.25d);

        Assert.True(triangular > square);
        Assert.Equal(0.16d, square, precision: 2);
        Assert.Equal(0.22d, triangular, precision: 2);
    }

    [Fact]
    public void Dp09dNormalCrossflowFraction_FollowsFigure11RearHeadCurves()
    {
        var fixedTubesheet = Dp09dShellAndTubeCorrelations.NormalCrossflowFraction(
            tubeBundleDiameterInches: 30d,
            passPartitionRatio: 0.25d,
            Dp09dRearHeadType.FixedTubesheet);
        var pullThrough = Dp09dShellAndTubeCorrelations.NormalCrossflowFraction(
            tubeBundleDiameterInches: 30d,
            passPartitionRatio: 0.25d,
            Dp09dRearHeadType.PullThroughFloatingHead);

        Assert.Equal(0.71d, fixedTubesheet, precision: 2);
        Assert.Equal(0.53d, pullThrough, precision: 2);
        Assert.True(fixedTubesheet > pullThrough);
    }

    [Fact]
    public void Dp09dTemperatureCorrectionFactor_ReducesCounterCurrentLmtdForOneShellPass()
    {
        var correctionFactor = Dp09dShellAndTubeCorrelations.OneShellPassTemperatureCorrectionFactor(
            hotInletTemperatureF: 300d,
            hotOutletTemperatureF: 200d,
            coldInletTemperatureF: 100d,
            coldOutletTemperatureF: 180d);

        Assert.InRange(correctionFactor, 0.80d, 0.95d);
    }

    [Fact]
    public void Dp09dTemperatureCorrectionFactor_HandlesEqualCapacityRatio()
    {
        var correctionFactor = Dp09dShellAndTubeCorrelations.OneShellPassTemperatureCorrectionFactor(
            hotInletTemperatureF: 300d,
            hotOutletTemperatureF: 220d,
            coldInletTemperatureF: 100d,
            coldOutletTemperatureF: 180d);

        Assert.InRange(correctionFactor, 0.85d, 1d);
    }

    [Fact]
    public void Dp09dTemperatureCorrectionFactor_ImprovesForMultipleShellPasses()
    {
        var oneShell = Dp09dShellAndTubeCorrelations.TemperatureCorrectionFactor(
            shellPasses: 1d,
            hotInletTemperatureF: 300d,
            hotOutletTemperatureF: 200d,
            coldInletTemperatureF: 100d,
            coldOutletTemperatureF: 180d);
        var twoShells = Dp09dShellAndTubeCorrelations.TemperatureCorrectionFactor(
            shellPasses: 2d,
            hotInletTemperatureF: 300d,
            hotOutletTemperatureF: 200d,
            coldInletTemperatureF: 100d,
            coldOutletTemperatureF: 180d);

        Assert.True(twoShells > oneShell);
        Assert.InRange(twoShells, 0.90d, 1.0d);
    }

    [Theory]
    [InlineData(48d, 47.5d)]
    [InlineData(54d, 53.375d)]
    [InlineData(66d, 65.25d)]
    public void Dp09cOuterTubeLimit_UsesFixedTubesheetAndUTubeTable(double shellInsideDiameter, double expected)
    {
        var isTableBased = Dp09cShellAndTubeCatalog.TryGetTableOuterTubeLimitInches(
            shellInsideDiameter,
            ShellAndTubeTubeConstruction.UTube,
            Dp09dRearHeadType.PullThroughFloatingHead,
            out var outerTubeLimit);

        Assert.True(isTableBased);
        Assert.Equal(expected, outerTubeLimit, precision: 6);
    }

    [Theory]
    [InlineData(22d, 20.75d)]
    [InlineData(24d, 22.625d)]
    [InlineData(30d, 28.375d)]
    [InlineData(40d, 38.25d)]
    [InlineData(54d, 52.125d)]
    public void Dp09cOuterTubeLimit_UsesSplitRingFloatingHeadTable(double shellInsideDiameter, double expected)
    {
        var isTableBased = Dp09cShellAndTubeCatalog.TryGetTableOuterTubeLimitInches(
            shellInsideDiameter,
            ShellAndTubeTubeConstruction.Straight,
            Dp09dRearHeadType.SplitRingFloatingHead,
            out var outerTubeLimit);

        Assert.True(isTableBased);
        Assert.Equal(expected, outerTubeLimit, precision: 6);
    }

    [Fact]
    public void Dp09cOuterTubeLimit_IdentifiesPullThroughFloatingHeadAsFigureBased()
    {
        var isTableBased = Dp09cShellAndTubeCatalog.TryGetTableOuterTubeLimitInches(
            shellInsideDiameterInches: 30d,
            ShellAndTubeTubeConstruction.Straight,
            Dp09dRearHeadType.PullThroughFloatingHead,
            out var outerTubeLimit);

        Assert.False(isTableBased);
        Assert.True(outerTubeLimit > 0d);
    }

    [Fact]
    public void Dp09cFigure7PullThroughOtl_DecreasesAsDesignPressureIncreases()
    {
        var lowPressureOtl = Dp09cShellAndTubeCatalog.GetPullThroughFloatingHeadOuterTubeLimitInches(
            shellInsideDiameterInches: 40d,
            designPressurePsig: 150d);
        var highPressureOtl = Dp09cShellAndTubeCatalog.GetPullThroughFloatingHeadOuterTubeLimitInches(
            shellInsideDiameterInches: 40d,
            designPressurePsig: 600d);

        Assert.InRange(lowPressureOtl, 36d, 37d);
        Assert.InRange(highPressureOtl, 35d, 36d);
        Assert.True(lowPressureOtl > highPressureOtl);
    }

    [Fact]
    public void Dp09cFigure7PullThroughClearance_InterpolatesPressureCurves()
    {
        var lowPressureClearance = Dp09cShellAndTubeCatalog.GetPullThroughFloatingHeadDiametralClearanceInches(
            outerTubeLimitInches: 40d,
            designPressurePsig: 150d);
        var interpolatedClearance = Dp09cShellAndTubeCatalog.GetPullThroughFloatingHeadDiametralClearanceInches(
            outerTubeLimitInches: 40d,
            designPressurePsig: 375d);
        var highPressureClearance = Dp09cShellAndTubeCatalog.GetPullThroughFloatingHeadDiametralClearanceInches(
            outerTubeLimitInches: 40d,
            designPressurePsig: 600d);

        Assert.Equal(3.32d, lowPressureClearance, precision: 2);
        Assert.InRange(interpolatedClearance, 3.70d, 4.15d);
        Assert.Equal(4.65d, highPressureClearance, precision: 2);
    }

    [Fact]
    public void Dp09cFigure8ShellNozzleCorrection_FollowsChartExample()
    {
        var correction = Dp09cShellAndTubeCatalog.GetShellNozzleCorrectionFactor(
            shellInsideDiameterInches: 10d,
            outerTubeLimitInches: 7.25d,
            tubeOuterDiameterInches: 1d,
            shellNozzleInsideDiameterInches: 6d);

        Assert.InRange(correction, 0.94d, 0.97d);
    }

    [Fact]
    public void Dp09cFigure8ShellNozzleCorrection_DecreasesForLargerNozzleRatio()
    {
        var smallNozzle = Dp09cShellAndTubeCatalog.GetShellNozzleCorrectionFactor(
            shellInsideDiameterInches: 20d,
            outerTubeLimitInches: 18d,
            tubeOuterDiameterInches: 1d,
            shellNozzleInsideDiameterInches: 2d);
        var largeNozzle = Dp09cShellAndTubeCatalog.GetShellNozzleCorrectionFactor(
            shellInsideDiameterInches: 20d,
            outerTubeLimitInches: 18d,
            tubeOuterDiameterInches: 1d,
            shellNozzleInsideDiameterInches: 12d);

        Assert.True(smallNozzle > largeNozzle);
        Assert.InRange(smallNozzle, 0.98d, 1.0d);
        Assert.InRange(largeNozzle, 0.84d, 0.95d);
    }

    [Theory]
    [InlineData(8d, 4)]
    [InlineData(12d, 6)]
    [InlineData(24d, 8)]
    [InlineData(34d, 10)]
    [InlineData(44d, 12)]
    [InlineData(54d, 14)]
    [InlineData(60d, 16)]
    public void Dp09cMaximumTubePasses_FollowsTable4(double shellInsideDiameter, int expected)
    {
        var maximumPasses = Dp09cShellAndTubeCatalog.GetRecommendedMaximumTubePasses(shellInsideDiameter);

        Assert.Equal(expected, maximumPasses);
    }

    [Theory]
    [InlineData(0d, 0.30d)]
    [InlineData(6d, 0.73d)]
    [InlineData(10d, 0.90d)]
    [InlineData(16d, 1.00d)]
    public void Dp09dLowPrandtlCorrection_FollowsFigure17(double prandtl, double expected)
    {
        var correction = Dp09dShellAndTubeCorrelations.LowPrandtlNumberCorrection(prandtl);

        Assert.Equal(expected, correction, precision: 2);
    }

    [Fact]
    public void Dp09dShortTubeCorrection_InterpolatesFigure16()
    {
        var correction = Dp09dShellAndTubeCorrelations.ShortTubeCorrectionFactor(
            reynolds: 1_000d,
            lengthToInsideDiameter: 30d);

        Assert.InRange(correction, 0.055d, 0.070d);
    }

    [Fact]
    public void Dp09dNaturalConvectionFactor_InterpolatesFigure15()
    {
        var horizontal = Dp09dShellAndTubeCorrelations.NaturalConvectionFactor(
            grashofNumber: 10_000d,
            orientation: Dp09dTubeOrientation.Horizontal);
        var verticalShort = Dp09dShellAndTubeCorrelations.NaturalConvectionFactor(
            grashofNumber: 10_000d,
            orientation: Dp09dTubeOrientation.Vertical,
            lengthToInsideDiameter: 20d);
        var verticalLong = Dp09dShellAndTubeCorrelations.NaturalConvectionFactor(
            grashofNumber: 10_000d,
            orientation: Dp09dTubeOrientation.Vertical,
            lengthToInsideDiameter: 400d);

        Assert.Equal(80d, horizontal, precision: 6);
        Assert.Equal(300d, verticalShort, precision: 6);
        Assert.Equal(45d, verticalLong, precision: 6);
        Assert.True(verticalShort > horizontal);
        Assert.True(horizontal > verticalLong);
    }

    [Fact]
    public void Dp09fHorizontalBundleCondensingCoefficient_CanDisableVelocityCorrectionForPureComponents()
    {
        var properties = new DesignPracticesFluidProperties(
            ViscosityLbFtHr: 0.45d,
            CpBtuLbF: 1d,
            ThermalConductivityBtuHrFtF: 0.35d,
            DensityLbFt3: 60d);
        var baseCoefficient = Dp09fCondensationZoneModel.CalculateHorizontalBundleCondensingCoefficient(
            condensateMassFlowLbPerHour: 1_000d,
            condensingLengthFeet: 10d,
            condensateStreams: 20d,
            properties,
            vaporMassVelocityLbSecFt2: 10d,
            applyVaporMassVelocityCorrection: false);
        var wideCutCoefficient = Dp09fCondensationZoneModel.CalculateHorizontalBundleCondensingCoefficient(
            condensateMassFlowLbPerHour: 1_000d,
            condensingLengthFeet: 10d,
            condensateStreams: 20d,
            properties,
            vaporMassVelocityLbSecFt2: 10d);

        Assert.True(wideCutCoefficient > baseCoefficient);
        Assert.Equal(Math.Pow(2d, 0.70d), wideCutCoefficient / baseCoefficient, precision: 6);
    }

    [Fact]
    public void Dp09fHorizontalBundleCondensingAreaIteration_ConvergesRequiredAreaAndVaporVelocity()
    {
        var zones = new[]
        {
            new DesignPracticesCondensationZone("Sensible desuperheating zone", 100_000d, 320d, 300d, 90d, 100d, 210d),
            new DesignPracticesCondensationZone("Sensible-balance condensation zone", 800_000d, 300d, 300d, 100d, 150d, 175d),
            new DesignPracticesCondensationZone("Sensible subcooling zone", 100_000d, 300d, 260d, 150d, 170d, 120d)
        };
        var properties = new DesignPracticesFluidProperties(
            ViscosityLbFtHr: 0.45d,
            CpBtuLbF: 1d,
            ThermalConductivityBtuHrFtF: 0.35d,
            DensityLbFt3: 60d);

        var result = Dp09fCondensationZoneModel.IterateHorizontalBundleCondensingArea(
            zones,
            totalDutyBtuPerHour: 1_000_000d,
            installedAreaSquareFeet: 500d,
            shellFreeAreaSquareFeet: 2d,
            baseVaporFreeAreaFraction: 0.5d,
            vaporMassFlowLbPerHour: 10_000d,
            condensateMassFlowLbPerHour: 20_000d,
            condensingLengthFeet: 16d,
            condensateStreams: 24d,
            properties,
            vaporCoolingCoefficient: 80d,
            bottomFlowLiquidCoolingCoefficient: 90d,
            applyVaporMassVelocityCorrection: true);

        Assert.InRange(result.Iterations, 1, 12);
        Assert.True(result.RequiredAreaSquareFeet > 0d);
        Assert.True(result.CondensingCoefficientBtuHrFt2F > 0d);
        Assert.True(result.DutyWeightedCoefficientBtuHrFt2F > 0d);
        Assert.InRange(result.VaporFreeAreaFraction, 0.05d, 1d);
        Assert.True(result.VaporMassVelocityLbSecFt2 > 0d);
    }

    [Fact]
    public void Dp09fInsideTubeCondensingCoefficient_UsesEquivalentLiquidMassVelocity()
    {
        var equivalentMassVelocity = Dp09fCondensationZoneModel.CalculateEquivalentLiquidMassVelocity(
            averageLiquidMassVelocityLbHrFt2: 1_000d,
            averageVaporMassVelocityLbHrFt2: 2_000d,
            liquidDensityLbFt3: 50d,
            vaporDensityLbFt3: 2d);
        var properties = new DesignPracticesFluidProperties(
            ViscosityLbFtHr: 0.40d,
            CpBtuLbF: 0.80d,
            ThermalConductivityBtuHrFtF: 0.08d,
            DensityLbFt3: 40d);
        var coefficient = Dp09fCondensationZoneModel.CalculateInsideTubeCondensingCoefficient(
            equivalentMassVelocity,
            tubeInsideDiameterFeet: 0.05d,
            properties);

        Assert.Equal(11_000d, equivalentMassVelocity, precision: 6);
        Assert.True(coefficient > 0d);
    }

    [Fact]
    public void Dp09eSingleTubeMaximumHeatFlux_FollowsFigureA3SampleRange()
    {
        var heatFlux = Dp09eVaporizationCorrelations.SingleTubeMaximumHeatFlux(
            criticalPressurePsia: 456d,
            operatingPressurePsia: 45.6d);

        Assert.InRange(heatFlux, 130_000d, 210_000d);
    }

    [Fact]
    public void Dp09eBundleCorrectionFactor_FollowsFigureA4PitchRatioCurves()
    {
        var correction = Dp09eVaporizationCorrelations.BundleCorrectionFactor(
            tubeCount: 100d,
            pitchRatio: 1.5d);

        Assert.InRange(correction, 0.48d, 0.56d);
    }

    [Fact]
    public void Dp09eSingleTubeNucleateBoilingReferenceCoefficient_FollowsFigureA5SampleRange()
    {
        var lowFlux = Dp09eVaporizationCorrelations.SingleTubeNucleateBoilingReferenceCoefficient(
            criticalPressurePsia: 400d,
            heatFluxBtuHrFt2: 6_000d);
        var highFlux = Dp09eVaporizationCorrelations.SingleTubeNucleateBoilingReferenceCoefficient(
            criticalPressurePsia: 400d,
            heatFluxBtuHrFt2: 35_700d);

        Assert.InRange(lowFlux, 260d, 330d);
        Assert.InRange(highFlux, 900d, 1_100d);
        Assert.True(highFlux > lowFlux);
    }

    [Fact]
    public void Dp09eSingleTubeNucleateBoilingReferenceCoefficient_IncreasesWithCriticalPressure()
    {
        var lowCriticalPressure = Dp09eVaporizationCorrelations.SingleTubeNucleateBoilingReferenceCoefficient(
            criticalPressurePsia: 200d,
            heatFluxBtuHrFt2: 10_000d);
        var highCriticalPressure = Dp09eVaporizationCorrelations.SingleTubeNucleateBoilingReferenceCoefficient(
            criticalPressurePsia: 1_000d,
            heatFluxBtuHrFt2: 10_000d);

        Assert.InRange(lowCriticalPressure, 250d, 310d);
        Assert.InRange(highCriticalPressure, 630d, 710d);
        Assert.True(highCriticalPressure > lowCriticalPressure);
    }

    [Theory]
    [InlineData(1_000d, 10d, 0.70d)]
    [InlineData(1_000d, 100d, 1.32d)]
    [InlineData(1_000d, 600d, 3.45d)]
    public void Dp09eNucleateBoilingPressureCorrectionFactor_FollowsFigureA6(
        double criticalPressurePsia,
        double operatingPressurePsia,
        double expected)
    {
        var correction = Dp09eVaporizationCorrelations.NucleateBoilingPressureCorrectionFactor(
            criticalPressurePsia,
            operatingPressurePsia);

        Assert.Equal(expected, correction, precision: 2);
    }

    [Fact]
    public void Dp09eNucleateBoilingPressureCorrectionFactor_IncreasesWithReducedPressure()
    {
        var lowPressure = Dp09eVaporizationCorrelations.NucleateBoilingPressureCorrectionFactor(
            criticalPressurePsia: 1_000d,
            operatingPressurePsia: 10d);
        var highPressure = Dp09eVaporizationCorrelations.NucleateBoilingPressureCorrectionFactor(
            criticalPressurePsia: 1_000d,
            operatingPressurePsia: 400d);

        Assert.True(highPressure > lowPressure);
        Assert.InRange(highPressure, 2.4d, 2.7d);
    }

    [Fact]
    public void Dp09eEffectiveMinimumHeatFlux_FollowsProcedureLimit()
    {
        var moderateBoilingRange = Dp09eVaporizationCorrelations.NucleateBoilingEffectiveMinimumHeatFlux(64d);
        var narrowBoilingRange = Dp09eVaporizationCorrelations.NucleateBoilingEffectiveMinimumHeatFlux(1d);

        Assert.Equal(892d, moderateBoilingRange, precision: 0);
        Assert.Equal(1_000d, narrowBoilingRange, precision: 6);
    }

    [Fact]
    public void Dp09eEffectiveTemperatureRange_FollowsFigureA7()
    {
        var effectiveTemperatureRange = Dp09eVaporizationCorrelations.EffectiveTemperatureRange(0.06d);

        Assert.Equal(90d, effectiveTemperatureRange, precision: 1);
    }

    [Fact]
    public void Dp09eMixtureCorrectionFactor_FollowsFigureA8SampleRange()
    {
        var mixtureCorrection = Dp09eVaporizationCorrelations.MixtureCorrectionFactor(
            heatFluxRatio: 6.7d,
            boilingRangeParameter: 0.711d);

        Assert.InRange(mixtureCorrection, 0.23d, 0.30d);
    }

    [Fact]
    public void Dp09eMixtureCorrectionFactor_DecreasesWithWiderBoilingRangeParameter()
    {
        var narrowRange = Dp09eVaporizationCorrelations.MixtureCorrectionFactor(
            heatFluxRatio: 10d,
            boilingRangeParameter: 0.1d);
        var wideRange = Dp09eVaporizationCorrelations.MixtureCorrectionFactor(
            heatFluxRatio: 10d,
            boilingRangeParameter: 1.0d);

        Assert.True(wideRange < narrowRange);
    }

    [Fact]
    public void Dp09eBundleNucleateBoilingCorrectionFactor_FollowsFigureA9PitchTrend()
    {
        var tightPitch = Dp09eVaporizationCorrelations.BundleNucleateBoilingCorrectionFactor(
            tubeCount: 1_000d,
            pitchRatio: 1.1d);
        var widePitch = Dp09eVaporizationCorrelations.BundleNucleateBoilingCorrectionFactor(
            tubeCount: 1_000d,
            pitchRatio: 3.0d);

        Assert.True(tightPitch > widePitch);
        Assert.InRange(tightPitch, 2.45d, 2.50d);
        Assert.InRange(widePitch, 1.5d, 1.7d);
    }

    [Fact]
    public void Dp09eBundleNucleateBoilingCorrectionFactor_IncreasesWithTubeCount()
    {
        var smallBundle = Dp09eVaporizationCorrelations.BundleNucleateBoilingCorrectionFactor(
            tubeCount: 10d,
            pitchRatio: 1.5d);
        var largeBundle = Dp09eVaporizationCorrelations.BundleNucleateBoilingCorrectionFactor(
            tubeCount: 1_000d,
            pitchRatio: 1.5d);

        Assert.True(largeBundle > smallBundle);
        Assert.InRange(smallBundle, 1.05d, 1.20d);
        Assert.InRange(largeBundle, 2.2d, 2.4d);
    }

    [Fact]
    public void Dp09eBundleNucleateBoilingCorrectionFactor_CapsAtFigureA9Limit()
    {
        var correction = Dp09eVaporizationCorrelations.BundleNucleateBoilingCorrectionFactor(
            tubeCount: 10_000d,
            pitchRatio: 1.1d);

        Assert.Equal(2.5d, correction, precision: 6);
    }

    [Fact]
    public void Dp09eVerticalThermosiphonChokeReferenceMaximumHeatFlux_FollowsFigureA14SampleRange()
    {
        var heatFlux = Dp09eVaporizationCorrelations.VerticalThermosiphonChokeReferenceMaximumHeatFlux(
            criticalPressurePsia: 500d,
            operatingPressurePsia: 50d);

        Assert.InRange(heatFlux, 27_000d, 33_000d);
    }

    [Fact]
    public void Dp09eVerticalThermosiphonChokeReferenceMaximumHeatFlux_DecreasesNearCriticalPressure()
    {
        var moderateReducedPressure = Dp09eVaporizationCorrelations.VerticalThermosiphonChokeReferenceMaximumHeatFlux(
            criticalPressurePsia: 500d,
            operatingPressurePsia: 100d);
        var highReducedPressure = Dp09eVaporizationCorrelations.VerticalThermosiphonChokeReferenceMaximumHeatFlux(
            criticalPressurePsia: 500d,
            operatingPressurePsia: 450d);

        Assert.True(highReducedPressure < moderateReducedPressure);
        Assert.InRange(highReducedPressure, 1_000d, 5_000d);
    }

    [Fact]
    public void Dp09eVerticalThermosiphonTubeGeometryCorrectionFactor_FollowsFigureA15SampleRange()
    {
        var correction = Dp09eVaporizationCorrelations.VerticalThermosiphonTubeGeometryCorrectionFactor(
            tubeInsideDiameterInches: 0.85d,
            tubeLengthFeet: 12d);

        Assert.InRange(correction, 0.85d, 0.98d);
    }

    [Fact]
    public void Dp09eVerticalThermosiphonTubeGeometryCorrectionFactor_IncreasesWithDiameterAndDecreasesWithLength()
    {
        var smallDiameter = Dp09eVaporizationCorrelations.VerticalThermosiphonTubeGeometryCorrectionFactor(
            tubeInsideDiameterInches: 0.6d,
            tubeLengthFeet: 12d);
        var largeDiameter = Dp09eVaporizationCorrelations.VerticalThermosiphonTubeGeometryCorrectionFactor(
            tubeInsideDiameterInches: 1.2d,
            tubeLengthFeet: 12d);
        var longTube = Dp09eVaporizationCorrelations.VerticalThermosiphonTubeGeometryCorrectionFactor(
            tubeInsideDiameterInches: 0.6d,
            tubeLengthFeet: 20d);

        Assert.True(largeDiameter > smallDiameter);
        Assert.True(longTube < smallDiameter);
    }

    [Fact]
    public void Dp09eVerticalThermosiphonNaturalConvectionCoefficient_FollowsFigureA16SampleRange()
    {
        var coefficient = Dp09eVaporizationCorrelations.VerticalThermosiphonNaturalConvectionCoefficient(
            criticalPressurePsia: 500d,
            heatFluxBtuHrFt2: 8_600d);

        Assert.InRange(coefficient, 55d, 80d);
    }

    [Fact]
    public void Dp09eVerticalThermosiphonNaturalConvectionCoefficient_IncreasesWithHeatFluxAndCriticalPressure()
    {
        var low = Dp09eVaporizationCorrelations.VerticalThermosiphonNaturalConvectionCoefficient(
            criticalPressurePsia: 300d,
            heatFluxBtuHrFt2: 5_000d);
        var highHeatFlux = Dp09eVaporizationCorrelations.VerticalThermosiphonNaturalConvectionCoefficient(
            criticalPressurePsia: 300d,
            heatFluxBtuHrFt2: 75_000d);
        var highCriticalPressure = Dp09eVaporizationCorrelations.VerticalThermosiphonNaturalConvectionCoefficient(
            criticalPressurePsia: 1_000d,
            heatFluxBtuHrFt2: 5_000d);

        Assert.True(highHeatFlux > low);
        Assert.True(highCriticalPressure > low);
    }

    [Fact]
    public void Dp09eVerticalThermosiphonFigureA17OutletVaporFractionLimit_FollowsSampleTrend()
    {
        var limit = Dp09eVaporizationCorrelations.VerticalThermosiphonFigureA17OutletVaporFractionLimit(
            reducedHeatFluxFtHr: 1.71d,
            inletVelocityFeetPerSecond: 1.14d);

        Assert.InRange(limit, 0.25d, 0.30d);
    }

    [Fact]
    public void Dp09eFinnedTubeSurfaceFactorForPureHydrocarbon_FollowsFigureA10Trend()
    {
        var lightHydrocarbon = Dp09eVaporizationCorrelations.FinnedTubeSurfaceFactorForPureHydrocarbon(
            molecularWeight: 50d,
            plainSurfaceNucleateBoilingCoefficient: 100d);
        var heavyHydrocarbon = Dp09eVaporizationCorrelations.FinnedTubeSurfaceFactorForPureHydrocarbon(
            molecularWeight: 200d,
            plainSurfaceNucleateBoilingCoefficient: 100d);

        Assert.InRange(lightHydrocarbon, 2.0d, 2.4d);
        Assert.InRange(heavyHydrocarbon, 1.1d, 1.4d);
        Assert.True(lightHydrocarbon > heavyHydrocarbon);
    }

    [Fact]
    public void Dp09eFinnedTubeSurfaceFactorForMixedHydrocarbon_FollowsFigureA11Trend()
    {
        var narrowBoilingRange = Dp09eVaporizationCorrelations.FinnedTubeSurfaceFactorForMixedHydrocarbon(
            boilingRangeF: 20d,
            plainSurfaceNucleateBoilingCoefficient: 100d);
        var wideBoilingRange = Dp09eVaporizationCorrelations.FinnedTubeSurfaceFactorForMixedHydrocarbon(
            boilingRangeF: 200d,
            plainSurfaceNucleateBoilingCoefficient: 100d);

        Assert.InRange(narrowBoilingRange, 1.8d, 2.1d);
        Assert.InRange(wideBoilingRange, 0.8d, 0.95d);
        Assert.True(narrowBoilingRange > wideBoilingRange);
    }

    [Fact]
    public void Dp09eFinEfficiencyFactor_FollowsFigureA12Trend()
    {
        var carbonSteel = Dp09eVaporizationCorrelations.FinEfficiencyFactor(
            tubeMaterialThermalConductivityBtuHrFtF: 28d,
            heatFluxBtuHrFt2: 5_000d);
        var stainlessSteel = Dp09eVaporizationCorrelations.FinEfficiencyFactor(
            tubeMaterialThermalConductivityBtuHrFtF: 9d,
            heatFluxBtuHrFt2: 5_000d);
        var highHeatFlux = Dp09eVaporizationCorrelations.FinEfficiencyFactor(
            tubeMaterialThermalConductivityBtuHrFtF: 28d,
            heatFluxBtuHrFt2: 20_000d);

        Assert.InRange(carbonSteel, 0.60d, 0.72d);
        Assert.True(carbonSteel > stainlessSteel);
        Assert.True(highHeatFlux < carbonSteel);
    }

    [Fact]
    public void Dp09cCatalog_ReturnsTubeMaterialThermalConductivity()
    {
        Assert.Equal(26d, Dp09cShellAndTubeCatalog.GetTubeMaterialThermalConductivityBtuHrFtF(ShellAndTubeTubeMaterial.CarbonSteel));
        Assert.Equal(9d, Dp09cShellAndTubeCatalog.GetTubeMaterialThermalConductivityBtuHrFtF(ShellAndTubeTubeMaterial.Type316StainlessSteel));
        Assert.Equal(223d, Dp09cShellAndTubeCatalog.GetTubeMaterialThermalConductivityBtuHrFtF(ShellAndTubeTubeMaterial.Copper));
    }

    [Fact]
    public void Dp09cCatalog_ReturnsBwgWallThickness()
    {
        Assert.Equal(0.065d, Dp09cShellAndTubeCatalog.GetTubeWallThicknessInches(16), precision: 6);
        Assert.Equal(0.035d, Dp09cShellAndTubeCatalog.GetTubeWallThicknessInches(20), precision: 6);
    }

    [Fact]
    public void Dp09bCatalog_ReturnsTypicalOverallCoefficientRanges()
    {
        var waterCooler = Dp09bHeatExchangerCatalog.GetTypicalShellAndTubeOverallCoefficientRange(
            DesignPracticesProcessRegime.NoPhaseChange,
            shellSideIsPureWater: true,
            tubeSideIsPureWater: true);
        var steamCondenser = Dp09bHeatExchangerCatalog.GetTypicalShellAndTubeOverallCoefficientRange(
            DesignPracticesProcessRegime.ShellSideCondensation,
            shellSideIsPureWater: true,
            tubeSideIsPureWater: true);

        Assert.Equal(150d, waterCooler.MinimumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Equal(210d, waterCooler.MaximumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Equal(400d, steamCondenser.MinimumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Equal(600d, steamCondenser.MaximumBtuPerHourSquareFootFahrenheit, precision: 6);
    }

    [Fact]
    public void Dp09bCatalog_UsesNamedTable1ServiceWhenAvailable()
    {
        var debutanizerCondenser = Dp09bHeatExchangerCatalog.GetTypicalShellAndTubeOverallCoefficientRange(
            DesignPracticesProcessRegime.ShellSideCondensation,
            shellSideIsPureWater: false,
            tubeSideIsPureWater: true,
            cooledFluidName: "Debutanizer overhead vapor",
            heatedFluidName: "Cooling water");
        var steamReboiler = Dp09bHeatExchangerCatalog.GetTypicalShellAndTubeOverallCoefficientRange(
            DesignPracticesProcessRegime.TubeSideVaporization,
            shellSideIsPureWater: true,
            tubeSideIsPureWater: false,
            cooledFluidName: "Steam",
            heatedFluidName: "Debutanizer bottoms");

        Assert.Equal(90d, debutanizerCondenser.MinimumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Equal(100d, debutanizerCondenser.MaximumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Contains("debutanizer overhead condenser", debutanizerCondenser.Basis, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(74d, steamReboiler.MinimumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Equal(100d, steamReboiler.MaximumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Contains("debutanizer bottoms reboiler", steamReboiler.Basis, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dp09bCatalog_ClassifiesCipServiceAsAqueousSolution()
    {
        var cipHeater = Dp09bHeatExchangerCatalog.GetTypicalShellAndTubeOverallCoefficientRange(
            DesignPracticesProcessRegime.ShellSideCondensation,
            shellSideIsPureWater: false,
            tubeSideIsPureWater: false,
            cooledFluidName: "Shell inlet steam shell outlet condensate",
            heatedFluidName: "Tube inlet CIP solution tube outlet CIP solution");

        Assert.Equal(400d, cipHeater.MinimumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Equal(600d, cipHeater.MaximumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Contains("aqueous solution", cipHeater.Basis, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dp09fPreliminaryZoneModel_CalculatesWeightedLmtd()
    {
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = new ShellAndTubeDesignVariables(),
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Shell inlet", 300d) },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Shell outlet", 300d) },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Tube inlet", 80d) },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Tube outlet", 120d) }
        };

        var zones = Dp09fCondensationZoneModel.BuildPreliminaryZones(
            request,
            DesignPracticesProcessRegime.ShellSideCondensation,
            1_000_000d);

        var weightedLmtd = Dp09fCondensationZoneModel.CalculateWeightedEffectiveLmtd(zones);

        Assert.Single(zones);
        Assert.Equal(zones[0].LogMeanTemperatureDifferenceF, weightedLmtd, precision: 8);
        Assert.InRange(weightedLmtd, 190d, 210d);
    }

    [Fact]
    public void Dp09fPreliminaryZoneModel_InfersDesuperheatingCondensationAndSubcoolingZones()
    {
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = new ShellAndTubeDesignVariables(),
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Shell inlet", 320d, 100d) },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Shell outlet", 190d, 0d) },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Tube inlet", 80d) },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Tube outlet", 150d) }
        };

        var zones = Dp09fCondensationZoneModel.BuildPreliminaryZones(
            request,
            DesignPracticesProcessRegime.ShellSideCondensation,
            1_000_000d);

        Assert.Collection(
            zones,
            zone => Assert.Contains("desuperheating", zone.Name, StringComparison.OrdinalIgnoreCase),
            zone => Assert.Contains("condensation", zone.Name, StringComparison.OrdinalIgnoreCase),
            zone => Assert.Contains("subcooling", zone.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1_000_000d, zones.Sum(zone => zone.DutyBtuPerHour), precision: 6);
        Assert.Equal(0.10d, zones[0].DutyBtuPerHour / 1_000_000d, precision: 6);
        Assert.Equal(0.80d, zones[1].DutyBtuPerHour / 1_000_000d, precision: 6);
        Assert.Equal(0.10d, zones[2].DutyBtuPerHour / 1_000_000d, precision: 6);
        Assert.All(zones, zone => Assert.True(zone.LogMeanTemperatureDifferenceF > 0d));
    }

    [Fact]
    public void Dp09fPreliminaryZoneModel_UsesSensibleDutyWhenMassFlowAndCpAreAvailable()
    {
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = new ShellAndTubeDesignVariables(),
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Shell inlet", 320d, 100d, 10_000d, 0.5d) },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Shell outlet", 190d, 0d, 10_000d, 1.0d) },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Tube inlet", 80d) },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Tube outlet", 150d) }
        };

        var zones = Dp09fCondensationZoneModel.BuildPreliminaryZones(
            request,
            DesignPracticesProcessRegime.ShellSideCondensation,
            1_000_000d);

        Assert.Collection(
            zones,
            zone => Assert.Contains("desuperheating", zone.Name, StringComparison.OrdinalIgnoreCase),
            zone => Assert.Contains("condensation", zone.Name, StringComparison.OrdinalIgnoreCase),
            zone => Assert.Contains("subcooling", zone.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1_000_000d, zones.Sum(zone => zone.DutyBtuPerHour), precision: 6);
        Assert.Equal(0.325d, zones[0].DutyBtuPerHour / 1_000_000d, precision: 6);
        Assert.Equal(0.325d, zones[1].DutyBtuPerHour / 1_000_000d, precision: 6);
        Assert.Equal(0.350d, zones[2].DutyBtuPerHour / 1_000_000d, precision: 6);
        Assert.All(zones, zone => Assert.True(zone.LogMeanTemperatureDifferenceF > 0d));
    }

    [Fact]
    public void Dp09fPreliminaryZoneModel_FallsBackToSingleZoneWhenVaporFractionsAreMissing()
    {
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = new ShellAndTubeDesignVariables(),
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Shell inlet", 320d) },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Shell outlet", 190d) },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Tube inlet", 80d) },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateStream("Tube outlet", 150d) }
        };

        var zones = Dp09fCondensationZoneModel.BuildPreliminaryZones(
            request,
            DesignPracticesProcessRegime.ShellSideCondensation,
            1_000_000d);

        var zone = Assert.Single(zones);
        Assert.Equal("Preliminary condensation zone", zone.Name);
    }

    [Fact]
    public void Dp09fZoneModel_CalculatesAreaWeightedPressureDrop()
    {
        var zones = new[]
        {
            new DesignPracticesCondensationZone("Desuperheating", 200_000d, 340d, 300d, 100d, 130d, 180d),
            new DesignPracticesCondensationZone("Condensation", 800_000d, 300d, 300d, 130d, 180d, 145d)
        };

        var zoneAreas = Dp09fCondensationZoneModel.CalculateZoneAreas(zones, 100d);
        var pressureDrop = Dp09fCondensationZoneModel.CalculateAreaWeightedPressureDrop(
            [2d, 8d],
            zoneAreas);

        Assert.Equal(1d, zoneAreas.Sum(zone => zone.AreaFraction), precision: 10);
        Assert.True(zoneAreas[1].AreaFraction > zoneAreas[0].AreaFraction);
        Assert.InRange(pressureDrop, 6d, 8d);
    }

    [Fact]
    public void Dp09fZoneModel_CalculatesZoneAverageDensitiesFromVaporFractionProfile()
    {
        var zones = new[]
        {
            new DesignPracticesCondensationZone("Sensible desuperheating zone", 100_000d, 340d, 300d, 100d, 130d, 180d),
            new DesignPracticesCondensationZone("Sensible-balance condensation zone", 800_000d, 300d, 300d, 130d, 180d, 145d),
            new DesignPracticesCondensationZone("Sensible subcooling zone", 100_000d, 300d, 260d, 180d, 210d, 120d)
        };

        var densities = Dp09fCondensationZoneModel.CalculateZoneAverageDensities(
            zones,
            totalMassFlowLbPerHour: 1_000d,
            inletVaporFraction: 1d,
            outletVaporFraction: 0d,
            vaporDensityLbFt3: 0.2d,
            liquidDensityLbFt3: 50d);

        Assert.Equal(0.2d, densities[0], precision: 6);
        Assert.Equal(0.398406d, densities[1], precision: 6);
        Assert.Equal(50d, densities[2], precision: 6);
        Assert.True(densities[0] < densities[1]);
        Assert.True(densities[1] < densities[2]);
    }

    [Fact]
    public void Dp09fZoneModel_CalculatesVaporFreeAreaFromVolumeFraction()
    {
        var zones = new[]
        {
            new DesignPracticesCondensationZone("Sensible desuperheating zone", 100_000d, 340d, 300d, 100d, 130d, 180d),
            new DesignPracticesCondensationZone("Inferred partial condensation zone", 900_000d, 300d, 280d, 130d, 180d, 145d)
        };

        var vaporFreeAreaFraction = Dp09fCondensationZoneModel.CalculateCondensingZoneVaporFreeAreaFraction(
            zones,
            inletVaporFraction: 1d,
            outletVaporFraction: 0.5d,
            vaporDensityLbFt3: 0.2d,
            liquidDensityLbFt3: 50d);

        Assert.Equal(0.998008d, vaporFreeAreaFraction, precision: 6);
        Assert.True(vaporFreeAreaFraction > 0.5d);
    }

    [Fact]
    public void Dp09fZoneModel_CalculatesPreliminaryZonePressureDropsByZoneType()
    {
        var zones = new[]
        {
            new DesignPracticesCondensationZone("Sensible desuperheating zone", 100_000d, 340d, 300d, 100d, 130d, 180d),
            new DesignPracticesCondensationZone("Sensible-balance condensation zone", 800_000d, 300d, 300d, 130d, 180d, 145d),
            new DesignPracticesCondensationZone("Sensible subcooling zone", 100_000d, 300d, 260d, 180d, 210d, 120d)
        };

        var drops = Dp09fCondensationZoneModel.CalculatePreliminaryZonePressureDrops(
            zones,
            basePressureDropPsi: 4d,
            twoPhaseDensityCorrectionFactor: 2d);

        Assert.Equal(4.6d, drops[0], precision: 6);
        Assert.Equal(8d, drops[1], precision: 6);
        Assert.Equal(2.6d, drops[2], precision: 6);
        Assert.True(drops[1] > drops[0]);
        Assert.True(drops[0] > drops[2]);
    }

    [Fact]
    public void Dp09fZoneModel_CalculatesPreliminaryZonePressureDropsByZoneDensity()
    {
        var zones = new[]
        {
            new DesignPracticesCondensationZone("Sensible desuperheating zone", 100_000d, 340d, 300d, 100d, 130d, 180d),
            new DesignPracticesCondensationZone("Sensible-balance condensation zone", 800_000d, 300d, 300d, 130d, 180d, 145d),
            new DesignPracticesCondensationZone("Sensible subcooling zone", 100_000d, 300d, 260d, 180d, 210d, 120d)
        };

        var densityCorrections = Dp09fCondensationZoneModel.CalculatePreliminaryZoneDensityCorrectionFactors(
            zones,
            baseDensityLbFt3: 10d,
            vaporDensityLbFt3: 2d,
            twoPhaseDensityLbFt3: 5d,
            liquidDensityLbFt3: 50d);
        var drops = Dp09fCondensationZoneModel.CalculatePreliminaryZonePressureDrops(
            zones,
            basePressureDropPsi: 4d,
            densityCorrections);

        Assert.Equal(5d, densityCorrections[0], precision: 6);
        Assert.Equal(2d, densityCorrections[1], precision: 6);
        Assert.Equal(0.2d, densityCorrections[2], precision: 6);
        Assert.Equal(23d, drops[0], precision: 6);
        Assert.Equal(8d, drops[1], precision: 6);
        Assert.Equal(0.52d, drops[2], precision: 6);
        Assert.True(drops[0] > drops[1]);
        Assert.True(drops[1] > drops[2]);
    }

    [Fact]
    public void Dp09fZoneModel_CalculatesZonePressureDropContributions()
    {
        var zones = new[]
        {
            new DesignPracticesCondensationZone("Sensible desuperheating zone", 100_000d, 340d, 300d, 100d, 130d, 180d),
            new DesignPracticesCondensationZone("Sensible-balance condensation zone", 800_000d, 300d, 300d, 130d, 180d, 145d),
            new DesignPracticesCondensationZone("Sensible subcooling zone", 100_000d, 300d, 260d, 180d, 210d, 120d)
        };
        var zoneAreas = Dp09fCondensationZoneModel.CalculateZoneAreas(zones, 100d);

        var contributions = Dp09fCondensationZoneModel.CalculatePreliminaryZonePressureDropContributions(
            zones,
            zoneAreas,
            fullLengthBasePressureDropPsi: 4d,
            zoneDensityCorrectionFactors: [5d, 2d, 0.2d]);

        Assert.Equal(3, contributions.Count);
        Assert.True(contributions[1] > contributions[0]);
        Assert.True(contributions[2] < contributions[0]);
        Assert.Equal(8.304078d, contributions.Sum(), precision: 6);
    }

    [Fact]
    public void Dp09fZoneModel_CalculatesDutyWeightedZoneCoefficient()
    {
        var zones = new[]
        {
            new DesignPracticesCondensationZone("Desuperheating", 100_000d, 340d, 300d, 100d, 130d, 180d),
            new DesignPracticesCondensationZone("Condensation", 800_000d, 300d, 300d, 130d, 180d, 145d),
            new DesignPracticesCondensationZone("Subcooling", 100_000d, 300d, 260d, 180d, 210d, 120d)
        };

        var coefficient = Dp09fCondensationZoneModel.CalculateDutyWeightedZoneCoefficient(
            zones,
            vaporCoolingCoefficient: 100d,
            condensingCoefficient: 1_000d,
            liquidCoolingCoefficient: 200d);

        Assert.Equal(434.7826087d, coefficient, precision: 6);
    }

    [Fact]
    public void Dp09fLiquidCoolingCoefficient_CombinesBottomFlowAndDripCooling()
    {
        var coefficient = Dp09fCondensationZoneModel.CalculateLiquidCoolingCoefficient(
            bottomFlowLiquidCoolingCoefficient: 100d,
            dripCoolingCoefficient: 300d);

        Assert.Equal(150d, coefficient, precision: 6);
    }

    private static IFacadeStream CreateStream(
        string name,
        double temperatureF,
        double? vaporFractionPercent = null,
        double? massFlowLbPerHour = null,
        double? massCpBtuLbF = null)
    {
        var stream = new FacadeStream(name);
        stream.Temperature.SetValue(new Temperature(temperatureF, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.StreamCalculated);

        if (vaporFractionPercent is not null)
        {
            stream.VaporFraction.SetValue(new Percentage(vaporFractionPercent.Value, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        }

        if (massFlowLbPerHour is not null)
        {
            stream.MassFlow.SetValue(new MassFlow(massFlowLbPerHour.Value, MassFlowUnits.lb_hr), VariableDefinedBy.StreamCalculated);
        }

        if (massCpBtuLbF is not null)
        {
            stream.MassCp.SetValue(new MassEntropy(massCpBtuLbF.Value, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.StreamCalculated);
        }

        return stream;
    }
}
