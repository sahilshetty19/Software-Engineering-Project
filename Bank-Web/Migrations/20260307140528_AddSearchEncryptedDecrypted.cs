using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchEncryptedDecrypted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SearchResponseDecrypted",
                table: "SearchResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "CipherAlgorithm",
                table: "SearchResponseEncrypted");

            migrationBuilder.DropColumn(
                name: "CiphertextBase64",
                table: "SearchResponseEncrypted");

            migrationBuilder.DropColumn(
                name: "KeyId",
                table: "SearchResponseEncrypted");

            migrationBuilder.DropColumn(
                name: "RequestRef",
                table: "SearchResponseEncrypted");

            migrationBuilder.DropColumn(
                name: "MatchScore",
                table: "SearchResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "MatchedCkycNumber",
                table: "SearchResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "SearchResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "RequestRef",
                table: "SearchResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "SearchStatus",
                table: "SearchResponseDecrypted");

            migrationBuilder.RenameColumn(
                name: "ReceivedAtUtc",
                table: "SearchResponseEncrypted",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "IvBase64",
                table: "SearchResponseEncrypted",
                newName: "ResponseHashSha256");

            migrationBuilder.RenameColumn(
                name: "SearchRespEncId",
                table: "SearchResponseEncrypted",
                newName: "SearchResponseEncryptedId");

            migrationBuilder.RenameColumn(
                name: "DecryptedAtUtc",
                table: "SearchResponseDecrypted",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "SearchRespDecId",
                table: "SearchResponseDecrypted",
                newName: "SearchResponseEncryptedId");

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedRequestBytes",
                table: "SearchResponseEncrypted",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedResponseBytes",
                table: "SearchResponseEncrypted",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "RequestHashSha256",
                table: "SearchResponseEncrypted",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SearchResponseDecryptedId",
                table: "SearchResponseDecrypted",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DecryptedJson",
                table: "SearchResponseDecrypted",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SearchResponseDecrypted",
                table: "SearchResponseDecrypted",
                column: "SearchResponseDecryptedId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchResponseDecrypted_SearchResponseEncryptedId",
                table: "SearchResponseDecrypted",
                column: "SearchResponseEncryptedId");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchResponseDecrypted_SearchResponseEncrypted_SearchRespo~",
                table: "SearchResponseDecrypted",
                column: "SearchResponseEncryptedId",
                principalTable: "SearchResponseEncrypted",
                principalColumn: "SearchResponseEncryptedId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchResponseDecrypted_SearchResponseEncrypted_SearchRespo~",
                table: "SearchResponseDecrypted");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SearchResponseDecrypted",
                table: "SearchResponseDecrypted");

            migrationBuilder.DropIndex(
                name: "IX_SearchResponseDecrypted_SearchResponseEncryptedId",
                table: "SearchResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "EncryptedRequestBytes",
                table: "SearchResponseEncrypted");

            migrationBuilder.DropColumn(
                name: "EncryptedResponseBytes",
                table: "SearchResponseEncrypted");

            migrationBuilder.DropColumn(
                name: "RequestHashSha256",
                table: "SearchResponseEncrypted");

            migrationBuilder.DropColumn(
                name: "SearchResponseDecryptedId",
                table: "SearchResponseDecrypted");

            migrationBuilder.DropColumn(
                name: "DecryptedJson",
                table: "SearchResponseDecrypted");

            migrationBuilder.RenameColumn(
                name: "ResponseHashSha256",
                table: "SearchResponseEncrypted",
                newName: "IvBase64");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "SearchResponseEncrypted",
                newName: "ReceivedAtUtc");

            migrationBuilder.RenameColumn(
                name: "SearchResponseEncryptedId",
                table: "SearchResponseEncrypted",
                newName: "SearchRespEncId");

            migrationBuilder.RenameColumn(
                name: "SearchResponseEncryptedId",
                table: "SearchResponseDecrypted",
                newName: "SearchRespDecId");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "SearchResponseDecrypted",
                newName: "DecryptedAtUtc");

            migrationBuilder.AddColumn<string>(
                name: "CipherAlgorithm",
                table: "SearchResponseEncrypted",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CiphertextBase64",
                table: "SearchResponseEncrypted",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KeyId",
                table: "SearchResponseEncrypted",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestRef",
                table: "SearchResponseEncrypted",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "MatchScore",
                table: "SearchResponseDecrypted",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchedCkycNumber",
                table: "SearchResponseDecrypted",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "SearchResponseDecrypted",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestRef",
                table: "SearchResponseDecrypted",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<short>(
                name: "SearchStatus",
                table: "SearchResponseDecrypted",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SearchResponseDecrypted",
                table: "SearchResponseDecrypted",
                column: "SearchRespDecId");
        }
    }
}
