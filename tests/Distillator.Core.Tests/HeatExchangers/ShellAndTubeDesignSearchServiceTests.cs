using Shared.UnitOperations.HeatExchangers.Design;

namespace Distillator.Core.Tests.HeatExchangers;

public sealed class ShellAndTubeDesignSearchServiceTests
{
    private readonly ShellAndTubeDesignSearchService searchService = new(
        new ShellAndTubeDesignEvaluator(new KernHeatExchangerCorrelations()),
        new KernShellTubeCapacityCorrelation());

    [Fact]
    public void Search_ReturnsRecommendedCandidate_WhenAtLeastOneConfigurationMeetsConstraints()
    {
        var result = searchService.Search(CreateInput());

        Assert.NotEmpty(result.Candidates);
        Assert.NotNull(result.Recommended);
        Assert.True(result.Recommended.IsValid);
        Assert.True(result.Recommended.Result.TubeCountFitsShell());
    }

    [Fact]
    public void Search_ReturnsDiagnostics_WhenCatalogOptionsAreMissing()
    {
        var input = CreateInput() with
        {
            Options = CreateOptions() with
            {
                TubeSpecifications = Array.Empty<HeatExchangerTubeSpecification>()
            }
        };

        var result = searchService.Search(input);

        Assert.Empty(result.Candidates);
        Assert.Contains("At least one tube specification is required.", result.Diagnostics);
    }

    [Fact]
    public void Search_ReturnsDiagnostics_WhenCandidateValuesAreNotPhysical()
    {
        var input = CreateInput() with
        {
            AssumedDirtyOverallCoefficientBtuHrFt2F = 0d
        };

        var result = searchService.Search(input);

        Assert.Empty(result.Candidates);
        Assert.Contains("Assumed dirty overall coefficient must be greater than zero.", result.Diagnostics);
    }

    private static ShellAndTubeDesignSearchInput CreateInput()
    {
        return new ShellAndTubeDesignSearchInput
        {
            TubeSide = new ShellAndTubeProcessSide
            {
                MassFlowLbHr = 20_000d,
                DensityLbFt3 = 62d,
                ViscosityLbFtHr = 2.42d,
                HeatCapacityBtuLbF = 1d,
                ThermalConductivityBtuHrFtF = 0.35d,
                AverageTemperatureF = 80d
            },
            ShellSide = new ShellAndTubeProcessSide
            {
                MassFlowLbHr = 30_000d,
                DensityLbFt3 = 50d,
                ViscosityLbFtHr = 1.8d,
                HeatCapacityBtuLbF = 0.75d,
                ThermalConductivityBtuHrFtF = 0.08d,
                AverageTemperatureF = 180d
            },
            HeatDutyBtuHr = 500_000d,
            LogMeanTemperatureDifferenceF = 100d,
            AssumedDirtyOverallCoefficientBtuHrFt2F = 80d,
            ThermalService = HeatExchangerThermalService.SensibleHeatingCooling,
            Constraints = new ShellAndTubeDesignConstraints
            {
                MinimumTubeVelocityFtS = 0.5d,
                MaximumTubeVelocityFtS = 10d,
                MaximumTubePressureDropPsi = 5d,
                MaximumShellPressureDropPsi = 5d,
                MinimumFoulingResistanceHrFt2FBtu = 0.0001d
            },
            Options = CreateOptions()
        };
    }

    private static ShellAndTubeDesignSearchOptions CreateOptions()
    {
        return new ShellAndTubeDesignSearchOptions
        {
            TubeSpecifications =
            [
                new HeatExchangerTubeSpecification
                {
                    Standard = "Kern",
                    NominalSize = "3/4 in",
                    Gauge = "16 BWG",
                    OuterDiameterFt = 0.0625d,
                    InnerDiameterFt = 0.0517d
                }
            ],
            TubeLengthsFt = [8d, 12d, 16d],
            ShellInnerDiametersFt = [1d, 1.25d, 1.5d],
            TubePasses = [1, 2, 4],
            Layouts =
            [
                new TubeLayoutSpecification
                {
                    Pattern = TubeLayoutPattern.Triangular,
                    PitchFt = 0.078125d
                }
            ],
            MaxCandidates = 5
        };
    }
}

internal static class ShellAndTubeDesignResultTestExtensions
{
    public static bool TubeCountFitsShell(this ShellAndTubeDesignResult result)
    {
        return result.Configuration.TubeCount <= result.MaximumTubeCountForShell;
    }
}
