using Shared.PropertiesDtos.Enums;
using Shared.Thermodynamics.PureComponents;
using Shared.Thermodynamics.Solvers;
using UnitSystem;

namespace Shared.Thermodynamics.Componentes
{
    public class VaporComponentNode : ChemicalComponentNode
    {
        // ========================================================================
        // PROPIEDADES DE EQUILIBRIO
        // ========================================================================
        public double FugacityCoefficient { get;  set; } = 1.0;
        public double VaporFugacityDenominator { get;  set; }
        public double CompressibilityFactor { get; private set; } = 1.0;
        public EosParameters EosParams { get; private set; } = new();

        // ========================================================================
        // CONSTRUCTOR
        // ========================================================================
        public VaporComponentNode() : base()
        {
        }

        // ========================================================================
        // MÉTODO PRINCIPAL: CALCULAR EQUILIBRIO
        // ========================================================================
        public void CalculateEquilibrium(Temperature temperature, Pressure pressure)
        {
            // 1. Asignar estado del sistema
            Temperature = temperature;
            Pressure = pressure;

            // 2. Calcular límites de saturación (PureComponentData)
            SaturationPressure = PureComponentData.GetVaporPressure(temperature);
            SaturationTemperature = PureComponentData.GetSaturationTemperature(pressure);

            // 3. Volumen molar saturado (EoS - raíz mayor)
            MolarVolume = CalcSaturatedMolarVolume(temperature, pressure);

            // 4. Parámetros EoS
            EosParams = EosParameterFactory.CreateForPureComponent(
                VaporModel,
                CriticalTemperature.GetValue(TemperatureUnits.Kelvin),
                CriticalPressure.GetValue(PressureUnits.KiloPascala),
                AcentricFactor,
                temperature.GetValue(TemperatureUnits.Kelvin),
                pressure.GetValue(PressureUnits.KiloPascala));

            // 5. Coeficiente de fugacidad
            FugacityCoefficient = CalculateFugacityCoefficient(temperature, pressure);

            // 6. Fugacidad de fase
            CalculatePhaseFugacity();
        }

        // ========================================================================
        // VOLUMEN MOLAR SATURADO (EoS - Raíz MAYOR)
        // ========================================================================
        private MolarVolumeSpecific CalcSaturatedMolarVolume(Temperature temperature, Pressure pressure)
        {
            double psatKpa = SaturationPressure.GetValue(PressureUnits.KiloPascala);
            double tKelvin = temperature.GetValue(TemperatureUnits.Kelvin);
            const double R_Gas = 8.314472;

            var paramSat = EosParameterFactory.CreateForPureComponent(
                VaporModel,
                CriticalTemperature.GetValue(TemperatureUnits.Kelvin),
                CriticalPressure.GetValue(PressureUnits.KiloPascala),
                AcentricFactor,
                tKelvin,
                psatKpa);

            var raices = CubicSolver.Solve(paramSat.Factors);
            var validas = raices.Where(r => r > 0.0).ToList();
            double zVaporSat = validas.Any() ? validas.Max() : 1.0;

            double molarVolumeResult = (zVaporSat * R_Gas * tKelvin) / psatKpa;

            return new MolarVolumeSpecific(molarVolumeResult, MolarVolumeSpecificUnits.m3_Kgmol);
        }

        // ========================================================================
        // COEFICIENTE DE FUGACIDAD (EoS - Raíz MAYOR)
        // ========================================================================
        private double CalculateFugacityCoefficient(Temperature temperature, Pressure pressure)
        {
            if (UsesReferenceVaporFugacity())
            {
                CompressibilityFactor = 1.0;
                return 1.0;
            }

            var raices = CubicSolver.Solve(EosParams.Factors);
            // ✅ Pasar B* para validación física completa
            double z = SelectRoot(raices, EosParams.BAsterisk);
            CompressibilityFactor = z;

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
        // SELECCIÓN DE RAÍZ CÚBICA (MAYOR para vapor)
        // ========================================================================
        /// <summary>
        /// Selecciona la raíz físicamente válida para fase vapor.
        /// RACIONAL:
        /// • Vapor → raíz MAYOR (Z más alto = fase menos densa)
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

            // ✅ Para vapor: seleccionar raíz MAYOR (fase menos densa)
            if (validRoots.Any())
                return validRoots.Max();

            // ✅ Fallback seguro: gas ideal (Z=1 siempre es físico)
            return 1.0;
        }

        // ========================================================================
        // FUGACIDAD DE FASE
        // ========================================================================
        private void CalculatePhaseFugacity()
        {
            double pKpa = Pressure.GetValue(PressureUnits.KiloPascala);
            VaporFugacityDenominator = FugacityCoefficient * pKpa;
        }
    }
}
