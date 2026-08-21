using Shared.SolverConsecutive;
using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;
using UnitSystem;

namespace Distillator.Core.Tests.Solver;

public sealed class SplitterEnthalpyEquationTests
{
    [Fact]
    public void Solve_WhenOutletVaporFractionIsStrongButEnthalpyCanBeSolved_ShouldPropagateMassEnthalpy()
    {
        var splitter = new SolverSplitter("SP-101");
        var inlet = new FacadeStream("Feed");
        var outlet = new FacadeStream("Outlet");

        splitter.SetInlet(inlet);
        splitter.AddOutlet(outlet);

        inlet.MassEnthalpy.SetValue(new MassEnergy(125, MassEnergyUnits.KJ_Kg), VariableDefinedBy.UserInput);
        outlet.VaporFraction.SetValue(new Percentage(0, PercentageUnits.Percentage), VariableDefinedBy.UserInput);

        var result = new NewtonSolver().Solve(new SplitterEnthalpyEquation(splitter));

        Assert.True(result.Converged);
        Assert.Equal(VariableDefinedBy.Solver, outlet.MassEnthalpy.DataProcedence);
        Assert.Equal(125, outlet.MassEnthalpy.Value.GetValue(MassEnergyUnits.KJ_Kg), precision: 6);
    }

    [Fact]
    public void Solve_WhenMassEnthalpyEquationIsNotSquare_ShouldFallbackToStrongVaporFraction()
    {
        var splitter = new SolverSplitter("SP-102");
        var inlet = new FacadeStream("Feed");
        var outlet = new FacadeStream("Outlet");

        splitter.SetInlet(inlet);
        splitter.AddOutlet(outlet);

        inlet.VaporFraction.SetValue(new Percentage(100, PercentageUnits.Percentage), VariableDefinedBy.UserInput);

        var result = new NewtonSolver().Solve(new SplitterEnthalpyEquation(splitter));

        Assert.True(result.Converged);
        Assert.Equal(VariableDefinedBy.Solver, outlet.VaporFraction.DataProcedence);
        Assert.Equal(100, outlet.VaporFraction.Value.GetValue(PercentageUnits.Percentage), precision: 6);
    }
}
