using Shared.ProcessFlowDiagram.Designs;
using Shared.SolverConsecutive;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class ShellAndTubeDesignVariables : IDesignVariables
{
    public ShellAndTubeCalculationStandard CalculationStandard { get; set; } = ShellAndTubeCalculationStandard.Kern;

    public ShellAndTubeTubeMaterial TubeMaterial { get; set; } = ShellAndTubeTubeMaterial.CarbonSteel;

    public ShellAndTubeTubeConstruction TubeConstruction { get; set; } = ShellAndTubeTubeConstruction.UTube;

    public ShellAndTubeFrontHeadType FrontHeadType { get; set; } = ShellAndTubeFrontHeadType.Bonnet;

    public Dp09dRearHeadType RearHeadType { get; set; } = Dp09dRearHeadType.PullThroughFloatingHead;

    public ShellAndTubeCleaningMethod TubeSideCleaningMethod { get; set; } = ShellAndTubeCleaningMethod.NotSpecified;

    public ShellAndTubeCleaningMethod ShellSideCleaningMethod { get; set; } = ShellAndTubeCleaningMethod.NotSpecified;

    public Variable<Length> TubeSideCorrosionAllowance { get; } =
        new(new Length(0, LengthUnits.Inch), LengthUnits.Inch, 1);

    public Variable<Length> ShellSideCorrosionAllowance { get; } =
        new(new Length(0, LengthUnits.Inch), LengthUnits.Inch, 1);

    public ShellAndTubeShellType ShellType { get; set; } = ShellAndTubeShellType.OnePass;

    public ShellAndTubeReboilerType ReboilerType { get; set; } = ShellAndTubeReboilerType.Kettle;

    public ShellAndTubeCoolingWaterType CoolingWaterType { get; set; } = ShellAndTubeCoolingWaterType.None;

    public ShellAndTubeEnhancedHeatTransferType EnhancedHeatTransferType { get; set; } = ShellAndTubeEnhancedHeatTransferType.None;

    public ShellAndTubeCondenserArrangement CondenserArrangement { get; set; } = ShellAndTubeCondenserArrangement.NotSpecified;

    public Variable<UnitLess> TubeGauge { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Diameter> TubeNominalDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Diameter> TubeOuterDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Diameter> TubeInnerDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Length> TubeLength { get; } =
        new(new Length(0, LengthUnits.Meter), LengthUnits.Meter, 1);

    public Variable<Length> BaffleSpacing { get; } =
        new(new Length(0, LengthUnits.Inch), LengthUnits.Inch, 1);

    public ShellAndTubeBaffleType BaffleType { get; set; } = ShellAndTubeBaffleType.SingleSegmental;

    public Variable<UnitLess> BaffleCutPercent { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Diameter> TubePitch { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public ShellAndTubeTubeLayout TubeLayout { get; set; } = ShellAndTubeTubeLayout.Triangular;

    public Variable<Velocity> MinimumTubeVelocity { get; } =
        new(new Velocity(0, VelocityUnits.FeetPerSecond), VelocityUnits.FeetPerSecond, 1);

    public Variable<UnitLess> ShellPasses { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubePasses { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> AllowedFoulingResistance { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubeSideAllowedFoulingResistance { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellSideAllowedFoulingResistance { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Area> TubeFlowArea { get; } =
        new(new Area(0, SurfaceUnits.inch2), SurfaceUnits.inch2, 1);

    public Variable<Length> TubeClearance { get; } =
        new(new Length(0, LengthUnits.Inch), LengthUnits.Inch, 1);

    public Variable<Area> TubeSurfaceArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<EnergyFlow> HeatDuty { get; } =
        new(new EnergyFlow(0, EnergyFlowUnits.BTUhr), EnergyFlowUnits.BTUhr, 3000);

    public Variable<Temperature> LogMeanTemperatureDifference { get; } =
        new(new Temperature(0, TemperatureUnits.DegreeFahrenheit), TemperatureUnits.DegreeFahrenheit, 1);

    public Variable<UnitLess> LogMeanTemperatureCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> AssumedDirtyOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> TypicalOverallCoefficientMinimum { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> TypicalOverallCoefficientMaximum { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> LastCalculatedDirtyOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<Area> AssumedArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<Area> RequiredArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<Area> ActualArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<UnitLess> AreaOverdesignPercent { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatSurfaceFlow> HeatFlux { get; } =
        new(new HeatSurfaceFlow(0, HeatSurfaceFlowUnits.BTU_hr_ft2), HeatSurfaceFlowUnits.BTU_hr_ft2, 1);

    public Variable<HeatSurfaceFlow> MaximumAllowableHeatFlux { get; } =
        new(new HeatSurfaceFlow(0, HeatSurfaceFlowUnits.BTU_hr_ft2), HeatSurfaceFlowUnits.BTU_hr_ft2, 1);

    public Variable<Pressure> VaporizingSideCriticalPressure { get; } =
        new(new Pressure(0, PressureUnits.Psia), PressureUnits.Psia, 1);

    public Variable<Temperature> VaporizingSideBoilingRange { get; } =
        new(new Temperature(0, TemperatureUnits.DegreeFahrenheit), TemperatureUnits.DegreeFahrenheit, 1);

    public Variable<HeatSurfaceFlow> SingleTubeMaximumHeatFlux { get; } =
        new(new HeatSurfaceFlow(0, HeatSurfaceFlowUnits.BTU_hr_ft2), HeatSurfaceFlowUnits.BTU_hr_ft2, 1);

    public Variable<UnitLess> BundleHeatFluxCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatSurfaceFlow> BundleMaximumHeatFlux { get; } =
        new(new HeatSurfaceFlow(0, HeatSurfaceFlowUnits.BTU_hr_ft2), HeatSurfaceFlowUnits.BTU_hr_ft2, 1);

    public Variable<HeatTransferCoefficient> SingleTubeNucleateBoilingReferenceCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<UnitLess> NucleateBoilingPressureCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> NucleateBoilingVaporToLiquidDensityRatio { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Temperature> NucleateBoilingEffectiveTemperatureRange { get; } =
        new(new Temperature(0, TemperatureUnits.DegreeFahrenheit), TemperatureUnits.DegreeFahrenheit, 1);

    public Variable<HeatSurfaceFlow> NucleateBoilingEffectiveMinimumHeatFlux { get; } =
        new(new HeatSurfaceFlow(0, HeatSurfaceFlowUnits.BTU_hr_ft2), HeatSurfaceFlowUnits.BTU_hr_ft2, 1);

    public Variable<UnitLess> NucleateBoilingRangeParameter { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> NucleateBoilingMixtureCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> SingleTubeNucleateBoilingCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<UnitLess> BundleNucleateBoilingCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> BundleNucleateBoilingCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<UnitLess> FinnedTubeSurfaceFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> FinEfficiencyFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> FinnedTubeCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> FinnedTubeCorrectedBundleBoilingCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatSurfaceFlow> VerticalThermosiphonChokeReferenceMaximumHeatFlux { get; } =
        new(new HeatSurfaceFlow(0, HeatSurfaceFlowUnits.BTU_hr_ft2), HeatSurfaceFlowUnits.BTU_hr_ft2, 1);

    public Variable<UnitLess> VerticalThermosiphonTubeGeometryCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatSurfaceFlow> VerticalThermosiphonChokeMaximumHeatFlux { get; } =
        new(new HeatSurfaceFlow(0, HeatSurfaceFlowUnits.BTU_hr_ft2), HeatSurfaceFlowUnits.BTU_hr_ft2, 1);

    public Variable<UnitLess> VerticalThermosiphonChokeHeatFluxUtilization { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> VerticalThermosiphonNaturalConvectionCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<UnitLess> VerticalThermosiphonReducedHeatFlux { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Velocity> VerticalThermosiphonInletVelocity { get; } =
        new(new Velocity(0, VelocityUnits.FeetPerSecond), VelocityUnits.FeetPerSecond, 1);

    public Variable<UnitLess> VerticalThermosiphonFigureA17OutletVaporFractionLimit { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> VerticalThermosiphonFigureA17OutletVaporFractionUtilization { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Diameter> ReboilerLiquidLineRecommendedDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Diameter> ReboilerLiquidLineDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<UnitLess> ReboilerLiquidLineResistanceCoefficient { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<PressureDrop> ReboilerLiquidLinePressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<Diameter> ReboilerVaporLineDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<UnitLess> ReboilerVaporLineResistanceCoefficient { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<PressureDrop> ReboilerVaporLinePressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<PressureDrop> ReboilerCircuitPressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<HeatSurfaceFlow> DesignHeatFluxLimit { get; } =
        new(new HeatSurfaceFlow(0, HeatSurfaceFlowUnits.BTU_hr_ft2), HeatSurfaceFlowUnits.BTU_hr_ft2, 1);

    public Variable<UnitLess> HeatFluxUtilization { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> VaporizedFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> VaporizedFractionLimit { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> VaporizedFractionUtilization { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<PressureDrop> VaporizingSidePressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<Length> ReboilerAvailableTowerElevation { get; } =
        new(new Length(0, LengthUnits.Foot), LengthUnits.Foot, 1);

    public Variable<Length> ReboilerRequiredTowerElevation { get; } =
        new(new Length(0, LengthUnits.Foot), LengthUnits.Foot, 1);

    public Variable<PressureDrop> ReboilerRequiredStaticHead { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<PressureDrop> ReboilerAvailableStaticHead { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<UnitLess> ReboilerStaticHeadMargin { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Length> ReboilerMinimumTowerElevation { get; } =
        new(new Length(0, LengthUnits.Foot), LengthUnits.Foot, 1);

    public Variable<Length> ReboilerMaximumTowerElevation { get; } =
        new(new Length(0, LengthUnits.Foot), LengthUnits.Foot, 1);

    public Variable<UnitLess> PumpThroughRecommendedCirculationRatio { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> CondensationZoneCount { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> DesuperheatingDutyFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> CondensationDutyFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> SubcoolingDutyFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> DesuperheatingAreaFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> CondensationAreaFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> SubcoolingAreaFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> CondensingZoneHeatTransferCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> VaporCoolingZoneHeatTransferCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> LiquidCoolingZoneHeatTransferCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> BottomFlowLiquidCoolingHeatTransferCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> DripCoolingHeatTransferCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<UnitLess> CondensingVaporFreeAreaFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> CondensingVaporMassVelocity { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> CondensingAreaIterationCount { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Area> CondensingIteratedRequiredArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<UnitLess> CondensingTwoPhaseAverageDensity { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> CondensingPressureDropDensityCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> DutyWeightedCondensingSideHeatTransferCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<UnitLess> DrumlessCondenserSurfaceAllowanceFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Area> DrumlessCondenserRequiredSurface { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<Diameter> DrumlessCondenserVentDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Diameter> DrumlessCondenserSeparatorPotDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Length> DrumlessCondenserSeparatorPotMinimumLength { get; } =
        new(new Length(0, LengthUnits.Foot), LengthUnits.Foot, 1);

    public Variable<Length> DrumlessCondenserSeparatorPotMaximumLength { get; } =
        new(new Length(0, LengthUnits.Foot), LengthUnits.Foot, 1);

    public Variable<Length> DrumlessCondenserMinimumElevation { get; } =
        new(new Length(0, LengthUnits.Foot), LengthUnits.Foot, 1);

    public Variable<Temperature> CoolingWaterOutletBulkTemperatureLimit { get; } =
        new(new Temperature(0, TemperatureUnits.DegreeFahrenheit), TemperatureUnits.DegreeFahrenheit, 1);

    public Variable<Temperature> CoolingWaterEstimatedTubeWallTemperature { get; } =
        new(new Temperature(0, TemperatureUnits.DegreeFahrenheit), TemperatureUnits.DegreeFahrenheit, 1);

    public Variable<Temperature> CoolingWaterEstimatedFilmTemperature { get; } =
        new(new Temperature(0, TemperatureUnits.DegreeFahrenheit), TemperatureUnits.DegreeFahrenheit, 1);

    public Variable<Temperature> CoolingWaterFilmTemperatureLimit { get; } =
        new(new Temperature(0, TemperatureUnits.DegreeFahrenheit), TemperatureUnits.DegreeFahrenheit, 1);

    public Variable<Velocity> CoolingWaterMinimumTubeVelocity { get; } =
        new(new Velocity(0, VelocityUnits.FeetPerSecond), VelocityUnits.FeetPerSecond, 1);

    public Variable<Velocity> CoolingWaterMaximumTubeVelocity { get; } =
        new(new Velocity(0, VelocityUnits.FeetPerSecond), VelocityUnits.FeetPerSecond, 1);

    public Variable<UnitLess> RequiredTubeCount { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ActualTubeCount { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> MaximumTubeCount { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubeCountCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> UTubeBendTubeLossFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ImpingementTubeLossFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellNozzleTubeLossFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Diameter> ShellInsideDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Velocity> TubeVelocity { get; } =
        new(new Velocity(0, VelocityUnits.FeetPerSecond), VelocityUnits.FeetPerSecond, 1);

    public Variable<Diameter> TubeSideNozzleDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Velocity> TubeSideNozzleVelocity { get; } =
        new(new Velocity(0, VelocityUnits.FeetPerSecond), VelocityUnits.FeetPerSecond, 1);

    public Variable<Area> AssumedTubeFlowArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<Area> ActualTubeFlowArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<Area> ShellFlowArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<Diameter> ShellEquivalentDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<HeatTransferCoefficient> TubeSideHeatTransferCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<UnitLess> TubeSideReynoldsNumber { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubeSidePrandtlNumber { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubeSideNaturalConvectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubeSideShortTubeCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubeSideLaminarLengthFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubeSideFrictionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubeSideViscosityGradientCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> TubeSidePressureDropNaturalConvectionCorrectionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> ShellSideHeatTransferCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<Velocity> ShellSideVelocity { get; } =
        new(new Velocity(0, VelocityUnits.FeetPerSecond), VelocityUnits.FeetPerSecond, 1);

    public Variable<Diameter> ShellSideNozzleDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Velocity> ShellSideNozzleVelocity { get; } =
        new(new Velocity(0, VelocityUnits.FeetPerSecond), VelocityUnits.FeetPerSecond, 1);

    public Variable<UnitLess> ShellSideReynoldsNumber { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellSidePrandtlNumber { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellSideFrictionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellSideCrossflowSections { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellSideNominalCrossflowFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellSidePressureDropCrossflowFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellSideHeatTransferCrossflowFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> CleanOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> CalculatedDirtyOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> ActualOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<UnitLess> CalculatedFoulingResistance { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> FoulingResistanceFraction { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<PressureDrop> TubeSidePressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<PressureDrop> TubeSideAllowedPressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<PressureDrop> TubeSideNozzlePressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<UnitLess> TubeSidePressureDropUtilization { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<PressureDrop> ShellSidePressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<PressureDrop> ShellSideAllowedPressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<PressureDrop> ShellSideNozzlePressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<UnitLess> ShellSidePressureDropUtilization { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);
}
