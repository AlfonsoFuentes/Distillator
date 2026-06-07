namespace Shared.SolverQwen.Variables
{
    public enum VariableDataProcedence
    {
        Undefined,
        UserInput,
        StreamCalculated,
        Phase1_LocalPropagation,
        Phase2_EasyEquipmentNet,
        Phase3_ThermoAdjustment
    }

  

  


    public class VariableUndefinedException : Exception
    {
        public VariableUndefinedException(string variableId)
            : base($"Variable '{variableId}' is Undefined. Cannot read value.") { }
    }

    /// <summary>
    /// Resultado de operación con mensaje de error opcional.
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; }
        public string Error { get; }

        public static OperationResult Ok() => new OperationResult(true, null!);
        public static OperationResult Fail(string error) => new OperationResult(false, error);

        private OperationResult(bool success, string error)
        {
            Success = success;
            Error = error;
        }
    }
}

