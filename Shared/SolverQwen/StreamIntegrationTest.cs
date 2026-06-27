using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverConsecutive.Equipments.Columns;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Shared.SolverQwen
{


    /// </summary>
    public class StreamIntegrationTest
    {

        MainSolver MainSolver = new MainSolver();
        SolverColumn Column = null!;
        SolverHeatExchanger Condenser = null!;
        SolverSplitter TopSpliter = null!;

        SolverSplitter BottomSpliter = null!;

        SolverHeatExchanger Reboiler = null!;

        IFacadeStream Distillate = null!;
        IFacadeStream ColumnFeed = null!;
        IFacadeStream ReboilerInlet = null!;
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
            MainSolver.ThermoMethod = thermoMethod;

        }

        public void RunSplitter()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            Console.WriteLine("=== SIMULACIÓN DE COLUMNA DE DESTILACIÓN COMPLETA ===\n");

            var spliter = new SolverSplitter("s-1");
            MainSolver.AddEquipment(spliter);

            var inlet = new FacadeStream("m-1");
            var outle1 = new FacadeStream("o-1");
            var outle2 = new FacadeStream("o-2");
            var outle3 = new FacadeStream("o-3");
            spliter.SetInlet(inlet);
            spliter.AddOutlet(outle1);
            spliter.AddOutlet(outle2);
            spliter.AddOutlet(outle3);

            MainSolver.AddStream(inlet);
            MainSolver.AddStream(outle3);
            MainSolver.AddStream(outle1);
            MainSolver.AddStream(outle2);

            var compo = inlet.Composition.Components;
            if (compo.Count >= 2)
            {
                compo[0].MassFraction.SetValue(new Percentage(10, PercentageUnits.Percentage), VariableDefinedBy.UserInput); // 90% Etanol en tope
                compo[1].MassFraction.SetValue(new Percentage(90, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 1");

            inlet.Pressure.SetValue(new Pressure(10, PressureUnits.Psig), VariableDefinedBy.UserInput); // 10 psig ≈ 1.70 bara

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 2");

            inlet.Temperature.SetValue(new Temperature(40, TemperatureUnits.DegreeCelcius), VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 3");

            inlet.Pressure.Clear(VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 4");

            inlet.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 5");


            inlet.VaporFraction.Clear(VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 6");

            outle3.VaporFraction.SetValue(new Percentage(50, PercentageUnits.Percentage), VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 7");

            outle3.VaporFraction.Clear(VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 8");

            inlet.Temperature.Clear(VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 9");

            outle2.Temperature.SetValue(new Temperature(60, TemperatureUnits.DegreeCelcius), VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 10");

            outle2.Temperature.Clear(VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 11");

            outle1.Pressure.SetValue(new Pressure(1, PressureUnits.Bara), VariableDefinedBy.UserInput); // 10 psig ≈ 1.70 bara

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 12");
        }
        public void Run()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }

            Console.WriteLine("=== SIMULACIÓN DE COLUMNA DE DESTILACIÓN COMPLETA ===\n");


            CreateColumn();
            CreateCondenser();
            CreateTopSplitter();

            Column.TopPressure.SetValue(new Pressure(10, PressureUnits.Psig), VariableDefinedBy.UserInput); // 10 psig ≈ 1.70 bara
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 1");

            Column.DeltaP.SetValue(new PressureDrop(2, PressureDropUnits.psi), VariableDefinedBy.UserInput); // 2 psig ≈ 0.14 bar
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 2");

            Condenser.HotInlet.VaporFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput); // Líquido saturado
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 3");

            var hotvaporoutletComp = Column.VaporOutlet!.Composition.Components;
            if (hotvaporoutletComp.Count >= 2)
            {
                hotvaporoutletComp[0].MassFraction.SetValue(new Percentage(90, PercentageUnits.Percentage), VariableDefinedBy.UserInput); // 90% Etanol en tope
                hotvaporoutletComp[1].MassFraction.SetValue(new Percentage(10, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 4");

            Condenser.HotOutlet.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput); // Líquido saturado
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 5");


            Condenser.DeltaPHot.SetValue(new PressureDrop(0.5, PressureDropUnits.Bar), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 6");



            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 7");
            var refluxSpec = new MultiplierSpecification
            {
                Source = Distillate,
                Destination = Column.RefluxInlet!,
                VariableType = SpecVariableType.TotalMassFlow,
                
            };
            TopSpliter.AddSpec(refluxSpec);

            Distillate.VolumetricFlow.SetValue(new VolumetricFlow(10, VolumetricFlowUnits.m3_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 8");

            Distillate.VolumetricFlow.SetValue(new VolumetricFlow(20, VolumetricFlowUnits.m3_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 9");

            Distillate.VolumetricFlow.Clear(VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 10");

            Distillate.MassFlow.SetValue(new MassFlow(10000, MassFlowUnits.Kg_day), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 11");


            Distillate.MassFlow.SetValue(new MassFlow(10000, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 12");

            ////TopSpliter.RemoveSpec(refluxSpec);
            //MainSolver.RunSimulation();
            //AllPrinter(MainSolver.Streams, "paso 13");

            ////var VaporSpec = new Specification
            ////{
            ////    Source = Column.RefluxInlet!,
            ////    Destination = Column.VaporOutlet!,
            ////    VariableType = SpecVariableType.TotalMassFlow,
            ////    Formula = sourceValue => sourceValue * 6.0
            ////};
            ////Column.AddSpec(VaporSpec);
            //MainSolver.RunSimulation();
            //AllPrinter(MainSolver.Streams, "paso 14");

            Column.VaporOutlet!.Composition.Clear();
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 15");

            var distilate = Distillate.Composition.Components;
            if (hotvaporoutletComp.Count >= 2)
            {
                distilate[0].MassFraction.SetValue(new Percentage(60, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                distilate[1].MassFraction.SetValue(new Percentage(40, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 16");

            Column.TopPressure.Clear(VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 17");

            Distillate.Pressure.SetValue(new Pressure(5, PressureUnits.Psia), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 18");

            Distillate.Composition.Clear();
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 19");

            var refluxcompoenet = Column.RefluxInlet!.Composition.Components;
            if (hotvaporoutletComp.Count >= 2)
            {
                refluxcompoenet[0].MassFraction.SetValue(new Percentage(92, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                refluxcompoenet[1].MassFraction.SetValue(new Percentage(8, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 20");


            var columnbotton = Column.BottomOutlet!.Composition.Components;

            if (columnbotton.Count >= 2)
            {
                columnbotton[0].MassFraction.SetValue(new Percentage(0.1, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                columnbotton[1].MassFraction.SetValue(new Percentage(100 - 0.1, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 21");



            var columnFeedComponenes = ColumnFeed!.Composition.Components;

            if (columnFeedComponenes.Count >= 2)
            {
                columnFeedComponenes[0].MassFraction.SetValue(new Percentage(8, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                columnFeedComponenes[1].MassFraction.SetValue(new Percentage(92, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 23");

            ColumnFeed!.Pressure.SetValue(new Pressure(40, PressureUnits.Psig), VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 24");

            ColumnFeed!.Temperature.SetValue(new Temperature(40, TemperatureUnits.DegreeCelcius), VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 25");


            Column.VaporInlet!.VaporFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput);



            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 26");

            Column.BottomOutlet!.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 27");



            CreateBottomSplitter();
            CreateReboiler();

            Reboiler.DeltaPCold.SetValue(new PressureDrop(0.1, PressureDropUnits.psi), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 28");

            Reboiler.DeltaPHot.SetValue(new PressureDrop(0.1, PressureDropUnits.psi), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 29");

            var SteamReboiler = new FacadeStream("steam reboiler");
            var condensatereboiler = new FacadeStream("condensate reboiler");
            MainSolver.AddStream(SteamReboiler);
            MainSolver.AddStream(condensatereboiler);

            Reboiler.SetHotInlet(SteamReboiler);
            Reboiler.SetHotOutlet(condensatereboiler);

            var condensatereboilercomponent = condensatereboiler!.Composition.Components;

            if (condensatereboilercomponent.Count >= 2)
            {
                condensatereboilercomponent[0].MassFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                condensatereboilercomponent[1].MassFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 30");

            SteamReboiler.Pressure.SetValue(new Pressure(40, PressureUnits.Psig), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 31");

            condensatereboiler.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 32");

            SteamReboiler.VaporFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 33");


            var coolingwaterinlet = new FacadeStream("cooling inlet");
            var coolingwateroutlet = new FacadeStream("cooling outlet");

            MainSolver.AddStream(coolingwaterinlet);
            MainSolver.AddStream(coolingwateroutlet);

            Condenser.SetColdInlet(coolingwaterinlet);
            Condenser.SetColdOutlet(coolingwateroutlet);

            var coolingwateroutletcomponent = coolingwateroutlet!.Composition.Components;

            if (coolingwateroutletcomponent.Count >= 2)
            {
                coolingwateroutletcomponent[0].MassFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                coolingwateroutletcomponent[1].MassFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 34");

            coolingwaterinlet.Pressure.SetValue(new Pressure(40, PressureUnits.Psig), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 35");

            coolingwateroutlet.Pressure.SetValue(new Pressure(35, PressureUnits.Psig), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 36");

            coolingwateroutlet.Temperature.SetValue(new Temperature(20, TemperatureUnits.DegreeCelcius), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 37");

            coolingwaterinlet.Temperature.SetValue(new Temperature(8, TemperatureUnits.DegreeCelcius), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 38");


            double steamMassFlow = SteamReboiler.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);
            Distillate.MassFlow.Clear(VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 39");

            SteamReboiler.MassFlow.SetValue(new MassFlow(steamMassFlow, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 40");

            SteamReboiler.MassFlow.SetValue(new MassFlow(steamMassFlow / 2, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 41");

            double FeedMassFlow = ColumnFeed.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);

            SteamReboiler.MassFlow.Clear(VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 42");

            ColumnFeed.MassFlow.SetValue(new MassFlow(FeedMassFlow, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 43");

            ColumnFeed.MassFlow.SetValue(new MassFlow(FeedMassFlow * 2, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 44");

        }
        void CreateColumn()
        {
            Column = new SolverColumn("Column-100");
            MainSolver.AddEquipment(Column);

            // Especificaciones de presión de la columna



            // Alimentación
            ColumnFeed = new FacadeStream("Feed");
            MainSolver.AddStream(ColumnFeed);
            Column.AddFeed(ColumnFeed);

            var vaporOutlet = new FacadeStream("Vapor outlet"); // Vapor que sale al tope
            MainSolver.AddStream(vaporOutlet);
            Column.SetTopVaporOutlet(vaporOutlet);

            var liquidOutlet = new FacadeStream("Liquid outlet"); // Líquido que sale al fondo
            MainSolver.AddStream(liquidOutlet);
            Column.SetBottomOutlet(liquidOutlet);

            // Recirculaciones (Reflujo y Vapor del rehervidor)
            var reflux = new FacadeStream("Reflux");
            MainSolver.AddStream(reflux);
            Column.SetRefluxInlet(reflux);

            var vaporInlet = new FacadeStream("Vapor inlet"); // Vapor que regresa del rehervidor
            MainSolver.AddStream(vaporInlet);
            Column.SetVaporInlet(vaporInlet);


        }

        void CreateCondenser()
        {
            Condenser = new SolverHeatExchanger("Hx-101");
            MainSolver.AddEquipment(Condenser);

            if (Column != null && Column.VaporOutlet != null)
            {
                var vaporinlet = Column.VaporOutlet;
                Condenser.SetHotInlet(vaporinlet);
            }
            var hotOutletCondenser = new FacadeStream("Hot OutletCondenser");
            MainSolver.AddStream(hotOutletCondenser);
            Condenser.SetHotOutlet(hotOutletCondenser);



        }
        void CreateTopSplitter()
        {
            TopSpliter = new SolverSplitter("S-1");
            MainSolver.AddEquipment(TopSpliter);

            if (Condenser != null && Column != null)
            {
                if (Condenser.HotOutlet != null)
                {
                    TopSpliter.SetInlet(Condenser.HotOutlet);

                }
                if (Column.RefluxInlet != null)
                {
                    TopSpliter.AddOutlet(Column.RefluxInlet);
                }
            }
            Distillate = new FacadeStream("Distilate");
            MainSolver.AddStream(Distillate);
            TopSpliter.AddOutlet(Distillate);
        }
        void CreateBottomSplitter()
        {
            BottomSpliter = new SolverSplitter("S-2");
            MainSolver.AddEquipment(BottomSpliter);

            if (Column != null)
            {

                if (Column.BottomOutlet != null)
                {
                    BottomSpliter.SetInlet(Column.BottomOutlet);
                }
            }
            ReboilerInlet = new FacadeStream("Reboiler Inlet");
            MainSolver.AddStream(ReboilerInlet);
            BottomSpliter.AddOutlet(ReboilerInlet);
            var Residue = new FacadeStream("Column residue");
            MainSolver.AddStream(Residue);
            BottomSpliter.AddOutlet(Residue);
        }
        void CreateReboiler()
        {
            Reboiler = new SolverHeatExchanger("Hx-102");
            MainSolver.AddEquipment(Reboiler);

            if (Column != null && Column.VaporInlet != null)
            {
                var vaporinlet = Column.VaporInlet;
                Reboiler.SetColdOutlet(vaporinlet);
            }

            if (ReboilerInlet != null)
                Reboiler.SetColdInlet(ReboilerInlet);



        }
        public void Run2()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }
            //favor hacer ejemplo de SolverColumn con una columna de destilación con 2 etapas, con alimentación en etapa 1, y con un reflux ratio definido, para probar que el modelo se ajusta automáticamente para cumplir con las condiciones dadas.
            Console.WriteLine("Simulacion de columna de destilacion con 2 etapas, con alimentacion en etapa 1, y con un reflux ratio definido");
            var column = new SolverColumn("Column-100");
            MainSolver.AddEquipment(column);

            var feed = new FacadeStream("Feed");
            MainSolver.AddStream(feed);
            column.AddFeed(feed);


            var vaporOutlet = new FacadeStream("Vapor outlet");
            MainSolver.AddStream(vaporOutlet);
            var liquidOutlet = new FacadeStream("Liquid outlet");
            MainSolver.AddStream(liquidOutlet);

            column.SetTopVaporOutlet(vaporOutlet);
            column.SetBottomOutlet(liquidOutlet);

            var reflux = new FacadeStream("Reflux");
            MainSolver.AddStream(reflux);
            column.SetRefluxInlet(reflux);

            var vaporInlet = new FacadeStream("Vapor inlet");
            MainSolver.AddStream(vaporInlet);
            column.SetVaporInlet(vaporInlet);

            var Hex1 = new SolverHeatExchanger("Hex-100");
            MainSolver.AddEquipment(Hex1);
            Hex1.SetHotInlet(vaporOutlet);
            var Hex1HotOutlet = new FacadeStream("Hex1 hot outlet");
            MainSolver.AddStream(Hex1HotOutlet);

            Hex1.SetHotOutlet(Hex1HotOutlet);
            Hex1.DeltaPHot.SetValue(new PressureDrop(0.5, PressureDropUnits.Bar), VariableDefinedBy.UserInput);

            var Splitter = new SolverSplitter("Splitter-100");
            MainSolver.AddEquipment(Splitter);
            var splitterOutlet1 = new FacadeStream("Splitter outlet 1");
            MainSolver.AddStream(splitterOutlet1);

            Splitter.SetInlet(Hex1HotOutlet);
            Splitter.AddOutlet(splitterOutlet1);

            Splitter.AddOutlet(reflux);

            var componentes = feed.Composition.Components;
            if (componentes.Count >= 2)
            {
                componentes[0].MolarFraction.SetValue(new Percentage(8, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                componentes[1].MolarFraction.SetValue(new Percentage(92, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 1");

            feed.MassFlow.SetValue(new MassFlow(10000, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 2");

            feed.Temperature.SetValue(new Temperature(25, TemperatureUnits.DegreeCelcius), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 3");



        }
        public void RunSplitter2()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }
            Console.WriteLine("Simulacion de Spliter saliendo del condensador");
            var hotsideoutlet = new FacadeStream("Hot side outlet");
            MainSolver.AddStream(hotsideoutlet);
            SolverSplitter splitter = new SolverSplitter("Splitter-100");
            MainSolver.AddEquipment(splitter);
            splitter.SetInlet(hotsideoutlet);

            var outlet1 = new FacadeStream("Outlet 1");
            MainSolver.AddStream(outlet1);
            var outlet2 = new FacadeStream("Outlet 2");
            MainSolver.AddStream(outlet2);
            splitter.AddOutlet(outlet1);
            splitter.AddOutlet(outlet2);

            hotsideoutlet.Pressure.SetValue(new Pressure(2, PressureUnits.Bara), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 1");

            hotsideoutlet.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 2");

            hotsideoutlet.MassFlow.SetValue(new MassFlow(6000, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 3");

            outlet1.MassFlow.SetValue(new MassFlow(1000, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 4");


            var componentes = outlet2.Composition.Components;
            if (componentes.Count >= 2)
            {
                componentes[0].MolarFraction.SetValue(new Percentage(90, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                componentes[1].MolarFraction.SetValue(new Percentage(10, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 5");

            outlet1.MassFlow.Clear(VariableDefinedBy.UserInput);   //limpiar para que se calcule nuevamente
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 6");


            double PercentageOutlet1 = 15.0 / 100.0;
            double flujoOutlet1 = hotsideoutlet.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr) * PercentageOutlet1;
            outlet1.MassFlow.SetValue(new MassFlow(flujoOutlet1, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);  //definir un flujo en outlet 1 que es el 10% del flujo total, para probar que el modelo se ajusta automáticamente para cumplir con esta nueva condición.
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 7");


            hotsideoutlet.MassFlow.Clear(VariableDefinedBy.UserInput);   //limpiar para que se calcule nuevamente
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 8");

            double relationFlujo = 5;
            double flujoOutlet2 = outlet1.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr) * relationFlujo;

            outlet2.MassFlow.SetValue(new MassFlow(flujoOutlet2, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);  //definir un flujo en outlet 2 que es 5 veces el flujo de outlet 1, para probar que el modelo se ajusta automáticamente para cumplir con esta nueva condición.
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 9");


        }
        public void RunHex()
        {
            if (ThermoMethod == null)
            {
                Console.WriteLine("❌ ERROR: Debes llamar a SetThermoMethod() antes de Run().");
                return;
            }
            Console.WriteLine("Simulacion de intercambiador con varios estados de entrada y salida");

            var hotsideinlet = new FacadeStream("Hot side inlet");
            var hotsideoutlet = new FacadeStream("Hot side outlet");

            var Hex1 = new SolverHeatExchanger("Hex-100");
            MainSolver.AddEquipment(Hex1);
            MainSolver.AddStream(hotsideoutlet);
            MainSolver.AddStream(hotsideinlet);
            Hex1.SetHotInlet(hotsideinlet);
            Hex1.SetHotOutlet(hotsideoutlet);

            hotsideinlet.VaporFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 1");

            hotsideoutlet.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 2");

            hotsideoutlet.Pressure.SetValue(new Pressure(2, PressureUnits.Bara), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 3");

            Hex1.DeltaPHot.SetValue(new PressureDrop(0.1, PressureDropUnits.psi), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 4");

            var componentes = hotsideinlet.Composition.Components;
            if (componentes.Count >= 2)
            {
                componentes[0].MolarFraction.SetValue(new Percentage(90, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                componentes[1].MolarFraction.SetValue(new Percentage(10, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 5");

            hotsideoutlet.MassFlow.SetValue(new MassFlow(10000, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 6");


            Console.WriteLine($"Cold Side Heat transfered {Hex1.TransferHeat.Value.GetValue(EnergyFlowUnits.Kcal_hr)} kcal/hr");


            var coldsideinlet = new FacadeStream("Cold side inlet");
            var coldsideoutlet = new FacadeStream("Cold side outlet");
            Hex1.SetColdInlet(coldsideinlet);
            Hex1.SetColdOutlet(coldsideoutlet);
            MainSolver.AddStream(coldsideinlet);
            MainSolver.AddStream(coldsideoutlet);

            coldsideoutlet.Temperature.SetValue(new Temperature(20, TemperatureUnits.DegreeCelcius), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 7");

            coldsideinlet.Temperature.SetValue(new Temperature(6, TemperatureUnits.DegreeCelcius), VariableDefinedBy.UserInput);     //temperatura de chiller
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 8");

            var componentesCold = coldsideoutlet.Composition.Components;
            if (componentesCold.Count >= 2)
            {
                componentesCold[0].MolarFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
                componentesCold[1].MolarFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput); //agua 100% pura
            }
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 9");

            coldsideinlet.Pressure.SetValue(new Pressure(60, PressureUnits.Psig), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 10");

            Hex1.DeltaPCold.SetValue(new PressureDrop(5, PressureDropUnits.psi), VariableDefinedBy.UserInput);     //caidas de presion en el Hex normales de diseño
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 11");


            var massflowCold = coldsideinlet.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);

            Console.WriteLine($"✅ Flujo másico en el lado frío: {massflowCold} kg/hr");    //flujo calculado ya que en paso 6 se definió el flujo del lado caliente y el calor transferido, por lo que el flujo del lado frío se ajusta automáticamente para cumplir con la transferencia de calor dada la temperatura de entrada y salida del lado frío.

            hotsideoutlet.VaporFraction.Clear(VariableDefinedBy.UserInput);   //limpiar para que se calcule nuevamente
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 12");
            Console.WriteLine(" se tuvo que haver descalculado el intercambiador");

            coldsideoutlet.MassFlow.SetValue(new MassFlow(massflowCold - 20000, MassFlowUnits.Kg_hr), VariableDefinedBy.UserInput);
            //definir un flujo en el lado frío que no corresponde con el flujo del lado caliente ni con la transferencia de calor, por lo que el modelo se descalcula porque no se pueden cumplir todas las condiciones al mismo tiempo.
            //este flujo debe hacer que se calcule el flujo del lado caliente pero debe salir con fraccion de vapor en la salida del lado caliente porque el flujo del lado frío no es suficiente para absorber todo el calor transferido, por lo que el modelo ajusta automáticamente la fracción de vapor en la salida del lado caliente para cumplir con el balance de energía dado el nuevo flujo del lado frío.

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 13");

            var valve = new SolverValve("V-01");
            valve.SetInlet(hotsideoutlet);
            MainSolver.AddEquipment(valve);


            var valveOutlet = new FacadeStream("oulet valve");
            MainSolver.AddStream(valveOutlet);
            valve.SetOutlet(valveOutlet);

            valve.DeltaP.SetValue(new PressureDrop(5, PressureDropUnits.psi), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 14");

            valve.DeltaP.SetValue(new PressureDrop(1, PressureDropUnits.Bar), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 15");

            valve.DeltaP.Clear(VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 16");

            valveOutlet.Pressure.SetValue(new Pressure(10, PressureUnits.Psig), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 17");


            var drum = new SolverDrum("D-01");
            MainSolver.AddEquipment(drum);

            var outletVapor = new FacadeStream("drum vapor outlet");
            MainSolver.AddStream(outletVapor);

            var outletLiquid = new FacadeStream("drum liquid outlet");
            MainSolver.AddStream(outletLiquid);

            drum.SetFeed(valveOutlet);
            drum.SetVaporOutlet(outletVapor);
            drum.SetLiquidOutlet(outletLiquid);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 18");


            var Hex2 = new SolverHeatExchanger("Hex-200");
            MainSolver.AddEquipment(Hex2);

            Hex2.SetHotInlet(outletVapor);
            var Hex2HotOutlet = new FacadeStream("Hex2 hot outlet");
            MainSolver.AddStream(Hex2HotOutlet);
            Hex2.SetHotOutlet(Hex2HotOutlet);

            Hex2HotOutlet.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 19");

            Hex2.DeltaPHot.SetValue(new PressureDrop(0.5, PressureDropUnits.Bar), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 20");


            Hex2.SetColdInlet(coldsideoutlet);
            var Hex2ColdOutlet = new FacadeStream("Hex2 cold outlet");
            MainSolver.AddStream(Hex2ColdOutlet);
            Hex2.SetColdOutlet(Hex2ColdOutlet);

            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 21");


            Hex2.DeltaPCold.SetValue(new PressureDrop(5, PressureDropUnits.psi), VariableDefinedBy.UserInput);
            MainSolver.RunSimulation();
            AllPrinter(MainSolver.Streams, "paso 22");


        }

        void AllPrinter(List<IFacadeStream> streams, string stepLabel)
        {
            Console.WriteLine($"\n📋 {stepLabel}");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            // ── 1. PRESIONES ───────────────────────────────────────────
            Console.WriteLine("PRESIONES (bara)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.Pressure.ToUiString("F2")} - {s.Pressure.DataProcedence}");
            Console.WriteLine();

            // ── 2. TEMPERATURAS ────────────────────────────────────────
            Console.WriteLine("TEMPERATURAS (°C)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.Temperature.ToUiString("F1")} - {s.Temperature.DataProcedence}");
            Console.WriteLine();

            // ── 3. ENTALPÍAS ──────────────────────────────────────────
            Console.WriteLine("ENTALPÍAS MÁSICAS (kcal/kg)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.MassEnthalpy.ToUiString("F2")}  - {s.MassEnthalpy.DataProcedence}");
            Console.WriteLine();

            Console.WriteLine("DENSIDADES MÁSICAS (Kg/m3)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.MassDensity.ToUiString("F2")}  - {s.MassDensity.DataProcedence}");
            Console.WriteLine();

            // ── 4. FLUJO MÁSICO TOTAL ─────────────────────────────────
            Console.WriteLine("FLUJO MÁSICO TOTAL (kg/h)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.MassFlow.ToUiString("F1")}  - {s.MassFlow.DataProcedence}");
            Console.WriteLine();

            // ── 5. FLUJO ENTALPÍA TOTAL ───────────────────────────────
            Console.WriteLine("FLUJO ENTALPÍA TOTAL (kcal/h)"); // Asumiendo kcal/h
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.EnthalpyFlow.ToUiString("F1")}  - {s.EnthalpyFlow.DataProcedence}");
            Console.WriteLine();

            // ── 6. FRACCIÓN DE VAPOR (%) ──────────────────────────────
            Console.WriteLine("FRACCIÓN DE VAPOR (%)");
            Console.WriteLine("───────────────────────────────────────────────────────────");
            foreach (var s in streams)
                Console.WriteLine($"  {s.Name ?? "Stream",-14}: {s.VaporFraction.ToUiString("F1")}  - {s.VaporFraction.DataProcedence}");
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
                    Console.WriteLine($"    {s.Name ?? "Stream",-12}: {comp?.MassFraction.ToUiString("F1") ?? "---"}%  - {comp?.MassFraction.DataProcedence}");
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
                    Console.WriteLine($"    {s.Name ?? "Stream",-12}: {comp?.MassFlow.ToUiString("F1") ?? "---"}  - {comp?.MassFlow.DataProcedence}");
                }
            }
            Console.WriteLine("═══════════════════════════════════════════════════════════");
        }

    }
}