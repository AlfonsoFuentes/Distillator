using Shared.PropertiesDtos.Enums;
using Shared.Thermodynamics.PureComponents;
using Shared.Thermodynamics.Solvers;
using UnitSystem;

namespace Shared.Thermodynamics.Componentes
{
    public class LiquidComponentNode : ChemicalComponentNode
    {
        // ========================================================================
        // PROPIEDADES DE EQUILIBRIO
        // ========================================================================
        public double ActivityCoefficient { get; set; } = 1.0;
        public double PoyntingFactor { get; private set; } = 1.0;
        public double SaturationFugacityCoefficient { get; private set; } = 1.0;
        public double FugacityCoefficient { get; private set; } = 1.0;
        public double PureLiquidFugacity { get; private set; }
        public double RealFugacity { get; private set; }
        public double LiquidFugacityNumerator { get; private set; }
        public double CompressibilityFactor { get; private set; } = 1.0;
        public EosParameters EosParams { get; private set; } = new();

        // ========================================================================
        // CONSTRUCTOR
        // ========================================================================
        public LiquidComponentNode() : base()
        {
        }

        // ========================================================================
        // MÉTODO PRINCIPAL: CALCULAR EQUILIBRIO
        // ========================================================================
        public void CalculatePureProperties(Temperature temperature, Pressure pressure)
        {
            // 1. Asignar estado del sistema
            Temperature = temperature;
            Pressure = pressure;

            // 2. Calcular límites de saturación (PureComponentData)
            SaturationPressure = PureComponentData.GetVaporPressure(temperature);
            SaturationTemperature = PureComponentData.GetSaturationTemperature(pressure);

            // 3. Volumen molar saturado (PureComponentData)
            MolarVolume = PureComponentData.GetSaturatedMolarVolume(temperature);

            // 4. Factor de Poynting
            PoyntingFactor = CalcPoyntingFactor(temperature, pressure);

            // 5. Parámetros EoS
            EosParams = EosParameterFactory.CreateForPureComponent(
                VaporModel,
                CriticalTemperature.GetValue(TemperatureUnits.Kelvin),
                CriticalPressure.GetValue(PressureUnits.KiloPascala),
                AcentricFactor,
                temperature.GetValue(TemperatureUnits.Kelvin),
                pressure.GetValue(PressureUnits.KiloPascala));

            // 6. Coeficientes de fugacidad
            SaturationFugacityCoefficient = CalculateFugacityCoefficient(
                temperature, SaturationPressure, isSaturationCalc: true);

            FugacityCoefficient = CalculateFugacityCoefficient(
                temperature, pressure, isSaturationCalc: false);

           
        }

        // ========================================================================
        // FACTOR DE POYNTING
        // ========================================================================
        private double CalcPoyntingFactor(Temperature temperature, Pressure pressure)
        {
            if (UsesReferenceVaporFugacity())
                return 1.0;

            double psKpa = SaturationPressure.GetValue(PressureUnits.KiloPascala);
            double pKpa = pressure.GetValue(PressureUnits.KiloPascala);
            double vMolar = MolarVolume.GetValue(MolarVolumeSpecificUnits.m3_Kgmol);
            double tKelvin = temperature.GetValue(TemperatureUnits.Kelvin);

            if (tKelvin <= 0) tKelvin = 298.15;

            const double R_Gas = 8.314472;
            double exponente = (vMolar * (pKpa - psKpa)) / (R_Gas * tKelvin);

            return Math.Exp(exponente);
        }

        // ========================================================================
        // COEFICIENTE DE FUGACIDAD (EoS)
        // ========================================================================
        private double CalculateFugacityCoefficient(Temperature temperature, Pressure pressure, bool isSaturationCalc)
        {
            if (UsesReferenceVaporFugacity())
            {
                if (!isSaturationCalc) CompressibilityFactor = 1.0;
                return 1.0;
            }

            var raices = CubicSolver.Solve(EosParams.Factors);
            // ✅ Pasar B* para validación física completa
            double z = SelectRoot(raices, EosParams.BAsterisk);

            if (!isSaturationCalc)
            {
                CompressibilityFactor = z;
            }

            double valor1 = z - 1.0;
            double argLog1 = z - EosParams.BAsterisk;
            if (argLog1 <= 0) argLog1 = 1e-10;
            double valor2 = -Math.Log(argLog1);
            double valor3 = Math.Sqrt(Math.Pow(EosParams.U, 2.0) - 4.0 * EosParams.W);
            if (valor3 == 0) valor3 = 1e-10;
            double valor4 = EosParams.AAsterisk / (EosParams.BAsterisk * valor3);
            double valor5 = 2.0 * z + EosParams.BAsterisk * (EosParams.U + valor3);
            double valor6 = 2.0 * z + EosParams.BAsterisk * (EosParams.U - valor3);
            double valor7 = 0;

            if (valor5 > 0 && valor6 > 0)
            {
                valor7 = valor1 + valor2 - valor4 * Math.Log(valor5 / valor6);
            }

            return Math.Exp(valor7);
        }

        private bool UsesReferenceVaporFugacity()
        {
            return VaporModel == VaporPhaseModel.IdealGas ||
                   VaporModel == VaporPhaseModel.SteamTables;
        }

        // ========================================================================
        // SELECCIÓN DE RAÍZ CÚBICA (MENOR para líquido)
        // ========================================================================
        /// <summary>
        /// Selecciona la raíz físicamente válida para fase líquida.
        /// RACIONAL:
        /// • Líquido → raíz MENOR (Z más bajo = fase más densa)
        /// • Z debe ser > 0 (volumen positivo)
        /// • Z debe ser > B* - tolerancia (volumen > covolumen)
        /// • Si no hay raíces válidas → fallback a Z=1 (gas ideal seguro)
        /// </summary>
        private double SelectRoot(List<double> roots, double bAsterisk)
        {
            const double minZ = 1e-6;  // Z mínimo físicamente razonable
            const double tolerance = 1e-8;  // Tolerancia para errores de punto flotante

            // ✅ Filtrar raíces físicamente válidas
            var validRoots = roots
                .Where(z => z > minZ && z > bAsterisk - tolerance)
                .ToList();

            // ✅ Para líquido: seleccionar raíz MENOR (fase más densa)
            if (validRoots.Any())
                return validRoots.Min();

            // ✅ Fallback seguro: gas ideal (Z=1 siempre es físico)
            return 1.0;
        }

        // ========================================================================
        // FUGACIDAD DE FASE
        // ========================================================================
        public void CalculatePhaseFugacity()
        {
            double psatKpa = SaturationPressure.GetValue(PressureUnits.KiloPascala);

            PureLiquidFugacity = SaturationFugacityCoefficient * psatKpa * PoyntingFactor;
            LiquidFugacityNumerator = ActivityCoefficient * PureLiquidFugacity;
            RealFugacity = MolarFraction * ActivityCoefficient * PureLiquidFugacity;
        }
    }
}
