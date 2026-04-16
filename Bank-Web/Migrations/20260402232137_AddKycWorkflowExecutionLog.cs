using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddKycWorkflowExecutionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KycWorkflowExecutionLog",
                columns: table => new
                {
                    KycWorkflowExecutionLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    KycUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    ErrorDetails = table.Column<string>(type: "text", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycWorkflowExecutionLog", x => x.KycWorkflowExecutionLogId);
                    table.ForeignKey(
                        name: "FK_KycWorkflowExecutionLog_KYCUpload_Details_KycUploadId",
                        column: x => x.KycUploadId,
                        principalTable: "KYCUpload_Details",
                        principalColumn: "KycUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KycWorkflowExecutionLog_KycUploadId",
                table: "KycWorkflowExecutionLog",
                column: "KycUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_KycWorkflowExecutionLog_KycUploadId_StartedAtUtc",
                table: "KycWorkflowExecutionLog",
                columns: new[] { "KycUploadId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KycWorkflowExecutionLog");
        }
    }
}
