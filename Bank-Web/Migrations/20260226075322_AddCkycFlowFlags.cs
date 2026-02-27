using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCkycFlowFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CkycDownloadedAtUtc",
                table: "KYCUpload_Details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SearchExecuted",
                table: "KYCUpload_Details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SearchFound",
                table: "KYCUpload_Details",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CkycDownloadedAtUtc",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "SearchExecuted",
                table: "KYCUpload_Details");

            migrationBuilder.DropColumn(
                name: "SearchFound",
                table: "KYCUpload_Details");
        }
    }
}
