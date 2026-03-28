using BlazorDownloadFile;
using Client.Services.HttpServices;
using Client.Services.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Globalization;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace Client.Services
{
    public static class WebAssemblyHostBuilderExtensions
    {
        public static string ClientName = "API";
        public static WebAssemblyHostBuilder AddRootComponents(this WebAssemblyHostBuilder builder)
        {
            builder.RootComponents.Add<App>("#app");

            return builder;
        }

        public static WebAssemblyHostBuilder AddClientServices(this WebAssemblyHostBuilder builder)
        {

            builder
                .Services

               
               

                .AddScoped(sp => sp
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(ClientName).EnableIntercept(sp))
                .AddHttpClient(ClientName, client =>
                {
                    client.DefaultRequestHeaders.AcceptLanguage.Clear();
                    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(CultureInfo.DefaultThreadCurrentCulture?.TwoLetterISOLanguageName);
                    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); // Fix: Use builder.HostEnvironment.BaseAddress

                });
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            builder.Services.AddMudServices();
            builder.Services.AddHttpClientInterceptor();
            builder.Services.AddScoped<IHttpService, HttpService>();
            builder.Services.AddScoped<ISnackBarService, SnackBarService>();
            builder.Services.AddBlazorDownloadFile();
            builder.Services.AddScoped<ExcelExportService>();
            builder.Services.AddScoped<ChemicalComponentReportService>();
            builder.Services.AddScoped<ThermodynamicMethodReportService>();
            
            builder.Logging.SetMinimumLevel(LogLevel.Information);
     
            return builder;
        }


    }
}
