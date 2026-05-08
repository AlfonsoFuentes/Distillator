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
                //component.MassFlowValue!.SetValue(0, MassFlowUnits.Kg_hr);
                //component.MolarFlowValue!.SetValue(0, MolarFlowUnits.Kgmol_hr);
            }
        }




    }
    public class FlowsCalculator2
    {
        private readonly IStreamFacade _facade;
        private readonly IMaterialStream _materialStream;
        private IFlowsStrategy? _currentStrategy;
        private FlowsMode _currentMode;



        public Action? FlowsReady;

        public FlowsMode CurrentMode => _currentMode;

        // ✅ Constructor inyecta interfaz
        public FlowsCalculator2(IStreamFacade facade)
        {
            _facade = facade;  // ← No conoce el Facade concreto
            _materialStream = facade.MaterialStream;
            _currentMode = FlowsMode.None;

        }

        public void Execute()
        {

            ResetComponetesFlows();
            _facade.RemoveFlowsCalculate();
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

            // 🔥 NUEVO: Detectar si TODOS los componentes fueron actualizados por el solver
            // Esto tiene PRIORIDAD sobre InputType, porque el solver ya resolvió los flujos individuales
            bool compMolarFlowFromSolver = _facade.StreamComposition.Value?.Components.Count > 0
                && _facade.StreamComposition.Value.Components.All(c => c.MolarFlowSolver.IsDefined);

            // Validamos si la termodinámica (T, P y Flash) ya está resuelta
            bool isEquilibriumReady = _facade.State == StreamStateType.EquilibriumCalculated ||
                                      _facade.State == StreamStateType.StreamCalculated;

            // ---------------------------------------------------------------------
            // 🔹 RUTA 1: FLUJO VOLUMÉTRICO (Dependencia Fuerte: Termodinámica)
            // ---------------------------------------------------------------------
            // 🔥 REORDENAR las condiciones para priorizar compMolarFlowFromSolver:

            // ---------------------------------------------------------------------
            // 🔹 RUTA 0: FLUJOS POR COMPONENTE DESDE SOLVER (PRIORIDAD MÁXIMA)
            // ---------------------------------------------------------------------
            if (compMolarFlowFromSolver)
            {
                _currentStrategy = new CompMolarFlowStrategy2(_facade);
                _currentStrategy.Execute();
                return;
            }

            // ---------------------------------------------------------------------
            // 🔹 RUTA 1: FLUJO VOLUMÉTRICO (Dependencia Fuerte: Termodinámica)
            // ---------------------------------------------------------------------
            if (volumetricFlow)
            {
                if (!isEquilibriumReady) return;
                _currentStrategy = new VolumetricFlowStrategy2(_facade);
                _currentStrategy.Execute();
                return;
            }

            // ---------------------------------------------------------------------
            // 🔹 RUTA 2: FLUJOS MÁSICOS Y MOLARES (Dependencia Fuerte: Composición)
            // ---------------------------------------------------------------------
            if (massFlow)
            {
                _currentStrategy = new MassFlowStrategy2(_facade);
                _currentStrategy.Execute();
                return;
            }

            if (molarFlow)
            {
                _currentStrategy = new MolarFlowStrategy2(_facade);
                _currentStrategy.Execute();
                return;
            }

            if (compMassFlow)
            {
                _currentStrategy = new CompMassFlowStrategy2(_facade);
                _currentStrategy.Execute();
                return;
            }

            // 👇 compMolarFlow (InputType = MolarFlow) ya está cubierto por compMolarFlowFromSolver
            // pero lo dejamos como fallback por seguridad:
            if (compMolarFlow)
            {
                _currentStrategy = new CompMolarFlowStrategy2(_facade);
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
                if (!component.MolarFlowSolver.IsDefinedByEquipmentSolver||!component.MolarFlowSolver.IsDefinedByGeneralSolver)
                {

                    component.MolarFlowSolver.ClearFromStream();

                }
                if (!component.MassFlowSolver.IsDefinedByEquipmentSolver||!component.MassFlowSolver.IsDefinedByGeneralSolver)
                {
                    component.MassFlowSolver.ClearFromStream();
                }



            }
        }






    }
}
