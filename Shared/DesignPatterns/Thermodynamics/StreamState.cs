namespace Shared.DesignPatterns.Thermodynamics
{
    public abstract class StreamState
    {
        // Un único método para manejar la lógica y la transición
        public abstract void Handle(StreamSimulationFacade facade);
    }
    // Estado 1: Cuando se crea
    public class StreamCreatedState : StreamState
    {
        public override void Handle(StreamSimulationFacade facade)
        {
            
        }
    }

    // Estado 2: Método termodinámico definido
    public class MethodDefinedState : StreamState
    {
        public override void Handle(StreamSimulationFacade facade)
        {
           
        }
    }

    // Estado 3: Equilibrio calculado
    public class EquilibriumCalculatedState : StreamState
    {
        public override void Handle(StreamSimulationFacade facade)
        {
           
        }
    }

    // Estado 4: Corriente calculada
    public class StreamCalculatedState : StreamState
    {
        public override void Handle(StreamSimulationFacade facade)
        {
           
        }
    }
}
