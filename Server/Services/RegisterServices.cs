using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Server.Databases;
using Server.Entities.UserManagement;
using System.Net;
using System.Reflection;

namespace Server.Services
{
    public static class RegisterServices
    {

        public static WebApplicationBuilder AddServerServices(this WebApplicationBuilder builder)
        {
        
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddEndPoints();
            builder.Services.AddForwarding(builder.Configuration);
            //QuestPDF.Settings.License = LicenseType.Community;
            builder.Services.AddCurrentUserService();

            builder.Services.AddDatabase(builder.Configuration);
            builder.Services.AddMemoryCache();
            builder.Services.AddSignalR();
         

           

            builder.Services.AddApplicationServices(builder.Configuration);
            builder.Services.AddRepositories();


            return builder;
        }
        public static IServiceCollection AddEndPoints(this IServiceCollection service)
        {
            service.AddEndPoints(Assembly.GetExecutingAssembly());
            return service;
        }
        static IServiceCollection AddEndPoints(this IServiceCollection service, Assembly assembly)
        {
            ServiceDescriptor[] serviceDescriptors = assembly
                .DefinedTypes
                .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                type.IsAssignableTo(typeof(IEndPoint)))
                .Select(type => ServiceDescriptor.Transient(typeof(IEndPoint), type)).ToArray();

            service.TryAddEnumerable(serviceDescriptors);
            return service;
        }
        internal static IServiceCollection AddForwarding(this IServiceCollection services, IConfiguration configuration)
        {
            var applicationSettingsConfiguration = configuration.GetSection(nameof(AppConfiguration));
            var config = applicationSettingsConfiguration.Get<AppConfiguration>();
            if (config!.BehindSSLProxy)
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    if (!string.IsNullOrWhiteSpace(config.ProxyIP))
                    {
                        var ipCheck = config.ProxyIP;
                        if (IPAddress.TryParse(ipCheck, out var proxyIP))
                            options.KnownProxies.Add(proxyIP);

                    }
                });
            }

            services.AddCors(options =>
            {
                // Le asignamos el nombre que estás llamando en UseApp
                options.AddPolicy("AllowBlazorWasm",
                    builder =>
                    {
                        builder
                            .AllowCredentials() // Vital para que viajen las Cookies
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithOrigins(config.ApplicationUrl.TrimEnd('/'));
                    });
            });

            return services;
        }
        internal static IServiceCollection AddCurrentUserService(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            return services;
        }
        internal static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            //services.AddDbContext<ApplicationDbContext>(options =>
            //        options.UseSqlServer(
            //            connectionString,
            //            sqlServerOptionsAction: sqlOptions =>
            //            {
            //                sqlOptions.EnableRetryOnFailure(
            //                    maxRetryCount: 5,
            //                    maxRetryDelay: TimeSpan.FromSeconds(30),
            //                    errorNumbersToAdd: null);
            //            }));
            services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(
            connectionString,
            npgsqlOptionsAction: pgOptions =>
            {
                pgOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null); // Nota: en Npgsql se llama errorCodesToAdd, no errorNumbersToAdd
            }));


            // CAMBIO 1: Usar AddIdentity en lugar de AddIdentityCore
            services.AddIdentity<ApplicationUser, IdentityRole>(options => {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders(); // Importante para recuperación de contraseñas futuras

            // CAMBIO 2: Evitar redirecciones HTML automáticas (vital para APIs)
            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

            return services;
        }


        internal static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            //services.Configure<MailConfiguration>(configuration.GetSection("MailConfiguration"));

            //services.AddTransient<IRoleClaimService, RoleClaimService>();
            //services.AddTransient<ITokenService, IdentityService>();
            //services.AddTransient<IRoleService, RoleService>();
            //services.AddTransient<IAccountService, AccountService>();
            //services.AddTransient<IUserService, UserService>();

            //services.AddTransient<IUploadService, UploadService>();
            ////services.AddTransient<IMailService, SMTPMailService>();
            //services.AddScoped<IExcelService, ExcelService>();
            return services;
        }
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            //services.AddScoped<IServerCrudService, ServerCrudService>();
            //services.AddScoped<IQueryRepository, QueryRepository>();
            //services.AddScoped<IRepository, Repository>();
            //services.AddScoped<IIgnoreQueryFilterRepository, IgnoreQueryFilterRepository>();
            return services;
        }
    }
}
