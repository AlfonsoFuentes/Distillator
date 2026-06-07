namespace Shared.SolverConsecutive
{
    public enum VariableDefinedBy
    {
        Undefined,
        UserInput,
        StreamCalculated,
        Solver,
        Specification,
    }
    public enum SolverEquationType
    {
        Pressure,
        Concentration,
        VaporFraction,
        Enthalpy,
        MassBalance,
        MassEnergyBalance
    }
}

