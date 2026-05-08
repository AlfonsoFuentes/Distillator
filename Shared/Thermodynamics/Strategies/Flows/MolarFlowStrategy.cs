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
            if (Molecularweight > 0)
            {
                _facade.MassFlow.SetValueCalculated(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);
            }

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
                double componentMassFlow = massFlow * (component.MassFractionSolver.Value);
                // 👇 CORRECCIÓN DE BUG: Usar SetValueCalculated
                //component.MassFlowValue!.SetValue(componentMassFlow, MassFlowUnits.Kg_hr);

                double componentMolarFlow = componentMassFlow / component.MolecularWeight;
                //component.MolarFlowValue!.SetValue(componentMolarFlow, MolarFlowUnits.Kgmol_hr);

            }
        }
    }
    public class MolarFlowStrategy2 : IFlowsStrategy
    {
        private readonly IStreamFacade _facade;
        public MolarFlowStrategy2(IStreamFacade facade)
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
            _facade.MassFlow.SetValueFromStream(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);

            if (_facade.IsEquilibriumSolved)
            {
                var enthalpy = _facade.MaterialStream.MolarEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol); // Suponiendo que el Facade tiene esta propiedad

                double energyFlow = molarFlow * enthalpy;
                _facade.EnthalpyFlow.SetValueFromStream(new(energyFlow, EnergyFlowUnits.KJ_hr), _facade.Name);


                double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
                double volumetricFlow = massFlow / density;
                _facade.VolumetricFlow.SetValueFromStream(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);


                _facade.IsFlowSolved = true;
            }


            foreach (var component in _facade.StreamComposition.Value!.Components)
            {
                double componentMassFlow = massFlow * (component.MassFractionSolver.Value) / 100;

                if (!component.MassFlowSolver.IsDefinedByGeneralSolver&&!component.MassFlowSolver.IsDefinedByEquipmentSolver)
                {

                    component.MassFlowSolver.SetValueFromStream(new(componentMassFlow, MassFlowUnits.Kg_hr), _facade.Name);
                }


                double componentMolarFlow = componentMassFlow / component.MolecularWeight;
                if (!component.MolarFlowSolver.IsDefinedByGeneralSolver&&!component.MolarFlowSolver.IsDefinedByEquipmentSolver)
                {
                    //component.MolarFlowValue!.SetValue(componentMolarFlow, MolarFlowUnits.Kgmol_hr);

                    component.MolarFlowSolver.SetValueFromStream(new(componentMolarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);

                }
            }
        }
    }
}
