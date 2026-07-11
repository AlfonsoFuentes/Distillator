namespace Shared.SolverConsecutive
{
    public enum VariableDefinedBy
    {
        Undefined,
        UserInput,
        StreamCalculated,
        Solver,
        Specification,
        Equipment,
    }
    public enum SolverEquationType
    {
        Pressure,
        Concentration,
        VaporFraction,
        Enthalpy,
        MassBalance,
        MassEnergyBalance,
        Specification
    }
    public enum SolverEquationTypeModifier
    {
        Regular,
        Spec
    }
}

