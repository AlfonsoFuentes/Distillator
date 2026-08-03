using Distillator.Domain.Configuration;
using Distillator.Domain.Persistence;
using Shared.Projects;
using UnitSystem;

namespace Distillator.Core.Tests.Persistence;

public sealed class ProjectConfigurationPersistenceMapperTests
{
    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "09")]
    [Trait("Level", "Unit")]
    public void ToDtoAndFromDto_ShouldRoundTripConfigurationDocument()
    {
        var configuration = new ProjectConfiguration(
            unitSystems: new List<IProjectUnitSystem>
            {
                ProjectUnitSystem.SI(),
                ProjectUnitSystem.English()
            },
            activeUnitSystemName: ProjectUnitSystem.English().Name,
            cameraDefaults: new CameraConfiguration(defaultZoom: 1.25, defaultPanX: 10, defaultPanY: 20),
            namingConfig: new NamingConfiguration(
                mode: NamingMode.DiagramSequentialWithAreaPrefix,
                pattern: "{Area}-{Prefix}-{Number}",
                startingNumber: 7,
                baseNumber: "A",
                areaPrefix: "PFD",
                counterScope: NamingCounterScope.Diagram),
            plantElevation: new UnitSystem.Length(1500, LengthUnits.Meter));

        var dto = ProjectConfigurationPersistenceMapper.ToDto(configuration);
        var restored = ProjectConfigurationPersistenceMapper.FromDto(dto);

        Assert.Equal(configuration.ActiveUnitSystemName, restored.ActiveUnitSystemName);
        Assert.Equal(configuration.PlantElevation.Value, restored.PlantElevation.Value, 8);
        Assert.Equal(configuration.PlantElevation.Unit.Name, restored.PlantElevation.Unit.Name);
        Assert.Equal(configuration.CameraDefaults.DefaultZoom, restored.CameraDefaults.DefaultZoom, 8);
        Assert.Equal(configuration.NamingConfig.Mode, restored.NamingConfig.Mode);
        Assert.Equal(configuration.NamingConfig.CounterScope, restored.NamingConfig.CounterScope);
        Assert.Equal(configuration.NamingConfig.StartingNumber, restored.NamingConfig.StartingNumber);
    }

    [Fact]
    [Trait("Spec", "03")]
    [Trait("Spec", "09")]
    [Trait("Level", "Unit")]
    public void FromDto_WhenPlantElevationUnitIsUnknown_ShouldUseMeterFallback()
    {
        var dto = new ProjectBasicConfigurationDto
        {
            PlantElevationValue = 42,
            PlantElevationUnit = "unknown-length-unit"
        };

        var restored = ProjectConfigurationPersistenceMapper.FromDto(dto);

        Assert.Equal(42, restored.PlantElevation.Value, 8);
        Assert.Equal(LengthUnits.Meter.Name, restored.PlantElevation.Unit.Name);
    }
}
