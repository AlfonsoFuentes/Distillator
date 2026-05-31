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
    using Shared.MatrixSolvers;
    using Shared.ProcessFlowDiagram; // Ajusta según tu namespace
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum MixerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }

    //public class MixerSimulationFacade : EquipmentFacade
    //{
    //    // ==============================================================================
    //    // 1. ESTADO Y VARIABLES DEL EQUIPO
    //    // ==============================================================================
    //    public MixerStateType State { get; set; } = MixerStateType.Created;

    //    // Topología Inversa al Splitter: N Entradas, 1 Salida
    //    public Dictionary<string, StreamSimulationFacade> InletStreams { get; } = new();
    //    public StreamSimulationFacade? OutletStream { get; private set; }

    //    public MixerSimulationFacade()
    //    {
    //        // Constructor vacío.
    //    }

       

    //    // ==============================================================================
    //    // 2. INTERFAZ DE USUARIO Y ESTADO VISUAL
    //    // ==============================================================================
    //    public override string StatusText => State switch
    //    {
    //        MixerStateType.Created => "Ready",
    //        MixerStateType.PartiallyConnected => "Underspecified",
    //        MixerStateType.ReadyToCalculate => "Ready to Solve",
    //        MixerStateType.Solved => "Converged",
    //        _ => "Unknown"
    //    };

    //    public override string StatusColor => State switch
    //    {
    //        MixerStateType.Created => "#CBD5E0",               // Gris
    //        MixerStateType.PartiallyConnected => "#F6AD55",    // Naranja
    //        MixerStateType.ReadyToCalculate => "#63B3ED",      // Azul
    //        MixerStateType.Solved => "#34D399",                // Verde
    //        _ => "#CBD5E0"
    //    };

    //    public override List<ToolTipLegend> GetToolTipLegend()
    //    {
    //        // En un mixer, es útil mostrar en el tooltip la presión final o el flujo total
    //        List<ToolTipLegend> result = new();
    //        if (OutletStream != null && OutletStream.MassFlow.IsDefined)
    //        {
    //            result.Add(new ToolTipLegend("Total Flow", $"{Math.Round(OutletStream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr), 2)} kg/hr"));
    //        }
    //        return result;
    //    }

    //    // ==============================================================================
    //    // 3. TOPOLOGÍA Y CONEXIONES (Soporta N Entradas)
    //    // ==============================================================================
    //    public override void AttachConnection(string portName, IFacade connectedFacade)
    //    {
    //        if (portName.StartsWith("Inlet"))
    //        {
    //            InletStreams[portName] = (StreamSimulationFacade)connectedFacade;
    //        }
    //        else if (portName == "Outlet")
    //        {
    //            OutletStream = connectedFacade as StreamSimulationFacade;
    //        }
    //    }

    //    public override void DetachConnection(string portName)
    //    {
    //        if (portName.StartsWith("Inlet") && InletStreams.ContainsKey(portName))
    //        {
    //            InletStreams.Remove(portName);
    //        }
    //        else if (portName == "Outlet")
    //        {
    //            OutletStream = null;
    //        }
    //    }

    //    // ==============================================================================
    //    // 4. MOTOR DE CÁLCULO
    //    // ==============================================================================
    //    protected override void CalculatedEquipment()
    //    {
    //        var allStreams = new List<StreamSimulationFacade>(InletStreams.Values);
    //        if (OutletStream != null) allStreams.Add(OutletStream);

    //        if (OutletStream == null || InletStreams.Count == 0)
    //        {
    //            State = MixerStateType.PartiallyConnected;
    //            return;
    //        }

    //        // 1. PROPAGAR TERMODINÁMICA (No requiere flujos)
    //        bool hasThermo = PropagateThermodynamicMethod(allStreams);
    //        if (!hasThermo)
    //        {
    //            State = MixerStateType.PartiallyConnected;
    //            return; // Si no hay termo, abortamos misión aquí mismo.
    //        }

    //        // 2. BALANCE DE MASA (La Masa manda)
    //        bool flowsOk = SolveMassBalance();

    //        // 3. BALANCE DE ENERGÍA Y PRESIÓN (Requiere flujos resueltos)
    //        bool intensiveOk = false;
    //        if (flowsOk)
    //        {
    //            intensiveOk = SolveEnergyAndPressure();
    //        }

         

    //        if (intensiveOk && flowsOk)
    //            State = MixerStateType.Solved;
    //        else
    //            State = MixerStateType.ReadyToCalculate;
    //    }
    //    // ==============================================================================
    //    // 🌡️ PASO 1: CONTAGIO TERMODINÁMICO
    //    // ==============================================================================
    //    private bool PropagateThermodynamicMethod(List<StreamSimulationFacade> allStreams)
    //    {
    //        // Buscamos si alguna de las corrientes conectadas tiene la termodinámica definida
    //        var masterThermo = allStreams.FirstOrDefault(s => s.ThermodynamicMethod != null && s.ThermodynamicMethod.IsDefined)?.ThermodynamicMethod.Value;

    //        if (masterThermo != null)
    //        {
    //            foreach (var stream in allStreams)
    //            {
    //                if (stream.ThermodynamicMethod != null && !stream.ThermodynamicMethod.IsDefined)
    //                {
    //                    stream.ThermodynamicMethod.SetValue(masterThermo, MethodSource.Other, Name);
    //                    this.AddCalculatedVariable(stream.ThermodynamicMethod);
    //                }
    //            }
    //            return true; // Éxito, tenemos termodinámica
    //        }

    //        return false; // Fracaso, nadie trajo el paquete termodinámico
    //    }

    //    // ==============================================================================
    //    // 🌊 PASO 3: CHOQUE TÉRMICO Y PRESIÓN (Requiere que SolveMassBalance haya sido exitoso)
    //    // ==============================================================================
    //    private bool SolveEnergyAndPressure()
    //    {
    //        if (OutletStream == null || !OutletStream.MassFlow.IsDefined) return false;

    //        double outMassFlow = OutletStream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
    //        if (outMassFlow <= 0) return false;

    //        // A. BALANCE DE PRESIÓN (La menor de todas las entradas definidas)
    //        var definedPressures = InletStreams.Values.Where(s => s.Pressure.IsDefined).Select(s => s.Pressure.GetValueInUnit(PressureUnits.Bara)).ToList();
    //        if (definedPressures.Any() && !OutletStream.Pressure.IsDefined)
    //        {
    //            double minPressure = definedPressures.Min();
    //            OutletStream.Pressure.SetValue(new Pressure(minPressure, PressureUnits.Bara), MethodSource.Other, Name);
    //            this.AddCalculatedVariable(OutletStream.Pressure);
    //        }

    //        // B. BALANCE DE ENERGÍA (Promedio Ponderado de Entalpías)
    //        if (InletStreams.Values.All(s => s.MolarEnthalpy.IsDefined && s.MassFlow.IsDefined))
    //        {
    //            if (!OutletStream.MolarEnthalpy.IsDefined)
    //            {
    //                double totalEnergy = 0;
    //                foreach (var inlet in InletStreams.Values)
    //                {
    //                    double m = inlet.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
    //                    double h = inlet.MolarEnthalpy.GetValueInUnit(MolarEnergyUnits.KJ_Kgmol);
    //                    totalEnergy += (m * h); // (Asegúrate de ajustar dimensiones si decides usar MolarFlow)
    //                }

    //                double mixEnthalpy = totalEnergy / outMassFlow;
    //                OutletStream.MolarEnthalpy.SetValue(new MolarEnergy(mixEnthalpy, MolarEnergyUnits.KJ_Kgmol), MethodSource.Other, Name);
    //                this.AddCalculatedVariable(OutletStream.MolarEnthalpy);
    //            }
    //        }

    //        return OutletStream.Pressure.IsDefined && OutletStream.MolarEnthalpy.IsDefined;
    //    }
    //    // ==============================================================================
    //    // ⚖️ MOTOR ITERATIVO DE MASA (Mixer)
    //    // ==============================================================================
    //    private bool SolveMassBalance()
    //    {
    //        if (OutletStream == null) return false;

    //        // REGLA 1: FORWARD COMPLETO (Sumar todas las entradas)
    //        if (InletStreams.Values.All(s => s.MassFlow.IsDefined))
    //        {
    //            double sumInlets = InletStreams.Values.Sum(s => s.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr));

    //            if (!OutletStream.MassFlow.IsDefined)
    //            {
    //                OutletStream.MassFlow.SetValue(new MassFlow(sumInlets, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
    //                this.AddCalculatedVariable(OutletStream.MassFlow);
    //            }
    //            return true;
    //        }

    //        // REGLA 2: BACKWARD PARCIAL (Conozco la Salida y me falta exactamente UNA Entrada)
    //        if (OutletStream.MassFlow.IsDefined)
    //        {
    //            var unknownInlets = InletStreams.Values.Where(s => !s.MassFlow.IsDefined).ToList();
    //            if (unknownInlets.Count == 1)
    //            {
    //                double outMass = OutletStream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
    //                double sumKnownInlets = InletStreams.Values.Where(s => s.MassFlow.IsDefined)
    //                                                           .Sum(s => s.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr));

    //                double missingMass = Math.Max(0, outMass - sumKnownInlets); // Evitar negativos por redondeo

    //                unknownInlets[0].MassFlow.SetValue(new MassFlow(missingMass, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
    //                this.AddCalculatedVariable(unknownInlets[0].MassFlow);

    //                return true;
    //            }
    //        }

    //        return false; // Sub-especificado, le falta "gasolina" al motor
    //    }

    //    // ==============================================================================
    //    // 🌊 CHOQUE TERMODINÁMICO (Presión, Energía y Termodinámica)
    //    // ==============================================================================
    //    private bool PropagateIntensiveProperties()
    //    {
    //        if (OutletStream == null || !OutletStream.MassFlow.IsDefined) return false;

    //        double outMassFlow = OutletStream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
    //        if (outMassFlow <= 0) return false;

    //        // --- PREPARAR LISTA DE TODAS LAS CORRIENTES ---
    //        var allStreams = new List<StreamSimulationFacade>(InletStreams.Values);
    //        allStreams.Add(OutletStream);

    //        // 1. 🚩 HEREDAR TERMODINÁMICA A TODAS LAS CORRIENTES
    //        var masterThermo = allStreams.FirstOrDefault(s => s.ThermodynamicMethod != null && s.ThermodynamicMethod.IsDefined)?.ThermodynamicMethod.Value;

    //        if (masterThermo != null)
    //        {
    //            foreach (var stream in allStreams)
    //            {
    //                if (stream.ThermodynamicMethod != null && !stream.ThermodynamicMethod.IsDefined)
    //                {
    //                    stream.ThermodynamicMethod.SetValue(masterThermo, MethodSource.Other, Name);
    //                    this.AddCalculatedVariable(stream.ThermodynamicMethod);
    //                }
    //            }
    //        }

    //        // 2. 🚩 BALANCE DE PRESIÓN (La menor de todas las entradas definidas)
    //        var definedPressures = InletStreams.Values.Where(s => s.Pressure.IsDefined).Select(s => s.Pressure.GetValueInUnit(PressureUnits.Bara)).ToList();
    //        if (definedPressures.Any() && !OutletStream.Pressure.IsDefined)
    //        {
    //            double minPressure = definedPressures.Min();
    //            OutletStream.Pressure.SetValue(new Pressure(minPressure, PressureUnits.Bara), MethodSource.Other, Name);
    //            this.AddCalculatedVariable(OutletStream.Pressure);
    //        }

    //        // 3. 🚩 BALANCE DE ENERGÍA (Promedio Ponderado de Entalpías)
    //        // ⚠️ Nota dimensional: Para que sea estrictamente correcto, 
    //        // considera usar MolarFlow si la entalpía es MolarEnthalpy.
    //        if (InletStreams.Values.All(s => s.MolarEnthalpy.IsDefined && s.MassFlow.IsDefined))
    //        {
    //            if (!OutletStream.MolarEnthalpy.IsDefined)
    //            {
    //                double totalEnergy = 0;
    //                foreach (var inlet in InletStreams.Values)
    //                {
    //                    double m = inlet.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
    //                    double h = inlet.MolarEnthalpy.GetValueInUnit(MolarEnergyUnits.KJ_Kgmol);
    //                    totalEnergy += (m * h);
    //                }

    //                double mixEnthalpy = totalEnergy / outMassFlow;
    //                OutletStream.MolarEnthalpy.SetValue(new MolarEnergy(mixEnthalpy, MolarEnergyUnits.KJ_Kgmol), MethodSource.Other, Name);
    //                this.AddCalculatedVariable(OutletStream.MolarEnthalpy);
    //            }
    //        }

    //        return OutletStream.Pressure.IsDefined && OutletStream.MolarEnthalpy.IsDefined;
    //    }
    //    public override void BuildEquations(EquationSystem eqs)
    //    {

    //    }

    //    public override IEnumerable<INewVariable> GetSolverVariables()
    //    {
    //        return null!;
    //    }
    //}
    public class MixerSimulationFacade2 : EquipmentFacade2
    {
        public MixerStateType State { get; set; } = MixerStateType.Created;
        public Dictionary<string, IStreamFacade2> InletStreams { get; } = new();
        public IStreamFacade2? OutletStream { get; private set; }

        public override string StatusText => State switch
        {
            MixerStateType.Created => "Ready",
            MixerStateType.PartiallyConnected => "Underspecified",
            MixerStateType.ReadyToCalculate => "Ready to Solve",
            MixerStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            MixerStateType.Created => "#CBD5E0",
            MixerStateType.PartiallyConnected => "#F6AD55",
            MixerStateType.ReadyToCalculate => "#63B3ED",
            MixerStateType.Solved => "#34D399",
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            var result = new List<ToolTipLegend>();
            if (OutletStream?.MassFlow?.IsDefined == true)
                result.Add(new("Total Flow", OutletStream.MassFlow.Value?.ToString() ?? string.Empty));
            return result;
        }

        public override void AttachConnection(string portName, IStreamFacade2 connectedFacade)
        {
            if (portName.StartsWith("Inlet")) InletStreams[portName] = connectedFacade;
            else if (portName == "Outlet") OutletStream = connectedFacade;
        }

        public override void DetachConnection(string portName)
        {
            if (portName.StartsWith("Inlet")) InletStreams.Remove(portName);
            else if (portName == "Outlet") OutletStream = null;
        }

        //public override void BuildEquations(EquationSystem eqs)
        //{
        //    if (OutletStream == null || InletStreams.Count == 0) return;

        //    // 🔥 Balance por componente (sin redundancia)
        //    if (OutletStream.StreamComposition?.Value?.Components.Count > 0)
        //    {
        //        foreach (var inlet in InletStreams.Values.Where(s => s.StreamComposition?.Value != null))
        //        {
        //            var compsIn = inlet.StreamComposition.Value.Components;
        //            var compsOut = OutletStream.StreamComposition.Value.Components;
        //            for (int i = 0; i < compsIn.Count && i < compsOut.Count; i++)
        //            {
        //                eqs.AddEquation(
        //                    x => x[compsOut[i].MolarFlowSolver.Index] - x[compsIn[i].MolarFlowSolver.Index],
        //                    EquationType.Model,
        //                    $"Mixer component balance {compsIn[i].ComponentName}"
        //                );
        //            }
        //        }
        //    }

        //    // 🔥 Balance de energía: H_out = Σ(mᵢ·Hᵢ) / Σmᵢ (ponderado por flujo másico)
        //    var Hout = OutletStream.MolarEnthalpy;
        //    eqs.AddEquation(
        //        x =>
        //        {
        //            double totalMass = 0, totalEnergy = 0;
        //            foreach (var inlet in InletStreams.Values)
        //            {
        //                if (inlet.MassFlow?.IsEspecified == true && inlet.MolarEnthalpy?.IsEspecified == true)
        //                {
        //                    double m = inlet.MassFlow.SolverValue;
        //                    double h = x[inlet.MolarEnthalpy.Index];
        //                    totalMass += m;
        //                    totalEnergy += m * h;
        //                }
        //            }
        //            return totalMass > 0 ? x[Hout.Index] - (totalEnergy / totalMass) : 0;
        //        },
        //        EquationType.Model,
        //        "Mixer energy balance"
        //    );

        //    // 🔥 Presión de salida = mínima presión de entrada definida
        //    var Pout = OutletStream.Pressure;
        //    eqs.AddEquation(
        //        x =>
        //        {
        //            var definedP = InletStreams.Values
        //                .Where(s => s.Pressure?.IsEspecified == true)
        //                .Select(s => x[s.Pressure.Index])
        //                .DefaultIfEmpty(1e5); // fallback 1 bar
        //            return x[Pout.Index] - definedP.Min();
        //        },
        //        EquationType.Model,
        //        "Mixer pressure balance"
        //    );
        //}

        //public override IEnumerable<INewVariable> GetSolverVariables()
        //{
        //    if (OutletStream != null)
        //    {
        //        yield return OutletStream.Pressure;
        //        yield return OutletStream.MolarEnthalpy;
        //        if (OutletStream.StreamComposition?.Value?.Components != null)
        //            foreach (var c in OutletStream.StreamComposition.Value.Components)
        //                yield return c.MolarFlowSolver;
        //    }
        //    foreach (var inlet in InletStreams.Values)
        //    {
        //        yield return inlet.Pressure;
        //        yield return inlet.MolarEnthalpy;
        //        if (inlet.StreamComposition?.Value?.Components != null)
        //            foreach (var c in inlet.StreamComposition.Value.Components)
        //                yield return c.MolarFlowSolver;
        //    }
        //}
    }
}