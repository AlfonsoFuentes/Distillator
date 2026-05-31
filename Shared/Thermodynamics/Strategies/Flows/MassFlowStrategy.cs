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
            double totalMassFlow = _facade.MassFlow.Value.GetValue(MassFlowUnits.Kg_sg);

            // 2. Calcular flujo molar total
            double molecularWeight = _facade.MaterialStream.MolecularWeight;
            if (molecularWeight > 0)
            {
                double totalMolarFlow = totalMassFlow / molecularWeight;
                var totalMolarAmount = new MolarFlow(totalMolarFlow, MolarFlowUnits.Kgmol_sg);
                _facade.MolarFlow.SetValue(totalMolarAmount, VariableDataProcedence.StreamCalculated);
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
                        var volAmount = new VolumetricFlow(volumetricFlow, VolumetricFlowUnits.m3_sg);
                        _facade.VolumetricFlow.SetValue(volAmount, VariableDataProcedence.StreamCalculated);
                    }
                }

                // Flujo de entalpía
                if (_facade.MaterialStream.MassEnthalpy != null)
                {
                    double massEnthalpy = _facade.MaterialStream.MassEnthalpy.GetValue(MassEnergyUnits.KJ_Kg);
                    double energyFlow = totalMassFlow * massEnthalpy;
                    var energyAmount = new EnergyFlow(energyFlow, EnergyFlowUnits.KJ_sg);
                    _facade.EnthalpyFlow.SetValue(energyAmount, VariableDataProcedence.StreamCalculated);
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

                var compMassAmount = new MassFlow(compMassFlow, MassFlowUnits.Kg_sg);
                double compMolarFlow = compMassFlow / comp.MolecularWeight;
                var compMolarAmount = new MolarFlow(compMolarFlow, MolarFlowUnits.Kgmol_sg);
                if (!comp.MassFlow.IsSpecToCalculate)
                {

                    comp.MassFlow.SetValue(compMassAmount, VariableDataProcedence.StreamCalculated);

                    // Calcular flujo molar del componente

                }
                if (!comp.MolarFlow.IsSpecToCalculate && molecularWeight > 0)
                {

                    comp.MolarFlow.SetValue(compMolarAmount, VariableDataProcedence.StreamCalculated);
                }
            }
        }
    }

    public class MassFlowStrategy3 : IFlowsStrategy
    {
        private readonly IStreamFacade _facade;
        public MassFlowStrategy3(IStreamFacade facade)
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

                if (!component.MassFlowSolver.IsDefinedByGeneralSolver && !component.MassFlowSolver.IsDefinedByEquipmentSolver)
                {
                    component.MassFlowSolver.SetValueFromStream(new(componentMassFlow, MassFlowUnits.Kg_hr), _facade.Name);
                }
                //component.MassFlowValue!.SetValue(componentMassFlow, MassFlowUnits.Kg_hr);

                double componentMolarFlow = componentMassFlow / component.MolecularWeight;
                if (!component.MolarFlowSolver.IsDefinedByGeneralSolver && !component.MolarFlowSolver.IsDefinedByEquipmentSolver)
                {
                    //component.MolarFlowValue!.SetValue(componentMolarFlow, MolarFlowUnits.Kgmol_hr);

                    component.MolarFlowSolver.SetValueFromStream(new(componentMolarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);

                }

            }


        }
    }

    public class MassFlowStrategy2 : IFlowsStrategy
    {
        private readonly IStreamFacade2 _facade;
        public MassFlowStrategy2(IStreamFacade2 facade)
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

                if (!component.MassFlowSolver.IsDefinedByGeneralSolver && !component.MassFlowSolver.IsDefinedByEquipmentSolver)
                {
                    component.MassFlowSolver.SetValueFromStream(new(componentMassFlow, MassFlowUnits.Kg_hr), _facade.Name);
                }
                //component.MassFlowValue!.SetValue(componentMassFlow, MassFlowUnits.Kg_hr);

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
