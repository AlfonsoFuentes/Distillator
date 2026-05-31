using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Flows
{
    public class MassFractionStrategy : IFlowsStrategy
    {
        private readonly IFacadeStream _facade;

        public MassFractionStrategy(IFacadeStream facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void Execute()
        {
            var components = _facade.Composition.Components;

            double sumMassFraction = components.Sum(c => c.MassFraction.Value.GetValue(PercentageUnits.Percentage));
            if(Math.Abs(sumMassFraction - 100) > 0.01)
            {
                // Log warning: Mass fractions do not sum to 100%
                return;
            }
            // 1. Calcular Peso Molecular base para convertir fracciones
            double sumMassBase = components.Sum(c =>
                (c.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0) / c.MolecularWeight);

            if (sumMassBase <= 0) return;

            // 2. Sincronizar fracciones másicas complementarias
            foreach (var comp in components)
            {
                double massFrac = comp.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
                double molarFracc = (massFrac / comp.MolecularWeight) / sumMassBase;

                comp.MolarFraction.SetValue(
                    new Percentage(molarFracc * 100, PercentageUnits.Percentage),
                    VariableDataProcedence.StreamCalculated);
            }

           

        }
    }
}
