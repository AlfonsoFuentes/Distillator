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

    public class ControlValveSimulationFacade : EquipmentFacade
    {
        public ControlValveSimulationFacade()
        {
            DeltaPressure.OnExecuteSolver += EvaluateSolverTrigger;
            ValveCv.OnExecuteSolver += EvaluateSolverTrigger;
        }

        private void EvaluateSolverTrigger()
        {
            OnExecuteSolver?.Invoke(this);
        }

        // =========================================================
        // 1. VARIABLES DEL EQUIPO
        // =========================================================

        // Caída de presión (Delta P)
        public ControlledAmountVariable<PressureDrop> DeltaPressure { get; set; }
            = new ControlledAmountVariable<PressureDrop>(
                preferredUnit: PressureDropUnits.Bar,
                initialValue: new PressureDrop(0, PressureDropUnits.Bar)
            );

        // Coeficiente de la válvula (Cv) - Adimensional o unidades específicas según tu framework
        public ControlledVariable<double> ValveCv { get; set; }
            = new ControlledVariable<double>(0.0);

        // =========================================================
        // 2. ESTADOS VISUALES
        // =========================================================
        public ValveStateType State { get; set; } = ValveStateType.Created;

        public override string StatusText => State switch
        {
            ValveStateType.Created => "Ready",
            ValveStateType.PartiallyConnected => "Underspecified",
            ValveStateType.ReadyToCalculate => "Ready to Solve",
            ValveStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            ValveStateType.Created => "#CBD5E0",               // Gris
            ValveStateType.PartiallyConnected => "#F6AD55",    // Naranja
            ValveStateType.ReadyToCalculate => "#63B3ED",      // Azul
            ValveStateType.Solved => "#34D399",                // Verde
            _ => "#CBD5E0"
        };

        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();

            if (DeltaPressure.IsDefined)
                result.Add(new("ΔP", DeltaPressure.Value?.ToString() ?? string.Empty));
            else
                result.Add(new("ΔP", "<Not Defined>"));

            if (ValveCv.IsDefined)
                result.Add(new("Cv", ValveCv.Value.ToString("F2")));
            else
                result.Add(new("Cv", "<Not Calculated>"));

            return result;
        }

        // =========================================================
        // 3. TOPOLOGÍA
        // =========================================================
        public StreamSimulationFacade? InletStream { get; private set; }
        public StreamSimulationFacade? OutletStream { get; private set; }

        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            if (portName == "Inlet") InletStream = connectedFacade as StreamSimulationFacade;
            else if (portName == "Outlet") OutletStream = connectedFacade as StreamSimulationFacade;
        
        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Inlet") InletStream = null;
            else if (portName == "Outlet") OutletStream = null;
    
        }

        // =========================================================
        // 4. MOTOR DE CÁLCULO
        // =========================================================
        protected override void CalculatedEquipment()
        {
            if (InletStream == null || OutletStream == null)
            {
                State = ValveStateType.PartiallyConnected;
                return;
            }

            var inlet = InletStream;
            var outlet = OutletStream;
            inlet.ResetCalculatedVariable();
            outlet.ResetCalculatedVariable();
            inlet.Calculate();
            outlet.Calculate();
            bool thermoOk = PropagateThermoMethod(inlet, outlet);
            bool compOk = PropagateComposition(inlet, outlet);
            bool massOk = PropagateMassFlow(inlet, outlet);

            // La presión debe ir antes de la energía para que el Flash funcione bien
            bool presOk = PropagatePressure(inlet, outlet);
            bool energyOk = PropagateEnergy(inlet, outlet);

            // Cálculo mecánico de la válvula
            bool cvOk = CalculateCv(inlet, outlet);

            if (thermoOk && compOk && massOk && presOk && energyOk && cvOk)
                State = ValveStateType.Solved;
            else
                State = ValveStateType.ReadyToCalculate;
        }

        private bool PropagateThermoMethod(StreamSimulationFacade inlet, StreamSimulationFacade outlet)
        {
            if (inlet.ThermodynamicMethod.IsDefined && outlet.ThermodynamicMethod.IsDefined &&
                inlet.ThermodynamicMethod.Value?.Name != outlet.ThermodynamicMethod.Value?.Name) return false;

            if (inlet.ThermodynamicMethod.IsDefined && !outlet.ThermodynamicMethod.IsDefined)
            {
                outlet.ThermodynamicMethod.SetValue(inlet.ThermodynamicMethod.Value, MethodSource.Other, Name);
                AddCalculatedVariable(outlet.ThermodynamicMethod);
                return true;
            }
            else if (outlet.ThermodynamicMethod.IsDefined && outlet.ThermodynamicMethod.Source == MethodSource.UserInterface && !inlet.ThermodynamicMethod.IsDefined)
            {
                inlet.ThermodynamicMethod.SetValue(outlet.ThermodynamicMethod.Value, MethodSource.Other, Name);
                AddCalculatedVariable(inlet.ThermodynamicMethod);
                return true;
            }
            return false;
        }

        private bool PropagateComposition(StreamSimulationFacade inlet, StreamSimulationFacade outlet)
        {
            if (inlet.StreamComposition.IsDefined && !outlet.StreamComposition.IsDefined)
            {
                outlet.StreamComposition.SetValue(inlet.StreamComposition.Value!.Clone(), MethodSource.Other, Name);
                AddCalculatedVariable(outlet.StreamComposition);
                return true;
            }
            if (outlet.StreamComposition.IsDefined && outlet.StreamComposition.Source == MethodSource.UserInterface && !inlet.StreamComposition.IsDefined)
            {
                inlet.StreamComposition.SetValue(outlet.StreamComposition.Value!.Clone(), MethodSource.Other, Name);
                AddCalculatedVariable(inlet.StreamComposition);
                return true;
            }
            return false;
        }

        private bool PropagateMassFlow(StreamSimulationFacade inlet, StreamSimulationFacade outlet)
        {
            if (inlet.MassFlow.IsDefined && !outlet.MassFlow.IsDefined)
            {
                double flow = inlet.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
                outlet.MassFlow.SetValue(new MassFlow(flow, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                AddCalculatedVariable(outlet.MassFlow);
                return true;
            }
            if (!inlet.MassFlow.IsDefined && outlet.MassFlow.IsDefined)
            {
                double flow = outlet.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
                inlet.MassFlow.SetValue(new MassFlow(flow, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                AddCalculatedVariable(inlet.MassFlow);
                return true;
            }
            return inlet.MassFlow.IsDefined && outlet.MassFlow.IsDefined;
        }

        private bool PropagatePressure(StreamSimulationFacade inlet, StreamSimulationFacade outlet)
        {
            bool hasInlet = inlet.Pressure.IsDefined;
            bool hasOutlet = outlet.Pressure.IsDefined;
            bool hasDeltaP = DeltaPressure.IsDefined;

            if (hasInlet && hasOutlet && hasDeltaP && DeltaPressure.Source == MethodSource.UserInterface) return false;

            if (hasDeltaP)
            {
                double deltaP = DeltaPressure.GetValueInUnit(PressureDropUnits.Bar);

                if (hasInlet && !hasOutlet)
                {
                    double pIn = inlet.Pressure.GetValueInUnit(PressureUnits.Bara);
                    // Válvula restringe el flujo, la presión SIEMPRE cae.
                    outlet.Pressure.SetValue(new Pressure(Math.Max(0.01, pIn - deltaP), PressureUnits.Bara), MethodSource.Other, Name);
                    AddCalculatedVariable(outlet.Pressure);
                    return true;
                }
                if (hasOutlet && !hasInlet)
                {
                    double pOut = outlet.Pressure.GetValueInUnit(PressureUnits.Bara);
                    inlet.Pressure.SetValue(new Pressure(pOut + deltaP, PressureUnits.Bara), MethodSource.Other, Name);
                    AddCalculatedVariable(inlet.Pressure);
                    return true;
                }
            }
            else if (hasInlet && hasOutlet)
            {
                double pIn = inlet.Pressure.GetValueInUnit(PressureUnits.Bara);
                double pOut = outlet.Pressure.GetValueInUnit(PressureUnits.Bara);

                DeltaPressure.SetValueCalculated(new PressureDrop(Math.Max(0, pIn - pOut), PressureDropUnits.Bar), Name);
                AddCalculatedVariable(DeltaPressure);
                return true;
            }
            return hasInlet && hasOutlet;
        }

        private bool PropagateEnergy(StreamSimulationFacade inlet, StreamSimulationFacade outlet)
        {
            // 🚩 EL SECRETO DE JOULE-THOMSON: PROCESO ISENTÁLPICO
            bool hasInletEnergy = inlet.Temperature.IsDefined || inlet.MolarEnthalpy.IsDefined;
            bool hasOutletEnergy = outlet.Temperature.IsDefined || outlet.MolarEnthalpy.IsDefined;

            if (hasInletEnergy && !hasOutletEnergy)
            {
                // Pasamos la Entalpía Molar INTACTA. El Flash de la salida verá la nueva presión
                // (más baja) y la misma entalpía, y calculará la temperatura correcta.
                double h_in = inlet.MolarEnthalpy.GetValueInUnit(MolarEnergyUnits.Kcal_Kgmol);

                outlet.MolarEnthalpy.SetValue(new MolarEnergy(h_in, MolarEnergyUnits.Kcal_Kgmol), MethodSource.Other, Name);
                AddCalculatedVariable(outlet.MolarEnthalpy);
                return true;
            }

            if (!hasInletEnergy && hasOutletEnergy)
            {
                double h_out = outlet.MolarEnthalpy.GetValueInUnit(MolarEnergyUnits.Kcal_Kgmol);
                inlet.MolarEnthalpy.SetValue(new MolarEnergy(h_out, MolarEnergyUnits.Kcal_Kgmol), MethodSource.Other, Name);
                AddCalculatedVariable(inlet.MolarEnthalpy);
                return true;
            }

            return hasInletEnergy && hasOutletEnergy;
        }

        private bool CalculateCv(StreamSimulationFacade inlet, StreamSimulationFacade outlet)
        {
            // Verificamos que tengamos Masa, Densidad y el Delta P
            if (!DeltaPressure.IsDefined || !inlet.MassFlow.IsDefined || !inlet.MassDensity.IsDefined || !inlet.Pressure.IsDefined)
                return false;

            double deltaP_bar = DeltaPressure.GetValueInUnit(PressureDropUnits.Bar);
            double massFlow_kgh = inlet.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
            double density_kgm3 = inlet.MassDensity.GetValueInUnit(MassDensityUnits.Kg_m3);
            double pIn_bar = inlet.Pressure.GetValueInUnit(PressureUnits.Bara);

            if (deltaP_bar <= 0 || density_kgm3 <= 0 || massFlow_kgh <= 0)
                return false;

            // Le preguntamos a la corriente si es líquido, mezcla o vapor puro
            double vaporFraction = inlet.VaporFraction.IsDefined ? inlet.VaporFraction.Value : 0.0;
            double cv = 0;

            // =======================================================
            // 1. CÁLCULO PARA LÍQUIDOS (Incompresible)
            // =======================================================
            if (vaporFraction < 0.01)
            {
                // Fórmula general métrica: Kv = masa / (31.62 * sqrt(dP * rho))
                double kv = massFlow_kgh / (31.62 * Math.Sqrt(deltaP_bar * density_kgm3));

                // Convertimos Kv (Métrico) a Cv (Imperial - Galones/min)
                cv = kv * 1.156;
            }
            // =======================================================
            // 2. CÁLCULO PARA GASES / VAPORES (Compresible)
            // =======================================================
            else
            {
                // CHOKED FLOW: Si el dP es mayor al ~50% de la presión de entrada, 
                // el flujo se ahorca. La válvula no ve un dP mayor a este límite.
                double max_dP = pIn_bar * 0.5; // (Asumiendo un factor de caída de presión crítica xT ≈ 0.5)
                double effective_dP = deltaP_bar;

                if (effective_dP > max_dP)
                {
                    effective_dP = max_dP; // El cálculo se topa aquí (Flujo Sónico)
                }

                // FACTOR DE EXPANSIÓN (Y): Ajusta la densidad a medida que el gas se expande
                double x = effective_dP / pIn_bar;
                double Y = 1.0 - (x / (3.0 * 0.5)); // Y varía entre 1.0 y 0.667

                // Fórmula ISA para gases usando densidad:
                double kv = massFlow_kgh / (31.62 * Y * Math.Sqrt(effective_dP * density_kgm3));
                cv = kv * 1.156;
            }

            // Guardamos el resultado
            ValveCv.SetValueCalculated(cv, Name);
            AddCalculatedVariable(ValveCv);

            return true;
        }
    }
}
