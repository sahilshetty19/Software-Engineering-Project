using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CKYC.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitCkycSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    AuditEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorType = table.Column<short>(type: "smallint", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestRef = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Outcome = table.Column<short>(type: "smallint", nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.AuditEventId);
                });

            migrationBuilder.CreateTable(
                name: "CkycProfiles",
                columns: table => new
                {
                    CkycProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CkycNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Nationality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    County = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Eircode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Occupation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceOfFunds = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsPEP = table.Column<bool>(type: "boolean", nullable: false),
                    RiskRating = table.Column<short>(type: "smallint", nullable: false),
                    IdentityHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CkycProfiles", x => x.CkycProfileId);
                });

            migrationBuilder.CreateTable(
                name: "CkycDocuments",
                columns: table => new
                {
                    CkycDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CkycProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<short>(type: "smallint", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OcrText = table.Column<string>(type: "text", nullable: false),
                    OcrConfidence = table.Column<float>(type: "real", nullable: false),
                    ExtractedDocNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExtractedExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    VerificationStatus = table.Column<short>(type: "smallint", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CkycDocuments", x => x.CkycDocumentId);
                    table.ForeignKey(
                        name: "FK_CkycDocuments_CkycProfiles_CkycProfileId",
                        column: x => x.CkycProfileId,
                        principalTable: "CkycProfiles",
                        principalColumn: "CkycProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InboundSubmissions",
                columns: table => new
                {
                    InboundSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequestRef = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LinkedCkycProfileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundSubmissions", x => x.InboundSubmissionId);
                    table.ForeignKey(
                        name: "FK_InboundSubmissions_CkycProfiles_LinkedCkycProfileId",
                        column: x => x.LinkedCkycProfileId,
                        principalTable: "CkycProfiles",
                        principalColumn: "CkycProfileId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InboundFiles",
                columns: table => new
                {
                    InboundFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    InboundSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileRole = table.Column<short>(type: "smallint", nullable: false),
                    ExtractedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LinkedCkycDocumentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundFiles", x => x.InboundFileId);
                    table.ForeignKey(
                        name: "FK_InboundFiles_CkycDocuments_LinkedCkycDocumentId",
                        column: x => x.LinkedCkycDocumentId,
                        principalTable: "CkycDocuments",
                        principalColumn: "CkycDocumentId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InboundFiles_InboundSubmissions_InboundSubmissionId",
                        column: x => x.InboundSubmissionId,
                        principalTable: "InboundSubmissions",
                        principalColumn: "InboundSubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InboundPackages",
                columns: table => new
                {
                    InboundPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    InboundSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZipFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ZipHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ZipSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReceivedPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StoredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundPackages", x => x.InboundPackageId);
                    table.ForeignKey(
                        name: "FK_InboundPackages_InboundSubmissions_InboundSubmissionId",
                        column: x => x.InboundSubmissionId,
                        principalTable: "InboundSubmissions",
                        principalColumn: "InboundSubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Action",
                table: "AuditEvents",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EntityType",
                table: "AuditEvents",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EventTimeUtc",
                table: "AuditEvents",
                column: "EventTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_RequestRef",
                table: "AuditEvents",
                column: "RequestRef");

            migrationBuilder.CreateIndex(
                name: "IX_CkycDocuments_CkycProfileId",
                table: "CkycDocuments",
                column: "CkycProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CkycDocuments_DocumentType",
                table: "CkycDocuments",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_CkycDocuments_FileHashSha256",
                table: "CkycDocuments",
                column: "FileHashSha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CkycProfiles_CkycNumber",
                table: "CkycProfiles",
                column: "CkycNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CkycProfiles_IdentityHash",
                table: "CkycProfiles",
                column: "IdentityHash");

            migrationBuilder.CreateIndex(
                name: "IX_InboundFiles_FileHashSha256",
                table: "InboundFiles",
                column: "FileHashSha256");

            migrationBuilder.CreateIndex(
                name: "IX_InboundFiles_InboundSubmissionId",
                table: "InboundFiles",
                column: "InboundSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundFiles_LinkedCkycDocumentId",
                table: "InboundFiles",
                column: "LinkedCkycDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundPackages_InboundSubmissionId",
                table: "InboundPackages",
                column: "InboundSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundSubmissions_LinkedCkycProfileId",
                table: "InboundSubmissions",
                column: "LinkedCkycProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundSubmissions_RequestRef",
                table: "InboundSubmissions",
                column: "RequestRef",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "InboundFiles");

            migrationBuilder.DropTable(
                name: "InboundPackages");

            migrationBuilder.DropTable(
                name: "CkycDocuments");

            migrationBuilder.DropTable(
                name: "InboundSubmissions");

            migrationBuilder.DropTable(
                name: "CkycProfiles");
        }
    }
}
