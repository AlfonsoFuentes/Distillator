using Shared.SolverQwen.Stream;

using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{
    public interface ISpecification
    {
        Guid Id { get; }
        string Name { get; } // 🔥 NUEVO
        SpecificationType Type { get; }
        SolverEquationType TargetEquationType { get; }
        bool CanEvaluate { get; }
        IReadOnlyCollection<IFacadeStream> AssociatedStreams { get; }

        double GetResidual();
        List<IVariable> GetVariables();
    }
    public enum SpecificationType
    {
        None,
        Multiplier,
        ComponentMassFlowMultiplier,
        Formula,
    }
    public abstract class StreamSpecificationBase : ISpecification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public abstract SpecificationType Type { get; }
        public string Name => $"Spec: {Source?.Name} -> {Destination?.Name}";
        public IFacadeStream Source { get; set; } = null!;
        public IFacadeStream Destination { get; set; } = null!;
        public SpecVariableType VariableType { get; set; }
        public virtual IReadOnlyCollection<IFacadeStream> AssociatedStreams => [Source, Destination];
        public virtual bool CanEvaluate =>
            Source != null &&
            Destination != null &&
            !ReferenceEquals(Source, Destination);

        // Obligamos al hijo a definir cómo se calcula su residual matemático
        public abstract double GetResidual();

        // Hacemos el método 'virtual'. 
        // Por defecto hace esto, pero si un hijo necesita algo raro, lo puede sobreescribir (override).
        protected virtual double GetVariableValue(IFacadeStream stream, SpecVariableType type)
        {
            return type switch
            {
                SpecVariableType.TotalMassFlow => stream.MassFlow.GetSolverValue(),
                SpecVariableType.TotalMolarFlow => stream.MolarFlow.GetSolverValue(),
                SpecVariableType.TotalVolumetricFlow => stream.VolumetricFlow.GetSolverValue(),
                _ => throw new NotImplementedException($"Tipo {type} no implementado")
            };
        }
        public List<IVariable> GetVariables()
        {
            var vars = new List<IVariable>();
            switch (VariableType)
            {
                case SpecVariableType.TotalMassFlow:
                    vars.Add(Source.MassFlow);
                    vars.Add(Destination.MassFlow);
                    break;
                case SpecVariableType.TotalMolarFlow:
                    vars.Add(Source.MolarFlow);
                    vars.Add(Destination.MolarFlow);
                    break;
                case SpecVariableType.TotalVolumetricFlow:
                    vars.Add(Source.VolumetricFlow);
                    vars.Add(Destination.VolumetricFlow);
                    break;
            }
            return vars;
        }
        public SolverEquationType TargetEquationType => VariableType switch
        {
            SpecVariableType.TotalMassFlow => SolverEquationType.MassBalance,
            SpecVariableType.TotalMolarFlow => SolverEquationType.MassBalance,
            // Si luego añades entalpía o presión, lo agregas aquí
            _ => SolverEquationType.MassBalance
        };
    }
    public class MultiplierSpecification : StreamSpecificationBase
    {
        public override SpecificationType Type => SpecificationType.Multiplier;
        public double Multiplier { get; set; } = 1.0;

        public override double GetResidual()
        {
            double sourceVal = GetVariableValue(Source, VariableType);
            double destVal = GetVariableValue(Destination, VariableType);

            // El residual exacto de un multiplicador: dest - (source * M) = 0
            return destVal - (sourceVal * Multiplier);
        }
    }

    public class ComponentMassFlowMultiplierSpecification : StreamSpecificationBase
    {
        public ComponentMassFlowMultiplierSpecification()
        {
            VariableType = SpecVariableType.TotalMassFlow;
        }

        public override SpecificationType Type => SpecificationType.ComponentMassFlowMultiplier;
        public Guid ComponentId { get; set; }
        public Percentage Recovery { get; set; } = new(100, PercentageUnits.Percentage);
        public override bool CanEvaluate
        {
            get
            {
                if (!base.CanEvaluate || ComponentId == Guid.Empty)
                {
                    return false;
                }

                var recovery = Recovery.GetValue(PercentageUnits.Percentage);
                if (!double.IsFinite(recovery) || recovery < 0 || recovery > 100)
                {
                    return false;
                }

                var sourceComponent = FindComponent(Source);
                var destinationComponent = FindComponent(Destination);
                if (sourceComponent == null || destinationComponent == null)
                {
                    return false;
                }

                return IsUsableMassFraction(sourceComponent)
                    && IsUsableMassFraction(destinationComponent, requirePositiveValue: true);
            }
        }

        public override double GetResidual()
        {
            var sourceComponent = FindComponent(Source)!;
            var destinationComponent = FindComponent(Destination)!;

            var sourceMassFraction = GetMassFraction(sourceComponent);
            var destinationMassFraction = GetMassFraction(destinationComponent);
            var recoveryFraction = Recovery.GetValue(PercentageUnits.Percentage) / 100.0;

            var sourceComponentMassFlow = Source.MassFlow.GetSolverValue() * sourceMassFraction;
            var destinationComponentMassFlow = Destination.MassFlow.GetSolverValue() * destinationMassFraction;

            return destinationComponentMassFlow - (sourceComponentMassFlow * recoveryFraction);
        }

        private ComponentFacade? FindComponent(IFacadeStream stream) =>
            stream.Composition.Components.SingleOrDefault(component => component.Id == ComponentId);

        private static double GetMassFraction(ComponentFacade component)
            => component.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;

        private static bool IsUsableMassFraction(
            ComponentFacade component,
            bool requirePositiveValue = false)
        {
            if (!component.MassFraction.IsDefined)
            {
                return false;
            }

            var value = component.MassFraction.Value.GetValue(PercentageUnits.Percentage);
            return double.IsFinite(value)
                && value >= 0
                && value <= 100
                && (!requirePositiveValue || value > 0);
        }
    }

    public class SpecificationEquation : ISolverEquation
    {
        private readonly ISpecification _spec; // 🔥 AHORA ACEPTA LA INTERFAZ PURA

        public SpecificationEquation(ISpecification spec)
        {
            _spec = spec;
        }

        public string Name => _spec.Name; // 🔥 USA EL NOMBRE DE LA INTERFAZ

        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public bool CanEvaluate => _spec.CanEvaluate;
        public List<double> Residuals => new List<double> { _spec.GetResidual() };
        public List<IVariable> Variables => _spec.GetVariables();
        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Spec;

    }

    public class CompositeEquation : ISolverEquation
    {
        // Pública para poder extraer las ecuaciones internas si convergen
        public readonly List<ISolverEquation> Equations;

        public CompositeEquation(List<ISolverEquation> equations)
        {
            Equations = equations;
        }

        public string Name => "Clúster: [" + string.Join(" | ", Equations.Select(e => e.Name)) + "]";

        public SolverEquationType EquationType => Equations.First().EquationType;

        // Une todos los residuos
        public List<double> Residuals => Equations.SelectMany(e => e.Residuals).ToList();

        // Une todas las variables sin duplicar las que están conectadas (ej. Destilado o Reflujo)
        public List<IVariable> Variables => Equations.SelectMany(e => e.Variables).Distinct().ToList();
        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Spec;
    }
    public class CompositeEquationEquipmentList : ISolverEquation
    {
        // Pública para poder extraer las ecuaciones internas si convergen
        public readonly List<ISolverEquation> Equations = new();

        public CompositeEquationEquipmentList()
        {

        }
        public void AddEquation(ISolverEquation _eq) => Equations.Add(_eq);
        public string Name => "Clúster: [" + string.Join(" | ", Equations.Select(e => e.Name)) + "]";

        public SolverEquationType EquationType => SolverEquationType.Specification;

        // Une todos los residuos
        public List<double> Residuals => Equations.SelectMany(e => e.Residuals).ToList();

        // Une todas las variables sin duplicar las que están conectadas (ej. Destilado o Reflujo)
        public List<IVariable> Variables => Equations.SelectMany(e => e.Variables).Distinct().ToList();

        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Spec;
    }
}
