using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimActorTypeToRcaIncident : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaimActorType",
                table: "rca_incidents",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "InternalArea")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_rca_incidents_TenantId_ClaimActorType_ClaimOwnerName",
                table: "rca_incidents",
                columns: new[] { "TenantId", "ClaimActorType", "ClaimOwnerName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rca_incidents_TenantId_ClaimActorType_ClaimOwnerName",
                table: "rca_incidents");

            migrationBuilder.DropColumn(
                name: "ClaimActorType",
                table: "rca_incidents");
        }
    }
}
