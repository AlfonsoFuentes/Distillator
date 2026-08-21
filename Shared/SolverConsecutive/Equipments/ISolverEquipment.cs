using Shared.SolverQwen.Stream;
using Shared.UnitOperations.Basiss;

namespace Shared.SolverConsecutive.Equipments
{
    // ============================================================================
    // INTERFACES Y CLASE BASE
    // ============================================================================

    public interface ISolverEquipment : IFacade
    {
        List<ISolverEquation> Equations { get; }
        IReadOnlyList<ISpecification> Specifications { get; }
        List<IFacadeStream> Inlets { get; }
        List<IFacadeStream> Outlets { get; }
        IEnumerable<ISolverEquipment> GetDownstreamEquipments();
        IEnumerable<ISolverEquipment> GetUpstreamEquipments();
        int GetComponentCount();

        List<IFacadeStream> AllStreams { get; }
    }

    public abstract class SolverEquipmentBase : ISolverEquipment, IEquipmentFacade
    {
        public List<IFacadeStream> AllStreams => Inlets.Concat(Outlets).ToList();
        public string Name { get; set; } = string.Empty;
        public ISolverTraceSink? TraceSink { get; set; }
        public abstract List<ISolverEquation> Equations { get; }

        public List<IFacadeStream> Inlets { get; private set; } = new();
        public List<IFacadeStream> Outlets { get; private set; } = new();

        public virtual IEnumerable<ISolverEquipment> GetUpstreamEquipments()
        {
            foreach (var stream in Inlets)
            {
                if (stream.EquipmentInlet != null)
                {
                    yield return stream.EquipmentInlet;
                }
            }
        }

        public virtual IEnumerable<ISolverEquipment> GetDownstreamEquipments()
        {
            foreach (var stream in Outlets)
            {
                if (stream.EquipmentOutlet != null)
                {
                    yield return stream.EquipmentOutlet;
                }
            }
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        private readonly List<ISpecification> _specifications = new();
        public IReadOnlyList<ISpecification> Specifications => _specifications.AsReadOnly();

        public SolverEquipmentBase()
        {
        }

        public virtual Task PostSolveAsync()
        {
            return Task.CompletedTask;
        }

        public void AddSpec(ISpecification spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (_specifications.Any(s => s.Id == spec.Id))
                throw new InvalidOperationException($"Spec {spec.Id} ya existe en {Name}");

            _specifications.Add(spec);
        }

        public void RemoveSpec(ISpecification spec)
        {
            if (spec != null) _specifications.Remove(spec);
        }

        public void ClearSpecs()
        {
            _specifications.Clear();
        }
        public int GetComponentCount()
        {
            return AllStreams
                .FirstOrDefault(stream => stream.Composition != null)
                ?.Composition.Components.Count ?? 0;
        }

    }


    public class MassFractionDistributorEquation : ISolverEquation
    {
        private readonly ISolverEquipment eq;

        public MassFractionDistributorEquation(ISolverEquipment eq)
        {
            this.eq = eq;
        }

        public SolverEquationTypeModifier EquationTypeModifer { get; } =
            SolverEquationTypeModifier.Regular;

        public string Name => $"{EquationType} Mass Fraction Distributor - {eq.Name}";

        public SolverEquationType EquationType => SolverEquationType.Concentration;

        public List<double> Residuals => GetResiduals();

        public List<IVariable> Variables => GetVariables();

        private List<double> GetResiduals()
        {
            var residuals = new List<double>();

            if (eq.Inlets.Count != 1 || eq.Outlets.Count == 0)
            {
                return residuals;
            }

            var inlet = eq.Inlets[0];
            var componentCount = eq.GetComponentCount();

            for (var outletIndex = 0; outletIndex < eq.Outlets.Count; outletIndex++)
            {
                var outlet = eq.Outlets[outletIndex];

                for (var componentIndex = 0; componentIndex < componentCount; componentIndex++)
                {
                    var inletFraction = inlet.Composition.Components[componentIndex]
                        .MassFraction
                        .GetSolverValue();

                    var outletFraction = outlet.Composition.Components[componentIndex]
                        .MassFraction
                        .GetSolverValue();

                    residuals.Add(inletFraction - outletFraction);
                }
            }

            return residuals;
        }

        private List<IVariable> GetVariables()
        {
            var variables = new List<IVariable>();

            if (eq.Inlets.Count != 1 || eq.Outlets.Count == 0)
            {
                return variables;
            }

            var inlet = eq.Inlets[0];
            var componentCount = eq.GetComponentCount();

            for (var componentIndex = 0; componentIndex < componentCount; componentIndex++)
            {
                variables.Add(inlet.Composition.Components[componentIndex].MassFraction);

                foreach (var outlet in eq.Outlets)
                {
                    variables.Add(outlet.Composition.Components[componentIndex].MassFraction);
                }
            }

            return variables;
        }
    }
    public class GlobalMassBalanceEquation : ISolverEquation
    {
        //Caso 1: Esta ecuacion sirve para resolver balance global de masa
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public GlobalMassBalanceEquation(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;

        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return r;
            double massin = eq.Inlets.Sum(i => i.MassFlow.GetSolverValue());
            double massout = eq.Outlets.Sum(o => o.MassFlow.GetSolverValue());
            r.Add(massin - massout);


            return r;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return v;

            foreach (var stream in eq.AllStreams)
            {
                v.Add(stream.MassFlow);
            }

            return v;
        }
    }

    public class ComponentMassBalanceEquationAllVariablesBackup : ISolverEquation
    {
        //Caso 2: Balance de masa por componente usando las variables primitivas que participan en el residual.
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public ComponentMassBalanceEquationAllVariablesBackup(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;

        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return r;

            int ncomp = eq.GetComponentCount();
            double massfraction = 0;
            double massflow = 0;
            var componentmasflow = new double[ncomp];

            foreach (var inlet in eq.Inlets)
            {
                massflow = inlet.MassFlow.GetSolverValue();

                for (int i = 0; i < ncomp; i++)
                {
                    var compo = inlet.Composition.Components[i];
                    massfraction = compo.MassFraction.GetSolverValue();

                    componentmasflow[i] += massflow * massfraction;
                }
            }

            foreach (var outlet in eq.Outlets)
            {
                massflow = outlet.MassFlow.GetSolverValue();

                for (int i = 0; i < ncomp; i++)
                {
                    var compo = outlet.Composition.Components[i];
                    massfraction = compo.MassFraction.GetSolverValue();

                    componentmasflow[i] -= massflow * massfraction;
                }
            }

            foreach (var comp in componentmasflow)
            {
                r.Add(comp);
            }

            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return v;

            int ncomp = eq.GetComponentCount();
            foreach (var stream in eq.Inlets.Concat(eq.Outlets))
            {
                v.Add(stream.MassFlow);

                for (int i = 0; i < ncomp; i++)
                {
                    var compo = stream.Composition.Components[i];
                    v.Add(compo.MassFraction);
                }
            }

            return v.Distinct().ToList();
        }
    }

    internal static class ComponentMassBalanceDof
    {
        public static bool CanSolveMassFractions(ISolverEquipment eq)
        {
            if (!HasTopology(eq)) return false;
            if (eq.AllStreams.Any(stream => !stream.MassFlow.IsDefined)) return false;

            var missingFractions = MissingMassFractions(eq);
            if (missingFractions.Count == 0) return false;

            return HasZeroDegreesOfFreedom(eq, BuildMassFractionMatrix(eq, missingFractions), missingFractions.Count);
        }

        public static bool CanSolveMassFlows(ISolverEquipment eq)
        {
            if (!HasTopology(eq)) return false;
            if (MissingMassFractions(eq).Count != 0) return false;

            var missingFlows = MissingMassFlowStreams(eq);
            if (missingFlows.Count == 0) return false;

            return HasZeroDegreesOfFreedom(eq, BuildMassFlowMatrix(eq, missingFlows), missingFlows.Count);
        }

        public static bool CanSolveMassEnergyFlows(ISolverEquipment eq)
        {
            if (!HasTopology(eq)) return false;
            if (MissingMassFractions(eq).Count != 0) return false;
            if (MissingMassEnthalpies(eq).Count != 0) return false;

            var missingFlows = MissingMassFlowStreams(eq);
            if (missingFlows.Count == 0) return false;

            int equationCount = eq.GetComponentCount() + 1;
            return HasZeroDegreesOfFreedom(equationCount, BuildMassEnergyFlowMatrix(eq, missingFlows), missingFlows.Count);
        }

        public static bool CanSolveMixed(ISolverEquipment eq)
        {
            if (!HasTopology(eq)) return false;

            var missingFlows = MissingMassFlowStreams(eq);
            var missingFractions = MissingMassFractions(eq);
            if (missingFlows.Count == 0 || missingFractions.Count == 0) return false;

            var unknownCount = missingFlows.Count + missingFractions.Count;
            var matrix = BuildMixedMatrix(eq, missingFlows, missingFractions);

            return HasZeroDegreesOfFreedom(eq, matrix, unknownCount);
        }

        private static bool HasTopology(ISolverEquipment eq)
        {
            return eq.Inlets.Count != 0 && eq.Outlets.Count != 0 && eq.GetComponentCount() != 0;
        }

        private static bool HasZeroDegreesOfFreedom(ISolverEquipment eq, double[,] matrix, int unknownCount)
        {
            return HasZeroDegreesOfFreedom(eq.GetComponentCount(), matrix, unknownCount);
        }

        private static bool HasZeroDegreesOfFreedom(int equationCount, double[,] matrix, int unknownCount)
        {
            if (unknownCount > equationCount) return false;

            var independentEquationCount = GetMatrixRank(matrix, equationCount, unknownCount);
            return unknownCount == independentEquationCount;
        }

        private static List<IFacadeStream> MissingMassFlowStreams(ISolverEquipment eq)
        {
            return eq.AllStreams
                .Where(stream => !stream.MassFlow.IsDefined)
                .Distinct()
                .ToList();
        }

        private static List<(IFacadeStream Stream, int ComponentIndex)> MissingMassFractions(ISolverEquipment eq)
        {
            var missingFractions = new List<(IFacadeStream Stream, int ComponentIndex)>();
            int ncomp = eq.GetComponentCount();

            foreach (var stream in eq.AllStreams)
            {
                for (int i = 0; i < ncomp; i++)
                {
                    if (!stream.Composition.Components[i].MassFraction.IsDefined)
                    {
                        missingFractions.Add((stream, i));
                    }
                }
            }

            return missingFractions.Distinct().ToList();
        }

        private static List<IVariable> MissingMassEnthalpies(ISolverEquipment eq)
        {
            return eq.AllStreams
                .Select(stream => (IVariable)stream.MassEnthalpy)
                .Where(variable => !variable.IsDefined)
                .Distinct()
                .ToList();
        }

        private static double[,] BuildMassFractionMatrix(
            ISolverEquipment eq,
            IReadOnlyList<(IFacadeStream Stream, int ComponentIndex)> missingFractions)
        {
            int rows = eq.GetComponentCount();
            var matrix = new double[rows, missingFractions.Count];

            for (int column = 0; column < missingFractions.Count; column++)
            {
                var missingFraction = missingFractions[column];
                matrix[missingFraction.ComponentIndex, column] =
                    GetStreamSign(eq, missingFraction.Stream) * missingFraction.Stream.MassFlow.GetSolverValue();
            }

            return matrix;
        }

        private static double[,] BuildMassFlowMatrix(
            ISolverEquipment eq,
            IReadOnlyList<IFacadeStream> missingFlows)
        {
            int rows = eq.GetComponentCount();
            var matrix = new double[rows, missingFlows.Count];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < missingFlows.Count; column++)
                {
                    var stream = missingFlows[column];
                    matrix[row, column] =
                        GetStreamSign(eq, stream) * stream.Composition.Components[row].MassFraction.GetSolverValue();
                }
            }

            return matrix;
        }

        private static double[,] BuildMassEnergyFlowMatrix(
            ISolverEquipment eq,
            IReadOnlyList<IFacadeStream> missingFlows)
        {
            int componentCount = eq.GetComponentCount();
            int rows = componentCount + 1;
            var matrix = new double[rows, missingFlows.Count];

            for (int row = 0; row < componentCount; row++)
            {
                for (int column = 0; column < missingFlows.Count; column++)
                {
                    var stream = missingFlows[column];
                    matrix[row, column] =
                        GetStreamSign(eq, stream) * stream.Composition.Components[row].MassFraction.GetSolverValue();
                }
            }

            for (int column = 0; column < missingFlows.Count; column++)
            {
                var stream = missingFlows[column];
                matrix[componentCount, column] =
                    GetStreamSign(eq, stream) * stream.MassEnthalpy.GetSolverValue();
            }

            return matrix;
        }

        private static double[,] BuildMixedMatrix(
            ISolverEquipment eq,
            IReadOnlyList<IFacadeStream> missingFlows,
            IReadOnlyList<(IFacadeStream Stream, int ComponentIndex)> missingFractions)
        {
            int rows = eq.GetComponentCount();
            var matrix = new double[rows, missingFlows.Count + missingFractions.Count];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < missingFlows.Count; column++)
                {
                    var stream = missingFlows[column];
                    matrix[row, column] =
                        GetStreamSign(eq, stream) * stream.Composition.Components[row].MassFraction.GetSolverValue();
                }
            }

            for (int column = 0; column < missingFractions.Count; column++)
            {
                var missingFraction = missingFractions[column];
                matrix[missingFraction.ComponentIndex, missingFlows.Count + column] =
                    GetStreamSign(eq, missingFraction.Stream) * missingFraction.Stream.MassFlow.GetSolverValue();
            }

            return matrix;
        }

        private static double GetStreamSign(ISolverEquipment eq, IFacadeStream stream)
        {
            if (eq.Inlets.Contains(stream)) return 1.0;
            if (eq.Outlets.Contains(stream)) return -1.0;

            return 0.0;
        }

        private static int GetMatrixRank(double[,] matrix, int rows, int columns)
        {
            const double tolerance = 1e-10;
            int rank = 0;
            int pivotRow = 0;

            for (int column = 0; column < columns && pivotRow < rows; column++)
            {
                int bestRow = pivotRow;

                for (int row = pivotRow + 1; row < rows; row++)
                {
                    if (Math.Abs(matrix[row, column]) > Math.Abs(matrix[bestRow, column]))
                    {
                        bestRow = row;
                    }
                }

                if (Math.Abs(matrix[bestRow, column]) < tolerance)
                {
                    continue;
                }

                if (bestRow != pivotRow)
                {
                    for (int swapColumn = column; swapColumn < columns; swapColumn++)
                    {
                        (matrix[pivotRow, swapColumn], matrix[bestRow, swapColumn]) =
                            (matrix[bestRow, swapColumn], matrix[pivotRow, swapColumn]);
                    }
                }

                double pivot = matrix[pivotRow, column];
                for (int row = pivotRow + 1; row < rows; row++)
                {
                    double factor = matrix[row, column] / pivot;

                    for (int eliminateColumn = column; eliminateColumn < columns; eliminateColumn++)
                    {
                        matrix[row, eliminateColumn] -= factor * matrix[pivotRow, eliminateColumn];
                    }
                }

                rank++;
                pivotRow++;
            }

            return rank;
        }
    }

    public class ComponentMassBalanceEquation : ISolverEquation
    {
        //Caso 2A: Resuelve fracciones masicas cuando todos los flujos masicos estan disponibles.
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public ComponentMassBalanceEquation(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} By Mass Fraction - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;

        public List<double> Residuals => CanSolve() ? GetResiduals() : new List<double>();
        public List<IVariable> Variables => CanSolve() ? GetVariables() : new List<IVariable>();

        bool CanSolve()
        {
            return ComponentMassBalanceDof.CanSolveMassFractions(eq);
        }

        List<double> GetResiduals()
        {
            List<double> r = new();
            int ncomp = eq.GetComponentCount();
            var componentmasflow = new double[ncomp];

            foreach (var inlet in eq.Inlets)
            {
                var massflow = inlet.MassFlow.GetSolverValue();

                for (int i = 0; i < ncomp; i++)
                {
                    componentmasflow[i] += massflow * inlet.Composition.Components[i].MassFraction.GetSolverValue();
                }
            }

            foreach (var outlet in eq.Outlets)
            {
                var massflow = outlet.MassFlow.GetSolverValue();

                for (int i = 0; i < ncomp; i++)
                {
                    componentmasflow[i] -= massflow * outlet.Composition.Components[i].MassFraction.GetSolverValue();
                }
            }

            foreach (var comp in componentmasflow)
            {
                r.Add(comp);
            }

            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            int ncomp = eq.GetComponentCount();

            foreach (var stream in eq.AllStreams)
            {
                for (int i = 0; i < ncomp; i++)
                {
                    v.Add(stream.Composition.Components[i].MassFraction);
                }
            }

            return v.Distinct().ToList();
        }

        List<IVariable> MissingMassFractions()
        {
            List<IVariable> v = new();
            int ncomp = eq.GetComponentCount();

            foreach (var stream in eq.AllStreams)
            {
                for (int i = 0; i < ncomp; i++)
                {
                    var massFraction = stream.Composition.Components[i].MassFraction;
                    if (!massFraction.IsDefined)
                    {
                        v.Add(massFraction);
                    }
                }
            }

            return v.Distinct().ToList();
        }
    }

    public class ComponentMassBalanceByMassFlowEquation : ISolverEquation
    {
        //Caso 2B: Resuelve flujos masicos cuando todas las fracciones masicas estan disponibles.
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public ComponentMassBalanceByMassFlowEquation(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} By Mass Flow - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;

        public List<double> Residuals => CanSolve() ? GetResiduals() : new List<double>();
        public List<IVariable> Variables => CanSolve() ? GetVariables() : new List<IVariable>();

        bool CanSolve()
        {
            return ComponentMassBalanceDof.CanSolveMassFlows(eq);
        }

        List<double> GetResiduals()
        {
            List<double> r = new();
            int ncomp = eq.GetComponentCount();
            var componentmasflow = new double[ncomp];

            foreach (var inlet in eq.Inlets)
            {
                var massflow = inlet.MassFlow.GetSolverValue();

                for (int i = 0; i < ncomp; i++)
                {
                    componentmasflow[i] += massflow * inlet.Composition.Components[i].MassFraction.GetSolverValue();
                }
            }

            foreach (var outlet in eq.Outlets)
            {
                var massflow = outlet.MassFlow.GetSolverValue();

                for (int i = 0; i < ncomp; i++)
                {
                    componentmasflow[i] -= massflow * outlet.Composition.Components[i].MassFraction.GetSolverValue();
                }
            }

            foreach (var comp in componentmasflow)
            {
                r.Add(comp);
            }

            return r;
        }

        List<IVariable> GetVariables()
        {
            return eq.AllStreams
                .Select(stream => (IVariable)stream.MassFlow)
                .Distinct()
                .ToList();
        }

        List<IVariable> MissingMassFlows()
        {
            return eq.AllStreams
                .Select(stream => (IVariable)stream.MassFlow)
                .Where(variable => !variable.IsDefined)
                .Distinct()
                .ToList();
        }

        List<IFacadeStream> MissingMassFlowStreams()
        {
            return eq.AllStreams
                .Where(stream => !stream.MassFlow.IsDefined)
                .Distinct()
                .ToList();
        }

        List<IVariable> MissingMassFractions()
        {
            List<IVariable> v = new();
            int ncomp = eq.GetComponentCount();

            foreach (var stream in eq.AllStreams)
            {
                for (int i = 0; i < ncomp; i++)
                {
                    var massFraction = stream.Composition.Components[i].MassFraction;
                    if (!massFraction.IsDefined)
                    {
                        v.Add(massFraction);
                    }
                }
            }

            return v.Distinct().ToList();
        }

        int GetCompositionRank(List<IFacadeStream> streams)
        {
            int rows = eq.GetComponentCount();
            int columns = streams.Count;
            var matrix = new double[rows, columns];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    matrix[row, column] = streams[column]
                        .Composition
                        .Components[row]
                        .MassFraction
                        .GetSolverValue();
                }
            }

            return GetMatrixRank(matrix, rows, columns);
        }

        static int GetMatrixRank(double[,] matrix, int rows, int columns)
        {
            const double tolerance = 1e-10;
            int rank = 0;
            int pivotRow = 0;

            for (int column = 0; column < columns && pivotRow < rows; column++)
            {
                int bestRow = pivotRow;

                for (int row = pivotRow + 1; row < rows; row++)
                {
                    if (Math.Abs(matrix[row, column]) > Math.Abs(matrix[bestRow, column]))
                    {
                        bestRow = row;
                    }
                }

                if (Math.Abs(matrix[bestRow, column]) < tolerance)
                {
                    continue;
                }

                if (bestRow != pivotRow)
                {
                    for (int swapColumn = column; swapColumn < columns; swapColumn++)
                    {
                        (matrix[pivotRow, swapColumn], matrix[bestRow, swapColumn]) =
                            (matrix[bestRow, swapColumn], matrix[pivotRow, swapColumn]);
                    }
                }

                double pivot = matrix[pivotRow, column];
                for (int row = pivotRow + 1; row < rows; row++)
                {
                    double factor = matrix[row, column] / pivot;

                    for (int eliminateColumn = column; eliminateColumn < columns; eliminateColumn++)
                    {
                        matrix[row, eliminateColumn] -= factor * matrix[pivotRow, eliminateColumn];
                    }
                }

                rank++;
                pivotRow++;
            }

            return rank;
        }
    }

    public class ComponentMassBalanceMixedEquation : ISolverEquation
    {
        //Caso 2C: Resuelve un caso mixto solo cuando los grados de libertad cierran exactamente.
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public ComponentMassBalanceMixedEquation(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} Mixed - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;

        public List<double> Residuals => CanSolve() ? GetResiduals() : new List<double>();
        public List<IVariable> Variables => CanSolve() ? GetVariables() : new List<IVariable>();

        bool CanSolve()
        {
            return ComponentMassBalanceDof.CanSolveMixed(eq);
        }

        List<double> GetResiduals()
        {
            List<double> r = new();
            int ncomp = eq.GetComponentCount();
            var componentmasflow = new double[ncomp];

            foreach (var inlet in eq.Inlets)
            {
                var massflow = inlet.MassFlow.GetSolverValue();

                for (int i = 0; i < ncomp; i++)
                {
                    componentmasflow[i] += massflow * inlet.Composition.Components[i].MassFraction.GetSolverValue();
                }
            }

            foreach (var outlet in eq.Outlets)
            {
                var massflow = outlet.MassFlow.GetSolverValue();

                for (int i = 0; i < ncomp; i++)
                {
                    componentmasflow[i] -= massflow * outlet.Composition.Components[i].MassFraction.GetSolverValue();
                }
            }

            foreach (var comp in componentmasflow)
            {
                r.Add(comp);
            }

            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            int ncomp = eq.GetComponentCount();

            foreach (var stream in eq.AllStreams)
            {
                v.Add(stream.MassFlow);

                for (int i = 0; i < ncomp; i++)
                {
                    v.Add(stream.Composition.Components[i].MassFraction);
                }
            }

            return v.Distinct().ToList();
        }

        List<IVariable> MissingMassFlows()
        {
            return eq.AllStreams
                .Select(stream => (IVariable)stream.MassFlow)
                .Where(variable => !variable.IsDefined)
                .Distinct()
                .ToList();
        }

        List<IVariable> MissingMassFractions()
        {
            List<IVariable> v = new();
            int ncomp = eq.GetComponentCount();

            foreach (var stream in eq.AllStreams)
            {
                for (int i = 0; i < ncomp; i++)
                {
                    var massFraction = stream.Composition.Components[i].MassFraction;
                    if (!massFraction.IsDefined)
                    {
                        v.Add(massFraction);
                    }
                }
            }

            return v.Distinct().ToList();
        }
    }

    public class GlobalEnergyBalanceByMassEnthalpyEquation : ISolverEquation
    {
        //Caso 3A: Resuelve una entalpia masica faltante cuando los flujos masicos ya estan disponibles.
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public GlobalEnergyBalanceByMassEnthalpyEquation(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} By Mass Enthalpy - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;

        public List<double> Residuals => CanSolve() ? GetResiduals() : new List<double>();
        public List<IVariable> Variables => CanSolve() ? GetVariables() : new List<IVariable>();

        bool CanSolve()
        {
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return false;
            if (eq.AllStreams.Any(stream => !stream.MassFlow.IsDefined)) return false;

            var missingEnthalpyStreams = eq.AllStreams
                .Where(stream => !stream.MassEnthalpy.IsDefined)
                .Distinct()
                .ToList();

            if (missingEnthalpyStreams.Count != 1) return false;

            return Math.Abs(missingEnthalpyStreams[0].MassFlow.GetSolverValue()) > 1e-10;
        }

        List<double> GetResiduals()
        {
            double energyflow = 0;

            foreach (var inlet in eq.Inlets)
            {
                energyflow += inlet.MassFlow.GetSolverValue() * inlet.MassEnthalpy.GetSolverValue();
            }

            foreach (var outlet in eq.Outlets)
            {
                energyflow -= outlet.MassFlow.GetSolverValue() * outlet.MassEnthalpy.GetSolverValue();
            }

            return new List<double> { energyflow };
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return v;

            foreach (var stream in eq.AllStreams)
            {
                v.Add(stream.MassEnthalpy);
            }

            return v.Distinct().ToList();
        }
    }

    public class GlobalMassEnergyBalanceEquation : ISolverEquation
    {
        //Caso 3: Resuelve flujos masicos usando balances de componentes y energia.
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        ISolverEquipment eq;
        public GlobalMassEnergyBalanceEquation(ISolverEquipment _eq) => eq = _eq;
        public string Name => $"{EquationType} Component Energy By Mass Flow - {eq.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;

        public List<double> Residuals => CanSolve() ? GetResiduals() : new List<double>();
        public List<IVariable> Variables => CanSolve() ? GetVariables() : new List<IVariable>();

        bool CanSolve()
        {
            return ComponentMassBalanceDof.CanSolveMassEnergyFlows(eq);
        }

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return r;

            int ncomp = eq.GetComponentCount();
            double massfraction = 0;
            double massflow = 0;
            var componentmasflow = new double[ncomp];
            double energyflow = 0;
            double massenthalpy = 0;

            foreach (var inlet in eq.Inlets)
            {
                massflow = inlet.MassFlow.GetSolverValue();
                massenthalpy = inlet.MassEnthalpy.GetSolverValue();
                energyflow += massflow * massenthalpy;

                for (int i = 0; i < ncomp; i++)
                {
                    var compo = inlet.Composition.Components[i];
                    massfraction = compo.MassFraction.GetSolverValue();

                    componentmasflow[i] += massflow * massfraction;
                }
            }

            foreach (var outlet in eq.Outlets)
            {
                massflow = outlet.MassFlow.GetSolverValue();
                massenthalpy = outlet.MassEnthalpy.GetSolverValue();
                energyflow -= massflow * massenthalpy;

                for (int i = 0; i < ncomp; i++)
                {
                    var compo = outlet.Composition.Components[i];
                    massfraction = compo.MassFraction.GetSolverValue();

                    componentmasflow[i] -= massflow * massfraction;
                }
            }

            foreach (var comp in componentmasflow)
            {
                r.Add(comp);
            }

            r.Add(energyflow);

            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return v;

            foreach (var stream in eq.AllStreams)
            {
                v.Add(stream.MassFlow);
            }

            return v.Distinct().ToList();
        }
    }

    //public class GlobalMassEnergyBalanceEquation2 : ISolverEquation
    //{
    //    //Caso 3: Esta ecuacion sirve para resolver balance global de masa y energia, se asume que la composicion de los streams es conocida, y se resuelve el flujo masico y el flujo de energia de cada stream
    //    //Se asume que la composicion de los streams es conocida, y se resuelve el flujo masico y el flujo de energia de cada stream
    //    public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
    //    ISolverEquipment eq;
    //    public GlobalMassEnergyBalanceEquation2(ISolverEquipment _eq) => eq = _eq;
    //    public string Name => $"{EquationType} - {eq.Name}";
    //    public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;

    //    public List<double> Residuals => GetResiduals();
    //    public List<IVariable> Variables => GetVariables();

    //    List<double> GetResiduals()
    //    {
    //        List<double> r = new();
    //        if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return r;
         
    //        int ncomp = eq.GetComponentCount();
    //        double massfraction = 0;
    //        double massflow = 0;
    //        var componentmasflow = new double[ncomp];
    //        double energyflow = 0;
    //        double massenthalpy = 0;
    //        foreach (var inlet in eq.Inlets)
    //        {

    //            massflow = inlet.MassFlow.GetSolverValue();
    //            massenthalpy = inlet.MassEnthalpy.GetSolverValue();
    //            energyflow += massflow * massenthalpy;

    //            for (int i = 0; i < ncomp; i++)
    //            {
    //                var compo = inlet.Composition.Components[i];
    //                massfraction = compo.MassFraction.GetSolverValue();

    //                componentmasflow[i] += massflow * massfraction;

    //            }
    //        }
    //        foreach (var outlet in eq.Outlets)
    //        {
    //            massflow = outlet.MassFlow.GetSolverValue();
    //            massenthalpy = outlet.MassEnthalpy.GetSolverValue();
    //            energyflow -= massflow * massenthalpy;
    //            for (int i = 0; i < ncomp; i++)
    //            {
    //                var compo = outlet.Composition.Components[i];
    //                massfraction = compo.MassFraction.GetSolverValue();


    //                componentmasflow[i] -= massflow * massfraction;
    //            }
    //        }
    //        foreach (var comp in componentmasflow)
    //        {
    //            r.Add(comp);
    //        }
    //        r.Add(energyflow);

    //        return r;
    //    }
    //    List<IVariable> GetVariables()
    //    {
    //        List<IVariable> v = new();
    //        if (eq.Inlets.Count == 0 || eq.Outlets.Count == 0) return v;

    //        foreach (var stream in eq.AllStreams)
    //        {
    //            v.Add(stream.MassFlow);
    //        }

    //        return v;
    //    }
    //}

}

