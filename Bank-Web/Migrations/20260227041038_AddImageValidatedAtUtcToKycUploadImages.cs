using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddImageValidatedAtUtcToKycUploadImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImageValidatedAtUtc",
                table: "KYCUpload_Images",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageValidatedAtUtc",
                table: "KYCUpload_Images");
        }
    }
}
