using Shared.MatrixSolvers;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Streams;

namespace Shared.UnitOperations.Basiss
{
    public interface IEquipmentFacade2 : ISolverEquationsProvider, IFacade
    {
        void AttachConnection(string portName, IStreamFacade2 connectedFacade);
        void DetachConnection(string portName);

    }

        public interface IEquipmentFacade : IFacade
        {
            // ═══════════════════════════════════════════════════════════
            // 🔹 CONEXIONES (multi-puerto)
            // ═══════════════════════════════════════════════════════════

            /// <summary>
            /// Conectar una corriente a un puerto nombrado del equipo.
            /// </summary>
            void AttachConnection(string portName, IStreamFacade connectedFacade);

            /// <summary>
            /// Desconectar una corriente de un puerto nombrado del equipo.
            /// </summary>
            void DetachConnection(string portName);

            /// <summary>
            /// Retorna los nombres de todos los puertos que este equipo expone.
            /// Ej: ["Suction", "Discharge"] para bomba, ["Inlet", "Outlet", "Bypass"] para válvula.
            /// </summary>
            IEnumerable<string> GetPortNames();

            /// <summary>
            /// Retorna la corriente conectada a un puerto específico, o null si no hay conexión.
            /// </summary>
            IStreamFacade? GetConnectedStream(string portName);

            // ═══════════════════════════════════════════════════════════
            // 🔹 VARIABLES CONTROLADAS (para que el solver NO sepa tipos concretos)
            // ═══════════════════════════════════════════════════════════

            /// <summary>
            /// Retorna TODAS las variables que este equipo controla o puede modificar.
            /// El solver usa esto para resetear/marcar variables sin saber el tipo concreto del equipo.
            /// </summary>
            IEnumerable<IVariable> GetControlledVariables();

            // ═══════════════════════════════════════════════════════════
            // 🔹 ECUACIONES PARA SOLVER REACTIVO
            // ═══════════════════════════════════════════════════════════

            /// <summary>
            /// Retorna las ecuaciones algebraicas que este equipo aporta al solver global.
            /// </summary>
            List<GlobalEquation> GetReactiveEquations(List<IVariable> allVariables);

          
        
    }
}
