using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IshikawaRca.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRcaWizardProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AddColumnIfMissing("WizardStep", "`WizardStep` varchar(32) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Problem'"));
            migrationBuilder.Sql(AddColumnIfMissing("WizardStepCompletedAt", "`WizardStepCompletedAt` datetime(6) NULL"));
            migrationBuilder.Sql(AddColumnIfMissing("WizardStepCompletedByUserId", "`WizardStepCompletedByUserId` varchar(160) CHARACTER SET utf8mb4 NULL"));
            migrationBuilder.Sql(AddColumnIfMissing("WizardStepNotes", "`WizardStepNotes` text CHARACTER SET utf8mb4 NULL"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DropColumnIfExists("WizardStepNotes"));
            migrationBuilder.Sql(DropColumnIfExists("WizardStepCompletedByUserId"));
            migrationBuilder.Sql(DropColumnIfExists("WizardStepCompletedAt"));
            migrationBuilder.Sql(DropColumnIfExists("WizardStep"));
        }

        private static string AddColumnIfMissing(string columnName, string columnDefinition)
        {
            var escapedColumnDefinition = columnDefinition.Replace("'", "''");

            return $@"
SET @column_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'rca_incidents'
      AND COLUMN_NAME = '{columnName}'
);
SET @ddl = IF(@column_exists = 0, 'ALTER TABLE `rca_incidents` ADD COLUMN {escapedColumnDefinition};', 'DO 0;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;";
        }

        private static string DropColumnIfExists(string columnName)
        {
            return $@"
SET @column_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'rca_incidents'
      AND COLUMN_NAME = '{columnName}'
);
SET @ddl = IF(@column_exists = 1, 'ALTER TABLE `rca_incidents` DROP COLUMN `{columnName}`;', 'DO 0;');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;";
        }
    }
}
