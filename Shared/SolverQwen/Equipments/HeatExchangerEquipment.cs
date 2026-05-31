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
        public ProcessVariable<EnergyFlow> Q { get; }

        public HeatExchangerEquipment(string name) : base(name)
        {
            // Inicializar variables
            DeltaPHot = new ProcessVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            DeltaPCold = new ProcessVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            Q = new ProcessVariable<EnergyFlow>(new EnergyFlow(0, EnergyFlowUnits.J_sg), EnergyFlowUnits.Kcal_hr, 3000);
        }

        // ── CONEXIONES ──
        public void ConnectHotSide(FacadeStream inlet, FacadeStream outlet)
        {
            HotInlet = inlet;
            HotOutlet = outlet;
            base.AddInlet(inlet);   // Alimenta al Orquestador (BFS)
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
            // ✅ Presión, Concentración y Masa
            yield return new HexHotPressurePhase2Strategy(this);
            yield return new HexColdPressurePhase2Strategy(this);
            yield return new HexHotConcentrationPhase2Strategy(this);
            yield return new HexColdConcentrationPhase2Strategy(this);
            yield return new HexHotMassPhase2Strategy(this);
            yield return new HexColdMassPhase2Strategy(this);

            // ✅ TU IDEA: Balances de Energía Aislados (Calculan QHot o QCold si el ramal está definido)
            yield return new HexHotEnergyPhase2Strategy(this);
            yield return new HexColdEnergyPhase2Strategy(this);
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase3Strategies()  // ← Cambiar a IEnumerable
        {
            // 🔹 Dos estrategias independientes, una por lado
            // Cada una se activa solo si su lado tiene specs suficientes
            yield return new HexHotMassEnergyPhase3Strategy(this);
            yield return new HexColdMassEnergyPhase3Strategy(this);

            // 🔹 NO hay estrategia de "acople": Q es la misma variable compartida
            // Si ambos lados convergen, Q tendrá el mismo valor por definición
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
    // FASE 3: ESTRATEGIA ACOPLADA (Masa y Energía)
    // ============================================================================

    // ============================================================================
    // FASE 2: ENERGÍA AISLADA (Tu idea brillante para la UI)
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
            double q = _eq.Q.GetSolverValue();

            // Asumimos Q positivo cuando el fluido se enfría
            return new double[] { (mIn * hIn) - (mOut * hOut) - q };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            // Solo exponemos Q y las Entalpías. 
            // ¡La Masa la borramos de aquí para que el solver no intente modificarla!
            yield return _eq.Q;

            if (_eq.HotInlet != null) { yield return _eq.HotInlet.MassEnthalpy; }
            if (_eq.HotOutlet != null) { yield return _eq.HotOutlet.MassEnthalpy; }
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
            double q = _eq.Q.GetSolverValue();

            // Asumimos Q positivo cuando el fluido se calienta
            return new double[] { (mIn * hIn) - (mOut * hOut) + q };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.Q;
            if (_eq.ColdInlet != null) {  yield return _eq.ColdInlet.MassEnthalpy; }
            if (_eq.ColdOutlet != null) {  yield return _eq.ColdOutlet.MassEnthalpy; }
        }
    }

    // ============================================================================
    // FASE 3: ESTRATEGIA ACOPLADA (El pegamento termodinámico)
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
            // 🔹 Validar que el lado caliente está completo y tiene specs mínimos
            if (_eq.HotInlet == null || _eq.HotOutlet == null) return Array.Empty<double>();
          

            double mIn = _eq.HotInlet.MassFlow.GetSolverValue();
            double mOut = _eq.HotOutlet.MassFlow.GetSolverValue();
            double hIn = _eq.HotInlet.MassEnthalpy.GetSolverValue();
            double hOut = _eq.HotOutlet.MassEnthalpy.GetSolverValue();
            double q = _eq.Q.GetSolverValue();  // Variable compartida

            // Balance de masa y energía (Hot pierde calor: -Q)
            return new double[]
            {
                mIn - mOut,                      // Eq 1: Conservación de masa
                (mIn * hIn) - (mOut * hOut) - q  // Eq 2: Conservación de energía
            };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            // 🔹 Solo exponer variables si el lado está activo
           if(_eq.HotInlet == null || _eq.HotOutlet == null) yield break;

            yield return _eq.Q;  // Variable de acople compartida
            yield return _eq.HotInlet.MassFlow;
            yield return _eq.HotOutlet.MassFlow;
            yield return _eq.HotInlet.MassEnthalpy;
            yield return _eq.HotOutlet.MassEnthalpy;
        }

        /// <summary>
        /// Determina si el lado caliente tiene specs suficientes para resolver.
        /// Necesitamos: flujo/entalpía de entrada (Phase 2) + al menos 1 spec de salida o Q.
        /// </summary>
       
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
            double q = _eq.Q.GetSolverValue();  // Misma variable compartida

            // Balance de masa y energía (Cold gana calor: +Q)
            return new double[]
            {
                mIn - mOut,                      // Eq 1: Conservación de masa
                (mIn * hIn) - (mOut * hOut) + q  // Eq 2: Conservación de energía
            };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
           if(_eq.ColdInlet == null || _eq.ColdOutlet == null) yield break;

            yield return _eq.Q;  // Misma variable compartida
            yield return _eq.ColdInlet.MassFlow;
            yield return _eq.ColdOutlet.MassFlow;
            yield return _eq.ColdInlet.MassEnthalpy;
            yield return _eq.ColdOutlet.MassEnthalpy;
        }

       
    }

}
