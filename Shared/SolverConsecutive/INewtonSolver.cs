using Shared.SolverQwen.Simlations;

namespace Shared.SolverConsecutive
{
    public interface INewtonSolver
    {
        SolverResult Solve(ISolverEquation mainSolver, double _alpha = 1.0);
    }

    internal class DampedStepResult
    {
        public double[] XNew { get; set; } = null!;
        public bool Converged { get; set; }
        public double FinalError { get; set; }
    }
}
