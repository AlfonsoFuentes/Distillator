using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics
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
        // 🔹 MÉTODOS DE CONVERSIÓN (in-place)
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Calcula fracción molar a partir de fracción másica.
        /// Requiere que TODOS los componentes tengan MassFraction definida.
        /// </summary>

    }
}
