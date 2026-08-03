using Shared.ProcessFlowDiagram.Designs;
using Shared.SolverConsecutive;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed class ShellAndTubeDesignVariables : IDesignVariables
{
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

    public Variable<HeatTransferCoefficient> AssumedDirtyOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> LastCalculatedDirtyOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<Area> AssumedArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<Area> RequiredArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<Area> ActualArea { get; } =
        new(new Area(0, SurfaceUnits.Foot2), SurfaceUnits.Foot2, 1);

    public Variable<UnitLess> RequiredTubeCount { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ActualTubeCount { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> MaximumTubeCount { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<Diameter> ShellInsideDiameter { get; } =
        new(new Diameter(0, DiameterUnits.Inch), DiameterUnits.Inch, 1);

    public Variable<Velocity> TubeVelocity { get; } =
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

    public Variable<UnitLess> TubeSideFrictionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> ShellSideHeatTransferCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<Velocity> ShellSideVelocity { get; } =
        new(new Velocity(0, VelocityUnits.FeetPerSecond), VelocityUnits.FeetPerSecond, 1);

    public Variable<UnitLess> ShellSideReynoldsNumber { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellSideFrictionFactor { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<UnitLess> ShellSideCrossflowSections { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<HeatTransferCoefficient> CleanOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> CalculatedDirtyOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<HeatTransferCoefficient> ActualOverallCoefficient { get; } =
        new(new HeatTransferCoefficient(0, HeatTransferCoefficientUnits.BTU_hr_ft2_F), HeatTransferCoefficientUnits.BTU_hr_ft2_F, 1);

    public Variable<UnitLess> CalculatedFoulingResistance { get; } =
        new(new UnitLess(0), UnitLessUnits.None, 1);

    public Variable<PressureDrop> TubeSidePressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);

    public Variable<PressureDrop> ShellSidePressureDrop { get; } =
        new(new PressureDrop(0, PressureDropUnits.psi), PressureDropUnits.psi, 1);
}
