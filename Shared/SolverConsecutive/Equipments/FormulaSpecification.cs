using Shared.Results;
using Shared.SolverQwen.Stream;
using System.Globalization;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{
    public readonly record struct FormulaDimension(int MassFlowPower)
    {
        public static FormulaDimension Dimensionless => new(0);
        public static FormulaDimension MassFlow => new(1);

        public static FormulaDimension operator *(FormulaDimension left, FormulaDimension right) =>
            new(left.MassFlowPower + right.MassFlowPower);

        public static FormulaDimension operator /(FormulaDimension left, FormulaDimension right) =>
            new(left.MassFlowPower - right.MassFlowPower);

        public override string ToString() => MassFlowPower switch
        {
            0 => "Dimensionless",
            1 => "Mass Flow",
            _ => $"Mass Flow^{MassFlowPower}"
        };
    }

    public abstract class FormulaExpressionNode
    {
        public abstract FormulaDimension Dimension { get; }
        public abstract IEnumerable<IVariable> Variables { get; }
        public abstract IEnumerable<IFacadeStream> Streams { get; }
        public abstract bool TryEvaluate(out double value);
        public abstract string ToFormulaText();
    }

    public sealed class FormulaConstantNode(double value) : FormulaExpressionNode
    {
        public override FormulaDimension Dimension => FormulaDimension.Dimensionless;
        public override IEnumerable<IVariable> Variables => [];
        public override IEnumerable<IFacadeStream> Streams => [];

        public override bool TryEvaluate(out double result)
        {
            result = value;
            return double.IsFinite(result);
        }

        public override string ToFormulaText() => value.ToString("G17", CultureInfo.InvariantCulture);
    }

    public sealed class StreamMassFlowNode(IFacadeStream stream) : FormulaExpressionNode
    {
        public override FormulaDimension Dimension => FormulaDimension.MassFlow;
        public override IEnumerable<IVariable> Variables => [stream.MassFlow];
        public override IEnumerable<IFacadeStream> Streams => [stream];

        public override bool TryEvaluate(out double value)
        {
            value = stream.MassFlow.GetSolverValue();
            return double.IsFinite(value);
        }

        public override string ToFormulaText() => $"{stream.Name}.MassFlow";
    }

    public sealed class ComponentMassFlowNode(
        IFacadeStream stream,
        ComponentFacade component) : FormulaExpressionNode
    {
        public override FormulaDimension Dimension => FormulaDimension.MassFlow;
        public override IEnumerable<IVariable> Variables => [stream.MassFlow];
        public override IEnumerable<IFacadeStream> Streams => [stream];

        public override bool TryEvaluate(out double value)
        {
            value = double.NaN;
            if (!component.MassFraction.IsDefined)
            {
                return false;
            }

            var massFraction = component.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
            if (!double.IsFinite(massFraction) || massFraction < 0 || massFraction > 1)
            {
                return false;
            }

            value = stream.MassFlow.GetSolverValue() * massFraction;
            return double.IsFinite(value);
        }

        public override string ToFormulaText() => $"{stream.Name}.Component.{component.Name}.MassFlow";
    }

    public sealed class FormulaUnaryNode(
        char operation,
        FormulaExpressionNode operand) : FormulaExpressionNode
    {
        public override FormulaDimension Dimension => operand.Dimension;
        public override IEnumerable<IVariable> Variables => operand.Variables;
        public override IEnumerable<IFacadeStream> Streams => operand.Streams;

        public override bool TryEvaluate(out double value)
        {
            if (!operand.TryEvaluate(out var operandValue))
            {
                value = double.NaN;
                return false;
            }

            value = operation == '-' ? -operandValue : operandValue;
            return double.IsFinite(value);
        }

        public override string ToFormulaText() => $"{operation}{FormatOperand(operand)}";

        private static string FormatOperand(FormulaExpressionNode node) =>
            node is FormulaBinaryNode ? $"({node.ToFormulaText()})" : node.ToFormulaText();
    }

    public sealed class FormulaBinaryNode : FormulaExpressionNode
    {
        private const double DivisionTolerance = 1e-12;
        private readonly char _operation;
        private readonly FormulaExpressionNode _left;
        private readonly FormulaExpressionNode _right;

        public FormulaBinaryNode(
            char operation,
            FormulaExpressionNode left,
            FormulaExpressionNode right)
        {
            _operation = operation;
            _left = left;
            _right = right;
            Dimension = operation switch
            {
                '+' or '-' => left.Dimension,
                '*' => left.Dimension * right.Dimension,
                '/' => left.Dimension / right.Dimension,
                _ => FormulaDimension.Dimensionless
            };
        }

        public override FormulaDimension Dimension { get; }
        public override IEnumerable<IVariable> Variables => _left.Variables.Concat(_right.Variables);
        public override IEnumerable<IFacadeStream> Streams => _left.Streams.Concat(_right.Streams);

        public override bool TryEvaluate(out double value)
        {
            value = double.NaN;
            if (!_left.TryEvaluate(out var leftValue) || !_right.TryEvaluate(out var rightValue))
            {
                return false;
            }

            if (_operation == '/' && Math.Abs(rightValue) <= DivisionTolerance)
            {
                return false;
            }

            value = _operation switch
            {
                '+' => leftValue + rightValue,
                '-' => leftValue - rightValue,
                '*' => leftValue * rightValue,
                '/' => leftValue / rightValue,
                _ => double.NaN
            };

            return double.IsFinite(value);
        }

        public override string ToFormulaText() =>
            $"{FormatOperand(_left)}{_operation}{FormatOperand(_right)}";

        private static string FormatOperand(FormulaExpressionNode node) =>
            node is FormulaBinaryNode ? $"({node.ToFormulaText()})" : node.ToFormulaText();
    }

    public sealed class FormulaEquationExpression(
        FormulaExpressionNode left,
        FormulaExpressionNode right)
    {
        public IReadOnlyCollection<IFacadeStream> LeftStreams { get; } = left.Streams
            .Distinct()
            .ToList();

        public IReadOnlyCollection<IVariable> LeftVariables { get; } = left.Variables
            .Distinct()
            .ToList();

        public IReadOnlyCollection<IVariable> Variables { get; } = left.Variables
            .Concat(right.Variables)
            .Distinct()
            .ToList();

        public IReadOnlyCollection<IFacadeStream> Streams { get; } = left.Streams
            .Concat(right.Streams)
            .Distinct()
            .ToList();

        public FormulaDimension Dimension => left.Dimension;

        public bool TryGetResidual(out double residual)
        {
            residual = double.NaN;
            if (!left.TryEvaluate(out var leftValue) || !right.TryEvaluate(out var rightValue))
            {
                return false;
            }

            residual = leftValue - rightValue;
            return double.IsFinite(residual);
        }

        public string ToFormulaText() => $"{left.ToFormulaText()}={right.ToFormulaText()}";
    }

    public sealed class FormulaSpecification : ISpecification
    {
        public FormulaSpecification(string formula, FormulaEquationExpression equation)
        {
            Formula = formula;
            Equation = equation;
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public string? DefinedByUserId { get; set; }
        public string? DefinedByUserName { get; set; }
        public DateTime? DefinedAtUtc { get; set; }
        public string Name => $"Formula: {Formula}";
        public SpecificationType Type => SpecificationType.Formula;
        public SolverEquationType TargetEquationType => SolverEquationType.MassBalance;
        public IReadOnlyCollection<IFacadeStream> AssociatedStreams => Equation.Streams;
        public string Formula { get; }
        public FormulaEquationExpression Equation { get; }
        public bool CanEvaluate => Equation.TryGetResidual(out _);

        public double GetResidual() =>
            Equation.TryGetResidual(out var residual) ? residual : double.NaN;

        public List<IVariable> GetVariables() => Equation.Variables.ToList();

        public List<IVariable> GetTargetVariables() => Equation.LeftVariables.ToList();
    }

    public sealed class FormulaParser
    {
        private readonly string _text;
        private readonly List<IFacadeStream> _streams;
        private int _position;
        private string? _error;

        private FormulaParser(string text, IEnumerable<IFacadeStream> streams)
        {
            _text = text;
            _streams = streams
                .Distinct()
                .OrderByDescending(stream => stream.Name.Length)
                .ToList();
        }

        public static Result<FormulaEquationExpression> Parse(
            string formula,
            IEnumerable<IFacadeStream> streams)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                return Result<FormulaEquationExpression>.Fail("Formula is required.");
            }

            var parser = new FormulaParser(formula, streams);
            var equation = parser.ParseEquation();
            if (equation == null)
            {
                return Result<FormulaEquationExpression>.Fail(parser._error ?? "Formula is invalid.");
            }

            return Result<FormulaEquationExpression>.Success(equation, "Formula is valid.");
        }

        private FormulaEquationExpression? ParseEquation()
        {
            var left = ParseAdditive();
            if (left == null)
            {
                return null;
            }

            SkipWhitespace();
            if (!TryConsume('='))
            {
                return Fail<FormulaEquationExpression>("Expected '=' between both sides of the formula.");
            }

            var right = ParseAdditive();
            if (right == null)
            {
                return null;
            }

            SkipWhitespace();
            if (_position != _text.Length)
            {
                return Fail<FormulaEquationExpression>($"Unexpected text at position {_position + 1}.");
            }

            if (left.Dimension != right.Dimension)
            {
                return Fail<FormulaEquationExpression>(
                    $"Both sides must have the same dimension ({left.Dimension} != {right.Dimension}).");
            }

            if (!left.Variables.Concat(right.Variables).Any())
            {
                return Fail<FormulaEquationExpression>("Formula must contain at least one stream variable.");
            }

            return new FormulaEquationExpression(left, right);
        }

        private FormulaExpressionNode? ParseAdditive()
        {
            var node = ParseMultiplicative();
            while (node != null)
            {
                SkipWhitespace();
                var operation = Peek();
                if (operation is not ('+' or '-'))
                {
                    break;
                }

                _position++;
                var right = ParseMultiplicative();
                if (right == null)
                {
                    return null;
                }

                if (node.Dimension != right.Dimension)
                {
                    return Fail<FormulaExpressionNode>(
                        $"Operator '{operation}' requires values with the same dimension.");
                }

                node = new FormulaBinaryNode(operation, node, right);
            }

            return node;
        }

        private FormulaExpressionNode? ParseMultiplicative()
        {
            var node = ParseUnary();
            while (node != null)
            {
                SkipWhitespace();
                var operation = Peek();
                if (operation is not ('*' or '/'))
                {
                    break;
                }

                _position++;
                var right = ParseUnary();
                if (right == null)
                {
                    return null;
                }

                node = new FormulaBinaryNode(operation, node, right);
            }

            return node;
        }

        private FormulaExpressionNode? ParseUnary()
        {
            SkipWhitespace();
            var operation = Peek();
            if (operation is '+' or '-')
            {
                _position++;
                var operand = ParseUnary();
                return operand == null ? null : new FormulaUnaryNode(operation, operand);
            }

            return ParsePrimary();
        }

        private FormulaExpressionNode? ParsePrimary()
        {
            SkipWhitespace();
            if (TryConsume('('))
            {
                var expression = ParseAdditive();
                if (expression == null)
                {
                    return null;
                }

                SkipWhitespace();
                return TryConsume(')')
                    ? expression
                    : Fail<FormulaExpressionNode>($"Missing ')' at position {_position + 1}.");
            }

            if (_streams.Any(candidate => TryMatchName(candidate.Name)))
            {
                return ParseStreamReference();
            }

            var number = ParseNumber();
            return number ?? ParseStreamReference();
        }

        private FormulaExpressionNode? ParseNumber()
        {
            SkipWhitespace();
            var start = _position;
            var hasDecimalPoint = false;

            while (_position < _text.Length)
            {
                var character = _text[_position];
                if (char.IsDigit(character))
                {
                    _position++;
                    continue;
                }

                if (character == '.' && !hasDecimalPoint)
                {
                    hasDecimalPoint = true;
                    _position++;
                    continue;
                }

                break;
            }

            if (_position == start)
            {
                return null;
            }

            var rawValue = _text[start.._position];
            return double.TryParse(
                rawValue,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value)
                ? new FormulaConstantNode(value)
                : Fail<FormulaExpressionNode>($"Invalid number '{rawValue}'.");
        }

        private FormulaExpressionNode? ParseStreamReference()
        {
            SkipWhitespace();
            var stream = _streams.FirstOrDefault(candidate => TryMatchName(candidate.Name));
            if (stream == null)
            {
                return Fail<FormulaExpressionNode>($"Expected a number, parenthesis or stream at position {_position + 1}.");
            }

            _position += stream.Name.Length;
            if (!TryConsume('.'))
            {
                return Fail<FormulaExpressionNode>($"Expected '.' after stream '{stream.Name}'.");
            }

            if (TryConsumeKeyword("MassFlow"))
            {
                return new StreamMassFlowNode(stream);
            }

            if (!TryConsumeKeyword("Component") || !TryConsume('.'))
            {
                return Fail<FormulaExpressionNode>(
                    $"Expected 'MassFlow' or 'Component' after stream '{stream.Name}'.");
            }

            var component = stream.Composition.Components
                .OrderByDescending(candidate => candidate.Name.Length)
                .FirstOrDefault(candidate => TryMatchName(candidate.Name));
            if (component == null)
            {
                return Fail<FormulaExpressionNode>($"Expected a component from stream '{stream.Name}'.");
            }

            _position += component.Name.Length;
            if (!TryConsume('.') || !TryConsumeKeyword("MassFlow"))
            {
                return Fail<FormulaExpressionNode>(
                    $"Expected '.MassFlow' after component '{component.Name}'.");
            }

            return new ComponentMassFlowNode(stream, component);
        }

        private bool TryMatchName(string name)
        {
            if (_position + name.Length > _text.Length
                || !_text.AsSpan(_position, name.Length).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var nextPosition = _position + name.Length;
            return nextPosition < _text.Length && _text[nextPosition] == '.';
        }

        private bool TryConsumeKeyword(string keyword)
        {
            SkipWhitespace();
            if (_position + keyword.Length > _text.Length
                || !_text.AsSpan(_position, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            _position += keyword.Length;
            return true;
        }

        private bool TryConsume(char expected)
        {
            SkipWhitespace();
            if (Peek() != expected)
            {
                return false;
            }

            _position++;
            return true;
        }

        private char Peek() => _position < _text.Length ? _text[_position] : '\0';

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        private T? Fail<T>(string message) where T : class
        {
            _error ??= message;
            return null;
        }
    }
}
