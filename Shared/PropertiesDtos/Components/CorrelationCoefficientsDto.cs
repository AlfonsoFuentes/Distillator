using Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.PropertiesDtos.Components
{
    public class CorrelationCoefficientsDto
    {
        [ExcelExport("C1")] public double C1 { get; set; }
        [ExcelExport("C2")] public double C2 { get; set; }
        [ExcelExport("C3")] public double C3 { get; set; }
        [ExcelExport("C4")] public double C4 { get; set; }
        [ExcelExport("C5")] public double C5 { get; set; }
        [ExcelExport("C6")] public double C6 { get; set; }
        [ExcelExport("C7")] public double C7 { get; set; }

        [ExcelExport("Tmin")]
        public Amount Tmin { get; set; } = new Temperature(0, TemperatureUnits.DegreeCelcius);

        [ExcelExport("Tmax")]
        public Amount Tmax { get; set; } = new Temperature(0, TemperatureUnits.DegreeCelcius);
    }
    public class ChemicalComponentListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public double MolecularWeight { get; set; }
    }
    public class ChemicalComponentDto
    {
        public Guid Id { get; set; }

        [ExcelExport("Component Name")]
        public string Name { get; set; } = string.Empty;

        [ExcelExport("Chemical Formula")]
        public string Formula { get; set; } = string.Empty;

        [ExcelExport("Structural Formula")]
        public string StructuralFormula { get; set; } = string.Empty;

        [ExcelExport("Family")]
        public string Family { get; set; } = string.Empty;

        [ExcelExport("Secondary Family")]
        public string SecondaryFamily { get; set; } = string.Empty;

        [ExcelExport("Molecular Weight")]
        public double MolecularWeight { get; set; }

        [ExcelExport("Critical Temp (Tc)")]
        public Temperature CriticalTemperature { get; set; } = new Temperature(0, TemperatureUnits.DegreeCelcius);

        [ExcelExport("Critical Pressure (Pc)")]
        public Pressure CriticalPressure { get; set; } = new Pressure(0, PressureUnits.KiloPascala);

        [ExcelExport("Boiling Point (Tb)")]
        public Temperature BoilingPoint { get; set; } = new Temperature(0, TemperatureUnits.DegreeCelcius);

        [ExcelExport("Melting Point (Tm)")]
        public Temperature MeltingPoint { get; set; } = new Temperature(0, TemperatureUnits.DegreeCelcius);

        [ExcelExport("Critical Volume (Vc)")]
        public MolarVolumeSpecific CriticalVolume { get; set; } = new MolarVolumeSpecific(0, MolarVolumeSpecificUnits.m3_gmol);

        [ExcelExport("Volume Asterisk (V*)")]
        public MolarVolumeSpecific VolumeAsterisk { get; set; } = new MolarVolumeSpecific(0, MolarVolumeSpecificUnits.m3_gmol);

        [ExcelExport("Critical Z")]
        public double CriticalZ { get; set; }

        [ExcelExport("Acentric Factor")]
        public double AcentricFactor { get; set; }

        [ExcelExport("Acentric Factor (Pitzer)")]
        public double AcentricFactorPitzer { get; set; }

        [ExcelExport("Enthalpy of Formation")]
        public MolarEnergy EnthalpyForm { get; set; } = new MolarEnergy(0, MolarEnergyUnits.KJ_gmol);

        [ExcelExport("Gibbs Free Energy")]
        public MolarEnergy GibbsForm { get; set; } = new MolarEnergy(0, MolarEnergyUnits.KJ_gmol);

        [ExcelExport("Entropy of Formation")]
        public MolarEntropy EntropyForm { get; set; } = new MolarEntropy(0, MolarEntropyUnits.KJ_Kgmol_C);

        [ExcelExport("Enthalpy of Combustion")]
        public MolarEnergy CombustionEnthalpy { get; set; } = new MolarEnergy(0, MolarEnergyUnits.KJ_gmol);

        // Decoración de Correlaciones para la "Sábana"
        [ExcelExport("Vapor Pressure")] public CorrelationCoefficientsDto VaporPressure { get; set; } = new();
        [ExcelExport("Heat of Vaporization")] public CorrelationCoefficientsDto HeatOfVaporization { get; set; } = new();
        [ExcelExport("Liquid Heat Capacity")] public CorrelationCoefficientsDto LiquidHeatCapacity { get; set; } = new();
        [ExcelExport("Gas Heat Capacity")] public CorrelationCoefficientsDto GasHeatCapacity { get; set; } = new();
        [ExcelExport("Liquid Viscosity")] public CorrelationCoefficientsDto LiquidViscosity { get; set; } = new();
        [ExcelExport("Gas Viscosity")] public CorrelationCoefficientsDto GasViscosity { get; set; } = new();
        [ExcelExport("Liquid Thermal Cond")] public CorrelationCoefficientsDto LiquidThermalCond { get; set; } = new();
        [ExcelExport("Gas Thermal Cond")] public CorrelationCoefficientsDto GasThermalCond { get; set; } = new();
        [ExcelExport("Density")] public CorrelationCoefficientsDto Density { get; set; } = new();
        [ExcelExport("Surface Tension")] public CorrelationCoefficientsDto SurfaceTension { get; set; } = new();

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
    }

    public class CreateChemicalComponent : ChemicalComponentDto
    {

    }
    public class EditChemicalComponent : ChemicalComponentDto
    {

    }
    public record GetAllCompleteComponents();
    public record GetAllComponents();
    public record GetComponentById(Guid Id);
    public record DeleteComponent(Guid Id);
    public class ValidateComponente : ChemicalComponentDto
    {

    }
}
