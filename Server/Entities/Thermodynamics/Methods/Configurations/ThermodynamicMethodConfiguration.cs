using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Entities.Thermodynamics.Methods.Configurations
{
    public class ThermodynamicMethodConfiguration : IEntityTypeConfiguration<ThermodynamicMethod>
    {
        public void Configure(EntityTypeBuilder<ThermodynamicMethod> builder)
        {
         

            // ==========================================
            // 1. CONFIGURACIÓN DE LA CLASE BASE (Entity)
            // ==========================================
            builder.HasKey(x => x.Id);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.CreatedOn).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.Order).HasDefaultValue(0);
            builder.Ignore(x => x.IsTenanted);

            // ==========================================
            // 2. CONFIGURACIÓN PROPIA DEL METODO
            // ==========================================
            builder.Property(m => m.Name).IsRequired().HasMaxLength(150);
            builder.Property(m => m.Description).HasMaxLength(500);

            builder.HasMany(m => m.BinaryParameters)
                .WithOne(p => p.Method)
                .HasForeignKey(p => p.MethodId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
