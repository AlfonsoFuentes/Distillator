using Shared.UnitOperations.HeatExchangers.Design;

namespace Distillator.Core.Tests.HeatExchangers;

public sealed class ShellAndTubeDesignEvaluatorTests
{
    private readonly ShellAndTubeDesignEvaluator evaluator = new(new KernHeatExchangerCorrelations());

    [Fact]
    public void Evaluate_ReturnsValidDesign_WhenConfigurationMeetsConstraints()
    {
        var result = evaluator.Evaluate(CreateInput());

        Assert.Equal(HeatExchangerDesignStatus.Valid, result.Status);
        Assert.True(result.InstalledAreaFt2 >= result.RequiredAreaFt2);
        Assert.All(result.Constraints, constraint => Assert.Equal(HeatExchangerConstraintStatus.Pass, constraint.Status));
        Assert.True(result.CleanOverallCoefficientBtuHrFt2F > result.DirtyOverallCoefficientBtuHrFt2F);
    }

    [Fact]
    public void Evaluate_ReturnsInvalidInput_WhenPhysicalValuesAreMissing()
    {
        var input = CreateInput() with
        {
            HeatDutyBtuHr = 0d
        };

        var result = evaluator.Evaluate(input);

        Assert.Equal(HeatExchangerDesignStatus.InvalidInput, result.Status);
        Assert.Contains("Heat duty must be greater than zero.", result.Diagnostics);
    }

    [Fact]
    public void Evaluate_ReturnsConstraintFailures_WhenAreaOrVelocityDoNotMeetLimits()
    {
        var input = CreateInput() with
        {
            Configuration = CreateConfiguration() with
            {
                TubeCount = 12
            }
        };

        var result = evaluator.Evaluate(input);

        Assert.Equal(HeatExchangerDesignStatus.ViolatesConstraints, result.Status);
        Assert.Contains(result.Constraints, constraint =>
            constraint.Name == "Tube velocity" && constraint.Status == HeatExchangerConstraintStatus.Fail);
    }

    [Fact]
    public void Evaluate_ReturnsConstraintFailure_WhenTubeCountDoesNotFitShell()
    {
        var input = CreateInput() with
        {
            Configuration = CreateConfiguration() with
            {
                TubeCount = 300
            }
        };

        var result = evaluator.Evaluate(input);

        Assert.Equal(HeatExchangerDesignStatus.ViolatesConstraints, result.Status);
        Assert.Equal(255, result.MaximumTubeCountForShell);
        Assert.Contains(result.Constraints, constraint =>
            constraint.Name == "Tube count for shell" && constraint.Status == HeatExchangerConstraintStatus.Fail);
    }

    private static ShellAndTubeDesignInput CreateInput()
    {
        return new ShellAndTubeDesignInput
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
            Configuration = CreateConfiguration(),
            Constraints = new ShellAndTubeDesignConstraints
            {
                MinimumTubeVelocityFtS = 0.5d,
                MaximumTubeVelocityFtS = 10d,
                MaximumTubePressureDropPsi = 5d,
                MaximumShellPressureDropPsi = 5d,
                MinimumFoulingResistanceHrFt2FBtu = 0.0001d
            }
        };
    }

    private static ShellAndTubeDesignConfiguration CreateConfiguration()
    {
        return new ShellAndTubeDesignConfiguration
        {
            Name = "Baseline",
            TubeOuterDiameterFt = 0.0625d,
            TubeInnerDiameterFt = 0.0517d,
            TubeLengthFt = 16d,
            TubeCount = 64,
            TubePasses = 2,
            ShellInnerDiameterFt = 1.5d,
            TubePitchFt = 0.078125d,
            BaffleSpacingFt = 0.5d
        };
    }
}
