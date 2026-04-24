using Microsoft.AspNetCore.Identity;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Identity;
using Shared.Results;
using System.Security.Claims;

namespace Server.Entities.UserManagement.EndPoints
{
    public class UserInfoEndpoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/GetUserInfoRequest", async (
                GetUserInfoRequest request, // Recibimos el DTO vacío para que coincida con tu HttpServices
                ClaimsPrincipal claimsPrincipal, // ASP.NET inyecta esto mágicamente gracias a la Cookie
                UserManager<ApplicationUser> userManager) =>
            {
                // 1. Verificamos si la cookie es válida y el usuario está autenticado
                if (claimsPrincipal.Identity?.IsAuthenticated != true)
                {
                    return Result<UserInfoResponse>.Fail("User is not authenticated.");
                }

                // 2. Extraemos el ID del usuario directamente de los claims de la cookie
                var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Result<UserInfoResponse>.Fail("Invalid session.");
                }

                // 3. Buscamos al usuario en la base de datos para obtener datos frescos
                var user = await userManager.FindByIdAsync(userId);

                // Si el administrador lo deshabilitó mientras estaba logueado, lo bloqueamos
                if (user == null || !user.IsActive)
                {
                    return Result<UserInfoResponse>.Fail("User account is disabled or missing.");
                }

                // 4. Obtenemos los roles actualizados desde la base de datos
                var roles = await userManager.GetRolesAsync(user);

                // 5. Construimos el DTO de respuesta
                var responseData = new UserInfoResponse
                {
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName, // Usamos la propiedad calculada de tu ApplicationUser
                    Roles = roles.ToList()
                };

                return Result<UserInfoResponse>.Success(responseData, "User info retrieved.");
            })
            .AllowAnonymous() // 🔒 Esto asegura que el endpoint rechace peticiones sin Cookie válida
            .WithTags("Identity");
        }
    }
}
