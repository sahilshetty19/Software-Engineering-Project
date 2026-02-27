using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CKYC.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionStatusMessageAndCkycNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CkycNumber",
                table: "InboundSubmissions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "InboundSubmissions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CkycNumber",
                table: "InboundSubmissions");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "InboundSubmissions");
        }
    }
}
