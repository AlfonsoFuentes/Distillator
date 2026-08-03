using Shared.PropertiesDtos.Components;
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

        public VaporPressureEquationType VaporPressureEquationType { get; set; } = VaporPressureEquationType.ExtendedAntoine;
        public SaturationTemperatureEquationType SaturationTemperatureEquationType { get; set; } = SaturationTemperatureEquationType.FromVaporPressureSecant;
        public HeatOfVaporizationEquationType HeatOfVaporizationEquationType { get; set; } = HeatOfVaporizationEquationType.Dippr106;
        public LiquidHeatCapacityEquationType LiquidHeatCapacityEquationType { get; set; } = LiquidHeatCapacityEquationType.Polynomial;
        public GasHeatCapacityEquationType GasHeatCapacityEquationType { get; set; } = GasHeatCapacityEquationType.AlyLee;
        public LiquidViscosityEquationType LiquidViscosityEquationType { get; set; } = LiquidViscosityEquationType.Dippr101;
        public GasViscosityEquationType GasViscosityEquationType { get; set; } = GasViscosityEquationType.Dippr102;
        public LiquidThermalConductivityEquationType LiquidThermalConductivityEquationType { get; set; } = LiquidThermalConductivityEquationType.Polynomial4;
        public GasThermalConductivityEquationType GasThermalConductivityEquationType { get; set; } = GasThermalConductivityEquationType.PolynomialRational;
        public LiquidDensityEquationType LiquidDensityEquationType { get; set; } = LiquidDensityEquationType.Rackett;
        public SurfaceTensionEquationType SurfaceTensionEquationType { get; set; } = SurfaceTensionEquationType.Dippr106;
        public LiquidEnthalpyEquationType LiquidEnthalpyEquationType { get; set; } = LiquidEnthalpyEquationType.IntegratedLiquidCp;
        public GasEnthalpyEquationType GasEnthalpyEquationType { get; set; } = GasEnthalpyEquationType.IntegratedGasCpWithHvap;
        public SaturatedMolarVolumeEquationType SaturatedMolarVolumeEquationType { get; set; } = SaturatedMolarVolumeEquationType.Rackett;

   
        public override bool IsTenanted => false;
    }
}
