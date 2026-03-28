using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Entities.UserManagement
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        // Para el requisito del Administrador de habilitar/deshabilitar
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Propiedad calculada para simplificar la UI en MudBlazor
        public string FullName => $"{FirstName} {LastName}";
        public bool MustChangePassword { get; set; } = true;
    }
}
