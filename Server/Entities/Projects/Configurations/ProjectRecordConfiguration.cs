using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Entities.Projects.Configurations
{
    public class ProjectRecordConfiguration : IEntityTypeConfiguration<ProjectRecord>
    {
        public void Configure(EntityTypeBuilder<ProjectRecord> builder)
        {
            builder.ToTable("Projects");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.CreatedOn).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.Order).HasDefaultValue(0);
            builder.Ignore(x => x.IsTenanted);

            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(160);
            builder.Property(x => x.OwnerUserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.PlantElevationUnit).IsRequired().HasMaxLength(50);
            builder.Property(x => x.ActiveUnitSystemName).IsRequired().HasMaxLength(80);
            builder.Property(x => x.UpdatedBy).HasMaxLength(100);

            builder.Property(x => x.UnitSystemsJson).HasColumnType("jsonb");
            builder.Property(x => x.CameraConfigurationJson).HasColumnType("jsonb");
            builder.Property(x => x.NamingConfigurationJson).HasColumnType("jsonb");
            builder.Property(x => x.ReportConfigurationJson).HasColumnType("jsonb");
            builder.Property(x => x.EquipmentDesignConfigurationJson).HasColumnType("jsonb");

            builder.HasOne(x => x.OwnerUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.Name });
            builder.HasIndex(x => new { x.TenantId, x.OwnerUserId });
        }
    }
}
