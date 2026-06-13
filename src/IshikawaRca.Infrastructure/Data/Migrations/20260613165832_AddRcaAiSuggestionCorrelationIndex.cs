using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRcaAiSuggestionCorrelationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_rca_ai_suggestions_TenantId_GatewayCorrelationId",
                table: "rca_ai_suggestions",
                columns: new[] { "TenantId", "GatewayCorrelationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rca_ai_suggestions_TenantId_GatewayCorrelationId",
                table: "rca_ai_suggestions");
        }
    }
}
