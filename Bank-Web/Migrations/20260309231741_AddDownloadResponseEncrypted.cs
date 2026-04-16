using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadResponseEncrypted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CipherAlgorithm",
                table: "DownloadResponseEncrypted");

            migrationBuilder.DropColumn(
                name: "CiphertextBase64",
                table: "DownloadResponseEncrypted");

            migrationBuilder.DropColumn(
                name: "RequestRef",
                table: "DownloadResponseEncrypted");

            migrationBuilder.RenameColumn(
                name: "ReceivedAtUtc",
                table: "DownloadResponseEncrypted",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "KeyId",
                table: "DownloadResponseEncrypted",
                newName: "CkycNumber");

            migrationBuilder.RenameColumn(
                name: "IvBase64",
                table: "DownloadResponseEncrypted",
                newName: "ResponseHashSha256");

            migrationBuilder.RenameColumn(
                name: "DownloadRespEncId",
                table: "DownloadResponseEncrypted",
                newName: "DownloadResponseEncryptedId");

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedResponseBytes",
                table: "DownloadResponseEncrypted",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedResponseBytes",
                table: "DownloadResponseEncrypted");

            migrationBuilder.RenameColumn(
                name: "ResponseHashSha256",
                table: "DownloadResponseEncrypted",
                newName: "IvBase64");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "DownloadResponseEncrypted",
                newName: "ReceivedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CkycNumber",
                table: "DownloadResponseEncrypted",
                newName: "KeyId");

            migrationBuilder.RenameColumn(
                name: "DownloadResponseEncryptedId",
                table: "DownloadResponseEncrypted",
                newName: "DownloadRespEncId");

            migrationBuilder.AddColumn<string>(
                name: "CipherAlgorithm",
                table: "DownloadResponseEncrypted",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CiphertextBase64",
                table: "DownloadResponseEncrypted",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestRef",
                table: "DownloadResponseEncrypted",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }
    }
}
