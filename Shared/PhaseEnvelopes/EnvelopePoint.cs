using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.PhaseEnvelopes
{
    public class EnvelopePoint
    {
        public Pressure Pressure { get; set; } = new Pressure(101325, PressureUnits.Pascala);
        public Temperature Temperature { get; set; } = new Temperature(25, TemperatureUnits.DegreeCelcius);
        public MassEnergy MassEnthalpy { get; set; } = new MassEnergy(0, MassEnergyUnits.KJ_Kg);
        public MolarEnergy MolarEnthalpy { get; set; } = new MolarEnergy(0, MolarEnergyUnits.KJ_Kgmol);
    }

    /// <summary>
    /// El paquete completo de datos que el Generador le entregará a la Interfaz Gráfica.
    /// </summary>
    public class PhaseEnvelopeData
    {
        public List<EnvelopePoint> BubbleCurve { get; set; } = new();
        public List<EnvelopePoint> DewCurve { get; set; } = new();

        // Banderas de estado para la UI
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
