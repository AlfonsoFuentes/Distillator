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
                    if (compositionChanged || !oldIsValid)
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
                    if (!_facade.IsEquilibriumSolved)
                    {
                        return;
                    }

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


    
}
