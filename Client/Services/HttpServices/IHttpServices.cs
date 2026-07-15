using Shared.Results;
using System.Net.Http.Json;

namespace Client.Services.HttpServices
{
    public interface IHttpService
    {
        // ❌ VIEJO: Permitía cualquier TResponse (peligro de Activator.CreateInstance)
        // Task<TResponse> PostAsync<TRequest, TResponse>(TRequest request)
        //     where TRequest : class
        //     where TResponse : class;

        // ✅ NUEVO: Solo acepta Result<T> — fuerza el patrón del proyecto
        Task<Result<T>> PostAsync<TRequest, T>(TRequest request, bool showSnackbar = true) where TRequest : class;
        // ✅ NUEVO: Para endpoints que devuelven Result sin datos
        Task<Result> PostAsync<TRequest>(TRequest request, bool showSnackbar = true) where TRequest : class;

        Task<bool> PostForValidationAsync<TRequest>(TRequest request) where TRequest : class;
    }
    public partial class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly ISnackBarService _snackbarService; // ✅ Tu servicio

        public HttpService(IHttpClientFactory httpClientFactory, ISnackBarService snackbarService)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _snackbarService = snackbarService;
        }

        // ❌ VIEJO: Genérico sin restricción de Result<T>
        // public async Task<TResponse> PostAsync<TRequest, TResponse>(TRequest request)
        //  where TRequest : class
        //  where TResponse : class
        // {
        //     try
        //     {
        //         var endpoint = request.GetType().Name;
        //         var response = await _httpClient.PostAsJsonAsync(endpoint, request);
        //
        //         if (response.IsSuccessStatusCode)
        //         {
        //             var result = await response.Content.ReadFromJsonAsync<TResponse>()
        //                 ?? Activator.CreateInstance<TResponse>();
        //
        //             if (result is Result generalDto)
        //                 _snackbarService.ShowMessage(generalDto);
        //
        //             return result;
        //         }
        //
        //         var errorContent = await response.Content.ReadAsStringAsync();
        //         var message = $"Error {response.StatusCode}: {errorContent}".Truncate(200);
        //
        //         _snackbarService.ShowError(message);
        //         return Activator.CreateInstance<TResponse>();
        //     }
        //     catch (HttpRequestException ex)
        //     {
        //         var message = ex.InnerException?.Message ?? ex.Message;
        //         _snackbarService.ShowError($"Connection error: {message}");
        //         return Activator.CreateInstance<TResponse>();
        //     }
        //     catch (TaskCanceledException)
        //     {
        //         _snackbarService.ShowError("Request timed out. Please try again.");
        //         return Activator.CreateInstance<TResponse>();
        //     }
        //     catch (Exception ex)
        //     {
        //         _snackbarService.ShowError($"Unexpected error: {ex.Message}");
        //         return Activator.CreateInstance<TResponse>();
        //     }
        // }

        // ✅ NUEVO: PostAsync que devuelve Result<T>
        public async Task<Result<T>> PostAsync<TRequest, T>(TRequest request, bool showSnackbar = true) where TRequest : class
        {
            try
            {
                var endpoint = ResolveEndpoint(request);
                var response = await _httpClient.PostAsJsonAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Result<T>>();
                    if (result != null)
                    {
                        if (showSnackbar && result is Result generalDto)
                            _snackbarService.ShowMessage(generalDto);
                        return result;
                    }
                    return new Result<T> { Succeeded = false, Messages = new List<string> { "Empty response" } };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var message = $"Error {response.StatusCode}: {errorContent}".Truncate(200);

                if (showSnackbar)
                    _snackbarService.ShowError(message);
                return Result<T>.Fail(message);
            }
            catch (HttpRequestException ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                if (showSnackbar)
                    _snackbarService.ShowError($"Connection error: {message}");
                return Result<T>.Fail($"Connection error: {message}");
            }
            catch (TaskCanceledException)
            {
                if (showSnackbar)
                    _snackbarService.ShowError("Request timed out. Please try again.");
                return Result<T>.Fail("Request timed out. Please try again.");
            }
            catch (Exception ex)
            {
                if (showSnackbar)
                    _snackbarService.ShowError($"Unexpected error: {ex.Message}");
                return Result<T>.Fail($"Unexpected error: {ex.Message}");
            }
        }

        // ✅ NUEVO: PostAsync que devuelve Result (sin tipo de datos)
        public async Task<Result> PostAsync<TRequest>(TRequest request, bool showSnackbar = true) where TRequest : class
        {
            try
            {
                var endpoint = ResolveEndpoint(request);
                var response = await _httpClient.PostAsJsonAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Result>();
                    if (result != null)
                    {
                        if (showSnackbar)
                            _snackbarService.ShowMessage(result);
                        return result;
                    }
                    return new Result { Succeeded = false, Messages = new List<string> { "Empty response" } };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var message = $"Error {response.StatusCode}: {errorContent}".Truncate(200);

                if (showSnackbar)
                    _snackbarService.ShowError(message);
                return new Result { Succeeded = false, Messages = new List<string> { message } };
            }
            catch (HttpRequestException ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                if (showSnackbar)
                    _snackbarService.ShowError($"Connection error: {message}");
                return new Result { Succeeded = false, Messages = new List<string> { $"Connection error: {message}" } };
            }
            catch (TaskCanceledException)
            {
                if (showSnackbar)
                    _snackbarService.ShowError("Request timed out. Please try again.");
                return new Result { Succeeded = false, Messages = new List<string> { "Request timed out. Please try again." } };
            }
            catch (Exception ex)
            {
                if (showSnackbar)
                    _snackbarService.ShowError($"Unexpected error: {ex.Message}");
                return new Result { Succeeded = false, Messages = new List<string> { $"Unexpected error: {ex.Message}" } };
            }
        }
        // ✅ Nuevo método: solo para validaciones (devuelve bool, sin Snackbar)
        public async Task<bool> PostForValidationAsync<TRequest>(TRequest request)
            where TRequest : class
        {
            var endpoint = ResolveEndpoint(request);
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);

            // ✅ Solo 200 OK es éxito; cualquier otro código → false
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<bool>>();
                return result?.Succeeded == true && result.Data == true;
            }

            return false;
        }

        private static string ResolveEndpoint<TRequest>(TRequest request)
            where TRequest : class
        {
            return request.GetType().Name;
        }
    }
}
