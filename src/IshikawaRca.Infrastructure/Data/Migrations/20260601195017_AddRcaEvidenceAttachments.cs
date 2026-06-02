using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRcaEvidenceAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentContentType",
                table: "rca_evidence",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "rca_evidence",
                type: "varchar(260)",
                maxLength: 260,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentSha256",
                table: "rca_evidence",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "AttachmentSizeBytes",
                table: "rca_evidence",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentStorageKey",
                table: "rca_evidence",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentStorageProvider",
                table: "rca_evidence",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentContentType",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "AttachmentSha256",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "AttachmentSizeBytes",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "AttachmentStorageKey",
                table: "rca_evidence");

            migrationBuilder.DropColumn(
                name: "AttachmentStorageProvider",
                table: "rca_evidence");
        }
    }
}
