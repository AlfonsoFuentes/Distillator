using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Componentes;
using UnitSystem;

namespace Shared.SolverQwen.Stream
{

    public class ComponentFacade
    {
        private readonly MethodComponentFullDto _DataBase;


        public Guid Id => _DataBase.ComponentId;
        public string Name => _DataBase.ComponentName;
        public string Formula => _DataBase.FullData?.Formula ?? string.Empty;
        public double MolecularWeight => _DataBase.FullData?.MolecularWeight ?? 0.0;

        public Variable<Percentage> MassFraction { get; }
        public Variable<Percentage> MolarFraction { get; }
        public Variable<MassFlow> MassFlow { get; }
        public Variable<MolarFlow> MolarFlow { get; }

        public ComponentFacade(MethodComponentFullDto database)
        {
            _DataBase = database ?? throw new ArgumentNullException(nameof(database));


            MassFraction = new Variable<Percentage>(new Percentage(0, PercentageUnits.Percentage), PercentageUnits.Percentage, 100, true);
            MolarFraction = new Variable<Percentage>(new Percentage(0, PercentageUnits.Percentage), PercentageUnits.Percentage,100);
            MassFlow = new Variable<MassFlow>(new MassFlow(0, MassFlowUnits.Kg_sg), MassFlowUnits.Kg_hr, 3, true);
            MolarFlow = new Variable<MolarFlow>(new MolarFlow(0, MolarFlowUnits.Kgmol_sg), MolarFlowUnits.Kgmol_hr, 3);
        }



    }




}