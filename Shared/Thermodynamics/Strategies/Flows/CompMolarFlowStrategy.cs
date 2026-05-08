using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{
    public class CompMolarFlowStrategy : IFlowsStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public CompMolarFlowStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }
        public void Execute()
        {
            var composition = _facade.StreamComposition.Value!;
            double totalMolarFlow = 0;// composition.Components.Sum(c => c.MolarFlowValue?.GetValue(MolarFlowUnits.Kgmol_hr) ?? 0);
            _facade.MolarFlow.SetValueCalculated(new(totalMolarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);
       

            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double massFlow = totalMolarFlow * Molecularweight;
            _facade.MassFlow.SetValueCalculated(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);
       

            double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
            double volumetricFlow = massFlow / density;
            _facade.VolumetricFlow.SetValueCalculated(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);
            _facade.IsFlowSolved = true;

        }
    }
    public class CompMolarFlowStrategy2 : IFlowsStrategy
    {
        private readonly IStreamFacade _facade;
        public CompMolarFlowStrategy2(IStreamFacade facade)
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
