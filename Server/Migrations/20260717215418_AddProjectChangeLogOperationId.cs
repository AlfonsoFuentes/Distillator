using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectChangeLogOperationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OperationId",
                table: "ProjectChangeLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChangeLogs_TenantId_ProjectId_UserId_OperationId",
                table: "ProjectChangeLogs",
                columns: new[] { "TenantId", "ProjectId", "UserId", "OperationId" },
                filter: "\"OperationId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectChangeLogs_TenantId_ProjectId_UserId_OperationId",
                table: "ProjectChangeLogs");

            migrationBuilder.DropColumn(
                name: "OperationId",
                table: "ProjectChangeLogs");
        }
    }
}
