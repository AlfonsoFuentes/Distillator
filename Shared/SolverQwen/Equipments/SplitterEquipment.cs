using Shared.SolverQwen.Variables;

namespace Shared.SolverQwen.Equipments
{
    // ---------------------------------------------------------
    // 1. IMPLEMENTACIÓN DEL EQUIPO (SplitterEquipment)
    // ---------------------------------------------------------
    // ============================================================================
    // SPLITTER EQUIPMENT + ESTRATEGIAS (Fase 1 y Fase 2)
    // ============================================================================

    public class SplitterEquipment : EquipmentBase
    {
        public SplitterEquipment(string name) : base(name) { }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies()
        {
            //yield return new SplitterMassFlowPhase1Strategy(this);
            yield return new SplitterPressurePhase1Strategy(this);
            yield return new SplitterConcentrationPhase1Strategy(this);
            yield return new SplitterEnthalpyPhase1Strategy(this);
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies()
        {
            // ✅ FASE 2: Pressure, Concentration, Enthalpy (NO MassBalance)
            yield return new SplitterPressurePhase2Strategy(this);
            yield return new SplitterConcentrationPhase2Strategy(this);
            yield return new SplitterEnthalpyPhase2Strategy(this);
            //yield return new SplitterMassBalancePhase2Strategy(this);
        }
        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase3Strategies()
        {
            yield   return new GlobalMassBalancePhase3Strategy(this);
        }
    }

    // ============================================================================
    // SPLITTER - FASE 1: PROPAGACIÓN DE FLUJO MÁSICO (ṁ_out[k] = ṁ_in)
    // ============================================================================
    public class SplitterMassFlowPhase1Strategy : ISolverPhaseStrategy
    {
        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
        private readonly SplitterEquipment _equipment;
        public StrategyType Type => StrategyType.MassBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;

        public SplitterMassFlowPhase1Strategy(SplitterEquipment equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public double[] GetResiduals()
        {
            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];

            double mIn = _equipment.Inlets.First().MassFlow.GetSolverValue();
            double mOutSum = _equipment.Outlets.Sum(o => o.MassFlow.GetSolverValue());

            // ✅ UNA sola ecuación: ṁ_in - Σṁ_out = 0
            return new double[] { mIn - mOutSum };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().MassFlow;
            foreach (var o in _equipment.Outlets) yield return o.MassFlow;
        }
    }
    public class SplitterPressurePhase1Strategy : ISolverPhaseStrategy
    {
        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
        private readonly SplitterEquipment _equipment;
        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;

        public SplitterPressurePhase1Strategy(SplitterEquipment equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public double[] GetResiduals()
        {
            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
            double pIn = _equipment.Inlets.First().Pressure.GetSolverValue();
            return _equipment.Outlets.Select(o => pIn - o.Pressure.GetSolverValue()).ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().Pressure;
            foreach (var o in _equipment.Outlets) yield return o.Pressure;
        }
    }

    // ============================================================================
    // SPLITTER - FASE 1: CONCENTRACIÓN (x_out[k] = x_in) - LOCAL
    // ============================================================================
    public class SplitterConcentrationPhase1Strategy : ISolverPhaseStrategy
    {
        private readonly SplitterEquipment _equipment;
        public StrategyType Type => StrategyType.Concentration;
        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;

        public SplitterConcentrationPhase1Strategy(SplitterEquipment equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public double[] GetResiduals()
        {
            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
            var inlet = _equipment.Inlets.First();
            var inComps = inlet.Composition.Components;
            var residuals = new List<double>();

            for (int i = 0; i < inComps.Count; i++)
            {
                double xIn = inComps[i].MassFraction.GetSolverValue();
                foreach (var outlet in _equipment.Outlets)
                {
                    double xOut = outlet.Composition.Components[i].MassFraction.GetSolverValue();
                    residuals.Add(xIn - xOut);
                }
            }
            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            var inlet = _equipment.Inlets.FirstOrDefault();
            if (inlet != null) foreach (var c in inlet.Composition.Components) yield return c.MassFraction;
            foreach (var outlet in _equipment.Outlets)
                foreach (var c in outlet.Composition.Components) yield return c.MassFraction;
        }
    }

    // ============================================================================
    // SPLITTER - FASE 1: ENTALPÍA (h_out[k] = h_in) - LOCAL
    // ============================================================================
    public class SplitterEnthalpyPhase1Strategy : ISolverPhaseStrategy
    {
        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
        private readonly SplitterEquipment _equipment;
        public StrategyType Type => StrategyType.Enthalpy;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;

        public SplitterEnthalpyPhase1Strategy(SplitterEquipment equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public double[] GetResiduals()
        {
            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
            double hIn = _equipment.Inlets.First().MassEnthalpy.GetSolverValue();
            return _equipment.Outlets.Select(o => hIn - o.MassEnthalpy.GetSolverValue()).ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().MassEnthalpy;
            foreach (var o in _equipment.Outlets) yield return o.MassEnthalpy;
        }
    }

    // ============================================================================
    // SPLITTER - FASE 2: PRESIÓN GLOBAL (P_out[k] = P_in) - RED ACOPLADA
    // ============================================================================
    public class SplitterPressurePhase2Strategy : ISolverPhaseStrategy
    {
        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
        private readonly SplitterEquipment _equipment;
        public StrategyType Type => StrategyType.Pressure;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

        public SplitterPressurePhase2Strategy(SplitterEquipment equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public double[] GetResiduals()
        {
            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
            double pIn = _equipment.Inlets.First().Pressure.GetSolverValue();
            return _equipment.Outlets.Select(o => pIn - o.Pressure.GetSolverValue()).ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().Pressure;
            foreach (var o in _equipment.Outlets) yield return o.Pressure;
        }
    }

    // ============================================================================
    // SPLITTER - FASE 2: CONCENTRACIÓN (x_out[k] = x_in) - SUBSISTEMA
    // ============================================================================
    public class SplitterConcentrationPhase2Strategy : ISolverPhaseStrategy
    {
        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
        private readonly SplitterEquipment _equipment;
        public StrategyType Type => StrategyType.Concentration;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

        public SplitterConcentrationPhase2Strategy(SplitterEquipment equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public double[] GetResiduals()
        {
            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
            var inlet = _equipment.Inlets.First();
            var inComps = inlet.Composition.Components;
            var residuals = new List<double>();

            for (int i = 0; i < inComps.Count; i++)
            {
                double xIn = inComps[i].MassFraction.GetSolverValue();
                foreach (var outlet in _equipment.Outlets)
                {
                    double xOut = outlet.Composition.Components[i].MassFraction.GetSolverValue();
                    residuals.Add(xIn - xOut);
                }
            }
            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            var inlet = _equipment.Inlets.FirstOrDefault();
            if (inlet != null) foreach (var c in inlet.Composition.Components) yield return c.MassFraction;
            foreach (var outlet in _equipment.Outlets)
                foreach (var c in outlet.Composition.Components) yield return c.MassFraction;
        }
    }

    // ============================================================================
    // SPLITTER - FASE 2: ENTALPÍA (h_out[k] = h_in) - SUBSISTEMA
    // ============================================================================
    public class SplitterEnthalpyPhase2Strategy : ISolverPhaseStrategy
    {
        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
        private readonly SplitterEquipment _equipment;
        public StrategyType Type => StrategyType.Enthalpy;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

        public SplitterEnthalpyPhase2Strategy(SplitterEquipment equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public double[] GetResiduals()
        {
            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
            double hIn = _equipment.Inlets.First().MassEnthalpy.GetSolverValue();
            return _equipment.Outlets.Select(o => hIn - o.MassEnthalpy.GetSolverValue()).ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().MassEnthalpy;
            foreach (var o in _equipment.Outlets) yield return o.MassEnthalpy;
        }
    }
    // ============================================================================
    // SPLITTER - FASE 2: BALANCE DE MASA (ṁ_in = Σṁ_out)
    // ============================================================================
    // ============================================================================
    // SPLITTER - FASE 3: BALANCE DE MASA Y ENERGÍA (Específico)
    // ============================================================================
    public class SplitterMassEnergyPhase3Strategy : ISolverPhaseStrategy
    {
        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
        private readonly SplitterEquipment _equipment;
        public StrategyType Type => StrategyType.MassEnergyBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;

        public SplitterMassEnergyPhase3Strategy(SplitterEquipment equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public double[] GetResiduals()
        {
            var residuals = new List<double>();

            // ── BALANCE DE MASA: ṁ_in = Σṁ_out ──
            if (_equipment.Inlets.Any() && _equipment.Outlets.Any())
            {
                double mIn = _equipment.Inlets.First().MassFlow.GetSolverValue();
                double mOutSum = _equipment.Outlets.Sum(o => o.MassFlow.GetSolverValue());
                residuals.Add(mIn - mOutSum);
            }

            // ── BALANCE DE ENERGÍA: (ṁ·h)_in = Σ(ṁ·h)_out ──
            if (_equipment.Inlets.Any() && _equipment.Outlets.Any())
            {
                double eIn = _equipment.Inlets.First().MassFlow.GetSolverValue() *
                             _equipment.Inlets.First().MassEnthalpy.GetSolverValue();

                double eOutSum = _equipment.Outlets.Sum(o =>
                    o.MassFlow.GetSolverValue() * o.MassEnthalpy.GetSolverValue());

                residuals.Add(eIn - eOutSum);
            }

            return residuals.ToArray();
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            if (_equipment.Inlets.Any())
            {
                yield return _equipment.Inlets.First().MassFlow;
                yield return _equipment.Inlets.First().MassEnthalpy;
            }
            foreach (var o in _equipment.Outlets)
            {
                yield return o.MassFlow;
                yield return o.MassEnthalpy;
            }
        }
    }


}