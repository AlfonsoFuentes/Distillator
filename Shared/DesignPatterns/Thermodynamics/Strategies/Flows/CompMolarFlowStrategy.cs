using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics.Strategies.Flows
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
            var composition = _facade.StreamCompositionControlled.Value!;
            double totalMolarFlow = composition.Components.Sum(c => c.MolarFlowValue?.GetValue(MolarFlowUnits.Kgmol_hr) ?? 0);
            _facade.MolarFlowControlled.SetValueCalculated(new(totalMolarFlow, MolarFlowUnits.Kgmol_hr), _facade.Name);
            _facade.AddFlowVariable(_facade.MolarFlowControlled);

            double Molecularweight = _facade.MaterialStream.MolecularWeight;
            double massFlow = totalMolarFlow * Molecularweight;
            _facade.MassFlowControlled.SetValueCalculated(new(massFlow, MassFlowUnits.Kg_hr), _facade.Name);
            _facade.AddFlowVariable(_facade.MassFlowControlled);

            double density = _facade.MaterialStream.MassDensity.GetValue(MassDensityUnits.Kg_m3); // Suponiendo que el Facade tiene esta propiedad
            double volumetricFlow = massFlow / density;
            _facade.VolumetricFlowControlled.SetValueCalculated(new(volumetricFlow, VolumetricFlowUnits.m3_hr), _facade.Name);

        }
    }
}
