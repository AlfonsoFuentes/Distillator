using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.UnitOperations.Helpers
{


    public enum SplitterStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }



    public class SplitterSimulationFacade : EquipmentFacade
    {
        // ==============================================================================
        // 1. ESTADO Y VARIABLES DEL EQUIPO
        // ==============================================================================
        public SplitterStateType State { get; set; } = SplitterStateType.Created;

        // Topología
        public StreamSimulationFacade? InletStream { get; private set; }
        public Dictionary<string, StreamSimulationFacade> OutletStreams { get; } = new();

        // Fracciones de separación (Diccionario para soportar N salidas dinámicamente)
        public Dictionary<string, ControlledVariable<double>> SplitFractions { get; set; } = new();

        public SplitterSimulationFacade()
        {
            // Constructor vacío.
        }

       

        // ==============================================================================
        // 2. INTERFAZ DE USUARIO Y ESTADO VISUAL
        // ==============================================================================
        public override string StatusText => State switch
        {
            SplitterStateType.Created => "Ready",
            SplitterStateType.PartiallyConnected => "Underspecified",
            SplitterStateType.ReadyToCalculate => "Ready to Solve",
            SplitterStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            SplitterStateType.Created => "#CBD5E0",               // Gris
            SplitterStateType.PartiallyConnected => "#F6AD55",    // Naranja
            SplitterStateType.ReadyToCalculate => "#63B3ED",      // Azul
            SplitterStateType.Solved => "#34D399",                // Verde
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();
            foreach (var kvp in SplitFractions)
            {
                if (kvp.Value.IsDefined)
                    result.Add(new ToolTipLegend($"Frac {kvp.Key}", $"{kvp.Value.Value}%"));
                else
                    result.Add(new ToolTipLegend($"Frac {kvp.Key}", "<Not Defined>"));
            }
            return result;
        }

        // ==============================================================================
        // 3. TOPOLOGÍA Y CONEXIONES (Soporta N salidas)
        // ==============================================================================
        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            if (portName == "Inlet")
            {
                InletStream = connectedFacade as StreamSimulationFacade;
            }
            else if (portName.StartsWith("Outlet"))
            {
                OutletStreams[portName] = (StreamSimulationFacade)connectedFacade;

                if (!SplitFractions.ContainsKey(portName))
                {
                    // 🚩 NACEN VACÍAS: Sin valor por defecto para permitir grados de libertad
                   
                   
                }
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Inlet")
            {
                InletStream = null;
            }
            else if (OutletStreams.ContainsKey(portName))
            {
                OutletStreams.Remove(portName);
            }
        }

        public void SyncFractionsWithPorts(List<string> outletPortNames)
        {
            foreach (var name in outletPortNames)
            {
                if (!SplitFractions.ContainsKey(name))
                {
                    // 🚩 NACEN VACÍAS
                    
                }
            }

            var toRemove = SplitFractions.Keys.Except(outletPortNames).ToList();
            foreach (var key in toRemove)
            {
                SplitFractions.Remove(key);
            }
        }

        // ==============================================================================
        // 4. MOTOR DE CÁLCULO
        // ==============================================================================
        protected override void CalculatedEquipment()
        {
           
        }
        public override void BuildEquations(EquationSystem eqs)
        {

        }

        public override IEnumerable<INewVariable> GetSolverVariables()
        {
            return null!;
        }
        // 🌊 PISCINA INTENSIVA Y TERMODINÁMICA

    }
    public class SplitterSimulationFacade2 : EquipmentFacade2
    {
        public SplitterStateType State { get; set; } = SplitterStateType.Created;
        public IStreamFacade? InletStream { get; private set; }
        public Dictionary<string, IStreamFacade> OutletStreams { get; } = new();
        public Dictionary<string, INewVariable<double>> SplitFractions { get; } = new();

        public override string StatusText => State switch
        {
            SplitterStateType.Created => "Ready",
            SplitterStateType.PartiallyConnected => "Underspecified",
            SplitterStateType.ReadyToCalculate => "Ready to Solve",
            SplitterStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            SplitterStateType.Created => "#CBD5E0",
            SplitterStateType.PartiallyConnected => "#F6AD55",
            SplitterStateType.ReadyToCalculate => "#63B3ED",
            SplitterStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            var result = new List<ToolTipLegend>();
            foreach (var kvp in SplitFractions)
            {
                string displayName = kvp.Key.Replace("Outlet", "");
                result.Add(new($"Frac {displayName}",
                    kvp.Value.IsEspecified ? $"{kvp.Value.SolverValue:F3}" : "<Calculating>"));
            }
            return result;
        }

        public override void AttachConnection(string portName, IStreamFacade connectedFacade)
        {
            if (portName == "Inlet")
            {
                InletStream = connectedFacade;
            }
            else if (portName.StartsWith("Outlet"))
            {
                OutletStreams[portName] = connectedFacade;
                if (!SplitFractions.ContainsKey(portName))
                {
                    SplitFractions[portName] = new NewControlledVariableDouble();
                }
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Inlet") InletStream = null;
            else if (OutletStreams.ContainsKey(portName))
            {
                OutletStreams.Remove(portName);
                SplitFractions.Remove(portName);
            }
        }

        //public override void BuildEquations(EquationSystem eqs)
        //{
        //    if (InletStream == null || OutletStreams.Count == 0) return;

        //    // ====================================================================
        //    // 🔥 ECUACIÓN 1: BALANCE GLOBAL - F_in = Σⱼ F_outⱼ
        //    // ====================================================================
        //    if (InletStream.MolarFlow != null && OutletStreams.Values.All(o => o.MolarFlow != null))
        //    {
        //        eqs.AddEquation(
        //            x => OutletStreams.Values.Sum(outlet => x[outlet.MolarFlow.Index]) - x[InletStream.MolarFlow.Index],
        //            EquationType.Model,
        //            "Splitter global balance"
        //        );
        //    }

        //    // ====================================================================
        //    // 🔥 ECUACIÓN 2: COMPOSICIÓN IGUALITARIA - nᵢ_in/F_in = nᵢ_outⱼ/F_outⱼ
        //    // Reescrita como: nᵢ_in · F_outⱼ = nᵢ_outⱼ · F_in  (evita división)
        //    // ====================================================================
        //    if (InletStream.MolarFlow != null && InletStream.StreamComposition?.Value?.Components.Count > 0)
        //    {
        //        var compsIn = InletStream.StreamComposition.Value.Components;

        //        foreach (var outlet in OutletStreams.Values)
        //        {
        //            if (outlet.MolarFlow == null) continue;

        //            // 🔍 Verificación explícita para satisfacer al compilador
        //            var compOutVal = outlet.StreamComposition?.Value;
        //            if (compOutVal == null || compOutVal.Components.Count == 0) continue;

        //            var compsOut = compOutVal.Components;

        //            for (int i = 0; i < compsIn.Count && i < compsOut.Count; i++)
        //            {
        //                eqs.AddEquation(
        //                    x => x[compsIn[i].MolarFlowSolver.Index] * x[outlet.MolarFlow.Index] -
        //                         x[compsOut[i].MolarFlowSolver.Index] * x[InletStream.MolarFlow.Index],
        //                    EquationType.Model,
        //                    $"Splitter comp% {compsIn[i].ComponentName} -> {outlet.Name}"
        //                );
        //            }
        //        }
        //    }
        //    foreach (var outlet in OutletStreams.Values)
        //    {
        //        eqs.AddEquation(x => x[outlet.Temperature.Index] - x[InletStream.Temperature.Index],
        //            EquationType.Model, $"Splitter T {outlet.Name}");
        //        eqs.AddEquation(x => x[outlet.Pressure.Index] - x[InletStream.Pressure.Index],
        //            EquationType.Model, $"Splitter P {outlet.Name}");
        //    }
        //}

        //// ====================================================================
        //// 🔥 SOLVER VARIABLES - SOLO FLUJOS DE COMPONENTE + INTENSIVAS
        //// ====================================================================
        //public override IEnumerable<INewVariable> GetSolverVariables()
        //{
        //    // 🔹 Inlet: intensivas + flujo total + flujos de componente
        //    if (InletStream != null)
        //    {
        //        yield return InletStream.Temperature;
        //        yield return InletStream.Pressure;
        //        yield return InletStream.MolarFlow;
        //        if (InletStream.StreamComposition?.Value?.Components != null)
        //            foreach (var c in InletStream.StreamComposition.Value.Components)
        //                yield return c.MolarFlowSolver;
        //    }

        //    // 🔹 Outlets: intensivas + flujo total + flujos de componente
        //    foreach (var outlet in OutletStreams.Values)
        //    {
        //        yield return outlet.Temperature;
        //        yield return outlet.Pressure;
        //        yield return outlet.MolarFlow;
        //        if (outlet.StreamComposition?.Value?.Components != null)
        //            foreach (var c in outlet.StreamComposition.Value.Components)
        //                yield return c.MolarFlowSolver;
        //    }
        //}


    }
}
