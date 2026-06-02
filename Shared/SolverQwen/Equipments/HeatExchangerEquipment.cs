using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.SolverQwen.Equipments
{
    public class HeatExchangerEquipment : EquipmentBase
    {
        // ── PUERTOS SEMÁNTICOS (Física Aislada) ──
        public FacadeStream? HotInlet { get; private set; }
        public FacadeStream? HotOutlet { get; private set; }
        public FacadeStream? ColdInlet { get; private set; }
        public FacadeStream? ColdOutlet { get; private set; }

        // ── VARIABLES DE EQUIPO ──
        public ProcessVariable<PressureDrop> DeltaPHot { get; }
        public ProcessVariable<PressureDrop> DeltaPCold { get; }

        // ✅ LA MAGIA: Calores separados para permitir resolución aislada reactiva
        public ProcessVariable<EnergyFlow> QHot { get; }
        public ProcessVariable<EnergyFlow> QCold { get; }

        public HeatExchangerEquipment(string name) : base(name)
        {
            DeltaPHot = new ProcessVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            DeltaPCold = new ProcessVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);

            QHot = new ProcessVariable<EnergyFlow>(new EnergyFlow(0, EnergyFlowUnits.J_sg), EnergyFlowUnits.Kcal_hr, 3000);
            QCold = new ProcessVariable<EnergyFlow>(new EnergyFlow(0, EnergyFlowUnits.J_sg), EnergyFlowUnits.Kcal_hr, 3000);
        }

        // ── CONEXIONES ──
        public void ConnectHotSide(FacadeStream inlet, FacadeStream outlet)
        {
            HotInlet = inlet;
            HotOutlet = outlet;
            base.AddInlet(inlet);
            base.AddOutlet(outlet);
        }

        public void ConnectColdSide(FacadeStream inlet, FacadeStream outlet)
        {
            ColdInlet = inlet;
            ColdOutlet = outlet;
            base.AddInlet(inlet);
            base.AddOutlet(outlet);
        }

        // ── INYECCIÓN DE ESTRATEGIAS ──
        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies()
        {
            yield return new HexHotPressurePhase1Strategy(this);
            yield return new HexColdPressurePhase1Strategy(this);
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies()
        {
            // Presión, Concentración y Masa
            yield return new HexHotPressurePhase2Strategy(this);
            yield return new HexColdPressurePhase2Strategy(this);
            yield return new HexHotConcentrationPhase2Strategy(this);
            yield return new HexColdConcentrationPhase2Strategy(this);
            yield return new HexHotMassPhase2Strategy(this);
            yield return new HexColdMassPhase2Strategy(this);

            // ✅ Balances de Energía Aislados
            yield return new HexHotEnergyPhase2Strategy(this);
            yield return new HexColdEnergyPhase2Strategy(this);

            // ✅ EL PUENTE TERMODINÁMICO: Acopla los dos ramales
            yield return new HexHeatCouplingPhase2Strategy(this);
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase3Strategies()
        {
            yield return new HexHotMassEnergyPhase3Strategy(this);
            yield return new HexColdMassEnergyPhase3Strategy(this);
            yield return new HexHeatCouplingPhase3Strategy(this); // Aseguramos que el puente exista si entra a Fase 3
        }
    }

    // ============================================================================
    // INTERCAMBIADOR - FASE 2: MASA (Propagación Rápida)
    // ============================================================================

    public class HexHotMassPhase2Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.MassBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - {Type} Hot - {Procedence}";

        public HexHotMassPhase2Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.HotInlet == null || _eq.HotOutlet == null) return new double[0];
            return new double[] { _eq.HotInlet.MassFlow.GetSolverValue() - _eq.HotOutlet.MassFlow.GetSolverValue() };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_eq.HotInlet != null) yield return _eq.HotInlet.MassFlow;
            if (_eq.HotOutlet != null) yield return _eq.HotOutlet.MassFlow;
        }
    }

    public class HexColdMassPhase2Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.MassBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - {Type} Cold - {Procedence}";

        public HexColdMassPhase2Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.ColdInlet == null || _eq.ColdOutlet == null) return new double[0];
            return new double[] { _eq.ColdInlet.MassFlow.GetSolverValue() - _eq.ColdOutlet.MassFlow.GetSolverValue() };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_eq.ColdInlet != null) yield return _eq.ColdInlet.MassFlow;
            if (_eq.ColdOutlet != null) yield return _eq.ColdOutlet.MassFlow;
        }
    }

    public class HexHotPressurePhase1Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;
        public string Name => $"{_eq.Name} - {Type} Hot - {Procedence}";

        public HexHotPressurePhase1Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.HotInlet == null || _eq.HotOutlet == null) return new double[0];
            return new double[] { _eq.HotInlet.Pressure.GetSolverValue() - _eq.DeltaPHot.GetSolverValue() - _eq.HotOutlet.Pressure.GetSolverValue() };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.DeltaPHot;
            if (_eq.HotInlet != null) yield return _eq.HotInlet.Pressure;
            if (_eq.HotOutlet != null) yield return _eq.HotOutlet.Pressure;
        }
    }

    public class HexColdPressurePhase1Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;
        public string Name => $"{_eq.Name} - {Type} Cold - {Procedence}";

        public HexColdPressurePhase1Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.ColdInlet == null || _eq.ColdOutlet == null) return new double[0];
            return new double[] { _eq.ColdInlet.Pressure.GetSolverValue() - _eq.DeltaPCold.GetSolverValue() - _eq.ColdOutlet.Pressure.GetSolverValue() };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.DeltaPCold;
            if (_eq.ColdInlet != null) yield return _eq.ColdInlet.Pressure;
            if (_eq.ColdOutlet != null) yield return _eq.ColdOutlet.Pressure;
        }
    }

    // ============================================================================
    // INTERCAMBIADOR - FASE 2: PRESIÓN (Red Acoplada Independiente)
    // ============================================================================

    public class HexHotPressurePhase2Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - {Type} Hot - {Procedence}";

        public HexHotPressurePhase2Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.HotInlet == null || _eq.HotOutlet == null) return new double[0];
            return new double[] { _eq.HotInlet.Pressure.GetSolverValue() - _eq.DeltaPHot.GetSolverValue() - _eq.HotOutlet.Pressure.GetSolverValue() };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.DeltaPHot;
            if (_eq.HotInlet != null) yield return _eq.HotInlet.Pressure;
            if (_eq.HotOutlet != null) yield return _eq.HotOutlet.Pressure;
        }
    }

    public class HexColdPressurePhase2Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - {Type} Cold - {Procedence}";

        public HexColdPressurePhase2Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.ColdInlet == null || _eq.ColdOutlet == null) return new double[0];
            return new double[] { _eq.ColdInlet.Pressure.GetSolverValue() - _eq.DeltaPCold.GetSolverValue() - _eq.ColdOutlet.Pressure.GetSolverValue() };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.DeltaPCold;
            if (_eq.ColdInlet != null) yield return _eq.ColdInlet.Pressure;
            if (_eq.ColdOutlet != null) yield return _eq.ColdOutlet.Pressure;
        }
    }

    // ============================================================================
    // INTERCAMBIADOR - FASE 2: CONCENTRACIÓN (Red Acoplada Independiente)
    // ============================================================================

    public class HexHotConcentrationPhase2Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.Concentration;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - {Type} Hot - {Procedence}";

        public HexHotConcentrationPhase2Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.HotInlet == null || _eq.HotOutlet == null) return new double[0];
            var res = new List<double>();
            for (int i = 0; i < _eq.HotInlet.Composition.Components.Count; i++)
                res.Add(_eq.HotInlet.Composition.Components[i].MassFraction.GetSolverValue() - _eq.HotOutlet.Composition.Components[i].MassFraction.GetSolverValue());
            return res.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_eq.HotInlet != null) foreach (var c in _eq.HotInlet.Composition.Components) yield return c.MassFraction;
            if (_eq.HotOutlet != null) foreach (var c in _eq.HotOutlet.Composition.Components) yield return c.MassFraction;
        }
    }

    public class HexColdConcentrationPhase2Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.Concentration;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - {Type} Cold - {Procedence}";

        public HexColdConcentrationPhase2Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.ColdInlet == null || _eq.ColdOutlet == null) return new double[0];
            var res = new List<double>();
            for (int i = 0; i < _eq.ColdInlet.Composition.Components.Count; i++)
                res.Add(_eq.ColdInlet.Composition.Components[i].MassFraction.GetSolverValue() - _eq.ColdOutlet.Composition.Components[i].MassFraction.GetSolverValue());
            return res.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_eq.ColdInlet != null) foreach (var c in _eq.ColdInlet.Composition.Components) yield return c.MassFraction;
            if (_eq.ColdOutlet != null) foreach (var c in _eq.ColdOutlet.Composition.Components) yield return c.MassFraction;
        }
    }


    // ============================================================================
    // ✅ EL PUENTE: ESTRATEGIAS DE ACOPLE TÉRMICO (Fase 2 y 3)
    // ============================================================================
    public class HexHeatCouplingPhase2Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.Enthalpy;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - Phase2_HeatCoupling";

        public HexHeatCouplingPhase2Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            // El puente asume conservación de energía perfecta: Q_ganado + Q_perdido = 0
            return new double[] { _eq.QHot.GetSolverValue() + _eq.QCold.GetSolverValue() };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.QHot;
            yield return _eq.QCold;
        }
    }

    public class HexHeatCouplingPhase3Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.MassEnergyBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;
        public string Name => $"{_eq.Name} - Phase3_HeatCoupling";

        public HexHeatCouplingPhase3Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            return new double[] { _eq.QHot.GetSolverValue() + _eq.QCold.GetSolverValue() };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.QHot;
            yield return _eq.QCold;
        }
    }

    // ============================================================================
    // FASE 2: ENERGÍA AISLADA 
    // ============================================================================
    public class HexHotEnergyPhase2Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.Enthalpy;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - Phase2_Energy Hot";

        public HexHotEnergyPhase2Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.HotInlet == null || _eq.HotOutlet == null) return new double[0];

            double mIn = _eq.HotInlet.MassFlow.GetSolverValue();
            double mOut = _eq.HotOutlet.MassFlow.GetSolverValue();
            double hIn = _eq.HotInlet.MassEnthalpy.GetSolverValue();
            double hOut = _eq.HotOutlet.MassEnthalpy.GetSolverValue();
            double qHot = _eq.QHot.GetSolverValue();

            // 1ra Ley: (m*h)_in - (m*h)_out + Q = 0
            double residual = (mIn * hIn) - (mOut * hOut) + qHot;

#if DEBUG
            if (double.IsNaN(residual) || double.IsInfinity(residual))
                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual es NaN/Inf! mIn:{mIn:F2}, mOut:{mOut:F2}, hIn:{hIn:F2}, hOut:{hOut:F2}, QHot:{qHot:F2}");
#endif
            return new double[] { residual };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.QHot;
            if (_eq.HotInlet != null) yield return _eq.HotInlet.MassEnthalpy;
            if (_eq.HotOutlet != null) yield return _eq.HotOutlet.MassEnthalpy;
        }
    }

    public class HexColdEnergyPhase2Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.Enthalpy;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - Phase2_Energy Cold";

        public HexColdEnergyPhase2Strategy(HeatExchangerEquipment eq) { _eq = eq ?? throw new ArgumentNullException(nameof(eq)); }

        public double[] GetResiduals()
        {
            if (_eq.ColdInlet == null || _eq.ColdOutlet == null) return new double[0];

            double mIn = _eq.ColdInlet.MassFlow.GetSolverValue();
            double mOut = _eq.ColdOutlet.MassFlow.GetSolverValue();
            double hIn = _eq.ColdInlet.MassEnthalpy.GetSolverValue();
            double hOut = _eq.ColdOutlet.MassEnthalpy.GetSolverValue();
            double qCold = _eq.QCold.GetSolverValue();

            // 1ra Ley: (m*h)_in - (m*h)_out + Q = 0
            double residual = (mIn * hIn) - (mOut * hOut) + qCold;

#if DEBUG
            if (double.IsNaN(residual) || double.IsInfinity(residual))
                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual es NaN/Inf! mIn:{mIn:F2}, mOut:{mOut:F2}, hIn:{hIn:F2}, hOut:{hOut:F2}, QCold:{qCold:F2}");
#endif
            return new double[] { residual };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.QCold;
            if (_eq.ColdInlet != null) yield return _eq.ColdInlet.MassEnthalpy;
            if (_eq.ColdOutlet != null) yield return _eq.ColdOutlet.MassEnthalpy;
        }
    }

    // ============================================================================
    // FASE 3: ESTRATEGIA ACOPLADA (Masa y Energía independientes por lado)
    // ============================================================================
    public class HexHotMassEnergyPhase3Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.MassEnergyBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;
        public string Name => $"{_eq.Name} - Phase3_HotSide";

        public HexHotMassEnergyPhase3Strategy(HeatExchangerEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (_eq.HotInlet == null || _eq.HotOutlet == null) return Array.Empty<double>();

            double mIn = _eq.HotInlet.MassFlow.GetSolverValue();
            double mOut = _eq.HotOutlet.MassFlow.GetSolverValue();
            double hIn = _eq.HotInlet.MassEnthalpy.GetSolverValue();
            double hOut = _eq.HotOutlet.MassEnthalpy.GetSolverValue();
            double qHot = _eq.QHot.GetSolverValue();

            double resMass = mIn - mOut;
            double resEnergy = (mIn * hIn) - (mOut * hOut) + qHot;

#if DEBUG
            if (double.IsNaN(resEnergy) || double.IsInfinity(resEnergy))
                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Energía es NaN/Inf! mIn:{mIn:F2}, mOut:{mOut:F2}, hIn:{hIn:F2}, hOut:{hOut:F2}, QHot:{qHot:F2}");
#endif

            return new double[] { resMass, resEnergy };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_eq.HotInlet == null || _eq.HotOutlet == null) yield break;

            yield return _eq.QHot;
            yield return _eq.HotInlet.MassFlow;
            yield return _eq.HotOutlet.MassFlow;
            yield return _eq.HotInlet.MassEnthalpy;
            yield return _eq.HotOutlet.MassEnthalpy;
        }
    }

    public class HexColdMassEnergyPhase3Strategy : ISolverPhaseStrategy
    {
        private readonly HeatExchangerEquipment _eq;
        public StrategyType Type => StrategyType.MassEnergyBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;
        public string Name => $"{_eq.Name} - Phase3_ColdSide";

        public HexColdMassEnergyPhase3Strategy(HeatExchangerEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (_eq.ColdInlet == null || _eq.ColdOutlet == null) return Array.Empty<double>();

            double mIn = _eq.ColdInlet.MassFlow.GetSolverValue();
            double mOut = _eq.ColdOutlet.MassFlow.GetSolverValue();
            double hIn = _eq.ColdInlet.MassEnthalpy.GetSolverValue();
            double hOut = _eq.ColdOutlet.MassEnthalpy.GetSolverValue();
            double qCold = _eq.QCold.GetSolverValue();

            double resMass = mIn - mOut;
            double resEnergy = (mIn * hIn) - (mOut * hOut) + qCold;

#if DEBUG
            if (double.IsNaN(resEnergy) || double.IsInfinity(resEnergy))
                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Energía es NaN/Inf! mIn:{mIn:F2}, mOut:{mOut:F2}, hIn:{hIn:F2}, hOut:{hOut:F2}, QCold:{qCold:F2}");
#endif

            return new double[] { resMass, resEnergy };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_eq.ColdInlet == null || _eq.ColdOutlet == null) yield break;

            yield return _eq.QCold;
            yield return _eq.ColdInlet.MassFlow;
            yield return _eq.ColdOutlet.MassFlow;
            yield return _eq.ColdInlet.MassEnthalpy;
            yield return _eq.ColdOutlet.MassEnthalpy;
        }
    }

}
