using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class PHStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        public PHStrategy(IFacadeStream facade) => _facade = facade;

        public void Execute()
        {
          

            // 1. Ejecutar Flash Riguroso
            _facade.MaterialStream.PerformFlashPH();

           

            // 3. Registrar en Facade
            _facade.Temperature.SetValue(_facade.MaterialStream.Temperature, VariableDataProcedence.StreamCalculated);

            _facade.VaporFraction.SetValue(_facade.MaterialStream.VaporFraction, VariableDataProcedence.StreamCalculated);
            _facade.MaterialStream.CalculateBulkProperties();

        }
    }
    public class PHStrategy3 : IEquilibriumStrategy
    {
        private readonly IStreamFacade _facade;
        public PHStrategy3(IStreamFacade facade) => _facade = facade;

        public void Execute()
        {
            //var pres = _facade.Pressure.Value!;
            //var massen = _facade.MassEnthalpy.Value;

            //// 1. Ejecutar Flash Riguroso
            //_facade.MaterialStream.PerformFlashPH(pres, massen);

            //// 2. Extraer T y VF calculados


            //double calculatedVF = _facade.MaterialStream.VaporFraction;

            //// 3. Registrar en Facade
            //_facade.Temperature.SetValueFromStream(_facade.MaterialStream.Temperature, _facade.Name);

            //_facade.VaporFraction.SetValueFromStream(calculatedVF, _facade.Name);
            //_facade.MaterialStream.CalculateBulkProperties();

        }
    }
    public class PHStrategy2 : IEquilibriumStrategy
    {
        private readonly IStreamFacade2 _facade;
        public PHStrategy2(IStreamFacade2 facade) => _facade = facade;

        public void Execute()
        {
            //var pres = _facade.Pressure.Value!;
            //var massen = _facade.MassEnthalpy.Value;
         
            //// 1. Ejecutar Flash Riguroso
            //_facade.MaterialStream.PerformFlashPH(pres, massen);

            //// 2. Extraer T y VF calculados


            //double calculatedVF = _facade.MaterialStream.VaporFraction;

            //// 3. Registrar en Facade
            //_facade.Temperature.SetValueFromStream(_facade.MaterialStream.Temperature, _facade.Name);

            //_facade.VaporFraction.SetValueFromStream(calculatedVF, _facade.Name);
            //_facade.MaterialStream.CalculateBulkProperties();

        }
    }
}
