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
            // ========== PRESIÓN DE VAPOR ==========
            IPropertyEvaluator<Temperature, Pressure> vpEval = CreateVaporPressureEvaluator(dto);

            // ========== TEMPERATURA DE SATURACIÓN ==========
            // Dependencia: vpEval inyectado en SecantSatTemperatureEvaluator
            IPropertyEvaluator<Pressure, Temperature> tsatEval = CreateSaturationTemperatureEvaluator(dto, vpEval);

            // ========== RESTO DE EVALUADORES ==========
            IPropertyEvaluator<Temperature, MolarEnergy> hvapEval = CreateHeatOfVaporizationEvaluator(dto);

            IPropertyEvaluator<Temperature, MolarEntropy> liqCpEval = CreateLiquidHeatCapacityEvaluator(dto);

            IPropertyEvaluator<Temperature, MolarEntropy> gasCpEval = CreateGasHeatCapacityEvaluator(dto);

            IPropertyEvaluator<Temperature, Viscosity> liqViscEval = CreateLiquidViscosityEvaluator(dto);

            IPropertyEvaluator<Temperature, Viscosity> gasViscEval = CreateGasViscosityEvaluator(dto);

            IPropertyEvaluator<Temperature, ThermalConductivity> liqCondEval = CreateLiquidThermalConductivityEvaluator(dto);

            IPropertyEvaluator<Temperature, ThermalConductivity> gasCondEval = CreateGasThermalConductivityEvaluator(dto);

            IPropertyEvaluator<Temperature, MolarDensity> liqDensEval = CreateLiquidDensityEvaluator(dto);

            IPropertyEvaluator<Temperature, SuperficialTension> surfTensEval = CreateSurfaceTensionEvaluator(dto);
            // Agregar después de surfTensEval:

            // ========== ENTALPÍA LÍQUIDA ==========
            IPropertyEvaluator<Temperature, MolarEnergy> liqEnthalpyEval = CreateLiquidEnthalpyEvaluator(dto);

            // Agregar evaluador de volumen molar saturado
            IPropertyEvaluator<Temperature, MolarVolumeSpecific> satVolEval = CreateSaturatedMolarVolumeEvaluator(dto);


            // ========== ENTALPÍA GAS (solo para no-agua por ahora) ==========
            IPropertyEvaluator<Temperature, MolarEnergy> gasEnthalpyEval = CreateGasEnthalpyEvaluator(dto, hvapEval, tsatEval);


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

        private static IPropertyEvaluator<Temperature, Pressure> CreateVaporPressureEvaluator(ChemicalComponentDto dto)
            => dto.VaporPressureEquationType switch
            {
                VaporPressureEquationType.IapwsSteamTables => new WaterVaporPressureEvaluator(dto.CriticalPressure, dto.CriticalTemperature),
                VaporPressureEquationType.ExtendedAntoine => new ExtendedAntoineEvaluator(dto.VaporPressure),
                _ => throw Unsupported(dto.Name, nameof(dto.VaporPressureEquationType), dto.VaporPressureEquationType)
            };

        private static IPropertyEvaluator<Pressure, Temperature> CreateSaturationTemperatureEvaluator(
            ChemicalComponentDto dto,
            IPropertyEvaluator<Temperature, Pressure> vaporPressureEvaluator)
            => dto.SaturationTemperatureEquationType switch
            {
                SaturationTemperatureEquationType.IapwsSteamTables => new WaterSatTemperatureEvaluator(),
                SaturationTemperatureEquationType.FromVaporPressureSecant => new SecantSatTemperatureEvaluator(
                    vaporPressureEvaluator,
                    dto.VaporPressure,
                    dto.CriticalTemperature,
                    dto.CriticalPressure),
                _ => throw Unsupported(dto.Name, nameof(dto.SaturationTemperatureEquationType), dto.SaturationTemperatureEquationType)
            };

        private static IPropertyEvaluator<Temperature, MolarEnergy> CreateHeatOfVaporizationEvaluator(ChemicalComponentDto dto)
            => dto.HeatOfVaporizationEquationType switch
            {
                HeatOfVaporizationEquationType.IapwsSteamTables => new WaterHeatOfVaporizationEvaluator(),
                HeatOfVaporizationEquationType.Dippr106 => new DipprHeatOfVaporizationEvaluator(dto.HeatOfVaporization, dto.CriticalTemperature),
                _ => throw Unsupported(dto.Name, nameof(dto.HeatOfVaporizationEquationType), dto.HeatOfVaporizationEquationType)
            };

        private static IPropertyEvaluator<Temperature, MolarEntropy> CreateLiquidHeatCapacityEvaluator(ChemicalComponentDto dto)
            => dto.LiquidHeatCapacityEquationType switch
            {
                LiquidHeatCapacityEquationType.IapwsSteamTables => new WaterLiquidCpEvaluator(),
                LiquidHeatCapacityEquationType.Polynomial => new PolynomialLiquidCpEvaluator(dto.LiquidHeatCapacity),
                _ => throw Unsupported(dto.Name, nameof(dto.LiquidHeatCapacityEquationType), dto.LiquidHeatCapacityEquationType)
            };

        private static IPropertyEvaluator<Temperature, MolarEntropy> CreateGasHeatCapacityEvaluator(ChemicalComponentDto dto)
            => dto.GasHeatCapacityEquationType switch
            {
                GasHeatCapacityEquationType.IapwsSteamTables => new WaterGasCpEvaluator(),
                GasHeatCapacityEquationType.AlyLee => new AlyLeeGasCpEvaluator(dto.GasHeatCapacity),
                _ => throw Unsupported(dto.Name, nameof(dto.GasHeatCapacityEquationType), dto.GasHeatCapacityEquationType)
            };

        private static IPropertyEvaluator<Temperature, Viscosity> CreateLiquidViscosityEvaluator(ChemicalComponentDto dto)
            => dto.LiquidViscosityEquationType switch
            {
                LiquidViscosityEquationType.IapwsSteamTables => new WaterLiquidViscosityEvaluator(),
                LiquidViscosityEquationType.Dippr101 => new AndradeLiquidViscosityEvaluator(dto.LiquidViscosity),
                _ => throw Unsupported(dto.Name, nameof(dto.LiquidViscosityEquationType), dto.LiquidViscosityEquationType)
            };

        private static IPropertyEvaluator<Temperature, Viscosity> CreateGasViscosityEvaluator(ChemicalComponentDto dto)
            => dto.GasViscosityEquationType switch
            {
                GasViscosityEquationType.IapwsSteamTables => new WaterGasViscosityEvaluator(),
                GasViscosityEquationType.Dippr102 => new DipprGasViscosityEvaluator(dto.GasViscosity),
                _ => throw Unsupported(dto.Name, nameof(dto.GasViscosityEquationType), dto.GasViscosityEquationType)
            };

        private static IPropertyEvaluator<Temperature, ThermalConductivity> CreateLiquidThermalConductivityEvaluator(ChemicalComponentDto dto)
            => dto.LiquidThermalConductivityEquationType switch
            {
                LiquidThermalConductivityEquationType.IapwsSteamTables => new WaterLiquidThermalCondEvaluator(),
                LiquidThermalConductivityEquationType.Ppds8 => new Ppds8LiquidThermalCondEvaluator(dto.LiquidThermalCond, dto.CriticalTemperature),
                LiquidThermalConductivityEquationType.Polynomial4 => new PolynomialLiquidThermalCondEvaluator(dto.LiquidThermalCond),
                _ => throw Unsupported(dto.Name, nameof(dto.LiquidThermalConductivityEquationType), dto.LiquidThermalConductivityEquationType)
            };

        private static IPropertyEvaluator<Temperature, ThermalConductivity> CreateGasThermalConductivityEvaluator(ChemicalComponentDto dto)
            => dto.GasThermalConductivityEquationType switch
            {
                GasThermalConductivityEquationType.IapwsSteamTables => new WaterGasThermalCondEvaluator(),
                GasThermalConductivityEquationType.PolynomialRational => new PolynomialGasThermalCondEvaluator(dto.GasThermalCond),
                _ => throw Unsupported(dto.Name, nameof(dto.GasThermalConductivityEquationType), dto.GasThermalConductivityEquationType)
            };

        private static IPropertyEvaluator<Temperature, MolarDensity> CreateLiquidDensityEvaluator(ChemicalComponentDto dto)
            => dto.LiquidDensityEquationType switch
            {
                LiquidDensityEquationType.IapwsSteamTables => new WaterLiquidDensityEvaluator(),
                LiquidDensityEquationType.Rackett => new RackettLiquidDensityEvaluator(dto.Density),
                _ => throw Unsupported(dto.Name, nameof(dto.LiquidDensityEquationType), dto.LiquidDensityEquationType)
            };

        private static IPropertyEvaluator<Temperature, SuperficialTension> CreateSurfaceTensionEvaluator(ChemicalComponentDto dto)
            => dto.SurfaceTensionEquationType switch
            {
                SurfaceTensionEquationType.IapwsSteamTables => new WaterSurfaceTensionEvaluator(dto.SurfaceTension, dto.CriticalTemperature),
                SurfaceTensionEquationType.Dippr106 => new DipprSurfaceTensionEvaluator(dto.SurfaceTension, dto.CriticalTemperature),
                _ => throw Unsupported(dto.Name, nameof(dto.SurfaceTensionEquationType), dto.SurfaceTensionEquationType)
            };

        private static IPropertyEvaluator<Temperature, MolarEnergy> CreateLiquidEnthalpyEvaluator(ChemicalComponentDto dto)
            => dto.LiquidEnthalpyEquationType switch
            {
                LiquidEnthalpyEquationType.IapwsSteamTables => new WaterLiquidEnthalpyEvaluator(),
                LiquidEnthalpyEquationType.IntegratedLiquidCp => new LiquidEnthalpyEvaluator(dto.LiquidHeatCapacity, dto.MolecularWeight),
                _ => throw Unsupported(dto.Name, nameof(dto.LiquidEnthalpyEquationType), dto.LiquidEnthalpyEquationType)
            };

        private static IPropertyEvaluator<Temperature, MolarVolumeSpecific> CreateSaturatedMolarVolumeEvaluator(ChemicalComponentDto dto)
            => dto.SaturatedMolarVolumeEquationType switch
            {
                SaturatedMolarVolumeEquationType.IapwsSteamTables => new WaterSaturatedMolarVolumeEvaluator(),
                SaturatedMolarVolumeEquationType.Rackett => new RackettSaturatedMolarVolumeEvaluator(
                    dto.CriticalTemperature,
                    dto.CriticalPressure,
                    dto.AcentricFactor),
                _ => throw Unsupported(dto.Name, nameof(dto.SaturatedMolarVolumeEquationType), dto.SaturatedMolarVolumeEquationType)
            };

        private static IPropertyEvaluator<Temperature, MolarEnergy> CreateGasEnthalpyEvaluator(
            ChemicalComponentDto dto,
            IPropertyEvaluator<Temperature, MolarEnergy> heatOfVaporizationEvaluator,
            IPropertyEvaluator<Pressure, Temperature> saturationTemperatureEvaluator)
            => dto.GasEnthalpyEquationType switch
            {
                GasEnthalpyEquationType.IapwsSteamTables => new WaterGasEnthalpyEvaluator(),
                GasEnthalpyEquationType.IntegratedGasCpWithHvap => new GasEnthalpyEvaluator(
                    dto.GasHeatCapacity,
                    dto.LiquidHeatCapacity,
                    heatOfVaporizationEvaluator,
                    saturationTemperatureEvaluator,
                    new Pressure(1.01325, PressureUnits.Bara),
                    dto.MolecularWeight),
                _ => throw Unsupported(dto.Name, nameof(dto.GasEnthalpyEquationType), dto.GasEnthalpyEquationType)
            };

        private static NotSupportedException Unsupported<TEnum>(string componentName, string propertyName, TEnum equationType)
            where TEnum : struct, Enum
            => new($"Equation type '{equationType}' is not supported for {propertyName} on component '{componentName}'.");
    }


}
