using Microsoft.EntityFrameworkCore;
using Server.Databases;
using Server.Entities;
using Server.Entities.BaseStructure.Components;
using System.Globalization;
using System.Text;
using UnitSystem;

namespace Server.Data.Seeders
{
    public static class ChemicalComponentSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, string contentRootPath)
        {
            var seedDataFolder = Path.Combine(contentRootPath, "SeedData");
            var filePath = Path.Combine(seedDataFolder, "MasterComponents.csv");

            // 1. SI NO EXISTE EL ARCHIVO, LO CREAMOS (ADN del sistema)
            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(seedDataFolder)) Directory.CreateDirectory(seedDataFolder);
                await GenerateMasterFileFromHardcode(filePath);
            }

            // 2. SI LA TABLA ESTÁ VACÍA, CARGAMOS DESDE EL ARCHIVO
            if (!await context.ChemicalComponents.AnyAsync())
            {
                var components = await ReadComponentsFromFile(filePath);
                if (components.Any())
                {
                    await context.ChemicalComponents.AddRangeAsync(components);
                    await context.SaveChangesAsync();
                }
            }
        }

        private static async Task GenerateMasterFileFromHardcode(string path)
        {
            var sb = new StringBuilder();

            // CONSTRUCCIÓN DEL HEADER (Asegurando match con todas tus tablas)
            var header = new List<string> {
                "Name", "Formula", "StructuralFormula", "Family", "SecondaryFamily",
                "MW_Val", "Tc_Val", "Pc_Val", "Tb_Val", "Tm_Val", "Vc_Val", "Va_Val", "Zc", "Acentric", "AcentricPitzer",
                "EntH", "EntG", "EntS", "EntC"
            };

            string[] prefixes = { "PV", "CV", "CPL", "CPG", "VL", "VV", "CTL", "CTV", "DE", "TS" };
            foreach (var p in prefixes)
            {
                for (int i = 1; i <= 7; i++) header.Add($"{p}_C{i}");
                header.Add($"{p}_Tmin"); header.Add($"{p}_Tmax");
            }
            sb.AppendLine(string.Join(";", header));

            // DATOS DE LAS IMÁGENES (Separados por ;)
            // Formato: Name;Formula;Struct;Fam;SecFam;MW;Tc;Pc;Tb;Tm;Vc;Va;Zc;Ac;AcP;EntH;EntG;EntS;EntC; [PV coeffs...]; [CV coeffs...]; etc.

            // ETANOL
            sb.Append("Etanol;C2H6O;C2H5OH;Alcohol;Primary;46.06904;514;6137;351.44;159.05;0.168;0.1752;0.24;0.644;0.644;-234950000;-167850000;280.64;-1235000000;");
            sb.Append("61.791;-7122.3;0;0;-7.1424;2.8853E-06;2;159.05;514;"); // PV
            sb.Append("55789000;0.31245;0;0;0;0;0;159.05;514;"); // CV
            sb.Append("102640;-139.63;-0.030341;0.0020386;0;0;0;159.05;390;"); // CPL
            sb.Append("49232;145.5;0.000166;139.4;744.7;0;0;273.15;1500;"); // CPG
            sb.Append("-14.187;1389.5;-3.0418;0;0;0;0;200;440;"); // VL
            sb.Append("0.1249;0.8247;52.7;0;0;0;0;273.15;1000;"); // VV
            sb.Append("0.2222;-0.000264;0;0;0;0;0;159.05;351.44;"); // CTL
            sb.Append("0.00115;0.7082;2684.3;-268000;0;0;0;273.15;1000;"); // CTV
            sb.Append("1.6288;0.27469;514;0.23177;0;0;0;159.05;514;"); // DE
            sb.AppendLine("37.64;-0.02157;1.233;0;0;0;0;159.05;503.15"); // TS

            // AGUA (H2O) - Datos especiales de tus capturas
            sb.Append("Agua;H2O;H2O;Inorganic;Water;18.0151;647.096;22064;373.14;273.15;0.0559;0.0436;0.229;0.344;0.344;-242000;-229000;188.724;0;");
            sb.Append("62.136;-7258.2;0;0;-7.3037;4.17E-06;2;273.16;647.1;"); // PV
            sb.Append("52100000;0.3199;-0.212;0.25795;0;0;0;1.85;373.98;"); // CV
            sb.Append("0;0;0;0;0;0;0;0;0;"); // CPL (Ceros según imagen)
            sb.Append("33363;26790;2610.5;8896;1169;0;0;-173.15;2000;"); // CPG
            sb.Append("-52.12;3603.5;5.22;-0.014;10;0;0;0.01;373;"); // VL
            sb.Append("1.07E-07;0.88;0;0;0;0;0;273.15;800;"); // VV
            sb.Append("0.000632;0.00757;-8.08E-06;1.86E-09;0;0;0;1;373.14;"); // CTL
            sb.Append("6.2E-06;1.1;0;0;0;0;0;0.01;800;"); // CTV
            sb.Append("-13.85;0.213;-0.00191;0;0;0;0;2;80;"); // DE
            sb.AppendLine("177.66;-256.7;-360;1.9699;0;0;0;273.16;647.1"); // TS

            // METANOL
            sb.Append("Metanol;CH4O;CH3OH;Alcohol;Primary;32.04216;512.5;8084;337.85;175.47;0.117;0.1198;0.224;0.565;0.565;-200940000;-162320000;239.88;-638200000;");
            sb.Append("71.205;-6904.5;0;0;-8.8622;7.4664E-06;2;175.47;512.5;"); // PV
            sb.Append("50451000;0.33594;0;0;0;0;0;175.47;512.5;"); // CV
            sb.Append("105300;-362.23;0.9379;0;0;0;0;175.47;400;"); // CPL
            sb.Append("40152;31046;1468;25850;170.3;0;0;273.15;1500;"); // CPG
            sb.Append("-20.158;1389.2;2.069;0;0;0;0;175.47;337.85;"); // VL
            sb.Append("0.0125;0.8904;205;0;0;0;0;273.15;1000;"); // VV
            sb.Append("0.2312;-0.000251;0;0;0;0;0;175.47;337.85;"); // CTL
            sb.Append("5.7992E-07;1.109;0;0;0;0;0;273.15;684.37;"); // CTV
            sb.Append("2.288;0.2685;512.5;0.2319;0;0;0;175.47;512.5;"); // DE
            sb.AppendLine("35.13;-0.00704;1.1895;0;0;0;0;175.47;337.85"); // TS

            // GLICEROL (Glicerina)
            sb.Append("Glicerol;C3H8O3;C3H5(OH)3;Alcohol;Polyol;92.09472;850;7500;561;291.33;0.264;0.4119;0.281;0.513;0.513;-577900000;-447100000;0;-1477000000;");
            sb.Append("88.473;-13808;0;0;-10.088;3.5712E-19;6;291.33;850;"); // PV
            sb.Append("11067000;0.48319;0;0;0;0;0;291.33;850;"); // CV
            sb.Append("143530;241.3;-0.6276;0.00115;5.2837E-06;0;0;291.33;850;"); // CPL
            sb.Append("96490;185.2;0.000215;163.4;832.5;0;0;273.15;1500;"); // CPG
            sb.Append("-49.771;6173.3;4.314;2693000;-2;0;0;291.33;680;"); // VL
            sb.Append("0.0401;0.902;0;0;0;0;0;273.15;1000;"); // VV
            sb.Append("0.282;0.0001134;0;0;0;0;0;291.33;561;"); // CTL
            sb.Append("-0.91351;0.1263;58.3;-4749600;0;0;0;273.15;1000;"); // CTV
            sb.Append("0.92382;0.23512;850;0.2367;0;0;0;291.33;850;"); // DE
            sb.AppendLine("63.145;-0.0016;1.08;0;0;0;0;291.33;453.15"); // TS

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        private static async Task<List<ChemicalComponent>> ReadComponentsFromFile(string path)
        {
            var list = new List<ChemicalComponent>();
            var lines = await File.ReadAllLinesAsync(path);
            var ci = CultureInfo.InvariantCulture;

            for (int i = 1; i < lines.Length; i++)
            {
                var r = lines[i].Split(';');
                if (r.Length < 20) continue;

                int idx = 0;
                double D(int index) => double.TryParse(r[index], NumberStyles.Any, ci, out var val) ? val : 0;

                var comp = new ChemicalComponent
                {
                    Name = r[idx++],
                    Formula = r[idx++],
                    StructuralFormula = r[idx++],
                    Family = r[idx++],
                    SecondaryFamily = r[idx++],
                    MolecularWeight = D(idx++),
                    CriticalTemperature = new StoredAmount(D(idx++), TemperatureUnits.Kelvin.Name),
                    CriticalPressure = new StoredAmount(D(idx++), PressureUnits.KiloPascal.Name),
                    BoilingPoint = new StoredAmount(D(idx++), TemperatureUnits.Kelvin.Name),
                    MeltingPoint = new StoredAmount(D(idx++), TemperatureUnits.Kelvin.Name),
                    CriticalVolume = new StoredAmount(D(idx++), MolarVolumeSpecificUnits.m3_Kgmol.Name),
                    VolumeAsterisk = new StoredAmount(D(idx++), MolarVolumeSpecificUnits.m3_Kgmol.Name),
                    CriticalZ = D(idx++),
                    AcentricFactor = D(idx++),
                    AcentricFactorPitzer = D(idx++),
                    EnthalpyForm = new StoredAmount(D(idx++), MolarEnergyUnits.J_Kgmol.Name),
                    GibbsForm = new StoredAmount(D(idx++), MolarEnergyUnits.J_Kgmol.Name),
                    EntropyForm = new StoredAmount(D(idx++), MolarEntropyUnits.KJ_Kgmol_C.Name),
                    CombustionEnthalpy = new StoredAmount(D(idx++), MolarEnergyUnits.J_Kgmol.Name)
                };

                comp.VaporPressure = MapCorr(r, ref idx);
                comp.HeatOfVaporization = MapCorr(r, ref idx);
                comp.LiquidHeatCapacity = MapCorr(r, ref idx);
                comp.GasHeatCapacity = MapCorr(r, ref idx);
                comp.LiquidViscosity = MapCorr(r, ref idx);
                comp.GasViscosity = MapCorr(r, ref idx);
                comp.LiquidThermalCond = MapCorr(r, ref idx);
                comp.GasThermalCond = MapCorr(r, ref idx);
                comp.Density = MapCorr(r, ref idx);
                comp.SurfaceTension = MapCorr(r, ref idx);

                list.Add(comp);
            }
            return list;
        }

        private static CorrelationCoefficients MapCorr(string[] r, ref int idx)
        {
            var ci = CultureInfo.InvariantCulture;
            double D(int i) => double.TryParse(r[i], NumberStyles.Any, ci, out var v) ? v : 0;
            var c = new CorrelationCoefficients
            {
                C1 = D(idx++),
                C2 = D(idx++),
                C3 = D(idx++),
                C4 = D(idx++),
                C5 = D(idx++),
                C6 = D(idx++),
                C7 = D(idx++),
                Tmin = new StoredAmount(D(idx++), TemperatureUnits.Kelvin.Name),
                Tmax = new StoredAmount(D(idx++), TemperatureUnits.Kelvin.Name)
            };
            return c;
        }
    }
}