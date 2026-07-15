using Distillator.Domain.Configuration;
using Distillator.Domain.Models;
using Shared.SolverConsecutive;
using Shared.UnitOperations.Basiss;
using System.Collections;
using System.Reflection;
using UnitSystem;

namespace Distillator.Domain.Services;

public static class ProjectUnitSystemApplier
{
    private static readonly Dictionary<Type, Func<IUnitConfiguration, UnitMeasure>> UnitByAmountType = new()
    {
        [typeof(Length)] = units => units.DefaultLengthUnit,
        [typeof(Diameter)] = units => units.DefaultDiameterUnit,
        [typeof(Area)] = units => units.DefaultSurfaceUnit,
        [typeof(Volume)] = units => units.DefaultVolumeUnit,
        [typeof(Time)] = units => units.DefaultTimeUnit,
        [typeof(Velocity)] = units => units.DefaultVelocityUnit,
        [typeof(Mass)] = units => units.DefaultMassUnit,
        [typeof(Force)] = units => units.DefaultForceUnit,
        [typeof(Electric)] = units => units.DefaultElectricUnit,
        [typeof(Power)] = units => units.DefaultPowerUnit,
        [typeof(Energy)] = units => units.DefaultEnergyUnit,
        [typeof(Temperature)] = units => units.DefaultTemperatureUnit,
        [typeof(Pressure)] = units => units.DefaultPressureUnit,
        [typeof(MotorVelocity)] = units => units.DefaultMotorVelocityUnit,
        [typeof(AmountOfSubstance)] = units => units.DefaultAmountOfSubstanceUnit,
        [typeof(HeatTransferCoefficient)] = units => units.DefaultHeatTransferCoefficientUnit,
        [typeof(MassDensity)] = units => units.DefaultDensityUnit,
        [typeof(MolarDensity)] = units => units.DefaultMolarDensityUnit,
        [typeof(MassVolumeSpecific)] = units => units.DefaultMassVolumeSpecificUnit,
        [typeof(MolarVolumeSpecific)] = units => units.DefaultMolarVolumeSpecificUnit,
        [typeof(PressureDropLength)] = units => units.DefaultPressureDropLengthUnit,
        [typeof(PressureDrop)] = units => units.DefaultPressureDropUnit,
        [typeof(ThermalConductivity)] = units => units.DefaultThermalConductivityUnit,
        [typeof(VolumeEnergy)] = units => units.DefaultVolumeEnergyUnit,
        [typeof(MassEnergy)] = units => units.DefaultMassEnergyUnit,
        [typeof(MolarEnergy)] = units => units.DefaultMolarEnergyUnit,
        [typeof(MassEntropy)] = units => units.DefaultMassEntropyUnit,
        [typeof(MolarEntropy)] = units => units.DefaultMolarEntropyUnit,
        [typeof(MassFlow)] = units => units.DefaultMassFlowUnit,
        [typeof(MolarFlow)] = units => units.DefaultMolarFlowUnit,
        [typeof(HeatSurfaceFlow)] = units => units.DefaultHeatSurfaceFlowUnit,
        [typeof(VolumetricFlow)] = units => units.DefaultVolumetricFlowUnit,
        [typeof(EnergyFlow)] = units => units.DefaultEnergyFlowUnit,
        [typeof(Viscosity)] = units => units.DefaultViscosityUnit,
        [typeof(SuperficialTension)] = units => units.DefaultSuperficialTensionUnit
    };

    public static void ApplyToFacade(IFacade? facade, IUnitConfiguration? units)
    {
        if (facade == null || units == null) return;
        ApplyToObjectGraph(facade, units, new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    public static void ApplyToProject(IProject project)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        var units = project.Configuration.UnitDefaults;
        foreach (var equipment in project.EquipmentRegistry.AllEquipments)
        {
            ApplyToFacade(equipment.Facade, units);
        }
    }

    private static void ApplyToObjectGraph(object? value, IUnitConfiguration units, HashSet<object> visited)
    {
        if (value == null) return;
        if (value is string or Amount or UnitMeasure) return;

        var valueType = value.GetType();
        if (valueType.IsValueType) return;
        if (!visited.Add(value)) return;

        if (value is IVariable variable)
        {
            ApplyToVariable(variable, units);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                ApplyToObjectGraph(item, units, visited);
            }

            return;
        }

        if (!ShouldInspectType(valueType)) return;

        foreach (var property in valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;

            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            ApplyToObjectGraph(propertyValue, units, visited);
        }
    }

    private static void ApplyToVariable(IVariable variable, IUnitConfiguration units)
    {
        var amountType = variable
            .GetType()
            .GetInterfaces()
            .Append(variable.GetType())
            .Where(type => type.IsGenericType)
            .FirstOrDefault(type => type.GetGenericTypeDefinition() == typeof(IVariable<>))
            ?.GetGenericArguments()[0];

        if (amountType == null) return;
        if (!UnitByAmountType.TryGetValue(amountType, out var resolveUnit)) return;

        variable.SetProjectDefaultDisplayUnit(resolveUnit(units));
    }

    private static bool ShouldInspectType(Type type)
    {
        var namespaceName = type.Namespace ?? string.Empty;

        return namespaceName.StartsWith("Shared.SolverConsecutive", StringComparison.Ordinal)
            || namespaceName.StartsWith("Shared.SolverQwen.Stream", StringComparison.Ordinal)
            || namespaceName.StartsWith("Shared.UnitOperations", StringComparison.Ordinal);
    }
}
