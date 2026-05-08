using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{
    public class CompMassFlowStrategy : IFlowsStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public CompMassFlowStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }
        public void Execute()
        {
            var composition = _facade.StreamComposition.Value!;
            double totalMassFlow = 0;// composition.Components.Sum(c => c.MassFlowValue?.GetValue(MassFlowUnits.Kg_hr) ?? 0);
            _facade.MassFlow.SetValueCalculated(new(totalMassFlow, MassFlowUnits.Kg_hr), _facade.Name);
  

            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double molarFlow = totalMassFlow / Molecularweight;
            _facade.MolarFlow.SetValueCalculated(new(molarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);
      

            double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
            double volumetricFlow = totalMassFlow / density;
            _facade.VolumetricFlow.SetValueCalculated(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);
            _facade.IsFlowSolved = true;
        }
    }
    public class CompMassFlowStrategy2 : IFlowsStrategy
    {
        private readonly IStreamFacade _facade;
        public CompMassFlowStrategy2(IStreamFacade facade)
        {
            _facade = facade;
        }
        public void Execute()
        {
            var composition = _facade.StreamComposition.Value!;
            double totalMassFlow = composition.Components.Sum(c => c.MassFlowSolver.Value?.GetValue(MassFlowUnits.Kg_hr) ?? 0);
            _facade.MassFlow.SetValueFromStream(new(totalMassFlow, MassFlowUnits.Kg_hr), _facade.Name);


            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double molarFlow = totalMassFlow / Molecularweight;
            _facade.MolarFlow.SetValueFromStream(new(molarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);


            double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
            double volumetricFlow = totalMassFlow / density;
            _facade.VolumetricFlow.SetValueFromStream(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);
            _facade.IsFlowSolved = true;
        }
    }
}
