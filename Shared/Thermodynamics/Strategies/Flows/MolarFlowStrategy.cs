using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{

    /// <summary>
    /// Estrategia: el flujo molar total está definido → calcular másicos, volumétricos y por componente.
    /// </summary>
    public class MolarFlowStrategy : IFlowsStrategy
    {
        private readonly IFacadeStream _facade;

        public MolarFlowStrategy(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void Execute()
        {
            // 1. Leer flujo molar total
            double totalMolarFlow = _facade.MolarFlow.Value.GetValue(MolarFlowUnits.Kgmol_sg);

            // 2. Calcular flujo másico total
            double molecularWeight = _facade.MaterialStream.MolecularWeight;
            if (molecularWeight > 0)
            {
                double totalMassFlow = totalMolarFlow * molecularWeight;
                var totalMassAmount = new MassFlow(totalMassFlow, MassFlowUnits.Kg_sg);
                _facade.MassFlow.SetValue(totalMassAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);

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
                            var volAmount = new VolumetricFlow(volumetricFlow, VolumetricFlowUnits.m3_sg);
                            _facade.VolumetricFlow.SetValue(volAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);
                        }
                    }

                    // Flujo de entalpía (usar entalpía molar)
                    if (_facade.MaterialStream.MolarEnthalpy != null)
                    {
                        double molarEnthalpy = _facade.MaterialStream.MolarEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol);
                        double energyFlow = totalMolarFlow * molarEnthalpy;
                        var energyAmount = new EnergyFlow(energyFlow, EnergyFlowUnits.KJ_sg);
                        _facade.EnthalpyFlow.SetValue(energyAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);
                    }
                }
                if (!_facade.Composition.IsValid)
                {
                    return;
                }
                // 4. Propagar a componentes usando fracciones molares
                foreach (var comp in _facade.Composition.Components)
                {
                    double moleFrac = comp.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
                    double compMolarFlow = totalMolarFlow * moleFrac;

                    var compMolarAmount = new MolarFlow(compMolarFlow, MolarFlowUnits.Kgmol_sg);
                    double compMassFlow = compMolarFlow * comp.MolecularWeight;
                    var compMassAmount = new MassFlow(compMassFlow, MassFlowUnits.Kg_sg);
                    if (!comp.MolarFlow.ShouldTriggerRecalculation)
                    {
                        comp.MolarFlow.SetValue(compMolarAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);

                        // Calcular flujo másico del componente

                    }
                    if (!comp.MassFlow.ShouldTriggerRecalculation)
                    {

                        comp.MassFlow.SetValue(compMassAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);
                    }
                }
            }
        }
    }

   
}
