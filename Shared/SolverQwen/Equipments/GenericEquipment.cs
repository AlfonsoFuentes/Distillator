using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using UnitSystem;

namespace Shared.SolverQwen.Equipments
{  // 1️⃣ Agrega este enum (si ya existe StrategyType, reemplázalo)
    public enum StrategyType
    {
        Concentration,
        MassBalance,          // Propagación de composición: x_out = x_in
        Enthalpy,             // Propagación de entalpía: h_out = h_in  
        Pressure,             // Ecuaciones hidráulicas: P_out = P_in ± ΔP
        MassEnergyBalance     // Balance global (Fase 3, equipos "muro")
    }

    // 2️⃣ Reemplaza TU interface ISolverPhaseStrategy por esta versión exacta:
    public interface ISolverPhaseStrategy
    {
        StrategyType Type { get; }                          // ✅ NUEVO: Identifica qué resuelve
        double[] GetResiduals();
        IEnumerable<IProcessVariable> GetCouplingVariables();
        VariableDataProcedence Procedence { get; }

        string Name { get; }
    }



    public interface IEquipment
    {
        string Name { get; }
        List<FacadeStream> Inlets { get; }
        List<FacadeStream> Outlets { get; }

        // El Orquestador solo ve esto. No le importa cómo se fabricaron.
        IEnumerable<ISolverPhaseStrategy> GetStrategies();
    }

    // ---------------------------------------------------------
    // 2. LA CLASE BASE ABSTRACTA (El "Factory Method" y "Template")
    // ---------------------------------------------------------
    public abstract class EquipmentBase : IEquipment
    {
        public string Name { get; }
        public List<FacadeStream> Inlets { get; } = new();
        public List<FacadeStream> Outlets { get; } = new();



        protected EquipmentBase(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));

        }

        public void AddInlet(FacadeStream stream) => Inlets.Add(stream);
        public void AddOutlet(FacadeStream stream) => Outlets.Add(stream);

        /// <summary>
        /// PATRÓN TEMPLATE METHOD: Define el esqueleto de la fábrica.
        /// Garantiza la estructura de las estrategias para el Orquestador.
        /// </summary>
        public IEnumerable<ISolverPhaseStrategy> GetStrategies()
        {
            var strategies = new List<ISolverPhaseStrategy>();

            // 1. FASE 1
            var phase1Strategies = CreatePhase1Strategies();
            if (phase1Strategies != null)
                strategies.AddRange(phase1Strategies);

            // 2. FASE 2 (AHORA RETORNA MÚLTIPLES ESTRATEGIAS)
            var phase2Strategies = CreatePhase2Strategies();  // ← CAMBIADO
            if (phase2Strategies != null)
                strategies.AddRange(phase2Strategies);

            // 3. FASE 3
            var phase3Strategy = CreatePhase3Strategies();
            if (phase3Strategy != null)
                strategies.AddRange(phase3Strategy);

            return strategies;
        }

        // Obliga a cada equipo particular a definir su lista de ecuaciones locales (Fase 1)
        protected abstract IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies();

        // ✅ CAMBIADO: Ahora retorna MÚLTIPLES estrategias de Fase 2
        protected abstract IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies();

        // Balance global (Fase 3)
       
        protected virtual  IEnumerable<ISolverPhaseStrategy> CreatePhase3Strategies()
        {
            return new ISolverPhaseStrategy[] { new GeneralMassEnergyBalancePhase3Strategy(this) };
        }



    }
    public class GeneralMassEnergyBalancePhase3Strategy : ISolverPhaseStrategy
    {
        private readonly EquipmentBase _equipment;

        public string Name => $"{_equipment.Name} - {Type} - {Procedence}";
        public StrategyType Type => StrategyType.MassEnergyBalance;
        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;

        public GeneralMassEnergyBalancePhase3Strategy(EquipmentBase equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        /// <summary>
        /// Residuos de Fase 3:
        /// 1. Balance de Masa GLOBAL: Σṁ_in - Σṁ_out = 0
        /// 2. Balance de Energía GLOBAL: Σ(ṁ·h)_in - Σ(ṁ·h)_out = 0
        /// </summary>
        public double[] GetResiduals()
        {
            var residuals = new List<double>();

            // ─────────────────────────────────────────────────────────
            // 1. BALANCE DE MASA GLOBAL (flujo másico total, no por componente)
            // ─────────────────────────────────────────────────────────
            if (_equipment.Inlets.Any() && _equipment.Outlets.Any())
            {
                double massFlowIn = _equipment.Inlets.Sum(s => s.MassFlow.GetSolverValue());
                double massFlowOut = _equipment.Outlets.Sum(s => s.MassFlow.GetSolverValue());
                residuals.Add(massFlowIn - massFlowOut);  // Σṁ_in - Σṁ_out = 0
            }

            // ─────────────────────────────────────────────────────────
            // 2. BALANCE DE ENERGÍA GLOBAL
            // ─────────────────────────────────────────────────────────
            if (_equipment.Inlets.Any() && _equipment.Outlets.Any())
            {
                double energyIn = _equipment.Inlets.Sum(s =>
                    s.MassFlow.GetSolverValue() * s.MassEnthalpy.GetSolverValue());

                double energyOut = _equipment.Outlets.Sum(s =>
                    s.MassFlow.GetSolverValue() * s.MassEnthalpy.GetSolverValue());

                residuals.Add(energyIn - energyOut);  // Σ(ṁ·h)_in - Σ(ṁ·h)_out = 0
            }

            return residuals.ToArray();
        }

        /// <summary>
        /// Variables de acoplamiento: flujo másico total y entalpía de cada corriente.
        /// </summary>
        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            foreach (var inlet in _equipment.Inlets)
            {
                yield return inlet.MassFlow;      // ← Flujo másico TOTAL de la corriente
                yield return inlet.MassEnthalpy;  // ← Entalpía másica
            }

            foreach (var outlet in _equipment.Outlets)
            {
                yield return outlet.MassFlow;     // ← Flujo másico TOTAL de la corriente
                yield return outlet.MassEnthalpy; // ← Entalpía másica
            }
        }
    }
    // ============================================================================
    // ESTRATEGIA DE FASE 3 REDUCIDA: SOLO BALANCE DE MASA (Evita matriz singular)
    // ============================================================================
    public class GlobalMassBalancePhase3Strategy : ISolverPhaseStrategy
    {
        private readonly EquipmentBase _equipment;

        public string Name => $"{_equipment.Name} - Phase3_GlobalMassOnly";

        // Mantenemos este Type para que el Orquestador lo recolecte en RunGlobalPhase
        public StrategyType Type => StrategyType.MassEnergyBalance;

        public VariableDataProcedence Procedence => VariableDataProcedence.Phase3_ThermoAdjustment;

        public GlobalMassBalancePhase3Strategy(EquipmentBase equipment)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public double[] GetResiduals()
        {
            if (!_equipment.Inlets.Any() || !_equipment.Outlets.Any())
                return new double[0];

            // ─────────────────────────────────────────────────────────
            // BALANCE DE MASA GLOBAL (flujo másico total)
            // ─────────────────────────────────────────────────────────
            double massFlowIn = _equipment.Inlets.Sum(s => s.MassFlow.GetSolverValue());
            double massFlowOut = _equipment.Outlets.Sum(s => s.MassFlow.GetSolverValue());

            // 🚨 SOLO UNA ECUACIÓN: Balance de masa. Cero energía.
            return new double[] { massFlowIn - massFlowOut };
        }

        public IEnumerable<IProcessVariable> GetCouplingVariables()
        {
            // 🚨 SOLO EXPORTAMOS LOS FLUJOS MÁSICOS. 
            // El Jacobiano no verá las entalpías para estos equipos.
            foreach (var inlet in _equipment.Inlets)
                yield return inlet.MassFlow;

            foreach (var outlet in _equipment.Outlets)
                yield return outlet.MassFlow;
        }
    }
    public class GenericBarrierEquipment : EquipmentBase
    {
        public GenericBarrierEquipment(string name) : base(name) { }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase1Strategies()
        {
            // ✅ No tiene ecuaciones locales simples (no propaga C ni P)
            yield break;
        }

        protected override IEnumerable<ISolverPhaseStrategy> CreatePhase2Strategies()
        {
            // ✅ No participa en red hidráulica simple
            yield break;
        }

        
    }
}

