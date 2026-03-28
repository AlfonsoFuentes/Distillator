using Microsoft.AspNetCore.Identity;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Identity;
using Shared.Results;

namespace Server.Entities.UserManagement.EndPoints
{
    public class LogoutEndpoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/LogoutRequest", async (
                LogoutRequest request, // Recibe el DTO para hacer match con la ruta
                SignInManager<ApplicationUser> signInManager) =>
            {
                // Esto destruye la cookie de autenticación en el navegador
                await signInManager.SignOutAsync();

                return Result.Success("Signed out successfully.");
            })
            .RequireAuthorization() // Solo permitimos llamar a esto si ya están logueados
            .WithTags("Identity");
        }
    }
}
