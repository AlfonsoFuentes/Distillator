using Shared.Calculator.Components;
using Shared.Thermodynamics.Enums;
using Shared.Thermodynamics.Methods;

namespace Shared.DesignPatterns.Thermodynamics.Phases
{
    public static class BinaryInteractionManager
    {
        // =========================================================================
        // K_ij PARA EoS (SIMÉTRICO) - HÍBRIDO: BD + Hardcode
        // =========================================================================
        public static double GetKij(
            Guid idI,
            Guid idJ,
            string nameI,
            string nameJ,
            VaporPhaseModel eosModel,
            List<BinaryInteractionParameterDto> dbParams)
        {
            if (idI == idJ) return 0.0;

            // 1. Intentar buscar en BD (Prioridad 1)
            // Nota: Para EoS, tratamos el parámetro como simétrico (Kij = Kji)
            var dbValue = SearchSymmetricEosParameter(idI, idJ, eosModel, dbParams);
            if (dbValue.HasValue) return dbValue.Value;

            // 2. Fallback al Hardcode de Prausnitz (Prioridad 2)
            return eosModel switch
            {
                VaporPhaseModel.PengRobinson => CalcularKij_PR_Hardcode(nameI, nameJ),
                VaporPhaseModel.SoaveRedlichKwong1972 or
                VaporPhaseModel.SoaveRedlichKwong1984 or
                VaporPhaseModel.SoaveRedlichKwong1995 or
                VaporPhaseModel.RedlichKwong => CalcularKij_SRK_Hardcode(nameI, nameJ),
                _ => 0.0
            };
        }

        private static double? SearchSymmetricEosParameter(Guid idI, Guid idJ, VaporPhaseModel model, List<BinaryInteractionParameterDto> db)
        {
            if (db == null) return null;

            // En este punto, podrías necesitar un BinaryParameterType específico para EoS 
            // Si no lo tienes en el Enum aún, asumo que usas una lógica de filtrado por modelo o un valor genérico.
            // Por ahora, buscaremos si existe algún parámetro que no sea de actividad para estos IDs.

            var param = db.FirstOrDefault(p =>
                ((p.ComponentI_Id == idI && p.ComponentJ_Id == idJ) ||
                 (p.ComponentI_Id == idJ && p.ComponentJ_Id == idI)));

            return param?.Value;
        }

        // =========================================================================
        // PARÁMETROS ASIMÉTRICOS (Modelos de Actividad)
        // =========================================================================

        public static double GetActivityParameter(
            Guid idI,
            Guid idJ,
            BinaryParameterType type,
            List<BinaryInteractionParameterDto> db)
        {
            if (idI == idJ)
            {
                // NRTL Alpha (C) suele ser 0.3 por defecto en la diagonal para cálculos de matriz,
                // aunque en la sumatoria final no afecte si xi*xj es 0.
                return (type == BinaryParameterType.NRTL_C) ? 0.3 : 0.0;
            }

            var param = db?.FirstOrDefault(p =>
                p.ComponentI_Id == idI &&
                p.ComponentJ_Id == idJ &&
                p.ParameterType == type);

            if (param != null) return param.Value;

            // Fallback: Si es NRTL_C y no está en BD, el estándar de la industria es 0.3
            return (type == BinaryParameterType.NRTL_C) ? 0.3 : 0.0;
        }

        // =========================================================================
        // TABLAS HARDCODEADAS (Lógica de Prausnitz/Reid)
        // =========================================================================

        private static double CalcularKij_PR_Hardcode(string comp1, string comp2)
        {
            comp1 = NormalizarNombre(comp1);
            comp2 = NormalizarNombre(comp2);

            if (comp1 == "metano")
            {
                if (new[] { "ibutano", "nbutano", "npentano", "ipentano" }.Contains(comp2)) return 0.02;
                if (new[] { "ihexano", "nhexano", "nheptano", "iheptano" }.Contains(comp2)) return 0.025;
                if (new[] { "noctano", "nnonano", "ndecano" }.Contains(comp2)) return 0.035;
                if (comp2 == "eicosano") return 0.054;
                if (comp2 == "ciclohexano") return 0.03;
                if (comp2 == "etano") return 0.00295;
                if (comp2 == "propano") return 0.00748;
            }

            // Simetría para el hardcode
            if (comp2 == "metano") return CalcularKij_PR_Hardcode(comp2, comp1);

            if ((comp1 == "co2" && EsHidrocarburo(comp2)) || (comp2 == "co2" && EsHidrocarburo(comp1))) return 0.15;
            if (comp1 == "nitrógeno" && EsHidrocarburo(comp2)) return 0.15;
            if (comp2 == "nitrógeno" && EsHidrocarburo(comp1)) return 0.12;

            // Etano y Propano
            if (comp1 == "etano" && EsHidrocarburo(comp2) && comp2 != "etano") return 0.01576;
            if (comp2 == "etano" && EsHidrocarburo(comp1) && comp1 != "etano") return 0.01576;

            return 0.0;
        }

        private static double CalcularKij_SRK_Hardcode(string comp1, string comp2)
        {
            comp1 = NormalizarNombre(comp1);
            comp2 = NormalizarNombre(comp2);

            // Ejemplo H2S
            if (comp1 == "h2s" || comp2 == "h2s")
            {
                string other = (comp1 == "h2s") ? comp2 : comp1;
                return other switch
                {
                    "co2" => 0.102,
                    "n2" => 0.14,
                    "metano" => 0.085,
                    "etano" => 0.0829,
                    "propano" => 0.0831,
                    "ndecano" => 0.04634,
                    _ => 0.0
                };
            }
            return 0.0;
        }

        private static string NormalizarNombre(string nombre) =>
            nombre?.Trim().Replace(" ", "").Replace("-", "").ToLower() ?? string.Empty;

        private static bool EsHidrocarburo(string nombre)
        {
            nombre = NormalizarNombre(nombre);
            return new[] { "metano", "etano", "propano", "butano", "pentano", "hexano", "heptano", "octano", "nonano", "decano", "benceno", "ciclo" }
                   .Any(h => nombre.Contains(h));
        }
    }
    public static class BinaryInteractionManager2
    {
        // =========================================================================
        // K_ij PARA EoS (SIMÉTRICO) - HÍBRIDO: BD + Hardcode
        // =========================================================================

        public static double GetKij(
        Guid compI_Id,
        Guid compJ_Id,
        string compI_Name,
        string compJ_Name,
        VaporPhaseModel eosModel)
        {
            // Mismo componente: k_ij = 0
            if (compI_Id == compJ_Id)
                return 0.0;

            // ✅ IR DIRECTO AL HARDCODE (sin buscar en BD)
            switch (eosModel)
            {
                case VaporPhaseModel.PengRobinson:
                    return CalcularKij_PR_Hardcode(compI_Name, compJ_Name);

                case VaporPhaseModel.SoaveRedlichKwong1972:
                case VaporPhaseModel.SoaveRedlichKwong1984:
                case VaporPhaseModel.SoaveRedlichKwong1995:
                case VaporPhaseModel.RedlichKwong:
                    return CalcularKij_SRK_Hardcode(compI_Name, compJ_Name);

                case VaporPhaseModel.IdealGas:
                default:
                    return 0.0;
            }
        }

        // =========================================================================
        // PARÁMETROS ASIMÉTRICOS (Actividad) - Búsqueda ESTRICTA direccional (SIN OR)
        // =========================================================================

        public static double GetNRTL_ParameterA(Guid compI_Id, Guid compJ_Id, List<BinaryInteractionParameterDto> binaryParameters)
        {
            if (compI_Id == compJ_Id) return 0.0;
            var param = binaryParameters.FirstOrDefault(p =>
                p.ComponentI_Id == compI_Id && p.ComponentJ_Id == compJ_Id &&
                p.ParameterType == BinaryParameterType.NRTL_A);
            return param != null ? param.Value : 0.0;
        }

        public static double GetNRTL_ParameterB(Guid compI_Id, Guid compJ_Id, List<BinaryInteractionParameterDto> binaryParameters)
        {
            if (compI_Id == compJ_Id) return 0.0;
            var param = binaryParameters.FirstOrDefault(p =>
                p.ComponentI_Id == compI_Id && p.ComponentJ_Id == compJ_Id &&
                p.ParameterType == BinaryParameterType.NRTL_B);
            return param != null ? param.Value : 0.0;
        }

        public static double GetNRTL_Alpha(Guid compI_Id, Guid compJ_Id, List<BinaryInteractionParameterDto> binaryParameters)
        {
            if (compI_Id == compJ_Id) return 0.3;
            var param = binaryParameters.FirstOrDefault(p =>
                p.ComponentI_Id == compI_Id && p.ComponentJ_Id == compJ_Id &&
                p.ParameterType == BinaryParameterType.NRTL_C);
            return param != null ? param.Value : 0.3;
        }

        public static double GetVanLaarParameter(Guid compI_Id, Guid compJ_Id, List<BinaryInteractionParameterDto> binaryParameters)
        {
            if (compI_Id == compJ_Id) return 0.0;
            var param = binaryParameters.FirstOrDefault(p =>
                p.ComponentI_Id == compI_Id && p.ComponentJ_Id == compJ_Id &&
                p.ParameterType == BinaryParameterType.VanLaar_Aij);
            return param != null ? param.Value : 0.0;
        }

        public static double GetWilsonParameter_A(Guid compI_Id, Guid compJ_Id, List<BinaryInteractionParameterDto> binaryParameters)
        {
            if (compI_Id == compJ_Id) return 0.0;
            var param = binaryParameters.FirstOrDefault(p =>
                p.ComponentI_Id == compI_Id && p.ComponentJ_Id == compJ_Id &&
                p.ParameterType == BinaryParameterType.Wilson_A);
            return param != null ? param.Value : 0.0;
        }

        public static double GetWilsonParameter_B(Guid compI_Id, Guid compJ_Id, List<BinaryInteractionParameterDto> binaryParameters)
        {
            if (compI_Id == compJ_Id) return 0.0;
            var param = binaryParameters.FirstOrDefault(p =>
                p.ComponentI_Id == compI_Id && p.ComponentJ_Id == compJ_Id &&
                p.ParameterType == BinaryParameterType.Wilson_B);
            return param != null ? param.Value : 0.0;
        }

        // =========================================================================
        // TABLAS HARDCODEADAS (De tu C++)
        // =========================================================================

        private static double CalcularKij_PR_Hardcode(string comp1, string comp2)
        {
            comp1 = NormalizarNombreComponente(comp1);
            comp2 = NormalizarNombreComponente(comp2);

            if (comp1 == "metano")
            {
                if (new[] { "i-butano", "n-butano", "n-pentano", "ipentano" }.Contains(comp2)) return 0.02;
                if (new[] { "i-hexano", "n-hexano", "n-heptano", "i-heptano" }.Contains(comp2)) return 0.025;
                if (new[] { "n-octano", "n-nonano", "n-decano" }.Contains(comp2)) return 0.035;
                if (comp2 == "eicosano") return 0.054;
                if (comp2 == "ciclohexano") return 0.03;
                if (comp2 == "etano") return 0.00295;
                if (comp2 == "propano") return 0.00748;
            }
            if (comp2 == "metano")
            {
                if (new[] { "i-butano", "n-butano", "n-pentano", "i-pentano" }.Contains(comp1)) return 0.02;
                if (new[] { "i-hexano", "n-hexano", "n-heptano", "i-heptano" }.Contains(comp1)) return 0.025;
                if (new[] { "n-octano", "n-nonano", "n-decano" }.Contains(comp1)) return 0.035;
                if (comp1 == "eicosano") return 0.054;
                if (comp1 == "ciclohexano") return 0.03;
                if (comp1 == "etano") return 0.00295;
                if (comp1 == "propano") return 0.00748;
            }
            if ((comp1 == "co2" && EsHidrocarburo(comp2)) || (comp2 == "co2" && EsHidrocarburo(comp1))) return 0.15;
            if ((comp1 == "nitrógeno" && EsHidrocarburo(comp2))) return 0.15;
            if ((comp2 == "nitrógeno" && EsHidrocarburo(comp1))) return 0.12;
            if (comp1 == "etano" && comp2 != "etano")
            {
                if (comp2 == "propano") return 0.00185;
                if (comp2 == "metano") return 0.00295;
                if (EsHidrocarburo(comp2)) return 0.01576;
            }
            if (comp2 == "etano" && comp1 != "etano")
            {
                if (comp1 == "metano") return 0.00295;
                if (comp1 == "propano") return 0.00185;
                if (EsHidrocarburo(comp1)) return 0.01576;
            }
            if (comp1 == "propano" && comp2 != "propano")
            {
                if (comp2 == "etano") return 0.00185;
                if (comp2 == "metano") return 0.00748;
                if (EsHidrocarburo(comp2)) return 0.01;
            }
            if (comp2 == "propano" && comp1 != "propano")
            {
                if (comp1 == "etano") return 0.00185;
                if (comp1 == "metano") return 0.00748;
                if (EsHidrocarburo(comp1)) return 0.01;
            }
            return 0.0;
        }

        private static double CalcularKij_SRK_Hardcode(string comp1, string comp2)
        {
            comp1 = NormalizarNombreComponente(comp1);
            comp2 = NormalizarNombreComponente(comp2);

            if (comp1 == "h2s")
            {
                if (comp2 == "co2") return 0.102; if (comp2 == "n2") return 0.14;
                if (comp2 == "metano") return 0.085; if (comp2 == "etano") return 0.0829;
                if (comp2 == "propano") return 0.0831; if (comp2 == "ibutano") return 0.0523;
                if (comp2 == "nbutano") return 0.0609; if (comp2 == "npentano") return 0.0697;
                if (comp2 == "nheptano") return 0.0737; if (comp2 == "nnonano") return 0.0542;
                if (comp2 == "ndecano") return 0.04634; if (comp2 == "ipropilciclohexano") return 0.0562;
                if (comp2 == "1-3-5trimetilbenceno") return 0.0282;
            }
            if (comp2 == "h2s")
            {
                if (comp1 == "co2") return 0.102; if (comp1 == "n2") return 0.14;
                if (comp1 == "metano") return 0.085; if (comp1 == "etano") return 0.0829;
                if (comp1 == "propano") return 0.0831; if (comp1 == "ibutano") return 0.0523;
                if (comp1 == "nbutano") return 0.0609; if (comp1 == "npentano") return 0.0697;
                if (comp1 == "nheptano") return 0.0737; if (comp1 == "nnonano") return 0.0542;
                if (comp1 == "ndecano") return 0.04634; if (comp1 == "ipropilciclohexano") return 0.0562;
                if (comp1 == "1-3-5trimetilbenceno") return 0.0282;
            }
            if (comp1 == "co2")
            {
                if (comp2 == "h2s") return 0.102; if (comp2 == "n2") return -0.022;
                if (comp2 == "co") return -0.064; if (comp2 == "metano") return 0.0973;
                if (comp2 == "etano") return 0.1346; if (comp2 == "propano") return 0.1018;
                if (comp2 == "ibutano") return 0.1358; if (comp2 == "nbutano") return 0.1474;
                if (comp2 == "ipentano") return 0.1262; if (comp2 == "npentano") return 0.1278;
                if (comp2 == "nheptano") return 0.1136; if (comp2 == "ndecano") return 0.1377;
                if (comp2 == "propileno") return 0.0914; if (comp2 == "ipropilciclohexano") return 0.1087;
                if (comp2 == "benceno") return 0.0810;
            }
            if (comp2 == "co2")
            {
                if (comp1 == "h2s") return 0.102; if (comp1 == "n2") return -0.022;
                if (comp1 == "co") return -0.064; if (comp1 == "metano") return 0.0973;
                if (comp1 == "etano") return 0.1346; if (comp1 == "propano") return 0.1018;
                if (comp1 == "ibutano") return 0.1358; if (comp1 == "nbutano") return 0.1474;
                if (comp1 == "ipentano") return 0.1262; if (comp1 == "npentano") return 0.1278;
                if (comp1 == "nheptano") return 0.1136; if (comp1 == "ndecano") return 0.1377;
                if (comp1 == "propileno") return 0.0914; if (comp1 == "ipropilciclohexano") return 0.1087;
                if (comp1 == "benceno") return 0.0810;
            }
            if (comp1 == "n2")
            {
                if (comp2 == "h2s") return 0.14; if (comp2 == "co2") return -0.022;
                if (comp2 == "co") return 0.046; if (comp2 == "metano") return 0.0319;
                if (comp2 == "etano") return 0.0388; if (comp2 == "propano") return 0.0807;
                if (comp2 == "ibutano") return 0.1357; if (comp2 == "nbutano") return 0.1007;
                if (comp2 == "nhexano") return 0.1444; if (comp2 == "ndecano") return 0.1293;
                if (comp2 == "benceno") return 0.2131;
            }
            if (comp2 == "n2")
            {
                if (comp1 == "h2s") return 0.14; if (comp1 == "co2") return -0.022;
                if (comp1 == "co") return 0.046; if (comp1 == "metano") return 0.0319;
                if (comp1 == "etano") return 0.0388; if (comp1 == "propano") return 0.0807;
                if (comp1 == "ibutano") return 0.1357; if (comp1 == "nbutano") return 0.1007;
                if (comp1 == "nhexano") return 0.1444; if (comp1 == "ndecano") return 0.1293;
                if (comp1 == "benceno") return 0.2131;
            }
            if (comp1 == "co")
            {
                if (comp2 == "co2") return -0.064; if (comp2 == "n2") return 0.046;
                if (comp2 == "metano") return 0.03; if (comp2 == "etano") return 0.0;
                if (comp2 == "propano") return 0.02; if (comp2 == "noctano") return 0.1;
                if (comp2 == "ipropilciclohexano") return 0.01;
            }
            if (comp2 == "co")
            {
                if (comp1 == "co2") return -0.064; if (comp1 == "n2") return 0.046;
                if (comp1 == "metano") return 0.03; if (comp1 == "etano") return 0.0;
                if (comp1 == "propano") return 0.02; if (comp1 == "noctano") return 0.1;
                if (comp1 == "ipropilciclohexano") return 0.01;
            }
            return 0.0;
        }

        private static string NormalizarNombreComponente(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return string.Empty;
            return nombre.Trim().Replace(" ", "").Replace("-", "").ToLower();
        }

        private static bool EsHidrocarburo(string nombre)
        {
            nombre = NormalizarNombreComponente(nombre);
            return nombre.Contains("metano") || nombre.Contains("etano") || nombre.Contains("propano") ||
                   nombre.Contains("butano") || nombre.Contains("pentano") || nombre.Contains("hexano") ||
                   nombre.Contains("heptano") || nombre.Contains("octano") || nombre.Contains("nonano") ||
                   nombre.Contains("decano") || nombre.Contains("benceno") || nombre.Contains("ciclo");
        }
    }
  
   
}
