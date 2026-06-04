using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRcaFactIndustrialClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlarmCode",
                table: "rca_facts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BatchOrLot",
                table: "rca_facts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FactSeverity",
                table: "rca_facts",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Info")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LineCode",
                table: "rca_facts",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MachineCode",
                table: "rca_facts",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MaterialCode",
                table: "rca_facts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MeasurementName",
                table: "rca_facts",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MeasurementUnit",
                table: "rca_facts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MeasurementValue",
                table: "rca_facts",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShiftCode",
                table: "rca_facts",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderCode",
                table: "rca_facts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_AlarmCode",
                table: "rca_facts",
                columns: new[] { "TenantId", "AlarmCode" });

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_FactSeverity",
                table: "rca_facts",
                columns: new[] { "TenantId", "FactSeverity" });

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_MachineCode_OccurredAt",
                table: "rca_facts",
                columns: new[] { "TenantId", "MachineCode", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_rca_facts_TenantId_ShiftCode_OccurredAt",
                table: "rca_facts",
                columns: new[] { "TenantId", "ShiftCode", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rca_facts_TenantId_AlarmCode",
                table: "rca_facts");

            migrationBuilder.DropIndex(
                name: "IX_rca_facts_TenantId_FactSeverity",
                table: "rca_facts");

            migrationBuilder.DropIndex(
                name: "IX_rca_facts_TenantId_MachineCode_OccurredAt",
                table: "rca_facts");

            migrationBuilder.DropIndex(
                name: "IX_rca_facts_TenantId_ShiftCode_OccurredAt",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "AlarmCode",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "BatchOrLot",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "FactSeverity",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "LineCode",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "MachineCode",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "MaterialCode",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "MeasurementName",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "MeasurementUnit",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "MeasurementValue",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "ShiftCode",
                table: "rca_facts");

            migrationBuilder.DropColumn(
                name: "WorkOrderCode",
                table: "rca_facts");
        }
    }
}
