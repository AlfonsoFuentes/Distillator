namespace Shared.UnitOperations.HeatExchangers.Design;

public static class Dp09fTubeSideWaterCorrelation
{
    public const string WaterHeatingCoefficientSource =
        "DP09F Table 2 condenser design procedure, page 24, step 13 tube-side water hio equation";

    public const string WaterPressureDropSource =
        "DP09F Table 2 condenser design procedure, page 24, step 13 tube-side water pressure-drop equation";

    public const string PlainSteelPressureDropFoulingFactorSource =
        "DP09D Pressure Drop Fouling Factors, page 41, plain steel tube Ft equation";

    public const string AlloyPressureDropFoulingFactorReviewSource =
        "DP09D Pressure Drop Fouling Factors, page 41, nonferrous alloy tube Ft table/equation";

    public static double CalculateInsideCoefficientCorrectedToOutsideArea(
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

    public static double CalculateTubeSidePressureDropPsi(
        double velocityFeetPerSecond,
        double tubeInsideDiameterInches,
        double tubeLengthFeet,
        double shellPasses,
        double tubePasses,
        double pressureDropFoulingFactor)
    {
        if (velocityFeetPerSecond <= 0d ||
            tubeInsideDiameterInches <= 0d ||
            tubeLengthFeet <= 0d ||
            shellPasses <= 0d ||
            tubePasses <= 0d ||
            pressureDropFoulingFactor <= 0d)
        {
            return 0d;
        }

        return 0.020d *
               pressureDropFoulingFactor *
               shellPasses *
               tubePasses *
               (velocityFeetPerSecond * velocityFeetPerSecond +
                0.158d * tubeLengthFeet * Math.Pow(velocityFeetPerSecond, 1.73d) /
                Math.Pow(tubeInsideDiameterInches, 1.27d));
    }

    public static double EstimatePlainSteelPressureDropFoulingFactor(
        double tubeOutsideDiameterInches,
        double tubeInsideDiameterInches)
    {
        if (tubeOutsideDiameterInches <= 0d ||
            tubeInsideDiameterInches <= 0d ||
            tubeInsideDiameterInches >= tubeOutsideDiameterInches)
        {
            return 1d;
        }

        var wallThicknessInches = (tubeOutsideDiameterInches - tubeInsideDiameterInches) / 2d;
        var fouledDiameter = tubeOutsideDiameterInches -
                             2.2d * wallThicknessInches -
                             0.0238d * Math.Pow(tubeOutsideDiameterInches, 0.3d);
        if (fouledDiameter <= 0d)
        {
            return 1d;
        }

        return Math.Max(1d, Math.Pow(tubeInsideDiameterInches / fouledDiameter, 5d));
    }

    public static DesignPracticesPressureDropFoulingFactor EstimatePressureDropFoulingFactor(
        ShellAndTubeTubeMaterial tubeMaterial,
        double tubeOutsideDiameterInches,
        double tubeInsideDiameterInches)
    {
        if (IsSteelTubeMaterial(tubeMaterial))
        {
            return new DesignPracticesPressureDropFoulingFactor(
                EstimatePlainSteelPressureDropFoulingFactor(tubeOutsideDiameterInches, tubeInsideDiameterInches),
                PlainSteelPressureDropFoulingFactorSource,
                RequiresMaterialReview: false);
        }

        return new DesignPracticesPressureDropFoulingFactor(
            1d,
            AlloyPressureDropFoulingFactorReviewSource,
            RequiresMaterialReview: true);
    }

    private static bool IsSteelTubeMaterial(ShellAndTubeTubeMaterial tubeMaterial)
    {
        return tubeMaterial is ShellAndTubeTubeMaterial.CarbonSteel
            or ShellAndTubeTubeMaterial.CarbonMoly
            or ShellAndTubeTubeMaterial.ChromeMoly1CrHalfMo
            or ShellAndTubeTubeMaterial.ChromeMoly214CrHalfMo
            or ShellAndTubeTubeMaterial.ChromeMoly5CrHalfMo
            or ShellAndTubeTubeMaterial.ChromeMoly12Cr1Mo;
    }
}

public sealed record DesignPracticesPressureDropFoulingFactor(
    double Value,
    string Source,
    bool RequiresMaterialReview);
