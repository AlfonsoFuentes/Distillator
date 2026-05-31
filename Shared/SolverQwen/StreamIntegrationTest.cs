using Shared.PropertiesDtos.Methods;
using Shared.SolverQwen.Equipments;
using Shared.SolverQwen.Simlations;
using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using System.Timers;
using UnitSystem;

namespace Shared.SolverQwen
{

    /// <summary>
    /// Prueba de integración: valida el flujo conceptual completo de una corriente.
    /// Casos:
    /// 1. Definir P + VF + Composición → Ejecutar equilibrio (modo P-FV, calcula T)
    /// 2. Calcular flujos derivados
    /// 3. "Des-definir" P → Invalidar equilibrio
    /// 4. Definir T (con VF aún definido) → Recalcular equilibrio (modo T-FV, calcula P)
    /// </summary>
    public class StreamIntegrationTest
    {



        public StreamIntegrationTest()
        {
            // 1. Crear modelo termodinámico base


            // 2. Crear fachada (orquestador UI/Solver)

        }



        /// <summary>
        /// Crea un DTO de método termodinámico con 2 componentes de ejemplo.
        /// Ajusta según tus componentes reales.
        /// </summary>
        private ThermodynamicMethodFullDto ThermoMethod = null!;
        public void SetThermoMethod(ThermodynamicMethodFullDto thermoMethod)
        {
            ThermoMethod = thermoMethod;
        }
        public void Runbomba()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            // 1️⃣ CONFIGURACIÓN INICIAL
            var inlet = new FacadeStream();
            var outlet = new FacadeStream();
            inlet.SetThermodynamicMethod(ThermoMethod);
            outlet.SetThermodynamicMethod(ThermoMethod);

            var pump = new PumpEquipment("P-101");
            pump.AddInlet(inlet);
            pump.AddOutlet(outlet);

            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(pump);

            Console.WriteLine("🚀 INICIO: PRUEBA BOMBA PASO A PASO");
            Console.WriteLine("====================================\n");

            // ─────────────────────────────────────────────────────────
            // 🔹 PASO 1: Definir concentración en SALIDA
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("📌 PASO 1: Definir composición másica en SALIDA (80% A / 20% B)");
            var outComps = outlet.Composition.Components;
            if (outComps.Count >= 2)
            {
                outComps[0].MassFraction.SetValue(new Percentage(80, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                outComps[1].MassFraction.SetValue(new Percentage(20, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            }
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // 🔹 PASO 2: Definir ΔP de la bomba
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 2: Definir ΔP = 5.0 bar");
            pump.DeltaP.SetValue(new PressureDrop(5, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // 🔹 PASO 3: Definir presión de SALIDA
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 3: Definir P_out = 12.0 bar");
            outlet.Pressure.SetValue(new Pressure(12, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // 🔹 PASO 4: Definir temperatura de ENTRADA
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 4: Definir T_in = 25 °C");
            inlet.Temperature.SetValue(new Temperature(25, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // 🔹 PASO 5: Definir flujo másico de ENTRADA ✅ NUEVO
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 5: Definir ṁ_in = 100 kg/h");
            inlet.MassFlow.SetValue(new MassFlow(100, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            Console.WriteLine("\n✅ PRUEBA FINALIZADA - BOMBA VALIDADA COMPLETAMENTE");
        }


        public void Run()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Thermodynamic method no definido. Ejecuta SetThermoMethod() antes de Run().");
                return;
            }
            var _facade = new FacadeStream();

            // 3. Definir método termodinámico y componentes (esto crea ComponentFacade)

            _facade.SetThermodynamicMethod(ThermoMethod);
            Console.WriteLine("=== INICIO: Prueba de Integración de Corriente ===\n");

            // ─────────────────────────────────────────────────────────
            // 🔹 PASO 1: Definir inputs iniciales (UI)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("🔹 PASO 1: Definiendo inputs desde UI...");

            // Pressure: 1 bar g = 1 bar + 1.01325 bar (atm) = ~2.01325 bar a
            var pressureGauge = new Pressure(1, PressureUnits.Atmospherea);


           _facade.Pressure.SetValue(pressureGauge, VariableDataProcedence.UserInput);
            Console.WriteLine($"   • Pressure = {pressureGauge.GetValue(PressureUnits.Bara):F3} bara (Owner: UI)");

            // MassFlow: 10000 kg/hr
            var massFlow = new MassFlow(10000, MassFlowUnits.Kg_hr);
            _facade.MassFlow.SetValue(massFlow, VariableDataProcedence.UserInput);
            Console.WriteLine($"   • MassFlow = {massFlow.GetValue(MassFlowUnits.Kg_hr):F0} kg/hr (Owner: UI)");

            // VaporFraction: 100% (vapor saturado)
            var newTemperature = new Temperature(88, TemperatureUnits.DegreeCelcius);
            _facade.Temperature.SetValue(newTemperature, VariableDataProcedence.UserInput);
            //_facade.VaporFraction.SetValue(new Percentage(50, PercentageUnits.Percentage), VariableDataProcedence.UserInput);

            //Console.WriteLine($"   • VaporFraction = {vaporFraction.GetValue(PercentageUnits.Percentage):F0}% (Owner: UI)");

            // Composición molar: ejemplo binario (80% CH4, 20% C2H6)
            Console.WriteLine("   • Definiendo composición molar...");
            var components = _facade.Composition.Components.ToList();
            if (components.Count >= 2)
            {
                components[0].MolarFraction.SetValue(new Percentage(20, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                components[1].MolarFraction.SetValue(new Percentage(80, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                Console.WriteLine($"     - {components[0].Name}: 80% mol");
                Console.WriteLine($"     - {components[1].Name}: 20% mol");
            }


            double hMasica = _facade.MassEnthalpy.Value.GetValue(MassEnergyUnits.Kcal_Kg);


            // ─────────────────────────────────────────────────────────
            // 🔹 PASO 2: Ejecutar equilibrio (debería activarse automáticamente)
            // ─────────────────────────────────────────────────────────

            // ─────────────────────────────────────────────────────────
            // 🔹 PASO 3: Ejecutar cálculo de flujos

            _facade.Temperature.Clear(VariableDataProcedence.UserInput);
            var sw = System.Diagnostics.Stopwatch.StartNew();


            _facade.MassEnthalpy.SetValue(new MassEnergy(hMasica, MassEnergyUnits.Kcal_Kg), VariableDataProcedence.UserInput);
            sw.Stop();


            var result = sw.Elapsed;

            double temperatura = _facade.Temperature.Value.GetValue(TemperatureUnits.DegreeCelcius);
            _facade.MassEnthalpy.Clear(VariableDataProcedence.UserInput);

            //var newTemperature = new Temperature(150, TemperatureUnits.DegreeCelcius);
            //_facade.Temperature.SetValue(newTemperature, VariableDataProcedence.UserInput);


            // ─────────────────────────────────────────────────────────
            // 🔹 PASO 6: Verificar que UI no fue sobrescrito
            // ─────────────────────────────────────────────────────────



            _facade.MassFlow.Clear(VariableDataProcedence.UserInput);
            components[0].MolarFraction.Clear(VariableDataProcedence.UserInput);
            components[1].MolarFraction.Clear(VariableDataProcedence.UserInput);


            components[0].MassFlow.SetValueFromSolver(280, VariableDataProcedence.Phase3_ThermoAdjustment);
            components[1].MassFlow.SetValueFromSolver(20, VariableDataProcedence.Phase3_ThermoAdjustment);
            _facade.Composition.ClearComposition();
            components[0].MassFlow.SetValueFromSolver(180, VariableDataProcedence.Phase3_ThermoAdjustment);
            components[1].MassFlow.SetValueFromSolver(120, VariableDataProcedence.Phase3_ThermoAdjustment);
        }

        public void Run2()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            // ─────────────────────────────────────────────────────────
            // CONFIGURACIÓN: Misma topología
            // ─────────────────────────────────────────────────────────
            var inlet = new FacadeStream();
            var interconnect = new FacadeStream();
            var outlet = new FacadeStream();

            inlet.SetThermodynamicMethod(ThermoMethod);
            interconnect.SetThermodynamicMethod(ThermoMethod);
            outlet.SetThermodynamicMethod(ThermoMethod);

            var pump = new PumpEquipment("P-101");
            var valve = new ValveEquipment("V-201");

            pump.AddInlet(inlet);
            pump.AddOutlet(interconnect);
            valve.AddInlet(interconnect);
            valve.AddOutlet(outlet);

            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(pump);
            orchestrator.AddEquipment(valve);

            Console.WriteLine("🚀 EJEMPLO CAÓTICO: ORDEN Y UBICACIÓN ALEATORIOS");
            Console.WriteLine("=================================================\n");
            Console.WriteLine("   Secuencial: w → ΔP_pump → P_in → ṁ → ΔP_valve → T → Q");
            Console.WriteLine("   Caótico:    T → ṁ → P_out → w → ΔP_valve → ΔP_pump → P_in → Q");
            Console.WriteLine("   🎲 ¡Desorden total!\n");

            // ─────────────────────────────────────────────────────────
            // PASO 1: TEMPERATURA en INTERCONNECT (era paso 6 en secuencial)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("📌 PASO 1: Definir T_interconnect = 25 °C");
            interconnect.Temperature.SetValue(new Temperature(25, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 2: FLUJO MÁSICO en OUTLET (era paso 4 en secuencial)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 2: Definir ṁ_outlet = 10000 kg/h");
            outlet.MassFlow.SetValue(new MassFlow(10000, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 3: PRESIÓN DE SALIDA (era parte del paso 3, pero ahora primero)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 3: Definir P_out = 11.0 bara");
            outlet.Pressure.SetValue(new Pressure(11, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 4: COMPOSICIÓN en INTERCONNECT (era paso 1 en secuencial)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 4: Definir composición en INTERCONNECT (80% A / 20% B)");
            var interComps = interconnect.Composition.Components;
            if (interComps.Count >= 2)
            {
                interComps[0].MassFraction.SetValue(new Percentage(80, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                interComps[1].MassFraction.SetValue(new Percentage(20, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            }
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 5: ΔP DE VÁLVULA (era paso 5 en secuencial)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 5: Definir ΔP_Valve = 1.0 bar");
            valve.DeltaP.SetValue(new PressureDrop(1, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 6: ΔP DE BOMBA (era paso 2 en secuencial)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 6: Definir ΔP_Pump = 5.0 bar");
            pump.DeltaP.SetValue(new PressureDrop(5, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 7: PRESIÓN DE ENTRADA (era paso 3 en secuencial, pero ahora casi al final)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 7: Definir P_in = 7.0 bara");
            inlet.Pressure.SetValue(new Pressure(7, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 8: LIMPIAR Y DEFINIR FLUJO VOLUMÉTRICO EN INLET (era pasos 7-8)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 8: Limpiar ṁ_outlet y definir Q_inlet = 8 m³/hr");
            outlet.MassFlow.Clear(VariableDataProcedence.UserInput);
            inlet.VolumetricFlow.SetValue(new VolumetricFlow(8, VolumetricFlowUnits.m3_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            Console.WriteLine("\n✅ EJEMPLO CAÓTICO FINALIZADO");
            Console.WriteLine("   🎯 Verificar: Mismos resultados que ejemplos anteriores");
            Console.WriteLine("      - Composición: 80% A / 20% B en todas las corrientes");
            Console.WriteLine("      - Presiones: P_in≈7, P_int≈12, P_out≈11 bara");
            Console.WriteLine("      - Flujo: ṁ≈7,200 kg/hr");
            Console.WriteLine("      - Temperatura: 25°C en todas las corrientes");
        }





        public void RunPumpValve()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            // ─────────────────────────────────────────────────────────
            // CONFIGURACIÓN: Pump → Valve conectados por corriente intermedia
            // ─────────────────────────────────────────────────────────
            var inlet = new FacadeStream();
            var interconnect = new FacadeStream();  // Salida de bomba = Entrada de válvula
            var outlet = new FacadeStream();

            inlet.SetThermodynamicMethod(ThermoMethod);
            interconnect.SetThermodynamicMethod(ThermoMethod);
            outlet.SetThermodynamicMethod(ThermoMethod);

            var pump = new PumpEquipment("P-101");
            var valve = new ValveEquipment("V-201");

            // Conectar topología: Inlet → Pump → Interconnect → Valve → Outlet
            pump.AddInlet(inlet);
            pump.AddOutlet(interconnect);
            valve.AddInlet(interconnect);
            valve.AddOutlet(outlet);

            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(pump);
            orchestrator.AddEquipment(valve);

            Console.WriteLine("🚀 EJEMPLO 1: PROPAGACIÓN SECUENCIAL (Pump → Valve)");
            Console.WriteLine("====================================================\n");

            // ─────────────────────────────────────────────────────────
            // PASO 1: Definir composición en ENTRADA de la bomba
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("📌 PASO 1: Definir composición en INLET (80% A / 20% B)");
            var inComps = inlet.Composition.Components;
            if (inComps.Count >= 2)
            {
                inComps[0].MassFraction.SetValue(new Percentage(80, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                inComps[1].MassFraction.SetValue(new Percentage(20, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            }
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 2: Definir ΔP de la bomba → debería propagar presión a través de Valve
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 2: Definir ΔP_Pump = 5.0 bar");
            pump.DeltaP.SetValue(new PressureDrop(5, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 3: Definir P_in → debería calcular toda la cadena de presiones
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 3: Definir P_in = 7.0 bara");
            inlet.Pressure.SetValue(new Pressure(7, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 4: Definir ṁ_in → debería propagar flujo a través de toda la cadena
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 4: Definir ṁ_in = 10000 kg/h");
            inlet.MassFlow.SetValue(new MassFlow(10000, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            Console.WriteLine("\n📌 PASO 5: Definir ΔP_Valve = 5.0 bar");
            valve.DeltaP.SetValue(new PressureDrop(1, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();



            Console.WriteLine("\n📌 PASO 6: Definir Temperatura entrada bomba = 25 °C");
            inlet.Temperature.SetValue(new Temperature(25, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            Console.WriteLine("\n📌 PASO 7: Limpiar flujo masico ");
            inlet.MassFlow.Clear(VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();



            Console.WriteLine("\n📌 PASO 8: Definir flujo volumetrico entrada = 8 m3/hr  ");
            inlet.VolumetricFlow.SetValue(new VolumetricFlow(8, VolumetricFlowUnits.m3_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();



        }

        /// </summary>
        public void Run5()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            // ─────────────────────────────────────────────────────────
            // CONFIGURACIÓN: Misma topología Pump → Valve
            // ─────────────────────────────────────────────────────────
            var inlet = new FacadeStream();
            var interconnect = new FacadeStream();
            var outlet = new FacadeStream();

            inlet.SetThermodynamicMethod(ThermoMethod);
            interconnect.SetThermodynamicMethod(ThermoMethod);
            outlet.SetThermodynamicMethod(ThermoMethod);

            var pump = new PumpEquipment("P-101");
            var valve = new ValveEquipment("V-201");

            pump.AddInlet(inlet);
            pump.AddOutlet(interconnect);
            valve.AddInlet(interconnect);
            valve.AddOutlet(outlet);

            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(pump);
            orchestrator.AddEquipment(valve);

            Console.WriteLine("🚀 EJEMPLO 2: ENTRADAS EN DESORDEN (Mixed Input)");
            Console.WriteLine("================================================\n");

            // ─────────────────────────────────────────────────────────
            // PASO 1: Definir composición en SALIDA de la válvula (orden inverso)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("📌 PASO 1: Definir composición en OUTLET (80% A / 20% B)");
            var outComps = outlet.Composition.Components;
            if (outComps.Count >= 2)
            {
                outComps[0].MassFraction.SetValue(new Percentage(80, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                outComps[1].MassFraction.SetValue(new Percentage(20, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            }
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 2: Definir ΔP de la VÁLVULA (no de la bomba)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 2: Definir ΔP_Valve = 2.0 bar");
            valve.DeltaP.SetValue(new PressureDrop(2, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 3: Definir P_out de la válvula
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 3: Definir P_out = 10.0 bara");
            outlet.Pressure.SetValue(new Pressure(10, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 4: Definir ΔP de la BOMBA (completando la cadena de presiones)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 4: Definir ΔP_Pump = 5.0 bar");
            pump.DeltaP.SetValue(new PressureDrop(5, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            // ─────────────────────────────────────────────────────────
            // PASO 5: Definir ṁ_out (flujo definido en salida, propagación hacia atrás)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 5: Definir ṁ_out = 100 kg/h");
            outlet.MassFlow.SetValue(new MassFlow(100, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();


            Console.WriteLine("\n✅ EJEMPLO 2 FINALIZADO - Convergencia con entradas en desorden validada");
        }/// <summary>
         /// Imprime estado detallado para sistema Pump → Valve
         /// </summary>


        public void Run7()
        {
            if (ThermoMethod == null) { Console.WriteLine("❌ Falta SetThermoMethod()"); return; }

            Console.WriteLine(" VALIDACIÓN COMPLETA: SPLITTER (P, T, ṁ, h, Balances)");
            Console.WriteLine("================================================================\n");

            // ── 1. CORRIENTES ──
            var inlet = new FacadeStream();
            var outlet1 = new FacadeStream();
            var outlet2 = new FacadeStream();

            var streams = new[] { inlet, outlet1, outlet2 };
            foreach (var s in streams) s.SetThermodynamicMethod(ThermoMethod);

            // ── 2. SPLITTER ─
            var splitter = new SplitterEquipment("SPL-100");
            splitter.AddInlet(inlet);
            splitter.AddOutlet(outlet1);
            splitter.AddOutlet(outlet2);

            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(splitter);

            // ─ PASO 1: Composición + Temperatura (Calcula h) ──
            Console.WriteLine("📌 PASO 1: Composición 80/20 y T = 30 °C");
            var comps = inlet.Composition.Components;
            if (comps.Count >= 2)
            {
                comps[0].MassFraction.SetValue(new Percentage(80, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                comps[1].MassFraction.SetValue(new Percentage(20, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            }
            orchestrator.RunSimulation();
            inlet.Temperature.SetValue(new Temperature(30, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            PrintSplitterValidation(inlet, outlet1, outlet2, splitter, "Paso 1: T y Composición");

            // ── PASO 2: Presiones (Propaga por splitter) ──
            Console.WriteLine("\n📌 PASO 2: P_inlet = 10.0 bara");
            inlet.Pressure.SetValue(new Pressure(10, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            PrintSplitterValidation(inlet, outlet1, outlet2, splitter, "Paso 2: Presión definida");

            // ── PASO 3: Cierre de Masa (ṁ_in + ṁ_out2) ──
            Console.WriteLine("\n PASO 3: ṁ_inlet = 200 kg/h | ṁ_outlet2 = 80 kg/h");
            inlet.MassFlow.SetValue(new MassFlow(200, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            outlet2.MassFlow.SetValue(new MassFlow(80, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            PrintSplitterValidation(inlet, outlet1, outlet2, splitter, "Paso 3: Balances cerrados");

            Console.WriteLine("\n✅ VALIDACIÓN FINALIZADA");
        }
        private void PrintSplitterValidation(FacadeStream inlet, FacadeStream out1, FacadeStream out2,
                                     SplitterEquipment splitter, string stepLabel)
        {
            Console.WriteLine($"\n   📋 {stepLabel}");
            Console.WriteLine("   ┌─────────────────────────────────────────────────────────────┐");
            Console.WriteLine("   │  P (bara) │  T (°C)  │   (kg/h)  │  h (kcal/kg)          │");

            // ✅ 100% UI: ToUiString() se encarga de formato, unidades y "---" si no está definido
            Console.WriteLine($"   │  In:  {inlet.Pressure.ToUiString("F2"),6}  │  {inlet.Temperature.ToUiString("F1"),6}  │  {inlet.MassFlow.ToUiString("F1"),8}  │  {inlet.MassEnthalpy.ToUiString("F2"),10}  │");
            Console.WriteLine($"   │  O1:  {out1.Pressure.ToUiString("F2"),6}  │  {out1.Temperature.ToUiString("F1"),6}  │  {out1.MassFlow.ToUiString("F1"),8}  │  {out1.MassEnthalpy.ToUiString("F2"),10}  │");
            Console.WriteLine($"   │  O2:  {out2.Pressure.ToUiString("F2"),6}  │  {out2.Temperature.ToUiString("F1"),6}  │  {out2.MassFlow.ToUiString("F1"),8}  │  {out2.MassEnthalpy.ToUiString("F2"),10}  │");
            Console.WriteLine("   ├─────────────────────────────────────────────────────────────┤");

            // ✅ Cálculo de balances solo para validación visual
            // Usamos GetSolverValue() porque trabaja en unidades internas consistentes (evita errores de conversión en residuos)
            double mIn = inlet.MassFlow.GetSolverValue();
            double mO1 = out1.MassFlow.GetSolverValue();
            double mO2 = out2.MassFlow.GetSolverValue();

            double hIn = inlet.MassEnthalpy.GetSolverValue();
            double hO1 = out1.MassEnthalpy.GetSolverValue();
            double hO2 = out2.MassEnthalpy.GetSolverValue();

            // Validamos si los datos están físicamente disponibles antes de calcular balances
            bool massOk = inlet.MassFlow.IsDefined && out1.MassFlow.IsDefined && out2.MassFlow.IsDefined;
            bool energyOk = massOk && inlet.MassEnthalpy.IsDefined && out1.MassEnthalpy.IsDefined && out2.MassEnthalpy.IsDefined;

            string massBal = massOk ? $"{mIn - (mO1 + mO2),8:F2}" : "---";
            string energyBal = energyOk ? $"{(mIn * hIn) - (mO1 * hO1 + mO2 * hO2),8:F2}" : "---";

            string massStatus = massOk && Math.Abs(mIn - (mO1 + mO2)) < 1.0 ? "✅" : "";
            string energyStatus = energyOk && Math.Abs((mIn * hIn) - (mO1 * hO1 + mO2 * hO2)) < 1.0 ? "✅" : "";

            Console.WriteLine($"   │  BALANCE MASA   : {massBal,-12} {massStatus}");
            Console.WriteLine($"   │  BALANCE ENERGÍA: {energyBal,-12} {energyStatus}");
            Console.WriteLine("   ─────────────────────────────────────────────────────────────┘");
        }


        public void Run1()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            // ─────────────────────────────────────────────────────────
            // CONFIGURACIÓN TOPOLOGÍA: Pump → Valve → Splitter → 2 ramas
            // ─────────────────────────────────────────────────────────

            // Corrientes principales (con Name para el printer)
            var inlet = new FacadeStream { Name = "Inlet" };
            var afterPump = new FacadeStream { Name = "AfterP-101" };
            var afterValve = new FacadeStream { Name = "AfterV-201" };
            var branch1 = new FacadeStream { Name = "Branch1" };
            var branch2 = new FacadeStream { Name = "Branch2" };
            var outlet1 = new FacadeStream { Name = "Outlet1" };
            var outlet2 = new FacadeStream { Name = "Outlet2" };

            // Configurar método termodinámico en todas las corrientes
            List<FacadeStream> streams = new List<FacadeStream> { inlet, afterPump, afterValve, branch1, branch2, outlet1, outlet2 };
            foreach (var s in streams) s.SetThermodynamicMethod(ThermoMethod);

            // Lista maestra para imprimir (orden lógico de flujo)
            List<FacadeStream> printList = streams;

            // Equipos
            var pumpMain = new PumpEquipment("P-101");
            var valveMain = new ValveEquipment("V-201");
            var splitter = new SplitterEquipment("S-301");
            var valveBranch1 = new ValveEquipment("V-401");
            var pumpBranch2 = new PumpEquipment("P-501");

            // Conectar topología
            // Troncal: Inlet → P-101 → V-201 → Splitter
            pumpMain.AddInlet(inlet); pumpMain.AddOutlet(afterPump);
            valveMain.AddInlet(afterPump); valveMain.AddOutlet(afterValve);
            splitter.AddInlet(afterValve);

            // Rama 1: Splitter → V-401 → Outlet1
            splitter.AddOutlet(branch1);
            valveBranch1.AddInlet(branch1); valveBranch1.AddOutlet(outlet1);

            // Rama 2: Splitter → P-501 → Outlet2
            splitter.AddOutlet(branch2);
            pumpBranch2.AddInlet(branch2); pumpBranch2.AddOutlet(outlet2);

            // Orquestador
            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(pumpMain);
            orchestrator.AddEquipment(valveMain);
            orchestrator.AddEquipment(splitter);
            orchestrator.AddEquipment(valveBranch1);
            orchestrator.AddEquipment(pumpBranch2);

            Console.WriteLine("🚀 EJEMPLO 3: SISTEMA CON SPLITTER + ENTRADAS EN DESORDEN");
            Console.WriteLine("==========================================================\n");
            Console.WriteLine("   Topología: P-101 → V-201 → S-301 → [V-401→Out1 | P-501→Out2]");
            Console.WriteLine("   Estrategia: Inputs en orden arbitrario para validar convergencia\n");

            // ─────────────────────────────────────────────────────────
            // PASO 1: Definir composición en SALIDA 2 (rama con bomba)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("📌 PASO 1: Definir composición en OUTLET2 (80% A / 20% B)");
            var out2Comps = outlet2.Composition.Components;
            if (out2Comps.Count >= 2)
            {
                out2Comps[0].MassFraction.SetValue(new Percentage(80, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                out2Comps[1].MassFraction.SetValue(new Percentage(20, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            }
            orchestrator.RunSimulation();
            AllPrinter(printList, "Paso 1: Comp. en Outlet2");

            // ─────────────────────────────────────────────────────────
            // PASO 2: Definir ΔP en válvula de RAMA 1 (V-401)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 2: Definir ΔP_V-401 = 1.5 bar (rama 1)");
            valveBranch1.DeltaP.SetValue(new PressureDrop(1.5, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(printList, "Paso 2: ΔP en V-401");

            // ─────────────────────────────────────────────────────────
            // PASO 3: Definir P_outlet1 - cierra presión en rama 1
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 3: Definir P_outlet1 = 8.0 bara");
            outlet1.Pressure.SetValue(new Pressure(8, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(printList, "Paso 3: P_outlet1 definido");

            // ─────────────────────────────────────────────────────────
            // PASO 4: Definir ΔP en bomba principal (P-101)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 4: Definir ΔP_P-101 = 6.0 bar");
            pumpMain.DeltaP.SetValue(new PressureDrop(6, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(printList, "Paso 4: ΔP_P-101 definido");

            // ─────────────────────────────────────────────────────────
            // PASO 5: Definir flujo másico en ENTRADA del sistema
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 5: Definir ṁ_inlet = 200 kg/h");
            inlet.MassFlow.SetValue(new MassFlow(200, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(printList, "Paso 5: ṁ_inlet definido");

            // ─────────────────────────────────────────────────────────
            // PASO 6: Definir ΔP en bomba de RAMA 2 (P-501)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 6: Definir ΔP_P-501 = 3.0 bar (rama 2)");
            pumpBranch2.DeltaP.SetValue(new PressureDrop(3, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(printList, "Paso 6: ΔP_P-501 definido");

            // ─────────────────────────────────────────────────────────
            // PASO 7: Definir ΔP en válvula principal (V-201) - cierra troncal
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 7: Definir ΔP_V-201 = 2.0 bar (troncal)");
            valveMain.DeltaP.SetValue(new PressureDrop(2, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(printList, "Paso 7: ΔP_V-201 definido");

            // ─────────────────────────────────────────────────────────
            // PASO 8: Definir TEMPERATURA en RAMA 1 (cierra entalpía)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 8: Definir T_outlet1 = 30 °C (rama 1)");
            outlet1.Temperature.SetValue(new Temperature(30, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(printList, "Paso 8: T_outlet1 definida");

            // ─────────────────────────────────────────────────────────
            // PASO 9: Definir FLUJO MÁSICO en RAMA 2 (cierra balance)
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n📌 PASO 9: Definir ṁ_branch2 = 80 kg/h (rama 2)");
            branch2.MassFlow.SetValue(new MassFlow(80, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(printList, "Paso 9: SISTEMA COMPLETO ✅");

            Console.WriteLine("\n✅ EJEMPLO 3 FINALIZADO - Sistema con splitter validado");
        }
        public void RunHex()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            Console.WriteLine("🚀 INICIO: TEST DE INTERCAMBIADOR DE CALOR (PASO A PASO)");
            Console.WriteLine("===================================================================\n");

            // ─────────────────────────────────────────────────────────
            // 1. INSTANCIACIÓN Y CONEXIÓN FÍSICA (Topología)
            // ─────────────────────────────────────────────────────────

            // Corrientes Lado Caliente (Ej: Gas caliente a enfriar)
            var hotInlet = new FacadeStream { Name = "Hot_In" };
            var hotOutlet = new FacadeStream { Name = "Hot_Out" };

            // Corrientes Lado Frío (Ej: Agua de Enfriamiento)
            var coldInlet = new FacadeStream { Name = "Cold_In" };
            var coldOutlet = new FacadeStream { Name = "Cold_Out" };

            var allStreams = new List<FacadeStream> { hotInlet, hotOutlet, coldInlet, coldOutlet };
            foreach (var s in allStreams) s.SetThermodynamicMethod(ThermoMethod);

            // Instanciar el Intercambiador
            var hex = new HeatExchangerEquipment("E-100");
            hex.ConnectHotSide(hotInlet, hotOutlet);
            hex.ConnectColdSide(coldInlet, coldOutlet);

            // Instanciar el Orquestador
            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(hex);

            Console.WriteLine("✅ Topología construida: E-100 (HotIn->HotOut | ColdIn->ColdOut)");
            Console.WriteLine("Presiona Enter para comenzar a inyectar datos...");


            // ─────────────────────────────────────────────────────────
            // 2. INYECCIÓN PASO A PASO (Con simulación e impresión)
            // ─────────────────────────────────────────────────────────

            // --- PASO A: Especificar el Fluido Caliente (Completo) ---
            Console.WriteLine("\n🔹 PASO A: Especificando fluido caliente de entrada (Hot_In)...");

            hotInlet.Pressure.SetValue(new Pressure(2.5, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO A: Especificando fluido caliente de entrada (Hot_In)...");

            hotInlet.Temperature.SetValue(new Temperature(200, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO A: Especificando fluido caliente de entrada (Hot_In)...");

            hotInlet.MassFlow.SetValue(new MassFlow(5000, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO A: Especificando fluido caliente de entrada (Hot_In)...");

            // Composición: Mezcla binaria
            var hotComps = hotInlet.Composition.Components;
            if (hotComps.Count >= 2)
            {
                hotComps[0].MassFraction.SetValue(new Percentage(60, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                hotComps[1].MassFraction.SetValue(new Percentage(40, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            }

            // Disparamos simulación
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "ESTADO TRAS PASO A: Fluido Caliente Definido");
            // Qué debería pasar: La Fase 2 empuja la masa y concentración a Hot_Out. La presión se propaga si deltaP=0. 
            // Entalpía y Temp no pasan porque la Fase 3 del HEX lo impide (el calor cambia).



            // --- PASO B: Especificar Hidráulica del Intercambiador ---
            Console.WriteLine("\n🔹 PASO B: Definiendo caídas de presión en E-100 (DeltaP)...");

            hex.DeltaPHot.SetValue(new PressureDrop(0.5, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "ESTADO TRAS PASO B: Caídas de Presión Definidas");

            hex.DeltaPCold.SetValue(new PressureDrop(0.2, PressureDropUnits.Bar), VariableDataProcedence.UserInput);

            orchestrator.RunSimulation();
            AllPrinter(allStreams, "ESTADO TRAS PASO B: Caídas de Presión Definidas");
            // Qué debería pasar: Hot_Out ahora tendrá 9.5 bara. Cold_Out no tendrá presión aún (falta Cold_In).



            // --- PASO C: Especificar el Fluido Frío (Entrada parcial) ---
            Console.WriteLine("\n🔹 PASO C: Definiendo presión y temperatura del agua de enfriamiento (Cold_In)...");

            coldInlet.Pressure.SetValue(new Pressure(3, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "ESTADO TRAS PASO C: Cold_In definido (Falta Flujo o Calor)");

            coldInlet.Temperature.SetValue(new Temperature(25, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "ESTADO TRAS PASO C: Cold_In definido (Falta Flujo o Calor)");


            // Composición: Suponemos agua pura (o el componente que tengas de índice 0)
            var coldComps = coldInlet.Composition.Components;
            coldComps[0].MassFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            coldComps[1].MassFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDataProcedence.UserInput);


            orchestrator.RunSimulation();
            AllPrinter(allStreams, "ESTADO TRAS PASO C: Cold_In definido (Falta Flujo o Calor)");
            // Qué debería pasar: Cold_In calcula su entalpía de entrada. Cold_Out recibe la presión (2.8 bara).
            // Aún no hay convergencia global porque el sistema está subespecificado (Falta Q o el flujo de agua).



            // --- PASO D: Cerrar los Grados de Libertad (Fijar T de salida caliente) ---
            Console.WriteLine("\n🔹 PASO D: Fijando Temperatura de salida deseada para Hot_Out...");
            // Esto es un diseño conceptual: Forzamos la salida caliente a 60°C. 
            // El simulador deberá calcular el Calor (Q) necesario Y el flujo de agua fría (Cold_In MassFlow) que lo logre.

            hotOutlet.Temperature.SetValue(new Temperature(60, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "ESTADO TRAS PASO D: Hot_Out definido (Falta Flujo o Calor)");
            // Para no tener grados de libertad infinitos, limitamos la temperatura de salida del agua a 45°C
            coldOutlet.Temperature.SetValue(new Temperature(45, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);

            orchestrator.RunSimulation();
            AllPrinter(allStreams, "ESTADO FINAL: Convergencia Total del Intercambiador");
            Console.WriteLine($"\n💡 RESULTADO DEL SOLVER PARA E-100:");
            Console.WriteLine($"Calor Transferido (Q): {hex.Q.ToUiString("F0")}");
         

            // Qué debería pasar: 
            // 1. Hot_Out calcula su entalpía final (a 60°C y 9.5 bara).
            // 2. La matriz (Fase 3) despeja el calor exacto Q que se robó del lado caliente.
            // 3. Con ese Q y las entalpías frías definidas (25°C y 45°C), la matriz despeja el MassFlow exacto que debe tener Cold_In.

            Console.WriteLine("\n✅ TEST COMPLETO FINALIZADO.");
        }

        /// </summary>
        void AllPrinter(List<FacadeStream> streams, string stepLabel)
        {
            Console.WriteLine($"\n📋 {stepLabel}");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            // ── 1. PRESIONES ───────────────────────────────────────────
            Console.WriteLine("PRESIONES (bara)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.Pressure.ToUiString("F2")}");
            Console.WriteLine();

            // ── 2. TEMPERATURAS ────────────────────────────────────────
            Console.WriteLine("TEMPERATURAS (°C)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.Temperature.ToUiString("F1")}");
            Console.WriteLine();

            // ── 3. ENTALPÍAS ──────────────────────────────────────────
            Console.WriteLine("ENTALPÍAS MÁSICAS (kcal/kg)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.MassEnthalpy.ToUiString("F2")}");
            Console.WriteLine();

            // ── 4. FLUJO MÁSICO TOTAL ─────────────────────────────────
            Console.WriteLine("FLUJO MÁSICO TOTAL (kg/h)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.MassFlow.ToUiString("F1")}");
            Console.WriteLine();

            // ── 5. FLUJO ENTALPÍA TOTAL ───────────────────────────────
            Console.WriteLine("FLUJO ENTALPÍA TOTAL (kcal/h)"); // Asumiendo kcal/h
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.EnthalpyFlow.ToUiString("F1")}");
            Console.WriteLine();

            // ── 6. FRACCIÓN DE VAPOR (%) ──────────────────────────────
            Console.WriteLine("FRACCIÓN DE VAPOR (%)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.VaporFraction.ToUiString("F1")}");
            Console.WriteLine();

            // ── 7. COMPOSICIÓN MÁSICA (%) ─────────────────────────────
            Console.WriteLine("COMPOSICIÓN MÁSICA (%)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            var comps = streams.First().Composition.Components;
            foreach (var c in comps)
            {
                Console.WriteLine($"  {c.Name}:");
                foreach (var s in streams)
                {
                    var comp = s.Composition.Components.FirstOrDefault(x => x.Name == c.Name);
                    Console.WriteLine($"    {s.Name ?? "Stream",-12}: {comp?.MassFraction.ToUiString("F1") ?? "---"}%");
                }
            }
            Console.WriteLine();

            // ── 8. FLUJO MÁSICO DE COMPONENTES (kg/h) ─────────────────
            Console.WriteLine("FLUJO MÁSICO DE COMPONENTES (kg/h)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var c in comps)
            {
                Console.WriteLine($"  {c.Name}:");
                foreach (var s in streams)
                {
                    var comp = s.Composition.Components.FirstOrDefault(x => x.Name == c.Name);
                    Console.WriteLine($"    {s.Name ?? "Stream",-12}: {comp?.MassFlow.ToUiString("F1") ?? "---"}");
                }
            }
            Console.WriteLine("═══════════════════════════════════════════════════════════");
        }
        public void RunHex2()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            Console.WriteLine("🚀 INICIO: TEST CONDENSADOR CONVERGENCIA CRUZADA (PASO A PASO ESTRICTO)");
            Console.WriteLine("===================================================================\n");

            // ─────────────────────────────────────────────────────────
            // 1. TOPOLOGÍA
            // ─────────────────────────────────────────────────────────
            var hotInlet = new FacadeStream { Name = "Hot_In" };
            var hotOutlet = new FacadeStream { Name = "Hot_Out" };
            var coldInlet = new FacadeStream { Name = "Cold_In" };
            var coldOutlet = new FacadeStream { Name = "Cold_Out" };

            var allStreams = new List<FacadeStream> { hotInlet, hotOutlet, coldInlet, coldOutlet };
            foreach (var s in allStreams) s.SetThermodynamicMethod(ThermoMethod);

            var hex = new HeatExchangerEquipment("E-100");
            hex.ConnectHotSide(hotInlet, hotOutlet);
            hex.ConnectColdSide(coldInlet, coldOutlet);

            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(hex);

            Console.WriteLine("✅ Topología construida. Presiona Enter para comenzar la inyección extrema...\n");


            // ─────────────────────────────────────────────────────────
            // 2. INYECCIÓN EN DESORDEN EXTREMO (1 SetValue -> 1 Solver -> 1 Print)
            // ─────────────────────────────────────────────────────────

            Console.WriteLine("\n🔹 Definiendo Caída de Presión Caliente...");
            hex.DeltaPHot.SetValue(new PressureDrop(1, PressureDropUnits.psi), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir DeltaP Hot");

            Console.WriteLine("\n🔹 Definiendo Caída de Presión Fría...");
            hex.DeltaPCold.SetValue(new PressureDrop(5, PressureDropUnits.psi), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir DeltaP Cold");

            Console.WriteLine("\n🔹 [DISPARO FINAL] Definiendo Lado Frío: Flujo Másico (85000 kg/hr)...");
            coldOutlet.MassFlow.SetValue(new MassFlow(85000, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);

            // Este último RunSimulation es el que debe amarrar las 3 fases
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "🔥 ESTADO FINAL: Convergencia Total del Condensador");

            Console.WriteLine("\n🔹 Definiendo Lado Frío: Fracción de Etanol (0%)...");
            coldOutlet.Composition.Components[0].MassFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Cold_In Etanol");

            Console.WriteLine("\n🔹 Definiendo Lado Caliente: Fracción de Etanol (90%)...");
            hotOutlet.Composition.Components[0].MassFraction.SetValue(new Percentage(90, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Hot_In Etanol");

            Console.WriteLine("\n🔹 Definiendo Lado Caliente: Fracción de Agua (10%)...");
            hotOutlet.Composition.Components[1].MassFraction.SetValue(new Percentage(10, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Hot_In Agua");

            Console.WriteLine("\n🔹 Definiendo Lado Frío: Fracción de Agua (100%)...");
            coldOutlet.Composition.Components[1].MassFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Cold_In Agua (Debería propagar comp a Cold_Out)");

            Console.WriteLine("\n🔹 Definiendo Lado Frío: Temperatura de Entrada (8 °C)...");
            coldInlet.Temperature.SetValue(new Temperature(8, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Cold_In Temperatura");

            Console.WriteLine("\n🔹 Definiendo Lado Caliente: Fracción de Vapor (100% - Vapor Saturado)...");
            hotInlet.VaporFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Hot_In Vapor Saturado (Debería calcular T_in y H_in caliente)");

            Console.WriteLine("\n🔹 Definiendo Lado Frío: Presión de Entrada (1.013 bara)...");
            coldOutlet.Pressure.SetValue(new Pressure(4, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Cold_In Presión (Debería calcular Entalpías frías)");

            Console.WriteLine("\n🔹 Definiendo Lado Caliente: Flujo Másico (5000 kg/hr)...");
            hotOutlet.MassFlow.SetValue(new MassFlow(5000, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Hot_In Flujo Másico");

            Console.WriteLine("\n🔹 Definiendo Lado Frío: Temperatura de Salida (20 °C)...");
            coldOutlet.Temperature.SetValue(new Temperature(20, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Cold_Out Temperatura");


            Console.WriteLine("\n🔹 Definiendo Lado Caliente: Presión (2.736 bara)...");
            hotOutlet.Pressure.SetValue(new Pressure(2.736, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "Tras definir Hot_In Presión");





            // ─────────────────────────────────────────────────────────
            // 3. EL DISPARO FINAL (Cierre de Grados de Libertad)
            // ─────────────────────────────────────────────────────────


            // ─────────────────────────────────────────────────────────
            // 4. VERIFICACIÓN DE RESULTADOS
            // ─────────────────────────────────────────────────────────
            Console.WriteLine($"\n💡 RESULTADOS DEL EQUIPO E-100:");
            Console.WriteLine($"Calor Transferido (Q_Cold): {hex.Q.ToUiString("F0")}");


            Console.WriteLine($"\n🌡️ VERIFICACIÓN FÍSICA EN CORRIENTE Hot_Out:");
            Console.WriteLine($"Temperatura Final : {hotOutlet.Temperature.ToUiString("F2")}");
            Console.WriteLine($"Fracción de Vapor : {hotOutlet.VaporFraction.ToUiString("F4")} (Debe ser < 1.0 si hubo condensación)");

            Console.WriteLine("\n✅ TEST FINALIZADO.");
        }
        public void RunTambor()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            Console.WriteLine("🚀 INICIO: TEST TAMBOR SEPARADOR V-100 (FLASH DRUM) - CONTINUIDAD DE PLANTA");
            Console.WriteLine("===================================================================\n");

            // ─────────────────────────────────────────────────────────
            // A. TOPOLOGÍA
            // ─────────────────────────────────────────────────────────
            var feed = new FacadeStream { Name = "Feed" }; // Esta es tu antigua Hot_Out
            var vaporOut = new FacadeStream { Name = "Vapor_Out" };
            var liquidOut = new FacadeStream { Name = "Liquid_Out" };

            var allStreams = new List<FacadeStream> { feed, vaporOut, liquidOut };
            foreach (var s in allStreams) s.SetThermodynamicMethod(ThermoMethod);

            var drum = new SeparatorDrumEquipment("V-100");
            drum.AddFeed(feed);
            drum.ConnectVaporOutlet(vaporOut);
            drum.ConnectLiquidOutlet(liquidOut);

            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(drum);

            Console.WriteLine("✅ Topología V-100 construida. Presiona Enter para iniciar secuencia...\n");


            // ─────────────────────────────────────────────────────────
            // B. INYECCIÓN EN DESORDEN EXTREMO (Datos del Intercambiador)
            // ─────────────────────────────────────────────────────────

            Console.WriteLine("\n🔹 [Paso 1] Definiendo Heat Duty (Adiabático = 0 Kcal/hr)...");




            Console.WriteLine("\n🔹 [Paso 3] Definiendo Salida Vapor: Fracción de Vapor (100%)...");
            vaporOut.VaporFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 3] Tras definir Vapor_Out VF=100%");

            Console.WriteLine("\n🔹 [Paso 4] Definiendo Salida Líquido: Fracción de Vapor (0%)...");
            liquidOut.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 4] Tras definir Liquid_Out VF=0%");

            Console.WriteLine("\n🔹 [Paso 5] Definiendo Alimentación: Composición Etanol (90%)...");
            feed.Composition.Components[0].MassFraction.SetValue(new Percentage(90, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 5] Tras definir Feed Etanol");

            Console.WriteLine("\n🔹 [Paso 6] Definiendo Alimentación: Composición Agua (10%)...");
            feed.Composition.Components[1].MassFraction.SetValue(new Percentage(10, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 6] Tras definir Feed Agua");

            Console.WriteLine("\n🔹 [Paso 7] Definiendo Alimentación: Flujo Másico (5000 kg/hr)...");
            feed.MassFlow.SetValue(new MassFlow(5000, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 7] Tras definir Feed Masa");

            Console.WriteLine("\n🔹 [Paso 8] Definiendo Alimentación: Temperatura (105.72 °C)...");
            feed.Temperature.SetValue(new Temperature(105.72, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 8] Tras definir Feed Temperatura");

            // ─────────────────────────────────────────────────────────
            // C. DISPARO FINAL
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n🔹 [Paso 9] [DISPARO FINAL] Definiendo Alimentación: Presión (2.736 bara)...");
            feed.Pressure.SetValue(new Pressure(2.736, PressureUnits.Bara), VariableDataProcedence.UserInput);

            // Este último gatillo cierra los grados de libertad de la entrada
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "🔥 ESTADO FINAL: Convergencia Tambor V-100");

            Console.WriteLine("\n✅ TEST FINALIZADO.");
        }
        public void RunTamb3()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            Console.WriteLine("🚀 INICIO: TEST SISTEMA (VÁLVULA VLV-100 + TAMBOR V-100)");
            Console.WriteLine("===================================================================\n");

            // ─────────────────────────────────────────────────────────
            // A. TOPOLOGÍA DE RED (Source -> Válvula -> DrumFeed -> Tambor)
            // ─────────────────────────────────────────────────────────
            var sourceStream = new FacadeStream { Name = "Source_Feed" }; // Entrada alta presión
            var drumFeed = new FacadeStream { Name = "Drum_Feed" };       // Tubería entre válvula y tambor (Baja presión)
            var vaporOut = new FacadeStream { Name = "Vapor_Out" };
            var liquidOut = new FacadeStream { Name = "Liquid_Out" };

            var allStreams = new List<FacadeStream> { sourceStream, drumFeed, vaporOut, liquidOut };
            foreach (var s in allStreams) s.SetThermodynamicMethod(ThermoMethod);

            // 1. Conectar la Válvula
            var valve = new ValveEquipment("VLV-100");
            valve.AddInlet(sourceStream);
            valve.AddOutlet(drumFeed); // La válvula inyecta a la tubería intermedia

            // 2. Conectar el Tambor Separador
            var drum = new SeparatorDrumEquipment("V-100");
            drum.AddFeed(drumFeed);    // El tambor se alimenta de la tubería intermedia
            drum.ConnectVaporOutlet(vaporOut);
            drum.ConnectLiquidOutlet(liquidOut);

            // 3. Orquestador
            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(valve);
            orchestrator.AddEquipment(drum);

            Console.WriteLine("✅ Topología VLV-100 -> V-100 construida. Presiona Enter para iniciar...\n");

            // ─────────────────────────────────────────────────────────
            // B. INYECCIÓN EN DESORDEN EXTREMO
            // ─────────────────────────────────────────────────────────



            Console.WriteLine("\n🔹 [Paso 3] Definiendo Válvula: DeltaP para llegar a 1 psig...");
            valve.DeltaP.SetValue(new PressureDrop(1.654, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 3] Tras definir Válvula DeltaP (1.654 bar)");

            Console.WriteLine("\n🔹 [Paso 4] Definiendo Origen: Composición Etanol (90%)...");
            sourceStream.Composition.Components[0].MassFraction.SetValue(new Percentage(90, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 4] Tras definir Feed Etanol (90%)");

            Console.WriteLine("\n🔹 [Paso 5] Definiendo Origen: Composición Agua (10%)...");
            sourceStream.Composition.Components[1].MassFraction.SetValue(new Percentage(10, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 5] Tras definir Feed Agua (10%)");

            Console.WriteLine("\n🔹 [Paso 6] Definiendo Origen: Flujo Másico (5000 kg/hr)...");
            sourceStream.MassFlow.SetValue(new MassFlow(5000, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 6] Tras definir Feed Masa (5000 kg/hr)");

            Console.WriteLine("\n🔹 [Paso 7] Definiendo Origen: Temperatura (105.72 °C)...");
            sourceStream.Temperature.SetValue(new Temperature(105.72, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 7] Tras definir Feed Temperatura (105.72 °C)");

            // ─────────────────────────────────────────────────────────
            // C. DISPARO FINAL
            // ─────────────────────────────────────────────────────────
            Console.WriteLine("\n🔹 [Paso 8] [DISPARO FINAL] Definiendo Origen: Presión (2.736 bara)...");
            sourceStream.Pressure.SetValue(new Pressure(2.736, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "🔥 ESTADO FINAL: Convergencia Planta (Válvula + Tambor)");

            Console.WriteLine("\n✅ TEST FINALIZADO.");
        }
        public void Run10()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            Console.WriteLine("🚀 INICIO: TEST ORDENADO (FLUJO FÍSICO L-TO-R)");
            Console.WriteLine("===================================================================\n");

            // ─────────────────────────────────────────────────────────
            // A. TOPOLOGÍA DE RED
            // ─────────────────────────────────────────────────────────
            var sourceStream = new FacadeStream { Name = "Source_Feed" };
            var drumFeed = new FacadeStream { Name = "Drum_Feed" };
            var vaporOut = new FacadeStream { Name = "Vapor_Out" };
            var liquidOut = new FacadeStream { Name = "Liquid_Out" };

            var allStreams = new List<FacadeStream> { sourceStream, drumFeed, vaporOut, liquidOut };
            foreach (var s in allStreams) s.SetThermodynamicMethod(ThermoMethod);

            var valve = new ValveEquipment("VLV-100");
            valve.AddInlet(sourceStream);
            valve.AddOutlet(drumFeed);

            var drum = new SeparatorDrumEquipment("V-100");
            drum.AddFeed(drumFeed);
            drum.ConnectVaporOutlet(vaporOut);
            drum.ConnectLiquidOutlet(liquidOut);

            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(valve);
            orchestrator.AddEquipment(drum);

            sourceStream.Temperature.SetValue(new Temperature(105.72, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 1");



            sourceStream.Composition.Components[0].MassFraction.SetValue(new Percentage(90, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            sourceStream.Composition.Components[1].MassFraction.SetValue(new Percentage(10, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 2");

            sourceStream.MassFlow.SetValue(new MassFlow(5000, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 3");


            valve.DeltaP.SetValue(new PressureDrop(5, PressureDropUnits.psi), VariableDataProcedence.UserInput);
            // Al correr aquí, Source_Feed debe calcular TODA su termodinámica interna.
            // La válvula aún no tiene DeltaP, así que la planta hacia adelante debería estar vacía o igual a la entrada.
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 4");

            sourceStream.Pressure.SetValue(new Pressure(2.736, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "[Paso 5");



            // ─────────────────────────────────────────────────────────
            // D. PASO 3: ENCENDER EL TAMBOR SEPARADOR
            // ─────────────────────────────────────────────────────────


            Console.WriteLine("\n✅ TEST FINALIZADO.");
        }
        public void Run20()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            Console.WriteLine("🚀 INICIO: TEST CONDENSADOR → VÁLVULA → TAMBOR (L-TO-R)");
            Console.WriteLine("===================================================================\n");

            // ─────────────────────────────────────────────────────────
            // A. TOPOLOGÍA DE RED
            // ─────────────────────────────────────────────────────────

            // === Lado Caliente del Condensador (Proceso) ===
            var condenserHotIn = new FacadeStream { Name = "Cond_Hot_In" };
            var condenserHotOut = new FacadeStream { Name = "Cond_Hot_Out" };  // → Válvula

            // === Lado Frío del Condensador (Servicio: Agua de Enfriamiento) ===
            var coolantIn = new FacadeStream { Name = "Coolant_In" };
            var coolantOut = new FacadeStream { Name = "Coolant_Out" };

            // === Válvula de Expansión ===
            var valveIn = condenserHotOut;  // Mismo stream (conexión física)
            var valveOut = new FacadeStream { Name = "Valve_Out" };  // → Tambor

            // === Tambor Separador ===
            var drumFeed = valveOut;  // Mismo stream
            var drumVaporOut = new FacadeStream { Name = "Drum_Vapor" };
            var drumLiquidOut = new FacadeStream { Name = "Drum_Liquid" };  // Producto final

            // Lista para impresión
            var allStreams = new List<FacadeStream>
    {
        condenserHotIn, condenserHotOut,
        coolantIn, coolantOut,
        valveOut,
        drumVaporOut, drumLiquidOut
    };
            foreach (var s in allStreams) s.SetThermodynamicMethod(ThermoMethod);

            // === Instanciar Equipos ===
            var condenser = new HeatExchangerEquipment("E-200");
            condenser.ConnectHotSide(condenserHotIn, condenserHotOut);
            condenser.ConnectColdSide(coolantIn, coolantOut);

            var valve = new ValveEquipment("VLV-200");
            valve.AddInlet(valveIn);
            valve.AddOutlet(valveOut);

            var drum = new SeparatorDrumEquipment("V-200");
            drum.AddFeed(drumFeed);
            drum.ConnectVaporOutlet(drumVaporOut);
            drum.ConnectLiquidOutlet(drumLiquidOut);

            // === Orquestador ===
            var orchestrator = new SimulationOrchestrator();
            orchestrator.AddEquipment(condenser);
            orchestrator.AddEquipment(valve);
            orchestrator.AddEquipment(drum);

            Console.WriteLine("✅ Topología: E-200 (Condensador) → VLV-200 → V-200 (Tambor)");
            Console.WriteLine("Presiona Enter para comenzar inyección de datos...\n");
            


            // ─────────────────────────────────────────────────────────
            // B. INYECCIÓN PASO A PASO (Izquierda → Derecha)
            // ─────────────────────────────────────────────────────────

            // === PASO A: Definir alimentación al condensador (Cond_Hot_In) ===
            Console.WriteLine("\n🔹 PASO A: Alimentación al condensador (Cond_Hot_In)...");

            condenserHotIn.Pressure.SetValue(new Pressure(2.0, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO A.1: P = 2.0 bara");

            condenserHotIn.Temperature.SetValue(new Temperature(120, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO A.2: T = 120°C");

            condenserHotIn.MassFlow.SetValue(new MassFlow(10000, MassFlowUnits.Kg_hr), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO A.3: F = 10000 kg/hr");

            // Composición: Mezcla binaria (ej: 80% Etanol / 20% Agua)
            var comps = condenserHotIn.Composition.Components;
            if (comps.Count >= 2)
            {
                comps[0].MassFraction.SetValue(new Percentage(80, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                comps[1].MassFraction.SetValue(new Percentage(20, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            }
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO A.4: Composición definida (80/20)");


            // === PASO B: Hidráulica del condensador ===
            Console.WriteLine("\n🔹 PASO B: Caídas de presión en E-200...");

            condenser.DeltaPHot.SetValue(new PressureDrop(0.2, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO B.1: ΔP_hot = 0.2 bar");

            condenser.DeltaPCold.SetValue(new PressureDrop(0.1, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO B.2: ΔP_cold = 0.1 bar");


            // === PASO C: Definir servicio de enfriamiento (Coolant_In) ===
            Console.WriteLine("\n🔹 PASO C: Agua de enfriamiento (Coolant_In)...");

            coolantIn.Pressure.SetValue(new Pressure(3.0, PressureUnits.Bara), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO C.1: Coolant P = 3.0 bara");

            coolantIn.Temperature.SetValue(new Temperature(8, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO C.2: Coolant T = 25°C");

            coolantOut.Temperature.SetValue(new Temperature(20, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO C.2: Coolant T = 25°C");

            // Composición: Agua pura (componente índice 1, por ejemplo)
            var coolantComps = coolantIn.Composition.Components;
            if (coolantComps.Count >= 2)
            {
                coolantComps[0].MassFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
                coolantComps[1].MassFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDataProcedence.UserInput);
            }
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO C.3: Coolant composición definida");


            // === PASO D: Especificar condición de salida del condensador ===
            Console.WriteLine("\n🔹 PASO D: Temperatura de salida del condensador (Cond_Hot_Out)...");

            // Forzamos condensación: salida a 90°C (líquido subenfriado a 2 bara)
            condenserHotOut.Temperature.SetValue(new Temperature(90, TemperatureUnits.DegreeCelcius), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO D: Cond_Hot_Out T = 90°C (condensado)");


            // === PASO E: Válvula de expansión (2 bara → 1 bara) ===
            Console.WriteLine("\n🔹 PASO E: Válvula VLV-200 (expansión 2→1 bara)...");

            // DeltaP = P_in - P_out = 2.0 - 1.0 = 1.0 bar
            valve.DeltaP.SetValue(new PressureDrop(1.0, PressureDropUnits.Bar), VariableDataProcedence.UserInput);
            orchestrator.RunSimulation();
            AllPrinter(allStreams, "PASO E: ΔP_válvula = 1.0 bar (2.0→1.0 bara)");


            // === PASO F: Tambor separador (convergencia final) ===
            Console.WriteLine("\n🔹 PASO F: Tambor V-200 (separación vapor/líquido)...");

            // El tambor calcula automáticamente VF a 1.0 bara con la entalpía que llega
            // No necesitamos especificar más: el sistema ya está determinado
            //orchestrator.RunSimulation();
            //AllPrinter(allStreams, "PASO F: CONVERGENCIA TOTAL");


            // ─────────────────────────────────────────────────────────
            // C. RESULTADOS FINALES
            // ─────────────────────────────────────────────────────────
            Console.WriteLine($"\n💡 RESULTADOS FINALES:");
            Console.WriteLine($"─────────────────────────────────────");
            Console.WriteLine($"Condensador E-200:");
            Console.WriteLine($"  • Q_transferido = {condenser.Q.ToUiString("F0")}");
            Console.WriteLine($"  • Cond_Hot_Out: T={condenserHotOut.Temperature.ToUiString("F1")}, VF={condenserHotOut.VaporFraction.ToUiString("F1")}");

            Console.WriteLine($"\nVálvula VLV-200:");
            Console.WriteLine($"  • P_in = {valveIn.Pressure.ToUiString("F2")}");
            Console.WriteLine($"  • P_out = {valveOut.Pressure.ToUiString("F2")}");
            Console.WriteLine($"  • T_out = {valveOut.Temperature.ToUiString("F1")} (flash adiabático)");

            Console.WriteLine($"\nTambor V-200:");
            Console.WriteLine($"  • Feed VF = {drumFeed.VaporFraction.ToUiString("F1")}");
            Console.WriteLine($"  • Vapor Out: F={drumVaporOut.MassFlow.ToUiString("F0")}, VF={drumVaporOut.VaporFraction.ToUiString("F1")}");
            Console.WriteLine($"  • Liquid Out: F={drumLiquidOut.MassFlow.ToUiString("F0")}, VF={drumLiquidOut.VaporFraction.ToUiString("F1")}");

            Console.WriteLine($"\n✅ TEST CONDENSADOR→VÁLVULA→TAMBOR FINALIZADO.");
        }
    }
}