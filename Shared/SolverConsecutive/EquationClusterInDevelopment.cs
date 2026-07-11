namespace Shared.SolverConsecutive
{
    public sealed class EquationClusterInDevelopment : ISolverEquation
    {
        private readonly List<ISolverEquation> _equations = new();

        public EquationClusterInDevelopment(
            SolverEquationType equationType,
            SolverEquationTypeModifier equationTypeModifier,
            IEnumerable<ISolverEquation>? equations = null)
        {
            EquationType = equationType;
            EquationTypeModifer = equationTypeModifier;

            if (equations == null)
            {
                return;
            }

            foreach (var equation in equations)
            {
                AddEquation(equation);
            }
        }

        public IReadOnlyList<ISolverEquation> Equations => _equations;

        public string Name => "Cluster: [" + string.Join(" | ", _equations.Select(equation => equation.Name)) + "]";

        public SolverEquationType EquationType { get; }

        public SolverEquationTypeModifier EquationTypeModifer { get; }

        public List<double> Residuals => _equations
            .SelectMany(equation => equation.Residuals)
            .ToList();

        public List<IVariable> Variables => _equations
            .SelectMany(equation => equation.Variables)
            .Distinct()
            .ToList();

        public void AddEquation(ISolverEquation equation)
        {
            if (_equations.Contains(equation))
            {
                return;
            }

            _equations.Add(equation);
        }
    }
}
