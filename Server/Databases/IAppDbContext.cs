using Microsoft.EntityFrameworkCore;
using Server.Entities.Projects;

namespace Server.Databases
{
    public interface IAppDbContext
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        DbSet<ProjectRecord> Projects { get; set; }
        DbSet<ProjectCollaborator> ProjectCollaborators { get; set; }
        DbSet<ProjectDiagramRecord> ProjectDiagrams { get; set; }
        DbSet<ProjectChangeLog> ProjectChangeLogs { get; set; }
    }
}
