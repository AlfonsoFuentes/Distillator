using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Server.Entities;
using Server.Entities.BaseStructure.Components;
using Server.Entities.BaseStructure.Components.Configurations;
using Server.Entities.Projects;
using Server.Entities.Thermodynamics.Methods;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Results;
using System.Reflection;

namespace Server.Databases
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(new SoftDeleteInterceptor());
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 👇 ESTA ES LA LÍNEA MÁGICA 👇
            // Escanea el ensamblado y aplica automáticamente todas las clases que hereden de IEntityTypeConfiguration
            builder.ApplyConfigurationsFromAssembly(typeof(ChemicalComponentConfiguration).Assembly);

            // Global Query Filter para todas las entidades que implementan ISoftDeletable
            foreach (var entityType in builder.Model.GetEntityTypes()
                .Where(e => typeof(ISoftDeletable).IsAssignableFrom(e.ClrType) && !e.IsOwned()))
            {
                ApplySoftDeleteFilter(builder, entityType.ClrType);
            }
        }

        private static void ApplySoftDeleteFilter(ModelBuilder builder, Type entityType)
        {
            var method = typeof(ApplicationDbContext)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .First(m => m.Name == nameof(ApplySoftDeleteFilterGeneric) && m.IsGenericMethod)
                .MakeGenericMethod(entityType);
            method.Invoke(null, new object[] { builder });
        }

        private static void ApplySoftDeleteFilterGeneric<TEntity>(ModelBuilder builder) where TEntity : class, ISoftDeletable
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new DbUpdateConcurrencyException(
                    $"{ex.Message}{Environment.NewLine}{BuildConcurrencyDetails(ex.Entries)}",
                    ex);
            }
        }

        public async Task<Result> SaveResultAsync(
            string successMessage,
            string failMessage,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var affectedRows = await SaveChangesAsync(cancellationToken);

                return affectedRows > 0
                    ? (Result)Result.Success(successMessage)
                    : (Result)Result.Fail(failMessage);
            }
            catch (DbUpdateException ex)
            {
                return (Result)Result.Fail(ex.Message);
            }
        }

        public async Task<Result<T>> SaveResultAsync<T>(
            Func<T> dataFactory,
            string successMessage,
            string failMessage,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var affectedRows = await SaveChangesAsync(cancellationToken);

                return affectedRows > 0
                    ? Result<T>.Success(dataFactory(), successMessage)
                    : Result<T>.Fail(failMessage);
            }
            catch (DbUpdateException ex)
            {
                return Result<T>.Fail(ex.Message);
            }
        }

        private string BuildConcurrencyDetails(IEnumerable<EntityEntry> entries)
        {
            var lines = new List<string> { "Concurrency entries:" };

            foreach (var entry in entries)
            {
                var keyValues = entry.Properties
                    .Where(property => property.Metadata.IsPrimaryKey())
                    .Select(property => $"{property.Metadata.Name}={property.CurrentValue}");

                var modifiedValues = entry.Properties
                    .Where(property => property.IsModified)
                    .Select(property => $"{property.Metadata.Name}: original={property.OriginalValue}, current={property.CurrentValue}");

                lines.Add($"- {entry.Metadata.ClrType.Name} State={entry.State} Keys=[{string.Join(", ", keyValues)}]");
                lines.Add($"  Modified=[{string.Join("; ", modifiedValues)}]");
            }

            lines.Add("Tracked entries:");
            lines.AddRange(ChangeTracker.Entries()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(entry => $"- {entry.Metadata.ClrType.Name} State={entry.State}"));

            return string.Join(Environment.NewLine, lines);
        }

        public DbSet<ChemicalComponent> ChemicalComponents {  get; set; }
        // ==========================================
        // THERMODYNAMIC METHODS MODULE
        // ==========================================
        public DbSet<ThermodynamicMethod> ThermodynamicMethods { get; set; }
        public DbSet<MethodComponent> MethodComponents { get; set; }
        public DbSet<BinaryInteractionParameter> BinaryInteractionParameters { get; set; }

        // ==========================================
        // PROJECT PERSISTENCE MODULE
        // ==========================================
        public DbSet<ProjectRecord> Projects { get; set; }
        public DbSet<ProjectCollaborator> ProjectCollaborators { get; set; }
        public DbSet<ProjectDiagramRecord> ProjectDiagrams { get; set; }
        public DbSet<ProjectChangeLog> ProjectChangeLogs { get; set; }
        public DbSet<ProjectUserWorkspaceState> ProjectUserWorkspaceStates { get; set; }
    }
}
