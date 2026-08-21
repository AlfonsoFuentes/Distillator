using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.HeatExchangers.Design;
using UnitSystem;

namespace Distillator.HeatExchangerDesign.Tests;

internal static class EnerquipShellAndTubeExamples
{
    private const double Enerquip47397DutyBtuPerHour = 1_801_472d;
    private static readonly Lazy<ThermodynamicMethodFullDto> WaterSteamTablesMethod =
        new(() => ThermodynamicSeedData.LoadMethod("Water (Steam Tables)"));

    public static HeatExchangerDesignRequest Create65168SteamCondensingRequest(
        ShellAndTubeCalculationStandard calculationStandard = ShellAndTubeCalculationStandard.DesignPractices)
    {
        var variables = Create65168SteamCondensingVariables(calculationStandard);
        return new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = Create65168ShellSideSteamInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = Create65168ShellSideCondensateOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = Create65168TubeSideCipInlet() },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = Create65168TubeSideCipOutlet() }
        };
    }

    public static HeatExchangerDesignRequest Create47397ProcessWaterCoolerRequest(
        ShellAndTubeCalculationStandard calculationStandard = ShellAndTubeCalculationStandard.DesignPractices)
    {
        var variables = Create47397ProcessWaterCoolerVariables(calculationStandard);
        return new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = Create47397ShellSideChilledWaterInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = Create47397ShellSideChilledWaterOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = Create47397TubeSideProcessWaterInlet() },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = Create47397TubeSideProcessWaterOutlet() }
        };
    }

    private static ShellAndTubeDesignVariables Create65168SteamCondensingVariables(ShellAndTubeCalculationStandard calculationStandard)
    {
        var variables = new ShellAndTubeDesignVariables
        {
            CalculationStandard = calculationStandard,
            TubeLayout = ShellAndTubeTubeLayout.Triangular
        };

        variables.TubeGauge.SetValue(new UnitLess(20), VariableDefinedBy.UserInput);
        variables.TubeNominalDiameter.SetValue(new Diameter(0.625d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.TubeOuterDiameter.SetValue(new Diameter(0.625d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.TubeInnerDiameter.SetValue(new Diameter(0.555d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.TubeLength.SetValue(new Length(4d, LengthUnits.Foot), VariableDefinedBy.UserInput);
        variables.BaffleSpacing.SetValue(new Length(9.5d, LengthUnits.Inch), VariableDefinedBy.UserInput);
        variables.TubePitch.SetValue(new Diameter(0.7812d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.ShellPasses.SetValue(new UnitLess(1), VariableDefinedBy.UserInput);
        variables.TubePasses.SetValue(new UnitLess(2), VariableDefinedBy.UserInput);
        variables.ActualTubeCount.SetValue(new UnitLess(32), VariableDefinedBy.UserInput);
        variables.ShellInsideDiameter.SetValue(new Diameter(6.407d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.ShellSideAllowedFoulingResistance.SetValue(new UnitLess(0.00025d), VariableDefinedBy.UserInput);
        variables.TubeSideAllowedFoulingResistance.SetValue(new UnitLess(0.00010d), VariableDefinedBy.UserInput);

        return variables;
    }

    private static ShellAndTubeDesignVariables Create47397ProcessWaterCoolerVariables(ShellAndTubeCalculationStandard calculationStandard)
    {
        var variables = new ShellAndTubeDesignVariables
        {
            CalculationStandard = calculationStandard,
            TubeLayout = ShellAndTubeTubeLayout.Triangular
        };

        variables.TubeGauge.SetValue(new UnitLess(20), VariableDefinedBy.UserInput);
        variables.TubeNominalDiameter.SetValue(new Diameter(0.375d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.TubeOuterDiameter.SetValue(new Diameter(0.375d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.TubeInnerDiameter.SetValue(new Diameter(0.305d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.TubeLength.SetValue(new Length(8d, LengthUnits.Foot), VariableDefinedBy.UserInput);
        variables.BaffleSpacing.SetValue(new Length(6d, LengthUnits.Inch), VariableDefinedBy.UserInput);
        variables.BaffleCutPercent.SetValue(new UnitLess(37d), VariableDefinedBy.UserInput);
        variables.TubePitch.SetValue(new Diameter(0.4688d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.ShellPasses.SetValue(new UnitLess(1), VariableDefinedBy.UserInput);
        variables.TubePasses.SetValue(new UnitLess(2), VariableDefinedBy.UserInput);
        variables.ActualTubeCount.SetValue(new UnitLess(100), VariableDefinedBy.UserInput);
        variables.ShellInsideDiameter.SetValue(new Diameter(8.329d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.ShellSideAllowedFoulingResistance.SetValue(new UnitLess(0.00025d), VariableDefinedBy.UserInput);
        variables.TubeSideAllowedFoulingResistance.SetValue(new UnitLess(0.00025d), VariableDefinedBy.UserInput);

        return variables;
    }

    private static IFacadeStream Create65168ShellSideSteamInlet()
    {
        var stream = CreateWaterStream("Shell inlet steam");
        stream.MassFlow.SetValue(new MassFlow(693.91d, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
        stream.Pressure.SetValue(new Pressure(4.46d, PressureUnits.Bara), VariableDefinedBy.UserInput);
        stream.Temperature.SetValue(new Temperature(419.399d, TemperatureUnits.Kelvin), VariableDefinedBy.UserInput);
        SetPureWaterComposition(stream);
        stream.ExecuteFlows();
        SetThermodynamicEnthalpyFlow(stream);
        stream.VaporFraction.SetValue(new Percentage(100d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        SetThermodynamicEnthalpyFlow(stream);

        return stream;
    }

    private static IFacadeStream Create65168ShellSideCondensateOutlet()
    {
        var stream = CreateWaterStream("Shell outlet condensate");
        stream.MassFlow.SetValue(new MassFlow(693.91d, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
        stream.Pressure.SetValue(new Pressure(4.46d, PressureUnits.Bara), VariableDefinedBy.UserInput);
        stream.Temperature.SetValue(new Temperature(419.329d, TemperatureUnits.Kelvin), VariableDefinedBy.UserInput);
        SetPureWaterComposition(stream);
        stream.ExecuteFlows();
        SetThermodynamicEnthalpyFlow(stream);
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        SetThermodynamicEnthalpyFlow(stream);

        return stream;
    }

    private static IFacadeStream Create65168TubeSideCipInlet()
    {
        var stream = CreateWaterStream("Tube inlet CIP");
        stream.MassFlow.SetValue(new MassFlow(17_641d, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
        stream.VolumetricFlow.SetValue(new VolumetricFlow(80d, VolumetricFlowUnits.gal_min), VariableDefinedBy.UserInput);
        stream.Pressure.SetValue(new Pressure(3.77d, PressureUnits.Bara), VariableDefinedBy.UserInput);
        stream.Temperature.SetValue(new Temperature(333.15d, TemperatureUnits.Kelvin), VariableDefinedBy.UserInput);
        SetPureWaterComposition(stream);
        stream.ExecuteFlows();
        SetThermodynamicEnthalpyFlow(stream);
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        SetThermodynamicEnthalpyFlow(stream);

        return stream;
    }

    private static IFacadeStream Create65168TubeSideCipOutlet()
    {
        var stream = CreateWaterStream("Tube outlet CIP");
        stream.MassFlow.SetValue(new MassFlow(17_641d, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
        stream.VolumetricFlow.SetValue(new VolumetricFlow(80d, VolumetricFlowUnits.gal_min), VariableDefinedBy.UserInput);
        stream.Pressure.SetValue(new Pressure(3.77d, PressureUnits.Bara), VariableDefinedBy.UserInput);
        stream.Temperature.SetValue(new Temperature(353.15d, TemperatureUnits.Kelvin), VariableDefinedBy.UserInput);
        SetPureWaterComposition(stream);
        stream.ExecuteFlows();
        SetThermodynamicEnthalpyFlow(stream);
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        SetThermodynamicEnthalpyFlow(stream);

        return stream;
    }

    private static IFacadeStream Create47397ShellSideChilledWaterInlet()
    {
        var stream = CreateWaterStream("Shell inlet chilled water");
        stream.MassFlow.SetValue(new MassFlow(41_290d, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
        stream.EnthalpyFlow.SetValue(new EnergyFlow(0d, EnergyFlowUnits.BTUhr), VariableDefinedBy.UserInput);
        stream.Temperature.SetValue(new Temperature(48.2d, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.UserInput);
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        stream.MassDensity.SetValue(new MassDensity(1.0004d * 62.4d, MassDensityUnits.lb_ft3), VariableDefinedBy.UserInput);
        stream.Viscosity.SetValue(new Viscosity(1.3440d, ViscosityUnits.cPoise), VariableDefinedBy.UserInput);
        stream.MassCp.SetValue(new MassEntropy(1.0028d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.UserInput);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.3353d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.UserInput);

        return stream;
    }

    private static IFacadeStream Create47397ShellSideChilledWaterOutlet()
    {
        var stream = CreateWaterStream("Shell outlet chilled water");
        stream.MassFlow.SetValue(new MassFlow(41_290d, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
        stream.EnthalpyFlow.SetValue(new EnergyFlow(Enerquip47397DutyBtuPerHour, EnergyFlowUnits.BTUhr), VariableDefinedBy.UserInput);
        stream.Temperature.SetValue(new Temperature(67.946d, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.UserInput);
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        stream.MassDensity.SetValue(new MassDensity(0.9988d * 62.4d, MassDensityUnits.lb_ft3), VariableDefinedBy.UserInput);
        stream.Viscosity.SetValue(new Viscosity(1.0021d, ViscosityUnits.cPoise), VariableDefinedBy.UserInput);
        stream.MassCp.SetValue(new MassEntropy(1.0000d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.UserInput);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.3465d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.UserInput);

        return stream;
    }

    private static IFacadeStream Create47397TubeSideProcessWaterInlet()
    {
        var stream = CreateWaterStream("Tube inlet process water");
        stream.MassFlow.SetValue(new MassFlow(22_710d, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
        stream.EnthalpyFlow.SetValue(new EnergyFlow(Enerquip47397DutyBtuPerHour, EnergyFlowUnits.BTUhr), VariableDefinedBy.UserInput);
        stream.Temperature.SetValue(new Temperature(113d, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.UserInput);
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        stream.MassDensity.SetValue(new MassDensity(0.9908d * 62.4d, MassDensityUnits.lb_ft3), VariableDefinedBy.UserInput);
        stream.Viscosity.SetValue(new Viscosity(0.5961d, ViscosityUnits.cPoise), VariableDefinedBy.UserInput);
        stream.MassCp.SetValue(new MassEntropy(0.9986d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.UserInput);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.3669d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.UserInput);

        return stream;
    }

    private static IFacadeStream Create47397TubeSideProcessWaterOutlet()
    {
        var stream = CreateWaterStream("Tube outlet process water");
        stream.MassFlow.SetValue(new MassFlow(22_710d, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
        stream.EnthalpyFlow.SetValue(new EnergyFlow(0d, EnergyFlowUnits.BTUhr), VariableDefinedBy.UserInput);
        stream.Temperature.SetValue(new Temperature(77d, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.UserInput);
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        stream.MassDensity.SetValue(new MassDensity(0.9976d * 62.4d, MassDensityUnits.lb_ft3), VariableDefinedBy.UserInput);
        stream.Viscosity.SetValue(new Viscosity(0.8900d, ViscosityUnits.cPoise), VariableDefinedBy.UserInput);
        stream.MassCp.SetValue(new MassEntropy(0.9993d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.UserInput);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.3511d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.UserInput);

        return stream;
    }

    private static FacadeStream CreateWaterStream(string name)
    {
        var stream = new FacadeStream(name);
        stream.SetThermodynamicMethod(WaterSteamTablesMethod.Value);
        return stream;
    }

    private static void SetPureWaterComposition(IFacadeStream stream)
    {
        var water = stream.Composition.Components.Single();
        water.MolarFraction.SetValue(new Percentage(100d, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        stream.Composition.InputType = ComponentInputType.MolarFraction;
        stream.Composition.CompositionChanged();
    }

    private static void SetThermodynamicEnthalpyFlow(IFacadeStream stream)
    {
        if (!stream.MassFlow.IsDefined || !stream.MassEnthalpy.IsDefined)
        {
            return;
        }

        var massFlowKgPerHour = stream.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);
        var massEnthalpyKjPerKg = stream.MassEnthalpy.Value.GetValue(MassEnergyUnits.KJ_Kg);
        stream.EnthalpyFlow.SetValue(
            new EnergyFlow(massFlowKgPerHour * massEnthalpyKjPerKg, EnergyFlowUnits.KJ_hr),
            VariableDefinedBy.StreamCalculated);
    }
}
