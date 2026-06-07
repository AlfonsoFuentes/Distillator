using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class TFVVaporStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream Stream => _facade.MaterialStream;



        public TFVVaporStrategy(IFacadeStream   facade)
        {
            _facade = facade;
        }

        public void Execute()
        {
            Stream.SolveSaturationPressure();

        
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Pressure.SetValue(Stream.Pressure, SolverConsecutive.VariableDefinedBy.StreamCalculated);

            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);
        }





    }
    //public class TFVVaporStrategy3 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade _facade;
    //    private IMaterialStream Stream => _facade.MaterialStream;



    //    public TFVVaporStrategy3(IStreamFacade facade)
    //    {
    //        _facade = facade;
    //    }

    //    public void Execute()
    //    {
    //        //double pBubble = Stream.SolveSaturationPressure();

    //        //var pressure = _facade.Pressure.Value;
    //        //pressure!.SetValue(pBubble, PressureUnits.KiloPascala);
    //        //// ✅ 4. Actualizar UI con resultado calculado
    //        //_facade.Pressure.SetValueFromStream(pressure, _facade.Name);

    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);
    //    }





    //}
    //public class TFVVaporStrategy2 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade2 _facade;
    //    private IMaterialStream Stream => _facade.MaterialStream;



    //    public TFVVaporStrategy2(IStreamFacade2 facade)
    //    {
    //        _facade = facade;
    //    }

    //    public void Execute()
    //    {
    //        //double pBubble = Stream.SolveSaturationPressure();

    //        //var pressure = _facade.Pressure.Value;
    //        //pressure!.SetValue(pBubble, PressureUnits.KiloPascala);
    //        //// ✅ 4. Actualizar UI con resultado calculado
    //        //_facade.Pressure.SetValueFromStream(pressure, _facade.Name);

    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);
    //    }





    //}

}
