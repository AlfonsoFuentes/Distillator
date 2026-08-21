using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Methods;
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

    [Fact]
    public void ShellAndTubeDesignFactory_UsesDesignPracticesEngineWhenSelected()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        var request = CreateEnerquip65168Request(variables);

        var design = new ShellAndTubeDesignFactory().Create(request);

        var result = Assert.IsType<HeatExchangerDesignResult>(design.Calculate());
        Assert.Equal(ShellAndTubeCalculationStandard.DesignPractices, result.CalculationStandard);
        Assert.StartsWith("DesignPracticesShellAndTubeDesign", result.DesignType);
        Assert.NotEmpty(result.Recommendations);
        AssertCalculatedByEquipment(variables.HeatDuty);
        AssertCalculatedByEquipment(variables.ActualArea);
        AssertCalculatedByEquipment(variables.TubeCountCorrectionFactor);
        AssertCalculatedByEquipment(variables.UTubeBendTubeLossFraction);
        AssertCalculatedByEquipment(variables.ImpingementTubeLossFraction);
        AssertCalculatedByEquipment(variables.ShellNozzleTubeLossFraction);
        AssertCalculatedByEquipment(variables.TypicalOverallCoefficientMinimum);
        AssertCalculatedByEquipment(variables.TypicalOverallCoefficientMaximum);
        AssertCalculatedByEquipment(variables.AreaOverdesignPercent);
        AssertCalculatedByEquipment(variables.LogMeanTemperatureCorrectionFactor);
        AssertCalculatedByEquipment(variables.CleanOverallCoefficient);
        AssertCalculatedByEquipment(variables.TubeSidePrandtlNumber);
        AssertCalculatedByEquipment(variables.ShellSidePrandtlNumber);
        AssertCalculatedByEquipment(variables.ShellSideNominalCrossflowFraction);
        AssertCalculatedByEquipment(variables.ShellSideHeatTransferCrossflowFraction);
        AssertCalculatedByEquipment(variables.FoulingResistanceFraction);
        AssertCalculatedByEquipment(variables.TubeSideAllowedPressureDrop);
        AssertCalculatedByEquipment(variables.ShellSideAllowedPressureDrop);
        AssertCalculatedByEquipment(variables.TubeSidePressureDropUtilization);
        AssertCalculatedByEquipment(variables.ShellSidePressureDropUtilization);
        AssertCalculatedByEquipment(variables.TubeSideNozzleDiameter);
        AssertCalculatedByEquipment(variables.TubeSideNozzleVelocity);
        AssertCalculatedByEquipment(variables.TubeSideNozzlePressureDrop);
        AssertCalculatedByEquipment(variables.ShellSideNozzleDiameter);
        AssertCalculatedByEquipment(variables.ShellSideNozzleVelocity);
        AssertCalculatedByEquipment(variables.ShellSideNozzlePressureDrop);
        AssertCalculatedByEquipment(variables.TubeSideViscosityGradientCorrectionFactor);
        AssertCalculatedByEquipment(variables.TubeSidePressureDropNaturalConvectionCorrectionFactor);
        AssertCalculatedByEquipment(variables.CondensationZoneCount);
        AssertCalculatedByEquipment(variables.DesuperheatingDutyFraction);
        AssertCalculatedByEquipment(variables.CondensationDutyFraction);
        AssertCalculatedByEquipment(variables.SubcoolingDutyFraction);
        AssertCalculatedByEquipment(variables.DesuperheatingAreaFraction);
        AssertCalculatedByEquipment(variables.CondensationAreaFraction);
        AssertCalculatedByEquipment(variables.SubcoolingAreaFraction);
        AssertCalculatedByEquipment(variables.CondensingZoneHeatTransferCoefficient);
        AssertCalculatedByEquipment(variables.VaporCoolingZoneHeatTransferCoefficient);
        AssertCalculatedByEquipment(variables.LiquidCoolingZoneHeatTransferCoefficient);
        AssertCalculatedByEquipment(variables.BottomFlowLiquidCoolingHeatTransferCoefficient);
        AssertCalculatedByEquipment(variables.DripCoolingHeatTransferCoefficient);
        AssertCalculatedByEquipment(variables.CondensingAreaIterationCount);
        AssertCalculatedByEquipment(variables.CondensingIteratedRequiredArea);
        AssertCalculatedByEquipment(variables.CondensingVaporFreeAreaFraction);
        AssertCalculatedByEquipment(variables.CondensingVaporMassVelocity);
        AssertCalculatedByEquipment(variables.CondensingTwoPhaseAverageDensity);
        AssertCalculatedByEquipment(variables.CondensingPressureDropDensityCorrectionFactor);
        AssertCalculatedByEquipment(variables.DutyWeightedCondensingSideHeatTransferCoefficient);
        Assert.True(variables.ShellSideNominalCrossflowFraction.Value.GetValue(UnitLessUnits.None) > 0d);
        Assert.InRange(variables.LogMeanTemperatureCorrectionFactor.Value.GetValue(UnitLessUnits.None), 0.99d, 1.0d);
        Assert.Equal(0.921283d, variables.TubeCountCorrectionFactor.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0.06d, variables.UTubeBendTubeLossFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0d, variables.ImpingementTubeLossFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0.018717d, variables.ShellNozzleTubeLossFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.True(variables.ShellSideHeatTransferCrossflowFraction.Value.GetValue(UnitLessUnits.None) >= variables.ShellSideNominalCrossflowFraction.Value.GetValue(UnitLessUnits.None));
        Assert.InRange(variables.FoulingResistanceFraction.Value.GetValue(UnitLessUnits.None), 0d, 1d);
        Assert.True(double.IsFinite(variables.AreaOverdesignPercent.Value.GetValue(UnitLessUnits.None)));
        Assert.Equal(17.5d, variables.TubeSideAllowedPressureDrop.Value.GetValue(PressureDropUnits.psi), precision: 6);
        Assert.Equal(1.25d, variables.ShellSideAllowedPressureDrop.Value.GetValue(PressureDropUnits.psi), precision: 6);
        var tubePressureDropUtilization = variables.TubeSidePressureDropUtilization.Value.GetValue(UnitLessUnits.None);
        var shellPressureDropUtilization = variables.ShellSidePressureDropUtilization.Value.GetValue(UnitLessUnits.None);
        Assert.True(double.IsFinite(tubePressureDropUtilization) && tubePressureDropUtilization >= 0d);
        Assert.True(double.IsFinite(shellPressureDropUtilization) && shellPressureDropUtilization >= 0d);
        Assert.True(tubePressureDropUtilization > 1d || shellPressureDropUtilization > 1d);
        Assert.True(variables.TubeSideNozzleDiameter.Value.GetValue(DiameterUnits.Inch) > 0d);
        Assert.True(variables.ShellSideNozzleDiameter.Value.GetValue(DiameterUnits.Inch) > 0d);
        Assert.True(variables.TubeSideNozzlePressureDrop.Value.GetValue(PressureDropUnits.psi) >= 0d);
        Assert.True(variables.ShellSideNozzlePressureDrop.Value.GetValue(PressureDropUnits.psi) >= 0d);
        Assert.InRange(variables.TubeSideViscosityGradientCorrectionFactor.Value.GetValue(UnitLessUnits.None), 0.90d, 1.0d);
        Assert.True(variables.TubeSidePressureDropNaturalConvectionCorrectionFactor.Value.GetValue(UnitLessUnits.None) >= 1d);
        Assert.Equal(1d, variables.CondensationZoneCount.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0d, variables.DesuperheatingDutyFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(1d, variables.CondensationDutyFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0d, variables.SubcoolingDutyFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0d, variables.DesuperheatingAreaFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(1d, variables.CondensationAreaFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0d, variables.SubcoolingAreaFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0.5d, variables.CondensingVaporFreeAreaFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.True(variables.CondensingAreaIterationCount.Value.GetValue(UnitLessUnits.None) > 0d);
        Assert.True(variables.CondensingIteratedRequiredArea.Value.GetValue(SurfaceUnits.Foot2) > 0d);
        Assert.True(variables.CondensingVaporMassVelocity.Value.GetValue(UnitLessUnits.None) > 0d);
        Assert.True(variables.CondensingTwoPhaseAverageDensity.Value.GetValue(UnitLessUnits.None) > 0d);
        Assert.True(variables.CondensingPressureDropDensityCorrectionFactor.Value.GetValue(UnitLessUnits.None) > 0d);
        Assert.Equal(
            variables.VaporCoolingZoneHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            variables.BottomFlowLiquidCoolingHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            precision: 6);
        Assert.Equal(
            variables.CondensingZoneHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F) * 1.5d,
            variables.DripCoolingHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            precision: 6);
        Assert.True(variables.LiquidCoolingZoneHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F) > 0d);
        Assert.True(variables.DutyWeightedCondensingSideHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F) > 0d);
        Assert.Equal(400d, variables.TypicalOverallCoefficientMinimum.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F), precision: 6);
        Assert.Equal(600d, variables.TypicalOverallCoefficientMaximum.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F), precision: 6);
        Assert.Equal(ShellAndTubeBaffleType.SingleSegmental, variables.BaffleType);
        Assert.Equal(25d, variables.BaffleCutPercent.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("initial area uses", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("nozzle pressure drop", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("pressure-drop utilization", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("preliminary condenser duty split", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("preliminary condenser area split", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("duty-weighted zone coefficient", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("condensation area iteration", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("bottom-flow/drip split", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("vapor free-area fraction", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("tube-count capacity", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("tube-side water/aqueous coefficient", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("Figure 1.9 and Figure 1.10 non-water friction corrections are not applied", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("baffle pitch/bundle diameter", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("DP09B Table 11", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShellAndTubeDesignFactory_UsesKernEngineWhenSelected()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.Kern;
        var request = CreateEnerquip65168Request(variables);

        var design = new ShellAndTubeDesignFactory().Create(request);

        var result = Assert.IsType<HeatExchangerDesignResult>(design.Calculate());
        Assert.Equal(ShellAndTubeCalculationStandard.Kern, result.CalculationStandard);
        Assert.False(
            result.DesignType.StartsWith("DesignPracticesShellAndTubeDesign", StringComparison.Ordinal),
            $"Expected Kern design engine, but selected '{result.DesignType}'.");
    }

    [Fact]
    public void DesignPracticesEngine_LimitsTemaFShellPressureDropAllowanceToDp09bRange()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.ShellType = ShellAndTubeShellType.TwoPass;
        var shellInlet = CreateShellSideSteamInlet();
        var shellOutlet = CreateShellSideCondensateOutlet();
        shellInlet.Pressure.SetValue(new Pressure(150d, PressureUnits.Psia), VariableDefinedBy.StreamCalculated);
        shellOutlet.Pressure.SetValue(new Pressure(150d, PressureUnits.Psia), VariableDefinedBy.StreamCalculated);
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = shellInlet },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = shellOutlet },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipInlet() },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipOutlet() }
        };

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        AssertCalculatedByEquipment(variables.TubeSideAllowedPressureDrop);
        AssertCalculatedByEquipment(variables.ShellSideAllowedPressureDrop);
        Assert.Equal(17.5d, variables.TubeSideAllowedPressureDrop.Value.GetValue(PressureDropUnits.psi), precision: 6);
        Assert.Equal(7.5d, variables.ShellSideAllowedPressureDrop.Value.GetValue(PressureDropUnits.psi), precision: 6);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("TEMA F shell-side pressure drop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_UsesDp09fInsideTubeCondensationWhenTubeSideCondenses()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSteamInlet() },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideCondensateOutlet() }
        };

        var design = new ShellAndTubeDesignFactory().Create(request);

        var result = Assert.IsType<HeatExchangerDesignResult>(design.Calculate());

        Assert.Contains("TubeSideCondensation", result.DesignType);
        AssertCalculatedByEquipment(variables.CondensingZoneHeatTransferCoefficient);
        AssertCalculatedByEquipment(variables.DutyWeightedCondensingSideHeatTransferCoefficient);
        AssertCalculatedByEquipment(variables.CondensingTwoPhaseAverageDensity);
        AssertCalculatedByEquipment(variables.CondensingPressureDropDensityCorrectionFactor);
        Assert.True(variables.CondensingZoneHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F) > 0d);
        Assert.True(variables.CondensingTwoPhaseAverageDensity.Value.GetValue(UnitLessUnits.None) > 0d);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("Akers-Deans-Crosser", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("tube-side condensing pressure drop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsCoolingWaterOutletBulkTemperatureLimit()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.CoolingWaterType = ShellAndTubeCoolingWaterType.FreshWater;
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        AssertCalculatedByEquipment(variables.CoolingWaterOutletBulkTemperatureLimit);
        AssertCalculatedByEquipment(variables.CoolingWaterEstimatedTubeWallTemperature);
        AssertCalculatedByEquipment(variables.CoolingWaterEstimatedFilmTemperature);
        AssertCalculatedByEquipment(variables.CoolingWaterFilmTemperatureLimit);
        AssertCalculatedByEquipment(variables.CoolingWaterMinimumTubeVelocity);
        AssertCalculatedByEquipment(variables.CoolingWaterMaximumTubeVelocity);
        Assert.Equal(130d, variables.CoolingWaterOutletBulkTemperatureLimit.Value.GetValue(TemperatureUnits.DegreeFahrenheit), precision: 6);
        Assert.Equal(150d, variables.CoolingWaterFilmTemperatureLimit.Value.GetValue(TemperatureUnits.DegreeFahrenheit), precision: 6);
        Assert.True(variables.CoolingWaterEstimatedTubeWallTemperature.Value.GetValue(TemperatureUnits.DegreeFahrenheit) > 176d);
        Assert.True(variables.CoolingWaterEstimatedFilmTemperature.Value.GetValue(TemperatureUnits.DegreeFahrenheit) > 176d);
        Assert.Equal(3d, variables.CoolingWaterMinimumTubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond), precision: 6);
        Assert.Equal(6d, variables.CoolingWaterMaximumTubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond), precision: 6);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("cooling water outlet bulk temperature", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("above the 130", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("estimated fresh water film temperature", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("cooling-water velocity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_UsesNamedDp09bTable1RangeForInitialAreaWhenServiceMatches()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        var request = CreateEnerquip65168Request(variables);
        request.ShellSideInlet.Stream.Name = "Debutanizer overhead vapor";
        request.ShellSideOutlet.Stream.Name = "Debutanizer overhead condensate";
        request.TubeSideInlet.Stream.Name = "Cooling water inlet";
        request.TubeSideOutlet.Stream.Name = "Cooling water outlet";

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Equal(90d, variables.TypicalOverallCoefficientMinimum.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F), precision: 6);
        Assert.Equal(100d, variables.TypicalOverallCoefficientMaximum.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F), precision: 6);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("debutanizer overhead condenser", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsNonEShellTypeAsPreliminaryHydraulicReview()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.ShellType = ShellAndTubeShellType.CrossFlow;
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Equal(ShellAndTubeShellType.CrossFlow, variables.ShellType);
        Assert.Contains(result.RequiredMethodImplementations, method => method.Contains("TEMA X shell", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("selected TEMA shell type X", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("E-shell assumptions", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_IncreasesDp09cTubeCountLossForLargeShellNozzle()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.ShellSideNozzleDiameter.SetValue(new Diameter(4d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        AssertCalculatedByEquipment(variables.ShellNozzleTubeLossFraction);
        Assert.True(variables.ShellNozzleTubeLossFraction.Value.GetValue(UnitLessUnits.None) > 0.02d);
        Assert.True(variables.TubeCountCorrectionFactor.Value.GetValue(UnitLessUnits.None) < 0.90d);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("Figure 8 shell-nozzle/impingement", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsNonSingleSegmentalBafflesAsPreliminaryHydraulicReview()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.BaffleType = ShellAndTubeBaffleType.HelicalBaffle;
        variables.BaffleCutPercent.SetValue(new UnitLess(50d), VariableDefinedBy.UserInput);
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Equal(ShellAndTubeBaffleType.HelicalBaffle, variables.BaffleType);
        Assert.Contains(result.RequiredMethodImplementations, method => method.Contains("helical", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("selected baffle type helical", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("baffle cut is 50", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsEnhancedHeatTransferAsPreliminaryBasisReview()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.EnhancedHeatTransferType = ShellAndTubeEnhancedHeatTransferType.TurbulencePromoters;
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Equal(ShellAndTubeEnhancedHeatTransferType.TurbulencePromoters, variables.EnhancedHeatTransferType);
        Assert.Contains(result.RequiredMethodImplementations, method => method.Contains("turbulence-promoter", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("turbulence promoters", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("plain-tube basis", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsTubePassCountsAboveDp09cConstructionGuides()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.TubeConstruction = ShellAndTubeTubeConstruction.UTube;
        variables.TubePasses.SetValue(new UnitLess(16d), VariableDefinedBy.UserInput);
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Equal(16d, variables.TubePasses.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("tube passes exceed the recommended maximum", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("normally recommended maximum is six", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsDrumlessCondenserArrangementForCondensationService()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.CondenserArrangement = ShellAndTubeCondenserArrangement.DrumlessCondenser;
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Equal(ShellAndTubeCondenserArrangement.DrumlessCondenser, variables.CondenserArrangement);
        AssertCalculatedByEquipment(variables.DrumlessCondenserSurfaceAllowanceFactor);
        AssertCalculatedByEquipment(variables.DrumlessCondenserRequiredSurface);
        AssertCalculatedByEquipment(variables.DrumlessCondenserVentDiameter);
        AssertCalculatedByEquipment(variables.DrumlessCondenserSeparatorPotDiameter);
        AssertCalculatedByEquipment(variables.DrumlessCondenserSeparatorPotMinimumLength);
        AssertCalculatedByEquipment(variables.DrumlessCondenserSeparatorPotMaximumLength);
        AssertCalculatedByEquipment(variables.DrumlessCondenserMinimumElevation);
        Assert.Equal(1.10d, variables.DrumlessCondenserSurfaceAllowanceFactor.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(
            variables.RequiredArea.Value.GetValue(SurfaceUnits.Foot2) * 1.10d,
            variables.DrumlessCondenserRequiredSurface.Value.GetValue(SurfaceUnits.Foot2),
            precision: 6);
        Assert.Equal(2d, variables.DrumlessCondenserVentDiameter.Value.GetValue(DiameterUnits.Inch), precision: 6);
        Assert.True(variables.DrumlessCondenserSeparatorPotDiameter.Value.GetValue(DiameterUnits.Inch) >= 2d);
        Assert.Equal(3d, variables.DrumlessCondenserSeparatorPotMinimumLength.Value.GetValue(LengthUnits.Foot), precision: 6);
        Assert.Equal(5d, variables.DrumlessCondenserSeparatorPotMaximumLength.Value.GetValue(LengthUnits.Foot), precision: 6);
        Assert.Equal(20d, variables.DrumlessCondenserMinimumElevation.Value.GetValue(LengthUnits.Foot), precision: 6);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("drumless condenser", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("110%", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("liquid-vapor separation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_IdentifiesPureSteamCondensationAsPureComponentDp09fService()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("pure steam/water condensation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("Eq. 9 vapor mass-velocity correction is not applied", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsHydrocarbonSteamCondensationAsRequiredDp09fZoneSplit()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        var request = CreateEnerquip65168Request(variables);
        SetMixedWaterHydrocarbonComposition(request.ShellSideInlet.Stream);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Contains(result.RequiredMethodImplementations, method => method.Contains("hydrocarbon/steam", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("hydrocarbon dew point", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("steam condensing coefficient follows the hydrocarbon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_MarksSurfaceCondenserAsHeiBoundary()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.CondenserArrangement = ShellAndTubeCondenserArrangement.SurfaceCondenser;
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Equal(ShellAndTubeCondenserArrangement.SurfaceCondenser, variables.CondenserArrangement);
        Assert.Contains(result.RequiredMethodImplementations, method => method.Contains("HEI", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("HEI-style", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_UsesDp09dLaminarTubeSideCoefficientWhenTubeReynoldsIsLow()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.ActualTubeCount.SetValue(new UnitLess(64d), VariableDefinedBy.UserInput);
        var request = CreateEnerquip65168Request(variables);
        request.TubeSideInlet.Stream.Name = "Tube inlet process liquid";
        request.TubeSideOutlet.Stream.Name = "Tube outlet process liquid";
        request.TubeSideInlet.Stream.VolumetricFlow.SetValue(new VolumetricFlow(0.02d, VolumetricFlowUnits.m3_hr), VariableDefinedBy.StreamCalculated);
        request.TubeSideOutlet.Stream.VolumetricFlow.SetValue(new VolumetricFlow(0.02d, VolumetricFlowUnits.m3_hr), VariableDefinedBy.StreamCalculated);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.InRange(variables.TubeSideReynoldsNumber.Value.GetValue(UnitLessUnits.None), 0d, 2_000d);
        AssertCalculatedByEquipment(variables.TubeSideNaturalConvectionFactor);
        AssertCalculatedByEquipment(variables.TubeSideShortTubeCorrectionFactor);
        AssertCalculatedByEquipment(variables.TubeSideLaminarLengthFactor);
        Assert.True(variables.TubeSideHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F) > 0d);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("tube-side laminar coefficient", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("estimated tube-wall temperature", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_UsesSelectedRearHeadForShellSideCrossflowFraction()
    {
        var fixedTubesheetVariables = CreateEnerquip65168Variables();
        fixedTubesheetVariables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        fixedTubesheetVariables.TubeConstruction = ShellAndTubeTubeConstruction.Straight;
        fixedTubesheetVariables.RearHeadType = Dp09dRearHeadType.FixedTubesheet;
        var pullThroughVariables = CreateEnerquip65168Variables();
        pullThroughVariables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        pullThroughVariables.TubeConstruction = ShellAndTubeTubeConstruction.Straight;
        pullThroughVariables.RearHeadType = Dp09dRearHeadType.PullThroughFloatingHead;

        new ShellAndTubeDesignFactory().Create(CreateEnerquip65168Request(fixedTubesheetVariables)).Calculate();
        new ShellAndTubeDesignFactory().Create(CreateEnerquip65168Request(pullThroughVariables)).Calculate();

        var fixedTubesheetFraction = fixedTubesheetVariables.ShellSideNominalCrossflowFraction.Value.GetValue(UnitLessUnits.None);
        var pullThroughFraction = pullThroughVariables.ShellSideNominalCrossflowFraction.Value.GetValue(UnitLessUnits.None);

        Assert.True(fixedTubesheetFraction > pullThroughFraction);
    }

    [Fact]
    public void DesignPracticesEngine_FlagsUTubeConstructionWhenTubeSideFoulingIsHigh()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.TubeConstruction = ShellAndTubeTubeConstruction.UTube;
        variables.TubeSideAllowedFoulingResistance.SetValue(new UnitLess(0.003d), VariableDefinedBy.UserInput);
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSteamInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideCondensateOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipInlet() },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipOutlet() }
        };

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("U-tube construction is not recommended", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsUTubeHighTubeFoulingWhenTubeSideIsNotCoolingWater()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.TubeConstruction = ShellAndTubeTubeConstruction.UTube;
        variables.TubeSideAllowedFoulingResistance.SetValue(new UnitLess(0.003d), VariableDefinedBy.UserInput);
        var request = CreateEnerquip65168Request(variables);
        request.TubeSideInlet.Stream.Name = "Process oil inlet";
        request.TubeSideOutlet.Stream.Name = "Process oil outlet";

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("high-pressure jetting", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("removable-bundle construction", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("mechanical tube-side cleaning", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsFixedTubesheetWhenShellSideFoulingIsHigh()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.TubeConstruction = ShellAndTubeTubeConstruction.Straight;
        variables.RearHeadType = Dp09dRearHeadType.FixedTubesheet;
        variables.ShellSideAllowedFoulingResistance.SetValue(new UnitLess(0.003d), VariableDefinedBy.UserInput);
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSteamInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideCondensateOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipInlet() },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipOutlet() }
        };

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("fixed tubesheet rear head needs review", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.RequiredMethodImplementations, method => method.Contains("TEMA cleaning-method", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.RequiredMethodImplementations, method => method.Contains("corrosion-allowance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_UsesDp09cCleaningMethodAndFrontHeadInputsInTemaReview()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.FrontHeadType = ShellAndTubeFrontHeadType.Bonnet;
        variables.TubeSideCleaningMethod = ShellAndTubeCleaningMethod.Mechanical;
        variables.ShellSideCleaningMethod = ShellAndTubeCleaningMethod.Mechanical;
        variables.TubeSideCorrosionAllowance.SetValue(new Length(0d, LengthUnits.Inch), VariableDefinedBy.UserInput);
        variables.ShellSideCorrosionAllowance.SetValue(new Length(0d, LengthUnits.Inch), VariableDefinedBy.UserInput);
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.DoesNotContain(result.RequiredMethodImplementations, method => method.Contains("TEMA cleaning-method", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.RequiredMethodImplementations, method => method.Contains("corrosion-allowance", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("removable-channel front head", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_FlagsAFrontHeadPreferenceWhenCorrosionAllowanceIsHigh()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.FrontHeadType = ShellAndTubeFrontHeadType.Bonnet;
        variables.TubeSideCleaningMethod = ShellAndTubeCleaningMethod.Chemical;
        variables.ShellSideCleaningMethod = ShellAndTubeCleaningMethod.Chemical;
        variables.TubeSideCorrosionAllowance.SetValue(new Length(0.125d, LengthUnits.Inch), VariableDefinedBy.UserInput);
        variables.ShellSideCorrosionAllowance.SetValue(new Length(0d, LengthUnits.Inch), VariableDefinedBy.UserInput);
        var request = CreateEnerquip65168Request(variables);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.DoesNotContain(result.RequiredMethodImplementations, method => method.Contains("corrosion-allowance", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("1/8 in", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("A front head", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_CalculatesHeatFluxForVaporizationReview()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.MaximumAllowableHeatFlux.SetValue(new HeatSurfaceFlow(20_000d, HeatSurfaceFlowUnits.BTU_hr_ft2), VariableDefinedBy.UserInput);
        var tubeInlet = CreateTubeSideCipInlet();
        var tubeOutlet = CreateTubeSideCipOutlet();
        tubeInlet.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        tubeOutlet.VaporFraction.SetValue(new Percentage(25d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = tubeInlet },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = tubeOutlet }
        };

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        AssertCalculatedByEquipment(variables.HeatFlux);
        AssertCalculatedByEquipment(variables.DesignHeatFluxLimit);
        AssertCalculatedByEquipment(variables.HeatFluxUtilization);
        AssertCalculatedByEquipment(variables.VaporizedFraction);
        AssertCalculatedByEquipment(variables.VaporizedFractionLimit);
        AssertCalculatedByEquipment(variables.VaporizedFractionUtilization);
        AssertCalculatedByEquipment(variables.VaporizingSidePressureDrop);
        AssertCalculatedByEquipment(variables.ReboilerMinimumTowerElevation);
        AssertCalculatedByEquipment(variables.ReboilerMaximumTowerElevation);
        AssertCalculatedByEquipment(variables.PumpThroughRecommendedCirculationRatio);
        Assert.True(variables.HeatFlux.Value.GetValue(HeatSurfaceFlowUnits.BTU_hr_ft2) > 0d);
        Assert.Equal(14_000d, variables.DesignHeatFluxLimit.Value.GetValue(HeatSurfaceFlowUnits.BTU_hr_ft2), precision: 6);
        Assert.True(variables.HeatFluxUtilization.Value.GetValue(UnitLessUnits.None) > 0d);
        Assert.Equal(0.25d, variables.VaporizedFraction.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0.50d, variables.VaporizedFractionLimit.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(0.50d, variables.VaporizedFractionUtilization.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(6d, variables.ReboilerMinimumTowerElevation.Value.GetValue(LengthUnits.Foot), precision: 6);
        Assert.Equal(10d, variables.ReboilerMaximumTowerElevation.Value.GetValue(LengthUnits.Foot), precision: 6);
        Assert.Equal(0d, variables.PumpThroughRecommendedCirculationRatio.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(
            variables.TubeSidePressureDrop.Value.GetValue(PressureDropUnits.psi),
            variables.VaporizingSidePressureDrop.Value.GetValue(PressureDropUnits.psi),
            precision: 6);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("preliminary heat flux", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("heat flux utilization", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("vaporized-fraction utilization", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("vaporizing-side exchanger pressure drop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_AppliesVerticalThermosiphonDp09eLimits()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.ReboilerType = ShellAndTubeReboilerType.VerticalThermosiphon;
        variables.MaximumAllowableHeatFlux.SetValue(new HeatSurfaceFlow(20_000d, HeatSurfaceFlowUnits.BTU_hr_ft2), VariableDefinedBy.UserInput);
        variables.ReboilerAvailableTowerElevation.SetValue(new Length(12d, LengthUnits.Foot), VariableDefinedBy.UserInput);
        variables.ReboilerLiquidLineDiameter.SetValue(new Diameter(3d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.ReboilerLiquidLineResistanceCoefficient.SetValue(new UnitLess(4d), VariableDefinedBy.UserInput);
        variables.ReboilerVaporLineDiameter.SetValue(new Diameter(4d, DiameterUnits.Inch), VariableDefinedBy.UserInput);
        variables.ReboilerVaporLineResistanceCoefficient.SetValue(new UnitLess(2d), VariableDefinedBy.UserInput);
        var tubeInlet = CreateTubeSideCipInlet();
        var tubeOutlet = CreateTubeSideCipOutlet();
        tubeInlet.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        tubeOutlet.VaporFraction.SetValue(new Percentage(60d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = tubeInlet },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = tubeOutlet }
        };

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Equal(12_000d, variables.DesignHeatFluxLimit.Value.GetValue(HeatSurfaceFlowUnits.BTU_hr_ft2), precision: 6);
        Assert.Equal(0.50d, variables.VaporizedFractionLimit.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(1.20d, variables.VaporizedFractionUtilization.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Equal(8d, variables.ReboilerMinimumTowerElevation.Value.GetValue(LengthUnits.Foot), precision: 6);
        Assert.Equal(20d, variables.ReboilerMaximumTowerElevation.Value.GetValue(LengthUnits.Foot), precision: 6);
        AssertCalculatedByEquipment(variables.ReboilerRequiredTowerElevation);
        AssertCalculatedByEquipment(variables.ReboilerRequiredStaticHead);
        AssertCalculatedByEquipment(variables.ReboilerAvailableStaticHead);
        AssertCalculatedByEquipment(variables.ReboilerStaticHeadMargin);
        AssertCalculatedByEquipment(variables.ReboilerLiquidLinePressureDrop);
        AssertCalculatedByEquipment(variables.ReboilerVaporLinePressureDrop);
        AssertCalculatedByEquipment(variables.ReboilerCircuitPressureDrop);
        Assert.True(variables.ReboilerRequiredTowerElevation.Value.GetValue(LengthUnits.Foot) > 0d);
        Assert.True(variables.ReboilerRequiredStaticHead.Value.GetValue(PressureDropUnits.psi) > variables.VaporizingSidePressureDrop.Value.GetValue(PressureDropUnits.psi));
        Assert.True(variables.ReboilerCircuitPressureDrop.Value.GetValue(PressureDropUnits.psi) > 0d);
        Assert.Contains(result.RequiredMethodImplementations, method => method.Contains("thermosiphon reboiler hydraulic balance", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("vertical thermosiphon vaporization should be limited to 50%", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("preliminary thermosiphon circuit balance", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("thermosiphon circulation is not proven", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("8-20 ft", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_AppliesPumpThroughDp09eCirculationAndElevationGuides()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.ReboilerType = ShellAndTubeReboilerType.PumpThrough;
        variables.MaximumAllowableHeatFlux.SetValue(new HeatSurfaceFlow(20_000d, HeatSurfaceFlowUnits.BTU_hr_ft2), VariableDefinedBy.UserInput);
        var tubeInlet = CreateTubeSideCipInlet();
        var tubeOutlet = CreateTubeSideCipOutlet();
        tubeInlet.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        tubeOutlet.VaporFraction.SetValue(new Percentage(25d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = tubeInlet },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = tubeOutlet }
        };

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        Assert.Equal(15d, variables.ReboilerMinimumTowerElevation.Value.GetValue(LengthUnits.Foot), precision: 6);
        Assert.Equal(15d, variables.ReboilerMaximumTowerElevation.Value.GetValue(LengthUnits.Foot), precision: 6);
        Assert.Equal(10d, variables.PumpThroughRecommendedCirculationRatio.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.Contains(result.RequiredMethodImplementations, method => method.Contains("pump-through reboiler circulation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("10:1 circulation ratio", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_CalculatesDp09eMaximumHeatFluxFromCriticalPressure()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.VaporizingSideCriticalPressure.SetValue(new Pressure(456d, PressureUnits.Psia), VariableDefinedBy.UserInput);
        var tubeInlet = CreateTubeSideCipInlet();
        var tubeOutlet = CreateTubeSideCipOutlet();
        tubeInlet.Pressure.SetValue(new Pressure(45.6d, PressureUnits.Psia), VariableDefinedBy.StreamCalculated);
        tubeInlet.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        tubeOutlet.VaporFraction.SetValue(new Percentage(25d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = tubeInlet },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = tubeOutlet }
        };

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        AssertCalculatedByEquipment(variables.SingleTubeMaximumHeatFlux);
        AssertCalculatedByEquipment(variables.BundleHeatFluxCorrectionFactor);
        AssertCalculatedByEquipment(variables.BundleMaximumHeatFlux);
        AssertCalculatedByEquipment(variables.MaximumAllowableHeatFlux);
        Assert.True(variables.BundleMaximumHeatFlux.Value.GetValue(HeatSurfaceFlowUnits.BTU_hr_ft2) > 0d);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("Figures A3/A4", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesignPracticesEngine_AppliesDp09eIntegralFinnedTubeBoilingCorrection()
    {
        var variables = CreateEnerquip65168Variables();
        variables.CalculationStandard = ShellAndTubeCalculationStandard.DesignPractices;
        variables.ReboilerType = ShellAndTubeReboilerType.Kettle;
        variables.EnhancedHeatTransferType = ShellAndTubeEnhancedHeatTransferType.IntegralFinnedTubes;
        variables.VaporizingSideCriticalPressure.SetValue(new Pressure(456d, PressureUnits.Psia), VariableDefinedBy.UserInput);
        variables.VaporizingSideBoilingRange.SetValue(new Temperature(50d, TemperatureUnits.DegreeFahrenheit), VariableDefinedBy.UserInput);
        var tubeInlet = CreateTubeSideCipInlet();
        var tubeOutlet = CreateTubeSideCipOutlet();
        tubeInlet.Pressure.SetValue(new Pressure(45.6d, PressureUnits.Psia), VariableDefinedBy.StreamCalculated);
        tubeInlet.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        tubeOutlet.VaporFraction.SetValue(new Percentage(25d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        var request = new HeatExchangerDesignRequest
        {
            HeatExchangerType = HeatExchangerType.ShellAndTube,
            Variables = variables,
            ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingInlet() },
            ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSensibleHeatingOutlet() },
            TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = tubeInlet },
            TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = tubeOutlet }
        };

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());

        AssertCalculatedByEquipment(variables.FinnedTubeSurfaceFactor);
        AssertCalculatedByEquipment(variables.FinEfficiencyFactor);
        AssertCalculatedByEquipment(variables.FinnedTubeCorrectionFactor);
        AssertCalculatedByEquipment(variables.FinnedTubeCorrectedBundleBoilingCoefficient);
        Assert.True(variables.FinnedTubeCorrectionFactor.Value.GetValue(UnitLessUnits.None) > 0d);
        Assert.Equal(
            variables.BundleNucleateBoilingCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            variables.FinnedTubeCorrectedBundleBoilingCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F),
            precision: 6);
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("Figure A11", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recommendations, recommendation => recommendation.Contains("Figure A12", StringComparison.OrdinalIgnoreCase));
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

    private static HeatExchangerDesignRequest CreateEnerquip65168Request(ShellAndTubeDesignVariables variables) => new()
    {
        HeatExchangerType = HeatExchangerType.ShellAndTube,
        Variables = variables,
        ShellSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideSteamInlet() },
        ShellSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateShellSideCondensateOutlet() },
        TubeSideInlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipInlet() },
        TubeSideOutlet = new HeatExchangerStreamSnapshot { Stream = CreateTubeSideCipOutlet() }
    };

    private static void SetMixedWaterHydrocarbonComposition(IFacadeStream stream)
    {
        var water = CreateComponent("Water", "H2O");
        var hydrocarbon = CreateComponent("n-Hexane", "C6H14");
        water.MassFraction.SetValue(new Percentage(20d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        hydrocarbon.MassFraction.SetValue(new Percentage(80d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        stream.Composition = new CompositionOrchestrator([water, hydrocarbon]);
    }

    private static ComponentFacade CreateComponent(string name, string formula)
    {
        return new ComponentFacade(new MethodComponentFullDto
        {
            ComponentId = Guid.NewGuid(),
            ComponentName = name,
            FullData = new ChemicalComponentDto
            {
                Name = name,
                Formula = formula
            }
        });
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
        stream.VaporFraction.SetValue(new Percentage(100d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
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
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        stream.MassDensity.SetValue(new MassDensity(919.24d, MassDensityUnits.Kg_m3), VariableDefinedBy.StreamCalculated);
        stream.Viscosity.SetValue(new Viscosity(0.1861d, ViscosityUnits.cPoise), VariableDefinedBy.StreamCalculated);
        stream.MassCp.SetValue(new MassEntropy(1.0700d, MassEntropyUnits.BTU_lb_F), VariableDefinedBy.StreamCalculated);
        stream.ThermalConductivity.SetValue(new ThermalConductivity(0.3954d, ThermalConductivityUnits.BTU_ft_hr_ft2_m_F), VariableDefinedBy.StreamCalculated);

        return stream;
    }

    private static IFacadeStream CreateShellSideSensibleHeatingInlet()
    {
        var stream = CreateShellSideSteamInlet();
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
        return stream;
    }

    private static IFacadeStream CreateShellSideSensibleHeatingOutlet()
    {
        var stream = CreateShellSideCondensateOutlet();
        stream.VaporFraction.SetValue(new Percentage(0d, PercentageUnits.Percentage), VariableDefinedBy.StreamCalculated);
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
