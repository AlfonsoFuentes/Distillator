using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{
    public class MolarFlowStrategy : IFlowsStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public MolarFlowStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }
        public void Execute()
        {
            // ✅ Implementar cálculo de flujos basado en moles
            // Ejemplo: Q = n_dot * h, donde n_dot es el flujo molar y h es la entalpía molar específica
            var molarFlow = _facade.MolarFlowControlled.Value!.GetValue(MolarFlowUnits.Kgmol_hr);
            var enthalpy = _facade.MaterialStream.MolarEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol); // Suponiendo que el Facade tiene esta propiedad

            double energyFlow = molarFlow * enthalpy;
            _facade.EnthalpyFlow.SetValueCalculated(new(energyFlow, EnergyFlowUnits.KJ_hr), _facade.Name);

            _facade.AddFlowVariable(_facade.EnthalpyFlow);

            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double massFlow = molarFlow * Molecularweight;
            _facade.MassFlowControlled.SetValueCalculated(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);
            _facade.AddFlowVariable(_facade.MassFlowControlled);

            double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
            double volumetricFlow = massFlow / density;
            _facade.VolumetricFlowControlled.SetValueCalculated(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);

            _facade.AddFlowVariable(_facade.VolumetricFlowControlled);
            foreach (var component in _facade.StreamCompositionControlled.Value!.Components)
            {
                double componentMolarFlow = molarFlow * (component.MolarFraction ?? 0);
                component.MolarFlowValue = new(componentMolarFlow, MolarFlowUnits.Kgmol_hr);
                double componentMassFlow = componentMolarFlow * component.MolecularWeight;
                component.MassFlowValue = new(componentMassFlow, MassFlowUnits.Kg_hr);
            }
        }
    }
}
