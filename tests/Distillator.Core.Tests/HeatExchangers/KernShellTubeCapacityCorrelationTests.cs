using Shared.UnitOperations.HeatExchangers.Design;

namespace Distillator.Core.Tests.HeatExchangers;

public sealed class KernShellTubeCapacityCorrelationTests
{
    private readonly KernShellTubeCapacityCorrelation capacity = new();

    [Fact]
    public void EstimateMaximumTubeCount_ReturnsTriangularCapacityFromLegacyCorrelation()
    {
        var result = capacity.EstimateMaximumTubeCount(new ShellTubeCapacityRequest
        {
            ShellInnerDiameterFt = 1.5d,
            TubeOuterDiameterFt = 0.0625d,
            TubePitchFt = 0.078125d,
            TubePasses = 2,
            LayoutPattern = TubeLayoutPattern.Triangular
        });

        Assert.Equal(255, result.MaximumTubeCount);
    }

    [Fact]
    public void EstimateMaximumTubeCount_RejectsPitchSmallerThanTubeOuterDiameter()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            capacity.EstimateMaximumTubeCount(new ShellTubeCapacityRequest
            {
                ShellInnerDiameterFt = 1.5d,
                TubeOuterDiameterFt = 0.0625d,
                TubePitchFt = 0.05d,
                TubePasses = 2,
                LayoutPattern = TubeLayoutPattern.Triangular
            }));
    }
}
