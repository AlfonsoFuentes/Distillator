using Microsoft.EntityFrameworkCore;

namespace Server.Databases
{
    public interface IAppDbContext
    {

        DbSet<TEntity> Set<TEntity>() where TEntity : class;
    }
}
