using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Entities.Projects.Configurations
{
    public class ProjectDiagramRecordConfiguration : IEntityTypeConfiguration<ProjectDiagramRecord>
    {
        public void Configure(EntityTypeBuilder<ProjectDiagramRecord> builder)
        {
            builder.ToTable("ProjectDiagrams");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.CreatedOn).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.Order).HasDefaultValue(0);
            builder.Ignore(x => x.IsTenanted);

            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(160);
            builder.Property(x => x.TypeCode).IsRequired().HasMaxLength(80);
            builder.Property(x => x.DiagramNumber).HasMaxLength(40);
            builder.Property(x => x.CanvasStateJson).HasColumnType("jsonb");

            builder.HasOne(x => x.Project)
                .WithMany(x => x.Diagrams)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.Order });
            builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.DiagramNumber }).IsUnique();
        }
    }
}
