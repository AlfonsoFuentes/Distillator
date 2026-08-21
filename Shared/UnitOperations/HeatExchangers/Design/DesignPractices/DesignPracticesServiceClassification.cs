using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public enum DesignPracticesServiceKind
{
    Unknown = 0,
    Water = 1,
    AqueousSolution = 2,
    Steam = 3,
    SteamCondensing = 4,
    HydrocarbonLiquid = 5,
    HydrocarbonVapor = 6,
    ProcessLiquid = 7,
    ProcessVapor = 8
}

public sealed record DesignPracticesServiceClassification(
    DesignPracticesServiceKind Kind,
    string Basis);

public static class DesignPracticesServiceClassifier
{
    private const double TraceFractionPercent = 1e-6;
    private const double MostlyVaporFraction = 0.5d;

    public static DesignPracticesServiceClassification Classify(IFacadeStream inlet, IFacadeStream outlet)
    {
        var serviceName = $"{inlet.Name} {outlet.Name}";
        var normalizedName = NormalizeServiceName(serviceName);

        if (ContainsAqueousSolutionToken(normalizedName))
        {
            return new DesignPracticesServiceClassification(
                DesignPracticesServiceKind.AqueousSolution,
                "aqueous solution service tokens");
        }

        if (IsPureWater(inlet) && IsPureWater(outlet))
        {
            if (IsCondensing(inlet, outlet) || ContainsSteamCondensateTokens(normalizedName))
            {
                return new DesignPracticesServiceClassification(
                    DesignPracticesServiceKind.SteamCondensing,
                    "pure water vapor condensing service");
            }

            if (normalizedName.Contains("steam"))
            {
                return new DesignPracticesServiceClassification(
                    DesignPracticesServiceKind.Steam,
                    "pure water steam service");
            }

            return new DesignPracticesServiceClassification(
                DesignPracticesServiceKind.Water,
                "pure water service");
        }

        if (HasWaterAndNonWaterComponents(inlet) || HasWaterAndNonWaterComponents(outlet))
        {
            return new DesignPracticesServiceClassification(
                DesignPracticesServiceKind.AqueousSolution,
                "water plus non-water components");
        }

        var averageVaporFraction = (ReadVaporFraction(inlet) + ReadVaporFraction(outlet)) / 2d;
        if (ContainsHydrocarbonToken(normalizedName))
        {
            return new DesignPracticesServiceClassification(
                averageVaporFraction >= MostlyVaporFraction
                    ? DesignPracticesServiceKind.HydrocarbonVapor
                    : DesignPracticesServiceKind.HydrocarbonLiquid,
                "hydrocarbon service tokens");
        }

        return new DesignPracticesServiceClassification(
            averageVaporFraction >= MostlyVaporFraction
                ? DesignPracticesServiceKind.ProcessVapor
                : DesignPracticesServiceKind.ProcessLiquid,
            "generic process service");
    }

    public static DesignPracticesServiceClassification ClassifyName(string serviceName)
    {
        var normalizedName = NormalizeServiceName(serviceName);

        if (ContainsAqueousSolutionToken(normalizedName))
        {
            return new DesignPracticesServiceClassification(
                DesignPracticesServiceKind.AqueousSolution,
                "aqueous solution service tokens");
        }

        if (ContainsSteamCondensateTokens(normalizedName))
        {
            return new DesignPracticesServiceClassification(
                DesignPracticesServiceKind.SteamCondensing,
                "steam/condensate service tokens");
        }

        if (normalizedName.Contains("steam"))
        {
            return new DesignPracticesServiceClassification(
                DesignPracticesServiceKind.Steam,
                "steam service tokens");
        }

        if (normalizedName.Contains("water") || normalizedName.Contains("agua"))
        {
            return new DesignPracticesServiceClassification(
                DesignPracticesServiceKind.Water,
                "water service tokens");
        }

        if (ContainsHydrocarbonToken(normalizedName))
        {
            return new DesignPracticesServiceClassification(
                normalizedName.Contains("vapor") || normalizedName.Contains("gas")
                    ? DesignPracticesServiceKind.HydrocarbonVapor
                    : DesignPracticesServiceKind.HydrocarbonLiquid,
                "hydrocarbon service tokens");
        }

        return new DesignPracticesServiceClassification(
            DesignPracticesServiceKind.Unknown,
            "unclassified service name");
    }

    private static bool IsCondensing(IFacadeStream inlet, IFacadeStream outlet)
    {
        var inletVaporFraction = ReadVaporFraction(inlet);
        var outletVaporFraction = ReadVaporFraction(outlet);
        return inletVaporFraction >= MostlyVaporFraction && outletVaporFraction < inletVaporFraction;
    }

    private static bool IsPureWater(IFacadeStream stream)
    {
        if (stream.Composition is null)
        {
            return IsNamedWaterService(stream.Name);
        }

        var activeComponents = stream.Composition.Components.Where(component =>
            component.MolarFraction.IsDefined && component.MolarFraction.Value.GetValue(PercentageUnits.Percentage) > TraceFractionPercent ||
            component.MassFraction.IsDefined && component.MassFraction.Value.GetValue(PercentageUnits.Percentage) > TraceFractionPercent).ToArray();

        if (activeComponents.Length == 0)
        {
            return IsNamedWaterService(stream.Name);
        }

        return activeComponents.Length == 1 && IsWaterComponent(activeComponents[0]);
    }

    private static bool HasWaterAndNonWaterComponents(IFacadeStream stream)
    {
        if (stream.Composition is null)
        {
            return false;
        }

        var activeComponents = stream.Composition.Components.Where(component =>
            component.MolarFraction.IsDefined && component.MolarFraction.Value.GetValue(PercentageUnits.Percentage) > TraceFractionPercent ||
            component.MassFraction.IsDefined && component.MassFraction.Value.GetValue(PercentageUnits.Percentage) > TraceFractionPercent).ToArray();

        return activeComponents.Any(IsWaterComponent) && activeComponents.Any(component => !IsWaterComponent(component));
    }

    private static bool IsWaterComponent(ComponentFacade component)
    {
        return string.Equals(component.Formula, "H2O", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(component.Name, "Water", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(component.Name, "Agua", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(component.Name, "Steam", StringComparison.OrdinalIgnoreCase);
    }

    private static double ReadVaporFraction(IFacadeStream stream)
    {
        if (!stream.VaporFraction.IsDefined)
        {
            return 0d;
        }

        var value = stream.VaporFraction.Value.GetValue(PercentageUnits.Percentage);
        return value > 1d ? value / 100d : value;
    }

    private static bool ContainsAqueousSolutionToken(string normalizedName)
    {
        return normalizedName.Contains("cip") ||
               normalizedName.Contains("wash") ||
               normalizedName.Contains("cleaning") ||
               normalizedName.Contains("caustic") ||
               normalizedName.Contains("acid") ||
               normalizedName.Contains("sanitizer") ||
               normalizedName.Contains("brine") ||
               normalizedName.Contains("solution") ||
               normalizedName.Contains("aqueous");
    }

    private static bool ContainsHydrocarbonToken(string normalizedName)
    {
        return normalizedName.Contains("hydrocarbon") ||
               normalizedName.Contains("hcbn") ||
               normalizedName.Contains("crude") ||
               normalizedName.Contains("oil") ||
               normalizedName.Contains("gasoline") ||
               normalizedName.Contains("debutanizer") ||
               normalizedName.Contains("deethanizer") ||
               normalizedName.Contains("depropanizer") ||
               normalizedName.Contains("stabilizer") ||
               normalizedName.Contains("splitter");
    }

    private static bool ContainsSteamCondensateTokens(string normalizedName)
    {
        return normalizedName.Contains("steam") &&
               (normalizedName.Contains("condensate") ||
                normalizedName.Contains("condensado") ||
                normalizedName.Contains("condensing"));
    }

    private static bool IsNamedWaterService(string name)
    {
        var normalizedName = NormalizeServiceName(name);
        return normalizedName.Contains("water") ||
               normalizedName.Contains("steam") ||
               normalizedName.Contains("condensate") ||
               normalizedName.Contains("agua") ||
               normalizedName.Contains("vapor de agua") ||
               normalizedName.Contains("condensado");
    }

    private static string NormalizeServiceName(string value)
    {
        return value
            .Replace("-", " ", StringComparison.Ordinal)
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace(".", " ", StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
