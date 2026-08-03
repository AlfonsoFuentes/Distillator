using System.Globalization;
using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Streams;
using UnitSystem;
using Xunit.Abstractions;

namespace Distillator.Thermodynamics.Tests;

public sealed class ThermodynamicMethodMatrixTests
{
    private static readonly double[] PressuresBara = [1.0, 2.0, 5.0];
    private static readonly double[] BinaryLightMolePercents = [0.0, 25.0, 50.0, 75.0, 100.0];
    private readonly ITestOutputHelper _output;

    public ThermodynamicMethodMatrixTests(ITestOutputHelper output)
    {
        _output = output;
        UnitManager.RegisterByAssembly(typeof(SIUnitTypes).Assembly);
    }

    [Fact]
    [Trait("Spec", "Thermodynamics")]
    [Trait("Level", "Regression")]
    public void WaterSteamTables_StateMatrix_ShouldMatchReferenceRangesAndPhysicalConsistency()
    {
        var method = ThermodynamicSeedData.LoadMethod("Water (Steam Tables)");
        var failures = new List<string>();

        WriteHeader();

        foreach (var pressureBara in PressuresBara)
        {
            var saturatedLiquid = SolveByPressureVaporFraction(method, pressureBara, 0.0, 100.0);
            WriteResult("Water (Steam Tables)", "SaturatedLiquid", pressureBara, null, 100.0, saturatedLiquid);
            Check(failures, $"Water SaturatedLiquid P={pressureBara}", () =>
            {
                AssertStreamSolved(saturatedLiquid, ThermodynamicState.SaturatedLiquid);
                AssertTransportAndBulkPropertiesArePhysical(saturatedLiquid);
            });

            var twoPhase = SolveByPressureVaporFraction(method, pressureBara, 50.0, 100.0);
            WriteResult("Water (Steam Tables)", "TwoPhase50", pressureBara, null, 100.0, twoPhase);
            Check(failures, $"Water TwoPhase50 P={pressureBara}", () =>
            {
                AssertStreamSolved(twoPhase, ThermodynamicState.VaporLiquidMixture);
                AssertTransportAndBulkPropertiesArePhysical(twoPhase);
            });

            var saturatedVapor = SolveByPressureVaporFraction(method, pressureBara, 100.0, 100.0);
            WriteResult("Water (Steam Tables)", "SaturatedVapor", pressureBara, null, 100.0, saturatedVapor);
            Check(failures, $"Water SaturatedVapor P={pressureBara}", () =>
            {
                AssertStreamSolved(saturatedVapor, ThermodynamicState.SaturatedVapor);
                AssertTransportAndBulkPropertiesArePhysical(saturatedVapor);
            });

            var subcooled = SolveByPressureTemperature(
                method,
                pressureBara,
                saturatedLiquid.Temperature.Value.GetValue(TemperatureUnits.Kelvin) - 20.0,
                100.0);
            WriteResult("Water (Steam Tables)", "SubcooledLiquid", pressureBara, null, 100.0, subcooled);
            Check(failures, $"Water SubcooledLiquid P={pressureBara}", () =>
            {
                AssertStreamSolved(subcooled, ThermodynamicState.SubcooledLiquid);
                AssertTransportAndBulkPropertiesArePhysical(subcooled);
            });

            var superheated = SolveByPressureTemperature(
                method,
                pressureBara,
                saturatedVapor.Temperature.Value.GetValue(TemperatureUnits.Kelvin) + 40.0,
                100.0);
            WriteResult("Water (Steam Tables)", "SuperheatedVapor", pressureBara, null, 100.0, superheated);
            Check(failures, $"Water SuperheatedVapor P={pressureBara}", () =>
            {
                AssertStreamSolved(superheated, ThermodynamicState.SuperheatedVapor);
                AssertTransportAndBulkPropertiesArePhysical(superheated);
            });
        }

        var reference = SolveByPressureTemperature(method, 1.0, 298.15, 100.0);
        Check(failures, "Water reference 25 C / 1 bar", () =>
        {
            Assert.InRange(reference.MassDensity.Value.GetValue(MassDensityUnits.Kg_m3), 995.0, 999.0);
            Assert.InRange(reference.MassCp.Value.GetValue(MassEntropyUnits.KJ_Kg_C), 4.15, 4.22);
            Assert.InRange(reference.Viscosity.Value.GetValue(ViscosityUnits.cPoise), 0.85, 0.95);
            Assert.InRange(reference.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.W_m_K), 0.58, 0.63);
            Assert.InRange(reference.SuperficialTension.Value.GetValue(SuperficialTensionUnits.dyn_cm), 70.0, 75.0);
        });

        AssertNoFailures(failures);
    }

    [Theory]
    [Trait("Spec", "Thermodynamics")]
    [Trait("Level", "Regression")]
    [InlineData("Ethanol-Water (NRTL)", "Ethanol", "Water")]
    [InlineData("Methanol-Water (Wilson)", "Methanol", "Water")]
    [InlineData("Methanol-Glycerol (Wilson)", "Methanol", "Glycerol")]
    public void BinaryMethods_StateCompositionMatrix_ShouldBeThermodynamicallyConsistent(
        string methodName,
        string lightComponent,
        string heavyComponent)
    {
        var method = ThermodynamicSeedData.LoadMethod(methodName);
        var failures = new List<string>();

        WriteHeader();

        foreach (var pressureBara in PressuresBara)
        foreach (var lightMolePercent in BinaryLightMolePercents)
        {
            var saturatedLiquid = SolveByPressureVaporFraction(
                method,
                pressureBara,
                0.0,
                lightMolePercent,
                lightComponent,
                heavyComponent);
            WriteResult(methodName, "SaturatedLiquid", pressureBara, lightMolePercent, null, saturatedLiquid);
            Check(failures, $"{methodName} SaturatedLiquid P={pressureBara} light={lightMolePercent}", () =>
            {
                AssertStreamSolved(saturatedLiquid, ThermodynamicState.SaturatedLiquid);
                AssertTransportAndBulkPropertiesArePhysical(saturatedLiquid);
                AssertPhaseFractionsAreNormalized(saturatedLiquid);
            });

            var twoPhase = SolveByPressureVaporFraction(
                method,
                pressureBara,
                50.0,
                lightMolePercent,
                lightComponent,
                heavyComponent);
            WriteResult(methodName, "TwoPhase50", pressureBara, lightMolePercent, null, twoPhase);
            Check(failures, $"{methodName} TwoPhase50 P={pressureBara} light={lightMolePercent}", () =>
            {
                AssertStreamSolved(twoPhase, ThermodynamicState.VaporLiquidMixture);
                AssertTransportAndBulkPropertiesArePhysical(twoPhase);
                AssertPhaseFractionsAreNormalized(twoPhase);
                AssertLightComponentEnrichesVapor(twoPhase, lightComponent, lightMolePercent);
            });

            var saturatedVapor = SolveByPressureVaporFraction(
                method,
                pressureBara,
                100.0,
                lightMolePercent,
                lightComponent,
                heavyComponent);
            WriteResult(methodName, "SaturatedVapor", pressureBara, lightMolePercent, null, saturatedVapor);
            Check(failures, $"{methodName} SaturatedVapor P={pressureBara} light={lightMolePercent}", () =>
            {
                AssertStreamSolved(saturatedVapor, ThermodynamicState.SaturatedVapor);
                AssertTransportAndBulkPropertiesArePhysical(saturatedVapor);
                AssertPhaseFractionsAreNormalized(saturatedVapor);
            });

            var subcooled = SolveByPressureTemperature(
                method,
                pressureBara,
                saturatedLiquid.Temperature.Value.GetValue(TemperatureUnits.Kelvin) - 15.0,
                lightMolePercent,
                lightComponent,
                heavyComponent);
            WriteResult(methodName, "SubcooledLiquid", pressureBara, lightMolePercent, null, subcooled);
            Check(failures, $"{methodName} SubcooledLiquid P={pressureBara} light={lightMolePercent}", () =>
            {
                AssertStreamSolved(subcooled, ThermodynamicState.SubcooledLiquid);
                AssertTransportAndBulkPropertiesArePhysical(subcooled);
            });

            var superheated = SolveByPressureTemperature(
                method,
                pressureBara,
                saturatedVapor.Temperature.Value.GetValue(TemperatureUnits.Kelvin) + 25.0,
                lightMolePercent,
                lightComponent,
                heavyComponent);
            WriteResult(methodName, "SuperheatedVapor", pressureBara, lightMolePercent, null, superheated);
            Check(failures, $"{methodName} SuperheatedVapor P={pressureBara} light={lightMolePercent}", () =>
            {
                AssertStreamSolved(superheated, ThermodynamicState.SuperheatedVapor);
                AssertTransportAndBulkPropertiesArePhysical(superheated);
            });
        }

        AssertNoFailures(failures);
    }

    [Fact]
    [Trait("Spec", "Thermodynamics")]
    [Trait("Level", "Regression")]
    public void MethanolGlycerol_TwoPhase50MassBasis_ShouldProduceConsistentPhaseSplitAndTransport()
    {
        var method = ThermodynamicSeedData.LoadMethod("Methanol-Glycerol (Wilson)");
        var stream = SolveByPressureVaporFractionMassBasis(
            method,
            pressureBara: 1.0,
            vaporFractionPercent: 50.0,
            firstComponent: "Glycerol",
            firstMassPercent: 50.0,
            secondComponent: "Methanol",
            secondMassPercent: 50.0);

        WriteResult("Methanol-Glycerol (Wilson)", "TwoPhase50MassBasis", 1.0, null, null, stream);

        AssertStreamSolved(stream, ThermodynamicState.VaporLiquidMixture);
        AssertTransportAndBulkPropertiesArePhysical(stream);
        AssertPhaseFractionsAreNormalized(stream);

        Assert.InRange(stream.VaporFraction.Value.GetValue(PercentageUnits.Percentage), 49.9, 50.1);
        Assert.InRange(stream.Temperature.Value.GetValue(TemperatureUnits.DegreeCelcius), 48.0, 49.5);

        var liquidGlycerol = stream.LiquidPhase.Components
            .Single(component => component.Name.Equals("Glycerol", StringComparison.OrdinalIgnoreCase));
        var vaporGlycerol = stream.VaporPhase.Components
            .Single(component => component.Name.Equals("Glycerol", StringComparison.OrdinalIgnoreCase));
        var liquidMethanol = stream.LiquidPhase.Components
            .Single(component => component.Name.Equals("Methanol", StringComparison.OrdinalIgnoreCase));
        var vaporMethanol = stream.VaporPhase.Components
            .Single(component => component.Name.Equals("Methanol", StringComparison.OrdinalIgnoreCase));

        var kGlycerol = vaporGlycerol.MolarFraction / liquidGlycerol.MolarFraction;
        var kMethanol = vaporMethanol.MolarFraction / liquidMethanol.MolarFraction;

        Assert.True(vaporMethanol.MolarFraction > liquidMethanol.MolarFraction);
        Assert.True(liquidGlycerol.MolarFraction > vaporGlycerol.MolarFraction);
        Assert.True(kMethanol > kGlycerol, $"Expected methanol K to exceed glycerol K. KMeOH={kMethanol:G6}, KGly={kGlycerol:G6}.");

        Assert.InRange(stream.MassDensity.Value.GetValue(MassDensityUnits.Kg_m3), 3.0, 4.5);
        Assert.InRange(stream.Viscosity.Value.GetValue(ViscosityUnits.cPoise), 0.02, 0.05);
        Assert.InRange(stream.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.W_m_K), 0.0005, 0.002);
    }

    private static IFacadeStream SolveByPressureVaporFraction(
        ThermodynamicMethodFullDto method,
        double pressureBara,
        double vaporFractionPercent,
        double pureComponentMolePercent)
    {
        var componentName = method.Components.Single().ComponentName;
        return SolveByPressureVaporFraction(
            method,
            pressureBara,
            vaporFractionPercent,
            pureComponentMolePercent,
            componentName,
            componentName);
    }

    private static IFacadeStream SolveByPressureVaporFraction(
        ThermodynamicMethodFullDto method,
        double pressureBara,
        double vaporFractionPercent,
        double lightMolePercent,
        string lightComponent,
        string heavyComponent)
    {
        var stream = CreateStream(method);
        stream.Pressure.SetValue(new Pressure(pressureBara, PressureUnits.Bara), VariableDefinedBy.UserInput);
        stream.VaporFraction.SetValue(new Percentage(vaporFractionPercent, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        SetMolarComposition(stream, lightComponent, heavyComponent, lightMolePercent);
        return stream;
    }

    private static IFacadeStream SolveByPressureTemperature(
        ThermodynamicMethodFullDto method,
        double pressureBara,
        double temperatureKelvin,
        double pureComponentMolePercent)
    {
        var componentName = method.Components.Single().ComponentName;
        return SolveByPressureTemperature(
            method,
            pressureBara,
            temperatureKelvin,
            pureComponentMolePercent,
            componentName,
            componentName);
    }

    private static IFacadeStream SolveByPressureTemperature(
        ThermodynamicMethodFullDto method,
        double pressureBara,
        double temperatureKelvin,
        double lightMolePercent,
        string lightComponent,
        string heavyComponent)
    {
        var stream = CreateStream(method);
        stream.Pressure.SetValue(new Pressure(pressureBara, PressureUnits.Bara), VariableDefinedBy.UserInput);
        stream.Temperature.SetValue(new Temperature(temperatureKelvin, TemperatureUnits.Kelvin), VariableDefinedBy.UserInput);
        SetMolarComposition(stream, lightComponent, heavyComponent, lightMolePercent);
        return stream;
    }

    private static IFacadeStream CreateStream(ThermodynamicMethodFullDto method)
    {
        var stream = new FacadeStream("S-TEST");
        stream.SetThermodynamicMethod(method);
        stream.MolarFlow.SetValue(new MolarFlow(100.0, MolarFlowUnits.Kgmol_hr), VariableDefinedBy.UserInput);
        return stream;
    }

    private static void SetMolarComposition(
        IFacadeStream stream,
        string lightComponent,
        string heavyComponent,
        double lightMolePercent)
    {
        foreach (var component in stream.Composition.Components)
        {
            var percent = component.Name.Equals(lightComponent, StringComparison.OrdinalIgnoreCase)
                ? lightMolePercent
                : component.Name.Equals(heavyComponent, StringComparison.OrdinalIgnoreCase)
                    ? 100.0 - lightMolePercent
                    : 0.0;

            component.MolarFraction.SetValue(new Percentage(percent, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        }

        stream.Composition.InputType = ComponentInputType.MolarFraction;
        stream.Composition.CompositionChanged();
    }

    private static IFacadeStream SolveByPressureVaporFractionMassBasis(
        ThermodynamicMethodFullDto method,
        double pressureBara,
        double vaporFractionPercent,
        string firstComponent,
        double firstMassPercent,
        string secondComponent,
        double secondMassPercent)
    {
        var stream = CreateStream(method);
        stream.Pressure.SetValue(new Pressure(pressureBara, PressureUnits.Bara), VariableDefinedBy.UserInput);
        stream.VaporFraction.SetValue(new Percentage(vaporFractionPercent, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        SetMassComposition(stream, firstComponent, firstMassPercent, secondComponent, secondMassPercent);
        return stream;
    }

    private static void SetMassComposition(
        IFacadeStream stream,
        string firstComponent,
        double firstMassPercent,
        string secondComponent,
        double secondMassPercent)
    {
        foreach (var component in stream.Composition.Components)
        {
            var percent = component.Name.Equals(firstComponent, StringComparison.OrdinalIgnoreCase)
                ? firstMassPercent
                : component.Name.Equals(secondComponent, StringComparison.OrdinalIgnoreCase)
                    ? secondMassPercent
                    : 0.0;

            component.MassFraction.SetValue(new Percentage(percent, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
        }

        stream.Composition.InputType = ComponentInputType.MassFraction;
        stream.Composition.CompositionChanged();
    }

    private static void AssertStreamSolved(IFacadeStream stream, ThermodynamicState expectedState)
    {
        Assert.True(stream.IsEquilibriumSolved, $"Equilibrium was not solved. Actual state: {stream.ThermodynamicState}");
        Assert.Equal(expectedState, stream.ThermodynamicState);
        Assert.True(stream.VaporFraction.IsDefined);
        Assert.True(stream.Temperature.IsDefined);
        Assert.True(stream.Pressure.IsDefined);
        Assert.True(double.IsFinite(stream.Temperature.Value.GetValue(TemperatureUnits.Kelvin)));
        Assert.True(double.IsFinite(stream.Pressure.Value.GetValue(PressureUnits.Bara)));
    }

    private static void AssertTransportAndBulkPropertiesArePhysical(IFacadeStream stream)
    {
        AssertPositive(stream.MolecularWeight.Value.Value, "Molecular weight");
        AssertPositive(stream.MassDensity.Value.GetValue(MassDensityUnits.Kg_m3), "Mass density");
        AssertPositive(stream.MolarDensity.Value.GetValue(MolarDensityUnits.Kgmol_m3), "Molar density");
        Assert.True(stream.MassEnthalpy.IsDefined, "Mass enthalpy must be defined.");
        Assert.True(double.IsFinite(stream.MassEnthalpy.Value.GetValue(MassEnergyUnits.KJ_Kg)));
        AssertPositive(stream.MassCp.Value.GetValue(MassEntropyUnits.KJ_Kg_C), "Mass Cp");
        AssertPositive(stream.Viscosity.Value.GetValue(ViscosityUnits.Pa_s), "Viscosity");
        AssertPositive(stream.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.W_m_K), "Thermal conductivity");
    }

    private static void AssertPhaseFractionsAreNormalized(IFacadeStream stream)
    {
        var liquidSum = stream.LiquidPhase.Components.Sum(component => component.MolarFraction);
        var vaporSum = stream.VaporPhase.Components.Sum(component => component.MolarFraction);

        Assert.InRange(liquidSum, 0.999, 1.001);
        Assert.InRange(vaporSum, 0.999, 1.001);

        foreach (var component in stream.LiquidPhase.Components)
        {
            Assert.InRange(component.MolarFraction, -1.0e-8, 1.0 + 1.0e-8);
            Assert.InRange(component.MassFraction, -1.0e-8, 1.0 + 1.0e-8);
        }

        foreach (var component in stream.VaporPhase.Components)
        {
            Assert.InRange(component.MolarFraction, -1.0e-8, 1.0 + 1.0e-8);
            Assert.InRange(component.MassFraction, -1.0e-8, 1.0 + 1.0e-8);
        }
    }

    private static void AssertLightComponentEnrichesVapor(
        IFacadeStream stream,
        string lightComponent,
        double lightMolePercent)
    {
        if (lightMolePercent <= 0.0 || lightMolePercent >= 100.0)
        {
            return;
        }

        var liquidLight = stream.LiquidPhase.Components
            .Single(component => component.Name.Equals(lightComponent, StringComparison.OrdinalIgnoreCase))
            .MolarFraction;
        var vaporLight = stream.VaporPhase.Components
            .Single(component => component.Name.Equals(lightComponent, StringComparison.OrdinalIgnoreCase))
            .MolarFraction;

        Assert.True(
            vaporLight > liquidLight,
            $"Expected {lightComponent} to enrich vapor. x={liquidLight:F6}, y={vaporLight:F6}.");
    }

    private static void AssertPositive(double value, string propertyName)
    {
        Assert.True(double.IsFinite(value), $"{propertyName} must be finite.");
        Assert.True(value > 0.0, $"{propertyName} must be positive. Actual: {value}");
    }

    private static void Check(List<string> failures, string caseId, Action assertion)
    {
        try
        {
            assertion();
        }
        catch (Exception ex)
        {
            failures.Add($"{caseId}: {ex.Message}");
        }
    }

    private static void AssertNoFailures(IReadOnlyCollection<string> failures)
    {
        Assert.True(
            failures.Count == 0,
            "Thermodynamic consistency failures:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    private void WriteHeader()
    {
        _output.WriteLine(
            "Method,Case,PressureBara,LightMolePercent,State,VaporFractionPercent,TemperatureC,MassDensityKgM3,MassEnthalpyKJkg,MassCpKJkgC,ViscosityCP,ThermalConductivityWmK,SurfaceTensionDynCm,LiquidLightMoleFraction,VaporLightMoleFraction");
    }

    private void WriteResult(
        string method,
        string caseName,
        double pressureBara,
        double? lightMolePercent,
        double? pureMolePercent,
        IFacadeStream stream)
    {
        var liquidLight = stream.LiquidPhase.Components.Count > 0
            ? stream.LiquidPhase.Components[0].MolarFraction
            : double.NaN;
        var vaporLight = stream.VaporPhase.Components.Count > 0
            ? stream.VaporPhase.Components[0].MolarFraction
            : double.NaN;
        var surfaceTension = stream.SuperficialTension.IsDefined
            ? stream.SuperficialTension.Value.GetValue(SuperficialTensionUnits.dyn_cm)
            : double.NaN;

        _output.WriteLine(string.Join(
            ",",
            method,
            caseName,
            Format(pressureBara),
            Format(lightMolePercent ?? pureMolePercent ?? double.NaN),
            stream.ThermodynamicState,
            Format(stream.VaporFraction.Value.GetValue(PercentageUnits.Percentage)),
            Format(stream.Temperature.Value.GetValue(TemperatureUnits.DegreeCelcius)),
            Format(stream.MassDensity.Value.GetValue(MassDensityUnits.Kg_m3)),
            Format(stream.MassEnthalpy.Value.GetValue(MassEnergyUnits.KJ_Kg)),
            Format(stream.MassCp.Value.GetValue(MassEntropyUnits.KJ_Kg_C)),
            Format(stream.Viscosity.Value.GetValue(ViscosityUnits.cPoise)),
            Format(stream.ThermalConductivity.Value.GetValue(ThermalConductivityUnits.W_m_K)),
            Format(surfaceTension),
            Format(liquidLight),
            Format(vaporLight)));
    }

    private static string Format(double value)
    {
        return double.IsFinite(value)
            ? value.ToString("G8", CultureInfo.InvariantCulture)
            : "";
    }

    private static class ThermodynamicSeedData
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
            var lines = File.ReadAllLines(path);
            return lines
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
            if (index < fields.Length && Enum.TryParse<TEnum>(fields[index], ignoreCase: true, out var value))
            {
                index++;
                return value;
            }

            return defaultValue;
        }

        private static void ApplyDefaultEquationTypes(ChemicalComponentDto component)
        {
            bool isWater = component.Name.Equals("Agua", StringComparison.OrdinalIgnoreCase) ||
                           component.Name.Equals("Water", StringComparison.OrdinalIgnoreCase);

            if (isWater)
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

        private static double ReadDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0.0;
        }

        private static Guid StableGuid(string value)
        {
            var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
            return new Guid(bytes);
        }
    }
}
