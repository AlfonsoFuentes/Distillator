using Shared.DesignPatterns.NewFolder;
using Shared.Thermodynamics.Enums;
using Shared.Thermodynamics.Methods;
using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics
{

    public abstract class ThermodynamicBase
    {

        protected ThermodynamicBase()
        {

        }

        // State Properties
        public Temperature Temperature { get; set; } = new Temperature(0);
        public Pressure Pressure { get; set; } = new Pressure(0);

        // Extensive Properties (Allowed to be set in the leaf)
        public MolarFlow MolarFlow { get; set; } = new MolarFlow(0);
        public MassFlow MassFlow { get; set; } = new MassFlow(0);
        public VolumetricFlow VolumetricFlow { get; set; } = new VolumetricFlow(0);
        public EnergyFlow EnthalpyFlow { get; set; } = new EnergyFlow(0);

        // Intensive Properties
        public double MolecularWeight { get; set; }
        public MassDensity MassDensity { get; set; } = new MassDensity(0);
        public MolarDensity MolarDensity { get; set; } = new MolarDensity(0);
        public MolarEnergy MolarEnthalpy { get; set; } = new MolarEnergy(0);
        public MassEnergy MassEnthalpy { get; set; } = new MassEnergy(0);
        public MassEntropy MassHeatCapacity { get; set; } = new MassEntropy(0);
        public MolarEntropy MolarHeatCapacity { get; set; } = new MolarEntropy(0);
        public Viscosity Viscosity { get; set; } = new Viscosity(0);
        public ThermalConductivity ThermalConductivity { get; set; } = new ThermalConductivity(0);
        public SuperficialTension SurfaceTension { get; set; } = new SuperficialTension(0);

        // Critical & Saturation Properties
        public Pressure SaturationPressure { get; set; } = new Pressure(0);
        public Temperature SaturationTemperature { get; set; } = new Temperature(0);
        public Temperature CriticalTemperature { get; set; } = new Temperature(0);
        public Pressure CriticalPressure { get; set; } = new Pressure(0);
        public MolarVolumeSpecific CriticalMolarVolume { get; set; } = new MolarVolumeSpecific(0);

        // Acentric Factor (Vital for Peng-Robinson/Soave-Redlich-Kwong)
        public double AcentricFactor { get; set; }

        public virtual void SetTemperature(Temperature? temperature)
        {
            Temperature = temperature ?? new Temperature(0);  // ✅ Usa default si es null
        }
        public virtual void SetPressure(Pressure? pressure)
        {
            Pressure = pressure ?? new Pressure(0);  // ✅ Usa default si es null
        }




        public virtual void SetMolarFlow(MolarFlow? molarFlow)
        {
            MolarFlow = molarFlow ?? new MolarFlow(0);
        }

        public virtual void SetMassFlow(MassFlow? massFlow)
        {

            MassFlow = massFlow ?? new MassFlow(0);
        }

        public virtual void SetVolumetricFlow(VolumetricFlow? volumetricFlow)
        {
            VolumetricFlow = volumetricFlow ?? new VolumetricFlow(0);
        }



    }
    public abstract class Phase : ThermodynamicBase
    {
        public Phase() : base()
        {

        }
        protected abstract IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation { get; }

        public ThermodynamicMethodFullDto ThermoMethod { get; protected set; } = null!;
        public LiquidPhaseModel LiquidModel => ThermoMethod.LiquidModel;
        public VaporPhaseModel VapourModel => ThermoMethod.VaporModel;
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto _method)
        {
            ThermoMethod = _method;
            SetComponentsProperties(_method);
        }
        public abstract void SetComponentsProperties(ThermodynamicMethodFullDto _method);
        public bool IsMethodDefined => ThermoMethod != null;
        // 👇 NUEVO: Método público para "des-definir" desde el Facade o UI
        public void ClearThermodynamicMethod()
        {
            ClearThermodynamicMethodInternal();
        }

        // 👇 Interno: hace el trabajo sucio de limpiar
        protected virtual void ClearThermodynamicMethodInternal()
        {
            ThermoMethod = null!;
            // Las clases hijas sobrescriben para limpiar sus componentes
        }

        public override void SetTemperature(Temperature? temperature)
        {
            base.SetTemperature(temperature);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetTemperature(temperature);
            }
        }

        public override void SetPressure(Pressure? pressure)
        {
            base.SetPressure(pressure);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetPressure(pressure);
            }
        }

        public override void SetMolarFlow(MolarFlow? molarFlow)
        {
            base.SetMolarFlow(molarFlow);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetMolarFlow(molarFlow);
            }
        }

        public override void SetMassFlow(MassFlow? massFlow)
        {
            base.SetMassFlow(massFlow);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetMassFlow(massFlow);
            }
        }

        public override void SetVolumetricFlow(VolumetricFlow? volumetricFlow)
        {
            base.SetVolumetricFlow(volumetricFlow);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetVolumetricFlow(volumetricFlow);
            }
        }
        public abstract void ClearComponentsProperties();
    }

    public interface INode
    {
        Guid Id { get; }
        string Name { get; }
        PureComponentData PureComponentData { get; }
    }
    public interface ICompositionFraction
    {
        double MassFraction { get; set; } // w_i
        double MolarFraction { get; set; } // z_i, x_i, y_i
    }
    public abstract class ChemicalComponentNode : ThermodynamicBase, INode, ICompositionFraction
    {
        public Guid Id => PureComponentData?.Id ?? Guid.Empty;
        public string Name { get; private set; } = string.Empty;
        public PureComponentData PureComponentData { get; private set; } = null!;
        // 👇 NUEVO: Trackear si este componente tiene datos aplicados
        public bool IsInitialized { get; private set; }
        protected ChemicalComponentNode() : base()
        {


        }
        public LiquidPhaseModel LiquidModel { get; protected set; }
        public VaporPhaseModel VaporModel { get; protected set; }

        public void SetComponentData(PureComponentData data, LiquidPhaseModel liquidModel, VaporPhaseModel vaporModel)
        {
            PureComponentData = data;
            CriticalMolarVolume = data.CriticalVolume;
            CriticalTemperature = data.CriticalTemperature;
            CriticalPressure = data.CriticalPressure;
            MolecularWeight = data.MolecularWeight;
            Name = data.Name;

            LiquidModel = liquidModel;
            VaporModel = vaporModel;

            IsInitialized = true; // 👇 Marcamos como "listo"
        }

        // 👇 NUEVO: Limpia solo lo dependiente del método, mantiene datos puros
        public void ClearComponentData()
        {
            LiquidModel = LiquidPhaseModel.None;
            VaporModel = VaporPhaseModel.None;
            IsInitialized = false;
            PureComponentData = null!;
            // 👇 Aquí en el futuro podrías limpiar diccionarios de coeficientes:
            // MethodCoefficients?.Clear();
        }
        public double MassFraction { get; set; } // w_i
        public double MolarFraction { get; set; } // z_i, x_i, y_i

    }

    public class MainComponentNode : ChemicalComponentNode
    {
        public MainComponentNode() : base()
        {

        }
    }
    public class LiquidComponentNode : ChemicalComponentNode
    {
        public LiquidComponentNode() : base()
        {

        }
    }
    public class VaporComponentNode : ChemicalComponentNode
    {
        public VaporComponentNode() : base()
        {

        }
    }
    public class LiquidPhase : Phase
    {
        public string Name { get; init; }

        public List<LiquidComponentNode> Components { get; } = new List<LiquidComponentNode>();
        protected override IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation => Components;

        public LiquidPhase(string name = "Liquid Phase")
        {
            Name = name;
        }

        public override void SetComponentsProperties(ThermodynamicMethodFullDto method)
        {
            Components.Clear();
            foreach (var componentDto in method.Components)
            {
                var newComponent = new LiquidComponentNode();
                newComponent.SetComponentData(
                    PureComponentFactory.CreateFromDto(componentDto.FullData),
                    method.LiquidModel,
                    method.VaporModel
                );
                Components.Add(newComponent);
            }
        }

        public override void ClearComponentsProperties()
        {
            foreach (var component in Components)
            {
                component.ClearComponentData();
            }
            Components.Clear();
        }

        protected override void ClearThermodynamicMethodInternal()
        {
            base.ClearThermodynamicMethodInternal();
            ClearComponentsProperties();
        }
    }

    public class VaporPhase : Phase
    {
        public string Name { get; init; }

        // Lista fuertemente tipada: Solo acepta componentes de vapor
        public List<VaporComponentNode> Components { get; } = new List<VaporComponentNode>();
        protected override IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation => Components;
        public VaporPhase(string name = "Vapor Phase")
        {
            Name = name;
        }
        public override void SetComponentsProperties(ThermodynamicMethodFullDto method)
        {
            Components.Clear();  // 👈 AGREGAR ESTA LÍNEA AL INICIO (línea 1 del método)

            foreach (var componentDto in method.Components)
            {
                var newComponent = new VaporComponentNode();
                newComponent.SetComponentData(
                    PureComponentFactory.CreateFromDto(componentDto.FullData),
                    method.LiquidModel,
                    method.VaporModel
                );
                Components.Add(newComponent);
            }
        }

        public override void ClearComponentsProperties()
        {
            foreach (var component in Components)
            {
                component.ClearComponentData();
            }
            Components.Clear();
        }

        protected override void ClearThermodynamicMethodInternal()
        {
            base.ClearThermodynamicMethodInternal();
            ClearComponentsProperties();
        }

    }
    // MaterialStream extiende Phase para reutilizar el contrato de propagación 
    // de métodos termodinámicos, aunque conceptualmente "contiene" fases.
    public class MaterialStream : Phase
    {
        public string Name { get; init; }

        // 1. Composición Global (La alimentación antes de separarse en fases)
        public List<MainComponentNode> Components { get; } = new List<MainComponentNode>();

        // 2. Las fases termodinámicas resultantes
        public LiquidPhase LiquidPhase { get; }
        public VaporPhase VaporPhase { get; }
        public double VaporFraction { get; private set; }
        public MaterialStream(string name = "New Stream")
        {
            Name = name;

            // Instanciamos las fases desde el inicio para evitar NullReferenceExceptions
            LiquidPhase = new LiquidPhase($"{name} - Liquid");
            VaporPhase = new VaporPhase($"{name} - Vapor");
        }
        public void SetVaporFraction(double? vaporFraction)
        {
            // 👇 VaporFraction es heredado de Phase como propiedad virtual
            VaporFraction = vaporFraction ?? 0;
        }
        public override void SetComponentsProperties(ThermodynamicMethodFullDto method)
        {
            Components.Clear();  // 👈 AGREGAR ESTA LÍNEA AL INICIO (línea 1 del método)

            foreach (var componentDto in method.Components)
            {
                var newComponent = new MainComponentNode();
                newComponent.SetComponentData(
                    PureComponentFactory.CreateFromDto(componentDto.FullData),
                    method.LiquidModel,
                    method.VaporModel
                );
                Components.Add(newComponent);
            }

            LiquidPhase.SetThermodynamicMethod(method);
            VaporPhase.SetThermodynamicMethod(method);
        }

        // 👇 NUEVO: Limpia componentes globales
        public override void ClearComponentsProperties()
        {
            foreach (var component in Components)
            {
                component.ClearComponentData();
            }
            Components.Clear();
        }

        // 👇 NUEVO: Limpia TODO el árbol en cascada
        protected override void ClearThermodynamicMethodInternal()
        {
            base.ClearThermodynamicMethodInternal();

            ClearComponentsProperties();        // Limpia componentes globales
            LiquidPhase.ClearThermodynamicMethod();  // Limpia fase líquida
            VaporPhase.ClearThermodynamicMethod();   // Limpia fase vapor
        }
        // 👇 OVERRIDE: retorna SOLO los componentes globales para la propagación base
        protected override IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation => Components;

        // 👇 PERO: sobrescribimos SetX() para propagar TAMBIÉN a las fases internas
        public override void SetTemperature(Temperature? temperature)
        {
            base.SetTemperature(temperature);  // 👈 Propaga a Components (globales) vía ComponentsForPropagation
            LiquidPhase.SetTemperature(temperature);  // 👈 Propaga a fase líquida
            VaporPhase.SetTemperature(temperature);   // 👈 Propaga a fase vapor
        }

        public override void SetPressure(Pressure? pressure)
        {
            base.SetPressure(pressure);
            LiquidPhase.SetPressure(pressure);
            VaporPhase.SetPressure(pressure);
        }

        public override void SetMolarFlow(MolarFlow? molarFlow)
        {
            base.SetMolarFlow(molarFlow);
            LiquidPhase.SetMolarFlow(molarFlow);
            VaporPhase.SetMolarFlow(molarFlow);
        }

        public override void SetMassFlow(MassFlow? massFlow)
        {
            base.SetMassFlow(massFlow);
            LiquidPhase.SetMassFlow(massFlow);
            VaporPhase.SetMassFlow(massFlow);
        }

        public override void SetVolumetricFlow(VolumetricFlow? volumetricFlow)
        {
            base.SetVolumetricFlow(volumetricFlow);
            LiquidPhase.SetVolumetricFlow(volumetricFlow);
            VaporPhase.SetVolumetricFlow(volumetricFlow);
        }
        public void SetCompositionData(StreamComposition _streamComposition)
        {
            if (_streamComposition?.Components == null) return;  // 👈 Guarda contra null

            foreach (var comp in _streamComposition.Components)
            {
                var localComponente = Components.FirstOrDefault(x => x.Id == comp.ComponentId);
                if (localComponente != null)
                {
                    // 👇 Usar null-conditional para evitar excepciones si el DTO no tiene valor
                    if (comp.MassFraction.HasValue)
                        localComponente.MassFraction = comp.MassFraction.Value;

                    if (comp.MolarFraction.HasValue)
                        localComponente.MolarFraction = comp.MolarFraction.Value;
                }
            }
        }



    }
    // 1. Enum para trackear el tipo de entrada
    public enum ComponentInputType
    {
        None = 0,           // Nada definido
        MassFraction = 1,   // % másico definido (w_i)
        MolarFraction = 2,  // % molar definido (z_i, x_i, y_i)
        MolarFlow = 3,      // Flujo molar definido
        MassFlow = 4,       // Flujo másico definido
        VolumetricFlow = 5  // Flujo volumétrico definido
    }

    // 2. DTO para entrada desde UI
    public class ComponentComposition
    {
        // ─────────────────────────────────────────────────────────
        // 🔹 IDENTIFICACIÓN
        // ─────────────────────────────────────────────────────────
        public Guid ComponentId { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public double MolecularWeight { get; set; }  // Necesario para conversiones

        // ─────────────────────────────────────────────────────────
        // 🔹 TIPO DE ENTRADA
        // ─────────────────────────────────────────────────────────


        // ─────────────────────────────────────────────────────────
        // 🔹 FRACCIONES
        // ─────────────────────────────────────────────────────────
        public double? MassFraction { get; set; }
        public double? MolarFraction { get; set; }

        // ─────────────────────────────────────────────────────────
        // 🔹 FLUJOS
        // ─────────────────────────────────────────────────────────
        public MassFlow? MassFlowValue { get; set; }
        public MolarFlow? MolarFlowValue { get; set; }
        public VolumetricFlow? VolumetricFlowValue { get; set; }

        // ─────────────────────────────────────────────────────────
        // 🔹 CONSTRUCTOR
        // ─────────────────────────────────────────────────────────
        public ComponentComposition()
        {
        }

        public ComponentComposition(Guid componentId, string componentName, double molecularWeight)
        {
            ComponentId = componentId;
            ComponentName = componentName;
            MolecularWeight = molecularWeight;
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 MÉTODOS DE CONVERSIÓN (in-place)
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Calcula fracción molar a partir de fracción másica.
        /// Requiere que TODOS los componentes tengan MassFraction definida.
        /// </summary>

    }
    public class StreamComposition
    {
        public ComponentInputType InputType { get; set; } = ComponentInputType.None;
        public List<ComponentComposition> Components { get; set; } = new();

        // ─────────────────────────────────────────────────────────
        // 🔹 CONSTRUCTOR
        // ─────────────────────────────────────────────────────────
        public StreamComposition()
        {
        }

        public StreamComposition(List<ComponentComposition> components)
        {
            Components = components;
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 MÉTODOS DE CONVERSIÓN (delegan a ComponentComposition)
        // ─────────────────────────────────────────────────────────

        public void CalculateMolarFractionsFromMass()
        {
            // 1. Validar que todos tengan MassFraction
            if (Components.Any(c => !c.MassFraction.HasValue))
                return;

            // 2. 👇 CLAVE: Calcular suma de fracciones másicas
            var massSum = Components.Sum(c => c.MassFraction!.Value);

            // 3. 👇 Solo calcular si la suma está cerca de 1.0 (tolerancia 1%)
            if (massSum < 0.99 || massSum > 1.01)
                return;  // 👇 NO calcular si suma ≠ 1.0

            // 4. Calcular suma de (w_i / MW_i) para normalización molar
            var sum = Components.Sum(c => c.MassFraction!.Value / c.MolecularWeight);

            if (sum <= 0)
                return;

            // 5. Calcular z_i = (w_i / MW_i) / sum
            foreach (var component in Components)
            {
                component.MolarFraction = (component.MassFraction!.Value / component.MolecularWeight) / sum;
            }
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 CONVERSIÓN: Molar → Mass (CORREGIDO)
        // ─────────────────────────────────────────────────────────
        public void CalculateMassFractionsFromMolar()
        {
            // 1. Validar que todos tengan MolarFraction
            if (Components.Any(c => !c.MolarFraction.HasValue))
                return;

            // 2. 👇 CLAVE: Calcular suma de fracciones molares
            var moleSum = Components.Sum(c => c.MolarFraction!.Value);

            // 3. 👇 Solo calcular si la suma está cerca de 1.0 (tolerancia 1%)
            if (moleSum < 0.99 || moleSum > 1.01)
                return;  // 👇 NO calcular si suma ≠ 1.0

            // 4. Calcular suma de (z_i * MW_i) para normalización másica
            var sum = Components.Sum(c => c.MolarFraction!.Value * c.MolecularWeight);

            if (sum <= 0)
                return;

            // 5. Calcular w_i = (z_i * MW_i) / sum
            foreach (var component in Components)
            {
                component.MassFraction = (component.MolarFraction!.Value * component.MolecularWeight) / sum;
            }
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 VALIDACIÓN: Suma de fracciones ≈ 1.0
        // ─────────────────────────────────────────────────────────
        public bool ValidateFractionsSum()
        {
            if (InputType == ComponentInputType.MassFraction)
            {
                var sum = Components.Sum(c => c.MassFraction ?? 0);
                return sum >= 0.99 && sum <= 1.01;  // Tolerancia 1%
            }
            else if (InputType == ComponentInputType.MolarFraction)
            {
                var sum = Components.Sum(c => c.MolarFraction ?? 0);
                return sum >= 0.99 && sum <= 1.01;  // Tolerancia 1%
            }
            return false;  // InputType = None → no válido
        }

        /// <summary>
        /// Normaliza las fracciones para que sumen exactamente 1.0
        /// </summary>
        public void NormalizeFractions()
        {
            var sum = InputType switch
            {
                ComponentInputType.MassFraction => Components.Sum(c => c.MassFraction ?? 0),
                ComponentInputType.MolarFraction => Components.Sum(c => c.MolarFraction ?? 0),
                _ => 0
            };

            if (sum <= 0)
                return;

            foreach (var component in Components)
            {
                if (InputType == ComponentInputType.MassFraction)
                    component.MassFraction = (component.MassFraction ?? 0) / sum;
                else if (InputType == ComponentInputType.MolarFraction)
                    component.MolarFraction = (component.MolarFraction ?? 0) / sum;
            }
        }  // En StreamComposition.cs
        public bool IsEthanolWaterMixture()
        {
            if (Components?.Count != 2) return false;

            var names = Components.Select(c => c.ComponentName.ToLower()).OrderBy(n => n).ToList();
            return names.Contains("ethanol") && names.Contains("water");
            // O por ComponentId si es más confiable:
            // return Components.Any(c => c.ComponentId == EthanolGuid) && 
            //        Components.Any(c => c.ComponentId == WaterGuid);
        }
    }
}
