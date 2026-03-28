using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Entities.Thermodynamics.Methods.Configurations
{
    public class MethodComponentConfiguration : IEntityTypeConfiguration<MethodComponent>
    {
        public void Configure(EntityTypeBuilder<MethodComponent> builder)
        {
           

            // ==========================================
            // 1. CONFIGURACIÓN DE LA CLASE BASE (Entity)
            // ==========================================
            builder.HasKey(x => x.Id);
            builder.HasQueryFilter(x => !x.IsDeleted);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.CreatedOn).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.Order).HasDefaultValue(0);
            builder.Ignore(x => x.IsTenanted);

            // ==========================================
            // 2. CONFIGURACIÓN PROPIA
            // ==========================================
            // Índice único para evitar duplicar el mismo componente en el mismo método
            builder.HasIndex(mc => new { mc.MethodId, mc.ComponentId }).IsUnique();
            builder.Property(mc => mc.MatrixIndex).IsRequired();

            builder.HasOne(mc => mc.Method)
                .WithMany(m => m.MethodComponents)
                .HasForeignKey(mc => mc.MethodId);

            builder.HasOne(mc => mc.Component)
                .WithMany()
                .HasForeignKey(mc => mc.ComponentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
