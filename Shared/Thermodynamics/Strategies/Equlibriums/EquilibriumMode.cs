namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    // ========================================================================
    // ENUM DE MODOS
    // ========================================================================
    public enum EquilibriumMode
    {
        None = 0,      // Sin modo definido
        PT = 1,        // P + T definidos → calcular VF
        PFV = 2,       // P + VF definidos → calcular T
        TFV = 3,       // T + VF definidos → calcular P
        PH = 4         // P + H definidos → calcular T
    }
}
