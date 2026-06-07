using Shared.SolverQwen.Variables;
using UnitSystem;

namespace Shared.SolverQwen.Equipments
{
//    public class ValveEquipment : EquipmentBase
//    {
//        public ProcessVariable<PressureDrop> DeltaP { get; }

//        public ValveEquipment(string name) : base(name)
//        {
//            DeltaP = new ProcessVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
//        }

//        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies()
//        {
//            // ✅ FASE 1: SOLO Pressure
//            yield return new ValvePressurePhase1Strategy(this);
//        }

//        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies()
//        {
//            // ✅ FASE 2: Pressure, Concentration, Enthalpy, MassBalance
//            yield return new ValvePressurePhase2Strategy(this);
//            yield return new ValveConcentrationPhase2Strategy(this);
//            yield return new ValveEnthalpyPhase2Strategy(this);
//            yield return new ValveMassBalancePhase2Strategy(this);
//        }

//        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase3Strategies()
//        {
//            yield return new GlobalMassBalancePhase3Strategy(this);
//        }
//    }

//    // ============================================================================
//    public class ValvePressurePhase1Strategy : ISolverPhaseStrategy
//    {
//        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
//        private readonly ValveEquipment _equipment;
//        public StrategyType Type => StrategyType.Pressure;
//        public VariableDataProcedence Procedence => VariableDataProcedence.Phase1_LocalPropagation;

//        public ValvePressurePhase1Strategy(ValveEquipment equipment)
//        {
//            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
//        }

//        public double[] GetResiduals()
//        {
//            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];

//            double pIn = _equipment.Inlets.First().Pressure.GetSolverValue();
//            double pOut = _equipment.Outlets.First().Pressure.GetSolverValue();
//            double deltaP = _equipment.DeltaP.GetSolverValue();

//            double residual = pIn - deltaP - pOut;

//#if DEBUG
//            if (double.IsNaN(residual) || double.IsInfinity(residual))
//                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Presión es NaN/Inf! pIn:{pIn:F2}, pOut:{pOut:F2}, deltaP:{deltaP:F2}");
//#endif

//            return new double[] { residual };  // Válvula: caída de presión
//        }

//        public IEnumerable<IProcessVariable> GetCouplingVariables()
//        {
//            yield return _equipment.DeltaP;

//            if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().Pressure;
//            if (_equipment.Outlets.Any()) yield return _equipment.Outlets.First().Pressure;
//        }
//    }

//    // ============================================================================
//    // VÁLVULA - FASE 2: PRESIÓN GLOBAL (P_out = P_in - ΔP) - RED ACOPLADA
//    // ============================================================================
//    public class ValvePressurePhase2Strategy : ISolverPhaseStrategy
//    {
//        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
//        private readonly ValveEquipment _equipment;
//        public StrategyType Type => StrategyType.Pressure;
//        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

//        public ValvePressurePhase2Strategy(ValveEquipment equipment)
//        {
//            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
//        }

//        public double[] GetResiduals()
//        {
//            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];

//            double pIn = _equipment.Inlets.First().Pressure.GetSolverValue();
//            double pOut = _equipment.Outlets.First().Pressure.GetSolverValue();
//            double deltaP = _equipment.DeltaP.GetSolverValue();

//            double residual = pIn - deltaP - pOut;

//#if DEBUG
//            if (double.IsNaN(residual) || double.IsInfinity(residual))
//                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Presión es NaN/Inf! pIn:{pIn:F2}, pOut:{pOut:F2}, deltaP:{deltaP:F2}");
//#endif

//            return new double[] { residual };  // Válvula: caída de presión
//        }
//        public IEnumerable<IProcessVariable> GetCouplingVariables()
//        {
//            yield return _equipment.DeltaP;

//            if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().Pressure;
//            if (_equipment.Outlets.Any()) yield return _equipment.Outlets.First().Pressure;
//        }
//    }

//    // ============================================================================
//    // VÁLVULA - FASE 2: CONCENTRACIÓN (x_out = x_in) - SUBSISTEMA
//    // ============================================================================
//    public class ValveConcentrationPhase2Strategy : ISolverPhaseStrategy
//    {
//        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
//        private readonly ValveEquipment _equipment;
//        public StrategyType Type => StrategyType.Concentration;
//        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

//        public ValveConcentrationPhase2Strategy(ValveEquipment equipment)
//        {
//            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
//        }

//        public double[] GetResiduals()
//        {
//            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];
//            var inlet = _equipment.Inlets.First();
//            var outlet = _equipment.Outlets.First();
//            var residuals = new List<double>();
//            var inComps = inlet.Composition.Components;
//            var outComps = outlet.Composition.Components;

//            for (int i = 0; i < inComps.Count && i < outComps.Count; i++)
//            {
//                double res = inComps[i].MassFraction.GetSolverValue() - outComps[i].MassFraction.GetSolverValue();
//#if DEBUG
//                if (double.IsNaN(res) || double.IsInfinity(res))
//                    Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Concentración es NaN/Inf para componente índice {i}!");
//#endif
//                residuals.Add(res);
//            }
//            return residuals.ToArray();
//        }

//        public IEnumerable<IProcessVariable> GetCouplingVariables()
//        {
//            var inlet = _equipment.Inlets.FirstOrDefault();
//            var outlet = _equipment.Outlets.FirstOrDefault();
//            if (inlet != null) foreach (var c in inlet.Composition.Components) yield return c.MassFraction;
//            if (outlet != null) foreach (var c in outlet.Composition.Components) yield return c.MassFraction;
//        }
//    }

//    // ============================================================================
//    // VÁLVULA - FASE 2: ENTALPÍA (h_out = h_in) - SUBSISTEMA
//    // ============================================================================
//    public class ValveEnthalpyPhase2Strategy : ISolverPhaseStrategy
//    {
//        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
//        private readonly ValveEquipment _equipment;
//        public StrategyType Type => StrategyType.Enthalpy;
//        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

//        public ValveEnthalpyPhase2Strategy(ValveEquipment equipment)
//        {
//            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
//        }

//        public double[] GetResiduals()
//        {
//            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];

//            double hIn = _equipment.Inlets.First().MassEnthalpy.GetSolverValue();
//            double hOut = _equipment.Outlets.First().MassEnthalpy.GetSolverValue();

//            double residual = hIn - hOut;

//#if DEBUG
//            if (double.IsNaN(residual) || double.IsInfinity(residual))
//                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Entalpía es NaN/Inf! hIn:{hIn:F2}, hOut:{hOut:F2}");
//#endif

//            return new double[] { residual };
//        }

//        public IEnumerable<IProcessVariable> GetCouplingVariables()
//        {
//            if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().MassEnthalpy;
//            if (_equipment.Outlets.Any()) yield return _equipment.Outlets.First().MassEnthalpy;
//        }
//    }

//    // ============================================================================
//    // VÁLVULA - FASE 2: BALANCE DE MASA (ṁ_out = ṁ_in) - SUBSISTEMA
//    // ============================================================================
//    public class ValveMassBalancePhase2Strategy : ISolverPhaseStrategy
//    {
//        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
//        private readonly ValveEquipment _equipment;
//        public StrategyType Type => StrategyType.MassBalance;
//        public VariableDataProcedence Procedence => VariableDataProcedence.Phase2_EasyEquipmentNet;

//        public ValveMassBalancePhase2Strategy(ValveEquipment equipment)
//        {
//            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
//        }

//        public double[] GetResiduals()
//        {
//            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any()) return new double[0];

//            double mIn = _equipment.Inlets.First().MassFlow.GetSolverValue();
//            double mOut = _equipment.Outlets.First().MassFlow.GetSolverValue();

//            double residual = mIn - mOut;

//#if DEBUG
//            if (double.IsNaN(residual) || double.IsInfinity(residual))
//                Console.WriteLine($"  [{Name} 🚨] FATAL: Residual de Masa es NaN/Inf! mIn:{mIn:F2}, mOut:{mOut:F2}");
//#endif

//            return new double[] { residual };
//        }

//        public IEnumerable<IProcessVariable> GetCouplingVariables()
//        {
//            if (_equipment.Inlets.Any()) yield return _equipment.Inlets.First().MassFlow;
//            if (_equipment.Outlets.Any()) yield return _equipment.Outlets.First().MassFlow;
//        }
//    }
}

