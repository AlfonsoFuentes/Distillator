using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.HeatExchangers.Design;
using UnitSystem;

namespace Distillator.HeatExchangerDesign.Tests;

public sealed class EnerquipShellAndTubeDesignRegressionTests
{
    [Fact]
    public void CalculateRequiredTubeCount_RoundsUpSoInstalledAreaCoversRequiredArea()
    {
        var variables = new ShellAndTubeDesignVariables();
        variables.AssumedArea.SetValue(new Area(355.851d, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);
        variables.TubeSurfaceArea.SetValue(new Area(2.5767656279443845d, SurfaceUnits.Foot2), VariableDefinedBy.Equipment);

        var design = new TubeLayoutSelectionProbe(CreateProbeRequest(variables));

        design.CalculateRequiredTubeCountForTest();

        var requiredTubeCount = variables.RequiredTubeCount.Value.GetValue(UnitLessUnits.None);
        var installedArea = requiredTubeCount * variables.TubeSurfaceArea.Value.GetValue(SurfaceUnits.Foot2);

        Assert.Equal(139d, requiredTubeCount);
        Assert.True(installedArea >= variables.AssumedArea.Value.GetValue(SurfaceUnits.Foot2));
    }

    [Fact]
    public void VerifyTubeSidePressureDrop_IncreasesEquipmentTubeCountWhenPressureDropIsTooHigh()
    {
        var variables = new ShellAndTubeDesignVariables();
        variables.ActualTubeCount.SetValue(new UnitLess(138d), VariableDefinedBy.Equipment);
        variables.TubeVelocity.SetValue(new Velocity(6.097d, VelocityUnits.FeetPerSecond), VariableDefinedBy.Equipment);
        variables.MinimumTubeVelocity.SetValue(new Velocity(4.5d, VelocityUnits.FeetPerSecond), VariableDefinedBy.Equipment);
        variables.TubeSidePressureDrop.SetValue(new PressureDrop(7.847d, PressureDropUnits.psi), VariableDefinedBy.Equipment);

        var design = new TubeLayoutSelectionProbe(CreateProbeRequest(variables));

        design.VerifyTubeSidePressureDropForTest();

        Assert.True(variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None) > 138d);
    }


    [Fact]
    public void SelectActualTubeCount_ReplacesEquipmentShellDiameterWithMinimumKernShellThatFits()
    {
        var variables = new ShellAndTubeDesignVariables
        {
            TubeLayout = ShellAndTubeTubeLayout.Triangular
        };
        variables.TubeOuterDiameter.SetValue(new Diameter(1d, DiameterUnits.Inch), VariableDefinedBy.Equipment);
        variables.TubePitch.SetValue(new Diameter(1.25d, DiameterUnits.Inch), VariableDefinedBy.Equipment);
        variables.TubePasses.SetValue(new UnitLess(2d), VariableDefinedBy.Equipment);
        variables.RequiredTubeCount.SetValue(new UnitLess(99d), VariableDefinedBy.Equipment);
        variables.ActualTubeCount.SetValue(new UnitLess(10d), VariableDefinedBy.Equipment);
        variables.ShellInsideDiameter.SetValue(new Diameter(8d, DiameterUnits.Inch), VariableDefinedBy.Equipment);

        var design = new TubeLayoutSelectionProbe(CreateProbeRequest(variables));

        design.SelectActualTubeCountForTest();

        Assert.Equal(17.25d, variables.ShellInsideDiameter.Value.GetValue(DiameterUnits.Inch), precision: 2);
        Assert.Equal(118d, variables.MaximumTubeCount.Value.GetValue(UnitLessUnits.None));
        Assert.Equal(99d, variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None));
    }

    [Fact]
    public void Calculate_MatchesEnerquip65168SteamCondensingServiceWithinEngineeringTolerance()
    {
        var variables = CreateEnerquip65168Variables();
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSteamInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideCondensateOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipInlet() },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipOutlet() }
        };
        var design = new ShellSidePureWaterVaporCondensingTubeSideLiquidWaterDesign(request);

        var result = Assert.IsType<HeatExchangerDesignResult>(design.Calculate());

        var diagnostic = string.Join(
            Environment.NewLine,
            result.Message,
            $"Actual area: {variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2):0.###} ft2",
            $"Actual tube flow area: {variables.ActualTubeFlowArea.Value.GetValue(SurfaceUnits.Foot2):0.######} ft2",
            $"Tube velocity: {variables.TubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond):0.###} ft/s",
            $"Tube Reynolds: {variables.TubeSideReynoldsNumber.Value.GetValue(UnitLessUnits.None):0.###}",
            $"Tube friction: {variables.TubeSideFrictionFactor.Value.GetValue(UnitLessUnits.None):0.######}",
            $"LMTD: {variables.LogMeanTemperatureDifference.Value.GetValue(TemperatureUnits.DegreeFahrenheit):0.###} F",
            $"Tube pressure drop: {variables.TubeSidePressureDrop.Value.GetValue(PressureDropUnits.psi):0.###} psi",
            $"Shell pressure drop: {variables.ShellSidePressureDrop.Value.GetValue(PressureDropUnits.psi):0.###} psi",
            $"Clean U: {variables.CleanOverallCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F):0.###}",
            $"Actual U: {variables.ActualOverallCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F):0.###}",
            $"Dirty U: {variables.CalculatedDirtyOverallCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F):0.###}",
            $"Rd: {variables.CalculatedFoulingResistance.Value.GetValue(UnitLessUnits.None):0.######}");

        Assert.True(string.IsNullOrWhiteSpace(result.Message), diagnostic);
        AssertCalculatedByEquipment(variables.HeatDuty);
        AssertCalculatedByEquipment(variables.LogMeanTemperatureDifference);
        AssertCalculatedByEquipment(variables.TubeFlowArea);
        AssertCalculatedByEquipment(variables.TubeClearance);
        AssertCalculatedByEquipment(variables.TubeSurfaceArea);
        AssertCalculatedByEquipment(variables.ActualArea);
        AssertCalculatedByEquipment(variables.TubeVelocity);
        AssertCalculatedByEquipment(variables.TubeSidePressureDrop);
        AssertCalculatedByEquipment(variables.ShellSidePressureDrop);
        AssertCalculatedByEquipment(variables.ActualOverallCoefficient);
        AssertCalculatedByEquipment(variables.CalculatedDirtyOverallCoefficient);
        AssertCalculatedByEquipment(variables.CalculatedFoulingResistance);
        AssertCalculatedByEquipment(variables.AllowedFoulingResistance);

        Assert.InRange(variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr), 1_420_126d * 0.995d, 1_420_126d * 1.005d);
        Assert.InRange(variables.LogMeanTemperatureDifference.Value.GetValue(TemperatureUnits.DegreeFahrenheit), 138.2d * 0.99d, 138.2d * 1.01d);
        Assert.Equal(0.00035d, variables.AllowedFoulingResistance.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.InRange(variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2), 21.662d * 0.95d, 21.662d * 1.05d);
        Assert.InRange(variables.TubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond), 6.59d * 0.85d, 6.59d * 1.15d);
        var tubeSidePressureDrop = variables.TubeSidePressureDrop.Value.GetValue(PressureDropUnits.psi);
        Assert.True(tubeSidePressureDrop is >= 1.7d and <= 4.0d, diagnostic);
        Assert.Equal(
            variables.CalculatedDirtyOverallCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            variables.ActualOverallCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            precision: 6);
        Assert.InRange(variables.CalculatedDirtyOverallCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F), 474.41d * 0.8d, 474.41d * 1.2d);
    }

    private static ShellAndTubeDesignVariables CreateEnerquip65168Variables()
    {
        var variables = new ShellAndTubeDesignVariables
        {
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

    private static void AssertCalculatedByEquipment<T>(Variable<T> variable)
        where T : Amount
    {
        Assert.Equal(VariableDefinedBy.Equipment, variable.DataProcedence);
    }

    private static HeatExchangerDesignRequest CreateProbeRequest(ShellAndTubeDesignVariables variables) => new()
    {
        HeatExchangerType = HeatExchangerType.ShellAndTube,
        Variables = variables,
        ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = new FacadeStream("Shell inlet") },
        ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = new FacadeStream("Shell outlet") },
        TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = new FacadeStream("Tube inlet") },
        TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = new FacadeStream("Tube outlet") }
    };

    private static IFacadeStream CreateShellSideSteamInlet()
    {
        var stream = new FacadeStream("Shell inlet steam");
        stream.MassFlow.SetValue(new MassFlow(693.91d, MassFlowUnits.Kg_hr), VariableDefinedBy.StreamCalculated);
        stream.VolumetricFlow.SetValue(new VolumetricFlow(1d, VolumetricFlowUnits.m3_hr), VariableDefinedBy.StreamCalculated);
        stream.EnthalpyFlow.SetValue(new EnergyFlow(1_420_126d, EnergyFlowUnits.BTUhr), VariableDefinedBy.StreamCalculated);
        stream.Temperature.SetValue(new Temperature(297.248d, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.StreamCalculated);
        stream.MassDensity.SetValue(new MassDensity(2.30d, MassDensityUnits.Kg_m3), VariableDefinedBy.StreamCalculated);
        stream.Viscosity.SetValue(new Viscosity(0.0139d, ViscosityUnits.cPoise), VariableDefinedBy.StreamCalculated);
        stream.MassCp.SetValue(new MassEntropy(0.4703d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.StreamCalculated);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.0176d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.StreamCalculated);

        return stream;
    }

    private static IFacadeStream CreateShellSideCondensateOutlet()
    {
        var stream = new FacadeStream("Shell outlet condensate");
        stream.MassFlow.SetValue(new MassFlow(693.91d, MassFlowUnits.Kg_hr), VariableDefinedBy.StreamCalculated);
        stream.VolumetricFlow.SetValue(new VolumetricFlow(1d, VolumetricFlowUnits.m3_hr), VariableDefinedBy.StreamCalculated);
        stream.EnthalpyFlow.SetValue(new EnergyFlow(0d, EnergyFlowUnits.BTUhr), VariableDefinedBy.StreamCalculated);
        stream.Temperature.SetValue(new Temperature(297.122d, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.StreamCalculated);
        stream.MassDensity.SetValue(new MassDensity(919.24d, MassDensityUnits.Kg_m3), VariableDefinedBy.StreamCalculated);
        stream.Viscosity.SetValue(new Viscosity(0.1861d, ViscosityUnits.cPoise), VariableDefinedBy.StreamCalculated);
        stream.MassCp.SetValue(new MassEntropy(1.0700d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.StreamCalculated);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.3954d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.StreamCalculated);

        return stream;
    }

    private static IFacadeStream CreateTubeSideCipInlet()
    {
        var stream = new FacadeStream("Tube inlet CIP");
        stream.MassFlow.SetValue(new MassFlow(17_641d, MassFlowUnits.Kg_hr), VariableDefinedBy.StreamCalculated);
        stream.VolumetricFlow.SetValue(new VolumetricFlow(80d, VolumetricFlowUnits.gal_min), VariableDefinedBy.StreamCalculated);
        stream.EnthalpyFlow.SetValue(new EnergyFlow(0d, EnergyFlowUnits.BTUhr), VariableDefinedBy.StreamCalculated);
        stream.Temperature.SetValue(new Temperature(140d, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.StreamCalculated);
        stream.MassDensity.SetValue(new MassDensity(0.9828d * 62.4d, MassDensityUnits.lb_ft3), VariableDefinedBy.StreamCalculated);
        stream.Viscosity.SetValue(new Viscosity(0.4664d, ViscosityUnits.cPoise), VariableDefinedBy.StreamCalculated);
        stream.MassCp.SetValue(new MassEntropy(1.0114d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.StreamCalculated);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.3761d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.StreamCalculated);

        return stream;
    }

    private static IFacadeStream CreateTubeSideCipOutlet()
    {
        var stream = new FacadeStream("Tube outlet CIP");
        stream.MassFlow.SetValue(new MassFlow(17_641d, MassFlowUnits.Kg_hr), VariableDefinedBy.StreamCalculated);
        stream.VolumetricFlow.SetValue(new VolumetricFlow(80d, VolumetricFlowUnits.gal_min), VariableDefinedBy.StreamCalculated);
        stream.EnthalpyFlow.SetValue(new EnergyFlow(1_420_126d, EnergyFlowUnits.BTUhr), VariableDefinedBy.StreamCalculated);
        stream.Temperature.SetValue(new Temperature(176d, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.StreamCalculated);
        stream.MassDensity.SetValue(new MassDensity(0.9720d * 62.4d, MassDensityUnits.lb_ft3), VariableDefinedBy.StreamCalculated);
        stream.Viscosity.SetValue(new Viscosity(0.3544d, ViscosityUnits.cPoise), VariableDefinedBy.StreamCalculated);
        stream.MassCp.SetValue(new MassEntropy(1.0178d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.StreamCalculated);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.3855d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.StreamCalculated);

        return stream;
    }

    private sealed class TubeLayoutSelectionProbe(HeatExchangerDesignRequest request) : Shared.UnitOperations.HeatExchangers.Design.HeatExchangerDesign(request)
    {
        public void SelectActualTubeCountForTest() => SelectActualTubeCount();

        public void CalculateRequiredTubeCountForTest() => CalculateRequiredTubeCount();

        public void VerifyTubeSidePressureDropForTest() => VerifyTubeSidePressureDrop();

        protected override string DesignType => nameof(TubeLayoutSelectionProbe);

        protected override void CalculateTubeSideHeatTransferCoefficient()
        {
        }

        protected override void CalculateTubeSidePressureDrop()
        {
        }

        protected override void CalculateInitialAssumedDirtyOverallCoefficient()
        {
        }

        protected override void CalculateShellSideHeatTransferCoefficient()
        {
        }

        protected override void CalculateShellSidePressureDrop()
        {
        }
    }
}
