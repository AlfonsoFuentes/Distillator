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
            var molarFlow = _facade.MolarFlow.Value!.GetValue(MolarFlowUnits.Kgmol_hr);

            
           

            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double massFlow = molarFlow * Molecularweight;
            _facade.MassFlow.SetValueCalculated(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);
       
            if (_facade.IsEquilibriumSolved)
            {
                var enthalpy = _facade.MaterialStream.MolarEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol); // Suponiendo que el Facade tiene esta propiedad

                double energyFlow = molarFlow * enthalpy;
                _facade.EnthalpyFlow.SetValueCalculated(new(energyFlow, EnergyFlowUnits.KJ_hr), _facade.Name);

              
                double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
                double volumetricFlow = massFlow / density;
                _facade.VolumetricFlow.SetValueCalculated(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);

          
                _facade.IsFlowSolved = true;
            }


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
