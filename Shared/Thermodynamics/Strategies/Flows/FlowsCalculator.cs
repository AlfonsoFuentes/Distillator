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
            // ✅ PASO 1: Validar Regla de Fases de Gibbs
            ResetComponetesFlows();
            if (_facade.State != StreamStateType.EquilibriumCalculated)
            {
                return;
            }

            // ✅ PASO 2: Sistema válido, proceder con estrategia
            bool massflow = _facade.MassFlowControlled.IsDefined;
            bool molarFlow = _facade.MolarFlowControlled.IsDefined;
            bool volumetricFlow = _facade.VolumetricFlowControlled.IsDefined;
            bool CompMassFlow = _facade.StreamCompositionControlled.IsDefined && _facade.StreamCompositionControlled.Value!.InputType == ComponentInputType.MassFlow;

            bool CompMolarFlow = _facade.StreamCompositionControlled.IsDefined && _facade.StreamCompositionControlled.Value!.InputType == ComponentInputType.MolarFlow;

            if (massflow)
            {
                _currentStrategy = new MassFlowStrategy(_facade);
                _currentStrategy.Execute();
                _facade.State = StreamStateType.StreamCalculated;
                return;
            }
            if (molarFlow)
            {
                _currentStrategy = new MolarFlowStrategy(_facade);
                _currentStrategy.Execute();
                _facade.State = StreamStateType.StreamCalculated;
                return;
            }
            if (volumetricFlow)
            {
                _currentStrategy = new VolumetricFlowStrategy(_facade);
                _currentStrategy.Execute();
                _facade.State = StreamStateType.StreamCalculated;
                return;
            }
            if (CompMassFlow)
            {
                _currentStrategy = new CompMassFlowStrategy(_facade);
                _currentStrategy.Execute();
                _facade.State = StreamStateType.StreamCalculated;
                return;
            }
            if (CompMolarFlow)
            {
                _currentStrategy = new CompMolarFlowStrategy(_facade);
                _currentStrategy.Execute();
                _facade.State = StreamStateType.StreamCalculated;
                return;
            }

        }

        void ResetComponetesFlows()
        {
            foreach (var component in _facade.StreamCompositionControlled.Value!.Components)
            {
                component.MassFlowValue!.SetValue(0, MassFlowUnits.Kg_hr);
                component.MolarFlowValue!.SetValue(0, MolarFlowUnits.Kgmol_hr);
            }
        }




    }
}
