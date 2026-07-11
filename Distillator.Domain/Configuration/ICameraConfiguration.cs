namespace Distillator.Domain.Configuration;

/// <summary>
/// Configuración de la cámara/visualización de un proyecto.
/// Al crear un Flowsheet se copia esta configuración como default,
/// pero cada Flowsheet puede sobreescribir sus propios valores.
/// </summary>
public interface ICameraConfiguration
{
    /// <summary>Zoom inicial por defecto (1.0 = 100%).</summary>
    double DefaultZoom { get; set; }

    /// <summary>Pan X inicial por defecto.</summary>
    double DefaultPanX { get; set; }

    /// <summary>Pan Y inicial por defecto.</summary>
    double DefaultPanY { get; set; }

    /// <summary>Escala base del mundo del diagrama (ej: 0.7 = 70%).</summary>
    double GlobalScale { get; set; }

    /// <summary>Tamaño de la grilla en píxeles (ej: 20).</summary>
    double GridSize { get; set; }

    /// <summary>Zoom mínimo permitido.</summary>
    double MinZoom { get; set; }

    /// <summary>Zoom máximo permitido.</summary>
    double MaxZoom { get; set; }
}
