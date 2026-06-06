using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRcaFactExternalCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalEventId",
                table: "rca_facts",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExternalRecordUri",
                table: "rca_facts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceSystem",
                table: "rca_facts",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_RcaIncidentId_ExternalSourceSystem_Extern~",
                table: "rca_facts",
                columns: new[] { "TenantId", "RcaIncidentId", "ExternalSourceSystem", "ExternalEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rca_facts_TenantId_RcaIncidentId_ExternalSourceSystem_Extern~",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "ExternalEventId",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "ExternalRecordUri",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "ExternalSourceSystem",
                table: "rca_facts");
        }
    }
}
