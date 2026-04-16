using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class ExpandKycUpdationResponsePayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "KYCUpdationResponse",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "KYCUpdationResponse",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponseHashSha256",
                table: "KYCUpdationResponse",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseJson",
                table: "KYCUpdationResponse",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponseType",
                table: "KYCUpdationResponse",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "KYCUpdationResponse");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "KYCUpdationResponse");

            migrationBuilder.DropColumn(
                name: "ResponseHashSha256",
                table: "KYCUpdationResponse");

            migrationBuilder.DropColumn(
                name: "ResponseJson",
                table: "KYCUpdationResponse");

            migrationBuilder.DropColumn(
                name: "ResponseType",
                table: "KYCUpdationResponse");
        }
    }
}
