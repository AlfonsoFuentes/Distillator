using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Entities.UserManagement.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // Nombre de tabla personalizado
            builder.ToTable("Users");

            // Propiedades personalizadas que agregamos
            builder.Property(u => u.FirstName)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(u => u.LastName)
                   .HasMaxLength(50)
                   .IsRequired();

            // Aquí podrías agregar más configuraciones de Identity si las necesitas
            // como índices o relaciones específicas.
        }
    }
}
