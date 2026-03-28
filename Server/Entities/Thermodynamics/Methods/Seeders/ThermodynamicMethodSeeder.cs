using Microsoft.EntityFrameworkCore;
using Server.Databases;
using Server.Entities.Thermodynamics.Methods;
using Shared.Thermodynamics.Enums;
using System.Globalization;
using System.Text;

public static class ThermodynamicMethodSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, string contentRootPath)
    {
        var seedDataFolder = Path.Combine(contentRootPath, "SeedData");
        var filePath = Path.Combine(seedDataFolder, "MasterThermodynamicMethods.csv");

        // 1. SI NO EXISTE EL ARCHIVO, LO CREAMOS (ADN del sistema)
        if (!File.Exists(filePath))
        {
            if (!Directory.Exists(seedDataFolder)) Directory.CreateDirectory(seedDataFolder);
            await GenerateMasterFileFromHardcode(filePath);
        }

        // 2. SI LA TABLA ESTÁ VACÍA, CARGAMOS DESDE EL ARCHIVO
        if (!await context.ThermodynamicMethods.AnyAsync())
        {
            await LoadFromCsvAsync(context, filePath);
        }
    }

    private static async Task GenerateMasterFileFromHardcode(string path)
    {
        var sb = new StringBuilder();

        // CONSTRUCCIÓN DEL HEADER
        sb.AppendLine("MethodName;Description;VaporModel;LiquidModel;ComponentI;ComponentJ;ParameterType;Value");

        // DATOS MAESTROS (Textos UI en Inglés, Búsqueda de Componentes Exacta)

        // METHOD 1: Ethanol-Water (NRTL)
        string m1 = "Ethanol-Water (NRTL);Ethanol-Water equilibrium using NRTL Aspen;IdealGas;NRTLAspen";
        sb.AppendLine($"{m1};Etanol;Agua;NRTL_A;-0.8009");
        sb.AppendLine($"{m1};Agua;Etanol;NRTL_A;3.4578");
        sb.AppendLine($"{m1};Etanol;Agua;NRTL_B;246.18");
        sb.AppendLine($"{m1};Agua;Etanol;NRTL_B;-586.0809");
        sb.AppendLine($"{m1};Etanol;Agua;NRTL_C;0.3"); // Parámetro Alfa

        // METHOD 2: Methanol-Glycerol (Wilson)
        string m2 = "Methanol-Glycerol (Wilson);Methanol-Glycerol mixture with SRK for Vapor;SoaveRedlichKwong1972;WilsonAspen";
        sb.AppendLine($"{m2};Metanol;Glicerol;Wilson_B;123.2875");
        sb.AppendLine($"{m2};Glicerol;Metanol;Wilson_B;-466.0408");

        // METHOD 3: Pure Water (Steam Tables)
        string m3 = "Water (Steam Tables);Pure water calculations based on IAPWS steam tables;SteamTables;SteamTables";
        sb.AppendLine($"{m3};Agua;;;0"); // Sin parámetros binarios

        // METHOD 4: Methanol-Water (Wilson)
        string m4 = "Methanol-Water (Wilson);Methanol-Water mixture using Wilson Aspen and SRK 1972;SoaveRedlichKwong1972;WilsonAspen";
        sb.AppendLine($"{m4};Metanol;Agua;Wilson_A;1.0837");
        sb.AppendLine($"{m4};Agua;Metanol;Wilson_A;-1.8842");
        sb.AppendLine($"{m4};Metanol;Agua;Wilson_B;-580.237");
        sb.AppendLine($"{m4};Agua;Metanol;Wilson_B;617.4097");

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
    }

    private static async Task LoadFromCsvAsync(ApplicationDbContext context, string path)
    {
        var lines = await File.ReadAllLinesAsync(path);
        if (lines.Length <= 1) return; // Solo tiene header o está vacío

        var ci = CultureInfo.InvariantCulture;
        var componentsDict = await context.ChemicalComponents.ToDictionaryAsync(c => c.Name);

        // Agrupar filas por MethodName para reconstruir las entidades relacionales
        var rowsByMethod = lines.Skip(1)
                                .Select(l => l.Split(';'))
                                .Where(r => r.Length == 8)
                                .GroupBy(r => r[0]); // r[0] es MethodName

        var metodosToInsert = new List<ThermodynamicMethod>();

        foreach (var group in rowsByMethod)
        {
            var firstRow = group.First();

            var method = new ThermodynamicMethod
            {
                Name = firstRow[0],
                Description = firstRow[1],
                VaporModel = Enum.Parse<VaporPhaseModel>(firstRow[2]),
                LiquidModel = Enum.Parse<LiquidPhaseModel>(firstRow[3]),
                MethodComponents = new List<MethodComponent>(),
                BinaryParameters = new List<BinaryInteractionParameter>()
            };

            // Extraer e indexar los componentes únicos de este método
            var componentNames = group.SelectMany(r => new[] { r[4], r[5] })
                                      .Where(n => !string.IsNullOrWhiteSpace(n))
                                      .Distinct()
                                      .ToList();

            for (int i = 0; i < componentNames.Count; i++)
            {
                if (componentsDict.TryGetValue(componentNames[i], out var comp))
                {
                    method.MethodComponents.Add(new MethodComponent
                    {
                        Component = comp,
                        MatrixIndex = i
                    });
                }
            }

            // Mapear los parámetros binarios
            foreach (var r in group)
            {
                var compI_Name = r[4];
                var compJ_Name = r[5];
                var paramTypeStr = r[6];
                var valueStr = r[7];

                if (!string.IsNullOrWhiteSpace(compJ_Name) && !string.IsNullOrWhiteSpace(paramTypeStr))
                {
                    if (componentsDict.TryGetValue(compI_Name, out var compI) &&
                        componentsDict.TryGetValue(compJ_Name, out var compJ) &&
                        Enum.TryParse<BinaryParameterType>(paramTypeStr, out var paramType))
                    {
                        method.BinaryParameters.Add(new BinaryInteractionParameter
                        {
                            ComponentI = compI,
                            ComponentJ = compJ,
                            ParameterType = paramType,
                            Value = double.TryParse(valueStr, NumberStyles.Any, ci, out var val) ? val : 0
                        });
                    }
                }
            }

            metodosToInsert.Add(method);
        }

        await context.ThermodynamicMethods.AddRangeAsync(metodosToInsert);
        await context.SaveChangesAsync();
    }
}
