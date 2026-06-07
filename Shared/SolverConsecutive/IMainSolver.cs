using Shared.MatrixSolvers;
using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Equipments;
using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using System;
using System.Collections.Generic;
using System.Text;
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

        ThermodynamicMethodFullDto ThermoMethod { get; }
        void SetThermodynamicMethod(ThermodynamicMethodFullDto method);
        void RunSimulation();


    }
    public class MainSolver : IMainSolver
    {
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
        }
        public List<IFacadeStream> Streams { get; } = new();
        public List<ISolverEquipment> Equipments { get; } = new();
        public ThermodynamicMethodFullDto ThermoMethod { get; private set; } = null!;
        public void AddStream(IFacadeStream stream)
        {
            Streams.Add(stream);
            stream.SetThermodynamicMethod(ThermoMethod);
        }
        public void RemoveStream(IFacadeStream stream) => Streams.Remove(stream);
        public void AddEquipment(ISolverEquipment equipment) => Equipments.Add(equipment);
        public void RemoveEquipment(ISolverEquipment equipment) => Equipments.Remove(equipment);
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto method) => ThermoMethod = method;
        public void RunSimulation()
        {
            ClearCalculatedBySolver();
            SolveEquations();

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
        void SolveEquations2()
        {
            Dictionary<SolverEquationType, List<ISolverEquation>> equationsByType = CreateEquationsByType();

            // ✅ Lista dinámica de tipos que AÚN tienen ecuaciones
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

                // ✅ Solo iterar sobre tipos que AÚN tienen ecuaciones
                foreach (var type in pendingTypes.ToList()) // ToList() para poder modificar durante iteración
                {
                    var equations = equationsByType[type];
                    bool localMovement = true;

                    while (localMovement)
                    {
                        localMovement = false;
                        int i = 0;

                        while (i < equations.Count)
                        {
                            var equation = equations[i];
                            var resultSolver = Solver.Solve(equation);

                            if (resultSolver.Converged)
                            {
                                localMovement = true;
                                globalMovement = true;
                                equations.RemoveAt(i);
                            }
                            else
                            {
                                i++;
                            }
                        }
                    }

                    // ✅ Si este tipo ya no tiene ecuaciones, eliminarlo de pendingTypes
                    if (equations.Count == 0)
                    {
                        pendingTypes.Remove(type);
                        Console.WriteLine($"✅ Tipo '{type}' completamente resuelto. Pendientes: {pendingTypes.Count}");
                    }
                }

                if (!globalMovement) break;
                iter++;
            }

            // ✅ Reporte final
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
        Dictionary<SolverEquationType, List<ISolverEquation>> CreateEquationsByType2()
        {
            var allTasksByType = new Dictionary<SolverEquationType, List<ISolverEquation>>();
            foreach (var type in EquationTypes)
            {
                foreach (var equipment in Equipments)
                {
                    var equationsOfType = equipment.Equations.Where(x => x.EquationType == type).ToList();
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
