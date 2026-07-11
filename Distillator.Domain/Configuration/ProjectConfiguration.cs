using Shared.PropertiesDtos.Methods;
using UnitSystem;

namespace Distillator.Domain.Configuration
{
    public class ProjectConfiguration : IProjectConfiguration
    {
        public IUnitConfiguration UnitDefaults { get; set; }
        public IList<IProjectUnitSystem> UnitSystems { get; set; }
        public string ActiveUnitSystemName { get; set; }
        public ICameraConfiguration CameraDefaults { get; set; }
        public INamingConfiguration NamingConfig { get; set; }
        public Guid ThermodynamicMethodId { get; set; }
        public ThermodynamicMethodFullDto? ThermodynamicMethod { get; set; }
        public IReportConfiguration ReportConfig { get; set; }
        public IEquipmentDesignConfiguration EquipmentDesignConfig { get; set; }
        public UnitSystem.Length PlantElevation { get; set; }

        public ProjectConfiguration(
            IUnitConfiguration? unitDefaults = null,
            IList<IProjectUnitSystem>? unitSystems = null,
            string? activeUnitSystemName = null,
            ICameraConfiguration? cameraDefaults = null,
            INamingConfiguration? namingConfig = null,
            Guid? thermodynamicMethodId = null,
            ThermodynamicMethodFullDto? thermodynamicMethod = null,
            IReportConfiguration? reportConfig = null,
            IEquipmentDesignConfiguration? equipmentDesignConfig = null,
            UnitSystem.Length? plantElevation = null)
        {
            UnitSystems = BuildUnitSystems(unitSystems, unitDefaults);
            ActiveUnitSystemName = ResolveActiveUnitSystemName(activeUnitSystemName, UnitSystems);
            UnitDefaults = UnitSystems.FirstOrDefault(system => system.Name == ActiveUnitSystemName)?.Units
                ?? unitDefaults
                ?? UnitConfiguration.SI();
            CameraDefaults = cameraDefaults ?? new CameraConfiguration();
            NamingConfig = namingConfig ?? new NamingConfiguration();
            ThermodynamicMethodId = thermodynamicMethodId ?? thermodynamicMethod?.Id ?? Guid.Empty;
            ThermodynamicMethod = thermodynamicMethod;
            ReportConfig = reportConfig ?? new ReportConfiguration();
            EquipmentDesignConfig = equipmentDesignConfig ?? new EquipmentDesignConfiguration();
            PlantElevation = plantElevation ?? new UnitSystem.Length(0, UnitSystem.LengthUnits.Meter);
        }

        private static IList<IProjectUnitSystem> BuildUnitSystems(IList<IProjectUnitSystem>? unitSystems, IUnitConfiguration? unitDefaults)
        {
            if (unitSystems != null && unitSystems.Count > 0)
            {
                return unitSystems;
            }

            var systems = new List<IProjectUnitSystem>
            {
                ProjectUnitSystem.SI(),
                ProjectUnitSystem.English()
            };

            if (unitDefaults != null && !Matches(unitDefaults, systems[0].Units) && !Matches(unitDefaults, systems[1].Units))
            {
                systems.Add(new ProjectUnitSystem("Custom", unitDefaults));
            }

            return systems;
        }

        private static string ResolveActiveUnitSystemName(string? activeUnitSystemName, IList<IProjectUnitSystem> unitSystems)
        {
            if (!string.IsNullOrWhiteSpace(activeUnitSystemName) && unitSystems.Any(system => system.Name == activeUnitSystemName))
            {
                return activeUnitSystemName;
            }

            return unitSystems.FirstOrDefault()?.Name ?? "SI";
        }

        private static bool Matches(IUnitConfiguration left, IUnitConfiguration right)
        {
            return left.DefaultPressureUnit == right.DefaultPressureUnit
                && left.DefaultTemperatureUnit == right.DefaultTemperatureUnit
                && left.DefaultMassFlowUnit == right.DefaultMassFlowUnit
                && left.DefaultMolarFlowUnit == right.DefaultMolarFlowUnit
                && left.DefaultEnergyUnit == right.DefaultEnergyUnit
                && left.DefaultPowerUnit == right.DefaultPowerUnit
                && left.DefaultLengthUnit == right.DefaultLengthUnit
                && left.DefaultDensityUnit == right.DefaultDensityUnit
                && left.DefaultViscosityUnit == right.DefaultViscosityUnit
                && left.DefaultThermalConductivityUnit == right.DefaultThermalConductivityUnit;
        }
    }

    public class ReportConfiguration : IReportConfiguration
    {
        public IEnumerable<string> AvailableTemplates { get; set; }
        public string DefaultFormat { get; set; }
        public bool AutoExportOnSimulation { get; set; }

        public ReportConfiguration(IEnumerable<string>? availableTemplates = null, string defaultFormat = "PDF", bool autoExportOnSimulation = false)
        {
            AvailableTemplates = availableTemplates?.ToList() ?? new List<string> { "Standard", "Summary", "Detailed" };
            DefaultFormat = defaultFormat;
            AutoExportOnSimulation = autoExportOnSimulation;
        }
    }

    public class EquipmentDesignConfiguration : IEquipmentDesignConfiguration
    {
        public string Standard { get; set; }
        public string RatingBasis { get; set; }

        public EquipmentDesignConfiguration(string standard = "API", string ratingBasis = "normal")
        {
            Standard = standard;
            RatingBasis = ratingBasis;
        }
    }
}
