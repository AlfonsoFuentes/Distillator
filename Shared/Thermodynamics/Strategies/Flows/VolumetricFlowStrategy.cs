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
            double volumetricFlow = _facade.VolumetricFlow.Value!.GetValue(VolumetricFlowUnits.m3_hr);
            if (_facade.IsEquilibriumSolved)
            {
                double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad

                double massFlow = volumetricFlow * density;
                _facade.MassFlow.SetValueCalculated(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);
              

                double Molecularweight = _facade.MaterialStream.MolecularWeight;
                double molarFlow = massFlow / Molecularweight;
                _facade.MolarFlow.SetValueCalculated(new(molarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);
            

                var enthalpy = _facade.MaterialStream.MassEnthalpy.GetValue(MassEnergyUnits.KJ_Kg); // Suponiendo que el Facade tiene esta propiedad

                double energyFlow = massFlow * enthalpy;
                _facade.EnthalpyFlow.SetValueCalculated(new(energyFlow, EnergyFlowUnits.KJ_hr), _facade.Name);
               


                _facade.IsFlowSolved = true;

                foreach (var component in _facade.StreamComposition.Value!.Components)
                {
                    double componentMassFlow = massFlow * (component.MassFraction ?? 0);
                    // 👇 CORRECCIÓN DE BUG: Usar SetValueCalculated
                    component.MassFlowValue!.SetValue(componentMassFlow, MassFlowUnits.Kg_hr);

                    double componentMolarFlow = componentMassFlow / component.MolecularWeight;
                    component.MolarFlowValue!.SetValue(componentMolarFlow, MolarFlowUnits.Kgmol_hr);
                }
            }
                
        }
    }
}
