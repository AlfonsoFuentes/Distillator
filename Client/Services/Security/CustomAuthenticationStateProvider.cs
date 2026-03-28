using Client.Services.HttpServices;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.Identity;
using Shared.Results;
using System.Security.Claims;

namespace Client.Services.Security
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpService _httpServices;

        public CustomAuthenticationStateProvider(IHttpService httpServices)
        {
            _httpServices = httpServices;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Consultamos silenciosamente al servidor si la cookie es válida
                var response = await _httpServices.PostAsync<GetUserInfoRequest, Result<UserInfoResponse>>(new GetUserInfoRequest());

                if (response != null && response.Succeeded && response.Data != null)
                {
                    // Si el servidor responde OK, construimos la identidad del usuario
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, response.Data.FullName),
                        new Claim(ClaimTypes.Email, response.Data.Email)
                    };

                    // Agregamos los roles para que el <AuthorizeView Roles="Administrator"> funcione
                    foreach (var role in response.Data.Roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    var identity = new ClaimsIdentity(claims, "CookieAuth");
                    var user = new ClaimsPrincipal(identity);

                    return new AuthenticationState(user);
                }
            }
            catch
            {
                // Si hay error de red o timeout, asumimos que no está logueado por seguridad
            }

            // Si falla, devolvemos un usuario anónimo (sin claims)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // Método vital para actualizar la UI en tiempo real cuando hacemos Login o Logout
        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
