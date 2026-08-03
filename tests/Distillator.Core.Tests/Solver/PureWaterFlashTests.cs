using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Distillator.Core.Tests.Solver;

public sealed class PureWaterFlashTests
{
    [Fact]
    [Trait("Spec", "TEST")]
    [Trait("Level", "Unit")]
    public void PureWater_PTSubcooledLiquid_ShouldCalculateVaporFractionAndProperties()
    {
        UnitManager.RegisterByAssembly(typeof(SIUnitTypes).Assembly);

        var stream = new FacadeStream("S-101");
        stream.SetThermodynamicMethod(CreateWaterSteamTablesMethod());

        stream.Temperature.SetValue(new Temperature(25, TemperatureUnits.DegreeCelcius), VariableDefinedBy.UserInput);
        stream.Pressure.SetValue(new Pressure(101325, PressureUnits.Pascala), VariableDefinedBy.UserInput);
        stream.MassFlow.SetValue(new MassFlow(1000, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);

        var water = stream.Composition.Components.Single();
        water.MassFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput);

        Assert.Equal(ThermodynamicState.SubcooledLiquid, stream.ThermodynamicState);
        Assert.True(stream.VaporFraction.IsDefined);
        Assert.Equal(0, stream.VaporFraction.Value.GetValue(PercentageUnits.Percentage), 6);
        Assert.True(stream.MassDensity.IsDefined);
        Assert.True(stream.Viscosity.IsDefined);
        Assert.True(stream.ThermalConductivity.IsDefined);
    }

    private static ThermodynamicMethodFullDto CreateWaterSteamTablesMethod()
    {
        var waterId = Guid.Parse("b595e4f2-54db-4058-a9c2-d36b7f88962c");

        return new ThermodynamicMethodFullDto
        {
            Id = Guid.Parse("9eb8f228-9d88-4ff2-9952-bf673d5f1a5a"),
            Name = "Water (Steam Tables)",
            LiquidModel = LiquidPhaseModel.SteamTables,
            VaporModel = VaporPhaseModel.SteamTables,
            Components =
            [
                new MethodComponentFullDto
                {
                    ComponentId = waterId,
                    ComponentName = "Water",
                    MatrixIndex = 0,
                    FullData = CreateWaterComponent(waterId)
                }
            ]
        };
    }

    private static ChemicalComponentDto CreateWaterComponent(Guid id)
    {
        return new ChemicalComponentDto
        {
            Id = id,
            Name = "Water",
            Formula = "H2O",
            StructuralFormula = "H2O",
            Family = "Inorganic",
            SecondaryFamily = "Water",
            MolecularWeight = 18.0151,
            CriticalTemperature = new Temperature(647.096, TemperatureUnits.Kelvin),
            CriticalPressure = new Pressure(22064, PressureUnits.KiloPascala),
            CriticalVolume = new MolarVolumeSpecific(0.0559, MolarVolumeSpecificUnits.m3_Kgmol),
            VolumeAsterisk = new MolarVolumeSpecific(0.0436, MolarVolumeSpecificUnits.m3_Kgmol),
            CriticalZ = 0.229,
            AcentricFactor = 0.344,
            AcentricFactorPitzer = 0.344,
            BoilingPoint = new Temperature(373.14, TemperatureUnits.Kelvin),
            MeltingPoint = new Temperature(273.15, TemperatureUnits.Kelvin),
            SurfaceTension = new CorrelationCoefficientsDto
            {
                C1 = 177.66,
                C2 = -256.7,
                C3 = -360,
                C4 = 1.9699,
                Tmin = new Temperature(273.16, TemperatureUnits.Kelvin),
                Tmax = new Temperature(647.1, TemperatureUnits.Kelvin)
            }
        };
    }
}
