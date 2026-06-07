using Shared.SolverQwen.Variables;
using UnitSystem;

namespace Shared.SolverQwen.Equipments
{
    //public class PumpEquipment : EquipmentBase
    //{
    //    public ProcessVariable<PressureDrop> DeltaP { get; }

    //    public PumpEquipment(string name) : base(name)
    //    {
    //        DeltaP = new ProcessVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
    //    }

    //    protected override IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies()
    //    {
    //        // ✅ FASE 1: SOLO Pressure (per tu tabla)
    //        yield return new PumpPressurePhase1Strategy(this);
    //    }

    //    protected override IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies()
    //    {
    //        // ✅ FASE 2: Pressure, Concentration, Enthalpy, MassBalance (per tu tabla)
    //        yield return new PumpPressurePhase2Strategy(this);
    //        yield return new PumpConcentrationPhase2Strategy(this);
    //        yield return new PumpEnthalpyPhase2Strategy(this);
    //        yield return new PumpMassBalancePhase2Strategy(this);
    //    }
    //    protected override IEnumerable<ISolverPhaseStrategy> CreatePhase3Strategies()
    //    {
    //        yield return new GlobalMassBalancePhase3Strategy(this);
    //    }
    //}



    //// ============================================================================
    //// BOMBA - FASE 1: PRESIÓN (P_out = P_in + ΔP) - LOCAL
    //// ============================================================================
    //public class PumpPressurePhase1Strategy : ISolverPhaseStrategy
    //{
    //    public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
    //    private readonly PumpEquipment _equipment;
    //    public StrategyType Type => StrategyType.Pressure;
    //    public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;

    //    public PumpPressurePhase1Strategy(PumpEquipment equipment)
    //    {
    //        _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
    //    }

    //    public double[] GetResiduals()
    //    {
    //        if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
    //        double pIn = _equipment.Inlets.First().Pressure.GetSolverValue();
    //        double pOut = _equipment.Outlets.First().Pressure.GetSolverValue();
    //        double deltaP = _equipment.DeltaP.GetSolverValue();
    //        return new double[] { pIn + deltaP - pOut };
    //    }

    //    public IEnumerable<IProcessVariable> GetCouplingVariables()
    //    {
    //        yield return _equipment.DeltaP;
    //        if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().Pressure;
    //        if (_equipment.Outlets.Any()) yield return _equipment.Outlets.First().Pressure;
    //    }
    //}

    //// ============================================================================
    //// BOMBA - FASE 2: PRESIÓN GLOBAL (P_out = P_in + ΔP) - RED ACOPLADA
    //// ============================================================================
    //public class PumpPressurePhase2Strategy : ISolverPhaseStrategy
    //{
    //    public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
    //    private readonly PumpEquipment _equipment;
    //    public StrategyType Type => StrategyType.Pressure;
    //    public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

    //    public PumpPressurePhase2Strategy(PumpEquipment equipment)
    //    {
    //        _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
    //    }

    //    public double[] GetResiduals()
    //    {
    //        if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
    //        double pIn = _equipment.Inlets.First().Pressure.GetSolverValue();
    //        double pOut = _equipment.Outlets.First().Pressure.GetSolverValue();
    //        double deltaP = _equipment.DeltaP.GetSolverValue();
    //        return new double[] { pIn + deltaP - pOut };
    //    }

    //    public IEnumerable<IProcessVariable> GetCouplingVariables()
    //    {
    //        yield return _equipment.DeltaP;
    //        if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().Pressure;
    //        if (_equipment.Outlets.Any()) yield return _equipment.Outlets.First().Pressure;
    //    }
    //}

    //// ============================================================================
    //// BOMBA - FASE 2: CONCENTRACIÓN (x_out = x_in) - SUBSISTEMA
    //// Procedence=Phase1_LocalPropagation para ser descubierta en Fase 2, pero NO ejecutada en Fase 1
    //// ============================================================================
    //public class PumpConcentrationPhase2Strategy : ISolverPhaseStrategy
    //{
    //    public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
    //    private readonly PumpEquipment _equipment;
    //    public StrategyType Type => StrategyType.Concentration;
    //    public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

    //    public PumpConcentrationPhase2Strategy(PumpEquipment equipment)
    //    {
    //        _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
    //    }

    //    public double[] GetResiduals()
    //    {
    //        if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
    //        var inlet = _equipment.Inlets.First();
    //        var outlet = _equipment.Outlets.First();
    //        var residuals = new List<double>();

    //        var inComps = inlet.Composition.Components;
    //        var outComps = outlet.Composition.Components;
    //        for (int i = 0; i < inComps.Count && i < outComps.Count; i++)
    //        {
    //            residuals.Add(inComps[i].MassFraction.GetSolverValue() - outComps[i].MassFraction.GetSolverValue());
    //        }
    //        return residuals.ToArray();
    //    }

    //    public IEnumerable<IProcessVariable> GetCouplingVariables()
    //    {
    //        var inlet = _equipment.Inlets.FirstOrDefault();
    //        var outlet = _equipment.Outlets.FirstOrDefault();
    //        if (inlet != null) foreach (var c in inlet.Composition.Components) yield return c.MassFraction;
    //        if (outlet != null) foreach (var c in outlet.Composition.Components) yield return c.MassFraction;
    //    }
    //}

    //// ============================================================================
    //// BOMBA - FASE 2: ENTALPÍA (h_out = h_in) - SUBSISTEMA
    //// ============================================================================
    //public class PumpEnthalpyPhase2Strategy : ISolverPhaseStrategy
    //{
    //    public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
    //    private readonly PumpEquipment _equipment;
    //    public StrategyType Type => StrategyType.Enthalpy;
    //    public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

    //    public PumpEnthalpyPhase2Strategy(PumpEquipment equipment)
    //    {
    //        _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
    //    }

    //    public double[] GetResiduals()
    //    {
    //        if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
    //        double hIn = _equipment.Inlets.First().MassEnthalpy.GetSolverValue();
    //        double hOut = _equipment.Outlets.First().MassEnthalpy.GetSolverValue();
    //        return new double[] { hIn - hOut };
    //    }

    //    public IEnumerable<IProcessVariable> GetCouplingVariables()
    //    {
    //        if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().MassEnthalpy;
    //        if (_equipment.Outlets.Any()) yield return _equipment.Outlets.First().MassEnthalpy;
    //    }
    //}

    //// ============================================================================
    //// BOMBA - FASE 2: BALANCE DE MASA (ṁ_out = ṁ_in) - SUBSISTEMA
    //// ============================================================================
    //public class PumpMassBalancePhase2Strategy : ISolverPhaseStrategy
    //{
    //    public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
    //    private readonly PumpEquipment _equipment;
    //    public StrategyType Type => StrategyType.MassBalance;
    //    public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

    //    public PumpMassBalancePhase2Strategy(PumpEquipment equipment)
    //    {
    //        _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
    //    }

    //    public double[] GetResiduals()
    //    {
    //        if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
    //        double mIn = _equipment.Inlets.First().MassFlow.GetSolverValue();
    //        double mOut = _equipment.Outlets.First().MassFlow.GetSolverValue();
    //        return new double[] { mIn - mOut };
    //    }

    //    public IEnumerable<IProcessVariable> GetCouplingVariables()
    //    {
    //        if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().MassFlow;
    //        if (_equipment.Outlets.Any()) yield return _equipment.Outlets.First().MassFlow;
    //    }
    //}

    // ============================================================================
    // VÁLVULA - FASE 1: PRESIÓN (P_out = P_in - ΔP) - LOCAL
    // ΔP puede ser: fijo, calculado desde Cv y flujo, o definido por usuario
    // ============================================================================

}

