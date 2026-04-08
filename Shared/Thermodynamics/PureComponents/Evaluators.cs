using UnitSystem;

namespace Shared.Thermodynamics.PureComponents
{
    public interface IPropertyEvaluator<TInput,TResult> where TInput : Amount where TResult : Amount
    {
        TResult EvaluateAt(TInput input);
    }

}
