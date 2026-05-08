using Shared.Thermodynamics.ControlledVariables;
using UnitSystem;

namespace Shared.MatrixSolvers
{
    public class EquationSystem
    {

        public List<INewNewVariable> Variables { get; } = new();
        List<INewNewVariable> _localVariables { get; } = new();
        public List<Equation> Equations { get; } = new();
        List<Equation> _localEquations { get; } = new();

        // 🔥 Control interno de especificaciones
        private readonly Dictionary<int, Equation> _specEquations = new();


        public void SolveEquipmet()
        {
            var _VariablesBySolver = Variables.Where(x => x.IsDefinedByEquipmentSolver).ToList();
            foreach (var v in _VariablesBySolver)
            {
                v.ClearFromEquipmentSolver();
            }

            int Index = 0;
            Variables.Clear();
            Equations.Clear();
            _specEquations.Clear();
            foreach (var v in _localVariables)
            {
                v.Index = Index++;
                Variables.Add(v);
                if (v.IsDefined)
                {
                    SetSpecification(v);

                }
                else
                {
                    v.IsToDefineByEquipmentSolver = true;
                }
            }
            foreach (var v in _localEquations)
            {
                Equations.Add(v);
            }

            if (Variables.Count == 0 || Equations.Count != Variables.Count)
            {
                var _VariablesTodefine = Variables.Where(x => x.IsToDefineByEquipmentSolver).ToList();
                foreach (var v in _VariablesTodefine)
                {
                    v.IsToDefineByEquipmentSolver = false;
                }
                return;
            }
            var solver = new NewtonSolver();

            var result = solver.SolveEquipment(this);
        }

        public void SolveGeneral()
        {
            var _VariablesBySolver = Variables.Where(x => x.IsDefinedByGeneralSolver).ToList();
            foreach (var v in _VariablesBySolver)
            {
                v.ClearFromGeneralSolver();
            }

            int Index = 0;
            Variables.Clear();
            Equations.Clear();
            _specEquations.Clear();
            foreach (var v in _localVariables)
            {
                v.Index = Index++;
                Variables.Add(v);
                if (v.IsDefined && !v.IsDefinedByEquipmentSolver)
                {
                    SetSpecification(v);
                }
                else
                {
                    v.IsToDefineByGeneralSolver = true;
                }
            }
            foreach (var v in _localEquations)
            {
                Equations.Add(v);
            }

            if (Variables.Count == 0 || Equations.Count != Variables.Count)
            {
                var _VariablesTodefine = Variables.Where(x => x.IsToDefineByGeneralSolver).ToList();
                foreach (var v in _VariablesTodefine)
                {
                    v.IsToDefineByGeneralSolver = false;
                }
                return;
            }
            var solver = new NewtonSolver();

            var result = solver.SolveGeneral(this);
        }
        public void Clear()
        {

            _localVariables.Clear();
            _localEquations.Clear();

        }

        public void CreateFromFacades(EquationSystem source)
        {
            // 1. Agregar variables nuevas (sin duplicar objetos)
            foreach (var v in source._localVariables)
            {
                if (!_localVariables.Contains(v))
                {
                    _localVariables.Add(v);
                    // 🔥 Reasignar índice en el contexto global
                    v.Index = _localVariables.Count - 1;
                }
            }

            // 2. Agregar ecuaciones nuevas (sin duplicar por referencia)
            foreach (var eq in source._localEquations)
            {
                if (!_localEquations.Contains(eq))  // Evita duplicados por referencia de objeto
                {
                    _localEquations.Add(eq);
                }
            }
        }

        public void AddVariables(IEnumerable<INewNewVariable> variables)
        {
            foreach (var v in variables)
            {
                if (!_localVariables.Contains(v))
                {
                    _localVariables.Add(v);
                    // 🔥 Asignar índice provisional (se reasignará en Solve*, pero sirve para crear ecuaciones)
                    v.Index = _localVariables.Count - 1;
                }
            }
        }


        public void AddEquations(List<Equation> _equations)
        {
            _localEquations.AddRange(_equations);
        }
        public void AddEquation(Equation _equations)
        {
            _localEquations.Add(_equations);
        }

        void SetSpecification(INewNewVariable v)
        {
            // 🔄 Si ya existe → actualizar
            if (_specEquations.TryGetValue(v.Index, out var existingEq))
            {
                existingEq.Function = x => x[v.Index] - v.SolverValue;

                return;
            }

            // ➕ Si no existe → crear
            var eq = new Equation
            {
                Function = x => x[v.Index] - v.SolverValue,
                Type = EquationType.Specification,

            };

            Equations.Add(eq);
            _specEquations[v.Index] = eq;
        }


        public void RemoveSpecification(INewNewVariable v)
        {
            if (_specEquations.TryGetValue(v.Index, out var eq))
            {
                Equations.Remove(eq);
                _specEquations.Remove(v.Index);
            }
        }

        /// <summary>
        /// Elimina todas las especificaciones
        /// </summary>
        public void RemoveAllSpecifications()
        {
            foreach (var eq in _specEquations.Values)
            {
                Equations.Remove(eq);
            }

            _specEquations.Clear();
        }

        // =========================
        // 🔹 EVALUACIÓN
        // =========================
        public double[] Evaluate(double[] x)
        {
            if (x.Length != Variables.Count)
                return new double[Equations.Count]; // O lanzar excepción

            var res = new double[Equations.Count];

            for (int i = 0; i < Equations.Count; i++)
            {
                res[i] = Equations[i].Function(x);
            }

            return res;
        }



    }
}
