using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{

    /// <summary>
    /// Estrategia: los flujos molares de componentes están definidos → calcular totales y derivados.
    /// </summary>
    public class CompMolarFlowStrategy : IFlowsStrategy
    {
        private readonly IFacadeStream _facade;

        public CompMolarFlowStrategy(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void Execute()
        {
            double totalMolarFlow = 0;
            double totalMassFlowBase = 0; // Usado únicamente como pivote matemático para fracciones

            // 1. Recorrer componentes para sumar base molar y másica
            foreach (var comp in _facade.Composition.Components)
            {
                double compMolarFlow = comp.MolarFlow.Value.GetValue(MolarFlowUnits.Kgmol_sg) ;
                totalMolarFlow += compMolarFlow;
                totalMassFlowBase += (compMolarFlow * comp.MolecularWeight);
            }

            if (totalMolarFlow <= 0 || totalMassFlowBase <= 0) return;

            // 2. Setear ÚNICAMENTE el flujo global principal que le corresponde a esta estrategia
            var totalMolarAmount = new MolarFlow(totalMolarFlow, MolarFlowUnits.Kgmol_sg);
            _facade.MolarFlow.SetValue(totalMolarAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);

            // 3. Setear ÚNICAMENTE las fracciones (dejamos la propagación a la estrategia global MolarFlowStrategy)
            foreach (var comp in _facade.Composition.Components)
            {
                double compMolarFlow =  comp.MolarFlow.Value.GetValue(MolarFlowUnits.Kgmol_sg) ;
                double compMassFlowBase = compMolarFlow * comp.MolecularWeight;

                comp.MolarFraction.SetValue(new Percentage((compMolarFlow / totalMolarFlow) * 100, PercentageUnits.Percentage), SolverConsecutive.VariableDefinedBy.StreamCalculated);
                comp.MassFraction.SetValue(new Percentage((compMassFlowBase / totalMassFlowBase) * 100, PercentageUnits.Percentage), SolverConsecutive.VariableDefinedBy.StreamCalculated);
            }
      
        }
    }
    
}
