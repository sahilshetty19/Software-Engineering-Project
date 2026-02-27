using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddImageValidationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageValidationMessage",
                table: "KYCUpload_Images",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImageValidated",
                table: "KYCUpload_Images",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageValidationMessage",
                table: "KYCUpload_Images");

            migrationBuilder.DropColumn(
                name: "IsImageValidated",
                table: "KYCUpload_Images");
        }
    }
}
