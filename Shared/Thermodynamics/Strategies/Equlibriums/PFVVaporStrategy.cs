using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class PFVVaporStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream Stream => _facade.MaterialStream;

        // Tolerancias locales


        public PFVVaporStrategy(IFacadeStream facade)
        {
            _facade = facade;
        }

        public void Execute()
        {


            Stream.SolveSaturationTemperature();

          
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Temperature.SetValue(Stream.Temperature, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);

        }





    }
    //public class PFVVaporStrategy3 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade _facade;
    //    private IMaterialStream Stream => _facade.MaterialStream;

    //    // Tolerancias locales


    //    public PFVVaporStrategy3(IStreamFacade facade)
    //    {
    //        _facade = facade;
    //    }

    //    public void Execute()
    //    {


    //        //double tBubble = Stream.SolveSaturationTemperature();

    //        //// ✅ 4. Actualizar UI con resultado calculado
    //        //var temperature = _facade.Temperature.Value;
    //        //temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);
    //        //// ✅ 4. Actualizar UI con resultado calculado
    //        //_facade.Temperature.SetValueFromStream(temperature, _facade.Name);
    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);

    //    }





    //}
    //public class PFVVaporStrategy2 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade2 _facade;
    //    private IMaterialStream Stream => _facade.MaterialStream;

    //    // Tolerancias locales


    //    public PFVVaporStrategy2(IStreamFacade2 facade)
    //    {
    //        _facade = facade;
    //    }

    //    public void Execute()
    //    {


    //        //double tBubble = Stream.SolveSaturationTemperature();

    //        //// ✅ 4. Actualizar UI con resultado calculado
    //        //var temperature = _facade.Temperature.Value;
    //        //temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);
    //        //// ✅ 4. Actualizar UI con resultado calculado
    //        //_facade.Temperature.SetValueFromStream(temperature, _facade.Name);
    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);

    //    }





    //}
}
