using Shared.SolverConsecutive;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies;

public static class CalculatedVariableSetter
{
    public static void SetStreamCalculated<T>(IVariable<T> variable, T value)
        where T : Amount
    {
        if (variable.DataProcedence is not (VariableDefinedBy.Undefined or VariableDefinedBy.StreamCalculated))
        {
            return;
        }

        variable.SetValue(value, VariableDefinedBy.StreamCalculated);
    }
}
