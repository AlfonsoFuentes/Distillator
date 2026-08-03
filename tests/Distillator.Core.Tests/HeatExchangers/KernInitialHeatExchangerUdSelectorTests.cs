using Shared.UnitOperations.HeatExchangers.Design;

namespace Distillator.Core.Tests.HeatExchangers;

public sealed class KernInitialHeatExchangerUdSelectorTests
{
    private readonly KernInitialHeatExchangerUdSelector selector = new();

    [Theory]
    [InlineData(HeatExchangerThermalService.SensibleHeatingCooling, 75d)]
    [InlineData(HeatExchangerThermalService.Condensing, 150d)]
    [InlineData(HeatExchangerThermalService.Boiling, 120d)]
    [InlineData(HeatExchangerThermalService.Unknown, 50d)]
    public void SelectUdBtuHrFt2F_ReturnsInitialDesignValue(HeatExchangerThermalService service, double expected)
    {
        var result = selector.SelectUdBtuHrFt2F(service);

        Assert.Equal(expected, result);
    }
}
