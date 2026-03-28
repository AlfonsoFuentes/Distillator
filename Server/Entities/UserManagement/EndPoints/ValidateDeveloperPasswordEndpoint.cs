using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Server.Services;
using Shared.Identity;
using Shared.Results;
using System.Security.Claims;

namespace Server.Entities.UserManagement.EndPoints
{
    public class ValidateDeveloperPasswordEndpoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/ValidateDeveloperPassword", async (
                ValidateDeveloperPassword request, // Recibimos el DTO que hace match con la ruta
                ClaimsPrincipal claimsPrincipal,   // ASP.NET inyecta esto mágicamente gracias a la Cookie
                UserManager<ApplicationUser> userManager) =>
            {
                // 1. Verificamos si la cookie es válida y el usuario está autenticado
                if (claimsPrincipal.Identity?.IsAuthenticated != true)
                {
                    return Result<bool>.Fail("User is not authenticated.");
                }

                // 2. Extraemos el ID del usuario directamente de los claims de la cookie
                var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Result<bool>.Fail("Invalid session.");
                }

                // 3. Buscamos al usuario en la base de datos para obtener datos frescos
                var user = await userManager.FindByIdAsync(userId);

                // Si el administrador lo deshabilitó mientras estaba logueado, lo bloqueamos
                if (user == null || !user.IsActive)
                {
                    return Result<bool>.Fail("User account is disabled or missing.");
                }

                // 4. Comparamos la contraseña recibida en el request contra el Hash de la BD
                bool isValid = await userManager.CheckPasswordAsync(user, request.Password);

                // 5. Retornamos el Result<bool> exacto que lee PostForValidationAsync
                return Result<bool>.Success(isValid, "Validation executed.");
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Developer" }) // 🔒 Asegura Cookie válida y Rol específico
            .WithTags("Security");
        }
    }
}
