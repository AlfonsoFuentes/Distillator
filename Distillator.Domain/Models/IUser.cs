using Distillator.Domain.Configuration;
using Distillator.Domain.Models;

namespace Distillator.Domain.Models
{
    /// <summary>
    /// Usuario del sistema. Raíz de la jerarquía de dominio.
    /// Un usuario tiene preferencias personales y N proyectos.
    /// </summary>
    public interface IUser
    {
        Guid Id { get; }
        string Email { get; }
        string FirstName { get; }
        string LastName { get; }
        string DisplayName => $"{FirstName} {LastName}".Trim();

        bool IsAdministrator { get; }
        bool IsActive { get; }
        DateTime CreatedAt { get; }

        /// <summary>
        /// Preferencias por defecto del usuario para nuevos proyectos.
        /// Heredan del sistema; el usuario puede personalizar.
        /// </summary>
        IProjectConfiguration DefaultPreferences { get; }

        /// <summary>Proyectos creados por este usuario.</summary>
        IReadOnlyCollection<IProject> Projects { get; }

        /// <summary>Crea un nuevo proyecto con las preferencias del usuario.</summary>
        IProject CreateProject(string name, IProjectConfiguration? configuration = null);

        /// <summary>Elimina un proyecto permanentemente (incluye todos sus flowsheets y snapshots).</summary>
        void RemoveProject(Guid projectId);

        /// <summary>Busca un proyecto por Id.</summary>
        IProject? GetProject(Guid id);

        /// <summary>Busca un proyecto por nombre.</summary>
        IProject? GetProjectByName(string name);
    }
}
