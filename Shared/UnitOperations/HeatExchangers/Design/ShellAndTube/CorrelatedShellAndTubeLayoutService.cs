namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class CorrelatedShellAndTubeLayoutService : IShellAndTubeLayoutService
{
    private const double MinimumShellInsideDiameterInches = 6d;
    private const double MaximumShellInsideDiameterInches = 120d;
    private const double SearchStepInches = 0.25d;

    public ShellAndTubeLayoutSelection SelectShellForTubeCount(ShellAndTubeLayoutRequest request)
    {
        Validate(request.TubeCount, request.TubePitchInches, request.TubePasses);

        for (var shellInsideDiameter = MinimumShellInsideDiameterInches;
             shellInsideDiameter <= MaximumShellInsideDiameterInches;
             shellInsideDiameter += SearchStepInches)
        {
            var maximumTubeCount = EstimateMaximumTubeCount(
                shellInsideDiameter,
                request.TubePitchInches,
                request.TubePasses);

            if (maximumTubeCount >= request.TubeCount)
            {
                return new ShellAndTubeLayoutSelection
                {
                    ShellInsideDiameterInches = shellInsideDiameter,
                    MaximumTubeCount = maximumTubeCount,
                    Source = ShellAndTubeLayoutSource.Correlation
                };
            }
        }

        throw new InvalidOperationException("Could not estimate a shell inside diameter for the requested tube count.");
    }

    public ShellAndTubeLayoutCapacity EstimateTubeCapacity(ShellAndTubeLayoutCapacityRequest request)
    {
        Validate(request.ShellInsideDiameterInches, request.TubePitchInches, request.TubePasses);

        return new ShellAndTubeLayoutCapacity
        {
            MaximumTubeCount = EstimateMaximumTubeCount(
                request.ShellInsideDiameterInches,
                request.TubePitchInches,
                request.TubePasses),
            Source = ShellAndTubeLayoutSource.Correlation
        };
    }

    private static int EstimateMaximumTubeCount(double shellInsideDiameterInches, double tubePitchInches, int tubePasses)
    {
        const double k1 = -1.04;
        const double k2 = -0.1;
        const double k3 = 0.43;
        const double k4 = -0.25;

        var a = Math.Pow(shellInsideDiameterInches - k1, 2d) * Math.PI / 4d + k2;
        var b = tubePitchInches * (shellInsideDiameterInches - k1) * (k3 * tubePasses + k4);
        var c = 1.223 * Math.Pow(tubePitchInches, 2d);

        return Math.Max(0, (int)((a - b) / c));
    }

    private static void Validate(double firstPositiveValue, double tubePitchInches, int tubePasses)
    {
        if (firstPositiveValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(firstPositiveValue), "Layout values must be positive.");
        }

        if (tubePitchInches <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tubePitchInches), "Tube pitch must be positive.");
        }

        if (tubePasses <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tubePasses), "Tube passes must be positive.");
        }
    }
}
