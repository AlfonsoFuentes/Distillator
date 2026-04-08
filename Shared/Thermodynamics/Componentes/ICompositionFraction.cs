namespace Shared.Thermodynamics.Componentes
{
    public interface ICompositionFraction
    {
        double MassFraction { get; set; } // w_i
        double MolarFraction { get; set; } // z_i, x_i, y_i
    }
}
