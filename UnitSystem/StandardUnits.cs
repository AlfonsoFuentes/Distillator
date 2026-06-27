using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UnitSystem
{
    public static class SIUnitTypes
    {
        public static readonly UnitType Diameter = new UnitType("diameter");
        public static readonly UnitType Length = new UnitType("metre");
        public static readonly UnitType Mol = new UnitType("Kg-mol");
        public static readonly UnitType Mass = new UnitType("kilogram");
        public static readonly UnitType Time = new UnitType("second");
        public static readonly UnitType ElectricCurrent = new UnitType("ampere");
        public static readonly UnitType ThermodynamicTemperature = new UnitType("kelvin");
        public static readonly UnitType AmountOfSubstance = new UnitType("mole");
        public static readonly UnitType LuminousIntensity = new UnitType("candela");
        public static readonly UnitType Percentage = new UnitType("Percentage");
        public static readonly UnitType UnitLess = new UnitType("UnitLess");
        public static readonly UnitType Currency = new UnitType("Currency");
        public static readonly UnitType VolumeFood = new UnitType("VolumeFood");
        public static readonly UnitType LineVelocity = new UnitType("LineVelocity");
        public static readonly UnitType EA = new UnitType("EA");
        public static readonly UnitType Case = new UnitType("Case");
        public static readonly UnitType Day = new UnitType("day");

    }
    [UnitDefinitionClass]
    public static class UnitLessUnits
    {
        public static readonly UnitMeasure None = new UnitMeasure("None", "xx", SIUnitTypes.UnitLess, "UnitLess");
    }
    [UnitDefinitionClass]
    public static class FoodUnits
    {
        public static readonly UnitMeasure Taza_1 = new UnitMeasure("Taza", "Taza", 250 * VolumeUnits.MilliLiter, "VolumeFood");
        public static readonly UnitMeasure Cucharada = new UnitMeasure("Cucharada", "Cucharada", 15 * VolumeUnits.MilliLiter, "VolumeFood");

        public static readonly UnitMeasure Cucharadita = new UnitMeasure("Cucharadita", "Cucharadita", 5 * VolumeUnits.MilliLiter, "VolumeFood");
        public static readonly UnitMeasure Pizca = new UnitMeasure("Pizca", "Pizca", 5 * VolumeUnits.MilliLiter, "VolumeFood");
        public static readonly UnitMeasure mL = new UnitMeasure("mL", "mL", VolumeUnits.MilliLiter, "VolumeFood");
    }
    [UnitDefinitionClass]
    public static class LineVelocityUnits
    {
        public static readonly UnitMeasure EA_min = new UnitMeasure("EA/min", "EA/min", EAUnits.EA / TimeUnits.Minute, "LineVelocity");
        public static readonly UnitMeasure EA_hr = new UnitMeasure("EA/hr", "EA/hr", EA_min / 60, "LineVelocity");
        public static readonly UnitMeasure EA_sg = new UnitMeasure("EA/sg", "EA/sg", EA_min * 60, "LineVelocity");
    }
    [UnitDefinitionClass]
    public static class EAUnits
    {
        public static readonly UnitMeasure EA = new UnitMeasure("EA", "EA", SIUnitTypes.EA, "EA");

    }
    [UnitDefinitionClass]
    public static class CaseUnits
    {
        public static readonly UnitMeasure Case = new UnitMeasure("Case", "Case", EAUnits.EA / EAUnits.EA, "Case");

    }
    [UnitDefinitionClass]
    public static class EACaseUnits
    {
        public static readonly UnitMeasure EACase = new UnitMeasure("EACase", "EACase", EAUnits.EA / CaseUnits.Case, "EACase");

    }
    [UnitDefinitionClass]
    public static class CurrencyUnits
    {
        public static readonly UnitMeasure COP = new UnitMeasure("$COP", "$COP", SIUnitTypes.Currency, "Currency");

    }
    [UnitDefinitionClass]
    public static class LengthUnits
    {
        public static readonly UnitMeasure Meter = new UnitMeasure("meter", "m", SIUnitTypes.Length, "Length");

        public static readonly UnitMeasure MilliMeter = new UnitMeasure("millimeter", "mm", 0.001 * Meter, "Length");
        public static readonly UnitMeasure CentiMeter = new UnitMeasure("centimeter", "cm", 0.01 * Meter, "Length");
        public static readonly UnitMeasure DeciMeter = new UnitMeasure("decimeter", "dm", 0.1 * Meter, "Length");
        public static readonly UnitMeasure DecaMeter = new UnitMeasure("decameter", "Dm", 10.0 * Meter, "Length");
        public static readonly UnitMeasure HectoMeter = new UnitMeasure("hectometer", "Hm", 100.0 * Meter, "Length");
        public static readonly UnitMeasure KiloMeter = new UnitMeasure("kilometer", "Km", 1000.0 * Meter, "Length");

        public static readonly UnitMeasure Inch = new UnitMeasure("inch", "in", 0.0254 * Meter, "Length");
        public static readonly UnitMeasure MicroInch = new UnitMeasure("microinch", "μ-in", 1e6 * Inch, "Length");
        public static readonly UnitMeasure Foot = new UnitMeasure("foot", "ft", 12.0 * Inch, "Length");
        public static readonly UnitMeasure Yard = new UnitMeasure("yard", "yd", 36.0 * Inch, "Length");
        public static readonly UnitMeasure Mile = new UnitMeasure("mile", "mi", 5280.0 * Foot, "Length");
        public static readonly UnitMeasure NauticalMile = new UnitMeasure("nautical mile", "nmi", 1852.0 * Meter, "Length");

        public static readonly UnitMeasure LightYear = new UnitMeasure("light-year", "ly", 9460730472580800.0 * Meter, "Length");
    }
    [UnitDefinitionClass]
    public static class DiameterUnits
    {
        public static readonly UnitMeasure Inch = new UnitMeasure("inch", "in", 0.0254 * LengthUnits.Meter, "diameter");
        public static readonly UnitMeasure MilliMeter = new UnitMeasure("millimeter", "mm", 0.001 * LengthUnits.Meter, "diameter");
    }

    [UnitDefinitionClass]
    public static class SurfaceUnits
    {
        public static readonly UnitMeasure Meter2 = new UnitMeasure("meter²", "m²", LengthUnits.Meter.Power(2), "Surface");
        public static readonly UnitMeasure Are = new UnitMeasure("are", "are", 100.0 * Meter2, "Surface");
        public static readonly UnitMeasure HectAre = new UnitMeasure("hectare", "ha", 10000.0 * Meter2, "Surface");
        public static readonly UnitMeasure KiloMeter2 = new UnitMeasure("kilometer²", "Km²", LengthUnits.KiloMeter.Power(2), "Surface");

        public static readonly UnitMeasure decimeter2 = new UnitMeasure("decimeter²", "dm²", LengthUnits.DeciMeter.Power(2), "Surface");
        public static readonly UnitMeasure centimeter2 = new UnitMeasure("centimeter²", "cm²", LengthUnits.CentiMeter.Power(2), "Surface");
        public static readonly UnitMeasure milimeter2 = new UnitMeasure("milimeter²", "mm²", LengthUnits.MilliMeter.Power(2), "Surface");

        public static readonly UnitMeasure Mile2 = new UnitMeasure("Mile²", "mile²", LengthUnits.Mile.Power(2), "Surface");

        public static readonly UnitMeasure Acre = new UnitMeasure("Acre", "Acre", 4046.854 * Meter2, "Surface");
        public static readonly UnitMeasure Foot2 = new UnitMeasure("Foot²", "ft²", LengthUnits.Foot.Power(2), "Surface");
        public static readonly UnitMeasure inch2 = new UnitMeasure("inch²", "in²", LengthUnits.Inch.Power(2), "Surface");

    }

    [UnitDefinitionClass]
    public static class VolumeUnits
    {
        public static readonly UnitMeasure Meter3 = new UnitMeasure("meter³", "m³", LengthUnits.Meter.Power(3), "Volume");
        public static readonly UnitMeasure KMeter3 = new UnitMeasure("Kmeter³", "Km³", LengthUnits.KiloMeter.Power(3), "Volume");
        public static readonly UnitMeasure dMeter3 = new UnitMeasure("dmeter³", "dm³", LengthUnits.DeciMeter.Power(3), "Volume");
        public static readonly UnitMeasure cMeter3 = new UnitMeasure("cmeter³", "cm³", LengthUnits.CentiMeter.Power(3), "Volume");
        public static readonly UnitMeasure mMeter3 = new UnitMeasure("mmeter³", "mm³", LengthUnits.MilliMeter.Power(3), "Volume");
        public static readonly UnitMeasure Foot3 = new UnitMeasure("Foot³", "ft³", LengthUnits.Foot.Power(3), "Volume");
        public static readonly UnitMeasure inch3 = new UnitMeasure("inch³", "in³", LengthUnits.Inch.Power(3), "Volume");

        public static readonly UnitMeasure Liter = new UnitMeasure("liter", "L", 0.001 * Meter3, "Volume");
        public static readonly UnitMeasure MilliLiter = new UnitMeasure("milliliter", "mL", 0.001 * Liter, "Volume");
        public static readonly UnitMeasure CentiLiter = new UnitMeasure("centiliter", "cL", 0.01 * Liter, "Volume");
        public static readonly UnitMeasure DeciLiter = new UnitMeasure("deciliter", "dL", 0.1 * Liter, "Volume");
        public static readonly UnitMeasure HectoLiter = new UnitMeasure("Hectoliter", "dL", 0.1 * Meter3, "Volume");

        public static readonly UnitMeasure Galon = new UnitMeasure("Galon", "gal", Meter3 * 0.00378541, "Volume");
        public static readonly UnitMeasure Galon_1_4 = new UnitMeasure("1/4 galon", "1_4_gal", Galon / 4, "Volume");
        public static readonly UnitMeasure Barrel = new UnitMeasure("Barrel", "B", Meter3 * 0.15898724, "Volume");
    }


    [UnitDefinitionClass]
    public static class TimeUnits
    {
        public static readonly UnitMeasure Second = new UnitMeasure("second", "s", SIUnitTypes.Time, "Time");
        public static readonly UnitMeasure MicroSecond = new UnitMeasure("microsecond", "μs", 0.000001 * Second, "Time");
        public static readonly UnitMeasure MilliSecond = new UnitMeasure("millisecond", "ms", 0.001 * Second, "Time");
        public static readonly UnitMeasure Minute = new UnitMeasure("minute", "min", 60.0 * Second, "Time");
        public static readonly UnitMeasure Hour = new UnitMeasure("hour", "h", 3600.0 * Second, "Time");
        public static readonly UnitMeasure Day = new UnitMeasure("day", "d", 24.0 * Hour, "Time");
        public static readonly UnitMeasure Month = new UnitMeasure("Month", "mo", 24.0 * Day, "Time");

        public static readonly UnitMeasure Year = new UnitMeasure("Year", "Y", 12 * Month, "Time");
    }
    [UnitDefinitionClass]
    public static class MilestoneDurationUnits
    {


        public static readonly UnitMeasure Day = new UnitMeasure("day", "d", SIUnitTypes.Day, "Time");
        public static readonly UnitMeasure Month = new UnitMeasure("Month", "mo", 24.0 * Day, "Time");
        public static readonly UnitMeasure Weeks = new UnitMeasure("Month", "w", 7 * Day, "Time");
        public static readonly UnitMeasure Year = new UnitMeasure("Year", "Y", 12 * Month, "Time");
    }

    [UnitDefinitionClass]
    public static class VelocityUnits
    {
        public static readonly UnitMeasure MeterPerSecond = new UnitMeasure("meter/second", "m/s", LengthUnits.Meter / TimeUnits.Second, "Speed");
        public static readonly UnitMeasure MeterPerMinute = new UnitMeasure("meter/min", "m/min", LengthUnits.Meter / TimeUnits.Minute, "Speed");
        public static readonly UnitMeasure MeterPerHour = new UnitMeasure("meter/hour", "m/hr", LengthUnits.Meter / TimeUnits.Hour, "Speed");

        public static readonly UnitMeasure KilometerPerSecond = new UnitMeasure("kilometer/second", "km/s", LengthUnits.KiloMeter / TimeUnits.Second, "Speed");
        public static readonly UnitMeasure KilometerPerMinute = new UnitMeasure("kilometer/min", "km/min", LengthUnits.KiloMeter / TimeUnits.Minute, "Speed");
        public static readonly UnitMeasure KilometerPerHour = new UnitMeasure("kilometer/hour", "km/h", LengthUnits.KiloMeter / TimeUnits.Hour, "Speed");


        public static readonly UnitMeasure MilePerHour = new UnitMeasure("mile/hour", "mi/h", LengthUnits.Mile / TimeUnits.Hour, "Speed");
        public static readonly UnitMeasure FeetPerSecond = new UnitMeasure("Feet/Second", "ft/s", LengthUnits.Foot / TimeUnits.Second, "Speed");
        public static readonly UnitMeasure FeetPerMinute = new UnitMeasure("Feet/Minute", "ft/min", LengthUnits.Foot / TimeUnits.Minute, "Speed");
        public static readonly UnitMeasure FeetPerHour = new UnitMeasure("Feet/Hour", "ft/hr", LengthUnits.Foot / TimeUnits.Hour, "Speed");

        public static readonly UnitMeasure Knot = new UnitMeasure("knot", "kn", 1.852 * KilometerPerHour, "Speed");
    }

    [UnitDefinitionClass]
    public static class MassUnits
    {
        public static readonly UnitMeasure KiloGram = new UnitMeasure("kilogram", "Kg", SIUnitTypes.Mass, "Mass");

        public static readonly UnitMeasure Gram = new UnitMeasure("gram", "g", 0.001 * KiloGram, "Mass");
        public static readonly UnitMeasure MilliGram = new UnitMeasure("milligram", "mg", 0.001 * Gram, "Mass");
        public static readonly UnitMeasure Ton = new UnitMeasure("ton", "ton", 1000.0 * KiloGram, "Mass");

        public static readonly UnitMeasure Pound = new UnitMeasure("Pound", "lib", KiloGram * 2.2, "Mass");
        public static readonly UnitMeasure Onze = new UnitMeasure("Onze", "Oz", KiloGram * 35.27394095, "Mass");
    }


    [UnitDefinitionClass]
    public static class ForceUnits
    {
        public static readonly UnitMeasure Newton = new UnitMeasure("newton", "N",
            LengthUnits.Meter * MassUnits.KiloGram * TimeUnits.Second.Power(-2), "Force");
    }

    [UnitDefinitionClass]
    public static class ElectricUnits
    {
        public static readonly UnitMeasure Ampere = new UnitMeasure("ampere", "amp", SIUnitTypes.ElectricCurrent, "Electric");
        public static readonly UnitMeasure Coulomb = new UnitMeasure("coulomb", "C", TimeUnits.Second * Ampere, "Electric");
        public static readonly UnitMeasure Volt = new UnitMeasure("volt", "V", PowerUnits.Watt / Ampere, "Electric");
        public static readonly UnitMeasure Ohm = new UnitMeasure("ohm", "Ω", Volt / Ampere, "Electric");
        public static readonly UnitMeasure Farad = new UnitMeasure("farad", "F", Coulomb / Volt, "Electric");
    }
    [UnitDefinitionClass]
    public static class PowerUnits
    {
        public static readonly UnitMeasure Watt = new UnitMeasure("watt", "W", EnergyUnits.Joule / TimeUnits.Second, "Power");
        public static readonly UnitMeasure KiloWatt = new UnitMeasure("kilowatt", "kW", 1000.0 * Watt, "Power");
        public static readonly UnitMeasure MegaWatt = new UnitMeasure("megawatt", "MW", 1000000.0 * Watt, "Power");
        public static readonly UnitMeasure WattSecond = new UnitMeasure("watt-second", "Wsec", Watt * TimeUnits.Second, "Power");
        public static readonly UnitMeasure WattHour = new UnitMeasure("watt-hour", "Wh", Watt * TimeUnits.Hour, "Power");
        public static readonly UnitMeasure KiloWattHour = new UnitMeasure("kilowatt-hour", "kWh", 1000.0 * WattHour, "Power");
        public static readonly UnitMeasure HorsePower = new UnitMeasure("horsepower", "hp", 0.73549875 * KiloWatt, "Power");
    }
    [UnitDefinitionClass]
    public static class EnergyUnits
    {
        public static readonly UnitMeasure Joule = new UnitMeasure("joule", "J",
            LengthUnits.Meter.Power(2) * MassUnits.KiloGram * TimeUnits.Second.Power(-2), "Energy");
        public static readonly UnitMeasure KiloJoule = new UnitMeasure("kilojoule", "kJ", 1000.0 * Joule, "Energy");
        public static readonly UnitMeasure MegaJoule = new UnitMeasure("megajoule", "MJ", 1000000.0 * Joule, "Energy");
        public static readonly UnitMeasure GigaJoule = new UnitMeasure("gigajoule", "GJ", 1000000000.0 * Joule, "Energy");


        public static readonly UnitMeasure Calorie = new UnitMeasure("calorie", "cal", 4.1868 * Joule, "Energy");
        public static readonly UnitMeasure KiloCalorie = new UnitMeasure("kilocalorie", "kcal", 1000.0 * Calorie, "Energy");



        public static readonly UnitMeasure BTU = new UnitMeasure("BTU", "BTU", 1055.0559 * Joule, "Energy");
        public static readonly UnitMeasure foot_lb = new UnitMeasure("Foot-pound", "ft-pound", 1.3558 * Joule, "Energy");
    }

    [UnitDefinitionClass, UnitConversionClass]
    public static class TemperatureUnits
    {
        public static readonly UnitMeasure Kelvin = new UnitMeasure("Kelvin", "K", SIUnitTypes.ThermodynamicTemperature, "Temperature");
        public static readonly UnitMeasure DegreeCelcius = new UnitMeasure("degree celcius", "°C", new UnitType("celcius temperature"), "Temperature");
        public static readonly UnitMeasure DegreeFahrenheit = new UnitMeasure("degree fahrenheit", "°F", new UnitType("fahrenheit temperature"), "Temperature");

        #region Conversion functions

        public static void RegisterConversions()
        {
            // Register conversion functions:

            // Convert Celcius to Fahrenheit:
            UnitManager.RegisterConversion(DegreeCelcius, DegreeFahrenheit, delegate (Amount amount)
            {
                return new Amount(amount.Value * 9.0 / 5.0 + 32.0, DegreeFahrenheit);
            }
            );

            // Convert Fahrenheit to Celcius:
            UnitManager.RegisterConversion(DegreeFahrenheit, DegreeCelcius, delegate (Amount amount)
            {
                return new Amount((amount.Value - 32.0) / 9.0 * 5.0, DegreeCelcius);
            }
            );

            // Convert Celcius to Kelvin:
            UnitManager.RegisterConversion(DegreeCelcius, Kelvin, delegate (Amount amount)
            {
                return new Amount(amount.Value + 273.15, Kelvin);
            }
            );

            // Convert Kelvin to Celcius:
            UnitManager.RegisterConversion(Kelvin, DegreeCelcius, delegate (Amount amount)
            {
                return new Amount(amount.Value - 273.15, DegreeCelcius);
            }
            );

            // Convert Fahrenheit to Kelvin:
            UnitManager.RegisterConversion(DegreeFahrenheit, Kelvin, delegate (Amount amount)
            {
                return amount.ConvertedTo(DegreeCelcius).ConvertedTo(Kelvin);
            }
            );

            // Convert Kelvin to Fahrenheit:
            UnitManager.RegisterConversion(Kelvin, DegreeFahrenheit, delegate (Amount amount)
            {
                return amount.ConvertedTo(DegreeCelcius).ConvertedTo(DegreeFahrenheit);
            }
            );
        }

        #endregion Conversion functions
    }


    /// <summary>
    /// Unidades de presión con sufijos (a) para absoluta y (g) para manométrica/gauge.
    /// NO hay unidades sin sufijo para evitar ambigüedad.
    /// Las unidades están definidas en pares (absoluta, gauge) para que aparezcan
    /// agrupadas en el dropdown de la UI en el orden: Pa (a), Pa (g), bar (a), bar (g), etc.
    /// </summary>
    [UnitDefinitionClass]
    public static class PressureUnits
    {
        // ─────────────────────────────────────────────────────────
        // 🔹 UNIDADES DEFINIDAS EN PARES (absoluta, gauge) POR BASE
        // El orden de declaración determina el orden en el dropdown de la UI
        // NO hay unidades sin sufijo para evitar ambigüedad
        // ─────────────────────────────────────────────────────────

        // ── Pascal ───────────────────────────────────────────────
        public static readonly UnitMeasure Pascala = new UnitMeasure("pascal (absolute)", "Pa (a)",
            ForceUnits.Newton * LengthUnits.Meter.Power(-2), "Pressure");

        public static readonly UnitMeasure Pascalg = new UnitMeasure("pascal (gauge)", "Pa (g)",
            ForceUnits.Newton * LengthUnits.Meter.Power(-2), "Pressure");

        // ── Kilopascal ───────────────────────────────────────────
        public static readonly UnitMeasure KiloPascala = new UnitMeasure("kilopascal (absolute)", "kPa (a)",
            1000.0 * Pascala, "Pressure");

        public static readonly UnitMeasure KiloPascalg = new UnitMeasure("kilopascal (gauge)", "kPa (g)",
            1000.0 * Pascalg, "Pressure");

        // ── Bar ──────────────────────────────────────────────────
        public static readonly UnitMeasure Bara = new UnitMeasure("bar (absolute)", "bar (a)",
            100000.0 * Pascala, "Pressure");

        public static readonly UnitMeasure Barg = new UnitMeasure("bar (gauge)", "bar (g)",
            100000.0 * Pascalg, "Pressure");

        // ── Atmosphere ───────────────────────────────────────────
        public static readonly UnitMeasure Atmospherea = new UnitMeasure("atmosphere (absolute)", "atm (a)",
            101325.0 * Pascala, "Pressure");

        public static readonly UnitMeasure Atmosphereg = new UnitMeasure("atmosphere (gauge)", "atm (g)",
            101325.0 * Pascalg, "Pressure");

        // ── PSI ──────────────────────────────────────────────────
        public static readonly UnitMeasure Psia = new UnitMeasure("psi (absolute)", "psi (a)",
            6.894759087 * KiloPascala, "Pressure");

        public static readonly UnitMeasure Psig = new UnitMeasure("psi (gauge)", "psi (g)",
            6.894759087 * KiloPascalg, "Pressure");

        // ── kgf/cm² ──────────────────────────────────────────────
        public static readonly UnitMeasure KgPercm2a = new UnitMeasure("Kg/cm2 (absolute)", "Kg/cm2 (a)",
            98.06652048 * KiloPascala, "Pressure");

        public static readonly UnitMeasure KgPercm2g = new UnitMeasure("Kg/cm2 (gauge)", "Kg/cm2 (g)",
            98.06652048 * KiloPascalg, "Pressure");

        // ── Meter Water Column ───────────────────────────────────
        public static readonly UnitMeasure MeterWatera = new UnitMeasure("Meter Water Column (absolute)", "MCA (a)",
            9.806382778 * KiloPascala, "Pressure");

        public static readonly UnitMeasure MeterWaterg = new UnitMeasure("Meter Water Column (gauge)", "MCA (g)",
            9.806382778 * KiloPascalg, "Pressure");

        // ── Inch Water Column ────────────────────────────────────
        public static readonly UnitMeasure InchWatera = new UnitMeasure("Inch Water Column (absolute)", "Inch WC (a)",
            0.249082008 * KiloPascala, "Pressure");

        public static readonly UnitMeasure InchWaterg = new UnitMeasure("Inch Water Column (gauge)", "Inch WC (g)",
            0.249082008 * KiloPascalg, "Pressure");

        // ── cm Water Column ──────────────────────────────────────
        public static readonly UnitMeasure centimeterWatera = new UnitMeasure("centimeter Water Column (absolute)", "cm WC (a)",
            MeterWatera * 100, "Pressure");

        public static readonly UnitMeasure centimeterWaterg = new UnitMeasure("centimeter Water Column (gauge)", "cm WC (g)",
            MeterWaterg * 100, "Pressure");

        // ── Feet Water Column ────────────────────────────────────
        // ── Feet Water Column ────────────────────────────────────
        public static readonly UnitMeasure feetWatera = new UnitMeasure("Feet Water Column (absolute)", "feet CA (a)",
            KiloPascala * 2.988301699, "Pressure");

        public static readonly UnitMeasure feetWaterg = new UnitMeasure("Feet Water Column (gauge)", "feet CA (g)",
            KiloPascalg * 2.988301699, "Pressure");

        // ── Inch Hg ──────────────────────────────────────────────
        public static readonly UnitMeasure InchHga = new UnitMeasure("inch Hg (absolute)", "inch Hg (a)",
            KiloPascala * 3.3863787, "Pressure");

        public static readonly UnitMeasure InchHgg = new UnitMeasure("inch Hg (gauge)", "inch Hg (g)",
            KiloPascalg * 3.3863787, "Pressure");

        // ── cm Hg ────────────────────────────────────────────────
        public static readonly UnitMeasure cmHga = new UnitMeasure("cm Hg (absolute)", "cm Hg (a)",
            KiloPascala * 1.332199, "Pressure");

        public static readonly UnitMeasure cmHgg = new UnitMeasure("cm Hg (gauge)", "cm Hg (g)",
            KiloPascalg * 1.332199, "Pressure");

        // ── mm Hg ────────────────────────────────────────────────
        public static readonly UnitMeasure mmHga = new UnitMeasure("mm Hg (absolute)", "mm Hg (a)",
            KiloPascala * 0.133322, "Pressure");

        public static readonly UnitMeasure mmHgg = new UnitMeasure("mm Hg (gauge)", "mm Hg (g)",
            KiloPascalg * 0.133322, "Pressure");


        // ─────────────────────────────────────────────────────────
        // 🔹 FUNCIONES DE CONVERSIÓN gauge ↔ absoluta
        // ─────────────────────────────────────────────────────────

       
    }

    [UnitDefinitionClass]
    public static class MotorVelocityUnits
    {
        public static readonly UnitMeasure Hertz = new UnitMeasure("Hertz", "Hz", TimeUnits.Second.Power(-1), "Frequency");
        public static readonly UnitMeasure MegaHerts = new UnitMeasure("MegaHertz", "Mhz", 1000000.0 * Hertz, "Frequency");
        public static readonly UnitMeasure RPM = new UnitMeasure("Rounds per minute", "rpm", TimeUnits.Minute.Power(-1), "Frequency");
        public static readonly UnitMeasure Percentage = new UnitMeasure("%", "%", SIUnitTypes.Percentage, "Frequency");
    }
    [UnitDefinitionClass]
    public static class PercentageUnits
    {
        public static readonly UnitMeasure Percentage = new UnitMeasure("%", "%", SIUnitTypes.Percentage, "Percentage");
    }
    [UnitDefinitionClass]
    public static class AmountOfSubstanceUnits
    {
        public static readonly UnitMeasure Mole = new UnitMeasure("mole", "mol", SIUnitTypes.AmountOfSubstance, "AmountOfSubstance");
        public static readonly UnitMeasure KMole = new UnitMeasure("Kmole", "Kmol", 1000 * Mole, "AmountOfSubstance");
        public static readonly UnitMeasure MilliMole = new UnitMeasure("m-mole", "m-mol", Mole / 1000, "AmountOfSubstance");
        public static readonly UnitMeasure lbMole = new UnitMeasure("lbmole", "lbmol", KMole / 2.2, "AmountOfSubstance");
    }
    [UnitDefinitionClass]
    public static class LuminousIntensityUnits
    {
        public static readonly UnitMeasure Candela = new UnitMeasure("candela", "cd", SIUnitTypes.LuminousIntensity, "Louminous");
    }
    [UnitDefinitionClass]
    public static class HeatTransferCoefficientUnits
    {
        public static readonly UnitMeasure BTU_hr_ft2_F = new UnitMeasure("BTU/(hr*ft2*ºF)", "BTU/(hr*ft2*ºF)",
         EnergyUnits.BTU / TimeUnits.Hour / SurfaceUnits.Foot2 / TemperatureUnits.DegreeFahrenheit, "HeatTransferCoefficient");

        public static readonly UnitMeasure cal_seg_cm2_C = new UnitMeasure("cal/(seg*cm2*ºC)", "cal/(seg*cm2*ºC)",
            BTU_hr_ft2_F / 0.00013562299126, "HeatTransferCoefficient");

        public static readonly UnitMeasure Joul_seg_m2_C = new UnitMeasure("Joule/(seg*m2*ºC)", "J/(seg*m2*ºC)",
           BTU_hr_ft2_F / 5.678263398, "HeatTransferCoefficient");

        public static readonly UnitMeasure kcal_hr_m2_C = new UnitMeasure("Kcal/(hr*m2*ºC)", "kcal/(hr*m2*ºC)",
           BTU_hr_ft2_F / 4.8824276853, "HeatTransferCoefficient");

        public static readonly UnitMeasure kcal_hr_ft2_C = new UnitMeasure("Kcal/(hr*ft2*ºC)", "kcal/(hr*ft2*ºC)",
           BTU_hr_ft2_F / 0.45359237435, "HeatTransferCoefficient");

        public static readonly UnitMeasure Watt_m2_K = new UnitMeasure("Watt/(m2*K)", "Watt/(m2*K)",
           BTU_hr_ft2_F / 5.678263398, "HeatTransferCoefficient");

        public static readonly UnitMeasure Watt_m2_C = new UnitMeasure("Watt/(m2*°C)", "Watt/(m2*°C)",
          BTU_hr_ft2_F / 5.678263398, "HeatTransferCoefficient");
    }
    [UnitDefinitionClass]
    public static class MassDensityUnits
    {
        public static readonly UnitMeasure Kg_m3 = new UnitMeasure("Kg/m3", "Kg/m3", MassUnits.KiloGram / VolumeUnits.Meter3, "MassDensity");
        public static readonly UnitMeasure g_m3 = new UnitMeasure("g/m3", "g/m3", MassUnits.Gram / VolumeUnits.Meter3, "MassDensity");
        public static readonly UnitMeasure mg_m3 = new UnitMeasure("mg/m3", "mg/m3", MassUnits.MilliGram / VolumeUnits.Meter3, "MassDensity");
        public static readonly UnitMeasure Kg_cm3 = new UnitMeasure("Kg/cm3", "Kg/cm3", MassUnits.KiloGram / VolumeUnits.cMeter3, "MassDensity");
        public static readonly UnitMeasure g_cm3 = new UnitMeasure("g/cm3", "g/cm3", MassUnits.Gram / VolumeUnits.cMeter3, "MassDensity");
        public static readonly UnitMeasure mg_L = new UnitMeasure("mg/L", "mg/L", MassUnits.MilliGram / VolumeUnits.Liter, "MassDensity");
        public static readonly UnitMeasure g_L = new UnitMeasure("g/L", "g/L", MassUnits.Gram / VolumeUnits.Liter, "MassDensity");
        public static readonly UnitMeasure g_mL = new UnitMeasure("g/mL", "g/mL", MassUnits.Gram / VolumeUnits.MilliLiter, "MassDensity");

        public static readonly UnitMeasure Kg_L = new UnitMeasure("Kg/L", "Kg/L", MassUnits.KiloGram / VolumeUnits.Liter, "MassDensity");
        public static readonly UnitMeasure lb_ft3 = new UnitMeasure("lb/ft3", "lb/ft3", MassUnits.Pound / VolumeUnits.Foot3, "MassDensity");
        public static readonly UnitMeasure lb_in3 = new UnitMeasure("lb/in3", "lb/in3", MassUnits.Pound / VolumeUnits.inch3, "MassDensity");
        public static readonly UnitMeasure lb_gal = new UnitMeasure("lb/gal", "lb/gal", MassUnits.Pound / VolumeUnits.Galon, "MassDensity");


    }
    [UnitDefinitionClass]
    public static class MolarDensityUnits
    {
        public static readonly UnitMeasure Kgmol_m3 = new UnitMeasure("Kgmol/m3", "Kgmol/m3", AmountOfSubstanceUnits.KMole / VolumeUnits.Meter3, "MolarDensity");
        public static readonly UnitMeasure gmol_m3 = new UnitMeasure("gmol/m3", "gmol/m3", AmountOfSubstanceUnits.Mole / VolumeUnits.Meter3, "MolarDensity");

        public static readonly UnitMeasure Kgmol_cm3 = new UnitMeasure("Kgmol/cm3", "Kgmol/cm3", AmountOfSubstanceUnits.KMole / VolumeUnits.cMeter3, "MolarDensity");
        public static readonly UnitMeasure gmol_cm3 = new UnitMeasure("gmol/cm3", "gmol/cm3", AmountOfSubstanceUnits.Mole / VolumeUnits.cMeter3, "MolarDensity");

        public static readonly UnitMeasure gmol_L = new UnitMeasure("gmol/L", "gmol/L", AmountOfSubstanceUnits.Mole / VolumeUnits.Liter, "MolarDensity");
        public static readonly UnitMeasure gmol_mL = new UnitMeasure("gmol/mL", "gmol/mL", AmountOfSubstanceUnits.Mole / VolumeUnits.MilliLiter, "MolarDensity");

        public static readonly UnitMeasure Kgmol_L = new UnitMeasure("Kgmol/L", "Kgmol/L", AmountOfSubstanceUnits.KMole / VolumeUnits.Liter, "MolarDensity");
        public static readonly UnitMeasure lbmol_ft3 = new UnitMeasure("lbmol/ft3", "lbmol/ft3", AmountOfSubstanceUnits.lbMole / VolumeUnits.Foot3, "MolarDensity");
        public static readonly UnitMeasure lbmol_in3 = new UnitMeasure("lbmol/in3", "lbmol/in3", AmountOfSubstanceUnits.lbMole / VolumeUnits.inch3, "MolarDensity");
        public static readonly UnitMeasure lbmol_gal = new UnitMeasure("lbmol/gal", "lbmol/gal", AmountOfSubstanceUnits.lbMole / VolumeUnits.Galon, "MolarDensity");


    }
    [UnitDefinitionClass]
    public static class MassVolumeSpecificUnits
    {
        public static readonly UnitMeasure m3_Kg = new UnitMeasure("m3/Kg", "m3/Kg", 1 / (MassUnits.KiloGram / VolumeUnits.Meter3), "MassVolumeSpecific");
        public static readonly UnitMeasure m3_g = new UnitMeasure("m3/g", "m3/g", 1 / (MassUnits.Gram / VolumeUnits.Meter3), "MassVolumeSpecific");
        public static readonly UnitMeasure m3_mg = new UnitMeasure("m3/mg", "m3/mg", 1 / (MassUnits.MilliGram / VolumeUnits.Meter3), "MassVolumeSpecific");
        public static readonly UnitMeasure cm3_Kg = new UnitMeasure("cm3/Kg", "cm3/Kg3", 1 / (MassUnits.KiloGram / VolumeUnits.cMeter3), "MassVolumeSpecific");
        public static readonly UnitMeasure cm3_g = new UnitMeasure("cm3/g", "cm3/g", 1 / (MassUnits.Gram / VolumeUnits.cMeter3), "MassVolumeSpecific");
        public static readonly UnitMeasure L_mg = new UnitMeasure("L/mg", "L/mg", 1 / (MassUnits.MilliGram / VolumeUnits.Liter), "MassVolumeSpecific");
        public static readonly UnitMeasure L_g = new UnitMeasure("L/g", "L/g", 1 / (MassUnits.Gram / VolumeUnits.Liter), "MassVolumeSpecific");
        public static readonly UnitMeasure mL_g = new UnitMeasure("mL/g", "mL/g", 1 / (MassUnits.Gram / VolumeUnits.MilliLiter), "MassVolumeSpecific");

        public static readonly UnitMeasure L_Kg = new UnitMeasure("L/Kg", "L/Kg", 1 / (MassUnits.KiloGram / VolumeUnits.Liter), "MassVolumeSpecific");
        public static readonly UnitMeasure ft3_lb = new UnitMeasure("ft3/lb", "ft3/lb", 1 / (MassUnits.Pound / VolumeUnits.Foot3), "MassVolumeSpecific");
        public static readonly UnitMeasure in3_lb = new UnitMeasure("in3/lb", "in3/lb", 1 / (MassUnits.Pound / VolumeUnits.inch3), "MassVolumeSpecific");
        public static readonly UnitMeasure gal_lb = new UnitMeasure("gal/lb", "gal/lb", 1 / (MassUnits.Pound / VolumeUnits.Galon), "MassVolumeSpecific");


    }
    [UnitDefinitionClass]
    public static class MolarVolumeSpecificUnits
    {
        public static readonly UnitMeasure m3_Kgmol = new UnitMeasure("m3/Kgmol", "m3/Kgmol", 1 / (AmountOfSubstanceUnits.KMole / VolumeUnits.Meter3), "MolarVolumeSpecific");
        public static readonly UnitMeasure m3_gmol = new UnitMeasure("m3/gmol", "m3/gmol", 1 / (AmountOfSubstanceUnits.Mole / VolumeUnits.Meter3), "MolarVolumeSpecific");
        public static readonly UnitMeasure m3_mgmol = new UnitMeasure("m3/mgmol", "m3/mgmol", 1 / (AmountOfSubstanceUnits.MilliMole / VolumeUnits.Meter3), "MolarVolumeSpecific");
        public static readonly UnitMeasure cm3_Kgmol = new UnitMeasure("cm3/Kgmol", "cm3/Kgmol", 1 / (AmountOfSubstanceUnits.KMole / VolumeUnits.cMeter3), "MolarVolumeSpecific");
        public static readonly UnitMeasure cm3_gmol = new UnitMeasure("cm3/gmol", "cm3/gmol", 1 / (AmountOfSubstanceUnits.Mole / VolumeUnits.cMeter3), "MolarVolumeSpecific");
        public static readonly UnitMeasure L_mgmol = new UnitMeasure("L/mgmol", "L/mgmol", 1 / (AmountOfSubstanceUnits.MilliMole / VolumeUnits.Liter), "MolarVolumeSpecific");
        public static readonly UnitMeasure L_gmol = new UnitMeasure("L/gmol", "L/gmol", 1 / (AmountOfSubstanceUnits.Mole / VolumeUnits.Liter), "MolarVolumeSpecific");
        public static readonly UnitMeasure mL_gmol = new UnitMeasure("mL/gmol", "mL/gmol", 1 / (AmountOfSubstanceUnits.Mole / VolumeUnits.MilliLiter), "MolarVolumeSpecific");

        public static readonly UnitMeasure L_Kgmol = new UnitMeasure("L/Kgmol", "L/Kgmol", 1 / (AmountOfSubstanceUnits.KMole / VolumeUnits.Liter), "MolarVolumeSpecific");
        public static readonly UnitMeasure ft3_lbmol = new UnitMeasure("ft3/lbmol", "ft3/lbmol", 1 / (AmountOfSubstanceUnits.lbMole / VolumeUnits.Foot3), "MolarVolumeSpecific");
        public static readonly UnitMeasure in3_lbmol = new UnitMeasure("in3/lbmol", "in3/lbmol", 1 / (AmountOfSubstanceUnits.lbMole / VolumeUnits.inch3), "MolarVolumeSpecific");
        public static readonly UnitMeasure gal_lbmol = new UnitMeasure("gal/lbmol", "gal/lbmol", 1 / (AmountOfSubstanceUnits.lbMole / VolumeUnits.Galon), "MolarVolumeSpecific");


    }
    [UnitDefinitionClass]
    public static class PressureDropLengthUnits
    {
        public static readonly UnitMeasure psi_100ft = new UnitMeasure("Pound/in2/100", "psi/100 ft", PressureDropUnits.psi / (100 * LengthUnits.Foot), "DropPressureLength");
        public static readonly UnitMeasure Kpa_m = new UnitMeasure("KiloPascal/meter", "kPa/m", PressureDropUnits.KiloPascal / LengthUnits.Meter, "DropPressureLength");

    }
    [UnitDefinitionClass]
    public static class PressureDropUnits
    {
        public static readonly UnitMeasure Pascal = new UnitMeasure("Pascal", "Pa", ForceUnits.Newton * LengthUnits.Meter.Power(-2), "DropPressure");
        public static readonly UnitMeasure HectoPascal = new UnitMeasure("hectoPascal", "hPa", 100.0 * Pascal, "DropPressure");
        public static readonly UnitMeasure KiloPascal = new UnitMeasure("kiloPascal", "KPa", 1000.0 * Pascal, "DropPressure");
        public static readonly UnitMeasure Bar = new UnitMeasure("bar", "bar", 100000.0 * Pascal, "DropPressure");
        public static readonly UnitMeasure MilliBar = new UnitMeasure("millibar", "mbar", 0.001 * Bar, "DropPressure");
        public static readonly UnitMeasure Atmosphere = new UnitMeasure("atmosphere", "atm", 101325.0 * Pascal, "DropPressure");

        public static readonly UnitMeasure KgPercm2 = new UnitMeasure("Kg/cm2", "Kg/cm2", 98.06652048 * KiloPascal, "DropPressure");
        public static readonly UnitMeasure psi = new UnitMeasure("Pound/in2", "psi", 6.894759087 * KiloPascal, "DropPressure");

        public static readonly UnitMeasure MeterWater = new UnitMeasure("Meter Water Column", "MCA", 9.806382778 * KiloPascal, "DropPressure");
        public static readonly UnitMeasure InchWater = new UnitMeasure("Inch Water Column", "Inch WC", 0.249082008 * KiloPascal, "DropPressure");
        public static readonly UnitMeasure centimeterWater = new UnitMeasure("cMeter Water Column", "cm WC", MeterWater * 100, "DropPressure");
        public static readonly UnitMeasure feetWater = new UnitMeasure("Feet Water Column", "feet CA", KiloPascal * 2.988301699, "DropPressure");

        public static readonly UnitMeasure InchHg = new UnitMeasure("Inch Hg", "Inch Hg", KiloPascal * 3.3863787, "DropPressure");
        public static readonly UnitMeasure cmHg = new UnitMeasure("cm Hg", "cm Hg", KiloPascal * 1.332199, "DropPressure");
        public static readonly UnitMeasure mmHg = new UnitMeasure("mm Hg", "mm Hg", KiloPascal * 0.133322, "DropPressure");
    }
    [UnitDefinitionClass, UnitConversionClass]
    public static class ThermalConductivityUnits
    {
        public static readonly UnitMeasure W_m_K = new UnitMeasure("W/m/K", "W/m/K",
            PowerUnits.Watt / LengthUnits.Meter / TemperatureUnits.Kelvin, "ThermalConductivity");

        public static readonly UnitMeasure W_cm_C = new UnitMeasure("W/cm/°C", "W/cm/°C",
           W_m_K / 0.01, "ThermalConductivity");

        public static readonly UnitMeasure kW_m_K = new UnitMeasure("kW/m/K", "kW/m/K",
            W_m_K / 0.001, "ThermalConductivity");

        public static readonly UnitMeasure cal_sg_cm_C = new UnitMeasure("cal/sg/cm/°C", "cal/sg/cm/°C",
          W_m_K / 0.002388459, "ThermalConductivity");

        public static readonly UnitMeasure kcal_hr_m_C = new UnitMeasure("kcal/hr/m/°C", "kcal/hr/m/°C",
         W_m_K / 0.8598452279, "ThermalConductivity");

        public static readonly UnitMeasure BTU_in_sg_ft2_m_F = new UnitMeasure("BTU in/sg/ft2/°F", "BTU in/sg/ft2/°F",
         W_m_K / 0.0019259644, "ThermalConductivity");

        public static readonly UnitMeasure BTU_ft_hr_ft2_m_F = new UnitMeasure("BTU ft/hr/ft2/°F", "BTU ft/hr/ft2/°F",
        W_m_K / 0.5777893165, "ThermalConductivity");

        public static readonly UnitMeasure BTU_in_hr_ft2_m_F = new UnitMeasure("BTU in/hr/ft2/°F", "BTU in/hr/ft2/°F",
        W_m_K / 6.9334717985, "ThermalConductivity");

    }

    [UnitDefinitionClass]
    public static class VolumeEnergyUnits
    {
        public static readonly UnitMeasure J_m3 = new UnitMeasure("Joule/m3", "Joule/m3",
            EnergyUnits.Joule / VolumeUnits.Meter3, "VolumeEnergy");
        public static readonly UnitMeasure KJ_m3 = new UnitMeasure("KJoule/m3", "KJoule/m3",
           EnergyUnits.KiloJoule / VolumeUnits.Meter3, "VolumeEnergy");

        public static readonly UnitMeasure cal_m3 = new UnitMeasure("cal/m3", "cal/m3",
           EnergyUnits.Calorie / VolumeUnits.Meter3, "VolumeEnergy");
        public static readonly UnitMeasure Kcal_m3 = new UnitMeasure("KJoule/m3", "KJoule/m3",
           EnergyUnits.KiloCalorie / VolumeUnits.Meter3, "VolumeEnergy");

        public static readonly UnitMeasure BTU_ft3 = new UnitMeasure("BTU/ft3", "BTU/ft3",
          EnergyUnits.BTU / VolumeUnits.Foot3, "VolumeEnergy");

        public static readonly UnitMeasure BTU_in3 = new UnitMeasure("BTU/in3", "BTU/in3",
         EnergyUnits.BTU / VolumeUnits.inch3, "VolumeEnergy");

        public static readonly UnitMeasure BTU_gal = new UnitMeasure("BTU/gal", "BTU/gal",
         EnergyUnits.BTU / VolumeUnits.Galon, "VolumeEnergy");

    }

    [UnitDefinitionClass]
    public static class MassEnergyUnits
    {
        public static readonly UnitMeasure J_Kg = new UnitMeasure("Joule/Kg", "Joule/Kg",
            EnergyUnits.Joule / MassUnits.KiloGram, "MassEnergy");
        public static readonly UnitMeasure J_g = new UnitMeasure("Joule/g", "Joule/g",
           EnergyUnits.Joule / MassUnits.Gram, "MassEnergy");

        public static readonly UnitMeasure KJ_Kg = new UnitMeasure("KJoule/Kg", "KJoule/Kg",
           EnergyUnits.KiloJoule / MassUnits.KiloGram, "MassEnergy");
        public static readonly UnitMeasure KJ_g = new UnitMeasure("KJoule/g", "KJoule/g",
          EnergyUnits.KiloJoule / MassUnits.Gram, "MassEnergy");


        public static readonly UnitMeasure cal_Kg = new UnitMeasure("cal/Kg", "cal/Kg",
           EnergyUnits.Calorie / MassUnits.KiloGram, "MassEnergy");
        public static readonly UnitMeasure cal_g = new UnitMeasure("cal/g", "cal/g",
          EnergyUnits.Calorie / MassUnits.Gram, "MassEnergy");

        public static readonly UnitMeasure Kcal_Kg = new UnitMeasure("Kcal/Kg", "Kcal/Kg",
          EnergyUnits.KiloCalorie / MassUnits.KiloGram, "MassEnergy");
        public static readonly UnitMeasure Kcal_g = new UnitMeasure("Kcal/g", "Kcal/g",
          EnergyUnits.KiloCalorie / MassUnits.Gram, "MassEnergy");


        public static readonly UnitMeasure BTU_lb = new UnitMeasure("BTU/lb", "BTU/lb",
          EnergyUnits.BTU / MassUnits.Pound, "MassEnergy");



    }
    [UnitDefinitionClass]
    public static class MolarEnergyUnits
    {
        public static readonly UnitMeasure J_Kgmol = new UnitMeasure("Joule/Kgmol", "Joule/Kgmol",
            EnergyUnits.Joule / AmountOfSubstanceUnits.KMole, "MolarEnergy");

        public static readonly UnitMeasure J_gmol = new UnitMeasure("Joule/gmol", "Joule/g",
           EnergyUnits.Joule / AmountOfSubstanceUnits.Mole, "MolarEnergy");

        public static readonly UnitMeasure KJ_Kgmol = new UnitMeasure("KJoule/Kgmol", "KJoule/Kgmol",
           EnergyUnits.KiloJoule / AmountOfSubstanceUnits.KMole, "MolarEnergy");

        public static readonly UnitMeasure KJ_gmol = new UnitMeasure("KJoule/gmol", "KJoule/gmol",
          EnergyUnits.KiloJoule / AmountOfSubstanceUnits.Mole, "MolarEnergy");


        public static readonly UnitMeasure cal_Kgmol = new UnitMeasure("cal/Kgmol", "cal/Kgmol",
           EnergyUnits.Calorie / AmountOfSubstanceUnits.KMole, "MolarEnergy");

        public static readonly UnitMeasure cal_gmol = new UnitMeasure("cal/gmol", "cal/gmol",
          EnergyUnits.Calorie / AmountOfSubstanceUnits.Mole, "MolarEnergy");

        public static readonly UnitMeasure Kcal_Kgmol = new UnitMeasure("Kcal/Kgmol", "Kcal/Kgmol",
          EnergyUnits.KiloCalorie / AmountOfSubstanceUnits.KMole, "MolarEnergy");

        public static readonly UnitMeasure Kcal_gmol = new UnitMeasure("Kcal/gmol", "Kcal/gmol",
          EnergyUnits.KiloCalorie / AmountOfSubstanceUnits.Mole, "MolarEnergy");


        public static readonly UnitMeasure BTU_lbmol = new UnitMeasure("BTU/lbmol", "BTU/lbmol",
          EnergyUnits.BTU / AmountOfSubstanceUnits.lbMole, "MolarEnergy");



    }
    [UnitDefinitionClass]
    public static class MassEntropyUnits
    {

        public static readonly UnitMeasure BTU_lb_F = new UnitMeasure("BTU/lb/°F", "BTU/lb/°F",
          EnergyUnits.BTU / MassUnits.Pound / TemperatureUnits.DegreeFahrenheit, "MassEntropy");

        public static readonly UnitMeasure KJ_Kg_C = new UnitMeasure("KJoule/Kg/°C", "KJoule/Kg/°C",
           BTU_lb_F / 4.1868, "MassEntropy");

        public static readonly UnitMeasure J_g_C = new UnitMeasure("Joule/g/°C", "Joule/g/°C",
           KJ_Kg_C, "MassEntropy");

        public static readonly UnitMeasure J_Kg_C = new UnitMeasure("Joule/Kg/°C", "Joule/Kg/°C",
             BTU_lb_F / 4186.8, "MassEntropy");



        public static readonly UnitMeasure cal_g_C = new UnitMeasure("cal/g/°C", "cal/g/°C",
       BTU_lb_F / 1, "MassEntropy");

        public static readonly UnitMeasure Kcal_Kg_C = new UnitMeasure("Kcal/Kg/°C", "Kcal/Kg/°C",
          BTU_lb_F / 1, "MassEntropy");



    }
    [UnitDefinitionClass]
    public static class MolarEntropyUnits
    {

        public static readonly UnitMeasure BTU_lbmol_F = new UnitMeasure("BTU/lbmol/°F", "BTU/lbmol/°F",
          EnergyUnits.BTU / AmountOfSubstanceUnits.lbMole / TemperatureUnits.DegreeFahrenheit, "MolarEntropy");

        public static readonly UnitMeasure KJ_Kgmol_C = new UnitMeasure("KJoule/Kgmol/°C", "KJoule/Kgmol/°C",
           BTU_lbmol_F / 4.1868, "MolarEntropy");

        public static readonly UnitMeasure J_mol_C = new UnitMeasure("Joule/gmol/°C", "Joule/gmol/°C",
           KJ_Kgmol_C, "MolarEntropy");

        public static readonly UnitMeasure J_Kgmol_C = new UnitMeasure("Joule/Kgmol/°C", "Joule/Kgmol/°C",
             BTU_lbmol_F / 4186.8, "MolarEntropy");



        public static readonly UnitMeasure cal_mol_C = new UnitMeasure("cal/gmol/°C", "cal/gmol/°C",
       BTU_lbmol_F / 1, "MolarEntropy");

        public static readonly UnitMeasure Kcal_Kgmol_C = new UnitMeasure("Kcal/Kgmol/°C", "Kcal/Kgmol/°C",
          BTU_lbmol_F / 1, "MolarEntropy");



    }
    [UnitDefinitionClass]
    public static class MassFlowUnits
    {
        public static readonly UnitMeasure Kg_min = new UnitMeasure("Kg/min", "Kg/min",
        MassUnits.KiloGram / TimeUnits.Minute, "MassFlow");
        public static readonly UnitMeasure Kg_sg = new UnitMeasure("Kg/sg", "Kg/sg",
        MassUnits.KiloGram / TimeUnits.Second, "MassFlow");
        public static readonly UnitMeasure Kg_hr = new UnitMeasure("Kg/hr", "Kg/hr",
         MassUnits.KiloGram / TimeUnits.Hour, "MassFlow");
        public static readonly UnitMeasure Kg_day = new UnitMeasure("Kg/day", "Kg/day",
         MassUnits.KiloGram / TimeUnits.Day, "MassFlow");
        public static readonly UnitMeasure Kg_Month = new UnitMeasure("Kg/month", "Kg/mo",
         MassUnits.KiloGram / TimeUnits.Month, "MassFlow");
        public static readonly UnitMeasure Kg_Year = new UnitMeasure("Kg/Year", "Kg/yr",
         MassUnits.KiloGram / TimeUnits.Year, "MassFlow");

        public static readonly UnitMeasure g_min = new UnitMeasure("g/min", "Kg/min",
        MassUnits.Gram / TimeUnits.Minute, "MassFlow");
        public static readonly UnitMeasure g_sg = new UnitMeasure("g/sg", "Kg/sg",
        MassUnits.Gram / TimeUnits.Second, "MassFlow");
        public static readonly UnitMeasure g_hr = new UnitMeasure("g/hr", "Kg/hr",
         MassUnits.Gram / TimeUnits.Hour, "MassFlow");
        public static readonly UnitMeasure g_day = new UnitMeasure("g/day", "Kg/day",
         MassUnits.Gram / TimeUnits.Day, "MassFlow");
        public static readonly UnitMeasure g_Month = new UnitMeasure("g/month", "Kg/mo",
         MassUnits.Gram / TimeUnits.Month, "MassFlow");
        public static readonly UnitMeasure g_Year = new UnitMeasure("g/Year", "Kg/yr",
         MassUnits.Gram / TimeUnits.Year, "MassFlow");

        public static readonly UnitMeasure Ton_min = new UnitMeasure("Ton/min", "Ton/min",
        MassUnits.KiloGram / TimeUnits.Minute, "MassFlow");
        public static readonly UnitMeasure Ton_sg = new UnitMeasure("Ton/sg", "Ton/sg",
        MassUnits.KiloGram / TimeUnits.Second, "MassFlow");
        public static readonly UnitMeasure Ton_hr = new UnitMeasure("Ton/hr", "Ton/hr",
         MassUnits.Ton / TimeUnits.Hour, "MassFlow");
        public static readonly UnitMeasure Ton_day = new UnitMeasure("Ton/day", "Ton/day",
         MassUnits.Ton / TimeUnits.Day, "MassFlow");
        public static readonly UnitMeasure Ton_Month = new UnitMeasure("Ton/month", "Ton/mo",
         MassUnits.Ton / TimeUnits.Hour, "MassFlow");
        public static readonly UnitMeasure Ton_Year = new UnitMeasure("Ton/Year", "Ton/yr",
         MassUnits.Ton / TimeUnits.Year, "MassFlow");


        public static readonly UnitMeasure lb_min = new UnitMeasure("lb/min", "lb/min",
        MassUnits.Pound / TimeUnits.Minute, "MassFlow");
        public static readonly UnitMeasure lb_sg = new UnitMeasure("lb/sg", "lb/sg",
        MassUnits.Pound / TimeUnits.Second, "MassFlow");

        public static readonly UnitMeasure lb_hr = new UnitMeasure("lb/hr", "lb/hr",
         MassUnits.Pound / TimeUnits.Hour, "MassFlow");
        public static readonly UnitMeasure lb_day = new UnitMeasure("lb/day", "lb/day",
         MassUnits.Pound / TimeUnits.Day, "MassFlow");
        public static readonly UnitMeasure lb_Month = new UnitMeasure("lb/month", "lb/mo",
         MassUnits.Pound / TimeUnits.Hour, "MassFlow");
        public static readonly UnitMeasure lb_Year = new UnitMeasure("lb/Year", "lb/yr",
         MassUnits.Pound / TimeUnits.Year, "MassFlow");
    }
    [UnitDefinitionClass]
    public static class MolarFlowUnits
    {
        public static readonly UnitMeasure Kgmol_min = new UnitMeasure("Kg-mol/min", "Kg-mol/min",
        AmountOfSubstanceUnits.KMole / TimeUnits.Minute, "MolarFlow");
        public static readonly UnitMeasure Kgmol_sg = new UnitMeasure("Kg-mol/sg", "Kg-mol/sg",
       AmountOfSubstanceUnits.KMole / TimeUnits.Second, "MolarFlow");
        public static readonly UnitMeasure Kgmol_hr = new UnitMeasure("Kg-mol/hr", "Kg-mol/hr",
        AmountOfSubstanceUnits.KMole / TimeUnits.Hour, "MolarFlow");

        public static readonly UnitMeasure gmol_min = new UnitMeasure("g-mol/min", "g-mol/min",
       AmountOfSubstanceUnits.Mole / TimeUnits.Minute, "MolarFlow");
        public static readonly UnitMeasure gmol_sg = new UnitMeasure("g-mol/sg", "g-mol/sg",
       AmountOfSubstanceUnits.Mole / TimeUnits.Second, "MolarFlow");
        public static readonly UnitMeasure gmol_hr = new UnitMeasure("g-mol/hr", "g-mol/hr",
        AmountOfSubstanceUnits.Mole / TimeUnits.Hour, "MolarFlow");
    }
    [UnitDefinitionClass]
    public static class HeatSurfaceFlowUnits
    {
        public static readonly UnitMeasure W_m2 = new UnitMeasure("W/m2", "W/m2",
           PowerUnits.Watt / SurfaceUnits.Meter2, "HeatSurfaceFlow");
        public static readonly UnitMeasure KW_m2 = new UnitMeasure("KW/m2", "KW/m2",
           PowerUnits.KiloWatt / SurfaceUnits.Meter2, "HeatSurfaceFlow");
        public static readonly UnitMeasure BTU_hr_ft2 = new UnitMeasure("BTU/(hr*ft2)", "BTU/(hr*ft2)",
           EnergyUnits.BTU / TimeUnits.Hour / SurfaceUnits.Foot2, "HeatSurfaceFlow");

    }
    [UnitDefinitionClass]
    public static class VolumetricFlowUnits
    {
        public static readonly UnitMeasure m3_min = new UnitMeasure("m3/min", "m3/min",
        VolumeUnits.Meter3 / TimeUnits.Minute, "VolumetricFlow");
        public static readonly UnitMeasure m3_sg = new UnitMeasure("m3/sg", "m3/sg",
        VolumeUnits.Meter3 / TimeUnits.Second, "VolumetricFlow");
        public static readonly UnitMeasure m3_hr = new UnitMeasure("m3/hr", "m3/hr",
         VolumeUnits.Meter3 / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure m3_day = new UnitMeasure("m3/day", "m3/day",
         VolumeUnits.Meter3 / TimeUnits.Day, "VolumetricFlow");
        public static readonly UnitMeasure m3_Month = new UnitMeasure("m3/month", "m3/mo",
         VolumeUnits.Meter3 / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure m3_Year = new UnitMeasure("m3/Year", "m3/yr",
         VolumeUnits.Meter3 / TimeUnits.Year, "VolumetricFlow");

        public static readonly UnitMeasure Lt_min = new UnitMeasure("Lt/min", "Lt/min",
        VolumeUnits.Liter / TimeUnits.Minute, "VolumetricFlow");
        public static readonly UnitMeasure Lt_sg = new UnitMeasure("Lt/sg", "Lt/sg",
        VolumeUnits.Liter / TimeUnits.Second, "VolumetricFlow");
        public static readonly UnitMeasure Lt_hr = new UnitMeasure("Lt/hr", "Lt/hr",
         VolumeUnits.Liter / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure Lt_day = new UnitMeasure("Lt/day", "Lt/day",
         VolumeUnits.Liter / TimeUnits.Day, "VolumetricFlow");
        public static readonly UnitMeasure Lt_Month = new UnitMeasure("Lt/month", "Lt/mo",
         VolumeUnits.Liter / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure Lt_Year = new UnitMeasure("Lt/Year", "Lt/yr",
         VolumeUnits.Liter / TimeUnits.Year, "VolumetricFlow");

        public static readonly UnitMeasure gal_min = new UnitMeasure("gal/min", "gal/min",
        VolumeUnits.Galon / TimeUnits.Minute, "VolumetricFlow");
        public static readonly UnitMeasure gal_sg = new UnitMeasure("gal/sg", "gal/sg",
        VolumeUnits.Galon / TimeUnits.Second, "VolumetricFlow");
        public static readonly UnitMeasure gal_hr = new UnitMeasure("gal/hr", "gal/hr",
         VolumeUnits.Galon / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure gal_day = new UnitMeasure("gal/day", "gal/day",
         VolumeUnits.Galon / TimeUnits.Day, "VolumetricFlow");
        public static readonly UnitMeasure gal_Month = new UnitMeasure("gal/month", "gal/mo",
         VolumeUnits.Galon / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure gal_Year = new UnitMeasure("gal/Year", "gal/yr",
         VolumeUnits.Galon / TimeUnits.Year, "VolumetricFlow");

        public static readonly UnitMeasure ft3_min = new UnitMeasure("ft3/min", "ft3/min",
        VolumeUnits.Foot3 / TimeUnits.Minute, "VolumetricFlow");
        public static readonly UnitMeasure ft3_sg = new UnitMeasure("ft3/sg", "ft3/sg",
        VolumeUnits.Foot3 / TimeUnits.Second, "VolumetricFlow");
        public static readonly UnitMeasure ft3_hr = new UnitMeasure("ft3/hr", "ft3/hr",
         VolumeUnits.Foot3 / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure ft3_day = new UnitMeasure("ft3/day", "ft3/day",
         VolumeUnits.Foot3 / TimeUnits.Day, "VolumetricFlow");
        public static readonly UnitMeasure ft3_Month = new UnitMeasure("ft3/month", "ft3/mo",
         VolumeUnits.Foot3 / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure ft3_Year = new UnitMeasure("ft3/Year", "ft3/yr",
         VolumeUnits.Foot3 / TimeUnits.Year, "VolumetricFlow");

        public static readonly UnitMeasure barrel_min = new UnitMeasure("barrel/min", "barrel/min",
        VolumeUnits.Barrel / TimeUnits.Minute, "VolumetricFlow");
        public static readonly UnitMeasure barrel_sg = new UnitMeasure("barrel/sg", "barrel/sg",
        VolumeUnits.Barrel / TimeUnits.Second, "VolumetricFlow");
        public static readonly UnitMeasure barrel_hr = new UnitMeasure("barrel/hr", "barrel/hr",
         VolumeUnits.Barrel / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure barrel_day = new UnitMeasure("barrel/day", "barrel/day",
         VolumeUnits.Barrel / TimeUnits.Day, "VolumetricFlow");
        public static readonly UnitMeasure barrel_Month = new UnitMeasure("barrel/month", "barrel/mo",
         VolumeUnits.Barrel / TimeUnits.Hour, "VolumetricFlow");
        public static readonly UnitMeasure barrel_Year = new UnitMeasure("barrel/Year", "barrel/yr",
         VolumeUnits.Barrel / TimeUnits.Year, "VolumetricFlow");

    }
    [UnitDefinitionClass]
    public static class EnergyFlowUnits
    {
        public static readonly UnitMeasure J_min = new UnitMeasure("J/min", "J/min",
       EnergyUnits.Joule / TimeUnits.Minute, "EnergyFlow");
        public static readonly UnitMeasure J_sg = new UnitMeasure("J/sg", "J/sg",
        EnergyUnits.Joule / TimeUnits.Second, "EnergyFlow");
        public static readonly UnitMeasure J_hr = new UnitMeasure("J/hr", "J/hr",
         EnergyUnits.Joule / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure J_day = new UnitMeasure("J/day", "J/day",
         EnergyUnits.Joule / TimeUnits.Day, "EnergyFlow");
        public static readonly UnitMeasure J_Month = new UnitMeasure("J/month", "J/mo",
         EnergyUnits.Joule / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure J_Year = new UnitMeasure("J/Year", "J/yr",
         EnergyUnits.Joule / TimeUnits.Year, "EnergyFlow");

        public static readonly UnitMeasure KJ_min = new UnitMeasure("KJ/min", "KJ/min",
       EnergyUnits.KiloJoule / TimeUnits.Minute, "EnergyFlow");
        public static readonly UnitMeasure KJ_sg = new UnitMeasure("KJ/sg", "KJ/sg",
        EnergyUnits.KiloJoule / TimeUnits.Second, "EnergyFlow");
        public static readonly UnitMeasure KJ_hr = new UnitMeasure("KJ/hr", "KJ/hr",
         EnergyUnits.KiloJoule / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure KJ_day = new UnitMeasure("KJ/day", "KJ/day",
         EnergyUnits.KiloJoule / TimeUnits.Day, "EnergyFlow");
        public static readonly UnitMeasure KJ_Month = new UnitMeasure("KJ/month", "KJ/mo",
         EnergyUnits.KiloJoule / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure KJ_Year = new UnitMeasure("KJ/Year", "KJ/yr",
         EnergyUnits.KiloJoule / TimeUnits.Year, "EnergyFlow");

        public static readonly UnitMeasure MJ_min = new UnitMeasure("MJ/min", "MJ/min",
       EnergyUnits.MegaJoule / TimeUnits.Minute, "EnergyFlow");
        public static readonly UnitMeasure MJ_sg = new UnitMeasure("MJ/sg", "MJ/sg",
        EnergyUnits.MegaJoule / TimeUnits.Second, "EnergyFlow");
        public static readonly UnitMeasure MJ_hr = new UnitMeasure("MJ/hr", "MJ/hr",
         EnergyUnits.MegaJoule / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure MJ_day = new UnitMeasure("MJ/day", "MJ/day",
         EnergyUnits.MegaJoule / TimeUnits.Day, "EnergyFlow");
        public static readonly UnitMeasure MJ_Month = new UnitMeasure("MJ/month", "MJ/mo",
         EnergyUnits.MegaJoule / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure MJ_Year = new UnitMeasure("MJ/Year", "MJ/yr",
         EnergyUnits.MegaJoule / TimeUnits.Year, "EnergyFlow");

        public static readonly UnitMeasure cal_min = new UnitMeasure("cal/min", "cal/min",
       EnergyUnits.Calorie / TimeUnits.Minute, "EnergyFlow");
        public static readonly UnitMeasure cal_sg = new UnitMeasure("cal/sg", "cal/sg",
        EnergyUnits.Calorie / TimeUnits.Second, "EnergyFlow");
        public static readonly UnitMeasure cal_hr = new UnitMeasure("cal/hr", "cal/hr",
         EnergyUnits.Calorie / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure cal_day = new UnitMeasure("cal/day", "cal/day",
         EnergyUnits.Calorie / TimeUnits.Day, "EnergyFlow");
        public static readonly UnitMeasure cal_Month = new UnitMeasure("cal/month", "cal/mo",
         EnergyUnits.Calorie / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure cal_Year = new UnitMeasure("cal/Year", "cal/yr",
         EnergyUnits.Calorie / TimeUnits.Year, "EnergyFlow");

        public static readonly UnitMeasure Kcal_min = new UnitMeasure("Kcal/min", "Kcal/min",
       EnergyUnits.KiloCalorie / TimeUnits.Minute, "EnergyFlow");
        public static readonly UnitMeasure Kcal_sg = new UnitMeasure("Kcal/sg", "Kcal/sg",
        EnergyUnits.KiloCalorie / TimeUnits.Second, "EnergyFlow");
        public static readonly UnitMeasure Kcal_hr = new UnitMeasure("Kcal/hr", "Kcal/hr",
         EnergyUnits.KiloCalorie / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure Kcal_day = new UnitMeasure("Kcal/day", "Kcal/day",
         EnergyUnits.KiloCalorie / TimeUnits.Day, "EnergyFlow");
        public static readonly UnitMeasure Kcal_Month = new UnitMeasure("Kcal/month", "Kcal/mo",
         EnergyUnits.KiloCalorie / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure Kcal_Year = new UnitMeasure("Kcal/Year", "Kcal/yr",
         EnergyUnits.KiloCalorie / TimeUnits.Year, "EnergyFlow");

        public static readonly UnitMeasure BTUmin = new UnitMeasure("BTU/min", "BTU/min",
       EnergyUnits.BTU / TimeUnits.Minute, "EnergyFlow");
        public static readonly UnitMeasure BTUsg = new UnitMeasure("BTU/sg", "BTU/sg",
        EnergyUnits.BTU / TimeUnits.Second, "EnergyFlow");
        public static readonly UnitMeasure BTUhr = new UnitMeasure("BTU/hr", "BTU/hr",
         EnergyUnits.BTU / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure BTUday = new UnitMeasure("BTU/day", "BTU/day",
         EnergyUnits.BTU / TimeUnits.Day, "EnergyFlow");
        public static readonly UnitMeasure BTUMonth = new UnitMeasure("BTU/month", "BTU/mo",
         EnergyUnits.BTU / TimeUnits.Hour, "EnergyFlow");
        public static readonly UnitMeasure BTUYear = new UnitMeasure("BTU/Year", "BTU/yr",
         EnergyUnits.BTU / TimeUnits.Year, "EnergyFlow");


    }
    [UnitDefinitionClass]
    public static class ViscosityUnits
    {
        public static readonly UnitMeasure Pa_s = new UnitMeasure("Pascal*second", "Pa*sg",
         PressureUnits.Pascala * TimeUnits.Second, "Viscosity");

        public static readonly UnitMeasure Poise = new UnitMeasure("Poise", "ps",
        Pa_s * 0.1, "Viscosity");

        public static readonly UnitMeasure cPoise = new UnitMeasure("cPoise", "cps",
        Poise * 0.01, "Viscosity");

        public static readonly UnitMeasure Kg_m_s = new UnitMeasure("Kg*m*seg", "Kg*m*seg",
        Pa_s, "Viscosity");

        public static readonly UnitMeasure Nt_seg_m2 = new UnitMeasure("Newton*seg/m2", "Newton*seg/m2",
       Pa_s, "Viscosity");

        public static readonly UnitMeasure lb_ft_sg = new UnitMeasure("lb*ft*seg", "lb*ft*seg",
       Pa_s * 1.4882, "Viscosity");

        public static readonly UnitMeasure lb_ft_hr = new UnitMeasure("lb*ft*hr", "lb*ft*hr",
       Pa_s * 0.00041338, "Viscosity");

    }
    [UnitDefinitionClass]
    public static class SuperficialTensionUnits
    {
        // 1. SISTEMA INTERNACIONAL (SI) - Unidad Base: Newton / metro
        public static readonly UnitMeasure N_m = new UnitMeasure("N/m", "N/m",
            ForceUnits.Newton / LengthUnits.Meter, "SuperficialTension");

        // 2. SISTEMA CGS - Dina / centímetro (Común en DIPPR)
        // 1 dyn/cm = 0.001 N/m
        public static readonly UnitMeasure dyn_cm = new UnitMeasure("dyn/cm", "dyn/cm",
            N_m / 1000.0, "SuperficialTension");

        // 3. SISTEMA INGLÉS - Libra-fuerza / pie
        // 1 lbf/ft = 14.5939 N/m
        public static readonly UnitMeasure lbf_ft = new UnitMeasure("lbf/ft", "lbf/ft",
            N_m * 14.5939029, "SuperficialTension");
    }
}

