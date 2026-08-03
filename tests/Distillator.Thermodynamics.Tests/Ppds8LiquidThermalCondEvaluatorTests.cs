using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents.Liquido.Others;
using UnitSystem;

namespace Distillator.Thermodynamics.Tests;

public sealed class Ppds8LiquidThermalCondEvaluatorTests
{
    [Fact]
    public void EvaluateAt_UsesKelvinAndCriticalTemperature()
    {
        var coeffs = new CorrelationCoefficientsDto
        {
            C1 = 0.0641126,
            C2 = 0.61057,
            C3 = -1.72442,
            C4 = 3.94394,
            Tmin = new Temperature(250, TemperatureUnits.Kelvin),
            Tmax = new Temperature(560, TemperatureUnits.Kelvin)
        };
        var evaluator = new Ppds8LiquidThermalCondEvaluator(
            coeffs,
            new Temperature(562.05, TemperatureUnits.Kelvin));

        double value = evaluator
            .EvaluateAt(new Temperature(500, TemperatureUnits.Kelvin))
            .GetValue(ThermalConductivityUnits.W_m_K);

        Assert.Equal(0.08536381765218425, value, precision: 12);
    }

    [Fact]
    public void EvaluateAt_ReturnsZeroOutsideCoefficientRange()
    {
        var coeffs = new CorrelationCoefficientsDto
        {
            C1 = 0.0641126,
            C2 = 0.61057,
            C3 = -1.72442,
            C4 = 3.94394,
            Tmin = new Temperature(250, TemperatureUnits.Kelvin),
            Tmax = new Temperature(500, TemperatureUnits.Kelvin)
        };
        var evaluator = new Ppds8LiquidThermalCondEvaluator(
            coeffs,
            new Temperature(562.05, TemperatureUnits.Kelvin));

        double value = evaluator
            .EvaluateAt(new Temperature(520, TemperatureUnits.Kelvin))
            .GetValue(ThermalConductivityUnits.W_m_K);

        Assert.Equal(0.0, value);
    }
}
