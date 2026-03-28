using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data.Seeders;
using Server.Databases;
using Server.Entities.BaseStructure.Components;
using Server.Entities.UserManagement;
using System.Globalization;
using System.Text;


namespace Server.Services
{
    public static class DatabaseSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider sp)
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var env = sp.GetRequiredService<IWebHostEnvironment>();

            // 1. Estructura de tablas
            await context.Database.MigrateAsync();

            // 2. Seguridad (Identity)
            await SeedIdentityAsync(sp);

            // 3. Base Química (El corazón que ya validamos)
            await ChemicalComponentSeeder.SeedAsync(context, env.ContentRootPath);

            await ThermodynamicMethodSeeder.SeedAsync(context, env.ContentRootPath);
        }

        private static async Task SeedIdentityAsync(IServiceProvider sp)
        {
            // (Tu código original de Identity se mantiene intacto aquí)
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Developer", "Administrator", "Creator", "Viewer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var initialUsers = new List<(string Email, string Name, string Last, string Role, bool MustChange)>
            {
                ("alfonsofuen@gmail.com", "Alfonso", "Developer", "Developer", false),
                ("villegas01@gmail.com", "System", "Admin", "Administrator", true)
            };

            foreach (var u in initialUsers)
            {
                if (await userManager.FindByEmailAsync(u.Email) == null)
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = u.Email,
                        Email = u.Email,
                        FirstName = u.Name,
                        LastName = u.Last,
                        EmailConfirmed = true,
                        IsActive = true,
                        MustChangePassword = u.MustChange,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(newUser, "Admin123!");
                    if (result.Succeeded) await userManager.AddToRoleAsync(newUser, u.Role);
                }
            }
        }

      

       

        // ======================================================================
        // NUEVO MÉTODO: SINCRONIZACIÓN DESDE LA UI HACIA EL ARCHIVO CSV
        // ======================================================================
        public static async Task SyncDatabaseToCsv(ApplicationDbContext context, string rootPath)
        {
            var directory = Path.Combine(rootPath, "SeedData");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, "MasterComponents.csv");

            var components = await context.ChemicalComponents.ToListAsync();
            var sb = new StringBuilder();

            // Usar exactamente los mismos encabezados del método de semilla
            var h = new List<string> { "Name", "Formula", "Struct", "Fam", "SecFam", "MW", "Tc", "Pc", "Tb", "Tm", "Vc", "Va", "Zc", "Ac", "AcP", "EntH", "EntG", "EntS", "EntC" };
            string[] pref = { "PV", "CV", "CPL", "CPG", "VL", "VV", "CTL", "CTV", "DE", "TS" };
            foreach (var p in pref) { for (int i = 1; i <= 7; i++) h.Add($"{p}_C{i}"); h.Add($"{p}_Tmin"); h.Add($"{p}_Tmax"); }
            sb.AppendLine(string.Join(";", h));

            var ci = CultureInfo.InvariantCulture;

            foreach (var c in components)
            {
                var row = new List<string>
                {
                    c.Name ?? string.Empty, c.Formula ?? string.Empty, c.StructuralFormula ?? string.Empty, c.Family ?? string.Empty, c.SecondaryFamily ?? string.Empty,
                    c.MolecularWeight.ToString(ci),
                    c.CriticalTemperature.Value.ToString(ci), c.CriticalPressure.Value.ToString(ci),
                    c.BoilingPoint.Value.ToString(ci), c.MeltingPoint.Value.ToString(ci),
                    c.CriticalVolume.Value.ToString(ci), c.VolumeAsterisk.Value.ToString(ci),
                    c.CriticalZ.ToString(ci), c.AcentricFactor.ToString(ci), c.AcentricFactorPitzer.ToString(ci),
                    c.EnthalpyForm.Value.ToString(ci), c.GibbsForm.Value.ToString(ci), c.EntropyForm.Value.ToString(ci), c.CombustionEnthalpy.Value.ToString(ci)
                };

                // Exportar correlaciones en estricto orden
                AddCorrelationToRow(row, c.VaporPressure, ci);
                AddCorrelationToRow(row, c.HeatOfVaporization, ci);
                AddCorrelationToRow(row, c.LiquidHeatCapacity, ci);
                AddCorrelationToRow(row, c.GasHeatCapacity, ci);
                AddCorrelationToRow(row, c.LiquidViscosity, ci);
                AddCorrelationToRow(row, c.GasViscosity, ci);
                AddCorrelationToRow(row, c.LiquidThermalCond, ci);
                AddCorrelationToRow(row, c.GasThermalCond, ci);
                AddCorrelationToRow(row, c.Density, ci);
                AddCorrelationToRow(row, c.SurfaceTension, ci);

                sb.AppendLine(string.Join(";", row));
            }

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static void AddCorrelationToRow(List<string> row, CorrelationCoefficients corr, CultureInfo ci)
        {
            if (corr == null)
            {
                row.AddRange(new[] { "0", "0", "0", "0", "0", "0", "0", "0", "0" });
                return;
            }
            row.Add(corr.C1.ToString(ci)); row.Add(corr.C2.ToString(ci)); row.Add(corr.C3.ToString(ci));
            row.Add(corr.C4.ToString(ci)); row.Add(corr.C5.ToString(ci)); row.Add(corr.C6.ToString(ci));
            row.Add(corr.C7.ToString(ci));
            row.Add(corr.Tmin.Value.ToString(ci)); row.Add(corr.Tmax.Value.ToString(ci));
        }
        public static async Task SyncMethodsToCsv(ApplicationDbContext context, string rootPath)
        {
            var directory = Path.Combine(rootPath, "SeedData");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, "MasterThermodynamicMethods.csv");

            // Traemos los métodos con todas sus relaciones (Eager Loading)
            var metodos = await context.ThermodynamicMethods
                .Include(m => m.MethodComponents).ThenInclude(mc => mc.Component)
                .Include(m => m.BinaryParameters).ThenInclude(bp => bp.ComponentI)
                .Include(m => m.BinaryParameters).ThenInclude(bp => bp.ComponentJ)
                .AsNoTracking()
                .ToListAsync();

            var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;

            // Header estricto (ADN)
            sb.AppendLine("MethodName;Description;VaporModel;LiquidModel;ComponentI;ComponentJ;ParameterType;Value");

            foreach (var m in metodos)
            {
                var baseInfo = $"{m.Name ?? string.Empty};{m.Description ?? string.Empty};{m.VaporModel};{m.LiquidModel}";

                // Escenario A: El método tiene parámetros de interacción binaria (Ej: NRTL, Wilson)
                if (m.BinaryParameters.Any())
                {
                    foreach (var bp in m.BinaryParameters)
                    {
                        var compIName = bp.ComponentI?.Name ?? string.Empty;
                        var compJName = bp.ComponentJ?.Name ?? string.Empty;
                        var valStr = bp.Value.ToString(ci);

                        sb.AppendLine($"{baseInfo};{compIName};{compJName};{bp.ParameterType};{valStr}");
                    }
                }
                // Escenario B: El método tiene componentes pero NO parámetros binarios (Ej: Agua Pura / Steam Tables)
                else if (m.MethodComponents.Any())
                {
                    foreach (var mc in m.MethodComponents)
                    {
                        var compName = mc.Component?.Name ?? string.Empty;
                        // Dejamos vacíos los campos ComponentJ y ParameterType, y el Value en 0
                        sb.AppendLine($"{baseInfo};{compName};;;0");
                    }
                }
                // Escenario C: Método vacío (Edge case de seguridad)
                else
                {
                    sb.AppendLine($"{baseInfo};;;;0");
                }
            }

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        }

    }
}
