using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class TFVTwoPhaseStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        public TFVTwoPhaseStrategy(IFacadeStream facade) => _facade = facade;

        public void Execute()
        {
         
            // Buscamos la P que genera ese VF a la Temperatura actual
           _facade.MaterialStream.SolveFlashTVF();

           

            _facade.Pressure.SetValue(_facade.MaterialStream.Pressure, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);
        }
    }
    //public class TFVTwoPhaseStrategy3 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade _facade;
    //    public TFVTwoPhaseStrategy3(IStreamFacade facade) => _facade = facade;

    //    public void Execute()
    //    {
    //        //double targetVF = _facade.VaporFraction.Value;
    //        //// Buscamos la P que genera ese VF a la Temperatura actual
    //        //double pFound = _facade.MaterialStream.SolveFlashTVF(_facade.Temperature.Value, targetVF);

    //        //var pres = _facade.Pressure.Value!;
    //        //pres.SetValue(pFound, PressureUnits.KiloPascala);

    //        //_facade.Pressure.SetValueFromStream(pres, _facade.Name);
    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);
    //    }
    //}
    //public class TFVTwoPhaseStrategy2 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade2 _facade;
    //    public TFVTwoPhaseStrategy2(IStreamFacade2 facade) => _facade = facade;

    //    public void Execute()
    //    {
    //        //double targetVF = _facade.VaporFraction.Value;
    //        //// Buscamos la P que genera ese VF a la Temperatura actual
    //        //double pFound = _facade.MaterialStream.SolveFlashTVF(_facade.Temperature.Value, targetVF);

    //        //var pres = _facade.Pressure.Value!;
    //        //pres.SetValue(pFound, PressureUnits.KiloPascala);

    //        //_facade.Pressure.SetValueFromStream(pres, _facade.Name);
    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);
    //    }
    //}
}
