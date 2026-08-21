using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{

    /// <summary>
    /// Estrategia: el flujo másico total está definido → calcular molares, volumétricos y por componente.
    /// </summary>
    public class MassFlowStrategy : IFlowsStrategy
    {
        private readonly IFacadeStream _facade;

        public MassFlowStrategy(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void Execute()
        {
            // 1. Leer flujo másico total
            double totalMassFlow = _facade.MassFlow.Value.GetValue(MassFlowUnits.Kg_hr);

            // 2. Calcular flujo molar total
            double molecularWeight = _facade.MaterialStream.MolecularWeight;
            if (molecularWeight > 0)
            {
                double totalMolarFlow = totalMassFlow / molecularWeight;
                var totalMolarAmount = new MolarFlow(totalMolarFlow, MolarFlowUnits.Kgmol_hr);
                CalculatedVariableSetter.SetStreamCalculated(_facade.MolarFlow, totalMolarAmount);
            }

            // 3. Calcular derivados si el equilibrio está resuelto
            if (_facade.IsEquilibriumSolved)
            {
                // Flujo volumétrico
                if (_facade.MaterialStream.MassDensity != null)
                {
                    double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3);
                    if (density > 0)
                    {
                        double volumetricFlow = totalMassFlow / density;
                        var volAmount = new VolumetricFlow(volumetricFlow, VolumetricFlowUnits.m3_hr);
                        CalculatedVariableSetter.SetStreamCalculated(_facade.VolumetricFlow, volAmount);
                    }
                }

                // Flujo de entalpía
                if (_facade.MaterialStream.MassEnthalpy != null)
                {
                    double massEnthalpy = _facade.MaterialStream.MassEnthalpy.GetValue(MassEnergyUnits.KJ_Kg);
                    double energyFlow = totalMassFlow * massEnthalpy;
                    var energyAmount = new EnergyFlow(energyFlow, EnergyFlowUnits.KJ_hr);
                    CalculatedVariableSetter.SetStreamCalculated(_facade.EnthalpyFlow, energyAmount);
                }
            }
            if (!_facade.Composition.IsValid)
            {
                return;
            }
            // 4. Propagar a componentes usando fracciones másicas
            foreach (var comp in _facade.Composition.Components)
            {
                double massFrac = comp.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
                double compMassFlow = totalMassFlow * massFrac;

                var compMassAmount = new MassFlow(compMassFlow, MassFlowUnits.Kg_hr);
                double compMolarFlow = compMassFlow / comp.MolecularWeight;
                var compMolarAmount = new MolarFlow(compMolarFlow, MolarFlowUnits.Kgmol_hr);
                if (!comp.MassFlow.ShouldTriggerRecalculation)
                {

                    CalculatedVariableSetter.SetStreamCalculated(comp.MassFlow, compMassAmount);

                    // Calcular flujo molar del componente

                }
                if (!comp.MolarFlow.ShouldTriggerRecalculation && molecularWeight > 0)
                {

                    CalculatedVariableSetter.SetStreamCalculated(comp.MolarFlow, compMolarAmount);
                }
            }
        }
    }

    
}
