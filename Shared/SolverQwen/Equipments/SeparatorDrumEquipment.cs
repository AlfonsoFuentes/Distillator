using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.SolverQwen.Equipments
{

    // ============================================================================
    // EQUIPO: TAMBOR SEPARADOR (Flash Drum)
    // ============================================================================
    public class SeparatorDrumEquipment : EquipmentBase
    {
        // ── PUERTOS SEMÁNTICOS (Física Aislada) ──
        public FacadeStream? VaporOutlet { get; private set; }    // Salida de gas (Tope)
        public FacadeStream? LiquidOutlet { get; private set; }   // Salida de líquido (Fondo)

        // ── VARIABLES DE EQUIPO ──
       

        public SeparatorDrumEquipment(string name) : base(name)
        {
           
        }

        // ── CONEXIONES ──
        public void AddFeed(FacadeStream inlet)
        {
            base.AddInlet(inlet);
        }

        public void ConnectVaporOutlet(FacadeStream outlet)
        {
            VaporOutlet = outlet;
            base.AddOutlet(outlet);
        }

        public void ConnectLiquidOutlet(FacadeStream outlet)
        {
            LiquidOutlet = outlet;
            base.AddOutlet(outlet);
        }

        // ── INYECCIÓN DE ESTRATEGIAS ──
        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies()
        {
            yield return new DrumPressurePhase1Strategy(this);
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies()
        {
            yield return new DrumPressurePhase2Strategy(this);
            yield return new DrumConcentrationPhase2Strategy(this);
            yield return new DrumEnthalpyPhase2Strategy(this);
            // Nota: El tambor NO tiene MassBalance en Fase 2, rompe la red másica intencionalmente
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase3Strategies()
        {
            yield return new DrumMassBalancePhase3Strategy(this);
        }


    }

    // ============================================================================
    // FASE 1: PRESIÓN (Isobárico en las salidas, caída de presión desde entradas)
    // ============================================================================
    public class DrumPressurePhase1Strategy : ISolverPhaseStrategy
    {
        private readonly SeparatorDrumEquipment _eq;
        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;
        public string Name => $"{_eq.Name} - {Type} - {Procedence}";

        public DrumPressurePhase1Strategy(SeparatorDrumEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (_eq.VaporOutlet == null || _eq.LiquidOutlet == null || !_eq.Inlets.Any())
                return new double[0];

            var residuals = new List<double>();
            double pVapor = _eq.VaporOutlet.Pressure.GetSolverValue();
            double pLiquid = _eq.LiquidOutlet.Pressure.GetSolverValue();
           

            // 1. Equilibrio mecánico interno: El vapor y el líquido salen a la misma presión
            residuals.Add(pVapor - pLiquid);

            // 2. Pérdida de carga de las entradas hacia el tambor
            foreach (var inlet in _eq.Inlets)
            {
                residuals.Add(inlet.Pressure.GetSolverValue()  - pVapor);
            }

            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
           
            if (_eq.VaporOutlet != null) yield return _eq.VaporOutlet.Pressure;
            if (_eq.LiquidOutlet != null) yield return _eq.LiquidOutlet.Pressure;
            //foreach (var inlet in _eq.Inlets) yield return inlet.Pressure;
        }
    }

    // ============================================================================
    // FASE 2: PRESIÓN GLOBAL (Red Acoplada)
    // ============================================================================
    public class DrumPressurePhase2Strategy : ISolverPhaseStrategy
    {
        private readonly SeparatorDrumEquipment _eq;
        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - {Type} - {Procedence}";

        public DrumPressurePhase2Strategy(SeparatorDrumEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (_eq.VaporOutlet == null || _eq.LiquidOutlet == null || !_eq.Inlets.Any())
                return new double[0];

            var residuals = new List<double>();
            double pVapor = _eq.VaporOutlet.Pressure.GetSolverValue();
            double pLiquid = _eq.LiquidOutlet.Pressure.GetSolverValue();
          

            residuals.Add(pVapor - pLiquid);

            foreach (var inlet in _eq.Inlets)
            {
                residuals.Add(inlet.Pressure.GetSolverValue() -  pVapor);
            }

            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
          
            if (_eq.VaporOutlet != null) yield return _eq.VaporOutlet.Pressure;
            if (_eq.LiquidOutlet != null) yield return _eq.LiquidOutlet.Pressure;
            foreach (var inlet in _eq.Inlets) yield return inlet.Pressure;
        }
    }

    // ============================================================================
    // FASE 2: ENRUTADOR DE MASA Y COMPOSICIÓN (Drum Phase 2)
    // ============================================================================
    public class DrumConcentrationPhase2Strategy : ISolverPhaseStrategy
    {
        private readonly SeparatorDrumEquipment _eq;
        public StrategyType Type => StrategyType.Concentration;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - Phase2_Concentration";

        public DrumConcentrationPhase2Strategy(SeparatorDrumEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) return Array.Empty<double>();
            var feed = _eq.Inlets.First();
            var residuals = new List<double>();

            int numComponents = feed.Composition.Components.Count;
            for (int i = 0; i < numComponents; i++)
            {
                var comp = feed.Composition.Components[i];
                var localComponentVapor = feed.VaporPhase.Components.FirstOrDefault(x => x.Id == comp.Id);
                var localComponentLiquido = feed.LiquidPhase.Components.FirstOrDefault(x => x.Id == comp.Id);

                if (localComponentVapor != null && localComponentLiquido != null)
                {
                    double vaporoutlet = _eq.VaporOutlet.Composition.Components[i].MassFraction.GetSolverValue();
                    double liquidoutlet = _eq.LiquidOutlet.Composition.Components[i].MassFraction.GetSolverValue();
                    residuals.Add(vaporoutlet - localComponentVapor.MassFraction);
                    residuals.Add(liquidoutlet - localComponentLiquido.MassFraction);
                }
            }
            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) yield break;
            int numComponents = _eq.Inlets.First().Composition.Components.Count;
            for (int i = 0; i < numComponents; i++)
            {
                yield return _eq.VaporOutlet.Composition.Components[i].MassFraction;
                yield return _eq.LiquidOutlet.Composition.Components[i].MassFraction;
            }
        }
    }
    public class DrumEnthalpyPhase2Strategy : ISolverPhaseStrategy
    {
        private readonly SeparatorDrumEquipment _eq;
        public StrategyType Type => StrategyType.Enthalpy;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - Phase2_Enthalpy";

        public DrumEnthalpyPhase2Strategy(SeparatorDrumEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) return Array.Empty<double>();
            var feed = _eq.Inlets.First();

            double h_vaporPhase_feed = feed.VaporPhase.MassEnthalpy.GetValue(feed.MassEnthalpy.InternalUnit) / feed.MassEnthalpy.NormalizeValue;
            double h_liquidPhase_feed = feed.LiquidPhase.MassEnthalpy.GetValue(feed.MassEnthalpy.InternalUnit) / feed.MassEnthalpy.NormalizeValue;

            double hVaporGuess = _eq.VaporOutlet.MassEnthalpy.GetSolverValue();
            double hLiquidGuess = _eq.LiquidOutlet.MassEnthalpy.GetSolverValue();

            return new double[]
            {
            hVaporGuess - h_vaporPhase_feed,
            hLiquidGuess - h_liquidPhase_feed
            };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) yield break;
            yield return _eq.VaporOutlet.MassEnthalpy;
            yield return _eq.LiquidOutlet.MassEnthalpy;
        }
    }
    public class DrumMassBalancePhase3Strategy : ISolverPhaseStrategy
    {
        private readonly SeparatorDrumEquipment _eq;
        public StrategyType Type => StrategyType.MassEnergyBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;
        public string Name => $"{_eq.Name} - Phase3_MassSplit";

        public DrumMassBalancePhase3Strategy(SeparatorDrumEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) return Array.Empty<double>();
            var feed = _eq.Inlets.First();

            // ── 1. Propiedades intensivas (Datos fijos de la Fase 2) ──
            double hFeed = feed.MassEnthalpy.GetSolverValue();
            double hvapor = _eq.VaporOutlet.MassEnthalpy.GetSolverValue();
            double hliquid = _eq.LiquidOutlet.MassEnthalpy.GetSolverValue();

            // ── 2. Variables acopladas (Lo que el Orquestador está adivinando) ──
            double mFeedSolver = feed.MassFlow.GetSolverValue();
            double mVaporSolver = _eq.VaporOutlet.MassFlow.GetSolverValue();
            double mLiquidSolver = _eq.LiquidOutlet.MassFlow.GetSolverValue();

            // ── 3. Ecuaciones (Tus balances macroscópicos exactos) ──
            return new double[]
            {
            mFeedSolver - mVaporSolver - mLiquidSolver,                                 // Ecuación 1: Balance de Masa Global
            (mFeedSolver * hFeed) - (mVaporSolver * hvapor) - (mLiquidSolver * hliquid) // Ecuación 2: Balance de Energía Global
            };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) yield break;
            var feed = _eq.Inlets.First();

            // Acoplamos las 3 variables que participan en las 2 ecuaciones de arriba
            yield return feed.MassFlow;
            yield return _eq.VaporOutlet.MassFlow;
            yield return _eq.LiquidOutlet.MassFlow;
        }
    }
}
