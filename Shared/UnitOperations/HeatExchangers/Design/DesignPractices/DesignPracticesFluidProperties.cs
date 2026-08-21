namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed record DesignPracticesFluidProperties(
    double ViscosityLbFtHr,
    double CpBtuLbF,
    double ThermalConductivityBtuHrFtF,
    double DensityLbFt3);
