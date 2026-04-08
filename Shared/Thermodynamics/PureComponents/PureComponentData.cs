using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents
{


    public class PureComponentData
    {
        // ========================================================================
        // PROPIEDADES ESCALARES (públicas - datos inmutables)
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
        // EVALUADORES PRIVADOS (Patrón Strategy)
        // ========================================================================
        private readonly IPropertyEvaluator<Temperature, Pressure> _vaporPressureEvaluator;
        private readonly IPropertyEvaluator<Pressure, Temperature> _saturationTemperatureEvaluator;
        private readonly IPropertyEvaluator<Temperature, MolarEnergy> _heatOfVaporizationEvaluator;
        private readonly IPropertyEvaluator<Temperature, MolarEntropy> _liquidHeatCapacityEvaluator;
        private readonly IPropertyEvaluator<Temperature, MolarEntropy> _gasHeatCapacityEvaluator;
        private readonly IPropertyEvaluator<Temperature, Viscosity> _liquidViscosityEvaluator;
        private readonly IPropertyEvaluator<Temperature, Viscosity> _gasViscosityEvaluator;
        private readonly IPropertyEvaluator<Temperature, ThermalConductivity> _liquidThermalCondEvaluator;
        private readonly IPropertyEvaluator<Temperature, ThermalConductivity> _gasThermalCondEvaluator;
        private readonly IPropertyEvaluator<Temperature, MolarDensity> _liquidDensityEvaluator;
        private readonly IPropertyEvaluator<Temperature, SuperficialTension> _surfaceTensionEvaluator;
        private readonly IPropertyEvaluator<Temperature, MolarEnergy> _liquidEnthalpyEvaluator;
        private readonly IPropertyEvaluator<Temperature, MolarEnergy> _gasEnthalpyEvaluator;
        private readonly IPropertyEvaluator<Temperature, MolarVolumeSpecific> _saturatedMolarVolumeEvaluator;

        // ========================================================================
        // MÉTODOS PÚBLICOS (API para las otras clases)
        // ========================================================================

        // Presión de vapor
        public Pressure GetVaporPressure(Temperature temperature)
            => _vaporPressureEvaluator.EvaluateAt(temperature);

        // Temperatura de saturación
        public Temperature GetSaturationTemperature(Pressure pressure)
            => _saturationTemperatureEvaluator.EvaluateAt(pressure);

        // Calor de vaporización
        public MolarEnergy GetHeatOfVaporization(Temperature temperature)
            => _heatOfVaporizationEvaluator.EvaluateAt(temperature);

        // Cp líquido
        public MolarEntropy GetLiquidHeatCapacity(Temperature temperature)
            => _liquidHeatCapacityEvaluator.EvaluateAt(temperature);

        // Cp gas
        public MolarEntropy GetGasHeatCapacity(Temperature temperature)
            => _gasHeatCapacityEvaluator.EvaluateAt(temperature);

        // Viscosidad líquida
        public Viscosity GetLiquidViscosity(Temperature temperature)
            => _liquidViscosityEvaluator.EvaluateAt(temperature);

        // Viscosidad gas
        public Viscosity GetGasViscosity(Temperature temperature)
            => _gasViscosityEvaluator.EvaluateAt(temperature);

        // Conductividad térmica líquida
        public ThermalConductivity GetLiquidThermalConductivity(Temperature temperature)
            => _liquidThermalCondEvaluator.EvaluateAt(temperature);

        // Conductividad térmica gas
        public ThermalConductivity GetGasThermalConductivity(Temperature temperature)
            => _gasThermalCondEvaluator.EvaluateAt(temperature);

        // Densidad líquida
        public MolarDensity GetLiquidDensity(Temperature temperature)
            => _liquidDensityEvaluator.EvaluateAt(temperature);

        // Tensión superficial
        public SuperficialTension GetSurfaceTension(Temperature temperature)
            => _surfaceTensionEvaluator.EvaluateAt(temperature);

        // Entalpía líquida
        public MolarEnergy GetLiquidEnthalpy(Temperature temperature)
            => _liquidEnthalpyEvaluator.EvaluateAt(temperature);

        // Entalpía gas
        public MolarEnergy GetGasEnthalpy(Temperature temperature)
            => _gasEnthalpyEvaluator.EvaluateAt(temperature);

        // Volumen molar saturado
        public MolarVolumeSpecific GetSaturatedMolarVolume(Temperature temperature)
            => _saturatedMolarVolumeEvaluator.EvaluateAt(temperature);

        // ========================================================================
        // CONSTRUCTOR
        // ========================================================================
        public PureComponentData(
            Guid id, string name, string formula, string structFormula,
            string family, string secFamily,
            double mw, Temperature tc, Pressure pc, MolarVolumeSpecific vc, double zc,
            Temperature tb, Temperature tm, MolarVolumeSpecific vAsterisk,
            double acentric, double acentricPitzer,
            MolarEnergy hForm, MolarEnergy gForm, MolarEntropy sForm, MolarEnergy hComb,

            IPropertyEvaluator<Temperature, Pressure> vaporPressure,
            IPropertyEvaluator<Pressure, Temperature> saturationTemperature,
            IPropertyEvaluator<Temperature, MolarEnergy> heatOfVap,
            IPropertyEvaluator<Temperature, MolarEntropy> liqHeatCap,
            IPropertyEvaluator<Temperature, MolarEntropy> gasHeatCap,
            IPropertyEvaluator<Temperature, Viscosity> liqVisc,
            IPropertyEvaluator<Temperature, Viscosity> gasVisc,
            IPropertyEvaluator<Temperature, ThermalConductivity> liqThermCond,
            IPropertyEvaluator<Temperature, ThermalConductivity> gasThermCond,
            IPropertyEvaluator<Temperature, MolarDensity> liqDensity,
            IPropertyEvaluator<Temperature, SuperficialTension> surfaceTension,
            IPropertyEvaluator<Temperature, MolarEnergy> liquidEnthalpy,
            IPropertyEvaluator<Temperature, MolarEnergy> gasEnthalpy,
            IPropertyEvaluator<Temperature, MolarVolumeSpecific> saturatedMolarVolumeEvaluator)
        {
            Id = id;
            Name = name;
            Formula = formula;
            StructuralFormula = structFormula;
            Family = family;
            SecondaryFamily = secFamily;
            MolecularWeight = mw;
            CriticalTemperature = tc;
            CriticalPressure = pc;
            CriticalVolume = vc;
            CriticalZ = zc;
            BoilingPoint = tb;
            MeltingPoint = tm;
            VolumeAsterisk = vAsterisk;
            AcentricFactor = acentric;
            AcentricFactorPitzer = acentricPitzer;
            EnthalpyForm = hForm;
            GibbsForm = gForm;
            EntropyForm = sForm;
            CombustionEnthalpy = hComb;

            // Evaluadores privados (Patrón Strategy)
            _vaporPressureEvaluator = vaporPressure;
            _saturationTemperatureEvaluator = saturationTemperature;
            _heatOfVaporizationEvaluator = heatOfVap;
            _liquidHeatCapacityEvaluator = liqHeatCap;
            _gasHeatCapacityEvaluator = gasHeatCap;
            _liquidViscosityEvaluator = liqVisc;
            _gasViscosityEvaluator = gasVisc;
            _liquidThermalCondEvaluator = liqThermCond;
            _gasThermalCondEvaluator = gasThermCond;
            _liquidDensityEvaluator = liqDensity;
            _surfaceTensionEvaluator = surfaceTension;
            _liquidEnthalpyEvaluator = liquidEnthalpy;
            _gasEnthalpyEvaluator = gasEnthalpy;
            _saturatedMolarVolumeEvaluator = saturatedMolarVolumeEvaluator;
        }
    }



}
