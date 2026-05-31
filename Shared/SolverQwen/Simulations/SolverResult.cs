namespace Shared.SolverQwen.Simlations
{

    public class SolverResult
    {
        public bool Converged { get; }
        public int Iterations { get; }
        public double FinalError { get; }

        public SolverResult(bool converged, int iterations, double finalError)
        {
            Converged = converged;
            Iterations = iterations;
            FinalError = finalError;
        }
    }
    // Ejemplo: Un Intercambiador de Calor implementando ISimulationSystem (o una parte de él)
   
}