namespace Shared.DesignPatterns.Thermodynamics
{
    // 1. Enum para trackear el tipo de entrada
    public enum ComponentInputType
    {
        None = 0,           // Nada definido
        MassFraction = 1,   // % másico definido (w_i)
        MolarFraction = 2,  // % molar definido (z_i, x_i, y_i)
        MolarFlow = 3,      // Flujo molar definido
        MassFlow = 4,       // Flujo másico definido
        VolumetricFlow = 5  // Flujo volumétrico definido
    }
}
