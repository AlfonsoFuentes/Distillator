using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class PTStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream Stream => _facade.MaterialStream;

        public PTStrategy(IFacadeStream facade)
        {
            _facade = facade;
        }

        public void Execute()
        {

            Stream.PerformFlashPT();


            _facade.VaporFraction.SetValue(Stream.VaporFraction, SolverConsecutive.VariableDefinedBy.StreamCalculated);

            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);

        }
    }
    //public class PTStrategy3 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade _facade;
    //    private IMaterialStream Stream => _facade.MaterialStream;

    //    public PTStrategy3(IStreamFacade facade)
    //    {
    //        _facade = facade;
    //    }

    //    public void Execute()
    //    {

    //        //Stream.PerformFlashPT();


    //        //_facade.VaporFraction.SetValueFromStream(Stream.VaporFraction, _facade.Name);

    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);

    //    }
    //}
    //public class PTStrategy2 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade2 _facade;
    //    private IMaterialStream Stream => _facade.MaterialStream;

    //    public PTStrategy2(IStreamFacade2 facade)
    //    {
    //        _facade = facade;
    //    }

    //    public void Execute()
    //    {

    //        //Stream.PerformFlashPT();


    //        //_facade.VaporFraction.SetValueFromStream(Stream.VaporFraction, _facade.Name);

    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);

    //    }
    //}

}
