using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CKYC.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCkycDocumentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivedPath",
                table: "InboundPackages");

            migrationBuilder.DropColumn(
                name: "StorageRef",
                table: "InboundPackages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CkycProfiles");

            migrationBuilder.DropColumn(
                name: "StorageRef",
                table: "CkycDocuments");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "CkycDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedAtUtc",
                table: "CkycDocuments");

            migrationBuilder.RenameColumn(
                name: "ZipSizeBytes",
                table: "InboundPackages",
                newName: "FileSizeBytes");

            migrationBuilder.RenameColumn(
                name: "ZipHashSha256",
                table: "InboundPackages",
                newName: "FileHashSha256");

            migrationBuilder.RenameColumn(
                name: "ZipFileName",
                table: "InboundPackages",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "StoredAtUtc",
                table: "InboundPackages",
                newName: "UploadedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "CkycDocuments",
                newName: "UploadedAtUtc");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "InboundPackages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "ZipBytes",
                table: "InboundPackages",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "CkycProfiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PPSN",
                table: "CkycProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "OcrText",
                table: "CkycDocuments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<float>(
                name: "OcrConfidence",
                table: "CkycDocuments",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<string>(
                name: "ExtractedDocNumber",
                table: "CkycDocuments",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<byte[]>(
                name: "FileBytes",
                table: "CkycDocuments",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_CkycProfiles_FirstName_LastName_DateOfBirth",
                table: "CkycProfiles",
                columns: new[] { "FirstName", "LastName", "DateOfBirth" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CkycProfiles_FirstName_LastName_DateOfBirth",
                table: "CkycProfiles");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "InboundPackages");

            migrationBuilder.DropColumn(
                name: "ZipBytes",
                table: "InboundPackages");

            migrationBuilder.DropColumn(
                name: "PPSN",
                table: "CkycProfiles");

            migrationBuilder.DropColumn(
                name: "FileBytes",
                table: "CkycDocuments");

            migrationBuilder.RenameColumn(
                name: "UploadedAtUtc",
                table: "InboundPackages",
                newName: "StoredAtUtc");

            migrationBuilder.RenameColumn(
                name: "FileSizeBytes",
                table: "InboundPackages",
                newName: "ZipSizeBytes");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "InboundPackages",
                newName: "ZipFileName");

            migrationBuilder.RenameColumn(
                name: "FileHashSha256",
                table: "InboundPackages",
                newName: "ZipHashSha256");

            migrationBuilder.RenameColumn(
                name: "UploadedAtUtc",
                table: "CkycDocuments",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<string>(
                name: "ReceivedPath",
                table: "InboundPackages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StorageRef",
                table: "InboundPackages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "CkycProfiles",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<short>(
                name: "Status",
                table: "CkycProfiles",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AlterColumn<string>(
                name: "OcrText",
                table: "CkycDocuments",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "OcrConfidence",
                table: "CkycDocuments",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExtractedDocNumber",
                table: "CkycDocuments",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageRef",
                table: "CkycDocuments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<short>(
                name: "VerificationStatus",
                table: "CkycDocuments",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAtUtc",
                table: "CkycDocuments",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
