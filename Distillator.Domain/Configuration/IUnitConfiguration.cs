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

        /// <summary>Unidad por defecto para densidad (ej: kg/m³, lb/ft³).</summary>
        UnitMeasure DefaultDensityUnit { get; set; }

        /// <summary>Unidad por defecto para viscosidad (ej: cP, Pa·s).</summary>
        UnitMeasure DefaultViscosityUnit { get; set; }

        /// <summary>Unidad por defecto para conductividad térmica.</summary>
        UnitMeasure DefaultThermalConductivityUnit { get; set; }
    }

    public interface IProjectUnitSystem
    {
        string Name { get; set; }
        bool IsBuiltIn { get; set; }
        IUnitConfiguration Units { get; set; }
    }
}
