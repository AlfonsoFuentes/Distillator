namespace Shared.Thermodynamics.ControlledVariables
{
    // ─────────────────────────────────────────────────────────
    // 🔹 INTERFAZ NO-GENÉRICA
    // ─────────────────────────────────────────────────────────

    public interface IControlledVariable
    {
        MethodSource Source { get; set; }
        string SourceId { get; set; }
        bool IsDefined { get; }

        // 👇 Agregamos los parámetros opcionales
        void ClearValue();
        void RevertCalculatedValue();
    }
}



