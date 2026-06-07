using Shared.MatrixSolvers;
using Shared.ProcessFlowDiagram;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Streams;

namespace Shared.UnitOperations.Basiss
{

    // ───────────────────────────────────────────────────────────────
    // 🔹 CLASE BASE: EquipmentFacade2 (con método virtual para nuevo solver)
    // ───────────────────────────────────────────────────────────────
    //public abstract class EquipmentFacade2 : IEquipmentFacade2  // ← Sin ISolverEquationsProvider
    //{
    //    public Action? OnExecuteSolver { get; set; }
    //    public Guid Id { get; set; } = Guid.NewGuid();
    //    public string Name { get; set; } = string.Empty;

    //    public abstract string StatusText { get; }
    //    public abstract string StatusColor { get; }
    //    public abstract List<ToolTipLegend> GetToolTipLegend();

    //    public abstract void AttachConnection(string portName, IStreamFacade2 connectedFacade);
    //    public abstract void DetachConnection(string portName);

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 MÉTODOS PARA SOLVER VIEJO (se mantienen)
    //    // ═══════════════════════════════════════════════════════════
    //    public virtual EquationSystem GetEquationSystem() => new EquationSystem();
    //    public virtual EquationSystem GetEquationConcentration() => new EquationSystem();
    //    public virtual EquationSystem GetEquationPressure() => new EquationSystem();


    //    protected void ExecuteSolver()
    //    {
    //        OnExecuteSolver?.Invoke();
    //    }
    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 HELPER: Limpieza de valores propagados
    //    // ═══════════════════════════════════════════════════════════
    //    protected void ClearPropagatedValues(IStreamFacade2 stream)
    //    {
    //        if (stream == null) return;

    //        // Solo limpiar si fue definido por EquipmentSolver (respetar UI/Stream)
    //        if (stream.MassFlow?.IsDefinedByEquipmentSolver == true)
    //            stream.MassFlow.ClearFromEquipmentSolver();
    //        if (stream.MassEnthalpy?.IsDefinedByEquipmentSolver == true)
    //            stream.MassEnthalpy.ClearFromEquipmentSolver();
    //        if (stream.Pressure?.IsDefinedByEquipmentSolver == true)
    //            stream.Pressure.ClearFromEquipmentSolver();
    //        if (stream.StreamComposition?.IsDefinedByEquipmentSolver == true)
    //            stream.StreamComposition.ClearFromEquipmentSolver();
    //    }
    //}

    //public abstract class EquipmentFacade : IEquipmentFacade
    //{
    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 IDENTIDAD
    //    // ═══════════════════════════════════════════════════════════
    //    public Guid Id { get; set; } = Guid.NewGuid();
    //    public string Name { get; set; } = string.Empty;

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 EVENTOS
    //    // ═══════════════════════════════════════════════════════════
    //    public Action? OnExecuteSolver { get; set; }

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 UI/STATUS (abstracciones)
    //    // ═══════════════════════════════════════════════════════════
    //    public abstract string StatusText { get; }
    //    public abstract string StatusColor { get; }
    //    public abstract List<ToolTipLegend> GetToolTipLegend();

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 CONEXIONES (abstracciones multi-puerto)
    //    // ═══════════════════════════════════════════════════════════
    //    public abstract void AttachConnection(string portName, IStreamFacade connectedFacade);
    //    public abstract void DetachConnection(string portName);

    //    /// <summary>
    //    /// Por defecto: sin puertos. Cada equipo sobrescribe para definir sus puertos.
    //    /// </summary>
    //    public virtual IEnumerable<string> GetPortNames() => Enumerable.Empty<string>();

    //    /// <summary>
    //    /// Por defecto: null. Cada equipo sobrescribe para retornar streams por puerto.
    //    /// </summary>
    //    public virtual IStreamFacade? GetConnectedStream(string portName) => null;

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 VARIABLES CONTROLADAS (OCP: virtual para extender)
    //    // ═══════════════════════════════════════════════════════════
    //    /// <summary>
    //    /// Por defecto: sin variables controladas. Cada equipo sobrescribe según necesite.
    //    /// </summary>
    //    public virtual IEnumerable<IVariable> GetControlledVariables() => Enumerable.Empty<IVariable>();

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 ECUACIONES PARA SOLVER REACTIVO (OCP: virtual para extender)
    //    // ═══════════════════════════════════════════════════════════
    //    public virtual List<GlobalEquation> GetReactiveEquations(List<IVariable> allVariables)
    //    {
    //        return new List<GlobalEquation>();
    //    }

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 PROTEGIDO: Ejecutar solver
    //    // ═══════════════════════════════════════════════════════════
    //    protected void ExecuteSolver() => OnExecuteSolver?.Invoke();

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 PROTEGIDO: Limpieza condicional
    //    // ═══════════════════════════════════════════════════════════
    //    protected void ClearCalculatedValues(IStreamFacade stream)
    //    {
    //        if (stream == null) return;
    //        ClearIfNotUI(stream.Pressure);
    //        ClearIfNotUI(stream.Temperature);
    //        ClearIfNotUI(stream.MassFlow);
    //        ClearIfNotUI(stream.MassEnthalpy);
    //        ClearIfNotUI(stream.StreamComposition);
    //    }

    //    private void ClearIfNotUI(IVariable variable)
    //    {
    //        if (variable != null && !variable.IsDefinedByUI)
    //            variable.ClearFromStream();
    //    }

    //    // ═══════════════════════════════════════════════════════════
    //    // 🔹 PROTEGIDO: Helper para obtener valor de variable
    //    // ═══════════════════════════════════════════════════════════
    //    protected double GetVarValue(List<IVariable> vars, IVariable target)
    //    {
    //        if (target == null) return 0;
    //        var found = vars?.FirstOrDefault(v => v == target)
    //                 ?? vars?.FirstOrDefault(v => v?.Index == target.Index);
    //        return found?.GetEffectiveSolverValue() ?? target.GetSolverValue();
    //    }
    //}



}
