using UnitSystem;

namespace Shared.SolverConsecutive.Equipments.Columns
{
    public class VLEPoint
    {
        // Composición
        public double x { get; set; }  // Fracción molar líquido (componente ligero)
        public double y { get; set; }  // Fracción molar vapor (componente ligero)

        // Condiciones de equilibrio
        public Pressure Pressure { get; set; } = new Pressure();
        public Temperature Temperature { get; set; } = new Temperature();

        // Propiedades del líquido saturado
        public MassEnergy LiquidEnthalpy { get; set; } = new MassEnergy();
        public MassDensity LiquidDensity { get; set; } = new MassDensity();
        public MolarDensity LiquidMolarDensity { get; set; } = new MolarDensity();

        // Propiedades del vapor saturado
        public MassEnergy VaporEnthalpy { get; set; } = new MassEnergy();
        public MassDensity VaporDensity { get; set; } = new MassDensity();
        public MolarDensity VaporMolarDensity { get; set; } = new MolarDensity();

        // Constructor vacío para serialización
        public VLEPoint() { }

        // Constructor completo
        public VLEPoint(double x, double y, Pressure pressure, Temperature temperature,
                        MassEnergy liquidH, MassDensity liquidRho, MolarDensity liquidMolarRho,
                        MassEnergy vaporH, MassDensity vaporRho, MolarDensity vaporMolarRho)
        {
            this.x = x;
            this.y = y;
            Pressure = pressure;
            Temperature = temperature;
            LiquidEnthalpy = liquidH;
            LiquidDensity = liquidRho;
            LiquidMolarDensity = liquidMolarRho;
            VaporEnthalpy = vaporH;
            VaporDensity = vaporRho;
            VaporMolarDensity = vaporMolarRho;
        }

        // Método helper para clonar
        public VLEPoint Clone()
        {
            return new VLEPoint(
                x, y,
                new Pressure(Pressure.Value, Pressure.Unit),
                new Temperature(Temperature.Value, Temperature.Unit),
                new MassEnergy(LiquidEnthalpy.Value, LiquidEnthalpy.Unit),
                new MassDensity(LiquidDensity.Value, LiquidDensity.Unit),
                new MolarDensity(LiquidMolarDensity.Value, LiquidMolarDensity.Unit),
                new MassEnergy(VaporEnthalpy.Value, VaporEnthalpy.Unit),
                new MassDensity(VaporDensity.Value, VaporDensity.Unit),
                new MolarDensity(VaporMolarDensity.Value, VaporMolarDensity.Unit)
            );
        }
    }


}
