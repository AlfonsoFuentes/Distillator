using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;
using UnitSystem;

namespace Shared.SolverConsecutive
{
    public interface IMainSolver
    {
        List<IFacadeStream> Streams { get; }
        List<ISolverEquipment> Equipments { get; }
        void AddStream(IFacadeStream stream);
        void RemoveStream(IFacadeStream stream);
        void RemoveEquipment(ISolverEquipment equipment);
        void AddEquipment(ISolverEquipment equipment);

        Length Altitude { get; set; }
        Pressure AtmosphericPressure { get; set; }
        ThermodynamicMethodFullDto ThermoMethod { get; set; }

        void RunSimulation();

        event Action? OnSimulationCompleted;
        void ClearOrphanStream(IFacadeStream stream);

    }
//    public class MainSolverLegacy : IMainSolver
//    {
//        public event Action? OnSimulationCompleted;
//        INewtonSolver Solver { get; } = null!;
//        SolverEquationType[] EquationTypes => new[] {
//         SolverEquationType.Pressure,
//        SolverEquationType.Concentration,
//        SolverEquationType.VaporFraction,
//        SolverEquationType.Enthalpy,
//        SolverEquationType.MassBalance,
//        SolverEquationType.MassEnergyBalance ,
//        SolverEquationType.Specification
//    };
//        public MainSolverLegacy()
//        {
//            Solver = new NewtonSolver();
//            AtmosphericPressure = new Pressure(101325, PressureUnits.Pascala);
//            Altitude = new Length(0, LengthUnits.Meter);
//        }
//        public Pressure AtmosphericPressure { get; set; }

//        Length _Altitude = null!;
//        public Length Altitude
//        {
//            get { return _Altitude; }
//            set
//            {
//                _Altitude = value;
//                CalculateAtmosPhericPressure();
//            }
//        }

//        void CalculateAtmosPhericPressure()
//        {
//            if (_Altitude == null) return;

//            var altitudeMeters = Altitude.GetValue(LengthUnits.Meter);

//            const double P0 = 101325.0;  // Pa
//            const double factor = 2.25577e-5;
//            const double exponent = 5.25588;

//            var pressure = P0 * Math.Pow(1 - factor * altitudeMeters, exponent);
//            AtmosphericPressure.SetValue(pressure, PressureUnits.Pascala);
//            UnitManager.SetAtmosphericPressureReference(AtmosphericPressure);
//        }
//        public List<IFacadeStream> Streams { get; } = new();
//        public List<ISolverEquipment> Equipments { get; } = new();
//        public ThermodynamicMethodFullDto ThermoMethod { get; set; } = null!;
//        public void AddStream(IFacadeStream stream)
//        {
//            Streams.Add(stream);
//            // El método termodinámico se asigna desde el Project, no desde el solver.
//        }
//        public void RemoveStream(IFacadeStream stream) => Streams.Remove(stream);
//        public void AddEquipment(ISolverEquipment equipment) => Equipments.Add(equipment);
//        public void RemoveEquipment(ISolverEquipment equipment) => Equipments.Remove(equipment);

//        public void RunSimulation()
//        {
//            // 🔥 Todo el flujo en hilo de fondo para no bloquear UI
//            _ = Task.Run(async () =>
//            {
//                try
//                {
//                    ClearCalculatedBySolver();
//                    SolveEquations();

//                    // 🔥 PostSolve se ejecuta DESPUÉS de que SolveEquations termine
//                    await ExecutePostSolveCalculationsAsync();
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"❌ Error en RunSimulation: {ex.Message}");
//                }
//            });
//        }
//        private async Task ExecutePostSolveCalculationsAsync()
//        {
//            try
//            {
//                // 1. Recopilamos todos los IFacade del Flowsheet
//                var allFacades = new List<IFacade>();
//                allFacades.AddRange(Equipments);
//                allFacades.AddRange(Streams);

//                // 2. Ejecutamos todos los Post-Cálculos (Envolventes, Cv, Potencia, FUG, etc.)
//                // Podemos hacerlo en paralelo para que el procesador use todos sus núcleos
//                var postSolveTasks = allFacades.Select(facade => facade.PostSolveAsync());
//                await Task.WhenAll(postSolveTasks);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error en Post-Cálculos: {ex.Message}");
//            }
//            finally
//            {
//                // 3. AHORA SÍ notificamos a la UI que TODA la matemática y los reportes terminaron.
//                // Aquí la UI apagará su Spinner de "Solving..." y refrescará pantallas.
//                OnSimulationCompleted?.Invoke();
//            }
//        }
//        void SolveEquations()
//        {
//            Dictionary<SolverEquationType, List<ISolverEquation>> equationsByType = CreateEquationsByTypeV2();
//            // Backup: Dictionary<SolverEquationType, List<ISolverEquation>> equationsByType = CreateEquationsByType_Old();

//            var globalClusters = BuildSpecificationClustersV3();
//            // Backup V2: var globalClusters = BuildSpecificationClustersV2();
//            // Backup: var globalClusters = MapSpecificationDependencyClusters_Old();

//            if (globalClusters.Any())
//            {
//                if (!equationsByType.ContainsKey(SolverEquationType.Specification))
//                {
//                    equationsByType[SolverEquationType.Specification] = new List<ISolverEquation>();
//                }
//                // 🚀 Inyectamos estos clústeres VIP a la lista de Especificaciones
//                // Como CompositeEquationEquipmentList implementa ISolverEquation, el diccionario lo acepta perfectamente.
//                equationsByType[SolverEquationType.Specification].AddRange(globalClusters);
//            }
//            var pendingTypes = equationsByType
//                .Where(kvp => kvp.Value != null && kvp.Value.Count > 0)
//                .Select(kvp => kvp.Key)
//                .ToList();

//            bool globalMovement = true;
//            int iter = 0;
//            int maxIterations = 10;

//            while (globalMovement && iter < maxIterations && pendingTypes.Count > 0)
//            {
//                globalMovement = false;

//                foreach (var type in pendingTypes.ToList())
//                {
//                    var rawEquations = equationsByType[type]; // La lista original suelta
//                    bool localMovement = true;

//                    while (localMovement)
//                    {
//                        localMovement = false;

//                        if(type== SolverEquationType.Specification)
//                        {

//                        }
//                        // MAGIA: Justo antes de iterar, agrupamos las ecuaciones que quedan
//                        var clusteredEquations = ClusterEquations(rawEquations);

//                        int i = 0;

//                        while (i < clusteredEquations.Count)
//                        {
//                            var equation = clusteredEquations[i];
//                            var resultSolver = Solver.Solve(equation);

//                            if (resultSolver.Converged)
//                            {
//                                localMovement = true;
//                                globalMovement = true;

//                                // Extraer y borrar de la lista original
//                                if (equation is CompositeEquation composite)
//                                {
//                                    foreach (var innerEq in composite.Equations)
//                                    {
//                                        rawEquations.Remove(innerEq);
//                                    }
//                                }
//                                else
//                                {
//                                    rawEquations.Remove(equation);
//                                }

//                                clusteredEquations.RemoveAt(i);
//                            }
//                            else
//                            {
//                                i++;
//                            }
//                        }
//                    }

//                    if (rawEquations.Count == 0)
//                    {
//                        pendingTypes.Remove(type);
//                        Console.WriteLine($"✅ Tipo '{type}' completamente resuelto. Pendientes: {pendingTypes.Count}");
//                    }
//                }

//                if (!globalMovement) break;
//                iter++;
//            }

//            if (pendingTypes.Count == 0)
//            {
//                Console.WriteLine($"🎉 Todas las ecuaciones resueltas en {iter} iteraciones globales");
//            }
//            else
//            {
//                Console.WriteLine($"⚠️ Convergencia incompleta. Tipos sin resolver: {string.Join(", ", pendingTypes)}");
//            }
//        }
//        Dictionary<SolverEquationType, List<ISolverEquation>> CreateEquationsByTypeV2()
//        {
//            var allTasksByType = new Dictionary<SolverEquationType, List<ISolverEquation>>();
//            foreach (var type in EquationTypes)
//            {
//                foreach (var equipment in Equipments)
//                {
//                    if (equipment.Equations.FirstOrDefault() == null) break;

//                    // Solo las ecuaciones físicas regulares entran al flujo secuencial normal.
//                    // Las ecuaciones Spec se agregan únicamente dentro de los clusters acoplados.
//                    var equationsOfType = equipment.Equations
//                        .Where(x => x.EquationType == type && x.EquationTypeModifer == SolverEquationTypeModifier.Regular)
//                        .ToList();

//                    if (equationsOfType.Any())
//                    {
//                        if (!allTasksByType.ContainsKey(type))
//                            allTasksByType[type] = new List<ISolverEquation>();
//                        allTasksByType[type].AddRange(equationsOfType);
//                    }
//                }
//            }
//            return allTasksByType;
//        }

//        Dictionary<SolverEquationType, List<ISolverEquation>> CreateEquationsByType_Old()
//        {
//            var allTasksByType = new Dictionary<SolverEquationType, List<ISolverEquation>>();
//            foreach (var type in EquationTypes)
//            {
//                foreach (var equipment in Equipments)
//                {
//                    if (equipment.Equations.FirstOrDefault() == null) break;

//                    // 1. Ecuaciones físicas normales del equipo
//                    var equationsOfType = equipment.Equations.Where(x => x.EquationType == type).ToList();

//                    // 🔥 CORRECCIÓN: Las especificaciones ahora entran en su propio tipo
//                    if (type == SolverEquationType.Specification)
//                    {
//                        var specs = equipment.Specifications
//                                             .Select(s => new SpecificationEquation(s))
//                                             .ToList();
//                        equationsOfType.AddRange(specs);
//                    }

//                    if (equationsOfType.Any())
//                    {
//                        if (!allTasksByType.ContainsKey(type))
//                            allTasksByType[type] = new List<ISolverEquation>();
//                        allTasksByType[type].AddRange(equationsOfType);
//                    }

//                }
//            }
//            return allTasksByType;
//        }
//        private List<ISolverEquation> ClusterEquations2(List<ISolverEquation> baseEquations)
//        {
//            var clusters = new List<List<ISolverEquation>>();
//            var unassigned = baseEquations.ToList();

//            while (unassigned.Count > 0)
//            {
//                var currentCluster = new List<ISolverEquation> { unassigned[0] };
//                unassigned.RemoveAt(0);

//                bool added = true;
//                while (added)
//                {
//                    added = false;
//                    // Extraer las incógnitas actuales del clúster
//                    var clusterUnknowns = currentCluster.SelectMany(e => e.AdjustableVariables()).Distinct().ToList();

//                    for (int i = unassigned.Count - 1; i >= 0; i--)
//                    {
//                        var eqUnknowns = unassigned[i].AdjustableVariables();
//                        var candidateEquation = unassigned[i];

//                        // 1. ¿Comparten variables?
//                        bool sharesVariables = eqUnknowns.Intersect(clusterUnknowns).Any();

//                        // 2. ¿Es de un tipo DIFERENTE a todas las que ya están en el clúster?
//                        // Esto evita agrupar 3 ecuaciones de Pressure, pero permite agrupar MassBalance + Specification
//                        bool isDifferentType = currentCluster.All(e => e.EquationType != candidateEquation.EquationType);

//                        if (sharesVariables && isDifferentType)
//                        {
//                            currentCluster.Add(candidateEquation);
//                            unassigned.RemoveAt(i);
//                            added = true;
//                        }
//                    }
//                }
//                clusters.Add(currentCluster);
//            }

//            // Convertimos a CompositeEquation si hay más de 1 en el clúster
//            return clusters.Select(c => c.Count == 1 ? c[0] : new CompositeEquation(c)).ToList();
//        }
//        private List<ISolverEquation> ClusterEquations(List<ISolverEquation> baseEquations)
//        {
//            // 1. SEPARACIÓN: Extraemos los clústeres de especificación (los que ya están listos)
//            // Los dejamos intactos y fuera del alcance del algoritmo de clustering.
//            var preClustered = baseEquations.OfType<CompositeEquationEquipmentList>().ToList();

//            // 2. EXTRAER: Nos quedamos solo con las ecuaciones físicas sueltas
//            var unassigned = baseEquations.Where(e => e is not CompositeEquationEquipmentList).ToList();

//            var clusters = new List<List<ISolverEquation>>();

//            // 3. CLÚSTERIZAR solo las sueltas
//            while (unassigned.Count > 0)
//            {
//                var currentCluster = new List<ISolverEquation> { unassigned[0] };
//                unassigned.RemoveAt(0);

//                bool added = true;
//                while (added)
//                {
//                    added = false;
//                    var clusterUnknowns = currentCluster.SelectMany(e => e.AdjustableVariables()).Distinct().ToList();

//                    for (int i = unassigned.Count - 1; i >= 0; i--)
//                    {
//                        var eqUnknowns = unassigned[i].AdjustableVariables();
//                        var candidateEquation = unassigned[i];

//                        bool sharesVariables = eqUnknowns.Intersect(clusterUnknowns).Any();
//                        bool isDifferentType = currentCluster.All(e => e.EquationType != candidateEquation.EquationType);

//                        if (sharesVariables && isDifferentType)
//                        {
//                            currentCluster.Add(candidateEquation);
//                            unassigned.RemoveAt(i);
//                            added = true;
//                        }
//                    }
//                }
//                clusters.Add(currentCluster);
//            }

//            // 4. ENSAMBLAR: Convertimos los clusters físicos a CompositeEquation
//            var finalResults = clusters.Select(c => c.Count == 1 ? c[0] : new CompositeEquation(c)).ToList();

//            // 5. MERGE: Devolvemos los físicos + los de especificación que estaban protegidos
//            finalResults.AddRange(preClustered);

//            return finalResults;
//        }
//        List<CompositeEquationEquipmentList> BuildSpecificationClustersV2()
//        {
//            var clusters = new List<CompositeEquationEquipmentList>();

//            foreach (var seedEquipment in Equipments.Where(eq => eq.Specifications.Any()))
//            {
//                foreach (var specification in seedEquipment.Specifications)
//                {
//                    if (specification is not StreamSpecificationBase streamSpec) continue;

//                    var cluster = new CompositeEquationEquipmentList();
//                    var clusterStreams = new HashSet<IFacadeStream>(
//                        seedEquipment.Inlets
//                            .Concat(seedEquipment.Outlets)
//                            .Concat(new[] { streamSpec.Source, streamSpec.Destination }));

//                    var clusterEquipments = new HashSet<ISolverEquipment> { seedEquipment };
//                    foreach (var stream in clusterStreams)
//                    {
//                        if (stream.EquipmentInlet != null)
//                        {
//                            clusterEquipments.Add(stream.EquipmentInlet);
//                        }

//                        if (stream.EquipmentOutlet != null)
//                        {
//                            clusterEquipments.Add(stream.EquipmentOutlet);
//                        }
//                    }

//                    var clusterVariables = GetSpecificationClusterVariables(clusterStreams, streamSpec.VariableType);

//                    foreach (var equipment in clusterEquipments)
//                    {
//                        var relatedEquations = equipment.Equations
//                            .Where(eq => eq.EquationTypeModifer == SolverEquationTypeModifier.Regular)
//                            .Where(eq => IsRelevantForSpecificationCluster(eq, streamSpec))
//                            .Where(eq => eq.Variables.Any(variable => clusterVariables.Contains(variable)))
//                            .ToList();

//                        foreach (var equation in relatedEquations)
//                        {
//                            cluster.AddEquation(equation);
//                        }
//                    }

//                    cluster.AddEquation(new SpecificationEquation(specification));
//                    clusters.Add(cluster);
//                }
//            }

//            return clusters;
//        }

//        List<CompositeEquationEquipmentList> BuildSpecificationClustersV3()
//        {
//            var clusters = new List<CompositeEquationEquipmentList>();

//            foreach (var seedEquipment in Equipments.Where(eq => eq.Specifications.Any()))
//            {
//                foreach (var specification in seedEquipment.Specifications)
//                {
//                    if (specification is not StreamSpecificationBase streamSpec) continue;

//                    var clusterStreams = new HashSet<IFacadeStream>(
//                        seedEquipment.Inlets
//                            .Concat(seedEquipment.Outlets)
//                            .Concat(new[] { streamSpec.Source, streamSpec.Destination }));

//                    var clusterEquipments = new HashSet<ISolverEquipment> { seedEquipment };
//                    foreach (var stream in clusterStreams)
//                    {
//                        if (stream.EquipmentInlet != null)
//                        {
//                            clusterEquipments.Add(stream.EquipmentInlet);
//                        }

//                        if (stream.EquipmentOutlet != null)
//                        {
//                            clusterEquipments.Add(stream.EquipmentOutlet);
//                        }
//                    }

//                    var clusterVariables = GetSpecificationClusterVariables(clusterStreams, streamSpec.VariableType);
//                    var specificationVariables = streamSpec.GetVariables().ToHashSet();
//                    clusterVariables.UnionWith(specificationVariables);

//                    var cluster = new CompositeEquationEquipmentList();
//                    foreach (var equipment in clusterEquipments)
//                    {
//                        var relatedEquations = equipment.Equations
//                            .Where(eq => eq.EquationTypeModifer == SolverEquationTypeModifier.Regular)
//                            .Where(eq => eq.Variables.Any(variable => clusterVariables.Contains(variable)))
//                            .ToList();

//                        foreach (var equation in relatedEquations)
//                        {
//                            cluster.AddEquation(equation);
//                        }
//                    }

//                    cluster.AddEquation(new SpecificationEquation(specification));
//                    clusters.Add(cluster);
//                }
//            }

//            return clusters;
//        }

//        private static bool IsRelevantForSpecificationCluster(
//            ISolverEquation equation,
//            StreamSpecificationBase specification)
//        {
//            if (equation.EquationType == specification.TargetEquationType)
//            {
//                return true;
//            }

//            // Las columnas integran masa y energía en una misma ecuación regular.
//            // Para specs de flujo, esa ecuación debe entrar al cluster si comparte variables.
//            return specification.TargetEquationType == SolverEquationType.MassBalance
//                && equation.EquationType == SolverEquationType.MassEnergyBalance;
//        }

//        private static HashSet<IVariable> GetSpecificationClusterVariables(
//            IEnumerable<IFacadeStream> streams,
//            SpecVariableType variableType)
//        {
//            var variables = new HashSet<IVariable>();

//            foreach (var stream in streams)
//            {
//                switch (variableType)
//                {
//                    case SpecVariableType.TotalMassFlow:
//                        variables.Add(stream.MassFlow);
//                        break;
//                    case SpecVariableType.TotalMolarFlow:
//                        variables.Add(stream.MolarFlow);
//                        break;
//                    case SpecVariableType.TotalVolumetricFlow:
//                        variables.Add(stream.VolumetricFlow);
//                        break;
//                }
//            }

//            return variables;
//        }

//        List<CompositeEquationEquipmentList> MapSpecificationDependencyClusters_Old()
//        {
//            var globalClusters = new List<CompositeEquationEquipmentList>();
//            var processedEquipments = new HashSet<ISolverEquipment>();

//            // 1. Filtrar los equipos que tienen especificación (Las Semillas)
//            // Se asume que 'Equipments' es la lista global de equipos en tu Solver
//            var seedEquipments = Equipments.Where(eq => eq.Specifications.Any()).ToList();

//            foreach (var seedEq in seedEquipments)
//            {
//                foreach (var specification in seedEq.Specifications)
//                {
//                    var composite = new CompositeEquationEquipmentList();

//                    // Casteamos para poder acceder a Source, Destination y TargetEquationType
//                    if (specification is not StreamSpecificationBase streamSpec) continue;

//                    // Identificamos explícitamente las corrientes afectadas por esta especificación
//                    List<IFacadeStream> affectedStreams = new();

//                    // 2. Buscar los vecinos (Topología Directa)
//                    var clusterEquipments = new HashSet<ISolverEquipment>();

//                    // La semilla siempre entra al clúster
//                    clusterEquipments.Add(seedEq);

//                    // Agregamos a los vecinos que están en los extremos de las corrientes afectadas
//                    foreach (var inlet in seedEq.Inlets)
//                    {
//                        if (inlet.EquipmentInlet != null)
//                        {
//                            clusterEquipments.Add(inlet.EquipmentInlet);
//                            affectedStreams.Add(inlet);
//                        }

//                    }
//                    foreach (var outlet in seedEq.Outlets)
//                    {
//                        if (outlet.EquipmentOutlet != null)
//                        {
//                            clusterEquipments.Add(outlet.EquipmentOutlet);
//                            affectedStreams.Add(outlet);
//                        }

//                    }

//                    // 3. Filtrar las ecuaciones de esos equipos (Lógica pura, sin LINQ problemático)
//                    foreach (var eqp in clusterEquipments)
//                    {
//                        processedEquipments.Add(eqp);

//                        var equations = eqp.Equations.OfType<ISpecSolverEquation>().ToList();
//                        foreach (var eq in equations)
//                        {
//                            // FILTRO A: Usamos tu propiedad exacta 'EquationTypeModifer'
//                            if (eq.EquationTypeModifer == SolverEquationTypeModifier.Spec)
//                            {
//                                // FILTRO B: Verificamos que sea del tipo físico correcto (ej. MassBalance)
//                                if (eq.EquationType == streamSpec.TargetEquationType)
//                                {
//                                    // FILTRO C: Casteamos a tu nueva interfaz para ver las corrientes
//                                    if (eq is ISpecSolverEquation specEq)
//                                    {
//                                        var streams = specEq.AsociatedStreams.ToList();
//                                        // Comprobamos si la ecuación toca las corrientes de la especificación
//                                        bool touchesAffectedStream = streams.Intersect(affectedStreams).Any();

//                                        if (touchesAffectedStream)
//                                        {
//                                            composite.AddEquation(eq);
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }

//                    // 4. Finalmente, agregamos la ecuación matemática de la especificación en sí misma
//                    composite.AddEquation(new SpecificationEquation(specification));

//                    globalClusters.Add(composite);
//                }
//            }
//            return globalClusters;
//        }
//        public void ClearCalculatedBySolver()
//        {

//            var equations = Equipments
//                .SelectMany(x => x.Equations).ToList();
//            if (equations.Count > 0 && equations[0] == null) return;

//            var variables = Equipments

//                .SelectMany(x => x.Equations)
//                .Where(x => x.Variables != null)
//                .Where(x => x.Variables.Any())
//                .SelectMany(x => x.Variables)
//                .Where(x => x.DataProcedence == VariableDefinedBy.Solver).ToList();

//            foreach (var variable in variables)
//            {
//                variable.Clear(VariableDefinedBy.Solver);

//            }
//        }

//        public void ClearOrphanStream(IFacadeStream stream)
//        {
//            if (stream == null) return;

//            // 1. Recopilamos todas las variables de la corriente explícitamente.
//            // Esto es mucho más rápido que usar Reflection en cada desconexión.
//            var streamVariables = new List<IVariable>
//    {
//        stream.Temperature,
//                stream.Pressure,
//                stream.MassFlow,
//        stream.MolarFlow, stream.VolumetricFlow,
//                stream.VaporFraction,
//        stream.EnthalpyFlow,
//                stream.ThermalConductivity,
//                stream.Viscosity,
//        stream.MassCp,
//                stream.MolarCp,
//                stream.MassEnthalpy,
//                stream.MolarEnthalpy,
//        stream.MassDensity,
//                stream.MolarDensity,
//                stream.MolecularWeight,
//        stream.SuperficialTension
//    };

//            // 2. Si la corriente tiene composición, también metemos esas variables
//            if (stream.Composition != null)
//            {
//                foreach (var comp in stream.Composition.Components)
//                {
//                    streamVariables.Add(comp.MassFlow);
//                    streamVariables.Add(comp.MolarFlow);
//                    streamVariables.Add(comp.MassFraction);
//                    streamVariables.Add(comp.MolarFraction);
//                }
//            }

//            // 3. Ejecutamos tu magia de auto-protección.
//            // Solo se borrarán los datos donde DataProcedence == VariableDefinedBy.Solver
//            foreach (var variable in streamVariables)
//            {
//                // El '?' previene errores si alguna variable no fue inicializada aún
//                variable?.Clear(VariableDefinedBy.Solver);
//            }

//#if DEBUG
//            Console.WriteLine($"[Solver] 🧹 Corriente huérfana '{stream.Name}' limpiada de cálculos del solver.");
//#endif
//        }

//    }
}
