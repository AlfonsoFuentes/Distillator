using Shared.UnitOperations.HeatExchangers.Design;

namespace Distillator.Core.Tests.HeatExchangers;

public sealed class KernHeatExchangerCorrelationsTests
{
    private readonly KernHeatExchangerCorrelations correlations = new();

    [Fact]
    public void HeatTransferFactor_ReturnsTubeSideTransitionCorrelation()
    {
        var result = correlations.HeatTransferFactor(KernCorrelationSide.Tube, 8_220d, hasBaffles: false);

        Assert.Equal(35.2535858090488d, result, precision: 10);
    }

    [Fact]
    public void FrictionFactor_ReturnsTubeSideTurbulentCorrelation()
    {
        var result = correlations.FrictionFactor(KernCorrelationSide.Tube, 8_220d, hasBaffles: false);

        Assert.Equal(0.000279922434255285d, result, precision: 15);
    }

    [Fact]
    public void HeatTransferFactor_ReturnsShellSideBaffleCorrelation()
    {
        var result = correlations.HeatTransferFactor(KernCorrelationSide.Shell, 25_300d, hasBaffles: true);

        Assert.Equal(97.0651464443651d, result, precision: 10);
    }

    [Fact]
    public void FrictionFactor_ReturnsShellSideBaffleCorrelation()
    {
        var result = correlations.FrictionFactor(KernCorrelationSide.Shell, 25_300d, hasBaffles: true);

        Assert.Equal(0.00170814975882592d, result, precision: 15);
    }

    [Fact]
    public void HeatTransferFactor_ReturnsShellSideNoBaffleCorrelation()
    {
        var result = correlations.HeatTransferFactor(KernCorrelationSide.Shell, 25_300d, hasBaffles: false);

        Assert.Equal(86.8201239160814d, result, precision: 10);
    }

    [Fact]
    public void FrictionFactor_ReturnsShellSideNoBaffleCorrelation()
    {
        var result = correlations.FrictionFactor(KernCorrelationSide.Shell, 25_300d, hasBaffles: false);

        Assert.Equal(0.000210812081177719d, result, precision: 15);
    }

    [Theory]
    [InlineData(25d, 0.0180783607481986d)]
    [InlineData(100d, 0.0062419753610513d)]
    [InlineData(25_300d, 0.00170814975882592d)]
    public void FrictionFactor_ReturnsShellSideBaffleCorrelationByReynoldsRange(double reynoldsNumber, double expected)
    {
        var result = correlations.FrictionFactor(KernCorrelationSide.Shell, reynoldsNumber, hasBaffles: true);

        Assert.Equal(expected, result, precision: 15);
    }

    [Fact]
    public void Correlations_RejectInvalidReynoldsNumber()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            correlations.HeatTransferFactor(KernCorrelationSide.Tube, 0d, hasBaffles: false));
    }
}
