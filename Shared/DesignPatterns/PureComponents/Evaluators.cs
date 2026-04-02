using Shared.DesignPatterns.NewFolder;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents
{
    public interface IPropertyEvaluator<TInput,TResult> where TInput : Amount where TResult : Amount
    {
        TResult EvaluateAt(TInput input);
    }

}
