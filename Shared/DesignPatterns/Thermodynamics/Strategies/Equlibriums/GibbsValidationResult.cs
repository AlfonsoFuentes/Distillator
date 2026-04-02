namespace Shared.DesignPatterns.Thermodynamics.Strategies.Equlibriums
{
    /// <summary>
    /// Resultado de validación de la Regla de Fases de Gibbs.
    /// </summary>
    public class GibbsValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        public static GibbsValidationResult Valid() => new() { IsValid = true };

        public static GibbsValidationResult Invalid(string message) => new()
        {
            IsValid = false,
            ErrorMessage = message
        };
    }
}
