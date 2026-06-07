using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{

    /// <summary>
    /// Estrategia: los flujos másicos de componentes están definidos → calcular totales y derivados.
    /// </summary>
    public class CompMassFlowStrategy : IFlowsStrategy
    {
        private readonly IFacadeStream _facade;

        public CompMassFlowStrategy(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void Execute()
        {
            double totalMassFlow = 0;
            double totalMolarFlowBase = 0; // Usado únicamente como pivote matemático para fracciones

            // 1. Recorrer componentes para sumar base másica y molar
            foreach (var comp in _facade.Composition.Components)
            {
                double compMassFlow = comp.MassFlow.Value.GetValue(MassFlowUnits.Kg_sg);
                totalMassFlow += compMassFlow;
                totalMolarFlowBase += (compMassFlow / comp.MolecularWeight);
            }

            if (totalMassFlow <= 0 || totalMolarFlowBase <= 0) return;

            // 2. Setear ÚNICAMENTE el flujo global principal que le corresponde a esta estrategia
            var totalMassAmount = new MassFlow(totalMassFlow, MassFlowUnits.Kg_sg);
            _facade.MassFlow.SetValue(totalMassAmount, SolverConsecutive.VariableDefinedBy.StreamCalculated);

            // 3. Setear ÚNICAMENTE las fracciones (dejamos la propagación a la estrategia global MassFlowStrategy)
            foreach (var comp in _facade.Composition.Components)
            {
                double compMassFlow = comp.MassFlow.Value.GetValue(MassFlowUnits.Kg_sg) ;
                double compMolarFlowBase = compMassFlow / comp.MolecularWeight;

                comp.MassFraction.SetValue(new Percentage((compMassFlow / totalMassFlow) * 100, PercentageUnits.Percentage), SolverConsecutive.VariableDefinedBy.StreamCalculated);
                comp.MolarFraction.SetValue(new Percentage((compMolarFlowBase / totalMolarFlowBase) * 100, PercentageUnits.Percentage), SolverConsecutive.VariableDefinedBy.StreamCalculated);
            }

        }
    }

    //public class CompMassFlowStrategy3 : IFlowsStrategy
    //{
    //    private readonly IStreamFacade _facade;
    //    public CompMassFlowStrategy3(IStreamFacade facade)
    //    {
    //        _facade = facade;
    //    }
    //    public void Execute()
    //    {
    //        var composition = _facade.StreamComposition.Value!;
    //        double totalMassFlow = composition.Components.Sum(c => c.MassFlowSolver.Value?.GetValue(MassFlowUnits.Kg_hr) ?? 0);
    //        _facade.MassFlow.SetValueFromStream(new(totalMassFlow, MassFlowUnits.Kg_hr), _facade.Name);


    //        double Molecularweight = _facade.MaterialStream.MolecularWeight;
    //        double molarFlow = totalMassFlow / Molecularweight;
    //        _facade.MolarFlow.SetValueFromStream(new(molarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);


    //        double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
    //        double volumetricFlow = totalMassFlow / density;
    //        _facade.VolumetricFlow.SetValueFromStream(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);
    //        _facade.IsFlowSolved = true;
    //    }
    //}
    //public class CompMassFlowStrategy2 : IFlowsStrategy
    //{
    //    private readonly IStreamFacade2 _facade;
    //    public CompMassFlowStrategy2(IStreamFacade2 facade)
    //    {
    //        _facade = facade;
    //    }
    //    public void Execute()
    //    {
    //        var composition = _facade.StreamComposition.Value!;
    //        double totalMassFlow = composition.Components.Sum(c => c.MassFlowSolver.Value?.GetValue(MassFlowUnits.Kg_hr) ?? 0);
    //        _facade.MassFlow.SetValueFromStream(new(totalMassFlow, MassFlowUnits.Kg_hr), _facade.Name);


    //        double Molecularweight = _facade.MaterialStream.MolecularWeight;
    //        double molarFlow = totalMassFlow / Molecularweight;
    //        _facade.MolarFlow.SetValueFromStream(new(molarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);


    //        double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
    //        double volumetricFlow = totalMassFlow / density;
    //        _facade.VolumetricFlow.SetValueFromStream(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);
    //        _facade.IsFlowSolved = true;
    //    }
    //}
}
