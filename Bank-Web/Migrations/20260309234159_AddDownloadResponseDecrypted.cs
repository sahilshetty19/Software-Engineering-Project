using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadResponseDecrypted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadStatus",
                table: "DownloadResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "DownloadResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "RequestRef",
                table: "DownloadResponseDecrypted");

            migrationBuilder.RenameColumn(
                name: "DecryptedAtUtc",
                table: "DownloadResponseDecrypted",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "DownloadRespDecId",
                table: "DownloadResponseDecrypted",
                newName: "DownloadResponseDecryptedId");

            migrationBuilder.AddColumn<string>(
                name: "ResponseHashSha256",
                table: "DownloadResponseDecrypted",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponseJson",
                table: "DownloadResponseDecrypted",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseHashSha256",
                table: "DownloadResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "ResponseJson",
                table: "DownloadResponseDecrypted");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "DownloadResponseDecrypted",
                newName: "DecryptedAtUtc");

            migrationBuilder.RenameColumn(
                name: "DownloadResponseDecryptedId",
                table: "DownloadResponseDecrypted",
                newName: "DownloadRespDecId");

            migrationBuilder.AddColumn<short>(
                name: "DownloadStatus",
                table: "DownloadResponseDecrypted",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "DownloadResponseDecrypted",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestRef",
                table: "DownloadResponseDecrypted",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }
    }
}
