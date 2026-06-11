using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
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
        SolverEquationType.MassEnergyBalance
    };
        public MainSolver()
        {
            Solver = new SolverNewtonSolver();
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
            try
            {
                ClearCalculatedBySolver();
                SolveEquations();
            }
            finally
            {
                // 🔥 NOTIFICAR QUE TERMINÓ (siempre, incluso si hay error)
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
                    // 1. Ecuaciones físicas del equipo
                    var equationsOfType = equipment.Equations.Where(x => x.EquationType == type).ToList();

                    // ✅ LA CORRECCIÓN: Como Specification ya no tiene EquationType, 
                    // las forzamos a entrar SOLO cuando el solver está evaluando MassBalance.
                    if (type == SolverEquationType.MassBalance)
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

            var variables = Equipments.SelectMany(x => x.Equations).SelectMany(x => x.Variables).Where(x => x.DataProcedence == VariableDefinedBy.Solver).ToList();

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
