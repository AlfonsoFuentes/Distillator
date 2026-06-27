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


    }
    public class MainSolver : IMainSolver
    {
        public event Action? OnSimulationCompleted;
        INewtonSolver Solver { get; } = null!;
        SolverEquationType[] EquationTypes => new[] {
         SolverEquationType.Pressure,
        SolverEquationType.Concentration,
        SolverEquationType.VaporFraction,
        SolverEquationType.Enthalpy,
        SolverEquationType.MassBalance,
        SolverEquationType.MassEnergyBalance ,
        SolverEquationType.Specification
    };
        public MainSolver()
        {
            Solver = new NewtonSolver();
            AtmosphericPressure = new Pressure(101325, PressureUnits.Pascala);
            Altitude = new Length(0, LengthUnits.Meter);
        }
        public Pressure AtmosphericPressure { get; set; }

        Length _Altitude = null!;
        public Length Altitude
        {
            get { return _Altitude; }
            set
            {
                _Altitude = value;
                CalculateAtmosPhericPressure();
            }
        }

        void CalculateAtmosPhericPressure()
        {
            if (_Altitude == null) return;

            var altitudeMeters = Altitude.GetValue(LengthUnits.Meter);

            const double P0 = 101325.0;  // Pa
            const double factor = 2.25577e-5;
            const double exponent = 5.25588;

            var pressure = P0 * Math.Pow(1 - factor * altitudeMeters, exponent);
            AtmosphericPressure.SetValue(pressure, PressureUnits.Pascala);
            UnitManager.SetAtmosphericPressureReference(AtmosphericPressure);
        }
        public List<IFacadeStream> Streams { get; } = new();
        public List<ISolverEquipment> Equipments { get; } = new();
        public ThermodynamicMethodFullDto ThermoMethod { get; set; } = null!;
        public void AddStream(IFacadeStream stream)
        {
            Streams.Add(stream);
            stream.SetThermodynamicMethod(ThermoMethod);
        }
        public void RemoveStream(IFacadeStream stream) => Streams.Remove(stream);
        public void AddEquipment(ISolverEquipment equipment) => Equipments.Add(equipment);
        public void RemoveEquipment(ISolverEquipment equipment) => Equipments.Remove(equipment);

        public void RunSimulation()
        {
            // 🔥 Todo el flujo en hilo de fondo para no bloquear UI
            _ = Task.Run(async () =>
            {
                try
                {
                    ClearCalculatedBySolver();
                    SolveEquations();

                    // 🔥 PostSolve se ejecuta DESPUÉS de que SolveEquations termine
                    await ExecutePostSolveCalculationsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error en RunSimulation: {ex.Message}");
                }
            });
        }
        private async Task ExecutePostSolveCalculationsAsync()
        {
            try
            {
                // 1. Recopilamos todos los IFacade del Flowsheet
                var allFacades = new List<IFacade>();
                allFacades.AddRange(Equipments);
                allFacades.AddRange(Streams);

                // 2. Ejecutamos todos los Post-Cálculos (Envolventes, Cv, Potencia, FUG, etc.)
                // Podemos hacerlo en paralelo para que el procesador use todos sus núcleos
                var postSolveTasks = allFacades.Select(facade => facade.PostSolveAsync());
                await Task.WhenAll(postSolveTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Post-Cálculos: {ex.Message}");
            }
            finally
            {
                // 3. AHORA SÍ notificamos a la UI que TODA la matemática y los reportes terminaron.
                // Aquí la UI apagará su Spinner de "Solving..." y refrescará pantallas.
                OnSimulationCompleted?.Invoke();
            }
        }
        void SolveEquations()
        {
            Dictionary<SolverEquationType, List<ISolverEquation>> equationsByType = CreateEquationsByType();

            var pendingTypes = equationsByType
                .Where(kvp => kvp.Value != null && kvp.Value.Count > 0)
                .Select(kvp => kvp.Key)
                .ToList();

            bool globalMovement = true;
            int iter = 0;
            int maxIterations = 10;

            while (globalMovement && iter < maxIterations && pendingTypes.Count > 0)
            {
                globalMovement = false;

                foreach (var type in pendingTypes.ToList())
                {
                    var rawEquations = equationsByType[type]; // La lista original suelta
                    bool localMovement = true;

                    while (localMovement)
                    {
                        localMovement = false;

                        // MAGIA: Justo antes de iterar, agrupamos las ecuaciones que quedan
                        var clusteredEquations = ClusterEquations(rawEquations);

                        int i = 0;

                        while (i < clusteredEquations.Count)
                        {
                            var equation = clusteredEquations[i];
                            var resultSolver = Solver.Solve(equation);

                            if (resultSolver.Converged)
                            {
                                localMovement = true;
                                globalMovement = true;

                                // Extraer y borrar de la lista original
                                if (equation is CompositeEquation composite)
                                {
                                    foreach (var innerEq in composite.Equations)
                                    {
                                        rawEquations.Remove(innerEq);
                                    }
                                }
                                else
                                {
                                    rawEquations.Remove(equation);
                                }

                                clusteredEquations.RemoveAt(i);
                            }
                            else
                            {
                                i++;
                            }
                        }
                    }

                    if (rawEquations.Count == 0)
                    {
                        pendingTypes.Remove(type);
                        Console.WriteLine($"✅ Tipo '{type}' completamente resuelto. Pendientes: {pendingTypes.Count}");
                    }
                }

                if (!globalMovement) break;
                iter++;
            }

            if (pendingTypes.Count == 0)
            {
                Console.WriteLine($"🎉 Todas las ecuaciones resueltas en {iter} iteraciones globales");
            }
            else
            {
                Console.WriteLine($"⚠️ Convergencia incompleta. Tipos sin resolver: {string.Join(", ", pendingTypes)}");
            }
        }
        Dictionary<SolverEquationType, List<ISolverEquation>> CreateEquationsByType()
        {
            var allTasksByType = new Dictionary<SolverEquationType, List<ISolverEquation>>();
            foreach (var type in EquationTypes)
            {
                foreach (var equipment in Equipments)
                {
                    if (equipment.Equations.FirstOrDefault() == null) break;

                    // 1. Ecuaciones físicas normales del equipo
                    var equationsOfType = equipment.Equations.Where(x => x.EquationType == type).ToList();

                    // 🔥 CORRECCIÓN: Las especificaciones ahora entran en su propio tipo
                    if (type == SolverEquationType.Specification)
                    {
                        var specs = equipment.Specifications
                                             .Select(s => new SpecificationEquation(s))
                                             .ToList();
                        equationsOfType.AddRange(specs);
                    }

                    if (equationsOfType.Any())
                    {
                        if (!allTasksByType.ContainsKey(type))
                            allTasksByType[type] = new List<ISolverEquation>();
                        allTasksByType[type].AddRange(equationsOfType);
                    }

                }
            }
            return allTasksByType;
        }
      

        public void ClearCalculatedBySolver()
        {

            var equations = Equipments
                .SelectMany(x => x.Equations).ToList();
            if (equations.Count > 0 && equations[0] == null) return;

            var variables = Equipments
          
                .SelectMany(x => x.Equations)
                .Where(x => x.Variables != null)
                .Where(x => x.Variables.Any())
                .SelectMany(x => x.Variables)
                .Where(x => x.DataProcedence == VariableDefinedBy.Solver).ToList();

            foreach (var variable in variables)
            {
                variable.Clear(VariableDefinedBy.Solver);

            }
        }
        private List<ISolverEquation> ClusterEquations(List<ISolverEquation> baseEquations)
        {
            var clusters = new List<List<ISolverEquation>>();
            var unassigned = baseEquations.ToList();

            while (unassigned.Count > 0)
            {
                var currentCluster = new List<ISolverEquation> { unassigned[0] };
                unassigned.RemoveAt(0);

                bool added = true;
                while (added)
                {
                    added = false;
                    // Extraer las incógnitas actuales del clúster
                    var clusterUnknowns = currentCluster.SelectMany(e => e.AdjustableVariables()).Distinct().ToList();

                    for (int i = unassigned.Count - 1; i >= 0; i--)
                    {
                        var eqUnknowns = unassigned[i].AdjustableVariables();
                        var candidateEquation = unassigned[i];

                        // 1. ¿Comparten variables?
                        bool sharesVariables = eqUnknowns.Intersect(clusterUnknowns).Any();

                        // 2. ¿Es de un tipo DIFERENTE a todas las que ya están en el clúster?
                        // Esto evita agrupar 3 ecuaciones de Pressure, pero permite agrupar MassBalance + Specification
                        bool isDifferentType = currentCluster.All(e => e.EquationType != candidateEquation.EquationType);

                        if (sharesVariables && isDifferentType)
                        {
                            currentCluster.Add(candidateEquation);
                            unassigned.RemoveAt(i);
                            added = true;
                        }
                    }
                }
                clusters.Add(currentCluster);
            }

            // Convertimos a CompositeEquation si hay más de 1 en el clúster
            return clusters.Select(c => c.Count == 1 ? c[0] : new CompositeEquation(c)).ToList();
        }
        private List<ISolverEquation> ClusterEquations2(List<ISolverEquation> baseEquations)
        {
            var clusters = new List<List<ISolverEquation>>();
            var unassigned = baseEquations.ToList();

            while (unassigned.Count > 0)
            {
                var currentCluster = new List<ISolverEquation> { unassigned[0] };
                unassigned.RemoveAt(0);

                bool added = true;
                while (added)
                {
                    added = false;
                    // Extraer las incógnitas actuales del clúster (las que no están definidas)
                    var clusterUnknowns = currentCluster.SelectMany(e => e.AdjustableVariables()).Distinct().ToList();

                    for (int i = unassigned.Count - 1; i >= 0; i--)
                    {
                        var eqUnknowns = unassigned[i].AdjustableVariables();

                        // Si tienen al menos una incógnita en común, las agrupamos
                        if (eqUnknowns.Intersect(clusterUnknowns).Any())
                        {
                            currentCluster.Add(unassigned[i]);
                            unassigned.RemoveAt(i);
                            added = true;
                        }
                    }
                }
                clusters.Add(currentCluster);
            }

            // Convertimos a CompositeEquation si hay más de 1 en el clúster
            return clusters.Select(c => c.Count == 1 ? c[0] : new CompositeEquation(c)).ToList();
        }
    }
}
