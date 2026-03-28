using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddThermoMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThermodynamicMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VaporModel = table.Column<int>(type: "int", nullable: false),
                    LiquidModel = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThermodynamicMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BinaryInteractionParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentI_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentJ_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterType = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatrixIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BinaryInteractionParameters");

            migrationBuilder.DropTable(
                name: "MethodComponents");

            migrationBuilder.DropTable(
                name: "ThermodynamicMethods");
        }
    }
}
