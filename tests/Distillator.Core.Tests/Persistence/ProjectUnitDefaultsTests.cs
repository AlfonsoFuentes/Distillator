using Distillator.Domain.Configuration;
using Distillator.Domain.Models;
using Distillator.Domain.Services;
using Shared.ProcessFlowDiagram;
using Shared.ProcessFlowDiagram.Streams;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Distillator.Core.Tests.Persistence;

public sealed class ProjectUnitDefaultsTests
{
    public ProjectUnitDefaultsTests()
    {
        UnitManager.RegisterByAssembly(typeof(SIUnitTypes).Assembly);
    }

    [Fact]
    [Trait("Spec", "09")]
    [Trait("Level", "Unit")]
    public void ApplyToProject_WhenDefaultChanges_ShouldUpdateVariablesWithoutOverrideAndKeepPhysicalValue()
    {
        var stream = new FacadeStream("S-101");
        var project = CreateProjectWithStream(stream);
        var originalPressurePascala = stream.Pressure.Value.GetValue(PressureUnits.Pascala);

        project.UpdateConfiguration(CreateConfigurationWithActiveUnits(
            CreateUnits(defaultPressureUnit: PressureUnits.Psia)));

        Assert.Equal(PressureUnits.Psia.Name, stream.Pressure.DisplayUnit.Name);
        Assert.False(stream.Pressure.HasDisplayUnitOverride);
        Assert.Equal(originalPressurePascala, stream.Pressure.Value.GetValue(PressureUnits.Pascala), 6);
    }

    [Fact]
    [Trait("Spec", "09")]
    [Trait("Level", "Unit")]
    public void ApplyToProject_WhenVariableHasOverride_ShouldKeepOverrideAfterDefaultChanges()
    {
        var stream = new FacadeStream("S-101");
        var project = CreateProjectWithStream(stream);
        stream.Pressure.SetDisplayUnit(PressureUnits.Barg);

        project.UpdateConfiguration(CreateConfigurationWithActiveUnits(
            CreateUnits(defaultPressureUnit: PressureUnits.Psia)));

        Assert.Equal(PressureUnits.Barg.Name, stream.Pressure.DisplayUnit.Name);
        Assert.True(stream.Pressure.HasDisplayUnitOverride);
    }

    [Fact]
    [Trait("Spec", "09")]
    [Trait("Level", "Unit")]
    public void FacadeStateSerializer_WhenReloading_ShouldRestoreOverridesAndProjectDefaults()
    {
        var source = new FacadeStream("S-101");
        source.Pressure.SetDisplayUnit(PressureUnits.Barg);
        source.Temperature.SetProjectDefaultDisplayUnit(TemperatureUnits.DegreeFahrenheit);
        var snapshot = FacadeStateSerializer.Serialize(source, includeTransientState: true);

        var target = new FacadeStream("S-101");
        ProjectUnitSystemApplier.ApplyToFacade(
            target,
            CreateUnits(
                defaultPressureUnit: PressureUnits.Psia,
                defaultTemperatureUnit: TemperatureUnits.Kelvin));

        FacadeStateSerializer.Apply(target, snapshot, restoreProjectDefaultDisplayUnits: true);

        Assert.Equal(PressureUnits.Barg.Name, target.Pressure.DisplayUnit.Name);
        Assert.True(target.Pressure.HasDisplayUnitOverride);
        Assert.Equal(TemperatureUnits.DegreeFahrenheit.Name, target.Temperature.DisplayUnit.Name);
        Assert.False(target.Temperature.HasDisplayUnitOverride);
    }

    private static Project CreateProjectWithStream(FacadeStream stream)
    {
        var owner = new User(Guid.Parse("af54e08e-c21d-4dfb-bca9-3e1dd8e923d2"), "owner@example.com", "Owner", "User", false);
        var project = new Project("Unit defaults test", owner);
        var flowsheet = project.CreateFlowsheet("Main", "PFD");
        var element = new StreamVisualElement
        {
            Id = stream.Id,
            Name = stream.Name,
            Facade = stream
        };

        flowsheet.AddElementReference(new FlowsheetElementReference(element.Id, element.X, element.Y));
        project.AddEquipment(element);
        return project;
    }

    private static IUnitConfiguration CreateUnits(
        UnitMeasure? defaultPressureUnit = null,
        UnitMeasure? defaultTemperatureUnit = null)
    {
        var units = UnitConfiguration.Clone(UnitConfiguration.SI());
        units.DefaultPressureUnit = defaultPressureUnit ?? units.DefaultPressureUnit;
        units.DefaultTemperatureUnit = defaultTemperatureUnit ?? units.DefaultTemperatureUnit;
        return units;
    }

    private static ProjectConfiguration CreateConfigurationWithActiveUnits(IUnitConfiguration units)
    {
        const string activeName = "M52 Custom";
        return new ProjectConfiguration(
            unitSystems: [new ProjectUnitSystem(activeName, units)],
            activeUnitSystemName: activeName);
    }
}
