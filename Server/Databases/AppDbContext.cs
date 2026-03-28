using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Server.Entities.BaseStructure.Components;
using Server.Entities.BaseStructure.Components.Configurations;
using Server.Entities.Thermodynamics.Methods;
using Server.Entities.UserManagement;

namespace Server.Databases
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

      

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 👇 ESTA ES LA LÍNEA MÁGICA 👇
            // Escanea el ensamblado y aplica automáticamente todas las clases que hereden de IEntityTypeConfiguration
            builder.ApplyConfigurationsFromAssembly(typeof(ChemicalComponentConfiguration).Assembly);
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
