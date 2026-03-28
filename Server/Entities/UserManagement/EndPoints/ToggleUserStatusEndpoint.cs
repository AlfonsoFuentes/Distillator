using Microsoft.AspNetCore.Identity;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Identity;
using Shared.Results;

namespace Server.Entities.UserManagement.EndPoints
{
    public class ToggleUserStatusEndpoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/ToggleUserStatusRequest", async (
                ToggleUserStatusRequest request,
                UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(request.UserId);

                if (user == null)
                {
                    return Result.Fail("User not found.");
                }

                // 🛡️ RED DE SEGURIDAD: Verificamos los roles antes de tocar el estado
                var roles = await userManager.GetRolesAsync(user);
                if (roles.Contains("Administrator"))
                {
                    // Devolvemos un error claro para que se muestre en el Snackbar del cliente
                    return Result.Fail("Critical Action Denied: Cannot modify the status of an Administrator.");
                }
    
                if (roles.Contains("Developer"))
                {
                    return Result.Fail("Action Denied: Developers cannot be disabled by administrators.");
                }

                // Invertimos el estado actual
                user.IsActive = !user.IsActive;

                var updateResult = await userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    var status = user.IsActive ? "enabled" : "disabled";
                    return Result.Success($"User account has been {status}.");
                }

                return Result.Fail("Failed to update user status.");
            })
           .RequireAuthorization(policy => policy.RequireRole("Administrator"))
            .WithTags("Identity");
        }
    }
}
