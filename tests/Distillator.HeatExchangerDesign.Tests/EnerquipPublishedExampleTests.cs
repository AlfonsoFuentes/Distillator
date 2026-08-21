using Shared.SolverConsecutive;
using Shared.UnitOperations.HeatExchangers.Design;
using UnitSystem;

namespace Distillator.HeatExchangerDesign.Tests;

public sealed class EnerquipPublishedExampleTests
{
    [Fact]
    public void Enerquip65168_DesignPracticesShellSideSteamCondensation_MatchesPublishedBaseline()
    {
        var request = EnerquipShellAndTubeExamples.Create65168SteamCondensingRequest();
        AssertStreamHasThermodynamicEnthalpyFlow(request.ShellSideInlet.Stream);
        AssertStreamHasThermodynamicEnthalpyFlow(request.ShellSideOutlet.Stream);
        AssertStreamHasThermodynamicEnthalpyFlow(request.TubeSideInlet.Stream);
        AssertStreamHasThermodynamicEnthalpyFlow(request.TubeSideOutlet.Stream);

        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());
        var variables = request.Variables;

        Assert.Equal(ShellAndTubeCalculationStandard.DesignPractices, result.CalculationStandard);
        Assert.Equal("DesignPracticesShellAndTubeDesign.ShellSideCondensation", result.DesignType);
        Assert.True(string.IsNullOrWhiteSpace(result.Message), result.Message);
        AssertCalculatedByEquipment(variables.HeatDuty);
        AssertCalculatedByEquipment(variables.LogMeanTemperatureDifference);
        AssertCalculatedByEquipment(variables.ActualArea);
        AssertCalculatedByEquipment(variables.ActualOverallCoefficient);
        AssertCalculatedByEquipment(variables.CalculatedDirtyOverallCoefficient);
        AssertCalculatedByEquipment(variables.ShellSidePressureDrop);
        AssertCalculatedByEquipment(variables.TubeSidePressureDrop);

        Assert.InRange(variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr), 1_300_000d, 1_500_000d);
        Assert.InRange(variables.LogMeanTemperatureDifference.Value.GetValue(TemperatureUnits.DegreeFahrenheit), 135.5d, 139.6d);
        Assert.InRange(variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2), 20.58d, 22.75d);
        Assert.Equal(0.00035d, variables.AllowedFoulingResistance.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.InRange(variables.TubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond), 5.60d, 7.58d);
        Assert.InRange(variables.TubeSideHeatTransferCoefficient.Value.GetValue(HeatTransferCoefficientUnits.BTU_hr_ft2_F), 1_620d, 1_670d);
        var expectedTubePressureDrop = Dp09fTubeSideWaterCorrelation.CalculateTubeSidePressureDropPsi(
            variables.TubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond),
            variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch),
            variables.TubeLength.Value.GetValue(LengthUnits.Foot),
            variables.ShellPasses.Value.GetValue(UnitLessUnits.None),
            variables.TubePasses.Value.GetValue(UnitLessUnits.None),
            Dp09fTubeSideWaterCorrelation.EstimatePressureDropFoulingFactor(
                variables.TubeMaterial,
                variables.TubeOuterDiameter.Value.GetValue(DiameterUnits.Inch),
                variables.TubeInnerDiameter.Value.GetValue(DiameterUnits.Inch)).Value);
        Assert.Equal(expectedTubePressureDrop, variables.TubeSidePressureDrop.Value.GetValue(PressureDropUnits.psi), precision: 6);
        Assert.Contains(
            result.Recommendations,
            recommendation => recommendation.Contains("steam condenser / aqueous solution", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.Recommendations,
            recommendation => recommendation.Contains("DP09F Table 2 condenser design procedure, page 24", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.Recommendations,
            recommendation => recommendation.Contains("tube-side water/aqueous pressure drop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Enerquip65168_DesignPracticesServiceClassification_SelectsSteamCondenserAqueousSolution()
    {
        var request = EnerquipShellAndTubeExamples.Create65168SteamCondensingRequest();
        var cooledService = DesignPracticesServiceClassifier.Classify(
            request.ShellSideInlet.Stream,
            request.ShellSideOutlet.Stream);
        var heatedService = DesignPracticesServiceClassifier.Classify(
            request.TubeSideInlet.Stream,
            request.TubeSideOutlet.Stream);

        var range = Dp09bHeatExchangerCatalog.GetTypicalShellAndTubeOverallCoefficientRange(
            DesignPracticesProcessRegime.ShellSideCondensation,
            cooledService,
            heatedService);

        Assert.Equal(DesignPracticesServiceKind.SteamCondensing, cooledService.Kind);
        Assert.Equal(DesignPracticesServiceKind.AqueousSolution, heatedService.Kind);
        Assert.Equal(400d, range.MinimumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Equal(600d, range.MaximumBtuPerHourSquareFootFahrenheit, precision: 6);
        Assert.Contains("steam condenser / aqueous solution", range.Basis, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Enerquip65168_DesignPracticesTubeCounts_AreCompatibleWithTubePasses()
    {
        var request = EnerquipShellAndTubeExamples.Create65168SteamCondensingRequest();
        request.Variables.ActualTubeCount.SetValue(new UnitLess(31d), VariableDefinedBy.UserInput);

        _ = new ShellAndTubeDesignFactory().Create(request).Calculate();
        var variables = request.Variables;
        var tubePasses = variables.TubePasses.Value.GetValue(UnitLessUnits.None);

        Assert.Equal(2d, tubePasses, precision: 6);
        Assert.Equal(0d, variables.RequiredTubeCount.Value.GetValue(UnitLessUnits.None) % tubePasses, precision: 6);
        Assert.Equal(0d, variables.MaximumTubeCount.Value.GetValue(UnitLessUnits.None) % tubePasses, precision: 6);
        Assert.Equal(32d, variables.ActualTubeCount.Value.GetValue(UnitLessUnits.None), precision: 6);
    }

    [Fact]
    public void Enerquip47397_DesignPracticesNoPhaseChangeCooler_RunsAsInitialNoPhaseChangeBaseline()
    {
        var request = EnerquipShellAndTubeExamples.Create47397ProcessWaterCoolerRequest();
        var result = Assert.IsType<HeatExchangerDesignResult>(new ShellAndTubeDesignFactory().Create(request).Calculate());
        var variables = request.Variables;

        Assert.Equal(ShellAndTubeCalculationStandard.DesignPractices, result.CalculationStandard);
        Assert.Equal("DesignPracticesShellAndTubeDesign.NoPhaseChange", result.DesignType);
        Assert.True(string.IsNullOrWhiteSpace(result.Message), result.Message);
        AssertCalculatedByEquipment(variables.HeatDuty);
        AssertCalculatedByEquipment(variables.LogMeanTemperatureDifference);
        AssertCalculatedByEquipment(variables.LogMeanTemperatureCorrectionFactor);
        AssertCalculatedByEquipment(variables.ActualArea);
        AssertCalculatedByEquipment(variables.ActualOverallCoefficient);
        AssertCalculatedByEquipment(variables.CleanOverallCoefficient);
        AssertCalculatedByEquipment(variables.CalculatedDirtyOverallCoefficient);

        Assert.InRange(variables.HeatDuty.Value.GetValue(EnergyFlowUnits.BTUhr), 1_801_472d * 0.999d, 1_801_472d * 1.001d);
        Assert.InRange(variables.LogMeanTemperatureDifference.Value.GetValue(TemperatureUnits.DegreeFahrenheit), 32.4d, 33.1d);
        Assert.InRange(variables.ActualArea.Value.GetValue(SurfaceUnits.Foot2), 74.6d, 82.5d);
        Assert.Equal(0.00050d, variables.AllowedFoulingResistance.Value.GetValue(UnitLessUnits.None), precision: 6);
        Assert.InRange(variables.TubeVelocity.Value.GetValue(VelocityUnits.FeetPerSecond), 0d, 15d);
        Assert.InRange(variables.ShellSidePressureDrop.Value.GetValue(PressureDropUnits.Bar), 0d, 100d);
        Assert.InRange(variables.TubeSidePressureDrop.Value.GetValue(PressureDropUnits.Bar), 0d, 100d);
    }

    private static void AssertCalculatedByEquipment<T>(Variable<T> variable)
        where T : Amount
    {
        Assert.Equal(VariableDefinedBy.Equipment, variable.DataProcedence);
    }

    private static void AssertStreamHasThermodynamicEnthalpyFlow(Shared.SolverQwen.Stream.IFacadeStream stream)
    {
        Assert.True(
            stream.EnthalpyFlow.IsDefined,
            $"{stream.Name}: EnthalpyFlow undefined. Thermo={stream.ThermoMethod?.Name ?? "<null>"}, CompositionValid={stream.Composition?.IsValid}, T={stream.Temperature.IsDefined}, P={stream.Pressure.IsDefined}, VF={stream.VaporFraction.IsDefined}, Equilibrium={stream.IsEquilibriumSolved}, Flow={stream.IsFlowSolved}, MassFlow={stream.MassFlow.IsDefined}, MassEnthalpy={stream.MassEnthalpy.IsDefined}, State={stream.ThermodynamicState}");
    }
}
