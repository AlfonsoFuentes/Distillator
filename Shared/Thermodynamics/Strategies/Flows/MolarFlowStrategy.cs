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
                _facade.MassFlow.SetValue(totalMassAmount, VariableDataProcedence.StreamCalculated);

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
                            _facade.VolumetricFlow.SetValue(volAmount, VariableDataProcedence.StreamCalculated);
                        }
                    }

                    // Flujo de entalpía (usar entalpía molar)
                    if (_facade.MaterialStream.MolarEnthalpy != null)
                    {
                        double molarEnthalpy = _facade.MaterialStream.MolarEnthalpy.GetValue(MolarEnergyUnits.KJ_Kgmol);
                        double energyFlow = totalMolarFlow * molarEnthalpy;
                        var energyAmount = new EnergyFlow(energyFlow, EnergyFlowUnits.KJ_sg);
                        _facade.EnthalpyFlow.SetValue(energyAmount, VariableDataProcedence.StreamCalculated);
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
                    if (!comp.MolarFlow.IsSpecToCalculate)
                    {
                        comp.MolarFlow.SetValue(compMolarAmount, VariableDataProcedence.StreamCalculated);

                        // Calcular flujo másico del componente

                    }
                    if (!comp.MassFlow.IsSpecToCalculate)
                    {

                        comp.MassFlow.SetValue(compMassAmount, VariableDataProcedence.StreamCalculated);
                    }
                }
            }
        }
    }

    public class MolarFlowStrategy3 : IFlowsStrategy
    {
        private readonly IStreamFacade _facade;
        public MolarFlowStrategy3(IStreamFacade facade)
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

                if (!component.MassFlowSolver.IsDefinedByGeneralSolver && !component.MassFlowSolver.IsDefinedByEquipmentSolver)
                {

                    component.MassFlowSolver.SetValueFromStream(new(componentMassFlow, MassFlowUnits.Kg_hr), _facade.Name);
                }


                double componentMolarFlow = componentMassFlow / component.MolecularWeight;
                if (!component.MolarFlowSolver.IsDefinedByGeneralSolver && !component.MolarFlowSolver.IsDefinedByEquipmentSolver)
                {
                    //component.MolarFlowValue!.SetValue(componentMolarFlow, MolarFlowUnits.Kgmol_hr);

                    component.MolarFlowSolver.SetValueFromStream(new(componentMolarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);

                }
            }
        }
    }
    public class MolarFlowStrategy2 : IFlowsStrategy
    {
        private readonly IStreamFacade2 _facade;
        public MolarFlowStrategy2(IStreamFacade2 facade)
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

                if (!component.MassFlowSolver.IsDefinedByGeneralSolver && !component.MassFlowSolver.IsDefinedByEquipmentSolver)
                {

                    component.MassFlowSolver.SetValueFromStream(new(componentMassFlow, MassFlowUnits.Kg_hr), _facade.Name);
                }


                double componentMolarFlow = componentMassFlow / component.MolecularWeight;
                if (!component.MolarFlowSolver.IsDefinedByGeneralSolver && !component.MolarFlowSolver.IsDefinedByEquipmentSolver)
                {
                    //component.MolarFlowValue!.SetValue(componentMolarFlow, MolarFlowUnits.Kgmol_hr);

                    component.MolarFlowSolver.SetValueFromStream(new(componentMolarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);

                }
            }
        }
    }
}
