using Server.Services;

namespace Server.Services
{
    public static class AppBuilder
    {
        public static WebApplication UseApp2(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {

                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.MapStaticAssets();

            app.UseRouting();
            app.UseCors("AllowBlazorWasm");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapEndPoint();
            app.MapRazorPages();
            app.MapFallbackToFile("index.html");
            app.MapControllers();
            return app;
        }
        // En AppBuilder.cs -> UseApp
        public static WebApplication UseApp(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseBlazorFrameworkFiles(); // <-- AÑADIR ESTO (Crucial para Blazor WASM Hosted)
            app.MapStaticAssets(); // o app.UseStaticFiles(); dependiendo de tu versión exacta de .NET

            app.UseRouting();
            app.UseCors("AllowBlazorWasm"); // Ahora hará match perfecto
            app.UseHttpsRedirection();

            app.UseAuthentication(); // <-- Lee la cookie
            app.UseAuthorization();  // <-- Valida permisos

            app.MapEndPoint();
            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html"); // Debe ir al final

            return app;
        }
        internal static IApplicationBuilder MapEndPoint(this WebApplication app)
        {
            // Usamos un Scope para asegurar la resolución de servicios
            using (var scope = app.Services.CreateScope())
            {
                var endpoints = scope.ServiceProvider.GetServices<IEndPoint>();

                foreach (var endpoint in endpoints)
                {
#if DEBUG
                    // 🛠️ MODO DESARROLLO (Debug)
                    // Aquí SI tenemos Try-Catch para que puedas inspeccionar el error
                    try
                    {
                        endpoint.MapEndPoint(app);
                    }
                    catch (Exception ex)
                    {
                        // Este comando hace que Visual Studio se detenga aquí AUTOMÁTICAMENTE
                        // como si hubieras puesto un Breakpoint rojo (Punto de interrupción).
                        // Es genial porque te lleva directo a la línea del error.
                        System.Diagnostics.Debugger.Break();

                        // Puedes inspeccionar 'msg' pasando el mouse por encima
                        string msg = ex.Message;
                        Console.WriteLine($"Error mapping {endpoint.GetType().Name}: {msg}");

                        // Opcional: Si quieres que siga intentando con los otros endpoints
                        // no pongas 'throw'. Si quieres que pare, pon 'throw'.
                    }
#else
                        // 🚀 MODO PRODUCCIÓN (Release)
                        // Aquí NO hay Try-Catch. Si falla, la aplicación se detiene inmediatamente.
                        // Esto es vital para que el servidor sepa que el despliegue falló.
                        endpoint.MapEndPoint(app);
#endif
                }
            }

            return app;
        }

        internal static IApplicationBuilder UseEndpoints(this IApplicationBuilder app)
        {

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();


            });
            return app;
        }
        public static async Task SeedDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            try
            {
                // Llamamos a tu clase Seeder que creamos anteriormente
                await DatabaseSeeder.SeedDataAsync(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding database: {ex.Message}");
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
            }
        }
    }

}
