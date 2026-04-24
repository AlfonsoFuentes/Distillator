using UnitSystem;

namespace Shared.Thermodynamics.ControlledVariables
{
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
        public MassFlow? MassFlowValue { get; set; } = new MassFlow(0);
        public MolarFlow? MolarFlowValue { get; set; }      =new MolarFlow(0);
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
        public ComponentComposition Clone()
        {
            var clone = new ComponentComposition(this.ComponentId, this.ComponentName, this.MolecularWeight)
            {
                MassFraction = this.MassFraction,
                MolarFraction = this.MolarFraction
            };

            // 👇 CLONACIÓN PROFUNDA REAL PARA OBJETOS 'AMOUNT'
            // Instanciamos objetos 100% nuevos en memoria copiando el Value y el UnitName

            if (this.MassFlowValue != null)
            {
                clone.MassFlowValue = new MassFlow(this.MassFlowValue.Value, this.MassFlowValue.UnitName);
            }

            if (this.MolarFlowValue != null)
            {
                clone.MolarFlowValue = new MolarFlow(this.MolarFlowValue.Value, this.MolarFlowValue.UnitName);
            }

            if (this.VolumetricFlowValue != null)
            {
                clone.VolumetricFlowValue = new VolumetricFlow(this.VolumetricFlowValue.Value, this.VolumetricFlowValue.UnitName);
            }

            return clone;
        }

    }
}
