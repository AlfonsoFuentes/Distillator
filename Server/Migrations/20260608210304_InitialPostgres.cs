using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChemicalComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Formula = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StructuralFormula = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SecondaryFamily = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MolecularWeight = table.Column<double>(type: "double precision", nullable: false),
                    CriticalTemperature_Value = table.Column<double>(type: "double precision", nullable: false),
                    CriticalTemperature_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BoilingPoint_Value = table.Column<double>(type: "double precision", nullable: false),
                    BoilingPoint_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MeltingPoint_Value = table.Column<double>(type: "double precision", nullable: false),
                    MeltingPoint_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CriticalPressure_Value = table.Column<double>(type: "double precision", nullable: false),
                    CriticalPressure_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CriticalVolume_Value = table.Column<double>(type: "double precision", nullable: false),
                    CriticalVolume_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VolumeAsterisk_Value = table.Column<double>(type: "double precision", nullable: false),
                    VolumeAsterisk_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnthalpyForm_Value = table.Column<double>(type: "double precision", nullable: false),
                    EnthalpyForm_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GibbsForm_Value = table.Column<double>(type: "double precision", nullable: false),
                    GibbsForm_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntropyForm_Value = table.Column<double>(type: "double precision", nullable: false),
                    EntropyForm_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CombustionEnthalpy_Value = table.Column<double>(type: "double precision", nullable: false),
                    CombustionEnthalpy_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CriticalZ = table.Column<double>(type: "double precision", nullable: false),
                    AcentricFactor = table.Column<double>(type: "double precision", nullable: false),
                    AcentricFactorPitzer = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_C1 = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_C2 = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_C3 = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_C4 = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_C5 = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_C6 = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_C7 = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VaporPressure_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    VaporPressure_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HeatOfVaporization_C1 = table.Column<double>(type: "double precision", nullable: false),
                    HeatOfVaporization_C2 = table.Column<double>(type: "double precision", nullable: false),
                    HeatOfVaporization_C3 = table.Column<double>(type: "double precision", nullable: false),
                    HeatOfVaporization_C4 = table.Column<double>(type: "double precision", nullable: false),
                    HeatOfVaporization_C5 = table.Column<double>(type: "double precision", nullable: false),
                    HeatOfVaporization_C6 = table.Column<double>(type: "double precision", nullable: false),
                    HeatOfVaporization_C7 = table.Column<double>(type: "double precision", nullable: false),
                    HeatOfVaporization_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    HeatOfVaporization_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HeatOfVaporization_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    HeatOfVaporization_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LiquidHeatCapacity_C1 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidHeatCapacity_C2 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidHeatCapacity_C3 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidHeatCapacity_C4 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidHeatCapacity_C5 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidHeatCapacity_C6 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidHeatCapacity_C7 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidHeatCapacity_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    LiquidHeatCapacity_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LiquidHeatCapacity_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    LiquidHeatCapacity_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GasHeatCapacity_C1 = table.Column<double>(type: "double precision", nullable: false),
                    GasHeatCapacity_C2 = table.Column<double>(type: "double precision", nullable: false),
                    GasHeatCapacity_C3 = table.Column<double>(type: "double precision", nullable: false),
                    GasHeatCapacity_C4 = table.Column<double>(type: "double precision", nullable: false),
                    GasHeatCapacity_C5 = table.Column<double>(type: "double precision", nullable: false),
                    GasHeatCapacity_C6 = table.Column<double>(type: "double precision", nullable: false),
                    GasHeatCapacity_C7 = table.Column<double>(type: "double precision", nullable: false),
                    GasHeatCapacity_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    GasHeatCapacity_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GasHeatCapacity_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    GasHeatCapacity_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LiquidViscosity_C1 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidViscosity_C2 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidViscosity_C3 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidViscosity_C4 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidViscosity_C5 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidViscosity_C6 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidViscosity_C7 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidViscosity_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    LiquidViscosity_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LiquidViscosity_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    LiquidViscosity_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GasViscosity_C1 = table.Column<double>(type: "double precision", nullable: false),
                    GasViscosity_C2 = table.Column<double>(type: "double precision", nullable: false),
                    GasViscosity_C3 = table.Column<double>(type: "double precision", nullable: false),
                    GasViscosity_C4 = table.Column<double>(type: "double precision", nullable: false),
                    GasViscosity_C5 = table.Column<double>(type: "double precision", nullable: false),
                    GasViscosity_C6 = table.Column<double>(type: "double precision", nullable: false),
                    GasViscosity_C7 = table.Column<double>(type: "double precision", nullable: false),
                    GasViscosity_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    GasViscosity_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GasViscosity_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    GasViscosity_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LiquidThermalCond_C1 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidThermalCond_C2 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidThermalCond_C3 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidThermalCond_C4 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidThermalCond_C5 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidThermalCond_C6 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidThermalCond_C7 = table.Column<double>(type: "double precision", nullable: false),
                    LiquidThermalCond_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    LiquidThermalCond_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LiquidThermalCond_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    LiquidThermalCond_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GasThermalCond_C1 = table.Column<double>(type: "double precision", nullable: false),
                    GasThermalCond_C2 = table.Column<double>(type: "double precision", nullable: false),
                    GasThermalCond_C3 = table.Column<double>(type: "double precision", nullable: false),
                    GasThermalCond_C4 = table.Column<double>(type: "double precision", nullable: false),
                    GasThermalCond_C5 = table.Column<double>(type: "double precision", nullable: false),
                    GasThermalCond_C6 = table.Column<double>(type: "double precision", nullable: false),
                    GasThermalCond_C7 = table.Column<double>(type: "double precision", nullable: false),
                    GasThermalCond_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    GasThermalCond_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GasThermalCond_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    GasThermalCond_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Density_C1 = table.Column<double>(type: "double precision", nullable: false),
                    Density_C2 = table.Column<double>(type: "double precision", nullable: false),
                    Density_C3 = table.Column<double>(type: "double precision", nullable: false),
                    Density_C4 = table.Column<double>(type: "double precision", nullable: false),
                    Density_C5 = table.Column<double>(type: "double precision", nullable: false),
                    Density_C6 = table.Column<double>(type: "double precision", nullable: false),
                    Density_C7 = table.Column<double>(type: "double precision", nullable: false),
                    Density_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    Density_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Density_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    Density_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SurfaceTension_C1 = table.Column<double>(type: "double precision", nullable: false),
                    SurfaceTension_C2 = table.Column<double>(type: "double precision", nullable: false),
                    SurfaceTension_C3 = table.Column<double>(type: "double precision", nullable: false),
                    SurfaceTension_C4 = table.Column<double>(type: "double precision", nullable: false),
                    SurfaceTension_C5 = table.Column<double>(type: "double precision", nullable: false),
                    SurfaceTension_C6 = table.Column<double>(type: "double precision", nullable: false),
                    SurfaceTension_C7 = table.Column<double>(type: "double precision", nullable: false),
                    SurfaceTension_Tmin_Value = table.Column<double>(type: "double precision", nullable: false),
                    SurfaceTension_Tmin_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SurfaceTension_Tmax_Value = table.Column<double>(type: "double precision", nullable: false),
                    SurfaceTension_Tmax_Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalComponents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThermodynamicMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VaporModel = table.Column<int>(type: "integer", nullable: false),
                    LiquidModel = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThermodynamicMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BinaryInteractionParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentI_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentJ_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinaryInteractionParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinaryInteractionParameters_ChemicalComponents_ComponentI_Id",
                        column: x => x.ComponentI_Id,
                        principalTable: "ChemicalComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BinaryInteractionParameters_ChemicalComponents_ComponentJ_Id",
                        column: x => x.ComponentJ_Id,
                        principalTable: "ChemicalComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BinaryInteractionParameters_ThermodynamicMethods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "ThermodynamicMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatrixIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodComponents_ChemicalComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "ChemicalComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MethodComponents_ThermodynamicMethods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "ThermodynamicMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BinaryInteractionParameters_ComponentI_Id",
                table: "BinaryInteractionParameters",
                column: "ComponentI_Id");

            migrationBuilder.CreateIndex(
                name: "IX_BinaryInteractionParameters_ComponentJ_Id",
                table: "BinaryInteractionParameters",
                column: "ComponentJ_Id");

            migrationBuilder.CreateIndex(
                name: "IX_BinaryInteractionParameters_MethodId",
                table: "BinaryInteractionParameters",
                column: "MethodId");

            migrationBuilder.CreateIndex(
                name: "IX_MethodComponents_ComponentId",
                table: "MethodComponents",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_MethodComponents_MethodId_ComponentId",
                table: "MethodComponents",
                columns: new[] { "MethodId", "ComponentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BinaryInteractionParameters");

            migrationBuilder.DropTable(
                name: "MethodComponents");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "ChemicalComponents");

            migrationBuilder.DropTable(
                name: "ThermodynamicMethods");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
