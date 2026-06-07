using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.UnitOperations.ControlValves
{
    public enum ValveStateType
    {
        Created,
        PartiallyConnected,
        ReadyToCalculate,
        Solved
    }
    //public class ControlValveSimulationFacade2 : EquipmentFacade2
    //{
    //    // =========================
    //    // 🔹 CONEXIONES
    //    // =========================
    //    public IStreamFacade2? Inlet { get; private set; }
    //    public IStreamFacade2? Outlet { get; private set; }
    //    public NewNewVariableAmount<PressureDrop> DeltaPressure { get; set; }
    //    public NewNewVariableDouble Cv { get; set; }

    //    // =========================
    //    // 🔹 CONSTRUCTOR
    //    // =========================
    //    public ControlValveSimulationFacade2()
    //    {
    //        DeltaPressure = new NewNewVariableAmount<PressureDrop>(
    //            new PressureDrop(), PressureDropUnits.Bar, PressureDropUnits.Pascal, (v, u) => new PressureDrop(v, u));
    //        DeltaPressure.ExecuteGeneralSolver += ExecuteSolver;
    //        DeltaPressure.ExecuteStreamCalculation += CalculateValveParameters;
    //        DeltaPressure.ExecuteEquipmentSolver += OnPropagatePressure;

    //        Cv = new NewNewVariableDouble(0);
    //    }

    //    // =========================
    //    // 🔹 ECUACIONES PARA SOLVER VIEJO (se mantienen)
    //    // =========================
    //    private EquationSystem eqConc = new EquationSystem();
    //    private EquationSystem eqMolarFlow = new EquationSystem();
    //    private EquationSystem eqPressure = new EquationSystem();

    //    public override EquationSystem GetEquationConcentration()
    //    {
    //        var eq = new EquationSystem();
    //        if (Inlet == null || Outlet == null) return eq;

    //        eq.AddVariables(GetConcentrationVariables());
    //        var compsIn = Inlet.StreamComposition?.Value?.Components;
    //        var compsOut = Outlet.StreamComposition?.Value?.Components;

    //        if (compsIn != null && compsOut != null)
    //        {
    //            for (int i = 0; i < compsIn.Count; i++)
    //            {
    //                eq.AddEquation(new Equation
    //                {
    //                    Function = x => x[compsOut[i].MolarFractionSolver.Index] - x[compsIn[i].MolarFractionSolver.Index],
    //                    Type = EquationType.Model
    //                });
    //            }
    //        }
    //        return eq;
    //    }

    //    public override EquationSystem GetEquationPressure()
    //    {
    //        var eq = new EquationSystem();
    //        if (Inlet == null || Outlet == null) return eq;

    //        eq.AddVariables(GetPressureVariables());
    //        eq.AddEquation(new Equation
    //        {
    //            Function = x => x[Outlet.Pressure.Index] - (x[Inlet.Pressure.Index] - x[DeltaPressure.Index]),  // ← Signo MENOS para válvula
    //            Type = EquationType.Model
    //        });
    //        return eq;
    //    }

    //    public override EquationSystem GetEquationSystem()
    //    {
    //        var eq = new EquationSystem();
    //        if (Inlet == null || Outlet == null) return eq;

    //        eq.AddVariables(GetEnergyBalanceVariables());

    //        // Balance de masa
    //        eq.AddEquation(new Equation
    //        {
    //            Function = x => x[Outlet.MassFlow.Index] - x[Inlet.MassFlow.Index],
    //            Type = EquationType.Model
    //        });

    //        // Balance de energía (isentalpico para válvula ideal)
    //        eq.AddEquation(new Equation
    //        {
    //            Function = x => x[Outlet.MassFlow.Index] * x[Outlet.MassEnthalpy.Index] - x[Inlet.MassFlow.Index] * x[Inlet.MassEnthalpy.Index],
    //            Type = EquationType.Model
    //        });

    //        return eq;
    //    }

    //    // =========================
    //    // 🔥 NUEVO: ECUACIONES PARA SOLVER REACTIVO
    //    // =========================
       
       

    //    // =========================
    //    // 🔹 MÉTODOS DE PROPAGACIÓN
    //    // =========================
    //    private void OnPropagateConcentrations()
    //    {
    //        if (Inlet == null || Outlet == null) return;
    //        eqConc = GetEquationConcentration();
    //        eqConc.SolveEquipmet();
    //    }

    //    private void OnPropagateMassFlow()
    //    {
    //        if (Inlet == null || Outlet == null) return;
    //        eqMolarFlow.Clear();
    //        eqMolarFlow.AddVariables(GetMassBalanceVariables());
    //        eqMolarFlow.AddEquation(new Equation
    //        {
    //            Function = x => x[Outlet.MassFlow.Index] - x[Inlet.MassFlow.Index],
    //            Type = EquationType.Model
    //        });
    //        eqMolarFlow.SolveEquipmet();
    //    }

    //    private void OnPropagatePressure()
    //    {
    //        if (Inlet == null || Outlet == null) return;
    //        eqPressure = GetEquationPressure();
    //        eqPressure.SolveEquipmet();
    //    }

    //    private void CalculateValveParameters()
    //    {
    //        // Implementación existente (no modificada)
    //        if (Inlet == null || Outlet == null) return;
    //        if (!DeltaPressure.IsDefined) return;

    //        // ... tu lógica existente de Cv ...
    //    }

    //    // =========================
    //    // 🔹 CONEXIÓN/DESCONEXIÓN
    //    // =========================
    //    public override void AttachConnection(string portName, IStreamFacade2 connectedFacade)
    //    {
    //        if (portName == "Inlet" && Inlet == null)
    //        {
    //            Inlet = connectedFacade;
    //            SubscribeStream(Inlet);
    //            TriggerPropagation();
    //        }
    //        else if (portName == "Outlet" && Outlet == null)
    //        {
    //            Outlet = connectedFacade;
    //            SubscribeStream(Outlet);
    //            TriggerPropagation();
    //        }
    //    }

    //    private void SubscribeStream(IStreamFacade2 stream)
    //    {
    //        if (stream?.StreamComposition != null)
    //        {
    //            stream.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
    //            stream.StreamComposition.ExecuteEquipmentSolver += OnPropagateConcentrations;
    //        }
    //        if (stream?.MassFlow != null)
    //        {
    //            stream.MassFlow.ExecuteEquipmentSolver -= OnPropagateMassFlow;
    //            stream.MassFlow.ExecuteEquipmentSolver += OnPropagateMassFlow;
    //            stream.MassFlow.ExecuteStreamCalculation -= CalculateValveParameters;
    //            stream.MassFlow.ExecuteStreamCalculation += CalculateValveParameters;
    //        }
    //        if (stream?.Pressure != null)
    //        {
    //            stream.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;
    //            stream.Pressure.ExecuteEquipmentSolver += OnPropagatePressure;
    //        }
    //    }

    //    private void TriggerPropagation()
    //    {
    //        OnPropagateConcentrations();
    //        OnPropagateMassFlow();
    //        OnPropagatePressure();
    //        ExecuteSolver();
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName == "Inlet")
    //        {
    //            UnsubscribeStream(Inlet!);
    //            ClearPropagatedValues(Inlet!);
    //            ClearPropagatedValues(Outlet!);
    //            eqConc.ClearEquipmentSolverDefinitions();
    //            eqMolarFlow.ClearEquipmentSolverDefinitions();
    //            eqPressure.ClearEquipmentSolverDefinitions();
    //            Cv?.ClearFromEquipmentSolver();
    //            Inlet = null;
    //            ExecuteSolver();
    //        }
    //        else if (portName == "Outlet")
    //        {
    //            UnsubscribeStream(Outlet!);
    //            ClearPropagatedValues(Outlet!);
    //            ClearPropagatedValues(Inlet!);
    //            eqConc.ClearEquipmentSolverDefinitions();
    //            eqMolarFlow.ClearEquipmentSolverDefinitions();
    //            eqPressure.ClearEquipmentSolverDefinitions();
    //            Cv?.ClearFromEquipmentSolver();
    //            Outlet = null;
    //            ExecuteSolver();
    //        }
    //    }

    //    private void UnsubscribeStream(IStreamFacade2 stream)
    //    {
    //        if (stream?.StreamComposition != null)
    //            stream.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
    //        if (stream?.MassFlow != null)
    //        {
    //            stream.MassFlow.ExecuteEquipmentSolver -= OnPropagateMassFlow;
    //            stream.MassFlow.ExecuteStreamCalculation -= CalculateValveParameters;
    //        }
    //        if (stream?.Pressure != null)
    //            stream.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;
    //    }

    //    // =========================
    //    // 🔹 HELPERS DE VARIABLES
    //    // =========================
    //    private IEnumerable<INewNewVariable> GetPressureVariables()
    //    {
    //        if (DeltaPressure != null) yield return DeltaPressure;
    //        if (Inlet?.Pressure != null) yield return Inlet.Pressure;
    //        if (Outlet?.Pressure != null) yield return Outlet.Pressure;
    //    }

    //    private IEnumerable<INewNewVariable> GetConcentrationVariables()
    //    {
    //        if (Inlet?.StreamComposition?.Value?.Components != null)
    //            foreach (var c in Inlet.StreamComposition.Value.Components)
    //                yield return c.MolarFractionSolver;
    //        if (Outlet?.StreamComposition?.Value?.Components != null)
    //            foreach (var c in Outlet.StreamComposition.Value.Components)
    //                yield return c.MolarFractionSolver;
    //    }

    //    private IEnumerable<INewNewVariable> GetMassBalanceVariables()
    //    {
    //        if (Inlet?.MassFlow != null) yield return Inlet.MassFlow;
    //        if (Outlet?.MassFlow != null) yield return Outlet.MassFlow;
    //    }

    //    public IEnumerable<INewNewVariable> GetEnergyBalanceVariables()
    //    {
    //        if (Inlet?.MassFlow != null) yield return Inlet.MassFlow;
    //        if (Inlet?.MassEnthalpy != null) yield return Inlet.MassEnthalpy;
    //        if (Outlet?.MassFlow != null) yield return Outlet.MassFlow;
    //        if (Outlet?.MassEnthalpy != null) yield return Outlet.MassEnthalpy;
    //    }

    //    // =========================
    //    // 🔹 ESTADO Y UI
    //    // =========================
    //    public ValveStateType State => GetState();
    //    private ValveStateType GetState()
    //    {
    //        if (Inlet == null || Outlet == null) return ValveStateType.PartiallyConnected;
    //        if (!DeltaPressure.IsDefined) return ValveStateType.ReadyToCalculate;
    //        if (Outlet?.Pressure?.IsDefined == true && Inlet?.Pressure?.IsDefined == true)
    //        {
    //            var expected = Inlet.Pressure.SolverValue - DeltaPressure.SolverValue;
    //            if (Math.Abs(Outlet.Pressure.SolverValue - expected) < 1e-3)
    //                return ValveStateType.Solved;
    //        }
    //        return ValveStateType.ReadyToCalculate;
    //    }

    //    public override string StatusText => State switch
    //    {
    //        ValveStateType.Created => "Ready",
    //        ValveStateType.PartiallyConnected => "Underspecified",
    //        ValveStateType.ReadyToCalculate => "Ready to Solve",
    //        ValveStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override string StatusColor => State switch
    //    {
    //        ValveStateType.Created => "#CBD5E0",
    //        ValveStateType.PartiallyConnected => "#F6AD55",
    //        ValveStateType.ReadyToCalculate => "#63B3ED",
    //        ValveStateType.Solved => "#34D399",
    //        _ => "#CBD5E0"
    //    };

    //    public override List<ToolTipLegend> GetToolTipLegend()
    //    {
    //        return new List<ToolTipLegend>
    //    {
    //        new("ΔP", DeltaPressure?.GetDisplayString() ?? "-"),
    //        new("Cv", Cv?.GetDisplayString() ?? "-")
    //    };
    //    }
    //}



    //public class ControlValveSimulationFacade : EquipmentFacade
    //{
    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 PUERTOS
    //    // ═══════════════════════════════════════════════════════════
    //    public const string PortInlet = "Inlet";
    //    public const string PortOutlet = "Outlet";

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 CONEXIONES
    //    // ═══════════════════════════════════════════════════════════
    //    private IStreamFacade? _inlet;
    //    private IStreamFacade? _outlet;

    //    public IStreamFacade? Inlet => _inlet;
    //    public IStreamFacade? Outlet => _outlet;

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 VARIABLES DE CONTROL
    //    // ═══════════════════════════════════════════════════════════
    //    public VariableAmount<PressureDrop> DeltaPressure { get; private set; }
    //    public VariableDouble Cv { get; private set; }

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 CONSTRUCTOR
    //    // ═══════════════════════════════════════════════════════════
    //    public ControlValveSimulationFacade()
    //    {
    //        DeltaPressure = new VariableAmount<PressureDrop>(
    //            new PressureDrop(), PressureDropUnits.Bar, PressureDropUnits.Pascal, (v, u) => new PressureDrop(v, u));
    //        Cv = new VariableDouble(0);
    //        SubscribeVariables();
    //    }

    //    private void SubscribeVariables()
    //    {
    //        DeltaPressure.ExecuteStreamCalculation += CalculateValveParametersIfPossible;
    //        DeltaPressure.ExecuteGeneralSolver += () => ExecuteSolver();
    //        Cv.ExecuteGeneralSolver += () => ExecuteSolver();
    //    }

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 IMPLEMENTACIÓN DE IEquipmentFacade
    //    // ═══════════════════════════════════════════════════════════
    //    public override IEnumerable<string> GetPortNames()
    //    {
    //        yield return PortInlet;
    //        yield return PortOutlet;
    //    }

    //    public override IStreamFacade? GetConnectedStream(string portName)
    //    {
    //        return portName switch
    //        {
    //            PortInlet => _inlet,
    //            PortOutlet => _outlet,
    //            _ => null
    //        };
    //    }

    //    public override IEnumerable<IVariable> GetControlledVariables()
    //    {
    //        yield return DeltaPressure;
    //        yield return Cv;
    //    }

    //    public override List<GlobalEquation> GetReactiveEquations(List<IVariable> allVariables)
    //    {
    //        var equations = new List<GlobalEquation>();
    //        if (_inlet == null || _outlet == null) return equations;

    //        // Presión: P_out = P_in - ΔP
    //        equations.Add(new GlobalEquation
    //        {
    //            Function = vars =>
    //            {
    //                var pIn = GetVarValue(vars, _inlet!.Pressure);
    //                var pOut = GetVarValue(vars, _outlet!.Pressure);
    //                var dP = GetVarValue(vars, DeltaPressure);
    //                return pOut - (pIn - dP);
    //            },
    //            Type = ReactiveEquationType.Model,
    //            EquipmentId = this.Id.ToString(),
    //            Description = "P_out = P_in - ΔP (Válvula)"
    //        });

    //        // Masa: ṁ_out = ṁ_in
    //        equations.Add(new GlobalEquation
    //        {
    //            Function = vars =>
    //            {
    //                var mIn = GetVarValue(vars, _inlet!.MassFlow);
    //                var mOut = GetVarValue(vars, _outlet!.MassFlow);
    //                return mOut - mIn;
    //            },
    //            Type = ReactiveEquationType.Connection,
    //            EquipmentId = this.Id.ToString(),
    //            Description = "ṁ_out = ṁ_in (Conservación de masa)"
    //        });

    //        // Energía: h_out = h_in (isentalpico)
    //        equations.Add(new GlobalEquation
    //        {
    //            Function = vars =>
    //            {
    //                var mIn = GetVarValue(vars, _inlet!.MassFlow);
    //                var hin = GetVarValue(vars, _inlet!.MassEnthalpy);
    //                var hout = GetVarValue(vars, _outlet!.MassEnthalpy);
    //                return (mIn * hout) - (mIn * hin);
    //            },
    //            Type = ReactiveEquationType.EnergyBalance,
    //            EquipmentId = this.Id.ToString(),
    //            Description = "h_out = h_in (Expansión isentalpica)"
    //        });

    //        return equations;
    //    }

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 CÁLCULO LOCAL DE Cv
    //    // ═══════════════════════════════════════════════════════════
    //    private void CalculateValveParametersIfPossible()
    //    {
    //        if (_inlet == null || _outlet == null) return;
    //        if (!DeltaPressure.IsDefined) return;

    //        var deltaP_bar = DeltaPressure.GetEffectiveSolverValue();
    //        var p1_bar = _inlet.Pressure.GetEffectiveSolverValue();
    //        var massFlow_kg_s = _inlet.MassFlow.GetEffectiveSolverValue();
    //        var massFlow_kg_hr = massFlow_kg_s * 3600.0;
    //        var rho = _inlet.MassDensity.IsDefined ? _inlet.MassDensity.GetEffectiveSolverValue() : 1000.0;
    //        var t_k = _inlet.Temperature.GetEffectiveSolverValue();
    //        bool isLiquid = rho > 100;
    //        double cv_calc = 0;

    //        if (isLiquid)
    //        {
    //            var sg = rho / 1000.0;
    //            var q_m3_hr = _inlet.VolumetricFlow.IsDefined
    //                ? _inlet.VolumetricFlow.GetEffectiveSolverValue()
    //                : massFlow_kg_hr / rho;
    //            if (deltaP_bar > 0 && sg > 0 && q_m3_hr > 0)
    //                cv_calc = q_m3_hr / Math.Sqrt(deltaP_bar / sg);
    //        }
    //        else
    //        {
    //            const double n = 0.001;
    //            var sg_gas = _inlet.MolecularWeight.IsDefined
    //                ? _inlet.MolecularWeight.GetEffectiveSolverValue() / 28.97
    //                : 1.0;
    //            var y = Math.Max(1.0 - (deltaP_bar / (3.0 * p1_bar * 0.72)), 0.667);
    //            if (deltaP_bar > 0 && p1_bar > 0 && y > 0)
    //            {
    //                var denominator = n * y * Math.Sqrt((deltaP_bar * p1_bar * sg_gas) / (t_k * 1.0));
    //                if (denominator > 0 && !double.IsNaN(denominator))
    //                    cv_calc = massFlow_kg_hr / denominator;
    //            }
    //        }

    //        if (cv_calc > 0 && !double.IsNaN(cv_calc) && !double.IsInfinity(cv_calc) && !Cv.IsDefinedByUI)
    //            Cv.NewSolverValue = cv_calc;
    //    }

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 CONEXIÓN/DESCONEXIÓN
    //    // ═══════════════════════════════════════════════════════════
    //    public override void AttachConnection(string portName, IStreamFacade connectedFacade)
    //    {
    //        if (portName == PortInlet && _inlet == null)
    //        {
    //            _inlet = connectedFacade;
    //            SubscribeStream(_inlet);
    //            TriggerPropagation();
    //        }
    //        else if (portName == PortOutlet && _outlet == null)
    //        {
    //            _outlet = connectedFacade;
    //            SubscribeStream(_outlet);
    //            TriggerPropagation();
    //        }
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName == PortInlet && _inlet != null)
    //        {
    //            UnsubscribeStream(_inlet);
    //            ClearCalculatedValues(_inlet);
    //            ClearCalculatedValues(_outlet!);
    //            if (!Cv.IsDefinedByUI) Cv.ClearFromStream();
    //            _inlet = null;
    //            ExecuteSolver();
    //        }
    //        else if (portName == PortOutlet && _outlet != null)
    //        {
    //            UnsubscribeStream(_outlet);
    //            ClearCalculatedValues(_outlet);
    //            ClearCalculatedValues(_inlet!);
    //            if (!Cv.IsDefinedByUI) Cv.ClearFromStream();
    //            _outlet = null;
    //            ExecuteSolver();
    //        }
    //    }

    //    private void SubscribeStream(IStreamFacade stream)
    //    {
    //        stream.Pressure.ExecuteStreamCalculation += CalculateValveParametersIfPossible;
    //        stream.MassFlow.ExecuteStreamCalculation += CalculateValveParametersIfPossible;
    //    }

    //    private void UnsubscribeStream(IStreamFacade stream)
    //    {
    //        stream.Pressure.ExecuteStreamCalculation -= CalculateValveParametersIfPossible;
    //        stream.MassFlow.ExecuteStreamCalculation -= CalculateValveParametersIfPossible;
    //    }

    //    private void TriggerPropagation()
    //    {
    //        CalculateValveParametersIfPossible();
    //        ExecuteSolver();
    //    }

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 ESTADO Y UI
    //    // ═══════════════════════════════════════════════════════════
    //    public override string StatusText => State switch
    //    {
    //        ValveStateType.Created => "Ready",
    //        ValveStateType.PartiallyConnected => "Underspecified",
    //        ValveStateType.ReadyToCalculate => "Ready to Solve",
    //        ValveStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override string StatusColor => State switch
    //    {
    //        ValveStateType.Created => "#CBD5E0",
    //        ValveStateType.PartiallyConnected => "#F6AD55",
    //        ValveStateType.ReadyToCalculate => "#63B3ED",
    //        ValveStateType.Solved => "#34D399",
    //        _ => "#CBD5E0"
    //    };

    //    public ValveStateType State => GetState();

    //    private ValveStateType GetState()
    //    {
    //        if (_inlet == null || _outlet == null) return ValveStateType.PartiallyConnected;
    //        if (!DeltaPressure.IsDefined || !_inlet.Pressure.IsDefined) return ValveStateType.ReadyToCalculate;
    //        if (_outlet.Pressure.IsDefined)
    //        {
    //            var expected = _inlet.Pressure.GetEffectiveSolverValue() - DeltaPressure.GetEffectiveSolverValue();
    //            var actual = _outlet.Pressure.GetEffectiveSolverValue();
    //            if (Math.Abs(actual - expected) < 1e-3) return ValveStateType.Solved;
    //        }
    //        return ValveStateType.ReadyToCalculate;
    //    }

    //    public override List<ToolTipLegend> GetToolTipLegend()
    //    {
    //        return new List<ToolTipLegend>
    //    {
    //        new("ΔP", DeltaPressure.GetDisplayString()),
    //        new("Cv", Cv.GetDisplayString())
    //    };
    //    }
    //}
}
