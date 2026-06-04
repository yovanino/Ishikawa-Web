using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRcaEvidenceValidationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceDetail",
                table: "rca_evidence",
                type: "varchar(220)",
                maxLength: 220,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "rca_evidence",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidatedAt",
                table: "rca_evidence",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidatedByUserId",
                table: "rca_evidence",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ValidationNotes",
                table: "rca_evidence",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ValidationStatus",
                table: "rca_evidence",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PendingReview")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_rca_evidence_TenantId_ValidationStatus",
                table: "rca_evidence",
                columns: new[] { "TenantId", "ValidationStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rca_evidence_TenantId_ValidationStatus",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "SourceDetail",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "ValidatedAt",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "ValidatedByUserId",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "ValidationNotes",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "ValidationStatus",
                table: "rca_evidence");
        }
    }
}
