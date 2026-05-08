using Shared.MatrixSolvers;
using Shared.UnitOperations.Basiss;

namespace Shared.Thermodynamics.ControlledVariables
{
    public interface IVariable
    {
        int Index { get; set; }
        string Name { get; set; }
        double UnitLessValue { get; set; }
        IFacade Owner { get; set; }
        bool IsDefined { get; }
        double? SpecifiedValue { get; }
    }
   

    public interface IControlledVariable
    {
        MethodSource Source { get; set; }
        string SourceId { get; set; }
        bool IsDefined { get; }

        // 👇 Agregamos los parámetros opcionales
        void ClearValue();
        void RevertCalculatedValue();
    }
   
}



