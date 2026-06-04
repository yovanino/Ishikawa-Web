using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRcaFactCorrectiveActionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CorrectiveActionId",
                table: "rca_facts",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_CorrectiveActionId",
                table: "rca_facts",
                columns: new[] { "TenantId", "CorrectiveActionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rca_facts_TenantId_CorrectiveActionId",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionId",
                table: "rca_facts");
        }
    }
}
