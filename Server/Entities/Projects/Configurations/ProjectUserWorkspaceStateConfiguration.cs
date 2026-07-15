using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Entities.Projects.Configurations
{
    public class ProjectUserWorkspaceStateConfiguration : IEntityTypeConfiguration<ProjectUserWorkspaceState>
    {
        public void Configure(EntityTypeBuilder<ProjectUserWorkspaceState> builder)
        {
            builder.ToTable("ProjectUserWorkspaceStates");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.CreatedOn).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.Order).HasDefaultValue(0);
            builder.Ignore(x => x.IsTenanted);

            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ExpandedDiagramTypeCodesJson).HasColumnType("jsonb");

            builder.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
        }
    }
}
