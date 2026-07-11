using Distillator.Domain.Configuration;
using Shared.PropertiesDtos.Methods;

namespace Distillator.Domain.Configuration
{
    /// <summary>
    /// Configuración global de un proyecto. Aplicable a todos sus flowsheets.
    /// Hereda defaults del sistema o del usuario; puede sobrescribirse por proyecto.
    /// </summary>
    public interface IProjectConfiguration
    {
        /// <summary>Sistema de unidades por defecto del proyecto.</summary>
        IUnitConfiguration UnitDefaults { get; set; }

        /// <summary>Sistemas de unidades disponibles para el proyecto.</summary>
        IList<IProjectUnitSystem> UnitSystems { get; set; }

        /// <summary>Nombre del sistema de unidades activo del proyecto.</summary>
        string ActiveUnitSystemName { get; set; }

        /// <summary>Configuración de cámara/visualización por defecto del proyecto.</summary>
        ICameraConfiguration CameraDefaults { get; set; }

        /// <summary>Configuración del servicio de nombrado de equipos del proyecto.</summary>
        INamingConfiguration NamingConfig { get; set; }

        /// <summary>Id del método termodinámico activo (ej: NRTL, Peng-Robinson).</summary>
        Guid ThermodynamicMethodId { get; set; }

        /// <summary>Método termodinámico completo activo en el proyecto.</summary>
        ThermodynamicMethodFullDto? ThermodynamicMethod { get; set; }

        /// <summary>Configuración de reportes del proyecto.</summary>
        IReportConfiguration ReportConfig { get; set; }

        /// <summary>Configuración de diseño de equipos (estándares, etc.).</summary>
        IEquipmentDesignConfiguration EquipmentDesignConfig { get; set; }

        /// <summary>Altitud de la planta sobre el nivel del mar.</summary>
        UnitSystem.Length PlantElevation { get; set; }
    }

    public interface IReportConfiguration
    {
        IEnumerable<string> AvailableTemplates { get; set; }
        string DefaultFormat { get; set; } // PDF, Excel
        bool AutoExportOnSimulation { get; set; }
    }

    public interface IEquipmentDesignConfiguration
    {
        string Standard { get; set; } // API, TEMA, ASME
        string RatingBasis { get; set; } // maximum, normal
    }
}
