using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Server.Entities;
using Server.Entities.BaseStructure.Components;
using Server.Entities.BaseStructure.Components.Configurations;
using Server.Entities.Thermodynamics.Methods;
using Server.Entities.UserManagement;
using Server.Services;
using System.Reflection;

namespace Server.Databases
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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

        public DbSet<ChemicalComponent> ChemicalComponents {  get; set; }
        // ==========================================
        // THERMODYNAMIC METHODS MODULE
        // ==========================================
        public DbSet<ThermodynamicMethod> ThermodynamicMethods { get; set; }
        public DbSet<MethodComponent> MethodComponents { get; set; }
        public DbSet<BinaryInteractionParameter> BinaryInteractionParameters { get; set; }
    }
}
