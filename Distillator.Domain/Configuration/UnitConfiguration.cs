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
        public UnitMeasure DefaultDiameterUnit { get; set; }
        public UnitMeasure DefaultSurfaceUnit { get; set; }
        public UnitMeasure DefaultVolumeUnit { get; set; }
        public UnitMeasure DefaultTimeUnit { get; set; }
        public UnitMeasure DefaultVelocityUnit { get; set; }
        public UnitMeasure DefaultMassUnit { get; set; }
        public UnitMeasure DefaultForceUnit { get; set; }
        public UnitMeasure DefaultElectricUnit { get; set; }
        public UnitMeasure DefaultMotorVelocityUnit { get; set; }
        public UnitMeasure DefaultAmountOfSubstanceUnit { get; set; }
        public UnitMeasure DefaultHeatTransferCoefficientUnit { get; set; }
        public UnitMeasure DefaultDensityUnit { get; set; }
        public UnitMeasure DefaultMolarDensityUnit { get; set; }
        public UnitMeasure DefaultMassVolumeSpecificUnit { get; set; }
        public UnitMeasure DefaultMolarVolumeSpecificUnit { get; set; }
        public UnitMeasure DefaultPressureDropLengthUnit { get; set; }
        public UnitMeasure DefaultPressureDropUnit { get; set; }
        public UnitMeasure DefaultViscosityUnit { get; set; }
        public UnitMeasure DefaultThermalConductivityUnit { get; set; }
        public UnitMeasure DefaultVolumeEnergyUnit { get; set; }
        public UnitMeasure DefaultMassEnergyUnit { get; set; }
        public UnitMeasure DefaultMolarEnergyUnit { get; set; }
        public UnitMeasure DefaultMassEntropyUnit { get; set; }
        public UnitMeasure DefaultMolarEntropyUnit { get; set; }
        public UnitMeasure DefaultHeatSurfaceFlowUnit { get; set; }
        public UnitMeasure DefaultVolumetricFlowUnit { get; set; }
        public UnitMeasure DefaultEnergyFlowUnit { get; set; }
        public UnitMeasure DefaultSuperficialTensionUnit { get; set; }

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
            UnitMeasure defaultThermalConductivityUnit,
            UnitMeasure? defaultDiameterUnit = null,
            UnitMeasure? defaultSurfaceUnit = null,
            UnitMeasure? defaultVolumeUnit = null,
            UnitMeasure? defaultTimeUnit = null,
            UnitMeasure? defaultVelocityUnit = null,
            UnitMeasure? defaultMassUnit = null,
            UnitMeasure? defaultForceUnit = null,
            UnitMeasure? defaultElectricUnit = null,
            UnitMeasure? defaultMotorVelocityUnit = null,
            UnitMeasure? defaultAmountOfSubstanceUnit = null,
            UnitMeasure? defaultHeatTransferCoefficientUnit = null,
            UnitMeasure? defaultMolarDensityUnit = null,
            UnitMeasure? defaultMassVolumeSpecificUnit = null,
            UnitMeasure? defaultMolarVolumeSpecificUnit = null,
            UnitMeasure? defaultPressureDropLengthUnit = null,
            UnitMeasure? defaultPressureDropUnit = null,
            UnitMeasure? defaultVolumeEnergyUnit = null,
            UnitMeasure? defaultMassEnergyUnit = null,
            UnitMeasure? defaultMolarEnergyUnit = null,
            UnitMeasure? defaultMassEntropyUnit = null,
            UnitMeasure? defaultMolarEntropyUnit = null,
            UnitMeasure? defaultHeatSurfaceFlowUnit = null,
            UnitMeasure? defaultVolumetricFlowUnit = null,
            UnitMeasure? defaultEnergyFlowUnit = null,
            UnitMeasure? defaultSuperficialTensionUnit = null)
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
            DefaultDiameterUnit = defaultDiameterUnit ?? DiameterUnits.MilliMeter;
            DefaultSurfaceUnit = defaultSurfaceUnit ?? SurfaceUnits.Meter2;
            DefaultVolumeUnit = defaultVolumeUnit ?? VolumeUnits.Meter3;
            DefaultTimeUnit = defaultTimeUnit ?? TimeUnits.Second;
            DefaultVelocityUnit = defaultVelocityUnit ?? VelocityUnits.MeterPerSecond;
            DefaultMassUnit = defaultMassUnit ?? MassUnits.KiloGram;
            DefaultForceUnit = defaultForceUnit ?? ForceUnits.Newton;
            DefaultElectricUnit = defaultElectricUnit ?? ElectricUnits.Ampere;
            DefaultMotorVelocityUnit = defaultMotorVelocityUnit ?? MotorVelocityUnits.RPM;
            DefaultAmountOfSubstanceUnit = defaultAmountOfSubstanceUnit ?? AmountOfSubstanceUnits.KMole;
            DefaultHeatTransferCoefficientUnit = defaultHeatTransferCoefficientUnit ?? HeatTransferCoefficientUnits.Watt_m2_K;
            DefaultMolarDensityUnit = defaultMolarDensityUnit ?? MolarDensityUnits.Kgmol_m3;
            DefaultMassVolumeSpecificUnit = defaultMassVolumeSpecificUnit ?? MassVolumeSpecificUnits.m3_Kg;
            DefaultMolarVolumeSpecificUnit = defaultMolarVolumeSpecificUnit ?? MolarVolumeSpecificUnits.m3_Kgmol;
            DefaultPressureDropLengthUnit = defaultPressureDropLengthUnit ?? PressureDropLengthUnits.Kpa_m;
            DefaultPressureDropUnit = defaultPressureDropUnit ?? PressureDropUnits.KiloPascal;
            DefaultVolumeEnergyUnit = defaultVolumeEnergyUnit ?? VolumeEnergyUnits.KJ_m3;
            DefaultMassEnergyUnit = defaultMassEnergyUnit ?? MassEnergyUnits.KJ_Kg;
            DefaultMolarEnergyUnit = defaultMolarEnergyUnit ?? MolarEnergyUnits.KJ_Kgmol;
            DefaultMassEntropyUnit = defaultMassEntropyUnit ?? MassEntropyUnits.KJ_Kg_C;
            DefaultMolarEntropyUnit = defaultMolarEntropyUnit ?? MolarEntropyUnits.KJ_Kgmol_C;
            DefaultHeatSurfaceFlowUnit = defaultHeatSurfaceFlowUnit ?? HeatSurfaceFlowUnits.W_m2;
            DefaultVolumetricFlowUnit = defaultVolumetricFlowUnit ?? VolumetricFlowUnits.m3_hr;
            DefaultEnergyFlowUnit = defaultEnergyFlowUnit ?? EnergyFlowUnits.KJ_hr;
            DefaultSuperficialTensionUnit = defaultSuperficialTensionUnit ?? SuperficialTensionUnits.N_m;
        }

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
                defaultThermalConductivityUnit: ThermalConductivityUnits.W_m_K,
                defaultDiameterUnit: DiameterUnits.MilliMeter,
                defaultSurfaceUnit: SurfaceUnits.Meter2,
                defaultVolumeUnit: VolumeUnits.Meter3,
                defaultTimeUnit: TimeUnits.Second,
                defaultVelocityUnit: VelocityUnits.MeterPerSecond,
                defaultMassUnit: MassUnits.KiloGram,
                defaultForceUnit: ForceUnits.Newton,
                defaultElectricUnit: ElectricUnits.Ampere,
                defaultMotorVelocityUnit: MotorVelocityUnits.RPM,
                defaultAmountOfSubstanceUnit: AmountOfSubstanceUnits.KMole,
                defaultHeatTransferCoefficientUnit: HeatTransferCoefficientUnits.Watt_m2_K,
                defaultMolarDensityUnit: MolarDensityUnits.Kgmol_m3,
                defaultMassVolumeSpecificUnit: MassVolumeSpecificUnits.m3_Kg,
                defaultMolarVolumeSpecificUnit: MolarVolumeSpecificUnits.m3_Kgmol,
                defaultPressureDropLengthUnit: PressureDropLengthUnits.Kpa_m,
                defaultPressureDropUnit: PressureDropUnits.KiloPascal,
                defaultVolumeEnergyUnit: VolumeEnergyUnits.KJ_m3,
                defaultMassEnergyUnit: MassEnergyUnits.KJ_Kg,
                defaultMolarEnergyUnit: MolarEnergyUnits.KJ_Kgmol,
                defaultMassEntropyUnit: MassEntropyUnits.KJ_Kg_C,
                defaultMolarEntropyUnit: MolarEntropyUnits.KJ_Kgmol_C,
                defaultHeatSurfaceFlowUnit: HeatSurfaceFlowUnits.W_m2,
                defaultVolumetricFlowUnit: VolumetricFlowUnits.m3_hr,
                defaultEnergyFlowUnit: EnergyFlowUnits.KJ_hr,
                defaultSuperficialTensionUnit: SuperficialTensionUnits.N_m);
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
                defaultThermalConductivityUnit: ThermalConductivityUnits.BTU_ft_hr_ft2_m_F,
                defaultDiameterUnit: DiameterUnits.Inch,
                defaultSurfaceUnit: SurfaceUnits.Foot2,
                defaultVolumeUnit: VolumeUnits.Foot3,
                defaultTimeUnit: TimeUnits.Hour,
                defaultVelocityUnit: VelocityUnits.FeetPerSecond,
                defaultMassUnit: MassUnits.Pound,
                defaultForceUnit: ForceUnits.Newton,
                defaultElectricUnit: ElectricUnits.Ampere,
                defaultMotorVelocityUnit: MotorVelocityUnits.RPM,
                defaultAmountOfSubstanceUnit: AmountOfSubstanceUnits.lbMole,
                defaultHeatTransferCoefficientUnit: HeatTransferCoefficientUnits.BTU_hr_ft2_F,
                defaultMolarDensityUnit: MolarDensityUnits.lbmol_ft3,
                defaultMassVolumeSpecificUnit: MassVolumeSpecificUnits.ft3_lb,
                defaultMolarVolumeSpecificUnit: MolarVolumeSpecificUnits.ft3_lbmol,
                defaultPressureDropLengthUnit: PressureDropLengthUnits.psi_100ft,
                defaultPressureDropUnit: PressureDropUnits.psi,
                defaultVolumeEnergyUnit: VolumeEnergyUnits.BTU_ft3,
                defaultMassEnergyUnit: MassEnergyUnits.BTU_lb,
                defaultMolarEnergyUnit: MolarEnergyUnits.BTU_lbmol,
                defaultMassEntropyUnit: MassEntropyUnits.BTU_lb_F,
                defaultMolarEntropyUnit: MolarEntropyUnits.BTU_lbmol_F,
                defaultHeatSurfaceFlowUnit: HeatSurfaceFlowUnits.BTU_hr_ft2,
                defaultVolumetricFlowUnit: VolumetricFlowUnits.ft3_hr,
                defaultEnergyFlowUnit: EnergyFlowUnits.BTUhr,
                defaultSuperficialTensionUnit: SuperficialTensionUnits.lbf_ft);
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
                source.DefaultThermalConductivityUnit,
                source.DefaultDiameterUnit,
                source.DefaultSurfaceUnit,
                source.DefaultVolumeUnit,
                source.DefaultTimeUnit,
                source.DefaultVelocityUnit,
                source.DefaultMassUnit,
                source.DefaultForceUnit,
                source.DefaultElectricUnit,
                source.DefaultMotorVelocityUnit,
                source.DefaultAmountOfSubstanceUnit,
                source.DefaultHeatTransferCoefficientUnit,
                source.DefaultMolarDensityUnit,
                source.DefaultMassVolumeSpecificUnit,
                source.DefaultMolarVolumeSpecificUnit,
                source.DefaultPressureDropLengthUnit,
                source.DefaultPressureDropUnit,
                source.DefaultVolumeEnergyUnit,
                source.DefaultMassEnergyUnit,
                source.DefaultMolarEnergyUnit,
                source.DefaultMassEntropyUnit,
                source.DefaultMolarEntropyUnit,
                source.DefaultHeatSurfaceFlowUnit,
                source.DefaultVolumetricFlowUnit,
                source.DefaultEnergyFlowUnit,
                source.DefaultSuperficialTensionUnit);
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
