using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationRetryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AutomationLockedUntilUtc",
                table: "KYCUpload_Details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "AutomationStatus",
                table: "KYCUpload_Details",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAutomationCompletedAtUtc",
                table: "KYCUpload_Details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastAutomationError",
                table: "KYCUpload_Details",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAutomationStartedAtUtc",
                table: "KYCUpload_Details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastFailedStep",
                table: "KYCUpload_Details",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetryAttempts",
                table: "KYCUpload_Details",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRetryAtUtc",
                table: "KYCUpload_Details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryAttemptCount",
                table: "KYCUpload_Details",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_KYCUpload_Details_AutomationLockedUntilUtc",
                table: "KYCUpload_Details",
                column: "AutomationLockedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_KYCUpload_Details_AutomationStatus_NextRetryAtUtc",
                table: "KYCUpload_Details",
                columns: new[] { "AutomationStatus", "NextRetryAtUtc" });

            migrationBuilder.Sql(
                """
                UPDATE "KYCUpload_Details"
                SET "MaxRetryAttempts" = 5
                WHERE "MaxRetryAttempts" = 0;

                UPDATE "KYCUpload_Details"
                SET
                    "AutomationStatus" = CASE
                        WHEN "Status" IN (5, 7) THEN 3
                        WHEN "Status" = 4 THEN 2
                        WHEN "Status" = 6 THEN 4
                        ELSE 0
                    END,
                    "NextRetryAtUtc" = CASE
                        WHEN "Status" = 4 THEN NOW()
                        ELSE NULL
                    END,
                    "LastFailedStep" = CASE
                        WHEN "Status" = 6 AND COALESCE("LastFailedStep", '') = '' THEN 'Automation'
                        ELSE "LastFailedStep"
                    END,
                    "LastAutomationError" = CASE
                        WHEN "Status" = 6 AND COALESCE("LastAutomationError", '') = '' THEN COALESCE("FailureReason", 'Previous automation attempt failed.')
                        ELSE "LastAutomationError"
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KYCUpload_Details_AutomationLockedUntilUtc",
                table: "KYCUpload_Details");

            migrationBuilder.DropIndex(
                name: "IX_KYCUpload_Details_AutomationStatus_NextRetryAtUtc",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "AutomationLockedUntilUtc",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "AutomationStatus",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "LastAutomationCompletedAtUtc",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "LastAutomationError",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "LastAutomationStartedAtUtc",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "LastFailedStep",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "MaxRetryAttempts",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "RetryAttemptCount",
                table: "KYCUpload_Details");
        }
    }
}
