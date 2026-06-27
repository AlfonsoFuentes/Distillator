using Shared.SolverConsecutive;
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

        public HashSet<IVariable> Variables { get; } = new();

        public void AddVariable(IVariable variable)
        {
            if (!Variables.Contains(variable) && variable.DataProcedence == VariableDefinedBy.StreamCalculated)
            {
                Variables.Add(variable);
            }
        }

        public void RemoveVariables(VariableDefinedBy _procedence)
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
                RemoveVariables(VariableDefinedBy.StreamCalculated);

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


    
}
