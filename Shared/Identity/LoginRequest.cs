using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Identity
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        // Esta propiedad le dirá al servidor si debe crear una cookie persistente (larga duración)
        // o una cookie de sesión (que se borra al cerrar el navegador).
        public bool RememberMe { get; set; }
    }
    public class GetUserInfoRequest
    {
    }

    public class UserInfoResponse
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
    public class LogoutRequest
    {
        // Vacío, solo lo usamos para el ruteo de tu HttpServices
    }

    // DTO vacío para solicitar la lista (para tu HttpServices)
   
}
