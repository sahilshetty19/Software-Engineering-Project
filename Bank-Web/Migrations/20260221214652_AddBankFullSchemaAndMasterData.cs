using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBankFullSchemaAndMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankCustomers");

            migrationBuilder.CreateTable(
                name: "County",
                columns: table => new
                {
                    CountyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_County", x => x.CountyId);
                });

            migrationBuilder.CreateTable(
                name: "City",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.CityId);
                    table.ForeignKey(
                        name: "FK_City_County_CountyId",
                        column: x => x.CountyId,
                        principalTable: "County",
                        principalColumn: "CountyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KYCUpload_Details",
                columns: table => new
                {
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestRef = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Source = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    OcrStatus = table.Column<short>(type: "smallint", nullable: false),
                    ValidationStatus = table.Column<short>(type: "smallint", nullable: false),
                    DedupeStatus = table.Column<short>(type: "smallint", nullable: false),
                    IdentityHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CkycNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Nationality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PPSN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Eircode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Occupation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceOfFunds = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsPEP = table.Column<bool>(type: "boolean", nullable: false),
                    RiskRating = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KYCUpload_Details", x => x.KycUploadId);
                    table.ForeignKey(
                        name: "FK_KYCUpload_Details_City_CityId",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "CityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KYCUpload_Details_County_CountyId",
                        column: x => x.CountyId,
                        principalTable: "County",
                        principalColumn: "CountyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankCustomerDetails",
                columns: table => new
                {
                    BankCustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CkycNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Nationality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PPSN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Eircode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Occupation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceOfFunds = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsPEP = table.Column<bool>(type: "boolean", nullable: false),
                    RiskRating = table.Column<short>(type: "smallint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCustomerDetails", x => x.BankCustomerId);
                    table.ForeignKey(
                        name: "FK_BankCustomerDetails_City_CityId",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "CityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankCustomerDetails_County_CountyId",
                        column: x => x.CountyId,
                        principalTable: "County",
                        principalColumn: "CountyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankCustomerDetails_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DownloadResponseDecrypted",
                columns: table => new
                {
                    DownloadRespDecId = table.Column<Guid>(type: "uuid", nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestRef = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CkycNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DownloadStatus = table.Column<short>(type: "smallint", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    DecryptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadResponseDecrypted", x => x.DownloadRespDecId);
                    table.ForeignKey(
                        name: "FK_DownloadResponseDecrypted_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DownloadResponseEncrypted",
                columns: table => new
                {
                    DownloadRespEncId = table.Column<Guid>(type: "uuid", nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestRef = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CipherAlgorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IvBase64 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CiphertextBase64 = table.Column<string>(type: "text", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadResponseEncrypted", x => x.DownloadRespEncId);
                    table.ForeignKey(
                        name: "FK_DownloadResponseEncrypted_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KYCUpdationResponse",
                columns: table => new
                {
                    KycUpdateRespId = table.Column<Guid>(type: "uuid", nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestRef = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CkycNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdateStatus = table.Column<short>(type: "smallint", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KYCUpdationResponse", x => x.KycUpdateRespId);
                    table.ForeignKey(
                        name: "FK_KYCUpdationResponse_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KYCUpload_Images",
                columns: table => new
                {
                    KycUploadImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<short>(type: "smallint", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ImageBytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    OcrText = table.Column<string>(type: "text", nullable: false),
                    OcrConfidence = table.Column<float>(type: "real", nullable: false),
                    ExtractedDocNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExtractedExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KYCUpload_Images", x => x.KycUploadImageId);
                    table.ForeignKey(
                        name: "FK_KYCUpload_Images_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchResponseDecrypted",
                columns: table => new
                {
                    SearchRespDecId = table.Column<Guid>(type: "uuid", nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestRef = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    SearchStatus = table.Column<short>(type: "smallint", nullable: false),
                    MatchedCkycNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MatchScore = table.Column<float>(type: "real", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    DecryptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchResponseDecrypted", x => x.SearchRespDecId);
                    table.ForeignKey(
                        name: "FK_SearchResponseDecrypted_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchResponseEncrypted",
                columns: table => new
                {
                    SearchRespEncId = table.Column<Guid>(type: "uuid", nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestRef = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CipherAlgorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IvBase64 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CiphertextBase64 = table.Column<string>(type: "text", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchResponseEncrypted", x => x.SearchRespEncId);
                    table.ForeignKey(
                        name: "FK_SearchResponseEncrypted_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ZipFileUploadDetails",
                columns: table => new
                {
                    ZipUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZipFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ZipHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ZipSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SftpRemotePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadStatus = table.Column<short>(type: "smallint", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZipFileUploadDetails", x => x.ZipUploadId);
                    table.ForeignKey(
                        name: "FK_ZipFileUploadDetails_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerImages",
                columns: table => new
                {
                    CustomerImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankCustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<short>(type: "smallint", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ImageBytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    StoredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerImages", x => x.CustomerImageId);
                    table.ForeignKey(
                        name: "FK_CustomerImages_BankCustomerDetails_BankCustomerId",
                        column: x => x.BankCustomerId,
                        principalTable: "BankCustomerDetails",
                        principalColumn: "BankCustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankCustomerDetails_CityId",
                table: "BankCustomerDetails",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_BankCustomerDetails_CountyId",
                table: "BankCustomerDetails",
                column: "CountyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankCustomerDetails_KycUploadId",
                table: "BankCustomerDetails",
                column: "KycUploadId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_CountyId_CityName",
                table: "City",
                columns: new[] { "CountyId", "CityName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_County_CountyName",
                table: "County",
                column: "CountyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerImages_BankCustomerId",
                table: "CustomerImages",
                column: "BankCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerImages_FileHashSha256",
                table: "CustomerImages",
                column: "FileHashSha256");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadResponseDecrypted_KycUploadId",
                table: "DownloadResponseDecrypted",
                column: "KycUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadResponseEncrypted_KycUploadId",
                table: "DownloadResponseEncrypted",
                column: "KycUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_KYCUpdationResponse_KycUploadId",
                table: "KYCUpdationResponse",
                column: "KycUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_KYCUpload_Details_CityId",
                table: "KYCUpload_Details",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_KYCUpload_Details_CountyId",
                table: "KYCUpload_Details",
                column: "CountyId");

            migrationBuilder.CreateIndex(
                name: "IX_KYCUpload_Details_IdentityHash",
                table: "KYCUpload_Details",
                column: "IdentityHash");

            migrationBuilder.CreateIndex(
                name: "IX_KYCUpload_Details_RequestRef",
                table: "KYCUpload_Details",
                column: "RequestRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KYCUpload_Images_FileHashSha256",
                table: "KYCUpload_Images",
                column: "FileHashSha256");

            migrationBuilder.CreateIndex(
                name: "IX_KYCUpload_Images_KycUploadId",
                table: "KYCUpload_Images",
                column: "KycUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchResponseDecrypted_KycUploadId",
                table: "SearchResponseDecrypted",
                column: "KycUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchResponseEncrypted_KycUploadId",
                table: "SearchResponseEncrypted",
                column: "KycUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_ZipFileUploadDetails_KycUploadId",
                table: "ZipFileUploadDetails",
                column: "KycUploadId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerImages");

            migrationBuilder.DropTable(
                name: "DownloadResponseDecrypted");

            migrationBuilder.DropTable(
                name: "DownloadResponseEncrypted");

            migrationBuilder.DropTable(
                name: "KYCUpdationResponse");

            migrationBuilder.DropTable(
                name: "KYCUpload_Images");

            migrationBuilder.DropTable(
                name: "SearchResponseDecrypted");

            migrationBuilder.DropTable(
                name: "SearchResponseEncrypted");

            migrationBuilder.DropTable(
                name: "ZipFileUploadDetails");

            migrationBuilder.DropTable(
                name: "BankCustomerDetails");

            migrationBuilder.DropTable(
                name: "KYCUpload_Details");

            migrationBuilder.DropTable(
                name: "City");

            migrationBuilder.DropTable(
                name: "County");

            migrationBuilder.CreateTable(
                name: "BankCustomers",
                columns: table => new
                {
                    BankCustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    County = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Eircode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPEP = table.Column<bool>(type: "boolean", nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nationality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Occupation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PPSN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RiskRating = table.Column<int>(type: "integer", nullable: false),
                    SourceOfFunds = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCustomers", x => x.BankCustomerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankCustomers_FirstName_LastName_DateOfBirth",
                table: "BankCustomers",
                columns: new[] { "FirstName", "LastName", "DateOfBirth" });

            migrationBuilder.CreateIndex(
                name: "IX_BankCustomers_PPSN",
                table: "BankCustomers",
                column: "PPSN",
                unique: true);
        }
    }
}
