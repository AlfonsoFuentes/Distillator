using Microsoft.AspNetCore.Identity;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Identity;
using Shared.Results;

namespace Server.Entities.UserManagement.EndPoints
{
    public class CreateUserEndpoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/CreateUserRequest", async (
                CreateUserRequest request,
                UserManager<ApplicationUser> userManager) =>
            {
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    IsActive = true,
                    MustChangePassword = true, // 🔒 Bandera activada
                    CreatedAt = DateTime.UtcNow
                };

                // Clave genérica que cumple con las reglas de Identity
                string temporaryPassword = "TemporalPassword123!";

                var result = await userManager.CreateAsync(user, temporaryPassword);

                if (!result.Succeeded)
                {
                    // Devolvemos los errores (ej. "El correo ya existe")
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result.Fail($"Error: {errors}");
                }

                // Asignación estricta del rol "Creator"
                await userManager.AddToRoleAsync(user, "Creator");

                return Result.Success($"User created successfully. The temporary password is: {temporaryPassword}");
            })
           .RequireAuthorization(policy => policy.RequireRole("Administrator"))
            .WithTags("Identity");
        }
    }
}
