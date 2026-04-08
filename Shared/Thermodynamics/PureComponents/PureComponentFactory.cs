using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents.Gas.Others;
using Shared.Thermodynamics.PureComponents.Gas.Water;
using Shared.Thermodynamics.PureComponents.Liquido.Others;
using Shared.Thermodynamics.PureComponents.Liquido.Water;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents
{
    public static class PureComponentFactory
    {
        public static PureComponentData CreateFromDto(ChemicalComponentDto dto)
        {
            bool isWater = dto.Name.Equals("Agua", StringComparison.OrdinalIgnoreCase) ||
                           dto.Name.Equals("Water", StringComparison.OrdinalIgnoreCase);

            // ========== PRESIÓN DE VAPOR ==========
            IPropertyEvaluator<Temperature, Pressure> vpEval = isWater
                ? new WaterVaporPressureEvaluator(dto.CriticalPressure, dto.CriticalTemperature)
                : new ExtendedAntoineEvaluator(dto.VaporPressure);

            // ========== TEMPERATURA DE SATURACIÓN ==========
            // Dependencia: vpEval inyectado en SecantSatTemperatureEvaluator
            IPropertyEvaluator<Pressure, Temperature> tsatEval = isWater
                ? new WaterSatTemperatureEvaluator()
                : new SecantSatTemperatureEvaluator(vpEval, dto.VaporPressure, dto.CriticalTemperature, dto.CriticalPressure);

            // ========== RESTO DE EVALUADORES ==========
            IPropertyEvaluator<Temperature, MolarEnergy> hvapEval = isWater
                ? new WaterHeatOfVaporizationEvaluator()
                : new DipprHeatOfVaporizationEvaluator(dto.HeatOfVaporization, dto.CriticalTemperature);

            IPropertyEvaluator<Temperature, MolarEntropy> liqCpEval = isWater
                ? new WaterLiquidCpEvaluator()
                : new PolynomialLiquidCpEvaluator(dto.LiquidHeatCapacity);

            IPropertyEvaluator<Temperature, MolarEntropy> gasCpEval = isWater
                ? new WaterGasCpEvaluator()
                : new AlyLeeGasCpEvaluator(dto.GasHeatCapacity);

            IPropertyEvaluator<Temperature, Viscosity> liqViscEval = isWater
                ? new WaterLiquidViscosityEvaluator()
                : new AndradeLiquidViscosityEvaluator(dto.LiquidViscosity);

            IPropertyEvaluator<Temperature, Viscosity> gasViscEval = isWater
                ? new WaterGasViscosityEvaluator()
                : new DipprGasViscosityEvaluator(dto.GasViscosity);

            IPropertyEvaluator<Temperature, ThermalConductivity> liqCondEval = isWater
                ? new WaterLiquidThermalCondEvaluator()
                : new PolynomialLiquidThermalCondEvaluator(dto.LiquidThermalCond);

            IPropertyEvaluator<Temperature, ThermalConductivity> gasCondEval = isWater
                ? new WaterGasThermalCondEvaluator()
                : new PolynomialGasThermalCondEvaluator(dto.GasThermalCond);

            IPropertyEvaluator<Temperature, MolarDensity> liqDensEval = isWater
                ? new WaterLiquidDensityEvaluator()
                : new RackettLiquidDensityEvaluator(dto.Density);

            IPropertyEvaluator<Temperature, SuperficialTension> surfTensEval = isWater
                ? new WaterSurfaceTensionEvaluator(dto.SurfaceTension, dto.CriticalTemperature)
                : new DipprSurfaceTensionEvaluator(dto.SurfaceTension, dto.CriticalTemperature);
            // Agregar después de surfTensEval:

            // ========== ENTALPÍA LÍQUIDA ==========
            IPropertyEvaluator<Temperature, MolarEnergy> liqEnthalpyEval = isWater
                ? new WaterLiquidEnthalpyEvaluator()
                : new LiquidEnthalpyEvaluator(dto.LiquidHeatCapacity, dto.MolecularWeight);

            // Agregar evaluador de volumen molar saturado
            IPropertyEvaluator<Temperature, MolarVolumeSpecific> satVolEval = isWater
                ? new WaterSaturatedMolarVolumeEvaluator()  // Necesitaría IAPWS
                : new RackettSaturatedMolarVolumeEvaluator(
                    dto.CriticalTemperature,
                    dto.CriticalPressure,
                    dto.AcentricFactor);


            // ========== ENTALPÍA GAS (solo para no-agua por ahora) ==========
            IPropertyEvaluator<Temperature, MolarEnergy> gasEnthalpyEval = isWater
     ? new WaterGasEnthalpyEvaluator()
     : new GasEnthalpyEvaluator(
         dto.GasHeatCapacity,
         dto.LiquidHeatCapacity,
         hvapEval,
         tsatEval,
         new Pressure(1.01325, PressureUnits.Bara),
         dto.MolecularWeight);


            // ========== ENSAMBLADO ==========
            return new PureComponentData(
                dto.Id, dto.Name, dto.Formula, dto.StructuralFormula,
                dto.Family, dto.SecondaryFamily, dto.MolecularWeight,
                dto.CriticalTemperature, dto.CriticalPressure, dto.CriticalVolume,
                dto.CriticalZ, dto.BoilingPoint, dto.MeltingPoint,
                dto.VolumeAsterisk, dto.AcentricFactor, dto.AcentricFactorPitzer,
                dto.EnthalpyForm, dto.GibbsForm, dto.EntropyForm, dto.CombustionEnthalpy,

                vpEval,        // VaporPressureEvaluator
                tsatEval,      // SaturationTemperatureEvaluator (NUEVO)
                hvapEval,
                liqCpEval,
                gasCpEval,
                liqViscEval,
                gasViscEval,
                liqCondEval,
                gasCondEval,
                liqDensEval,
                surfTensEval,
                liqEnthalpyEval,  // ✅ Agregar
                gasEnthalpyEval , satVolEval
            );
        }
    }


}
