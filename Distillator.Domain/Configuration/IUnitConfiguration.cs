using UnitSystem;

namespace Distillator.Domain.Configuration
{
    /// <summary>
    /// Configuración de unidades por defecto de un proyecto.
    /// Cuando se crea una corriente o equipo nuevo, se usa la unidad
    /// definida aquí. El usuario puede cambiarla en ejecución (Amount permite convertir).
    /// </summary>
    public interface IUnitConfiguration
    {
        /// <summary>Unidad por defecto para presiones (ej: bar, psi, kPa).</summary>
        UnitMeasure DefaultPressureUnit { get; set; }

        /// <summary>Unidad por defecto para temperaturas (ej: °C, K, °F).</summary>
        UnitMeasure DefaultTemperatureUnit { get; set; }

        /// <summary>Unidad por defecto para flujos másicos (ej: kg/h, lb/h, ton/d).</summary>
        UnitMeasure DefaultMassFlowUnit { get; set; }

        /// <summary>Unidad por defecto para flujos molares (ej: kgmol/h, lbmol/h).</summary>
        UnitMeasure DefaultMolarFlowUnit { get; set; }

        /// <summary>Unidad por defecto para energía / entalpía (ej: kJ, BTU, kcal).</summary>
        UnitMeasure DefaultEnergyUnit { get; set; }

        /// <summary>Unidad por defecto para potencia (ej: kW, hp, MW).</summary>
        UnitMeasure DefaultPowerUnit { get; set; }

        /// <summary>Unidad por defecto para longitud/diámetro (ej: m, mm, in).</summary>
        UnitMeasure DefaultLengthUnit { get; set; }

        UnitMeasure DefaultDiameterUnit { get; set; }
        UnitMeasure DefaultSurfaceUnit { get; set; }
        UnitMeasure DefaultVolumeUnit { get; set; }
        UnitMeasure DefaultTimeUnit { get; set; }
        UnitMeasure DefaultVelocityUnit { get; set; }
        UnitMeasure DefaultMassUnit { get; set; }
        UnitMeasure DefaultForceUnit { get; set; }
        UnitMeasure DefaultElectricUnit { get; set; }
        UnitMeasure DefaultMotorVelocityUnit { get; set; }
        UnitMeasure DefaultAmountOfSubstanceUnit { get; set; }
        UnitMeasure DefaultHeatTransferCoefficientUnit { get; set; }

        /// <summary>Unidad por defecto para densidad (ej: kg/m³, lb/ft³).</summary>
        UnitMeasure DefaultDensityUnit { get; set; }

        UnitMeasure DefaultMolarDensityUnit { get; set; }
        UnitMeasure DefaultMassVolumeSpecificUnit { get; set; }
        UnitMeasure DefaultMolarVolumeSpecificUnit { get; set; }
        UnitMeasure DefaultPressureDropLengthUnit { get; set; }
        UnitMeasure DefaultPressureDropUnit { get; set; }

        /// <summary>Unidad por defecto para viscosidad (ej: cP, Pa·s).</summary>
        UnitMeasure DefaultViscosityUnit { get; set; }

        /// <summary>Unidad por defecto para conductividad térmica.</summary>
        UnitMeasure DefaultThermalConductivityUnit { get; set; }

        UnitMeasure DefaultVolumeEnergyUnit { get; set; }
        UnitMeasure DefaultMassEnergyUnit { get; set; }
        UnitMeasure DefaultMolarEnergyUnit { get; set; }
        UnitMeasure DefaultMassEntropyUnit { get; set; }
        UnitMeasure DefaultMolarEntropyUnit { get; set; }
        UnitMeasure DefaultHeatSurfaceFlowUnit { get; set; }
        UnitMeasure DefaultVolumetricFlowUnit { get; set; }
        UnitMeasure DefaultEnergyFlowUnit { get; set; }
        UnitMeasure DefaultSuperficialTensionUnit { get; set; }
    }

    public interface IProjectUnitSystem
    {
        string Name { get; set; }
        bool IsBuiltIn { get; set; }
        IUnitConfiguration Units { get; set; }
    }
}
