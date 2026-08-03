using Shared.PropertiesDtos.Methods;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.ControlledVariables;
using UnitSystem;

namespace Shared.SolverQwen.Stream
{
    public enum CompositionSource
    {
        None,    // Sin composición definida
        UI,      // Definida por usuario
        Solver   // Calculada/heredada del solver
    }
    public class CompositionOrchestrator
    {
       

        public ComponentInputType InputType { get; set; } = ComponentInputType.None;



        public CompositionSource Source => DetectSource();

        private CompositionSource DetectSource()
        {
            if (!_components.Any()) return CompositionSource.None;

            // 🔥 PRIORIDAD 1: Si el usuario definió algo, es UI (sin importar si el solver recalculó)
            if (InputType != ComponentInputType.None)
                return CompositionSource.UI;

            // PRIORIDAD 2: Si no hay input de usuario pero hay valores calculados
            bool anyFromSolver = _components.Any(c =>
                c.MassFraction.DataProcedence == VariableDefinedBy.StreamCalculated ||
                c.MassFraction.DataProcedence == VariableDefinedBy.Solver ||
                c.MassFraction.DataProcedence == VariableDefinedBy.Specification ||
                c.MolarFraction.DataProcedence == VariableDefinedBy.StreamCalculated ||
                c.MolarFraction.DataProcedence == VariableDefinedBy.Solver ||
                c.MolarFraction.DataProcedence == VariableDefinedBy.Specification);

            if (anyFromSolver) return CompositionSource.Solver;

            return CompositionSource.None;
        }

        private readonly IReadOnlyList<ComponentFacade> _components;

        public List<ComponentFacade> Components => _components.ToList();
        public void Clear()
        {
            if (_components.Count == 0) return;


            foreach (var component in _components)
            {
                component.MolarFraction.Clear(component.MolarFraction.DataProcedence);
                component.MassFlow.Clear(component.MassFlow.DataProcedence);
                component.MolarFlow.Clear(component.MolarFlow.DataProcedence);
                component.MassFraction.Clear(component.MassFraction.DataProcedence);
            }

            // 🔥 CRÍTICO: Resetear el InputType cuando se limpia toda la composición
            InputType = ComponentInputType.None;


            // 🔥 Notificar a los suscriptores
            CompositionChanged();
        }

        public event Action OnCompositionChanged = null!;

        // ✅ Detección de Estado Efímero Dinámico
        public bool HasChanged => _components.Any(c =>
            c.MassFraction.HasChanged ||
            c.MolarFraction.HasChanged ||
            c.MassFlow.HasChanged ||
            c.MolarFlow.HasChanged);

        public CompositionOrchestrator(IReadOnlyList<ComponentFacade> components)
        {
            _components = components ?? throw new ArgumentNullException(nameof(components));
        }

        public void CompositionChanged()
        {
            OnCompositionChanged?.Invoke();
        }

        public bool ValidateMassFractions(out string error)
        {
            error = null!;
            if (!_components.Any()) { error = "No components"; return false; }

            double sum = _components.Where(c => c.MassFraction.IsDefined)
                                    .Sum(c => c.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0);

            if (sum == 0)
            {
                error = "No mass fractions defined";
                return false;
            }

            if (Math.Abs(sum - 1.0) > 1e-6)
            {
                error = $"Mass fractions sum {sum * 100:F2}% (expected 100%)";
                return false;
            }
            return true;
        }

        public bool ValidateMoleFractions(out string error)
        {
            error = null!;
            if (!_components.Any()) { error = "No components"; return false; }

            double sum = _components.Where(c => c.MolarFraction.IsDefined)
                                    .Sum(c => c.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0);

            if (sum == 0)
            {
                error = "No mole fractions defined";
                return false;
            }

            if (Math.Abs(sum - 1.0) > 1e-6)
            {
                error = $"Mole fractions sum {sum * 100:F2}% (expected 100%)";
                return false;
            }
            return true;
        }

        public bool IsValid
        {
            get
            {
                if (!_components.Any()) return false;

                bool allMolarDefined = _components.All(c => c.MolarFraction.IsDefined);
                bool allMassDefined = _components.All(c => c.MassFraction.IsDefined);

                if (!allMolarDefined && !allMassDefined) return false;

                if (allMassDefined && !ValidateMassFractions(out _)) return false;
                if (allMolarDefined && !ValidateMoleFractions(out _)) return false;

                return true;
            }
        }

    }


    
}
