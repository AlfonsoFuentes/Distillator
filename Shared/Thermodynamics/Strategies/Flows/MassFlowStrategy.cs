using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{
    public class MassFlowStrategy : IFlowsStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public MassFlowStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }
        public void Execute()
        {
            // ✅ Implementar cálculo de flujos basado en masa
            // Ejemplo: Q = m_dot * h, donde m_dot es el flujo másico y h es la entalpía específica
            var massFlow = _facade.MassFlow.Value!.GetValue(MassFlowUnits.Kg_hr);
           

            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double molarFlow = massFlow / Molecularweight;

            _facade.MolarFlow.SetValueCalculated(new(molarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);
      

            if (_facade.IsEquilibriumSolved)
            {
                var enthalpy = _facade.MaterialStream.MassEnthalpy.GetValue(MassEnergyUnits.KJ_Kg); // Suponiendo que el Facade tiene esta propiedad

                double energyFlow = massFlow * enthalpy;
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
