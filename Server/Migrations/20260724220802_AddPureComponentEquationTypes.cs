using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPureComponentEquationTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GasEnthalpyEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "IntegratedGasCpWithHvap");

            migrationBuilder.AddColumn<string>(
                name: "GasHeatCapacityEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "AlyLee");

            migrationBuilder.AddColumn<string>(
                name: "GasThermalConductivityEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "PolynomialRational");

            migrationBuilder.AddColumn<string>(
                name: "GasViscosityEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Dippr102");

            migrationBuilder.AddColumn<string>(
                name: "HeatOfVaporizationEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Dippr106");

            migrationBuilder.AddColumn<string>(
                name: "LiquidDensityEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Rackett");

            migrationBuilder.AddColumn<string>(
                name: "LiquidEnthalpyEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "IntegratedLiquidCp");

            migrationBuilder.AddColumn<string>(
                name: "LiquidHeatCapacityEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Polynomial");

            migrationBuilder.AddColumn<string>(
                name: "LiquidThermalConductivityEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Polynomial4");

            migrationBuilder.AddColumn<string>(
                name: "LiquidViscosityEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Dippr101");

            migrationBuilder.AddColumn<string>(
                name: "SaturatedMolarVolumeEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Rackett");

            migrationBuilder.AddColumn<string>(
                name: "SaturationTemperatureEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "FromVaporPressureSecant");

            migrationBuilder.AddColumn<string>(
                name: "SurfaceTensionEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Dippr106");

            migrationBuilder.AddColumn<string>(
                name: "VaporPressureEquationType",
                table: "ChemicalComponents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "ExtendedAntoine");

            migrationBuilder.Sql("""
                UPDATE "ChemicalComponents"
                SET
                    "GasEnthalpyEquationType" = 'IapwsSteamTables',
                    "GasHeatCapacityEquationType" = 'IapwsSteamTables',
                    "GasThermalConductivityEquationType" = 'IapwsSteamTables',
                    "GasViscosityEquationType" = 'IapwsSteamTables',
                    "HeatOfVaporizationEquationType" = 'IapwsSteamTables',
                    "LiquidDensityEquationType" = 'IapwsSteamTables',
                    "LiquidEnthalpyEquationType" = 'IapwsSteamTables',
                    "LiquidHeatCapacityEquationType" = 'IapwsSteamTables',
                    "LiquidThermalConductivityEquationType" = 'IapwsSteamTables',
                    "LiquidViscosityEquationType" = 'IapwsSteamTables',
                    "SaturatedMolarVolumeEquationType" = 'IapwsSteamTables',
                    "SaturationTemperatureEquationType" = 'IapwsSteamTables',
                    "SurfaceTensionEquationType" = 'IapwsSteamTables',
                    "VaporPressureEquationType" = 'IapwsSteamTables'
                WHERE "Name" IN ('Water', 'Agua');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GasEnthalpyEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "GasHeatCapacityEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "GasThermalConductivityEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "GasViscosityEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "HeatOfVaporizationEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "LiquidDensityEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "LiquidEnthalpyEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "LiquidHeatCapacityEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "LiquidThermalConductivityEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "LiquidViscosityEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "SaturatedMolarVolumeEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "SaturationTemperatureEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "SurfaceTensionEquationType",
                table: "ChemicalComponents");

            migrationBuilder.DropColumn(
                name: "VaporPressureEquationType",
                table: "ChemicalComponents");
        }
    }
}
