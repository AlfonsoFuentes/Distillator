using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{

    /// <summary>
    /// Estrategia: los flujos molares de componentes están definidos → calcular totales y derivados.
    /// </summary>
    public class CompMolarFlowStrategy : IFlowsStrategy
    {
        private readonly IFacadeStream _facade;

        public CompMolarFlowStrategy(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void Execute()
        {
            double totalMolarFlow = 0;
            double totalMassFlowBase = 0; // Usado únicamente como pivote matemático para fracciones

            // 1. Recorrer componentes para sumar base molar y másica
            foreach (var comp in _facade.Composition.Components)
            {
                double compMolarFlow = comp.MolarFlow.Value.GetValue(MolarFlowUnits.Kgmol_sg) ;
                totalMolarFlow += compMolarFlow;
                totalMassFlowBase += (compMolarFlow * comp.MolecularWeight);
            }

            if (totalMolarFlow <= 0 || totalMassFlowBase <= 0) return;

            // 2. Setear ÚNICAMENTE el flujo global principal que le corresponde a esta estrategia
            var totalMolarAmount = new MolarFlow(totalMolarFlow, MolarFlowUnits.Kgmol_sg);
            _facade.MolarFlow.SetValue(totalMolarAmount, VariableDataProcedence.StreamCalculated);

            // 3. Setear ÚNICAMENTE las fracciones (dejamos la propagación a la estrategia global MolarFlowStrategy)
            foreach (var comp in _facade.Composition.Components)
            {
                double compMolarFlow =  comp.MolarFlow.Value.GetValue(MolarFlowUnits.Kgmol_sg) ;
                double compMassFlowBase = compMolarFlow * comp.MolecularWeight;

                comp.MolarFraction.SetValue(new Percentage((compMolarFlow / totalMolarFlow) * 100, PercentageUnits.Percentage), VariableDataProcedence.StreamCalculated);
                comp.MassFraction.SetValue(new Percentage((compMassFlowBase / totalMassFlowBase) * 100, PercentageUnits.Percentage), VariableDataProcedence.StreamCalculated);
            }
      
        }
    }
    public class CompMolarFlowStrategy3 : IFlowsStrategy
    {
        private readonly IStreamFacade _facade;
        public CompMolarFlowStrategy3(IStreamFacade facade)
        {
            _facade = facade;
        }
        public void Execute()
        {
            //var composition = _facade.StreamComposition.Value!;
            //double totalMolarFlow = composition.Components.Sum(c => c.MolarFlowSolver.Value?.GetValue(MolarFlowUnits.Kgmol_hr) ?? 0);
            //_facade.MolarFlow.SetValueFromStream(new(totalMolarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);


            //double Molecularweight = _facade.MaterialStream.MolecularWeight;
            //double massFlow = totalMolarFlow * Molecularweight;
            //_facade.MassFlow.SetValueFromStream(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);


            //double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
            //double volumetricFlow = massFlow / density;
            //_facade.VolumetricFlow.SetValueFromStream(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);
            //_facade.IsFlowSolved = true;

        }
    }

    public class CompMolarFlowStrategy2 : IFlowsStrategy
    {
        private readonly IStreamFacade2 _facade;
        public CompMolarFlowStrategy2(IStreamFacade2 facade)
        {
            _facade = facade;
        }
        public void Execute()
        {
            var composition = _facade.StreamComposition.Value!;
            double totalMolarFlow = composition.Components.Sum(c => c.MolarFlowSolver.Value?.GetValue(MolarFlowUnits.Kgmol_hr) ?? 0);
            _facade.MolarFlow.SetValueFromStream(new(totalMolarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);


            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double massFlow = totalMolarFlow * Molecularweight;
            _facade.MassFlow.SetValueFromStream(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);


            double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
            double volumetricFlow = massFlow / density;
            _facade.VolumetricFlow.SetValueFromStream(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);
            _facade.IsFlowSolved = true;

        }
    }
}
