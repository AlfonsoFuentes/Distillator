using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class DesignPracticesShellAndTubeDesignFactory : IShellAndTubeDesignFactory
{
    private const double TraceVaporFraction = 1e-8;
    private const double MostlyVaporFraction = 0.5d;

    public IHeatExchangerDesign Create(HeatExchangerDesignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DesignPracticesShellAndTubeDesign(request, ClassifyProcess(request));
    }

    private static DesignPracticesProcessRegime ClassifyProcess(HeatExchangerDesignRequest request)
    {
        var shellService = DesignPracticesServiceClassifier.Classify(
            request.ShellSideInlet.Stream,
            request.ShellSideOutlet.Stream);
        var tubeService = DesignPracticesServiceClassifier.Classify(
            request.TubeSideInlet.Stream,
            request.TubeSideOutlet.Stream);
        var shellInletVapor = GetVaporFraction(request.ShellSideInlet.Stream);
        var shellOutletVapor = GetVaporFraction(request.ShellSideOutlet.Stream);
        var tubeInletVapor = GetVaporFraction(request.TubeSideInlet.Stream);
        var tubeOutletVapor = GetVaporFraction(request.TubeSideOutlet.Stream);

        if (shellInletVapor > MostlyVaporFraction && shellOutletVapor < shellInletVapor)
        {
            return DesignPracticesProcessRegime.ShellSideCondensation;
        }

        if (tubeInletVapor > MostlyVaporFraction && tubeOutletVapor < tubeInletVapor)
        {
            return DesignPracticesProcessRegime.TubeSideCondensation;
        }

        if (shellOutletVapor > shellInletVapor + TraceVaporFraction)
        {
            return DesignPracticesProcessRegime.ShellSideVaporization;
        }

        if (tubeOutletVapor > tubeInletVapor + TraceVaporFraction)
        {
            return DesignPracticesProcessRegime.TubeSideVaporization;
        }

        if (shellService.Kind == DesignPracticesServiceKind.SteamCondensing)
        {
            return DesignPracticesProcessRegime.ShellSideCondensation;
        }

        if (tubeService.Kind == DesignPracticesServiceKind.SteamCondensing)
        {
            return DesignPracticesProcessRegime.TubeSideCondensation;
        }

        return DesignPracticesProcessRegime.NoPhaseChange;
    }

    private static double GetVaporFraction(IFacadeStream stream)
    {
        if (!stream.VaporFraction.IsDefined)
        {
            return 0d;
        }

        var value = stream.VaporFraction.Value.GetValue(PercentageUnits.Percentage);
        return value > 1d ? value / 100d : value;
    }
}

public enum DesignPracticesProcessRegime
{
    NoPhaseChange,
    ShellSideCondensation,
    TubeSideCondensation,
    ShellSideVaporization,
    TubeSideVaporization
}
