using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDedupeFlagsToKycUploadDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DedupeCheckedAtUtc",
                table: "KYCUpload_Details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DedupeExecuted",
                table: "KYCUpload_Details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DedupeMessage",
                table: "KYCUpload_Details",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DedupePassed",
                table: "KYCUpload_Details",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DedupeCheckedAtUtc",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "DedupeExecuted",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "DedupeMessage",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "DedupePassed",
                table: "KYCUpload_Details");
        }
    }
}
