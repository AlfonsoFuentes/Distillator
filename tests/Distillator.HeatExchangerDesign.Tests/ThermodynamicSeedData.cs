using System.Globalization;
using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Distillator.HeatExchangerDesign.Tests;

internal static class ThermodynamicSeedData
{
    public static ThermodynamicMethodFullDto LoadMethod(string methodName)
    {
        var root = FindRepositoryRoot();
        var components = LoadComponents(Path.Combine(root, "Server", "SeedData", "MasterComponents.csv"));

        return LoadMethods(Path.Combine(root, "Server", "SeedData", "MasterThermodynamicMethods.csv"), components)
            .Single(method => method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Server", "SeedData", "MasterComponents.csv")) &&
                File.Exists(Path.Combine(current.FullName, "Server", "SeedData", "MasterThermodynamicMethods.csv")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Distillator repository root.");
    }

    private static Dictionary<string, ChemicalComponentDto> LoadComponents(string path)
    {
        return File.ReadAllLines(path)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseComponent)
            .ToDictionary(component => component.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static List<ThermodynamicMethodFullDto> LoadMethods(
        string path,
        IReadOnlyDictionary<string, ChemicalComponentDto> components)
    {
        var methods = new Dictionary<string, ThermodynamicMethodFullDto>(StringComparer.OrdinalIgnoreCase);
        var componentIndexes = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadLines(path).Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            var fields = line.Split(';');
            var methodName = fields[0];
            var method = GetOrCreateMethod(methods, methodName, fields[1], fields[2], fields[3]);
            var indexes = GetOrCreateIndexes(componentIndexes, methodName);

            var componentI = fields[4];
            var componentJ = fields[5];
            var parameterType = fields[6];
            var value = fields[7];

            if (!string.IsNullOrWhiteSpace(componentI) && string.IsNullOrWhiteSpace(componentJ))
            {
                if (!indexes.ContainsKey(componentI))
                {
                    indexes[componentI] = indexes.Count;
                    method.Components.Add(new MethodComponentFullDto
                    {
                        ComponentId = components[componentI].Id,
                        ComponentName = componentI,
                        MatrixIndex = indexes[componentI],
                        FullData = components[componentI]
                    });
                }

                continue;
            }

            if (!string.IsNullOrWhiteSpace(componentI) &&
                !string.IsNullOrWhiteSpace(componentJ) &&
                !string.IsNullOrWhiteSpace(parameterType) &&
                double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parameterValue))
            {
                method.BinaryParameters.Add(new BinaryInteractionParameterDto
                {
                    ComponentI_Id = components[componentI].Id,
                    ComponentI_Name = componentI,
                    ComponentJ_Id = components[componentJ].Id,
                    ComponentJ_Name = componentJ,
                    ParameterType = Enum.Parse<BinaryParameterType>(parameterType),
                    Value = parameterValue
                });
            }
        }

        return methods.Values.ToList();
    }

    private static ThermodynamicMethodFullDto GetOrCreateMethod(
        Dictionary<string, ThermodynamicMethodFullDto> methods,
        string name,
        string description,
        string vaporModel,
        string liquidModel)
    {
        if (methods.TryGetValue(name, out var method))
        {
            return method;
        }

        method = new ThermodynamicMethodFullDto
        {
            Id = StableGuid(name),
            Name = name,
            VaporModel = Enum.Parse<VaporPhaseModel>(vaporModel),
            LiquidModel = Enum.Parse<LiquidPhaseModel>(liquidModel)
        };
        methods[name] = method;
        return method;
    }

    private static Dictionary<string, int> GetOrCreateIndexes(
        Dictionary<string, Dictionary<string, int>> indexes,
        string methodName)
    {
        if (!indexes.TryGetValue(methodName, out var methodIndexes))
        {
            methodIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            indexes[methodName] = methodIndexes;
        }

        return methodIndexes;
    }

    private static ChemicalComponentDto ParseComponent(string line)
    {
        var fields = line.Split(';');
        var index = 0;
        var name = fields[index++];
        var formula = fields[index++];
        var structuralFormula = fields[index++];
        var family = fields[index++];
        var secondaryFamily = fields[index++];
        var molecularWeight = ReadDouble(fields[index++]);
        var criticalTemperature = ReadDouble(fields[index++]);
        var criticalPressure = ReadDouble(fields[index++]);
        var boilingPoint = ReadDouble(fields[index++]);
        var meltingPoint = ReadDouble(fields[index++]);
        var criticalVolume = ReadDouble(fields[index++]);
        var volumeAsterisk = ReadDouble(fields[index++]);
        var criticalZ = ReadDouble(fields[index++]);
        var acentric = ReadDouble(fields[index++]);
        var acentricPitzer = ReadDouble(fields[index++]);
        var enthalpyForm = ReadDouble(fields[index++]);
        var gibbsForm = ReadDouble(fields[index++]);
        var entropyForm = ReadDouble(fields[index++]);
        var combustionEnthalpy = ReadDouble(fields[index++]);

        var dto = new ChemicalComponentDto
        {
            Id = StableGuid(name),
            Name = name,
            Formula = formula,
            StructuralFormula = structuralFormula,
            Family = family,
            SecondaryFamily = secondaryFamily,
            MolecularWeight = molecularWeight,
            CriticalTemperature = new Temperature(criticalTemperature, TemperatureUnits.Kelvin),
            CriticalPressure = new Pressure(criticalPressure, PressureUnits.KiloPascala),
            BoilingPoint = new Temperature(boilingPoint, TemperatureUnits.Kelvin),
            MeltingPoint = new Temperature(meltingPoint, TemperatureUnits.Kelvin),
            CriticalVolume = new MolarVolumeSpecific(criticalVolume, MolarVolumeSpecificUnits.m3_Kgmol),
            VolumeAsterisk = new MolarVolumeSpecific(volumeAsterisk, MolarVolumeSpecificUnits.m3_Kgmol),
            CriticalZ = criticalZ,
            AcentricFactor = acentric,
            AcentricFactorPitzer = acentricPitzer,
            EnthalpyForm = new MolarEnergy(enthalpyForm, MolarEnergyUnits.J_Kgmol),
            GibbsForm = new MolarEnergy(gibbsForm, MolarEnergyUnits.J_Kgmol),
            EntropyForm = new MolarEntropy(entropyForm, MolarEntropyUnits.KJ_Kgmol_C),
            CombustionEnthalpy = new MolarEnergy(combustionEnthalpy, MolarEnergyUnits.J_Kgmol)
        };

        ApplyDefaultEquationTypes(dto);
        dto.VaporPressureEquationType = ReadEquationType(fields, ref index, dto.VaporPressureEquationType);
        dto.VaporPressure = ReadCorrelation(fields, ref index);
        dto.HeatOfVaporizationEquationType = ReadEquationType(fields, ref index, dto.HeatOfVaporizationEquationType);
        dto.HeatOfVaporization = ReadCorrelation(fields, ref index);
        dto.LiquidHeatCapacityEquationType = ReadEquationType(fields, ref index, dto.LiquidHeatCapacityEquationType);
        dto.LiquidHeatCapacity = ReadCorrelation(fields, ref index);
        dto.GasHeatCapacityEquationType = ReadEquationType(fields, ref index, dto.GasHeatCapacityEquationType);
        dto.GasHeatCapacity = ReadCorrelation(fields, ref index);
        dto.LiquidViscosityEquationType = ReadEquationType(fields, ref index, dto.LiquidViscosityEquationType);
        dto.LiquidViscosity = ReadCorrelation(fields, ref index);
        dto.GasViscosityEquationType = ReadEquationType(fields, ref index, dto.GasViscosityEquationType);
        dto.GasViscosity = ReadCorrelation(fields, ref index);
        dto.LiquidThermalConductivityEquationType = ReadEquationType(fields, ref index, dto.LiquidThermalConductivityEquationType);
        dto.LiquidThermalCond = ReadCorrelation(fields, ref index);
        dto.GasThermalConductivityEquationType = ReadEquationType(fields, ref index, dto.GasThermalConductivityEquationType);
        dto.GasThermalCond = ReadCorrelation(fields, ref index);
        dto.LiquidDensityEquationType = ReadEquationType(fields, ref index, dto.LiquidDensityEquationType);
        dto.Density = ReadCorrelation(fields, ref index);
        dto.SurfaceTensionEquationType = ReadEquationType(fields, ref index, dto.SurfaceTensionEquationType);
        dto.SurfaceTension = ReadCorrelation(fields, ref index);

        return dto;
    }

    private static TEnum ReadEquationType<TEnum>(string[] fields, ref int index, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (index < fields.Length &&
            Enum.TryParse<TEnum>(fields[index], ignoreCase: true, out var value) &&
            Enum.IsDefined(value))
        {
            index++;
            return value;
        }

        return defaultValue;
    }

    private static void ApplyDefaultEquationTypes(ChemicalComponentDto component)
    {
        if (component.Name.Equals("Agua", StringComparison.OrdinalIgnoreCase) ||
            component.Name.Equals("Water", StringComparison.OrdinalIgnoreCase))
        {
            component.VaporPressureEquationType = VaporPressureEquationType.IapwsSteamTables;
            component.SaturationTemperatureEquationType = SaturationTemperatureEquationType.IapwsSteamTables;
            component.HeatOfVaporizationEquationType = HeatOfVaporizationEquationType.IapwsSteamTables;
            component.LiquidHeatCapacityEquationType = LiquidHeatCapacityEquationType.IapwsSteamTables;
            component.GasHeatCapacityEquationType = GasHeatCapacityEquationType.IapwsSteamTables;
            component.LiquidViscosityEquationType = LiquidViscosityEquationType.IapwsSteamTables;
            component.GasViscosityEquationType = GasViscosityEquationType.IapwsSteamTables;
            component.LiquidThermalConductivityEquationType = LiquidThermalConductivityEquationType.IapwsSteamTables;
            component.GasThermalConductivityEquationType = GasThermalConductivityEquationType.IapwsSteamTables;
            component.LiquidDensityEquationType = LiquidDensityEquationType.IapwsSteamTables;
            component.SurfaceTensionEquationType = SurfaceTensionEquationType.IapwsSteamTables;
            component.LiquidEnthalpyEquationType = LiquidEnthalpyEquationType.IapwsSteamTables;
            component.GasEnthalpyEquationType = GasEnthalpyEquationType.IapwsSteamTables;
            component.SaturatedMolarVolumeEquationType = SaturatedMolarVolumeEquationType.IapwsSteamTables;
            return;
        }

        component.VaporPressureEquationType = VaporPressureEquationType.ExtendedAntoine;
        component.SaturationTemperatureEquationType = SaturationTemperatureEquationType.FromVaporPressureSecant;
        component.HeatOfVaporizationEquationType = HeatOfVaporizationEquationType.Dippr106;
        component.LiquidHeatCapacityEquationType = LiquidHeatCapacityEquationType.Polynomial;
        component.GasHeatCapacityEquationType = GasHeatCapacityEquationType.AlyLee;
        component.LiquidViscosityEquationType = LiquidViscosityEquationType.Dippr101;
        component.GasViscosityEquationType = GasViscosityEquationType.Dippr102;
        component.LiquidThermalConductivityEquationType = LiquidThermalConductivityEquationType.Polynomial4;
        component.GasThermalConductivityEquationType = GasThermalConductivityEquationType.PolynomialRational;
        component.LiquidDensityEquationType = LiquidDensityEquationType.Rackett;
        component.SurfaceTensionEquationType = SurfaceTensionEquationType.Dippr106;
        component.LiquidEnthalpyEquationType = LiquidEnthalpyEquationType.IntegratedLiquidCp;
        component.GasEnthalpyEquationType = GasEnthalpyEquationType.IntegratedGasCpWithHvap;
        component.SaturatedMolarVolumeEquationType = SaturatedMolarVolumeEquationType.Rackett;
    }

    private static CorrelationCoefficientsDto ReadCorrelation(string[] fields, ref int index)
    {
        return new CorrelationCoefficientsDto
        {
            C1 = ReadDouble(fields[index++]),
            C2 = ReadDouble(fields[index++]),
            C3 = ReadDouble(fields[index++]),
            C4 = ReadDouble(fields[index++]),
            C5 = ReadDouble(fields[index++]),
            C6 = ReadDouble(fields[index++]),
            C7 = ReadDouble(fields[index++]),
            Tmin = new Temperature(ReadDouble(fields[index++]), TemperatureUnits.Kelvin),
            Tmax = new Temperature(ReadDouble(fields[index++]), TemperatureUnits.Kelvin)
        };
    }

    private static double ReadDouble(string value) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0d;

    private static Guid StableGuid(string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }
}
