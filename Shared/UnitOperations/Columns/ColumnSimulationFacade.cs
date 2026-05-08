using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Columns
{


    public enum ColumnStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    public class ColumnSimulationFacade : EquipmentFacade
    {
        // ==============================================================================
        // 1. ESTADO Y VARIABLES DEL EQUIPO
        // ==============================================================================
        public ColumnStateType State { get; set; } = ColumnStateType.Created;

        // --- Topología Estática (Condensador y Rehervidor) ---
        public StreamSimulationFacade? OverheadStream { get; private set; }
        public StreamSimulationFacade? BottomsStream { get; private set; }
        public StreamSimulationFacade? RefluxStream { get; private set; }
        public StreamSimulationFacade? ReboilerReturnStream { get; private set; }

        // --- Topología Dinámica (Alimentaciones y Extracciones Múltiples) ---
        public Dictionary<string, StreamSimulationFacade> Feeds { get; } = new();
        public Dictionary<string, StreamSimulationFacade> SideDraws { get; } = new();

        public ColumnSimulationFacade()
        {
            // Constructor vacío.
        }

     
        // ==============================================================================
        // 2. INTERFAZ DE USUARIO Y ESTADO VISUAL
        // ==============================================================================
        public override string StatusText => State switch
        {
            ColumnStateType.Created => "Ready",
            ColumnStateType.PartiallyConnected => "Underspecified",
            ColumnStateType.ReadyToCalculate => "Ready to Solve",
            ColumnStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            ColumnStateType.Created => "#CBD5E0",               // Gris
            ColumnStateType.PartiallyConnected => "#F6AD55",    // Naranja
            ColumnStateType.ReadyToCalculate => "#63B3ED",      // Azul
            ColumnStateType.Solved => "#34D399",                // Verde
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();

            // Aquí en el futuro puedes agregar indicadores como "Reflux Ratio", "Number of Stages", etc.
            result.Add(new ToolTipLegend("Feeds", Feeds.Count.ToString()));
            result.Add(new ToolTipLegend("Side Draws", SideDraws.Count.ToString()));

            return result;
        }

        // ==============================================================================
        // 3. TOPOLOGÍA Y CONEXIONES
        // ==============================================================================
        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            var stream = connectedFacade as StreamSimulationFacade;
            if (stream == null) return;

            // Puertos Estáticos
            if (portName == "Overhead") OverheadStream = stream;
            else if (portName == "Bottoms") BottomsStream = stream;
            else if (portName == "Reflux") RefluxStream = stream;
            else if (portName == "ReboilerReturn") ReboilerReturnStream = stream;

            // Puertos Dinámicos
            else if (portName.StartsWith("Feed"))
            {
                Feeds[portName] = stream;
            }
            else if (portName.StartsWith("SideDraw"))
            {
                SideDraws[portName] = stream;
            }
        }

        public override void DetachConnection(string portName)
        {
            // Puertos Estáticos
            if (portName == "Overhead") OverheadStream = null;
            else if (portName == "Bottoms") BottomsStream = null;
            else if (portName == "Reflux") RefluxStream = null;
            else if (portName == "ReboilerReturn") ReboilerReturnStream = null;

            // Puertos Dinámicos
            else if (portName.StartsWith("Feed") && Feeds.ContainsKey(portName))
            {
                Feeds.Remove(portName);
            }
            else if (portName.StartsWith("SideDraw") && SideDraws.ContainsKey(portName))
            {
                SideDraws.Remove(portName);
            }
        }

        // ==============================================================================
        // 4. MOTOR DE CÁLCULO (PLACEHOLDER)
        // ==============================================================================
        protected override void CalculatedEquipment()
        {
            // TODO: Implementar el cálculo riguroso (Matrices tridiagonales, balances MESH, perfiles de T y P)
            // Por ahora se mantiene vacío esperando la lógica histórica del ingeniero.

            State = ColumnStateType.ReadyToCalculate;
        }

        public override void BuildEquations(EquationSystem eqs)
        {
           
        }

        public override IEnumerable<INewVariable> GetSolverVariables()
        {
            return null!;
        }
    }
    public class ColumnSimulationFacade2 : EquipmentFacade2
    {
        public ColumnStateType State { get; set; } = ColumnStateType.Created;

        // 🔗 Conexiones estáticas
        public IStreamFacade? OverheadVapor { get; private set; }  // Vapor que sale por topo
        public IStreamFacade? Reflux { get; private set; }          // Líquido que retorna como reflujo
        public IStreamFacade? BottomsLiquid { get; private set; }   // Líquido que sale por fondo
        public IStreamFacade? ReboilerReturn { get; private set; }  // Vapor que retorna desde reboiler

        // 🔗 Conexiones dinámicas
        public Dictionary<string, IStreamFacade> Feeds { get; } = new();
        public Dictionary<string, IStreamFacade> SideDraws { get; } = new();

        // 🔹 Variables operativas (para especificar grados de libertad)
        public INewVariable<double> RefluxRatio { get; set; }      // R = L/D
        public INewVariable<double> BoilupRatio { get; set; }      // V/B

        public ColumnSimulationFacade2()
        {
            RefluxRatio = new NewControlledVariableDouble();
            RefluxRatio.OnExecuteSolver += ExecuteSolver;

            BoilupRatio = new NewControlledVariableDouble();
            BoilupRatio.OnExecuteSolver += ExecuteSolver;
        }

        public override string StatusText => State switch
        {
            ColumnStateType.Created => "Ready",
            ColumnStateType.PartiallyConnected => "Underspecified",
            ColumnStateType.ReadyToCalculate => "Ready to Solve",
            ColumnStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            ColumnStateType.Created => "#CBD5E0",
            ColumnStateType.PartiallyConnected => "#F6AD55",
            ColumnStateType.ReadyToCalculate => "#63B3ED",
            ColumnStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            var result = new List<ToolTipLegend>();
            if (RefluxRatio.IsEspecified) result.Add(new("R", $"{RefluxRatio.SolverValue:F2}"));
            if (BoilupRatio.IsEspecified) result.Add(new("V/B", $"{BoilupRatio.SolverValue:F2}"));
            return result;
        }

        public override void AttachConnection(string portName, IStreamFacade connectedFacade)
        {
            if (portName == "OverheadVapor") OverheadVapor = connectedFacade;
            else if (portName == "Reflux") Reflux = connectedFacade;
            else if (portName == "BottomsLiquid") BottomsLiquid = connectedFacade;
            else if (portName == "ReboilerReturn") ReboilerReturn = connectedFacade;
            else if (portName.StartsWith("Feed")) Feeds[portName] = connectedFacade;
            else if (portName.StartsWith("SideDraw")) SideDraws[portName] = connectedFacade;
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "OverheadVapor") OverheadVapor = null;
            else if (portName == "Reflux") Reflux = null;
            else if (portName == "BottomsLiquid") BottomsLiquid = null;
            else if (portName == "ReboilerReturn") ReboilerReturn = null;
            else if (Feeds.ContainsKey(portName)) Feeds.Remove(portName);
            else if (SideDraws.ContainsKey(portName)) SideDraws.Remove(portName);
        }

        //public override void BuildEquations(EquationSystem eqs)
        //{
        //    // 🔹 Validar conexiones mínimas
        //    if (OverheadVapor == null || BottomsLiquid == null || Feeds.Count == 0) return;

        //    // 🔥 1. BALANCE GLOBAL DE MASA: ΣF_feed = D + B + ΣSideDraws
        //    // Usamos flujos molares totales para el balance global
        //    var F_total_in = Feeds.Values.Select(f => f.MolarFlow).Where(v => v.IsEspecified).ToList();
        //    var D = OverheadVapor.MolarFlow;
        //    var B = BottomsLiquid.MolarFlow;
        //    var S = SideDraws.Values.Select(s => s.MolarFlow).Where(v => v.IsEspecified).ToList();

        //    if (F_total_in.Count > 0 && (D.IsEspecified || B.IsEspecified || S.Any()))
        //    {
        //        eqs.AddEquation(
        //            x =>
        //            {
        //                double sumIn = Feeds.Values.Sum(f => x[f.MolarFlow.Index]);
        //                double sumOut = x[D.Index] + x[B.Index] + SideDraws.Values.Sum(s => x[s.MolarFlow.Index]);
        //                return sumOut - sumIn;
        //            },
        //            EquationType.Model,
        //            "Column global mass balance"
        //        );
        //    }

        //    // 🔥 2. BALANCE POR COMPONENTE: Σ(zᵢ·F_feed) = xᵢ·D + yᵢ·B + Σ(xᵢ,s·S)
        //    // Asumimos que todos los streams tienen la misma lista de componentes (mismo orden)
        //    var firstFeed = Feeds.Values.FirstOrDefault(f => f.StreamComposition?.Value?.Components.Count > 0);
        //    if (firstFeed?.StreamComposition?.Value != null &&
        //        OverheadVapor.StreamComposition?.Value != null &&
        //        BottomsLiquid.StreamComposition?.Value != null)
        //    {
        //        var compsRef = firstFeed.StreamComposition.Value.Components;
        //        var compsD = OverheadVapor.StreamComposition.Value.Components;
        //        var compsB = BottomsLiquid.StreamComposition.Value.Components;

        //        for (int i = 0; i < compsRef.Count && i < compsD.Count && i < compsB.Count; i++)
        //        {
        //            eqs.AddEquation(
        //                x =>
        //                {
        //                    // Σ(zᵢ·F_feed)
        //                    double sumIn = Feeds.Values.Sum(f =>
        //                    {
        //                        var comps = f.StreamComposition?.Value?.Components;
        //                        return comps != null && i < comps.Count ?
        //                            comps[i].MolarFlowSolver.SolverValue : 0;
        //                    });

        //                    // xᵢ·D + yᵢ·B + Σ(xᵢ,s·S)
        //                    double sumOut = x[compsD[i].MolarFlowSolver.Index] +
        //                                   x[compsB[i].MolarFlowSolver.Index] +
        //                                   SideDraws.Values.Sum(s =>
        //                                   {
        //                                       var comps = s.StreamComposition?.Value?.Components;
        //                                       return comps != null && i < comps.Count ?
        //                                           x[comps[i].MolarFlowSolver.Index] : 0;
        //                                   });

        //                    return sumOut - sumIn;
        //                },
        //                EquationType.Model,
        //                $"Column component balance {compsRef[i].ComponentName}"
        //            );
        //        }
        //    }

        //    // 🔥 3. BALANCE DE ENERGÍA (Shortcut): Σ(H_feed·F_feed) = H_D·D + H_B·B + Q_cond - Q_reb
        //    // Para Fase 1: asumimos Q_cond y Q_reb como variables que pueden especificarse
        //    var H_in_sum = Feeds.Values.Sum(f => f.MolarEnthalpy.SolverValue * f.MolarFlow.SolverValue);
        //    var H_D = OverheadVapor.MolarEnthalpy;
        //    var H_B = BottomsLiquid.MolarEnthalpy;

        //    // Solo agregar si hay suficientes variables especificadas para que la ecuación sea útil
        //    if (Feeds.Values.All(f => f.MolarEnthalpy.IsEspecified && f.MolarFlow.IsEspecified) &&
        //        (H_D.IsEspecified || H_B.IsEspecified))
        //    {
        //        eqs.AddEquation(
        //            x =>
        //            {
        //                double H_in = Feeds.Values.Sum(f => x[f.MolarEnthalpy.Index] * x[f.MolarFlow.Index]);
        //                double H_out = x[H_D.Index] * x[OverheadVapor.MolarFlow.Index] +
        //                              x[H_B.Index] * x[BottomsLiquid.MolarFlow.Index];
        //                // Q_cond y Q_reb se manejan como variables separadas si se necesitan
        //                return H_out - H_in;
        //            },
        //            EquationType.Model,
        //            "Column energy balance (shortcut)"
        //        );
        //    }

        //    // 🔥 4. RELACIONES OPERATIVAS (Grados de libertad)
        //    // Reflux Ratio: R = L/D → L = R·D (Reflux.MolarFlow = RefluxRatio * OverheadVapor.MolarFlow)
        //    if (RefluxRatio.IsEspecified && Reflux?.MolarFlow != null && OverheadVapor?.MolarFlow != null)
        //    {
        //        eqs.AddEquation(
        //            x => x[Reflux.MolarFlow.Index] - (RefluxRatio.SolverValue * x[OverheadVapor.MolarFlow.Index]),
        //            EquationType.Model,
        //            "Column reflux ratio definition"
        //        );
        //    }

        //    // Boilup Ratio: V/B → V = (V/B)·B (ReboilerReturn.MolarFlow = BoilupRatio * BottomsLiquid.MolarFlow)
        //    if (BoilupRatio.IsEspecified && ReboilerReturn?.MolarFlow != null && BottomsLiquid?.MolarFlow != null)
        //    {
        //        eqs.AddEquation(
        //            x => x[ReboilerReturn.MolarFlow.Index] - (BoilupRatio.SolverValue * x[BottomsLiquid.MolarFlow.Index]),
        //            EquationType.Model,
        //            "Column boilup ratio definition"
        //        );
        //    }

        //    // 🔥 5. PRESIONES (Shortcut: misma presión en toda la columna, o ΔP especificado)
        //    // Por simplicidad Fase 1: P_top = P_bottom (sin gradiente)
        //    if (OverheadVapor?.Pressure != null && BottomsLiquid?.Pressure != null)
        //    {
        //        eqs.AddEquation(
        //            x => x[BottomsLiquid.Pressure.Index] - x[OverheadVapor.Pressure.Index],
        //            EquationType.Model,
        //            "Column pressure equilibrium (shortcut)"
        //        );
        //    }
        //}

        //public override IEnumerable<INewVariable> GetSolverVariables()
        //{
        //    // 🔹 Variables propias
        //    yield return RefluxRatio;
        //    yield return BoilupRatio;

        //    // 🔹 Streams estáticos
        //    foreach (var s in new[] { OverheadVapor, Reflux, BottomsLiquid, ReboilerReturn }.Where(s => s != null))
        //    {
        //        yield return s.Pressure;
        //        yield return s.MolarEnthalpy;
        //        yield return s.MolarFlow;
        //        if (s.StreamComposition?.Value?.Components != null)
        //            foreach (var c in s.StreamComposition.Value.Components)
        //                yield return c.MolarFlowSolver;
        //    }

        //    // 🔹 Feeds y SideDraws
        //    foreach (var s in Feeds.Values.Concat(SideDraws.Values))
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
