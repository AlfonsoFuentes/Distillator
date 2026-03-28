using UnitSystem;

namespace Server.Entities.BaseStructure.Components
{
    public class ChemicalComponent : Entity
    {
        // --- IDENTIFICACIÓN ---
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string StructuralFormula { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string SecondaryFamily { get; set; } = string.Empty;

       
        public double MolecularWeight { get; set; } 
        public StoredAmount CriticalTemperature { get; set; } = new();
        public StoredAmount BoilingPoint { get; set; } = new();
        public StoredAmount MeltingPoint { get; set; } = new();
        public StoredAmount CriticalPressure { get; set; } = new();
        public StoredAmount CriticalVolume { get; set; } = new();
        public StoredAmount VolumeAsterisk { get; set; } = new();

        public StoredAmount EnthalpyForm { get; set; } = new();
        public StoredAmount GibbsForm { get; set; } = new();
        public StoredAmount EntropyForm { get; set; } = new();
        public StoredAmount CombustionEnthalpy { get; set; } = new();

     
        public double CriticalZ { get; set; }
        public double AcentricFactor { get; set; }
        public double AcentricFactorPitzer { get; set; }



        public CorrelationCoefficients VaporPressure { get; set; } = new();       // _PV
        public CorrelationCoefficients HeatOfVaporization { get; set; } = new();  // _CV
        public CorrelationCoefficients LiquidHeatCapacity { get; set; } = new();  // _CPL
        public CorrelationCoefficients GasHeatCapacity { get; set; } = new();     // _CPG
        public CorrelationCoefficients LiquidViscosity { get; set; } = new();     // _VL
        public CorrelationCoefficients GasViscosity { get; set; } = new();        // _VV
        public CorrelationCoefficients LiquidThermalCond { get; set; } = new();   // _CTL
        public CorrelationCoefficients GasThermalCond { get; set; } = new();      // _CTV
        public CorrelationCoefficients Density { get; set; } = new();             // _DE
        public CorrelationCoefficients SurfaceTension { get; set; } = new();      // _TS

   
        public override bool IsTenanted => false;
    }
}
