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
                double componentMassFlow = massFlow * (component.MassFractionSolver.Value);
                // 👇 CORRECCIÓN DE BUG: Usar SetValueCalculated
                //component.MassFlowValue!.SetValue(componentMassFlow, MassFlowUnits.Kg_hr);

                double componentMolarFlow = componentMassFlow / component.MolecularWeight;
                //component.MolarFlowValue!.SetValue(componentMolarFlow, MolarFlowUnits.Kgmol_hr);
            }


        }
    }
    public class MassFlowStrategy2 : IFlowsStrategy
    {
        private readonly IStreamFacade _facade;
        public MassFlowStrategy2(IStreamFacade facade)
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
            if (Molecularweight > 0)
            {
                _facade.MolarFlow.SetValueFromStream(new(molarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);
            }
               


            if (_facade.IsEquilibriumSolved)
            {
                var enthalpy = _facade.MaterialStream.MassEnthalpy.GetValue(MassEnergyUnits.KJ_Kg); // Suponiendo que el Facade tiene esta propiedad

                double energyFlow = massFlow * enthalpy;
                _facade.EnthalpyFlow.SetValueFromStream(new(energyFlow, EnergyFlowUnits.KJ_hr), _facade.Name);

                double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
                double volumetricFlow = massFlow / density;
                _facade.VolumetricFlow.SetValueFromStream(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);

                _facade.IsFlowSolved = true;
            }

            foreach (var component in _facade.StreamComposition.Value!.Components)
            {
                double componentMassFlow = massFlow * (component.MassFractionSolver.Value / 100);
                
                if(!component.MassFlowSolver.IsDefinedByGeneralSolver&&!component.MassFlowSolver.IsDefinedByEquipmentSolver)
                {
                  component.MassFlowSolver.SetValueFromStream(new(componentMassFlow, MassFlowUnits.Kg_hr), _facade.Name);
                }
                //component.MassFlowValue!.SetValue(componentMassFlow, MassFlowUnits.Kg_hr);
                
                double componentMolarFlow = componentMassFlow / component.MolecularWeight;
                if(!component.MolarFlowSolver.IsDefinedByGeneralSolver && !component.MolarFlowSolver.IsDefinedByEquipmentSolver)
                {
                    //component.MolarFlowValue!.SetValue(componentMolarFlow, MolarFlowUnits.Kgmol_hr);

                    component.MolarFlowSolver.SetValueFromStream(new(componentMolarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);

                }
               
            }


        }
    }
}
