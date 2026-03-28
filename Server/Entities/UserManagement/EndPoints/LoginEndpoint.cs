using Microsoft.AspNetCore.Identity;
using Server.Entities.UserManagement;
using Server.Services;
using Shared.Identity;
using Shared.Results;

namespace Server.Entities.UserManagement.EndPoints
{
    public class LoginEndpoint : IEndPoint
    {
        public void MapEndPoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/LoginRequest", async (
                LoginRequest request,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                    return Result.Fail("Invalid email or password.");
                }

                // Validación de Administrador (Habilitado/Deshabilitado)
                if (!user.IsActive)
                {
                    return Result.Fail("Your account has been disabled. Please contact the Administrator.");
                }

                // 🛡️ INTERCEPCIÓN DE USUARIOS NUEVOS (Cambio de clave obligatorio)
                if (user.MustChangePassword)
                {
                    // Validamos que la clave ingresada sea la genérica correcta
                    var isPasswordCorrect = await userManager.CheckPasswordAsync(user, request.Password);

                    if (isPasswordCorrect)
                    {
                        // Devolvemos la bandera exacta que el Client leerá para redirigir
                        return Result.Fail("REQUIRE_PASSWORD_CHANGE");
                    }

                    return Result.Fail("Invalid email or password.");
                }

                // Proceso de Login estándar con soporte para Cookies
                var signInResult = await signInManager.PasswordSignInAsync(
                    user.UserName!,
                    request.Password,
                    isPersistent: request.RememberMe,
                    lockoutOnFailure: false);

                if (signInResult.Succeeded)
                {
                    return Result.Success("Sign in successful.");
                }

                return Result.Fail("Invalid email or password.");
            })
            .WithTags("Identity") // Agrupa los endpoints en Swagger
            .AllowAnonymous();    // Vital: Permite entrar sin estar logueado
        }
    }
}
