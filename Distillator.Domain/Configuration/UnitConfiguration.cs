using UnitSystem;

namespace Distillator.Domain.Configuration
{
    public class UnitConfiguration : IUnitConfiguration
    {
        public UnitMeasure DefaultPressureUnit { get; set; }
        public UnitMeasure DefaultTemperatureUnit { get; set; }
        public UnitMeasure DefaultMassFlowUnit { get; set; }
        public UnitMeasure DefaultMolarFlowUnit { get; set; }
        public UnitMeasure DefaultEnergyUnit { get; set; }
        public UnitMeasure DefaultPowerUnit { get; set; }
        public UnitMeasure DefaultLengthUnit { get; set; }
        public UnitMeasure DefaultDensityUnit { get; set; }
        public UnitMeasure DefaultViscosityUnit { get; set; }
        public UnitMeasure DefaultThermalConductivityUnit { get; set; }

        public UnitConfiguration(
            UnitMeasure defaultPressureUnit,
            UnitMeasure defaultTemperatureUnit,
            UnitMeasure defaultMassFlowUnit,
            UnitMeasure defaultMolarFlowUnit,
            UnitMeasure defaultEnergyUnit,
            UnitMeasure defaultPowerUnit,
            UnitMeasure defaultLengthUnit,
            UnitMeasure defaultDensityUnit,
            UnitMeasure defaultViscosityUnit,
            UnitMeasure defaultThermalConductivityUnit)
        {
            DefaultPressureUnit = defaultPressureUnit ?? UnitMeasure.None;
            DefaultTemperatureUnit = defaultTemperatureUnit ?? UnitMeasure.None;
            DefaultMassFlowUnit = defaultMassFlowUnit ?? UnitMeasure.None;
            DefaultMolarFlowUnit = defaultMolarFlowUnit ?? UnitMeasure.None;
            DefaultEnergyUnit = defaultEnergyUnit ?? UnitMeasure.None;
            DefaultPowerUnit = defaultPowerUnit ?? UnitMeasure.None;
            DefaultLengthUnit = defaultLengthUnit ?? UnitMeasure.None;
            DefaultDensityUnit = defaultDensityUnit ?? UnitMeasure.None;
            DefaultViscosityUnit = defaultViscosityUnit ?? UnitMeasure.None;
            DefaultThermalConductivityUnit = defaultThermalConductivityUnit ?? UnitMeasure.None;
        }

        /// <summary>
        /// Attempts to build a SI-like default configuration using UnitManager.
        /// Falls back to UnitMeasure.None for any unit not found.
        /// </summary>
        public static IUnitConfiguration SI()
        {
            return new UnitConfiguration(
                defaultPressureUnit: PressureUnits.Bara,
                defaultTemperatureUnit: TemperatureUnits.DegreeCelcius,
                defaultMassFlowUnit: MassFlowUnits.Kg_hr,
                defaultMolarFlowUnit: MolarFlowUnits.Kgmol_hr,
                defaultEnergyUnit: EnergyUnits.KiloJoule,
                defaultPowerUnit: PowerUnits.KiloWatt,
                defaultLengthUnit: LengthUnits.Meter,
                defaultDensityUnit: MassDensityUnits.Kg_m3,
                defaultViscosityUnit: ViscosityUnits.cPoise,
                defaultThermalConductivityUnit: ThermalConductivityUnits.W_m_K
            );
        }

        public static IUnitConfiguration English()
        {
            return new UnitConfiguration(
                defaultPressureUnit: PressureUnits.Psia,
                defaultTemperatureUnit: TemperatureUnits.DegreeFahrenheit,
                defaultMassFlowUnit: MassFlowUnits.lb_hr,
                defaultMolarFlowUnit: MolarFlowUnits.Kgmol_hr,
                defaultEnergyUnit: EnergyUnits.BTU,
                defaultPowerUnit: PowerUnits.HorsePower,
                defaultLengthUnit: LengthUnits.Foot,
                defaultDensityUnit: MassDensityUnits.lb_ft3,
                defaultViscosityUnit: ViscosityUnits.cPoise,
                defaultThermalConductivityUnit: ThermalConductivityUnits.BTU_ft_hr_ft2_m_F
            );
        }

        public static UnitConfiguration Clone(IUnitConfiguration source)
        {
            return new UnitConfiguration(
                source.DefaultPressureUnit,
                source.DefaultTemperatureUnit,
                source.DefaultMassFlowUnit,
                source.DefaultMolarFlowUnit,
                source.DefaultEnergyUnit,
                source.DefaultPowerUnit,
                source.DefaultLengthUnit,
                source.DefaultDensityUnit,
                source.DefaultViscosityUnit,
                source.DefaultThermalConductivityUnit);
        }
    }

    public class ProjectUnitSystem : IProjectUnitSystem
    {
        public string Name { get; set; }
        public bool IsBuiltIn { get; set; }
        public IUnitConfiguration Units { get; set; }

        public ProjectUnitSystem(string name, IUnitConfiguration units, bool isBuiltIn = false)
        {
            Name = name;
            IsBuiltIn = isBuiltIn;
            Units = UnitConfiguration.Clone(units);
        }

        public static ProjectUnitSystem SI() => new("SI", UnitConfiguration.SI(), true);

        public static ProjectUnitSystem English() => new("English", UnitConfiguration.English(), true);

        public ProjectUnitSystem CloneAs(string name)
        {
            return new ProjectUnitSystem(name, Units, false);
        }
    }
}
