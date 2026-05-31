using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.SolverQwen.Equipments
{
    // ============================================================================
    // EQUIPO: COLUMNA DE DESTILACIÓN (Física Pura - Con Entradas y Salidas Laterales)
    // ============================================================================
    public class DistillationColumnEquipment : EquipmentBase
    {
        // ── PUERTOS SEMÁNTICOS ÚNICOS ──
        public FacadeStream? RefluxInlet { get; private set; }       // Líquido del condensador
        public FacadeStream? BoilupInlet { get; private set; }       // Vapor del rehervidor
        public FacadeStream? TopVaporOutlet { get; private set; }    // Vapor hacia condensador
        public FacadeStream? BottomLiquidOutlet { get; private set; } // Líquido hacia rehervidor

        // ── LISTADOS SEMÁNTICOS (Para múltiples alimentaciones y extracciones) ──
        public List<FacadeStream> Feeds { get; } = new();
        public List<FacadeStream> SideDraws { get; } = new();

        // ── VARIABLES DE EQUIPO ──
        public ProcessVariable<PressureDrop> DeltaP { get; }

        public DistillationColumnEquipment(string name) : base(name)
        {
            DeltaP = new ProcessVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
        }

        // ── MÉTODOS DE CONEXIÓN DE ENTRADAS (Alimentan base.Inlets) ──
        public void ConnectReflux(FacadeStream inlet)
        {
            RefluxInlet = inlet;
            base.AddInlet(inlet);
        }

        public void ConnectBoilup(FacadeStream inlet)
        {
            BoilupInlet = inlet;
            base.AddInlet(inlet);
        }

        public void AddFeed(FacadeStream inlet)
        {
            Feeds.Add(inlet);
            base.AddInlet(inlet);
        }

        // ── MÉTODOS DE CONEXIÓN DE SALIDAS (Alimentan base.Outlets) ──
        public void ConnectTopVapor(FacadeStream outlet)
        {
            TopVaporOutlet = outlet;
            base.AddOutlet(outlet);
        }

        public void ConnectBottomLiquid(FacadeStream outlet)
        {
            BottomLiquidOutlet = outlet;
            base.AddOutlet(outlet);
        }

        public void AddSideDraw(FacadeStream outlet)
        {
            SideDraws.Add(outlet);
            base.AddOutlet(outlet);
        }


        // ── INYECCIÓN DE ESTRATEGIAS ──
        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies()
        {
            yield return new ColumnPressurePhase1Strategy(this);
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies()
        {
            yield return new ColumnPressurePhase2Strategy(this);
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase3Strategies()
        {
            // El balance macroscópico funciona perfecto porque usa base.Inlets y base.Outlets
            yield return new ColumnMassEnergyPhase3Strategy(this);
        }
    }

    // ============================================================================
    // COLUMNA - FASE 1: PRESIÓN (Propagación Local)
    // ============================================================================
    public class ColumnPressurePhase1Strategy : ISolverPhaseStrategy
    {
        private readonly DistillationColumnEquipment _eq;

        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;
        public string Name => $"{_eq.Name} - {Type} - {Procedence}";

        public ColumnPressurePhase1Strategy(DistillationColumnEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            // Las anclas hidráulicas principales deben estar conectadas
            if (_eq.BottomLiquidOutlet == null || _eq.TopVaporOutlet == null)
                return new double[0];

            var residuals = new List<double>();

            double pBottom = _eq.BottomLiquidOutlet.Pressure.GetSolverValue();
            double pTop = _eq.TopVaporOutlet.Pressure.GetSolverValue();
            double deltaP = _eq.DeltaP.GetSolverValue();

            // 1. Perfil base de la columna (Caída de presión en los platos)
            residuals.Add(pBottom - deltaP - pTop);

            // 2. Zona de Tope: El Reflujo se iguala a la presión de vapor del tope
            if (_eq.RefluxInlet != null)
            {
                residuals.Add(_eq.RefluxInlet.Pressure.GetSolverValue() - pTop);
            }

            // 3. Zona de Fondo: El Boilup se iguala a la presión del fondo
            if (_eq.BoilupInlet != null)
            {
                residuals.Add(_eq.BoilupInlet.Pressure.GetSolverValue() - pBottom);
            }

            // 4. Alimentaciones múltiples: Deben vencer/igualar la presión del fondo para entrar
            foreach (var feed in _eq.Feeds)
            {
                residuals.Add(feed.Pressure.GetSolverValue() - pBottom);
            }

            // 5. Extracciones laterales: Salen a la presión hidrostática local del fondo (simplificación macro)
            foreach (var side in _eq.SideDraws)
            {
                residuals.Add(side.Pressure.GetSolverValue() - pBottom);
            }

            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.DeltaP;

            if (_eq.BottomLiquidOutlet != null) yield return _eq.BottomLiquidOutlet.Pressure;
            if (_eq.TopVaporOutlet != null) yield return _eq.TopVaporOutlet.Pressure;
            if (_eq.RefluxInlet != null) yield return _eq.RefluxInlet.Pressure;
            if (_eq.BoilupInlet != null) yield return _eq.BoilupInlet.Pressure;

            foreach (var feed in _eq.Feeds) yield return feed.Pressure;
            foreach (var side in _eq.SideDraws) yield return side.Pressure;
        }
    }

    // ============================================================================
    // COLUMNA - FASE 2: PRESIÓN (Red Acoplada)
    // ============================================================================
    public class ColumnPressurePhase2Strategy : ISolverPhaseStrategy
    {
        private readonly DistillationColumnEquipment _eq;

        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - {Type} - {Procedence}";

        public ColumnPressurePhase2Strategy(DistillationColumnEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            // Las anclas hidráulicas principales deben estar conectadas
            if (_eq.BottomLiquidOutlet == null || _eq.TopVaporOutlet == null)
                return new double[0];

            var residuals = new List<double>();

            double pBottom = _eq.BottomLiquidOutlet.Pressure.GetSolverValue();
            double pTop = _eq.TopVaporOutlet.Pressure.GetSolverValue();
            double deltaP = _eq.DeltaP.GetSolverValue();

            // 1. Perfil base de la columna (Caída de presión en los platos)
            residuals.Add(pBottom - deltaP - pTop);

            // 2. Zona de Tope: El Reflujo se iguala a la presión de vapor del tope
            if (_eq.RefluxInlet != null)
            {
                residuals.Add(_eq.RefluxInlet.Pressure.GetSolverValue() - pTop);
            }

            // 3. Zona de Fondo: El Boilup se iguala a la presión del fondo
            if (_eq.BoilupInlet != null)
            {
                residuals.Add(_eq.BoilupInlet.Pressure.GetSolverValue() - pBottom);
            }

            // 4. Alimentaciones múltiples: Deben vencer/igualar la presión del fondo para entrar
            foreach (var feed in _eq.Feeds)
            {
                residuals.Add(feed.Pressure.GetSolverValue() - pBottom);
            }

            // 5. Extracciones laterales: Salen a la presión hidrostática local del fondo (simplificación macro)
            foreach (var side in _eq.SideDraws)
            {
                residuals.Add(side.Pressure.GetSolverValue() - pBottom);
            }

            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            yield return _eq.DeltaP;

            if (_eq.BottomLiquidOutlet != null) yield return _eq.BottomLiquidOutlet.Pressure;
            if (_eq.TopVaporOutlet != null) yield return _eq.TopVaporOutlet.Pressure;
            if (_eq.RefluxInlet != null) yield return _eq.RefluxInlet.Pressure;
            if (_eq.BoilupInlet != null) yield return _eq.BoilupInlet.Pressure;

            foreach (var feed in _eq.Feeds) yield return feed.Pressure;
            foreach (var side in _eq.SideDraws) yield return side.Pressure;
        }
    }


    // ============================================================================
    // FASE 3: BALANCE MACROSCÓPICO (Agnóstico a la semántica)
    // ============================================================================
    public class ColumnMassEnergyPhase3Strategy : ISolverPhaseStrategy
    {
        private readonly DistillationColumnEquipment _eq;
        public StrategyType Type => StrategyType.MassEnergyBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;
        public string Name => $"{_eq.Name} - Phase3_PhysicalColumnBalance";

        public ColumnMassEnergyPhase3Strategy(DistillationColumnEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (!_eq.Inlets.Any() || !_eq.Outlets.Any()) return new double[0];

            var residuals = new List<double>();

            // 1. BALANCE DE MASA GLOBAL (Suma Alimentaciones + Reflujo + Boilup vs Tope + Fondo + Laterales)
            double mInSum = _eq.Inlets.Sum(s => s.MassFlow.GetSolverValue());
            double mOutSum = _eq.Outlets.Sum(s => s.MassFlow.GetSolverValue());
            residuals.Add(mInSum - mOutSum);

            // 2. BALANCE DE ENERGÍA GLOBAL
            double eInSum = _eq.Inlets.Sum(s => s.MassFlow.GetSolverValue() * s.MassEnthalpy.GetSolverValue());
            double eOutSum = _eq.Outlets.Sum(s => s.MassFlow.GetSolverValue() * s.MassEnthalpy.GetSolverValue());
            residuals.Add(eInSum - eOutSum);

            // 3. BALANCES POR COMPONENTE (N-1 para evitar matriz singular)
            var referenceStream = _eq.Inlets.First();
            int numComponents = referenceStream.Composition.Components.Count;

            if (numComponents > 1)
            {
                for (int i = 0; i < numComponents - 1; i++)
                {
                    double compMassIn = _eq.Inlets.Sum(s =>
                        s.MassFlow.GetSolverValue() * s.Composition.Components[i].MassFraction.GetSolverValue());

                    double compMassOut = _eq.Outlets.Sum(s =>
                        s.MassFlow.GetSolverValue() * s.Composition.Components[i].MassFraction.GetSolverValue());

                    residuals.Add(compMassIn - compMassOut);
                }
            }

            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            // Extrae acoplamientos de todas las entradas (Feeds, Reflujo, Boilup)
            foreach (var s in _eq.Inlets)
            {
                yield return s.MassFlow;
                yield return s.MassEnthalpy;

                int numComponents = s.Composition.Components.Count;
                for (int i = 0; i < numComponents - 1; i++)
                    yield return s.Composition.Components[i].MassFraction;
            }

            // Extrae acoplamientos de todas las salidas (Top, Bottom, SideDraws)
            foreach (var s in _eq.Outlets)
            {
                yield return s.MassFlow;
                yield return s.MassEnthalpy;

                int numComponents = s.Composition.Components.Count;
                for (int i = 0; i < numComponents - 1; i++)
                    yield return s.Composition.Components[i].MassFraction;
            }
        }
    }
}