using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Columns;
using Shared.UnitOperations.ControlValves;
using Shared.UnitOperations.HeatExchangers;
using Shared.UnitOperations.Helpers;
using Shared.UnitOperations.Pumps;
using Shared.UnitOperations.Streams;
using Shared.UnitOperations.Vessels;
using UnitSystem;

namespace Shared.MatrixSolvers
{

    public class SolverMatrixManager
    {
        private readonly List<IEquipmentFacade> _equipments = new();
        private readonly List<IStreamFacade> _streams = new();
        public ThermodynamicMethodFullDto? ThermoMethod => Configuration.ThermodynamicMethod;
        public SolverConfiguration Configuration { get; private set; } = new();
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto method)
        {
            Configuration.ThermodynamicMethod = method;

            foreach (var stream in _streams)
            {
                stream.SetThermodynamicMethod(method);
            }
        }





        public void RegisterStream(IStreamFacade stream)
        {
            if (stream == null) return;
            if (_streams.Contains(stream)) return;

            _streams.Add(stream);
            if (ThermoMethod != null)
                stream.SetThermodynamicMethod(ThermoMethod);
            stream.OnExecuteSolver += HandleExecuteSolver;


        }

        public void UnregisterStream(IStreamFacade stream)
        {
            if (stream == null) return;
            if (!_streams.Contains(stream)) return;

            stream.OnExecuteSolver -= HandleExecuteSolver;


            _streams.Remove(stream);

        }

        public void RegisterEquipment(IEquipmentFacade equipment)
        {
            if (equipment == null) return;
            if (_equipments.Contains(equipment)) return;
            equipment.OnExecuteSolver += HandleExecuteSolver;

            _equipments.Add(equipment);

        }

        public void UnregisterEquipment(IEquipmentFacade equipment)
        {
            if (equipment == null) return;
            if (!_equipments.Contains(equipment)) return;
            equipment.OnExecuteSolver -= HandleExecuteSolver;

            _equipments.Remove(equipment);


        }



        EquationSystem EqConc = new EquationSystem();
        EquationSystem EqPress = new EquationSystem();
        EquationSystem EqMassEnergy = new EquationSystem();
        private void HandleExecuteSolver()
        {
            EqConc.Clear();
            foreach (var eqProvider in _equipments)
            {
                var equipmentEquation = eqProvider.GetEquationConcentration();

                EqConc.CreateFromFacades(equipmentEquation);
            }
            EqConc.SolveGeneral();
            EqPress.Clear();
            foreach (var eqProvider in _equipments)
            {
                var equipmentEquation = eqProvider.GetEquationPressure();

                EqPress.CreateFromFacades(equipmentEquation);
            }
            EqPress.SolveGeneral();
            EqMassEnergy.Clear();
            foreach (var eqProvider in _equipments)
            {
                var equipmentEquation = eqProvider.GetEquationSystem();

                EqMassEnergy.CreateFromFacades(equipmentEquation);
            }
            EqMassEnergy.SolveGeneral();
        }






        void ExecuteSampleValve()
        {
            if (ThermoMethod != null)
            {
                var stream1 = new StreamFacade();
                stream1.Name = "Stream 1";
                RegisterStream(stream1);
                var stream2 = new StreamFacade();
                stream2.Name = "Stream 2";
                RegisterStream(stream2);

                var valve = new ControlValveSimulationFacade2();
                RegisterEquipment(valve);
                valve.AttachConnection("Inlet", stream1);
                valve.AttachConnection("Outlet", stream2);


                var streamComposition = stream1.StreamComposition.Value.Clone();
                streamComposition.Components[0].MassFractionSolver.SetValueFromUI(10);

                streamComposition.Components[1].MassFractionSolver.SetValueFromUI(90);
                streamComposition.CalculateMolarFractionsFromMass();

                stream1.StreamComposition.SetValueFromUI(streamComposition);
                //stream2.StreamComposition.SetValueFromUI(streamComposition);
                stream1.Temperature.SetValueFromUI(new Temperature(25, TemperatureUnits.DegreeCelcius));

                stream1.Pressure.SetValueFromUI(new Pressure(5, PressureUnits.Bara));


                stream1.MassFlow.SetValueFromUI(new MassFlow(10000, MassFlowUnits.Kg_hr));

                valve.DeltaPressure.SetValueFromUI(new PressureDrop(2, PressureDropUnits.Bar));

                valve.DeltaPressure.ClearFromUI();


                stream2.Pressure.SetValueFromUI(new Pressure(2, PressureUnits.Bara));

            }


        }
        public void ExecuteSample()
        {

        }
        void ExecuteSample1()
        {
            if (ThermoMethod != null)
            {
                var stream1 = new StreamFacade();
                stream1.Name = "Stream 1";
                RegisterStream(stream1);
                var stream2 = new StreamFacade();
                stream2.Name = "Stream 2";
                RegisterStream(stream2);

                var stream3 = new StreamFacade();
                stream3.Name = "Stream 3";
                RegisterStream(stream3);

                var pump1 = new PumpSimulationFacade2();
                RegisterEquipment(pump1);
                pump1.AttachConnection("Inlet", stream1);
                pump1.AttachConnection("Outlet", stream2);

                var valve = new ControlValveSimulationFacade2();
                RegisterEquipment(valve);
                valve.AttachConnection("Inlet", stream2);
                valve.AttachConnection("Outlet", stream3);


                var streamComposition = stream1.StreamComposition.Value.Clone();
                streamComposition.Components[0].MassFractionSolver.SetValueFromUI(10);

                streamComposition.Components[1].MassFractionSolver.SetValueFromUI(90);
                streamComposition.CalculateMolarFractionsFromMass();

                stream3.StreamComposition.SetValueFromUI(streamComposition);
                //stream2.StreamComposition.SetValueFromUI(streamComposition);
                stream1.VaporFraction.SetValueFromUI(0);

                stream1.Temperature.SetValueFromUI(new Temperature(350, TemperatureUnits.Kelvin));

                stream1.MassFlow.SetValueFromUI(new MassFlow(10000, MassFlowUnits.Kg_hr));
                pump1.Efficiency.SetValueFromUI(0.5);
                pump1.DeltaPressure.SetValueFromUI(new PressureDrop(2, PressureDropUnits.Bar));
                valve.DeltaPressure.SetValueFromUI(new PressureDrop(1, PressureDropUnits.Bar));
                pump1.DeltaPressure.ClearFromUI();
                stream2.Pressure.SetValueFromUI(new Pressure(5, PressureUnits.Bara));
                stream1.VaporFraction.ClearFromUI();



                stream1.MassFlow.ClearFromUI();

                stream2.VolumetricFlow.SetValueFromUI(new VolumetricFlow(10, VolumetricFlowUnits.m3_hr));

                stream1.Temperature.ClearFromUI();

                stream2.Temperature.SetValueFromUI(new Temperature(350, TemperatureUnits.Kelvin));
                stream1.Pressure.SetValueFromUI(new Pressure(1, PressureUnits.Bara));
            }


        }
        void ExecuteSamplePump()
        {
            if (ThermoMethod != null)
            {
                var stream1 = new StreamFacade();
                stream1.Name = "Stream 1";
                RegisterStream(stream1);
                var stream2 = new StreamFacade();
                stream2.Name = "Stream 2";
                RegisterStream(stream2);


                var pump1 = new PumpSimulationFacade2();
                RegisterEquipment(pump1);
                pump1.AttachConnection("Inlet", stream1);
                pump1.AttachConnection("Outlet", stream2);


                var streamComposition = stream1.StreamComposition.Value.Clone();
                streamComposition.Components[0].MassFractionSolver.SetValueFromUI(10);

                streamComposition.Components[1].MassFractionSolver.SetValueFromUI(90);
                streamComposition.CalculateMolarFractionsFromMass();

                stream1.StreamComposition.SetValueFromUI(streamComposition);
                //stream2.StreamComposition.SetValueFromUI(streamComposition);
                stream1.VaporFraction.SetValueFromUI(0);

                stream1.Temperature.SetValueFromUI(new Temperature(350, TemperatureUnits.Kelvin));

                stream1.MassFlow.SetValueFromUI(new MassFlow(10000, MassFlowUnits.Kg_hr));
                pump1.Efficiency.SetValueFromUI(0.5);
                pump1.DeltaPressure.SetValueFromUI(new PressureDrop(2, PressureDropUnits.Bar));

                pump1.DeltaPressure.ClearFromUI();
                stream2.Pressure.SetValueFromUI(new Pressure(5, PressureUnits.Bara));
                stream1.VaporFraction.ClearFromUI();



                stream1.MassFlow.ClearFromUI();

                stream2.VolumetricFlow.SetValueFromUI(new VolumetricFlow(10, VolumetricFlowUnits.m3_hr));

                stream1.Temperature.ClearFromUI();

                stream2.Temperature.SetValueFromUI(new Temperature(350, TemperatureUnits.Kelvin));
                stream1.Pressure.SetValueFromUI(new Pressure(1, PressureUnits.Bara));
            }


        }
        void ExecuteSample_Mixer2()
        {
            if (ThermoMethod == null) return;

            // 🔹 Streams de entrada
            var feed1 = new StreamFacade(); feed1.Name = "Feed1"; RegisterStream(feed1);
            var feed2 = new StreamFacade(); feed2.Name = "Feed2"; RegisterStream(feed2);

            // 🔹 Stream de salida
            var mixed = new StreamFacade(); mixed.Name = "Mixed"; RegisterStream(mixed);

            // 🔹 Equipo Mixer
            var mixer = new MixerSimulationFacade2();
            RegisterEquipment(mixer);
            mixer.AttachConnection("Inlet1", feed1);
            mixer.AttachConnection("Inlet2", feed2);
            mixer.AttachConnection("Outlet", mixed);

            // 🔹 Configurar composición (misma para ambos feeds)
            var comp = feed1.StreamComposition.Value.Clone();
            comp.Components[0].MassFractionSolver.SetValueFromUI(10);  // Etanol
            comp.Components[1].MassFractionSolver.SetValueFromUI(90);  // Agua
            comp.CalculateMolarFractionsFromMass();

            feed1.StreamComposition.SetValueFromUI(comp);
            feed2.StreamComposition.SetValueFromUI(comp);
            feed1.VaporFraction.SetValueFromUI(0);
            feed2.VaporFraction.SetValueFromUI(0);

            // 🔹 Especificar flujos y condiciones de entrada
            feed1.Temperature.SetValueFromUI(new Temperature(350, TemperatureUnits.Kelvin));
            feed1.MassFlow.SetValueFromUI(new MassFlow(5000, MassFlowUnits.Kg_hr));

            feed2.Temperature.SetValueFromUI(new Temperature(360, TemperatureUnits.Kelvin));
            feed2.MassFlow.SetValueFromUI(new MassFlow(3000, MassFlowUnits.Kg_hr));

            // 🔹 Presión de salida especificada → el mixer calculará presión de mezcla (mínima)
            mixed.Pressure.SetValueFromUI(new Pressure(4, PressureUnits.Bara));

            // 🔹 Limpio temperatura de salida → el solver la calculará por balance de energía
            mixed.Temperature.ClearFromUI();

            // 🔹 Ejecutar solver
            HandleExecuteSolver();

            // 🔹 Validar resultados
            Console.WriteLine($"✅ Mixer - Mixed MassFlow: {mixed.MassFlow.Value?.ValueUnit} (esperado: ~8000 kg/hr)");
            Console.WriteLine($"✅ Mixer - Mixed Temperature: {mixed.Temperature.Value?.ValueUnit}");
            Console.WriteLine($"✅ Mixer - Mixed Pressure: {mixed.Pressure.Value?.ValueUnit} (esperado: 4 bara)");
            Console.WriteLine($"✅ Mixer - IsEquilibriumSolved: {mixed.IsEquilibriumSolved}");
        }
        void ExecuteSampleSplitter()
        {
            if (ThermoMethod == null) return;

            // 🔹 Stream de entrada
            var feed = new StreamFacade(); feed.Name = "Feed"; RegisterStream(feed);

            // 🔹 Streams de salida
            var prodA = new StreamFacade(); prodA.Name = "ProductA"; RegisterStream(prodA);
            var prodB = new StreamFacade(); prodB.Name = "ProductB"; RegisterStream(prodB);

            // 🔹 Equipo Splitter
            var splitter = new SplitterSimulationFacade2();
            RegisterEquipment(splitter);
            splitter.AttachConnection("Inlet", feed);
            splitter.AttachConnection("OutletA", prodA);
            splitter.AttachConnection("OutletB", prodB);

            // 🔹 Configurar composición en feed
            var comp = feed.StreamComposition.Value.Clone();
            comp.Components[0].MassFractionSolver.SetValueFromUI(10);
            comp.Components[1].MassFractionSolver.SetValueFromUI(90);
            comp.CalculateMolarFractionsFromMass();
            feed.StreamComposition.SetValueFromUI(comp);


            // 🔹 Especificar condiciones del feed
            feed.Temperature.SetValueFromUI(new Temperature(350, TemperatureUnits.Kelvin));
            feed.Pressure.SetValueFromUI(new Pressure(5, PressureUnits.Bara));
            feed.MassFlow.SetValueFromUI(new MassFlow(10000, MassFlowUnits.Kg_hr));

            // 🔹 Especificar fracción de split para OutletA (70% del flujo va a A)
            // Nota: en una implementación real, SplitFractions se conectaría a variables del solver
            // Aquí simulamos especificando el flujo de salida A y dejando B para que el solver calcule
            prodA.MassFlow.SetValueFromUI(new MassFlow(7000, MassFlowUnits.Kg_hr));

            // 🔹 Limpio flujo de OutletB → el solver lo calculará por balance: 10000 - 7000 = 3000


            // 🔹 Ejecutar solver


            // 🔹 Validar resultados
            Console.WriteLine($"✅ Splitter - ProductA MassFlow: {prodA.MassFlow.Value?.ValueUnit} (esperado: 7000)");
            Console.WriteLine($"✅ Splitter - ProductB MassFlow: {prodB.MassFlow.Value?.ValueUnit} (esperado: ~3000)");
            Console.WriteLine($"✅ Splitter - ProductA Temperature: {prodA.Temperature.Value?.ValueUnit} (esperado: 350 K)");
            Console.WriteLine($"✅ Splitter - ProductB Pressure: {prodB.Pressure.Value?.ValueUnit} (esperado: 5 bara)");
        }
        void ExecuteSample_Splitter_1to4_MixedSpec()
        {
            if (ThermoMethod == null) return;

            // 🔹 Streams
            var inlet = new StreamFacade(); inlet.Name = "Feed"; RegisterStream(inlet);
            var out1 = new StreamFacade(); out1.Name = "Outlet1"; RegisterStream(out1);
            var out2 = new StreamFacade(); out2.Name = "Outlet2"; RegisterStream(out2);
            var out3 = new StreamFacade(); out3.Name = "Outlet3"; RegisterStream(out3);
            var out4 = new StreamFacade(); out4.Name = "Outlet4"; RegisterStream(out4);

            // 🔹 Equipo
            var splitter = new SplitterSimulationFacade2();
            RegisterEquipment(splitter);
            splitter.AttachConnection("Inlet", inlet);
            splitter.AttachConnection("Outlet1", out1);
            splitter.AttachConnection("Outlet2", out2);
            splitter.AttachConnection("Outlet3", out3);
            splitter.AttachConnection("Outlet4", out4);

            // 🔹 Configuración del Feed
            var comp = inlet.StreamComposition.Value.Clone();
            comp.Components[0].MassFractionSolver.SetValueFromUI(10); // Etanol
            comp.Components[1].MassFractionSolver.SetValueFromUI(90); // Agua
            comp.CalculateMolarFractionsFromMass();
            inlet.StreamComposition.SetValueFromUI(comp);
            inlet.VaporFraction.SetValueFromUI(0);
            inlet.Temperature.SetValueFromUI(new Temperature(350, TemperatureUnits.Kelvin));
            inlet.Pressure.SetValueFromUI(new Pressure(5, PressureUnits.Bara));
            inlet.MassFlow.SetValueFromUI(new MassFlow(20000, MassFlowUnits.Kg_hr));

            // 🔹 Especificaciones en salidas (mezcla de % y flujo)
            // Salidas 1 y 2: definidas por porcentaje
            splitter.SplitFractions["Outlet1"].SetValueFromUI(0.40); // 40%
            splitter.SplitFractions["Outlet2"].SetValueFromUI(0.25); // 25%

            // Salidas 3 y 4: definidas por flujo másico (el solver calculará sus fracciones)
            out3.MassFlow.SetValueFromUI(new MassFlow(3000, MassFlowUnits.Kg_hr));
            out4.MassFlow.SetValueFromUI(new MassFlow(2000, MassFlowUnits.Kg_hr));

            // 🔹 Ejecutar solver
            HandleExecuteSolver();

            // 🔹 Validar resultados
            Console.WriteLine("🔍 === SPLITTER 1→4 (Mezcla % y Flujo) ===");
            Console.WriteLine($"✅ Inlet MassFlow: {inlet.MassFlow.Value?.ValueUnit}");
            Console.WriteLine($"✅ Outlet1 Flow: {out1.MassFlow.Value?.ValueUnit} (esperado: ~8000)");
            Console.WriteLine($"✅ Outlet2 Flow: {out2.MassFlow.Value?.ValueUnit} (esperado: ~5000)");
            Console.WriteLine($"✅ Outlet3 Flow: {out3.MassFlow.Value?.ValueUnit} (definido: 3000)");
            Console.WriteLine($"✅ Outlet4 Flow: {out4.MassFlow.Value?.ValueUnit} (definido: 2000)");
            Console.WriteLine($"✅ f₁: {splitter.SplitFractions["Outlet1"].SolverValue:F3}");
            Console.WriteLine($"✅ f₂: {splitter.SplitFractions["Outlet2"].SolverValue:F3}");
            Console.WriteLine($"✅ f₃: {splitter.SplitFractions["Outlet3"].SolverValue:F3} (calculada)");
            Console.WriteLine($"✅ f₄: {splitter.SplitFractions["Outlet4"].SolverValue:F3} (calculada)");
            Console.WriteLine($"✅ Σfⱼ: {splitter.SplitFractions.Values.Sum(f => f.SolverValue):F3} (debe ser 1.000)");
            Console.WriteLine($"✅ Conservación: {out1.MassFlow.SolverValue + out2.MassFlow.SolverValue + out3.MassFlow.SolverValue + out4.MassFlow.SolverValue:F0} ≈ {inlet.MassFlow.SolverValue:F0}");
        }
        void ExecuteSample_Splitter_5to1_CalcInlet()
        {
            if (ThermoMethod == null) return;

            // 🔹 Streams
            var inlet = new StreamFacade(); inlet.Name = "Feed_Calculated"; RegisterStream(inlet);
            var outA = new StreamFacade(); outA.Name = "StreamA"; RegisterStream(outA);
            var outB = new StreamFacade(); outB.Name = "StreamB"; RegisterStream(outB);
            var outC = new StreamFacade(); outC.Name = "StreamC"; RegisterStream(outC);
            var outD = new StreamFacade(); outD.Name = "StreamD"; RegisterStream(outD);
            var outE = new StreamFacade(); outE.Name = "StreamE"; RegisterStream(outE);

            // 🔹 Equipo
            var splitter = new SplitterSimulationFacade2();
            RegisterEquipment(splitter);
            splitter.AttachConnection("Inlet", inlet);
            splitter.AttachConnection("OutletA", outA);
            splitter.AttachConnection("OutletB", outB);
            splitter.AttachConnection("OutletC", outC);
            splitter.AttachConnection("OutletD", outD);
            splitter.AttachConnection("OutletE", outE);

            // 🔹 Configuración común (composición y T/P iguales en todas las salidas)
            var compBase = inlet.StreamComposition.Value.Clone();
            compBase.Components[0].MassFractionSolver.SetValueFromUI(30);
            compBase.Components[1].MassFractionSolver.SetValueFromUI(70);
            compBase.CalculateMolarFractionsFromMass();

            foreach (var s in new[] { outA, outB, outC, outD, outE })
            {
                s.StreamComposition.SetValueFromUI(compBase.Clone());
                s.VaporFraction.SetValueFromUI(0);
                s.Temperature.SetValueFromUI(new Temperature(360, TemperatureUnits.Kelvin));
                s.Pressure.SetValueFromUI(new Pressure(4.5, PressureUnits.Bara));
            }

            // 🔹 Especificar FLUJO MÁSICO de las 5 salidas
            outA.MassFlow.SetValueFromUI(new MassFlow(4000, MassFlowUnits.Kg_hr));
            outB.MassFlow.SetValueFromUI(new MassFlow(3500, MassFlowUnits.Kg_hr));
            outC.MassFlow.SetValueFromUI(new MassFlow(2500, MassFlowUnits.Kg_hr));
            outD.MassFlow.SetValueFromUI(new MassFlow(2000, MassFlowUnits.Kg_hr));
            outE.MassFlow.SetValueFromUI(new MassFlow(1000, MassFlowUnits.Kg_hr));

            // ❗ INLET MassFlow y TODAS las fracciones quedan LIBRES → el solver las calcula
            inlet.MassFlow.ClearFromUI();

            // 🔹 Ejecutar solver
            HandleExecuteSolver();

            // 🔹 Validar resultados
            Console.WriteLine("\n🔍 === SPLITTER 5→1 (Calcula Entrada) ===");
            Console.WriteLine($"✅ Inlet MassFlow: {inlet.MassFlow.Value?.ValueUnit} (esperado: 13000)");
            Console.WriteLine($"✅ OutletA: {outA.MassFlow.Value?.ValueUnit}");
            Console.WriteLine($"✅ OutletB: {outB.MassFlow.Value?.ValueUnit}");
            Console.WriteLine($"✅ OutletC: {outC.MassFlow.Value?.ValueUnit}");
            Console.WriteLine($"✅ OutletD: {outD.MassFlow.Value?.ValueUnit}");
            Console.WriteLine($"✅ OutletE: {outE.MassFlow.Value?.ValueUnit}");
            Console.WriteLine($"\n✅ Fracciones calculadas:");
            foreach (var f in splitter.SplitFractions)
                Console.WriteLine($"   {f.Key}: {f.Value.SolverValue:F4}");
            Console.WriteLine($"✅ Σfⱼ: {splitter.SplitFractions.Values.Sum(f => f.SolverValue):F4} (debe ser 1.0000)");
            Console.WriteLine($"✅ T Inlet: {inlet.Temperature.Value?.ValueUnit} (igual a salidas: 360 K)");
        }
        void ExecuteSample_FlashTank2()
        {
            if (ThermoMethod == null) return;

            // 🔹 Feed al flash
            var feed = new StreamFacade(); feed.Name = "Feed"; RegisterStream(feed);

            // 🔹 Salidas: vapor y líquido
            var vapor = new StreamFacade(); vapor.Name = "Vapor"; RegisterStream(vapor);
            var liquid = new StreamFacade(); liquid.Name = "Liquid"; RegisterStream(liquid);

            // 🔹 Equipo FlashTank
            var flash = new FlashTankSimulationFacade2();
            RegisterEquipment(flash);
            flash.AttachConnection("Feed", feed);
            flash.AttachConnection("Vapor", vapor);
            flash.AttachConnection("Liquid", liquid);

            // 🔹 Configurar composición del feed
            var comp = feed.StreamComposition.Value.Clone();
            comp.Components[0].MassFractionSolver.SetValueFromUI(10);  // Etanol (más volátil)
            comp.Components[1].MassFractionSolver.SetValueFromUI(90);  // Agua
            comp.CalculateMolarFractionsFromMass();
            feed.StreamComposition.SetValueFromUI(comp);
            feed.VaporFraction.SetValueFromUI(0);  // Feed es líquido subenfriado

            // 🔹 Especificar condiciones del feed
            feed.Temperature.SetValueFromUI(new Temperature(380, TemperatureUnits.Kelvin)); // Por encima de Tb etanol
            feed.Pressure.SetValueFromUI(new Pressure(2, PressureUnits.Bara));
            feed.MassFlow.SetValueFromUI(new MassFlow(10000, MassFlowUnits.Kg_hr));

            // 🔹 Especificar presión del flash → el solver calculará T de equilibrio y fracción de vapor
            vapor.Pressure.SetValueFromUI(new Pressure(1.5, PressureUnits.Bara));
            liquid.Pressure.SetValueFromUI(new Pressure(1.5, PressureUnits.Bara)); // Misma presión

            // 🔹 Limpio temperatura y fracción de vapor → el solver las calculará por flash isentálpico
            vapor.Temperature.ClearFromUI();
            vapor.VaporFraction.ClearFromUI();

            // 🔹 Ejecutar solver
            HandleExecuteSolver();

            // 🔹 Validar resultados
            Console.WriteLine($"✅ FlashTank - Vapor Flow: {vapor.MassFlow.Value?.ValueUnit}");
            Console.WriteLine($"✅ FlashTank - Liquid Flow: {liquid.MassFlow.Value?.ValueUnit}");
            Console.WriteLine($"✅ FlashTank - Vapor Temperature: {vapor.Temperature.Value?.ValueUnit}");
            Console.WriteLine($"✅ FlashTank - Vapor Fraction: {vapor.VaporFraction.Value.ToString("F3")}");
            Console.WriteLine($"✅ FlashTank - IsEquilibriumSolved: {vapor.IsEquilibriumSolved}");
        }
        void ExecuteSample_Vessel2()
        {
            if (ThermoMethod == null) return;

            // 🔹 Entradas al vessel
            var inlet1 = new StreamFacade(); inlet1.Name = "Inlet1"; RegisterStream(inlet1);
            var inlet2 = new StreamFacade(); inlet2.Name = "Inlet2"; RegisterStream(inlet2);

            // 🔹 Salidas del vessel
            var outlet1 = new StreamFacade(); outlet1.Name = "Outlet1"; RegisterStream(outlet1);
            var outlet2 = new StreamFacade(); outlet2.Name = "Outlet2"; RegisterStream(outlet2);

            // 🔹 Equipo Vessel (mezclador perfecto)
            var vessel = new VesselSimulationFacade2();
            RegisterEquipment(vessel);
            vessel.AttachConnection("Inlet1", inlet1);
            vessel.AttachConnection("Inlet2", inlet2);
            vessel.AttachConnection("Outlet1", outlet1);
            vessel.AttachConnection("Outlet2", outlet2);

            // 🔹 Configurar composición (misma para todas las corrientes)
            var comp = inlet1.StreamComposition.Value.Clone();
            comp.Components[0].MassFractionSolver.SetValueFromUI(10);
            comp.Components[1].MassFractionSolver.SetValueFromUI(90);
            comp.CalculateMolarFractionsFromMass();

            foreach (var s in new[] { inlet1, inlet2, outlet1, outlet2 })
            {
                s.StreamComposition.SetValueFromUI(comp);
                s.VaporFraction.SetValueFromUI(0);
            }

            // 🔹 Especificar condiciones de entrada
            inlet1.Temperature.SetValueFromUI(new Temperature(350, TemperatureUnits.Kelvin));
            inlet1.Pressure.SetValueFromUI(new Pressure(5, PressureUnits.Bara));
            inlet1.MassFlow.SetValueFromUI(new MassFlow(6000, MassFlowUnits.Kg_hr));

            inlet2.Temperature.SetValueFromUI(new Temperature(370, TemperatureUnits.Kelvin));
            inlet2.Pressure.SetValueFromUI(new Pressure(5, PressureUnits.Bara));
            inlet2.MassFlow.SetValueFromUI(new MassFlow(4000, MassFlowUnits.Kg_hr));

            // 🔹 Especificar flujo de una salida → el vessel calculará la otra por balance global
            outlet1.MassFlow.SetValueFromUI(new MassFlow(7000, MassFlowUnits.Kg_hr));
            // outlet2.MassFlow queda libre → solver calcula: 10000 - 7000 = 3000

            // 🔹 Limpio temperatura de salidas → el vessel calculará T de mezcla perfecta (ponderada por flujo)
            outlet1.Temperature.ClearFromUI();
            outlet2.Temperature.ClearFromUI();

            // 🔹 Ejecutar solver
            HandleExecuteSolver();

            // 🔹 Validar resultados
            Console.WriteLine($"✅ Vessel - Outlet2 MassFlow: {outlet2.MassFlow.Value?.ValueUnit} (esperado: ~3000)");
            Console.WriteLine($"✅ Vessel - Outlet1 Temperature: {outlet1.Temperature.Value?.ValueUnit}");
            Console.WriteLine($"✅ Vessel - Outlet2 Temperature: {outlet2.Temperature.Value?.ValueUnit} (debería ser igual a Outlet1)");
            Console.WriteLine($"✅ Vessel - All pressures equal: {outlet1.Pressure.Value?.ValueUnit} == {outlet2.Pressure.Value?.ValueUnit}");
        }
        void ExecuteSample_PlateExchanger2()
        {
            if (ThermoMethod == null) return;

            // 🔹 Lado caliente (Hot)
            var hotIn = new StreamFacade(); hotIn.Name = "HotIn"; RegisterStream(hotIn);
            var hotOut = new StreamFacade(); hotOut.Name = "HotOut"; RegisterStream(hotOut);

            // 🔹 Lado frío (Cold)
            var coldIn = new StreamFacade(); coldIn.Name = "ColdIn"; RegisterStream(coldIn);
            var coldOut = new StreamFacade(); coldOut.Name = "ColdOut"; RegisterStream(coldOut);

            // 🔹 Equipo PlateExchanger
            var hx = new PlateExchangerSimulationFacade2();
            RegisterEquipment(hx);
            hx.AttachConnection("HotIn", hotIn);
            hx.AttachConnection("HotOut", hotOut);
            hx.AttachConnection("ColdIn", coldIn);
            hx.AttachConnection("ColdOut", coldOut);

            // 🔹 Configurar composiciones (pueden ser diferentes entre lados)
            var compHot = hotIn.StreamComposition.Value.Clone();
            compHot.Components[0].MassFractionSolver.SetValueFromUI(10);
            compHot.Components[1].MassFractionSolver.SetValueFromUI(90);
            compHot.CalculateMolarFractionsFromMass();
            hotIn.StreamComposition.SetValueFromUI(compHot);
            hotIn.VaporFraction.SetValueFromUI(0);

            var compCold = coldIn.StreamComposition.Value.Clone();
            compCold.Components[0].MassFractionSolver.SetValueFromUI(50);
            compCold.Components[1].MassFractionSolver.SetValueFromUI(50);
            compCold.CalculateMolarFractionsFromMass();
            coldIn.StreamComposition.SetValueFromUI(compCold);
            coldIn.VaporFraction.SetValueFromUI(0);

            // 🔹 Especificar condiciones de entrada
            hotIn.Temperature.SetValueFromUI(new Temperature(400, TemperatureUnits.Kelvin));
            hotIn.Pressure.SetValueFromUI(new Pressure(3, PressureUnits.Bara));
            hotIn.MassFlow.SetValueFromUI(new MassFlow(5000, MassFlowUnits.Kg_hr));

            coldIn.Temperature.SetValueFromUI(new Temperature(300, TemperatureUnits.Kelvin));
            coldIn.Pressure.SetValueFromUI(new Pressure(2.5, PressureUnits.Bara));
            coldIn.MassFlow.SetValueFromUI(new MassFlow(8000, MassFlowUnits.Kg_hr));

            // 🔹 Especificar temperatura de salida caliente → el intercambiador calculará Q y T_salida_fría
            hotOut.Temperature.SetValueFromUI(new Temperature(350, TemperatureUnits.Kelvin));

            // 🔹 Limpio temperatura de salida fría → el solver la calculará por balance de energía: Q_hot = -Q_cold
            coldOut.Temperature.ClearFromUI();

            // 🔹 Ejecutar solver
            HandleExecuteSolver();

            // 🔹 Validar resultados
            Console.WriteLine($"✅ PlateHX - HotOut Temperature: {hotOut.Temperature.Value?.ValueUnit} (esperado: 350 K)");
            Console.WriteLine($"✅ PlateHX - ColdOut Temperature: {coldOut.Temperature.Value?.ValueUnit} (calculada por balance)");
            Console.WriteLine($"✅ PlateHX - HotOut Pressure: {hotOut.Pressure.Value?.ValueUnit}");
            Console.WriteLine($"✅ PlateHX - ColdOut Pressure: {coldOut.Pressure.Value?.ValueUnit}");
        }
        void ExecuteSample_HeatExchanger2()
        {
            if (ThermoMethod == null) return;

            // 🔹 Lado Tubos (Tube)
            var tubeIn = new StreamFacade(); tubeIn.Name = "TubeIn"; RegisterStream(tubeIn);
            var tubeOut = new StreamFacade(); tubeOut.Name = "TubeOut"; RegisterStream(tubeOut);

            // 🔹 Lado Coraza (Shell)
            var shellIn = new StreamFacade(); shellIn.Name = "ShellIn"; RegisterStream(shellIn);
            var shellOut = new StreamFacade(); shellOut.Name = "ShellOut"; RegisterStream(shellOut);

            // 🔹 Equipo HeatExchanger
            var hx = new HeatExchangerSimulationFacade2();
            RegisterEquipment(hx);
            hx.AttachConnection("TubeIn", tubeIn);
            hx.AttachConnection("TubeOut", tubeOut);
            hx.AttachConnection("ShellIn", shellIn);
            hx.AttachConnection("ShellOut", shellOut);

            // 🔹 Configurar composiciones
            var compTube = tubeIn.StreamComposition.Value.Clone();
            compTube.Components[0].MassFractionSolver.SetValueFromUI(10);
            compTube.Components[1].MassFractionSolver.SetValueFromUI(90);
            compTube.CalculateMolarFractionsFromMass();
            tubeIn.StreamComposition.SetValueFromUI(compTube);
            tubeIn.VaporFraction.SetValueFromUI(0);

            var compShell = shellIn.StreamComposition.Value.Clone();
            compShell.Components[0].MassFractionSolver.SetValueFromUI(100); // Vapor de agua puro (servicio)
            compShell.CalculateMolarFractionsFromMass();
            shellIn.StreamComposition.SetValueFromUI(compShell);
            shellIn.VaporFraction.SetValueFromUI(1);

            // 🔹 Especificar condiciones de entrada
            tubeIn.Temperature.SetValueFromUI(new Temperature(320, TemperatureUnits.Kelvin));
            tubeIn.Pressure.SetValueFromUI(new Pressure(4, PressureUnits.Bara));
            tubeIn.MassFlow.SetValueFromUI(new MassFlow(10000, MassFlowUnits.Kg_hr));

            shellIn.Temperature.SetValueFromUI(new Temperature(450, TemperatureUnits.Kelvin)); // Vapor sobrecalentado
            shellIn.Pressure.SetValueFromUI(new Pressure(10, PressureUnits.Bara));
            shellIn.MassFlow.SetValueFromUI(new MassFlow(2000, MassFlowUnits.Kg_hr));

            // 🔹 Especificar Duty → el HX calculará temperaturas de salida
            hx.Duty.SetValueFromUI(new EnergyFlow(1.5e6, EnergyFlowUnits.Kcal_hr));

            // 🔹 Especificar ΔP para ambos lados
            hx.DeltaP_Tube.SetValueFromUI(new PressureDrop(0.3, PressureDropUnits.Bar));
            hx.DeltaP_Shell.SetValueFromUI(new PressureDrop(0.2, PressureDropUnits.Bar));

            // 🔹 Limpio temperaturas de salida → el solver las calculará por balance de energía
            tubeOut.Temperature.ClearFromUI();
            shellOut.Temperature.ClearFromUI();

            // 🔹 Ejecutar solver
            HandleExecuteSolver();

            // 🔹 Validar resultados
            Console.WriteLine($"✅ HX - TubeOut Temperature: {tubeOut.Temperature.Value?.ValueUnit}");
            Console.WriteLine($"✅ HX - ShellOut Temperature: {shellOut.Temperature.Value?.ValueUnit}");
            Console.WriteLine($"✅ HX - TubeOut Pressure: {tubeOut.Pressure.Value?.ValueUnit} (esperado: ~3.7 bara)");
            Console.WriteLine($"✅ HX - ShellOut Pressure: {shellOut.Pressure.Value?.ValueUnit} (esperado: ~9.8 bara)");
            Console.WriteLine($"✅ HX - Duty Calculated: {hx.Duty.Value?.ValueUnit}");
        }
        void ExecuteSample_Reboiler2()
        {
            if (ThermoMethod == null) return;

            // 🔹 Lado Proceso (Tubos): líquido de fondo → vapor que retorna
            var processIn = new StreamFacade(); processIn.Name = "ProcessIn"; RegisterStream(processIn);
            var processOut = new StreamFacade(); processOut.Name = "ProcessOut"; RegisterStream(processOut);

            // 🔹 Lado Servicio (Coraza): vapor de calentamiento → condensado
            var steamIn = new StreamFacade(); steamIn.Name = "SteamIn"; RegisterStream(steamIn);
            var condensateOut = new StreamFacade(); condensateOut.Name = "CondensateOut"; RegisterStream(condensateOut);

            // 🔹 Equipo Reboiler
            var reboiler = new ReboilerSimulationFacade2();
            RegisterEquipment(reboiler);
            reboiler.AttachConnection("TubeIn", processIn);
            reboiler.AttachConnection("TubeOut", processOut);
            reboiler.AttachConnection("ShellIn", steamIn);
            reboiler.AttachConnection("ShellOut", condensateOut);

            // 🔹 Configurar composición del proceso (mezcla etanol/agua)
            var compProc = processIn.StreamComposition.Value.Clone();
            compProc.Components[0].MassFractionSolver.SetValueFromUI(10);
            compProc.Components[1].MassFractionSolver.SetValueFromUI(90);
            compProc.CalculateMolarFractionsFromMass();
            processIn.StreamComposition.SetValueFromUI(compProc);
            processIn.VaporFraction.SetValueFromUI(0); // Líquido subenfriado

            // 🔹 Servicio: vapor de agua puro
            var compSteam = steamIn.StreamComposition.Value.Clone();
            compSteam.Components[0].MassFractionSolver.SetValueFromUI(100);
            compSteam.CalculateMolarFractionsFromMass();
            steamIn.StreamComposition.SetValueFromUI(compSteam);
            steamIn.VaporFraction.SetValueFromUI(1);

            // 🔹 Especificar condiciones del proceso
            processIn.Temperature.SetValueFromUI(new Temperature(370, TemperatureUnits.Kelvin));
            processIn.Pressure.SetValueFromUI(new Pressure(1.2, PressureUnits.Bara));
            processIn.MassFlow.SetValueFromUI(new MassFlow(15000, MassFlowUnits.Kg_hr));

            // 🔹 Especificar condiciones del servicio (vapor saturado a 10 bar)
            steamIn.Temperature.SetValueFromUI(new Temperature(453, TemperatureUnits.Kelvin)); // ~Tsat @ 10 bar
            steamIn.Pressure.SetValueFromUI(new Pressure(10, PressureUnits.Bara));
            steamIn.MassFlow.SetValueFromUI(new MassFlow(3000, MassFlowUnits.Kg_hr));

            // 🔹 Especificar Duty → el reboiler calculará vaporización del proceso y condensación del servicio
            reboiler.Duty.SetValueFromUI(new EnergyFlow(2.0e6, EnergyFlowUnits.Kcal_hr));

            // 🔹 Especificar ΔP
            reboiler.DeltaP_Tube.SetValueFromUI(new PressureDrop(0.1, PressureDropUnits.Bar));
            reboiler.DeltaP_Shell.SetValueFromUI(new PressureDrop(0.05, PressureDropUnits.Bar));

            // 🔹 Limpio temperaturas y fracciones de vapor de salida → el solver las calculará
            processOut.Temperature.ClearFromUI();
            processOut.VaporFraction.ClearFromUI();
            condensateOut.Temperature.ClearFromUI();
            condensateOut.VaporFraction.ClearFromUI();

            // 🔹 Ejecutar solver
            HandleExecuteSolver();

            // 🔹 Validar resultados
            Console.WriteLine($"✅ Reboiler - ProcessOut VaporFraction: {processOut.VaporFraction.Value.ToString("F3")} (esperado: >0)");
            Console.WriteLine($"✅ Reboiler - ProcessOut Temperature: {processOut.Temperature.Value?.ValueUnit}");
            Console.WriteLine($"✅ Reboiler - CondensateOut Temperature: {condensateOut.Temperature.Value?.ValueUnit} (esperado: ~Tsat @ 10 bar)");
            Console.WriteLine($"✅ Reboiler - CondensateOut VaporFraction: {condensateOut.VaporFraction.Value.ToString("F3")} (esperado: 0)");
            Console.WriteLine($"✅ Reboiler - Duty: {reboiler.Duty.Value?.ValueUnit}");
        }
        void ExecuteSample_Column2()
        {
            if (ThermoMethod == null) return;

            // 🔹 Feed a la columna
            var feed = new StreamFacade(); feed.Name = "Feed"; RegisterStream(feed);

            // 🔹 Salidas: overhead (vapor), reflux (líquido), bottoms (líquido), reboiler return (vapor)
            var overhead = new StreamFacade(); overhead.Name = "Overhead"; RegisterStream(overhead);
            var reflux = new StreamFacade(); reflux.Name = "Reflux"; RegisterStream(reflux);
            var bottoms = new StreamFacade(); bottoms.Name = "Bottoms"; RegisterStream(bottoms);
            var rebReturn = new StreamFacade(); rebReturn.Name = "RebReturn"; RegisterStream(rebReturn);

            // 🔹 Equipo Column (shortcut)
            var column = new ColumnSimulationFacade2();
            RegisterEquipment(column);
            column.AttachConnection("Feed", feed);
            column.AttachConnection("OverheadVapor", overhead);
            column.AttachConnection("Reflux", reflux);
            column.AttachConnection("BottomsLiquid", bottoms);
            column.AttachConnection("ReboilerReturn", rebReturn);

            // 🔹 Configurar composición del feed
            var comp = feed.StreamComposition.Value.Clone();
            comp.Components[0].MassFractionSolver.SetValueFromUI(10); // Etanol (LK)
            comp.Components[1].MassFractionSolver.SetValueFromUI(90); // Agua (HK)
            comp.CalculateMolarFractionsFromMass();
            feed.StreamComposition.SetValueFromUI(comp);
            feed.VaporFraction.SetValueFromUI(0.1); // Feed parcialmente vaporizado

            // 🔹 Especificar condiciones del feed
            feed.Temperature.SetValueFromUI(new Temperature(360, TemperatureUnits.Kelvin));
            feed.Pressure.SetValueFromUI(new Pressure(1.5, PressureUnits.Bara));
            feed.MassFlow.SetValueFromUI(new MassFlow(10000, MassFlowUnits.Kg_hr));

            // 🔹 Especificar relaciones operativas (grados de libertad del shortcut)
            column.RefluxRatio.SetValueFromUI(2.5);  // R = L/D = 2.5
            column.BoilupRatio.SetValueFromUI(1.8);  // V/B = 1.8

            // 🔹 Especificar flujo de bottoms → el solver calculará overhead por balance global
            bottoms.MassFlow.SetValueFromUI(new MassFlow(9000, MassFlowUnits.Kg_hr));
            // overhead.MassFlow queda libre → solver calcula: 10000 - 9000 = 1000

            // 🔹 Especificar presión de columna (misma en topo y fondo para shortcut)
            overhead.Pressure.SetValueFromUI(new Pressure(1.2, PressureUnits.Bara));
            bottoms.Pressure.SetValueFromUI(new Pressure(1.2, PressureUnits.Bara));

            // 🔹 Limpio temperaturas y composiciones de salida → el solver las calculará por balances
            overhead.Temperature.ClearFromUI();
            bottoms.Temperature.ClearFromUI();
            // Las composiciones se calculan por balance de componente + relaciones de separación implícitas

            // 🔹 Ejecutar solver
            HandleExecuteSolver();

            // 🔹 Validar resultados
            Console.WriteLine($"✅ Column - Overhead MassFlow: {overhead.MassFlow.Value?.ValueUnit} (esperado: ~1000)");
            Console.WriteLine($"✅ Column - Bottoms MassFlow: {bottoms.MassFlow.Value?.ValueUnit} (esperado: 9000)");
            Console.WriteLine($"✅ Column - Reflux MassFlow: {reflux.MassFlow.Value?.ValueUnit} (esperado: R*D = 2.5*1000 = 2500)");
            Console.WriteLine($"✅ Column - ReboilerReturn MassFlow: {rebReturn.MassFlow.Value?.ValueUnit} (esperado: V/B*B = 1.8*9000 = 16200)");
            Console.WriteLine($"✅ Column - Overhead Temperature: {overhead.Temperature.Value?.ValueUnit}");
            Console.WriteLine($"✅ Column - Bottoms Temperature: {bottoms.Temperature.Value?.ValueUnit}");
        }
    }

    public class SolverConfiguration
    {
        // Método termodinámico
        public ThermodynamicMethodFullDto? ThermodynamicMethod { get; set; }

        // 🔥 Altura sobre nivel del mar (con tu sistema de unidades)
        public NewNewVariableAmount<Length> Altitude { get;  set; }

        // 🔥 Presión atmosférica calculada (con tu sistema de unidades)
        public Pressure AtmosphericPressure { get; private set; } = new Pressure(101325, PressureUnits.Pascala);

        // 🔥 Evento para notificar cambios a la UI
        public event Action? ConfigurationChanged;

        public SolverConfiguration()
        {
            // Inicializar Altitude con unidad por defecto (metros) y valor 0
            Altitude = new NewNewVariableAmount<Length>(
                new Length(0, LengthUnits.Meter),
                LengthUnits.Meter,      // UnitForUI
                LengthUnits.Meter,      // UnitForSolver (internamente trabajamos en metros)
                (v, u) => new Length(v, u),
                0  // InitValue
            );

            // Suscribirse a cambios en Altitude para recalcular presión
            Altitude.ExecuteStreamCalculation += CalculateAtmosphericPressure;
        }

        /// <summary>
        /// Calcula la presión atmosférica basada en la altura usando la fórmula barométrica
        /// </summary>
        public void CalculateAtmosphericPressure()
        {
            // Fórmula barométrica simplificada para la troposfera
            const double P0 = 101325.0;      // Presión al nivel del mar (Pa)
            const double L = 0.0065;          // Lapse rate (K/m)
            const double T0 = 288.15;         // Temperatura estándar (K)

            // Obtener altura en metros para el cálculo
            double altitudeMeters = Altitude.Value.GetValue(LengthUnits.Meter);

            // Validar rango (troposfera: 0-11000 m)
            if (altitudeMeters < 0) altitudeMeters = 0;
            if (altitudeMeters > 11000) altitudeMeters = 11000;

            // Calcular presión en Pascal
            double pressurePa = P0 * Math.Pow(1 - (L * altitudeMeters) / T0, 5.255);

            // Actualizar la presión atmosférica
            AtmosphericPressure.SetValue(pressurePa, PressureUnits.Pascala);

            // Actualizar referencia global si existe
            UnitManager.SetAtmosphericPressureReference(AtmosphericPressure);

            // Notificar a la UI
            ConfigurationChanged?.Invoke();
        }

        /// <summary>
        /// Obtiene la presión atmosférica en diferentes unidades (para display)
        /// </summary>
     
    }


}
