using Shared.DesignPatterns.PureComponents;
using Shared.Thermodynamics.Components;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.DesignPatterns.NewFolder
{
  

    public class PureComponentData
    {
        // ========================================================================
        // PROPIEDADES ESCALARES (El ADN inmutable de la sustancia)
        // ========================================================================
        public Guid Id { get; }
        public string Name { get; }
        public string Formula { get; }
        public string StructuralFormula { get; }
        public string Family { get; }
        public string SecondaryFamily { get; }

        public double MolecularWeight { get; }
        public Temperature CriticalTemperature { get; }
        public Pressure CriticalPressure { get; }
        public MolarVolumeSpecific CriticalVolume { get; }
        public double CriticalZ { get; }

        public Temperature BoilingPoint { get; }
        public Temperature MeltingPoint { get; }
        public MolarVolumeSpecific VolumeAsterisk { get; }

        public double AcentricFactor { get; }
        public double AcentricFactorPitzer { get; }

        public MolarEnergy EnthalpyForm { get; }
        public MolarEnergy GibbsForm { get; }
        public MolarEntropy EntropyForm { get; }
        public MolarEnergy CombustionEnthalpy { get; }

        // ========================================================================
        // PATRÓN STRATEGY: Los 10 motores de cálculo dependientes de la T
        // ========================================================================
        public IPropertyEvaluator VaporPressureEvaluator { get; }
        public IPropertyEvaluator HeatOfVaporizationEvaluator { get; }
        public IPropertyEvaluator LiquidHeatCapacityEvaluator { get; }
        public IPropertyEvaluator GasHeatCapacityEvaluator { get; }
        public IPropertyEvaluator LiquidViscosityEvaluator { get; }
        public IPropertyEvaluator GasViscosityEvaluator { get; }
        public IPropertyEvaluator LiquidThermalCondEvaluator { get; }
        public IPropertyEvaluator GasThermalCondEvaluator { get; }
        public IPropertyEvaluator LiquidDensityEvaluator { get; }
        public IPropertyEvaluator SurfaceTensionEvaluator { get; }

        // Constructor completo (ensamblado por la Fábrica)
        public PureComponentData(
            Guid id, string name, string formula, string structFormula, string family, string secFamily,
            double mw, Temperature tc, Pressure pc, MolarVolumeSpecific vc, double zc, Temperature tb, Temperature tm, MolarVolumeSpecific vAsterisk,
            double acentric, double acentricPitzer, MolarEnergy hForm, MolarEnergy gForm, MolarEntropy sForm, MolarEnergy hComb,
            IPropertyEvaluator vaporPressure, IPropertyEvaluator heatOfVap,
            IPropertyEvaluator liqHeatCap, IPropertyEvaluator gasHeatCap,
            IPropertyEvaluator liqVisc, IPropertyEvaluator gasVisc,
            IPropertyEvaluator liqThermCond, IPropertyEvaluator gasThermCond,
            IPropertyEvaluator liqDensity, IPropertyEvaluator surfaceTension)
        {
            Id = id; Name = name; Formula = formula; StructuralFormula = structFormula;
            Family = family; SecondaryFamily = secFamily;
            MolecularWeight = mw; CriticalTemperature = tc; CriticalPressure = pc;
            CriticalVolume = vc; CriticalZ = zc; BoilingPoint = tb; MeltingPoint = tm;
            VolumeAsterisk = vAsterisk; AcentricFactor = acentric; AcentricFactorPitzer = acentricPitzer;
            EnthalpyForm = hForm; GibbsForm = gForm; EntropyForm = sForm; CombustionEnthalpy = hComb;

            VaporPressureEvaluator = vaporPressure;
            HeatOfVaporizationEvaluator = heatOfVap;
            LiquidHeatCapacityEvaluator = liqHeatCap;
            GasHeatCapacityEvaluator = gasHeatCap;
            LiquidViscosityEvaluator = liqVisc;
            GasViscosityEvaluator = gasVisc;
            LiquidThermalCondEvaluator = liqThermCond;
            GasThermalCondEvaluator = gasThermCond;
            LiquidDensityEvaluator = liqDensity;
            SurfaceTensionEvaluator = surfaceTension;
        }
    }
    public static class PureComponentFactory
    {
        public static PureComponentData CreateFromDto(ChemicalComponentDto dto)
        {
            // 1. Identificamos si es agua para usar las estrategias de la librería especial
            bool isWater = dto.Name.Equals("Agua", StringComparison.OrdinalIgnoreCase) ||
                           dto.Name.Equals("Water", StringComparison.OrdinalIgnoreCase);

            // 2. Instanciación de las 10 estrategias matemáticas (Patrón Strategy)

            IPropertyEvaluator vpEval = isWater
                ? new WaterVaporPressureEvaluator(dto.CriticalPressure, dto.CriticalTemperature)
                : new ExtendedAntoineEvaluator(dto.VaporPressure);

            IPropertyEvaluator hvapEval = isWater
                ? new WaterHeatOfVaporizationEvaluator()
                : new DipprHeatOfVaporizationEvaluator(dto.HeatOfVaporization, dto.CriticalTemperature);

            IPropertyEvaluator liqCpEval = isWater
                ? new WaterLiquidCpEvaluator()
                : new PolynomialLiquidCpEvaluator(dto.LiquidHeatCapacity);

            IPropertyEvaluator gasCpEval = isWater
                ? new WaterGasCpEvaluator()
                : new AlyLeeGasCpEvaluator(dto.GasHeatCapacity);

            IPropertyEvaluator liqViscEval = isWater
                ? new WaterLiquidViscosityEvaluator()
                : new AndradeLiquidViscosityEvaluator(dto.LiquidViscosity);

            IPropertyEvaluator gasViscEval = isWater
                ? new WaterGasViscosityEvaluator()
                : new DipprGasViscosityEvaluator(dto.GasViscosity);

            IPropertyEvaluator liqCondEval = isWater
                ? new WaterLiquidThermalCondEvaluator()
                : new PolynomialLiquidThermalCondEvaluator(dto.LiquidThermalCond);

            IPropertyEvaluator gasCondEval = isWater
                ? new WaterGasThermalCondEvaluator()
                : new PolynomialGasThermalCondEvaluator(dto.GasThermalCond);

            IPropertyEvaluator liqDensEval = isWater
                ? new WaterLiquidDensityEvaluator()
                : new RackettLiquidDensityEvaluator(dto.Density);

            IPropertyEvaluator surfTensEval = isWater
                ? new WaterSurfaceTensionEvaluator()
                : new DipprSurfaceTensionEvaluator(dto.SurfaceTension, dto.CriticalTemperature);

            // 3. Ensamblado y retorno de la Entidad de Dominio ("El Átomo")
            return new PureComponentData(
                dto.Id,
                dto.Name,
                dto.Formula,
                dto.StructuralFormula,
                dto.Family,
                dto.SecondaryFamily,
                dto.MolecularWeight,
                dto.CriticalTemperature,
                dto.CriticalPressure,
                dto.CriticalVolume,
                dto.CriticalZ,
                dto.BoilingPoint,
                dto.MeltingPoint,
                dto.VolumeAsterisk,
                dto.AcentricFactor,
                dto.AcentricFactorPitzer,
                dto.EnthalpyForm,
                dto.GibbsForm,
                dto.EntropyForm,
                dto.CombustionEnthalpy,

                // Inyección de los motores de cálculo
                vpEval,
                hvapEval,
                liqCpEval,
                gasCpEval,
                liqViscEval,
                gasViscEval,
                liqCondEval,
                gasCondEval,
                liqDensEval,
                surfTensEval
            );
        }
    }
   
}
