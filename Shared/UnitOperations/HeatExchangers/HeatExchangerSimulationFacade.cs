using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.UnitOperations.HeatExchangers
{
   

    public enum HeatExchangerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    //public class HeatExchangerSimulationFacade : EquipmentFacade
    //{
    //    public HeatExchangerStateType State { get; set; } = HeatExchangerStateType.Created;

    //    // --- Lado de los Tubos ---
    //    public StreamSimulationFacade? TubeInStream { get; private set; }
    //    public StreamSimulationFacade? TubeOutStream { get; private set; }

    //    // --- Lado de la Coraza (Shell) ---
    //    public StreamSimulationFacade? ShellInStream { get; private set; }
    //    public StreamSimulationFacade? CondensateOutStream { get; private set; }
    //    public StreamSimulationFacade? VaporVentStream { get; private set; }

    //    public override string StatusColor => State switch
    //    {
    //        HeatExchangerStateType.Created => "#CBD5E0",
    //        HeatExchangerStateType.PartiallyConnected => "#F6AD55",
    //        HeatExchangerStateType.ReadyToCalculate => "#63B3ED",
    //        HeatExchangerStateType.Solved => "#34D399",
    //        _ => "#CBD5E0"
    //    };

    //    public override string StatusText => State switch
    //    {
    //        HeatExchangerStateType.Created => "Ready",
    //        HeatExchangerStateType.PartiallyConnected => "Underspecified",
    //        HeatExchangerStateType.ReadyToCalculate => "Ready to Solve",
    //        HeatExchangerStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override List<ToolTipLegend> GetToolTipLegend()
    //    {
    //        return new List<ToolTipLegend>(); // Se llenará con el Duty (Heat Load) o el UxA luego
    //    }

    //    public override void AttachConnection(string portName, IFacade connectedFacade)
    //    {
    //        var stream = connectedFacade as StreamSimulationFacade;
    //        if (stream == null) return;

    //        if (portName == "TubeIn") TubeInStream = stream;
    //        else if (portName == "TubeOut") TubeOutStream = stream;
    //        else if (portName == "ShellIn") ShellInStream = stream;
    //        else if (portName == "CondensateOut") CondensateOutStream = stream;
    //        else if (portName == "VaporVent") VaporVentStream = stream;

         
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName == "TubeIn") TubeInStream = null;
    //        else if (portName == "TubeOut") TubeOutStream = null;
    //        else if (portName == "ShellIn") ShellInStream = null;
    //        else if (portName == "CondensateOut") CondensateOutStream = null;
    //        else if (portName == "VaporVent") VaporVentStream = null;

     
    //    }

    //    protected override void CalculatedEquipment()
    //    {
    //        // Placeholder para balance de energía, LMTD, o método NTU
    //        State = HeatExchangerStateType.ReadyToCalculate;
    //    }
    //    public override void BuildEquations(EquationSystem eqs)
    //    {

    //    }

    //    public override IEnumerable<INewVariable> GetSolverVariables()
    //    {
    //        return null!;
    //    }
    //}
    //public class HeatExchangerSimulationFacade2 : EquipmentFacade2
    //{
    //    public HeatExchangerStateType State { get; set; } = HeatExchangerStateType.Created;

    //    // 🔗 Conexiones: Tubos (Tube) y Coraza (Shell)
    //    public IStreamFacade2? TubeIn { get; private set; }
    //    public IStreamFacade2? TubeOut { get; private set; }
    //    public IStreamFacade2? ShellIn { get; private set; }
    //    public IStreamFacade2? ShellOut { get; private set; }

    //    // 🔹 Variables operativas (pueden ser especificadas o calculadas)
    //    //public INewVariableAmount<EnergyFlow> Duty { get; set; }  // Q > 0: Shell calienta Tube
    //    //public INewVariableAmount<PressureDrop> DeltaP_Tube { get; set; }
    //    //public INewVariableAmount<PressureDrop> DeltaP_Shell { get; set; }

    //    public HeatExchangerSimulationFacade2()
    //    {
    //        //Duty = new NewControlledVariableAmount<EnergyFlow>(
    //        //    new EnergyFlow(), EnergyFlowUnits.Kcal_hr, EnergyFlowUnits.Kcal_hr,
    //        //    (v, u) => new EnergyFlow(v, u));
    //        //Duty.OnExecuteSolver += ExecuteSolver;
    //        //Duty.OnGoToLocalCalculation += CalculateHeatTransferParams;

    //        //DeltaP_Tube = new NewControlledVariableAmount<PressureDrop>(
    //        //    new PressureDrop(), PressureDropUnits.Bar, PressureDropUnits.Pascal,
    //        //    (v, u) => new PressureDrop(v, u));
    //        //DeltaP_Tube.OnExecuteSolver += ExecuteSolver;

    //        //DeltaP_Shell = new NewControlledVariableAmount<PressureDrop>(
    //        //    new PressureDrop(), PressureDropUnits.Bar, PressureDropUnits.Pascal,
    //        //    (v, u) => new PressureDrop(v, u));
    //        //DeltaP_Shell.OnExecuteSolver += ExecuteSolver;
    //    }

    //    public override string StatusText => State switch
    //    {
    //        HeatExchangerStateType.Created => "Ready",
    //        HeatExchangerStateType.PartiallyConnected => "Underspecified",
    //        HeatExchangerStateType.ReadyToCalculate => "Ready to Solve",
    //        HeatExchangerStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override string StatusColor => State switch
    //    {
    //        HeatExchangerStateType.Created => "#CBD5E0",
    //        HeatExchangerStateType.PartiallyConnected => "#F6AD55",
    //        HeatExchangerStateType.ReadyToCalculate => "#63B3ED",
    //        HeatExchangerStateType.Solved => "#34D399",
    //        _ => "#CBD5E0"
    //    };

    //    public override List<ToolTipLegend> GetToolTipLegend()
    //    {
    //        var result = new List<ToolTipLegend>();
    //        //if (Duty.IsEspecified) result.Add(new("Duty", Duty.Value?.ToString() ?? string.Empty));
    //        return result;
    //    }

    //    public override void AttachConnection(string portName, IStreamFacade2 connectedFacade)
    //    {
    //        if (portName == "TubeIn") TubeIn = connectedFacade;
    //        else if (portName == "TubeOut") TubeOut = connectedFacade;
    //        else if (portName == "ShellIn") ShellIn = connectedFacade;
    //        else if (portName == "ShellOut") ShellOut = connectedFacade;
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName == "TubeIn") TubeIn = null;
    //        else if (portName == "TubeOut") TubeOut = null;
    //        else if (portName == "ShellIn") ShellIn = null;
    //        else if (portName == "ShellOut") ShellOut = null;
    //    }

    //    //public override void BuildEquations(EquationSystem eqs)
    //    //{
    //    //    if (TubeIn == null || TubeOut == null || ShellIn == null || ShellOut == null) return;

    //    //    // 🔥 BALANCE POR COMPONENTE - Lado Tubos (sin reacción, sin mezcla con Shell)
    //    //    if (TubeIn.StreamComposition?.Value?.Components.Count > 0 && TubeOut.StreamComposition?.Value != null)
    //    //    {
    //    //        var compsIn = TubeIn.StreamComposition.Value.Components;
    //    //        var compsOut = TubeOut.StreamComposition.Value.Components;
    //    //        for (int i = 0; i < compsIn.Count && i < compsOut.Count; i++)
    //    //        {
    //    //            eqs.AddEquation(
    //    //                x => x[compsOut[i].MolarFlowSolver.Index] - x[compsIn[i].MolarFlowSolver.Index],
    //    //                EquationType.Model,
    //    //                $"HX Tube component {compsIn[i].ComponentName}"
    //    //            );
    //    //        }
    //    //    }

    //    //    // 🔥 BALANCE POR COMPONENTE - Lado Coraza
    //    //    if (ShellIn.StreamComposition?.Value?.Components.Count > 0 && ShellOut.StreamComposition?.Value != null)
    //    //    {
    //    //        var compsIn = ShellIn.StreamComposition.Value.Components;
    //    //        var compsOut = ShellOut.StreamComposition.Value.Components;
    //    //        for (int i = 0; i < compsIn.Count && i < compsOut.Count; i++)
    //    //        {
    //    //            eqs.AddEquation(
    //    //                x => x[compsOut[i].MolarFlowSolver.Index] - x[compsIn[i].MolarFlowSolver.Index],
    //    //                EquationType.Model,
    //    //                $"HX Shell component {compsIn[i].ComponentName}"
    //    //            );
    //    //        }
    //    //    }

    //    //    // 🔥 BALANCE DE ENERGÍA - Lado Tubos: H_out = H_in ± Q/(F·MW)
    //    //    // Signo: si Duty > 0, Shell calienta Tube → Tube gana energía
    //    //    var H_tube_in = TubeIn.MolarEnthalpy;
    //    //    var H_tube_out = TubeOut.MolarEnthalpy;
    //    //    var F_tube = TubeIn.MolarFlow; // Asumimos F_in = F_out por balance de masa

    //    //    // Solo agregar ecuación si Duty está especificado O si H_tube_in y F_tube lo están
    //    //    if (Duty.IsEspecified || (H_tube_in.IsEspecified && F_tube.IsEspecified))
    //    //    {
    //    //        eqs.AddEquation(
    //    //            x =>
    //    //            {
    //    //                double duty = Duty.IsEspecified ? Duty.SolverValue : x[Duty.Index];
    //    //                double f_tube = x[F_tube.Index];
    //    //                // Q positivo: Shell → Tube. Tube gana: H_out = H_in + Q/(F·MW)
    //    //                // MW aproximado: usar valor de entrada si está disponible
    //    //                double MW = TubeIn.MaterialStream?.MolecularWeight ?? 18.0;
    //    //                return f_tube > 1e-6 ? x[H_tube_out.Index] - (x[H_tube_in.Index] + duty / (f_tube * MW)) : 0;
    //    //            },
    //    //            EquationType.Model,
    //    //            "HX Tube energy balance"
    //    //        );
    //    //    }

    //    //    // 🔥 BALANCE DE ENERGÍA - Lado Coraza: H_out = H_in ∓ Q/(F·MW) (signo opuesto)
    //    //    var H_shell_in = ShellIn.MolarEnthalpy;
    //    //    var H_shell_out = ShellOut.MolarEnthalpy;
    //    //    var F_shell = ShellIn.MolarFlow;

    //    //    if (Duty.IsEspecified || (H_shell_in.IsEspecified && F_shell.IsEspecified))
    //    //    {
    //    //        eqs.AddEquation(
    //    //            x =>
    //    //            {
    //    //                double duty = Duty.IsEspecified ? Duty.SolverValue : x[Duty.Index];
    //    //                double f_shell = x[F_shell.Index];
    //    //                // Shell pierde si Duty > 0: H_out = H_in - Q/(F·MW)
    //    //                double MW = ShellIn.MaterialStream?.MolecularWeight ?? 18.0;
    //    //                return f_shell > 1e-6 ? x[H_shell_out.Index] - (x[H_shell_in.Index] - duty / (f_shell * MW)) : 0;
    //    //            },
    //    //            EquationType.Model,
    //    //            "HX Shell energy balance"
    //    //        );
    //    //    }

    //    //    // 🔥 BALANCE DE PRESIÓN - Tubos: P_out = P_in - ΔP_tube
    //    //    if (DeltaP_Tube.IsEspecified || TubeIn.Pressure.IsEspecified)
    //    //    {
    //    //        eqs.AddEquation(
    //    //            x => x[TubeOut.Pressure.Index] - (x[TubeIn.Pressure.Index] - x[DeltaP_Tube.Index]),
    //    //            EquationType.Model,
    //    //            "HX Tube pressure drop"
    //    //        );
    //    //    }

    //    //    // 🔥 BALANCE DE PRESIÓN - Coraza
    //    //    if (DeltaP_Shell.IsEspecified || ShellIn.Pressure.IsEspecified)
    //    //    {
    //    //        eqs.AddEquation(
    //    //            x => x[ShellOut.Pressure.Index] - (x[ShellIn.Pressure.Index] - x[DeltaP_Shell.Index]),
    //    //            EquationType.Model,
    //    //            "HX Shell pressure drop"
    //    //        );
    //    //    }
    //    //}

    //    private void CalculateHeatTransferParams()
    //    {
    //        // Placeholder para Fase 2: cálculo de U·A, LMTD, etc. con Kern
    //        // Por ahora: solo propagar Duty si se calculó por diferencia de entalpías
    //        if (TubeIn?.MolarEnthalpy?.IsDefined == true && TubeOut?.MolarEnthalpy?.IsDefined == true &&
    //            TubeIn?.MolarFlow?.IsDefined == true)
    //        {
    //            double MW = TubeIn.MaterialStream?.MolecularWeight ?? 18.0;
    //            double Q_tube = (TubeOut.MolarEnthalpy.SolverValue - TubeIn.MolarEnthalpy.SolverValue) *
    //                           TubeIn.MolarFlow.SolverValue * MW;
    //            //Duty.SetValueFromSolver(Q_tube);
    //        }
    //    }

    //    //public override IEnumerable<INewVariable> GetSolverVariables()
    //    //{
    //    //    // 🔹 Variables propias del equipo
    //    //    yield return Duty;
    //    //    yield return DeltaP_Tube;
    //    //    yield return DeltaP_Shell;

    //    //    // 🔹 Variables de streams (solo si están conectados)
    //    //    foreach (var s in new[] { TubeIn, TubeOut, ShellIn, ShellOut }.Where(s => s != null))
    //    //    {
    //    //        yield return s.Pressure;
    //    //        yield return s.MolarEnthalpy;
    //    //        yield return s.MolarFlow;
    //    //        if (s.StreamComposition?.Value?.Components != null)
    //    //            foreach (var c in s.StreamComposition.Value.Components)
    //    //                yield return c.MolarFlowSolver;
    //    //    }
    //    //}
    //}
}
