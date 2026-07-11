using Shared.PropertiesDtos.Components;
using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.SolverQwen;

public sealed class StreamMixerBalanceRegressionTest
{
    private const double Tolerance = 1e-2;

    public IReadOnlyList<string> RunAll()
    {
        var results = new List<string>
        {
            RunCase1_GlobalMassFlowsOnly(),
            RunCase2_InletCompositionsDefined(),
            RunCase3_OneInletCompositionUndefined(),
            RunCase4_OutletCompositionDefined()
        };

        return results;
    }

    public string RunCase1_GlobalMassFlowsOnly()
    {
        var caseData = CreateCase(setThermoMethod: true);

        SetMassFlow(caseData.Inlet1, 1000);
        SetMassFlow(caseData.Inlet2, 2000);

        RunSimulation(caseData.Solver);

        AssertClose("Case 1 outlet mass flow", 3000, GetMassFlow(caseData.Outlet));
        AssertUndefined("Case 1 outlet ethanol mass flow", caseData.Outlet.Composition?.Components.FirstOrDefault()?.MassFlow);

        return "Case 1 OK: solo flujos globales de entrada -> salida = 3000 kg/hr.";
    }

    public string RunCase2_InletCompositionsDefined()
    {
        var caseData = CreateCase(setThermoMethod: true);

        SetMassFlow(caseData.Inlet1, 1000);
        SetMassFlow(caseData.Inlet2, 2000);
        SetMassFractions(caseData.Inlet1, ethanolPercent: 8.00);
        SetMassFractions(caseData.Inlet2, ethanolPercent: 16.24);

        RunSimulation(caseData.Solver);

        AssertClose("Case 2 outlet mass flow", 3000, GetMassFlow(caseData.Outlet));
        AssertClose("Case 2 outlet ethanol component mass flow", 404.8, GetComponentMassFlow(caseData.Outlet, 0));
        AssertClose("Case 2 outlet ethanol mass fraction", 13.4933, GetMassFraction(caseData.Outlet, 0));

        return "Case 2 OK: composiciones de entradas definidas -> calcula salida y composicion mezclada.";
    }

    public string RunCase3_OneInletCompositionUndefined()
    {
        var caseData = CreateCase(setThermoMethod: true);

        SetMassFlow(caseData.Inlet1, 1000);
        SetMassFlow(caseData.Inlet2, 2000);
        SetMassFractions(caseData.Inlet2, ethanolPercent: 16.24);

        RunSimulation(caseData.Solver);

        AssertClose("Case 3 outlet mass flow", 3000, GetMassFlow(caseData.Outlet));
        AssertUndefined("Case 3 inlet1 ethanol fraction", caseData.Inlet1.Composition.Components[0].MassFraction);
        AssertUndefined("Case 3 outlet ethanol fraction", caseData.Outlet.Composition.Components[0].MassFraction);

        return "Case 3 OK: una composicion de entrada indefinida -> solo cierra masa global, no inventa composicion.";
    }

    public string RunCase4_OutletCompositionDefined()
    {
        var caseData = CreateCase(setThermoMethod: true);

        SetMassFlow(caseData.Inlet1, 1000);
        SetMassFlow(caseData.Inlet2, 2000);
        SetMassFractions(caseData.Inlet2, ethanolPercent: 16.24);
        SetMassFractions(caseData.Outlet, ethanolPercent: 13.4933);

        RunSimulation(caseData.Solver);

        AssertClose("Case 4 outlet mass flow", 3000, GetMassFlow(caseData.Outlet));
        AssertClose("Case 4 inlet1 ethanol component mass flow", 80.0, GetComponentMassFlow(caseData.Inlet1, 0), tolerance: 0.2);
        AssertClose("Case 4 inlet1 ethanol mass fraction", 8.0, GetMassFraction(caseData.Inlet1, 0), tolerance: 0.2);

        return "Case 4 OK: salida e inlet2 con composicion -> calcula composicion faltante de inlet1.";
    }

    private static MixerCase CreateCase(bool setThermoMethod)
    {
        var solver = new MainSolver();
        var mixer = new SolverStreamMixer("M-101");
        var inlet1 = new FacadeStream("S-101");
        var inlet2 = new FacadeStream("S-102");
        var outlet = new FacadeStream("S-103");

        if (setThermoMethod)
        {
            var method = CreateEthanolWaterMethod();
            inlet1.SetThermodynamicMethod(method);
            inlet2.SetThermodynamicMethod(method);
            outlet.SetThermodynamicMethod(method);
        }

        mixer.AddInlet(inlet1);
        mixer.AddInlet(inlet2);
        mixer.SetOutlet(outlet);

        solver.AddEquipment(mixer);
        solver.AddStream(inlet1);
        solver.AddStream(inlet2);
        solver.AddStream(outlet);

        return new MixerCase(solver, inlet1, inlet2, outlet);
    }

    private static ThermodynamicMethodFullDto CreateEthanolWaterMethod()
    {
        return new ThermodynamicMethodFullDto
        {
            Id = Guid.NewGuid(),
            Name = "Regression Ethanol Water",
            LiquidModel = LiquidPhaseModel.IdealLiquid,
            VaporModel = VaporPhaseModel.IdealGas,
            Components =
            [
                CreateComponent("Ethanol", "C2H6O", molecularWeight: 46.07, matrixIndex: 0),
                CreateComponent("Water", "H2O", molecularWeight: 18.015, matrixIndex: 1)
            ]
        };
    }

    private static MethodComponentFullDto CreateComponent(
        string name,
        string formula,
        double molecularWeight,
        int matrixIndex)
    {
        var id = Guid.NewGuid();
        return new MethodComponentFullDto
        {
            ComponentId = id,
            ComponentName = name,
            MatrixIndex = matrixIndex,
            FullData = new ChemicalComponentDto
            {
                Id = id,
                Name = name,
                Formula = formula,
                MolecularWeight = molecularWeight,
                CriticalTemperature = new Temperature(500, TemperatureUnits.Kelvin),
                CriticalPressure = new Pressure(50, PressureUnits.Bara),
                BoilingPoint = new Temperature(350, TemperatureUnits.Kelvin),
                MeltingPoint = new Temperature(250, TemperatureUnits.Kelvin)
            }
        };
    }

    private static void SetMassFlow(IFacadeStream stream, double kgPerHour)
    {
        stream.MassFlow.SetValue(new MassFlow(kgPerHour, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
    }

    private static void SetMassFractions(IFacadeStream stream, double ethanolPercent)
    {
        stream.Composition.Components[0].MassFraction.SetValue(
            new Percentage(ethanolPercent, PercentageUnits.Percentage),
            VariableDefinedBy.UserInput);

        stream.Composition.Components[1].MassFraction.SetValue(
            new Percentage(100 - ethanolPercent, PercentageUnits.Percentage),
            VariableDefinedBy.UserInput);

        stream.Composition.InputType = Shared.Thermodynamics.ControlledVariables.ComponentInputType.MassFraction;
        stream.Composition.CompositionChanged();
    }

    private static double GetMassFlow(IFacadeStream stream)
    {
        return stream.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);
    }

    private static double GetComponentMassFlow(IFacadeStream stream, int componentIndex)
    {
        return stream.Composition.Components[componentIndex].MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);
    }

    private static double GetMassFraction(IFacadeStream stream, int componentIndex)
    {
        return stream.Composition.Components[componentIndex].MassFraction.Value.GetValue(PercentageUnits.Percentage);
    }

    private static void RunSimulation(MainSolver solver)
    {
        solver.RunSimulation();
        Thread.Sleep(250);
    }

    private static void AssertClose(string name, double expected, double actual, double tolerance = Tolerance)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException($"{name}: expected {expected}, actual {actual}");
        }
    }

    private static void AssertUndefined(string name, IVariable? variable)
    {
        if (variable?.IsDefined == true)
        {
            throw new InvalidOperationException($"{name}: expected undefined, actual {variable.ToUiString()}");
        }
    }

    private sealed record MixerCase(
        MainSolver Solver,
        IFacadeStream Inlet1,
        IFacadeStream Inlet2,
        IFacadeStream Outlet);
}
