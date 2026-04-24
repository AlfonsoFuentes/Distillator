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
                    var newFraction = new ControlledVariable<double>(50.0);
                    newFraction.OnExecuteSolver += EvaluateSolverTrigger;
                    // 🚩 PATERNIDAD: Le decimos a la variable que pertenece a este Splitter
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
                    var newFraction = new ControlledVariable<double>();
                    newFraction.OnExecuteSolver += EvaluateSolverTrigger;
                    // 🚩 PATERNIDAD: Le decimos a la variable que pertenece a este Splitter
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

            // 🚩 LÍNEA ELIMINADA AQUÍ (El foreach con el ResetCalculatedVariable sobraba)

            // 1. REGLA N-1: Auto-completar fracciones sobre 100%
            BalanceFractions();

            // 2. PROPAGACIÓN: Repartir variables intensivas y extensivas
            bool intensiveOk = PropagateIntensiveProperties(allStreams);
            bool flowsOk = PropagateFlows();

            // 3. CÁLCULO FINAL: Cada corriente se resetea y se calcula a sí misma internamente
            foreach (var s in allStreams) s.Calculate();

        }

            // REGLA N-1
        private void BalanceFractions()
        {
            var userDefined = SplitFractions.Values.Where(f => f.Source == MethodSource.UserInterface).ToList();
            var missing = SplitFractions.Values.Where(f => f.Source != MethodSource.UserInterface).ToList();

            double sumDefined = userDefined.Sum(f => f.Value );

            if (missing.Count == 1 && sumDefined <= 100.0)
            {
                double calculatedValue = Math.Round(100.0 - sumDefined, 4);
                missing[0].SetValueCalculated(calculatedValue, Name);
                // Ya no hace falta el AddCalculatedVariable explícito aquí porque amarramos el delegado arriba
            }
        }

        // 🌊 PISCINA INTENSIVA Y TERMODINÁMICA
        // 🌊 PISCINA INTENSIVA Y TERMODINÁMICA
        private bool PropagateIntensiveProperties(List<StreamSimulationFacade> allStreams)
        {
            var masterT = allStreams.FirstOrDefault(s => s.Temperature.IsDefined)?.Temperature.Value;
            var masterP = allStreams.FirstOrDefault(s => s.Pressure.IsDefined)?.Pressure.Value;
            var masterComp = allStreams.FirstOrDefault(s => s.StreamComposition.IsDefined)?.StreamComposition;
            var masterVapFrac = allStreams.FirstOrDefault(s => s.VaporFraction.IsDefined)?.VaporFraction;
            var masterEnthalpy = allStreams.FirstOrDefault(s => s.MolarEnthalpy.IsDefined)?.MolarEnthalpy.Value;

            // 🚩 CORRECCIÓN: Tratamos al Método Termo como el ControlledVariable que es
            var masterThermo = allStreams.FirstOrDefault(s => s.ThermodynamicMethod != null && s.ThermodynamicMethod.IsDefined)?.ThermodynamicMethod.Value;

            foreach (var stream in allStreams)
            {
                // 1. 🚩 PROPAGAR EL MÉTODO TERMODINÁMICO CON SETVALUE
                if (masterThermo != null && stream.ThermodynamicMethod != null && !stream.ThermodynamicMethod.IsDefined)
                {
                    stream.ThermodynamicMethod.SetValue(masterThermo, MethodSource.Other, Name);
                    this.AddCalculatedVariable(stream.ThermodynamicMethod); // El Splitter asume la paternidad
                }

                // 2. Propagar variables asumiendo la paternidad desde el Splitter
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

        // ⚖️ BALANCE EXTENSIVO
        private bool PropagateFlows()
        {
            bool propagated = false;

            // ==============================================================================
            // FASE 0: INFERENCIA DE FRACCIONES DESDE FLUJOS (Grados de Libertad)
            // ==============================================================================
            // Si tenemos la entrada y alguna salida con flujo, calculamos su porcentaje real
            if (InletStream != null && InletStream.MassFlow.IsDefined)
            {
                double totalInlet = InletStream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);

                if (totalInlet > 0)
                {
                    foreach (var kvp in OutletStreams)
                    {
                        var portName = kvp.Key;
                        var stream = kvp.Value;

                        // Si la salida tiene flujo y el porcentaje NO lo definió el usuario, lo calculamos
                        if (stream.MassFlow.IsDefined && SplitFractions.TryGetValue(portName, out var fraction))
                        {
                            if (fraction.Source != MethodSource.UserInterface)
                            {
                                double calcPercent = (stream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr) / totalInlet) * 100.0;

                                // Seteamos el valor y registramos la paternidad del Splitter
                                fraction.SetValueCalculated(Math.Round(calcPercent, 4), Name);
                                this.AddCalculatedVariable(fraction);
                                propagated = true;
                            }
                        }
                    }
                }
            }

            // Una vez inferidas las fracciones desde los flujos, intentamos balancear las que faltan (N-1)
            BalanceFractions();

            // ==============================================================================
            // FASE 1: ESCENARIO FORWARD (Entrada -> Salidas)
            // ==============================================================================
            if (InletStream != null && InletStream.MassFlow.IsDefined)
            {
                double inletMass = InletStream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);

                foreach (var kvp in OutletStreams)
                {
                    var portName = kvp.Key;
                    var outletStream = kvp.Value;

                    // Si tenemos la fracción (venga de donde venga) y el flujo de salida no está definido
                    if (SplitFractions.TryGetValue(portName, out var fraction) && fraction.IsDefined)
                    {
                        if (!outletStream.MassFlow.IsDefined)
                        {
                            double outletMass = inletMass * (fraction.Value / 100.0);
                            outletStream.MassFlow.SetValue(new MassFlow(outletMass, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                            this.AddCalculatedVariable(outletStream.MassFlow);
                            propagated = true;
                        }
                    }
                }
                // Si logramos llegar aquí con entrada definida, el balance de flujos se considera exitoso
                return true;
            }

            // ==============================================================================
            // FASE 2: ESCENARIO BACKWARD (Salida -> Entrada)
            // ==============================================================================
            if (InletStream != null && !InletStream.MassFlow.IsDefined)
            {
                foreach (var kvp in OutletStreams)
                {
                    var portName = kvp.Key;
                    var outletStream = kvp.Value;

                    // Buscamos una salida que el usuario haya definido Y que tenga fracción válida
                    if (outletStream.MassFlow.IsDefined &&
                        SplitFractions.TryGetValue(portName, out var fraction) &&
                        fraction.IsDefined && fraction.Value > 0)
                    {
                        double knownOutletMass = outletStream.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);

                        // Reconstruimos la entrada
                        double calculatedInletMass = knownOutletMass / (fraction.Value / 100.0);
                        InletStream.MassFlow.SetValue(new MassFlow(calculatedInletMass, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                        this.AddCalculatedVariable(InletStream.MassFlow);
                        propagated = true;

                        // Ahora que "descubrimos" la entrada, propagamos hacia los hermanos que falten
                        foreach (var siblingKvp in OutletStreams)
                        {
                            if (siblingKvp.Key == portName) continue;

                            var siblingStream = siblingKvp.Value;
                            if (SplitFractions.TryGetValue(siblingKvp.Key, out var siblingFrac) &&
                                siblingFrac.IsDefined && !siblingStream.MassFlow.IsDefined)
                            {
                                double siblingMass = calculatedInletMass * (siblingFrac.Value / 100.0);
                                siblingStream.MassFlow.SetValue(new MassFlow(siblingMass, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                                this.AddCalculatedVariable(siblingStream.MassFlow);
                            }
                        }
                        return true;
                    }
                }
            }

            return propagated;
        }
    }
}
