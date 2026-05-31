using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{
    public class MolarFractionStrategy : IFlowsStrategy
    {
        private readonly IFacadeStream _facade;

        public MolarFractionStrategy(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void Execute()
        {
            var components = _facade.Composition.Components;

            // 1. Calcular Peso Molecular base para convertir fracciones
            double sumMassBase = components.Sum(c =>
                (c.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0) * c.MolecularWeight);

            if (sumMassBase <= 0) return;

            // 2. Sincronizar fracciones másicas complementarias
            foreach (var comp in components)
            {
                double moleFrac = comp.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
                double massFrac = (moleFrac * comp.MolecularWeight) / sumMassBase;

                comp.MassFraction.SetValue(
                    new Percentage(massFrac * 100, PercentageUnits.Percentage),
                    VariableDataProcedence.StreamCalculated);
            }
 

        }
    }
}
