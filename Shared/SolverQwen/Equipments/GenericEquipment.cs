using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using UnitSystem;

namespace Shared.SolverQwen.Equipments
{  // 1️⃣ Agrega este enum (si ya existe StrategyType, reemplázalo)
//    public enum StrategyType
//    {
//        Concentration,
//        MassBalance,          // Propagación de composición: x_out = x_in
//        Enthalpy,             // Propagación de entalpía: h_out = h_in  
//        Pressure,             // Ecuaciones hidráulicas: P_out = P_in ± ΔP
//        MassEnergyBalance     // Balance global (Fase 3, equipos "muro")
//    }

//    public interface ISolverPhaseStrategy
//    {
//        StrategyType Type { get; }
//        double[] GetResiduals();
//        IEnumerable<IProcessVariable> GetCouplingVariables();
//        VariableDataProcedence Procedence { get; }
//        string Name { get; }
//    }

//    public interface IEquipment
//    {
//        string Name { get; }
//        List<FacadeStream> Inlets { get; }
//        List<FacadeStream> Outlets { get; }

//        IEnumerable<ISolverPhaseStrategy> GetStrategies();
//    }

//    public abstract class EquipmentBase : IEquipment
//    {
//        public string Name { get; }
//        public List<FacadeStream> Inlets { get; } = new();
//        public List<FacadeStream> Outlets { get; } = new();

//        protected EquipmentBase(string name)
//        {
//            Name = name ?? throw new ArgumentNullException(nameof(name));
//        }

//        public void AddInlet(FacadeStream stream) => Inlets.Add(stream);
//        public void AddOutlet(FacadeStream stream) => Outlets.Add(stream);

//        public IEnumerable<ISolverPhaseStrategy> GetStrategies()
//        {
//            var strategies = new List<ISolverPhaseStrategy>();

//#if DEBUG
//            Console.WriteLine($"\n  [EquipmentBase] 🏭 Inyectando estrategias para: {Name}...");
//#endif

//            // 1. FASE 1
//            var phase1Strategies = CreatePhase1Strategies();
//            if (phase1Strategies != null)
//                strategies.AddRange(phase1Strategies);

//            // 2. FASE 2
//            var phase2Strategies = CreatePhase2Strategies();
//            if (phase2Strategies != null)
//                strategies.AddRange(phase2Strategies);

//            // 3. FASE 3
//            var phase3Strategy = CreatePhase3Strategies();
//            if (phase3Strategy != null)
//                strategies.AddRange(phase3Strategy);

//#if DEBUG
//            Console.WriteLine($"  [EquipmentBase] ✅ {Name} inyectó un total de {strategies.Count} estrategias al Orquestador.");
//#endif

//            return strategies;
//        }

//        protected abstract IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies();
//        protected abstract IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies();

//        protected virtual IEnumerable<ISolverPhaseStrategy> CreatePhase3Strategies()
//        {
//            return new ISolverPhaseStrategy[] { new GeneralMassEnergyBalancePhase3Strategy(this) };
//        }
//    }

//    public class GeneralMassEnergyBalancePhase3Strategy : ISolverPhaseStrategy
//    {
//        private readonly EquipmentBase _equipment;

//        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
//        public StrategyType Type => StrategyType.MassEnergyBalance;
//        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;

//        public GeneralMassEnergyBalancePhase3Strategy(EquipmentBase equipment)
//        {
//            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
//        }

//        public double[] GetResiduals()
//        {
//            var residuals = new List<double>();

//            // ─────────────────────────────────────────────────────────
//            // 1. BALANCE DE MASA GLOBAL
//            // ─────────────────────────────────────────────────────────
//            if (_equipment.Inlets.Any() && _equipment.Outlets.Any())
//            {
//                double massFlowIn = _equipment.Inlets.Sum(s => s.MassFlow.GetSolverValue());
//                double massFlowOut = _equipment.Outlets.Sum(s => s.MassFlow.GetSolverValue());
//                double massRes = massFlowIn - massFlowOut;

//#if DEBUG
//                if (double.IsNaN(massRes) || double.IsInfinity(massRes))
//                    Console.WriteLine($"  [Phase3-🚨] FATAL: Balance de MASA en {Name} retornó NaN/Infinity!");
//#endif
//                residuals.Add(massRes);
//            }

//            // ─────────────────────────────────────────────────────────
//            // 2. BALANCE DE ENERGÍA GLOBAL
//            // ─────────────────────────────────────────────────────────
//            if (_equipment.Inlets.Any() && _equipment.Outlets.Any())
//            {
//                double energyIn = _equipment.Inlets.Sum(s =>
//                    s.MassFlow.GetSolverValue() * s.MassEnthalpy.GetSolverValue());

//                double energyOut = _equipment.Outlets.Sum(s =>
//                    s.MassFlow.GetSolverValue() * s.MassEnthalpy.GetSolverValue());

//                double energyRes = energyIn - energyOut;

//#if DEBUG
//                if (double.IsNaN(energyRes) || double.IsInfinity(energyRes))
//                    Console.WriteLine($"  [Phase3-🚨] FATAL: Balance de ENERGÍA en {Name} retornó NaN/Infinity!");
//#endif
//                residuals.Add(energyRes);
//            }

//            return residuals.ToArray();
//        }

//        public IEnumerable<IProcessVariable> GetCouplingVariables()
//        {
//            foreach (var inlet in _equipment.Inlets)
//            {
//                yield return inlet.MassFlow;
//                yield return inlet.MassEnthalpy;
//            }

//            foreach (var outlet in _equipment.Outlets)
//            {
//                yield return outlet.MassFlow;
//                yield return outlet.MassEnthalpy;
//            }
//        }
//    }

//    public class GlobalMassBalancePhase3Strategy : ISolverPhaseStrategy
//    {
//        private readonly EquipmentBase _equipment;

//        public string Name => $"{_equipment.Name} - Phase3_GlobalMassOnly";
//        public StrategyType Type => StrategyType.MassEnergyBalance;
//        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;

//        public GlobalMassBalancePhase3Strategy(EquipmentBase equipment)
//        {
//            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
//        }

//        public double[] GetResiduals()
//        {
//            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any())
//                return new double[0];

//            double massFlowIn = _equipment.Inlets.Sum(s => s.MassFlow.GetSolverValue());
//            double massFlowOut = _equipment.Outlets.Sum(s => s.MassFlow.GetSolverValue());

//            double res = massFlowIn - massFlowOut;

//#if DEBUG
//            if (double.IsNaN(res) || double.IsInfinity(res))
//                Console.WriteLine($"  [Phase3-🚨] FATAL: Balance de MASA (GlobalMassOnly) en {Name} retornó NaN/Infinity!");
//#endif

//            return new double[] { res };
//        }

//        public IEnumerable<IProcessVariable> GetCouplingVariables()
//        {
//            foreach (var inlet in _equipment.Inlets)
//                yield return inlet.MassFlow;

//            foreach (var outlet in _equipment.Outlets)
//                yield return outlet.MassFlow;
//        }
//    }
//    public class GenericBarrierEquipment : EquipmentBase
//    {
//        public GenericBarrierEquipment(string name) : base(name) { }

//        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies()
//        {
//            // ✅ No tiene ecuaciones locales simples (no propaga C ni P)
//            yield break;
//        }

//        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies()
//        {
//            // ✅ No participa en red hidráulica simple
//            yield break;
//        }

        
//    }
}

