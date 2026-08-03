using System.Text.Json;
using Distillator.Domain.Configuration;
using Shared.Projects;
using Shared.PropertiesDtos.Methods;
using UnitSystem;

namespace Distillator.Domain.Persistence;

public static class ProjectConfigurationPersistenceMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ProjectBasicConfigurationDto ToDto(IProjectConfiguration configuration)
    {
        return new ProjectBasicConfigurationDto
        {
            ThermodynamicMethodId = configuration.ThermodynamicMethodId == Guid.Empty
                ? null
                : configuration.ThermodynamicMethodId,
            PlantElevationValue = configuration.PlantElevation.Value,
            PlantElevationUnit = UnitName(configuration.PlantElevation.Unit),
            ActiveUnitSystemName = configuration.ActiveUnitSystemName,
            UnitSystemsJson = Serialize(configuration.UnitSystems.Select(ToSnapshot).ToList()),
            CameraConfigurationJson = Serialize(ToSnapshot(configuration.CameraDefaults)),
            NamingConfigurationJson = Serialize(ToSnapshot(configuration.NamingConfig)),
            ReportConfigurationJson = Serialize(ToSnapshot(configuration.ReportConfig)),
            EquipmentDesignConfigurationJson = Serialize(ToSnapshot(configuration.EquipmentDesignConfig))
        };
    }

    public static IProjectConfiguration FromDto(
        ProjectBasicConfigurationDto configuration,
        ThermodynamicMethodFullDto? thermodynamicMethod = null)
    {
        var unitSystems = BuildUnitSystemsFromSnapshots(
            Deserialize(configuration.UnitSystemsJson, new List<ProjectUnitSystemSnapshot>()));

        return new ProjectConfiguration(
            unitSystems: unitSystems,
            activeUnitSystemName: configuration.ActiveUnitSystemName,
            cameraDefaults: FromSnapshot(Deserialize(configuration.CameraConfigurationJson, ToSnapshot(new CameraConfiguration()))),
            namingConfig: FromSnapshot(Deserialize(configuration.NamingConfigurationJson, ToSnapshot(new NamingConfiguration()))),
            thermodynamicMethodId: configuration.ThermodynamicMethodId,
            thermodynamicMethod: thermodynamicMethod,
            reportConfig: FromSnapshot(Deserialize(configuration.ReportConfigurationJson, ToSnapshot(new ReportConfiguration()))),
            equipmentDesignConfig: FromSnapshot(Deserialize(configuration.EquipmentDesignConfigurationJson, ToSnapshot(new EquipmentDesignConfiguration()))),
            plantElevation: new UnitSystem.Length(
                configuration.PlantElevationValue,
                ResolveUnit(configuration.PlantElevationUnit, LengthUnits.Meter)));
    }

    private static ProjectUnitSystemSnapshot ToSnapshot(IProjectUnitSystem system)
    {
        return new ProjectUnitSystemSnapshot(
            system.Name,
            system.IsBuiltIn,
            ToSnapshot(system.Units));
    }

    private static UnitConfigurationSnapshot ToSnapshot(IUnitConfiguration units)
    {
        return new UnitConfigurationSnapshot(
            UnitName(units.DefaultPressureUnit),
            UnitName(units.DefaultTemperatureUnit),
            UnitName(units.DefaultMassFlowUnit),
            UnitName(units.DefaultMolarFlowUnit),
            UnitName(units.DefaultEnergyUnit),
            UnitName(units.DefaultPowerUnit),
            UnitName(units.DefaultLengthUnit),
            UnitName(units.DefaultDiameterUnit),
            UnitName(units.DefaultSurfaceUnit),
            UnitName(units.DefaultVolumeUnit),
            UnitName(units.DefaultTimeUnit),
            UnitName(units.DefaultVelocityUnit),
            UnitName(units.DefaultMassUnit),
            UnitName(units.DefaultForceUnit),
            UnitName(units.DefaultElectricUnit),
            UnitName(units.DefaultMotorVelocityUnit),
            UnitName(units.DefaultAmountOfSubstanceUnit),
            UnitName(units.DefaultHeatTransferCoefficientUnit),
            UnitName(units.DefaultDensityUnit),
            UnitName(units.DefaultMolarDensityUnit),
            UnitName(units.DefaultMassVolumeSpecificUnit),
            UnitName(units.DefaultMolarVolumeSpecificUnit),
            UnitName(units.DefaultPressureDropLengthUnit),
            UnitName(units.DefaultPressureDropUnit),
            UnitName(units.DefaultViscosityUnit),
            UnitName(units.DefaultThermalConductivityUnit),
            UnitName(units.DefaultVolumeEnergyUnit),
            UnitName(units.DefaultMassEnergyUnit),
            UnitName(units.DefaultMolarEnergyUnit),
            UnitName(units.DefaultMassEntropyUnit),
            UnitName(units.DefaultMolarEntropyUnit),
            UnitName(units.DefaultHeatSurfaceFlowUnit),
            UnitName(units.DefaultVolumetricFlowUnit),
            UnitName(units.DefaultEnergyFlowUnit),
            UnitName(units.DefaultSuperficialTensionUnit));
    }

    private static CameraConfigurationSnapshot ToSnapshot(ICameraConfiguration camera)
    {
        return new CameraConfigurationSnapshot(
            camera.DefaultZoom,
            camera.DefaultPanX,
            camera.DefaultPanY,
            camera.GlobalScale,
            camera.GridSize,
            camera.MinZoom,
            camera.MaxZoom);
    }

    private static NamingConfigurationSnapshot ToSnapshot(INamingConfiguration naming)
    {
        return new NamingConfigurationSnapshot(
            naming.Mode.ToString(),
            naming.Pattern,
            naming.StartingNumber,
            naming.BaseNumber,
            naming.AreaPrefix,
            naming.CounterScope.ToString(),
            naming.PatternParts.Select(part => new NamingPatternPartSnapshot(part.Kind.ToString(), part.Value)).ToList(),
            new Dictionary<string, string>(naming.PrefixesByEquipmentType, StringComparer.OrdinalIgnoreCase));
    }

    private static ReportConfigurationSnapshot ToSnapshot(IReportConfiguration report)
    {
        return new ReportConfigurationSnapshot(
            report.AvailableTemplates.ToList(),
            report.DefaultFormat,
            report.AutoExportOnSimulation);
    }

    private static EquipmentDesignConfigurationSnapshot ToSnapshot(IEquipmentDesignConfiguration design)
    {
        return new EquipmentDesignConfigurationSnapshot(design.Standard, design.RatingBasis);
    }

    private static ProjectUnitSystem FromSnapshot(ProjectUnitSystemSnapshot snapshot)
    {
        return new ProjectUnitSystem(
            string.IsNullOrWhiteSpace(snapshot.Name) ? "Custom" : snapshot.Name,
            FromSnapshot(snapshot.Units),
            snapshot.IsBuiltIn);
    }

    private static List<IProjectUnitSystem> BuildUnitSystemsFromSnapshots(List<ProjectUnitSystemSnapshot> snapshots)
    {
        var systems = new List<IProjectUnitSystem>
        {
            ProjectUnitSystem.SI(),
            ProjectUnitSystem.English()
        };

        foreach (var snapshot in snapshots.Where(item => !item.IsBuiltIn))
        {
            var name = string.IsNullOrWhiteSpace(snapshot.Name) ? "Custom" : snapshot.Name;
            if (systems.Any(system => system.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            systems.Add(FromSnapshot(snapshot));
        }

        return systems;
    }

    private static UnitConfiguration FromSnapshot(UnitConfigurationSnapshot snapshot)
    {
        return new UnitConfiguration(
            ResolveUnit(snapshot.Pressure, PressureUnits.Bara),
            ResolveUnit(snapshot.Temperature, TemperatureUnits.DegreeCelcius),
            ResolveUnit(snapshot.MassFlow, MassFlowUnits.Kg_hr),
            ResolveUnit(snapshot.MolarFlow, MolarFlowUnits.Kgmol_hr),
            ResolveUnit(snapshot.Energy, EnergyUnits.KiloJoule),
            ResolveUnit(snapshot.Power, PowerUnits.KiloWatt),
            ResolveUnit(snapshot.Length, LengthUnits.Meter),
            ResolveUnit(snapshot.Density, MassDensityUnits.Kg_m3),
            ResolveUnit(snapshot.Viscosity, ViscosityUnits.cPoise),
            ResolveUnit(snapshot.ThermalConductivity, ThermalConductivityUnits.W_m_K),
            ResolveUnit(snapshot.Diameter, DiameterUnits.MilliMeter),
            ResolveUnit(snapshot.Surface, SurfaceUnits.Meter2),
            ResolveUnit(snapshot.Volume, VolumeUnits.Meter3),
            ResolveUnit(snapshot.Time, TimeUnits.Second),
            ResolveUnit(snapshot.Velocity, VelocityUnits.MeterPerSecond),
            ResolveUnit(snapshot.Mass, MassUnits.KiloGram),
            ResolveUnit(snapshot.Force, ForceUnits.Newton),
            ResolveUnit(snapshot.Electric, ElectricUnits.Ampere),
            ResolveUnit(snapshot.MotorVelocity, MotorVelocityUnits.RPM),
            ResolveUnit(snapshot.AmountOfSubstance, AmountOfSubstanceUnits.KMole),
            ResolveUnit(snapshot.HeatTransferCoefficient, HeatTransferCoefficientUnits.Watt_m2_K),
            ResolveUnit(snapshot.MolarDensity, MolarDensityUnits.Kgmol_m3),
            ResolveUnit(snapshot.MassVolumeSpecific, MassVolumeSpecificUnits.m3_Kg),
            ResolveUnit(snapshot.MolarVolumeSpecific, MolarVolumeSpecificUnits.m3_Kgmol),
            ResolveUnit(snapshot.PressureDropLength, PressureDropLengthUnits.Kpa_m),
            ResolveUnit(snapshot.PressureDrop, PressureDropUnits.KiloPascal),
            ResolveUnit(snapshot.VolumeEnergy, VolumeEnergyUnits.KJ_m3),
            ResolveUnit(snapshot.MassEnergy, MassEnergyUnits.KJ_Kg),
            ResolveUnit(snapshot.MolarEnergy, MolarEnergyUnits.KJ_Kgmol),
            ResolveUnit(snapshot.MassEntropy, MassEntropyUnits.KJ_Kg_C),
            ResolveUnit(snapshot.MolarEntropy, MolarEntropyUnits.KJ_Kgmol_C),
            ResolveUnit(snapshot.HeatSurfaceFlow, HeatSurfaceFlowUnits.W_m2),
            ResolveUnit(snapshot.VolumetricFlow, VolumetricFlowUnits.m3_hr),
            ResolveUnit(snapshot.EnergyFlow, EnergyFlowUnits.KJ_hr),
            ResolveUnit(snapshot.SuperficialTension, SuperficialTensionUnits.N_m));
    }

    private static CameraConfiguration FromSnapshot(CameraConfigurationSnapshot snapshot)
    {
        return new CameraConfiguration(
            snapshot.DefaultZoom,
            snapshot.DefaultPanX,
            snapshot.DefaultPanY,
            snapshot.GlobalScale,
            snapshot.GridSize,
            snapshot.MinZoom,
            snapshot.MaxZoom);
    }

    private static NamingConfiguration FromSnapshot(NamingConfigurationSnapshot snapshot)
    {
        return new NamingConfiguration(
            mode: ParseEnum(snapshot.Mode, NamingMode.ProjectSequential),
            pattern: snapshot.Pattern,
            startingNumber: snapshot.StartingNumber,
            baseNumber: snapshot.BaseNumber,
            areaPrefix: snapshot.AreaPrefix,
            counterScope: ParseEnum(snapshot.CounterScope, NamingCounterScope.Project),
            patternParts: (snapshot.PatternParts ?? new List<NamingPatternPartSnapshot>())
                .Select(part => new NamingPatternPart(ParseEnum(part.Kind, NamingPatternPartKind.Literal), part.Value))
                .ToList(),
            prefixesByEquipmentType: snapshot.PrefixesByEquipmentType ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    private static ReportConfiguration FromSnapshot(ReportConfigurationSnapshot snapshot)
    {
        return new ReportConfiguration(
            snapshot.AvailableTemplates ?? new List<string>(),
            snapshot.DefaultFormat,
            snapshot.AutoExportOnSimulation);
    }

    private static EquipmentDesignConfiguration FromSnapshot(EquipmentDesignConfigurationSnapshot snapshot)
    {
        return new EquipmentDesignConfiguration(snapshot.Standard, snapshot.RatingBasis);
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static T Deserialize<T>(string json, T fallback)
    {
        if (string.IsNullOrWhiteSpace(json)) return fallback;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static UnitMeasure ResolveUnit(string? unitName, UnitMeasure fallback)
    {
        if (string.IsNullOrWhiteSpace(unitName)) return fallback;

        try
        {
            return UnitManager.GetUnitByName(unitName);
        }
        catch
        {
            return fallback;
        }
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }

    private static string UnitName(UnitMeasure? unit)
    {
        return string.IsNullOrWhiteSpace(unit?.Name) ? UnitMeasure.None.Name : unit.Name;
    }

    private sealed record ProjectUnitSystemSnapshot(string Name, bool IsBuiltIn, UnitConfigurationSnapshot Units);

    private sealed class UnitConfigurationSnapshot
    {
        public UnitConfigurationSnapshot()
        {
        }

        public UnitConfigurationSnapshot(
            string pressure,
            string temperature,
            string massFlow,
            string molarFlow,
            string energy,
            string power,
            string length,
            string diameter,
            string surface,
            string volume,
            string time,
            string velocity,
            string mass,
            string force,
            string electric,
            string motorVelocity,
            string amountOfSubstance,
            string heatTransferCoefficient,
            string density,
            string molarDensity,
            string massVolumeSpecific,
            string molarVolumeSpecific,
            string pressureDropLength,
            string pressureDrop,
            string viscosity,
            string thermalConductivity,
            string volumeEnergy,
            string massEnergy,
            string molarEnergy,
            string massEntropy,
            string molarEntropy,
            string heatSurfaceFlow,
            string volumetricFlow,
            string energyFlow,
            string superficialTension)
        {
            Pressure = pressure;
            Temperature = temperature;
            MassFlow = massFlow;
            MolarFlow = molarFlow;
            Energy = energy;
            Power = power;
            Length = length;
            Diameter = diameter;
            Surface = surface;
            Volume = volume;
            Time = time;
            Velocity = velocity;
            Mass = mass;
            Force = force;
            Electric = electric;
            MotorVelocity = motorVelocity;
            AmountOfSubstance = amountOfSubstance;
            HeatTransferCoefficient = heatTransferCoefficient;
            Density = density;
            MolarDensity = molarDensity;
            MassVolumeSpecific = massVolumeSpecific;
            MolarVolumeSpecific = molarVolumeSpecific;
            PressureDropLength = pressureDropLength;
            PressureDrop = pressureDrop;
            Viscosity = viscosity;
            ThermalConductivity = thermalConductivity;
            VolumeEnergy = volumeEnergy;
            MassEnergy = massEnergy;
            MolarEnergy = molarEnergy;
            MassEntropy = massEntropy;
            MolarEntropy = molarEntropy;
            HeatSurfaceFlow = heatSurfaceFlow;
            VolumetricFlow = volumetricFlow;
            EnergyFlow = energyFlow;
            SuperficialTension = superficialTension;
        }

        public string Pressure { get; set; } = UnitName(PressureUnits.Bara);
        public string Temperature { get; set; } = UnitName(TemperatureUnits.DegreeCelcius);
        public string MassFlow { get; set; } = UnitName(MassFlowUnits.Kg_hr);
        public string MolarFlow { get; set; } = UnitName(MolarFlowUnits.Kgmol_hr);
        public string Energy { get; set; } = UnitName(EnergyUnits.KiloJoule);
        public string Power { get; set; } = UnitName(PowerUnits.KiloWatt);
        public string Length { get; set; } = UnitName(LengthUnits.Meter);
        public string Diameter { get; set; } = UnitName(DiameterUnits.MilliMeter);
        public string Surface { get; set; } = UnitName(SurfaceUnits.Meter2);
        public string Volume { get; set; } = UnitName(VolumeUnits.Meter3);
        public string Time { get; set; } = UnitName(TimeUnits.Second);
        public string Velocity { get; set; } = UnitName(VelocityUnits.MeterPerSecond);
        public string Mass { get; set; } = UnitName(MassUnits.KiloGram);
        public string Force { get; set; } = UnitName(ForceUnits.Newton);
        public string Electric { get; set; } = UnitName(ElectricUnits.Ampere);
        public string MotorVelocity { get; set; } = UnitName(MotorVelocityUnits.RPM);
        public string AmountOfSubstance { get; set; } = UnitName(AmountOfSubstanceUnits.KMole);
        public string HeatTransferCoefficient { get; set; } = UnitName(HeatTransferCoefficientUnits.Watt_m2_K);
        public string Density { get; set; } = UnitName(MassDensityUnits.Kg_m3);
        public string MolarDensity { get; set; } = UnitName(MolarDensityUnits.Kgmol_m3);
        public string MassVolumeSpecific { get; set; } = UnitName(MassVolumeSpecificUnits.m3_Kg);
        public string MolarVolumeSpecific { get; set; } = UnitName(MolarVolumeSpecificUnits.m3_Kgmol);
        public string PressureDropLength { get; set; } = UnitName(PressureDropLengthUnits.Kpa_m);
        public string PressureDrop { get; set; } = UnitName(PressureDropUnits.KiloPascal);
        public string Viscosity { get; set; } = UnitName(ViscosityUnits.cPoise);
        public string ThermalConductivity { get; set; } = UnitName(ThermalConductivityUnits.W_m_K);
        public string VolumeEnergy { get; set; } = UnitName(VolumeEnergyUnits.KJ_m3);
        public string MassEnergy { get; set; } = UnitName(MassEnergyUnits.KJ_Kg);
        public string MolarEnergy { get; set; } = UnitName(MolarEnergyUnits.KJ_Kgmol);
        public string MassEntropy { get; set; } = UnitName(MassEntropyUnits.KJ_Kg_C);
        public string MolarEntropy { get; set; } = UnitName(MolarEntropyUnits.KJ_Kgmol_C);
        public string HeatSurfaceFlow { get; set; } = UnitName(HeatSurfaceFlowUnits.W_m2);
        public string VolumetricFlow { get; set; } = UnitName(VolumetricFlowUnits.m3_hr);
        public string EnergyFlow { get; set; } = UnitName(EnergyFlowUnits.KJ_hr);
        public string SuperficialTension { get; set; } = UnitName(SuperficialTensionUnits.N_m);
    }

    private sealed record CameraConfigurationSnapshot(
        double DefaultZoom,
        double DefaultPanX,
        double DefaultPanY,
        double GlobalScale,
        double GridSize,
        double MinZoom,
        double MaxZoom);

    private sealed record NamingConfigurationSnapshot(
        string Mode,
        string Pattern,
        int StartingNumber,
        string BaseNumber,
        string AreaPrefix,
        string CounterScope,
        List<NamingPatternPartSnapshot> PatternParts,
        Dictionary<string, string> PrefixesByEquipmentType);

    private sealed record NamingPatternPartSnapshot(string Kind, string Value);

    private sealed record ReportConfigurationSnapshot(
        List<string> AvailableTemplates,
        string DefaultFormat,
        bool AutoExportOnSimulation);

    private sealed record EquipmentDesignConfigurationSnapshot(string Standard, string RatingBasis);
}
