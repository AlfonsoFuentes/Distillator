using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class TFVLiquidStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream Stream => _facade.MaterialStream;



        public TFVLiquidStrategy(IFacadeStream facade)
        {
            _facade = facade;
        }

        public void Execute()
        {




            // ✅ 3. Resolver P_bubble: encontrar P donde Σ(K_i · z_i) = 1
            Stream.SolveSaturationPressure();

           
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Pressure.SetValue(_facade.MaterialStream.Pressure, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);
        }




    }
    //public class TFVLiquidStrategy3 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade _facade;
    //    private IMaterialStream Stream => _facade.MaterialStream;



    //    public TFVLiquidStrategy3(IStreamFacade facade)
    //    {
    //        _facade = facade;
    //    }

    //    public void Execute()
    //    {




    //        // ✅ 3. Resolver P_bubble: encontrar P donde Σ(K_i · z_i) = 1
    //        //double pBubble = Stream.SolveSaturationPressure();

    //        //var pressure = _facade.Pressure.Value;
    //        //pressure!.SetValue(pBubble, PressureUnits.KiloPascala);
    //        //// ✅ 4. Actualizar UI con resultado calculado
    //        //_facade.Pressure.SetValueFromStream(pressure, _facade.Name);
    //        //_facade.MaterialStream.CalculateBulkProperties();
    //        //_facade.MassEnthalpy.SetValueFromStream(_facade.MaterialStream.MassEnthalpy, _facade.Name);
    //    }




    //}
    ////public class TFVLiquidStrategy2 : IEquilibriumStrategy
    //{
    //    private readonly IStreamFacade2 _facade;
    //    private IMaterialStream Stream => _facade.MaterialStream;



    //    public TFVLiquidStrategy2(IStreamFacade2 facade)
    //    {
    //        _facade = facade;
    //    }

    //    public void Execute()
    //    {




    //        // ✅ 3. Resolver P_bubble: encontrar P donde Σ(K_i · z_i) = 1
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
