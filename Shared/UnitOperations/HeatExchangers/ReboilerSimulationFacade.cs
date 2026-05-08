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
 

    public enum ReboilerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class ReboilerSimulationFacade : EquipmentFacade
    {
        public ReboilerStateType State { get; set; } = ReboilerStateType.Created;

        // --- Lado de los Tubos (Líquido de fondo de la torre) ---
        public StreamSimulationFacade? TubeInStream { get; private set; }
        public StreamSimulationFacade? TubeOutStream { get; private set; }

        // --- Lado de la Coraza (Vapor de calentamiento) ---
        public StreamSimulationFacade? ShellInStream { get; private set; }
        public StreamSimulationFacade? CondensateOutStream { get; private set; }

        public override string StatusColor => State switch
        {
            ReboilerStateType.Created => "#CBD5E0",
            ReboilerStateType.PartiallyConnected => "#F6AD55",
            ReboilerStateType.ReadyToCalculate => "#63B3ED",
            ReboilerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override string StatusText => State switch
        {
            ReboilerStateType.Created => "Ready",
            ReboilerStateType.PartiallyConnected => "Underspecified",
            ReboilerStateType.ReadyToCalculate => "Ready to Solve",
            ReboilerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            return new List<ToolTipLegend>(); // Se llenará con el Heat Duty cuando se calcule
        }

        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            var stream = connectedFacade as StreamSimulationFacade;
            if (stream == null) return;

            if (portName == "TubeIn") TubeInStream = stream;
            else if (portName == "TubeOut") TubeOutStream = stream;
            else if (portName == "ShellIn") ShellInStream = stream;
            else if (portName == "CondensateOut") CondensateOutStream = stream;

           
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "TubeIn") TubeInStream = null;
            else if (portName == "TubeOut") TubeOutStream = null;
            else if (portName == "ShellIn") ShellInStream = null;
            else if (portName == "CondensateOut") CondensateOutStream = null;

       
        }

        protected override void CalculatedEquipment()
        {
            // Placeholder para balance térmico del fondo de la columna
            State = ReboilerStateType.ReadyToCalculate;
        }
        public override void BuildEquations(EquationSystem eqs)
        {

        }

        public override IEnumerable<INewVariable> GetSolverVariables()
        {
            return null!;
        }
    }
    public class ReboilerSimulationFacade2 : EquipmentFacade2
    {
        public ReboilerStateType State { get; set; } = ReboilerStateType.Created;

        // 🔗 Conexiones: Tubos (proceso) y Coraza (servicio)
        public IStreamFacade? TubeIn { get; private set; }    // Líquido de fondo de columna
        public IStreamFacade? TubeOut { get; private set; }   // Vapor que retorna a columna
        public IStreamFacade? ShellIn { get; private set; }   // Vapor de calentamiento (ej: steam)
        public IStreamFacade? ShellOut { get; private set; }  // Condensado

        // 🔹 Variables operativas
        public INewVariableAmount<EnergyFlow> Duty { get; set; }
        public INewVariableAmount<PressureDrop> DeltaP_Tube { get; set; }
        public INewVariableAmount<PressureDrop> DeltaP_Shell { get; set; }

        public ReboilerSimulationFacade2()
        {
            Duty = new NewControlledVariableAmount<EnergyFlow>(
                new EnergyFlow(), EnergyFlowUnits.Kcal_hr, EnergyFlowUnits.Kcal_hr,
                (v, u) => new EnergyFlow(v, u));
            Duty.OnExecuteSolver += ExecuteSolver;

            DeltaP_Tube = new NewControlledVariableAmount<PressureDrop>(
                new PressureDrop(), PressureDropUnits.Bar, PressureDropUnits.Pascal,
                (v, u) => new PressureDrop(v, u));
            DeltaP_Tube.OnExecuteSolver += ExecuteSolver;

            DeltaP_Shell = new NewControlledVariableAmount<PressureDrop>(
                new PressureDrop(), PressureDropUnits.Bar, PressureDropUnits.Pascal,
                (v, u) => new PressureDrop(v, u));
            DeltaP_Shell.OnExecuteSolver += ExecuteSolver;
        }

        public override string StatusText => State switch
        {
            ReboilerStateType.Created => "Ready",
            ReboilerStateType.PartiallyConnected => "Underspecified",
            ReboilerStateType.ReadyToCalculate => "Ready to Solve",
            ReboilerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            ReboilerStateType.Created => "#CBD5E0",
            ReboilerStateType.PartiallyConnected => "#F6AD55",
            ReboilerStateType.ReadyToCalculate => "#63B3ED",
            ReboilerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            var result = new List<ToolTipLegend>();
            if (Duty.IsEspecified) result.Add(new("Duty", Duty.Value?.ToString() ?? string.Empty));
            return result;
        }

        public override void AttachConnection(string portName, IStreamFacade connectedFacade)
        {
            if (portName == "TubeIn") TubeIn = connectedFacade;
            else if (portName == "TubeOut") TubeOut = connectedFacade;
            else if (portName == "ShellIn") ShellIn = connectedFacade;
            else if (portName == "ShellOut") ShellOut = connectedFacade;
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "TubeIn") TubeIn = null;
            else if (portName == "TubeOut") TubeOut = null;
            else if (portName == "ShellIn") ShellIn = null;
            else if (portName == "ShellOut") ShellOut = null;
        }

        //public override void BuildEquations(EquationSystem eqs)
        //{
        //    if (TubeIn == null || TubeOut == null || ShellIn == null || ShellOut == null) return;

        //    // 🔥 BALANCE POR COMPONENTE - Lado Proceso (Tubos): vaporización parcial
        //    if (TubeIn.StreamComposition?.Value?.Components.Count > 0 && TubeOut.StreamComposition?.Value != null)
        //    {
        //        var compsIn = TubeIn.StreamComposition.Value.Components;
        //        var compsOut = TubeOut.StreamComposition.Value.Components;
        //        for (int i = 0; i < compsIn.Count && i < compsOut.Count; i++)
        //        {
        //            // n_i_out = n_i_in (sin reacción, solo cambio de fase)
        //            eqs.AddEquation(
        //                x => x[compsOut[i].MolarFlowSolver.Index] - x[compsIn[i].MolarFlowSolver.Index],
        //                EquationType.Model,
        //                $"Reboiler component {compsIn[i].ComponentName}"
        //            );
        //        }
        //    }

        //    // 🔥 BALANCE POR COMPONENTE - Lado Servicio (Coraza): condensación de vapor (ej: steam)
        //    if (ShellIn.StreamComposition?.Value?.Components.Count > 0 && ShellOut.StreamComposition?.Value != null)
        //    {
        //        var compsIn = ShellIn.StreamComposition.Value.Components;
        //        var compsOut = ShellOut.StreamComposition.Value.Components;
        //        for (int i = 0; i < compsIn.Count && i < compsOut.Count; i++)
        //        {
        //            eqs.AddEquation(
        //                x => x[compsOut[i].MolarFlowSolver.Index] - x[compsIn[i].MolarFlowSolver.Index],
        //                EquationType.Model,
        //                $"Reboiler service component {compsIn[i].ComponentName}"
        //            );
        //        }
        //    }

        //    // 🔥 BALANCE DE ENERGÍA - Lado Proceso: H_out = H_in + Q/(F·MW)
        //    var H_tube_in = TubeIn.MolarEnthalpy;
        //    var H_tube_out = TubeOut.MolarEnthalpy;
        //    var F_tube = TubeIn.MolarFlow;

        //    if (Duty.IsEspecified || (H_tube_in.IsEspecified && F_tube.IsEspecified))
        //    {
        //        eqs.AddEquation(
        //            x =>
        //            {
        //                double duty = Duty.IsEspecified ? Duty.SolverValue : x[Duty.Index];
        //                double f_tube = x[F_tube.Index];
        //                double MW = TubeIn.MaterialStream?.MolecularWeight ?? 18.0;
        //                return f_tube > 1e-6 ? x[H_tube_out.Index] - (x[H_tube_in.Index] + duty / (f_tube * MW)) : 0;
        //            },
        //            EquationType.Model,
        //            "Reboiler process energy balance"
        //        );
        //    }

        //    // 🔥 BALANCE DE ENERGÍA - Lado Servicio: H_out = H_in - Q/(F·MW) (pierde calor)
        //    var H_shell_in = ShellIn.MolarEnthalpy;
        //    var H_shell_out = ShellOut.MolarEnthalpy;
        //    var F_shell = ShellIn.MolarFlow;

        //    if (Duty.IsEspecified || (H_shell_in.IsEspecified && F_shell.IsEspecified))
        //    {
        //        eqs.AddEquation(
        //            x =>
        //            {
        //                double duty = Duty.IsEspecified ? Duty.SolverValue : x[Duty.Index];
        //                double f_shell = x[F_shell.Index];
        //                double MW = ShellIn.MaterialStream?.MolecularWeight ?? 18.0;
        //                return f_shell > 1e-6 ? x[H_shell_out.Index] - (x[H_shell_in.Index] - duty / (f_shell * MW)) : 0;
        //            },
        //            EquationType.Model,
        //            "Reboiler service energy balance"
        //        );
        //    }

        //    // 🔥 BALANCE DE PRESIÓN
        //    if (DeltaP_Tube.IsEspecified || TubeIn.Pressure.IsEspecified)
        //    {
        //        eqs.AddEquation(
        //            x => x[TubeOut.Pressure.Index] - (x[TubeIn.Pressure.Index] - x[DeltaP_Tube.Index]),
        //            EquationType.Model,
        //            "Reboiler tube pressure drop"
        //        );
        //    }

        //    if (DeltaP_Shell.IsEspecified || ShellIn.Pressure.IsEspecified)
        //    {
        //        eqs.AddEquation(
        //            x => x[ShellOut.Pressure.Index] - (x[ShellIn.Pressure.Index] - x[DeltaP_Shell.Index]),
        //            EquationType.Model,
        //            "Reboiler shell pressure drop"
        //        );
        //    }
        //}

        //public override IEnumerable<INewVariable> GetSolverVariables()
        //{
        //    yield return Duty;
        //    yield return DeltaP_Tube;
        //    yield return DeltaP_Shell;

        //    foreach (var s in new[] { TubeIn, TubeOut, ShellIn, ShellOut }.Where(s => s != null))
        //    {
        //        yield return s.Pressure;
        //        yield return s.MolarEnthalpy;
        //        yield return s.MolarFlow;
        //        if (s.StreamComposition?.Value?.Components != null)
        //            foreach (var c in s.StreamComposition.Value.Components)
        //                yield return c.MolarFlowSolver;
        //    }
        //}
    }
}
