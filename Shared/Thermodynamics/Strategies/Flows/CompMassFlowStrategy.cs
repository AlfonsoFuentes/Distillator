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
            var composition = _facade.StreamCompositionControlled.Value!;
            double totalMassFlow = composition.Components.Sum(c => c.MassFlowValue?.GetValue(MassFlowUnits.Kg_hr) ?? 0);
            _facade.MassFlowControlled.SetValueCalculated(new(totalMassFlow, MassFlowUnits.Kg_hr), _facade.Name);
            _facade.AddFlowVariable(_facade.MassFlowControlled);

            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double molarFlow = totalMassFlow / Molecularweight;
            _facade.MolarFlowControlled.SetValueCalculated(new(molarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);
            _facade.AddFlowVariable(_facade.MolarFlowControlled);

            double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
            double volumetricFlow = totalMassFlow / density;
            _facade.VolumetricFlowControlled.SetValueCalculated(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);
            _facade.State = StreamStateType.StreamCalculated;
        }
    }
}
