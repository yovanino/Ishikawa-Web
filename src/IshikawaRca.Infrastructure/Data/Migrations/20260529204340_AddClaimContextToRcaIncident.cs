using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimContextToRcaIncident : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaimOwnerName",
                table: "rca_incidents",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ClaimScope",
                table: "rca_incidents",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Internal")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_rca_incidents_TenantId_ClaimScope_ClaimOwnerName",
                table: "rca_incidents",
                columns: new[] { "TenantId", "ClaimScope", "ClaimOwnerName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rca_incidents_TenantId_ClaimScope_ClaimOwnerName",
                table: "rca_incidents");

            migrationBuilder.DropColumn(
                name: "ClaimOwnerName",
                table: "rca_incidents");

            migrationBuilder.DropColumn(
                name: "ClaimScope",
                table: "rca_incidents");
        }
    }
}
