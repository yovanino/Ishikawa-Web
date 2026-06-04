using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRcaFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rca_facts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false, collation: "ascii_general_ci")
                        .Annotation("MySql:CharSet", "ascii"),
                    RcaIncidentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CauseId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    EvidenceId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ExternalIntakeId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    FactType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceDetail = table.Column<string>(type: "varchar(220)", maxLength: 220, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(220)", maxLength: 220, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CapturedByUserId = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false, collation: "ascii_general_ci")
                        .Annotation("MySql:CharSet", "ascii"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rca_facts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rca_facts_rca_incidents_RcaIncidentId",
                        column: x => x.RcaIncidentId,
                        principalTable: "rca_incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_RcaIncidentId",
                table: "rca_facts",
                column: "RcaIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_CauseId",
                table: "rca_facts",
                columns: new[] { "TenantId", "CauseId" });

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_EvidenceId",
                table: "rca_facts",
                columns: new[] { "TenantId", "EvidenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_ExternalIntakeId",
                table: "rca_facts",
                columns: new[] { "TenantId", "ExternalIntakeId" });

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_FactType",
                table: "rca_facts",
                columns: new[] { "TenantId", "FactType" });

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_IsDeleted",
                table: "rca_facts",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_RcaIncidentId_OccurredAt",
                table: "rca_facts",
                columns: new[] { "TenantId", "RcaIncidentId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rca_facts");
        }
    }
}
