namespace Shared.PropertiesDtos.Components
{
    public enum VaporPressureEquationType
    {
        ExtendedAntoine,
        IapwsSteamTables
    }

    public enum SaturationTemperatureEquationType
    {
        FromVaporPressureSecant,
        IapwsSteamTables
    }

    public enum HeatOfVaporizationEquationType
    {
        Dippr106,
        IapwsSteamTables
    }

    public enum LiquidHeatCapacityEquationType
    {
        Polynomial,
        IapwsSteamTables
    }

    public enum GasHeatCapacityEquationType
    {
        AlyLee,
        IapwsSteamTables
    }

    public enum LiquidViscosityEquationType
    {
        Dippr101,
        IapwsSteamTables
    }

    public enum GasViscosityEquationType
    {
        Dippr102,
        IapwsSteamTables
    }

    public enum LiquidThermalConductivityEquationType
    {
        Polynomial4,
        Ppds8,
        IapwsSteamTables
    }

    public enum GasThermalConductivityEquationType
    {
        PolynomialRational,
        IapwsSteamTables
    }

    public enum LiquidDensityEquationType
    {
        Rackett,
        IapwsSteamTables
    }

    public enum SurfaceTensionEquationType
    {
        Dippr106,
        IapwsSteamTables
    }

    public enum LiquidEnthalpyEquationType
    {
        IntegratedLiquidCp,
        IapwsSteamTables
    }

    public enum GasEnthalpyEquationType
    {
        IntegratedGasCpWithHvap,
        IapwsSteamTables
    }

    public enum SaturatedMolarVolumeEquationType
    {
        Rackett,
        IapwsSteamTables
    }
}
