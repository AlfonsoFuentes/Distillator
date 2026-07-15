using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectUserWorkspaceState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectUserWorkspaceStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    LastProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastFlowsheetId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsProjectExplorerCollapsed = table.Column<bool>(type: "boolean", nullable: false),
                    IsDiagramExplorerCollapsed = table.Column<bool>(type: "boolean", nullable: false),
                    ExpandedDiagramTypeCodesJson = table.Column<string>(type: "jsonb", nullable: true),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUserWorkspaceStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserWorkspaceStates_TenantId_UserId",
                table: "ProjectUserWorkspaceStates",
                columns: new[] { "TenantId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectUserWorkspaceStates");
        }
    }
}
