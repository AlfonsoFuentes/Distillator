using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments.Columns.Orchestrador
{


    public interface IColumnPlate
    {
        int nPlate { get; }
        string Name { get; }
        //Plato generico un plato es igual en balance de energia a una columna
        IFacadeStream VaporOutlet { get; }
        IFacadeStream LiquidOutlet { get; }
        IFacadeStream VaporInlet { get; }
        IFacadeStream LiquidInlet { get; }

        List<IFacadeStream> SideFeeds { get; }
        List<IFacadeStream> SideDraws { get; }
        IColumnPlatesSolverEquation Equation { get; }

    }

    public class IdealColumnPlateTopBottomStrategy : IColumnPlate
    {

        public string Name => $"{_columnName} - {nPlate}";
        public int nPlate { get; }
        public IColumnPlatesSolverEquation Equation { get; private set; } = null!;
        public IFacadeStream VaporOutlet { get; private set; } = null!;

        public IFacadeStream LiquidOutlet { get; private set; } = null!;

        public IFacadeStream VaporInlet { get; private set; } = null!;

        public IFacadeStream LiquidInlet { get; private set; } = null!;

        string _columnName = string.Empty;

        public IdealColumnPlateTopBottomStrategy(SolverColumn _column, int _nplate, IFacadeStream _VaporOutlet, IFacadeStream _LiquidInlet)
        {
            _columnName = _column.Name;
            nPlate = _nplate;

            VaporOutlet = _VaporOutlet;
            LiquidInlet = _LiquidInlet;
            var thermoMethod = _VaporOutlet.ThermoMethod;
            var pressurePlate = _VaporOutlet.Pressure.Value.GetValue(PressureUnits.Pascala);
            LiquidOutlet = new FacadeStream($"LO-{Name}");
            VaporInlet = new FacadeStream($"VI-{Name}");
            LiquidOutlet.SetThermodynamicMethod(thermoMethod);
            VaporInlet.SetThermodynamicMethod(thermoMethod);

            LiquidOutlet.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.Solver);
            VaporInlet.VaporFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.Solver);

            var phaseLiquidVapOulet = VaporOutlet.LiquidPhase.Components;
            for (int i = 0; i < phaseLiquidVapOulet.Count; i++)
            {
                var mLiquidVaporfase = phaseLiquidVapOulet[i].MassFraction * 100.0;
                LiquidOutlet.Composition.Components[i].MassFraction.SetValue(new Percentage(mLiquidVaporfase, PercentageUnits.Percentage), VariableDefinedBy.Solver);
            }
            LiquidOutlet.Pressure.SetValue(new Pressure(pressurePlate, PressureUnits.Pascala), VariableDefinedBy.Solver);
            VaporInlet.Pressure.SetValue(new Pressure(pressurePlate, PressureUnits.Pascala), VariableDefinedBy.Solver);



            double seedLiquidFlow = LiquidInlet.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);
            LiquidOutlet.MassFlow.SetValue(new MassFlow(seedLiquidFlow, MassFlowUnits.Kg_hr), VariableDefinedBy.Solver);


            // Semilla para la composición de VaporInlet
            for (int i = 0; i < phaseLiquidVapOulet.Count; i++)
            {
                var mFraction = VaporOutlet.Composition.Components[i].MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);
                VaporInlet.Composition.Components[i].MassFlow.SetValue(new MassFlow(mFraction, MassFlowUnits.Kg_hr), VariableDefinedBy.Solver);
            }


            Equation = new ColumnPlateTopBottomEquation(this);

        }



        //En memoria estas corrientes son iguales a las de las columnas y no se crearan nuevos en memoria
        public List<IFacadeStream> SideFeeds { get; } = new();

        public List<IFacadeStream> SideDraws { get; } = new();
    }



    public class ColumnPlateMassEnergyEquation : ISolverEquation
    {
        IColumnPlate _plate;
        public ColumnPlateMassEnergyEquation(IColumnPlate plate)
        {
            _plate = plate;
        }
        public string Name => $"{EquationType} - - {_plate.Name}";

        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;


        public List<double> Residuals => GetResiduals();

        List<double> GetResiduals()
        {
            var residuals = new List<double>();

            // Validación de seguridad inicial
            if (_plate.VaporOutlet == null) return residuals;

            double totalEnergyIn = 0;
            double totalEnergyOut = 0;
            double totalmassIn = 0;
            double totalmassOut = 0;

            int numComponents = _plate.VaporOutlet.Composition.Components.Count;

            // AQUI ESTÁ LA CORRECCIÓN MATEMÁTICA: Usar arreglos en vez de escalares
            double[] massCompIn = new double[numComponents];
            double[] massCompoOut = new double[numComponents];

            if (_plate.VaporOutlet != null)
            {
                double mVaporOutlet = _plate.VaporOutlet.MassFlow.GetSolverValue();
                double HVaporOutlet = _plate.VaporOutlet.MassEnthalpy.GetSolverValue();
                totalmassOut += mVaporOutlet;
                totalEnergyOut += mVaporOutlet * HVaporOutlet;

                for (int i = 0; i < numComponents; i++)
                {
                    var compo = _plate.VaporOutlet.Composition.Components[i];
                    massCompoOut[i] += compo.MassFraction.GetSolverValue() * mVaporOutlet;
                }
            }

            if (_plate.LiquidOutlet != null)
            {
                double mBottomOutlet = _plate.LiquidOutlet.MassFlow.GetSolverValue();
                double HBottomOutlet = _plate.LiquidOutlet.MassEnthalpy.GetSolverValue();
                totalmassOut += mBottomOutlet;
                totalEnergyOut += mBottomOutlet * HBottomOutlet;
                for (int i = 0; i < numComponents; i++)
                {
                    var compo = _plate.LiquidOutlet.Composition.Components[i];
                    massCompoOut[i] += compo.MassFraction.GetSolverValue() * mBottomOutlet;
                }
            }

            if (_plate.VaporInlet != null)
            {
                double mVaporInlet = _plate.VaporInlet.MassFlow.GetSolverValue();
                double HVaporInlet = _plate.VaporInlet.MassEnthalpy.GetSolverValue();
                totalmassIn += mVaporInlet;
                totalEnergyIn += mVaporInlet * HVaporInlet;

                for (int i = 0; i < numComponents; i++)
                {
                    var compo = _plate.VaporInlet.Composition.Components[i];
                    massCompIn[i] += compo.MassFraction.GetSolverValue() * mVaporInlet;
                }
            }

            if (_plate.LiquidInlet != null)
            {
                double mRefluxInlet = _plate.LiquidInlet.MassFlow.GetSolverValue();
                double HRefluxInlet = _plate.LiquidInlet.MassEnthalpy.GetSolverValue();
                totalmassIn += mRefluxInlet;
                totalEnergyIn += mRefluxInlet * HRefluxInlet;

                for (int i = 0; i < numComponents; i++)
                {
                    var compo = _plate.LiquidInlet.Composition.Components[i];
                    massCompIn[i] += compo.MassFraction.GetSolverValue() * mRefluxInlet;
                }
            }

            foreach (var sidedraw in _plate.SideDraws)
            {
                double msidedraw = sidedraw.MassFlow.GetSolverValue();
                double Hsidedraw = sidedraw.MassEnthalpy.GetSolverValue();
                totalmassOut += msidedraw;
                totalEnergyOut += msidedraw * Hsidedraw;
                for (int i = 0; i < sidedraw.Composition.Components.Count; i++)
                {
                    var compo = sidedraw.Composition.Components[i];
                    massCompoOut[i] += compo.MassFraction.GetSolverValue() * msidedraw;
                }
            }

            foreach (var feed in _plate.SideFeeds)
            {
                double mfeed = feed.MassFlow.GetSolverValue();
                double Hfeed = feed.MassEnthalpy.GetSolverValue();
                totalmassIn += mfeed;
                totalEnergyIn += mfeed * Hfeed;
                for (int i = 0; i < feed.Composition.Components.Count; i++)
                {
                    var compo = feed.Composition.Components[i];
                    massCompIn[i] += compo.MassFraction.GetSolverValue() * mfeed;
                }
            }


            for (int i = 0; i < numComponents; i++)
            {
                residuals.Add(massCompIn[i] - massCompoOut[i]);

            }


            residuals.Add(totalEnergyIn - totalEnergyOut);

            return residuals;
        }

        public List<IVariable> Variables => GetVariables();
        List<IVariable> GetVariables()
        {
            var variables = new List<IVariable>();


            if (_plate.LiquidOutlet != null)
            {

                variables.Add(_plate.LiquidOutlet.MassFlow);



            }
            if (_plate.VaporInlet != null)
            {

                for (int i = 0; i < _plate.VaporInlet.Composition.Components.Count; i++)
                {
                    var compo = _plate.VaporInlet.Composition.Components[i];
                    variables.Add(compo.MassFlow);
                }

            }




            return variables;
        }

        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Regular;
    }

    public interface IColumnPlatesSolverEquation
    {
        List<ISolverEquation> Equations { get; }

    }

    public class ColumnPlateTopBottomEquation : IColumnPlatesSolverEquation
    {

        private readonly IColumnPlate plate;
        public ColumnPlateTopBottomEquation(IColumnPlate _plate)
        {

            plate = _plate;
        }


        public List<ISolverEquation> Equations => GetEquations().ToList();

        private IEnumerable<ISolverEquation> GetEquations()
        {

            yield return new ColumnPlateMassEnergyEquation(plate);

        }

    }
    public interface IColumnPlateSolver
    {
        //Este usa la ecuacion y le aplica el NewtonSolver ya creado el cual solo necesita que el pasen la ecuacion
        void Solve();
    }

    public class ColumnIdealPlateSolver : IColumnPlateSolver
    {
        private List<IFacadeStream> _pendingFeeds = new();
        private List<IFacadeStream> _pendingDraws = new();

        private readonly SolverColumn column;

        public ColumnIdealPlateSolver(SolverColumn _column)
        {
            column = _column;
        }
        ColumnPlateNewtonSolver Solver = new ColumnPlateNewtonSolver();
        // En ColumnIdealPlateSolver.cs, modificar el método Solve()
        private StoppingCriteriaEvaluator _stoppingEvaluator = null!;
        public void Solve()
        {
            // 1. Limpieza inicial

            _pendingFeeds = column.Feeds.ToList();
            _pendingDraws = column.SideDraws.ToList();

            // 2. Corrientes iniciales
            IFacadeStream currentVaporOutlet = column.VaporOutlet!;
            IFacadeStream currentLiquidInlet = column.RefluxInlet!;

            int nPlate = 1;
            bool targetReached = false;

            // 3. Calcular límite de seguridad
            int maxSafetyPlates = 200;
            double nTheoretical = 0;



            // 4. 🔥 NUEVO: Inicializar evaluador de criterios
            _stoppingEvaluator = new StoppingCriteriaEvaluator(new IColumnStoppingCriterion[]
            {
            new TargetConcentrationCriterion(),
            new CompositionInversionCriterion(),
            new InvalidCompositionCriterion(),
            new ThermodynamicErrorCriterion(),
            new PinchPointCriterion(),
            new RelativeVolatilityCriterion(1.05),
            new OscillationCriterion(),
            new MaxUsefulPlatesCriterion(nTheoretical, 3.0)
            });

            // 5. Identificar componente clave
            ComponentFacade? heavyKeyComp = null;
            double targetConcentration = 0;

            if (column.BottomOutlet != null && column.BottomOutlet.State == StreamStateType.Calculated)
            {
                heavyKeyComp = column.BottomOutlet.Composition.Components
                                .OrderByDescending(c => c.MassFraction.GetSolverValue())
                                .First();
                targetConcentration = heavyKeyComp.MassFraction.GetSolverValue();
            }

            // 6. Historial de composiciones para detectar oscilación
            List<double> compositionHistory = new();

            IColumnPlate previousPlate = null!;
            // 7. Bucle principal con criterios de parada inteligentes
            while (!targetReached && nPlate <= maxSafetyPlates)
            {
                // A. Instanciar plato
                IColumnPlate currentPlate = new IdealColumnPlateTopBottomStrategy(
                    column, nPlate, currentVaporOutlet, currentLiquidInlet);

                // B. Resolver
                try
                {
                    SolvePlate(currentPlate);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" ERROR EN PLATO {nPlate}: {ex.Message}");
                    break;
                }

                // C. Almacenar


                // D. Validar estados
                if (currentPlate.LiquidOutlet.State != StreamStateType.Calculated ||
                    currentPlate.VaporInlet.State != StreamStateType.Calculated)
                {
                    Console.WriteLine($"❌ ERROR CRÍTICO: Termodinámica colapsó en Plato {nPlate}");
                    break;
                }
                if (column.Orchestrator != null)
                {
                    bool isFeedStage = currentPlate.SideFeeds.Any();
                    bool isCondenser = nPlate == 1;
                    bool isReboiler = false;  // Se actualizará al final

                    column.Orchestrator.OnPlateSolved(currentPlate, nPlate, isFeedStage, isCondenser, isReboiler);
                }
                
                if (CheckConvergence(currentPlate, previousPlate, nPlate, targetConcentration, heavyKeyComp, compositionHistory))
                {
                    targetReached = true;
                    break;
                }
                // G. Empalme para siguiente iteración
                if (!targetReached)
                {
                    nPlate++;
                    currentLiquidInlet = currentPlate.LiquidOutlet;
                    currentVaporOutlet = currentPlate.VaporInlet;
                    previousPlate = currentPlate;
                }
            }

            // 8. Mensaje final
            // 🔥 NUEVO: Marcar el último plato como reboiler
            if (column.Orchestrator != null && previousPlate != null)
            {
                column.Orchestrator.OnPlateSolved(previousPlate, nPlate, previousPlate.SideFeeds.Any(), nPlate == 1, true);
            }

            // 8. Mensaje final
            if (!targetReached && nPlate > maxSafetyPlates)
            {
                Console.WriteLine($"\n⚠️ ADVERTENCIA: Límite de {maxSafetyPlates} platos alcanzado");
            }
        }

        SolverEquationType[] EquationTypes => new[] {
        // SolverEquationType.Pressure,
        //SolverEquationType.Concentration,

        SolverEquationType.MassEnergyBalance ,
        //SolverEquationType.Specification
    };
        private bool CheckConvergence(IColumnPlate currentPlate, IColumnPlate previousPlate, int nPlate, double targetConcentration, ComponentFacade? heavyKeyComp, List<double> compositionHistory)
        {
            bool targetReached = false;

            double currentComposition = 0;
            double previousComposition = previousPlate != null ?
                previousPlate.LiquidOutlet.Composition.Components
                    .FirstOrDefault(c => c.Id == heavyKeyComp?.Id)?.MassFraction.GetSolverValue() ?? 0 : 0;

            if (heavyKeyComp != null)
            {
                currentComposition = currentPlate.LiquidOutlet.Composition.Components
                    .FirstOrDefault(c => c.Id == heavyKeyComp.Id)?.MassFraction.GetSolverValue() ?? 0;
            }

            compositionHistory.Add(currentComposition);

            var context = new PlateContext
            {
                PlateNumber = nPlate,
                CurrentComposition = currentComposition,
                PreviousComposition = previousComposition,
                TargetComposition = targetConcentration,
                RelativeVolatility = 1.0, // TODO: Calcular α_eff
                IsValidComposition = currentComposition >= 0 && currentComposition <= 1,
                IsThermodynamicallyValid = true,
                CompositionHistory = compositionHistory
            };

            var stoppingResult = _stoppingEvaluator.EvaluateAll(context);

            // F. Procesar resultado
            if (stoppingResult.Level == StopLevel.Success)
            {
                Console.WriteLine($"\n✅ {stoppingResult.Reason}");
                Console.WriteLine($"   Total de platos teóricos: {nPlate}");
                targetReached = true;
            }
            else if (stoppingResult.Level == StopLevel.HardStop)
            {
                Console.WriteLine($"\n❌ PARADA DURA: {stoppingResult.Reason}");
                Console.WriteLine($"   Platos calculados: {nPlate}");
                return true;  // 🔥 Retornar inmediatamente para hacer break
            }
            else if (stoppingResult.Level == StopLevel.Warning)
            {
                Console.WriteLine($"\n⚠️ ADVERTENCIA: {stoppingResult.Reason}");
            }

            return targetReached;
        }
        void SolvePlate(IColumnPlate _Plate)
        {
            // FASE 1: Resolver el esqueleto base del plato (corrientes principales)
            ResolveEquationsSequentially(_Plate);

            // FASE 2: Evaluar el estado recién calculado y decidir si entran/salen laterales
            bool topologyChanged = EvaluateAndInjectSideStreams(_Plate);

            // FASE 3: Si se inyectó masa o energía lateral, el plato se desestabiliza.
            // Volvemos a llamar al micro-solver para que absorba el impacto suavemente.
            if (topologyChanged)
            {
                ResolveEquationsSequentially(_Plate);
            }
        }
        private void ResolveEquationsSequentially(IColumnPlate plate)
        {


            // Extraemos la lista de ecuaciones armadas por la estrategia del plato
            var plateEquations = plate.Equation.Equations;

            foreach (var type in EquationTypes)
            {
                // Buscamos si el plato tiene una ecuación de este tipo
                var eq = plateEquations.FirstOrDefault(e => e.EquationType == type);

                if (eq != null)
                {
                    // Lanzamos el Newton-Raphson contra esta única ecuación
                    var result = Solver.Solve(eq);

                    if (!result.Converged)
                    {
                        // Fail-Fast: Si una ecuación explota, detenemos el cálculo del plato
                        throw new Exception($"Falla de convergencia: La ecuación {type} en {plate.Name} no convergió.");
                    }


                }
            }
        }
       
        private bool EvaluateAndInjectSideStreams2(IColumnPlate plate)
        {
            bool feedsInjected = false;
            bool drawsInjected = false;

            var liquidinlet = plate.LiquidInlet.Composition.Components;
            var liquidoutlet = plate.LiquidOutlet.Composition.Components;
            var vaporinlet = plate.VaporInlet.Composition.Components;
            var vaporoutlet = plate.VaporOutlet.Composition.Components;
            List<IFacadeStream> pendingToRemove = new();
            if (_pendingDraws.Count == 0 && _pendingFeeds.Count == 0) return false;
            for (int i = 0; i < _pendingFeeds.Count; i++)
            {
                var feed = _pendingFeeds[i];

                for (int j = 0; j < feed.Composition.Components.Count - 1; j++)
                {
                    var comp = feed.Composition.Components[j];

                    bool isLiquid = feed.ThermodynamicState == ThermodynamicState.SaturatedLiquid ||
                                feed.ThermodynamicState == ThermodynamicState.SubcooledLiquid;
                    if (isLiquid)
                    {
                        var compoinlet = liquidinlet.FirstOrDefault(x => x.Id == comp.Id);
                        var compooutlet = liquidoutlet.FirstOrDefault(x => x.Id == comp.Id);
                        if (compoinlet != null && compooutlet != null)
                        {
                            double fraccioninlet = compoinlet.MolarFraction.GetSolverValue();
                            double fraccion = comp.MolarFraction.GetSolverValue();
                            double fraccionoutlet = compooutlet.MolarFraction.GetSolverValue();
                            if (fraccioninlet > fraccion && fraccion > fraccionoutlet)
                            {
                                plate.SideFeeds.Add(feed); // Conectamos la corriente al plato
                                feedsInjected = true;
                                pendingToRemove.Add(feed);
                                break;
                            }


                        }
                    }
                    else
                    {
                        var compoinlet = vaporinlet.FirstOrDefault(x => x.Id == comp.Id);
                        var compooutlet = vaporoutlet.FirstOrDefault(x => x.Id == comp.Id);
                        if (compoinlet != null && compooutlet != null)
                        {
                            double fraccioninlet = compoinlet.MolarFraction.GetSolverValue();
                            double fraccion = comp.MolarFraction.GetSolverValue();
                            double fraccionoutlet = compooutlet.MolarFraction.GetSolverValue();
                            if (fraccion > fraccioninlet && fraccion < fraccionoutlet)
                            {

                                plate.SideFeeds.Add(feed); // Conectamos la corriente al plato
                                feedsInjected = true;
                                pendingToRemove.Add(feed);
                                break;
                            }


                        }
                    }
                }
            }
            foreach (var remove in pendingToRemove)
            {
                _pendingFeeds.Remove(remove);
            }




            return feedsInjected || drawsInjected;
        }

        private bool EvaluateAndInjectSideStreams(IColumnPlate plate)
        {
            if (_pendingFeeds.Count == 0 && _pendingDraws.Count == 0)
                return false;

            bool anyInjected = false;

            // Pre-construir diccionarios de lookup UNA SOLA VEZ por plato (O(N))
            var liquidInletDict = plate.LiquidInlet.Composition.Components
                .ToDictionary(c => c.Id, c => c.MolarFraction.GetSolverValue());
            var liquidOutletDict = plate.LiquidOutlet.Composition.Components
                .ToDictionary(c => c.Id, c => c.MolarFraction.GetSolverValue());
            var vaporInletDict = plate.VaporInlet.Composition.Components
                .ToDictionary(c => c.Id, c => c.MolarFraction.GetSolverValue());
            var vaporOutletDict = plate.VaporOutlet.Composition.Components
                .ToDictionary(c => c.Id, c => c.MolarFraction.GetSolverValue());

            // Evaluar Feeds
            anyInjected |= ProcessPendingStreams(_pendingFeeds, plate.SideFeeds,
                                                 liquidInletDict, liquidOutletDict,
                                                 vaporInletDict, vaporOutletDict,
                                                 plate.nPlate, "Entrada");

            // Evaluar Draws
            anyInjected |= ProcessPendingStreams(_pendingDraws, plate.SideDraws,
                                                 liquidInletDict, liquidOutletDict,
                                                 vaporInletDict, vaporOutletDict,
                                                 plate.nPlate, "Salida");

            return anyInjected;
        }

        // Motor reutilizable para Feeds y Draws
        private bool ProcessPendingStreams(
            List<IFacadeStream> pending,
            List<IFacadeStream> targetList,
            Dictionary<Guid, double> liquidInletDict,
            Dictionary<Guid, double> liquidOutletDict,
            Dictionary<Guid, double> vaporInletDict,
            Dictionary<Guid, double> vaporOutletDict,
            int nPlate,
            string logType)
        {
            bool injected = false;

            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var stream = pending[i];
                bool isLiquid = stream.ThermodynamicState == ThermodynamicState.SaturatedLiquid ||
                                stream.ThermodynamicState == ThermodynamicState.SubcooledLiquid;

                bool matchFound = false;

                for (int j = 0; j < stream.Composition.Components.Count - 1; j++)
                {
                    var comp = stream.Composition.Components[j];
                    double streamFrac = comp.MolarFraction.GetSolverValue();

                    if (isLiquid)
                    {
                        if (liquidInletDict.TryGetValue(comp.Id, out double inletFrac) &&
                            liquidOutletDict.TryGetValue(comp.Id, out double outletFrac))
                        {
                            if (streamFrac <= inletFrac && streamFrac >= outletFrac)
                            {
                                matchFound = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (vaporInletDict.TryGetValue(comp.Id, out double inletFrac) &&
                            vaporOutletDict.TryGetValue(comp.Id, out double outletFrac))
                        {
                            if (streamFrac >= inletFrac && streamFrac <= outletFrac)
                            {
                                matchFound = true;
                                break;
                            }
                        }
                    }
                }

                if (matchFound)
                {
                    targetList.Add(stream);
                    pending.RemoveAt(i);
                    injected = true;
                    Console.WriteLine($"[Topología] {logType} '{stream.Name}' asignada al Plato {nPlate}");
                }
            }

            return injected;
        }
    }

}
