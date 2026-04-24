using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.UnitOperations.Pumps
{
    public enum PumpStateType
    {
        Created,
        PartiallyConnected,
        ReadyToCalculate,
        Solved
    }
    public class PumpSimulationFacade : EquipmentFacade
    {
        public PumpSimulationFacade()
        {
            DeltaPressure.OnExecuteSolver += EvaluateSolverTrigger;
            AdiabaticEfficiency.OnExecuteSolver += EvaluateSolverTrigger;


        }

        private void EvaluateSolverTrigger()
        {
            OnExecuteSolver?.Invoke(this);
        }

        // 2. VARIABLES DEL EQUIPO
        public ControlledAmountVariable<PressureDrop> DeltaPressure { get; set; }
             = new ControlledAmountVariable<PressureDrop>(
                 preferredUnit: PressureDropUnits.Bar, // Usa el enum de tu dominio
                 initialValue: new PressureDrop(0, PressureDropUnits.Bar)
             );

        // Opcional: Permitir al usuario definir la presión de salida exacta en lugar del Delta P


        // Eficiencia (Adimensional / Porcentaje). Sigue la misma lógica que VaporFraction
        public ControlledVariable<double> AdiabaticEfficiency { get; set; }
            = new ControlledVariable<double>(75.0);

        // Potencia Consumida (Calculada por el PumpCalculator)
        public ControlledAmountVariable<Power> PowerConsumed { get; set; }
            = new ControlledAmountVariable<Power>(
                preferredUnit: PowerUnits.KiloWatt, // Usa el enum de tu dominio (ej. kW, HP)
                initialValue: new Power(0, PowerUnits.KiloWatt)
            );

        // 👇 EL NUEVO ESTADO DE LA MÁQUINA
        public PumpStateType State { get; set; } = PumpStateType.Created;

        // 3. ESTADO VISUAL (Aplicando tu lógica de colores)
        public override string StatusText => State switch
        {
            PumpStateType.Created => "Ready",
            PumpStateType.PartiallyConnected => "Underspecified",
            PumpStateType.ReadyToCalculate => "Ready to Solve",
            PumpStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public override string StatusColor => State switch
        {
            PumpStateType.Created => "#CBD5E0",              // Gris
            PumpStateType.PartiallyConnected => "#F6AD55",   // Naranja
            PumpStateType.ReadyToCalculate => "#63B3ED",     // Azul
            PumpStateType.Solved => "#34D399",               // Verde
            _ => "#CBD5E0"
        };
        public override List<ToolTipLegend> GetToolTipLegend()
        {
            List<ToolTipLegend> result = new();
            if (DeltaPressure.IsDefined)
            {
                result.Add(new("ΔP", DeltaPressure.Value?.ToString() ?? string.Empty));
            }
            else
            {
                result.Add(new("ΔP", "<Not Defined>"));
            }

            if (AdiabaticEfficiency.IsDefined)
            {
                result.Add(new ToolTipLegend("%Efficiency", $"{AdiabaticEfficiency.Value}"));
            }
            else
            {
                result.Add(new("%Efficiency", "<Not Defined>"));
            }
            if (PowerConsumed.IsDefined)
            {
                result.Add(new ToolTipLegend("Power", PowerConsumed.Value?.ToString() ?? string.Empty));
            }
            else
            {
                result.Add(new("Power", "<Not Calculated>"));
            }
            return result;

        }
        

        // 4. TOPOLOGÍA DE SIMULACIÓN
        public StreamSimulationFacade? SuctionStream { get; private set; }
        public StreamSimulationFacade? DischargeStream { get; private set; }



        public override void AttachConnection(string portName, IFacade connectedFacade)
        {
            if (portName == "Suction") SuctionStream = connectedFacade as StreamSimulationFacade;
            else if (portName == "Discharge") DischargeStream = connectedFacade as StreamSimulationFacade;

  

        }

        public override void DetachConnection(string portName)
        {
            if (portName == "Suction") SuctionStream = null;
            else if (portName == "Discharge") DischargeStream = null;
 

        }

        protected override void CalculatedEquipment()
        {
            if (SuctionStream == null || DischargeStream == null)
            {
                State = PumpStateType.PartiallyConnected;
                return;
            }

            var suction = SuctionStream;
            var discharge = DischargeStream;
            suction.ResetCalculatedVariable();
            discharge.ResetCalculatedVariable();
            suction.Calculate();
            discharge.Calculate();
            bool thermoOk = PropagateThermoMethod(suction, discharge);
            bool compOk = PropagateComposition(suction, discharge);
            bool presOk = PropagatePressure(suction, discharge);
            bool massOk = PropagateMassFlow(suction, discharge);

            // 🚩 POTENCIA ANTES DE ENERGÍA
            // Necesitamos la potencia para calcular el salto de entalpía.
            bool powerOk = CalculatePower(suction, discharge);

            // 🚩 AHORA SÍ PROPAGAMOS ENERGÍA
            bool energyOk = PropagateEnergy(suction, discharge);

            if (thermoOk && compOk && presOk && energyOk && massOk && powerOk)
            {
                State = PumpStateType.Solved;
            }
            else
            {
                State = PumpStateType.ReadyToCalculate;
            }
        }

        private bool PropagateThermoMethod(StreamSimulationFacade suction, StreamSimulationFacade discharge)
        {
            // Conflicto Severo: Tienen métodos diferentes definidos explícitamente
            if (suction.ThermodynamicMethod.IsDefined && discharge.ThermodynamicMethod.IsDefined &&
                suction.ThermodynamicMethod.Value?.Name != discharge.ThermodynamicMethod.Value?.Name)
            {
                // ❌ El estado es inválido. Detenemos la máquina.
                return false;
            }

            // Forward
            if (suction.ThermodynamicMethod.IsDefined && !discharge.ThermodynamicMethod.IsDefined)
            {
                discharge.ThermodynamicMethod.SetValue(suction.ThermodynamicMethod.Value, MethodSource.Other, Name);
                AddCalculatedVariable(discharge.ThermodynamicMethod);
                return true;
            }
            // Backward (Solo confiamos en la descarga si fue el Humano/UI quien la puso ahí)
            else if (discharge.ThermodynamicMethod.IsDefined && discharge.ThermodynamicMethod.Source == MethodSource.UserInterface && !suction.ThermodynamicMethod.IsDefined)
            {
                suction.ThermodynamicMethod.SetValue(discharge.ThermodynamicMethod.Value, MethodSource.Other, Name);
                AddCalculatedVariable(suction.ThermodynamicMethod);
                return true;
            }

            // ✅ Todo está bien
            return false;
        }

        private bool PropagateComposition(StreamSimulationFacade suction, StreamSimulationFacade discharge)
        {
            // Forward
            if (suction.StreamComposition.IsDefined && !discharge.StreamComposition.IsDefined)
            {
                discharge.StreamComposition.SetValue(suction.StreamComposition.Value!.Clone(), MethodSource.Other, Name);
                AddCalculatedVariable(discharge.StreamComposition);
                return true;
            }
            // Backward
            if (discharge.StreamComposition.IsDefined && discharge.StreamComposition.Source == MethodSource.UserInterface && !suction.StreamComposition.IsDefined)
            {
                suction.StreamComposition.SetValue(discharge.StreamComposition.Value!.Clone(), MethodSource.Other, Name);
                AddCalculatedVariable(suction.StreamComposition);
                return true;
            }

            // ✅ Todo está bien
            return false;
        }
        private bool PropagateEnergy(StreamSimulationFacade suction, StreamSimulationFacade discharge)
        {
            // Revisamos si la corriente ya tiene energía (ya sea por T o por Entalpía Molar)
            bool hasSuctionEnergy = suction.Temperature.IsDefined || suction.MolarEnthalpy.IsDefined;
            bool hasDischargeEnergy = discharge.Temperature.IsDefined || discharge.MolarEnthalpy.IsDefined;

            // Si la potencia o el flujo MOLAR no están definidos aún, no calculamos.
            if (!PowerConsumed.IsDefined || !suction.MolarFlow.IsDefined)
                return false;

            double power_kW = PowerConsumed.GetValueInUnit(PowerUnits.KiloWatt);
            double molarFlow_kgmol_h = suction.MolarFlow.GetValueInUnit(MolarFlowUnits.Kgmol_hr);

            if (molarFlow_kgmol_h <= 0) return false;

            // 1 kW equivale aprox a 859.845 kcal/hr. 
            // (Ajusta este factor si usas una clase conversora interna en tu framework)
            double power_kcal_h = power_kW * 859.845;

            // Delta H Molar = (kcal/h) / (kgmol/h) = kcal/kgmol
            double deltaH_molar = power_kcal_h / molarFlow_kgmol_h;

            // Forward: Succión a Descarga
            if (hasSuctionEnergy && !hasDischargeEnergy)
            {
                double h_in = suction.MolarEnthalpy.GetValueInUnit(MolarEnergyUnits.Kcal_Kgmol);
                double h_out = h_in + deltaH_molar;

                // Le pasamos directamente la MOLAR y la corriente hace el resto
                discharge.MolarEnthalpy.SetValue(new MolarEnergy(h_out, MolarEnergyUnits.Kcal_Kgmol), MethodSource.Other, Name);
                AddCalculatedVariable(discharge.MolarEnthalpy);
                return true;
            }

            // Backward: Descarga a Succión
            if (!hasSuctionEnergy && hasDischargeEnergy)
            {
                double h_out = discharge.MolarEnthalpy.GetValueInUnit(MolarEnergyUnits.Kcal_Kgmol);
                double h_in = h_out - deltaH_molar;

                suction.MolarEnthalpy.SetValue(new MolarEnergy(h_in, MolarEnergyUnits.Kcal_Kgmol), MethodSource.Other, Name);
                AddCalculatedVariable(suction.MolarEnthalpy);
                return true;
            }

            return hasSuctionEnergy && hasDischargeEnergy;
        }
        private bool PropagateEnergy2(StreamSimulationFacade suction, StreamSimulationFacade discharge)
        {
            if (AdiabaticEfficiency.Value <= 0) return true; // No hay eficiencia, no hay cálculo de calor.

            bool isTemperatureSuccionDefined = suction.Temperature.IsDefined;
            bool isTemperatureDischargeDeined = discharge.Temperature.IsDefined;

            if (isTemperatureSuccionDefined && !isTemperatureDischargeDeined)
            {
                double temperature = suction.Temperature.GetValueInUnit(TemperatureUnits.Kelvin);
                discharge.Temperature.SetValue(new Temperature(temperature, TemperatureUnits.Kelvin), MethodSource.Other, Name);
                AddCalculatedVariable(discharge.Temperature);
                return true;
            }
            if (!isTemperatureSuccionDefined && isTemperatureDischargeDeined)
            {
                double temperature = discharge.Temperature.GetValueInUnit(TemperatureUnits.Kelvin);
                suction.Temperature.SetValue(new Temperature(temperature, TemperatureUnits.Kelvin), MethodSource.Other, Name);
                AddCalculatedVariable(suction.Temperature);
                return true;
            }
            return false;
        }

        private bool PropagatePressure(StreamSimulationFacade suction, StreamSimulationFacade discharge)
        {
            if (DeltaPressure.IsDefined)
            {
                // Forward: Tengo Succión, calculo Descarga
                if (suction.Pressure.IsDefined && !discharge.Pressure.IsDefined)
                {
                    var deltaP = DeltaPressure.GetValueInUnit(PressureDropUnits.Bar);
                    var suctionPressure = suction.Pressure.GetValueInUnit(PressureUnits.Bara);
                    var dischargepressure = suctionPressure + deltaP;

                    discharge.Pressure.SetValue(new Pressure(dischargepressure, PressureUnits.Bara), MethodSource.Other, Name);
                    AddCalculatedVariable(discharge.Pressure);
                    return true;
                }
                // Backward: Tengo Descarga, calculo Succión
                if (discharge.Pressure.IsDefined && !suction.Pressure.IsDefined)
                {
                    var deltaP = DeltaPressure.GetValueInUnit(PressureDropUnits.Bar);
                    var dischargepressure = discharge.Pressure.GetValueInUnit(PressureUnits.Bara);
                    var suctionPressure = dischargepressure - deltaP;

                    // 👇 Corregido: Asignamos a la SUCCIÓN
                    suction.Pressure.SetValue(new Pressure(suctionPressure, PressureUnits.Bara), MethodSource.Other, Name);
                    AddCalculatedVariable(suction.Pressure);
                    return true;
                }
            }
            // Si el usuario borró el Delta P, pero definió las dos corrientes a mano
            if (suction.Pressure.IsDefined && discharge.Pressure.IsDefined)
            {
                var dischargepressure = discharge.Pressure.GetValueInUnit(PressureUnits.Bara);
                var suctionPressure = suction.Pressure.GetValueInUnit(PressureUnits.Bara);
                var deltaP = dischargepressure - suctionPressure;

                // Calculamos el Delta P interno de la bomba
                DeltaPressure.SetValueCalculated(new PressureDrop(deltaP, PressureDropUnits.Bar), Name);
                AddCalculatedVariable(DeltaPressure);
                return true;
            }

            return false;
        }
        private bool PropagateMassFlow(StreamSimulationFacade suction, StreamSimulationFacade discharge)
        {
            bool isSuctionFlowDefined = suction.MassFlow.IsDefined;
            bool isDischargeFlowDefined = discharge.MassFlow.IsDefined;

            // Forward: Tengo masa en la entrada, la paso a la salida
            if (isSuctionFlowDefined && !isDischargeFlowDefined)
            {
                double flow = suction.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
                discharge.MassFlow.SetValue(new MassFlow(flow, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                AddCalculatedVariable(discharge.MassFlow);
                return true;
            }

            // Backward: Tengo masa en la salida, la paso a la entrada
            if (!isSuctionFlowDefined && isDischargeFlowDefined)
            {
                double flow = discharge.MassFlow.GetValueInUnit(MassFlowUnits.Kg_hr);
                suction.MassFlow.SetValue(new MassFlow(flow, MassFlowUnits.Kg_hr), MethodSource.Other, Name);
                AddCalculatedVariable(suction.MassFlow);
                return true;
            }

            // Si llega hasta aquí, puede ser porque AMBOS están definidos (Retorna true para continuar)
            // O porque NINGUNO está definido (Retorna false para evitar que pase a Solved)
            return isSuctionFlowDefined && isDischargeFlowDefined;
        }
        private bool CalculatePower(StreamSimulationFacade suction, StreamSimulationFacade discharge)
        {
            // Verificamos si tenemos lo mínimo necesario para calcular la potencia
            bool hasFlow = suction.VolumetricFlow.IsDefined || suction.MassFlow.IsDefined;
            bool hasDeltaP = DeltaPressure.IsDefined;

            if (hasFlow && hasDeltaP && AdiabaticEfficiency.IsDefined )
            {
                // 1. Obtenemos el Delta P en Bar
                double deltaP_bar = DeltaPressure.GetValueInUnit(PressureDropUnits.Bar);

                // 2. Obtenemos el flujo volumétrico en m3/h
                // Si tu simulador ya traduce masa a volumen en las corrientes, leemos el volumétrico.
                // (Si no, tendrías que leer el másico y dividir por la densidad).
                double volFlow_m3h = suction.VolumetricFlow.GetValueInUnit(VolumetricFlowUnits.m3_hr);

                // 3. Eficiencia en fracción (ej. 75% -> 0.75)
                double efficiency = AdiabaticEfficiency.Value / 100.0;

                // 4. Fórmula Hidráulica: kW = (m3/h * bar) / (36 * ef)
                double power_kW = (volFlow_m3h * deltaP_bar) / (36.0 * efficiency);

                // 5. Inyectamos el valor calculado en la bomba
                PowerConsumed.SetValueCalculated(new Power(power_kW, PowerUnits.KiloWatt), Name);
                AddCalculatedVariable(PowerConsumed);

                return true;
            }

            // Si faltan datos, limpiamos la variable por si el usuario borró el flujo

            return false;
        }

    }
}
