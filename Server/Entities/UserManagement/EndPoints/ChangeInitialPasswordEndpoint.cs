using Microsoft.AspNetCore.Identity;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Identity;
using Shared.Results;

namespace Server.Entities.UserManagement.EndPoints
{
    public class ChangeInitialPasswordEndpoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/ChangeInitialPasswordRequest", async (
                ChangeInitialPasswordRequest request,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager) =>
            {
                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    return Result.Fail("The new passwords do not match.");
                }

                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    return Result.Fail("Invalid request.");
                }

                // Intentamos cambiar la contraseña
                var changeResult = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

                if (!changeResult.Succeeded)
                {
                    var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
                    return Result.Fail(errors); // Ejemplo: "Debe contener un número, una mayúscula, etc."
                }

                // Si se cambió con éxito, apagamos la bandera
                user.MustChangePassword = false;
                await userManager.UpdateAsync(user);

                // Iniciamos sesión automáticamente para una experiencia fluida
                await signInManager.SignInAsync(user, isPersistent: false);

                return Result.Success("Password changed successfully. You are now logged in.");
            })
            .AllowAnonymous() // Debe ser anónimo porque aún no tiene sesión válida
            .WithTags("Identity");
        }
    }
}
