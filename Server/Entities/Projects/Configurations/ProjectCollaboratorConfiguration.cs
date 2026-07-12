using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Entities.Projects.Configurations
{
    public class ProjectCollaboratorConfiguration : IEntityTypeConfiguration<ProjectCollaborator>
    {
        public void Configure(EntityTypeBuilder<ProjectCollaborator> builder)
        {
            builder.ToTable("ProjectCollaborators");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.CreatedOn).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.Order).HasDefaultValue(0);
            builder.Ignore(x => x.IsTenanted);

            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);

            builder.HasOne(x => x.Project)
                .WithMany(x => x.Collaborators)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.UserId }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.UserId });
        }
    }
}
