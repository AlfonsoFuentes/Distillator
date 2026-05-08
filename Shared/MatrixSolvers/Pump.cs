namespace Shared.MatrixSolvers
{
    //public class Pump : SimulationEquipmentBase, IVariableOwner
    //{
    //    public IStreamMaterial Inlet { get; set; } = null!;
    //    public IStreamMaterial Outlet { get; set; } = null!;

    //    public Variable DeltaP { get; private set; }  // ← variable del sistema

    //    public Pump(string name, EquationSystem eqs) : base(name)
    //    {
    //        // crear ΔP como variable del sistema
    //        DeltaP = eqs.CreateVariable("DeltaP", this);
    //    }

    //    public override void BuildEquations(EquationSystem eqs)
    //    {
    //        if (Inlet == null || Outlet == null)
    //            throw new InvalidOperationException("Pump no conectada");

    //        // Masa
    //        eqs.AddEquation(x =>
    //            x[Outlet.MassFlow.Index] - x[Inlet.MassFlow.Index],
    //            EquationType.Model, "Mass balance pump");

    //        // Relación de presión
    //        eqs.AddEquation(x =>
    //            x[Outlet.Pressure.Index]
    //            - x[Inlet.Pressure.Index]
    //            - x[DeltaP.Index],
    //            EquationType.Model, "Pressure rise pump");
    //    }
    //}
    //public class ControlValve : SimulationEquipmentBase, IVariableOwner
    //{
    //    public IStreamMaterial Inlet { get; set; } = null!;
    //    public IStreamMaterial Outlet { get; set; } = null!;

    //    public Variable DeltaP { get; private set; }

    //    public ControlValve(string name, EquationSystem eqs) : base(name)
    //    {
    //        DeltaP = eqs.CreateVariable("DeltaP", this);
    //    }

    //    public override void BuildEquations(EquationSystem eqs)
    //    {
    //        if (Inlet == null || Outlet == null)
    //            throw new InvalidOperationException("Valve no conectada");

    //        // 🔹 Balance de masa (igual que bomba)
    //        eqs.AddEquation(x =>
    //            x[Outlet.MassFlow.Index] - x[Inlet.MassFlow.Index],
    //            EquationType.Model,
    //            "Mass balance valve");

    //        // 🔹 Caída de presión (diferente a bomba)
    //        eqs.AddEquation(x =>
    //            x[Inlet.Pressure.Index]
    //            - x[Outlet.Pressure.Index]
    //            - x[DeltaP.Index],
    //            EquationType.Model,
    //            "Pressure drop valve");
    //    }
    //}

}
