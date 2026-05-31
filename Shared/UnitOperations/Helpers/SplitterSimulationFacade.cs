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



    //public class SplitterSimulationFacade : EquipmentFacade
    //{
    //    // ==============================================================================
    //    // 1. ESTADO Y VARIABLES DEL EQUIPO
    //    // ==============================================================================
    //    public SplitterStateType State { get; set; } = SplitterStateType.Created;

    //    // Topología
    //    public StreamSimulationFacade? InletStream { get; private set; }
    //    public Dictionary<string, StreamSimulationFacade> OutletStreams { get; } = new();

    //    // Fracciones de separación (Diccionario para soportar N salidas dinámicamente)
    //    public Dictionary<string, ControlledVariable<double>> SplitFractions { get; set; } = new();

    //    public SplitterSimulationFacade()
    //    {
    //        // Constructor vacío.
    //    }

       

    //    // ==============================================================================
    //    // 2. INTERFAZ DE USUARIO Y ESTADO VISUAL
    //    // ==============================================================================
    //    public override string StatusText => State switch
    //    {
    //        SplitterStateType.Created => "Ready",
    //        SplitterStateType.PartiallyConnected => "Underspecified",
    //        SplitterStateType.ReadyToCalculate => "Ready to Solve",
    //        SplitterStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override string StatusColor => State switch
    //    {
    //        SplitterStateType.Created => "#CBD5E0",               // Gris
    //        SplitterStateType.PartiallyConnected => "#F6AD55",    // Naranja
    //        SplitterStateType.ReadyToCalculate => "#63B3ED",      // Azul
    //        SplitterStateType.Solved => "#34D399",                // Verde
    //        _ => "#CBD5E0"
    //    };

    //    public override List<ToolTipLegend> GetToolTipLegend()
    //    {
    //        List<ToolTipLegend> result = new();
    //        foreach (var kvp in SplitFractions)
    //        {
    //            if (kvp.Value.IsDefined)
    //                result.Add(new ToolTipLegend($"Frac {kvp.Key}", $"{kvp.Value.Value}%"));
    //            else
    //                result.Add(new ToolTipLegend($"Frac {kvp.Key}", "<Not Defined>"));
    //        }
    //        return result;
    //    }

    //    // ==============================================================================
    //    // 3. TOPOLOGÍA Y CONEXIONES (Soporta N salidas)
    //    // ==============================================================================
    //    public override void AttachConnection(string portName, IFacade connectedFacade)
    //    {
    //        if (portName == "Inlet")
    //        {
    //            InletStream = connectedFacade as StreamSimulationFacade;
    //        }
    //        else if (portName.StartsWith("Outlet"))
    //        {
    //            OutletStreams[portName] = (StreamSimulationFacade)connectedFacade;

    //            if (!SplitFractions.ContainsKey(portName))
    //            {
    //                // 🚩 NACEN VACÍAS: Sin valor por defecto para permitir grados de libertad
                   
                   
    //            }
    //        }
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName == "Inlet")
    //        {
    //            InletStream = null;
    //        }
    //        else if (OutletStreams.ContainsKey(portName))
    //        {
    //            OutletStreams.Remove(portName);
    //        }
    //    }

    //    public void SyncFractionsWithPorts(List<string> outletPortNames)
    //    {
    //        foreach (var name in outletPortNames)
    //        {
    //            if (!SplitFractions.ContainsKey(name))
    //            {
    //                // 🚩 NACEN VACÍAS
                    
    //            }
    //        }

    //        var toRemove = SplitFractions.Keys.Except(outletPortNames).ToList();
    //        foreach (var key in toRemove)
    //        {
    //            SplitFractions.Remove(key);
    //        }
    //    }

    //    // ==============================================================================
    //    // 4. MOTOR DE CÁLCULO
    //    // ==============================================================================
    //    protected override void CalculatedEquipment()
    //    {
           
    //    }
    //    public override void BuildEquations(EquationSystem eqs)
    //    {

    //    }

    //    public override IEnumerable<INewVariable> GetSolverVariables()
    //    {
    //        return null!;
    //    }
    //    // 🌊 PISCINA INTENSIVA Y TERMODINÁMICA

    //}
    public class SplitterSimulationFacade2 : EquipmentFacade2
    {
        public SplitterStateType State { get; set; } = SplitterStateType.Created;
        public IStreamFacade2? InletStream { get; private set; }
        public Dictionary<string, IStreamFacade2> OutletStreams { get; } = new();
        public Dictionary<string, NewNewVariableDouble> SplitFractions { get; } = new();

        // =========================
        // 🔹 CONSTRUCTOR
        // =========================
        public SplitterSimulationFacade2()
        {
            // Las fracciones se crean dinámicamente en AttachConnection
        }

        // =========================
        // 🔹 ECUACIONES DEL EQUIPO - PROPAGACIÓN LOCAL
        // =========================

        // Campos persistentes para EquationSystem (patrón bomba/válvula)
        EquationSystem eqConc = new EquationSystem();
        EquationSystem eqMolarFlow = new EquationSystem();
        EquationSystem eqPressure = new EquationSystem();

        // 🔹 Propagación de Concentración (x_out[k] = x_in para TODOS los outlets)
        private void OnPropagateConcentrations()
        {
            if (InletStream == null || OutletStreams.Count == 0) return;
            eqConc = GetEquationConcentration();
            eqConc.SolveEquipmet();
        }

        public override EquationSystem GetEquationConcentration()
        {
            EquationSystem eq = new EquationSystem();
            if (InletStream == null || OutletStreams.Count == 0) return eq;

            eq.AddVariables(GetConcentrationVariables());
            var compsIn = InletStream.StreamComposition.Value.Components;

            // 🔥 Para CADA outlet: x_out[k][i] = x_in[i]
            foreach (var outletKvp in OutletStreams)
            {
                string outletName = outletKvp.Key;
                var outlet = outletKvp.Value;
                var compsOut = outlet.StreamComposition.Value.Components;

                for (int i = 0; i < compsIn.Count && i < compsOut.Count; i++)
                {
                    var ni_in = compsIn[i].MolarFractionSolver;
                    var ni_out = compsOut[i].MolarFractionSolver;

                    eq.AddEquation(new Equation
                    {
                        Function = x => x[ni_out.Index] - x[ni_in.Index],
                        Type = EquationType.Model
                    });
                }
            }
            return eq;
        }

        // 🔹 Propagación de Flujo Molar (F_out[k] = α[k] * F_in)
        private void OnPropagateMolarFlow()
        {
            if (InletStream == null || OutletStreams.Count == 0) return;
            eqMolarFlow.Clear();
            eqMolarFlow.AddVariables(GetMassBalanceVariables());

            // 🔥 Para CADA outlet: F_out[k] = α[k] * F_in
            foreach (var outletKvp in OutletStreams)
            {
                string outletName = outletKvp.Key;
                var outlet = outletKvp.Value;

                if (!SplitFractions.TryGetValue(outletName, out var fraction))
                    continue;  // Sin fracción definida, skip

                double alpha = fraction.Value;

                eqMolarFlow.AddEquation(new Equation
                {
                    Function = x => x[outlet.MolarFlow.Index] - (alpha * x[InletStream.MolarFlow.Index]),
                    Type = EquationType.Model
                });
            }

            eqMolarFlow.SolveEquipmet();
        }

        // 🔹 Propagación de Presión (P_out[k] = P_in para splitter ideal)
        private void OnPropagatePressure()
        {
            if (InletStream == null || OutletStreams.Count == 0) return;
            eqPressure = GetEquationPressure();
            eqPressure.SolveEquipmet();
        }

        public override EquationSystem GetEquationPressure()
        {
            EquationSystem eq = new EquationSystem();
            if (InletStream == null || OutletStreams.Count == 0) return eq;

            eq.AddVariables(GetPressureVariables());
            var Pin = InletStream.Pressure;

            // 🔥 Para CADA outlet: P_out[k] = P_in
            foreach (var outletKvp in OutletStreams)
            {
                var outlet = outletKvp.Value;
                var Pout = outlet.Pressure;

                eq.AddEquation(new Equation
                {
                    Function = x => x[Pout.Index] - x[Pin.Index],
                    Type = EquationType.Model
                });
            }

            return eq;
        }

        // =========================
        // 🔹 BALANCE GLOBAL (Masa/Energía) - Para SolverMatrixManager
        // =========================

        public override EquationSystem GetEquationSystem()
        {
            EquationSystem equationSystem = new EquationSystem();
            if (InletStream == null || OutletStreams.Count == 0) return equationSystem;

            equationSystem.AddVariables(GetEnergyBalanceVariables());

            // 🔥 BALANCE GLOBAL DE FLUJO: F_in = Σ F_out[k]
            var inletFlow = InletStream.MolarFlow;
            var outletFlows = OutletStreams.Values.Select(o => o.MolarFlow).ToList();

            equationSystem.AddEquation(new Equation
            {
                Function = x => x[inletFlow.Index] - outletFlows.Sum(f => x[f.Index]),
                Type = EquationType.Model
            });

            // 🔥 PRESIÓN: P_out[k] = P_in para cada outlet
            var Pin = InletStream.Pressure;
            foreach (var outletKvp in OutletStreams)
            {
                var Pout = outletKvp.Value.Pressure;
                equationSystem.AddEquation(new Equation
                {
                    Function = x => x[Pout.Index] - x[Pin.Index],
                    Type = EquationType.Model
                });
            }

            // 🔥 ENERGÍA: H_out[k] = H_in para cada outlet (isotérmico/isentálpico)
            var Hin = InletStream.MolarEnthalpy;
            foreach (var outletKvp in OutletStreams)
            {
                var Hout = outletKvp.Value.MolarEnthalpy;
                equationSystem.AddEquation(new Equation
                {
                    Function = x => x[Hout.Index] - x[Hin.Index],
                    Type = EquationType.Model
                });
            }

            return equationSystem;
        }

        // =========================
        // 🔹 MÉTODOS AUXILIARES PARA OBTENER VARIABLES
        // =========================

        IEnumerable<INewNewVariable> GetPressureVariables()
        {
            if (InletStream != null) yield return InletStream.Pressure;
            foreach (var outlet in OutletStreams.Values)
                yield return outlet.Pressure;
        }

        IEnumerable<INewNewVariable> GetConcentrationVariables()
        {
            if (InletStream != null)
                foreach (var comp in InletStream.StreamComposition.Value.Components)
                    yield return comp.MolarFractionSolver;

            foreach (var outlet in OutletStreams.Values)
                foreach (var comp in outlet.StreamComposition.Value.Components)
                    yield return comp.MolarFractionSolver;
        }

        IEnumerable<INewNewVariable> GetMassBalanceVariables()
        {
            if (InletStream != null) yield return InletStream.MolarFlow;
            foreach (var outlet in OutletStreams.Values)
                yield return outlet.MolarFlow;
        }

        public IEnumerable<INewNewVariable> GetEnergyBalanceVariables()
        {
            if (InletStream != null)
            {
                yield return InletStream.MolarFlow;
                yield return InletStream.MolarEnthalpy;
            }
            foreach (var outlet in OutletStreams.Values)
            {
                yield return outlet.MolarFlow;
                yield return outlet.MolarEnthalpy;
            }
        }

        // =========================
        // 🔹 CÁLCULO LOCAL DE PARÁMETROS
        // =========================

        private void CalculateSplitParameters()
        {
            if (InletStream == null || OutletStreams.Count == 0) return;

            // 🔹 Calcular fracciones reales si los flujos están definidos
            if (InletStream.MolarFlow.IsDefined)
            {
                double F_in = InletStream.MolarFlow.SolverValue;
                if (F_in <= 0) return;

                foreach (var outletKvp in OutletStreams)
                {
                    string outletName = outletKvp.Key;
                    var outlet = outletKvp.Value;

                    if (outlet.MolarFlow.IsDefined)
                    {
                        double F_out = outlet.MolarFlow.SolverValue;
                        double alpha_calc = F_out / F_in;

                        // Actualizar fracción si existe
                        if (SplitFractions.TryGetValue(outletName, out var fraction))
                        {
                            // fraction.SetValueFromEquipmentSolver(alpha_calc); // Opcional
                        }
                    }
                }
            }
        }

        // =========================
        // 🔹 CONEXIONES (Dinámicas para N outlets)
        // =========================

        public override void AttachConnection(string portName, IStreamFacade2 connectedFacade)
        {
            if (portName == "Inlet")
            {
                if (InletStream == null)
                {
                    InletStream = connectedFacade;

                    // Suscribirse a eventos de propagación
                    InletStream.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                    InletStream.StreamComposition.ExecuteEquipmentSolver += OnPropagateConcentrations;
                    InletStream.MolarFlow.ExecuteEquipmentSolver -= OnPropagateMolarFlow;
                    InletStream.MolarFlow.ExecuteEquipmentSolver += OnPropagateMolarFlow;
                    InletStream.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;
                    InletStream.Pressure.ExecuteEquipmentSolver += OnPropagatePressure;

                    // Disparar propagación inicial
                    OnPropagateConcentrations();
                    OnPropagateMolarFlow();
                    OnPropagatePressure();
                }
            }
            else if (portName.StartsWith("Outlet"))
            {
                if (!OutletStreams.ContainsKey(portName))
                {
                    OutletStreams[portName] = connectedFacade;

                    // Crear fracción de split si no existe
                    if (!SplitFractions.ContainsKey(portName))
                    {
                        SplitFractions[portName] = new NewNewVariableDouble(1.0 / (OutletStreams.Count));  // Distribución inicial equitativa
                        SplitFractions[portName].ExecuteGeneralSolver += ExecuteSolver;
                        SplitFractions[portName].ExecuteStreamCalculation += CalculateSplitParameters;
                        SplitFractions[portName].ExecuteEquipmentSolver += OnPropagateMolarFlow;
                    }

                    var outlet = OutletStreams[portName];

                    // Suscribirse a eventos de propagación para ESTE outlet
                    outlet.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                    outlet.StreamComposition.ExecuteEquipmentSolver += OnPropagateConcentrations;
                    outlet.MolarFlow.ExecuteEquipmentSolver -= OnPropagateMolarFlow;
                    outlet.MolarFlow.ExecuteEquipmentSolver += OnPropagateMolarFlow;
                    outlet.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;
                    outlet.Pressure.ExecuteEquipmentSolver += OnPropagatePressure;

                    // Disparar propagación inicial
                    OnPropagateConcentrations();
                    OnPropagateMolarFlow();
                    OnPropagatePressure();
                }
            }
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Inlet")
            {
                InletStream?.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                InletStream?.MolarFlow.ExecuteEquipmentSolver -= OnPropagateMolarFlow;
                InletStream?.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;

                OnPropagateConcentrations();
                OnPropagateMolarFlow();
                OnPropagatePressure();

                InletStream = null;
            }
            else if (OutletStreams.ContainsKey(portName))
            {
                var outlet = OutletStreams[portName];

                outlet?.StreamComposition.ExecuteEquipmentSolver -= OnPropagateConcentrations;
                outlet?.MolarFlow.ExecuteEquipmentSolver -= OnPropagateMolarFlow;
                outlet?.Pressure.ExecuteEquipmentSolver -= OnPropagatePressure;

                OnPropagateConcentrations();
                OnPropagateMolarFlow();
                OnPropagatePressure();

                OutletStreams.Remove(portName);
                if (SplitFractions.ContainsKey(portName))
                {
                    SplitFractions.Remove(portName);
                }
            }
        }

        // =========================
        // 🔹 UI: ESTADO Y TOOLTIP
        // =========================

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
                    kvp.Value.IsDefined ? $"{kvp.Value.Value:P1}" : "<Not Defined>"));
            }
            return result;
        }
    }
}
