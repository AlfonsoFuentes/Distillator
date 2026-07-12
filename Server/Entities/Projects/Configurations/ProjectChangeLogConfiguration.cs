using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Entities.Projects.Configurations
{
    public class ProjectChangeLogConfiguration : IEntityTypeConfiguration<ProjectChangeLog>
    {
        public void Configure(EntityTypeBuilder<ProjectChangeLog> builder)
        {
            builder.ToTable("ProjectChangeLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.CreatedOn).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.Order).HasDefaultValue(0);
            builder.Ignore(x => x.IsTenanted);

            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Operation).HasConversion<string>().HasMaxLength(40);
            builder.Property(x => x.EntityType).IsRequired().HasMaxLength(80);
            builder.Property(x => x.EntityId).IsRequired().HasMaxLength(80);
            builder.Property(x => x.Path).IsRequired().HasMaxLength(240);
            builder.Property(x => x.OldValueJson).HasColumnType("jsonb");
            builder.Property(x => x.NewValueJson).HasColumnType("jsonb");

            builder.HasOne(x => x.Project)
                .WithMany(x => x.ChangeLogs)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.ProjectVersion });
            builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.OccurredOnUtc });
        }
    }
}
