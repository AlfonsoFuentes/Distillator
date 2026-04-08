using Shared.Thermodynamics.ControlledVariables;

namespace Shared.UnitOperations.Streams
{
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
            if (massSum < 99 || massSum > 101)
                return;  // 👇 NO calcular si suma ≠ 1.0

            // 4. Calcular suma de (w_i / MW_i) para normalización molar
            var sum = Components.Sum(c => c.MassFraction!.Value / c.MolecularWeight);

            if (sum <= 0)
                return;

            // 5. Calcular z_i = (w_i / MW_i) / sum
            foreach (var component in Components)
            {
                component.MolarFraction = (component.MassFraction!.Value / component.MolecularWeight) / sum * 100;
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
            if (moleSum < 99 || moleSum > 101)
                return;  // 👇 NO calcular si suma ≠ 1.0

            // 4. Calcular suma de (z_i * MW_i) para normalización másica
            var sum = Components.Sum(c => c.MolarFraction!.Value * c.MolecularWeight);

            if (sum <= 0)
                return;

            // 5. Calcular w_i = (z_i * MW_i) / sum
            foreach (var component in Components)
            {
                component.MassFraction = (component.MolarFraction!.Value * component.MolecularWeight) / sum * 100;
            }
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 VALIDACIÓN: Suma de fracciones ≈ 1.0
        // ─────────────────────────────────────────────────────────


        /// <summary>
        /// Normaliza las fracciones para que sumen exactamente 1.0
        /// </summary>

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
