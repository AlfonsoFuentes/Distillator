using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class PFVTwoPhaseStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        public PFVTwoPhaseStrategy(IFacadeStream facade) => _facade = facade;

        public void Execute()
        {
           
            // Buscamos la T que genera ese VF a la Presión actual
            _facade.MaterialStream.SolveFlashPVF();

           

            _facade.Temperature.SetValue(_facade.MaterialStream.Temperature, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);

        }
    }
    //public class PFVTwoPhaseStrategy3 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade _facade;
    //    public PFVTwoPhaseStrategy3(IStreamFacade facade) => _facade = facade;

    //    public void Execute()
    //    {
    //        //double targetVF = _facade.VaporFraction.Value;
    //        //// Buscamos la T que genera ese VF a la Presión actual
    //        //double tFound = _facade.MaterialStream.SolveFlashPVF(_facade.Pressure.Value, targetVF);

    //        //var temp = _facade.Temperature.Value!;
    //        //temp.SetValue(tFound, TemperatureUnits.Kelvin);

    //        //_facade.Temperature.SetValueFromStream(temp, _facade.Name);
    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);

    //    }
    //}
    //public class PFVTwoPhaseStrategy2 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade2 _facade;
    //    public PFVTwoPhaseStrategy2(IStreamFacade2 facade) => _facade = facade;

    //    public void Execute()
    //    {
    //        //double targetVF = _facade.VaporFraction.Value;
    //        //// Buscamos la T que genera ese VF a la Presión actual
    //        //double tFound = _facade.MaterialStream.SolveFlashPVF(_facade.Pressure.Value, targetVF);

    //        //var temp = _facade.Temperature.Value!;
    //        //temp.SetValue(tFound, TemperatureUnits.Kelvin);

    //        //_facade.Temperature.SetValueFromStream(temp, _facade.Name);
    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);

    //    }
    //}

}
