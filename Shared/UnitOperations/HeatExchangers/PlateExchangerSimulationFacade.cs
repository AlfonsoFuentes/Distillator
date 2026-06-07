using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.HeatExchangers
{
    

    public enum PlateExchangerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    //public class PlateExchangerSimulationFacade : EquipmentFacade
    //{
    //    public PlateExchangerStateType State { get; set; } = PlateExchangerStateType.Created;

    //    // --- Lado Caliente (Hot Side) ---
    //    public StreamSimulationFacade? HotInStream { get; private set; }
    //    public StreamSimulationFacade? HotOutStream { get; private set; }

    //    // --- Lado Frío (Cold Side) ---
    //    public StreamSimulationFacade? ColdInStream { get; private set; }
    //    public StreamSimulationFacade? ColdOutStream { get; private set; }

    //    public override string StatusColor => State switch
    //    {
    //        PlateExchangerStateType.Created => "#CBD5E0",
    //        PlateExchangerStateType.PartiallyConnected => "#F6AD55",
    //        PlateExchangerStateType.ReadyToCalculate => "#63B3ED",
    //        PlateExchangerStateType.Solved => "#34D399",
    //        _ => "#CBD5E0"
    //    };

    //    public override string StatusText => State switch
    //    {
    //        PlateExchangerStateType.Created => "Ready",
    //        PlateExchangerStateType.PartiallyConnected => "Underspecified",
    //        PlateExchangerStateType.ReadyToCalculate => "Ready to Solve",
    //        PlateExchangerStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override List<ToolTipLegend> GetToolTipLegend()
    //    {
    //        return new List<ToolTipLegend>();
    //    }

    //    public override void AttachConnection(string portName, IFacade connectedFacade)
    //    {
    //        var stream = connectedFacade as StreamSimulationFacade;
    //        if (stream == null) return;

    //        if (portName == "HotIn") HotInStream = stream;
    //        else if (portName == "HotOut") HotOutStream = stream;
    //        else if (portName == "ColdIn") ColdInStream = stream;
    //        else if (portName == "ColdOut") ColdOutStream = stream;

           
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName == "HotIn") HotInStream = null;
    //        else if (portName == "HotOut") HotOutStream = null;
    //        else if (portName == "ColdIn") ColdInStream = null;
    //        else if (portName == "ColdOut") ColdOutStream = null;

    
    //    }

    //    protected override void CalculatedEquipment()
    //    {
    //        // Placeholder para balance térmico
    //        State = PlateExchangerStateType.ReadyToCalculate;
    //    }
    //    public override void BuildEquations(EquationSystem eqs)
    //    {

    //    }

    //    public override IEnumerable<INewVariable> GetSolverVariables()
    //    {
    //        return null!;
    //    }
    //}
    //public class PlateExchangerSimulationFacade2 : EquipmentFacade2
    //{
    //    public PlateExchangerStateType State { get; set; } = PlateExchangerStateType.Created;
    //    public IStreamFacade2? HotIn { get; private set; }
    //    public IStreamFacade2? HotOut { get; private set; }
    //    public IStreamFacade2? ColdIn { get; private set; }
    //    public IStreamFacade2? ColdOut { get; private set; }

    //    public override string StatusText => State switch
    //    {
    //        PlateExchangerStateType.Created => "Ready",
    //        PlateExchangerStateType.PartiallyConnected => "Underspecified",
    //        PlateExchangerStateType.ReadyToCalculate => "Ready to Solve",
    //        PlateExchangerStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override string StatusColor => State switch
    //    {
    //        PlateExchangerStateType.Created => "#CBD5E0",
    //        PlateExchangerStateType.PartiallyConnected => "#F6AD55",
    //        PlateExchangerStateType.ReadyToCalculate => "#63B3ED",
    //        PlateExchangerStateType.Solved => "#34D399",
    //        _ => "#CBD5E0"
    //    };

    //    public override List<ToolTipLegend> GetToolTipLegend() => new();

    //    public override void AttachConnection(string portName, IStreamFacade2 connectedFacade)
    //    {
    //        if (portName == "HotIn") HotIn = connectedFacade;
    //        else if (portName == "HotOut") HotOut = connectedFacade;
    //        else if (portName == "ColdIn") ColdIn = connectedFacade;
    //        else if (portName == "ColdOut") ColdOut = connectedFacade;
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName == "HotIn") HotIn = null;
    //        else if (portName == "HotOut") HotOut = null;
    //        else if (portName == "ColdIn") ColdIn = null;
    //        else if (portName == "ColdOut") ColdOut = null;
    //    }

    //    //public override void BuildEquations(EquationSystem eqs)
    //    //{
    //    //    if (HotIn == null || HotOut == null || ColdIn == null || ColdOut == null) return;

    //    //    // 🔥 Balance por componente en cada lado (sin reacción, sin mezcla entre lados)
    //    //    if (HotIn.StreamComposition?.Value?.Components.Count > 0)
    //    //    {
    //    //        var compsHotIn = HotIn.StreamComposition.Value.Components;
    //    //        var compsHotOut = HotOut.StreamComposition?.Value?.Components;
    //    //        if (compsHotOut != null)
    //    //            for (int i = 0; i < compsHotIn.Count; i++)
    //    //                eqs.AddEquation(x => x[compsHotOut[i].MolarFlowSolver.Index] - x[compsHotIn[i].MolarFlowSolver.Index], EquationType.Model, $"PlateHX hot side {compsHotIn[i].ComponentName}");
    //    //    }
    //    //    if (ColdIn.StreamComposition?.Value?.Components.Count > 0)
    //    //    {
    //    //        var compsColdIn = ColdIn.StreamComposition.Value.Components;
    //    //        var compsColdOut = ColdOut.StreamComposition?.Value?.Components;
    //    //        if (compsColdOut != null)
    //    //            for (int i = 0; i < compsColdIn.Count; i++)
    //    //                eqs.AddEquation(x => x[compsColdOut[i].MolarFlowSolver.Index] - x[compsColdIn[i].MolarFlowSolver.Index], EquationType.Model, $"PlateHX cold side {compsColdIn[i].ComponentName}");
    //    //    }

    //    //    // 🔥 Balance de energía: Q_hot = -Q_cold (adiabático al entorno)
    //    //    var Qhot = HotIn.MolarEnthalpy.SolverValue * HotIn.MolarFlow.SolverValue - HotOut.MolarEnthalpy.SolverValue * HotOut.MolarFlow.SolverValue;
    //    //    var Qcold = ColdOut.MolarEnthalpy.SolverValue * ColdOut.MolarFlow.SolverValue - ColdIn.MolarEnthalpy.SolverValue * ColdIn.MolarFlow.SolverValue;
    //    //    eqs.AddEquation(x => (x[HotOut.MolarEnthalpy.Index] * x[HotOut.MolarFlow.Index] - x[HotIn.MolarEnthalpy.Index] * x[HotIn.MolarFlow.Index]) +
    //    //                         (x[ColdOut.MolarEnthalpy.Index] * x[ColdOut.MolarFlow.Index] - x[ColdIn.MolarEnthalpy.Index] * x[ColdIn.MolarFlow.Index]),
    //    //                     EquationType.Model, "PlateHX energy balance");
    //    //}

    //    //public override IEnumerable<INewVariable> GetSolverVariables()
    //    //{
    //    //    foreach (var s in new[] { HotIn, HotOut, ColdIn, ColdOut }.Where(s => s != null))
    //    //    {
    //    //        yield return s.Temperature;
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
