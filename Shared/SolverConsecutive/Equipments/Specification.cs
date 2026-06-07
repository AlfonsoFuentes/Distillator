using Shared.SolverQwen.Stream;

namespace Shared.SolverConsecutive.Equipments
{

    public class Specification
    {
        public Guid Id { get; } = Guid.NewGuid();
        public IFacadeStream Source { get; set; } = null!;
        public IFacadeStream Destination { get; set; } = null!;
        public SpecVariableType VariableType { get; set; }

        // Ya NO tiene EquationType. Es una regla global.
        public Func<double, double> Formula { get; set; } = null!;

        public double GetResidual()
        {
            double sourceVal = GetVariableValue(Source, VariableType);
            double destVal = GetVariableValue(Destination, VariableType);
            return destVal - Formula(sourceVal);
        }

        private double GetVariableValue(IFacadeStream stream, SpecVariableType type)
        {
            return type switch
            {
                SpecVariableType.TotalMassFlow => stream.MassFlow.GetSolverValue(),
                SpecVariableType.TotalMolarFlow => stream.MolarFlow.GetSolverValue(),
                SpecVariableType.TotalVolumetricFlow => stream.VolumetricFlow.GetSolverValue(),
                _ => throw new NotImplementedException($"Tipo {type} no implementado")
            };
        }
    }

    public class SpecificationEquation : ISolverEquation
    {
        private readonly Specification _spec;

        public SpecificationEquation(Specification spec)
        {
            _spec = spec;
        }

        public string Name => $"Spec: {_spec.Source.Name} -> {_spec.Destination.Name}";

        // ✅ CORRECCIÓN AQUÍ: Le asignamos MassBalance por defecto
        public SolverEquationType EquationType => SolverEquationType.MassBalance;

        public List<double> Residuals => new List<double> { _spec.GetResidual() };

        public List<INewVariable> Variables => GetVariables();

        private List<INewVariable> GetVariables()
        {
            var vars = new List<INewVariable>();
            switch (_spec.VariableType)
            {
                case SpecVariableType.TotalMassFlow:
                    vars.Add(_spec.Source.MassFlow);
                    vars.Add(_spec.Destination.MassFlow);
                    break;
                case SpecVariableType.TotalMolarFlow:
                    vars.Add(_spec.Source.MolarFlow);
                    vars.Add(_spec.Destination.MolarFlow);
                    break;
                case SpecVariableType.TotalVolumetricFlow:
                    vars.Add(_spec.Source.VolumetricFlow);
                    vars.Add(_spec.Destination.VolumetricFlow);
                    break;
            }
            return vars;
        }
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
        public List<INewVariable> Variables => Equations.SelectMany(e => e.Variables).Distinct().ToList();
    }
}
