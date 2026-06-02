using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.ControlledVariables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{
    public class FlowsCalculator : IProcessVariableOwner
    {
        private readonly IFacadeStream _facade;
        private IFlowsStrategy? _currentStrategy;

        public FlowsCalculator(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public HashSet<IProcessVariable> Variables { get; } = new();

        public void AddVariable(IProcessVariable variable)
        {
            if (!Variables.Contains(variable) && variable.DataProcedence == VariableDataProcedence.StreamCalculated)
            {
                Variables.Add(variable);
            }
        }

        public void RemoveVariables(VariableDataProcedence _procedence)
        {
            var toRemove = Variables.Where(v => v.DataProcedence == _procedence).ToList();
            foreach (var v in toRemove)
            {
                v.Clear(_procedence);
                Variables.Remove(v);
            }
        }

        bool IsSolvingbyFlows = false;

        public void Execute()
        {
            if (IsSolvingbyFlows)
                return;

            try
            {
                IsSolvingbyFlows = true;
                _facade.IsFlowSolved = false;
                RemoveVariables(VariableDataProcedence.StreamCalculated);

#if DEBUG
                Console.WriteLine($"\n  [FlowsCalc] 🌊 INICIANDO CÁLCULO DE FLUJOS para '{_facade.Name}'");
#endif

                // ✅ Solo recalcula fracciones si hubo un cambio real
                bool molarFractionsDefined = _facade.Composition.Components.All(c => c.MolarFraction.IsDefined);
                bool massFractionsDefined = _facade.Composition.Components.All(c => c.MassFraction.IsDefined);
                bool compMolarFlowsDefined = _facade.Composition.Components.All(c => c.MolarFlow.IsDefined);
                bool compMassFlowsDefined = _facade.Composition.Components.All(c => c.MassFlow.IsDefined);

                bool oldIsValid = _facade.Composition.IsValid;

                if (compMolarFlowsDefined)
                {
#if DEBUG
                    Console.WriteLine($"  [FlowsCalc] 🔹 Fase 1: Detectados Flujos Molares por Componente.");
#endif
                    _currentStrategy = new CompMolarFlowStrategy(_facade);
                    _currentStrategy.Execute();
                }
                else if (compMassFlowsDefined)
                {
#if DEBUG
                    Console.WriteLine($"  [FlowsCalc] 🔹 Fase 1: Detectados Flujos Másicos por Componente.");
#endif
                    _currentStrategy = new CompMassFlowStrategy(_facade);
                    _currentStrategy.Execute();
                }
                else if (molarFractionsDefined)
                {
#if DEBUG
                    Console.WriteLine($"  [FlowsCalc] 🔹 Fase 1: Detectadas Fracciones Molares.");
#endif
                    _currentStrategy = new MolarFractionStrategy(_facade);
                    _currentStrategy.Execute();
                }
                else if (massFractionsDefined)
                {
#if DEBUG
                    Console.WriteLine($"  [FlowsCalc] 🔹 Fase 1: Detectadas Fracciones Másicas.");
#endif
                    _currentStrategy = new MassFractionStrategy(_facade);
                    _currentStrategy.Execute();
                }

                if (_facade.Composition.IsValid)
                {
                    bool compositionChanged = _facade.Composition.HasChanged;
#if DEBUG
                    if (compositionChanged || !oldIsValid)
                    {
                        Console.WriteLine($"  [FlowsCalc] ♻️ Composición Válida y ha cambiado. Disparando CompositionChanged()...");
                    }
#endif
                    if (compositionChanged || !oldIsValid)
                    {
                        _facade.Composition.CompositionChanged();
                    }
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"  [FlowsCalc] 🛑 Abortando: La Composición no es válida o está incompleta.");
#endif
                    return;
                }

                // ✅ Fase de propagación global
                bool massFlowDefined = _facade.MassFlow.IsDefined;
                bool molarFlowDefined = _facade.MolarFlow.IsDefined;
                bool volumetricFlowDefined = _facade.VolumetricFlow.IsDefined;

                if (volumetricFlowDefined)
                {
                    if (!_facade.IsEquilibriumSolved)
                    {
#if DEBUG
                        Console.WriteLine($"  [FlowsCalc] ⚠️ VolumetricFlow detectado, pero el Equilibrio NO está resuelto. Esperando a Termodinámica...");
#endif
                        return;
                    }

#if DEBUG
                    Console.WriteLine($"  [FlowsCalc] 🌊 Fase 2: Aplicando VolumetricFlowStrategy...");
#endif
                    _currentStrategy = new VolumetricFlowStrategy(_facade);
                    _currentStrategy.Execute();
                    _facade.IsFlowSolved = true;
                    return;
                }

                if (massFlowDefined)
                {
#if DEBUG
                    Console.WriteLine($"  [FlowsCalc] 🌊 Fase 2: Aplicando MassFlowStrategy...");
#endif
                    _currentStrategy = new MassFlowStrategy(_facade);
                    _currentStrategy.Execute();
                    _facade.IsFlowSolved = true;
                    return;
                }

                if (molarFlowDefined)
                {
#if DEBUG
                    Console.WriteLine($"  [FlowsCalc] 🌊 Fase 2: Aplicando MolarFlowStrategy...");
#endif
                    _currentStrategy = new MolarFlowStrategy(_facade);
                    _currentStrategy.Execute();
                    _facade.IsFlowSolved = true;
                    return;
                }

#if DEBUG
                Console.WriteLine($"  [FlowsCalc] ℹ️ Fase 2 omitida: No hay Flujo Global especificado (Masa, Molar, o Volumétrico).");
#endif
            }
            finally
            {
                IsSolvingbyFlows = false;
            }
        }
    }


    public class FlowsCalculator4 : IProcessVariableOwner
    {
        private readonly IFacadeStream _facade;
        private IFlowsStrategy? _currentStrategy;



        public FlowsCalculator4(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));

        }
        public HashSet<IProcessVariable> Variables { get; } = new();
        public void AddVariable(IProcessVariable variable)
        {
            if (!Variables.Contains(variable) && variable.DataProcedence == VariableDataProcedence.StreamCalculated)
            {
                Variables.Add(variable);
            }
        }
        public void RemoveVariables(VariableDataProcedence _procedence)
        {
            var toRemove = Variables.Where(v => v.DataProcedence == _procedence).ToList();
            foreach (var v in toRemove)
            {
                v.Clear(_procedence);
                Variables.Remove(v);
            }

        }
        bool IsSolvingbyFlows = false;
        public void Execute()
        {
            if (IsSolvingbyFlows)
                return;

            try
            {
                IsSolvingbyFlows = true;
                _facade.IsFlowSolved = false;
                RemoveVariables(VariableDataProcedence.StreamCalculated);

                // ✅ Captura del estado de mutación


                // ✅ Solo recalcula fracciones si hubo un cambio real
                bool molarFractionsDefined = _facade.Composition.Components.All(c => c.MolarFraction.IsDefined);
                bool massFractionsDefined = _facade.Composition.Components.All(c => c.MassFraction.IsDefined);
                bool compMolarFlowsDefined = _facade.Composition.Components.All(c => c.MolarFlow.IsDefined);
                bool compMassFlowsDefined = _facade.Composition.Components.All(c => c.MassFlow.IsDefined);

                bool oldIsValid = _facade.Composition.IsValid;
                if (compMolarFlowsDefined)
                {
                    _currentStrategy = new CompMolarFlowStrategy(_facade);
                    _currentStrategy.Execute();
                }
                else if (compMassFlowsDefined)
                {
                    _currentStrategy = new CompMassFlowStrategy(_facade);
                    _currentStrategy.Execute();
                }
                else if (molarFractionsDefined)
                {
                    _currentStrategy = new MolarFractionStrategy(_facade);
                    _currentStrategy.Execute();
                }
                else if (massFractionsDefined)
                {
                    _currentStrategy = new MassFractionStrategy(_facade);
                    _currentStrategy.Execute();
                }

                if (_facade.Composition.IsValid)
                {
                    bool compositionChanged = _facade.Composition.HasChanged;
                    if (compositionChanged|| !oldIsValid)
                    {
                        _facade.Composition.CompositionChanged();
                    }
                }
                else
                {
                    return;
                }
                


                // ✅ Fase de propagación global
                bool massFlowDefined = _facade.MassFlow.IsDefined;
                bool molarFlowDefined = _facade.MolarFlow.IsDefined;
                bool volumetricFlowDefined = _facade.VolumetricFlow.IsDefined;

                if (volumetricFlowDefined)
                {
                    if (!_facade.IsEquilibriumSolved) return;

                    _currentStrategy = new VolumetricFlowStrategy(_facade);
                    _currentStrategy.Execute();
                    _facade.IsFlowSolved = true;
                    return;
                }

                if (massFlowDefined)
                {
                    _currentStrategy = new MassFlowStrategy(_facade);
                    _currentStrategy.Execute();
                    _facade.IsFlowSolved = true;
                    return;
                }

                if (molarFlowDefined)
                {
                    _currentStrategy = new MolarFlowStrategy(_facade);
                    _currentStrategy.Execute();
                    _facade.IsFlowSolved = true;
                    return;
                }
            }
            finally
            {
                IsSolvingbyFlows = false;
            }

        }


    }

    public class FlowsCalculator3
    {
        private readonly IStreamFacade _facade;
        private readonly IMaterialStream _materialStream;
        private IFlowsStrategy? _currentStrategy;




        public Action? FlowsReady;



        // ✅ Constructor inyecta interfaz
        public FlowsCalculator3(IStreamFacade facade)
        {
            _facade = facade;  // ← No conoce el Facade concreto
            _materialStream = facade.MaterialStream;


        }

        public void Execute()
        {
            _facade.IsFlowSolved = false;
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
            bool isEquilibriumReady = true;// _facade.State == StreamStateType.EquilibriumCalculated ||
                                           //   _facade.State == StreamStateType.StreamCalculated;

            // ---------------------------------------------------------------------
            // 🔹 RUTA 1: FLUJO VOLUMÉTRICO (Dependencia Fuerte: Termodinámica)
            // ---------------------------------------------------------------------
            // 🔥 REORDENAR las condiciones para priorizar compMolarFlowFromSolver:

            // ---------------------------------------------------------------------
            // 🔹 RUTA 0: FLUJOS POR COMPONENTE DESDE SOLVER (PRIORIDAD MÁXIMA)
            // ---------------------------------------------------------------------
            if (compMolarFlowFromSolver)
            {
                _currentStrategy = new CompMolarFlowStrategy3(_facade);
                _currentStrategy.Execute();
                return;
            }

            // ---------------------------------------------------------------------
            // 🔹 RUTA 1: FLUJO VOLUMÉTRICO (Dependencia Fuerte: Termodinámica)
            // ---------------------------------------------------------------------
            if (volumetricFlow)
            {
                if (!isEquilibriumReady) return;
                _currentStrategy = new VolumetricFlowStrategy3(_facade);
                _currentStrategy.Execute();
                return;
            }

            // ---------------------------------------------------------------------
            // 🔹 RUTA 2: FLUJOS MÁSICOS Y MOLARES (Dependencia Fuerte: Composición)
            // ---------------------------------------------------------------------
            if (massFlow)
            {
                _currentStrategy = new MassFlowStrategy3(_facade);
                _currentStrategy.Execute();
                return;
            }

            if (molarFlow)
            {
                _currentStrategy = new MolarFlowStrategy3(_facade);
                _currentStrategy.Execute();
                return;
            }

            if (compMassFlow)
            {
                _currentStrategy = new CompMassFlowStrategy3(_facade);
                _currentStrategy.Execute();
                return;
            }

            // 👇 compMolarFlow (InputType = MolarFlow) ya está cubierto por compMolarFlowFromSolver
            // pero lo dejamos como fallback por seguridad:
            if (compMolarFlow)
            {
                _currentStrategy = new CompMolarFlowStrategy3(_facade);
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
                if (!component.MolarFlowSolver.IsDefinedByEquipmentSolver || !component.MolarFlowSolver.IsDefinedByGeneralSolver)
                {

                    component.MolarFlowSolver.ClearFromStream();

                }
                if (!component.MassFlowSolver.IsDefinedByEquipmentSolver || !component.MassFlowSolver.IsDefinedByGeneralSolver)
                {
                    component.MassFlowSolver.ClearFromStream();
                }



            }
        }






    }

    public class FlowsCalculator2
    {
        private readonly IStreamFacade2 _facade;
        private readonly IMaterialStream _materialStream;
        private IFlowsStrategy? _currentStrategy;




        public Action? FlowsReady;


        // ✅ Constructor inyecta interfaz
        public FlowsCalculator2(IStreamFacade2 facade)
        {
            _facade = facade;  // ← No conoce el Facade concreto
            _materialStream = facade.MaterialStream;


        }

        public void Execute()
        {
            _facade.IsFlowSolved = false;
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
            bool isEquilibriumReady = true;// _facade.State == StreamStateType.EquilibriumCalculated ||
                                           // _facade.State == StreamStateType.StreamCalculated;

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
                if (!component.MolarFlowSolver.IsDefinedByEquipmentSolver || !component.MolarFlowSolver.IsDefinedByGeneralSolver)
                {

                    component.MolarFlowSolver.ClearFromStream();

                }
                if (!component.MassFlowSolver.IsDefinedByEquipmentSolver || !component.MassFlowSolver.IsDefinedByGeneralSolver)
                {
                    component.MassFlowSolver.ClearFromStream();
                }



            }
        }






    }
}
