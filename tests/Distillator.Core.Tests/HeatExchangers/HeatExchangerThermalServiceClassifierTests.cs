using Shared.UnitOperations.HeatExchangers.Design;

namespace Distillator.Core.Tests.HeatExchangers;

public sealed class HeatExchangerThermalServiceClassifierTests
{
    private readonly HeatExchangerThermalServiceClassifier classifier = new();

    [Fact]
    public void Classify_ReturnsCondensing_WhenHotSideVaporFractionDrops()
    {
        var service = classifier.Classify(CreateSnapshot() with
        {
            HotInletVaporFraction = 1d,
            HotOutletVaporFraction = 0d
        });

        Assert.Equal(HeatExchangerThermalService.Condensing, service);
    }

    [Fact]
    public void Classify_ReturnsBoiling_WhenColdSideVaporFractionIncreases()
    {
        var service = classifier.Classify(CreateSnapshot() with
        {
            ColdInletVaporFraction = 0d,
            ColdOutletVaporFraction = 1d
        });

        Assert.Equal(HeatExchangerThermalService.Boiling, service);
    }

    [Fact]
    public void Classify_ReturnsSensibleHeatingCooling_WhenHotCoolsAndColdHeats()
    {
        var service = classifier.Classify(CreateSnapshot());

        Assert.Equal(HeatExchangerThermalService.SensibleHeatingCooling, service);
    }

    private static HeatExchangerThermalServiceSnapshot CreateSnapshot()
    {
        return new HeatExchangerThermalServiceSnapshot
        {
            HotInletTemperatureF = 220d,
            HotOutletTemperatureF = 160d,
            ColdInletTemperatureF = 80d,
            ColdOutletTemperatureF = 130d,
            HotInletVaporFraction = 0d,
            HotOutletVaporFraction = 0d,
            ColdInletVaporFraction = 0d,
            ColdOutletVaporFraction = 0d
        };
    }
}
