using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.SolverQwen.Equipments
{

   
    public class SeparatorDrumEquipment : EquipmentBase
    {
        // ── PUERTOS SEMÁNTICOS (Física Aislada) ──
        public FacadeStream? VaporOutlet { get; private set; }    // Salida de gas (Tope)
        public FacadeStream? LiquidOutlet { get; private set; }   // Salida de líquido (Fondo)

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
            yield break;
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies()
        {
            yield return new DrumPressurePhase2Strategy(this);
            yield return new DrumConcentrationPhase2Strategy(this);
            yield return new DrumEnthalpyPhase2Strategy(this);
            yield return new DrumMassBalancePhase2Strategy(this);
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

            residuals.Add(pVapor - pLiquid);

            foreach (var inlet in _eq.Inlets)
            {
                residuals.Add(inlet.Pressure.GetSolverValue() - pVapor);
            }

#if DEBUG
            if (residuals.Any(r => double.IsNaN(r) || double.IsInfinity(r)))
                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Presión es NaN/Inf!");
#endif

            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_eq.VaporOutlet != null) yield return _eq.VaporOutlet.Pressure;
            if (_eq.LiquidOutlet != null) yield return _eq.LiquidOutlet.Pressure;
            // Inlets are acting as boundaries/muros in Phase 1
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
                return Array.Empty<double>();

            var residuals = new List<double>();
            double pVapor = _eq.VaporOutlet.Pressure.GetSolverValue();
            double pLiquid = _eq.LiquidOutlet.Pressure.GetSolverValue();

            // 1. Equilibrio mecánico interno (Isobaric outputs)
            residuals.Add(pVapor - pLiquid);

            // 2. Equilibrio con las entradas (Assuming 0 DeltaP across the drum for now)
            foreach (var inlet in _eq.Inlets)
            {
                double pInlet = inlet.Pressure.GetSolverValue();
                residuals.Add(pInlet - pVapor);
            }

#if DEBUG
            if (residuals.Any(r => double.IsNaN(r) || double.IsInfinity(r)))
                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Presión es NaN/Inf!");
#endif

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
    // FASE 2: ENRUTADOR DE CONCENTRACIÓN (Drum Phase 2)
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

                    double resV = vaporoutlet - localComponentVapor.MassFraction;
                    double resL = liquidoutlet - localComponentLiquido.MassFraction;

#if DEBUG
                    if (double.IsNaN(resV) || double.IsNaN(resL))
                        Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Concentración NaN en comp {i}");
#endif

                    residuals.Add(resV);
                    residuals.Add(resL);
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
                // Solo las salidas son incógnitas acopladas, la alimentación actúa como parámetro fijo (Graph Tearing)
                yield return _eq.VaporOutlet.Composition.Components[i].MassFraction;
                yield return _eq.LiquidOutlet.Composition.Components[i].MassFraction;
            }
        }
    }

    // ============================================================================
    // FASE 2: ENRUTADOR DE ENTALPÍAS
    // ============================================================================
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

            double resV = hVaporGuess - h_vaporPhase_feed;
            double resL = hLiquidGuess - h_liquidPhase_feed;

#if DEBUG
            if (double.IsNaN(resV) || double.IsNaN(resL))
                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Entalpía NaN. hVFeed:{h_vaporPhase_feed}, hLFeed:{h_liquidPhase_feed}");
#endif

            return new double[] { resV, resL };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) yield break;
            yield return _eq.VaporOutlet.MassEnthalpy;
            yield return _eq.LiquidOutlet.MassEnthalpy;
        }
    }

    // ============================================================================
    // FASE 3: BALANCE DE MASA/ENERGÍA (Split Matemático)
    // ============================================================================
    public class DrumMassBalancePhase2Strategy : ISolverPhaseStrategy
    {
        private readonly SeparatorDrumEquipment _eq;
        public StrategyType Type => StrategyType.MassBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;
        public string Name => $"{_eq.Name} - Phase2_MassSplit";

        public DrumMassBalancePhase2Strategy(SeparatorDrumEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) return Array.Empty<double>();
            var feed = _eq.Inlets.First();

            double hFeed = feed.MassEnthalpy.GetSolverValue();
            double hvapor = _eq.VaporOutlet.MassEnthalpy.GetSolverValue();
            double hliquid = _eq.LiquidOutlet.MassEnthalpy.GetSolverValue();

            double mFeedSolver = feed.MassFlow.GetSolverValue();
            double mVaporSolver = _eq.VaporOutlet.MassFlow.GetSolverValue();
            double mLiquidSolver = _eq.LiquidOutlet.MassFlow.GetSolverValue();

            double resMass = mFeedSolver - mVaporSolver - mLiquidSolver;
            double resEnergy = (mFeedSolver * hFeed) - (mVaporSolver * hvapor) - (mLiquidSolver * hliquid);

#if DEBUG
            if (double.IsNaN(resMass) || double.IsNaN(resEnergy))
                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual NaN! mIn:{mFeedSolver}, mV:{mVaporSolver}, mL:{mLiquidSolver}");
#endif

            return new double[] { resMass, resEnergy };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) yield break;
            var feed = _eq.Inlets.First();

          
            yield return _eq.VaporOutlet.MassFlow;
            yield return _eq.LiquidOutlet.MassFlow;
        }
    }

    public class DrumMassBalancePhase3Strategy : ISolverPhaseStrategy
    {
        private readonly SeparatorDrumEquipment _eq;
        public StrategyType Type => StrategyType.MassEnergyBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;
        public string Name => $"{_eq.Name} - Phase3_MassEnergyBalance";

        public DrumMassBalancePhase3Strategy(SeparatorDrumEquipment eq)
        {
            _eq = eq ?? throw new ArgumentNullException(nameof(eq));
        }

        public double[] GetResiduals()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) return Array.Empty<double>();
            var feed = _eq.Inlets.First();

            double hFeed = feed.MassEnthalpy.GetSolverValue();
            double hvapor = _eq.VaporOutlet.MassEnthalpy.GetSolverValue();
            double hliquid = _eq.LiquidOutlet.MassEnthalpy.GetSolverValue();

            double mFeedSolver = feed.MassFlow.GetSolverValue();
            double mVaporSolver = _eq.VaporOutlet.MassFlow.GetSolverValue();
            double mLiquidSolver = _eq.LiquidOutlet.MassFlow.GetSolverValue();

            double resMass = mFeedSolver - mVaporSolver - mLiquidSolver;
            double resEnergy = (mFeedSolver * hFeed) - (mVaporSolver * hvapor) - (mLiquidSolver * hliquid);

#if DEBUG
            if (double.IsNaN(resMass) || double.IsNaN(resEnergy))
                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual NaN! mIn:{mFeedSolver}, mV:{mVaporSolver}, mL:{mLiquidSolver}");
#endif

            return new double[] { resMass, resEnergy };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (!_eq.Inlets.Any() || _eq.VaporOutlet == null || _eq.LiquidOutlet == null) yield break;
            var feed = _eq.Inlets.First();


            yield return _eq.VaporOutlet.MassFlow;
            yield return _eq.LiquidOutlet.MassFlow;
        }
    }
}
