using Shared.SolverConsecutive;
using System.Diagnostics;
using UnitSystem;

namespace Shared.SolverQwen.Variables
{
    public interface IProcessVariableOwner
    {
        HashSet<IVariable> Variables { get; }
        void AddVariable(IVariable variable);
        void RemoveVariables(VariableDefinedBy _DataProcedence);
    }

   

}