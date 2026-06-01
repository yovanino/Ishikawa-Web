using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIntakeRejection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectedAt",
                table: "rca_external_intake_requests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedByUserId",
                table: "rca_external_intake_requests",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "rca_external_intake_requests",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "rca_external_intake_requests");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "rca_external_intake_requests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "rca_external_intake_requests");
        }
    }
}
