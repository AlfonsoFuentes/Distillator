namespace Shared.Calculator.MaterialStreams
{
    public class ReducedProperties
    {
        public double Temperature { get; set; } // Tr = T / Tc
        public double Pressure { get; set; }    // Pr = P / Pc
        public double Volume { get; set; }      // Vr = V / Vc (Opcional, pero útil)

        public ReducedProperties()
        {
            Temperature = 0.0;
            Pressure = 0.0;
            Volume = 0.0;
        }
    }
   
}
