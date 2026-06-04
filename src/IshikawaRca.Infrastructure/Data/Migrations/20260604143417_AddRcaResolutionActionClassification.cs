using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRcaResolutionActionClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "corrective_actions",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Corrective")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResolutionScope",
                table: "corrective_actions",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "RootCause")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_corrective_actions_TenantId_RcaIncidentId_ResolutionScope_Ac~",
                table: "corrective_actions",
                columns: new[] { "TenantId", "RcaIncidentId", "ResolutionScope", "ActionType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_corrective_actions_TenantId_RcaIncidentId_ResolutionScope_Ac~",
                table: "corrective_actions");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "corrective_actions");

            migrationBuilder.DropColumn(
                name: "ResolutionScope",
                table: "corrective_actions");
        }
    }
}
