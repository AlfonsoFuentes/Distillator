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

        private void EvaluateSolverTrigger()
        {
            OnExecuteSolver?.Invoke(this);
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
                    var newFraction = new ControlledVariable<double>();
                    newFraction.OnExecuteSolver += EvaluateSolverTrigger;
                    newFraction.AddCalculatedVariable = this.AddCalculatedVariable;
                    SplitFractions[portName] = newFraction;
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
                    var newFraction = new ControlledVariable<double>();
                    newFraction.OnExecuteSolver += EvaluateSolverTrigger;
                    newFraction.AddCalculatedVariable = this.AddCalculatedVariable;
                    SplitFractions[name] = newFraction;
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
            var allStreams = new List<StreamSimulationFacade>();
            if (InletStream != null) allStreams.Add(InletStream);
            allStreams.AddRange(OutletStreams.Values);

            if (InletStream == null || OutletStreams.Count == 0)
            {
                State = SplitterStateType.PartiallyConnected;
                return;
            }

            // 🚩 ALCABALA ESTRICTA: Sin termodinámica no se calcula NADA
            var hasThermo = allStreams.Any(s => s.ThermodynamicMethod != null && s.ThermodynamicMethod.IsDefined);
            if (!hasThermo)
            {
                State = SplitterStateType.PartiallyConnected;
                return;
            }

            // 1. PROPAGACIÓN INTENSIVA: Repartir variables intensivas y termodinámica
            bool intensiveOk = PropagateIntensiveProperties(allStreams);

            // 2. MOTOR ITERATIVO DE MASA: Resuelve flujos y fracciones sin importar el orden
            bool flowsOk = SolveMassBalance();

            // 3. CÁLCULO FINAL: Cada corriente se calcula a sí misma internamente (Densidad, Entalpía, etc.)
            foreach (var s in allStreams) s.Calculate();

            if (intensiveOk && flowsOk)
            {
                State = SplitterStateType.Solved;
            }
            else
            {
                State = SplitterStateType.ReadyToCalculate;
            }
        }

        // 🌊 PISCINA INTENSIVA Y TERMODINÁMICA
        private bool PropagateIntensiveProperties(List<StreamSimulationFacade> allStreams)
        {
            var masterT = allStreams.FirstOrDefault(s => s.Temperature.IsDefined)?.Temperature.Value;
            var masterP = allStreams.FirstOrDefault(s => s.Pressure.IsDefined)?.Pressure.Value;
            var masterComp = allStreams.FirstOrDefault(s => s.StreamComposition.IsDefined)?.StreamComposition;
            var masterVapFrac = allStreams.FirstOrDefault(s => s.VaporFraction.IsDefined)?.VaporFraction;
            var masterEnthalpy = allStreams.FirstOrDefault(s => s.MolarEnthalpy.IsDefined)?.MolarEnthalpy.Value;

            var masterThermo = allStreams.FirstOrDefault(s => s.ThermodynamicMethod != null && s.ThermodynamicMethod.IsDefined)?.ThermodynamicMethod.Value;

            foreach (var stream in allStreams)
            {
                if (masterThermo != null && stream.ThermodynamicMethod != null && !stream.ThermodynamicMethod.IsDefined)
                {
                    stream.ThermodynamicMethod.SetValue(masterThermo, MethodSource.Other, Name);
                    this.AddCalculatedVariable(stream.ThermodynamicMethod);
                }

                if (masterT != null && !stream.Temperature.IsDefined)
                {
                    stream.Temperature.SetValue(new Temperature(masterT.Value, masterT.Unit), MethodSource.Other, Name);
                    this.AddCalculatedVariable(stream.Temperature);
                }

                if (masterP != null && !stream.Pressure.IsDefined)
                {
                    stream.Pressure.SetValue(new Pressure(masterP.Value, masterP.Unit), MethodSource.Other, Name);
                    this.AddCalculatedVariable(stream.Pressure);
                }

                if (masterComp != null && !stream.StreamComposition.IsDefined)
                {
                    stream.StreamComposition.SetValue(masterComp.Value!.Clone(), MethodSource.Other, Name);
                    this.AddCalculatedVariable(stream.StreamComposition);
                }

                if (masterVapFrac != null && !stream.VaporFraction.IsDefined)
                {
                    stream.VaporFraction.SetValue(masterVapFrac.Value, MethodSource.Other, Name);
                    this.AddCalculatedVariable(stream.VaporFraction);
                }

                if (masterEnthalpy != null && !stream.MolarEnthalpy.IsDefined)
                {
                    stream.MolarEnthalpy.SetValue(new MolarEnergy(masterEnthalpy.Value, masterEnthalpy.Unit), MethodSource.Other, Name);
                    this.AddCalculatedVariable(stream.MolarEnthalpy);
                }
            }

            return masterT != null && masterP != null;
        }

        // ==============================================================================
        // ⚖️ MOTOR ITERATIVO DE MASA (Reemplaza a PropagateFlows y BalanceFractions)
        // ==============================================================================
        private bool SolveMassBalance()
        {
           
            bool keepChecking = true;
            int maxIterations = 10; // Evita bucles infinitos
            int iter = 0;

            while (keepChecking && iter < maxIterations)
            {
                keepChecking = false;
                iter++;

                double? inletMass = InletStream?.MassFlow.IsDefined == true
                    ? InletStream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr)
                    : null;

                // REGLA 1: BALANCE GLOBAL (BACKWARD COMPLETO)
                // Si no hay entrada, pero todas las salidas tienen flujo -> Sumamos
                if (inletMass == null && OutletStreams.Count > 0 && OutletStreams.All(s => s.Value.MassFlow.IsDefined))
                {
                    double sumOutlets = OutletStreams.Sum(s => s.Value.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr));
                    InletStream!.MassFlow.SetValue(new MassFlow(sumOutlets, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                    this.AddCalculatedVariable(InletStream.MassFlow);
                    inletMass = sumOutlets;
             
                    keepChecking = true;
                }

                // REGLA 2: BALANCE GLOBAL (FALTA UNA SALIDA)
                // Si hay entrada, y falta exactamente UN flujo de salida -> Diferencia
                if (inletMass != null && OutletStreams.Count > 1)
                {
                    var unknownOutlets = OutletStreams.Where(s => !s.Value.MassFlow.IsDefined).ToList();
                    if (unknownOutlets.Count == 1)
                    {
                        double sumKnownOutlets = OutletStreams.Where(s => s.Value.MassFlow.IsDefined)
                                                              .Sum(s => s.Value.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr));

                        double missingMass = Math.Max(0, inletMass.Value - sumKnownOutlets);
                        unknownOutlets[0].Value.MassFlow.SetValue(new MassFlow(missingMass, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                        this.AddCalculatedVariable(unknownOutlets[0].Value.MassFlow);
                        keepChecking = true;
                    }
                }

                // REGLA 3: RELACIONES FLUJO <-> FRACCIÓN
                foreach (var kvp in OutletStreams)
                {
                    var portName = kvp.Key;
                    var stream = kvp.Value;
                    SplitFractions.TryGetValue(portName, out var fractionVar);

                    bool hasFlow = stream.MassFlow.IsDefined;
                    bool hasFrac = fractionVar != null && fractionVar.IsDefined;

                    // A. Flujo de Salida conocido + Entrada conocida -> Calcular Fracción (Si falta)
                    if (hasFlow && inletMass != null && inletMass > 0 && !hasFrac && fractionVar != null)
                    {
                        double calcFrac = (stream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr) / inletMass.Value) * 100.0;
                        fractionVar.SetValueCalculated(Math.Round(calcFrac, 4), Name);
                        this.AddCalculatedVariable(fractionVar);
                     
                        keepChecking = true;
                    }

                    // B. Fracción conocida + Entrada conocida -> Calcular Flujo de Salida (FORWARD)
                    if (hasFrac && inletMass != null && !hasFlow)
                    {
                        double calcFlow = inletMass.Value * (fractionVar!.Value / 100.0);
                        stream.MassFlow.SetValue(new MassFlow(calcFlow, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                        this.AddCalculatedVariable(stream.MassFlow);
                       
                        keepChecking = true;
                    }

                    // C. Flujo de Salida conocido + Fracción conocida -> Calcular Entrada (BACKWARD)
                    if (hasFlow && hasFrac && fractionVar!.Value > 0 && inletMass == null)
                    {
                        double calcInlet = stream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr) / (fractionVar.Value / 100.0);
                        InletStream!.MassFlow.SetValue(new MassFlow(calcInlet, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                        this.AddCalculatedVariable(InletStream.MassFlow);
                        inletMass = calcInlet;
                 
                        keepChecking = true;
                    }
                }

                // REGLA 4: REGLA N-1 SOBRE LOS PORCENTAJES
                var unknownFractions = SplitFractions.Where(f => !f.Value.IsDefined).ToList();
                if (unknownFractions.Count == 1)
                {
                    double sumKnownFrac = SplitFractions.Where(f => f.Value.IsDefined).Sum(f => f.Value.Value);
                    if (sumKnownFrac <= 100.0)
                    {
                        double calcFrac = Math.Round(100.0 - sumKnownFrac, 4);
                        unknownFractions[0].Value.SetValueCalculated(calcFrac, Name);
                        this.AddCalculatedVariable(unknownFractions[0].Value);
              
                        keepChecking = true;
                    }
                }
            }

            // El balance es exitoso si, después de iterar, tenemos la entrada y TODAS las salidas definidas
            return InletStream != null && InletStream.MassFlow.IsDefined && OutletStreams.All(s => s.Value.MassFlow.IsDefined);
        }
    }
}
