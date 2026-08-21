using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class KernShellAndTubeDesignFactory : IShellAndTubeDesignFactory
{
    private const double TraceFractionPercent = 1e-6;
    private const double VaporFractionCondensingThresholdPercent = 50d;

    public IHeatExchangerDesign Create(HeatExchangerDesignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shellSideInlet = request.ShellSideInlet.Stream;
        var shellSideOutlet = request.ShellSideOutlet.Stream;
        var tubeSideInlet = request.TubeSideInlet.Stream;
        var tubeSideOutlet = request.TubeSideOutlet.Stream;

        var isShellSideCondensing = IsCondensing(shellSideInlet, shellSideOutlet);
        var isShellSidePureWater = IsPureWater(shellSideInlet);
        var isTubeSidePureWater = IsPureWater(tubeSideInlet);
        var isTubeSideMixture = IsMixture(tubeSideInlet);
        var tubeSideProcess = GetTubeSideProcess(tubeSideInlet, tubeSideOutlet);

        if (isShellSideCondensing && tubeSideProcess == TubeSideProcess.VaporizingLiquid)
        {
            return new ShellSideVaporCondensingTubeSideVaporizingLiquidDesign(request);
        }

        if (isShellSideCondensing && tubeSideProcess == TubeSideProcess.TwoPhaseOutlet)
        {
            return new ShellSideVaporCondensingTubeSideTwoPhaseOutletDesign(request);
        }

        if (isShellSideCondensing && tubeSideProcess == TubeSideProcess.Vapor)
        {
            return new ShellSideVaporCondensingTubeSideVaporDesign(request);
        }

        if (isShellSideCondensing && isShellSidePureWater && isTubeSidePureWater)
        {
            return new ShellSidePureWaterVaporCondensingTubeSideLiquidWaterDesign(request);
        }

        if (isShellSideCondensing && isTubeSidePureWater)
        {
            return new ShellSideVaporMixtureCondensingTubeSideLiquidWaterDesign(request);
        }

        if (isShellSideCondensing && isTubeSideMixture)
        {
            return new ShellSideVaporCondensingTubeSideLiquidMixtureDesign(request);
        }

        if (tubeSideProcess == TubeSideProcess.VaporizingLiquid)
        {
            return new ShellSideLiquidCoolingTubeSideVaporizingLiquidDesign(request);
        }

        if (tubeSideProcess == TubeSideProcess.TwoPhaseOutlet)
        {
            return new ShellSideLiquidCoolingTubeSideTwoPhaseOutletDesign(request);
        }

        if (tubeSideProcess == TubeSideProcess.Vapor)
        {
            return new ShellSideLiquidCoolingTubeSideVaporDesign(request);
        }

        if (isTubeSidePureWater)
        {
            return new ShellSideLiquidCoolingTubeSideLiquidWaterDesign(request);
        }

        return new ShellSideLiquidCoolingTubeSideLiquidMixtureDesign(request);
    }

    private static bool IsCondensing(IFacadeStream inlet, IFacadeStream outlet)
    {
        if (!inlet.VaporFraction.IsDefined)
        {
            return false;
        }

        var inletVaporFraction = inlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage);
        var outletVaporFraction = outlet.VaporFraction.IsDefined
            ? outlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage)
            : inletVaporFraction;

        return inletVaporFraction >= VaporFractionCondensingThresholdPercent &&
               outletVaporFraction < inletVaporFraction;
    }

    private static TubeSideProcess GetTubeSideProcess(IFacadeStream inlet, IFacadeStream outlet)
    {
        var inletVaporFraction = GetVaporFractionPercent(inlet);
        var outletVaporFraction = GetVaporFractionPercent(outlet);

        if (inletVaporFraction >= 99d && outletVaporFraction >= 99d)
        {
            return TubeSideProcess.Vapor;
        }

        if (inletVaporFraction < 50d && outletVaporFraction > inletVaporFraction)
        {
            return TubeSideProcess.VaporizingLiquid;
        }

        if (outletVaporFraction > TraceFractionPercent && outletVaporFraction < 99d)
        {
            return TubeSideProcess.TwoPhaseOutlet;
        }

        return TubeSideProcess.Liquid;
    }

    private static double GetVaporFractionPercent(IFacadeStream stream)
    {
        return stream.VaporFraction.IsDefined
            ? stream.VaporFraction.Value.GetValue(PercentageUnits.Percentage)
            : 0d;
    }

    private static bool IsPureWater(IFacadeStream stream)
    {
        var components = GetActiveComponents(stream).ToArray();

        return components.Length == 1 && IsWater(components[0].Component);
    }

    private static bool IsMixture(IFacadeStream stream)
    {
        return GetActiveComponents(stream).Take(2).Count() > 1;
    }

    private static IEnumerable<(ComponentFacade Component, double FractionPercent)> GetActiveComponents(IFacadeStream stream)
    {
        if (stream.Composition is null)
        {
            yield break;
        }

        foreach (var component in stream.Composition.Components)
        {
            var fraction = GetDefinedFractionPercent(component);
            if (fraction > TraceFractionPercent)
            {
                yield return (component, fraction);
            }
        }
    }

    private static double GetDefinedFractionPercent(ComponentFacade component)
    {
        if (component.MolarFraction.IsDefined)
        {
            return component.MolarFraction.Value.GetValue(PercentageUnits.Percentage);
        }

        if (component.MassFraction.IsDefined)
        {
            return component.MassFraction.Value.GetValue(PercentageUnits.Percentage);
        }

        return 0d;
    }

    private static bool IsWater(ComponentFacade component)
    {
        return string.Equals(component.Formula, "H2O", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(component.Name, "Water", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(component.Name, "Agua", StringComparison.OrdinalIgnoreCase);
    }

    private enum TubeSideProcess
    {
        Liquid,
        VaporizingLiquid,
        TwoPhaseOutlet,
        Vapor
    }
}
