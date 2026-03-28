using Microsoft.AspNetCore.Identity;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Identity;
using Shared.Results;

namespace Server.Entities.UserManagement.EndPoints
{
    public class RegisterEndpoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/RegisterRequest", async (
                RegisterRequest request,
                UserManager<ApplicationUser> userManager) =>
            {
                if (request.Password != request.ConfirmPassword)
                {
                    return Result.Fail("Passwords do not match.");
                }

                // Verificamos si el correo ya existe
                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return Result.Fail("An account with this email already exists.");
                }

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    IsActive = true, // Nace activo para que pueda entrar de inmediato
                    MustChangePassword = false, // Como él mismo puso la clave, no necesita cambiarla
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result.Fail(errors);
                }

                // 🛡️ REGLA DE NEGOCIO: Todo auto-registrado es estrictamente "Viewer"
                await userManager.AddToRoleAsync(user, "Viewer");

                return Result.Success("Registration successful. You can now sign in.");
            })
            .AllowAnonymous() // Vital para que funcione sin estar logueado
            .WithTags("Identity");
        }
    }
}
