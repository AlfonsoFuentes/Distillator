namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class KernShellAndTubeLayoutCatalogService : IShellAndTubeLayoutService
{
    private readonly IShellAndTubeLayoutService fallback = new CorrelatedShellAndTubeLayoutService();

    public ShellAndTubeLayoutSelection SelectShellForTubeCount(ShellAndTubeLayoutRequest request)
    {
        Validate(request.TubeCount, request.TubeOuterDiameterInches, request.TubePitchInches, request.TubePasses);

        var catalogRows = FindCatalogRows(
            request.TubeOuterDiameterInches,
            request.TubePitchInches,
            request.TubeLayout);

        var catalogSelection = catalogRows
            .Select(row => new
            {
                Row = row,
                TubeCount = row.GetTubeCount(request.TubePasses)
            })
            .Where(candidate => candidate.TubeCount >= request.TubeCount)
            .OrderBy(candidate => candidate.Row.ShellInsideDiameterInches)
            .FirstOrDefault();

        if (catalogSelection is not null)
        {
            return new ShellAndTubeLayoutSelection
            {
                ShellInsideDiameterInches = catalogSelection.Row.ShellInsideDiameterInches,
                MaximumTubeCount = catalogSelection.TubeCount,
                Source = ShellAndTubeLayoutSource.KernTable
            };
        }

        return fallback.SelectShellForTubeCount(request);
    }

    public ShellAndTubeLayoutCapacity EstimateTubeCapacity(ShellAndTubeLayoutCapacityRequest request)
    {
        Validate(request.ShellInsideDiameterInches, request.TubeOuterDiameterInches, request.TubePitchInches, request.TubePasses);

        var catalogRow = FindCatalogRows(
                request.TubeOuterDiameterInches,
                request.TubePitchInches,
                request.TubeLayout)
            .FirstOrDefault(row => AreClose(row.ShellInsideDiameterInches, request.ShellInsideDiameterInches));

        if (catalogRow is not null)
        {
            return new ShellAndTubeLayoutCapacity
            {
                MaximumTubeCount = catalogRow.GetTubeCount(request.TubePasses),
                Source = ShellAndTubeLayoutSource.KernTable
            };
        }

        return fallback.EstimateTubeCapacity(request);
    }

    private static IReadOnlyList<KernShellAndTubeLayoutRow> FindCatalogRows(
        double tubeOuterDiameterInches,
        double tubePitchInches,
        ShellAndTubeTubeLayout tubeLayout)
    {
        return KernRows
            .Where(row =>
                AreClose(row.TubeOuterDiameterInches, tubeOuterDiameterInches) &&
                AreClose(row.TubePitchInches, tubePitchInches) &&
                row.TubeLayout == tubeLayout)
            .ToArray();
    }

    private static void Validate(
        double firstPositiveValue,
        double tubeOuterDiameterInches,
        double tubePitchInches,
        int tubePasses)
    {
        if (firstPositiveValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(firstPositiveValue), "Layout values must be positive.");
        }

        if (tubeOuterDiameterInches <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tubeOuterDiameterInches), "Tube outside diameter must be positive.");
        }

        if (tubePitchInches <= tubeOuterDiameterInches)
        {
            throw new ArgumentOutOfRangeException(nameof(tubePitchInches), "Tube pitch must be greater than tube outside diameter.");
        }

        if (tubePasses <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tubePasses), "Tube passes must be positive.");
        }
    }

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) < 0.005d;
    }

    private static readonly IReadOnlyList<KernShellAndTubeLayoutRow> KernRows =
    [
        // Kern, Tabla 9, tubos de 1 in DE, arreglo triangular de 1 1/4 in.
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 8d, 21, 16, 16, 14, 0),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 10d, 32, 32, 26, 24, 0),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 12d, 55, 52, 48, 46, 44),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 15.25d, 91, 86, 80, 74, 72),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 17.25d, 131, 118, 106, 104, 94),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 19.25d, 163, 152, 140, 136, 128),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 21.25d, 199, 188, 170, 164, 160),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 23.25d, 241, 232, 212, 212, 202),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 25d, 294, 282, 256, 252, 242),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 27d, 349, 334, 302, 296, 280),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 29d, 397, 376, 338, 334, 318),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 31d, 472, 454, 430, 424, 400),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 33d, 538, 522, 486, 470, 454),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 35d, 608, 592, 562, 546, 532),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 37d, 674, 664, 632, 614, 598),
        new(1d, 1.25d, ShellAndTubeTubeLayout.Triangular, 39d, 766, 736, 700, 688, 672)
    ];

    private sealed record KernShellAndTubeLayoutRow(
        double TubeOuterDiameterInches,
        double TubePitchInches,
        ShellAndTubeTubeLayout TubeLayout,
        double ShellInsideDiameterInches,
        int OnePassTubeCount,
        int TwoPassTubeCount,
        int FourPassTubeCount,
        int SixPassTubeCount,
        int EightPassTubeCount)
    {
        public int GetTubeCount(int tubePasses)
        {
            return tubePasses switch
            {
                1 => OnePassTubeCount,
                2 => TwoPassTubeCount,
                4 => FourPassTubeCount,
                6 => SixPassTubeCount,
                8 => EightPassTubeCount,
                _ => 0
            };
        }
    }
}
