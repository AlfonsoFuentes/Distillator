using Client.Services.HttpServices;
using Distillator.Domain.Configuration;
using Distillator.Domain.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.Identity;
using Shared.Results;
using System.Security.Claims;

namespace Client.Services.Security
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpService _httpServices;
        private readonly IProjectConfiguration _defaultProjectPreferences;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private Task<UserInfoResponse?>? _currentUserInfoTask;

        public UserInfoResponse? CurrentUserInfo { get; private set; }
        public User? CurrentUser { get; private set; }
        public event Action? CurrentUserChanged;

        public CustomAuthenticationStateProvider(IHttpService httpServices, IProjectConfiguration? defaultProjectPreferences = null)
        {
            _httpServices = httpServices;
            _defaultProjectPreferences = defaultProjectPreferences ?? new ProjectConfiguration();
        }

        public async Task<UserInfoResponse?> GetCurrentUserInfoAsync()
        {
            if (CurrentUserInfo != null) return CurrentUserInfo;

            await _semaphore.WaitAsync();
            try
            {
                // Si mientras esperábamos otra llamada ya cargó la info, la devolvemos.
                if (CurrentUserInfo != null) return CurrentUserInfo;

                // Si ya hay una tarea en curso, esperamos a ella.
                if (_currentUserInfoTask != null)
                    return await _currentUserInfoTask;

                // Iniciamos la llamada HTTP y guardamos la tarea para que otros esperen.
                _currentUserInfoTask = LoadCurrentUserInfoAsync();
                CurrentUserInfo = await _currentUserInfoTask;
                CurrentUser = CreateDomainUser(CurrentUserInfo);
                CurrentUserChanged?.Invoke();
                return CurrentUserInfo;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<UserInfoResponse?> LoadCurrentUserInfoAsync()
        {
            var response = await _httpServices.PostAsync<GetUserInfoRequest, UserInfoResponse>(
                new GetUserInfoRequest(),
                showSnackbar: false);
            if (response?.Succeeded == true && response.Data != null)
            {
                return response.Data;
            }

            return null;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var userInfo = await GetCurrentUserInfoAsync();

                if (userInfo != null)
                {
                    // Si el servidor responde OK, construimos la identidad del usuario
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
                        new Claim(ClaimTypes.Name, userInfo.FullName),
                        new Claim(ClaimTypes.Email, userInfo.Email)
                    };

                    // Agregamos los roles para que el <AuthorizeView Roles="Administrator"> funcione
                    foreach (var role in userInfo.Roles)
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

        private User? CreateDomainUser(UserInfoResponse? userInfo)
        {
            if (userInfo == null) return null;
            if (!Guid.TryParse(userInfo.UserId, out var id)) return null;

            var (firstName, lastName) = SplitFullName(userInfo.FullName);
            var isAdministrator = userInfo.Roles.Contains("Administrator", StringComparer.OrdinalIgnoreCase);

            return new User(id, userInfo.Email, firstName, lastName, isAdministrator, _defaultProjectPreferences);
        }

        private static (string FirstName, string LastName) SplitFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (string.Empty, string.Empty);

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return (parts[0], string.Empty);

            var lastName = parts.Length > 2 ? string.Join(" ", parts.Skip(1)) : parts[1];
            return (parts[0], lastName);
        }

        // Método vital para actualizar la UI en tiempo real cuando hacemos Login o Logout
        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task RefreshAuthenticationStateAsync()
        {
            ClearUserInfo();
            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public void ClearUserInfo()
        {
            CurrentUserInfo = null;
            CurrentUser = null;
            _currentUserInfoTask = null;
            CurrentUserChanged?.Invoke();
        }
    }
}
