using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{

    /// <summary>
    /// Estrategia: el flujo volumétrico está definido → calcular másicos, molares y por componente.
    /// Requiere equilibrio resuelto para obtener densidad.
    /// </summary>
    public class VolumetricFlowStrategy : IFlowsStrategy
    {
        private readonly IFacadeStream _facade;

        public VolumetricFlowStrategy(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void Execute()
        {
            // Validar que el equilibrio esté resuelto (necesario para densidad)
            if (!_facade.IsEquilibriumSolved || _facade.MaterialStream.MassDensity == null)
            {
              
                return;
            }

            // 1. Leer flujo volumétrico
            double volumetricFlow = _facade.VolumetricFlow.Value.GetValue(VolumetricFlowUnits.m3_sg);

            // 2. Calcular flujo másico total usando densidad
            double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3);
            if (density <= 0)
            {
                
                return;
            }

            double totalMassFlow = volumetricFlow * density;
            var totalMassAmount = new MassFlow(totalMassFlow, MassFlowUnits.Kg_sg);
            _facade.MassFlow.SetValue(totalMassAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);

            // 3. Calcular flujo molar total
            double molecularWeight = _facade.MaterialStream.MolecularWeight;
            if (molecularWeight > 0)
            {
                double totalMolarFlow = totalMassFlow / molecularWeight;
                var totalMolarAmount = new MolarFlow(totalMolarFlow, MolarFlowUnits.Kgmol_sg);
                _facade.MolarFlow.SetValue(totalMolarAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            }

            // 4. Calcular flujo de entalpía
            if (_facade.MaterialStream.MassEnthalpy != null)
            {
                double massEnthalpy = _facade.MaterialStream.MassEnthalpy.GetValue(MassEnergyUnits.KJ_Kg);
                double energyFlow = totalMassFlow * massEnthalpy;
                var energyAmount = new EnergyFlow(energyFlow, EnergyFlowUnits.KJ_sg);
                _facade.EnthalpyFlow.SetValue(energyAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            }

            if(!_facade.Composition.IsValid)   
            {
                return;
            }
    
            foreach (var comp in _facade.Composition.Components)
            {
                if (comp.MassFlow.DataProcedence != SolverConsecutive.VariableDefinedBy.UserInput    )
                {
                    double massFrac = comp.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
                    double compMassFlow = totalMassFlow * massFrac;

                    var compMassAmount = new MassFlow(compMassFlow, MassFlowUnits.Kg_sg);
                    comp.MassFlow.SetValue(compMassAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);

                    // Calcular flujo molar del componente
                    if (!comp.MolarFlow.ShouldTriggerRecalculation && molecularWeight > 0)
                    {
                        double compMolarFlow = compMassFlow / comp.MolecularWeight;
                        var compMolarAmount = new MolarFlow(compMolarFlow, MolarFlowUnits.Kgmol_sg);
                        comp.MolarFlow.SetValue(compMolarAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);
                    }
                }
            }
        }
    }

   
}
