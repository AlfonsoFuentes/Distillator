using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.HeatExchangers.Design;
using UnitSystem;

namespace Distillator.Core.Tests.HeatExchangers;

public sealed class ShellAndTubeDesignInputBuilderTests
{
    private readonly ShellAndTubeDesignInputBuilder builder = new(
        new HeatExchangerThermalServiceClassifier(),
        new KernInitialHeatExchangerUdSelector());

    [Fact]
    public void Build_ReturnsSearchInput_WhenStreamsContainDesignProperties()
    {
        var heatExchanger = CreateHeatExchanger();

        var result = builder.Build(heatExchanger, CreateOptions());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Input);
        Assert.Equal(HeatExchangerThermalService.SensibleHeatingCooling, result.ThermalService);
        Assert.Equal(75d, result.Input.AssumedDirtyOverallCoefficientBtuHrFt2F);
        Assert.True(result.Input.HeatDutyBtuHr > 0d);
        Assert.True(result.Input.LogMeanTemperatureDifferenceF > 0d);
    }

    [Fact]
    public void Build_ReturnsDiagnostics_WhenHeatExchangerIsNotConnected()
    {
        var result = builder.Build(new SolverHeatExchanger("E-100"), CreateOptions());

        Assert.False(result.Succeeded);
        Assert.Contains("Hot inlet stream is required.", result.Diagnostics);
        Assert.Contains("Cold outlet stream is required.", result.Diagnostics);
    }

    private static SolverHeatExchanger CreateHeatExchanger()
    {
        var heatExchanger = new SolverHeatExchanger("E-100");
        heatExchanger.SetHotInlet(CreateStream("Hot In", 220d, 0d, 120_000d));
        heatExchanger.SetHotOutlet(CreateStream("Hot Out", 160d, 0d, 20_000d));
        heatExchanger.SetColdInlet(CreateStream("Cold In", 80d, 0d, 10_000d));
        heatExchanger.SetColdOutlet(CreateStream("Cold Out", 130d, 0d, 105_000d));

        return heatExchanger;
    }

    private static FacadeStream CreateStream(string name, double temperatureF, double vaporFraction, double enthalpyFlowBtuHr)
    {
        var stream = new FacadeStream(name);
        stream.Temperature.SetValue(new Temperature(temperatureF, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.UserInput);
        stream.MassFlow.SetValue(new MassFlow(20_000d, MassFlowUnits.lb_hr), VariableDefinedBy.UserInput);
        stream.EnthalpyFlow.SetValue(new EnergyFlow(enthalpyFlowBtuHr, EnergyFlowUnits.BTUhr), VariableDefinedBy.UserInput);
        stream.VaporFraction.SetValue(new Percentage(vaporFraction * 100d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        stream.MassDensity.SetValue(new MassDensity(62d, MassDensityUnits.lb_ft3), VariableDefinedBy.StreamCalculated);
        stream.Viscosity.SetValue(new Viscosity(2.42d, ViscosityUnits.lb_ft_hr), VariableDefinedBy.StreamCalculated);
        stream.MassCp.SetValue(new MassEntropy(1d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.StreamCalculated);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.35d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.StreamCalculated);

        return stream;
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
            TubeLengthsFt = [16d],
            ShellInnerDiametersFt = [1.5d],
            TubePasses = [2]
        };
    }
}
