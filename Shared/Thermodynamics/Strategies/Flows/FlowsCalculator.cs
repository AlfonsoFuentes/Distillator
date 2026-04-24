using Shared.Thermodynamics.ControlledVariables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{
    public class FlowsCalculator
    {
        private readonly StreamSimulationFacade _facade;
        private readonly MaterialStream _materialStream;
        private IFlowsStrategy? _currentStrategy;
        private FlowsMode _currentMode;



        public Action? FlowsReady;

        public FlowsMode CurrentMode => _currentMode;

        // ✅ Constructor inyecta interfaz
        public FlowsCalculator(StreamSimulationFacade facade)
        {
            _facade = facade;  // ← No conoce el Facade concreto
            _materialStream = facade.MaterialStream;
            _currentMode = FlowsMode.None;
      
        }

        public void OnConstraintsChanged()
        {
            _facade.ResetFlowsCalculatedVariable();
            ResetComponetesFlows();

            // 1. CONDICIÓN BASE ESTEQUIOMÉTRICA
            // Sin composición, no hay Peso Molecular. Sin Peso Molecular, no podemos 
            // convertir entre masa y moles. Por lo tanto, abortamos si no hay composición.
            if (!_facade.StreamComposition.IsDefined)
            {
                return;
            }

            // 2. LECTURA DE VARIABLES DEFINIDAS
            bool massFlow = _facade.MassFlow.IsDefined;
            bool molarFlow = _facade.MolarFlow.IsDefined;
            bool volumetricFlow = _facade.VolumetricFlow.IsDefined;
            bool compMassFlow = _facade.StreamComposition.Value!.InputType == ComponentInputType.MassFlow;
            bool compMolarFlow = _facade.StreamComposition.Value!.InputType == ComponentInputType.MolarFlow;

            // Validamos si la termodinámica (T, P y Flash) ya está resuelta
            bool isEquilibriumReady = _facade.State == StreamStateType.EquilibriumCalculated ||
                                      _facade.State == StreamStateType.StreamCalculated;

            // ---------------------------------------------------------------------
            // 🔹 RUTA 1: FLUJO VOLUMÉTRICO (Dependencia Fuerte: Termodinámica)
            // ---------------------------------------------------------------------
            if (volumetricFlow)
            {
                if (!isEquilibriumReady)
                {
                    // Se definió volumétrico pero falta densidad. Nos detenemos elegantemente.
                    // La corriente se quedará en estado "Underspecified" hasta que el usuario meta T y P.
                    return;
                }

                _currentStrategy = new VolumetricFlowStrategy(_facade);
                _currentStrategy.Execute();
         
                return;
            }

            // ---------------------------------------------------------------------
            // 🔹 RUTA 2: FLUJOS MÁSICOS Y MOLARES (Dependencia Fuerte: Composición)
            // (Llegan aquí sin importar si isEquilibriumReady es true o false)
            // ---------------------------------------------------------------------
            if (massFlow)
            {
                _currentStrategy = new MassFlowStrategy(_facade);
                _currentStrategy.Execute();
             
                return;
            }

            if (molarFlow)
            {
                _currentStrategy = new MolarFlowStrategy(_facade);
                _currentStrategy.Execute();
         
                return;
            }

            if (compMassFlow)
            {
                _currentStrategy = new CompMassFlowStrategy(_facade);
                _currentStrategy.Execute();
         
                return;
            }

            if (compMolarFlow)
            {
                _currentStrategy = new CompMolarFlowStrategy(_facade);
                _currentStrategy.Execute();
             
                return;
            }
        }

        // Método de seguridad para evaluar el semáforo de la corriente
       

        void ResetComponetesFlows()
        {
            if (!_facade.StreamComposition.IsDefined) return;

            foreach (var component in _facade.StreamComposition.Value!.Components)
            {
                component.MassFlowValue!.SetValue(0, MassFlowUnits.Kg_hr);
                component.MolarFlowValue!.SetValue(0, MolarFlowUnits.Kgmol_hr);
            }
        }




    }
}
