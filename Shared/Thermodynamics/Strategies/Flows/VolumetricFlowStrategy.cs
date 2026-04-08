using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{
    public class VolumetricFlowStrategy : IFlowsStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public VolumetricFlowStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }
        public void Execute()
        {
            double volumetricFlow = _facade.VolumetricFlowControlled.Value!.GetValue(VolumetricFlowUnits.m3_hr);
            double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad

            double massFlow = volumetricFlow * density;
            _facade.MassFlowControlled.SetValueCalculated(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);
            _facade.AddFlowVariable(_facade.MassFlowControlled);

            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double molarFlow = massFlow / Molecularweight;
            _facade.MolarFlowControlled.SetValueCalculated(new(molarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);
            _facade.AddFlowVariable(_facade.MolarFlowControlled);

            var enthalpy = _facade.MaterialStream.MassEnthalpy.GetValue(MassEnergyUnits.KJ_Kg); // Suponiendo que el Facade tiene esta propiedad

            double energyFlow = massFlow * enthalpy;
            _facade.EnthalpyFlow.SetValueCalculated(new(energyFlow, EnergyFlowUnits.KJ_hr), _facade.Name);
            _facade.AddFlowVariable(_facade.EnthalpyFlow);




            foreach (var component in _facade.StreamCompositionControlled.Value!.Components)
            {
                double componentMassFlow = massFlow * (component.MassFraction ?? 0);
                component.MassFlowValue = new(componentMassFlow, MassFlowUnits.Kg_hr);
                double componentMolarFlow = componentMassFlow / component.MolecularWeight;
                component.MolarFlowValue = new(componentMolarFlow, MolarFlowUnits.Kgmol_hr);
            }
        }
    }
}
