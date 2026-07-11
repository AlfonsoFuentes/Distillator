using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Entities.Thermodynamics.Methods.Configurations
{
    public class BinaryInteractionParameterConfiguration : IEntityTypeConfiguration<BinaryInteractionParameter>
    {
        public void Configure(EntityTypeBuilder<BinaryInteractionParameter> builder)
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
            // 2. CONFIGURACIÓN PROPIA
            // ==========================================
            builder.Property(p => p.Value).IsRequired();

            builder.HasOne(p => p.ComponentI)
                .WithMany()
                .HasForeignKey(p => p.ComponentI_Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ComponentJ)
                .WithMany()
                .HasForeignKey(p => p.ComponentJ_Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
