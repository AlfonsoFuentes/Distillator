using UnitSystem;

namespace Server.Entities.BaseStructure.Components
{
    public class CorrelationCoefficients
    {
        public double C1 { get; set; }
        public double C2 { get; set; }
        public double C3 { get; set; }
        public double C4 { get; set; }
        public double C5 { get; set; }
        public double C6 { get; set; }
        public double C7 { get; set; }

        // 👇 Ahora protegidos con tu sistema de unidades 👇
        public StoredAmount Tmin { get; set; } = new();
        public StoredAmount Tmax { get; set; } = new();
    }
}
