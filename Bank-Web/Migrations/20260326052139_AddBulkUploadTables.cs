using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkUploadTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkUploadBatch",
                columns: table => new
                {
                    BulkUploadBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessRows = table.Column<int>(type: "integer", nullable: false),
                    FailedRows = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkUploadBatch", x => x.BulkUploadBatchId);
                });

            migrationBuilder.CreateTable(
                name: "BulkUploadRowResult",
                columns: table => new
                {
                    BulkUploadRowResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    BulkUploadBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    RowRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkUploadRowResult", x => x.BulkUploadRowResultId);
                    table.ForeignKey(
                        name: "FK_BulkUploadRowResult_BulkUploadBatch_BulkUploadBatchId",
                        column: x => x.BulkUploadBatchId,
                        principalTable: "BulkUploadBatch",
                        principalColumn: "BulkUploadBatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BulkUploadRowResult_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkUploadBatch_UploadedAtUtc",
                table: "BulkUploadBatch",
                column: "UploadedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BulkUploadRowResult_BulkUploadBatchId",
                table: "BulkUploadRowResult",
                column: "BulkUploadBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkUploadRowResult_BulkUploadBatchId_RowRef",
                table: "BulkUploadRowResult",
                columns: new[] { "BulkUploadBatchId", "RowRef" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BulkUploadRowResult_KycUploadId",
                table: "BulkUploadRowResult",
                column: "KycUploadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkUploadRowResult");

            migrationBuilder.DropTable(
                name: "BulkUploadBatch");
        }
    }
}
