namespace Shared.UnitOperations.HeatExchangers.Design;

public static class Dp09bHeatExchangerCatalog
{
    public static DesignPracticesOverallCoefficientRange GetTypicalShellAndTubeOverallCoefficientRange(
        DesignPracticesProcessRegime processRegime,
        bool shellSideIsPureWater,
        bool tubeSideIsPureWater,
        string cooledFluidName,
        string heatedFluidName)
    {
        var namedRange = TryFindNamedTable1Range(cooledFluidName, heatedFluidName, out var range)
            ? range
            : null;

        if (namedRange is not null)
        {
            return namedRange;
        }

        var cooledService = DesignPracticesServiceClassifier.ClassifyName(cooledFluidName);
        var heatedService = DesignPracticesServiceClassifier.ClassifyName(heatedFluidName);
        if (cooledService.Kind == DesignPracticesServiceKind.Unknown &&
            heatedService.Kind == DesignPracticesServiceKind.Unknown)
        {
            return GetTypicalShellAndTubeOverallCoefficientRange(
                processRegime,
                shellSideIsPureWater,
                tubeSideIsPureWater);
        }

        return SelectClassifiedRange(processRegime, cooledService, heatedService);
    }

    public static DesignPracticesOverallCoefficientRange GetTypicalShellAndTubeOverallCoefficientRange(
        DesignPracticesProcessRegime processRegime,
        DesignPracticesServiceClassification cooledService,
        DesignPracticesServiceClassification heatedService)
    {
        return SelectClassifiedRange(
            processRegime,
            cooledService,
            heatedService);
    }

    public static DesignPracticesOverallCoefficientRange GetTypicalShellAndTubeOverallCoefficientRange(
        DesignPracticesProcessRegime processRegime,
        DesignPracticesServiceClassification cooledService,
        DesignPracticesServiceClassification heatedService,
        string cooledFluidName,
        string heatedFluidName)
    {
        return TryFindNamedTable1Range(cooledFluidName, heatedFluidName, out var range)
            ? range
            : SelectClassifiedRange(processRegime, cooledService, heatedService);
    }

    public static DesignPracticesOverallCoefficientRange GetTypicalShellAndTubeOverallCoefficientRange(
        DesignPracticesProcessRegime processRegime,
        bool shellSideIsPureWater,
        bool tubeSideIsPureWater)
    {
        return processRegime switch
        {
            DesignPracticesProcessRegime.ShellSideCondensation when shellSideIsPureWater =>
                new DesignPracticesOverallCoefficientRange(400d, 600d, "DP09B Table 1 steam condenser / water"),
            DesignPracticesProcessRegime.TubeSideCondensation when tubeSideIsPureWater =>
                new DesignPracticesOverallCoefficientRange(400d, 600d, "DP09B Table 1 steam condenser / water"),
            DesignPracticesProcessRegime.ShellSideCondensation or DesignPracticesProcessRegime.TubeSideCondensation =>
                new DesignPracticesOverallCoefficientRange(70d, 130d, "DP09B Table 1 refinery condenser / water range"),
            DesignPracticesProcessRegime.ShellSideVaporization or DesignPracticesProcessRegime.TubeSideVaporization =>
                new DesignPracticesOverallCoefficientRange(70d, 115d, "DP09B Table 1 steam reboiler range"),
            _ when shellSideIsPureWater && tubeSideIsPureWater =>
                new DesignPracticesOverallCoefficientRange(150d, 210d, "DP09B Table 1 water / water cooler"),
            _ =>
                new DesignPracticesOverallCoefficientRange(45d, 85d, "DP09B Table 1 smooth-tube process/process exchanger range")
        };
    }

    private static DesignPracticesOverallCoefficientRange SelectClassifiedRange(
        DesignPracticesProcessRegime processRegime,
        DesignPracticesServiceClassification cooledService,
        DesignPracticesServiceClassification heatedService)
    {
        if (IsSteamCondensing(cooledService.Kind) && heatedService.Kind == DesignPracticesServiceKind.AqueousSolution)
        {
            return new DesignPracticesOverallCoefficientRange(400d, 600d, "DP09B Table 1 steam condenser / aqueous solution");
        }

        if (IsSteamCondensing(cooledService.Kind) && IsWaterLike(heatedService.Kind))
        {
            return new DesignPracticesOverallCoefficientRange(400d, 600d, "DP09B Table 1 steam condenser / water");
        }

        if (processRegime == DesignPracticesProcessRegime.NoPhaseChange &&
            IsWaterLike(cooledService.Kind) &&
            IsWaterLike(heatedService.Kind))
        {
            return new DesignPracticesOverallCoefficientRange(150d, 210d, "DP09B Table 1 water / water cooler");
        }

        if (processRegime == DesignPracticesProcessRegime.NoPhaseChange &&
            IsWaterLike(cooledService.Kind) &&
            heatedService.Kind == DesignPracticesServiceKind.AqueousSolution)
        {
            return new DesignPracticesOverallCoefficientRange(150d, 210d, "DP09B Table 1 water / aqueous solution cooler");
        }

        return processRegime switch
        {
            DesignPracticesProcessRegime.ShellSideCondensation or DesignPracticesProcessRegime.TubeSideCondensation =>
                new DesignPracticesOverallCoefficientRange(70d, 130d, "DP09B Table 1 refinery condenser / water range"),
            DesignPracticesProcessRegime.ShellSideVaporization or DesignPracticesProcessRegime.TubeSideVaporization =>
                new DesignPracticesOverallCoefficientRange(70d, 115d, "DP09B Table 1 steam reboiler range"),
            _ when IsWaterLike(cooledService.Kind) && IsWaterLike(heatedService.Kind) =>
                new DesignPracticesOverallCoefficientRange(150d, 210d, "DP09B Table 1 water / water cooler"),
            _ =>
                new DesignPracticesOverallCoefficientRange(45d, 85d, "DP09B Table 1 smooth-tube process/process exchanger range")
        };
    }

    private static bool IsSteamCondensing(DesignPracticesServiceKind serviceKind)
    {
        return serviceKind is DesignPracticesServiceKind.Steam or DesignPracticesServiceKind.SteamCondensing;
    }

    private static bool IsWaterLike(DesignPracticesServiceKind serviceKind)
    {
        return serviceKind is DesignPracticesServiceKind.Water
            or DesignPracticesServiceKind.Steam
            or DesignPracticesServiceKind.SteamCondensing;
    }

    private static bool TryFindNamedTable1Range(
        string cooledFluidName,
        string heatedFluidName,
        out DesignPracticesOverallCoefficientRange range)
    {
        var cooled = NormalizeServiceName(cooledFluidName);
        var heated = NormalizeServiceName(heatedFluidName);

        foreach (var row in Table1Rows)
        {
            if (ContainsAllTokens(cooled, row.CooledFluidTokens) &&
                ContainsAllTokens(heated, row.HeatedFluidTokens))
            {
                range = new DesignPracticesOverallCoefficientRange(
                    row.MinimumBtuPerHourSquareFootFahrenheit,
                    row.MaximumBtuPerHourSquareFootFahrenheit,
                    $"DP09B Table 1 {row.Basis}");
                return true;
            }
        }

        range = default!;
        return false;
    }

    private static bool ContainsAllTokens(string value, IReadOnlyList<string> tokens) =>
        tokens.All(value.Contains);

    private static string NormalizeServiceName(string value)
    {
        var normalized = value
            .Replace("-", " ", StringComparison.Ordinal)
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace(".", " ", StringComparison.Ordinal)
            .ToLowerInvariant();

        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (tokens.Any(token => token is "cip" or "wash" or "cleaning" or "caustic" or "acid" or "sanitizer" or "brine" or "solution"))
        {
            tokens.Add("aqueous");
            tokens.Add("solution");
            tokens.Add("water");
        }

        if (tokens.Contains("condensate"))
        {
            tokens.Add("water");
        }

        if (tokens.Contains("steam"))
        {
            tokens.Add("water");
            tokens.Add("vapor");
        }

        return string.Join(' ', tokens.Distinct(StringComparer.Ordinal));
    }

    private static readonly IReadOnlyList<Table1OverallCoefficientRow> Table1Rows =
    [
        new(["atmospheric", "top", "pumparound"], ["crude"], 60d, 70d, "atmospheric P/S top pumparound / crude"),
        new(["atmospheric", "bottom", "pumparound"], ["crude"], 55d, 85d, "atmospheric P/S bottom pumparound / crude"),
        new(["hydrocracker", "effluent"], ["hydrocracker", "feed"], 75d, 75d, "hydrocracker effluent / feed"),
        new(["hydrof", "effluent"], ["hydrof", "feed"], 50d, 68d, "hydrofiner unit effluent / feed"),
        new(["powerforming", "effluent"], ["powerforming", "feed"], 50d, 80d, "powerforming unit effluent / feed"),
        new(["regenerated", "dea"], ["foul", "dea"], 110d, 110d, "regenerated DEA / foul DEA"),
        new(["debutanizer", "bottom"], ["water"], 68d, 75d, "debutanizer bottoms cooler / water"),
        new(["debutanizer", "overhead"], ["water"], 90d, 100d, "debutanizer overhead condenser / water"),
        new(["deethanizer", "overhead"], ["water"], 90d, 113d, "deethanizer overhead condenser / water"),
        new(["stabilizer", "overhead"], ["water"], 75d, 85d, "stabilizer overhead condenser / water"),
        new(["splitter", "overhead"], ["water"], 85d, 113d, "splitter overhead condenser / water"),
        new(["hydrocracker", "effluent"], ["water"], 85d, 85d, "hydrocracker effluent condenser / water"),
        new(["propylene"], ["water"], 120d, 120d, "propylene condenser / water"),
        new(["hydrocarbon", "vapor"], ["water"], 38d, 60d, "hydrocarbon vapor gas cooler / water"),
        new(["primary", "fractionator", "gas"], ["water"], 27d, 27d, "primary fractionator gas cooler / water"),
        new(["steam"], ["aqueous", "solution"], 400d, 600d, "steam condenser / aqueous solution"),
        new(["steam"], ["water"], 400d, 600d, "steam condenser / water"),
        new(["water"], ["aqueous", "solution"], 150d, 210d, "water / aqueous solution cooler"),
        new(["steam"], ["deethanizer", "bottom"], 73d, 86d, "steam / deethanizer bottoms reboiler"),
        new(["steam"], ["depropanizer", "bottom"], 89d, 89d, "steam / depropanizer bottoms reboiler"),
        new(["steam"], ["debutanizer", "bottom"], 74d, 100d, "steam / debutanizer bottoms reboiler"),
        new(["steam"], ["stabilizer", "bottom"], 115d, 115d, "steam / stabilizer bottoms reboiler"),
        new(["steam"], ["dea", "regenerator", "bottom"], 240d, 240d, "steam / DEA regenerator bottoms reboiler")
    ];

    private sealed record Table1OverallCoefficientRow(
        IReadOnlyList<string> CooledFluidTokens,
        IReadOnlyList<string> HeatedFluidTokens,
        double MinimumBtuPerHourSquareFootFahrenheit,
        double MaximumBtuPerHourSquareFootFahrenheit,
        string Basis);
}

public sealed record DesignPracticesOverallCoefficientRange(
    double MinimumBtuPerHourSquareFootFahrenheit,
    double MaximumBtuPerHourSquareFootFahrenheit,
    string Basis)
{
    public double MidpointBtuPerHourSquareFootFahrenheit =>
        (MinimumBtuPerHourSquareFootFahrenheit + MaximumBtuPerHourSquareFootFahrenheit) / 2d;
}
